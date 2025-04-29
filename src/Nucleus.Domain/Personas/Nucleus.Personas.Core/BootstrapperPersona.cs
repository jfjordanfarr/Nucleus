using Microsoft.Extensions.Configuration; // For IConfigurationSection
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider, IServiceScopeFactory
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.Extensions.AI; // For IChatClient, AIContent, TextContent, ChatMessage, ChatRole, ChatResponse etc.
// Removed redundant/incorrect using statement as types are in Microsoft.Extensions.AI namespace from Abstractions package
// using Microsoft.Extensions.AI.Chat; 
using Nucleus.Abstractions; // For IPersona, UserQuery, PersonaQueryResult
using Nucleus.Abstractions.Models; // For ArtifactContent, ArtifactMetadata
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text; // Required for StringBuilder
using System.IO;    // Required for StreamReader

namespace Nucleus.Domain.Personas.Core;

/// <summary>
/// A basic persona implementation that uses the injected IChatClient to
/// respond to queries, incorporating fetched artifact content.
/// Represents the default fallback or initial interaction handler.
/// See: <seealso cref="../../../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
/// See: <seealso cref="../../../../../Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md"/>
/// </summary>
public class BootstrapperPersona : IPersona<EmptyAnalysisData>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<BootstrapperPersona> _logger;

    /// <inheritdoc />
    public string PersonaId => "Bootstrapper_v1";

    /// <inheritdoc />
    public string DisplayName => "Bootstrapper Persona";

    /// <inheritdoc />
    public string Description => "Basic Bootstrapping Persona for simple queries.";

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapperPersona"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client for interacting with the AI model.</param>
    /// <param name="logger">The logger instance.</param>
    public BootstrapperPersona(IChatClient chatClient, ILogger<BootstrapperPersona> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a user query directed at this persona.
    /// This basic implementation sends the query directly to the AI model,
    /// optionally prepending context derived from fetched artifact content.
    /// </summary>
    /// <param name="query">The user's query details.</param>
    /// <param name="fetchedArtifactContent">Optional collection of artifact content fetched by the orchestrator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the query.</returns>
    /// <remarks>
    /// This implementation uses the provided <paramref name="fetchedArtifactContent"/> (if any) to construct a system context
    /// before sending the user's query to the AI model via the injected IChatClient.
    /// </remarks>
    /// <seealso cref="PersonaQueryResult"/>
    public async Task<PersonaQueryResult> HandleQueryAsync(UserQuery query, IEnumerable<ArtifactContent>? fetchedArtifactContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // TODO: Implement metadata lookup based on query context (User/Conversation) to find relevant
        //       ArtifactMetadata/PersonaKnowledge entries from IArtifactMetadataRepository.
        //       Currently, relies solely on fetchedArtifactContent provided by the orchestrator.

        bool hasFetchedContent = fetchedArtifactContent?.Any() ?? false;
        _logger.LogInformation("Handling query for user {UserId} via {PersonaId}: '{QueryText}' (Fetched Content Provided: {ContextProvided})",
            query.UserId, PersonaId, query.QueryText, hasFetchedContent);

        try
        {
            var messages = new List<ChatMessage>();
            var contextBuilder = new StringBuilder();

            // Build system context from fetched content if available
            if (hasFetchedContent)
            {
                contextBuilder.AppendLine("--- Provided Context ---");
                int contentCount = 0;
                foreach (var content in fetchedArtifactContent)
                {
                    contentCount++;
                    contextBuilder.AppendLine($"\n[Context {contentCount} - Source: {content.SourceUri ?? "Unknown"}]\n");
                    contextBuilder.AppendLine(content.Text);
                    _logger.LogTrace("Added fetched content from source {SourceUri} to context.", content.SourceUri ?? "Unknown");
                }
                contextBuilder.AppendLine("\n--- End Provided Context ---");
                _logger.LogInformation("Built context from {Count} provided artifact(s).", contentCount);
            }

            // Add the user's query
            messages.Add(new ChatMessage(ChatRole.User, query.QueryText));

            // Request options (if needed, e.g., temperature) can be set via ChatOptions
            ChatOptions options = new ChatOptions();

            _logger.LogDebug("Sending chat request to AI service via IChatClient. Messages: {@Messages}, Options: {@Options}", messages, options);

            // Use GetResponseAsync instead of CompleteAsync
            ChatResponse response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);

            // Extract the response content from ChatResponse
            // Assuming the primary response is the first message in the response's Messages collection
            // Need to confirm the structure of ChatResponse if this assumption is wrong.
            ChatMessage? assistantMessage = response?.Messages?.FirstOrDefault(m => m.Role == ChatRole.Assistant);
            string aiResponse = string.Empty;
            if (assistantMessage?.Contents?.FirstOrDefault() is TextContent textContent)
            {
                aiResponse = textContent.Text ?? string.Empty;
            }

            if (string.IsNullOrEmpty(aiResponse))
            {
                _logger.LogWarning("Received empty response content from AI service. Response: {@Response}", response);
                aiResponse = "Sorry, I received an empty response from the AI service."; // Provide a default user message
            }
            else
            {
                _logger.LogInformation("Received response from AI service via IChatClient. Length: {ResponseLength}.", aiResponse.Length);
            }

            return new PersonaQueryResult(aiResponse, new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling query via {PersonaId} and IChatClient: {ErrorMessage}", PersonaId, ex.Message);
            // Consider returning a more specific error or logging details
            return new PersonaQueryResult($"Sorry, an internal error occurred while processing your request. Error: {ex.Message}", new List<string>());
        }
    }

    /// <inheritdoc />
    /// <remarks>BootstrapperPersona does not perform analysis on persisted content.</remarks>
    public Task<PersonaAnalysisResult<EmptyAnalysisData>> AnalyzeContentAsync(ContentItem content, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("{PersonaId}.AnalyzeContentAsync is not implemented.", PersonaId);
        // Return a default result indicating no analysis was performed.
        var emptyAnalysis = new EmptyAnalysisData();
        var result = new PersonaAnalysisResult<EmptyAnalysisData>(emptyAnalysis, "No analysis performed by BootstrapperPersona.");
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    /// <remarks>BootstrapperPersona does not perform analysis.</remarks>
    public static Task<EmptyAnalysisData?> AnalyzeInputAsync(UserQuery query, string? contextContent, CancellationToken cancellationToken = default)
    {
        // This persona doesn't perform analysis, returns null.
        return Task.FromResult<EmptyAnalysisData?>(null);
    }

    /// <inheritdoc />
    /// <remarks>BootstrapperPersona does not generate distinct plans.</remarks>
    public static Task<string?> GeneratePlanAsync(UserQuery query, EmptyAnalysisData? analysisData, string? contextContent, CancellationToken cancellationToken = default)
    {
        // This persona doesn't generate plans, returns null.
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Placeholder: Configuration is not implemented in the initial Bootstrapper.
    /// </summary>
    public Task ConfigureAsync(IConfigurationSection configurationSection)
    {
        _logger.LogInformation("ConfigureAsync called for {PersonaId} but is not implemented.", PersonaId);
        return Task.CompletedTask;
    }

    // Placeholder for handling ephemeral content analysis (e.g., analyzing a newly uploaded doc)
    // Maybe update metadata based on content?
    public Task<EmptyAnalysisData> AnalyzeEphemeralContentAsync(string content, string mimeType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("BootstrapperPersona AnalyzeEphemeralContentAsync called (Not Implemented).");
        // In the Runtime model, this logic likely moves to a strategy handler or ingestion process
        return Task.FromResult(new EmptyAnalysisData()); // Return empty data as per IPersona<EmptyAnalysisData>
    }

    // Placeholder for handling a user query with potentially relevant artifact content
    public Task<AdapterResponse> HandleQueryAsync(UserQuery userQuery, IReadOnlyList<ArtifactContent> contextArtifacts, CancellationToken cancellationToken)
    {
        _logger.LogInformation("BootstrapperPersona HandleQueryAsync called (Not Implemented).");
        // In the Runtime model, this logic moves to a strategy handler (like EchoAgenticStrategyHandler)
        var response = new AdapterResponse
        {
            Success = true,
            MessageId = userQuery.MessageId, // Echo back relevant IDs
            ConversationId = userQuery.ConversationId,
            ResponseText = "BootstrapperPersona received query but is not fully implemented in HandleQueryAsync.",
            Prompt = userQuery.QueryText,
            ShowYourWork = new List<string> { "Called BootstrapperPersona.HandleQueryAsync (Stub Implementation)" }
        };
        return Task.FromResult(response);
    }
}
