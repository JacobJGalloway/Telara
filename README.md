# Telara 
A unified manufacturing operations platform built on Theory of Constraints principles, making station, equipment, and product telemetry visible in real time across every layer of the organization.

---

# Technology and Architecture Stack

- SQL Server 2025 Express (Azure once funding gets into play) - CQRS implemented from the start
- .NET 10 C# micro-services with Entity Framework Core (repository & UoW patterns in use)
- AI Abstraction Interface/Adapter pattern to interface the main .NET codebase to AI model operations — connectors to Analytics & AI 
  reasoning layer (Phase 3 constraint analysis, RAG-based recommendation engine, AI-assisted bottleneck identification)
- Java/Spring - Escalation and Notification communications domain service (best suited libraries tied to this language)
- GraphQL with Hot Chocolate Library (domain contract layer between front-end and back-end to support tighter data requests from cross-
  platform frontend interfaces, including the potential for public facing APIs)
- Claude Sonnet 5 - base AI model for single and multiple Agent workflows.
- Microsoft Agent Framework - AI Agent/Tool orchestration and management
- Blazor - Server-first UI layer handling routing, styling, and page layouts as MVC islands, supporting progressive static page    
  application patterns with real-time SignalR integration for telemetry and monitoring surfaces
- Island Architecture - This will generate  individual islands based on existing platform operations and support, while allowing for 
  future cross-platform display and interaction (mobile, watch, tablet, laptop/desktop, etc.)

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
Controlled release to domain-knowledgeable testers. Feedback collected against real operational intuition rather than developer assumptions. Backlog refined from what's actually missing rather than what was guessed at.
Phase 3 — Feedback-Driven Expansion
Backlog priorities driven by beta feedback. Analytics and AI-assisted constraint reasoning introduced as the data foundation matures enough to support it.
Phase 4 — Facility-Wide Footprint
Expansion beyond the production floor into inventory, energy monitoring, environmental conditions, safety compliance, and emergency response coordination. Telara's name was chosen to hold this scope from day one.
Phase 5 — Enterprise & Multi-Facility
Multi-facility coordination, public-facing API exposure, and client-specific data layer configuration. The architecture was built for this conversation — it just doesn't need to have it yet.
