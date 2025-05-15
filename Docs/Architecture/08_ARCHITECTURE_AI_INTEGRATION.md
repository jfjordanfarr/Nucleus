---
title: "Architecture: AI Integration (Microsoft.Extensions.AI)"
description: "Overview of strategies for integrating external AI models using Microsoft.Extensions.AI, emphasizing ephemeral content retrieval via the API service and IArtifactProvider."
version: 1.9
date: 2025-05-15
---

# Architecture: AI Integration (using `Microsoft.Extensions.AI`)

**Parent:** [00_ARCHITECTURE_OVERVIEW.md](./00_ARCHITECTURE_OVERVIEW.md)
**Version:** 1.9
**Date:** 2025-05-15

## 1. Overview

This document outlines the architectural approach for integrating various third-party AI models and services (like Large Language Models) within the Nucleus ecosystem. The **primary architectural choice** is to leverage the **`Microsoft.Extensions.AI`** abstractions, specifically `Microsoft.Extensions.AI.IChatClient` for chat completions.

**Key Benefits:**
*   **Provider Agnosticism:** Using `IChatClient` allows the application core (e.g., Personas) to interact with different AI providers (Google Gemini, Azure OpenAI, Ollama, etc.) through a unified interface without code changes in the consuming components. Switching providers primarily involves changing configuration and dependency injection registration.
*   **Flexibility & Extensibility:** The `Microsoft.Extensions.AI` framework supports middleware for adding capabilities like logging, caching, telemetry, and function calling in a standard way across providers.
*   **Standardization:** Aligns with emerging .NET standards for AI integration, facilitating interoperability with other libraries and frameworks in the ecosystem.

**Current Status:** The *current* implementation uses the `Mscc.GenerativeAI.Microsoft` package as a **temporary solution** to provide an `IChatClient` implementation for Google Gemini, pending an official .NET SDK for the latest Google AI APIs.
*   **Reference:** [../HelpfulMarkdownFiles/Library-References/Mscc.GenerativeAI.Microsoft-Reference.md](../HelpfulMarkdownFiles/Library-References/Mscc.GenerativeAI.Microsoft-Reference.md)

**Reference:** For background on `Microsoft.Extensions.AI`, see [../HelpfulMarkdownFiles/Library-References/MicrosoftExtensionsAI.md](../HelpfulMarkdownFiles/Library-References/MicrosoftExtensionsAI.md).

## 2. Core Integration Pattern (`IChatClient`)

The core pattern relies on registering and injecting `Microsoft.Extensions.AI.IChatClient`. The specific implementation registered determines the backend AI provider.

### 2.1. Current Implementation (Temporary: Google Gemini via `Mscc.GenerativeAI.Microsoft`)

The current working integration uses the `Mscc.GenerativeAI.Microsoft` NuGet package to bridge Gemini to `IChatClient`.

**Note:** While previous investigations noted discrepancies, the current setup *is* functional using this pattern.

1.  **Dependencies:** `Mscc.GenerativeAI.Microsoft` package reference in `Nucleus.Services.Api`.
2.  **Configuration:** `AI:GoogleAI:ApiKey` and `AI:GoogleAI:Model` in `appsettings.json` (using keys defined in [NucleusConstants](../../../src/Nucleus.Abstractions/NucleusConstants.cs)).
    *   **Code Link:** [Nucleus.Services.Api/appsettings.Development.json](../../../src/Services/Nucleus.Services.Api/appsettings.Development.json)
    *   **Config Class:** [GoogleAiOptions](../../../src/Nucleus.Abstractions/Models/Configuration/GoogleAiOptions.cs)
3.  **Dependency Injection (`Program.cs`):** Registration performed in [WebApplicationBuilderExtensions](../../../src/Services/Nucleus.Services.Api/WebApplicationBuilderExtensions.cs)
    ```csharp
    // Register Google AI Chat Client using Mscc.GenerativeAI.Microsoft extension method
    builder.Services.AddGeminiChat(options =>
    {
        options.ApiKey = builder.Configuration["AI:GoogleAI:ApiKey"];
        options.Model = builder.Configuration["AI:GoogleAI:Model"] ?? "gemini-1.5-flash-001"; // Example default
    });
    ```
    *   This registers `IChatClient` backed by Gemini.
    *   **Code Link:** [Nucleus.Services.Api/Program.cs](../../../src/Services/Nucleus.Services.Api/Program.cs)
4.  **Usage:** Inject `IChatClient` into consumers (e.g., `BootstrapperPersona`).
    *   Create `ChatRequest` with `ChatMessage` list.
    *   Call `IChatClient.CompleteAsync(chatRequest)`.
    *   Process `ChatCompletion` response.
    *   **Code Link:** [Nucleus.Personas.Core/BootstrapperPersona.cs](../../../src/Personas/Nucleus.Personas.Core/BootstrapperPersona.cs) (`HandleQueryAsync`)

### 2.2. Switching Providers (Example: Azure OpenAI)

Leveraging the `IChatClient` abstraction makes switching providers straightforward:

1.  **Dependencies:** Add the required provider package (e.g., `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, `Azure.Identity`).
2.  **Configuration:** Add necessary configuration for the new provider (e.g., `AI:AzureOpenAI:Endpoint`, `AI:AzureOpenAI:DeploymentName`) in `appsettings.json`.
3.  **Dependency Injection (`Program.cs`):** Replace the `AddGeminiChat` registration with the appropriate registration for the new provider. For Azure OpenAI, this might look like (referencing patterns from `MicrosoftExtensionsAI.md`):
    ```csharp
    // Remove or comment out AddGeminiChat registration
    // builder.Services.AddGeminiChat(...);

    // Register Azure OpenAI Chat Client
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var endpoint = builder.Configuration["AI:AzureOpenAI:Endpoint"];
        var deploymentName = builder.Configuration["AI:AzureOpenAI:DeploymentName"];
        // Ensure endpoint and deploymentName are configured
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint and deployment name must be configured.");
        }

        // Using DefaultAzureCredential for authentication
        return new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                 .AsChatClient(deploymentName);
    });
    ```
    *   Ensure the necessary using statements (`Azure.AI.OpenAI`, `Azure.Identity`, `Microsoft.Extensions.AI`) are added.
4.  **Usage:** No changes required in consuming classes like `BootstrapperPersona` as they still inject and use `IChatClient`.

This demonstrates the power of the abstraction layer – the core application logic remains unchanged.

### 2.3. Providing Ephemeral Context to AI Models

Nucleus ensures user file content remains in user-controlled storage and is accessed ephemerally only when needed for processing. Providing this content as context to AI models follows the standard API interaction flow:

1.  **Adapter Request Construction:**
    *   The Client Adapter identifies relevant artifacts for the interaction (e.g., files mentioned in a prompt, files explicitly attached).
    *   The Adapter constructs an `AdapterRequest` and populates the `ArtifactReferences` list with `ArtifactReference` objects for each relevant artifact. These objects contain the necessary details (e.g., `ReferenceType`, `SourceUri`, `SourceIdentifier`) for the API service to locate the artifact later.
    *   **Code Link:** [Nucleus.Abstractions/Models/AdapterRequest.cs](../../../Abstractions/Nucleus.Abstractions/Models/AdapterRequest.cs)
    *   **Code Link:** [Nucleus.Abstractions/Models/ArtifactReference.cs](../../../Abstractions/Nucleus.Abstractions/Models/ArtifactReference.cs)
2.  **API Interaction Request:**
    *   The Adapter sends the `AdapterRequest` (containing the `ArtifactReferences`) to the primary API endpoint: `POST /api/v1/interactions`.
    *   **Code Link:** [Api/ARCHITECTURE_API_CLIENT_INTERACTION.md](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
3.  **API Service Orchestration & Content Retrieval:**
    *   The `Nucleus.Services.Api` receives the `AdapterRequest`.
    *   The responsible `OrchestrationService` identifies the need for context (based on persona logic, user query, etc.).
    *   It determines the required `ArtifactReference`s (potentially involving searches against `IArtifactMetadataRepository`).
    *   It iterates through the required references and uses the appropriate [IArtifactProvider](../../../src/Nucleus.Abstractions/IArtifactProvider.cs) (resolved via DI or a manager) to ephemerally retrieve the `ArtifactContent` (stream + metadata) for each.
    *   The retrieved content streams are read, potentially processed/chunked, and formatted into prompts (e.g., `ChatMessage` objects).
    *   The formatted prompt context is sent to the AI model via `IChatClient`.
    *   The response is processed, and the retrieved content streams are disposed.

This process ensures that:
*   User content is only accessed when necessary for a specific interaction.
*   Content retrieval is orchestrated centrally by the API service, respecting security and source system boundaries via `IArtifactProvider`.
*   No intermediate storage of user content is required within Nucleus.
*   The AI model receives rich, full-document context derived directly from the user's authoritative source, which enables the AI model to generate higher-quality responses based on deeper understanding compared to approaches limited by pre-chunked or summarized data.

## 3. Future Considerations & Multi-Provider Strategy

*   **Azure OpenAI Integration:** Will require investigation into the official Azure OpenAI SDK for .NET. We will need to determine if a common abstraction (like those potentially offered by `Microsoft.Extensions.AI`) can be used or if provider-specific logic is necessary initially.
*   **Abstraction Layer:** As support for more providers (like Azure OpenAI) is added, if their SDKs differ significantly or if a common interface like `Microsoft.Extensions.AI` isn't suitable/available, introducing a Nucleus-specific abstraction layer (e.g., `INucleusChatService`, `INucleusEmbeddingService`) might become necessary to decouple Personas and other components from specific SDK implementations. This decision will be revisited when integrating the second provider.
*   **Configuration:** Refine the `appsettings.json` structure to clearly delineate configurations for different AI providers (e.g., `AiProviders:Gemini:ApiKey`, `AiProviders:AzureOpenAI:Endpoint`, `AiProviders:AzureOpenAI:ApiKey`). A mechanism to select the active provider(s) will also be needed.

## 4. Semantic Kernel Analysis and Strategic Considerations

Recent investigation explored Microsoft's Semantic Kernel (SK) framework and its potential relevance to Nucleus, focusing on Hybrid Search, Model Context Protocol (MCP), and the Agent Framework.

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
| **Ephemeral Cache**  | Not a primary SK Memory feature; likely remains app-level.  | Nucleus retrieves context ephemerally via `IArtifactProvider` when needed for prompts. Provider-level prompt/response caching is separate. | **Critical:** Nucleus requires ephemeral context *retrieval*. AI Provider caching is a distinct mechanism.                     |

### 4.3. Strategic Options

Integrating SK presents significant architectural choices:

1.  **Full Custom Build:** Continue current path. Maximum control, significant development effort for advanced features (RAG, orchestration).
2.  **Full SK Adoption:** Re-architect Nucleus around SK (Agents, Memory, Plugins). Potential acceleration, but introduces major dependency, complexity, and requires validating control requirements (Sec 4.2).
3.  **Hybrid Approach:** Selectively adopt SK components (e.g., Memory/RAG, specific Plugins) while retaining custom logic for core persona/processing and critical control points. Balances benefits and risks.

### 4.4. Decision Pending

A strategic decision is needed, weighing development velocity, long-term maintainability, the need for fine-grained control (especially chat history/ephemeral cache), and confidence in SK's stability and flexibility. The current direct integration path (Section 2.2) remains viable while this decision is pending. This analysis should inform future architecture reviews.

This design leverages the `AdapterRequest` model ([Code Link: ../../../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterRequest.cs](../../../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterRequest.cs)) which can carry `ArtifactReference`s.
It details how clients provide `ArtifactReference`s which the API Service then uses with `IArtifactProvider` implementations ([Code Link: ../../../../src/Nucleus.Abstractions/IArtifactProvider.cs](../../../../src/Nucleus.Abstractions/IArtifactProvider.cs)) to fetch content ephemerally. This mechanism ensures that Nucleus does not store user data from external systems, a core tenet also discussed in [06_ARCHITECTURE_SECURITY.md](./06_ARCHITECTURE_SECURITY.md) and a key part of the [Phase 2 RAG requirements](../Requirements/02_REQUIREMENTS_PHASE2_CORE_RAG.md) <!-- [BROKEN LINK - File Not Found] -->.

Key points:
