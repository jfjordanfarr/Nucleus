---
title: Namespace - Nucleus.Infrastructure.Data.Persistence
description: Describes the core infrastructure project responsible for all data persistence concerns, primarily implementing repository interfaces for Azure Cosmos DB.
version: 3.0
date: 2025-05-29
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Data.Persistence

**Relative Path:** `src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Data.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj`

## 1. Purpose

This project is the primary infrastructure component for all data persistence operations within the Nucleus platform. It implements data access logic, focusing on interactions with Azure Cosmos DB. It belongs to the Infrastructure Layer and is responsible for translating domain or application requests into concrete database operations, encapsulating all Cosmos DB-specific implementation details. This project implements repository interfaces defined in `Nucleus.Shared.Kernel.Abstractions.Data`.

## 2. Key Components

*   **Repository Implementations:** Concrete implementations of repository interfaces defined in `Nucleus.Shared.Kernel.Abstractions.Data` (e.g., `CosmosDbArtifactMetadataRepository` implementing `IArtifactMetadataRepository`, `CosmosDbPersonaKnowledgeRepository` implementing `IPersonaKnowledgeRepository`, `CosmosDbAgentStateRepository` implementing `IAgentStateRepository`).
*   **Cosmos DB Client Setup & Configuration:** Configuration and initialization of the `CosmosClient`, including connection string management, database/container creation logic (if managed by the app), and retry policies.
*   **Data Mapping Logic:** Logic to map between domain models (from `Nucleus.Shared.Kernel.Domain` or `Nucleus.Shared.Kernel.Abstractions`) and any internal data models specific to the Cosmos DB schema or SDK requirements.
*   **Querying Logic:** Implementation of specific queries using the Cosmos DB SDK (e.g., SQL queries, LINQ if using the EF Core provider, or direct SDK methods).
*   **Service Collection Extensions:** Extension methods for `IServiceCollection` to register repository implementations and other necessary services for dependency injection.

## 3. Dependencies

*   `src/Nucleus.Shared.Kernel/Abstractions/Nucleus.Shared.Kernel.Abstractions.csproj` (References data access interfaces like `IArtifactMetadataRepository`, `IPersonaKnowledgeRepository`, `IAgentStateRepository`, and related DTOs/Models)
*   `src/Nucleus.Shared.Kernel/Domain/Nucleus.Shared.Kernel.Domain.csproj` (References core domain entities if direct mapping occurs)
*   Azure Cosmos DB SDK (`Microsoft.Azure.Cosmos`)
*   `Microsoft.Extensions.DependencyInjection.Abstractions`
*   `Microsoft.Extensions.Options.ConfigurationExtensions` (For configuration handling)

## 4. Dependents

*   `src/Nucleus.Core/RAGLogic/Nucleus.Core.RAGLogic.csproj` (Consumes repositories for RAG operations)
*   `src/Nucleus.Core/AgentRuntime/Nucleus.Core.AgentRuntime.csproj` (Consumes repositories for agent state and configuration)
*   `src/Nucleus.AppHost/Nucleus.AppHost.csproj` (For dependency injection setup and service orchestration)
*   Potentially other Core or Service layer projects requiring data access.

## 5. Related Documents

*   [../01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [../../../CoreNucleus/04_DATABASE_AND_STATE.md](../../../CoreNucleus/04_DATABASE_AND_STATE.md)
*   [../../../Deployment/02_INFRASTRUCTURE_PROVISIONING.md](../../../Deployment/02_INFRASTRUCTURE_PROVISIONING.md) (Related to Cosmos DB setup)
