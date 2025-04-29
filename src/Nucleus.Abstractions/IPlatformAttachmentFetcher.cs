// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines a contract for fetching the actual content of a platform-specific file/attachment reference.
/// Implementations will use the appropriate platform SDKs (e.g., MS Graph, File IO) based on the
/// context provided in the <see cref="PlatformAttachmentReference"/>.
/// See: ../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// <seealso cref="../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </summary>
public interface IPlatformAttachmentFetcher
{
    /// <summary>
    /// Gets the platform type this fetcher supports (e.g., "Teams", "Console", "Discord").
    /// This value MUST match the <see cref="NucleusIngestionRequest.PlatformType"/> for the
    /// requests it's intended to handle. Used for resolving the correct implementation via DI.
    /// </summary>
    string SupportedPlatformType { get; }

    /// <summary>
    /// Asynchronously retrieves the content stream for a given platform attachment reference.
    /// </summary>
    /// <param name="reference">The reference containing platform-specific IDs and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A Task representing the asynchronous operation, containing a tuple:
    /// - <c>FileStream</c>: A readable Stream with the attachment's content. The caller is responsible for disposing this stream.
    /// - <c>FileName</c>: The determined filename.
    /// - <c>ContentType</c>: The determined MIME content type.
    /// - <c>Error</c>: An error message if retrieval failed, otherwise null.
    /// If retrieval fails fundamentally (e.g., cannot connect), the FileStream will be null.
    /// </returns>
    Task<(Stream? FileStream, string? FileName, string? ContentType, string? Error)> GetAttachmentStreamAsync(
        PlatformAttachmentReference reference,
        CancellationToken cancellationToken = default);
}
