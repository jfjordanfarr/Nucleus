// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines a contract for sending notifications or responses back to the originating platform.
/// Implementations will use the appropriate platform SDKs (e.g., Bot Framework, Slack API).
/// See: ../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md
/// </summary>
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

    // Potential future additions:
    // - Sending typing indicators
    // - Updating existing messages
    // - Sending structured messages/cards
}
