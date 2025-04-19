// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Nucleus.Abstractions;

/// <summary>
/// Standardized request structure sent from any client adapter to the Nucleus API Service.
/// </summary>
public record AdapterRequest(
    /// <summary>
    /// Identifier for the user or session initiating the request.
    /// This might be a user ID, session ID, or conversation ID depending on the adapter.
    /// </summary>
    string SessionId,

    /// <summary>
    /// The primary text input from the user (e.g., query, command, message content).
    /// </summary>
    string InputText,

    /// <summary>
    /// Optional. References to artifacts (files, messages) relevant to this request.
    /// </summary>
    List<ArtifactReference>? Artifacts = null,

     /// <summary>
    /// Optional. An identifier representing the broader context, potentially linking
    /// multiple requests (e.g., the SharePoint path from a 'list files' command, 
    /// used later in a 'summarize file X' command from that list).
    /// </summary>
    string? ContextSourceIdentifier = null
);
