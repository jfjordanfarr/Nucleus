// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents a reference to an artifact located in an external source system.
/// This object is typically constructed by a client adapter and passed to the API service.
/// The API service uses this reference, along with an appropriate <see cref="IArtifactProvider"/>,
/// to ephemerally retrieve the artifact's content when needed for processing.
/// As described in the <seealso cref="../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>,
/// this record is a core data model that enables the system to handle diverse content sources
/// by abstracting the specifics of artifact retrieval.
/// </summary>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md"/>
/// <seealso cref="ArtifactContent"/>
/// <seealso cref="AdapterRequest"/>
/// <seealso cref="../../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md">Client Architecture</seealso>
/// <seealso cref="../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md">Storage Architecture - Reference-Based Access</seealso>
/// <seealso cref="../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
/// <seealso cref="../../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md"/>
/// <seealso cref="../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md"/>
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
