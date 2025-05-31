<!-- 
**THIS DOCUMENT IS ARCHIVED**

This document is no longer actively maintained and is preserved for historical context only. 
Refer to the main architectural documents for current information.
-->

<!-- filepath: /workspaces/Nucleus/Docs/Architecture/DevelopmentLifecycle/Namespaces/NAMESPACE_SERVICES_API.md -->
---
title: Namespace - Nucleus.Services.Api
description: Describes the main API service project for Nucleus, providing the HTTP endpoints for client interaction.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Services.Api

**Relative Path:** `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj`

## 1. Purpose

This project implements the primary backend HTTP API for the Nucleus system. It follows ASP.NET Core conventions and serves as the main entry point for all client adapters (Console, Teams, etc.). It receives requests, orchestrates the necessary domain logic (via `OrchestrationService` and `PersonaManager`), interacts with persistence layers, and returns responses.

## 2. Key Components

*   **Controllers:** ASP.NET Core API Controllers defining endpoints (e.g., `InteractionController`).
*   **`Program.cs` / `Startup.cs`:** Configures the ASP.NET Core host, including dependency injection, authentication, authorization, middleware pipeline, etc.
*   **Dependency Injection Setup:** Registers services from Domain, Infrastructure, and Abstractions layers.
*   **API Models/DTOs:** May define specific models for API contracts if they differ from core Abstractions (though often Abstraction DTOs are used directly).

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj`
*   `src/Nucleus.Domain/Nucleus.Domain.Processing/Nucleus.Domain.Processing.csproj`
*   `src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Nucleus.Domain.Personas.Core.csproj`
*   `src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj`
*   `Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj` (Applies shared Aspire configurations)
*   Potentially `src/Nucleus.Application/`.
*   ASP.NET Core packages (`Microsoft.AspNetCore.*`).

## 4. Dependents

*   `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (Hosts this service during development).
*   `tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj` (Tests this API).
*   **Implicitly Depended On By:** Client adapters (`Nucleus.Adapters.Console`, `Nucleus.Adapters.Teams`) via HTTP calls.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [10_ARCHITECTURE_API.md](../10_ARCHITECTURE_API.md)
*   [07_ARCHITECTURE_DEPLOYMENT.md](../07_ARCHITECTURE_DEPLOYMENT.md)
