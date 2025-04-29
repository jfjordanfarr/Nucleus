using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Nucleus.Services.Shared.Extraction;

/// <summary>
/// Interface for services that can extract textual content (and potentially other data)
/// from a source artifact stream.
/// Related Architecture: [Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md]
/// </summary>
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
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, string? sourceUri = null);
}

/// <summary>
/// Holds the result of content extraction.
/// </summary>
/// <param name="ExtractedText">The primary textual content extracted from the source.</param>
/// <param name="AdditionalMetadata">Optional dictionary containing additional metadata extracted (e.g., page numbers, title, author, structural info).</param>
public record ContentExtractionResult(
    string ExtractedText,
    Dictionary<string, object>? AdditionalMetadata = null
);
