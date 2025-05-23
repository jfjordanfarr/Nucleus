// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models; // For NucleusIngestionRequest XML comment reference

namespace Nucleus.Abstractions.Adapters; // << MODIFIED NAMESPACE

/// <summary>
/// Defines a contract for sending notifications or responses back to the originating platform.
/// Implementations will use the appropriate platform SDKs (e.g., Bot Framework, Slack API).
/// <seealso cref="../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md#iplatformnotifier"/>
/// <seealso href="../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md">Processing Architecture - Shared Interfaces (IPlatformNotifier)</seealso>
/// <seealso href="../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md">Adapter Interfaces</seealso>
/// <seealso cref="../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md"/>
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#344-iplatformnotifiercs"/> // TODO: This link might need updating if IPlatformNotifier's primary doc location changes definitively to 01_ARCHITECTURE_PROCESSING.md
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for sending messages back to the originating platform adapter (e.g., Teams, Slack, Console) asynchronously.
/// The specific implementation is resolved by the `OrchestrationService` based on the `PlatformType` from the original `NucleusIngestionRequest`.
/// </remarks>
public interface IPlatformNotifier
{
    /// <summary>
    /// Gets the platform type this notifier supports (e.g., "Teams", "Console", "Discord").
    /// This value MUST match the <see cref="NucleusIngestionRequest.PlatformType"/> for the
    /// requests it's intended to handle. Used for resolving the correct implementation via DI.
    /// </summary>
    string SupportedPlatformType { get; }

    /// <summary>
    /// Asynchronously sends a notification message back to the originating platform context.
    /// </summary>
    /// <param name="conversationId">The platform-specific conversation ID where the message should be sent
    /// (e.g., Teams Channel ID, Console Session ID).</param>
    /// <param name="messageText">The content of the message to send.</param>
    /// <param name="replyToMessageId">Optional. The platform-specific ID of the message to reply to, enabling threading if supported by the platform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A Task representing the asynchronous operation, containing a tuple:
    /// - <c>Success</c>: A boolean indicating if the message was sent successfully.
    /// - <c>SentMessageId</c>: The platform-specific ID of the message that was sent, if applicable and available.
    /// - <c>Error</c>: An error message if sending failed, otherwise null.
    /// </returns>
    Task<(bool Success, string? SentMessageId, string? Error)> SendNotificationAsync(
        string conversationId,
        string messageText,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a simple acknowledgement or typing indicator to the platform.
    /// This is useful for providing immediate feedback for potentially long-running operations.
    /// The implementation might be a no-op for platforms where this isn't applicable (e.g., Console).
    /// </summary>
    /// <param name="conversationId">The platform-specific conversation ID.</param>
    /// <param name="replyToMessageId">Optional. The platform-specific ID of the message being acknowledged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation. Returns true on success, false on failure.</returns>
    Task<bool> SendAcknowledgementAsync(
        string conversationId,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default);

    // Potential future additions:
    // - Sending typing indicators
    // - Updating existing messages
    // - Sending structured messages/cards
}
