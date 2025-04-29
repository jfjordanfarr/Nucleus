---
title: Architecture - Persona Configuration
description: Defines the structure and available settings for configuring Nucleus Personas for execution by the Persona Runtime.
version: 1.3
date: 2025-04-29
parent: ../02_ARCHITECTURE_PERSONAS.md
---

# Persona Configuration

## 1. Overview

This document outlines the structure and parameters used to configure individual Nucleus Personas. Configuration defines a Persona's behavior, capabilities, activation triggers, operational settings, and core prompts/strategies, shaping how the generic [Persona Runtime/Engine](../02_ARCHITECTURE_PERSONAS.md#22-persona-runtimeengine) executes interactions within the [Processing Orchestration](../Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md) framework.

Configuration is managed server-side (loaded by the API service) and defines the runtime characteristics used by the Persona Runtime when processing a request associated with a specific `PersonaId`.

## 2. Configuration Schema (Conceptual)

Each Persona configuration includes the following key properties. While a corresponding C# class like `PersonaConfiguration.cs` exists, this documentation defines the authoritative structure required for the Runtime.

### 2.1 Core Identification

*   `PersonaId`: (String, Required) A unique identifier for this specific persona configuration (e.g., `Bootstrapper`, `Educator_Grade5`, `Helpdesk_IT`). This ID is used by the [Orchestration Router](../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md) to select the correct configuration for the Persona Runtime to load.
*   `DisplayName`: (String, Required) A user-friendly name for the persona.
*   `Description`: (String, Optional) A brief description of the persona's purpose and capabilities.

### 2.2 Operational Settings

*   **`ShowYourWork`**: (Boolean, Optional, Default: `true`)
    *   If `true`, the Persona Runtime's interaction processing will generate an internal reasoning/planning artifact.
    *   This artifact is included in the API response to the calling Client Adapter.
    *   The **Client Adapter** is responsible for saving this artifact to user-controlled storage (e.g., `.Nucleus/Personas/{PersonaId}/ShowYourWork/` via Teams/Graph, or local directory for Console Adapter) for auditability.
    *   Referenced in: [Interaction Lifecycle](../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md#conditional-artifact-generation-show-your-work)

### 2.3 Activation Rules

*   Defines how and when a specific `PersonaId` (and thus its configuration) is selected by the [Orchestration Router](../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md).
*   `ActivationTriggers`: (Array<String>, Optional) Specifies events that can activate the persona (e.g., `["@mention", "DirectMessage", "SpecificKeyword"]`).
*   `ContextScope`: (Object, Optional) Defines the context (e.g., channels, users) where the persona is active. Structure depends on the Client Adapter platform (e.g., `TeamsChannelIds`, `UserIds`).

### 2.4 Capability Settings

*   **`LlmConfiguration`**: (Object, Required)
    *   `Provider`: (String, Enum, Required) AI Service provider (e.g., `GoogleGemini`, `AzureOpenAI`).
    *   `ChatModelId`: (String, Required) Identifier for the primary chat/reasoning model (e.g., `gemini-1.5-pro-latest`).
    *   `EmbeddingModelId`: (String, Required) Identifier for the text embedding model (e.g., `text-embedding-004`).
    *   `Temperature`: (Float, Optional) Sampling temperature for generation.
    *   `MaxOutputTokens`: (Integer, Optional) Maximum tokens for the LLM response.
*   **`EnabledTools`**: (Array<String>, Optional) List of tool IDs or names the Persona Runtime is permitted to use when executing this configuration. (Requires a separate tool registration/definition mechanism and Runtime support for tool execution).
*   **`KnowledgeScope`**: (Object, Required)
    *   `Strategy`: (String, Enum, Required) Defines how the persona accesses user knowledge (e.g., `AllUserArtifacts`, `SpecificCollectionIds`, `MetadataOnly`, `NoUserArtifacts`).
    *   `CollectionIds`: (Array<String>, Optional) Used when `Strategy` is `SpecificCollectionIds`.
    *   `TargetKnowledgeContainerId`: (String, Optional) Specifies the ID of a dedicated [Persona Knowledge Container](../04_ARCHITECTURE_DATABASE.md#4-personaidknowledgecontainer-schema) to query for `PersonaKnowledgeEntry` records (e.g., `EduFlow_v1KnowledgeContainer`). If null/empty, only `ArtifactMetadata` is searched.
    *   `MaxContextDocuments`: (Integer, Optional, Default: 10) Maximum number of full documents to retrieve ephemerally based on metadata search to form the context window (See: [Rich Ephemeral Context Handling](../06_ARCHITECTURE_SECURITY.md#6-ai--prompt-security)).

### 2.5 Prompt Configuration

*   Defines the core instructional prompts used by the Persona Runtime when interacting with the configured LLM.
*   **`SystemMessage`**: (String, Required) The primary instruction or meta-prompt defining the persona's role, tone, capabilities, constraints, and how it should process the user query and context.
*   **`ResponseGuidelines`**: (String, Optional) Additional guidelines for formatting or structuring the final response (e.g., "Always cite sources using markdown links.", "Use bullet points for lists.").
*   *(Future): May include specific templates for different interaction phases (e.g., summarization prompt, analysis prompt).* 

### 2.6 Agentic Strategy Configuration

*   Defines the high-level execution strategy the Persona Runtime should employ.
*   **`StrategyType`**: (String, Enum, Required, Default: `SimpleRag`)
    *   `SimpleRag`: Basic Retrieve-Augment-Generate. Performs metadata search, retrieves full content if needed, synthesizes response.
    *   `MultiStepReasoning`: Allows for internal planning and potentially multiple LLM calls or retrieval steps to answer complex queries. (Requires sophisticated Runtime implementation).
    *   `ToolUsing`: Enables the Runtime to plan and execute configured `EnabledTools` as part of the interaction. (Requires sophisticated Runtime implementation).
*   **`MaxIterations`**: (Integer, Optional, Default: 1) Used by `MultiStepReasoning` or `ToolUsing` strategies to limit execution loops.
*   *(Future): May include strategy-specific parameters (e.g., planning prompts for MultiStep).* 

### 2.7 Custom Properties

*   **`CustomProperties`**: (Dictionary<String, String>, Optional)
    *   A flexible key-value store for persona-specific configuration not covered by the standard schema.
    *   Examples:
        *   `Educator_Grade5`: `{"PedagogicalTreeRef": "trees/pedagogical_v1.json", "TautologicalTreeRef": "trees/tautological_v1.json"}`
        *   `Helpdesk_IT`: `{"TroubleshootingGuideUrl": "https://kb.example.com/ts_guide"}`
    *   The Persona Runtime implementation needs to be aware of how to interpret and use these properties based on the `PersonaId` or other configuration flags.

## 3. Configuration Management

*   Persona configurations are managed server-side as part of the system's deployment.
*   They are loaded by the central API service (or directly by the Persona Runtime via a configuration provider) during startup or on-demand based on the requested `PersonaId`.
*   Storage mechanism: configuration files (`appsettings.json`), dedicated database, environment variables, Azure App Configuration, etc. The specific provider is registered in the dependency injection container (see `WebApplicationBuilderExtensions.AddNucleusServices`).
    *   **Current Implementation (Development/Testing):** An [`InMemoryPersonaConfigurationProvider`](../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Configuration/InMemoryPersonaConfigurationProvider.cs) is currently registered, providing hardcoded configurations.
    *   **Production:** A production environment would typically use a provider leveraging the `.NET IConfiguration` system, reading from `appsettings.json`, environment variables, or external sources like Azure App Configuration.
*   The system might support inheritance or layering, but the effective configuration used by the Runtime for a given `PersonaId` is a single, resolved set of parameters.
