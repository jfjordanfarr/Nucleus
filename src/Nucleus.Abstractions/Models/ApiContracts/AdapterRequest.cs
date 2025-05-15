// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models.ApiContracts;

/// <summary>
/// Represents a request originating from a specific client adapter (e.g., Teams, Console).
/// This is the primary DTO used for incoming interactions to the Nucleus API.
/// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/Ingestion/ARCHITECTURE_PROCESSING_INGESTION.md"/>
/// <seealso cref="../../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
/// <seealso cref="../../../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md"/>
/// <seealso cref="../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
/// <seealso cref="../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_EMAIL.md"/>
/// </summary>
/// <param name="PlatformType">The type of platform originating the request (e.g., "Teams", "Console").</param>
/// <param name="ConversationId">A unique identifier for the conversation context.</param>
/// <param name="UserId">A unique identifier for the user making the request.</param>
/// <param name="QueryText">The primary text query or command from the user.</param>
/// <param name="MessageId">Optional: The ID of the specific message initiating this request.</param>
/// <param name="ReplyToMessageId">Optional: The ID of the message this request is replying to.</param>
/// <param name="ArtifactReferences">Optional: References to any artifacts (files, etc.) associated with the request.</param>
/// <param name="Metadata">Optional: Additional key-value metadata from the platform adapter.</param>
/// <param name="InteractionType">Identifies the type of interaction (e.g., "UserMessage", "SystemCommand").</param>
/// <param name="TenantId">Optional: The ID of the tenant this request is associated with.</param>
/// <param name="PersonaId">Optional: The ID of the persona to be used for this request.</param>
/// <param name="TimestampUtc">Optional: The UTC timestamp when the original event occurred.</param>
public record AdapterRequest(
    PlatformType PlatformType,
    string ConversationId,
    string UserId,
    string QueryText,
    string? MessageId = null,
    string? ReplyToMessageId = null,
    List<ArtifactReference>? ArtifactReferences = null,
    Dictionary<string, string>? Metadata = null,
    string? InteractionType = null,
    string? TenantId = null,
    string? PersonaId = null,
    DateTimeOffset? TimestampUtc = null);
