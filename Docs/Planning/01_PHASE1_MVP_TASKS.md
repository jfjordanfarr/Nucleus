---
title: "Phase 1: MVP - Core API Foundation & Initial Validation Tasks"
description: "Detailed tasks for implementing the Nucleus OmniRAG Minimum Viable Product (MVP) focused on the core backend API Service and its validation via an initial Console client."
version: 1.3 # Aligned with API-First MVP
date: 2025-04-22 # Today's date
---

# Phase 1: MVP - Core **API Foundation** & Initial Validation Tasks

**Epic:** [`EPIC-MVP-API`](./00_ROADMAP.md#phase-1-mvp---core-api-foundation--initial-validation)
**Requirements:** [`01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`](../Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md) *(Note: Title reflects Console Client, but content is API-focused)*
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

## `ISSUE-MVP-PROCESS-01`: Develop Basic Content Extraction (Foundation for Ingestion)
*(Ref Arch: [`01_ARCHITECTURE_PROCESSING.md`](../Architecture/01_ARCHITECTURE_PROCESSING.md), [`ARCHITECTURE_PROCESSING_INGESTION.md`](../Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md))*
*   [ ] **TASK-MVP-EXT-01:** Define `IContentExtractor` interface. (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-MVP-EXT-02:** Implement `PlainTextExtractor` for `text/plain` MIME type. (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-03:** Implement `HtmlExtractor` for `text/html` MIME type (use a library like HtmlAgilityPack to sanitize/extract text). (Ref Code: `Nucleus.Processing/Services/Extractors/` - TBD)
*   [ ] **TASK-MVP-EXT-04:** Integrate `IContentExtractor` selection logic within the **`Nucleus.ApiService`** based on artifact MIME type. *(Used by Console `ingest` command later)* (Ref Code: `Nucleus.ApiService/Controllers/IngestController.cs` - TBD)

## `ISSUE-MVP-PERSONA-01`: Create Initial **Bootstrapper Persona**
*(Ref Arch: [`02_ARCHITECTURE_PERSONAS.md`](../Architecture/02_ARCHITECTURE_PERSONAS.md), [`ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md`](../Architecture/Personas/ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md))*
*   [ ] **TASK-MVP-PER-01:** Define the output C# record model(s) for the `BootstrapperPersona`'s structured analysis/knowledge representation. (Ref Code: `Nucleus.Personas/Bootstrapper/Models/` - TBD)
*   [X] **TASK-MVP-PER-02:** Implement `BootstrapperPersona` class inheriting `IPersona<T>`. (Ref Code: `Nucleus.Personas/Bootstrapper/BootstrapperPersona.cs`)
*   [X] **TASK-MVP-PER-03:** Implement basic `HandleQueryAsync` logic in `BootstrapperPersona` to call the configured AI model (e.g., Gemini via `Microsoft.Extensions.AI` abstractions). (Ref Code: `Nucleus.Personas/Bootstrapper/BootstrapperPersona.cs`)
*   [X] **TASK-MVP-PER-04:** Implement basic `HandleIngestionAsync` placeholder logic in `BootstrapperPersona` (may just log for MVP). (Ref Code: `Nucleus.Personas/Bootstrapper/BootstrapperPersona.cs`)

## `ISSUE-MVP-API-01`: Develop Backend **API Service**
*(Ref Arch: [`10_ARCHITECTURE_API.md`](../Architecture/10_ARCHITECTURE_API.md), [`ARCHITECTURE_API_ENDPOINTS.md`](../Architecture/Api/ARCHITECTURE_API_ENDPOINTS.md) - TBD)*
*   [X] **TASK-MVP-API-01:** **Re-implement/Refine** `Nucleus.ApiService` project (replacing any template placeholders) with necessary services (DI, logging, configuration, controllers). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [X] **TASK-MVP-API-02:** Define API controllers and endpoints relevant for initial interaction (e.g., `POST /api/v1/query`, `POST /api/v1/ingest`, `GET /api/v1/status`). (Ref Code: `Nucleus.ApiService/Controllers/` - TBD)
*   [X] **TASK-MVP-API-03:** Implement controller logic to handle requests, call the appropriate persona (`BootstrapperPersona`), and return responses/status codes. (Ref Code: `Nucleus.ApiService/Controllers/` - TBD)
*   [X] **TASK-MVP-API-04:** Configure DI container in `Nucleus.ApiService` to register personas, services (`IContentExtractor`, `IPersonaKnowledgeRepository`, `IEmbeddingGenerator`, etc.). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [ ] **TASK-MVP-API-05:** Implement basic health checks (`/healthz`).

## `ISSUE-MVP-CONSOLE-01`: Create Minimal **Console Client** (for API Validation)
*(Ref Arch: [`05_ARCHITECTURE_CLIENTS.md`](../Architecture/05_ARCHITECTURE_CLIENTS.md), [`ARCHITECTURE_ADAPTERS_CONSOLE.md`](../Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md))* 
*   [ ] **TASK-MVP-CON-01:** Set up `Nucleus.Console` project structure (e.g., using `System.CommandLine` or similar library for command parsing). (Ref Code: `Nucleus.Console/Program.cs`)
*   [ ] **TASK-MVP-CON-02:** Implement basic command structure to invoke API endpoints (e.g., `nucleus query "<text>"`, `nucleus ingest <path>`, `nucleus status`).
*   [ ] **TASK-MVP-CON-03:** Implement HTTP client logic within `Nucleus.Console` to call the `Nucleus.ApiService` endpoints.
    *   Ensure `HttpClient` is configured correctly (base address injected by Aspire). (Ref Code: `Nucleus.Console/Services/ApiClient.cs` - TBD)
*   [ ] **TASK-MVP-CON-04:** Implement logic for the `query` command to call `/api/v1/query` and display the response.
*   [ ] **TASK-MVP-CON-05:** Implement basic logic for the `ingest` command to call `/api/v1/ingest`.
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

## `ISSUE-MVP-RETRIEVAL-01`: Implement Basic Knowledge Store & Retrieval
*(Ref Arch: [`04_ARCHITECTURE_DATABASE.md`](../Architecture/04_ARCHITECTURE_DATABASE.md))*
*   [ ] **TASK-MVP-RET-01:** Define `PersonaKnowledgeEntry` C# record (include persona ID, analysis result, relevant text/snippet, source identifier, timestamp, embeddings). (Ref Code: `Nucleus.Core/Models/` - TBD)
*   [ ] **TASK-MVP-RET-02:** Define `IPersonaKnowledgeRepository` interface (methods for Save, GetById, Query/Search). (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-MVP-RET-03:** Implement `CosmosDbPersonaKnowledgeRepository` adapter in `Nucleus.Infrastructure`. (Ref Code: `Nucleus.Infrastructure/Repositories/CosmosDb/` - TBD)
*   [ ] **TASK-MVP-RET-04:** Define `IEmbeddingGenerator` interface (using `Microsoft.Extensions.AI`). (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-MVP-RET-05:** Implement adapter for chosen embedding model (e.g., `GoogleGeminiEmbeddingGenerator`). (Ref Code: `Nucleus.Infrastructure/Services/AI/` - TBD)
*   [ ] **TASK-MVP-RET-06:** **(Defer Complex Retrieval)** Define `IRetrievalService` interface (simple initial version?).
*   [ ] **TASK-MVP-RET-07:** **(Defer Complex Retrieval)** Implement `BasicRetrievalService`.
*   [ ] **TASK-MVP-RET-08:** Integrate embedding generation and saving `PersonaKnowledgeEntry` into the `BootstrapperPersona`'s interaction flow (e.g., after getting a query response, store query+response pair and embeddings via the repository).

---
