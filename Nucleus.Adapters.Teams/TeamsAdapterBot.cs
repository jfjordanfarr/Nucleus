// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Adapters.Teams
{
    /// <summary>
    /// Handles incoming activities from Microsoft Teams.
    /// This is a basic implementation for the initial prototype.
    /// See: Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
    /// </summary>
    public class TeamsAdapterBot : ActivityHandler
    {
        private readonly ILogger<TeamsAdapterBot> _logger;
        private readonly GraphClientService _graphClientService;

        // TODO: Inject Nucleus orchestration/processing services later
        public TeamsAdapterBot(ILogger<TeamsAdapterBot> logger, GraphClientService graphClientService)
        {
            _logger = logger;
            _graphClientService = graphClientService;
            _logger.LogInformation("TeamsAdapterBot initialized.");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message received: {Text}", turnContext.Activity.Text);

            var inputText = turnContext.Activity.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            string replyText;

            // --- Prototype command handling ---
            if (inputText == "list files")
            {
                _logger.LogInformation("'list files' command received. Calling Graph API...");
                var files = await _graphClientService.ListTeamFilesAsync();

                if (files != null)
                {
                    if (files.Any())
                    {
                        var fileListBuilder = new StringBuilder("Files found:\n");
                        foreach (var file in files)
                        {
                            // Only list actual files, not folders, for clarity
                            if (file.File != null)
                            {
                                fileListBuilder.AppendLine($"- {file.Name} (ID: {file.Id})"); 
                            }
                        }
                        replyText = fileListBuilder.ToString();
                    }
                    else
                    {
                        replyText = "No files found in the configured SharePoint location.";
                    }
                }
                else
                {
                     _logger.LogWarning("GraphClientService.ListTeamFilesAsync returned null. Check logs for errors.");
                    replyText = "Error retrieving file list from SharePoint. Please check logs.";
                }
            }
            else
            {
                // Basic echo for other messages
                replyText = $"Prototype Echo: You sent '{turnContext.Activity.Text}'";
            }
            // --- End Prototype command handling ---

            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            // TODO: Add logic to detect commands/requests
            // TODO: Add logic to check for attachments (Files)
            // TODO: Integrate with Graph API to access Team files
            // TODO: Trigger Nucleus processing pipeline
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
