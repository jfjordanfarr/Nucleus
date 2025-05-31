---
title: "MCP Tool: LlmOrchestration Server Architecture"
description: "Detailed architecture for the Nucleus_LlmOrchestration_McpServer, providing a centralized and configurable interface for M365 Agents and other MCP Tools to interact with various Large Language Models (LLMs)."
version: 1.0
date: 2025-05-28
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
  - title: "AI Integration Strategy"
    link: "../../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md"
  - title: "Core Abstractions, DTOs, and Interfaces"
    link: "../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md"
  - title: "Persona Configuration Schema"
    link: "../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md"
  - title: "MCP Tools Overview"
    link: ../01_MCP_TOOLS_OVERVIEW.md
  - title: "Comprehensive System Architecture"
    link: "../../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md" # Corrected
  - title: "Foundations: MCP and M365 Agents SDK Primer" # New
    link: "../../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md"
  - title: "M365 Agents Overview" # New
    link: "../../Agents/01_M365_AGENTS_OVERVIEW.md"
---

# MCP Tool: LlmOrchestration Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide a standardized, configurable, and resilient gateway for Nucleus M365 Persona Agents (and other authorized MCP Tools) to interact with various configured Large Language Models (LLMs).
*   **Capabilities Encapsulated:**
    *   Abstraction of `Microsoft.Extensions.AI.IChatClient` and `Microsoft.Extensions.AI.ITextEmbeddingGenerator` implementations for different LLM providers (e.g., Azure OpenAI, Google Gemini, OpenRouter.AI).
    *   Secure management and resolution of LLM-specific configurations (API keys from Azure Key Vault, endpoints, model IDs from Azure App Configuration).
    *   Facilitating streaming responses from LLMs back to the calling agent/tool.
    *   Potentially handling common pre/post-processing of LLM requests/responses if not managed by the caller.
    *   Supporting advanced interaction patterns like parallel requests for "Cognitive Forking" by M365 Agents, including passing through prompt caching hints to underlying LLM providers where feasible.
*   **Contribution to M365 Agents:** Simplifies LLM interaction by providing a consistent MCP interface. Allows for centralized management of LLM provider details, model selection strategies (e.g., routing to fast vs. powerful models), and common interaction patterns. Enables M365 Agents to be configured to use different LLMs without changing their core logic for calling this MCP tool.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `LlmOrchestration.GenerateChatCompletion`
*   **Description:** Sends a prompt (chat history) to a configured LLM and returns the completion. This is the primary interaction point for M365 Agents needing generative text.
*   **Input (DTO):** `ChatCompletionRequest { string TenantId, string PersonaId, List<ChatMessage> Messages, LlmRequestSettings Settings, bool StreamResponse }`.
    *   `ChatMessage`: `{ string Role, string Content }`.
    *   `LlmRequestSettings`: `{ string? ModelIdOverride, double? Temperature, int? MaxTokens, List<string>? StopSequences, string? ProviderHint, string? PromptCacheIdHint, bool? UsePromptCache }`.
*   **Output (DTO):** `ChatCompletionResponse { ChatMessage Completion, UsageInformation? Usage, string? ErrorMessage }`. (Streamed if `StreamResponse` is true).
    *   If `StreamResponse` is true, the `Completion` content will be streamed back to the caller via MCP's streaming mechanism. `UsageInformation` might be sent as a final message in the stream or as part of a non-streamed final confirmation.
*   **Idempotency:** Generally not idempotent, as LLM responses can vary.

### 2.2. `LlmOrchestration.GenerateEmbeddings`
*   **Description:** Generates text embeddings using a configured embedding model. This provides a centralized service for agents or other MCP tools that might need to generate embeddings on the fly.
*   **Input (DTO):** `EmbeddingRequest { string TenantId, string PersonaId, List<string> TextsToEmbed, LlmRequestSettings Settings }`.
    *   `LlmRequestSettings`: `{ string? ModelIdOverride, string? ProviderHint }`.
*   **Output (DTO):** `EmbeddingResponse { List<EmbeddingVector> Embeddings, UsageInformation? Usage, string? ErrorMessage }`.
    *   `EmbeddingVector`: `{ List<float> Vector, int Dimensions }`.
*   **Idempotency:** Generally idempotent for the same text and model.

### 2.3. (Optional) `LlmOrchestration.GetConfiguredModels`
*   **Description:** Returns a list of available/configured LLM models that this server can route to, potentially filtered by `TenantId` and/or `PersonaId` if tenant/persona-specific configurations exist.
*   **Input (DTO):** `GetModelsRequest { string TenantId, string? PersonaId }`.
*   **Output (DTO):** `GetModelsResponse { List<ConfiguredModelInfo> Models, string? ErrorMessage }`.
    *   `ConfiguredModelInfo`: `{ string ModelId, string ProviderName, List<string> SupportedTaskTypes (e.g., "Chat", "Embedding"), Dictionary<string, object>? Capabilities (e.g., "MaxInputTokens": 1000000, "SupportsPromptCaching": true) }`.

## 3. Core Internal Logic & Components

*   **`IChatClient` / `ITextEmbeddingGenerator` Resolution:**
    *   Uses Dependency Injection (DI) to access a collection of named (or keyed) `IChatClient` and `ITextEmbeddingGenerator` instances, registered during startup (e.g., "AzureOpenAIChat", "GeminiChat", "AzureOpenAIEmbedding"). These instances are typically provided by `Microsoft.Extensions.AI` and specific provider packages.
    *   Selects the appropriate client instance based on `LlmRequestSettings.ProviderHint`, `LlmRequestSettings.ModelIdOverride`, or default configurations retrieved from Azure App Configuration (potentially scoped by `TenantId` and/or `PersonaId`).
    *   This selection logic allows dynamic routing to different LLM providers or specific model deployments based on configuration or request parameters.
*   **LLM Interaction Logic:**
    *   Constructs the provider-specific request objects (e.g., `ChatRequestParameters` for `IChatClient`) using the `Messages` and `Settings` from the MCP request DTO.
    *   Handles streaming responses by iterating over the `IAsyncEnumerable<string>` (or similar streaming type like `IAsyncEnumerable<StreamingChatCompletionsUpdate>`) returned by the `IChatClient` and pushing chunks via the MCP streaming mechanism to the caller.
    *   Manages prompt caching hints or session IDs for providers that support them (e.g., Gemini), passing these through from the `LlmRequestSettings`.
    *   Parses responses from the LLM clients and maps them to the MCP output DTOs (`ChatCompletionResponse`, `EmbeddingResponse`), including any usage information provided by the LLM.
*   **Configuration Management:**
    *   Securely loads LLM API keys from Azure Key Vault.
    *   Retrieves LLM endpoints, model IDs, routing rules, default model preferences per tenant/persona, and feature flags from Azure App Configuration.
*   **Error Handling & Resilience:**
    *   Catches exceptions from `IChatClient` or `ITextEmbeddingGenerator` calls (e.g., API errors, rate limits, timeouts, authentication issues, content filtering flags).
    *   Translates these provider-specific errors into standardized MCP error responses within the output DTOs, providing meaningful error messages to the calling agent.
    *   Implements resilience patterns (e.g., using Polly policies for retries, circuit breakers) for calls to external LLM services to improve robustness.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault: For secure storage and retrieval of LLM API keys and other sensitive configuration values.
    *   Azure App Configuration: For managing non-sensitive configurations, routing rules, default model preferences, feature flags, and LLM endpoint details.
*   **External Services/LLMs:**
    *   Directly depends on the APIs of the configured LLM providers (e.g., Azure OpenAI Service, Google Gemini API, OpenRouter.AI, or any other provider integrated via `Microsoft.Extensions.AI`).
*   **Other Nucleus MCP Tools:**
    *   Generally, this tool does not have strong dependencies on other Nucleus MCP tools. It acts as a foundational service consumed by M365 Persona Agents or potentially other MCP tools if they require direct LLM interaction.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions` (or a similar shared DTO project): For core DTOs like `ChatMessage`, `LlmRequestSettings`, `UsageInformation`, `EmbeddingVector`, `ConfiguredModelInfo`, etc.
    *   `Microsoft.Extensions.AI`: Heavily relies on this for `IChatClient`, `ITextEmbeddingGenerator`, and their concrete implementations for various providers (e.g., `Microsoft.Extensions.AI.AzureOpenAI`, and potentially community or custom wrappers for other providers like Gemini or OpenRouter if not directly supported by Microsoft libraries yet).
    *   `Polly`: For implementing resilience policies (retries, circuit breakers) for external LLM calls.

## 5. Security Model

*   **Authentication of Callers:**
    *   All incoming MCP requests must be authenticated by validating Azure AD tokens issued for the calling M365 Persona Agent or authorized service principal (e.g., another MCP Tool).
    *   The server validates the token's signature, audience (must match this MCP tool's App ID URI), issuer, and ensures the token has not expired.
*   **Authorization within the Tool:**
    *   The `TenantId` and `PersonaId` claims from the validated token (or passed in the request DTO, cross-verified with token claims) are used to scope configuration lookups from Azure App Configuration (e.g., which LLMs or models a tenant/persona is permitted to use, default model preferences, feature flags).
    *   Application roles defined in this MCP tool's App Registration and assigned to the calling agent's service principal can be used for more granular access control (e.g., a role like "PremiumLlmAccess" allowing access to more powerful/expensive models, or a role like "EmbeddingGenerationAccess").
*   **Authentication to Dependencies:**
    *   **Azure Key Vault & Azure App Configuration:** Uses its Managed Identity (when deployed in Azure services like Azure Container Apps or Azure App Service) to securely access these services.
    *   **LLM Providers:**
        *   **Azure OpenAI Service:** Can use its Managed Identity for authentication if the Azure OpenAI resource is configured to allow it (preferred method).
        *   **Google Gemini API, OpenRouter.AI, etc.:** Typically uses API keys. These keys are securely stored as secrets in Azure Key Vault and retrieved by this MCP tool's Managed Identity at runtime. The keys are then used in the HTTP requests to the respective LLM APIs. Under no circumstances should API keys be hardcoded or stored in App Configuration directly.

## 6. Data Handling & Persistence

*   This MCP tool is fundamentally **stateless** regarding user-provided prompt/completion content.
*   It orchestrates calls to external LLM services and does not persist any of the request content (prompts, messages) or response content (completions, embeddings) itself beyond the lifetime of a request.
*   **Logging:** Critical for audit, troubleshooting, monitoring, and cost tracking.
    *   Logs should include: Request metadata (calling agent ID/App ID, `TenantId`, `PersonaId`, requested model/provider, timestamp, request ID for correlation).
    *   Response metadata (actual model/provider used, tokens consumed for prompt and completion, duration, success/failure status, error codes if any, stream vs. non-stream).
    *   **PII/Sensitive Data:** Extreme care must be taken to **avoid logging the actual content of prompts or LLM completions by default**. If such logging is required for specific diagnostic scenarios, it must be explicitly enabled, auditable, and adhere to strict data handling, retention, and access control policies. Focus on logging metadata and anonymized/aggregated metrics.
*   No direct database persistence of user content is performed by this tool.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Typically deployed as an Azure Container App or Azure App Service, allowing for robust scalability, managed identity, and integration with other Azure services like Azure Monitor for logging and metrics.
*   **Key Configurations (managed via Azure App Configuration and Key Vault):**
    *   **Provider-Specific Settings:** For each supported LLM provider (e.g., Azure OpenAI, Gemini, OpenRouter):
        *   API Key Secret Name (referencing a secret in Key Vault for providers requiring keys).
        *   Service Endpoint URL.
        *   Default Model IDs for chat and embeddings (can be overridden by request).
        *   API Version (if applicable for the provider).
    *   **Routing & Default Preferences:**
        *   Default `LlmRequestSettings` (e.g., default temperature, max tokens) if not specified in the request.
        *   Rules or mappings for selecting LLM providers/models based on `TenantId`, `PersonaId`, or `ProviderHint` from the request.
    *   **Resilience Policies (Polly):** Configuration for retry attempts, backoff strategies, circuit breaker thresholds, and timeout durations when calling external LLM APIs.
    *   **Feature Flags:** For enabling/disabling specific LLM providers, models, or features (e.g., prompt caching experiments).
    *   **Logging Configuration:** Log levels, destinations (e.g., Azure Application Insights).
    *   **Streaming Configuration:** Buffer sizes, timeout settings for streaming operations.

## 8. Future Considerations / Potential Enhancements

*   **Wider LLM provider support:** Continuously expand support for new LLM providers and models as they become available and are integrated into `Microsoft.Extensions.AI` or through community/custom wrappers.
*   **Advanced Prompt Templating/Management:** While M365 Agents might use Semantic Kernel or other templating engines, this tool could offer centralized storage and rendering of pre-approved prompt templates for common, sensitive, or highly optimized interactions.
*   **Built-in Request/Response Caching:** Implement configurable caching (e.g., using Azure Cache for Redis) for identical, non-sensitive requests to reduce latency, cost, and load on LLM providers. Cache keys would need to be carefully designed.
*   **More Sophisticated Load Balancing/Routing:** Implement intelligent routing across multiple LLM instances, regions, or even providers based on real-time health, load, cost, performance metrics, or specific task requirements (e.g., routing summarization to a cheaper model vs. complex reasoning to a more powerful one).
*   **Centralized Token Usage Tracking & Quota Management:** Implement robust tracking of token consumption per `TenantId`/`PersonaId`/model, potentially enforcing pre-configured quotas or rate limits to manage costs and prevent abuse.
*   **Dynamic Model Selection:** Introduce logic to dynamically select the most appropriate model for a given task based on its complexity, required capabilities (e.g., context window size), cost constraints, or even user preferences, potentially using a smaller LLM to classify the request first.
*   **Support for Function Calling/Tool Use Abstraction:** If LLMs support function calling, this tool could provide a standardized way for agents to declare available functions and for the orchestrator to manage the function calling loop with the LLM.
*   **Enhanced Observability:** Deeper integration with Azure Monitor for detailed metrics on request latency, token usage per model/provider, error rates, and costs, enabling better operational insights and cost management.
