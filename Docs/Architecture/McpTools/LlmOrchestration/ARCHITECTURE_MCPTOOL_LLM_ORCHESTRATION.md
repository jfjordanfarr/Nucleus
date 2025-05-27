---
title: "MCP Tool: LlmOrchestration Server Architecture"
description: "Detailed architecture for the Nucleus_LlmOrchestration_McpServer, providing a centralized interface for M365 Agents to interact with configured LLMs for chat completion, summarization, and analysis."
version: 1.0
date: 2025-05-27
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
  - title: "AI Integration Strategy"
    link: ../../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md
  - title: "Core Abstractions, DTOs, and Interfaces"
    link: ../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md
  - title: "Persona Configuration Schema"
    link: ../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md
  - title: "MCP Tools Overview"
    link: ../01_MCP_TOOLS_OVERVIEW.md
  - title: "Comprehensive System Architecture"
    link: ../../00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md
---

# MCP Tool: LlmOrchestration Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide a standardized, configurable, and potentially enhanced gateway for Nucleus M365 Persona Agents to interact with various Large Language Models (LLMs).
*   **Capabilities Encapsulated:**
    *   Abstraction of `Microsoft.Extensions.AI.IChatClient` implementations for different providers (e.g., Azure OpenAI, Google Gemini, OpenRouter.AI).
    *   Management of LLM-specific configurations (API keys, endpoints, model IDs) securely.
    *   Potentially, common prompt templating, context management, or basic pre/post-processing of LLM requests/responses if not handled by the agent directly or via Semantic Kernel within the agent.
    *   Facilitating streaming responses from LLMs back to the calling agent.
*   **Contribution to M365 Agents:** Simplifies LLM interaction for agents by providing a consistent MCP interface. Allows for centralized management and updating of LLM provider details and common interaction patterns. Enables agents to be configured to use different LLMs without changing their core logic for calling this MCP tool.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `LlmOrchestration.GenerateChatCompletion`
*   **Description:** Sends a prompt (chat history) to a configured LLM and returns the completion. This is the primary interaction point for M365 Agents needing generative text.
*   **Input (DTO):** `ChatCompletionRequest { string TenantId, string PersonaId, List<ChatMessage> Messages, LlmRequestSettings Settings (e.g., ModelIdOverride, Temperature, MaxTokens, StopSequences, ProviderHint), bool StreamResponse }`.
    *   `ChatMessage` is `{ string Role, string Content }`.
    *   `LlmRequestSettings` allows for fine-tuning the request to the LLM.
*   **Output (DTO):** `ChatCompletionResponse { ChatMessage Completion, UsageInformation? Usage, string? ErrorMessage }`.
    *   If `StreamResponse` is true, the `Completion` content will be streamed back to the caller via MCP's streaming mechanism. `UsageInformation` might be sent as a final message in the stream or as part of a non-streamed final confirmation.
*   **Idempotency:** Generally not idempotent, as LLM responses can vary.

### 2.2. `LlmOrchestration.GenerateEmbeddings`
*   **Description:** Generates text embeddings using a configured embedding model. This provides a centralized service for agents or other MCP tools that might need to generate embeddings on the fly.
*   **Input (DTO):** `EmbeddingRequest { string TenantId, List<string> TextsToEmbed, LlmRequestSettings Settings (e.g., ModelIdOverride, ProviderHint) }`.
*   **Output (DTO):** `EmbeddingResponse { List<float[]> Embeddings, UsageInformation? Usage, string? ErrorMessage }`.
*   **Idempotency:** Generally idempotent for the same text and model.

### 2.3. (Optional) `LlmOrchestration.GetConfiguredModels`
*   **Description:** Returns a list of available/configured LLM models that this server can route to, potentially filtered by `TenantId` if tenant-specific configurations exist.
*   **Input (DTO):** `GetModelsRequest { string TenantId }`.
*   **Output (DTO):** `GetModelsResponse { List<ConfiguredModelInfo> Models, string? ErrorMessage }`.
    *   `ConfiguredModelInfo` would include `ModelId`, `ProviderName`, `Description`, `SupportedTaskTypes (e.g., Chat, Embedding)`.

## 3. Core Internal Logic & Components

*   **`IChatClient` / `ITextEmbeddingGenerator` Resolution:**
    *   Maintains a collection of named `Microsoft.Extensions.AI.IChatClient` and `Microsoft.Extensions.AI.ITextEmbeddingGenerator` instances, registered during startup (e.g., "AzureOpenAIChat", "GeminiChat", "AzureOpenAIEmbedding").
    *   Based on the `LlmRequestSettings.ProviderHint` and/or `LlmRequestSettings.ModelIdOverride` from the incoming request, or default configurations associated with the `TenantId`/`PersonaId` (retrieved from App Configuration), it selects the appropriate client instance.
    *   This selection logic allows routing to different LLM providers or specific model deployments.
*   **LLM Interaction Logic:**
    *   Constructs the appropriate request object for the selected `IChatClient` (e.g., `ChatRequestParameters`) or `ITextEmbeddingGenerator` using the `Messages` and `Settings` from the MCP request DTO.
    *   Handles streaming responses by iterating over the `IAsyncEnumerable<string>` (or similar streaming type) returned by the `IChatClient` and pushing chunks via the MCP streaming mechanism.
    *   Parses responses from the LLM clients and maps them to the MCP output DTOs (`ChatCompletionResponse`, `EmbeddingResponse`).
*   **Configuration Management:**
    *   Securely loads LLM API keys, endpoints, model deployment names, and other provider-specific settings from Azure Key Vault.
    *   Retrieves routing rules, default model preferences per tenant/persona, and feature flags from Azure App Configuration.
*   **Error Handling:**
    *   Catches exceptions from `IChatClient` or `ITextEmbeddingGenerator` calls (e.g., API errors, rate limits, timeouts, authentication issues).
    *   Translates these provider-specific errors into standardized MCP error responses within the output DTOs, providing meaningful error messages to the calling agent.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault: For secure storage and retrieval of LLM API keys and other sensitive configuration values.
    *   Azure App Configuration: For managing non-sensitive configurations, routing rules, default model preferences, and feature flags.
*   **External Services/LLMs:**
    *   Directly depends on the APIs of the configured LLM providers (e.g., Azure OpenAI Service, Google Gemini API, OpenRouter.AI).
*   **Other Nucleus MCP Tools:**
    *   Generally, this tool does not have strong dependencies on other Nucleus MCP tools. It acts as a foundational service consumed by M365 Persona Agents or potentially other MCP tools if they require direct LLM interaction.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions`: For core DTOs (`ChatMessage`, `LlmRequestSettings`, `UsageInformation`, etc.) and potentially shared interfaces if not directly using `Microsoft.Extensions.AI`.
    *   `Microsoft.Extensions.AI`: Heavily relies on this for `IChatClient`, `ITextEmbeddingGenerator`, and their concrete implementations for various providers (e.g., `Microsoft.Extensions.AI.AzureOpenAI`, `Microsoft.Extensions.AI.Gemini` (if/when available), or custom wrappers for providers not directly supported).
    *   `Nucleus.Mcp.Client` (or similar): If it ever needed to call another MCP service, though this is not its primary role.

## 5. Security Model

*   **Authentication of Callers:**
    *   All incoming MCP requests must be authenticated by validating Azure AD tokens issued for the calling M365 Persona Agent or authorized service.
    *   The server validates the token's signature, audience (must match this MCP tool's App ID URI), and issuer.
*   **Authorization within the Tool:**
    *   The `TenantId` claim from the validated token is used to scope any tenant-specific configurations (e.g., which LLMs or models a tenant is permitted to use, default model preferences).
    *   Further authorization could be based on application roles defined in the MCP tool's App Registration and assigned to the calling agent's service principal (e.g., a role allowing access to more powerful/expensive models vs. standard models).
*   **Authentication to Dependencies:**
    *   **Azure Key Vault & Azure App Configuration:** Uses its Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access these services.
    *   **LLM Providers:**
        *   **Azure OpenAI Service:** Can use its Managed Identity for authentication if the Azure OpenAI resource is configured to allow it.
        *   **Google Gemini API, OpenRouter.AI, etc.:** Typically uses API keys. These keys are securely stored in Azure Key Vault and retrieved by the MCP tool's Managed Identity at runtime. The keys are then used in the HTTP requests to the respective LLM APIs.

## 6. Data Handling & Persistence (If Applicable)

*   This MCP tool is primarily **stateless**. It orchestrates calls to external LLM services and does not persist any of the request or response content itself.
*   **Logging:** Critical for audit, troubleshooting, and monitoring. Logs should include:
    *   Request metadata (calling agent ID, `TenantId`, `PersonaId`, requested model, timestamp).
    *   Response metadata (model used, tokens consumed, duration, success/failure status).
    *   **PII/Sensitive Data:** Care must be taken to avoid logging the actual content of prompts or LLM completions unless explicitly configured for diagnostic purposes with appropriate data handling policies in place. Focus on logging metadata.
*   No direct database persistence is performed by this tool.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Typically deployed as an Azure Container App, allowing for scalability and integration with other Azure services.
*   **Key Configurations (managed via Azure App Configuration and Key Vault):**
    *   **Provider-Specific Settings:** For each supported LLM provider (e.g., Azure OpenAI, Gemini):
        *   API Key Secret Name (referencing a secret in Key Vault).
        *   Service Endpoint URL.
        *   Default Model IDs for chat and embeddings.
        *   API Version (if applicable).
    *   **Routing & Default Preferences:**
        *   Default `LlmRequestSettings` (e.g., default temperature, max tokens).
        *   Rules for selecting LLM providers/models based on `TenantId`, `PersonaId`, or `ProviderHint`.
    *   **Resilience Policies:** Configuration for Polly policies (or similar) for retries, circuit breakers, and timeouts when calling external LLM APIs.
    *   **Feature Flags:** For enabling/disabling specific LLM providers or features.
    *   **Logging Configuration:** Log levels and destinations.

## 8. Future Considerations / Potential Enhancements

*   **Support for more LLM providers:** Easily extendable by adding new `IChatClient` / `ITextEmbeddingGenerator` implementations and their configurations.
*   **Advanced Prompt Templating/Management:** While agents might use Semantic Kernel, this tool could offer basic, centralized prompt templating for common use cases if desired.
*   **Built-in Caching for LLM Responses:** Configurable caching (e.g., Azure Cache for Redis) for identical requests to reduce latency and cost, especially for non-sensitive, frequently asked queries.
*   **More Sophisticated Load Balancing/Routing:** Intelligent routing across multiple LLM instances, regions, or even providers based on health, load, cost, or performance.
*   **Token Usage Tracking & Quota Management:** Centralized tracking of token consumption per tenant/persona, potentially enforcing quotas.
*   **Integration with `Microsoft.SemanticKernel.PromptTemplateEngine`:** Could expose an MCP operation to render prompts using Semantic Kernel templates stored centrally.
