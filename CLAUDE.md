# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project Overview

Telara - oversight, notification, services as well as providing reccomendations for remediating "bottlenecks" in a manufacturing plant's workflows for a part or sub-assembly.

Overall solution file and service projects are still to be created as this is a new project.

**Projects and Services:**
1. `Telara.Domain/` - Domain model definitions along with adapters and connections to the data store(database) for data persistence
2. `Telara.Core/` - initial home for services. As a group of services may be grouped together into a given workflow, those services may be broken out into a separate project.
3. `Telara.OpsApi/` - The internal API of the backend architecture of Telara to communicate to the frontend visualizations or other services (notifications, automation, etc.) as needed.

**Databases:** The primary database of `telara-ops` will serve as the full database initially. Febore the project reaches a demo stable state, a CQRS pattern will need to be implemented to allow a separate read database. Database initialization, modifications, and seeding for initialization and demonstrations will live in `Telara.SQLScripts`. The database will be on the same machine as the running operations. Initialization will be manually triggered only. Modifications will initially be allowed to be included as code updates and follow normal validation and commit/push process. After that, Modifications will fall in with the Initialization rule. Seeding can be done on command. This will allow the re-seeding to occur for purposes of relative seeding setups for demonstrations.

# Architectural Guidelines: Telara Multi-Language Island Architecture

## Repository Commands & Lifecycle
- Initialize Entire Workspace: `.\init-workspace.ps1` (PowerShell)
- Build .NET 10 Solution: `dotnet build src/Backend/DotNet/Telara.sln`
- Run DB Migrations: `dotnet ef database update --project src/Backend/DotNet/Telara.Domain --startup-project src/Backend/DotNet/Telara.OpsApi`
- Run OpsApi Service (Includes fIoT / Banana Cake Pop): `dotnet run --project src/Backend/DotNet/Telara.OpsApi`
- Run All Tests (Unit & Integration): `dotnet test src/Backend/DotNet/Telara.sln`
- Run Single Test Case: `dotnet test --filter "FullyQualifiedName~YourTestName"`
- Build Java Services: `mvn clean compile -f src/Backend/Java/Telara.NotificationService/pom.xml`

## Namespace roots
- Telara Data Domain, Repositories, & CQRS: `Telara.Domain`
- Telara Internal Services & AI Adapters: `Telara.Core`
- Telara API Layer & GraphQL: `Telara.OpsApi`

## Core Stack Rules & Architectural Boundaries

### 1. Persistence & CQRS (.NET 10 & SQL Server 2025)
- **Data Processing**: Commands (Write path) and Queries (Read path) are managed as processing pipelines directly within `Telara.Domain` via MediatR handlers.
- **EF Core Optimization**: Database read queries must explicitly leverage `.AsNoTracking()` inside Domain handlers to optimize performance for high-frequency sensor streams.

### 2. API Contract Layer (Hot Chocolate GraphQL)
- **Design**: Direct client-facing operational interactions live strictly within the Hot Chocolate schema layer. Banana Cake Pop (`/graphql`) is the primary interface for testing and contract verification.
- **Queries & Mutations**: Use MediatR records to cleanly isolate GraphQL entry points. Explicitly append `.UseFiltering()` and `.UseSorting()` to query endpoints to empower cross-platform frontend selection.

### 3. Background Processing & Fake IoT Diagnostics
- **Simulation**: High-frequency equipment telemetry streams tagged per station equipment. This will run as one service, but will likely have a topic queue or compound key to handle transmission from the station equipment to the readings database table and the notifications service if the readings are outside of equipment thresholds. 
- **System Controls**: Toggling simulation states or forcing equipment breakdown scenarios can be executed via GraphQL mutations, or triggered programmatically via diagnostic REST Minimal API routes (`/api/simulator/*`) to maintain a clean UI-free testing architecture.

### 4. Frontend Island Architecture (Self-Hosted, Per-Platform)
- **Structure**: An "Island" is a fully self-hosted, independently runnable frontend app scoped to one platform surface (web, mobile, watch, TV, etc.), living under `src/Frontend/Islands/<Platform>/`. There is no shared gateway host multiplexing between Islands — each Island runs on its own port/process, and system- or user-level configuration decides which Island(s) are active/reachable.
- **Web Island (current)**: `Telara.Client` is a standalone Blazor WebAssembly SPA — it owns its own routing and renders entirely in-browser, with no server-rendered MVC views or page layer in front of it. `Telara.Web` is that Island's own thin ASP.NET Core host, whose only job is serving `Telara.Client`'s compiled static files and falling back to `index.html`; it holds no view/page logic of its own.
- **Data access**: Every Island talks to `Telara.OpsApi` independently over GraphQL/HTTP (Hot Chocolate) — no SignalR dependency for basic rendering, and no cross-Island shared state. This is what lets a future React or native Island be added as a sibling under `src/Frontend/Islands/` without touching the Web Island or the backend.

### 5. Escalation & Communications Domain (Java 21 / Spring Boot)
- **Boundary**: Completely isolated within the `Telara.NotificationService` directory utilizing Maven (`pom.xml`). 
- **Role**: Functions as a dedicated microservice sandbox designed to execute high-volume messaging and automated escalation workflows, integrating with the .NET layer via lightweight network contracts.

## Coding Style & Standards
- **C# 14**: Implement file-scoped namespaces, implicit usings, nullable annotations, and primary constructors where applicable. 
- **Namespaces**: Keep a clean namespace layout (`Telara.Core.*`, `Telara.Domain.*`, `Telara.OpsApi.*`). Interfaces live in `Interfaces/` subdirectories with the namespace suffix `.Interfaces`. Domain models implement their corresponding interface (e.g., `Tool : ITool`). Do not apply interface inheritance models directly onto rich or behavior-driven domain models.
