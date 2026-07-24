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

**`Station`** — persisted entity, the machine-level record. `(FacilityId, StationId)` is the compound primary key; `StationId` doubles as the correlation key for logging/tracing across the system.

- `FacilityId` (string, PK) — stubbed for this sprint: single hardcoded/default value, no multi-facility logic reads it yet. Included in the key now, per [[project-stack-migration-pattern|the same avoid-a-retrofit reasoning]] used elsewhere, so Phase 5 multi-facility support (if it happens) doesn't require re-keying every table.
- `StationId` (string, PK)
- `Status` (enum: Operational, Idle, Faulted, etc.) — purely descriptive, last known state
- `LastSensorReading` (DateTime?) — machine health signal
- `LastOperatorAction` (DateTime?) — presence/staffing signal
- `ActiveInstanceId` (string?) — which generator instance currently holds the write lease
- `LeaseExpiresAt` (DateTime?) — liveness lease, separate concern from `Status`

**`SensorReading`** — telemetry scoped to a parent station, not a duplicate of it.

- `SensorId`, `FacilityId` + `StationId` (FK), `ReadingType`, value, timestamp

**Uniqueness/identity is governed by a compound key**, not a flat ID check, since telemetry exists at multiple levels (machine, sensor, and potentially sub-component later):

```csharp
public readonly record struct TelemetryKey(string FacilityId, string StationId, string TelemetryLevel, string TelemetryId);
public enum TelemetryLevel { Machine, Sensor, Component }
```

A boot-time audit builds a `HashSet<TelemetryKey>` across all configured generator instances and flags exact duplicates. Same `StationId` at different `TelemetryLevel`s (a machine and its sensors) is expected and not a conflict. `FacilityId` is a stub for this sprint — every generator instance uses the same default value — but is part of the key/PK now specifically to avoid a schema retrofit later.

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

## Open Questions

Gaps surfaced reviewing README.md and OVERVIEW.md against the current level of design detail. Not blockers for Branch 1.1, but should be resolved or explicitly deferred before the phases that depend on them.

### Messaging / Transport — RESOLVED (2026-07-22)
Message queue is **Apache Kafka**, self-hosted on-prem for now (single broker, KRaft mode — no separate ZooKeeper), with **Azure Event Hubs' Kafka-compatible endpoint** as the eventual managed target once budget/scale justify it. Decision drivers:
- Volume is not the concern — even a full-plant reconnect burst (10 stations × ~10 pieces of equipment each, emitting on a 15-minute interval to start) is on the order of 100 messages, far under Kafka's comfortable range. The concern is decoupling ingestion from the synchronous API path so a burst doesn't stall response times or hammer SQL Server with concurrent writes.
- The multi-level consumer pattern the analytics roles need (station supervisor → equipment-level, shift manager → station-level, plant manager → part/sub-assembly-level) maps naturally onto Kafka's log model: each role's consumer group tails/replays the same topic independently at its own pace, without one consumer's read affecting another's. This is a real advantage over RabbitMQ's queue model, which would need a fanout exchange plus one queue per consumer type to approximate the same thing.
- Kafka is free/OSS to self-host, and Event Hubs speaks the Kafka wire protocol — so today's .NET producer and Java (Spring Kafka) consumer code carries forward unchanged when the org eventually migrates to the managed service; only the broker connection config changes. This follows the same on-prem-now/Azure-compatible-later pattern already used for SQL Server 2025 Express → Azure SQL.
- Retention on Kafka topics should stay short (hours to a few days) — SQL Server remains the system of record; Kafka is a transient buffer/replay window, not long-term storage.
- Partition key should be `EquipmentId` (or `StationId`+`EquipmentId`) rather than `StationId` alone, to preserve per-device ordering and to match the smaller, equipment-scoped domain model (readings originate from station equipment, not the station itself) that keeps API response times fast.
- Schema Registry (Confluent's OSS Schema Registry, self-hosted) is deferred until there are actual cross-language producers/consumers sharing a topic — not needed for Branch 1.1, but expected once Escalation & Notification (Java/Spring) is wired in.

Confirmed pattern: Escalation & Notification is one of potentially several fan-out subscribers on a shared publish event — e.g., a detected issue publishes once, and the SQL persistence path and the Escalation & Notification push path each subscribe independently and run in parallel, with no dependency between them.

### Identity & Multi-Facility — RESOLVED (2026-07-22)
`FacilityId` is stubbed into the compound key now (`Station` PK, `TelemetryKey`) — see Domain Model above — so the schema doesn't need a retrofit if multi-facility is ever built. That covers the data-model foundation only.

Full multi-facility *operation* is a larger extension than the key alone and stays explicitly out of scope until Phase 5 (if it happens at all — Phase 5 sits past the demo-stable pause point for pilot feedback, and was included partly to demonstrate domain awareness rather than as a committed build target). In particular, running from a centralized data center instead of a plant-level control room raises questions the schema doesn't answer: cross-facility aggregation/rollup views, network latency and connectivity assumptions between facilities and the central system, and whether write-lease semantics (`ActiveInstanceId`/`LeaseExpiresAt`) still hold the same way across a WAN instead of a plant LAN. Flag these for actual Phase 5 planning rather than resolving now.

### AI / RAG — RESOLVED (2026-07-22)
**Vector storage:** SQL Server 2025's native `VECTOR` type, in the same database/tables as the relational fallback data — no separate vector database. This gets DiskANN-based approximate-nearest-neighbor indexing and `VECTOR_DISTANCE()` similarity queries, giving practical parity with pgvector's HNSW/IVFFlat + distance-operator approach, without a second data store to keep in sync.

**Collapse mechanism:** because vectors and relational fallback data live in the same store, the vector-match-then-fallback check is a single atomic query (CTE or stored proc) — run the vector search, check the top match's distance against a threshold, fall back to the relational lookup in the same round trip if nothing clears it. One hop, not an application-layer two-step check.

**Embedding generation:** not yet decided which model/service produces the embeddings written into the `VECTOR` column — but the call is routed through the existing **AI Abstraction Interface/Adapter** (already named in README's stack) rather than called directly from domain code. This means the current implementation (local/on-prem, TBD) can be swapped for an Azure OpenAI embeddings implementation later without touching any caller — same on-prem-now/Azure-compatible-later shape as [[project-stack-migration-pattern|SQL Server and Kafka]].

**Two distinct embedding lifecycles** (pattern confirmed against a working prior implementation — see Prior Art note below):
- *Persisted embeddings* — generated and written to the `VECTOR` column when an aggregate/summary row is upserted (e.g., a shift/batch close). This is the "maintain" half: embeddings regenerate whenever the underlying aggregate changes, not once and left stale. Natural trigger point once Telara's own shift-based compaction job exists (currently a fast-follow per this document's earlier scope notes).
- *Ephemeral query-time embeddings* — generated fresh, in memory, for the *current* context at the point a reasoning step needs similarity search; used only as the search input, never persisted. No separate table or lifecycle needed for these — they exist for the duration of one query.

**Text-before-embedding construction:** embed a natural-language description built from structured fields (equipment type, station role, reading type, values), not raw numeric telemetry directly. This is what lets semantic similarity capture categorical/contextual meaning (e.g., a reading pattern on a stamping press vs. the same numeric pattern on a conveyor motor should not be treated as similar just because the numbers are close).

**Query-time interceptor location — still open, deliberately out of scope for Branch 1.1:** the ReAct-style loop that would decide "I need similarity search now" and generate the ephemeral embedding is a **tool orchestrator**, not the system orchestrator — a distinct layer from MAF. MAF is the top-level orchestrator (per the README diagram) and owns state management and other system-level concerns; ReAct-style tool selection/invocation reasoning is a narrower concern sitting closer to (or inside) the MCP tool layer. Because MAF orchestration is already deferred past this sprint (see Sprint Goal above), the interceptor's exact home isn't being decided yet either. Leaning candidate: expose it as its own MCP tool (e.g. `find_similar_readings`), and current preference is to keep that tool **in the same MCP server as the main process** rather than splitting it into a separate server — undecided/worth a real debate once orchestration work starts, not settled now.

**Open strategic question — is a vector/RAG pipeline worth building at all:** the user flagged that the broader RAG-via-embeddings approach may be losing ground industry-wide — some teams achieve the same "collapsed context" result (grounded, non-hallucinated answers) by having an agent run a linear sequence of ordinary tool calls instead of a vector-search step. Worth revisiting before investing further in the `VECTOR` column / embedding-generation infrastructure above: could Telara's Collapsed RAG goal be met by a linear tool-calling agent over the existing relational/EF Core data, without embeddings at all? Not resolved — flagging so the vector-infrastructure work isn't treated as a foregone conclusion.

**Prior art:** `vector_service.py` in the Yearly Yields project (Python/PostgreSQL/pgvector) is the closest existing implementation of this pattern — worth reviewing directly when this is picked back up, including its `find_similar_weeks` query shape (relational pre-filter on foreign keys + `embedding IS NOT NULL`, then `ORDER BY cosine distance LIMIT n` — the same one-query hybrid shape as the Collapse mechanism above) and its job-triggered `upsert_historical_summary`. One flag before porting the embedding-generation call shape specifically: that file calls `anthropic.AsyncAnthropic().embeddings.create(model="voyage-3", ...)`, and it's unconfirmed whether the Anthropic SDK actually exposes an embeddings endpoint that way (embeddings are typically served through Voyage AI's own SDK/service, not the Anthropic client) — verify this actually hits a real endpoint before modeling the .NET embedding-service call the same way.

**Confidence threshold:** per-query-type, not a single global cutoff — a lookup table maps context/query type to its required confidence level (e.g. defect lookup vs. equipment troubleshooting may need different minimum similarity), rather than tuning one fixed number for every use case.

**LLM-unavailability degraded mode:** substantially de-risked by the above — the vector match and relational fallback both execute inside SQL Server and don't call the reasoning LLM (Claude Sonnet 5) to produce an answer. The external LLM is a downstream consumer of an already-resolved lookup, not a dependency of the lookup itself, so floor monitoring/telemetry queries aren't blocked by LLM API availability. (Embedding generation at write time is a separate dependency — revisit once the embedding model/service is chosen.)

### Ingestion / Hardware — RESOLVED (2026-07-22)
Real sensor/PLC wire protocol (OPC-UA, Modbus, MQTT, vendor-specific) is intentionally **not** being decided yet. Ingestion is built behind an **adapter pattern** — raw device data goes through an ETL adapter into domain models before persistence — so whichever protocol real hardware eventually speaks is isolated to that adapter layer and doesn't ripple through the rest of the system. This was the plan from the start; it just hadn't been written down here yet.

Initial and current implementation is a **fIoT (fake IoT) generator** — simulated station/equipment telemetry, following the same generator pattern already adapted from Yearly Yields (see Generator Design above) — specifically so the workflow (generator → adapter/ETL → MCP write tool → SQL Server) can be proven end to end without investing in real hardware under current budget constraints. Real protocol selection is deferred until hardware procurement is actually funded; the adapter seam is what keeps that a swap-in decision later rather than a rework of the ingestion pipeline.

### Auth / RBAC — RESOLVED (2026-07-22)
**1.1:** stays as originally scoped — single "developer" SQL login, static connection string, no real auth. Non-goal for this sprint, unchanged.

**1.2:** modern ASP.NET Core identity, not the legacy `System.Web` Membership/RoleProvider model (that API isn't available in .NET 10 at all — `System.Web.Security.SqlMembershipProvider` and the `<membership defaultProvider="...">` config pattern are .NET Framework-only). The equivalent shape in current .NET is `AddAuthentication()`/`AddAuthorization()` registered on the builder alongside the rest of the app's DI/config setup:
- A `Users` table keyed by a stable `UserId` (GUID, distinct from the mutable `Username` — same correlation-key role `StationId` plays for `Station`), with properly hashed passwords (bcrypt/Argon2id, not a fast general-purpose hash), validated by a dedicated login mutation that takes plaintext credentials over TLS and returns a signed token/cookie carrying role claims. The hash itself never transits GraphQL or any other API surface — no legitimate reason for a client to ever receive it.
- Role claims from that token drive authorization uniformly at **both** layers: Blazor page/component level (`[Authorize(Roles = "...")]` on pages/components) and the GraphQL/Hot Chocolate layer (`[Authorize(Roles = "...")]` on resolvers/fields). Same principal, same claims, checked at whichever layer is relevant — this is the mechanism that lets a station supervisor's query return a different data shape than a plant manager's without separate endpoints, which was the original goal for leaning on GraphQL/Hot Chocolate here.
- **No live SQL-Server-login fallback.** An earlier version of this plan considered falling back to `TrySqlServerLogin()` with user-submitted credentials when no `Users` row matched, both as a "what if nobody remembers a password" escape hatch and to obscure which backend handled a given login. Dropped: forwarding arbitrary user input into a live database authentication attempt turns the login form into a SQL Server credential-testing oracle, and conflates database principals with application user accounts — a real risk for a benefit ("obfuscation") that doesn't hold up as an actual defense. The escape-hatch need is solved instead by a **seeded developer/break-glass account directly in the `Users` table** (EF Core seed data, hashed like any other row) — same authentication path as everyone else, no second mechanism, no forwarded credentials. That seeded account should be disabled or rotated before any deployment beyond local dev.
- **Forward-looking seam for Azure Entra ID:** local `Users`-table identity (1.2) is the on-prem stand-in, not the end state — ASP.NET Core Identity integrates cleanly with Entra ID as an external provider (via `Microsoft.Identity.Web`), and Entra ID's object IDs give stable, traceable principal identifiers that correlate cleanly with Azure-native logging/monitoring (Application Insights/Azure Monitor) when tracing who-did-what during a load-bearing production issue — the same tracing story is much weaker with a purely local username. Same [[project-stack-migration-pattern|on-prem-now/Azure-compatible-later shape]] as SQL Server and Kafka: reserve a nullable `ExternalPrincipalId` (Entra object ID) column on `Users` now, unused until Entra integration happens, so identity doesn't need restructuring when it does. Directly relevant to the still-open Operational Resilience question below.

### Theory of Constraints Data Dependency
- The backlog/surplus diagnostic depends on the station-to-workflow routing graph, which is deferred past Phase 3 prerequisite work. Worth stating explicitly that Herbie-finding isn't meaningful until that graph exists.
- **Scoped, decision deferred to 1.2 (2026-07-22):** the test/demo plant will need generators for multiple distinct equipment types (drill press, band saw, planer, plasma cutter, 3D printer, etc. — exact list still open, intentionally not fully enumerated yet). The workflow model needs to demonstrate three station-level operation patterns: **parallel** equipment operation at a station, **sequential** equipment operation at a station, and a **combination** of both. Whether that's expressed as one long chained workflow with blocks representing each scenario, or as separate independent blocks with unique outputs, is explicitly left open — either is acceptable — and will be finalized once UI work starts in Branch 1.2, after minimal workflows and their target parts/sub-assemblies are picked. This is the concrete shape the deferred routing graph needs to support once it's built.

### Operational Resilience — RESOLVED (2026-07-22)
- **Backup:** SQL Server 2025 Express has no true HA/failover option (no Always On Availability Groups) — the honest resilience story at this stage is backup-and-restore, not zero-downtime, and that's an accepted tradeoff of the on-prem-now/budget-constrained approach rather than an oversight. A scheduled full backup runs during off-hours (the plant isn't 24/7), using **Simple recovery model** — sufficient since backups only need to happen once, off-hours, and there's no need to restore to a moment between backups. RPO is "since last night's backup." The backup job is a **separate supporting system**, not built into any domain microservice — a standalone scheduled process (own executable, or a Windows Task Scheduler job invoking a backup script) rather than a `BackgroundService` inside the app, specifically so it can be disabled/paused for a demo without touching or restarting the application being demoed. This also sidesteps SQL Server Express not shipping SQL Server Agent — nothing built into Express can trigger a scheduled backup on its own, so external scheduling is required regardless.
- **User-action tracing doesn't need to wait on Entra:** `Users.UserId` (stable GUID PK, distinct from the mutable `Username`) is the correlation key for logging/tracing on the on-prem side now — same pattern already used for `Station.StationId` in the Domain Model above. The Entra object ID reserved in Auth/RBAC becomes an *additional* correlation dimension once that integration exists, not the only path to tracing.
- **Structured logging/alerting envelope:** one shared shape across all domains rather than per-domain log formats, keyed by the relevant entity's stable id (never a mutable display name) plus a `datetime2` timestamp:
  ```csharp
  public sealed record TelemetryEvent(
      string EntityId,        // the relevant domain entity's stable id (StationId, UserId, etc.)
      string EntityType,      // discriminator: "Station", "User", "SensorReading", ...
      DateTime2 Timestamp,    // when it happened, for ordering/tracing
      string EventType,       // e.g. "Created", "StatusChanged", "ThresholdBreached"
      string? Payload         // domain-specific JSON, opaque to the logging/alerting layer
  );
  ```
  Alerts reuse the same envelope — `EventType` values the Escalation & Notification fan-out subscriber watches for — so that pub/sub pattern can subscribe generically without domain-aware routing logic.

### Data Retention — RESOLVED (2026-07-22)
Modeled on the same tiered approach used in Yearly Yields:
- **Detail records** (raw telemetry): kept in full for the **current quarter + previous quarter** — a rolling 2-quarter window.
- **Compaction**: once a quarter ages out of that window, its detail records are compacted down to **one summary row per shift** (aggregated stats — min/max/avg, matching the shift-based operational unit already used elsewhere in the domain model), rather than kept as raw readings.
- **Purge**: summarized data is retained until **one calendar year plus one month** of total age, then removed entirely.
- **Configurability**: the detail-window size (quarters), and total retention duration, are environment-variable-configured rather than hardcoded, so they can be tuned without a code change as real usage patterns become clear.

This gives the compaction job mentioned as a Branch 1.1 fast-follow (see Sprint Goal / Explicit Non-Goals above) a concrete shape, and directly answers the AI/RAG section's note above: the shift-level summary row this job produces is the natural trigger point for regenerating persisted embeddings once that pipeline exists — same aggregate-upsert-triggers-embedding-regeneration pattern as Yearly Yields' `upsert_historical_summary`, just shift-scoped instead of week-scoped.

### Public API (Phase 5) — RESOLVED (2026-07-22)
Not anonymous/open access — "public-facing" (README's Phase 5 language) means externally reachable, not unauthenticated. Same flow as Auth/RBAC above: successful login through Identity issues a user-specific signed JWT, and that token is what any external consumer (Phase 5 client, partner integration, etc.) authenticates with — no separate anonymous-access design needed. This pattern is already working in Yearly Yields, so it's proven, not speculative.

Rate limiting and query cost/depth limits for the GraphQL layer are still worth a pass once Phase 5 is actually being built, but scoped per-authenticated-token rather than as a defense against anonymous traffic — a materially smaller problem than originally framed.

### Documentation Consistency — OWNED BY USER (2026-07-22)
OVERVIEW.md names React Native for the mobile island; README.md's Island Architecture section doesn't name a mobile technology. User is aligning these directly rather than as a quick sync — intent is to lay out the **full device/platform roadmap** (mobile, tablet, watch, desktop/laptop, etc., per the Island Architecture section's stated future scope) across phases now, even for devices that won't be built out until a later phase, so the roadmap is visible up front rather than added piecemeal. Not resolved by this document — pending the user's edit to README.md/OVERVIEW.md.