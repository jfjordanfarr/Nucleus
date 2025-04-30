// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Services.Shared;

/// <summary>
/// Provides utility methods for handling MIME types, particularly for mapping to Content Extractor keys.
/// </summary>
public static class MimeTypeHelper
{
    // Dictionary to map common MIME types to the keys used for registering IContentExtractor services.
    // Keys should match the keys used in DI registration (e.g., WebApplicationBuilderExtensions.cs)
    private static readonly Dictionary<string, string> MimeTypeToExtractorKeyMap = new(System.StringComparer.OrdinalIgnoreCase)
    {
        // Textual Types
        { NucleusConstants.MimeTypes.Textual.Plain, NucleusConstants.ExtractorKeys.PlainText },
        { NucleusConstants.MimeTypes.Textual.Html, NucleusConstants.ExtractorKeys.Html },
        { NucleusConstants.MimeTypes.Textual.Markdown, NucleusConstants.ExtractorKeys.Markdown }, // Assuming a future Markdown extractor
        { NucleusConstants.MimeTypes.Textual.Csv, NucleusConstants.ExtractorKeys.PlainText }, // CSV can often be treated as plain text
        { NucleusConstants.MimeTypes.Application.Xml, NucleusConstants.ExtractorKeys.Xml }, // Assuming a future XML extractor

        // Application Types
        { NucleusConstants.MimeTypes.Application.Json, NucleusConstants.ExtractorKeys.Json }, // Assuming a future JSON extractor
        { NucleusConstants.MimeTypes.Application.Pdf, NucleusConstants.ExtractorKeys.Pdf }, // Assuming a future PDF extractor
        { NucleusConstants.MimeTypes.Application.OctetStream, NucleusConstants.ExtractorKeys.DefaultBinary }, // Fallback for generic binary

        // Add mappings for Microsoft Office documents if/when extractors are available
        // { NucleusConstants.MimeTypes.Application.WordprocessingML, NucleusConstants.ExtractorKeys.OfficeDocument },
        // { NucleusConstants.MimeTypes.Application.SpreadsheetML, NucleusConstants.ExtractorKeys.OfficeDocument },
        // { NucleusConstants.MimeTypes.Application.PresentationML, NucleusConstants.ExtractorKeys.OfficeDocument },

        // Add other mappings as needed
    };

    /// <summary>
    /// Gets the Dependency Injection key for the appropriate <see cref="IContentExtractor"/> based on the provided MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type string (e.g., "text/plain").</param>
    /// <returns>The DI key for the content extractor, or <see cref="NucleusConstants.ExtractorKeys.DefaultBinary"/> if no specific mapping is found.</returns>
    public static string GetExtractorKeyForMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            // Default to plain text if MIME type is unknown or missing?
            // Or maybe DefaultBinary is safer?
            return NucleusConstants.ExtractorKeys.PlainText; // Or DefaultBinary
        }

        // Normalize or handle parameters (e.g., text/plain; charset=utf-8 -> text/plain)
        var baseMimeType = mimeType.Split(';')[0].Trim();

        if (MimeTypeToExtractorKeyMap.TryGetValue(baseMimeType, out var key))
        {
            return key;
        }

        // Fallback for unmapped types (e.g., images, audio, video, unknown application types)
        // Consider logging a warning here for unmapped types.
        return NucleusConstants.ExtractorKeys.DefaultBinary; // Or potentially throw?
    }

    /// <summary>
    /// Tries to get the Dependency Injection key for the appropriate <see cref="IContentExtractor"/>.
    /// </summary>
    /// <param name="mimeType">The MIME type string.</param>
    /// <param name="extractorKey">The resolved extractor key, if found.</param>
    /// <returns><c>true</c> if a specific mapping was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetExtractorKeyForMimeType(string? mimeType, [NotNullWhen(true)] out string? extractorKey)
    {
        extractorKey = null;
        if (string.IsNullOrWhiteSpace(mimeType))
        {            return false;
        }

        var baseMimeType = mimeType.Split(';')[0].Trim();

        if (MimeTypeToExtractorKeyMap.TryGetValue(baseMimeType, out extractorKey))
        {            return true;
        }

        return false;
    }
}
