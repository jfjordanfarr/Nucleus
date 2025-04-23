---
title: "Architecture: AI Integration"
description: "Overview of strategies and patterns for integrating external AI models and services into the Nucleus platform."
version: 1.4
date: 2025-04-22
---

# Architecture: AI Integration

**Parent:** [00_ARCHITECTURE_OVERVIEW.md](./00_ARCHITECTURE_OVERVIEW.md)

## 1. Overview

This document outlines the architectural approach for integrating various third-party AI models and services (like Large Language Models, Embedding Generators, etc.) within the Nucleus ecosystem. The **primary goal** is to provide a flexible and extensible framework that allows different AI providers (e.g., Google Gemini, Azure OpenAI) to be configured and used, ideally interchangeably where appropriate, through suitable abstraction layers.

While the initial implementation focuses on Google Gemini, the overall architecture should accommodate other providers as the project evolves.

## 2. Core Integration Pattern (Current: Google Gemini via `Mscc.GenerativeAI`)

The initial integration focuses on Google's Gemini models via the `Mscc.GenerativeAI` NuGet package ecosystem. This section details the findings and the working pattern established for *this specific provider*.

### 2.1. Findings and Challenges (Gemini Integration)

Integration attempts revealed discrepancies between the documentation/intended usage of the `Mscc.GenerativeAI.Microsoft` integration package (v2.5.0) and its actual implementation:

*   **Missing Abstractions:** Key types like `IChatClient` and `ChatOptions`, expected based on documentation and examples related to `Microsoft.Extensions.AI` integration, were not found in the installed package's source code or compiled DLLs.
*   **Build Failures:** Attempts to use these missing types resulted in `CS0246` build errors.
*   **Investigation:** Extensive searching of source files, DLL inspection via reflection, and web searches confirmed the absence of these types in the installed version (Reference: Cascade Session April 16-17, 2025, Steps 300-356).

### 2.2. Recommended Pattern (Gemini via `IGenerativeAI`)

Due to the issues with the `Mscc.GenerativeAI.Microsoft` layer, the recommended and currently implemented pattern for **Gemini** relies directly on the base `Mscc.GenerativeAI` package:

1.  **Dependency Injection:** Register the core `Mscc.GenerativeAI.IGenerativeAI` interface in the service container. The specific implementation (`Mscc.GenerativeAI.GoogleAI`) is instantiated with the necessary configuration (like API keys) retrieved primarily from environment variables (`GEMINI_API_KEY`), falling back to application settings (`appsettings.json`). Register `IMemoryCache` for ephemeral storage.
    *   **Code Link:** [Nucleus.ApiService/Program.cs](../../../Services/Nucleus.Services.Api/Program.cs)
2.  **Usage in Personas/Services:** Inject `IGenerativeAI` and `IMemoryCache` into consuming classes (like Personas).
3.  **Chat Interaction:** To initiate a chat:
    *   Get a specific model instance using `IGenerativeAI.GenerativeModel()`.
    *   Start a chat session using `GenerativeModel.StartChat()`.
    *   Send messages using `ChatSession.SendMessageAsync(prompt)`. If ephemeral context is provided (see Section 2.3), it is prepended to the user's query within the prompt.
    *   **Code Link:** [Nucleus.Personas.Core/BootstrapperPersona.cs](../../../Personas/Nucleus.Personas.Core/BootstrapperPersona.cs) (`HandleQueryAsync`)
4.  **API Invocation:** The API layer (`InteractionController`) receives client requests (`AdapterRequest`) via the `POST /api/Interaction/process` endpoint. This single endpoint handles both direct queries and requests providing context artifacts. The controller maps the request to a `NucleusIngestionRequest` and invokes the `IOrchestrationService`.
    *   **Code Link:** [Nucleus.ApiService/Controllers/InteractionController.cs](../../../Services/Nucleus.Services.Api/Controllers/InteractionController.cs)
    *   **Code Link:** [Nucleus.Abstractions/Models/AdapterRequest.cs](../../../Abstractions/Nucleus.Abstractions/Models/AdapterRequest.cs)
    *   **Code Link:** [Nucleus.Abstractions/Models/AdapterResponse.cs](../../../Abstractions/Nucleus.Abstractions/Models/AdapterResponse.cs)

This pattern aligns with the examples provided in the `Mscc.GenerativeAI` library's primary documentation and avoids the problematic dependencies of the Microsoft integration layer *for Gemini*. It serves as the blueprint for the initial MVP and has been successfully implemented and tested, enabling basic query interaction with the Gemini API via the `/api/Interaction/process` endpoint, optionally using context provided via artifacts.

### 2.3. Ephemeral Context via API

To support providing context to the AI model without persisting user file content, an ephemeral caching mechanism is used in conjunction with the standard interaction flow:

1.  **Interaction Request (`POST /api/Interaction/process`):**
    *   The client sends an `AdapterRequest` to the endpoint.
    *   To provide context, the client includes one or more `ArtifactReference` objects in the `AdapterRequest.ArtifactReferences` list.
    *   Critically, the `ArtifactReference.OptionalContext` field should contain an identifier (e.g., a local file path for the Console Adapter) that the persona can use to look up pre-cached content.
    *   **Code Link:** [Nucleus.Abstractions/Models/AdapterRequest.cs](../../../Abstractions/Nucleus.Abstractions/Models/AdapterRequest.cs)
    *   **Code Link:** [Nucleus.Abstractions/Models/ArtifactReference.cs](../../../Abstractions/Nucleus.Abstractions/Models/ArtifactReference.cs)
2.  **Orchestration & Caching (`OrchestrationService` -> Persona `AnalyzeEphemeralContentAsync`):**
    *   The `InteractionController` passes the request to the `IOrchestrationService`.
    *   The orchestration service (or relevant persona logic triggered by it, like `BootstrapperPersona.AnalyzeEphemeralContentAsync`) uses the `ArtifactReference.OptionalContext` as a key to look up content previously stored in the `IMemoryCache`.
    *   *Note: The mechanism for initially populating the cache (e.g., a separate ingestion step or pre-loading) is currently specific to the adapter implementation (e.g., the Console Adapter might read a local file based on the context identifier before sending the interaction request).* This document previously described a dedicated ingestion endpoint which is now superseded by the unified interaction flow.
    *   **Code Link:** [Nucleus.Personas.Core/BootstrapperPersona.cs](../../../Personas/Nucleus.Personas.Core/BootstrapperPersona.cs) (`AnalyzeEphemeralContentAsync` - Example of cache usage)
3.  **Contextual Query Handling (Persona `HandleQueryAsync`):**
    *   If cached content associated with the `ArtifactReference.OptionalContext` is found during orchestration/persona processing, it is made available to the persona's primary query handling logic (e.g., `HandleQueryAsync`).
    *   The persona then incorporates this retrieved context into the prompt sent to the AI model (as detailed in Section 2.2, Step 3).

This mechanism allows users to reference context for a specific interaction without that context being permanently stored by Nucleus, addressing potential privacy and data management concerns, by leveraging the `IMemoryCache` and context identifiers passed within the standard `AdapterRequest`.

## 3. Future Considerations & Multi-Provider Strategy

*   **Azure OpenAI Integration:** Will require investigation into the official Azure OpenAI SDK for .NET. We will need to determine if a common abstraction (like those potentially offered by `Microsoft.Extensions.AI`) can be used or if provider-specific logic is necessary initially.
*   **Abstraction Layer:** As support for more providers (like Azure OpenAI) is added, if their SDKs differ significantly or if a common interface like `Microsoft.Extensions.AI` isn't suitable/available, introducing a Nucleus-specific abstraction layer (e.g., `INucleusChatService`, `INucleusEmbeddingService`) might become necessary to decouple Personas and other components from specific SDK implementations. This decision will be revisited when integrating the second provider.
*   **Configuration:** Refine the `appsettings.json` structure to clearly delineate configurations for different AI providers (e.g., `AiProviders:Gemini:ApiKey`, `AiProviders:AzureOpenAI:Endpoint`, `AiProviders:AzureOpenAI:ApiKey`). A mechanism to select the active provider(s) will also be needed.

## 4. Semantic Kernel Analysis and Strategic Considerations

Recent investigation (April 2025, Cascade Steps 2875-2901) explored Microsoft's Semantic Kernel (SK) framework and its potential relevance to Nucleus, focusing on Hybrid Search, Model Context Protocol (MCP), and the Agent Framework.

### 4.1. Key Semantic Kernel Features

*   **Agent Framework:** Provides abstractions for proactive, potentially collaborative AI "Agents" (conceptually similar to Nucleus "Personas") with support for multi-agent/multi-model orchestration, selection strategies, and planners.
*   **Plugins (Tools):** Formal mechanism for defining agent capabilities via native code or prompts.
*   **Memory & RAG:** Abstractions for Vector Stores (e.g., Azure AI Search, ChromaDB) and sophisticated retrieval strategies like **Hybrid Search** (vector + keyword).
*   **Standardized Interfaces:** Aims to provide common interfaces for Chat, Embeddings, etc., though potential stability issues were noted with specific integration layers (see Section 2.1).
*   **Interoperability (MCP):** Allows SK plugins/tools to be exposed via a standardized protocol.
*   **Cross-Cutting Concerns:** Integrated features like Filters and Observability.

### 4.2. Comparison with Nucleus

| Feature Area         | Semantic Kernel Approach                                      | Nucleus Planned/Current Approach                                                                                              | Overlap/Difference/Consideration                                                                                                     |
| :------------------- | :------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------ | :----------------------------------------------------------------------------------------------------------------------------------- |
| **Core Agent/AI**    | Agents (Proactive, Collaborative)                             | Personas (Specialized Domains)                                                                                                  | Similar concepts. SK may have more built-in orchestration.                                                                          |
| **LLM Interaction**  | Standardized interfaces (e.g., `IChatCompletionService`)      | Provider-specific adapters (`IGenerativeAI`), potential future Nucleus abstraction.                                               | SK aims for higher abstraction; requires validation per provider.                                                                     |
| **Tools/Capabilities** | Formal "Plugins" (Native/Prompt)                            | Services via DI.                                                                                                                | SK offers explicit plugin management.                                                                                                |
| **Memory/RAG**       | Vector Stores, Hybrid Search Abstraction                      | Embeddings in Cosmos DB, custom RAG logic planned.                                                                              | SK offers higher-level RAG features. Nucleus aims for specific RAG strategy on Cosmos DB.                                           |
| **Orchestration**    | Agent selection/termination, Planners                         | Processing Pipeline, `OrchestrationService`, future Durable Functions.                                                          | SK offers more built-in agent orchestration logic.                                                                                    |
| **Interoperability** | MCP Server                                                    | REST API for external, DI for internal.                                                                                         | SK/MCP standardizes tool exposure.                                                                                                  |
| **Chat History Ctrl**| Managed within SK context; fine-grained control needs validation. | Full control via custom adapter/persona logic.                                                                                | **Critical:** Nucleus requires fine-grained history pruning; SK's flexibility here needs confirmation.                                |
| **Ephemeral Cache**  | Not a primary SK Memory feature; likely remains app-level.  | `IMemoryCache` for interaction-specific context.                                                                               | **Critical:** Nucleus requires ephemeral prompt/response caching, a feature provided by AI providers themselves, with varied specific implementation details.                   |

### 4.3. Strategic Options

Integrating SK presents significant architectural choices:

1.  **Full Custom Build:** Continue current path. Maximum control, significant development effort for advanced features (RAG, orchestration).
2.  **Full SK Adoption:** Re-architect Nucleus around SK (Agents, Memory, Plugins). Potential acceleration, but introduces major dependency, complexity, and requires validating control requirements (Sec 4.2).
3.  **Hybrid Approach:** Selectively adopt SK components (e.g., Memory/RAG, specific Plugins) while retaining custom logic for core persona/processing and critical control points. Balances benefits and risks.

### 4.4. Decision Pending

A strategic decision is needed, weighing development velocity, long-term maintainability, the need for fine-grained control (especially chat history/ephemeral cache), and confidence in SK's stability and flexibility. The current direct integration path (Section 2.2) remains viable while this decision is pending. This analysis should inform future architecture reviews.
