---
title: Processing Architecture - Shared Interfaces
description: Defines common C# interfaces used across various stages of the Nucleus OmniRAG processing pipeline.
version: 1.1
date: 2025-04-23
parent: ../05_ARCHITECTURE_PROCESSING.md
---

# Processing Architecture: Shared Interfaces

This document defines common C# interfaces shared by different components within the Nucleus processing architecture, promoting consistency and modularity. It complements the main [Processing Architecture document](../05_ARCHITECTURE_PROCESSING.md).

## 1. `IContentExtractor`

This interface provides a standard way to handle the *initial parsing* and content retrieval from different source artifact types before further processing or synthesis.

```csharp
/// <summary>
/// Interface for services that can extract textual content (and potentially other data)
/// from a source artifact stream.
/// Related Architecture: [ARCHITECTURE_PROCESSING_INGESTION.md](./ARCHITECTURE_PROCESSING_INGESTION.md)
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// Checks if this extractor supports the given MIME type.
    /// </summary>
    bool SupportsMimeType(string mimeType);

    /// <summary>
    /// Extracts content from the provided stream.
    /// </summary>
    /// <param name="sourceStream">The stream containing the artifact content.</param>
    /// <param name="mimeType">The MIME type of the content.</param>
    /// <param name="sourceUri">Optional URI of the source for context.</param>
    /// <returns>A result containing extracted text and potentially other metadata.</returns>
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, string? sourceUri = null);
}

/// <summary>
/// Holds the result of content extraction.
/// </summary>
public record ContentExtractionResult(
    string ExtractedText,
    Dictionary<string, object>? AdditionalMetadata = null // e.g., page numbers, structural info
);

## 2. `IOrchestrationService`

This interface defines the central coordinating service for processing incoming requests within the `Nucleus.Services.Api`. It's responsible for managing the overall flow for tasks like ingestion or querying. **In the API-First architecture, implementations of this service are typically invoked by the API's request handlers (e.g., ASP.NET Core Controllers) after initial request validation and authentication.** It orchestrates steps like fetching/validating source data, invoking specific processors (e.g., content extraction, chunking, embedding, LLM interaction), handling session state, and potentially triggering notifications or responses.

*   **Code:** [`Nucleus.Abstractions/IOrchestrationService.cs`](cci:7://file:///d:/Projects/Nucleus/Nucleus.Abstractions/IOrchestrationService.cs:0:0-0:0)
*   **Related Architecture:**
    *   [ARCHITECTURE_PROCESSING_INGESTION.md](./ARCHITECTURE_PROCESSING_INGESTION.md)
    *   [ARCHITECTURE_PROCESSING_ORCHESTRATION.md](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md)

```csharp
using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines the contract for the central service responsible for orchestrating
/// the processing of incoming ingestion requests within the Nucleus API service.
/// This includes session management, persona selection, routing, and invoking specific processing steps.
/// Implementations are typically invoked by the API request handling layer (e.g., Controllers).
/// See: ../05_ARCHITECTURE_PROCESSING.md
/// See: ./ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// </summary>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes a standardized ingestion request, typically constructed by the API layer
    /// from an incoming API call.
    /// </summary>
    /// <param name="request">The details of the ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessIngestionRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);
}

---
**(More interfaces may be added here as the processing pipeline evolves)**
