using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Nucleus.Services.Api.Controllers;

/// <summary>
/// API Controller responsible for receiving interaction requests from various client adapters.
/// It serves as the primary entry point for user interactions into the Nucleus system.
/// </summary>
/// <remarks>
/// This controller validates incoming requests (<see cref="AdapterRequest"/>) and forwards them
/// to the <see cref="IOrchestrationService"/> for processing. It handles basic request validation
/// and returns appropriate HTTP status codes based on the orchestration result.
/// </remarks>
/// <seealso cref="IOrchestrationService"/>
/// <seealso cref="AdapterRequest"/>
/// <seealso cref="OrchestrationResult"/>
/// <seealso cref="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// <seealso cref="../../../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly ILogger<InteractionController> _logger;
    private readonly IOrchestrationService _orchestrationService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public InteractionController(ILogger<InteractionController> logger, IOrchestrationService orchestrationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// Process an interaction request.
    /// </summary>
    /// <param name="request">The interaction request details.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An IActionResult indicating the result of the processing.</returns>
    [HttpPost("query")] // Ensure route is explicit if needed
    public async Task<IActionResult> Post([FromBody] AdapterRequest request, CancellationToken cancellationToken)
    {
        // TODO: Add robust validation for the incoming request
        if (request == null)
        {
            _logger.LogWarning("Received null request body.");
            return BadRequest("Request body cannot be null.");
        }

        _logger.LogInformation("Received interaction request for ConversationId: {ConversationId}", request.ConversationId);

        try
        {
            // Extract user information if available (e.g., from JWT)
            // For now, we might pass a default or null user
            // string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            // _logger.LogInformation("Processing request for User ID: {UserId}", userId);

            // The 'files' parameter is removed, pass null or an empty collection if the interface still requires it temporarily.
            var result = await _orchestrationService.ProcessInteractionAsync(request, cancellationToken); // Pass request and cancellationToken

            // Check the orchestration status
            switch (result.Status)
            {
                case OrchestrationStatus.ProcessedSync:
                    // Synchronous success, return the response from the AdapterResponse
                    _logger.LogInformation("[API Controller] Interaction processed synchronously. ConversationId: {ConversationId}", request.ConversationId);
                    // Ensure Response is not null before accessing its properties
                    if (result.Response != null && result.Response.Success)
                    {
                        return Ok(result.Response); // Or just result.Response.ResponseMessage if that's the intended payload
                    }
                    else
                    {
                        // Handle unexpected null response or failure within sync processing
                        _logger.LogError("[API Controller] Synchronous processing indicated success status but response was null or failed. ConversationId: {ConversationId}", request.ConversationId);
                        return StatusCode(StatusCodes.Status500InternalServerError, result.Response?.ErrorMessage ?? "Internal error during synchronous processing.");
                    }

                case OrchestrationStatus.AcceptedAsync:
                    // Asynchronous processing initiated, return 202 Accepted
                    _logger.LogInformation("[API Controller] Interaction accepted for asynchronous processing. ConversationId: {ConversationId}", request.ConversationId);
                    return Accepted(); // Standard response for async task initiation

                case OrchestrationStatus.NotActivated:
                    // Interaction was not activated, return 200 OK with no content (as per design)
                    _logger.LogInformation("[API Controller] Interaction not activated. ConversationId: {ConversationId}", request.ConversationId);
                    return Ok(); // Or NoContent() if preferred

                case OrchestrationStatus.Error:
                    // Orchestration reported an error
                    _logger.LogError("[API Controller] Orchestration failed. Error: {ErrorMessage}, ConversationId: {ConversationId}", 
                        result.Response?.ErrorMessage ?? "Unknown orchestration error", request.ConversationId);
                    return StatusCode(StatusCodes.Status500InternalServerError, result.Response?.ErrorMessage ?? "An orchestration error occurred.");

                default:
                    // Should not happen, but handle unexpected status
                    _logger.LogError("[API Controller] Unexpected OrchestrationStatus: {Status}. ConversationId: {ConversationId}", result.Status, request.ConversationId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected orchestration status.");
            }
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid arguments during interaction processing for ConversationId: {ConversationId}", request.ConversationId);
            return BadRequest(new AdapterResponse(false, "Invalid request data.", argEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing interaction for ConversationId: {ConversationId}", request.ConversationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new AdapterResponse(false, "An unexpected internal error occurred.", ex.Message));
        }
    }
}
