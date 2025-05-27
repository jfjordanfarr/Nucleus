---
title: "Nucleus AI Integration Strategy"
description: "Defines how Large Language Models (LLMs) are integrated into Nucleus M365 Persona Agents and backend MCP Tools, leveraging Microsoft.Extensions.AI, .NET Aspire, and supporting multi-provider flexibility."
version: 1.0
date: 2025-05-26
---

# Nucleus AI Integration Strategy

**Parent:** [../00_SYSTEM_OVERVIEW.md](../00_SYSTEM_OVERVIEW.md)

## 1. Introduction

This document outlines the architectural approach for integrating various third-party AI models and services, primarily Large Language Models (LLMs), within the Nucleus ecosystem. The strategy centers on **Nucleus M365 Persona Agents** (hosted as services within a .NET Aspire application) and backend **Nucleus Model Context Protocol (MCP) Tool/Server applications** (also managed by .NET Aspire).

A core tenet of Nucleus is **AI provider flexibility**. While enterprise deployments might favor Azure OpenAI Service, other scenarios may benefit from Google Gemini or OpenRouter.AI. The primary mechanism for direct LLM interaction (chat completions, embeddings) within .NET components is the **`Microsoft.Extensions.AI`** abstraction layer.

.NET Aspire plays a crucial role in orchestrating the various services (Agents, MCP Tools) that consume these AI capabilities, managing their configuration and facilitating service discovery.

**Related Documents:**
*   [MCP Tool: LLM Orchestration](../../McpTools/LlmOrchestration/ARCHITECTURE_MCPTOOL_LLM_ORCHESTRATION.md) (if LLM calls are centralized)
*   [M365 Agents Overview](../../Agents/01_M365_AGENTS_OVERVIEW.md)

## 2. Core LLM Interaction: `Microsoft.Extensions.AI`

Nucleus leverages `Microsoft.Extensions.AI.IChatClient` for chat completions and `Microsoft.Extensions.AI.ITextEmbeddingGenerator` for generating text embeddings. These abstractions offer:

*   **Provider Agnosticism:** Enables switching between AI providers (Google Gemini, Azure OpenAI, etc.) with minimal code changes, primarily through configuration and dependency injection.
*   **Flexibility & Extensibility:** Supports middleware for logging, caching, telemetry, and standardized function/tool integration.
*   **Standardization:** Aligns with .NET best practices for AI integration.

**Primary Consumers (as Aspire-managed services):**

*   **Nucleus M365 Persona Agents:** For core reasoning, response generation, and orchestrating MCP tool calls based on LLM decisions.
*   **Nucleus MCP Tools:** Specific MCP tools might interact directly with LLMs:
    *   `KnowledgeStore_McpServer`: May use `ITextEmbeddingGenerator` to create embeddings for `PersonaKnowledgeEntry` records.
    *   `LlmOrchestration_McpServer` (Optional): If LLM interactions are centralized into a dedicated MCP tool, this server would be a heavy user of `IChatClient`.

## 3. Supported AI Providers & Configuration within .NET Aspire

Each Aspire-hosted service (Agent or MCP Tool) that requires direct LLM access will configure the desired `IChatClient` and `ITextEmbeddingGenerator` implementation in its `Program.cs`. This selection is driven by externalized configuration, managed by .NET Aspire, which can draw from Azure App Configuration and Azure Key Vault in production environments.

### 3.1. Azure OpenAI Service

*   **SDKs:** Official `Azure.AI.OpenAI` .NET SDK, which provides extensions for `Microsoft.Extensions.AI`.
*   **Authentication:** Managed Identity of the consuming Azure-hosted service (e.g., M365 Agent running in Azure App Service/Container Apps) is preferred.
*   **Configuration (via Aspire/App Config/Key Vault):** `AI:AzureOpenAI:Endpoint`, `AI:AzureOpenAI:ChatDeploymentName`, `AI:AzureOpenAI:EmbeddingDeploymentName`.

### 3.2. Google Gemini

*   **SDKs:** Official Google .NET SDK (e.g., `Google.Ai.Generativelanguage`) wrapped in a custom `IChatClient`/`ITextEmbeddingGenerator`, or community packages like `Mscc.GenerativeAI.Microsoft` if compatible.
*   **Configuration (via Aspire/App Config/Key Vault):** `AI:GoogleAI:ApiKey`, `AI:GoogleAI:ChatModel`, `AI:GoogleAI:EmbeddingModel`.

### 3.3. OpenRouter.AI

*   **Integration:** Requires a custom `IChatClient`/`ITextEmbeddingGenerator` implementation to handle HTTP calls, authentication (API key), and request/response mapping.
*   **Configuration (via Aspire/App Config/Key Vault):** `AI:OpenRouter:ApiKey`, `AI:OpenRouter:Endpoint`, specific model identifiers.

### 3.4. Dependency Injection Example (Conceptual)

Within an Aspire-managed service's `Program.cs`:

```csharp
// In Program.cs of an M365 Agent or MCP Tool service

// Configuration is loaded by Aspire (e.g., from appsettings.json, environment variables, Azure App Config)
var chosenProvider = builder.Configuration["AI:ProviderName"]; // e.g., "AzureOpenAI"

if (chosenProvider == "AzureOpenAI")
{
    builder.Services.AddAzureOpenAIChatCompletion(
        builder.Configuration.GetSection("AI:AzureOpenAI:Chat")
    );
    // Add AzureOpenAIEmbeddingGeneration similarly
}
else if (chosenProvider == "GoogleGemini")
{
    // Register custom or third-party Gemini IChatClient/ITextEmbeddingGenerator
    // services.AddSingleton<IChatClient, GeminiChatClient>(); 
}
// ... other providers

// Services then inject IChatClient or ITextEmbeddingGenerator
```

## 4. LLM Tool Calling of Backend Nucleus MCP Tools

This is a primary mode of AI integration, where the M365 Persona Agent's LLM invokes capabilities exposed by backend Nucleus MCP Tools. All involved services (Agent, MCP Tools) are orchestrated by .NET Aspire.

### 4.1. Mechanism

The M365 Persona Agent's LLM utilizes its native function/tool calling features.

### 4.2. Orchestration within the M365 Agent

The M365 Agent (an Aspire-hosted service) typically employs an orchestration framework like **Semantic Kernel**:

1.  **MCP Tool Definition:** Backend Nucleus MCP Tools (e.g., `KnowledgeStore_McpServer`) define their capabilities per MCP standards (name, description, JSON schema).
2.  **Tool Discovery & Registration:** The M365 Agent's Semantic Kernel instance connects to these MCP Tools. The endpoints for these tools are discoverable via .NET Aspire's service discovery mechanisms.
3.  Semantic Kernel represents these MCP Tools as `KernelFunction`s within its plugins.
4.  **LLM Interaction:** The Agent's LLM (invoked via `IChatClient`, often wrapped by Semantic Kernel's chat completion services) is made aware of these available `KernelFunction`s.
5.  **LLM Decision & Invocation:** Based on user intent, the LLM decides to call one or more MCP Tools and generates a structured function call request.
6.  **Translation to MCP Call:** Semantic Kernel (or custom agent logic) translates this request into a formal MCP call and dispatches it to the appropriate Nucleus MCP Tool service (its address resolved by Aspire).
7.  **MCP Tool Execution:** The target MCP Tool executes its logic.
8.  **Response to LLM:** The result is returned to Semantic Kernel/Agent, then to the LLM for formulating the final user response.

.NET Aspire facilitates this by managing the lifecycle, configuration, and network discoverability of both the M365 Agent and the various MCP Tool services.

## 5. Ephemeral Context Provisioning for LLMs

Nucleus adheres to a Zero Trust principle regarding persistent storage of raw user file content. Ephemeral context is provided to LLMs as follows:

1.  **Interaction:** M365 Agent receives an interaction (e.g., a message with a file shared in Teams).
2.  **MCP Call for Content:** Agent calls the `FileAccess_McpTool` (an Aspire-managed service) with an `ArtifactReference`.
3.  **Ephemeral Fetch:** The `FileAccess_McpTool` ephemerally retrieves the `ArtifactContent` (stream + metadata) from the user's M365 storage using appropriate permissions.
4.  **Content Routing:** The content stream is returned to the Agent or routed to another MCP Tool (e.g., `ContentProcessing_McpTool`, also an Aspire-managed service).
5.  **Prompt Assembly:** The M365 Agent (or a dedicated `LlmOrchestration_McpTool`) assembles the prompt for its configured LLM, including the ephemeral text content.
6.  **LLM Call:** The prompt is sent via the injected `IChatClient`.
7.  **Cleanup:** The ephemeral content stream is discarded after use.

## 6. Role of Semantic Kernel within M365 Persona Agents

Semantic Kernel is primarily utilized *within* each M365 Persona Agent service. Its role includes:

*   Managing interaction with the chosen LLM (via `IChatClient`).
*   Integrating backend Nucleus MCP Tools (as plugins/functions), whose endpoints are resolved by Aspire.
*   Handling complex prompt engineering, short-term conversational memory, and planning logic for the agent.

## 7. Future Considerations

*   **Provider-Specific Features:** Balancing the use of common abstractions (`IChatClient`) with the need to leverage unique strengths or mitigate limitations of specific LLM providers (e.g., context windows, rate limits, advanced tool calling nuances, prompt caching APIs).
*   **Advanced Configuration:** Supporting complex configuration scenarios, potentially allowing tenants in multi-tenant deployments or administrators in self-hosted setups to select and configure preferred LLM backends. .NET Aspire's configuration model will be key here.
*   **Prompt Caching:** Investigating and implementing strategies for prompt caching to optimize performance and reduce costs, potentially via a dedicated MCP Tool or middleware for `IChatClient`.
