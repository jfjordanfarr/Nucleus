---
title: AgentOps - Nucleus Project Context
description: High-level project context including vision, goals, tech stack, and links to detailed architecture.
version: 3.1 # Refactored based on DRY pass
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

This section provides a comprehensive listing of the files and directories within the Nucleus project, derived from the `tree_gitignore.py` script output. It serves as a persistent context for the AI, detailing the purpose of each significant file and project component.

*   **Root Directory:** `Nucleus/` (D:\Projects\Nucleus)

### Top-Level Configuration & Meta-Directories

*   `.devcontainer/`: Configuration for development containers.
    *   `devcontainer.json`: Defines the dev container environment settings.
*   `.github/`: GitHub-specific files, primarily CI/CD workflows.
    *   `workflows/`: Contains GitHub Actions workflow definitions (if any).
*   `.vs/`: Visual Studio specific files (typically gitignored, but listed for completeness if present in tree).
*   `.vscode/`: VS Code specific settings.
    *   `launch.json`: Debugging configurations for VS Code.
*   `_LocalData/`: Directory for storing local data, potentially large files (likely gitignored).
*   `AgentOps/`: Files related to AI agent operations, methodology, and context management.
    *   `Archive/`: Storage for historical agent conversations, plans, or analysis.
        *   `STORY_01_NavigatingEvolvingAILibraries.md`: Narrative log.
        *   `STORY_02_MCP_Server_Integration_Lessons.md`: Narrative log.
        *   `STORY_03_LinterIntegration.md`: Narrative log.
        *   `tmp_currentplan.md`: Temporary planning document.
        *   `tmp_DirectoryStructure.md`: Temporary directory structure notes.
    *   `Logs/`: Directory for agent-related logs.
    *   `Scripts/`: Utility scripts for agent operations.
        *   `codebase_to_markdown.py`: Script to generate markdown from codebase structure.
        *   `csharp_code_analyzer.csx`: C# script for code analysis tasks.
        *   `tree_gitignore.py`: Python script to display directory tree, respecting .gitignore.
    *   `00_START_HERE_METHODOLOGY.md`: Core document defining the AI-driven development methodology.
    *   `01_PROJECT_CONTEXT.md`: High-level overview and context of the Nucleus project.
    *   `02_CURRENT_SESSION_STATE.md`: Document tracking the state of the current development session.
    *   `03_PROJECT_PLAN_KANBAN.md`: Kanban-style project plan/backlog.
    *   `04_AGENT_TO_AGENT_CONVERSATION.md`: Log for meta-conversations about the agent's process.
    *   `README.md`: Readme specific to the AgentOps directory.
*   `Docs/`: Project documentation.
    *   `Architecture/`: Contains markdown files describing the system architecture.
        *   `ClientAdapters/`: Architecture for connecting Nucleus to different client platforms.
            *   `Console/`: Console adapter specifics.
                *   `ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md`: Interfaces for the Console adapter.
            *   `Teams/`: Microsoft Teams adapter specifics.
                *   `ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md`: Details on fetching attachments from Teams.
                *   `ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md`: Interfaces for the Teams adapter.
            *   `ARCHITECTURE_ADAPTER_INTERFACES.md`: Common interfaces for all client adapters.
            *   `ARCHITECTURE_ADAPTERS_CONSOLE.md`: Overview of the Console adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_DISCORD.md`: (Placeholder/Planned) Discord adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_EMAIL.md`: (Placeholder/Planned) Email adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_SLACK.md`: (Placeholder/Planned) Slack adapter architecture.
            *   `ARCHITECTURE_ADAPTERS_TEAMS.md`: Overview of the Teams adapter architecture.
        *   `Deployment/`: Architecture related to deployment strategies and hosting.
            *   `Hosting/`: Specific hosting environment details.
                *   `ARCHITECTURE_HOSTING_AZURE.md`: Azure hosting architecture.
                *   `ARCHITECTURE_HOSTING_CLOUDFLARE.md`: Cloudflare hosting architecture.
                *   `ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md`: Self-hosting architecture.
            *   `ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md`: Abstractions used in deployment.
            *   `ARCHITECTURE_DEPLOYMENT_CICD_OSS.md`: CI/CD strategy using open-source tools.
        *   `Personas/`: Architecture for different AI personas within Nucleus.
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
                *   `ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md`: Lifecycle of a user interaction.
                *   `ARCHITECTURE_ORCHESTRATION_ROUTING.md`: Request routing logic.
                *   `ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md`: How sessions are started.
            *   `ARCHITECTURE_PROCESSING_DATAVIZ.md`: Overview of the Dataviz architecture.
            *   `ARCHITECTURE_PROCESSING_INGESTION.md`: Overview of the Ingestion architecture.
            *   `ARCHITECTURE_PROCESSING_INTERFACES.md`: Common interfaces for processing components.
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
*   `Nucleus.AppHost/`: .NET Aspire application host project.
    *   `Properties/`: Project properties, including launch settings.
        *   `launchSettings.json`: Debug launch profiles for the AppHost.
    *   `appsettings.Development.json`: Development-specific configuration for AppHost.
    *   `appsettings.json`: Base configuration for AppHost.
    *   `Nucleus.AppHost.csproj`: C# project file for the Aspire AppHost.
    *   `Program.cs`: Main entry point for the Aspire AppHost, defines service orchestration.

### Source Code (`src/`)

*   `src/`: Root directory for all primary source code.
    *   `Abstractions/`: Projects defining interfaces, DTOs, and core models.
        *   `Nucleus.Abstractions/`: Main abstractions project.
            *   `Models/`: Data Transfer Objects (DTOs) and simple models.
                *   `Configuration/`: Configuration model classes.
                    *   `GoogleAiOptions.cs`: Configuration options for the Google AI (Gemini) client.
                *   `AdapterRequest.cs`: Represents an incoming request from a client adapter.
                *   `AdapterResponse.cs`: Represents a response sent back to a client adapter.
                *   `ArtifactMetadata.cs`: Metadata associated with an ingested artifact.
                *   `NucleusIngestionRequest.cs`: Represents a request to ingest data.
                *   `PlatformAttachmentReference.cs`: Platform-specific reference to an attachment.
                *   `PlatformType.cs`: Enum defining supported client platforms.
            *   `Orchestration/`: Interfaces related to request processing and orchestration.
                *   `InteractionContext.cs`: Holds context during interaction processing.
                *   `IOrchestrationService.cs`: Interface for the main orchestration service.
                *   `IPersonaManager.cs`: Interface for managing personas.
                *   `IPersonaResolver.cs`: Interface for resolving which persona to use.
                *   `NewSessionEvaluationResult.cs`: Result of evaluating if a new session is needed.
            *   `Repositories/`: Interfaces defining data access patterns.
                *   `IArtifactMetadataRepository.cs`: Repository for artifact metadata.
                *   `IPersonaKnowledgeRepository.cs`: Repository for persona-specific knowledge.
            *   `IArtifactProvider.cs`: Interface for providing access to artifact content.
            *   `IMessageQueuePublisher.cs`: Interface for publishing messages to a queue.
            *   `IPersona.cs`: Interface representing an AI persona.
            *   `IPlatformAttachmentFetcher.cs`: Interface for fetching attachments from specific platforms.
            *   `IPlatformNotifier.cs`: Interface for sending notifications back to platforms.
            *   `Nucleus.Abstractions.csproj`: C# project file for Abstractions.
    *   `Adapters/`: Projects implementing client adapters.
        *   `Nucleus.Adapters.Console/`: Command-Line Interface (CLI) adapter project.
            *   `Services/`: Services specific to the Console adapter.
                *   `ConsoleArtifactProvider.cs`: Implements `IArtifactProvider` for local file system access via console.
                *   `NucleusApiServiceAgent.cs`: Client agent to communicate with the backend Nucleus API.
            *   `appsettings.json`: Configuration for the Console adapter.
            *   `Nucleus.Console.csproj`: C# project file for the Console adapter.
            *   `Program.cs`: Main entry point for the Console application.
        *   `Nucleus.Adapters.Teams/`: Microsoft Teams adapter project.
            *   `GraphClientService.cs`: Service for interacting with Microsoft Graph API.
            *   `Nucleus.Adapters.Teams.csproj`: C# project file for the Teams adapter.
            *   `TeamsAdapterBot.cs`: Bot Framework `ActivityHandler` implementation for Teams.
            *   `TeamsAdapterConfiguration.cs`: Configuration settings specific to the Teams adapter.
            *   `TeamsGraphFileFetcher.cs`: Implements `IPlatformAttachmentFetcher` using MS Graph API.
            *   `TeamsNotifier.cs`: Implements `IPlatformNotifier` for sending messages to Teams.
    *   `DataAccess/`: (Currently Empty) Parent directory for data persistence logic projects.
    *   `Domain/`: Projects containing core business logic.
        *   `Nucleus.Domain.Processing/`: Central domain services project.
            *   `Infrastructure/`: Infrastructure-specific implementations used by the domain.
                *   `GoogleAiChatClientAdapter.cs`: Adapter for interacting with the Google AI Chat service (Mscc.GenerativeAI).
            *   `Resources/`: Embedded resource files.
                *   `Dataviz/`: Resources for the Dataviz HTML builder.
                    *   `dataviz_plotly_script.py`: Python script (likely for Plotly generation, used by builder).
                    *   `dataviz_script.js`: JavaScript for the dataviz HTML output.
                    *   `dataviz_styles.css`: CSS styles for the dataviz HTML output.
                    *   `dataviz_template.html`: HTML template file used by `DatavizHtmlBuilder`.
                    *   `dataviz_worker.js`: Web worker script for dataviz processing.
            *   `DefaultPersonaResolver.cs`: Default implementation of `IPersonaResolver`.
            *   `Nucleus.Processing.csproj`: C# project file for Domain Processing.
            *   `OrchestrationService.cs`: Implements `IOrchestrationService`, coordinating request processing.
            *   `ServiceCollectionExtensions.cs`: Extension methods for dependency injection registration.
    *   `Features/`: (Currently Empty) Parent directory for potential feature-specific modules.
    *   `Personas/`: Projects defining specific AI persona implementations.
        *   `Nucleus.Personas.Core/`: Core project for basic persona implementations.
            *   `BootstrapperPersona.cs`: Implementation of the `IPersona` interface for the Bootstrapper persona.
            *   `EmptyAnalysisData.cs`: Placeholder/empty data structure potentially used by personas.
            *   `Nucleus.Personas.Core.csproj`: C# project file for Core Personas.
    *   `Services/`: Projects containing backend services, including the main API.
        *   `Nucleus.ServiceDefaults/`: Shared service defaults project (common configuration, health checks, etc., often used with Aspire).
            *   `Extensions.cs`: Extension methods for configuring service defaults.
            *   `Nucleus.ServiceDefaults.csproj`: C# project file for Service Defaults.
        *   `Nucleus.Services.Api/`: The main backend API service project.
            *   `Configuration/`: Configuration-related classes.
            *   `Controllers/`: API controllers handling incoming HTTP requests.
                *   `InteractionController.cs`: Controller handling core user interactions.
            *   `Diagnostics/`: Services related to diagnostics and health checks.
                *   `ServiceBusSmokeTestService.cs`: Background service for testing Service Bus connectivity.
            *   `Infrastructure/`: Infrastructure-related components like messaging.
                *   `Messaging/`: Implementations for message queue interactions.
                    *   `AzureServiceBusPublisher.cs`: Implements `IMessageQueuePublisher` using Azure Service Bus.
                    *   `NullMessageQueuePublisher.cs`: No-op implementation of `IMessageQueuePublisher`.
                    *   `ServiceBusQueueConsumerService.cs`: Background service for consuming messages from Azure Service Bus.
                *   `NullArtifactProvider.cs`: No-op implementation of `IArtifactProvider` for API service context.
            *   `Properties/`: Project properties.
                *   `launchSettings.json`: Debug launch profiles for the API service.
            *   `AdapterWithErrorHandler.cs`: Bot Framework adapter wrapper with error handling.
            *   `appsettings.Development.json`: Development-specific configuration for the API service.
            *   `appsettings.json`: Base configuration for the API service.
            *   `Nucleus.ApiService.csproj`: C# project file for the API Service.
            *   `Program.cs`: Main entry point for the API service, configures ASP.NET Core host.

### Testing (`tests/`)

*   `tests/`: Root directory for all test projects.
    *   `Adapters/`: Test projects for client adapters.
        *   `Nucleus.Adapters.Console.Tests/`: Test project for the Console adapter.
            *   `TestData/`: Contains data files used in tests.
                *   `test_context.txt`: Sample context file for testing.
            *   `test_ingest_agent_api.ps1`: PowerShell script likely used for integration/e2e testing of ingestion via console.
            *   `test_query_agent_api.ps1`: PowerShell script likely used for integration/e2e testing of queries via console.

### Root Files

*   `.editorconfig`: Defines coding styles enforced by editors.
*   `.gitattributes`: Defines attributes for paths in Git.
*   `.gitignore`: Specifies intentionally untracked files that Git should ignore.
*   `.windsurfrules`: **This file.** Defines rules and context for the AI development agent (Cascade).
*   `LICENSE.txt`: Project license file.
*   `Nucleus.sln`: Visual Studio Solution file, organizes the projects.
*   `README.md`: Main project README file.

---