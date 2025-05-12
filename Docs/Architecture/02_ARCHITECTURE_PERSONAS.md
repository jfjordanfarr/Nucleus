---
title: Architecture - Personas & Multi-Platform Interaction
description: Details the architecture for Personas, including multi-platform identities, interaction patterns, and persona-to-persona communication, all within the API-First model.
version: 2.4
date: 2025-05-06
---

# Nucleus: Persona Architecture

**Version:** 2.4
**Date:** 2025-05-06

This document details the architecture for implementing specialized AI assistants, referred to as "Personas" or "Verticals," within the Nucleus platform, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It covers their core concept, structure, configuration, and crucially, how they operate as **agentic entities** coordinated via the `Nucleus.Services.Api`, leveraging **ephemeral content retrieval** based on **artifact references** executed by a central **Persona Runtime/Engine** based on configuration.

*   **Related Architecture:**
    *   [Overall System Architecture](./00_ARCHITECTURE_OVERVIEW.md)
    *   [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md)
    *   [Security Architecture](./06_ARCHITECTURE_SECURITY.md)
    *   [Client Architecture](./05_ARCHITECTURE_CLIENTS.md) (and specific adapters in `../ClientAdapters/`)
    *   [Processing Orchestration](./Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)
    *   [Database](./04_ARCHITECTURE_DATABASE.md)

## 1. Core Concept: Personas as Specialized Agents

Personas are distinct, configurable AI agents designed to address specific domains or user needs (e.g., education, business knowledge, personal finance). They encapsulate domain-specific logic, **agentic reasoning capabilities (operating through iterative, multi-step processing loops)**, analysis methods, and interaction patterns, **all defined via configuration and executed by the Persona Runtime/Engine**. They leverage the core platform's infrastructure for **secure metadata indexing (querying sanitized, derived knowledge stored in the database)**, processing (see [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md)), **ephemeral artifact content retrieval (fetching full content transiently from user storage via `IArtifactProvider` when needed)** using `ArtifactReference` (adhering to **strict security principles like Zero Trust for user content**), storage of derived knowledge ([Storage Architecture](./03_ARCHITECTURE_STORAGE.md)), and retrieval from the [Database](./04_ARCHITECTURE_DATABASE.md).

*   **Key Data Structure:** Persona-generated insights are stored as [`PersonaKnowledgeEntry`](../../../Nucleus.Abstractions/Models/PersonaKnowledgeEntry.cs) records within dedicated Cosmos DB containers (see [Database](./04_ARCHITECTURE_DATABASE.md)). Crucially, the `AnalysisData` property of this record uses `System.Text.Json.JsonElement?`, allowing flexible, configuration-defined JSON structures instead of fixed C# types. The interpretation of this `JsonElement` is handled by the specific `IAgenticStrategyHandler` configured for the Persona.

A key design principle is that a Persona exists as an abstraction *above* specific communication platforms. The same "Professional Colleague" persona, with its unique knowledge and capabilities, should be accessible via Teams, Email, Slack, etc., if configured.

## 2. Conceptual Responsibilities (Handled by Runtime)

While previously embodied in an `IPersona` interface (now deprecated, see Section 11.5), the core *functions* performed by the Persona Runtime, guided by configuration, include:

*   **Identification:** Using the `PersonaId` from the configuration to log and potentially tailor behavior.
*   **Contextual Interpretation:** Understanding the user's query and the provided `InteractionContext`.
*   **Agentic Reasoning & Planning:** Based on the configured `AgenticStrategy`, determining the steps needed to fulfill the request. This might involve:
    *   Querying the **Secure Metadata Index** (`ArtifactMetadata`, `PersonaKnowledgeEntry` in the configured `TargetKnowledgeContainerId`) to identify relevant artifacts/knowledge.
    *   Triggering **ephemeral content retrieval** for artifacts via `IArtifactProvider` using `ArtifactReference` to build **rich ephemeral context** when deeper analysis is required.
    *   Potentially planning and executing **Tools** if configured.
*   **LLM Interaction:** Communicating with the configured AI service (`IChatClient`) using the appropriate `SystemMessage` and context.
*   **Knowledge Generation/Persistence:** Optionally generating structured insights (e.g., `PersonaKnowledgeEntry`) and requesting their persistence via repositories.
*   **Response Formulation:** Crafting the final response according to configured `ResponseGuidelines`.

{{ ... sections 3-10 unchanged ... }}

## 11. Configuration-Driven Model

The architecture for Personas has been refactored to reflect a configuration-driven model executed by a generic Persona Runtime/Engine, rather than distinct IPersona implementations for each persona type.

### 11.1 Overview

Instead of coding unique C# classes for each persona's core logic, personas are now primarily defined by **configuration data**. This configuration specifies the persona's identity, behavior, capabilities, prompts, and how it should interact with the system's resources.

A central **Persona Runtime/Engine** component reads this configuration and executes the necessary steps to handle a user interaction for the specified persona.

**Benefits:**
*   **Flexibility:** New personas or variations can be defined largely through configuration changes, reducing the need for recompilation and deployment.
*   **Maintainability:** Core agentic logic (e.g., RAG process, tool execution flow) is centralized in the Runtime, making updates easier.
*   **Consistency:** Ensures all personas adhere to the same core processing patterns and security protocols enforced by the Runtime.

### 11.2 Persona Runtime/Engine

The **Persona Runtime/Engine** is a central component, implemented as one or more C# services within the **`Nucleus.Personas.Core`** project. It is responsible for:

1.  **Loading Configuration:** Retrieving and parsing a specific `PersonaConfiguration` based on the `PersonaId` determined during [Routing](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md).
2.  **Executing Agentic Loop:** Orchestrating the interaction lifecycle based on the loaded configuration's parameters (e.g., prompts, knowledge scope, agentic strategy, enabled tools).
3.  **Interfacing with Services:** Communicating with necessary services like `IChatClient`, `IArtifactMetadataRepository`, `IPersonaKnowledgeRepository<T>`, and triggering content retrieval via the orchestrator.

This engine acts as the executor for the behavior defined *in* the configuration.

### 11.3 Persona Configuration

Defines *what* a persona is and *how* it should behave. Key elements are outlined in detail in [Persona Configuration](./Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md) and include:

*   **Core Identification:** `PersonaId`, `DisplayName`, `Description`
*   **Operational Settings:** `ShowYourWork`
*   **Activation Rules:** `ActivationTriggers`, `ContextScope`
*   **Capability Settings:**
    *   `LlmConfiguration`: Provider, Models, Parameters
    *   `EnabledTools`: List of allowed tools
    *   `KnowledgeScope`: Strategy, Collections, `TargetKnowledgeContainerId`, Limits (Note: The `TargetKnowledgeContainerId` points to the Cosmos DB container holding `PersonaKnowledgeEntry` records with `JsonElement` analysis data for this persona)
*   **Prompt Configuration:** `SystemMessage`, `ResponseGuidelines`
*   **Agentic Strategy Configuration:** `StrategyType`, `MaxIterations`
*   **Custom Properties:** Flexible key-value pairs for persona-specific data (e.g., `PedagogicalTreeRef` for Educator)

### 11.4 Core Responsibilities (Executed by Runtime based on Config)

{{ ... }}

### 11.5 Specific Persona Configurations

While the core logic resides in the generic Runtime, specific behaviors are defined via configurations detailed in linked documents:

*   **[Bootstrapper](./Personas/ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md):** Configuration for basic interaction, fallback, and testing.
*   **[Educator](./Personas/ARCHITECTURE_PERSONAS_EDUCATOR.md):** Configuration focused on analyzing educational artifacts and tracking learning progress.
*   **[Professional Colleague](./Personas/ARCHITECTURE_PERSONAS_PROFESSIONAL.md):** Configuration for assisting with workplace tasks, information retrieval, potentially integrating with enterprise tools.
    *   Example Deployment: [Azure .NET IT Helpdesk](./Personas/Professional/ARCHITECTURE_AZURE_DOTNET_HELPDESK.md)

*(Note: The previously mentioned `IPersona` interface and specific implementations like `BootstrapperPersona.cs`, `EducatorPersona.cs`, `ProfessionalPersona.cs` are now considered **deprecated** in favor of this configuration-driven Runtime model. Code references should be updated accordingly in future refactoring.)*

### 11.6 Key Design Considerations

*   **Statelessness:** The Persona Runtime should aim to be stateless, relying on the `InteractionContext` and retrieved data for processing each request. State, if needed, should be managed externally (e.g., in a cache or database) or passed explicitly.
*   **Configuration Schema:** A well-defined and versioned schema for `PersonaConfiguration` is crucial for stability and extensibility.
*   **Runtime Extensibility:** The Runtime might need mechanisms to load custom logic or tools referenced in configurations, potentially via dependency injection or plugins.
*   **Security:** All interactions involving content retrieval and AI processing must strictly adhere to the [Security Architecture](./06_ARCHITECTURE_SECURITY.md).

### 11.7 Next Steps

1.  **(DONE)** ~~Finalize & Implement `PersonaConfiguration` Schema: Ensure the C# class matches the documentation and integrate it into the configuration loading mechanism.~~
2.  **Implement Persona Runtime/Engine:** Develop the core service(s) in `Nucleus.Personas.Core` responsible for loading configurations and executing the agentic loop based on the defined strategies (`SimpleRag`, `MultiStepReasoning`, `ToolUsing`).
3.  **Refactor Existing Code:** Adapt `OrchestrationService` and related components to invoke the Runtime with the appropriate `PersonaConfiguration` instead of resolving and calling specific `IPersona` implementations.
4.  **(DONE)** ~~Update Specific Persona Docs: Modify `_BOOTSTRAPPER`, `_EDUCATOR`, `_PROFESSIONAL` docs to reflect their status as configurations, removing references to direct C# implementation of core logic.~~
5.  **Define Configuration Storage/Loading:** Decide how configurations will be stored (e.g., `appsettings.json`, database) and implement the loading mechanism for the Runtime/API Service.
6.  **Implement Agentic Strategies:** Build out the logic within the Runtime to handle the different `StrategyType` options.
