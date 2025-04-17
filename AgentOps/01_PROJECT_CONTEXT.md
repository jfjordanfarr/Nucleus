# Nucleus OmniRAG: Project Context (.NET / Azure - Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the .NET/Azure implementation using Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/Requirements/00_PROJECT_MANDATE.md`) for motivation.

**Version:** 2.0 # Reflects successful base AI integration
**Date:** 2025-04-17

## Vision & Goal

*   **Vision:** Build the Nucleus OmniRAG infrastructure for knowledge work enhanced by contextual AI, adaptable across domains via specialized "Personas" (EduFlow, Business Knowledge Assistant, etc.), seamlessly integrated into users' existing workflows via **platform bots/apps (Teams, Slack, Discord, Email)**. See `/docs/Requirements/00_PROJECT_MANDATE.md`.
*   **Initial Goal (Phase 1 MVP):** Implement a minimal viable product focused on a **Console Application (`Nucleus.Console`)** interacting with a backend **ASP.NET Core WebAPI (`Nucleus.ApiService`)**. This prioritizes **accelerating the development iteration loop for backend logic, persona integration, and agentic workflows** before building user-facing UIs. Integrate the initial `BootstrapperPersona` and basic knowledge storage (Cosmos DB) accessible via the API.

## Key Technologies

*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** Nucleus OmniRAG (.NET Solution)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Cosmos DB** - Stores `ArtifactMetadata` objects alongside knowledge entries (potentially separate container).
*   **Key Azure Services:** Cosmos DB, **Azure OpenAI Service / Google Gemini AI**, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider:** **Google Gemini AI** (Primary, integrated via `Mscc.GenerativeAI` v2.5.0+), Azure OpenAI Service (Secondary/Future).
*   **Platform Integration (Phase 2+):** Microsoft Bot Framework SDK / Graph API (Teams), Slack Bolt/API, Discord.NET/API, Email Processing (e.g., MailKit/MimeKit).
*   **MVP Client (Phase 1):** **.NET Console Application (`Nucleus.Console`)** using `System.CommandLine`.
*   **MVP API (Phase 1):** **ASP.NET Core WebAPI (`Nucleus.ApiService`)**
*   **Development:** Git, VS Code / Windsurf, .NET SDK 9.x, NuGet, **DotNet Aspire** (9.2+), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** `Mscc.GenerativeAI.IGenerativeAI` (for Gemini interaction), `Microsoft.Extensions.AI` (Potential future use, especially for Azure OpenAI `IEmbeddingGenerator`).
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot (Phase 1 - Console App Focus)

*   **Interaction:** Users interact via commands in the **`Nucleus.Console` application**.
*   **Client:** .NET Console Application (`Nucleus.Console`).
*   **Backend Core:** .NET 9 / Aspire
    *   **API Endpoints (`Nucleus.ApiService`):** **ASP.NET Core WebAPI** handling requests from the `Nucleus.Console` client. Orchestrates interaction with AI services and Personas.
    *   **AI Integration:** Uses `Mscc.GenerativeAI.IGenerativeAI` injected into personas (e.g., `BootstrapperPersona`) for interaction with the configured provider (Google Gemini).
    *   **Persona Implementations (`Nucleus.Personas.*`):** Initial `BootstrapperPersona` invoked by the API.
    *   **Database (`Nucleus.Infrastructure`):** Adapters (`IPersonaKnowledgeRepository`) for Cosmos DB to store/retrieve persona-generated knowledge.
    *   *Note: Platform Adapters, complex Orchestration, Metadata Service, File Storage are deferred to Phase 2+.*
*   **Data Flow (Phase 1 - Console Interaction):**
    1.  User enters command (e.g., `nucleus query "<text>"`) in **`Nucleus.Console`**.
    2.  Console App parses command and sends request (e.g., HTTP POST) to **Backend API (`Nucleus.ApiService`)**.
    3.  API processes the request. This may involve:
        *   Retrieving relevant context from Cosmos DB via `IPersonaKnowledgeRepository`.
        *   Invoking the `BootstrapperPersona` logic (`HandleQueryAsync`).
        *   Calling the configured LLM (via `IGenerativeAI`.`GenerativeModel().StartChat().SendMessageAsync()`) for response generation.
    4.  API sends response back to the **`Nucleus.Console`**.
    5.  Console App formats and displays the response to the user.
*   **Deployment Model (P1):** Backend API (`Nucleus.ApiService`) hosted on Azure App Service / Azure Container Apps. Console App runs locally or can be distributed.
*   **Core Principles:** Build core backend logic, establish API<->Console interaction, integrate `BootstrapperPersona`, defer complex Nucleus architecture and platform UIs.

## Non-Goals (Explicit)

*   Building any Web UI (Blazor, etc.) in Phase 1.
*   Implementing platform adapters (Email, Teams, etc.) in Phase 1.
*   Storing original user files in Phase 1.
*   Complex workflow orchestration in Phase 1.

## Key Links & References (Planned & Existing)

*   **Project Mandate:** `/docs/Requirements/00_PROJECT_MANDATE.md`
*   **Phase 1 Tasks:** `/docs/Planning/01_PHASE1_MVP_TASKS.md`
*   **Architecture Docs:** `/docs/Architecture/` (Refer relevant sections, noting P1 scope)
*   **Core Abstractions:** `/src/Nucleus.Abstractions/`
*   **Core Models:** `/src/Nucleus.Core/Models/`
*   **Infrastructure:** `/src/Nucleus.Infrastructure/`
*   **Personas:** `/src/Nucleus.Personas/Bootstrapper/` (Bootstrapper logic initially)

## Current Project Structure Overview (Anticipated Initial from Template)

*Note: This structure reflects the current state based on the Console App MVP.* 

```
NucleusOmniRAG.sln
├── AgentOps/
├── docs/
├── aspire/
│   ├── Nucleus.AppHost/
│   └── Nucleus.ServiceDefaults/
├── src/
│   ├── Nucleus.Console/         # Basic interactor
│   ├── Nucleus.ApiService/      # ASP.NET Core WebAPI Backend
│   ├── Nucleus.Abstractions/    # Core Interfaces (May be minimal initially)
│   ├── Nucleus.Core/            # Core Models (May be minimal initially)
│   ├── Nucleus.Infrastructure/  # Adapters (Cosmos DB, AI - partially from Template)
│   ├── Nucleus.Personas/        # Persona Base & Bootstrapper logic
│   │   └── Bootstrapper/      # Placeholder for Bootstrapper logic
│   ├── Nucleus.Processing/      # Processing services (like Dataviz)
│   └── # Other Nucleus projects (Orchestrations, Adapters) deferred
├── tests/ 
│   └── Nucleus.Tests/         # Unit/Integration tests
├── .gitignore
└── README.md
```

## Current Focus / Next Steps (High Level)

1.  **Develop Console App Interface (`ISSUE-MVP-CONSOLE-01`):** Implement command structure (`System.CommandLine`), API client logic.
2.  **Develop Backend API (`ISSUE-MVP-API-01`):** Define and implement API endpoints required by the Console App (`/api/query`, `/api/ingest`, etc.).
3.  **Configure Aspire (`ISSUE-MVP-SETUP-01`):** Ensure AppHost correctly manages dependencies and configurations for `Nucleus.ApiService` and `Nucleus.Console` (including emulated Cosmos DB).
4.  **Integrate `BootstrapperPersona` (`ISSUE-MVP-PERSONA-01`):** Implement the basic Bootstrapper logic and connect it to the API flow.
5.  **Implement Basic Knowledge Storage/Retrieval (`ISSUE-MVP-RETRIEVAL-01`):** Implement `IPersonaKnowledgeRepository` using Cosmos DB and integrate saving/retrieval within the Persona/API flow.
6.  **Configure AI Provider (`ISSUE-MVP-SETUP-01`):** Set up chosen LLM provider (e.g., Gemini) via `Mscc.GenerativeAI` and manage secrets. **(DONE)**
7.  **Implement File Content Ingestion (`ISSUE-MVP-INGEST-01`):** Define API mechanism (e.g., modify `/api/query` or add `/api/ingest`) to accept file content for AI context.