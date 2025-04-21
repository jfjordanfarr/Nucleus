// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json; // Requires System.Net.Http.Json package
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options; // For configuration
using Nucleus.Abstractions;
using System; // For DateTimeOffset
using Newtonsoft.Json.Linq; // For parsing attachment content
using Microsoft.Extensions.Configuration; // Added
using Nucleus.Abstractions.Models; // Added for NucleusIngestionRequest, PlatformAttachmentReference

namespace Nucleus.Adapters.Teams
{
    /// <summary>
    /// Handles incoming message activities from Microsoft Teams.
    /// Validates mentions, extracts relevant information, constructs a <see cref="NucleusIngestionRequest"/>,
    /// sends it to the backend API, and provides an initial acknowledgement using <see cref="IPlatformNotifier"/>.
    /// See: Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
    /// </summary>
    public class TeamsAdapterBot : ActivityHandler
    {
        private readonly ILogger<TeamsAdapterBot> _logger;
        private readonly IPlatformNotifier _platformNotifier;
        private readonly IMessageQueuePublisher<NucleusIngestionRequest> _messageQueuePublisher; // Inject the publisher
        private readonly TeamsAdapterConfiguration _config;
        private readonly IConfiguration _configuration; // Added

        public TeamsAdapterBot(
            ILogger<TeamsAdapterBot> logger,
            IPlatformNotifier platformNotifier,
            IMessageQueuePublisher<NucleusIngestionRequest> messageQueuePublisher,
            IOptions<TeamsAdapterConfiguration> configOptions,
            IConfiguration configuration) // Added configuration
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _platformNotifier = platformNotifier ?? throw new ArgumentNullException(nameof(platformNotifier));
            _messageQueuePublisher = messageQueuePublisher ?? throw new ArgumentNullException(nameof(messageQueuePublisher));
            _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration)); // Added assignment

            // Ensure API endpoint is configured
            if (string.IsNullOrWhiteSpace(_config.NucleusApiEndpoint))
            {
                _logger.LogError("NucleusApiEndpoint is not configured in TeamsAdapterConfiguration.");
                throw new InvalidOperationException("Nucleus API endpoint is not configured.");
            }
            _logger.LogInformation("TeamsAdapterBot initialized.");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            _logger.LogInformation("Message received (ActivityId: {ActivityId}, ConversationId: {ConversationId}): {Text}", activity.Id, activity.Conversation.Id, activity.Text);

            // Check if the bot was mentioned
            var mention = activity.GetMentions()?.FirstOrDefault(m => m.Mentioned.Id == activity.Recipient.Id);

            if (mention == null)
            {
                _logger.LogInformation("Bot was not mentioned. Ignoring message (ActivityId: {ActivityId}).", activity.Id);
                return; // Ignore messages where the bot is not mentioned
            }

            _logger.LogInformation("Bot was mentioned. Processing message (ActivityId: {ActivityId}).", activity.Id);

            // Remove the mention text from the activity text to clean it for command parsing
            var textWithoutMention = activity.RemoveRecipientMention()?.Trim() ?? string.Empty;

            // Construct the NucleusIngestionRequest
            var request = new NucleusIngestionRequest(
                PlatformType: "Teams",
                OriginatingUserId: activity.From.Id,
                OriginatingConversationId: activity.Conversation.Id,
                OriginatingMessageId: activity.Id,
                TimestampUtc: activity.Timestamp ?? DateTimeOffset.UtcNow,
                TriggeringText: textWithoutMention,
                AttachmentReferences: ExtractAttachmentReferences(activity, _logger),
                CorrelationId: activity.Id, // Use activity ID as correlation ID for now
                AdditionalPlatformContext: new Dictionary<string, string>
                {
                    { "TenantId", activity.Conversation.TenantId ?? "" },
                    { "TeamId", activity.ChannelData != null ? JObject.FromObject(activity.ChannelData)?["team"]?["id"]?.ToString() ?? "" : "" },
                    { "ChannelId", activity.ChannelData != null ? JObject.FromObject(activity.ChannelData)?["channel"]?["id"]?.ToString() ?? "" : "" }
                }
            );

            _logger.LogInformation("Constructed NucleusIngestionRequest for ActivityId: {ActivityId}", activity.Id);

            // Publish request to message queue
            try
            {
                string queueName = _configuration["NucleusIngestionQueueName"] ?? throw new InvalidOperationException("NucleusIngestionQueueName is not configured."); // Changed source
                _logger.LogInformation("Publishing NucleusIngestionRequest to message queue '{QueueName}'. CorrelationID: {CorrelationId}", queueName, request.CorrelationId);

                await _messageQueuePublisher.PublishAsync(request, queueName, cancellationToken);

                _logger.LogInformation("Successfully published NucleusIngestionRequest to message queue '{QueueName}'. CorrelationID: {CorrelationId}", queueName, request.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish NucleusIngestionRequest to message queue. CorrelationID: {CorrelationId}", request.CorrelationId);
                // Optionally notify user of backend submission failure
                await _platformNotifier.SendNotificationAsync(activity.Conversation.Id, "Sorry, I couldn't submit your request for processing right now.", activity.Id, cancellationToken);
            }

            // Send acknowledgement back to the user via IPlatformNotifier
            _logger.LogInformation("Sending processing acknowledgement for ActivityId: {ActivityId}", activity.Id);
            var ackResult = await _platformNotifier.SendNotificationAsync(
                conversationId: activity.Conversation.Id,
                messageText: "Got it! Processing your request...",
                replyToMessageId: activity.Id,
                cancellationToken: cancellationToken
            );

            if (!ackResult.Success)
            {
                _logger.LogError("Failed to send processing acknowledgement for ActivityId: {ActivityId}. Error: {Error}", activity.Id, ackResult.Error);
            }
        }

        /// <summary>
        /// Extracts file attachment information from the activity and creates PlatformAttachmentReference objects.
        /// </summary>
        private static List<PlatformAttachmentReference> ExtractAttachmentReferences(IMessageActivity activity, ILogger logger)
        {
            var references = new List<PlatformAttachmentReference>();
            if (activity.Attachments == null || !activity.Attachments.Any())
            {
                return references;
            }

            logger.LogInformation("Found {AttachmentCount} attachments for ActivityId: {ActivityId}. Parsing...", activity.Attachments.Count, activity.Id);

            foreach (var attachment in activity.Attachments)
            {
                // Teams file attachments often have ContentType 'application/vnd.microsoft.teams.file.download.info'
                // and the actual file info (including Graph IDs) in the Content property as JSON.
                if (attachment.ContentType == "application/vnd.microsoft.teams.file.download.info" && attachment.Content != null)
                {
                    logger.LogDebug("Parsing 'file.download.info' attachment: {AttachmentName}", attachment.Name);
                    try
                    {
                        var fileInfo = JObject.FromObject(attachment.Content);
                        string? downloadUrl = fileInfo["downloadUrl"]?.ToString();
                        string? uniqueId = fileInfo["uniqueId"]?.ToString(); // This is often the DriveItem ID
                        string? fileType = fileInfo["fileType"]?.ToString();
                        // ETag often contains site/drive/item IDs concatenated, might be parsable
                        string? etag = fileInfo["etag"]?.ToString();

                        if (string.IsNullOrWhiteSpace(uniqueId))
                        {
                            logger.LogWarning("Could not extract 'uniqueId' (DriveItem ID) from attachment content for {AttachmentName} in ActivityId: {ActivityId}. Skipping.", attachment.Name, activity.Id);
                            continue;
                        }

                        // Attempt to parse DriveId from ETag (this is fragile and based on observed patterns)
                        string? driveId = null;
                        if (!string.IsNullOrWhiteSpace(etag))
                        {
                            // Example ETag: "{GUID},1" for list item, or potentially containing Drive info
                            // Graph DriveItem ETag format: "a:{GUID}#RevisionNumber" (e.g., "a:OUVERRTFRERTYRERTYQERTMTQFAQ#1")
                            // SharePoint item ETag format: "\"{GUID},RevisionNumber\"" (e.g., "\"{E312DB6F-3105-4935-8740-1C5D0807618F},3\"")
                            // Sometimes the 'uniqueId' itself might be in a format like 'driveId!itemId'
                            // This parsing logic is highly unreliable and likely needs a proper Graph call or better context.
                            logger.LogWarning("ETag parsing for DriveId is unreliable. ETag: {ETag}, UniqueId: {UniqueId}. DriveId will likely be missing.", etag, uniqueId);
                            // Placeholder for potential future ETag parsing logic
                        }

                        var context = new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(driveId))
                        {
                            context.Add("DriveId", driveId);
                        }
                        else
                        {
                            // Log if DriveId couldn't be determined - the fetcher will need it.
                            logger.LogWarning("DriveId could not be determined for attachment {AttachmentName} (DriveItemId: {DriveItemId}). The TeamsGraphFileFetcher might fail unless it can resolve the DriveId from configuration or other context.", attachment.Name, uniqueId);
                        }

                        references.Add(new PlatformAttachmentReference(
                            PlatformSpecificId: uniqueId, // DriveItem ID
                            FileName: attachment.Name,
                            ContentType: null, // ContentType in main attachment might be misleading, Graph will know.
                            SizeBytes: null, // Not easily available here
                            DownloadUrlHint: downloadUrl,
                            PlatformContext: context.Any() ? context : null
                        ));
                        logger.LogInformation("Successfully parsed attachment reference: Name='{Name}', DriveItemId='{DriveItemId}', DriveId='{DriveId}'", attachment.Name, uniqueId, driveId ?? "<Not Found>");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse attachment content JSON for {AttachmentName} in ActivityId: {ActivityId}", attachment.Name, activity.Id);
                    }
                }
                else
                {
                    // Log other attachment types but don't create references for now
                    logger.LogInformation("Skipping non-file attachment: Name='{Name}', ContentType='{ContentType}'", attachment.Name, attachment.ContentType);
                }
            }

            return references;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome to the Nucleus OmniRAG Teams Adapter Prototype!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    _logger.LogInformation("Member added: {MemberName} ({MemberId})", member.Name, member.Id);
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
