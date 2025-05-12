// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines the contract for components responsible for retrieving the actual content
/// of an artifact based on an ArtifactReference. Implementations are specific to the
/// source system or protocol (e.g., local file system, Microsoft Graph, HTTP).
/// Crucially, these providers operate ephemerally and do not store content long-term.
/// <seealso cref="Models.ArtifactReference"/>
/// <seealso cref="Models.ArtifactContent"/>
/// <seealso cref="../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
/// <seealso cref="../../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md"/>
/// <seealso cref="../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md"/>
/// <seealso cref="../../Docs/Architecture/Processing/ARCHITECTURE_INGESTION_FILECOLLECTIONS.md"/>
/// <seealso cref="../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// <seealso cref="../../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md"/>
/// <seealso cref="../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
/// <seealso cref="../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#341-iartifactprovidercs"/>
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
    /// </returns>
    Task<ArtifactContent?> GetContentAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default);
}
