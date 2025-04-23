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

namespace Nucleus.Adapters.Teams;

/// <summary>
/// Implements <see cref="IPlatformNotifier"/> for Microsoft Teams using the Bot Framework SDK.
/// Sends messages back to Teams conversations/channels.
/// See: [ARCHITECTURE_ADAPTERS_TEAMS.md](../../../../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS.md)
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
                // ServiceUrl = "YOUR_SERVICE_URL_HERE" // Placeholder - This needs a real value!
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
                // Handle the case where the adapter is not a CloudAdapter (log error, throw exception, etc.)
                _logger.LogError("The provided IBotFrameworkHttpAdapter is not a CloudAdapter and does not support ContinueConversationAsync.");
                // Depending on desired behavior, you might throw or return an error tuple here.
                // For now, let the method continue, potentially returning null resourceResponse below.
                resourceResponse = null; // Ensure resourceResponse is null if conversation couldn't continue
            }

            if (resourceResponse != null)
            {
                _logger.LogInformation("Successfully sent message {MessageId} to conversation {ConversationId}", resourceResponse.Id, conversationId);
                return (true, resourceResponse.Id, null);
            }
            else
            {
                // This scenario could happen if the callback completed without SendActivityAsync returning a response,
                // or if ContinueConversationAsync itself had an issue not surfaced as an exception.
                _logger.LogWarning("Notification possibly sent to conversation {ConversationId}, but no ResourceResponse was captured.", conversationId);
                // We might consider this a success if no exception was thrown, but it's ambiguous.
                return (true, null, "Message sent but no confirmation ID received.");
            }
        }
        catch (ErrorResponseException ex) // Catch specific Bot Framework errors
        {
            _logger.LogError(ex, "Bot Framework error sending notification to conversation {ConversationId}. Status Code: {StatusCode}. Body: {ErrorBody}",
                             conversationId, ex.Response.StatusCode, ex.Response.Content);
            return (false, null, $"Bot Framework error: {ex.Response.ReasonPhrase} - {ex.Body?.Error?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General failure sending notification to conversation {ConversationId}. Error: {ErrorMessage}", conversationId, ex.Message);
            return (false, null, $"Failed to send notification: {ex.Message}");
        }
    }
}
