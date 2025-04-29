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
using System; // For DateTimeOffset, ArgumentNullException, InvalidOperationException
using Newtonsoft.Json.Linq; // For parsing attachment content
using Microsoft.Extensions.Configuration; // Added
using Nucleus.Abstractions.Models; // Added for AdapterRequest/Response, ArtifactReference
using System.Text.Json; // Added for JsonSerializerOptions, JsonSerializerDefaults

namespace Nucleus.Infrastructure.Adapters.Teams
{
    /// <summary>
    /// Represents the core logic for the Nucleus Teams Adapter, handling incoming activities,
    /// interaction with the Nucleus backend API, and basic conversation management.
    /// It uses the Bot Framework SDK ActivityHandler as its base.
    /// See: Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
    /// </summary>
    public class TeamsAdapterBot : ActivityHandler
    {
        private readonly ILogger<TeamsAdapterBot> _logger;
        private readonly IPlatformNotifier _platformNotifier;
        private readonly HttpClient _httpClient; // Replaced MessageQueuePublisher
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly TeamsAdapterConfiguration _config; // Still useful for other potential config
        private readonly IConfiguration _configuration; // Still potentially useful

        /// <summary>
        /// The reference type used for Microsoft Graph drive items.
        /// </summary>
        private const string MsGraphReferenceType = "msgraph";

        public TeamsAdapterBot(
            ILogger<TeamsAdapterBot> logger,
            IPlatformNotifier platformNotifier,
            HttpClient httpClient, // Inject HttpClient
            IOptions<TeamsAdapterConfiguration> configOptions,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _platformNotifier = platformNotifier ?? throw new ArgumentNullException(nameof(platformNotifier));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Base address for HttpClient should be configured during DI registration
            if (_httpClient.BaseAddress == null)
            {
                 _logger.LogWarning("HttpClient BaseAddress is not set. Ensure it's configured during DI setup.");
                 // Depending on requirements, might throw or rely on relative path "/api/interaction/process"
            }

            _logger.LogInformation("TeamsAdapterBot initialized.");
        }

        /// <summary>
        /// Handles incoming message activities from Microsoft Teams.
        /// Validates mentions, extracts relevant information, constructs an <see cref="AdapterRequest"/>,
        /// sends it to the backend API via HTTP, and provides an initial acknowledgement using <see cref="IPlatformNotifier"/>.
        /// See: Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
        /// <seealso cref="d:\Projects\Nucleus\Docs\Architecture\ClientAdapters\Teams\ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md"/>
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// This method extracts user prompts, attachment references, and critical context like ReplyToActivityId,
        /// then constructs and sends the <see cref="AdapterRequest"/> to the Nucleus API.
        /// </remarks>
        /// <seealso cref="Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md"/>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(turnContext);
            var activity = turnContext.Activity;
            _logger.LogInformation("Message received (ActivityId: {ActivityId}, ConversationId: {ConversationId}): {Text}", activity.Id, activity.Conversation?.Id, activity.Text);

            // Check if the bot was mentioned
            var mention = activity.GetMentions()?.FirstOrDefault(m => m.Mentioned.Id == activity.Recipient.Id);

            if (mention == null)
            {
                _logger.LogInformation("Bot was not mentioned. Ignoring message (ActivityId: {ActivityId}).", activity.Id);
                return; // Ignore messages where the bot is not mentioned
            }

            _logger.LogInformation("Bot was mentioned. Processing message (ActivityId: {ActivityId}).", activity.Id);

            // Remove the mention text from the activity text
            string cleanedText = activity.Text;
            if (mention.Text != null)
            {
                cleanedText = cleanedText.Replace(mention.Text, string.Empty).Trim();
                _logger.LogDebug("Mention text removed. Cleaned text: '{CleanedText}'", cleanedText);
            }

            // Extract attachment references (returns List<ArtifactReference> now)
            var artifactReferences = ExtractAttachmentReferences(activity, _logger);

            // --- Create Standard AdapterRequest ---
            var request = new AdapterRequest(
                PlatformType: "Teams", // Platform identifier
                ConversationId: activity.Conversation?.Id ?? "UnknownConversation", // Ensure non-null
                UserId: activity.From?.AadObjectId ?? activity.From?.Id ?? "UnknownUser", // Use AAD ID or fallback
                QueryText: cleanedText, // The user's message text (mention removed)
                MessageId: activity.Id, // Use MessageId
                ReplyToMessageId: activity.ReplyToId, // Use ReplyToMessageId
                ArtifactReferences: artifactReferences,
                Metadata: new Dictionary<string, string> // Add relevant metadata if needed
                {
                    { "ChannelId", activity.ChannelId },
                    { "TeamId", activity.Conversation?.Properties["teamId"]?.ToString() ?? "N/A" }
                    // Add other relevant info like user name, etc.
                }
            );

            _logger.LogInformation("Attempting to send interaction request to API for ActivityId: {ActivityId}", activity.Id);

            // Send an initial acknowledgement via Teams Notifier
            try
            {
                // Fixed CS8602: Add null check for Conversation
                if (activity.Conversation != null)
                {
                    // Pass conversation ID and activity ID (as replyToId)
                    await _platformNotifier.SendAcknowledgementAsync(activity.Conversation.Id, activity.Id, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Cannot send acknowledgement; Conversation context is missing in ActivityId: {ActivityId}", activity.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send initial acknowledgement for ActivityId: {ActivityId}", activity.Id);
                // Continue processing even if acknowledgement fails
            }

            // --- Call API Endpoint via HttpClient ---
            AdapterResponse? apiResponse = null;
            try
            {
                // Relative path assumes BaseAddress is set correctly on HttpClient
                using var httpResponse = await _httpClient.PostAsJsonAsync("/api/interaction/process", request, _jsonOptions, cancellationToken);

                if (httpResponse.IsSuccessStatusCode)
                {
                    apiResponse = await httpResponse.Content.ReadFromJsonAsync<AdapterResponse>(_jsonOptions, cancellationToken);
                    _logger.LogInformation("API request successful for ActivityId: {ActivityId}. API Response Success: {ApiResponseSuccess}, Message: {ApiResponseMessage}",
                        activity.Id, apiResponse?.Success, apiResponse?.ResponseMessage);

                    // Optionally send a follow-up based on the response (if API provides one and it's not just an ack)
                    if (apiResponse != null && !string.IsNullOrWhiteSpace(apiResponse.ResponseMessage) && apiResponse.Success)
                    {
                        // Consider if this message is redundant with the initial ack or intended as the final result
                        // In async scenarios, the API might just return 202 Accepted + JobId
                        // await turnContext.SendActivityAsync(MessageFactory.Text($"API: {apiResponse.ResponseMessage}"), cancellationToken);
                        _logger.LogDebug("API provided response message: {Message}", apiResponse.ResponseMessage);
                    }
                    else if (apiResponse != null && !apiResponse.Success)
                    {
                         _logger.LogWarning("API request succeeded ({StatusCode}) but indicated failure in response body for ActivityId: {ActivityId}. Error: {Error}", httpResponse.StatusCode, activity.Id, apiResponse.ErrorMessage);
                         await turnContext.SendActivityAsync(MessageFactory.Text($"Sorry, there was a problem processing your request: {apiResponse.ErrorMessage ?? "Unknown API error"}"), cancellationToken);
                    }
                     else if (apiResponse == null)
                    {
                         _logger.LogWarning("API request succeeded ({StatusCode}) but response body was empty or failed to deserialize for ActivityId: {ActivityId}", httpResponse.StatusCode, activity.Id);
                         await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, there was an issue interpreting the API response."), cancellationToken);
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("API request failed for ActivityId: {ActivityId}. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}",
                                     activity.Id, httpResponse.StatusCode, httpResponse.ReasonPhrase, errorContent);

                    // Try to deserialize error response if possible
                    string displayError = $"API Error ({httpResponse.StatusCode})";
                    try
                    {
                         var errorResponse = JsonSerializer.Deserialize<AdapterResponse>(errorContent, _jsonOptions);
                         if (errorResponse != null && !string.IsNullOrWhiteSpace(errorResponse.ErrorMessage))
                         {
                             displayError = errorResponse.ErrorMessage;
                         }
                    } catch (JsonException) { /* Ignore if content isn't AdapterResponse JSON */ }

                    await turnContext.SendActivityAsync(MessageFactory.Text($"Sorry, the request failed: {displayError}"), cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception during interaction for ActivityId: {ActivityId}. Ensure API service is running at {BaseAddress}.", activity.Id, _httpClient.BaseAddress);
                 await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, I couldn't connect to the backend service."), cancellationToken);
            }
            catch (JsonException ex)
            {
                 _logger.LogError(ex, "JSON serialization/deserialization error during interaction for ActivityId: {ActivityId}.", activity.Id);
                 await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, there was a problem processing the data for the request."), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during interaction processing for ActivityId: {ActivityId}.", activity.Id);
                await turnContext.SendActivityAsync(MessageFactory.Text("An unexpected error occurred while processing your request."), cancellationToken);
            }
        }

        /// <summary>
        /// Extracts file attachment information from the activity and creates ArtifactReference objects.
        /// Focuses on hints needed by the API, avoids complex local parsing.
        /// <seealso cref="d:\Projects\Nucleus\Docs\Architecture\ClientAdapters\Teams\ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md"/>
        /// </summary>
        private static List<ArtifactReference>? ExtractAttachmentReferences(
            IMessageActivity activity, 
            ILogger logger)
        {
            if (activity.Attachments == null || !activity.Attachments.Any())
            {
                return null;
            }

            var references = new List<ArtifactReference>();
            logger.LogDebug("Found {AttachmentCount} attachments in ActivityId: {ActivityId}", activity.Attachments.Count, activity.Id);

            foreach (var attachment in activity.Attachments)
            {
                // Heuristic: Check for file attachments typically having a "content" property with metadata
                // or specific content types. This might need adjustment based on actual Graph API attachment formats.
                if (attachment.ContentType == "application/vnd.microsoft.card.file" ||
                    (attachment.ContentType != null && attachment.ContentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase))) // Broader check with comparison
                {
                     logger.LogDebug("Processing potential file attachment: Name='{Name}', ContentType='{ContentType}'", attachment.Name, attachment.ContentType);

                    string? uniqueId = null; // e.g., DriveItem ID
                    string? downloadUrl = null;
                    string? etag = null; // Usually not useful for identification alone

                    // Try parsing 'content' if it's structured JSON (like JObject)
                    if (attachment.Content is JObject contentJson)
                    {
                        uniqueId = contentJson["uniqueId"]?.ToString();
                        downloadUrl = contentJson["downloadUrl"]?.ToString();
                        etag = contentJson["etag"]?.ToString();
                         logger.LogDebug("Parsed from JObject content: uniqueId='{UniqueId}', downloadUrl='{DownloadUrl}', etag='{ETag}'", uniqueId, downloadUrl, etag);
                    }
                    else if (attachment.Content != null)
                    {
                        // Attempt to parse if content is a string containing JSON
                        try
                        {
                            var contentStr = attachment.Content.ToString();
                            if (!string.IsNullOrWhiteSpace(contentStr) && contentStr.TrimStart().StartsWith("{", StringComparison.Ordinal))
                            {
                                var parsedContent = JObject.Parse(contentStr);
                                uniqueId = parsedContent["uniqueId"]?.ToString();
                                downloadUrl = parsedContent["downloadUrl"]?.ToString();
                                etag = parsedContent["etag"]?.ToString();
                                logger.LogDebug("Parsed from string content: uniqueId='{UniqueId}', downloadUrl='{DownloadUrl}', etag='{ETag}'", uniqueId, downloadUrl, etag);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to parse attachment.Content as JSON string for {AttachmentName}.", attachment.Name);
                        }
                    }

                    // We need at least a UniqueId (DriveItemId) to make a useful reference
                    if (string.IsNullOrWhiteSpace(uniqueId))
                    {
                        logger.LogWarning("Could not extract 'uniqueId' (DriveItemId) for attachment '{AttachmentName}'. Skipping reference.", attachment.Name);
                        continue;
                    }

                    // REMOVED: Complex DriveId parsing logic. API will handle resolution.

                    Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? sourceUri);

                    // Get TenantId from conversation context
                    string tenantId = activity.Conversation?.Properties["tenantId"]?.ToString() 
                                       ?? throw new InvalidOperationException("TenantId is missing from conversation context.");

                    // Create the standard ArtifactReference
                    references.Add(new ArtifactReference(
                        ReferenceId: uniqueId, // Use the DriveItem ID
                        ReferenceType: MsGraphReferenceType, // Use the constant for consistency
                        SourceUri: sourceUri ?? new Uri("urn:nucleus:missing-uri"), // Provide a placeholder if null, SourceUri is required
                        TenantId: tenantId, // Get from conversation
                        FileName: attachment.Name, // Map to FileName
                        MimeType: attachment.ContentType // Map to MimeType
                    ));
                    logger.LogInformation("Created ArtifactReference: ReferenceId='{ReferenceId}', ReferenceType='{ReferenceType}', TenantId='{TenantId}', FileName='{FileName}'", 
                        uniqueId, MsGraphReferenceType, tenantId, attachment.Name);
                }
                else
                {
                    logger.LogInformation("Skipping non-file-like attachment: Name='{Name}', ContentType='{ContentType}'", attachment.Name, attachment.ContentType);
                }
            }

            return references.Any() ? references : null;
        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(membersAdded);
            ArgumentNullException.ThrowIfNull(turnContext);

            var welcomeText = "Hello and welcome to the Nucleus OmniRAG Teams Adapter Prototype!";
            foreach (var member in membersAdded)
            {
                // Greet anyone else besides the bot itself
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    _logger.LogInformation("Member added: {MemberName} ({MemberId})", member.Name, member.Id);
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}