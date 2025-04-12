# Nucleus OmniRAG: Project Plan (Kanban) - .NET/Azure (Cosmos DB Focus)

**Attention AI Assistant:** This is the **MACROSTATE**. Use this for overall context and task progression. Update less frequently than Session State, primarily when tasks move between columns.

**Last Updated:** `2025-03-30` (Adjust to current date)

---

## 🚀 Backlog (Ideas & Future Work)

*   [ ] Implement `Nucleus.Orchestrations` using Azure Durable Functions for stateful persona workflows.
*   [ ] Implement advanced agentic query strategies (multi-step, recursive confidence) in `Nucleus.Api`/`Nucleus.Functions` (`query_service` logic).
*   [ ] Implement `IStateStore` interface and adapters (Cosmos DB?) for Durable Functions state.
*   [ ] Implement `IEventPublisher` interface and `ServiceBusPublisher` adapter in `Nucleus.Infrastructure`.
*   [ ] Implement additional personas (HealthcareIntelligence, GeneralKnowledge) in `Nucleus.Personas`.
*   [ ] Create Bicep/Terraform templates for Azure resource deployment (`infra/`).
*   [ ] Implement comprehensive integration tests (`tests/Nucleus.IntegrationTests`) using Testcontainers for Cosmos DB/Azurite.
*   [ ] Add robust configuration validation (`Microsoft.Extensions.Options.DataAnnotations`).
*   [ ] Implement caching strategies (`IDistributedCache`).
*   [ ] Develop UI/Frontend integration strategy (consider Blazor, React, etc.).
*   [ ] Add detailed logging/telemetry integration with Application Insights via Aspire ServiceDefaults.
*   [ ] Implement TDD: Write unit tests for implemented services/adapters.

## 🔨 Ready (Prioritized for Near-Term Development - After Abstractions)

*   [ ] **TASK-ID-002:** Implement Infrastructure Adapters (`Nucleus.Infrastructure`) for Core Services (Cosmos DB Repo, Blob Storage, Cloud AI Client).
*   [ ] **TASK-ID-003:** Implement Processing Services (`Nucleus.Processing`) for Chunking and Embedding.
*   [ ] **TASK-ID-004:** Implement `Nucleus.Api` project (ASP.NET Core) with basic setup and DI wiring for services/repositories.
*   [ ] **TASK-ID-005:** Implement `Nucleus.Functions` project with basic setup (Isolated Worker) and Service Bus trigger template.
*   [ ] **TASK-ID-006:** Configure `Nucleus.AppHost` (Aspire) to launch API, Functions, Cosmos DB emulator, Azurite.
*   [ ] **TASK-ID-007:** Implement basic file ingestion endpoint/workflow orchestrating upload, analysis, chunking, embedding, and storage via defined services/interfaces.
*   [ ] **TASK-ID-008:** Implement basic retrieval service (`IRetrievalService`) including custom ranking logic.
*   [ ] **TASK-ID-009:** Write Unit Tests for Core Abstractions and Models (`tests/Nucleus.Core.Tests`, `tests/Nucleus.Abstractions.Tests`).

## 👨‍💻 In Progress (Max 1-2 Active Items)

*   [ ] **TASK-ID-001:** Implement Core Abstractions (Cosmos DB Focus) *(See `02_CURRENT_SESSION_STATE.md` for active sub-task)*
    *   [x] Initial Project Scaffolding & AgentOps Setup Complete (Cosmos DB).
    *   [ ] Implement `LearningChunkDocument` model in `Nucleus.Core`.
    *   [ ] Implement `RankedResult` model in `Nucleus.Core`.
    *   [ ] Implement `ILearningChunkRepository` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IChunkerService` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IEmbeddingService` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IRankingStrategy` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IRetrievalService` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IAiClient` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IPersona` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IFileMetadataRepository` interface in `Nucleus.Abstractions`.
    *   [ ] Implement `IFileStorage` interface in `Nucleus.Abstractions`.
    *   [ ] Ensure all abstractions use `Task`/`Task<T>` and `CancellationToken`.
    *   [ ] Add XML documentation comments to all public interfaces/models/methods/properties.

## ✅ Done (Recently Completed)

*   [x] **TASK-ID-000:** Initial Project Scaffolding & AgentOps Setup (Cosmos DB Decision)
    *   [x] Created Solution (`.sln`) and complete Project (`.csproj`) structure.
    *   [x] Configured `.gitignore` for .NET/Aspire.
    *   [x] Created initial `README.md`.
    *   [x] Created and populated `AgentOps` folder reflecting Cosmos DB architecture & collaboration guidelines.
    *   [x] Created Project Mandate document (`docs/00_PROJECT_MANDATE.md`).
    *   [x] Generated complete code skeletons for all projects.

## 🚧 Blockers

*   [ ] *(Track any specific blockers preventing progress on 'In Progress' items)*

---
*(Optional: Add sections for Milestones, Releases, etc. as needed)*