using Microsoft.Extensions.Configuration; // For IConfigurationSection
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider, IServiceScopeFactory
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.Extensions.Caching.Memory; // Added for IMemoryCache
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
/// A simple persona used during application startup or for basic interactions.
/// It demonstrates direct interaction with the configured AI model via IChatClient.
/// It does not perform complex analysis or maintain long-term state beyond the current query context.
/// </summary>
public class BootstrapperPersona : IPersona<EmptyAnalysisData>
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<BootstrapperPersona> _logger;
    private readonly IMemoryCache _memoryCache;

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
    /// <param name="memoryCache">In-memory cache for temporary data storage.</param>
    public BootstrapperPersona(IChatClient chatClient, ILogger<BootstrapperPersona> logger, IMemoryCache memoryCache)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
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

        _logger.LogInformation("Handling query for user {UserId} via {PersonaId}: '{QueryText}' (Fetched Content Provided: {ContextProvided})",
            query.UserId, PersonaId, query.QueryText, fetchedArtifactContent?.Any() ?? false);

        try
        {
            var messages = new List<ChatMessage>();
            var systemContextBuilder = new StringBuilder();

            // Process fetchedArtifactContent into a suitable system prompt string.
            if (fetchedArtifactContent?.Any() == true)
            {
                _logger.LogDebug("Processing {Count} fetched artifact(s) for system context.", fetchedArtifactContent.Count());
                int artifactIndex = 0;
                foreach (var artifact in fetchedArtifactContent)
                {
                    if (artifact?.ContentStream == null || !artifact.ContentStream.CanRead)
                    {
                        _logger.LogWarning("Skipping artifact at index {Index} due to null or unreadable stream.", artifactIndex);
                        artifactIndex++;
                        continue;
                    }

                    try
                    {
                        // Ensure stream position is at the beginning
                        if (artifact.ContentStream.CanSeek)
                        {
                            artifact.ContentStream.Position = 0;
                        }

                        // Use StreamReader with leaveOpen=true to avoid disposing the original stream
                        using (var reader = new StreamReader(artifact.ContentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                        {
                            string content = await reader.ReadToEndAsync(cancellationToken);
                            
                            // Add header/separator for multiple artifacts
                            if (fetchedArtifactContent.Count() > 1)
                            {
                                var fileName = artifact.Metadata?.GetValueOrDefault("FileName", "Unnamed");
                                var sourceId = artifact.Metadata?.GetValueOrDefault("SourceIdentifier", "Unknown Source");
                                systemContextBuilder.AppendLine($"--- Artifact {artifactIndex + 1}: {fileName} ({sourceId}) ---");
                            }
                            systemContextBuilder.AppendLine(content);
                            _logger.LogTrace("Processed artifact {Index} ({Name}) with {Length} characters.", 
                                artifactIndex, artifact.Metadata?.GetValueOrDefault("FileName", "Unnamed"), content.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading content from artifact stream at index {Index} ({Name}).", 
                            artifactIndex, artifact.Metadata?.GetValueOrDefault("FileName", "Unnamed"));
                        // Optionally add an error message to the context
                        systemContextBuilder.AppendLine($"[Error reading artifact {artifactIndex + 1}: {ex.Message}]");
                    }
                    finally
                    {
                        artifactIndex++;
                    }
                }

                string systemContext = systemContextBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(systemContext))
                {
                    messages.Add(new ChatMessage(ChatRole.System, systemContext));
                    _logger.LogDebug("Added system context message based on {Count} processed artifacts. Total length: {Length}", 
                        fetchedArtifactContent.Count(), systemContext.Length);
                }
                else
                {
                    _logger.LogWarning("System context generated from artifacts was empty or whitespace.");
                }
            }

            // Add the user's query
            // Revert to simple ChatMessage(ChatRole, string) constructor
            messages.Add(new ChatMessage(ChatRole.User, query.QueryText)); 

            // Request options (if needed, e.g., temperature) can be set via ChatOptions
            ChatOptions options = new ChatOptions(); 
            // options.Temperature = 0.7f; // Example

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
    /// Analyzes ephemeral content (e.g., raw text from ingestion) by storing it in a temporary cache.
    /// Called by <see cref="Nucleus.ApiService.Controllers.IngestionController.IngestLocalFileAsync(IngestLocalFileRequest)"/>.
    /// This implementation is for the MVP to provide context to subsequent queries within a short time frame.
    /// See: [Architecture: AI Integration - Caching](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#2-caching-bootstrapperpersonaanalyzeephemeralcontentasync)
    /// </summary>
    /// <param name="ephemeralContent">The content to be analyzed/cached.</param>
    /// <param name="sourceIdentifier">A unique identifier for the content source (e.g., file path).</param>
    /// <param name="cancellationToken">Cancellation token (not used in this simple implementation).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AnalyzeEphemeralContentAsync(string ephemeralContent, string sourceIdentifier, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Caching ephemeral content from {SourceIdentifier} under key '{CacheKey}'.", sourceIdentifier, sourceIdentifier);

        if (string.IsNullOrWhiteSpace(sourceIdentifier))
        {
            _logger.LogWarning("Cannot cache ephemeral content without a valid sourceIdentifier.");
            return Task.CompletedTask;
        }

        if (ephemeralContent == null) // Allow empty string, but not null
        {
             _logger.LogWarning("Received null ephemeral content for {SourceIdentifier}. Caching empty string instead.", sourceIdentifier);
             ephemeralContent = string.Empty;
        }

        // Cache the content with a sliding expiration (e.g., 10 minutes)
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10)); 

        _memoryCache.Set(sourceIdentifier, ephemeralContent, cacheEntryOptions);
        _logger.LogDebug("Successfully cached ephemeral content for {SourceIdentifier} ({ContentLength} chars).", sourceIdentifier, ephemeralContent.Length);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Placeholder: Configuration is not implemented in the initial Bootstrapper.
    /// </summary>
    public Task ConfigureAsync(IConfigurationSection configurationSection)
    {
        _logger.LogInformation("ConfigureAsync called for {PersonaId} but is not implemented.", PersonaId);
        return Task.CompletedTask;
    }
}
