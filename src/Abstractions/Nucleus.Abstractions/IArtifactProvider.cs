// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines the contract for services that can retrieve the content of an artifact
/// based on its reference information. Implemented by adapters or specific data source connectors.
/// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md
/// </summary>
public interface IArtifactProvider
{
    /// <summary>
    /// Gets the type of platform this provider supports (e.g., "Teams", "Console").
    /// Used for routing retrieval requests.
    /// </summary>
    string SupportedPlatformType { get; }

    /// <summary>
    /// Retrieves the content stream for a given artifact reference.
    /// </summary>
    /// <param name="reference">The reference identifying the artifact to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing:
    /// - Stream: The content stream of the artifact (or null if not found/error).
    /// - FileName: The original name of the artifact file (or null).
    /// - MimeType: The MIME type of the artifact (or null).
    /// - Error: An error message if retrieval failed (null on success).
    /// </returns>
    Task<(Stream? Stream, string? FileName, string? MimeType, string? Error)> GetArtifactStreamAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default);
}
