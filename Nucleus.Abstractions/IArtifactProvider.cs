// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines a contract for retrieving the content of an artifact based on its reference.
/// Implementations will handle specific artifact types (e.g., local files, SharePoint items).
/// </summary>
public interface IArtifactProvider
{
    /// <summary>
    /// Gets the type of artifact this provider handles (e.g., "localfile", "sharepoint").
    /// Used by a factory or resolver to select the correct provider.
    /// </summary>
    string SupportedArtifactType { get; }

    /// <summary>
    /// Asynchronously retrieves the content of the specified artifact as a stream.
    /// </summary>
    /// <param name="reference">The reference to the artifact to retrieve.</param>
    /// <returns>A Task representing the asynchronous operation, containing a Stream with the artifact's content, or null if the artifact cannot be retrieved.</returns>
    Task<Stream?> GetArtifactContentAsync(ArtifactReference reference);
}
