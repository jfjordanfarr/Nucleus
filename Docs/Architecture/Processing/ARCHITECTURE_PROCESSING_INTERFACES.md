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
```

---

**(More interfaces may be added here as the processing pipeline evolves)**
