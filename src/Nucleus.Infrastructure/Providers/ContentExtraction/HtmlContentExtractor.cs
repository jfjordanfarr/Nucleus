using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions; // For NucleusConstants
using Nucleus.Abstractions.Extraction; // For IContentExtractor

namespace Nucleus.Infrastructure.Providers.ContentExtraction;

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
        NucleusConstants.MimeTypes.Textual.Html
    };

    /// <inheritdoc />
    public IReadOnlySet<string> SupportedMimeTypes => _supportedMimeTypes;

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return _supportedMimeTypes.Contains(mimeType);
    }

    /// <inheritdoc />
    public Task<ContentExtractionResult> ExtractContentAsync(
        Stream sourceStream,
        string mimeType,
        Uri? sourceUri = null)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        if (!SupportsMimeType(mimeType))
        {
            return Task.FromResult(ContentExtractionResult.UnsupportedMimeType(mimeType));
        }

        if (!sourceStream.CanRead)
        {
            return Task.FromResult(ContentExtractionResult.Failure("Source stream is not readable."));
        }

        try
        {
            var doc = new HtmlDocument();
            doc.Load(sourceStream);

            var textBuilder = new StringBuilder();
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    string? decodedText = HtmlEntity.DeEntitize(node.InnerText);
                    if (!string.IsNullOrWhiteSpace(decodedText))
                    {
                        textBuilder.Append(decodedText.Trim()).Append(' ');
                    }
                }
            }

            string extractedText = textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Task.FromResult(ContentExtractionResult.Success(string.Empty, "Extracted text was empty or whitespace."));
            }

            return Task.FromResult(ContentExtractionResult.Success(extractedText));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ContentExtractionResult.Failure($"Error extracting content from HTML: {ex.Message}", ex));
        }
        finally
        {
        }
    }
}
