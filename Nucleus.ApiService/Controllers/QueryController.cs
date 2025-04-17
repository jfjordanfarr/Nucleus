using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.ApiService.Contracts; // For DTOs
using Nucleus.Personas.Core; // For EmptyAnalysisData
using System;
using System.Threading.Tasks;

namespace Nucleus.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")] // Route will be /api/query
public class QueryController : ControllerBase
{
    private readonly IPersona<EmptyAnalysisData> _persona;
    private readonly ILogger<QueryController> _logger;

    public QueryController(IPersona<EmptyAnalysisData> persona, ILogger<QueryController> logger)
    {
        _persona = persona ?? throw new ArgumentNullException(nameof(persona));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a user query by routing it to the appropriate Persona.
    /// </summary>
    /// <param name="request">The query request details.</param>
    /// <returns>The response from the Persona.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QueryResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> HandleQuery([FromBody] QueryRequest request)
    {
        _logger.LogDebug("HandleQuery method entered."); // Log entry

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
        var userId = request.UserId ?? "anonymous-user"; 
        var context = request.Context ?? new Dictionary<string, object>();
        var userQuery = new UserQuery(request.QueryText, userId, context);

        _logger.LogInformation("Received query via API for user {UserId}: '{QueryText}'", userId, request.QueryText);

        try
        {
            // Call the injected persona
            var personaResult = await _persona.HandleQueryAsync(userQuery, HttpContext.RequestAborted); // Pass cancellation token

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
