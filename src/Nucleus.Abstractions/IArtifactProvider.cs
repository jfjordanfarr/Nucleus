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
/// Defines the contract for services that can retrieve the content of an artifact from a specific source system.
/// Implementations of this interface are responsible for understanding how to fetch artifacts
/// based on a given <see cref="Models.ArtifactReference"/> and its <see cref="Models.ArtifactReference.ReferenceType"/>.
/// This abstraction is a key component of the Nucleus architecture, enabling pluggable content sources,
/// as detailed in <seealso cref="../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>.
/// </summary>
/// <seealso cref="Models.ArtifactReference"/>
/// <seealso cref="Models.ArtifactContent"/>
/// <seealso cref="../../../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md"/>
/// <seealso cref="../../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md">Storage Architecture - Reference-Based Access</seealso>
/// <seealso cref="../../../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md"/>
/// <seealso cref="../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
/// <seealso cref="../../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#341-iartifactprovidercs"/>
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
