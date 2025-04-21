// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Standardized request payload sent from any client adapter (e.g., Teams, Console, Slack)
/// to the central Nucleus backend API endpoint (e.g., /api/ingestion/generic-trigger)
/// to initiate processing. This decouples the backend from platform-specific event structures.
/// See: ../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// </summary>
public record NucleusIngestionRequest(
    // --- Core Context ---

    /// <summary>
    /// REQUIRED: Identifies the source platform adapter.
    /// Used by the backend to select appropriate fetchers and notifiers.
    /// Examples: "Teams", "Discord", "Slack", "Email", "Console", "WebUpload".
    /// Must match the value provided by the corresponding <see cref="IPlatformAttachmentFetcher.SupportedPlatformType"/>
    /// and <see cref="IPlatformNotifier.SupportedPlatformType"/>.
    /// </summary>
    string PlatformType,

    /// <summary>
    /// REQUIRED: The platform-specific ID of the user who initiated the action or interaction.
    /// </summary>
    string OriginatingUserId,

    /// <summary>
    /// REQUIRED: The platform-specific ID of the conversation context (e.g., Channel ID,
    /// Direct Message ID, Thread ID, Console Session ID). Used for sending responses back.
    /// </summary>
    string OriginatingConversationId,

    /// <summary>
    /// Optional: The platform-specific ID of the message that triggered this request,
    /// if applicable. Useful for threading responses or associating with a specific message.
    /// </summary>
    string? OriginatingMessageId,

    /// <summary>
    /// REQUIRED: The timestamp (UTC) when the original event occurred on the platform.
    /// </summary>
    DateTimeOffset TimestampUtc,

    // --- Optional Triggering Info ---

    /// <summary>
    /// Optional: The textual content associated with the trigger, if any.
    /// This should typically be the cleaned text after removing mentions or bot commands.
    /// Examples: User's query, command parameters.
    /// </summary>
    string? TriggeringText,

    // --- Attachment/File References ---

    /// <summary>
    /// REQUIRED (but can be empty): A list of references to any attachments or files included
    /// in the original platform event. The actual content will be fetched later by the backend
    /// using an appropriate <see cref="IPlatformAttachmentFetcher"/>.
    /// </summary>
    List<PlatformAttachmentReference> AttachmentReferences,

    // --- Tracing/Metadata ---

    /// <summary>
    /// Optional: A correlation ID that can be used to trace this request across
    /// different services or log entries. If not provided by the adapter, the backend might generate one.
    /// </summary>
    string? CorrelationId, // Consider making non-nullable and generated if null? Default constructor needed then.

    /// <summary>
    /// Optional: A dictionary for any additional platform-specific context that doesn't fit
    /// the standard fields but might be useful for specialized backend logic or future enhancements.
    /// Use sparingly.
    /// </summary>
    Dictionary<string, string>? AdditionalPlatformContext
);
