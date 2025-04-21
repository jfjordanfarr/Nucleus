// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions;

/// <summary>
/// Represents a reference to an attachment or file originating from a specific platform.
/// Contains enough information for the backend processing logic to request the actual content
/// from the appropriate <see cref="IPlatformAttachmentFetcher"/> implementation later.
/// This record standardizes how adapters describe external artifacts without embedding their content.
/// See: ../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// </summary>
public record PlatformAttachmentReference(
    /// <summary>
    /// REQUIRED: The unique identifier for this artifact on the originating platform.
    /// Examples: Graph DriveItem ID, Slack File ID, Discord Attachment ID, local file path for Console.
    /// </summary>
    string PlatformSpecificId,

    /// <summary>
    /// Optional but helpful: The original filename as presented on the platform.
    /// </summary>
    string? FileName,

    /// <summary>
    /// Optional: The MIME type of the file, if known by the adapter.
    /// </summary>
    string? ContentType,

    /// <summary>
    /// Optional: The size of the file in bytes, if known by the adapter.
    /// </summary>
    long? SizeBytes,

    /// <summary>
    /// Optional: A direct download URL hint, if easily available from the platform.
    /// This might be used by the fetcher as an optimization but shouldn't be solely relied upon.
    /// Examples: Discord CDN URL, Graph `@microsoft.graph.downloadUrl`.
    /// </summary>
    System.Uri? DownloadUrlHint,

    /// <summary>
    /// Optional: A dictionary containing any additional context required by the specific
    /// <see cref="IPlatformAttachmentFetcher"/> to retrieve the content.
    /// Examples: For Teams/Graph, this might include {"DriveId": "...", "SiteId": "..."}.
    /// For local files, this might be empty.
    /// </summary>
    Dictionary<string, string>? PlatformContext
);
