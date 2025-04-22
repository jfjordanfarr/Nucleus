// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents a reference to a unique piece of source content (artifact) within a specific platform.
/// This is used by adapters to identify content and by the core system to request retrieval.
/// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md
/// </summary>
/// <param name="PlatformType">The type of platform the artifact originates from (e.g., "Teams", "Console", "SharePoint").</param>
/// <param name="ArtifactId">A unique identifier for the artifact within its platform (e.g., file ID, message ID).</param>
/// <param name="OptionalContext">Additional platform-specific context if needed for retrieval (e.g., SharePoint site/list ID, Teams channel ID).</param>
public record ArtifactReference(
    string PlatformType,
    string ArtifactId,
    Dictionary<string, string>? OptionalContext = null);
