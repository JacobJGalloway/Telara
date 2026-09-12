# Telara 
A unified manufacturing operations platform built on Theory of Constraints principles, making station, equipment, and product telemetry visible in real time across every layer of the organization.

---

# Architectural Philosophy & Core Innovation

Telara is intentionally engineered to address the systemic flaws of traditional Manufacturing Execution Systems (MES) and modern enterprise AI engineering:

## Systemic Flow Over Local Efficiency: 

Legacy platforms reward high local efficiency, often creating dangerous, unmanaged inventory stacks at non-bottleneck stations. Telara monitors millisecond-level inter-part completion delays to identify the true system constraint ("Herbie"), dynamically recalculating output cadences to protect floor flow.

## Collapsed RAG Model (Anti-Hallucination): 

Traditional AI pipelines add brittle network layers to check for vector data. Telara utilizes an atomic database lookup pattern—if a semantic vector match fails to meet the confidence threshold, the query instantly collapses into a relational fallback record at the database level.

## Zero-Drift Predictability: 

By ensuring the LLM only receives hyper-localized context or an explicit engineering baseline command, we eradicate hallucinations and data drift. We use deterministic, relational software patterns to make non-deterministic AI completely predictable.

# Technology and Architecture Stack

- SQL Server 2025 Express - Localized data slicing (with a direct migration path to Azure once enterprise scaling triggers it).
- CQRS Pattern - Implemented ASAP to rigidly separate high-frequency telemetry write paths from heavy analytical read operations.
- .NET 10 Microservices - Core business and state domains utilizing Entity Framework Core backed by Repository and Unit of Work patterns.
- AI Abstraction Interface/Adapter – Decouples the .NET core from GenAI platforms, exposing specialized connectors to the analytics and reasoning layers.
- Java / Spring Boot – Isolated Escalation and Notification domain service, strategically scoped to leverage native ecosystem libraries and establish cross-platform fluency.
- GraphQL via Hot Chocolate – The contract layer between frontend islands and backend services, allowing optimized data requests and public-facing API flexibility.
- Claude Sonnet 5 – Underlying foundational LLM executing single-agent workflows and multi-agent loops.
- Island Architecture – Each platform surface (web, mobile, watch, TV, etc.) is a fully self-hosted, independently runnable "Island" app, not a shared shell multiplexing between them. System- or user-level configuration decides which Island(s) are active. The current Web Island is a standalone, in-browser Blazor WebAssembly client (`Telara.Client`) served by its own thin ASP.NET Core host (`Telara.Web`), talking to the backend purely over GraphQL/HTTP. Future Islands (mobile, watch, TV) plug in as siblings under `src/Frontend/Islands/` without touching this one or the backend.

# Running Locally

The full stack is several independent processes — start them in this order so each one's dependencies are already listening when it boots (`Telara.OpsApi`'s background telemetry generator calls MAF on startup and will crash the host if MAF isn't up yet):

1. **Internal Functionality MCP Server** – `dotnet run --project src/McpServers/Telara.Mcp.Internal` (`http://localhost:5217`)
2. **Claude Haiku MCP Server** – `dotnet run --project src/McpServers/Telara.Mcp.Haiku` (`http://localhost:5286`)
3. **MAF Orchestrator** – `dotnet run --project src/McpServers/Telara.Maf.Orchestrator` (`http://localhost:5009`)
4. **Telara.OpsApi** (GraphQL API + Banana Cake Pop) – `dotnet run --project src/Backend/DotNet/Telara.OpsApi --launch-profile https` (`https://localhost:7162`). **The `--launch-profile https` is required** — `launchSettings.json` lists the `http`-only profile first, so a bare `dotnet run` binds only `:5158` and leaves `:7162` unreachable, which is what the Web Island, the CORS policy, and the `Secure` refresh cookie all expect.
5. **Telara.Web** (Blazor Web Island) – `dotnet run --project src/Frontend/Islands/Web/Telara.Web --launch-profile https`, then browse to **`https://localhost:7040`** (not the `:5051` http URL). **This one matters for auth, not just habit**: the refresh-token cookie is `SameSite=Strict`, and browsers apply "schemeful same-site" — `http://localhost:5051` and `https://localhost:7162` count as different *sites* purely because the scheme differs, even though the host is identical, so the cookie silently never gets sent and a page refresh always forces a fresh login. Serving the Web Island over `https://localhost:7040` instead makes it same-scheme with OpsApi, so `SameSite=Strict` actually applies as intended.

Before step 4 the first time, or after a schema change, apply migrations:
`dotnet ef database update --project src/Backend/DotNet/Telara.Domain --startup-project src/Backend/DotNet/Telara.OpsApi`

Seed reference/demo data as needed from `Telara.SQLScripts/` (run in numeric filename order against `telara_ops`).

## Base Orchestration to MCP Server(s) managing tools and Agents

This system architecture is designed to eliminate the most common failure points in enterprise AI engineering — addressing agent prompt 
bloat by moving rules into compiled .NET microservices, preventing context window dilution by using SQL Server Express data slices 
instead of massive data store dumps, eliminating complex asynchronous state management and custom loop handling through the Microsoft 
Agent Framework, and optimizing compute costs by reserving linear GenAI for high-volume manufacturing telemetry while saving Autonomous 
Agents for real bottleneck logic.

                    ┌─────────────────────────────────────────────┐
                    │                 User Input                  │
                    └────────────────────┬────────────────────────┘
                                         │
                  ┌──────────────────────┴────────────────────────┐
                  │  .NET Agent Framework ORCHESTRATION LAYER     │
                  │            (Top-level Orchestrator)           │
                  └────────────────────┬──────────────────────────┘
                                       │
         ┌─────────────────────────────┴─────────────────────────────┐
         │ (JSON-RPC over network)                                   │
         ▼                                                           ▼
┌─────────────────────────────────┐                 ┌──────────────────────────────────────┐
│     .NET MCP SERVER A           │                 │          .NET MCP SERVER B           │
│   (Linear Station Processes)    │                 │   (Heavy Math Calculations/Analysis) │
├─────────────────────────────────┤                 ├──────────────────────────────────────┤
│ SCALE: Auto-scales to 100 pods  │                 │ SCALE: Stays idle at 1 pod           │
│ during peak ingestion hours     │                 │ until a factory bottleneck hits      │
└─────────────────────────────────┘                 └──────────────────────────────────────┘

# Planned Domains

Telara's initial implementation is organized around six core service domains, each independently deployable and scoped to a distinct operational concern. Domain boundaries will be refined during MVP planning but the following represents the intended service landscape at day zero.

## Equipment & Sensor Monitoring
Real-time telemetry from station and equipment sensors, anomaly detection, health status tracking, and alert generation. The layer that makes the invisible visible at the hardware level — encoding the kind of tribal knowledge that currently walks out the door when an experienced technician retires.

## Production & Product Tracking
Throughput monitoring, defect capture and tracing, order status, and batch lifecycle tracking. Defects are treated as bugs — traced to their origin, resolved at the source, and closed out so the same conditions don't produce the same result at the next shift or the next batch.

## Station Management
Station configuration, sensor assignments, threshold definitions, and concurrent workflow support where station dependencies allow it. The layer that knows how the floor is arranged, what each station is expected to produce, and where inventory backlog or surplus conditions are beginning to form.
## Personnel & Scheduling
Shift management, shift station assignment, and role-based access. Maintains operational awareness of resource distribution across the floor to support constraint resolution and situational or seasonal reassignment. The supervisor's job is managing people holistically — Telara's job is giving them the floor context to do that well, not replacing that judgment with a compliance report.

## Analytics & Reporting
The multi-layer view that surfaces the right data to the right role — floor worker, supervisor, plant manager, or executive — without requiring any of them to know how to write a stored procedure to get it. Each role sees the surface they need without the noise the others generate.

## Escalation & Notification
Delivers alerts to their initial recipient list and escalates beyond it when a threshold is crossed without acknowledgment or resolution — widening from station-level to supervisor to plant-manager as needed, rather than simply re-notifying the original list. Isolated from the core .NET services by design; deliberately scoped as a contained domain to re-establish Java/Spring fluency ahead of Mosaic's larger Java footprint.

# Roadmap

Telara is designed to grow without outgrowing its architecture. Each phase is independently demoable and independently researchable — feedback doesn't wait for the full system to be complete.
Phase 1 — MVP
Core telemetry, station monitoring, and dashboard functionality with time-relative seeded demo data. The floor is visible. Herbie can be found.
Phase 2 — Internal Beta
Controlled release to domain-knowledgeable testers. Feedback collected against real operational intuition rather than developer assumptions. Backlog refined from what's actually missing rather than what was guessed at. UI platform support added for tablet as a small screen access to monitoring and notification services within Telara. This is a simple scale down of functionality in preparation of full cross-platform functionality potential implementation intended for phase 3. Tablet should be able to be simulated by scaling a browser window to an appropriate scale.
Phase 3 — Feedback-Driven Expansion
Backlog priorities driven by beta feedback. Analytics and AI-assisted constraint reasoning introduced as the data foundation matures enough to support it. UI platform support added for mobile push notifications and brief overview visualizations via UI intended for initial release at this point as well to show intended cross-platform functionality potential.
Phase 4 — Facility-Wide Footprint
Expansion beyond the production floor into inventory, energy monitoring, environmental conditions, safety compliance, and emergency response coordination. Telara's name was chosen to hold this scope from day one. Mobile implementation is refined based on finalized user feedback from Phase 3, and watch (limited functionality) plus other future devices (AR-augmented, TV, etc.) come online as their own Islands. Each device family gets exactly the data it needs through GraphQL query shaping, with platform-specific rendering (including OS-level differences like Android vs. iOS) handled inside that device's own Island — no separate adapter layer required, and the same pattern extends cleanly to non-standard/edge devices as they show up.
Phase 5 — Enterprise & Multi-Facility
Multi-facility coordination, public-facing API exposure, and client-specific data layer configuration. The architecture was built for this conversation — it just doesn't need to have it yet.
