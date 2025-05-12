// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Adapters;

namespace Nucleus.Abstractions.Models.ApiContracts;

/// <summary>
/// Record representing a request submitted by a client adapter to the Nucleus API.
/// This serves as the primary data transfer object for initiating interaction processing.
/// It includes core context, source information, and optional artifacts.
/// </summary>
/// <remarks>
/// This record is also used as the message payload when queueing interactions for asynchronous processing.
/// See related architecture documents for data flow and processing logic.
/// </remarks>
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md"/>
/// <seealso cref="../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md"/>
public record NucleusIngestionRequest(
    // --- Core Context ---

    /// <summary>
    /// REQUIRED: Identifies the source platform adapter.
    /// Used by the backend to select appropriate fetchers and notifiers.
    /// Examples: "Teams", "Discord", "Slack", "Email", "Console", "WebUpload".
    /// Must match the value provided by the corresponding <see cref="IPlatformAttachmentFetcher.SupportedPlatformType"/>
    /// and <see cref="IPlatformNotifier.SupportedPlatformType"/>.
    /// </summary>
    PlatformType PlatformType,

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
    /// Optional: The platform-specific ID of the message this interaction is replying to, if applicable.
    /// Important for maintaining conversation context, especially for async replies.
    /// </summary>
    string? OriginatingReplyToMessageId,

    /// <summary>
    /// Optional: The platform-specific ID of the message that triggered this request,
    /// if applicable. Useful for threading responses or associating with a specific message.
    /// </summary>
    string? OriginatingMessageId,

    /// <summary>
    /// Optional but important for async processing: The canonical Persona ID resolved
    /// for this interaction by the IPersonaResolver during the initial API request.
    /// </summary>
    string? ResolvedPersonaId,

    /// <summary>
    /// REQUIRED: The timestamp (UTC) when the original event occurred on the platform.
    /// </summary>
    DateTimeOffset TimestampUtc,

    // --- Optional Triggering Info ---

    /// <summary>
    /// Optional: The primary textual query or content from the user's message, potentially
    /// after removing mentions or bot commands if done by the adapter.
    /// </summary>
    string? QueryText,

    // --- Attachment/File References ---

    /// <summary>
    /// Optional: A list of references to artifacts (files, links, etc.) provided
    /// in the original interaction. These are direct references usable by the backend's
    /// <see cref="IArtifactProvider"/> implementations.
    /// </summary>
    List<ArtifactReference>? ArtifactReferences,

    // --- Tracing/Metadata ---

    /// <summary>
    /// Optional: A correlation ID that can be used to trace this request across
    /// different services or log entries. If not provided by the adapter, the backend might generate one.
    /// </summary>
    string? CorrelationId, // Consider making non-nullable and generated if null? Default constructor needed then.

    /// <summary>
    /// Optional: A dictionary for any additional adapter-specific context or metadata
    /// that doesn't fit the standard fields but might be useful for specialized backend logic or future enhancements.
    /// Use sparingly.
    /// </summary>
    Dictionary<string, string>? Metadata
);
