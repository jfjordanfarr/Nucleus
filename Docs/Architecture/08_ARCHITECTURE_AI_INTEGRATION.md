---
title: "Architecture: AI Integration"
description: "Overview of strategies and patterns for integrating external AI models and services into the Nucleus platform."
version: 1.3
date: 2025-04-21
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
    *   **Code Link:** [Nucleus.ApiService/Program.cs](../../../Nucleus.ApiService/Program.cs)
2.  **Usage in Personas/Services:** Inject `IGenerativeAI` and `IMemoryCache` into consuming classes (like Personas).
3.  **Chat Interaction:** To initiate a chat:
    *   Get a specific model instance using `IGenerativeAI.GenerativeModel()`.
    *   Start a chat session using `GenerativeModel.StartChat()`.
    *   Send messages using `ChatSession.SendMessageAsync(prompt)`. If ephemeral context is provided (see Section 2.3), it is prepended to the user's query within the prompt.
    *   **Code Link:** [Nucleus.Personas.Core/BootstrapperPersona.cs](../../../Nucleus.Personas.Core/BootstrapperPersona.cs) (`HandleQueryAsync`)
4.  **API Invocation:** The API layer (e.g., `QueryController`) receives client requests, validates them, and invokes the relevant persona method (`HandleQueryAsync`). The query request can optionally include a `ContextSourceIdentifier` to utilize cached ephemeral context (see Section 2.3).
    *   **Code Link:** [Nucleus.ApiService/Controllers/QueryController.cs](../../../Nucleus.ApiService/Controllers/QueryController.cs)
    *   **Code Link:** [Nucleus.ApiService/Contracts/QueryRequest.cs](../../../Nucleus.ApiService/Contracts/QueryRequest.cs)

This pattern aligns with the examples provided in the `Mscc.GenerativeAI` library's primary documentation and avoids the problematic dependencies of the Microsoft integration layer *for Gemini*. It serves as the blueprint for the initial MVP and has been successfully implemented and tested, enabling basic query interaction with the Gemini API via the `/api/query` endpoint, optionally using context provided via the ingestion endpoint described below.

### 2.3. Ephemeral Context via API (Local Files)

To support providing context to the AI model without persisting user file content, an ephemeral caching mechanism has been implemented:

1.  **Ingestion Endpoint (`POST /api/ingestion/localfile`):**
    *   Accepts a request containing the full path to a local file (`IngestLocalFileRequest`).
    *   The `IngestionController` reads the content of the specified file (currently treating it as plaintext).
    *   It passes the file content and the file path (as the `sourceIdentifier`) to the persona's `AnalyzeEphemeralContentAsync` method.
    *   **Code Link:** [Nucleus.ApiService/Controllers/IngestionController.cs](../../../Nucleus.ApiService/Controllers/IngestionController.cs)
    *   **Code Link:** [Nucleus.ApiService/Contracts/IngestLocalFileRequest.cs](../../../Nucleus.ApiService/Contracts/IngestLocalFileRequest.cs)
2.  **Caching (`BootstrapperPersona.AnalyzeEphemeralContentAsync`):**
    *   The persona uses the injected `IMemoryCache` service to store the provided content.
    *   The `sourceIdentifier` (the file path in this case) is used as the cache key.
    *   This caching is temporary and in-memory, aligning with the Nucleus principle of avoiding persistent storage for raw/intermediate user content.
    *   **Code Link:** [Nucleus.Personas.Core/BootstrapperPersona.cs](../../../Nucleus.Personas.Core/BootstrapperPersona.cs) (`AnalyzeEphemeralContentAsync`)
3.  **Contextual Query (`POST /api/query`):**
    *   The `QueryRequest` model now includes an optional `ContextSourceIdentifier` field.
    *   If this identifier is provided, the `QueryController` attempts to retrieve the corresponding content from the `IMemoryCache`.
    *   If found, the cached content is passed to the persona's `HandleQueryAsync` method alongside the user's query text.
    *   The persona then incorporates this context into the prompt sent to the AI model (as detailed in Section 2.2, Step 3).

This mechanism allows users to upload context for a specific interaction without that context being permanently stored by Nucleus, addressing potential privacy and data management concerns.

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
