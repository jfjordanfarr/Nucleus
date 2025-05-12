using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Nucleus.Abstractions.Orchestration;

namespace Nucleus.Abstractions.Extraction;

/// <summary>
/// Interface for services that can extract textual content (and potentially other data)
/// from a source artifact stream.
/// </summary>
/// <remarks>
/// Implementations are responsible for handling specific MIME types and extracting relevant information.
/// See the main processing interfaces document for how this fits into the broader architecture.
/// Related Architecture: [Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md]
/// </remarks>
/// <seealso href="../../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md">Processing Architecture - Shared Interfaces (IContentExtractor)</seealso>
public interface IContentExtractor
{
    /// <summary>
    /// Checks if this extractor supports the given MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type string (e.g., "text/plain", "text/html").</param>
    /// <returns>True if the MIME type is supported, false otherwise.</returns>
    bool SupportsMimeType(string mimeType);

    /// <summary>
    /// Extracts content from the provided stream.
    /// </summary>
    /// <param name="sourceStream">The stream containing the artifact content. The stream should be readable.</param>
    /// <param name="mimeType">The MIME type of the content.</param>
    /// <param name="sourceUri">Optional URI of the source for context/logging.</param>
    /// <returns>A task representing the asynchronous operation, yielding a result containing extracted text and potentially other metadata.</returns>
    /// <remarks>
    /// Implementations should handle potential exceptions during stream reading and parsing.
    /// The ownership of the stream (disposing it) is typically handled by the caller.
    /// </remarks>
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, Uri? sourceUri = null);

}
