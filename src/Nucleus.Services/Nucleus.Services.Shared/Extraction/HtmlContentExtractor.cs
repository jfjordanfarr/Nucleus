using HtmlAgilityPack;
using Nucleus.Abstractions;
using Nucleus.Services.Shared.Extraction;
using System.Text;

namespace Nucleus.Services.Shared.Extraction;

/// <summary>
/// Extracts text content from HTML documents using HtmlAgilityPack.
/// </summary>
/// <remarks>
/// Handles MIME types like "text/html".
/// See: [ARCHITECTURE_PROCESSING_INTERFACES.md](../../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md)
/// </remarks>
public sealed class HtmlContentExtractor : IContentExtractor
{
    private readonly HashSet<string> _supportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        NucleusMimeTypes.Text.Html
    };

    /// <inheritdoc />
    public IReadOnlySet<string> SupportedMimeTypes => _supportedMimeTypes;

    /// <inheritdoc />
    public async Task<ContentExtractionResult> ExtractContentAsync(
        Stream sourceStream,
        string mimeType,
        string? sourceUri = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        if (!SupportedMimeTypes.Contains(mimeType))
        {
            return ContentExtractionResult.UnsupportedMimeType(mimeType);
        }

        if (!sourceStream.CanRead)
        {
            return ContentExtractionResult.Failure("Source stream is not readable.");
        }

        try
        {
            var doc = new HtmlDocument();
            // Load detects encoding from meta tags or uses default
            // Consider allowing explicit encoding specification if needed later.
            doc.Load(sourceStream);

            // Extract text nodes, preserving some structure might be useful later?
            // For now, just concatenate all inner text.
            var textBuilder = new StringBuilder();
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    // Decode HTML entities like &nbsp;
                    string decodedText = HtmlEntity.DeEntitize(node.InnerText);
                    if (!string.IsNullOrWhiteSpace(decodedText))
                    {
                        textBuilder.Append(decodedText.Trim()).Append(' '); // Add space between text blocks
                    }
                }
                // Consider handling block elements like <p>, <div> to add newlines later?
            }

            string extractedText = textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return ContentExtractionResult.Success(string.Empty, "Extracted text was empty or whitespace.");
            }

            return ContentExtractionResult.Success(extractedText);
        }
        catch (Exception ex)
        {
            // Log the exception details (consider adding ILogger later)
            return ContentExtractionResult.Failure($"Error extracting content from HTML: {ex.Message}", ex);
        }
        finally
        {
            // Ensure the stream is disposed if we created it, but here it's passed in.
            // The caller is responsible for disposing the stream.
        }
    }
}
