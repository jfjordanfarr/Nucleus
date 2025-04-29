---
title: AgentOps - Nucleus Project Context
description: High-level project context including vision, goals, tech stack, and links to detailed architecture.
version: 3.1 
date: 2025-04-18
---

# Nucleus: Project Context (.NET 9 / Aspire / Azure Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the Nucleus project using .NET 9, Aspire, and Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/Requirements/00_PROJECT_MANDATE.md`) for motivation. **The primary source for agent behavior and tool usage guidelines is `.windsurfrules` in the project root.**

## Vision & Goal

Build the Nucleus infrastructure for knowledge work enhanced by contextual AI Personas, integrated into users' existing workflows. See the [Project Mandate](../../Docs/Requirements/00_PROJECT_MANDATE.md) and the root [README.md](../../README.md) for the full vision. The initial goal focuses on the core backend API and services using .NET Aspire; see [Phase 1 Requirements](../../Docs/Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md).

## Key Technologies

*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** .NET Aspire (Managing the Nucleus Solution)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Cosmos DB** - Stores `ArtifactMetadata` objects alongside knowledge entries (potentially separate container).
*   **Key Azure Services:** Cosmos DB, **Azure OpenAI Service / Google Gemini AI**, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider:** Google Gemini AI (Primary, integrated via `Mscc.GenerativeAI`). See [AI Integration Architecture](../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md) for details. Azure OpenAI Service (Secondary/Future).
*   **Platform Integration (Phase 2+):** Microsoft Bot Framework SDK / Graph API (Teams), Slack Bolt/API, Discord.NET/API, Email Processing (e.g., MailKit/MimeKit).
*   **Development:** Git, VS Code / Windsurf, .NET SDK 9.x, NuGet, **DotNet Aspire** (9.2+), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** `Mscc.GenerativeAI.IGenerativeAI` (for Gemini interaction), `Microsoft.Extensions.AI` (Potential future use).
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot

The system uses .NET Aspire for orchestration and includes core components like `Nucleus.ApiService`, `Nucleus.Personas.*`, `Nucleus.Processing.*`, and `Nucleus.Infrastructure`. See the [System Architecture Overview](../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md) for component diagrams and relationships.

*   **Ephemeral Processing Model:** Nucleus processes data transiently within a session scope, avoiding persistent storage of intermediate or generated content. See the [Ephemeral Processing principle in .windsurfrules](../.windsurfrules) for details.
*   **Target Deployment:** Initial target is **Azure Container Apps (ACA) as a 'Modular Monolith'**. See the [Deployment Architecture Overview](../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md) for details and alternatives.

## Data Flow (Typical API Request)

The typical data flow involves API requests triggering persona logic, interaction with AI services, and retrieval/storage of metadata in Cosmos DB. See the [Processing Architecture](../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md) for a detailed flow diagram.

## Deployment Model (Target)

The primary target deployment uses Azure Container Apps and supporting Azure services. See the [Deployment Architecture Overview](../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md) for details.

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

See the [System Architecture Overview](../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md#5-high-level-codebase-structure-conceptual) for the conceptual codebase structure.


## - Project Structure & File Census

This section provides a comprehensive listing of the files and directories within the Nucleus project, derived from the `tree_gitignore.py` script output as of **2025-04-27**. It serves as a persistent context for the AI, detailing the purpose of each significant file and project component based on the refactored structure.

*   **Root Directory:** `Nucleus/` (D:\Projects\Nucleus)

### Top-Level Configuration & Meta-Directories

*   `.devcontainer/`: Configuration for development containers.
    *   `devcontainer.json`: Defines the dev container environment settings.
*   `.github/`: GitHub-specific files, primarily CI/CD workflows.
    *   `workflows/`: Contains GitHub Actions workflow definitions (if any).
*   `.vs/`: Visual Studio specific files (typically gitignored).
*   `.vscode/`: VS Code specific settings.
    *   `launch.json`: Debugging configurations for VS Code.
*   `_LocalData/`: Directory for storing local data, potentially large files (likely gitignored).
*   `AgentOps/`: Files related to AI agent operations, methodology, and context management.
    *   `Archive/`:
        *   `STORY_*.md`: Narrative logs of previous sessions.
    *   `Scripts/`:
        *   `codebase_to_markdown.py`
        *   `csharp_code_analyzer.csx`
        *   `tree_gitignore.py`: Script to display directory tree, respecting .gitignore.
    *   `00_START_HERE_METHODOLOGY.md`: Core methodology document.
    *   `01_PROJECT_CONTEXT.md`: **This file.**
    *   `02_CURRENT_SESSION_STATE.md`: Current session state tracking.
    *   `03_AGENT_TO_AGENT_CONVERSATION.md`: Agent meta-conversation log.
    *   `CodebaseDump.md`: Raw dump or snapshot of the codebase structure/content.
    *   `README.md`: AgentOps directory readme.
    *   `tmp_currentplan.md`: Temporary plan document for the current agent session.
*   `Docs/`: Project documentation.
    *   `Architecture/`: Contains markdown files describing the system architecture.
        *   `Api/`: Architecture related to the central API service.
            *   `ARCHITECTURE_API_CLIENT_INTERACTION.md`: Defines interaction patterns (DTOs, synchronous/asynchronous handling) between Client Adapters and the Nucleus API Service.
            *   `ARCHITECTURE_API_INGESTION.md`: Defines the API contract for data ingestion (path-based, potentially others).
        *   `ClientAdapters/`: Architecture for connecting Nucleus to different client platforms.
            *   `Console/`: Console adapter specifics.
                *   `Archive/`: Archived documents related to the Console adapter.
                    *   `ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md`: Archived interfaces specific to the older Console adapter design.
            *   `Teams/`: Microsoft Teams adapter specifics.
                *   `ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md`: Details on *how* the adapter used to fetch attachments (Now references API for this task).
                *   `ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md`: Interfaces for the Teams adapter (**Partially Deprecated:** Focus shifts to API DTOs, but Bot Framework interfaces remain relevant).
            *   `ARCHITECTURE_ADAPTER_INTERFACES.md`: Common interfaces for client adapters (**Partially Deprecated:** Focus shifts to API contracts, but platform-specific interfaces like `IPlatformNotifier` might remain).
            *   `ARCHITECTURE_ADAPTERS_CONSOLE.md`: Overview of the Console adapter architecture (Updated for API-First).
            *   `ARCHITECTURE_ADAPTERS_DISCORD.md`: Placeholder for Discord adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_EMAIL.md`: Placeholder for Email adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_SLACK.md`: Placeholder for Slack adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_TEAMS.md`: Overview of the Teams adapter architecture (Needs review for API-First alignment).
        *   `Deployment/`: Architecture related to deployment strategies and hosting.
            *   `Hosting/`: Specific hosting environment details.
                *   `ARCHITECTURE_HOSTING_AZURE.md`: Azure hosting architecture.
                *   `ARCHITECTURE_HOSTING_CLOUDFLARE.md`: Cloudflare hosting architecture.
                *   `ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md`: Self-hosting architecture.
            *   `ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md`: Abstractions used in deployment.
            *   `ARCHITECTURE_DEPLOYMENT_CICD_OSS.md`: CI/CD strategy using open-source tools.
        *   `Personas/`: Architecture for different AI persona implementations.
            *   `Bootstrapper/`: (Empty directory, potentially for Bootstrapper persona specifics).
            *   `Educator/`: Architecture for the Educator persona.
                *   `Pedagogical_And_Tautological_Trees_Of_Knowledge/`: Knowledge representation for Educator.
                    *   `Age*.md`: (Condensed) Age-specific knowledge tree files (Age05-Age18).
                *   `ARCHITECTURE_EDUCATOR_KNOWLEDGE_TREES.md`: Overview of the Educator's knowledge structure.
                *   `ARCHITECTURE_EDUCATOR_REFERENCE.md`: Reference materials for the Educator persona.
                *   `NumeracyAndTimelinesWebappConcept.md`: Concept document for a related web application.
            *   `Professional/`: Architecture for professional/workplace personas.
                *   `ARCHITECTURE_AZURE_DOTNET_HELPDESK.md`: Specific architecture for an Azure/.NET helpdesk persona.
            *   `ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md`: Overview of the Bootstrapper persona.
            *   `ARCHITECTURE_PERSONAS_CONFIGURATION.md`: Defines how Personas are configured.
            *   `ARCHITECTURE_PERSONAS_EDUCATOR.md`: Overview of the Educator persona.
            *   `ARCHITECTURE_PERSONAS_PROFESSIONAL.md`: Overview of professional personas.
        *   `Processing/`: Architecture for core data processing components.
            *   `Dataviz/`: Data visualization architecture.
                *   `Examples/`: Example outputs of the Dataviz component.
                    *   `dataviz.html`: A sample dataviz HTML file.
                    *   `EXAMPLE_OUTPUT_nucleus_dataviz_*.html`: Specific generated examples.
                *   `ARCHITECTURE_DATAVIZ_TEMPLATE.md`: Architecture of the Dataviz HTML templating.
            *   `Ingestion/`: Architecture for data ingestion pipelines.
                *   `ARCHITECTURE_INGESTION_FILECOLLECTIONS.md`: Handling collections of files.
                *   `ARCHITECTURE_INGESTION_MULTIMEDIA.md`: Handling multimedia files.
                *   `ARCHITECTURE_INGESTION_PDF.md`: Handling PDF files.
                *   `ARCHITECTURE_INGESTION_PLAINTEXT.md`: Handling plain text files.
            *   `Orchestration/`: Architecture for request handling and workflow orchestration.
                *   `ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md`: Lifecycle of a user interaction (Updated for API-First & Hybrid Execution).
                *   `ARCHITECTURE_ORCHESTRATION_ROUTING.md`: Request routing logic (Updated for API Activation/Routing).
                *   `ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md`: How sessions are started (Updated for API-First).
            *   `ARCHITECTURE_PROCESSING_DATAVIZ.md`: Overview of the Dataviz architecture (Updated for API-First).
            *   `ARCHITECTURE_PROCESSING_INGESTION.md`: Overview of the Ingestion architecture.
            *   `ARCHITECTURE_PROCESSING_INTERFACES.md`: Common interfaces for processing components (Internal to `ApiService`).
            *   `ARCHITECTURE_PROCESSING_ORCHESTRATION.md`: Overview of the Orchestration architecture.
        *   `Storage/`: (Empty directory, planned for storage architecture documents).
        *   `00_ARCHITECTURE_OVERVIEW.md`: Top-level entry point for architecture documents.
        *   `01_ARCHITECTURE_PROCESSING.md`: High-level overview of processing.
        *   `02_ARCHITECTURE_PERSONAS.md`: High-level overview of personas.
        *   `03_ARCHITECTURE_STORAGE.md`: High-level overview of storage.
        *   `04_ARCHITECTURE_DATABASE.md`: High-level overview of the database schema/choice.
        *   `05_ARCHITECTURE_CLIENTS.md`: High-level overview of client adapters.
        *   `06_ARCHITECTURE_SECURITY.md`: Overview of security considerations.
        *   `07_ARCHITECTURE_DEPLOYMENT.md`: High-level overview of deployment.
        *   `08_ARCHITECTURE_AI_INTEGRATION.md`: Overview of AI integration points.
        *   `09_ARCHITECTURE_TESTING.md`: Overview of testing strategy and methodologies (Updated for API-First integration testing).
        *   `10_ARCHITECTURE_API.md`: Top-level overview of the API-First architecture.
        *   `11_ARCHITECTURE_NAMESPACES_FOLDERS.md`: Overview of namespaces and folder structure (Needs update).
    *   `HelpfulMarkdownFiles/`: Collection of useful reference documents and reports.
        *   `Library-References/`: Documentation links/notes for key libraries.
            *   `AzureAI.md`: Azure AI services reference.
            *   `AzureCosmosDBDocumentation.md`: Cosmos DB reference.
            *   `DotnetAspire.md`: .NET Aspire reference.
            *   `MicrosoftExtensionsAI.md`: Microsoft AI extensions reference.
        *   `Nucleus Teams Adapter Report.md`: Report related to the Teams adapter.
        *   `Secure-Bot-Framework-Azure-Deployment.md`: Guide for secure Bot Framework deployment.
        *   `Slack-Email-Discord-Adapter-Report.md`: Report on other potential adapters.
        *   `Windsurf Dev Container Integration Feasibility_.md`: Feasibility study notes.
    *   `Planning/`: Project planning documents.
        *   `00_ROADMAP.md`: High-level project roadmap.
        *   `01_PHASE1_MVP_TASKS.md`: Tasks for Phase 1.
        *   `02_PHASE2_MULTI_PLATFORM_TASKS.md`: Tasks for Phase 2.
        *   `03_PHASE3_ENHANCEMENTS_TASKS.md`: Tasks for Phase 3.
        *   `04_PHASE4_MATURITY_TASKS.md`: Tasks for Phase 4.
    *   `Requirements/`: Project requirements documents.
        *   `00_PROJECT_MANDATE.md`: The core project goals and mandate.
        *   `01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md`: Requirements for Phase 1.
        *   `02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`: Requirements for Phase 2.
        *   `03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md`: Requirements for Phase 3.
        *   `04_REQUIREMENTS_PHASE4_MATURITY.md`: Requirements for Phase 4.

### Aspire Layer (`Aspire/`)

*   `Aspire/`: Contains projects specific to .NET Aspire orchestration and configuration.
    *   `Nucleus.AppHost/`: .NET Aspire application host project.
        *   `Properties/launchSettings.json`: Debug launch profiles.
        *   `appsettings.*.json`: Configuration files.
        *   `Nucleus.AppHost.csproj`: C# project file.
        *   `Program.cs`: Main entry point, defines service orchestration.
    *   `Nucleus.ServiceDefaults/`: Shared service defaults project.
        *   `Extensions.cs`: Extension methods for configuring service defaults.
        *   `Nucleus.ServiceDefaults.csproj`: C# project file.

### Source Code (`src/`)

*   `src/` (Primary Source Code)
    *   `Nucleus.Abstractions/` (Core Interfaces, DTOs, Base Types)
        *   `Models/`
            *   `Configuration/`
                *   `GoogleAiOptions.cs`
                *   `PersonaConfiguration.cs`
            *   `AdapterRequest.cs`
            *   `AdapterResponse.cs`
            *   `ArtifactContent.cs`
            *   `ArtifactMetadata.cs`
            *   `ArtifactReference.cs`
            *   `NucleusIngestionRequest.cs`
            *   `PlatformAttachmentReference.cs`
            *   `PlatformType.cs`
        *   `Orchestration/`
            *   `IActivationChecker.cs`
            *   `IBackgroundTaskQueue.cs`
            *   `InteractionContext.cs`
            *   `IOrchestrationService.cs`
            *   `IPersonaManager.cs`
            *   `IPersonaResolver.cs`
            *   `NewSessionEvaluationResult.cs`
            *   `SalienceCheckResult.cs`
        *   `Repositories/`
            *   `IArtifactMetadataRepository.cs`
            *   `IPersonaKnowledgeRepository.cs`
        *   `IArtifactProvider.cs`
        *   `IMessageQueuePublisher.cs`
        *   `IPersona.cs`
        *   `IPlatformAttachmentFetcher.cs`
        *   `IPlatformNotifier.cs`
        *   `Nucleus.Abstractions.csproj`
    *   `Nucleus.Application/` (Placeholder)
    *   `Nucleus.Domain/` (Core Business Logic, Entities, Domain Services)
        *   `Nucleus.Domain.Processing/` (Central Domain Services)
            *   `Infrastructure/` (Empty)
            *   `Resources/Dataviz/` (HTML Dataviz Assets)
                *   `dataviz_plotly_script.py`
                *   `dataviz_script.js`
                *   `dataviz_styles.css`
                *   `dataviz_template.html`
                *   `dataviz_worker.js`
            *   `Services/`
                *   `DatavizHtmlBuilder.cs`
            *   `ActivationChecker.cs`
            *   `DefaultPersonaManager.cs`
            *   `DefaultPersonaResolver.cs`
            *   `InMemoryBackgroundTaskQueue.cs`
            *   `Nucleus.Domain.Processing.csproj`
            *   `OrchestrationService.cs`
            *   `QueuedInteractionProcessorService.cs`
            *   `ServiceCollectionExtensions.cs`
        *   `Personas/`
            *   `Nucleus.Personas.Core/` (Core Persona Implementations)
                *   `BootstrapperPersona.cs`
                *   `EmptyAnalysisData.cs`
                *   `Nucleus.Domain.Personas.Core.csproj`
    *   `Nucleus.Infrastructure/` (External Concerns: Data, Adapters, etc.)
        *   `Adapters/`
            *   `Nucleus.Adapters.Console/` (CLI Adapter)
                *   `_LocalData/`
                *   `Services/`
                    *   `ConsoleArtifactProvider.cs`
                    *   `NucleusApiServiceAgent.cs`
                *   `appsettings.json`
                *   `Nucleus.Infrastructure.Adapters.Console.csproj`
                *   `Program.cs`
            *   `Nucleus.Adapters.Teams/` (Microsoft Teams Adapter)
                *   `GraphClientService.cs`
                *   `Nucleus.Infrastructure.Adapters.Teams.csproj`
                *   `TeamsAdapterBot.cs`
                *   `TeamsAdapterConfiguration.cs`
                *   `TeamsNotifier.cs`
        *   `Data/`
            *   `Nucleus.Infrastructure.Persistence/` (Persistence Implementations)
                *   `ArtifactProviders/` (Empty)
                *   `Repositories/`
                    *   `CosmosDbArtifactMetadataRepository.cs`
                    *   `InMemoryArtifactMetadataRepository.cs`
                *   `Nucleus.Infrastructure.Data.Persistence.csproj`
    *   `Nucleus.Services/` (Hosting Layer: APIs, Workers)
        *   `Nucleus.Services.Api/` (Main Backend API Service)
            *   `Configuration/`
                *   `GeminiOptions.cs`
            *   `Controllers/`
                *   `InteractionController.cs`
            *   `Diagnostics/`
                *   `ServiceBusSmokeTestService.cs`
            *   `Infrastructure/` (API-Specific Infrastructure)
                *   `Artifacts/`
                    *   `LocalFileArtifactProvider.cs`
                *   `Messaging/`
                    *   `AzureServiceBusPublisher.cs`
                *   `NullArtifactProvider.cs`
            *   `Properties/`
                *   `launchSettings.json`
            *   `AdapterWithErrorHandler.cs`
            *   `appsettings.Development.json`
            *   `appsettings.json`
            *   `Nucleus.Services.Api.csproj`
            *   `Program.cs`
            *   `WebApplicationBuilderExtensions.cs`

### Testing (`tests/`)

*   `tests/`: Root directory for all test projects.
    *   `Integration/`: Integration test projects.
        *   `Nucleus.Services.Api.IntegrationTests/`: Integration tests for the `Nucleus.Services.Api`.
            *   `Infrastructure/`: Test-specific infrastructure mocks/stubs.
                *   `NullArtifactMetadataRepository.cs`: A test double repository that does nothing, used to isolate tests from real persistence.
            *   `TestData/`: Sample data for tests.
                *   `test_artifact.txt`: Sample artifact file for testing
            *   `TestResults/`: (Typically gitignored) Output from test runs.
            *   `ApiIntegrationTests.cs`: Tests using `HttpClient` against the API via `WebApplicationFactory`.
            *   `Nucleus.Services.Api.IntegrationTests.csproj`: C# project file.
            *   `test_ingest_agent_api.ps1`: Script for testing ingestion (Potentially obsolete).
            *   `test_query_agent_api.ps1`: Script for testing query endpoint.

### Root Files

*   `.editorconfig`: Coding style definitions.
*   `.gitattributes`: Git path attributes.
*   `.gitignore`: Specifies files ignored by Git.
*   `.windsurfrules`: AI agent configuration and rules.
*   `LICENSE.txt`: Project license.
*   `Nucleus.sln`: Visual Studio Solution file.
*   `README.md`: Main project README.

---