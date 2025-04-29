using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Services.Shared.Extraction;

/// <summary>
/// Extracts text content from plain text streams (MIME type "text/plain").
/// </summary>
public class PlainTextContentExtractor : IContentExtractor
{
    private const string SupportedMimeType = "text/plain";

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return SupportedMimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, string? sourceUri = null)
    {
        if (!SupportsMimeType(mimeType))
        {
            throw new ArgumentException($"Unsupported MIME type: {mimeType}. This extractor only supports {SupportedMimeType}.", nameof(mimeType));
        }

        if (sourceStream == null || !sourceStream.CanRead)
        {
            throw new ArgumentNullException(nameof(sourceStream), "Source stream cannot be null and must be readable.");
        }

        // Keep the stream open for the caller to manage, but reset position if possible
        if (sourceStream.CanSeek)
        {
            sourceStream.Position = 0;
        }

        // Use StreamReader to handle potential encoding issues (defaults to UTF-8)
        // Pass leaveOpen: true so the StreamReader doesn't dispose the sourceStream
        using (var reader = new StreamReader(sourceStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: true))
        {
            string extractedText = await reader.ReadToEndAsync();
            return new ContentExtractionResult(extractedText);
        }
    }
}
