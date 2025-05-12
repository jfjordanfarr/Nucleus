using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Adapters;
using Microsoft.Extensions.Logging;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

/// <summary>
/// An implementation of <see cref="IPlatformNotifier"/> that performs no actual notification.
/// Used for platforms like the direct API where notification is handled via the primary response channel (e.g., HTTP response).
/// </summary>
public class NullPlatformNotifier : IPlatformNotifier
{
    private readonly ILogger<NullPlatformNotifier> _logger;

    public NullPlatformNotifier(ILogger<NullPlatformNotifier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string SupportedPlatformType => PlatformType.Api.ToString();

    /// <inheritdoc />
    public Task<(bool Success, string? SentMessageId, string? Error)> SendNotificationAsync(
        string conversationId,
        string messageText,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullPlatformNotifier: SendNotificationAsync called for ConversationId {ConversationId}, ReplyToMessageId {ReplyToMessageId}. No actual notification sent.", 
            conversationId, replyToMessageId ?? "N/A");
        
        // No-op: Return success tuple for the null implementation.
        return Task.FromResult<(bool Success, string? SentMessageId, string? Error)>((true, null, null));
    }

    /// <inheritdoc />
    public Task<bool> SendAcknowledgementAsync(
        string conversationId,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullPlatformNotifier: SendAcknowledgementAsync called for ConversationId {ConversationId}, ReplyToMessageId {ReplyToMessageId}. No actual acknowledgement sent.",
            conversationId, replyToMessageId ?? "N/A");
        
        // No-op: Return true for the null implementation.
        return Task.FromResult(true);
    }
}
