---
title: "Architecture - Persona Configuration (for M365 Agents & MCP Tools)"
description: "Defines the structure and settings for configuring Nucleus M365 Persona Agents, including multi-LLM provider support and dynamic/behavioral configurations."
version: 2.1
date: 2025-05-25
parent: ./01_PERSONA_CONCEPTS.md
---

# Persona Configuration (for M365 Agents & MCP Tools)

## 1. Overview (Revised for M365 Agents & MCP)

This document outlines the structure and parameters used to configure individual **Nucleus M365 Persona Agents**. This configuration is encapsulated in the [`PersonaConfiguration.cs`](../../../src/Nucleus.Abstractions/Models/Configuration/PersonaConfiguration.cs) POCO and is loaded by the `IPersonaRuntime` operating *within* each M365 Persona Agent application.

The configuration defines a Persona Agent's behavior, capabilities (including which backend **Nucleus MCP Tools** it can call), LLM provider choices (Azure OpenAI, Google Gemini, OpenRouter.AI, etc.), operational settings, and core prompts/strategies.

**Configuration Management (Hybrid Model - New):**
Nucleus M365 Persona Agents will adopt a hybrid configuration management strategy:
1.  **Foundational/Static Configuration:** Core operational parameters, LLM API keys (for the agent's direct use), critical MCP Tool endpoints, and default model IDs will be loaded from **Azure App Configuration and Azure Key Vault** (via Key Vault references), managed by .NET Aspire during deployment and accessed by the M365 Agent using its Managed Identity.
2.  **Dynamic/Behavioral Configuration:** Persona-specific system prompts, response guidelines, adaptive parameters, and tenant/user customizations will be stored in **Azure Cosmos DB** and accessed/updated by the M365 Agent via a dedicated **`Nucleus_PersonaBehaviourConfig_McpServer`** (an MCP Tool). This allows for runtime adaptability and potentially agent self-improvement based on feedback.

The `IPersonaConfigurationProvider` interface will be responsible for abstracting the loading of this merged configuration.

## 2. Configuration Schema (`PersonaConfiguration.cs` - Key Sections Revised)

Each Persona configuration includes the following key properties.

### 2.1 Core Identification

*   `PersonaId`: (String, Required) A unique identifier for this specific persona configuration (e.g., `EduFlowOmniEducator`, `HelpdeskITProfessional`). This ID is intrinsic to the deployed M365 Persona Agent application itself.
*   `DisplayName`: (String, Required) A user-friendly name for the persona.
*   `Description`: (String, Optional) A brief description of the persona's purpose.

### 2.2 Operational Settings

*   **`ShowYourWork`**: (Boolean, Optional, Default: `true`)
    *   If `true`, the M365 Agent's `IPersonaRuntime` processing will generate an internal reasoning/planning artifact.
    *   This artifact is included in the M365 Agent's response payload (e.g., an `Activity` attachment).
    *   The **Nucleus M365 Persona Agent** is responsible for saving this artifact to user-controlled M365 storage (e.g., `.Nucleus/Personas/{PersonaId}/ShowYourWork/` in SharePoint/OneDrive via Microsoft Graph API, using its Entra Agent ID permissions) for auditability.
    *   Referenced in: [Interaction Lifecycle](./../Processing/02_ARCHITECTURE_PROCESSING_INTERACTION_LIFECYCLE.md) <!-- Adjusted link to be relative to CoreNucleus, assuming a Processing folder exists at the same level as CoreNucleus -->

### 2.3 Activation Rules (Revised Context)

*   **Context:** Activation of a specific Nucleus M365 Persona Agent is now primarily determined by the user interacting with that specific registered agent on an M365 platform (e.g., `@EduFlowAgent` in Teams).
*   `ActivationTriggers`: (Array<String>, Optional) **[REVISED ROLE]** May now define specific commands, keywords, or `Activity` types (within messages directed *to this M365 Agent*) that trigger particular `IAgenticStrategyHandler`s or sub-flows *within this Persona Agent*.
*   `ContextScope`: (Object, Optional) **[REVISED ROLE]** May define specific M365 contexts (e.g., specific Teams channel IDs, M365 Group IDs) where *this M365 Persona Agent* has specialized behaviors or access rights, enforced by its internal logic and its Entra Agent ID permissions.

### 2.4 Capability Settings (Revised for Multi-LLM & MCP)

*   **`LlmConfiguration`**: (Object, Required)
    *   `Provider`: (String, Enum, Required) AI Service provider. **Values now include `GoogleGemini`, `AzureOpenAI`, `OpenRouterAI`, `CustomIChatClient`.** This allows selection of the LLM backend.
    *   `ChatModelId`: (String, Required) Identifier for the primary chat/reasoning model (e.g., `gemini-2.5-pro`, `gpt-4o`, a model name from OpenRouter).
    *   `EmbeddingModelId`: (String, Required) Identifier for the text embedding model (e.g., `text-embedding-004`, `text-embedding-3-large`).
    *   `ApiKeySecretName`: (String, Optional) **[NEW]** Name of the secret in Azure Key Vault holding the API key for the selected `Provider` (e.g., "GoogleAiApiKey", "OpenRouterApiKey"). If `Provider` is `AzureOpenAI` and Managed Identity is used for auth, this might be omitted.
    *   `EndpointUrl`: (String, Optional) **[NEW]** Specific endpoint URL if not default for the provider (e.g., for OpenRouter or a self-hosted model).
    *   `Temperature`: (Float, Optional) Sampling temperature for generation.
    *   `MaxOutputTokens`: (Integer, Optional) Maximum tokens for the LLM response.
*   **`EnabledTools`**: (Array<String>, Optional) **[REVISED ROLE]** List of **Nucleus backend MCP Tool IDs or fully qualified MCP tool names** that this M365 Persona Agent's `IPersonaRuntime` is permitted to call (e.g., `["Nucleus.KnowledgeStore.Search", "Nucleus.FileAccess.GetContent"]`).
*   **`KnowledgeScope`**: (Object, Required)
    *   `Strategy`: (String, Enum, Required) Defines how the Persona Agent (via MCP calls to `Nucleus_KnowledgeStore_McpServer` or `Nucleus_RAGPipeline_McpServer`) accesses user knowledge.
    *   `CollectionIds`: (Array<String>, Optional) Used when `Strategy` is `SpecificCollectionIds`.
    *   `TargetKnowledgeContainerId`: (String, Optional) Specifies the ID of a dedicated [Persona Knowledge Container](./03_DATA_PERSISTENCE_STRATEGY.md#4-persona-knowledge-container-schema) (e.g., `EduFlow_v1KnowledgeContainer`) to be queried by the `Nucleus_KnowledgeStore_McpServer`.
    *   `MaxContextDocuments`: (Integer, Optional, Default: 10) Maximum number of full documents whose content will be ephemerally fetched (via `Nucleus_FileAccess_McpServer`) based on metadata search results, to form the context for the M365 Agent's LLM.

### 2.5 Prompt Configuration (Dynamic Sourcing from DB)

*   Defines the core instructional prompts used by the M365 Agent's `IPersonaRuntime` when interacting with its configured LLM.
*   **These prompts are now prime candidates for being part of the dynamic/behavioral configuration stored in Cosmos DB and accessed via `Nucleus_PersonaBehaviourConfig_McpServer`.**
*   **`SystemMessageKey`**: (String, Required) **[NEW]** A key to look up the persona's primary system prompt from the dynamic configuration store.
*   **`ResponseGuidelinesKey`**: (String, Optional) **[NEW]** A key to look up additional response guidelines.
*   *(Fallback prompts can be defined in static config if DB access fails or for bootstrapping).*

### 2.6 Agentic Strategy Configuration

**[CONTENT LARGELY KEPT - Strategy types are still relevant for `IPersonaRuntime`]**
*   Defines the high-level execution strategy the `IPersonaRuntime` (within the M365 Agent) should employ.
*   **`StrategyType`**: (String, Enum, Required, Default: `SimpleRag`)
    *   `SimpleRag`: Basic MCP calls: KnowledgeStore (Search) -> FileAccess (GetContent if needed) -> LLM (Synthesize).
    *   `MultiStepReasoning`: Allows for internal planning within the M365 Agent, potentially multiple LLM calls or a sequence of MCP tool calls.
    *   `ToolUsing`: Enables the `IPersonaRuntime` to explicitly plan and execute configured `EnabledTools` (backend Nucleus MCP Tools) as part of the interaction. This heavily involves the LLM's function/tool calling capabilities.
*   **`MaxIterations`**: (Integer, Optional, Default: 1) Used by `MultiStepReasoning` or `ToolUsing` strategies to limit execution loops.
*   *(Future): May include strategy-specific parameters (e.g., planning prompts for MultiStep).* 

### 2.7 Custom Properties (Dynamic Sourcing from DB)

**[CONTENT LARGELY KEPT - Still useful, now potentially dynamic]**
*   **`CustomPropertiesKeys`**: (Array<String>, Optional) **[NEW]** Keys to look up sets of custom properties from the dynamic configuration store (Cosmos DB via `Nucleus_PersonaBehaviourConfig_McpServer`).
*   The `IPersonaRuntime` uses these to guide persona-specific behaviors.

## 3. Configuration Management (Revised Hybrid Model)

*   **Nucleus M365 Persona Agent applications** load their `PersonaConfiguration` using `IPersonaConfigurationProvider`.
*   This provider implements a **hybrid loading strategy**:
    1.  **Foundational/Static Parts:** Reads from Azure App Configuration + Key Vault (e.g., `PersonaId`, `DisplayName`, default `LlmConfiguration` including `ApiKeySecretName` and `EndpointUrl`, `EnabledTools` list, fallback prompts). These are typically set at deployment.
    2.  **Dynamic/Behavioral Parts:** Makes an MCP call to `Nucleus_PersonaBehaviourConfig_McpServer` to fetch tenant-specific or globally updated prompts, response guidelines, and custom properties from Cosmos DB, using keys specified in the static configuration (e.g., `SystemMessageKey`).
*   The final, effective `PersonaConfiguration` used by `IPersonaRuntime` is a merged view of these static and dynamic settings.
