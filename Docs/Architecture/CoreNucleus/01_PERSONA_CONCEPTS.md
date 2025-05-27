---
title: "Architecture - Nucleus Personas (Embodied by M365 Agents)"
description: "Details the architecture for Nucleus Personas, now implemented as Microsoft 365 Agent applications, driven by PersonaConfiguration and IPersonaRuntime, and interacting with backend capabilities via MCP."
version: 3.1
date: 2025-05-25
---

# Nucleus: Persona Architecture (Embodied by M365 Agents)

**Version:** 3.1
**Date:** 2025-05-25

This document details the architecture for implementing specialized AI assistants, referred to as "Personas" (e.g., `EduFlowOmniEducator`, `Professional_IT_Helpdesk`), within the Nucleus platform. This architecture is updated to reflect the strategic pivot to the **Microsoft 365 Agents SDK** and **Model Context Protocol (MCP)**. Personas are now embodied as distinct **Nucleus M365 Persona Agent applications**.

*   **Related Architecture:**
    *   [Overall System Architecture](./00_ARCHITECTURE_OVERVIEW.md) <!-- Assuming this is a sibling, if it's CoreNucleus/00_... then it should be ./00_FOUNDATIONS_TECHNOLOGY_PRIMER.md or similar -->
    *   [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md) <!-- Assuming this is a sibling, if it's CoreNucleus/01_... then it should be ./CoreNucleus/01_ARCHITECTURE_PROCESSING.md or similar -->
    *   [Security Architecture](./06_ARCHITECTURE_SECURITY.md) <!-- Assuming this is a sibling -->
    *   [Client & Agent Interaction Architecture](./05_ARCHITECTURE_CLIENTS.md) <!-- Assuming this is a sibling -->
    *   [Backend MCP Tools Overview](./McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md) **[NEW]**
    *   [Storage Architecture](./03_ARCHITECTURE_STORAGE.md) <!-- Assuming this is a sibling -->
    *   [Data Persistence Strategy](./03_DATA_PERSISTENCE_STRATEGY.md)

## 1. Core Concept: Personas as Specialized, Configurable M365 Agents

Nucleus Personas are distinct, configurable AI agents designed to address specific domains or user needs. They are now implemented as individual **Microsoft 365 Agent applications** (built with the .NET M365 Agents SDK). Each Nucleus M365 Persona Agent:

*   Encapsulates domain-specific logic, **agentic reasoning capabilities (operating through iterative, multi-step processing loops via `IPersonaRuntime` and `IAgenticStrategyHandler`)**, analysis methods, and interaction patterns.
*   Is driven entirely by its loaded **[`PersonaConfiguration`](../../../src/Nucleus.Abstractions/Models/Configuration/PersonaConfiguration.cs)** which defines its behavior, prompts, LLM settings (including support for Azure OpenAI, Google Gemini, OpenRouter.AI via `IChatClient`), and the backend Nucleus MCP Tools it can access.
*   Leverages backend **Nucleus MCP Tool/Server applications** for core platform capabilities:
    *   **Secure metadata indexing and knowledge retrieval:** Via MCP calls to a `Nucleus_KnowledgeStore_McpServer` (querying `ArtifactMetadata` and `PersonaKnowledgeEntry` from Cosmos DB).
    *   **Ephemeral artifact content retrieval:** Via MCP calls to a `Nucleus_FileAccess_McpServer` (which uses `IArtifactProvider` logic and the M365 Agent's Entra Agent ID context for permissions) using `ArtifactReference` objects.
    *   **Specialized content processing:** Potentially via MCP calls to a `Nucleus_ContentProcessing_McpServer`.
*   Adheres to strict security principles, including Zero Trust for user file content, with all file access being ephemeral and mediated through the `Nucleus_FileAccess_McpServer`.
*   Has its own **Microsoft Entra Agent ID** for secure authentication and authorization within the M365 ecosystem and to backend MCP Tools.

*   **Key Data Structure (Accessed via MCP Tool):** Persona-generated insights are stored as [`PersonaKnowledgeEntry`](../../../src/Nucleus.Abstractions/Models/PersonaKnowledgeEntry.cs) records within dedicated Cosmos DB containers (see [Data Persistence Strategy](./03_DATA_PERSISTENCE_STRATEGY.md)), managed by the `Nucleus_KnowledgeStore_McpServer`. The `AnalysisData` property (type `System.Text.Json.JsonElement?`) remains crucial for flexible, configuration-defined JSON structures.

The same underlying Nucleus Persona logic (driven by `IPersonaRuntime` and `PersonaConfiguration`) can be surfaced across any channel supported by the M365 Agents SDK, as each Persona is its own M365 Agent application.

## 2. Conceptual Responsibilities (of a Nucleus M365 Persona Agent)

The core functions performed by a **Nucleus M365 Persona Agent**, driven by its `IPersonaRuntime` executing a loaded `PersonaConfiguration`, include:

*   **Receiving & Interpreting User Interactions:** Handling `Activity` objects from the M365 Agents SDK, understanding user queries and context (including `tenantId`, `userId`).
*   **Agentic Reasoning & Planning (via `IPersonaRuntime` & `IAgenticStrategyHandler`):** Based on its `PersonaConfiguration` (prompts, `AgenticStrategyType`):
    *   Determining the steps needed to fulfill the request.
    *   Making MCP calls to `Nucleus_KnowledgeStore_McpServer` to query `ArtifactMetadata` and `PersonaKnowledgeEntry`.
    *   Making MCP calls to `Nucleus_FileAccess_McpServer` (with `ArtifactReference`s) to retrieve ephemeral full content of artifacts if needed.
    *   Making MCP calls to other specialized Nucleus MCP Tools or even external MCP tools (if configured and authorized).
    *   Interacting with its configured LLM (Azure OpenAI, Google Gemini, OpenRouter.AI via `IChatClient`) for analysis, synthesis, and response generation.
*   **Response Generation:** Crafting the final output as an `Activity` and sending it back to the user via the M365 Agents SDK.
*   **Knowledge Update (via MCP Tool):** Sending new insights or summaries (as `PersonaKnowledgeEntry` data) to the `Nucleus_KnowledgeStore_McpServer` for persistence.
*   **Asynchronous Task Offloading:** For long-running operations, enqueuing a task (with `ConversationReference`, `ArtifactReference`s, `tenantId`, `personaId`) to the `IBackgroundTaskQueue` (Azure Service Bus). The background worker will then orchestrate MCP tool calls and trigger a proactive response via this M365 Agent.

**(Note: The original sections 3-10, which likely covered topics such as specific persona types, detailed interaction patterns, or inter-persona communication, have been superseded by the M365 Agents SDK architecture. For instance, multi-platform identity is now managed by the M365 Agent's registration with Azure Bot Service. Inter-agent communication, if needed, would occur via MCP calls between M365 Agents (if they expose MCP tools) or through a broader orchestration framework like Semantic Kernel, should it be adopted.)**

## 3. Configuration-Driven Model (Central Role of `IPersonaRuntime` within M365 Agent)

**[CONTENT LARGELY KEPT & REINFORCED - The core idea of a config-driven runtime is now even more central, operating *within* each M365 Agent application.]**

The architecture for Personas remains fundamentally configuration-driven, executed by a generic **Persona Runtime engine** ([`IPersonaRuntime`](../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs), [`PersonaRuntime`](../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs)). This engine now operates *within each deployed Nucleus M365 Persona Agent application*.

### 3.1 Overview (Revised Context)

The `IPersonaRuntime` is a central component within each Nucleus M365 Persona Agent, responsible for bringing its specific `PersonaConfiguration` to life. It orchestrates the agent's turn. Key aspects:

*   **Configuration-Driven:** Its behavior is entirely dictated by the loaded [`PersonaConfiguration`](../../../src/Nucleus.Abstractions/Models/Configuration/PersonaConfiguration.cs) specific to that M365 Agent. This configuration is loaded via `IPersonaConfigurationProvider` (which may use Azure App Config/Key Vault for static parts and a `Nucleus_PersonaBehaviourConfig_McpServer` for dynamic/behavioral parts).
*   **Strategy Execution:** It selects and invokes the appropriate [`IAgenticStrategyHandler`](../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IAgenticStrategyHandler.cs) based on the `StrategyType` defined in the configuration.
*   **Lifecycle Management:** Manages the flow of an interaction turn received by the M365 Agent, from understanding the incoming `Activity` to generating the outgoing `Activity` response.
*   **MCP Tool Orchestration:** A key responsibility of the `IPersonaRuntime` (and its `IAgenticStrategyHandler`s) is to make appropriate MCP calls to backend Nucleus MCP Tool/Servers (`Nucleus_FileAccess_McpServer`, `Nucleus_KnowledgeStore_McpServer`, etc.) to fulfill its tasks.

### 3.2 Core Steps in Runtime Execution (within M365 Agent)

1.  **Loading Configuration:** The M365 Agent, upon receiving an interaction (or at startup for some elements), uses `IPersonaConfigurationProvider` to retrieve and parse its specific [`PersonaConfiguration`](../../../src/Nucleus.Abstractions/Models/Configuration/PersonaConfiguration.cs) (using its own `PersonaId` and the `tenantId` from the interaction).
2.  **Executing Agentic Loop (via `IAgenticStrategyHandler`):** Orchestrating the interaction lifecycle based on the loaded configuration's parameters (e.g., prompts, knowledge scope strategy, agentic strategy type, enabled backend MCP tools).
    *   This involves invoking the designated `IAgenticStrategyHandler`.
    *   The handler performs RAG (by making MCP calls to `Nucleus_RAGPipeline_McpServer` and `Nucleus_KnowledgeStore_McpServer`), uses configured LLMs (via `IChatClient` supporting Azure OpenAI, Gemini, OpenRouter), or follows a multi-step reasoning process, potentially calling various Nucleus MCP Tools.
3.  **Artifact Handling (via `Nucleus_FileAccess_McpServer`):** Managing `ArtifactReference` objects (derived from M365 SDK file info) and making MCP calls to the `Nucleus_FileAccess_McpServer` for ephemeral content retrieval when necessary and permitted by the `KnowledgeScopeStrategy` in its configuration.
4.  **Response Formatting (M365 SDK):** Ensuring the response is formatted as an `Activity` suitable for sending back via the Microsoft 365 Agents SDK.

### 3.3 `PersonaConfiguration` Overview

**[CONTENT KEPT - This POCO remains the blueprint for persona behavior]**

The [`PersonaConfiguration.cs`](../../../src/Nucleus.Abstractions/Models/Configuration/PersonaConfiguration.cs) object is the blueprint for a Nucleus M365 Persona Agent. See [Persona Configuration Schema](./02_PERSONA_CONFIGURATION_SCHEMA.md) for a full schema breakdown. Key sections include:
*   ... (Core Identification, Operational Settings, Activation Rules [now more about internal agent logic if applicable]) ...
*   **Capability Settings:**
    *   `LlmConfiguration`: Provider (AzureOpenAI, GoogleGemini, OpenRouterAI), Models, Parameters.
    *   `EnabledTools`: List of **Nucleus MCP Tool IDs** or names the `IPersonaRuntime` is permitted to call.
    *   `KnowledgeScope`: Strategy, Collections, `TargetKnowledgeContainerId` (for `Nucleus_KnowledgeStore_McpServer`).
*   ... (Prompt Configuration, Agentic Strategy Configuration, Custom Properties) ...

### 3.4 Core Responsibilities (Executed by `IPersonaRuntime` within M365 Agent, using MCP Tools)

**[CONTENT REVISED - Actions are now often MCP calls]**
...
2.  **Implement `IPersonaRuntime` and `IAgenticStrategyHandler`s:** These remain in `Nucleus.Domain.Personas.Core` but their implementations will now make MCP calls to backend Nucleus services.
3.  **Refactor `OrchestrationService` (Old API):** The old `OrchestrationService` is deprecated. Its relevant logic is partly absorbed by the M365 Agent's turn handling and partly by the `QueuedInteractionProcessorService` (which calls MCP tools).
4.  ...
5.  **Define Configuration Storage/Loading (Hybrid Model):**
    *   Azure App Configuration/Key Vault for foundational M365 Agent settings (LLM keys, MCP Tool endpoints).
    *   Cosmos DB (via `Nucleus_PersonaBehaviourConfig_McpServer`) for dynamic/behavioral `PersonaConfiguration` elements (prompts, etc.).
    *   Implement `IPersonaConfigurationProvider` to support this hybrid loading.
6.  **Implement Agentic Strategies:** Ensure these strategies within `IPersonaRuntime` are adapted to make MCP calls.
