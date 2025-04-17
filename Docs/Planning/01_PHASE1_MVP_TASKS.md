---
title: "Phase 1: MVP - Core Console Interaction & Basic Backend Tasks"
description: "Detailed tasks for implementing the Nucleus OmniRAG Minimum Viable Product (MVP) focused on a Console Application client and backend API."
version: 1.2 # Updated after basic AI integration
date: 2025-04-17 # Today's date
---

# Phase 1: MVP - Core **Console Interaction** & Basic Backend Tasks

**Epic:** [`EPIC-MVP-CONSOLE`](./00_ROADMAP.md#phase-1-mvp---core-console-interaction--basic-backend)
**Requirements:** [`01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`](../Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md)
**Architecture:** [`00_ARCHITECTURE_OVERVIEW.md`](../Architecture/00_ARCHITECTURE_OVERVIEW.md), [`05_ARCHITECTURE_CLIENTS.md`](../Architecture/05_ARCHITECTURE_CLIENTS.md)

This document details the specific tasks required to complete Phase 1. The focus is on establishing a **Console Application (`Nucleus.Console`)** as the primary interaction point. This approach prioritizes **accelerating the development iteration loop for backend logic, persona integration, and agentic workflows**, providing strong synergy with AI-assisted development before building user-facing UIs.

We will leverage **.NET 9 and Aspire** for local development orchestration and service configuration, including emulated Azure services (Cosmos DB).

---

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
*   [X] **TASK-MVP-PER-02:** Implement `BootstrapperPersona` class inheriting `IPersona<T>`. (Ref Code: `Nucleus.Personas/Bootstrapper/BootstrapperPersona.cs` - TBD)
*   [X] **TASK-MVP-PER-03:** Integrate preferred LLM client (`IGenerativeAI`) into the `BootstrapperPersona` via DI. (Ref Code: `Nucleus.Personas/Bootstrapper/BootstrapperPersona.cs`)
*   [ ] **TASK-MVP-PER-04:** Define `IPersona` interface (refine as needed from architecture doc, include `HandleQueryAsync`). (Ref Code: `Nucleus.Abstractions/Interfaces/IPersona.cs` - TBD)
*   [ ] **TASK-MVP-PER-05:** Implement `HandleQueryAsync` logic for `BootstrapperPersona`:
    *   Construct prompt using query and potentially minimal context (TBD).
    *   Call LLM (via `IChatClient`) to generate response.
    *   Return response to caller (API).
*   [ ] **TASK-MVP-PER-06:** Register `BootstrapperPersona` with Dependency Injection in `Nucleus.ApiService`. (Ref Code: `Nucleus.ApiService/Program.cs`)

## `ISSUE-MVP-API-01`: Develop Backend API (WebAPI for Console)
*(Ref Arch: [`07_ARCHITECTURE_DEPLOYMENT.md`](../Architecture/07_ARCHITECTURE_DEPLOYMENT.md))*
*   [X] **TASK-MVP-API-01:** **Re-implement/Refine** `Nucleus.ApiService` project (replacing any template placeholders) with necessary services (DI, logging, configuration, controllers). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [X] **TASK-MVP-API-02:** Define API controllers and endpoints relevant for Console App interaction (e.g., `/api/ingest`, `/api/query`, `/api/status`). (Ref Code: `Nucleus.ApiService/Controllers/` - TBD)
*   [X] **TASK-MVP-API-03:** Wire up the `BootstrapperPersona` within the API controllers (e.g., `/api/query` should invoke the persona). (Ref Code: `Nucleus.ApiService/Controllers/QueryController.cs`)
*   [ ] **TASK-MVP-API-04:** Implement the `/api/ingest` endpoint to inject and call `IContentExtractor` selection logic. (Ref Code: `Nucleus.ApiService/Controllers/IngestController.cs` - TBD)
*   [ ] **TASK-MVP-API-05:** Implement basic health check endpoint (`/healthz`). (Ref Code: `Nucleus.ApiService/Program.cs`)
*   [ ] **TASK-MVP-API-06:** Ensure API configuration and DI are correctly set up. (Ref Code: `Nucleus.ApiService/Program.cs`)

## `ISSUE-MVP-CONSOLE-01`: Create Minimal **Console Application** Interface
*(Ref Arch: [`05_ARCHITECTURE_CLIENTS.md`](../Architecture/05_ARCHITECTURE_CLIENTS.md), [`ARCHITECTURE_ADAPTERS_CONSOLE.md`](../Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md))*
*   [ ] **TASK-MVP-CON-01:** Set up `Nucleus.Console` project structure (e.g., using `System.CommandLine` or similar library for command parsing). (Ref Code: `Nucleus.Console/Program.cs`)
*   [ ] **TASK-MVP-CON-02:** Implement basic command structure (e.g., `nucleus ingest <path>`, `nucleus query "<text>"`, `nucleus status`).
*   [ ] **TASK-MVP-CON-03:** Implement HTTP client logic within `Nucleus.Console` to call the `Nucleus.ApiService` endpoints.
    *   Ensure `HttpClient` is configured correctly (base address injected by Aspire). (Ref Code: `Nucleus.Console/Services/ApiClient.cs` - TBD)
*   [ ] **TASK-MVP-CON-04:** Implement logic for the `query` command:
    *   Parse query text.
    *   Call `/api/query` endpoint.
    *   Display formatted response to console.
*   [ ] **TASK-MVP-CON-05:** Implement basic logic for the `ingest` command (TBD - initial version might just send a path to `/api/ingest`).
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
