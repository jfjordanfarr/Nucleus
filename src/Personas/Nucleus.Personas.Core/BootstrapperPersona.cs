using Microsoft.Extensions.Configuration; // For IConfigurationSection
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider, IServiceScopeFactory
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.Extensions.Caching.Memory; // Added for IMemoryCache
using Nucleus.Abstractions; // For IPersona, UserQuery, PersonaQueryResult
using Nucleus.Personas.Core; // Added correct namespace for EmptyAnalysisData
using Nucleus.Domain.Processing.Infrastructure; // Added for VertexAiChatClientAdapter
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Personas.Core;

/// <summary>
/// A simple persona used during application startup or for basic interactions.
/// It demonstrates direct interaction with the configured AI model via VertexAiChatClientAdapter.
/// It does not perform complex analysis or maintain long-term state beyond the current query context.
/// </summary>
public class BootstrapperPersona : IPersona<EmptyAnalysisData>
{
    private readonly VertexAiChatClientAdapter _vertexAiAdapter;
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
    /// <param name="vertexAiAdapter">The Vertex AI adapter for interacting with the AI model.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="memoryCache">In-memory cache for temporary data storage.</param>
    public BootstrapperPersona(VertexAiChatClientAdapter vertexAiAdapter, ILogger<BootstrapperPersona> logger, IMemoryCache memoryCache)
    {
        _vertexAiAdapter = vertexAiAdapter ?? throw new ArgumentNullException(nameof(vertexAiAdapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation uses the provided <paramref name="contextContent"/> as system context
    /// before sending the user's query to the AI model via VertexAiChatClientAdapter.
    /// </remarks>
    /// <seealso cref="PersonaQueryResult"/>
    public async Task<PersonaQueryResult> HandleQueryAsync(UserQuery query, string? contextContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling query for user {UserId} via {PersonaId}: '{QueryText}' (Context Provided: {ContextProvided})",
            query.UserId, PersonaId, query.QueryText, !string.IsNullOrEmpty(contextContent));

        try
        {
            string prompt;
            if (!string.IsNullOrWhiteSpace(contextContent))
            {
                prompt = $"Use the following context to answer the query:\n\n---\nCONTEXT START\n---\n{contextContent}\n---\nCONTEXT END\n---\n\nUser Query: {query.QueryText}";
                _logger.LogDebug("Added context to prompt ({ContextLength} chars).", contextContent.Length);
            }
            else
            {
                prompt = query.QueryText;
                _logger.LogDebug("No context provided.");
            }

            _logger.LogDebug("Sending prompt to Vertex AI service. Prompt length: {PromptLength}.", prompt.Length);
            string aiResponse = await _vertexAiAdapter.GetCompletionAsync(prompt, cancellationToken);

            _logger.LogInformation("Received response from Vertex AI. Length: {ResponseLength}.", aiResponse.Length);
            return new PersonaQueryResult(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling query in BootstrapperPersona.");
            return new PersonaQueryResult($"Error: {ex.Message}");
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
    public Task<EmptyAnalysisData?> AnalyzeInputAsync(UserQuery query, string? contextContent, CancellationToken cancellationToken = default)
    {
        // This persona doesn't perform analysis, returns null.
        return Task.FromResult<EmptyAnalysisData?>(null);
    }

    /// <inheritdoc />
    /// <remarks>BootstrapperPersona does not generate distinct plans.</remarks>
    public Task<string?> GeneratePlanAsync(UserQuery query, EmptyAnalysisData? analysisData, string? contextContent, CancellationToken cancellationToken = default)
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
