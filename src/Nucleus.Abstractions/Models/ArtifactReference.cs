// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents a reference to an artifact provided by a client adapter.
/// This contains the minimal information needed for the ApiService to identify
/// and potentially retrieve the artifact's content or metadata.
/// See: ../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: ../../Docs/Architecture/Api/03_API_DTOs.md
/// </summary>
public record ArtifactReference(
    /// <summary>
    /// The unique identifier for the artifact within its source system (e.g., file path, Graph API item ID, URL).
    /// This will be used to construct the SourceIdentifier in ArtifactMetadata.
    /// </summary>
    string ReferenceId,

    /// <summary>
    /// The type of the reference (e.g., "file", "url", "teams_attachment").
    /// This is used by the DefaultPersonaManager to find a compatible IArtifactProvider.
    /// Comparison should generally be case-insensitive.
    /// </summary>
    string ReferenceType,

    /// <summary>
    /// The specific URI pointing to the artifact in its source system.
    /// </summary>
    System.Uri SourceUri,

    /// <summary>
    /// The ID of the tenant owning or associated with this artifact.
    /// </summary>
    string TenantId,

    /// <summary>
    /// The original filename, if applicable and known by the adapter.
    /// </summary>
    string? FileName = null,

    /// <summary>
    /// The MIME type, if known by the adapter.
    /// </summary>
    string? MimeType = null);
