# Architecture — Branch 1.1

## Sprint Goal

Prove the core data flow works end to end, without an orchestration layer yet:

Simulated station telemetry → MCP server (read/write tools) → SQL Server 2025 Express (via EF Core) → manual on-screen request displays the latest reading.

The MAF orchestrator is deliberately deferred to a later sprint. This sprint isolates one unknown — does the MCP tool layer work correctly — rather than proving MCP and orchestration together. If something breaks in the next sprint, it will be attributable to orchestration, not to the tools underneath it.

## Explicit Non-Goals for This Slice

- No MAF orchestrator — the calling client invokes the MCP server's tools directly
- No real bottleneck/constraint logic or AI-assisted reasoning
- No RAG or recommendation engine
- No escalation routing (Java/Spring domain untouched this sprint)
- No authentication/authorization (planned for the following sprint)
- No push/real-time updates — SignalR integration is deferred; reads are manual/on-request only
- No process routing table (station-to-workflow dependency graph) — needed before Phase 3 constraint analysis is meaningful, not before this sprint
- No compaction job — shift-based compaction and 90-day retention are a fast-follow once read/write is proven

## Domain Model (This Sprint)

**`Station`** — persisted entity, the machine-level record. `StationId` is the primary key and doubles as the correlation key for logging/tracing across the system.

- `StationId` (string, PK)
- `Status` (enum: Operational, Idle, Faulted, etc.) — purely descriptive, last known state
- `LastSensorReading` (DateTime?) — machine health signal
- `LastOperatorAction` (DateTime?) — presence/staffing signal
- `ActiveInstanceId` (string?) — which generator instance currently holds the write lease
- `LeaseExpiresAt` (DateTime?) — liveness lease, separate concern from `Status`

**`SensorReading`** — telemetry scoped to a parent station, not a duplicate of it.

- `SensorId`, `StationId` (FK), `ReadingType`, value, timestamp

**Uniqueness/identity is governed by a compound key**, not a flat ID check, since telemetry exists at multiple levels (machine, sensor, and potentially sub-component later):

```csharp
public readonly record struct TelemetryKey(string StationId, string TelemetryLevel, string TelemetryId);
public enum TelemetryLevel { Machine, Sensor, Component }
```

A boot-time audit builds a `HashSet<TelemetryKey>` across all configured generator instances and flags exact duplicates. Same `StationId` at different `TelemetryLevel`s (a machine and its sensors) is expected and not a conflict.

## Generator Design

- Station/sensor generators are **plain objects held in a `ConcurrentDictionary<TelemetryKey, ...>`**, not DI-managed services — this keeps them queryable for liveness/staleness checks without scope or resolution overhead, and avoids the EF Core captive-dependency problem (DbContext is Scoped by default; a Singleton or long-lived plain object must never hold one for its full lifetime).
- A single **boot orchestrator** (`IHostedService`/`BackgroundService`) reads instance configs at startup, runs the uniqueness audit, and constructs instances via a **factory**.
- Each write uses `IServiceScopeFactory.CreateScope()` to resolve a fresh `DbContext` per operation — one scope per write, not one scope for a generator's lifetime.
- Telemetry generation logic is **adapted from the existing Yearly Yields generator pattern** rather than built new, since that pattern is already proven at smaller scale.

### Get-or-Create / Lease Semantics

On startup, a generator instance looks up its `TelemetryKey`:

- **No existing row** → create new. `Status = Operational`. Claim the lease (`ActiveInstanceId` = this generator's own stable `InstanceId`, `LeaseExpiresAt` = now + configured interval).
- **Existing row, lease expired or unclaimed** → safe takeover. This is the crash-recovery path: a generator restarting after a hard failure finds its own stale lease and reclaims it. **`Status` is inherited as-is** from the last recorded value (per the "duplicate the last recorded status" requirement) — lease state and status are independent concerns.
- **Existing row, lease still active under a *different* `InstanceId`** → genuine conflict. Throws a distinct `StationAlreadyOperationalException` (carrying the conflicting `TelemetryKey`), translated at the API layer to `409 Conflict` with a structured body the UI can act on (prompt: assign a new ID). This is an expected business outcome, not a system error, and should not be logged/alerted as one.

Each generator instance has its own stable `InstanceId` (GUID, assigned at construction, independent of `StationId`) so the lease can distinguish "this is me coming back" from "this is a different writer."

Race condition note: the get-or-create check-then-insert has a narrow race window (near-simultaneous startup attempts on the same key). `TelemetryKey` should carry a unique constraint at the database level regardless of the in-memory audit, with conflict-on-insert handled via retry/re-fetch.

## Definition of Done

A simulated station reading is generated by a generator instance, written via the MCP server's write tool, and persisted to SQL Server through EF Core. A manual on-screen request triggers the MCP server's read tool and displays the latest reading for a given station. The boot-time audit correctly rejects a configuration with a duplicate `TelemetryKey`. A generator instance that restarts reclaims its existing `Station` row (same `StationId`, inherited `Status`) rather than creating a duplicate. A second live instance attempting to claim an already-leased `TelemetryKey` receives a `409 Conflict`, not silent data corruption.

No orchestrator, no auth, no push updates, no compaction — all explicitly out of scope per above.

## Decisions Made This Sprint

- Deferred MAF orchestration to its own sprint to isolate the MCP tool layer as a single unknown.
- Chose plain objects in a `ConcurrentDictionary` over DI-managed services for generator instances, to avoid EF Core captive-dependency issues and keep the collection directly queryable for liveness checks.
- Separated `Status` (descriptive) from lease ownership (`ActiveInstanceId`/`LeaseExpiresAt`) after recognizing that conflating them would block legitimate crash-recovery restarts.
- Adopted a compound `(StationId, TelemetryLevel, TelemetryId)` key instead of a flat ID check, to support multi-level telemetry (machine vs. sensor vs. future sub-component) uniformly.
- Deferred process routing (station-to-workflow dependency graph) past this sprint, but flagged it as a prerequisite for Phase 3 constraint analysis to be meaningful once multiple part types share stations.

## Open Risks / Unknowns

- [Add as encountered]