# Architecture — Branch 1.3

## Carried Over from 1.2

1.2 closed on backend/data-flow scope: simulated telemetry → MAF (stateless router) → Claude Haiku MCP Server (ingestion) → Internal Functionality MCP Server (write) → SQL Server via EF Core, plus the manual on-screen read path and Station/StationEquipment registration, both also routed through MAF. `Telara.Domain.Tests`/`Telara.Core.Tests` scaffolding landed under `src/Tests/DotNet/`. Backend auth (JWT + rolling refresh tokens, `[Authorize]` on GraphQL resolvers) shipped even earlier, in 1.1.

**Deferred out of 1.2, now 1.3's starting scope:**
- Client-side User Auth — Blazor login screen + page-level `[Authorize]` wiring (backend already exists, unaffected)
- Basic UI — Station/StationEquipment registration forms
- Basic UI — workflow diagram screen (box-and-line, equipment-box click-through to a domain-data modal)
- Test coverage — 1.2 only scaffolded Domain/Core; OpsApi, the MCP servers, and MAF itself have zero tests

## Sprint Goal

Two threads this sprint:

1. **Client-side UI**, closing out what 1.2 didn't get to: a Blazor login screen wired to the existing JWT/refresh backend, Station/StationEquipment registration forms, and a workflow diagram screen — a semi-compact box-and-line view of a station's equipment, clickable into a modal showing that equipment's domain data, with the latest sensor readings for all viewed equipment shown below the diagram.
2. **Single-line routing prereq** ("the wild card"): a minimal, linear station-to-station path — raw material intake through stations to the loading dock — so the workflow diagram has real topology to render and 1.4 has a concrete dataset to point Sonnet-driven cross-line bottleneck analysis at. This is deliberately thin: no raw-materials/assembly-line domain model, just enough routing data to answer "what does this station feed into, and where does this line terminate?"

Both threads point at the same screen: the diagram needs the routing data to draw its lines, and the routing data has no reason to exist yet without the diagram needing it.

## Explicit Non-Goals for This Slice

- No Claude Sonnet MCP Server, and no deep-reasoning/RAG orchestration through MAF — still 1.4+, once single-line routing data actually exists to reason over
- No cross-line or multi-line bottleneck/Herbie-finding logic — this sprint proves a *single* line's topology; cross-line analysis is what 1.4's Sonnet server is for
- No `RawMaterial`/`Assembly` domain entities — the routing prereq below reuses `Station`, it does not model materials or parts moving through it
- No parallel/branching station sequences — this sprint's routing stays linear, consistent with `StationEquipment.StationSequence` already being linear-only since 1.2
- No RAG or recommendation engine
- No escalation routing (Java/Spring domain untouched this sprint)
- No push/real-time updates — SignalR integration is deferred; reads stay manual/on-request
- No compaction job — shift-based compaction and 90-day retention are a fast-follow once more read/write volume exists
- No multi-facility logic — `FacilityId` stays a single stubbed value, same as 1.2

## Domain Model Additions (This Sprint)

**Single-line routing on `Station`** — the minimal shape needed to answer "what does this station feed into," reusing the existing entity rather than introducing new ones:

- `NextStationId` (string?, self-referencing FK to `Station` within the same `FacilityId`) — nullable; null means this station is a terminal node in its line. Linear only: one outgoing edge per station, no branching/merging this sprint.
- `IsLoadingDock` (bool) — marks a station as a line's terminal/output node. A station with `IsLoadingDock == true` is expected (not yet enforced) to have `NextStationId == null`.

This is the station-to-workflow dependency graph that's been an explicit non-goal since 1.2 — scoped down here to a single line's linear path, not the full multi-line/cross-plant graph Phase 5 eventually needs.

**`GetStationWorkflow(facilityId, stationId)`** — new GraphQL query, the concrete endpoint use case for the routing fields above. Walks the `NextStationId` chain from the given station to its `IsLoadingDock` terminus and returns the ordered station list (each with its `StationEquipment` and latest `SensorReading` per equipment) for the workflow diagram screen to render. Read-only; follows the existing direct-MediatR pattern (`GetSensorReadings`), not routed through MAF — this is bulk read shaping for a UI screen, not an operational/write workflow.

## Definition of Done

**Client-side auth:** a Blazor login screen posts to the existing `Login` GraphQL mutation, stores the resulting access token, and attaches it to subsequent GraphQL requests. Page/component-level `[Authorize]` is enforced in the Blazor router consistent with the resolver-level `[Authorize]` already in place server-side — same principal, same claims, checked at both layers per the Auth/RBAC decision in `DECISIONS.md`. Logout clears the session and the refresh cookie via the existing `Logout` mutation.

**Basic UI:** a form registers a new `Station` and any `StationEquipment` to add with it, calling the existing `RegisterStation`/`RegisterStationEquipment` mutations (already routed through MAF per 1.2). A workflow diagram screen renders a station's equipment as a semi-compact box-and-line layout; clicking a box opens a modal with that equipment's domain data; the latest sensor reading for each viewed piece of equipment is listed below the diagram.

**Single-line routing:** `Station.NextStationId`/`IsLoadingDock` exist as an EF Core migration. At least one seeded demo line (raw-material-facing station → intermediate stations → a station with `IsLoadingDock = true`) exists via `Telara.SQLScripts` seed data. `GetStationWorkflow` returns the correct ordered station list for a seeded line, terminating cleanly at the loading dock, and the workflow diagram screen renders it end to end.

**Test coverage:** OpsApi, the MCP servers, and MAF each get initial test coverage this sprint — not the full 85-90% target yet, but no longer zero. Coverage on `Telara.Domain`/`Telara.Core` moves from 1.2's scaffolding toward the 85-90% goal.

No Sonnet server, no cross-line analysis, no raw-materials domain model, no push updates, no compaction, no escalation work — all explicitly out of scope per above.

## Decisions Made This Sprint

- **2026-09-01:** Single-line station routing (`NextStationId`/`IsLoadingDock` on `Station`) added as this sprint's "wild card," justified specifically by the workflow diagram screen's need for real topology to render — not a step toward the full raw-materials/assembly-line domain model, which stays out of scope until a specific use case needs it. Chosen over a new `StationRoute`/`Assembly` entity to keep the addition minimal and reversible; a richer model can replace this once 1.4's cross-line Sonnet analysis defines what it actually needs.
- **2026-09-01:** `GetStationWorkflow` reads go through direct MediatR, not MAF — same reasoning as `GetSensorReadings` in 1.2 (bulk UI-shaping reads for high-frequency/UI-driven access don't need MAF's routing hop; MAF stays reserved for the ingestion/registration flows it already owns).

## Open Risks / Unknowns

- `NextStationId`/`IsLoadingDock` referential integrity (a station pointing at itself, a cycle, more than one loading dock terminus per line) isn't enforced by a DB constraint this sprint — flagged for validation logic or a follow-up constraint once real seed scenarios expose what actually needs guarding.
- **Known limitation, not addressed this sprint:** `NextStationId` is a single self-referencing FK, so it can only represent one outgoing edge per station. A station that cross-feeds multiple downstream lines (a larger station splitting output across more than one line) can't be modeled this way — that would need a one-to-many edge table (e.g. a `StationRoute(FromStationId, ToStationId)` join) instead of a column on `Station`. Not a requirement now; flagged so the single-FK choice isn't mistaken for a permanent decision when that scenario shows up.

## Open Questions

**Resolved questions have moved to [`DECISIONS.md`](./DECISIONS.md)** — that file survives merges to `main`, unlike this one, since it holds the "why" behind the stack rather than this sprint's scope. Anything still genuinely open stays below.

### Theory of Constraints Data Dependency
- Cross-line/multi-line Herbie-finding still depends on the Sonnet MCP Server (1.4+) and on more than one seeded line existing to compare — single-line routing this sprint is a prerequisite, not the analysis itself.
- The demo plant's equipment/workflow patterns (which stations, in what order, feeding which loading dock) still need to be scoped for seed data — tracked here since it's implementation-scoped for 1.3, not architectural.
