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

This section provides a comprehensive listing of the files and directories within the Nucleus project, derived from the `tree_gitignore.py` script output as of **2025-04-30**. It is structured by major functional areas (Aspire, Src, Tests) and then by individual C# projects within `Src`, serving as a persistent context for the AI, detailing the purpose of each significant file and project component based on the refactored structure.

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

#### `Nucleus.Abstractions/` (Core Interfaces, DTOs, Base Types)

*   `Models/` (Data Transfer Objects and Configuration Models)
    *   `Configuration/`
        *   `AgenticStrategyParametersBase.cs`: Base class enabling specific agentic strategies to receive unique parameters. See: [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
        *   `GoogleAiOptions.cs`: Configuration binding class for Google AI services, supporting the modular AI provider design. See: [AI Integration](../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md)
        *   `IPersonaConfigurationProvider.cs`: Interface abstracting the source of persona configurations (e.g., files, DB), central to persona resolution. See: [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
        *   `PersonaConfiguration.cs`: Represents the loaded configuration defining a persona's behavior, including its agentic strategy. See: [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
    *   `AdapterRequest.cs`: Primary DTO encapsulating an incoming interaction from any client adapter, embodying the API-First principle. Includes essential context and potentially `ArtifactReference`s. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Client Interaction Pattern](../../Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
    *   `AdapterResponse.cs`: Standard response DTO from the API back to adapters, ensuring consistent communication structure. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md)
    *   `ArtifactContent.cs`: Represents the *ephemeral*, disposable content stream (and metadata like MIME type) retrieved via `IArtifactProvider`, crucial for Zero Trust. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md)
    *   `ArtifactMetadata.cs`: Core persisted record for an artifact, stored centrally (Cosmos DB) and linked via IDs. Contains descriptive info but *not* content. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md), [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md)
    *   `ArtifactReference.cs`: **Key DTO** representing a pointer to content in user-controlled storage. Enables reference-based processing, avoids direct uploads, and is fundamental to Nucleus's OmniRAG and security model. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md), [Security Architecture](../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md), [Overview (OmniRAG)](../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md#data-processing-approach-comparison-nucleus-vs-standard-rag)
    *   `NucleusIngestionRequest.cs`: DTO used both for direct API processing initiation and as the payload for the async background queue. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   `PlatformAttachmentReference.cs`: Represents a platform-specific attachment reference, enabling `IPlatformAttachmentFetcher` to retrieve content from sources like Teams messages. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md)
    *   `PlatformType.cs`: Enum identifying the interaction's source platform, enabling platform-specific logic selection.
*   `Orchestration/` (Interfaces and DTOs for the interaction processing pipeline)
    *   `ExtractedArtifact.cs`: Holds textual content extracted from a source artifact during ephemeral processing. See: [Processing Architecture](../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md)
    *   `IActivationChecker.cs`: Interface for logic determining if an incoming `AdapterRequest` warrants activation based on rules. See: [Orchestration Routing](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)
    *   `IBackgroundTaskQueue.cs`: Abstraction for queuing `NucleusIngestionRequest` for reliable asynchronous processing, decoupling acceptance from execution. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   `InteractionContext.cs`: Holds the consolidated state (resolved IDs, request, extracted content) for a single interaction *after* initial orchestration. See: [Interaction Lifecycle](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
    *   `IOrchestrationService.cs`: **Central interface** responsible for the initial handling of `AdapterRequest`s: activation, persona resolution, sync/async decision, and initiating processing via `IPersonaRuntime` or `IBackgroundTaskQueue`. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md), [Interaction Lifecycle](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
    *   `IPersonaResolver.cs`: Interface for resolving the canonical Persona ID from the incoming request context. See: [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md), [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   `NewSessionEvaluationResult.cs`, `SalienceCheckResult.cs`: DTOs related to determining if an interaction starts or belongs to a new/existing logical session. See: [Session Initiation](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md)
    *   `OrchestrationResult.cs`: DTO returned by the initial synchronous part of `IOrchestrationService` indicating immediate outcome (e.g., queued, rejected, simple response). See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   `Repositories/` (Interfaces defining data access contracts - Implementations in Infrastructure)
    *   `IArtifactMetadataRepository.cs`: Contract for managing persisted `ArtifactMetadata` records. See: [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md)
    *   `IPersonaKnowledgeRepository.cs`: Contract for managing persisted persona-specific knowledge (embeddings, analysis). See: [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md)
*   `IArtifactProvider.cs`: **Key interface** defining the contract for *ephemerally* retrieving artifact content based on an `ArtifactReference`. Implementations handle specific source systems (Graph, file system) while adhering to Zero Trust. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md), [Security Architecture](../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md)
*   `IMessageQueuePublisher.cs`: Abstraction for publishing messages (typically `NucleusIngestionRequest`) to the async queue. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   `IPersona.cs`: Base interface defining the core contract for persona implementations. *(Note: Current architecture favors configuration-driven strategies via `PersonaRuntime` over direct `IPersona` implementations.)* See: [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
*   `IPlatformAttachmentFetcher.cs`: Interface for retrieving content of platform-specific attachments (e.g., Teams file uploads) using `PlatformAttachmentReference`. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md)
*   `IPlatformNotifier.cs`: Abstraction for sending notifications or final responses back to the originating platform, used often for completing async operations. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md), [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   `NucleusConstants.cs`: Shared constant string values to avoid magic strings and ensure consistency.
*   `Nucleus.Abstractions.csproj`: C# project file defining dependencies and build settings.

#### `Nucleus.Domain.Processing/` (Central Domain Services & Processing Logic)

*   `Resources/Dataviz/`: Contains static assets (HTML, CSS, JS, Python example) used by `DatavizHtmlBuilder` to construct interactive, Pyodide-based HTML visualization artifacts. Represents a specific type of structured output generation within the processing pipeline. See: [Processing Architecture](../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md)
    *   `dataviz_plotly_script.py`: Example Python script (using Plotly) intended for execution by Pyodide within the visualization HTML.
    *   `dataviz_script.js`: Main JavaScript for the visualization; manages Pyodide, Python execution via worker, data injection.
    *   `dataviz_styles.css`: Styling for the visualization page.
    *   `dataviz_template.html`: Core HTML template with placeholders for injected content.
    *   `dataviz_worker.js`: Web worker script for running Pyodide/Python off the main thread.
*   `Services/`
    *   `DatavizHtmlBuilder.cs`: Service responsible for assembling the final, self-contained HTML visualization by reading assets and injecting data/code. See: [Processing Architecture](../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md)
*   [`ActivationChecker.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs): Implements `IActivationChecker`. Contains the logic to decide if an incoming `AdapterRequest` warrants activating Nucleus. Integral part of the initial routing decision. See: [Orchestration Routing](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)
*   [`DefaultPersonaResolver.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/DefaultPersonaResolver.cs): Default implementation of `IPersonaResolver`. Returns a hardcoded Persona ID, representing the component responsible for determining which Persona handles an interaction. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md), [Personas](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
*   [`InMemoryBackgroundTaskQueue.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/InMemoryBackgroundTaskQueue.cs): In-memory implementation of `IBackgroundTaskQueue` using `System.Threading.Channels`. Enables reliable asynchronous processing for single-instance deployments. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md), [API Async Pattern](../../Docs/Architecture/10_ARCHITECTURE_API.md#6-asynchronous-processing-pattern)
*   [`OrchestrationService.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs): **Core implementation** of `IOrchestrationService`. Orchestrates the entire initial interaction flow: receives `AdapterRequest`, performs activation checks, resolves the persona, decides sync/async path, queues work via `IBackgroundTaskQueue`, and delegates to `IPersonaRuntime` for actual processing (both sync and async via `HandleQueuedInteractionAsync`). See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md), [Interaction Lifecycle](../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [`QueuedInteractionProcessorService.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs): `BackgroundService` that dequeues `NucleusIngestionRequest` items from `IBackgroundTaskQueue`. Creates DI scopes and invokes `IOrchestrationService.HandleQueuedInteractionAsync` to process queued work, decoupling processing from the initial request handling. See: [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md), [API Async Pattern](../../Docs/Architecture/10_ARCHITECTURE_API.md#6-asynchronous-processing-pattern)
*   [`ServiceCollectionExtensions.cs`](../../src/Nucleus.Domain/Nucleus.Domain.Processing/ServiceCollectionExtensions.cs): Standard DI extension methods for registering services defined within this project.
*   `Nucleus.Domain.Processing.csproj`: C# project file for the domain processing services library.

#### `Nucleus.Domain.Personas.Core/` (Core Persona Implementations & Runtime)

*   [`PersonaRuntime.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs): Implements `IPersonaRuntime`. **Core execution engine** for executing persona logic. Receives `PersonaConfiguration` (defining the persona's strategy and parameters) and `InteractionContext`. Uses keyed DI to resolve the appropriate `IAgenticStrategyHandler` based on the `StrategyKey` in the configuration and invokes it. This decouples the orchestration logic from the specific persona behavior implementation. See: [Personas Architecture](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md), [Processing Orchestration](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   `Strategies/` (Implementations of different agentic strategies)
    *   [`EchoAgenticStrategyHandler.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Strategies/EchoAgenticStrategyHandler.cs): Example implementation of `IAgenticStrategyHandler` for the "EchoStrategy". Demonstrates the basic mechanics of a strategy handler receiving context and configuration, and returning a simple response. See: [Personas Architecture](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
    *   *(Placeholder for future strategy handlers, e.g., SimpleChat, RAG, MultiStep, etc.)*
*   `Interfaces/`
    *   [`IAgenticStrategyHandler.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IAgenticStrategyHandler.cs): Defines the contract for specific agentic processing strategies (e.g., SimpleChat, RAG). Implementations encapsulate the core logic for a particular persona behavior, are identified by a unique `StrategyKey`, and are resolved/invoked by `PersonaRuntime`. This enables the Strategy Pattern for personas. See: [Personas Architecture](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
    *   [`IPersonaRuntime.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs): Defines the contract for the runtime engine (`PersonaRuntime`) responsible for executing a specific `PersonaConfiguration` within an `InteractionContext`. Abstracts the mechanism of selecting and delegating execution to the appropriate strategy handler. See: [Personas Architecture](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
*   [`EmptyAnalysisData.cs`](../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/EmptyAnalysisData.cs): Placeholder/marker class, often used as a generic type argument for personas or strategies that don't produce specific analysis data structures.
*   `Nucleus.Domain.Personas.Core.csproj`: C# project file for core persona implementations.

#### `Nucleus.Infrastructure.Adapters.Console/` (Command Line Interface Adapter)

A basic command-line client application demonstrating interaction with the Nucleus API. Primarily used for development, testing, and simple file ingestion scenarios. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md)

*   `_LocalData/`: Directory for local data specific to the adapter (e.g., test files), typically excluded from source control.
*   `Services/`
    *   [`ConsoleArtifactProvider.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/ConsoleArtifactProvider.cs): Implements `IArtifactProvider` to retrieve content for `ArtifactReference`s with type `"file"`, using the `ReferenceId` as a local path. Note: This is typically *not registered* in DI for the adapter itself, as content retrieval is handled by the central API Service based on the reference provided in `AdapterRequest`, adhering to the API-First principle. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md)
    *   [`NucleusApiServiceAgent.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/NucleusApiServiceAgent.cs): **Core client agent** responsible for sending `AdapterRequest` DTOs to the central Nucleus API's `/api/interaction/process` endpoint using a configured `HttpClient`. Handles serialization and deserialization of the request/response. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Client Interaction Pattern](../../Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
*   `appsettings.json`: Standard ASP.NET Core configuration file, typically defining the `NucleusApiEndpoint`.
*   [`Program.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Program.cs): Main entry point. Sets up DI (hosting, typed `HttpClient` for `NucleusApiServiceAgent`, potentially leveraging Aspire service discovery), configures command-line parsing (`System.CommandLine`), defines commands (`interactive`, `ingest`), constructs `AdapterRequest` with file-based `ArtifactReference`s, and triggers processing via `NucleusApiServiceAgent`. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md)
*   `Nucleus.Infrastructure.Adapters.Console.csproj`: C# project file for the Console Adapter application.

#### `Nucleus.Infrastructure.Adapters.Teams/` (Microsoft Teams Adapter)

Integrates Nucleus with Microsoft Teams, allowing users to interact via chat messages and file uploads within Teams channels or chats. Uses the Microsoft Bot Framework SDK. See: [Client Architecture](../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md), [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_OVERVIEW.md)

*   [`GraphClientService.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs): Provides authenticated access to the Microsoft Graph API using MSAL and `TeamsAdapterConfiguration`. Offers methods for potential file listing (`ListTeamFilesAsync`) or direct content retrieval (`DownloadDriveItemContentStreamAsync`) from the configured Team's SharePoint drive. While artifact content retrieval is primarily handled API-side based on references, this service provides the necessary Graph interaction capabilities if needed directly by the adapter or for supporting functions. See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_GRAPH.md)
*   [`TeamsAdapterBot.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs): **Core bot logic** inheriting from Bot Framework's `ActivityHandler`. Processes incoming Teams messages (`OnMessageActivityAsync`), extracts user query, identifies file attachments (creating `ArtifactReference`s with type `"msgraph"` and Graph item IDs), constructs the standard `AdapterRequest`, and sends it to the Nucleus API via a configured `HttpClient`. Uses `IPlatformNotifier` for initial user feedback (acknowledgements). See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_BOT_LOGIC.md), [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Client Interaction Pattern](../../Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
*   [`TeamsAdapterConfiguration.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterConfiguration.cs): Configuration POCO loaded via `IOptions`, holding settings essential for the Teams adapter (App ID/Secret, Tenant ID, Target Team/Library, API endpoint). See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_CONFIGURATION.md)
*   [`TeamsNotifier.cs`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsNotifier.cs): Implements `IPlatformNotifier` specifically for Teams. Uses the Bot Framework `CloudAdapter` and stored `ConversationReference` details to send proactive messages (final responses, notifications, acknowledgements like typing indicators) back to the correct Teams conversation, often used to complete asynchronous operations initiated by the API. See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_NOTIFIER.md)
*   `Nucleus.Infrastructure.Adapters.Teams.csproj`: C# project file for the Teams Adapter application.

#### `Nucleus.Infrastructure.Data.Persistence/` (Persistence Implementations)

Provides concrete implementations for data access interfaces defined in `Nucleus.Abstractions`, primarily focusing on repositories and configuration providers. See: [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md), [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)

*   `Repositories/`
    *   [`CosmosDbArtifactMetadataRepository.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs): **Production implementation** of `IArtifactMetadataRepository` using Azure Cosmos DB. Leverages the Cosmos SDK to perform CRUD operations on `ArtifactMetadata` documents, utilizing partition keys for scalability. See: [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md)
    *   [`InMemoryArtifactMetadataRepository.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/InMemoryArtifactMetadataRepository.cs): **Testing/Development implementation** of `IArtifactMetadataRepository` using a `ConcurrentDictionary`. Data is volatile and lost on application restart. Useful for local development or integration tests where a persistent database is not required or desired. See: [Database Architecture](../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md)
*   `Configuration/`: Contains implementations related to loading configuration data.
    *   [`InMemoryPersonaConfigurationProvider.cs`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Configuration/InMemoryPersonaConfigurationProvider.cs): **Development/testing ONLY.** Implements `IPersonaConfigurationProvider` using hardcoded `PersonaConfiguration` objects loaded into a dictionary. Provides a simple way to test different personas without external configuration files during early development. MUST be replaced by a production provider (e.g., reading from `IConfiguration`) for real deployments. See: [Personas Architecture](../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md), [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)
*   [`Nucleus.Infrastructure.Data.Persistence.csproj`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj): C# project file for the persistence layer implementations.

#### `Nucleus.Services.Api/` (Main Backend API Service)

The central ASP.NET Core web service hosting the Nucleus API. It receives requests from client adapters, orchestrates processing (potentially asynchronously via message queues), and interacts with underlying data stores and AI models. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Aspire Orchestration](../../Docs/Architecture/11_ARCHITECTURE_ASPIRE.md)

*   `Configuration/`
    *   [`GeminiOptions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Configuration/GeminiOptions.cs): Configuration POCO for Google Gemini, mapping to the `"Gemini"` section in `appsettings.json`, primarily holding the `ApiKey`. See: [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)
*   `Controllers/`
    *   [`InteractionController.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs): **Primary API endpoint**. Exposes `POST /api/interaction/process`. Receives `AdapterRequest`, delegates to `IOrchestrationService`, and maps `OrchestrationResult` to appropriate HTTP responses (e.g., 200 OK for sync success/not activated, 202 Accepted for async). See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Orchestration Architecture](../../Docs/Architecture/01_ARCHITECTURE_ORCHESTRATION.md)
*   `Diagnostics/`
    *   [`ServiceBusSmokeTestService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Diagnostics/ServiceBusSmokeTestService.cs): **Development ONLY.** `IHostedService` performing a startup check to publish a test message via `IMessageQueuePublisher<NucleusIngestionRequest>`, verifying basic messaging configuration and connectivity. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
*   `Infrastructure/` (API-Specific Infrastructure Components)
    *   `Messaging/`
        *   [`AzureServiceBusPublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs): Generic `IMessageQueuePublisher<T>` implementation using Azure Service Bus SDK. Serializes messages to JSON and sends them. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`NullMessageQueuePublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullMessageQueuePublisher.cs): No-op `IMessageQueuePublisher<T>` for testing/dev environments. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`NullPlatformNotifier.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullPlatformNotifier.cs): No-op `IPlatformNotifier` for the "Api" platform. API responses are handled via HTTP, so direct notification is unnecessary. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`ServiceBusQueueConsumerService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs): **Background worker** (`IHostedService`) that consumes `NucleusIngestionRequest` messages from Azure Service Bus. Creates DI scopes, resolves `IOrchestrationService`, calls `HandleQueuedInteractionAsync`, and manages message lifecycle (complete/abandon/dead-letter). See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md), [Async Processing](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ASYNC.md)
    *   [`NullArtifactProvider.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/NullArtifactProvider.cs): No-op `IArtifactProvider`. The API service relies on client adapters or dedicated artifact services to provide content based on references; it does not retrieve artifacts directly. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md)
*   `Properties/`
    *   [`launchSettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/Properties/launchSettings.json): Standard ASP.NET Core development launch profiles.
*   [`AdapterWithErrorHandler.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/AdapterWithErrorHandler.cs): Bot Framework `CloudAdapter` with error handling. (Note: Likely misplaced here; typically belongs in a specific Bot Framework adapter project like Teams). Consider refactoring. See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_OVERVIEW.md)
*   [`appsettings.Development.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.Development.json): Development environment-specific configuration overrides.
*   [`appsettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.json): Primary configuration file (defaults).
*   [`Nucleus.Services.Api.csproj`](../../src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj): MSBuild project file for the API service.
*   [`Program.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs): **Application entry point.** Initializes `WebApplicationBuilder`, integrates Aspire (`AddServiceDefaults`), loads configuration, calls extension methods for service registration and endpoint mapping, and runs the app. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md)
*   [`WebApplicationBuilderExtensions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/WebApplicationBuilderExtensions.cs): **DI and Pipeline Configuration.** Contains extension methods (`AddNucleusServices`, `MapNucleusEndpoints`) used in `Program.cs` to register services (Config, AI, Data, Messaging, Orchestration, etc.) and configure the HTTP request pipeline (Controllers, Aspire defaults, potentially Bot Framework endpoints). See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)

#### `Nucleus.Services.Api/` (Main Backend API Service)

The central ASP.NET Core web service hosting the Nucleus API. It receives requests from client adapters, orchestrates processing (potentially asynchronously via message queues), and interacts with underlying data stores and AI models. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Aspire Orchestration](../../Docs/Architecture/11_ARCHITECTURE_ASPIRE.md)

*   `Configuration/`
    *   [`GeminiOptions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Configuration/GeminiOptions.cs): Configuration POCO for Google Gemini, mapping to the `"Gemini"` section in `appsettings.json`, primarily holding the `ApiKey`. See: [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)
*   `Controllers/`
    *   [`InteractionController.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs): **Primary API endpoint**. Exposes `POST /api/interaction/process`. Receives `AdapterRequest`, delegates to `IOrchestrationService`, and maps `OrchestrationResult` to appropriate HTTP responses (e.g., 200 OK for sync success/not activated, 202 Accepted for async). See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Orchestration Architecture](../../Docs/Architecture/01_ARCHITECTURE_ORCHESTRATION.md)
*   `Diagnostics/`
    *   [`ServiceBusSmokeTestService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Diagnostics/ServiceBusSmokeTestService.cs): **Development ONLY.** `IHostedService` performing a startup check to publish a test message via `IMessageQueuePublisher<NucleusIngestionRequest>`, verifying basic messaging configuration and connectivity. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
*   `Infrastructure/` (API-Specific Infrastructure Components)
    *   `Messaging/`
        *   [`AzureServiceBusPublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs): Generic `IMessageQueuePublisher<T>` implementation using Azure Service Bus SDK. Serializes messages to JSON and sends them. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`NullMessageQueuePublisher.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullMessageQueuePublisher.cs): No-op `IMessageQueuePublisher<T>` for testing/dev environments. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`NullPlatformNotifier.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullPlatformNotifier.cs): No-op `IPlatformNotifier` for the "Api" platform. API responses are handled via HTTP, so direct notification is unnecessary. See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md)
        *   [`ServiceBusQueueConsumerService.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs): **Background worker** (`IHostedService`) that consumes `NucleusIngestionRequest` messages from Azure Service Bus. Creates DI scopes, resolves `IOrchestrationService`, calls `HandleQueuedInteractionAsync`, and manages message lifecycle (complete/abandon/dead-letter). See: [Messaging Architecture](../../Docs/Architecture/06_ARCHITECTURE_MESSAGING.md), [Async Processing](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ASYNC.md)
    *   [`NullArtifactProvider.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/NullArtifactProvider.cs): No-op `IArtifactProvider`. The API service relies on client adapters or dedicated artifact services to provide content based on references; it does not retrieve artifacts directly. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md)
*   `Properties/`
    *   [`launchSettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/Properties/launchSettings.json): Standard ASP.NET Core development launch profiles.
*   [`AdapterWithErrorHandler.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/AdapterWithErrorHandler.cs): Bot Framework `CloudAdapter` with error handling. (Note: Likely misplaced here; typically belongs in a specific Bot Framework adapter project like Teams). Consider refactoring. See: [Teams Adapter Architecture](../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_OVERVIEW.md)
*   [`appsettings.Development.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.Development.json): Development environment-specific configuration overrides.
*   [`appsettings.json`](../../src/Nucleus.Services/Nucleus.Services.Api/appsettings.json): Primary configuration file (defaults).
*   [`Nucleus.Services.Api.csproj`](../../src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj): MSBuild project file for the API service.
*   [`Program.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs): **Application entry point.** Initializes `WebApplicationBuilder`, integrates Aspire (`AddServiceDefaults`), loads configuration, calls extension methods for service registration and endpoint mapping, and runs the app. See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md)
*   [`WebApplicationBuilderExtensions.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/WebApplicationBuilderExtensions.cs): **DI and Pipeline Configuration.** Contains extension methods (`AddNucleusServices`, `MapNucleusEndpoints`) used in `Program.cs` to register services (Config, AI, Data, Messaging, Orchestration, etc.) and configure the HTTP request pipeline (Controllers, Aspire defaults, potentially Bot Framework endpoints). See: [API Architecture](../../Docs/Architecture/10_ARCHITECTURE_API.md), [Configuration](../../Docs/Architecture/09_ARCHITECTURE_CONFIGURATION.md)

#### `Nucleus.Services.Shared/` (Shared Code Across Services)

A utility library project containing common interfaces, base classes, and helper methods used by multiple Nucleus service projects (e.g., `Nucleus.Services.Api`, potentially future processing workers). Promotes code reuse and consistency.

*   `Extraction/`: Implements a strategy pattern for extracting text content from different source formats (identified by MIME type).
    *   [`HtmlContentExtractor.cs`](../../src/Nucleus.Services/Nucleus.Services.Shared/Extraction/HtmlContentExtractor.cs): Extracts text from HTML using a suitable parsing library (e.g., HtmlAgilityPack).
    *   [`IContentExtractor.cs`](../../src/Nucleus.Services/Nucleus.Services.Shared/Extraction/IContentExtractor.cs): Defines the common interface for all content extractors, requiring a `SupportedMimeType` property and an `ExtractTextAsync` method.
    *   [`PlainTextContentExtractor.cs`](../../src/Nucleus.Services/Nucleus.Services.Shared/Extraction/PlainTextContentExtractor.cs): Handles plain text files, essentially passing the content through.
    *   See: [Artifact Processing](../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ARTIFACT_PIPELINE.md)
*   [`MimeTypeHelper.cs`](../../src/Nucleus.Services/Nucleus.Services.Shared/MimeTypeHelper.cs): Provides utility methods for determining the MIME type of a file, likely based on its file extension. Essential for selecting the correct `IContentExtractor`. See: [Storage Architecture](../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md)
*   [`Nucleus.Services.Shared.csproj`](../../src/Nucleus.Services/Nucleus.Services.Shared/Nucleus.Services.Shared.csproj): C# project file for this shared utility library.

### Testing (`tests/`)

Contains automated tests for the Nucleus system. Primarily focuses on integration tests against the API layer, ensuring core functionality and contracts are met. See: [Testing Architecture](../../Docs/Architecture/12_ARCHITECTURE_TESTING.md)

*   `tests/`: Root directory for all test projects.
    *   `Integration/`: Integration test projects.
        *   `Nucleus.Services.Api.IntegrationTests/`: Integration tests specifically targeting the `Nucleus.Services.Api` endpoints.
            *   `Infrastructure/`: Test-specific infrastructure mocks/stubs.
                *   [`NullArtifactMetadataRepository.cs`](../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Infrastructure/NullArtifactMetadataRepository.cs): A test double repository that does nothing, used to isolate tests from real persistence.
            *   `TestData/`: Sample data files used by tests.
                *   `sensitive_data_test.txt`: Sample file possibly containing sensitive data patterns for testing redaction/security.
                *   `test_artifact.txt`: Generic sample artifact file for testing processing flows.
            *   `TestResults/`: (Typically gitignored) Output directory for test run results.
            *   [`ApiIntegrationTests.cs`](../../tests/Integration/Nucleus.Services.Api.IntegrationTests/ApiIntegrationTests.cs): C# integration tests using `HttpClient` against the API, likely bootstrapped via `WebApplicationFactory<Program>` for in-memory testing.
            *   [`Nucleus.Services.Api.IntegrationTests.csproj`](../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj): C# project file for the integration tests.
            *   [`test_ingest_agent_api.ps1`](../../tests/Integration/Nucleus.Services.Api.IntegrationTests/test_ingest_agent_api.ps1): PowerShell script for invoking the API's ingestion endpoint, likely used for manual or scripted integration testing.
            *   [`test_query_agent_api.ps1`](../../tests/Integration/Nucleus.Services.Api.IntegrationTests/test_query_agent_api.ps1): PowerShell script for invoking API endpoints related to querying agent state or results, used for manual or scripted testing.

### Root Files

*   `.editorconfig`: Coding style definitions.
*   `.gitattributes`: Git path attributes.
*   `.gitignore`: Specifies files ignored by Git.
*   `.windsurfrules`: AI agent configuration and rules.
*   `LICENSE.txt`: Project license.
*   `Nucleus.sln`: Visual Studio Solution file.
*   `README.md`: Main project README.

---