// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Enum representing the status of a content extraction operation.
/// </summary>
/// <seealso href="../../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md">Processing Architecture - Shared Interfaces (ContentExtractionResult Status)</seealso>
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
/// <seealso href="../../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md">Processing Architecture - Shared Interfaces (ContentExtractionResult)</seealso>
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
