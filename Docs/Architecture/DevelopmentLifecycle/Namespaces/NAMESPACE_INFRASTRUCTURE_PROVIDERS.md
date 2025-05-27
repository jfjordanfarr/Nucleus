---
title: "Namespace - Nucleus.Infrastructure.ExternalServices"
description: "Provides concrete implementations for interacting with various external services and APIs, such as AI models and file access services."
version: 1.0
date: 2025-05-29
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.ExternalServices

**Relative Path:** `src/Nucleus.Infrastructure/Nucleus.Infrastructure.ExternalServices/Nucleus.Infrastructure.ExternalServices.csproj`

## 1. Purpose

This project consolidates concrete implementations for interacting with various external services and APIs. It acts as a bridge between the Nucleus core logic/agents and the outside world, abstracting the specifics of external communication.

Key responsibilities include:

*   **AI Model Interaction:** Implementing interfaces (e.g., from `Nucleus.Shared.Kernel.Abstractions.AI`) to communicate with AI inference providers (like Google Gemini) using `Microsoft.Extensions.AI` abstractions and clients.
*   **File/Artifact Provisioning:** Implementing interfaces (e.g., `IArtifactProvider` from `Nucleus.Shared.Kernel.Abstractions.ExternalServices.FileAccess`) to retrieve artifact content from various sources (e.g., local file system, Microsoft Graph for OneDrive/SharePoint).
*   **Content Extraction:** Implementing interfaces (e.g., `IContentExtractor` from `Nucleus.Shared.Kernel.Abstractions.ExternalServices.FileAccess`) to process raw artifact content (e.g., from HTML, PDF) to extract meaningful text and metadata.
*   **Other External Service Integrations:** Providing clients or wrappers for any other third-party APIs or services that Nucleus might need to interact with (e.g., specialized data sources, notification services if not handled by platform adapters).

These implementations primarily implement interfaces defined in `Nucleus.Shared.Kernel.Abstractions` and are consumed by core logic projects like `Nucleus.Core.RAGLogic` and `Nucleus.Core.AgentRuntime`, as well as potentially by MCP Tools if they require direct external access not mediated by an agent.

## 2. Key Components

This project is organized into sub-namespaces based on the type of external service interaction.

*   **AI Model Services (`Nucleus.Infrastructure.ExternalServices.AI`):**
    *   `GeminiChatCompletionService.cs` (Illustrative): Implements an interface (e.g., `IChatCompletionService`) using `Microsoft.Extensions.AI.ChatCompletion.ChatCompletionService` configured for Gemini.
    *   `GeminiEmbeddingGenerationService.cs` (Illustrative): Implements an interface (e.g., `ITextEmbeddingGenerationService`) using `Microsoft.Extensions.AI.Embeddings.TextEmbeddingGenerationService` configured for Gemini.
    *   Configuration classes for AI model settings.

*   **File Access Providers (`Nucleus.Infrastructure.ExternalServices.FileAccess.Providers`):**
    *   `IArtifactProvider` Implementations:
        *   `LocalFileArtifactProvider.cs`: Fetches content for artifacts referenced via local file system paths.
        *   `GraphArtifactProvider.cs` (Illustrative): Fetches content from Microsoft Graph (e.g., OneDrive, SharePoint).
        *   **(Future Providers):** Implementations for other artifact sources (e.g., Web URLs).

*   **Content Extractors (`Nucleus.Infrastructure.ExternalServices.FileAccess.Extractors`):**
    *   `IContentExtractor` Implementations & Helpers:
        *   `HtmlContentExtractor.cs`: Extracts text content from HTML documents.
        *   `PlainTextContentExtractor.cs`: Extracts text content from plain text files.
        *   `PdfContentExtractor.cs` (Illustrative): Extracts text content from PDF documents.
        *   `MimeTypeHelper.cs`: Utility for working with MIME types.

*   **Service Collection Extensions (`Nucleus.Infrastructure.ExternalServices.Extensions`):**
    *   `ServiceCollectionExtensions.cs`: Provides extension methods to register the various external service clients and implementations with the dependency injection container, handling necessary configurations (e.g., `IHttpClientFactory` setup, options binding).

## 3. Dependencies

*   `Nucleus.Shared.Kernel.Abstractions` (References interfaces for AI services, file access, etc.).
*   `Microsoft.Extensions.AI` (Core abstractions for AI model interaction).
*   `Microsoft.Extensions.AI.Gemini` (If using the Gemini-specific package from `Microsoft.Extensions.AI`).
*   `Microsoft.Extensions.Http` (For `IHttpClientFactory`).
*   Microsoft Graph SDK (e.g., `Microsoft.Graph`) for `GraphArtifactProvider`.
*   Relevant file parsing libraries (e.g., a PDF parsing library for `PdfContentExtractor`).
*   Other external service SDKs as needed.

## 4. Dependents

*   `Nucleus.Core.RAGLogic` (Consumes AI model services, content extractors).
*   `Nucleus.Core.AgentRuntime` (May consume AI model services or other external services).
*   `Nucleus.AppHost` (For registering these services in the DI container during application startup).
*   Potentially MCP Tools if they require direct access to these providers.

## 5. Related Documents

*   [01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [05_ARCHITECTURE_INTEGRATIONS.md](../../Integrations/05_ARCHITECTURE_INTEGRATIONS.md) (New or existing overview of external service integrations)
*   [NAMESPACE_SHARED_KERNEL_ABSTRACTIONS.md](./NAMESPACE_SHARED_KERNEL_ABSTRACTIONS.md)
*   Relevant MCP Tool architecture documents if they directly consume these providers.

---

*Self-Correction/Note during transformation: The LCG instructions suggested `Nucleus.Infrastructure.ExternalServices` as the target project. The existing document was `NAMESPACE_INFRASTRUCTURE_PROVIDERS.md` with a target of `Nucleus.Infrastructure.FileAccessProviders`. I have merged the concepts, focusing on the broader `ExternalServices` scope which includes AI model interactions (a key part of the new architecture) alongside file access. The file access components from the original document have been retained and integrated into this new structure. The `InMemoryPersonaConfigurationProvider` mentioned in the old document should indeed be moved to `Nucleus.Infrastructure.Testing` as it's a test double, not a true external service provider.*
