// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions;

/// <summary>
/// Represents a reference to an artifact (e.g., a file, a message) that can be 
/// retrieved by an appropriate IArtifactProvider.
/// </summary>
public record ArtifactReference(
    /// <summary>
    /// Type of the artifact source (e.g., "localfile", "sharepoint", "teamsmessage", "url").
    /// </summary>
    string Type,

    /// <summary>
    /// Unique identifier for the artifact within its source type.
    /// (e.g., Absolute file path, SharePoint drive/item ID, message ID, URL).
    /// </summary>
    string Identifier,

    /// <summary>
    /// Optional. Additional metadata like a user-friendly name or original filename.
    /// </summary>
    string? Name = null
)
{
    /// <summary>
    /// Attempts to parse a string representation into an ArtifactReference.
    /// Expects the format "type:identifier".
    /// </summary>
    /// <param name="potentialReference">The string to parse.</param>
    /// <param name="artifactReference">The resulting ArtifactReference if parsing succeeds.</param>
    /// <returns>True if parsing was successful, false otherwise.</returns>
    public static bool TryParse(string? potentialReference, out ArtifactReference? artifactReference)
    {
        artifactReference = null;
        if (string.IsNullOrWhiteSpace(potentialReference))
        {
            return false;
        }

        var parts = potentialReference.Split(':', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false; // Format must be "type:identifier" with non-empty parts
        }

        artifactReference = new ArtifactReference(Type: parts[0], Identifier: parts[1]);
        return true;
    }
}
