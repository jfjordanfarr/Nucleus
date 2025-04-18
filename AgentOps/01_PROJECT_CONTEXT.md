# Nucleus: Project Context (.NET 9 / Aspire / Azure Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the Nucleus project using .NET 9, Aspire, and Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/Requirements/00_PROJECT_MANDATE.md`) for motivation. **The primary source for agent behavior and tool usage guidelines is `.windsurfrules` in the project root.**

**Version:** 3.0 # Reflects Aspire integration and updated architecture focus
**Date:** 2025-04-18

## Vision & Goal

*   **Vision:** Build the Nucleus infrastructure for knowledge work enhanced by contextual AI, adaptable across domains via specialized "Personas", seamlessly integrated into users' existing workflows via **platform bots/apps (Teams, Slack, Discord, Email)**. See `/docs/Requirements/00_PROJECT_MANDATE.md`.
*   **Initial Goal:** Implement the core backend services managed by **DotNet Aspire**, including the main API (`Nucleus.ApiService`), essential infrastructure (`Nucleus.Infrastructure`), and initial persona logic (`Nucleus.Personas`). Prioritize establishing robust backend functionality, AI integration (Gemini), and data persistence (Cosmos DB) before developing extensive user-facing interfaces or platform adapters.

## Key Technologies

*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** Nucleus (.NET Solution managed by DotNet Aspire)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Cosmos DB** - Stores `ArtifactMetadata` objects alongside knowledge entries (potentially separate container).
*   **Key Azure Services:** Cosmos DB, **Azure OpenAI Service / Google Gemini AI**, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider:** **Google Gemini AI** (Primary, integrated via `Mscc.GenerativeAI` v2.5.0+), Azure OpenAI Service (Secondary/Future).
*   **Platform Integration (Phase 2+):** Microsoft Bot Framework SDK / Graph API (Teams), Slack Bolt/API, Discord.NET/API, Email Processing (e.g., MailKit/MimeKit).
*   **Development:** Git, VS Code / Windsurf, .NET SDK 9.x, NuGet, **DotNet Aspire** (9.2+), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** `Mscc.GenerativeAI.IGenerativeAI` (for Gemini interaction), `Microsoft.Extensions.AI` (Potential future use).
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot

*   **Orchestration:** **DotNet Aspire (`Nucleus.AppHost`)** manages the development environment and service dependencies (API, potentially background workers, databases, etc.). `Nucleus.ServiceDefaults` provides common configurations.
*   **Primary Interaction Point:** **ASP.NET Core WebAPI (`Nucleus.ApiService`)** serves as the main entry point for requests (from future UIs, platform adapters, or testing tools).
*   **Core Logic:** Resides within `Nucleus.ApiService`, `Nucleus.Personas.*` (implementing specific domain logic), and `Nucleus.Processing.*` (for specialized tasks).
*   **Infrastructure (`Nucleus.Infrastructure`):** Contains adapters for external services, primarily **Azure Cosmos DB** (using `IPersonaKnowledgeRepository` for `PersonaKnowledgeEntry` and `IArtifactMetadataRepository` for `ArtifactMetadata`) and AI providers (`IGenerativeAI`).
*   **Ephemeral Processing Model:** Nucleus operates primarily on a session-scoped, ephemeral processing model. 
    *   Full generated content (e.g., synthesized Markdown) exists **only in memory** during request processing and is **discarded afterward**. 
    *   Nucleus **avoids persisting potentially sensitive or large generated artifacts** directly to its managed storage (like Cosmos DB or Blob Storage).
    *   Each request typically involves fetching fresh source artifacts (via future Adapters) and **reprocessing them on-demand**.
    *   **Cosmos DB stores only structured data:** `ArtifactMetadata` (pointers to original sources) and `PersonaKnowledgeEntry` (derived insights, summaries, vectors).
    *   This approach prioritizes **data privacy and freshness**, mitigating risks of storing sensitive user content and avoiding stale cached representations.
*   **Target Deployment:** **Azure Container Apps (ACA) as a 'Modular Monolith'**. Initially, key components (API, Personas, potentially background processing) are hosted within a single ACA instance for simplicity. Components are designed modularly to allow future separation and scaling if needed. Supporting Azure services include Cosmos DB and potentially Service Bus.

## Data Flow (Typical API Request)

1.  External client (e.g., test utility, future UI/Adapter) sends request to **`Nucleus.ApiService`**.
2.  API endpoint receives the request, authenticates/authorizes (future).
3.  Request handler coordinates necessary actions:
    *   Retrieves relevant `ArtifactMetadata` and/or `PersonaKnowledgeEntry` from Cosmos DB via repositories.
    *   Invokes appropriate `Persona` logic (e.g., `EduFlowPersona.AnalyzeDocumentAsync`).
    *   Persona interacts with AI Service (`IGenerativeAI`) for analysis, summarization, RAG, etc.
    *   (If ingestion) Stores new/updated `ArtifactMetadata` and `PersonaKnowledgeEntry` in Cosmos DB.
4.  API Service formats and returns the response to the client.

## Deployment Model (Target)

*   **Primary:** Single **Azure Container App (ACA)** instance hosting the `Nucleus.ApiService` and potentially related background processing logic (Modular Monolith).
*   **Supporting:** Azure Cosmos DB (NoSQL API), Azure Key Vault, Azure OpenAI / Google AI Services.
*   **Future Scaling:** Components within the monolith can be factored out into separate ACA instances or Azure Functions if required.
*   **Orchestration:** DotNet Aspire used for local development orchestration; deployment might leverage Bicep/Terraform and ACA features.

## Non-Goals (Explicit Initial Focus)

*   Building complex Web UIs (Blazor, etc.) initially.
*   Implementing production-ready platform adapters (Email, Teams, etc.) initially.
*   Implementing complex cross-persona orchestration workflows initially.

## Key Links & References (Planned & Existing)

*   **Agent Rules:** `.windsurfrules` (Project Root)
*   **Project Mandate:** `/docs/Requirements/00_PROJECT_MANDATE.md`
*   **Phase 1 Tasks:** `/docs/Planning/01_PHASE1_MVP_TASKS.md`
*   **Architecture Docs:** `/docs/Architecture/` (Refer relevant sections, noting P1 scope)
*   **Core Abstractions:** `/src/Nucleus.Abstractions/`
*   **Core Models:** `/src/Nucleus.Core/Models/`
*   **Infrastructure:** `/src/Nucleus.Infrastructure/`
*   **Personas:** `/src/Nucleus.Personas/`

## Current Project Structure Overview (Aspire-based)

```
Nucleus.sln
├── AgentOps/                  # Documentation for AI agent collaboration
├── Docs/                      # Project documentation (requirements, architecture, etc.)
├── {Individual C# Project Directories (i.e. AppHost, ServiceDefaults, ApiService, etc.)}
├── .gitignore
└── README.md
```

## Current High-Level Goals

*   Establish robust core service structure within the Aspire framework.
*   Implement reliable Cosmos DB persistence for `ArtifactMetadata` and `PersonaKnowledgeEntry`.
*   Integrate Google Gemini for core AI capabilities within Personas.
*   Develop initial Persona logic (e.g., Bootstrapper, potentially EduFlow basics).
*   Ensure comprehensive test coverage for core components.