// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines the contract for services that can retrieve the content of an artifact
/// based on its reference information. Implemented by adapters or specific data source connectors.
/// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: ../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ARTIFACT_PROVIDERS.md
/// </summary>
public interface IArtifactProvider
{
    /// <summary>
    /// Gets the collection of reference type strings (e.g., "local_file_path", "msgraph", "url")
    /// that this provider is responsible for handling.
    /// Comparisons by the manager should be case-insensitive.
    /// </summary>
    IEnumerable<string> SupportedReferenceTypes { get; }

    /// <summary>
    /// Retrieves the artifact content based on the provided reference.
    /// </summary>
    /// <param name="reference">The reference identifying the artifact to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="ArtifactContent"/> object containing the stream and metadata,
    /// or null if the artifact could not be retrieved or does not exist.
    /// The provider should log errors internally rather than throwing exceptions here.
    /// </summary>
    Task<ArtifactContent?> GetContentAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default);
}
