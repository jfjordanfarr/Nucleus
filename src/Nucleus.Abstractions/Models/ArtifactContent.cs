using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents the retrieved content of an artifact along with essential details.
/// This object is returned by <see cref="IArtifactProvider.GetContentAsync"/>.
/// It is disposable to ensure the underlying stream is properly managed.
/// See: ../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md
/// </summary>
/// <seealso cref="IArtifactProvider"/>
/// <seealso cref="ArtifactReference"/>
/// <seealso cref="../../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md"/>
public sealed class ArtifactContent : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the original reference used to fetch this content.
    /// Provides context about where the content came from.
    /// </summary>
    public ArtifactReference OriginalReference { get; }

    /// <summary>
    /// Gets the stream containing the artifact's content.
    /// The stream may be positioned at the beginning or wherever the provider left it.
    /// The consumer is responsible for reading and potentially disposing of the stream
    /// via this class's Dispose method.
    /// </summary>
    public Stream? ContentStream { get; }

    /// <summary>
    /// Gets the detected or specified content type (MIME type) of the artifact.
    /// e.g., "text/plain", "application/pdf", "image/jpeg".
    /// May be null if the type could not be determined.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the character encoding detected or specified for text-based content.
    /// e.g., Encoding.UTF8, Encoding.ASCII.
    /// May be null for binary content or if encoding is unknown.
    /// </summary>
    public Encoding? TextEncoding { get; }

    /// <summary>
    /// Gets optional additional metadata provided by the IArtifactProvider.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactContent"/> class.
    /// </summary>
    /// <param name="originalReference">The reference that sourced this content.</param>
    /// <param name="contentStream">The stream containing the content.</param>
    /// <param name="contentType">The MIME type of the content (optional).</param>
    /// <param name="textEncoding">The text encoding, if applicable (optional).</param>
    /// <param name="metadata">Additional metadata (optional).</param>
    public ArtifactContent(
        ArtifactReference originalReference,
        Stream? contentStream,
        string? contentType = null,
        Encoding? textEncoding = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(originalReference);

        OriginalReference = originalReference;
        ContentStream = contentStream; // Allow null stream if content couldn't be fetched/doesn't exist
        ContentType = contentType;
        TextEncoding = textEncoding;
        Metadata = metadata;
    }

    /// <summary>
    /// Asynchronously reads the entire content stream as a string, using the specified or detected encoding.
    /// Resets the stream position to the beginning before reading and leaves it at the end.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content as a string, or null if the stream is null or empty.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the object or stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">Thrown if the stream does not support reading or seeking.</exception>
    public async Task<string?> ReadAsStringAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed || (ContentStream != null && !ContentStream.CanRead), this);

        if (ContentStream == null || ContentStream.Length == 0)
        {
            return null;
        }

        if (!ContentStream.CanSeek)
        {
            throw new NotSupportedException("The content stream does not support seeking, cannot guarantee reading from the beginning.");
        }

        ContentStream.Seek(0, SeekOrigin.Begin); // Ensure reading from the start

        // Use provided encoding, fallback to UTF8, then let StreamReader detect
        var encodingToUse = TextEncoding ?? Encoding.UTF8;
        // Pass detectEncodingFromByteOrderMarks: true and leaveOpen: true
        using var reader = new StreamReader(ContentStream, encodingToUse, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Disposes the underlying ContentStream if it exists and is disposable.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this); // Suppress finalization
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                ContentStream?.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null

            _disposed = true;
        }
    }

     // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
     // ~ArtifactContent()
     // {
     //     Dispose(disposing: false);
     // }
}
