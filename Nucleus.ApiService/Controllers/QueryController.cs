using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Nucleus.Abstractions;
using Nucleus.Personas.Core;
using Nucleus.ApiService.Contracts; 
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Nucleus.ApiService.Controllers;

/// <summary>
/// Controller responsible for handling user queries.
/// Routes queries to the appropriate persona, potentially enriching them with cached context.
/// See: [Architecture: AI Integration - API Invocation](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#4-api-invocation)
/// See: [Architecture: AI Integration - Contextual Query](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#3-contextual-query-post-apiquery)
/// </summary>
[ApiController]
[Route("api/[controller]")] 
public class QueryController : ControllerBase
{
    private readonly IPersona<EmptyAnalysisData> _persona;
    private readonly ILogger<QueryController> _logger;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryController"/> class.
    /// </summary>
    /// <param name="persona">The persona instance responsible for handling queries.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="memoryCache">The memory cache instance for retrieving ephemeral context.</param>
    public QueryController(IPersona<EmptyAnalysisData> persona, ILogger<QueryController> logger, IMemoryCache memoryCache)
    {
        _persona = persona ?? throw new ArgumentNullException(nameof(persona));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <summary>
    /// Handles a user query by routing it to the appropriate Persona.
    /// If a <see cref="QueryRequest.ContextSourceIdentifier"/> is provided, attempts to retrieve cached content
    /// and pass it to <see cref="IPersona{TAnalysisData}.HandleQueryAsync"/>.
    /// </summary>
    /// <param name="request">The query request details.</param>
    /// <returns>The response from the Persona.</returns>
    /// <seealso cref="QueryRequest"/>
    /// <seealso cref="QueryResponse"/>
    /// <seealso cref="IPersona{TAnalysisData}.HandleQueryAsync"/>
    [HttpPost]
    [ProducesResponseType(typeof(QueryResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> HandleQuery([FromBody] QueryRequest request)
    {
        _logger.LogDebug("HandleQuery method entered."); 

        if (request == null)
        {
            _logger.LogWarning("Model binding failed: QueryRequest object is null.");
            return BadRequest("Query request payload is missing or invalid.");
        }
        if (string.IsNullOrWhiteSpace(request.QueryText))
        {
             _logger.LogWarning("Model validation failed: QueryText is null or whitespace. Request: {@QueryRequest}", request);
            return BadRequest("QueryText cannot be empty.");
        }

        // If we reach here, binding and basic validation passed.
        _logger.LogInformation("Model validation successful. Received QueryText: '{QueryText}'", request.QueryText);

        // Map DTO to internal UserQuery record
        // TODO: Replace placeholder UserId later (e.g., from authentication)
        var userId = request.UserId ?? "anonymous"; 
        // TODO: Review UserQuery.Context usage. Currently, the context string is passed separately to HandleQueryAsync,
        // and this dictionary is unused in this flow. Should contextContent populate this dictionary instead?
        // UserQuery requires a Context dictionary per its definition in IPersona.cs
        var userQuery = new UserQuery(request.QueryText, userId, new Dictionary<string, object>());

        _logger.LogInformation("Received query via API for user {UserId}: '{QueryText}'", userId, request.QueryText);

        string? contextContent = null; 

        // Attempt to retrieve context from cache if an identifier is provided
        if (!string.IsNullOrWhiteSpace(request.ContextSourceIdentifier))
        {
            if (_memoryCache.TryGetValue(request.ContextSourceIdentifier, out string? cachedContent))
            {
                contextContent = cachedContent;
                _logger.LogInformation("Retrieved cached context for identifier: {ContextId} ({ContentLength} chars)", 
                    request.ContextSourceIdentifier, contextContent?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning("Context identifier '{ContextId}' provided, but no matching content found in cache.", 
                    request.ContextSourceIdentifier);
                // Optional: Return a BadRequest or specific error if context is required but missing?
                // For now, proceed without context.
            }
        }

        try
        {
            // Call the injected persona
            // Use the userQuery object created earlier, passing the retrieved contextContent
            var personaResult = await _persona.HandleQueryAsync(userQuery, contextContent, HttpContext.RequestAborted);

            // Map internal result to response DTO
            var response = new QueryResponse(personaResult.ResponseText, personaResult.SourceReferences);

            _logger.LogInformation("Successfully processed query via API for user {UserId}.", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query via API for user {UserId}.", userId);
            // Return a generic server error to the client
            return StatusCode(500, "An internal error occurred while processing your query.");
        }
    }
}
