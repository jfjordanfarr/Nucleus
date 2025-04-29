// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Adapters.Teams;

/// <summary>
/// Implements the <see cref="IPlatformNotifier"/> interface for Microsoft Teams.
/// Uses the Bot Framework SDK to send notifications and acknowledgements back to the user.
/// See: d:\Projects\Nucleus\Docs\Architecture\ClientAdapters\ARCHITECTURE_ADAPTERS_TEAMS.md
/// </summary>
public class TeamsNotifier : IPlatformNotifier
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly string _botAppId;
    private readonly ILogger<TeamsNotifier> _logger;
    private readonly IConfiguration _configuration;

    public string SupportedPlatformType => "Teams";

    public TeamsNotifier(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ILogger<TeamsNotifier> logger)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Retrieve the AppId from configuration
        _botAppId = _configuration["MicrosoftAppId"] ?? throw new ArgumentNullException("MicrosoftAppId", "Configuration value for 'MicrosoftAppId' cannot be null.");
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? SentMessageId, string? Error)> SendNotificationAsync(
        string conversationId,
        string messageText,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            _logger.LogError("Cannot send notification: conversationId is null or empty.");
            return (false, null, "Conversation ID cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogWarning("Sending empty message to conversation {ConversationId}", conversationId);
            messageText = string.Empty; // Avoid sending null
        }

        try
        {
            // Construct the conversation reference needed to address the message
            var conversationReference = new ConversationReference
            {
                Bot = new ChannelAccount { Id = _botAppId },
                Conversation = new ConversationAccount { Id = conversationId },
                // ServiceUrl is crucial for proactive messaging. It must be obtained reliably.
                // Typically, it's captured from an incoming activity and stored alongside the conversation ID.
                // If this notifier is only used for *immediate* replies within the same TurnContext,
                // the ServiceUrl might be implicitly handled by the framework when using TurnContext.SendActivityAsync.
                // However, for sending messages *outside* the original turn (truly proactive),
                // the ServiceUrl from the original interaction MUST be stored and provided here.
                // TODO: Determine how ServiceUrl will be managed. Likely requires storing full ConversationReference.
            };

            ResourceResponse? resourceResponse = null;

            // The BotCallback function is executed by ContinueConversationAsync
            async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                var activity = MessageFactory.Text(messageText);
                if (!string.IsNullOrWhiteSpace(replyToMessageId))
                {
                    activity.ReplyToId = replyToMessageId;
                }

                // Send the activity using the TurnContext provided by ContinueConversationAsync
                resourceResponse = await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            }

            // Use the adapter's ContinueConversationAsync method to send a proactive message.
            // This requires the BotAppId and the ConversationReference.
            // Note: This method creates a new TurnContext internally for the callback.
            if (_adapter is CloudAdapter cloudAdapter)
            {
                await cloudAdapter.ContinueConversationAsync(_botAppId, conversationReference, BotCallback, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("The provided IBotFrameworkHttpAdapter is not a CloudAdapter and does not support ContinueConversationAsync.");
                // Depending on desired behavior, you might throw or return an error tuple here.
                // For now, let the method continue, potentially returning null resourceResponse below.
            }

            // Check if resourceResponse is null, which might happen if the adapter wasn't a CloudAdapter or if sending failed silently
            if (resourceResponse == null)
            {                
                _logger.LogWarning("Sending notification to {ConversationId} potentially failed (no resource response).", conversationId);
                // Returning success=true here as the lack of ResourceResponse ID doesn't *always* mean failure in Bot Framework
                // but it's ambiguous. Consider adjusting based on observed behavior.
                return (true, null, null);
            }

            _logger.LogInformation("Successfully sent notification to conversation {ConversationId}. Message ID: {MessageId}", conversationId, resourceResponse.Id);
            return (true, resourceResponse.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to conversation {ConversationId}", conversationId);
            return (false, null, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendAcknowledgementAsync(
        string conversationId,
        string? replyToMessageId = null, // Teams typing indicator doesn't use replyToId
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            _logger.LogError("Cannot send acknowledgement: conversationId is null or empty.");
            return false;
        }

        try
        {
            // Construct the conversation reference needed to address the message
            var conversationReference = new ConversationReference
            {
                Bot = new ChannelAccount { Id = _botAppId },
                Conversation = new ConversationAccount { Id = conversationId },
                // ServiceUrl is crucial. See TODO in SendNotificationAsync.
                // If ServiceUrl isn't available here, this call will likely fail.
            };

            // The BotCallback function is executed by ContinueConversationAsync
            async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                var typingActivity = new Activity { Type = ActivityTypes.Typing };
                // replyToMessageId is not typically used for typing indicators in Teams

                // Send the typing activity using the TurnContext provided by ContinueConversationAsync
                await turnContext.SendActivityAsync(typingActivity, cancellationToken).ConfigureAwait(false);
            }

            // Use the adapter's ContinueConversationAsync method to send the typing indicator.
            if (_adapter is CloudAdapter cloudAdapter)
            {
                await cloudAdapter.ContinueConversationAsync(_botAppId, conversationReference, BotCallback, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Sent acknowledgement (typing indicator) to conversation {ConversationId}", conversationId);
                return true;
            }
            else
            {
                _logger.LogError("The provided IBotFrameworkHttpAdapter is not a CloudAdapter and does not support ContinueConversationAsync for sending acknowledgements.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send acknowledgement to conversation {ConversationId}", conversationId);
            return false;
        }
    }
}
