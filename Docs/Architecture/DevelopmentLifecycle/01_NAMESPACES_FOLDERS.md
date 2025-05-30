---
title: "Nucleus Project Namespaces and Folder Structure"
description: "Defines the standardized namespace and folder organization for the Nucleus project, ensuring consistency and maintainability across the codebase."
version: 4.2 # Incremented version
date: 2025-05-29 # Current date
parent: ./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md
see_also:
    - title: "Comprehensive System Architecture"
      link: "../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md" # Corrected link
    - title: "Testing Strategy"
      link: ./02_TESTING_STRATEGY.md
    - title: "CI/CD Strategy" # Updated title
      link: ./03_CICD_STRATEGY.md # Corrected link
    - ./04_CODE_STYLE_LINTING.md
    - ./05_API_DESIGN_GUIDELINES.md
    - ./06_DEVOPS_CICD_PIPELINE.md
    - ./07_TELEMETRY_LOGGING_STRATEGY.md
    - ./08_ERROR_HANDLING_RESILIENCY.md
    - ./09_SECURITY_DEVELOPMENT_LIFECYCLE.md
    - ./10_DOCUMENTATION_STANDARDS.md
    - ../CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md
---

# Nucleus Project Namespaces and Folder Structure

## 1. Introduction

This document outlines the standardized namespace and folder structure for the Nucleus project. Adherence to these conventions is crucial for maintaining a clean, understandable, and scalable codebase. It facilitates easier navigation, reduces merge conflicts, and promotes a consistent development experience.

The structure is designed to reflect the logical components of the Nucleus system, aligning with the overall architecture.

## 2. Top-Level Solution Structure (`.sln`)

The Nucleus solution (`Nucleus.sln`) is organized into solution folders to group related projects. This provides a high-level organization within IDEs like Visual Studio or Rider.

*   **`src/`**: Contains all source code for the deployable applications and reusable libraries.
*   **`tests/`**: Contains all test projects, mirroring the structure of `src/`.
*   **`tools/`**: Contains utility projects, scripts, or tools used during development but not deployed as part of the main application.
*   **`docs/`**: Contains all project documentation, including architecture, requirements, and operational guides. (This folder is not part of the .sln but is a top-level folder in the repository).
*   **`build/`**: Contains build-related scripts and configurations. (This folder is not part of the .sln but is a top-level folder in the repository).

## 3. `src/` Layer Structure and Namespaces

Projects within the `src/` directory follow a consistent naming convention: `Nucleus.<Layer>.<Component>`.

### 3.1. Core Architectural Layers

*   **`Nucleus.Shared.Kernel`**: (Formerly `Nucleus.Domain.Personas.Core`)
    *   **Purpose**: Contains the absolute core, shared abstractions, interfaces, and foundational models used across the entire Nucleus ecosystem. This includes `IPersona`, `ArtifactMetadata`, `PersonaKnowledgeEntry`, core `Result` types, and other fundamental building blocks that are not specific to any single application or domain but are essential for the interoperability and coherence of all Nucleus components. It has minimal dependencies.
    *   **Namespace**: `Nucleus.Shared.Kernel.*`
    *   **Examples**: `Nucleus.Shared.Kernel.Abstractions`, `Nucleus.Shared.Kernel.Models`, `Nucleus.Shared.Kernel.Results`
    *   **Key Dependencies**: None (or only .NET BCL and essential, stable, third-party libraries like `MediatR` if absolutely necessary for core patterns).
    *   **Key Dependents**: Almost all other Nucleus projects.

*   **`Nucleus.Domain.RAGLogic`**: (Formerly `Nucleus.Domain.Processing`, purpose clarified)
    *   **Purpose**: Encapsulates the domain logic specifically related to the Retrieval Augmented Generation (RAG) pipeline, content processing, analysis, and knowledge extraction. This includes services for interacting with LLMs for analysis (not orchestration of user-facing interactions), text processing, embedding generation, and defining the logic for how `PersonaKnowledgeEntry` items are created and structured. This layer is responsible for the "intelligence" in transforming raw content into structured knowledge.
    *   **Namespace**: `Nucleus.Domain.RAGLogic.*`
    *   **Examples**: `Nucleus.Domain.RAGLogic.Analysis`, `Nucleus.Domain.RAGLogic.Extraction`, `Nucleus.Domain.RAGLogic.Embedding`
    *   **Key Dependencies**: `Nucleus.Shared.Kernel`.
    *   **Key Dependents**: MCP Tools (especially ContentProcessing, RAGPipeline), potentially Agent applications if they perform specialized pre-processing.

*   **`Nucleus.Infrastructure.<Provider>`**:
    *   **Purpose**: Implements external concerns and infrastructure-specific details, such as database access, message queue interactions, file system access, or integrations with third-party services (excluding AI model providers, which are handled by `Nucleus.Infrastructure.Ai`). Each provider is a separate project.
    *   **Namespace**: `Nucleus.Infrastructure.<Provider>.*`
    *   **Examples**:
        *   `Nucleus.Infrastructure.CosmosDb` (for Azure Cosmos DB persistence)
        *   `Nucleus.Infrastructure.ServiceBus` (for Azure Service Bus messaging)
        *   `Nucleus.Infrastructure.FileProviders.Platform` (for accessing files via platform APIs like Microsoft Graph)
        *   `Nucleus.Infrastructure.Ai.Google` (for Google Gemini integration via `Microsoft.Extensions.AI`)
    *   **Key Dependencies**: `Nucleus.Shared.Kernel`, relevant Azure SDKs or third-party client libraries.
    *   **Key Dependents**: MCP Tools, Agent applications, Backend services.

### 3.2. Application Layers

*   **`Nucleus.Agent.<PersonaName>`**: (e.g., `Nucleus.Agent.EduFlow`, `Nucleus.Agent.Helpdesk`)
    *   **Purpose**: Represents a specific M365 Persona Agent Application. This project contains the entry point for the agent (e.g., Teams Bot), platform-specific adapter logic (receiving events, sending messages), user interaction handling (`HandleInteractionAsync`), and orchestration of calls to MCP Tools or backend services. It adapts platform events into Nucleus concepts and vice-versa.
    *   **Namespace**: `Nucleus.Agent.<PersonaName>.*`
    *   **Examples**: `Nucleus.Agent.EduFlow.Bot`, `Nucleus.Agent.Helpdesk.TeamsAdapter`
    *   **Key Dependencies**: `Nucleus.Shared.Kernel`, `Nucleus.Mcp.Client` (for MCP Tool interaction), relevant platform SDKs (e.g., Bot Framework).
    *   **Key Dependents**: None (these are top-level applications).

*   **`Nucleus.McpTool.<ToolName>`**: (e.g., `Nucleus.McpTool.KnowledgeStore`, `Nucleus.McpTool.FileAccess`, `Nucleus.McpTool.ContentProcessing`)
    *   **Purpose**: Represents a Backend Nucleus MCP (Model Context Protocol) Tool/Server Application. Each tool is a distinct, deployable service (e.g., an ASP.NET Core Web API or a gRPC service) that exposes specific functionalities as defined by its MCP interface. These tools are the workhorses that perform discrete tasks like storing knowledge, accessing files, processing content, etc.
    *   **Namespace**: `Nucleus.McpTool.<ToolName>.*`
    *   **Examples**: `Nucleus.McpTool.KnowledgeStore.Api`, `Nucleus.McpTool.FileAccess.Service`
    *   **Key Dependencies**: `Nucleus.Shared.Kernel`, `Nucleus.Domain.RAGLogic` (if applicable, e.g., for ContentProcessing), `Nucleus.Infrastructure.*` (for its specific needs, e.g., KnowledgeStore uses CosmosDb).
    *   **Key Dependents**: `Nucleus.Mcp.Client`, potentially other MCP Tools if inter-tool communication is required (though direct dependencies should be minimized in favor of orchestration by Agents or a dedicated orchestrator service if complexity grows).

*   **`Nucleus.Mcp.Client`**:
    *   **Purpose**: A client library for Agent applications (and potentially other consumers) to interact with the various `Nucleus.McpTool.<ToolName>` services. It provides typed clients or proxies for calling MCP Tool APIs/gRPC methods, abstracting the direct HTTP/gRPC communication.
    *   **Namespace**: `Nucleus.Mcp.Client.*`
    *   **Examples**: `Nucleus.Mcp.Client.KnowledgeStore`, `Nucleus.Mcp.Client.FileAccess`
    *   **Key Dependencies**: `Nucleus.Shared.Kernel` (for shared models), HTTP client libraries, gRPC client libraries.
    *   **Key Dependents**: `Nucleus.Agent.<PersonaName>` applications.

*   **`Nucleus.AppHost`**:
    *   **Purpose**: The .NET Aspire application host project. Orchestrates the development and deployment of various Nucleus services (Agents, MCP Tools, Background Workers).
    *   **Namespace**: `Nucleus.AppHost`
    *   **Key Dependencies**: .NET Aspire SDK, references to all orchestrable service projects.
    *   **Key Dependents**: None (it's the orchestrator).

*   **`Nucleus.BackgroundWorker.ServiceBus`**:
    *   **Purpose**: A background worker service that processes messages from Azure Service Bus. This is used for decoupling long-running tasks, handling events asynchronously, and improving system resilience. For example, an Agent might publish an event to a queue, and this worker would pick it up to trigger an MCP Tool for content processing.
    *   **Namespace**: `Nucleus.BackgroundWorker.ServiceBus.*`
    *   **Key Dependencies**: `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.ServiceBus`, `Nucleus.Mcp.Client` (if it needs to call MCP tools after processing a message).
    *   **Key Dependents**: None (it's a top-level application/service).

### 3.3. Documentation Substructure (`Docs/Architecture/`)

The `Docs/Architecture/` folder is further organized to mirror the system's components and concerns:

*   **`Docs/Architecture/Agents/`**: Documentation specific to Persona Agents, their interaction models, and platform adaptations.
    *   `./<PersonaName>/` (e.g., `EduFlow/`) for persona-specific details.
*   **`Docs/Architecture/CoreNucleus/`**: Foundational architectural principles, technology choices, and cross-cutting concerns.
*   **`Docs/Architecture/Deployment/`**: Deployment models, infrastructure considerations, and hosting options.
*   **`Docs/Architecture/DevelopmentLifecycle/`**: Standards for development, testing, CI/CD, etc. (this set of documents).
*   **`Docs/Architecture/McpTools/`**: Overview of MCP Tools and detailed architecture for each tool.
    *   `./<ToolName>/` (e.g., `KnowledgeStore/`) for tool-specific details.
*   **`Docs/Architecture/Security/`**: Security architecture, data governance, and compliance.
*   **`Docs/Architecture/Archive_OldArchitecture/`**: Older architectural documents preserved for historical context.

## 4. Project Breakdown and Responsibilities (Summary Table)

| Project Name                          | Path (`src/`)                     | Namespace Prefix                | Purpose                                                                                                | Key Dependencies (Examples)                                                                 | Key Dependents (Examples)                                                                |
| :------------------------------------ | :-------------------------------- | :------------------------------ | :----------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------ | :--------------------------------------------------------------------------------------- |
| **Core Shared**                       |                                   |                                 |                                                                                                        |                                                                                             |                                                                                          |
| `Nucleus.Shared.Kernel`               | `Nucleus.Shared.Kernel/`          | `Nucleus.Shared.Kernel`         | Core abstractions, models (`IPersona`, `ArtifactMetadata`), `Result` types. Minimal dependencies.      | .NET BCL                                                                                    | Almost all other Nucleus projects                                                        |
| **Domain Logic**                      |                                   |                                 |                                                                                                        |                                                                                             |                                                                                          |
| `Nucleus.Domain.RAGLogic`             | `Nucleus.Domain.RAGLogic/`        | `Nucleus.Domain.RAGLogic`       | RAG pipeline, content processing, analysis, knowledge extraction logic.                                | `Nucleus.Shared.Kernel`                                                                     | `Nucleus.McpTool.ContentProcessing`, `Nucleus.McpTool.RAGPipeline`                       |
| **Infrastructure**                    |                                   |                                 |                                                                                                        |                                                                                             |                                                                                          |
| `Nucleus.Infrastructure.CosmosDb`     | `Nucleus.Infrastructure.CosmosDb/`  | `Nucleus.Infrastructure.CosmosDb` | Azure Cosmos DB persistence implementation.                                                            | `Nucleus.Shared.Kernel`, Azure Cosmos DB SDK                                                | `Nucleus.McpTool.KnowledgeStore`                                                         |
| `Nucleus.Infrastructure.ServiceBus`   | `Nucleus.Infrastructure.ServiceBus/`| `Nucleus.Infrastructure.ServiceBus`| Azure Service Bus messaging implementation.                                                          | `Nucleus.Shared.Kernel`, Azure Service Bus SDK                                              | `Nucleus.BackgroundWorker.ServiceBus`, Agents/MCP Tools (for publishing)                 |
| `Nucleus.Infrastructure.FileProviders.Platform` | `Nucleus.Infrastructure.FileProviders.Platform/` | `Nucleus.Infrastructure.FileProviders.Platform` | Accessing files via platform APIs (e.g., MS Graph).                                        | `Nucleus.Shared.Kernel`, MS Graph SDK                                                       | `Nucleus.McpTool.FileAccess`                                                             |
| `Nucleus.Infrastructure.Ai.Google`    | `Nucleus.Infrastructure.Ai.Google/` | `Nucleus.Infrastructure.Ai.Google`| Google Gemini integration via `Microsoft.Extensions.AI`.                                               | `Nucleus.Shared.Kernel`, `Microsoft.Extensions.AI`                                          | `Nucleus.Domain.RAGLogic`, `Nucleus.McpTool.LlmOrchestration`                            |
| **Applications & Services**           |                                   |                                 |                                                                                                        |                                                                                             |                                                                                          |
| `Nucleus.Agent.EduFlow`               | `Nucleus.Agent.EduFlow/`          | `Nucleus.Agent.EduFlow`         | EduFlow M365 Persona Agent application.                                                                | `Nucleus.Shared.Kernel`, `Nucleus.Mcp.Client`, Bot Framework SDK                            | None (top-level app)                                                                     |
| `Nucleus.Agent.Helpdesk`              | `Nucleus.Agent.Helpdesk/`         | `Nucleus.Agent.Helpdesk`        | Helpdesk M365 Persona Agent application.                                                               | `Nucleus.Shared.Kernel`, `Nucleus.Mcp.Client`, Bot Framework SDK                            | None (top-level app)                                                                     |
| `Nucleus.McpTool.KnowledgeStore`      | `Nucleus.McpTool.KnowledgeStore/` | `Nucleus.McpTool.KnowledgeStore`| MCP Tool for knowledge storage and retrieval.                                                          | `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.CosmosDb`                                  | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.McpTool.FileAccess`          | `Nucleus.McpTool.FileAccess/`     | `Nucleus.McpTool.FileAccess`    | MCP Tool for abstracting file access from various sources.                                             | `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.FileProviders.Platform`                    | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.McpTool.ContentProcessing`   | `Nucleus.McpTool.ContentProcessing/`| `Nucleus.McpTool.ContentProcessing`| MCP Tool for content extraction, chunking (intelligent, not blind), and initial processing.        | `Nucleus.Shared.Kernel`, `Nucleus.Domain.RAGLogic`                                          | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.McpTool.RAGPipeline`         | `Nucleus.McpTool.RAGPipeline/`    | `Nucleus.McpTool.RAGPipeline`   | MCP Tool orchestrating the RAG pipeline (retrieval, synthesis).                                        | `Nucleus.Shared.Kernel`, `Nucleus.Domain.RAGLogic`, `Nucleus.Mcp.Client` (for other tools) | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.McpTool.LlmOrchestration`    | `Nucleus.McpTool.LlmOrchestration/`| `Nucleus.McpTool.LlmOrchestration`| MCP Tool for managing interactions with LLMs (prompt engineering, response parsing).                 | `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.Ai.Google`                                 | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.McpTool.PersonaBehaviourConfig`| `Nucleus.McpTool.PersonaBehaviourConfig/`| `Nucleus.McpTool.PersonaBehaviourConfig`| MCP Tool for managing persona-specific configurations, prompts, and behaviors.                   | `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.CosmosDb` (potentially)                    | `Nucleus.Mcp.Client`                                                                     |
| `Nucleus.Mcp.Client`                  | `Nucleus.Mcp.Client/`             | `Nucleus.Mcp.Client`            | Client library for Agents to communicate with MCP Tools.                                               | `Nucleus.Shared.Kernel`                                                                     | `Nucleus.Agent.*` applications                                                           |
| `Nucleus.AppHost`                     | `Nucleus.AppHost/`                | `Nucleus.AppHost`               | .NET Aspire application host for orchestrating services.                                               | .NET Aspire SDK, All service projects                                                       | None (orchestrator)                                                                      |
| `Nucleus.BackgroundWorker.ServiceBus` | `Nucleus.BackgroundWorker.ServiceBus/` | `Nucleus.BackgroundWorker.ServiceBus` | Background worker processing Azure Service Bus messages.                                               | `Nucleus.Shared.Kernel`, `Nucleus.Infrastructure.ServiceBus`, `Nucleus.Mcp.Client`        | None (top-level service)                                                                 |

## 5. `tests/` Layer Structure and Namespaces

Test projects should mirror the structure and naming of the `src/` projects they are testing.

*   **Convention**: `Nucleus.<Layer>.<Component>.Tests.<TestType>`
    *   `<TestType>` can be `Unit`, `Integration`, or `EndToEnd` (though EndToEnd might be structured differently, potentially at a higher level).

*   **Examples**:
    *   `tests/Nucleus.Shared.Kernel.Tests.Unit/`
        *   Namespace: `Nucleus.Shared.Kernel.Tests.Unit`
    *   `tests/Nucleus.Domain.RAGLogic.Tests.Unit/`
        *   Namespace: `Nucleus.Domain.RAGLogic.Tests.Unit`
    *   `tests/Nucleus.McpTool.KnowledgeStore.Tests.Integration/`
        *   Namespace: `Nucleus.McpTool.KnowledgeStore.Tests.Integration`
    *   `tests/Nucleus.Agent.EduFlow.Tests.Integration/`
        *   Namespace: `Nucleus.Agent.EduFlow.Tests.Integration`

### Test Project Restructure:

Test projects will be organized to clearly distinguish between unit and integration tests and to align with the refactored `src/` structure.

*   **Unit Tests**: Focus on individual classes and methods in isolation.
    *   `Nucleus.Shared.Kernel.Tests.Unit`
    *   `Nucleus.Domain.RAGLogic.Tests.Unit`
    *   `Nucleus.Infrastructure.CosmosDb.Tests.Unit` (testing logic within the provider, not Cosmos DB itself)
    *   `Nucleus.McpTool.KnowledgeStore.Tests.Unit` (testing API controllers or service logic in isolation)
    *   `Nucleus.Agent.EduFlow.Tests.Unit` (testing bot logic or adapter mapping in isolation)
*   **Integration Tests**: Focus on interactions between components or with external services (mocked or real, e.g., in-memory databases, Testcontainers).
    *   `Nucleus.McpTool.KnowledgeStore.Tests.Integration` (testing API against an in-memory store or Testcontainer for Cosmos DB)
    *   `Nucleus.Agent.EduFlow.Tests.Integration` (testing the agent's interaction with a mocked MCP Client or platform)
    *   `Nucleus.AppHost.Tests.Integration` (testing service discovery and basic inter-service communication orchestrated by Aspire, if feasible and valuable).

## 6. Folder Naming Conventions

*   Use **PascalCase** for folder names that correspond to namespaces or major components (e.g., `SharedKernel/`, `DomainRAGLogic/`, `McpToolKnowledgeStore/`).
*   Use **kebab-case** for general-purpose folders that do not directly map to a primary namespace component (e.g., `build-scripts/`, `docs-assets/`). This is less common within `src/` and `tests/`.

## 7. Dependency Rules (The Acyclic Dependencies Principle)

Dependencies should generally flow in one direction:

`Applications (Agents, MCP Tools, Workers)` -> `Domain Logic (RAGLogic)` -> `Shared Kernel`
`Applications/Domain Logic` -> `Infrastructure Abstractions (defined in Domain or Kernel)`
`Infrastructure Providers` -> `Shared Kernel` (and implement abstractions)

*   **`Nucleus.Shared.Kernel`** should have minimal to no dependencies on other Nucleus projects. It is the foundation.
*   **`Nucleus.Domain.RAGLogic`** depends on `Nucleus.Shared.Kernel`. It should not depend on `Infrastructure` directly but can depend on abstractions defined in `Kernel` or within `Domain.RAGLogic` itself that `Infrastructure` projects then implement.
*   **`Nucleus.Infrastructure.<Provider>`** projects depend on `Nucleus.Shared.Kernel` and implement interfaces defined in `Kernel` or `Domain.RAGLogic`. They should not depend on `Application` layer projects or other `Infrastructure` projects directly (unless one infrastructure component genuinely builds upon another, e.g., a specialized cache on top of a generic storage provider).
*   **Application Layer Projects** (`Nucleus.Agent.*`, `Nucleus.McpTool.*`, `Nucleus.BackgroundWorker.*`) depend on `Nucleus.Shared.Kernel`, `Nucleus.Domain.RAGLogic` (if they need its specific logic), and relevant `Nucleus.Infrastructure.<Provider>` projects (or the `Nucleus.Mcp.Client`). They should not be depended upon by lower-level layers like `Domain` or `Kernel`.
*   **`Nucleus.Mcp.Client`** depends on `Nucleus.Shared.Kernel` (for shared request/response models). It does not depend on the server-side `Nucleus.McpTool.*` projects themselves but rather knows how to call their exposed endpoints.
*   Circular dependencies between projects are strictly prohibited.

## 8. Visualizing Dependencies (Conceptual)

A dependency graph would show `Nucleus.Shared.Kernel` at the center or bottom, with layers radiating outwards. `Nucleus.AppHost` would sit at the top, orchestrating the runnable applications.

```mermaid
graph TD
    subgraph Applications
        AppHost[Nucleus.AppHost]
        AgentEduFlow[Nucleus.Agent.EduFlow]
        AgentHelpdesk[Nucleus.Agent.Helpdesk]
        McpKnowledgeStore[Nucleus.McpTool.KnowledgeStore]
        McpFileAccess[Nucleus.McpTool.FileAccess]
        McpContentProcessing[Nucleus.McpTool.ContentProcessing]
        McpRAGPipeline[Nucleus.McpTool.RAGPipeline]
        McpLlmOrchestration[Nucleus.McpTool.LlmOrchestration]
        McpPersonaConfig[Nucleus.McpTool.PersonaBehaviourConfig]
        BackgroundWorker[Nucleus.BackgroundWorker.ServiceBus]
        McpClient[Nucleus.Mcp.Client]
    end

    subgraph Domain
        DomainRAG[Nucleus.Domain.RAGLogic]
    end

    subgraph Infrastructure
        InfraCosmos[Nucleus.Infrastructure.CosmosDb]
        InfraServiceBus[Nucleus.Infrastructure.ServiceBus]
        InfraFileProviders[Nucleus.Infrastructure.FileProviders.Platform]
        InfraAiGoogle[Nucleus.Infrastructure.Ai.Google]
    end

    subgraph Shared
        Kernel[Nucleus.Shared.Kernel]
    end

    AppHost --> AgentEduFlow
    AppHost --> AgentHelpdesk
    AppHost --> McpKnowledgeStore
    AppHost --> McpFileAccess
    AppHost --> McpContentProcessing
    AppHost --> McpRAGPipeline
    AppHost --> McpLlmOrchestration
    AppHost --> McpPersonaConfig
    AppHost --> BackgroundWorker

    AgentEduFlow --> McpClient
    AgentHelpdesk --> McpClient
    BackgroundWorker --> McpClient

    McpClient --> Kernel % For shared models with MCP Tools

    McpKnowledgeStore --> InfraCosmos
    McpKnowledgeStore --> Kernel
    McpFileAccess --> InfraFileProviders
    McpFileAccess --> Kernel
    McpContentProcessing --> DomainRAG
    McpContentProcessing --> Kernel
    McpRAGPipeline --> DomainRAG
    McpRAGPipeline --> McpClient % Potentially, to call other tools like KnowledgeStore
    McpRAGPipeline --> Kernel
    McpLlmOrchestration --> InfraAiGoogle
    McpLlmOrchestration --> Kernel
    McpPersonaConfig --> InfraCosmos % Example, could be other storage
    McpPersonaConfig --> Kernel

    BackgroundWorker --> InfraServiceBus
    BackgroundWorker --> Kernel

    DomainRAG --> Kernel

    InfraCosmos --> Kernel
    InfraServiceBus --> Kernel
    InfraFileProviders --> Kernel
    InfraAiGoogle --> Kernel

    %% Ensuring all top-level apps also depend on Kernel if not shown via another lib
    AgentEduFlow --> Kernel
    AgentHelpdesk --> Kernel
```

This diagram illustrates the primary dependency flows. The `Nucleus.Mcp.Client` acts as an intermediary for Agents to consume MCP Tools, promoting decoupling.

## 9. File Naming Conventions

*   Use **PascalCase** for C# file names (e.g., `MyClass.cs`, `IMyInterface.cs`).
*   Prefix interfaces with `I` (e.g., `IPersonaService.cs`).
*   Configuration files (e.g., `appsettings.json`, `.editorconfig`) should follow their standard naming conventions.
*   Documentation files (Markdown) should use **UPPER_SNAKE_CASE** for primary architecture documents (e.g., `01_NAMESPACES_FOLDERS.md`) or **PascalCase** for more general documentation if appropriate.

## 10. Related Documents

This document is part of a larger set of architectural and development lifecycle guidelines. For a complete understanding, refer to:

*   **Core Architectural Pillars:**
    *   [Nucleus System Architecture Comprehensive Guide](../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md) <!-- Corrected link -->
    *   [Technology Primer](../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md) <!-- Corrected link -->
*   **Development Lifecycle Overviews:**
    *   [Development Lifecycle Overview](./00_DEVELOPMENT_LIFECYCLE_OVERVIEW.md)
    *   [Testing Strategy](./02_TESTING_STRATEGY.md)
    *   [CI/CD Strategy](./03_CICD_STRATEGY.md) <!-- Corrected link and title -->
