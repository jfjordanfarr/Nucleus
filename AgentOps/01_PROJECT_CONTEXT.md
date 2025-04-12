# Nucleus OmniRAG: Project Context (.NET / Azure - Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the .NET/Azure implementation using Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/00_PROJECT_MANDATE.md`) for motivation.

**Version:** 1.7
**Date:** 2025-04-09

## Vision & Goal

*   **Vision:** Build the Nucleus OmniRAG infrastructure for knowledge work enhanced by contextual AI, adaptable across domains via specialized "Personas" (EduFlow, Business Knowledge Assistant, etc.), seamlessly integrated into users' existing workflows via **platform bots/apps (Teams, Slack, Discord, Email)**. See `/docs/Requirements/00_PROJECT_MANDATE.md`.
*   **Initial Goal (Phase 1 MVP):** Implement a minimal viable product using the **.NET AI Chat App template** as a foundation. Focus on direct user interaction with the initial `BootstrapperPersona` via the template's Blazor WebAssembly chat interface and **ASP.NET Core WebAPI** backend. Integrate basic knowledge storage (Cosmos DB) and retrieval.

## Key Technologies

*   **Language:** C# (using .NET 8.0 - Requires SDK 8.x)
*   **Core Framework:** Nucleus OmniRAG (.NET Solution), **.NET AI Chat App Template** (Foundation for P1)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Blob Storage (Metadata or Separate Files)** - Stores `ArtifactMetadata` objects (Less critical for P1 Chat focus).
*   **Key Azure Services:** Cosmos DB, Blob Storage, **Azure AI Search**, **Azure OpenAI Service**, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider:** **Azure OpenAI Service** (Primary for Template, via `Microsoft.Extensions.AI`), Google Gemini AI (Secondary/Future).
*   **Platform Integration (Phase 2+):** Microsoft Bot Framework SDK / Graph API (Teams), Slack Bolt/API, Discord.NET/API, Email Processing (e.g., MailKit/MimeKit).
*   **MVP UI (Phase 1):** **Blazor WebAssembly** (from Chat Template)
*   **MVP API (Phase 1):** **ASP.NET Core WebAPI** (Replacing Template's Minimal API)
*   **Development:** Git, VS Code / Windsurf, .NET SDK 8.x, NuGet, **DotNet Aspire** (RC1+ for .NET 8), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** **`Microsoft.Extensions.AI`** (`IChatClient`, `IEmbeddingGenerator`), **Semantic Kernel** (Used by Chat Template).
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot (Phase 1 - Chat Template Focus)

*   **Interaction:** Users interact directly with the **Blazor WASM UI** provided by the .NET AI Chat App template.
*   **Frontend:** Blazor WebAssembly application.
*   **Backend Core:** .NET 8 / Aspire
    *   **API Endpoints (`Nucleus.Api` - adapted from Template):** **ASP.NET Core WebAPI** handling requests from the Blazor frontend. Orchestrates interaction with AI services and potentially the `BootstrapperPersona`.
    *   **AI Integration (Template):** Leverages Azure OpenAI for chat completion and Azure AI Search for RAG, orchestrated by Semantic Kernel.
    *   **Persona Implementations (`Nucleus.Personas.*`):** Initial `BootstrapperPersona` needs to be integrated into the template's flow (e.g., potentially as a Semantic Kernel plugin/function or invoked by the API).
    *   **Database (`Nucleus.Infrastructure`):** Adapters (`IPersonaKnowledgeRepository`) for Cosmos DB to store/retrieve persona-generated knowledge (if needed for the simple `BootstrapperPersona` in P1).
    *   *Note: Platform Adapters, complex Orchestration, Metadata Service, File Storage are deferred to Phase 2+.*
*   **Data Flow (Phase 1 - Chat):**
    1.  User types message in **Blazor WASM UI**.
    2.  UI sends request to **Backend API**.
    3.  API uses Semantic Kernel and configured AI services (Azure OpenAI, Azure AI Search) to process the request. This may involve:
        *   Retrieving relevant context (RAG) from Azure AI Search (or potentially our Cosmos DB via `IPersonaKnowledgeRepository` later).
        *   Invoking the `BootstrapperPersona` logic.
        *   Calling Azure OpenAI for chat completion.
    4.  API streams response back to the **Blazor WASM UI**.
*   **Deployment Model (P1):** Azure App Service / Azure Container Apps.
*   **Core Principles:** Leverage template structure, adapt template components, integrate `BootstrapperPersona` logic, defer complex Nucleus architecture.

## Non-Goals (Explicit)

*   Building a full-featured standalone chat application *from scratch* in Phase 1 (Leveraging template instead).
*   Implementing platform adapters (Email, Teams, etc.) in Phase 1.
*   Storing original user files in Phase 1.
*   Complex workflow orchestration in Phase 1.

## Key Links & References (Planned & Existing)

*   **Project Mandate:** `/docs/Requirements/00_PROJECT_MANDATE.md`
*   **Phase 1 Tasks:** `/docs/Planning/01_PHASE1_MVP_TASKS.md`
*   **.NET AI Chat App Template:** [Reference URL TBD]
*   **Architecture Docs:** `/docs/Architecture/` (Refer relevant sections, noting P1 scope)
*   **Core Abstractions:** `/src/Nucleus.Abstractions/`
*   **Core Models:** `/src/Nucleus.Core/Models/`
*   **Infrastructure:** `/src/Nucleus.Infrastructure/`
*   **Personas:** `/src/Nucleus.Personas/EduFlow/` (Bootstrapper logic initially)

## Current Project Structure Overview (Anticipated Initial from Template)

*Note: This structure is an *estimate* based on typical chat templates and will be confirmed/updated after installation. Aspire setup will integrate these.* 

```
NucleusOmniRAG.sln
├── AgentOps/
├── docs/
├── infra/ 
├── aspire/
│   ├── Nucleus.AppHost/
│   └── Nucleus.ServiceDefaults/
├── src/
│   ├── Nucleus.Console/         # Basic interactor
│   ├── Nucleus.Api/             # ASP.NET Core WebAPI Backend (Replacing Template's Minimal API)
│   ├── Nucleus.Abstractions/    # Core Interfaces (May be minimal initially)
│   ├── Nucleus.Core/            # Core Models (May be minimal initially)
│   ├── Nucleus.Infrastructure/  # Adapters (Cosmos DB, AI - partially from Template)
│   ├── Nucleus.Personas/        # Persona Base & Bootstrapper logic
│   │   └── Bootstrapper/      # Placeholder for Bootstrapper logic
│   └── # Other Nucleus projects (Processing, Orchestrations, Adapters) deferred
├── tests/ 
│   └── # Basic tests from template, Nucleus tests deferred
├── .gitignore
└── README.md
```

## Current Focus / Next Steps (High Level)

1.  **Set up .NET AI Chat App Template (`ISSUE-MVP-TEMPLATE-01`):** Install SDK/template, create initial project based on it.
2.  **Configure Aspire (`TASK-MVP-TMPL-02`):** Integrate the template projects (`WebApp`, `Api`) into the Aspire AppHost for local development with emulators/dependencies.
3.  **Adapt Template (`TASK-MVP-TMPL-03`+):** Configure template to use required Azure services (Cosmos DB for `IPersonaKnowledgeRepository` if needed, Azure AI Search, Azure OpenAI), update UI/API as necessary.
4.  **Integrate `BootstrapperPersona` (`ISSUE-MVP-PERSONA-01`):** Implement the basic Bootstrapper logic and connect it to the API/Semantic Kernel flow.
5.  **Implement Basic Knowledge Storage/Retrieval (`ISSUE-MVP-RETRIEVAL-01`):** If Bootstrapper needs persistent knowledge, implement `IPersonaKnowledgeRepository` using Cosmos DB and integrate retrieval.