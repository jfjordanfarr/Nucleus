// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents a request sent from a Client Adapter to the Nucleus API Service,
/// typically encapsulating a user query or command and associated context.
/// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// </summary>
/// <param name="PlatformType">The type of platform originating the request (e.g., "Teams", "Console").</param>
/// <param name="ConversationId">A unique identifier for the conversation context.</param>
/// <param name="UserId">A unique identifier for the user making the request.</param>
/// <param name="QueryText">The primary text query or command from the user.</param>
/// <param name="MessageId">Optional: The ID of the specific message initiating this request.</param>
/// <param name="ReplyToMessageId">Optional: The ID of the message this request is replying to.</param>
/// <param name="ArtifactReferences">Optional: References to any artifacts (files, etc.) associated with the request.</param>
/// <param name="Metadata">Optional: Additional key-value metadata from the platform adapter.</param>
public record AdapterRequest(
    string PlatformType,
    string ConversationId,
    string UserId,
    string QueryText,
    string? MessageId = null,
    string? ReplyToMessageId = null,
    List<ArtifactReference>? ArtifactReferences = null,
    Dictionary<string, string>? Metadata = null);
