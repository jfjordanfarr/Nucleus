---
title: "Phase 1: MVP - Core API Foundation & Initial Validation Tasks"
description: "Detailed tasks for implementing the Nucleus Minimum Viable Product (MVP) focused on the core backend API Service and its validation via internal integration (e.g., `LocalAdapter`) and direct API testing."
version: 1.7
date: 2025-05-06
---

# Phase 1: MVP - Core **API Foundation** & Initial Validation Tasks

**Epic:** [`EPIC-MVP-API`](./00_ROADMAP.md#phase-1-mvp---core-api-foundation--initial-validation)
**Requirements:** [`01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`](../Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md)
**Architecture:** [`00_ARCHITECTURE_OVERVIEW.md`](../Architecture/00_ARCHITECTURE_OVERVIEW.md), [`10_ARCHITECTURE_API.md`](../Architecture/10_ARCHITECTURE_API.md)

This document details the specific tasks required to complete Phase 1. The focus is on establishing the **Core API Service (`Nucleus.ApiService`)** and its foundational components (backend logic, persona integration, basic data storage). Validation will occur through **internal integration (e.g., using `Nucleus.Infrastructure.Adapters.Local`) and direct API testing**. This approach prioritizes building a robust API foundation first.

We will leverage **.NET 9 and Aspire** for local development orchestration and service configuration, including emulated Azure services (Cosmos DB).

## `ISSUE-MVP-SETUP-01`: Establish Core Project Structure & Local Environment
*   [X] **TASK-MVP-SETUP-01:** Create Solution Structure (`Nucleus.sln`, `src/`, `tests/`, `aspire/`, `docs/`, etc.). (Ref Code: `Nucleus.sln`)
*   [X] **TASK-MVP-SETUP-02:** Set up `Nucleus.AppHost` project using Aspire. (Ref Code: `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-03:** Configure Aspire AppHost to launch required emulated services (Cosmos DB). *(Queues/Service Bus emulation deferred)*. (Ref Code: `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-04:** Create core projects: `Nucleus.Abstractions`, `Nucleus.Core`, `Nucleus.Infrastructure`, `Nucleus.Processing`, `Nucleus.Personas`. (Ref Code: `Nucleus.sln`)
*   [X] **TASK-MVP-SETUP-05:** Create `Nucleus.ApiService` (ASP.NET Core WebAPI) project and add to AppHost. (Ref Code: `Nucleus.ApiService/`, `Nucleus.AppHost/Program.cs`)
*   [X] **TASK-MVP-SETUP-07:** Ensure AppHost correctly injects connection strings/service URIs into `Nucleus.ApiService`. (Ref Config: Aspire Configuration)
*   [X] **TASK-MVP-SETUP-08:** Configure preferred LLM provider (e.g., Google Gemini) and necessary configuration (API keys via user secrets/env vars).

## `ISSUE-MVP-PROCESS-01`: Develop Basic Content Extraction (Foundation for API Processing)
*(Ref Arch: [`01_ARCHITECTURE_PROCESSING.md`](../Architecture/01_ARCHITECTURE_PROCESSING.md), [`ARCHITECTURE_PROCESSING_INGESTION.md`](../Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md))* // Note: Extraction happens within API after content retrieval.
*   [ ] **TASK-MVP-EXT-01:** Define `IContentExtractor` interface. (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD) // Used by API after getting content via IArtifactProvider
*   [ ] **TASK-MVP-EXT-02:** Implement `PlainTextExtractor` for `text/plain` MIME type. (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-03:** Implement `HtmlExtractor` for `text/html` MIME type (use a library like HtmlAgilityPack to sanitize/extract text). (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-04:** Integrate `IContentExtractor` selection logic within the **`Nucleus.ApiService`** processing pipeline (called after ephemeral content retrieval via `IArtifactProvider`). (Ref Code: `Nucleus.ApiService/...` - TBD)

## `ISSUE-MVP-PERSONA-01`: Create Initial **Bootstrapper Persona**
*(Ref Arch: [`02_ARCHITECTURE_PERSONAS.md`](../Architecture/02_ARCHITECTURE_PERSONAS.md), [`ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md`](../Architecture/Personas/ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md))*
*   [ ] **TASK-MVP-PER-01:** Define the expected JSON structure for the `BootstrapperPersona`'s analytical output (to be stored in `PersonaKnowledgeEntry.AnalysisData`). (Ref Arch: `ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md` for schema details)
*   [ ] **TASK-MVP-PER-02:** Implement `BootstrapperPersona` logic (`AnalyzeContentAsync`, `HandleInteractionAsync`).
*   [ ] **TASK-MVP-PER-03:** Integrate `BootstrapperPersona` into the API's interaction handling flow.

## `ISSUE-MVP-API-01`: Develop Backend API (Unified Interaction Endpoint)
*(Ref Arch: [`10_ARCHITECTURE_API.md`](../Architecture/10_ARCHITECTURE_API.md), [`ARCHITECTURE_API_INTERACTIONS.md`](../Architecture/Api/ARCHITECTURE_API_INTERACTIONS.md))* 
*   [X] **TASK-MVP-API-01:** **Re-implement/Refine** `Nucleus.ApiService` project (replacing any template placeholders) with necessary services (DI, logging, configuration, controllers). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [ ] **TASK-MVP-API-02:** Define API controllers and the primary **unified interaction endpoint** (`POST /api/v1/interactions`). (Ref Code: `Nucleus.ApiService/Controllers/InteractionsController.cs` - TBD)
*   [ ] **TASK-MVP-API-03:** Define DTOs for the unified interaction model (`InteractionRequest`, `InteractionResponse`, `ArtifactReference`, `ProcessingStatus`, etc.). (Ref Code: `Nucleus.Core/Models/Api/` - TBD)
*   [ ] **TASK-MVP-API-04:** Implement core logic within the `/interactions` endpoint:
    *   [ ] Receive `InteractionRequest`.
    *   [ ] If `ArtifactReference` is present, invoke `IArtifactProvider` to get content ephemerally (Note: MVP has only `LocalFileArtifactProvider` initially, called by API directly).
    *   [ ] Invoke `IContentExtractor` (if content retrieved).
    *   [ ] Route to appropriate `IPersona` (`BootstrapperPersona` for MVP).
    *   [ ] Handle synchronous response or initiate async processing (via queue/orchestrator later - MVP likely sync).
    *   [ ] Return `InteractionResponse` (with sync result or Job ID).
*   [ ] **TASK-MVP-API-05:** Implement API endpoint for querying job status (e.g., `GET /api/v1/jobs/{jobId}/status`).
*   [ ] **TASK-MVP-API-06:** Implement basic health checks (`/healthz`).
*   [ ] **TASK-MVP-API-07:** Implement initial `IArtifactProvider` for local files (`LocalFileArtifactProvider`) - potentially within `Nucleus.Infrastructure` or `Nucleus.ApiService` for MVP simplicity. *(Note: This is technically used by the API directly for MVP)*

## `ISSUE-MVP-INFRA-01`: Define Basic Infrastructure (as Code)
*(Ref Arch: [`07_ARCHITECTURE_DEPLOYMENT.md`](../Architecture/07_ARCHITECTURE_DEPLOYMENT.md), [`ARCHITECTURE_HOSTING_AZURE.md`](../Architecture/Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md))*
*   [ ] **TASK-MVP-INFRA-01:** Define basic infrastructure required for hosting the **API** (e.g., App Service/Container App).
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
