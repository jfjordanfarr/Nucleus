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
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md"/>
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
    /// Optional: The platform-specific ID of the message this interaction is replying to, if applicable.
    /// Important for maintaining conversation context, especially for async replies.
    /// </summary>
    string? OriginatingMessageId, // Corrected: this was OriginatingReplyToMessageId before, which is duplicative. This should be the primary message ID.

    /// <summary>
    /// REQUIRED: The ID of the Persona that has been resolved to handle this request.
    /// This is determined by the API layer before queueing.
    /// </summary>
    string ResolvedPersonaId,

    /// <summary>
    /// Optional: The UTC timestamp when the original event (e.g., message creation) occurred on the source platform.
    /// Defaults to UtcNow if not provided.
    /// </summary>
    DateTimeOffset? TimestampUtc,

    /// <summary>
    /// Optional: The primary text query or command from the user.
    /// Can be null if the interaction is purely artifact-based (e.g., file upload with no text).
    /// </summary>
    string? QueryText,

    /// <summary>
    /// Optional: A list of references to artifacts (files, URLs, etc.) associated with the request.
    /// These references will be used by <see cref="IArtifactProvider"/> implementations to fetch content.
    /// </summary>
    List<ArtifactReference>? ArtifactReferences,

    /// <summary>
    /// Optional: A unique identifier for tracing the request through various systems and logs.
    /// If not provided by the client, the API should generate one.
    /// </summary>
    string? CorrelationId,

    /// <summary>
    /// Optional: Additional key-value metadata from the platform adapter or client.
    /// Can be used for platform-specific information or custom data.
    /// </summary>
    Dictionary<string, string>? Metadata,

    /// <summary>
    /// Optional: The ID of the tenant this request is associated with.
    /// This is crucial for data partitioning and access control in multi-tenant deployments.
    /// </summary>
    string? TenantId

    // --- Control & Status Flags (Future Use) ---
    // Example: bool RequestImmediateResponse = false;
    // Example: ProcessingPriority Priority = ProcessingPriority.Normal;
);
