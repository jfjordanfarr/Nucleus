using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Extraction;

namespace Nucleus.Infrastructure.Providers.ContentExtraction;

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
    public async Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, Uri? sourceUri = null)
    {
        if (!SupportsMimeType(mimeType))
        {
            // Return specific result instead of throwing exception
            return ContentExtractionResult.UnsupportedMimeType(mimeType);
        }

        // Check sourceStream before null check for clarity
        if (sourceStream == null || !sourceStream.CanRead)
        {
            // Return specific result instead of throwing exception
            return ContentExtractionResult.Failure("Source stream cannot be null and must be readable.");
        }

        try
        {
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
                // Use static factory method
                return ContentExtractionResult.Success(extractedText);
            }
        }
        catch (Exception ex)
        {
            // Return failure result on exception
            return ContentExtractionResult.Failure($"Error reading plain text stream: {ex.Message}", ex);
        }
    }
}
