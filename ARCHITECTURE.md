# Architecture — Branch 1.4

## Carried Over from 1.3

1.3 closed on client-side UI and single-line routing scope: a Blazor login screen wired to the JWT/refresh backend, Station/StationEquipment registration forms, and a per-station workflow diagram screen (box-and-line, equipment-box hover/click into sensor-trend detail). `Station.NextStationId`/`IsLoadingDock` gave the first real line topology to render, via the new `GetStationWorkflow` query. Test coverage landed across `Telara.OpsApi`, both MCP servers, and MAF — no longer zero, though not yet at the 85-90% goal.

Late in 1.3, role-based write authorization was added (`RegisterStation`/`RegisterStationEquipment` restricted to Station Supervisor, reads open to all three roles), backed by a new GraphQL-pipeline test harness (`AuthorizationTests.cs`), and two DB check constraints closed a raw-SQL seeding gap in `Station` routing integrity. All durable reasoning from 1.3 lives in `DECISIONS.md` now — this file starts fresh.

**Deferred out of 1.3, now 1.4's starting scope:**
- The Shift Manager dashboard — line-level/Assembly Line topology view (1.3 only ever built the per-station Workflow screen)
- A second, multi-station assembly line — 1.3's single demo line ("Line 3") stays as the simplest-possible baseline; a real linear line is needed to exercise inter-station Transfer for the first time
- New Station Supervisor demo users for that second line — 1.3 only ever seeded one Station Supervisor (`jacobjgalloway@gmail.com`), tied to Line 3
- Single-layer role enforcement — 1.3's `[Authorize(Roles = ...)]` restriction lives only in the GraphQL attribute; the same command reached via the `register_station`/`register_equipment` MCP tools has no role check at all (flagged in 1.3's code review, not fixed there)
- `GetStationOutput` has no server-side facility filter, unlike its siblings (`GetStations`/`GetStationWorkflow`) — low impact while single-facility, but the one query that doesn't follow the established filter-in-handler pattern

## Sprint Goal

Build the **Shift Manager dashboard**, starting at the assembly-line/topology view — and give it a real second line to render, since 1.3's topology work only ever had one station to draw.

1. **New 3-station linear assembly line** — a second demo line, separate from Line 3 (which stays untouched as the simplest-possible single-station baseline). Confirmed topology: station 1's output is a Transfer point, station 2's input and output are both Transfer points, station 3's input is a Transfer point and its output is Dispatch (station 3 is this line's `IsLoadingDock`). This is exactly what 1.3's existing boundary-box rendering (`StationFlowDiagram`/`StationEquipmentDiagram`) already draws — Raw Materials only before the first station, a Transfer box between every consecutive pair, Dispatch only after the true terminal station — so this is a seed-data/topology exercise, not a rendering change.
2. **New Station Supervisor demo users** for the new line's stations, so each can be logged into and checked independently (same pattern as 1.3's `006.SeedDemoUsersForDashboardRoles.sql`, extended).
3. **Assembly Line view** — the line-topology screen, one level up from 1.3's per-station Workflow screen:
   - Each station renders as a diamond marker (per the logo's own diamond-as-station meaning), colored by a **worst-of rollup over that station's own equipment** — same "Faulted → critical, Idle → watch, Operational → ok" rule already built for `StationFlowDiagram` in 1.3, just one level of granularity up. A station that's otherwise operational except for one piece of equipment stuck idling shows as the anomaly at the line level.
   - **Hover/click, same interaction rule as the equipment-level diagram**: hovering a station diamond shows a small summary list of that station's individual equipment and their statuses (explains *why* the diamond is showing its worst-of color); clicking drills into the full per-station equipment view (1.3's Workflow screen, reused as-is — see Role-Based View Reuse in `DECISIONS.md`).
   - This is the Shift Manager's default landing; the same per-station drill-down view Station Supervisors land on by default is reused, not rebuilt Shift-Manager-flavored.

## Explicit Non-Goals for This Slice

- No Claude Sonnet MCP Server, no real-time/predictive analysis, no optimization/remediation paths, no growth-pathway (loading-dock order volume) work — that's Plant Manager dashboard scope, deliberately deferred past 1.4
- No cross-line comparison/analysis between Line 3 and the new line — 1.4 proves a *second* line exists and renders correctly on its own; comparing lines is Plant Manager/Sonnet territory
- No Venn-style shared-station rendering — both lines stay fully separate (no station shared between them) this sprint; the Venn metaphor is scoped for whenever cross-line/shared-station topology actually exists
- No line-switcher control on the Assembly Line view — mentioned in `DECISIONS.md`'s frontend-direction notes as intended, not required to land this sprint
- No fix yet for the two carried-over gaps above (MCP-path role enforcement, `GetStationOutput`'s missing facility filter) unless they block new work directly — otherwise tracked, not required for this sprint's DoD
- No `Alert`/`Employee` domain entities — still 1.4/1.5-or-later per `DECISIONS.md`'s Employee Domain Model note, not pulled forward just because staffing/alerts appear in the dashboard mockups
- No push/real-time updates — reads stay manual/on-request, same as 1.3
- No compaction job, no multi-facility logic — unchanged from 1.3

## Definition of Done

**New assembly line:** a second `FacilityId`-scoped line (3 stations, linear) exists via `Telara.SQLScripts` seed data (or `RegisterStation`'s predecessor-linking mutation, preferred where it fits — see `DECISIONS.md`'s Single-Line Station Routing decision), with the Transfer/Dispatch topology described above. New Station Supervisor demo users exist, one per new station (or otherwise assigned per however staffing ends up scoped), documented in the local startup README alongside 1.3's existing demo logins.

**Assembly Line view:** a Shift Manager landing screen renders both lines' stations as diamond markers, each colored by worst-of-equipment rollup, with hover-summary and click-through-to-per-station-view both wired up per the interaction rule above.

**Role enforcement:** at minimum, confirm/document whether the MCP-path role-enforcement gap (see Carried Over above) needs closing this sprint or can stay tracked — don't let it silently regress further as new registration paths get added for the second line's seed data.

## Decisions Made This Sprint

*(none yet — sprint just started)*

## Open Risks / Unknowns

- The MCP-tool-path role-enforcement gap and `GetStationOutput`'s missing facility filter (both carried over from 1.3's code review) — still unaddressed, revisit if either blocks new work or becomes exploitable once a second facility/line makes the data-scoping gap more visible.
- Longer-cycle validation for `Station` routing (a multi-hop `NextStationId` loop with no `IsLoadingDock` anywhere in it) is still only a forward-looking `DECISIONS.md` note, not built — the DB check constraints added in 1.3 only catch self-reference and the loading-dock contradiction, not longer cycles. A second real line raises the chance of a bad seed script actually hitting this; worth a second look if seeding the new line gets complicated enough to risk it.
- Design-system palette/typography finalization from Claude Design was still pending as of 1.3 — confirm current state before hardcoding any new colors for the Assembly Line view's diamond markers.

## Open Questions

**Resolved questions have moved to [`DECISIONS.md`](./DECISIONS.md)** — that file survives merges to `main`, unlike this one. Anything still genuinely open stays below.

- Exact staffing assignment for the new line's Station Supervisors (one-to-one per station, or some other split) — not yet decided, scope when seeding the new line.
