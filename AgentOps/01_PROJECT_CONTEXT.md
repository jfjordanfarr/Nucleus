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
    *   `README.md`: AgentOps directory readme.
*   `Docs/`: Project documentation.
    *   `Architecture/`: Contains markdown files describing the system architecture.
        *   `Api/`: Architecture related to the central API service.
            *   `ARCHITECTURE_API_CLIENT_INTERACTION.md`: Defines API-First interaction patterns (DTOs, sync/async handling via job IDs) for Client Adapters communicating with the Nucleus API Service.
            *   `ARCHITECTURE_API_INGESTION.md`: Defines the API contract for data ingestion (path-based, potentially others).
        *   `ClientAdapters/`: Architecture for connecting Nucleus to different client platforms.
            *   `Console/`: Console adapter specifics.
                *   `ARCHITECTURE_ADAPTERS_CONSOLE.md`: Overview of the Console adapter architecture (Updated for API-First).
            *   `Teams/`: Microsoft Teams adapter specifics.
                *   `ARCHITECTURE_ADAPTERS_TEAMS.md`: Overview of the Teams adapter architecture (Needs review for API-First alignment).
        *   `Deployment/`: Architecture related to deployment strategies and hosting.
            *   `Hosting/`: Specific hosting environment details.
                *   `ARCHITECTURE_HOSTING_AZURE.md`: Azure hosting architecture.
            *   `ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md`: Abstractions used in deployment.
            *   `ARCHITECTURE_DEPLOYMENT_CICD_OSS.md`: CI/CD strategy using open-source tools.
        *   `Namespaces/`: Documentation defining the purpose of key C# namespaces.
            *   `NAMESPACE_ABSTRACTIONS.md`: Documents `Nucleus.Abstractions`.
            *   `NAMESPACE_ADAPTERS_CONSOLE.md`: Documents `Nucleus.Infrastructure.Adapters.Console`.
            *   `NAMESPACE_ADAPTERS_TEAMS.md`: Documents `Nucleus.Infrastructure.Adapters.Teams`.
            *   `NAMESPACE_API_INTEGRATION_TESTS.md`: Documents `Nucleus.Services.Api.IntegrationTests`.
            *   `NAMESPACE_APP_HOST.md`: Documents `Nucleus.AppHost`.
            *   `NAMESPACE_DOMAIN_PROCESSING.md`: Documents `Nucleus.Domain.Processing`.
            *   `NAMESPACE_INFRASTRUCTURE_PERSISTENCE.md`: Documents `Nucleus.Infrastructure.Data.Persistence`.
            *   `NAMESPACE_PERSONAS_CORE.md`: Documents `Nucleus.Domain.Personas.Core`.
            *   `NAMESPACE_SERVICE_DEFAULTS.md`: Documents `Nucleus.ServiceDefaults`.
            *   `NAMESPACE_SERVICES_API.md`: Documents `Nucleus.Services.Api`.
        *   `Personas/`: Architecture for different AI persona implementations.
            *   `ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md`: Overview of the Bootstrapper persona concept.
            *   `ARCHITECTURE_PERSONAS_CONFIGURATION.md`: Overview of persona configuration architecture.
            *   `ARCHITECTURE_PERSONAS_EDUCATOR.md`: Overview of the Educator persona concept.
            *   `ARCHITECTURE_PERSONAS_PROFESSIONAL.md`: Overview of the Professional persona concept.
            *   `Bootstrapper/`: (Empty directory, potentially for Bootstrapper persona specifics).
            *   `Educator/`: Architecture for the Educator persona.
                *   `ARCHITECTURE_EDUCATOR_KNOWLEDGE_TREES.md`: Overview of the Educator's knowledge structure.
                *   `ARCHITECTURE_EDUCATOR_REFERENCE.md`: Reference materials for the Educator persona.
                *   `NumeracyAndTimelinesWebappConcept.md`: Concept document for a related web application.
                *   `Pedagogical_And_Tautological_Trees_Of_Knowledge/`: Detailed knowledge representation files.
                    *   `Age*.md`: Age-specific knowledge tree markdown files.
            *   `Professional/`: Architecture for professional/workplace personas.
                *   `ARCHITECTURE_AZURE_DOTNET_HELPDESK.md`: Specific architecture for an Azure/.NET helpdesk persona.
        *   `Processing/`: Architecture for core data processing components.
            *   `ARCHITECTURE_PROCESSING_DATAVIZ.md`: Overview of data visualization architecture.
            *   `ARCHITECTURE_PROCESSING_INGESTION.md`: Overview of data ingestion architecture.
            *   `ARCHITECTURE_PROCESSING_INTERFACES.md`: Definition of core processing interfaces.
            *   `ARCHITECTURE_PROCESSING_ORCHESTRATION.md`: Overview of orchestration architecture.
            *   `Dataviz/`: Data visualization architecture.
                *   `ARCHITECTURE_DATAVIZ_TEMPLATE.md`: Architecture of the Dataviz HTML templating.
                *   `Examples/`: Example outputs of the Dataviz component.
                    *   `dataviz.html`: A sample dataviz HTML file.
                    *   `EXAMPLE_OUTPUT*.html`: Specific generated examples.
            *   `Ingestion/`: Architecture for data ingestion pipelines.
                *   `ARCHITECTURE_INGESTION_FILECOLLECTIONS.md`: Handling collections of files.
                *   `ARCHITECTURE_INGESTION_MULTIMEDIA.md`: Handling multimedia files.
                *   `ARCHITECTURE_INGESTION_PDF.md`: Handling PDF files.
                *   `ARCHITECTURE_INGESTION_PLAINTEXT.md`: Handling plain text files.
            *   `Orchestration/`: Architecture for request handling and workflow orchestration.
                *   `ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md`: Lifecycle of a user interaction (Updated for API-First & Hybrid Execution).
                *   `ARCHITECTURE_ORCHESTRATION_ROUTING.md`: Request routing logic (Updated for API Activation/Routing).
                *   `ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md`: How sessions are started (Updated for API-First).
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
            *   `Mscc.GenerativeAI.Micrsosoft-Reference.md`: Reference notes for the Mscc.GenerativeAI library.
        *   `NET Aspire Testing Landscape_.md`: Notes on testing approaches with .NET Aspire.
        *   `Nucleus Teams Adapter Report.md`: Report related to the Teams adapter.
        *   `Secure-Bot-Framework-Azure-Deployment.md`: Guide for secure Bot Framework deployment.
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
                *   `AgenticStrategyParametersBase.cs`: Base class for parameters passed to agentic strategy handlers.
                *   `GeminiOptions.cs`: Configuration class holding settings for interacting with the Gemini AI provider, primarily the API Key. Maps to the `Gemini` configuration section.
                *   `IPersonaConfigurationProvider.cs`: Interface for providers that load persona configurations.
                *   `PersonaConfiguration.cs`: Represents the loaded configuration for a specific persona, including strategy, LLM settings, etc.
            *   `AdapterRequest.cs`: Represents the primary DTO for incoming interaction requests from client adapters to the Nucleus API.
            *   `AdapterResponse.cs`: Represents the response DTO sent from the Nucleus API back to client adapters.
            *   `ArtifactContent.cs`: Represents the retrieved, disposable content of an artifact (e.g., file stream) obtained via an `IArtifactProvider`.
            *   `ArtifactMetadata.cs`: Represents the core, persisted metadata for an artifact (e.g., source info, filename, size, timestamps) stored in Cosmos DB.
            *   `ArtifactReference.cs`: Represents a reference passed from an adapter to the API, pointing to an artifact in its source system, allowing retrieval via `IArtifactProvider`.
            *   `NucleusIngestionRequest.cs`: Primary DTO for initiating interaction processing via the API, also used for async queue messages. Combines context, query, and artifact references.
            *   `PlatformAttachmentReference.cs`: Represents a reference to an attachment originating from a specific platform, used by `IPlatformAttachmentFetcher` to retrieve content.
            *   `PlatformType.cs`: Enum defining the possible source platforms for an interaction (e.g., Teams, Console, Api).
        *   `Orchestration/`: Contains interfaces and models related to the orchestration process.
            *   `IActivationChecker.cs`: Interface for checking if an incoming interaction should activate processing based on rules.
            *   `IBackgroundTaskQueue.cs`: Interface for queuing work items (like `NucleusIngestionRequest`) for asynchronous background processing.
            *   `InteractionContext.cs`: Represents the context of a single interaction after activation, containing the original request, resolved IDs, and extracted artifact content.
            *   `IOrchestrationService.cs`: Defines the central interface responsible for orchestrating incoming interaction requests. It acts as the primary entry point after the API, handling activation checks, determining sync/async processing, resolving personas, routing requests, and processing both initial and queued interactions.
            *   `IPersonaManager.cs`: Interface for managing the lifecycle and interaction processing for a *specific type* of Persona (Note: role may overlap/conflict with `PersonaRuntime`).
            *   `IPersonaResolver.cs`: Interface for resolving a canonical Persona ID based on platform-specific identifiers in a request.
            *   `NewSessionEvaluationResult.cs`: Represents the result of evaluating whether a new session should be initiated.
            *   `SalienceCheckResult.cs`: Represents the result of checking if an interaction is relevant (salient) to an existing session.
        *   `Repositories/`
            *   `IArtifactMetadataRepository.cs`: Defines the contract for storing, retrieving, updating, and deleting artifact metadata records.
            *   `IPersonaKnowledgeRepository.cs`: Defines the contract for storing and retrieving persona-specific knowledge entries, including analysis results linked to specific artifacts.
        *   `IArtifactProvider.cs`: Defines the contract for retrieving artifact content based on an `ArtifactReference`. Implementations handle ephemeral fetching from user-controlled storage.
        *   `IMessageQueuePublisher.cs`: Defines the contract for publishing messages to an asynchronous message queue or topic, decoupling producers from consumers.
        *   `IPersona.cs`: Base interface defining the core contract for persona implementations, including methods for interaction processing and analysis.
        *   `IPlatformAttachmentFetcher.cs`: Defines the contract for fetching the content stream of platform-specific files or attachments.
        *   `IPlatformNotifier.cs`: Defines the contract for sending notifications, acknowledgements, or responses back to the originating platform.
        *   `NucleusConstants.cs`: Defines shared constant strings (e.g., Persona keys, Configuration keys, Platform identifiers, LLM providers) used throughout the application to avoid magic strings and promote consistency.
        *   `Nucleus.Abstractions.csproj`
    *   `Nucleus.Domain/` (Core Business Logic, Entities, Domain Services)
        *   `Nucleus.Domain.Processing/` (Central Domain Services)
            *   `Resources/Dataviz/`: Contains static asset files (HTML template, CSS, JS, Python example) used by `DatavizHtmlBuilder` to create interactive, Pyodide-based visualizations.
                *   `dataviz_plotly_script.py`: Example Python script (using Plotly) intended to be executed by Pyodide within the generated HTML to render visualizations from provided JSON data.
                *   `dataviz_script.js`: Main JavaScript for the visualization HTML. Initializes Pyodide, manages the Python execution via the worker, handles data injection, and interacts with the HTML template.
                *   `dataviz_styles.css`: Defines the visual styling for the generated HTML visualization page.
                *   `dataviz_template.html`: The core HTML template file used by `DatavizHtmlBuilder`. Contains placeholders for injecting CSS, JS, Python code (for display), and JSON data (for display).
                *   `dataviz_worker.js`: JavaScript web worker script used to run Pyodide and the Python visualization script off the main browser thread.
            *   `Services/`
                *   `DatavizHtmlBuilder.cs`: Service class that reads assets from `Resources/Dataviz/`, injects Python code and JSON data, and builds the final self-contained HTML visualization artifact.
            *   [`ActivationChecker.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs): Implements `IActivationChecker`. Determines if an incoming interaction request (`AdapterRequest`) should trigger processing. Current implementation checks for a hardcoded '@Nucleus' mention. Future enhancements plan to use configurable rules.
            *   [`DefaultPersonaManager.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/DefaultPersonaManager.cs): Implements `IPersonaManager` for the hardcoded "Default_v1" persona type. Retrieves configuration and delegates `InitiateNewSessionAsync` to a specific `IPersona`. Methods for existing session management are not implemented. Role may overlap with `PersonaRuntime`.
            *   [`DefaultPersonaResolver.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/DefaultPersonaResolver.cs): Implements `IPersonaResolver`. Always returns a hardcoded default Persona ID ("Default_v1"). Includes a TODO to implement configurable resolution logic based on request details.
            *   `EmptyAnalysisData.cs`
            *   `IAgenticStrategyHandler.cs`: Interface defining the contract for handlers executing specific agentic strategy steps.
            *   `IPersonaRuntime.cs`: Interface defining the contract for the Persona execution runtime engine.
            *   [`InMemoryBackgroundTaskQueue.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/InMemoryBackgroundTaskQueue.cs): Implements `IBackgroundTaskQueue` using `System.Threading.Channels.Channel<NucleusIngestionRequest>`. Provides an in-memory queue suitable for single-instance deployments or testing.
            *   `Nucleus.Domain.Processing.csproj`
            *   [`OrchestrationService.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs): Implements `IOrchestrationService`, the central hub for processing interactions. Handles activation checks (`IActivationChecker`), persona resolution (`IPersonaResolver`), determines sync/async processing, queues tasks (`IBackgroundTaskQueue`), delegates core processing to `IPersonaRuntime` using configuration from `IPersonaConfigurationProvider`, and processes dequeued tasks.
            *   [`QueuedInteractionProcessorService.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs): A background service (`BackgroundService`) monitoring `IBackgroundTaskQueue`. Dequeues `NucleusIngestionRequest` items, creates DI scopes, uses `IOrchestrationService.HandleQueuedInteractionAsync` for processing, and `IPlatformNotifier` to send responses.
            *   [`ServiceCollectionExtensions.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/ServiceCollectionExtensions.cs): Provides extension methods (`AddProcessingServices`) for `IServiceCollection` to register services from this project (e.g., keyed `IPersonaManager`, `DatavizHtmlBuilder`) via dependency injection.
        *   `Personas/`
            *   `Nucleus.Personas.Core/` (Core Persona Implementations)
                *   [`BootstrapperPersona.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/BootstrapperPersona.cs): A basic `IPersona<EmptyAnalysisData>` implementation (`Bootstrapper_v1`). Uses `IChatClient` directly to handle queries, potentially prepending artifact content. Acts as a simple default or fallback persona, lacking complex analysis or planning logic.
                *   [`PersonaRuntime.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs): Implements `IPersonaRuntime`. This is the core execution engine for personas. It receives `PersonaConfiguration` and `InteractionContext`, uses the `StrategyKey` from the configuration to resolve the correct `IAgenticStrategyHandler` via keyed DI, and invokes its `HandleAsync` method with configuration, parameters, and context.
                *   `Strategies/` (Implementations of different agentic strategies)
                    *   [`EchoAgenticStrategyHandler.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Strategies/EchoAgenticStrategyHandler.cs): Implements `IAgenticStrategyHandler` for the "EchoStrategy". A simple test handler that returns the user's query text, prefixed with persona info, demonstrating the `PersonaRuntime` delegation mechanism.
                    *   *(Placeholder for future strategy handlers, e.g., SimpleChat, RAG, MultiStep, etc.)*
                *   `Interfaces/`
                    *   [`IAgenticStrategyHandler.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IAgenticStrategyHandler.cs): Defines the contract for specific agentic processing strategies (e.g., SimpleChat, RAG, EchoStrategy). Implementations contain the core logic for a particular strategy, are identified by a unique `StrategyKey`, and are resolved by `PersonaRuntime` via DI.
                    *   [`IPersonaRuntime.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs): Defines the contract for the runtime engine (`PersonaRuntime`) responsible for executing a specific `PersonaConfiguration` within an `InteractionContext`. Its primary role is to select and delegate execution to the appropriate `IAgenticStrategyHandler` based on the configuration.
                *   `Nucleus.Domain.Personas.Core.csproj`
    *   `Nucleus.Infrastructure/` (External Concerns: Data, Adapters, etc.)
        *   `Adapters/`
            *   `Nucleus.Adapters.Console/` (CLI Adapter)
                *   `_LocalData/`
                *   `Services/`
                    *   [`ConsoleArtifactProvider.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/ConsoleArtifactProvider.cs): Implements `IArtifactProvider` for the Console adapter. Supports references of type `"file"`, treating the `ReferenceId` as a local file path. Opens a `FileStream` for the file and returns it within an `ArtifactContent`, transferring stream ownership to the consumer. This class is responsible for providing artifact content from local files, enabling the Console adapter to interact with the Nucleus API using file-based artifacts.
                    *   [`NucleusApiServiceAgent.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/NucleusApiServiceAgent.cs): Client agent within the Console Adapter that communicates with the central Nucleus API. Uses an injected `HttpClient` (configured via DI, potentially with Aspire service discovery) to POST a standardized `AdapterRequest` to the `/api/interaction/process` endpoint and deserializes the `AdapterResponse`.
                *   `appsettings.json`: Standard ASP.NET Core configuration file for the Console Adapter.
                *   `Nucleus.Infrastructure.Adapters.Console.csproj`: C# project file for the Console Adapter.
                *   [`Program.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Program.cs): Main entry point for the Console Adapter application. Configures Dependency Injection (`Microsoft.Extensions.Hosting`), registers the `NucleusApiServiceAgent` typed `HttpClient` (leveraging Aspire service discovery), and sets up command-line handling using `System.CommandLine`. Defines `interactive` (default) and `ingest` commands. Both parse local file paths (e.g., `@"path/to/file.txt"`), create `ArtifactReference` objects (type `"file"`, ID=local path), construct an `AdapterRequest`, and send it to the central API via `NucleusApiServiceAgent`. Note: Registration of `ConsoleArtifactProvider` is currently commented out, aligning with the API-First model where artifact content retrieval is handled by the API service based on the reference, not resolved by the adapter itself.
            *   `Nucleus.Adapters.Teams/` (Microsoft Teams Adapter)
                *   [`GraphClientService.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs): Provides authenticated access to the Microsoft Graph API. Configured via `TeamsAdapterConfiguration`, it uses MSAL (`ConfidentialClientApplication`) to acquire application tokens and initializes a `GraphServiceClient`. Provides methods to list files in the configured Team's SharePoint drive (`ListTeamFilesAsync`) and to download the content stream and metadata of a specific file (`DownloadDriveItemContentStreamAsync`) using Drive and Item IDs. Includes robust error handling for Graph API calls.
                *   `Nucleus.Infrastructure.Adapters.Teams.csproj`: C# project file for the Teams Adapter. This project contains the core logic for integrating Nucleus with Microsoft Teams, including bot framework interactions and Graph API access.
                *   [`TeamsAdapterBot.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs): Core bot logic inheriting from Bot Framework's `ActivityHandler`. Handles incoming Teams messages (`OnMessageActivityAsync`), extracts user query and file attachment metadata, constructs `ArtifactReference` objects (type `"msgraph"`) without downloading content, builds an `AdapterRequest`, sends it to the Nucleus API via `HttpClient`, and provides initial user feedback via `IPlatformNotifier`. Also handles welcome messages (`OnMembersAddedAsync`).
                *   [`TeamsAdapterConfiguration.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterConfiguration.cs): Plain Old C# Object (POCO) class representing configuration settings specifically for the Teams Adapter. Typically loaded from `appsettings.json` using `IOptions<TeamsAdapterConfiguration>`. Contains properties like `MicrosoftAppId`, `MicrosoftAppPassword`, `TenantId` (optional), `TargetTeamId`, `TargetSharePointLibraryPath` (relative path within the Team's default library), and the `NucleusApiEndpoint`.
                *   [`TeamsNotifier.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsNotifier.cs): Implements `IPlatformNotifier` for the "Teams" platform (identified by `SupportedPlatformType`). Uses the injected `IBotFrameworkHttpAdapter` (must be a `CloudAdapter`) and stored `ConversationReference` data (retrieved based on `conversationId`, must include `ServiceUrl`) to send proactive messages via `CloudAdapter.ContinueConversationAsync`. `SendNotificationAsync` sends text messages (optionally replying to `replyToMessageId`), and `SendAcknowledgementAsync` sends typing indicators.
        *   `Data/`
            *   `Nucleus.Infrastructure.Persistence/` (Persistence Implementations)
                *   `ArtifactProviders/` (Empty)
                *   `Repositories/`
                    *   [`CosmosDbArtifactMetadataRepository.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs): Implements `IArtifactMetadataRepository` using Azure Cosmos DB. Injected with a configured Cosmos `Container`, it handles CRUD operations for `ArtifactMetadata` using the Cosmos SDK. Provides `GetByIdAsync` (requires ID and partition key), `GetBySourceIdentifierAsync` (potentially cross-partition query), `SaveAsync` (upsert, respecting partition key), and `DeleteAsync` (requires ID and partition key).
                    *   [`InMemoryArtifactMetadataRepository.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/InMemoryArtifactMetadataRepository.cs): In-memory implementation of `IArtifactMetadataRepository` using a `ConcurrentDictionary<string, ArtifactMetadata>`. Primarily intended for testing or local development; data is not persisted across application runs. Importantly, this implementation **ignores** partition keys provided in method arguments. Provides the standard `GetByIdAsync`, `GetBySourceIdentifierAsync`, `SaveAsync`, and `DeleteAsync` methods.
                *   `Configuration/`: Contains configuration-related classes for persistence.
                    *   [`InMemoryPersonaConfigurationProvider.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Configuration/InMemoryPersonaConfigurationProvider.cs): **Development/testing ONLY.** Implements `IPersonaConfigurationProvider` by loading hardcoded `PersonaConfiguration` objects (currently for Bootstrapper, Educator, Professional personas) into an internal `Dictionary<string, PersonaConfiguration>` upon instantiation. `GetConfigurationAsync` retrieves a specific persona's configuration from this dictionary by `personaId`. This provider must be replaced with a production-ready implementation (e.g., using `.NET IConfiguration`) for actual deployments.
                *   [`Nucleus.Infrastructure.Data.Persistence.csproj`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj): C# project file for the persistence layer implementations. Defines the target framework (`net9.0`), project references (`Nucleus.Abstractions`), and key NuGet dependencies like `Microsoft.Azure.Cosmos`, `Newtonsoft.Json`, and `Microsoft.Extensions.Logging.Abstractions` required for the data repositories and configuration providers within this project.
    *   `Nucleus.Services/` (Hosting Layer: APIs, Workers)
        *   `Nucleus.Services.Api/` (Main Backend API Service)
            *   `Configuration/`
                *   [`GeminiOptions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Configuration/GeminiOptions.cs): POCO class defining configuration options specifically for the Google Gemini AI provider. Maps to the `"Gemini"` section in `appsettings.json` or other configuration sources. Primarily holds the `ApiKey` required for authentication.
            *   `Controllers/`
                *   [`InteractionController.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs): The primary ASP.NET Core API controller handling incoming interaction requests from client adapters. Exposes the `POST /api/interaction/query` endpoint. Receives an `AdapterRequest`, performs basic validation, and delegates processing to the injected `IOrchestrationService`. It translates the returned `OrchestrationResult` status (Sync, Async, NotActivated, Error) into appropriate HTTP responses (e.g., `Ok(AdapterResponse)`, `Accepted()`, `Ok()`, `StatusCode(500)`).
            *   `Diagnostics/`
                *   [`ServiceBusSmokeTestService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Diagnostics/ServiceBusSmokeTestService.cs): **Development ONLY.** An `IHostedService` that runs once on application startup (via `IHostApplicationLifetime.ApplicationStarted`). It performs a basic smoke test by resolving `IMessageQueuePublisher<NucleusIngestionRequest>`, creating a test `NucleusIngestionRequest`, and attempting to publish it to the Azure Service Bus queue specified by the `NucleusIngestionQueueName` configuration setting. This verifies basic publisher configuration and connectivity.
            *   `Infrastructure/` (API-Specific Infrastructure)
                *   [`NullArtifactProvider.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/NullArtifactProvider.cs): A null implementation of `IArtifactProvider` specifically used in contexts like the core API service where direct artifact retrieval is architecturally disallowed (Zero Trust). It fulfills DI requirements but supports no reference types (`SupportedReferenceTypes` is empty) and logs warnings if its `GetContentAsync` or `GetArtifactStreamAsync` methods are called, returning null/failure.
                *   `Messaging/`
                    *   [`AzureServiceBusPublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs): Generic implementation (`AzureServiceBusPublisher<T>`) of `IMessageQueuePublisher<T>` using the Azure Service Bus SDK (`Azure.Messaging.ServiceBus`). Injected with `ServiceBusClient`, its `PublishAsync` method serializes the provided message payload (`T`) to JSON, creates a `ServiceBusSender` for the target queue/topic name, and sends the message using `SendMessageAsync`. Includes error handling and logging for serialization and publishing exceptions.
                    *   [`NullMessageQueuePublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullMessageQueuePublisher.cs): Generic null/no-op implementation (`NullMessageQueuePublisher<T>`) of `IMessageQueuePublisher<T>`. Used for testing, development, or scenarios where message queuing is not needed. Logs a warning on instantiation and a debug message when `PublishAsync` is called, returning `Task.CompletedTask` without sending any message.
                    *   [`NullPlatformNotifier.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullPlatformNotifier.cs): Implements `IPlatformNotifier` specifically for `PlatformType.Api`. As notifications for direct API interactions are handled via the HTTP response itself, this is a no-op implementation. Its methods log debug messages and return success without performing any actions.
                    *   [`ServiceBusQueueConsumerService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs): An `IHostedService` background service that consumes `NucleusIngestionRequest` messages from the configured Azure Service Bus queue (`NucleusIngestionQueueName`) using `ServiceBusProcessor`. For each message, it creates a DI scope, resolves `IOrchestrationService`, calls its `HandleQueuedInteractionAsync` method, and then explicitly completes, abandons, or dead-letters the message based on the processing outcome.
            *   `Properties/`
                *   [`launchSettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/Properties/launchSettings.json): Standard ASP.NET Core configuration file defining development-time launch profiles, environment variables, application URLs, and debug settings.
            *   [`AdapterWithErrorHandler.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/AdapterWithErrorHandler.cs): A Microsoft Bot Framework `CloudAdapter` implementation that provides an `OnTurnError` handler to catch, log, and report errors during bot turn processing. (Note: Its presence in the core API project is unusual; Bot Framework components typically reside in specific adapter projects like Nucleus.Adapters.Teams).
            *   [`appsettings.Development.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.Development.json): ASP.NET Core configuration file providing settings specifically for the 'Development' environment, overriding values in `appsettings.json`.
            *   [`appsettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.json): The primary ASP.NET Core configuration file containing default application settings (connection strings, logging levels, etc.).
            *   [`Nucleus.Services.Api.csproj`](../../src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj): The MSBuild project file for the Nucleus API service. Defines target framework, NuGet package dependencies, project references, and other build configurations.
            *   [`Program.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs): The main entry point for the ASP.NET Core API service. Initializes the `WebApplicationBuilder`, integrates with .NET Aspire (`AddServiceDefaults`, service discovery), loads configuration, and delegates service registration (`AddNucleusServices`) and endpoint mapping (`MapNucleusEndpoints`) to extension methods before running the application.
            *   [`WebApplicationBuilderExtensions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/WebApplicationBuilderExtensions.cs): Defines extension methods called from `Program.cs`. `AddNucleusServices` configures Dependency Injection for core services (config, AI, data, messaging, personas, adapters). `MapNucleusEndpoints` configures the HTTP pipeline and maps endpoints (API controllers, Bot Framework, Aspire defaults).
        *   `Nucleus.Services.Shared/`: Shared code potentially used across multiple services.
            *   `Extraction/`: Classes related to extracting content from various sources.
                *   `HtmlContentExtractor.cs`: Extracts text content from HTML.
                *   `IContentExtractor.cs`: Interface for content extraction logic.
                *   `PlainTextContentExtractor.cs`: Extracts text content from plain text (essentially a pass-through).
            *   `Nucleus.Services.Shared.csproj`: C# project file for shared service components.

### Testing (`tests/`)

*   `tests/`: Root directory for all test projects.
    *   `Integration/`: Integration test projects.
        *   `Nucleus.Services.Api.IntegrationTests/`: Integration tests for the `Nucleus.Services.Api`.
            *   `Infrastructure/`: Test-specific infrastructure mocks/stubs.
                *   `NullArtifactMetadataRepository.cs`: A test double repository that does nothing, used to isolate tests from real persistence.
            *   `TestData/`: Sample data for tests.
                *   `sensitive_data_test.txt`: Sample file possibly containing sensitive data patterns for testing redaction/security.
                *   `test_artifact.txt`: Sample artifact file for testing
            *   `TestResults/`: (Typically gitignored) Output from test runs.
            *   `ApiIntegrationTests.cs`: Tests using `HttpClient` against the API via `WebApplicationFactory`.
            *   `Nucleus.Services.Api.IntegrationTests.csproj`: C# project file.

### Root Files

*   `.editorconfig`: Coding style definitions.
*   `.gitattributes`: Git path attributes.
*   `.gitignore`: Specifies files ignored by Git.
*   `.windsurfrules`: AI agent configuration and rules.
*   `LICENSE.txt`: Project license.
*   `Nucleus.sln`: Visual Studio Solution file.
*   `README.md`: Main project README.

---