---
title: Processing Architecture - Shared Interfaces
description: Defines common C# interfaces used across various stages of the Nucleus OmniRAG processing pipeline.
version: 1.0
date: 2025-04-18
---

# Processing Architecture: Shared Interfaces

**Version:** 1.0
**Date:** 2025-04-18

This document defines common C# interfaces shared by different components within the Nucleus processing architecture, promoting consistency and modularity. It complements the main [Processing Architecture document](../01_ARCHITECTURE_PROCESSING.md).

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

This interface defines the central coordinating service for processing incoming requests. It's responsible for managing the overall flow, including potentially fetching source data (via `IPlatformAttachmentFetcher`), invoking specific processors, handling session state, and ensuring responses are sent back (via `IPlatformNotifier`).

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
/// the processing of incoming ingestion requests.
/// This includes session management, persona selection, routing, and invoking specific processing steps.
/// See: ../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md
/// See: ../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// See: ../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md
/// </summary>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes a standardized ingestion request received from a platform adapter
    /// (typically via the ServiceBusQueueConsumerService).
    /// </summary>
    /// <param name="request">The details of the ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessIngestionRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);
}

---
**(More interfaces may be added here as the processing pipeline evolves)**
