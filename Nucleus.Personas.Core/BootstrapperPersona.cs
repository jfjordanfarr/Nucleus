using Mscc.GenerativeAI; // Added for IGenerativeAI, GenerativeModel, ChatSession etc.
using Microsoft.Extensions.Configuration; // For IConfigurationSection
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.Extensions.Caching.Memory; // Added for IMemoryCache
using Nucleus.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Personas.Core;

/// <summary>
/// A foundational persona responsible for basic query handling and bootstrapping interactions.
/// Implements <see cref="AnalyzeEphemeralContentAsync"/> for temporary context caching and
/// <see cref="HandleQueryAsync"/> for interacting with the configured AI model (currently Gemini).
/// **Architectural Goal:** To flexibly interact with various AI providers (e.g., Google Gemini, Azure OpenAI) through appropriate abstractions.
/// **Current Implementation (Gemini):** Uses the `Mscc.GenerativeAI` library's `IGenerativeAI` and `ChatSession` pattern.
///
/// **Implementation Notes:**
/// *   Refactored away from `Mscc.GenerativeAI.Microsoft.GeminiChatClient` due to missing
///     dependencies (`IChatClient`, `ChatOptions`) and unclear API usage compared to base library examples
///     (See Cascade Steps 300-348).
/// *   Integration of other providers (e.g., Azure OpenAI) will require separate SDK integration and potentially
///     a higher-level Nucleus abstraction if common patterns differ significantly.
///
/// **References:**
/// *   [Docs/Planning/01_PHASE1_MVP_TASKS.md](../../../Docs/Planning/01_PHASE1_MVP_TASKS.md) (TASK-MVP-PERSONA-01, TASK-MVP-PERSONA-02)
/// *   [Docs/Architecture/02_ARCHITECTURE_PERSONAS.md](../../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md)
/// *   [Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md](../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md)
/// </summary>
public class BootstrapperPersona : IPersona<EmptyAnalysisData>
{
    // Represents the currently configured AI service abstraction.
    // For the initial Gemini implementation, this is Mscc.GenerativeAI.IGenerativeAI.
    // Future implementations might use a different interface or a Nucleus-specific abstraction
    // to accommodate multiple providers (e.g., Azure OpenAI).
    private readonly IGenerativeAI _generativeAi;
    private readonly ILogger<BootstrapperPersona> _logger;
    private readonly IMemoryCache _memoryCache; // Added for ephemeral storage

    public string PersonaId => "core-bootstrapper";
    public string DisplayName => "Bootstrapper Persona";
    public string Description => "Handles initial queries and basic interactions.";

    /// <param name="generativeAi">The configured generative AI service abstraction. Currently `Mscc.GenerativeAI.IGenerativeAI` for Gemini support.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="memoryCache">In-memory cache for temporary data storage.</param>
    public BootstrapperPersona(IGenerativeAI generativeAi, ILogger<BootstrapperPersona> logger, IMemoryCache memoryCache)
    {
        _generativeAi = generativeAi ?? throw new ArgumentNullException(nameof(generativeAi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache)); // Added cache injection
    }

    /// <summary>
    /// Placeholder: AnalyzeContentAsync is not implemented in the initial Bootstrapper.
    /// </summary>
    public Task<PersonaAnalysisResult<EmptyAnalysisData>> AnalyzeContentAsync(ContentItem content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AnalyzeContentAsync called for {SourceIdentifier} but is not implemented by {PersonaId}.", 
            content.SourceIdentifier, PersonaId);
            
        // Return a default result indicating no analysis was performed.
        var emptyAnalysis = new EmptyAnalysisData();
        var result = new PersonaAnalysisResult<EmptyAnalysisData>(emptyAnalysis, string.Empty); 
        return Task.FromResult(result);
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
    /// Handles a user query by starting a chat session with the configured IGenerativeAI model.
    /// If contextContent is provided, it is prepended to the user's query in the prompt.
    /// Called by <see cref="Nucleus.ApiService.Controllers.QueryController.HandleQuery(QueryRequest)"/>.
    /// See: [Architecture: AI Integration - Chat Interaction](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#3-chat-interaction)
    /// See: [Architecture: AI Integration - Context Usage](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#3-contextual-query-post-apiquery)
    /// </summary>
    /// <param name="query">The user's query details.</param>
    /// <param name="contextContent">Optional cached content to provide context for the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the query, including the AI's response.</returns>
    /// <seealso cref="UserQuery"/>
    /// <seealso cref="PersonaQueryResult"/>
    public async Task<PersonaQueryResult> HandleQueryAsync(UserQuery query, string? contextContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling query for user {UserId} via {PersonaId}: '{QueryText}' (Context Provided: {ContextProvided})", 
            query.UserId, PersonaId, query.QueryText, !string.IsNullOrEmpty(contextContent));

        try
        {
            // Construct the prompt, including context if available
            string prompt = query.QueryText;
            if (!string.IsNullOrWhiteSpace(contextContent))
            {
                prompt = $"Use the following context to answer the query:\n\n---\nCONTEXT START\n---\n{contextContent}\n---\nCONTEXT END\n---\n\nQUERY:\n{query.QueryText}";
                _logger.LogDebug("Prepended context to prompt ({ContextLength} chars).", contextContent.Length);
            }
            else
            {
                _logger.LogDebug("No context provided, using query directly as prompt.");
            }

            // Get the default generative model (e.g., gemini-1.5-pro)
            var model = _generativeAi.GenerativeModel(); 
            // Start a new chat session for this query
            // TODO: Consider chat history management for multi-turn conversations later
            var chat = model.StartChat(); 

            // Send the user's query using the ChatSession pattern
            // No ChatOptions needed here based on examples
            var response = await chat.SendMessage(prompt, cancellationToken: cancellationToken);

            // TODO: Extract source references if the AI model provides them.
            var sourceReferences = new List<string>(); 

            _logger.LogInformation("Received response from Generative AI for user {UserId}.", query.UserId);

            // Extract text from the response
            // Assuming response object has a 'Text' property based on examples and prior usage
            string responseText = response.Text ?? "No response content.";

            return new PersonaQueryResult(responseText, sourceReferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling query via IGenerativeAI in {PersonaId} for user {UserId}.", PersonaId, query.UserId);
            return new PersonaQueryResult($"Error processing query: {ex.Message}", new List<string>());
        }
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
