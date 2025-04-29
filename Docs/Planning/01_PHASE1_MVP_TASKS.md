---
title: "Phase 1: MVP - Core API Foundation & Initial Validation Tasks"
description: "Detailed tasks for implementing the Nucleus Minimum Viable Product (MVP) focused on the core backend API Service and its validation via an initial Console client."
version: 1.5
date: 2025-04-27
---

# Phase 1: MVP - Core **API Foundation** & Initial Validation Tasks

**Epic:** [`EPIC-MVP-API`](./00_ROADMAP.md#phase-1-mvp---core-api-foundation--initial-validation)
**Requirements:** [`01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`](../Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md)
**Architecture:** [`00_ARCHITECTURE_OVERVIEW.md`](../Architecture/00_ARCHITECTURE_OVERVIEW.md), [`10_ARCHITECTURE_API.md`](../Architecture/10_ARCHITECTURE_API.md)

This document details the specific tasks required to complete Phase 1. The focus is on establishing the **Core API Service (`Nucleus.ApiService`)** and its foundational components (backend logic, persona integration, basic data storage). An initial **Console Application (`Nucleus.Console`)** will be developed concurrently to serve as a **reference client for validating** the API endpoints and interaction flows. This approach prioritizes building a robust API foundation first, enabling parallel development and testing.

We will leverage **.NET 9 and Aspire** for local development orchestration and service configuration, including emulated Azure services (Cosmos DB).

## `ISSUE-MVP-SETUP-01`: Establish Core Project Structure & Local Environment
*   [X] **TASK-MVP-SETUP-01:** Create Solution Structure (`Nucleus.sln`, `src/`, `tests/`, `aspire/`, `docs/`, etc.). (Ref Code: `Nucleus.sln`)
*   [X] **TASK-MVP-SETUP-02:** Set up `Nucleus.AppHost` project using Aspire. (Ref Code: `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-03:** Configure Aspire AppHost to launch required emulated services (Cosmos DB). *(Queues/Service Bus emulation deferred)*. (Ref Code: `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-04:** Create core projects: `Nucleus.Abstractions`, `Nucleus.Core`, `Nucleus.Infrastructure`, `Nucleus.Processing`, `Nucleus.Personas`. (Ref Code: `Nucleus.sln`)
*   [X] **TASK-MVP-SETUP-05:** Create `Nucleus.ApiService` (ASP.NET Core WebAPI) project and add to AppHost. (Ref Code: `Nucleus.ApiService/`, `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-06:** Create `Nucleus.Console` (.NET Console App) project and add to AppHost. (Ref Code: `Nucleus.Console/`, `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-07:** Ensure AppHost correctly injects connection strings/service URIs into `Nucleus.ApiService` and `Nucleus.Console`. (Ref Config: Aspire Configuration)
*   [X] **TASK-MVP-SETUP-08:** Configure preferred LLM provider (e.g., Google Gemini) and necessary configuration (API keys via user secrets/env vars).

## `ISSUE-MVP-PROCESS-01`: Develop Basic Content Extraction (Foundation for API Processing)
*(Ref Arch: [`01_ARCHITECTURE_PROCESSING.md`](../Architecture/01_ARCHITECTURE_PROCESSING.md), [`ARCHITECTURE_PROCESSING_INGESTION.md`](../Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md))* // Note: Extraction happens within API after content retrieval.
*   [ ] **TASK-MVP-EXT-01:** Define `IContentExtractor` interface. (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD) // Used by API after getting content via IArtifactProvider
*   [ ] **TASK-MVP-EXT-02:** Implement `PlainTextExtractor` for `text/plain` MIME type. (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-03:** Implement `HtmlExtractor` for `text/html` MIME type (use a library like HtmlAgilityPack to sanitize/extract text). (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-04:** Integrate `IContentExtractor` selection logic within the **`Nucleus.ApiService`** processing pipeline (called after ephemeral content retrieval via `IArtifactProvider`). (Ref Code: `Nucleus.ApiService/...` - TBD)

## `ISSUE-MVP-PERSONA-01`: Create Initial **Bootstrapper Persona**
*(Ref Arch: [`02_ARCHITECTURE_PERSONAS.md`](../Architecture/02_ARCHITECTURE_PERSONAS.md), [`ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md`](../Architecture/Personas/ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md))*
*   [ ] **TASK-MVP-PER-01:** Define the output C# record model(s) for the `BootstrapperPersona`'s structured analysis/knowledge representation. (Ref Code: `Nucleus.Personas/Bootstrapper/Models/` - TBD)
*   [ ] **TASK-MVP-PER-02:** Implement `BootstrapperPersona` logic (`AnalyzeContentAsync`, `HandleInteractionAsync`).
*   [ ] **TASK-MVP-PER-03:** Integrate `BootstrapperPersona` into the API's interaction handling flow.

## `ISSUE-MVP-API-01`: Develop Backend API (Unified Interaction Endpoint)
*(Ref Arch: [`10_ARCHITECTURE_API.md`](../Architecture/10_ARCHITECTURE_API.md), [`ARCHITECTURE_API_INTERACTIONS.md`](../Architecture/Api/ARCHITECTURE_API_INTERACTIONS.md))* 
*   [X] **TASK-MVP-API-01:** **Re-implement/Refine** `Nucleus.ApiService` project (replacing any template placeholders) with necessary services (DI, logging, configuration, controllers). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [ ] **TASK-MVP-API-02:** Define API controllers and the primary **unified interaction endpoint** (`POST /api/v1/interactions`). (Ref Code: `Nucleus.ApiService/Controllers/InteractionsController.cs` - TBD)
*   [ ] **TASK-MVP-API-03:** Define DTOs for the unified interaction model (`InteractionRequest`, `InteractionResponse`, `ArtifactReference`, `ProcessingStatus`, etc.). (Ref Code: `Nucleus.Core/Models/Api/` - TBD)
*   [ ] **TASK-MVP-API-04:** Implement core logic within the `/interactions` endpoint:
    *   [ ] Receive `InteractionRequest`.
    *   [ ] If `ArtifactReference` is present, invoke `IArtifactProvider` to get content ephemerally (Note: MVP has only `LocalFileArtifactProvider` initially, called by Console).
    *   [ ] Invoke `IContentExtractor` (if content retrieved).
    *   [ ] Route to appropriate `IPersona` (`BootstrapperPersona` for MVP).
    *   [ ] Handle synchronous response or initiate async processing (via queue/orchestrator later - MVP likely sync).
    *   [ ] Return `InteractionResponse` (with sync result or Job ID).
*   [ ] **TASK-MVP-API-05:** Implement API endpoint for querying job status (e.g., `GET /api/v1/jobs/{jobId}/status`).
*   [ ] **TASK-MVP-API-06:** Implement basic health checks (`/healthz`).
*   [ ] **TASK-MVP-API-07:** Implement initial `IArtifactProvider` for local files (`LocalFileArtifactProvider`) - potentially within `Nucleus.Infrastructure` or `Nucleus.ApiService` for MVP simplicity. *(Note: This is technically used by the Console client indirectly via the API call in MVP)*

## `ISSUE-MVP-CONSOLE-01`: Create Minimal **Console Client** (Reads local files, passes ArtifactReference to API)
*(Ref Arch: [`05_ARCHITECTURE_CLIENTS.md`](../Architecture/05_ARCHITECTURE_CLIENTS.md), [`ARCHITECTURE_ADAPTERS_CONSOLE.md`](../Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md))*
*   [ ] **TASK-MVP-CON-01:** Set up `Nucleus.Console` project structure (e.g., using `System.CommandLine` or similar library for command parsing). (Ref Code: `Nucleus.Console/Program.cs`)
*   [ ] **TASK-MVP-CON-02:** Implement basic command structure (e.g., `nucleus ask "<query>" [--file <path>]`, `nucleus status <jobId>`).
*   [ ] **TASK-MVP-CON-03:** Implement HTTP client logic within `Nucleus.Console` to call the `Nucleus.ApiService` endpoints (`/interactions`, `/jobs/.../status`).
    *   Ensure `HttpClient` is configured correctly (base address injected by Aspire). (Ref Code: `Nucleus.Console/Services/ApiClient.cs` - TBD)
*   [ ] **TASK-MVP-CON-04:** Implement logic for the `ask` command:
    *   [ ] Construct `InteractionRequest` DTO.
    *   [ ] If `--file` is provided, **read the local file metadata (path, name, type)** and create an appropriate `ArtifactReference` (e.g., using a `file://` scheme or specific type indicator for local files).
    *   [ ] Call `POST /api/v1/interactions` with the request DTO.
    *   [ ] Display the `InteractionResponse` (sync result or Job ID).
*   [ ] **TASK-MVP-CON-05:** Implement logic for the `status` command to call `GET /api/v1/jobs/{jobId}/status` and display the result.
*   [ ] **TASK-MVP-CON-06:** Implement basic error handling and display for API call failures.

## `ISSUE-MVP-INFRA-01`: Define Basic Infrastructure (as Code)
*(Ref Arch: [`07_ARCHITECTURE_DEPLOYMENT.md`](../Architecture/07_ARCHITECTURE_DEPLOYMENT.md), [`ARCHITECTURE_HOSTING_AZURE.md`](../Architecture/Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md))*
*   [ ] **TASK-MVP-INFRA-01:** Define basic infrastructure required for hosting the **API** (e.g., App Service/Container App). *(Console App runs locally via AppHost for MVP)*.
*   [ ] **TASK-MVP-INFRA-02:** Define Azure resources needed:
    *   App Service Plan / Container Apps Environment
    *   API App/Container App for Backend (**`Nucleus.ApiService`**)
    *   Cosmos DB for NoSQL account & database/container
    *   (Optional) Azure AI Search (if used for RAG)
    *   (Optional) Google Gemini or other LLM endpoint access config
*   [ ] **TASK-MVP-INFRA-03:** Write/modify Bicep or Terraform templates for these resources. (Ref: `infra/` folder - TBD)
*   [ ] **TASK-MVP-INFRA-04:** Parameterize templates for different environments (dev/test).
*   [ ] **TASK-MVP-INFRA-05:** Set up basic deployment pipeline (GitHub Actions / ADO / `azd`) for the API and infrastructure. (Ref: `.github/workflows/` - TBD)

## `ISSUE-MVP-RETRIEVAL-01`: Implement Basic **Metadata** Store & Retrieval Foundation
*(Ref Arch: [`04_ARCHITECTURE_DATABASE.md`](../Architecture/04_ARCHITECTURE_DATABASE.md))*
*   [ ] **TASK-MVP-RET-01:** Define `ArtifactMetadata` C# record (source info like `ArtifactReference`, basic properties like name/type, timestamp, ID, owner/user ID). (Ref Code: `Nucleus.Core/Models/` - TBD)
*   [ ] **TASK-MVP-RET-02:** Define `PersonaKnowledgeEntry` C# record (persona ID, analysis result/summary, source `ArtifactMetadata` ID, timestamp, embeddings). (Ref Code: `Nucleus.Core/Models/` - TBD)
*   [ ] **TASK-MVP-RET-03:** Define `IArtifactMetadataRepository` & `IPersonaKnowledgeRepository` interfaces. (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-MVP-RET-04:** Implement Cosmos DB adapters for repositories in `Nucleus.Infrastructure`. (Ref Code: `Nucleus.Infrastructure/Repositories/CosmosDb/` - TBD)
*   [ ] **TASK-MVP-RET-05:** Define `IEmbeddingGenerator` interface (using `Microsoft.Extensions.AI`). (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-MVP-RET-06:** Implement adapter for chosen embedding model (e.g., `GoogleGeminiEmbeddingGenerator`). (Ref Code: `Nucleus.Infrastructure/Services/AI/` - TBD)
*   [ ] **TASK-MVP-RET-07:** **(Defer Complex Retrieval)** Define `IRetrievalService` interface (simple initial version focusing on metadata/knowledge saving, maybe basic lookup by ID?).
*   [ ] **TASK-MVP-RET-08:** **(Defer Complex Retrieval)** Implement `BasicRetrievalService` (mostly placeholder/basic save logic integration).
*   [ ] **TASK-MVP-RET-09:** Integrate metadata/knowledge saving into the `BootstrapperPersona`'s interaction flow (e.g., after analysis, save `ArtifactMetadata` and `PersonaKnowledgeEntry` via repositories).

---
