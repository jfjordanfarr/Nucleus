using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

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
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, Uri? sourceUri = null);

}

/// <summary>
/// Enum representing the status of a content extraction operation.
/// </summary>
public enum ExtractionStatus
{
    /// <summary>Indicates the extraction was successful.</summary>
    Success,
    /// <summary>Indicates the extraction failed due to an error.</summary>
    Failure,
    /// <summary>Indicates the MIME type is not supported by the extractor.</summary>
    UnsupportedMimeType
}

/// <summary>
/// Holds the result of content extraction, including status and any extracted data or errors.
/// </summary>
public sealed record ContentExtractionResult
{
    /// <summary>
    /// Gets the overall status of the extraction operation.
    /// </summary>
    public ExtractionStatus Status { get; private init; }

    /// <summary>
    /// Gets the primary textual content extracted from the source. Null if extraction failed or wasn't applicable.
    /// </summary>
    public string? ExtractedText { get; private init; }

    /// <summary>
    /// Gets an optional message providing more details about the result, especially in case of failure or warnings.
    /// </summary>
    public string? Message { get; private init; }

    /// <summary>
    /// Gets the exception that occurred during extraction, if any.
    /// </summary>
    public Exception? Exception { get; private init; }

    /// <summary>
    /// Gets an optional dictionary containing additional metadata extracted (e.g., page numbers, title, author, structural info).
    /// </summary>
    public Dictionary<string, object>? AdditionalMetadata { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the extraction was successful.
    /// </summary>
    public bool IsSuccess => Status == ExtractionStatus.Success;

    // Private constructor to force usage of factory methods
    private ContentExtractionResult() { }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static ContentExtractionResult Success(string extractedText, string? message = null, Dictionary<string, object>? additionalMetadata = null)
    {
        return new ContentExtractionResult
        {
            Status = ExtractionStatus.Success,
            ExtractedText = extractedText,
            Message = message,
            AdditionalMetadata = additionalMetadata
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static ContentExtractionResult Failure(string message, Exception? exception = null)
    {
        return new ContentExtractionResult
        {
            Status = ExtractionStatus.Failure,
            Message = message,
            Exception = exception
        };
    }

    /// <summary>
    /// Creates an unsupported MIME type result.
    /// </summary>
    public static ContentExtractionResult UnsupportedMimeType(string mimeType)
    {
        return new ContentExtractionResult
        {
            Status = ExtractionStatus.UnsupportedMimeType,
            Message = $"Extractor does not support MIME type: {mimeType}"
        };
    }
}
