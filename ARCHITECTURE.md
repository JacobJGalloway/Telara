# Architecture — Branch 1.2

## Carried Over from 1.1

None of the core data-flow goal below was actually built in 1.1 — that sprint instead landed auth (JWT + rolling refresh tokens, ahead of its original 1.2 schedule) and a `StationEquipmentReading` read query over GraphQL. The Sprint Goal, Domain Model, Generator Design, and Definition of Done sections below are the unmet 1.1 scope, carried forward as-is into 1.2 rather than rewritten, since they were never invalidated — just not started.

### What Was Actually Built in 1.1
- **Auth (JWT + rolling refresh tokens):** `User`, `Role`, `RefreshToken` domain entities; `TokenService` (`Telara.OpsApi/Auth/`) issues tokens; a GraphQL login mutation plus `AuthPayload`/`LoginInput`/`UserProfile` types expose it. Landed ahead of its original 1.2 schedule (see Auth/RBAC in `DECISIONS.md`).
- **`StationEquipmentReading`** (retired in 1.2): the 1.1 placeholder was a flat telemetry-reading entity — `Id`, `StationEquipmentId`, `ReadingDateTime`, plus nullable sensor fields (`BeltSpeed`, `BeltTemp`, `OilTemp`, `BladeSpeed`, `BladeTemp`, `MotorSpeed`, `MotorTemp`, `BeltVibration`) — with `StationEquipmentId` as a flat string, not FK'd to any real entity. Replaced by the actual `Station`/`StationEquipment`/`SensorReading` model below, now implemented in `Telara.Domain`: compound-keyed entities, `SensorReading` normalized to one row per reading (`ReadingType` as data, not a column) with a real FK down through `StationEquipment`. `GetStationEquipmentReadingsQuery`/Handler and the GraphQL field were replaced by `GetSensorReadingsQuery`/Handler and `Query.GetSensorReadings`.

## MCP Server Topology

**Resolved — moved to [`DECISIONS.md`](./DECISIONS.md).** Three servers split by workload shape: an Internal Functionality Server (no LLM, parameterized domain-data/data-slice lookups), a Claude Haiku server (linear, high-throughput/low-cost workflows), and a Claude Sonnet server (deep reasoning, houses RAG). Physical geography is single-machine through demo-stable, cloud-portable later.

**Still open, tracked here since it's implementation-scoped, not architectural:** exact tool inventory per server — which specific lookups/workflows belong to which of the three — pending as MCP implementation actually starts this sprint.

## Sprint Goal

Prove the core data flow works end to end, without an orchestration layer yet:

Simulated station telemetry → MCP server (read/write tools) → SQL Server 2025 Express (via EF Core) → manual on-screen request displays the latest reading. (confirmed not completed in 1.1).

Basic UI has added screens for viewing station workflow diagrams and a form for registering a new Station along with any Station Equipment to be added with the Station. Diagram should also have a breakdown below showing the last sensor readings of all viewed Sation Equipment.

The MAF orchestrator is deliberately deferred to a later sprint. This sprint isolates one unknown — does the MCP tool layer work correctly — rather than proving MCP and orchestration together. If something breaks in a later sprint, it will be attributable to orchestration, not to the tools underneath it.

## Explicit Non-Goals for This Slice

- No MAF orchestrator — the calling client invokes the MCP server's tools directly
- No real bottleneck/constraint logic or AI-assisted reasoning
- No RAG or recommendation engine
- No escalation routing (Java/Spring domain untouched this sprint)
- No push/real-time updates — SignalR integration is deferred; reads are manual/on-request only
- No process routing table (station-to-workflow dependency graph) — needed before Phase 3 constraint analysis is meaningful, not before this sprint
- No compaction job — shift-based compaction and 90-day retention are a fast-follow once read/write is proven

Authentication/authorization is no longer a non-goal — it landed in 1.1 (see Carried Over note above) and is already in place.

## Domain Model (This Sprint)

Three levels now, not two — `Station` is the physical location/cell, `StationEquipment` is the actual machine, `SensorReading` is a single telemetry point off a piece of equipment. (`Station` was previously described as "the machine-level record"; that was wrong once it became clear a station houses multiple pieces of equipment, potentially running in parallel — see TOC's parallel/sequential/combination requirement in `DECISIONS.md`.)

**`Station`** — persisted entity, the physical station/location, not a machine itself. `(FacilityId, StationId)` is the compound primary key; `StationId` doubles as the correlation key for logging/tracing across the system.

- `FacilityId` (string, PK) — stubbed for this sprint: single hardcoded/default value, no multi-facility logic reads it yet. Included in the key now, per [[project-stack-migration-pattern|the same avoid-a-retrofit reasoning]] used elsewhere, so Phase 5 multi-facility support (if it happens) doesn't require re-keying every table.
- `StationId` (string, PK)
- `LastOperatorAction` (DateTime?) — presence/staffing signal, stays station-level since staffing is about the location, not any one machine
- `IsOperational()` — **behavior, not stored state.** Returns true only if every `StationEquipment` row at this station reports `Status == Operational`. Deliberately not persisted as its own column — it's a rollup of equipment-level truth, not a separate fact that could drift out of sync with it. Doubles as an under-staffing/bottleneck signal for Personnel & Scheduling analytics: a station can be effectively down (or artificially "up") independent of any single equipment fault.

Station also implicitly anchors an assembly-line/workflow producing either a finished part (to distribution) or a sub-assembly (sent to another plant for final assembly) — this cross-plant handoff is part of why Phase 5 exists (finding a "Herbie" between plants, not just within one). That relationship is intentionally **not modeled yet**: the station-to-workflow dependency graph remains an explicit non-goal for this sprint (see Explicit Non-Goals above and Theory of Constraints Data Dependency below).

**`StationEquipment`** — persisted entity, the actual machine record (this is the level `Station` used to incorrectly represent). `(FacilityId, StationId, EquipmentId)` compound PK, FK to `Station` via `(FacilityId, StationId)`.

- `EquipmentId` (string, PK)
- `EquipmentType` (string, backed by a small `EquipmentTypes` reference table rather than a raw free-text column or a compiled C# enum) — the demo plant's equipment list (drill press, band saw, planer, plasma cutter, 3D printer, etc.) is explicitly still open per Theory of Constraints Data Dependency in `DECISIONS.md`; a reference table gets referential integrity and a natural home for a future "expected sensor telemetry per equipment type" join, without a code deploy every time a new machine type is added. A compiled enum is a reasonable upgrade once that list actually stabilizes, not before.
- `StationSequence` — a non-unique number that indicates this machine's order in the station's workflow sequence. While a simple Station may only have enough equipment to do a simple workflow, there will be more complex station which will have sub-processes that run sequences in parallel. denoting the nested sub-sequence for analysis is being deferred for a later sprint currently. This sprint will focus on linear, single machine-order workflows.
- `Status` (enum: Operational, Idle, Faulted, etc.) — moved down from `Station`; this is the real machine-health state
- `LastSensorReading` (DateTime?) — machine health signal, moved down from `Station`
- `ActiveInstanceId` (string?) — which generator instance currently holds the write lease **for this specific piece of equipment**
- `LeaseExpiresAt` (DateTime?) — liveness lease, separate concern from `Status`

`Status`/`LastSensorReading`/lease fields all moved down from `Station` to here: since equipment can run in parallel at the same station, a saw and a conveyor at one station can't share a single lease or a single status without one masking the other.

**`SensorReading`** — telemetry scoped to a parent `StationEquipment`, not a duplicate of it.

- `SensorId`, `FacilityId` + `StationId` + `EquipmentId` (FK to `StationEquipment`), `ReadingType`, value, timestamp

**Uniqueness/identity is governed by a compound key**, not a flat ID check, now spanning the three real levels above instead of a station/sensor split with a hypothetical third:

```csharp
public readonly record struct TelemetryKey(string FacilityId, string StationId, string TelemetryLevel, string TelemetryId);
public enum TelemetryLevel { Station, Equipment, Sensor }
```

A boot-time audit builds a `HashSet<TelemetryKey>` across all configured generator instances and flags exact duplicates. Same `StationId` at different `TelemetryLevel`s (a station and its equipment) is expected and not a conflict. `FacilityId` is a stub for this sprint — every generator instance uses the same default value — but is part of the key/PK now specifically to avoid a schema retrofit later.

## Generator Design

- Generators are **one per `StationEquipment`**, each producing readings for its own sensors — held as plain objects in a `ConcurrentDictionary<TelemetryKey, ...>`, not DI-managed services. This keeps them queryable for liveness/staleness checks without scope or resolution overhead, and avoids the EF Core captive-dependency problem (DbContext is Scoped by default; a Singleton or long-lived plain object must never hold one for its full lifetime).
- A single **boot orchestrator** (`IHostedService`/`BackgroundService`) reads instance configs at startup, runs the uniqueness audit, and constructs instances via a **factory**.
- Each write uses `IServiceScopeFactory.CreateScope()` to resolve a fresh `DbContext` per operation — one scope per write, not one scope for a generator's lifetime.
- Telemetry generation logic is **adapted from the existing Yearly Yields generator pattern** rather than built new, since that pattern is already proven at smaller scale.

### Get-or-Create / Lease Semantics

On startup, a generator instance looks up its `TelemetryKey`:

- **No existing row** → create new. `Status = Operational`. Claim the lease (`ActiveInstanceId` = this generator's own stable `InstanceId`, `LeaseExpiresAt` = now + configured interval).
- **Existing row, lease expired or unclaimed** → safe takeover. This is the crash-recovery path: a generator restarting after a hard failure finds its own stale lease and reclaims it. **`Status` is inherited as-is** from the last recorded value (per the "duplicate the last recorded status" requirement) — lease state and status are independent concerns.
- **Existing row, lease still active under a *different* `InstanceId`** → genuine conflict. Throws a distinct `StationEquipmentAlreadyOperationalException` (carrying the conflicting `TelemetryKey`), translated at the API layer to `409 Conflict` with a structured body the UI can act on (prompt: assign a new ID). This is an expected business outcome, not a system error, and should not be logged/alerted as one.

Each generator instance has its own stable `InstanceId` (GUID, assigned at construction, independent of `StationId`) so the lease can distinguish "this is me coming back" from "this is a different writer."

Race condition note: the get-or-create check-then-insert has a narrow race window (near-simultaneous startup attempts on the same key). `TelemetryKey` should carry a unique constraint at the database level regardless of the in-memory audit, with conflict-on-insert handled via retry/re-fetch.

## Definition of Done

A simulated equipment reading is generated by a generator instance, written via the MCP server's write tool, and persisted to SQL Server through EF Core. A manual on-screen request triggers the MCP server's read tool and displays the latest reading for a given piece of equipment. The boot-time audit correctly rejects a configuration with a duplicate `TelemetryKey`. A generator instance that restarts reclaims its existing `StationEquipment` row (same `EquipmentId`, inherited `Status`) rather than creating a duplicate. A second live instance attempting to claim an already-leased `TelemetryKey` receives a `409 Conflict`, not silent data corruption. `Station.IsOperational()` correctly returns false when any of its `StationEquipment` rows is `Faulted`, and true when all are `Operational`.

No orchestrator, no push updates, no compaction — all explicitly out of scope per above. (Auth is already built — see Carried Over from 1.1.)

Station and StationEquipment Registration need to be operational from the API endpoints. Station can run through the internal MCP Server tools. Equipment registration can run through the Haiku MCP Server to still utilize the internal MCP server validation tools, but will handle registration hand-off to the data repository on it's own (which may end up being internal MCP Server tools as well). Remember, reads will occur through the internal MCP server, but operational tasks will be handled by the Claude Haiku MCP server (which may still use internal MCP tools until the orchestrator is online and then the workflow can be broken up. This quick response will allow the orchestrator to switch between data syncing and operations as it's primary function along-side routing.)

If time allows, a basic UI form can be created to perform these operations from the UI as well. There should also be a screen that draws out a simple, semi-compact box and line screen showing the Station's workflow through the equipment boxes. Clicking on the boxes can bring up a modal showing the equipment's domain model data. If this work can not be done in time, be sure to note it and make it a priority for 1.3, as Station scenarios will need to be created to generate the topology to properly develop and test the Herbie-finding logic and related diagrams.

## Decisions Made This Sprint

- Deferred MAF orchestration to its own sprint to isolate the MCP tool layer as a single unknown.
- Chose plain objects in a `ConcurrentDictionary` over DI-managed services for generator instances, to avoid EF Core captive-dependency issues and keep the collection directly queryable for liveness checks.
- Separated `Status` (descriptive) from lease ownership (`ActiveInstanceId`/`LeaseExpiresAt`) after recognizing that conflating them would block legitimate crash-recovery restarts.
- Adopted a compound `(StationId, TelemetryLevel, TelemetryId)` key instead of a flat ID check, to support multi-level telemetry (station vs. equipment vs. sensor) uniformly.
- Deferred process routing (station-to-workflow dependency graph) past this sprint, but flagged it as a prerequisite for Phase 3 constraint analysis to be meaningful once multiple part types share stations.
- Introduced `StationEquipment` as its own persisted entity between `Station` and `SensorReading` (1.2): `Station` was previously mis-described as "the machine-level record," but a station houses multiple pieces of equipment, potentially running in parallel — `Status`, `LastSensorReading`, and lease ownership all moved down to `StationEquipment` accordingly. `Station` itself gained `IsOperational()` as a computed rollup rather than stored state, doubling as a Personnel & Scheduling under-staffing signal.

## Open Risks / Unknowns

- [Add as encountered]

## Open Questions

Gaps surfaced reviewing README.md and OVERVIEW.md against the current level of design detail. Not blockers for Branch 1.2, but should be resolved or explicitly deferred before the phases that depend on them.

**Resolved questions have moved to [`DECISIONS.md`](./DECISIONS.md)** — that file survives merges to `main`, unlike this one, since it holds the "why" behind the stack rather than this sprint's scope. Anything still genuinely open stays below.

### Theory of Constraints Data Dependency
- The backlog/surplus diagnostic depends on the station-to-workflow routing graph, which is deferred past Phase 3 prerequisite work. Worth stating explicitly that Herbie-finding isn't meaningful until that graph exists.
- Scoping decision for the demo plant's equipment/workflow patterns has moved to `DECISIONS.md` (deferred implementation to 1.2, not this sprint).