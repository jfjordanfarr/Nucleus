# Phase 1: MVP - Core **Console Interaction** & Basic Backend Tasks

**Epic:** [`EPIC-MVP-CONSOLE`](./00_ROADMAP.md#phase-1-mvp---core-console-interaction--basic-backend)
**Requirements:** [`01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`](../Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md) *(Note: Requirements doc may need renaming/updating)*

This document details the specific tasks required to complete Phase 1. The focus is on establishing a **Console Application (`Nucleus.Console`)** as the primary interaction point. This approach prioritizes **accelerating the development iteration loop for backend logic, persona integration, and agentic workflows**, providing strong synergy with AI-assisted development before building user-facing UIs.

We will leverage **.NET 9 and Aspire** for local development orchestration and service configuration, including emulated Azure services.

---

## `ISSUE-MVP-SETUP-01`: Establish Core Project Structure & Local Environment

*   [X] **TASK-MVP-SETUP-01:** Create Solution Structure (`NucleusOmniRAG.sln`, `src/`, `tests/`, `aspire/`, `docs/`, etc.).
*   [X] **TASK-MVP-SETUP-02:** Set up `Nucleus.AppHost` project using Aspire.
*   [X] **TASK-MVP-SETUP-03:** Configure Aspire AppHost to launch required emulated services (Cosmos DB, Azurite - Blobs). *(Queues/Service Bus emulation deferred unless specific external integration requires them in MVP)*.
*   [X] **TASK-MVP-SETUP-04:** Create core projects: `Nucleus.Abstractions`, `Nucleus.Core`, `Nucleus.Infrastructure`, `Nucleus.Personas`.
*   [X] **TASK-MVP-SETUP-05:** Create `Nucleus.Api` (ASP.NET Core WebAPI) project and add to AppHost.
*   [X] **TASK-MVP-SETUP-06:** Create `Nucleus.Console` (.NET Console App) project and add to AppHost.
*   [X] **TASK-MVP-SETUP-07:** Ensure AppHost correctly injects connection strings/service URIs into `Nucleus.Api` and `Nucleus.Console`.
*   [ ] **TASK-MVP-SETUP-08:** Configure preferred LLM provider (e.g., Google Gemini) and necessary configuration (API keys via user secrets/env vars).

## `ISSUE-MVP-PROCESS-01`: Develop Basic Content Extraction (Foundation for Ingestion)

*   [ ] **TASK-MVP-EXT-01:** Define `IContentExtractor` interface.
*   [ ] **TASK-MVP-EXT-02:** Implement `PlainTextExtractor` for `text/plain` MIME type.
*   [ ] **TASK-MVP-EXT-03:** Implement `HtmlExtractor` for `text/html` MIME type (use a library like HtmlAgilityPack to sanitize/extract text).
*   [ ] **TASK-MVP-EXT-04:** Integrate `IContentExtractor` selection logic within the **`Nucleus.Api`** based on artifact MIME type. *(Used by Console `ingest` command later)*

## `ISSUE-MVP-PERSONA-01`: Create Initial **Bootstrapper Persona**

*   [ ] **TASK-MVP-PER-01:** Define the output C# record model(s) for the `BootstrapperPersona`'s structured analysis/knowledge representation.
*   [ ] **TASK-MVP-PER-02:** Implement `BootstrapperPersona` class inheriting `IPersona<T>`.
*   [ ] **TASK-MVP-PER-03:** Implement `AnalyzeContentAsync` logic (Initial focus on basic metadata extraction or identifying content type).
*   [ ] **TASK-MVP-PER-04:** Define `IPersona` interface (refine as needed from architecture doc, include `HandleQueryAsync`).
*   [ ] **TASK-MVP-PER-05:** Implement `HandleQueryAsync` logic for `BootstrapperPersona`:
    *   Construct prompt using query and potentially minimal context (TBD).
    *   Call LLM (via `IChatClient`) to generate response.
    *   Return response to caller (API).
*   [ ] **TASK-MVP-PER-06:** Register `BootstrapperPersona` with Dependency Injection in `Nucleus.Api`.

## `ISSUE-MVP-API-01`: Develop Backend API (WebAPI for Console)

*   [ ] **TASK-MVP-API-01:** **Re-implement/Refine** `Nucleus.Api` project (replacing any template placeholders) with necessary services (DI, logging, configuration, controllers).
*   [ ] **TASK-MVP-API-02:** Define API controllers and endpoints relevant for Console App interaction (e.g., `/api/ingest`, `/api/query`, `/api/status`).
*   [ ] **TASK-MVP-API-03:** Implement the `/api/query` endpoint to inject and call `BootstrapperPersona.HandleQueryAsync`.
*   [ ] **TASK-MVP-API-04:** Implement the `/api/ingest` endpoint (details TBD - might receive file path/content and trigger **in-process background processing**).
*   [ ] **TASK-MVP-API-05:** Implement basic health check endpoint (`/healthz`).
*   [ ] **TASK-MVP-API-06:** Ensure API configuration and DI are correctly set up.

## `ISSUE-MVP-CONSOLE-01`: Create Minimal **Console Application** Interface

*   [ ] **TASK-MVP-CON-01:** Set up `Nucleus.Console` project structure (e.g., using `System.CommandLine` or similar library for command parsing).
*   [ ] **TASK-MVP-CON-02:** Implement basic command structure (e.g., `nucleus ingest <path>`, `nucleus query "<text>"`, `nucleus status`).
*   [ ] **TASK-MVP-CON-03:** Implement HTTP client logic within `Nucleus.Console` to call the `Nucleus.Api` endpoints.
    *   Ensure `HttpClient` is configured correctly (base address injected by Aspire).
*   [ ] **TASK-MVP-CON-04:** Implement logic for the `query` command:
    *   Parse query text.
    *   Call `/api/query` endpoint.
    *   Display formatted response to console.
*   [ ] **TASK-MVP-CON-05:** Implement basic logic for the `ingest` command (TBD - initial version might just send a path to `/api/ingest`).
*   [ ] **TASK-MVP-CON-06:** Implement basic error handling and display for API call failures.

## `ISSUE-MVP-INFRA-01`: Define Basic Infrastructure (as Code)

*   [ ] **TASK-MVP-INFRA-01:** Define basic infrastructure required for hosting the **API** (e.g., App Service/Container App). *(Console App runs locally via AppHost for MVP)*.
*   [ ] **TASK-MVP-INFRA-02:** Define Azure resources needed:
    *   App Service Plan / Container Apps Environment
    *   API App/Container App for Backend (**`Nucleus.Api`**)
    *   Cosmos DB for NoSQL account & database/container
    *   Azure AI Search (if used for RAG)
    *   Google Gemini or other LLM endpoint
*   [ ] **TASK-MVP-INFRA-03:** Write/modify Bicep or Terraform templates for these resources.
*   [ ] **TASK-MVP-INFRA-04:** Parameterize templates for different environments (dev/test).
*   [ ] **TASK-MVP-INFRA-05:** Set up basic deployment pipeline (GitHub Actions / ADO / `azd`) for the API and infrastructure.

## `ISSUE-MVP-RETRIEVAL-01`: Implement Basic Knowledge Store & Retrieval

*   [ ] **TASK-MVP-RET-01:** Define `PersonaKnowledgeEntry` C# record (include persona ID, analysis result, relevant text/snippet, source identifier, timestamp, embeddings).
*   [ ] **TASK-MVP-RET-02:** Define `IPersonaKnowledgeRepository` interface (methods for Save, GetById, Query/Search).
*   [ ] **TASK-MVP-RET-03:** Choose storage mechanism (Azure Cosmos DB for NoSQL recommended).
*   [ ] **TASK-MVP-RET-04:** Implement `CosmosDbPersonaKnowledgeRepository` adapter in `Nucleus.Infrastructure`.
*   [ ] **TASK-MVP-RET-05:** Define `IEmbeddingGenerator` interface (using `Microsoft.Extensions.AI`).
*   [ ] **TASK-MVP-RET-06:** Implement adapter for chosen embedding model (e.g., `GoogleGeminiEmbeddingGenerator`).
*   [ ] **TASK-MVP-RET-07:** **(Defer Complex Retrieval)** Define `IRetrievalService` interface (simple initial version?).
*   [ ] **TASK-MVP-RET-08:** **(Defer Complex Retrieval)** Implement `BasicRetrievalService`.
*   [ ] **TASK-MVP-RET-09:** Integrate embedding generation and saving `PersonaKnowledgeEntry` into the `BootstrapperPersona`'s interaction flow (e.g., after getting a response, store query+response pair and embeddings via the repository).

---
