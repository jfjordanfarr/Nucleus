using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Adapters.Local;
using Nucleus.Abstractions.Results; // Added for Result<TSuccess, TError> and OrchestrationError
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Nucleus.Services.Api.Controllers;

/// <summary>
/// API controller for handling incoming interaction requests from client adapters.
/// It serves as the primary entry point for interactions into the Nucleus system.
/// </summary>
/// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md">Interaction Processing Lifecycle - API Request Received</seealso>
/// <seealso cref="IOrchestrationService"/>
/// <seealso cref="AdapterRequest"/>
/// <seealso cref="OrchestrationResult"/>
/// <seealso cref="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// <seealso cref="../../../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
/// <seealso cref="../../../../../Docs/Architecture/05_ARCHITECTURE_CLIENTS.md"/>
/// <seealso cref="../../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
/// <seealso cref="../../../../../Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md"/>
[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly ILogger<InteractionController> _logger;
    private readonly IOrchestrationService _orchestrationService;
    private readonly ILocalAdapterClient _localAdapterClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public InteractionController(ILogger<InteractionController> logger, IOrchestrationService orchestrationService, ILocalAdapterClient localAdapterClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        _localAdapterClient = localAdapterClient ?? throw new ArgumentNullException(nameof(localAdapterClient));
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <summary>
    /// Process an interaction request.
    /// </summary>
    /// <param name="request">The interaction request details.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>An IActionResult indicating the result of the processing.</returns>
    [HttpPost("query")]
    public async Task<IActionResult> Post([FromBody] AdapterRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            _logger.LogWarning("Received null request body.");
            return BadRequest(new AdapterResponse(false, "Request body cannot be null.", ErrorMessage: "Request body cannot be null."));
        }

        if (request.PlatformType == PlatformType.Unknown)
        {
            _logger.LogWarning("Received request with Unknown PlatformType.");
            return BadRequest(new AdapterResponse(false, "PlatformType cannot be Unknown.", ErrorMessage: "PlatformType cannot be Unknown."));
        }
        if (string.IsNullOrEmpty(request.ConversationId))
        {
            _logger.LogWarning("Received request with null or empty ConversationId.");
            return BadRequest(new AdapterResponse(false, "ConversationId cannot be null or empty.", ErrorMessage: "ConversationId cannot be null or empty."));
        }
        if (string.IsNullOrEmpty(request.UserId))
        {
            _logger.LogWarning("Received request with null or empty UserId.");
            return BadRequest(new AdapterResponse(false, "UserId cannot be null or empty.", ErrorMessage: "UserId cannot be null or empty."));
        }
        if (string.IsNullOrEmpty(request.QueryText) && (request.ArtifactReferences == null || !request.ArtifactReferences.Any()))
        {
            _logger.LogWarning("Received request with null or empty QueryText and no ArtifactReferences.");
            return BadRequest(new AdapterResponse(false, "QueryText cannot be null or empty if no ArtifactReferences are provided.", ErrorMessage: "QueryText cannot be null or empty if no ArtifactReferences are provided."));
        }

        await _localAdapterClient.PersistInteractionAsync(request, cancellationToken);

        _logger.LogInformation("Received interaction request for ConversationId: {ConversationId}", request.ConversationId);

        try
        {
            _logger.LogInformation("Invoking orchestration service for ConversationId: {ConversationId}...", request.ConversationId);
            Result<AdapterResponse, OrchestrationError> result = await _orchestrationService.ProcessInteractionAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Orchestration successful for ConversationId: {ConversationId}. Response: {ResponseMessage}", 
                    request.ConversationId, result.SuccessValue.ResponseMessage);
                // Assuming a successful orchestration (e.g., queued) returns an AdapterResponse indicating this.
                // If SuccessValue itself is the direct response to send to the client:
                return Ok(result.SuccessValue);
            }
            else // IsFailure
            {
                _logger.LogWarning("Orchestration failed for ConversationId: {ConversationId}. Error: {Error}", 
                    request.ConversationId, result.ErrorValue);

                // Map OrchestrationError to a suitable IActionResult
                switch (result.ErrorValue)
                {
                    case OrchestrationError.InvalidRequest:
                        return BadRequest(new AdapterResponse(false, "The request was invalid.", ErrorMessage: result.ErrorValue.ToString()));
                    
                    case OrchestrationError.ActivationCheckFailed:
                        // This might be considered a "success" from the API perspective (request processed, determined no action needed)
                        // Or it could be a specific client error if activation was expected.
                        // For now, treating as a 200 OK with a specific message.
                        return Ok(new AdapterResponse(true, "Interaction did not meet activation criteria and was ignored.", ErrorMessage: result.ErrorValue.ToString()));

                    case OrchestrationError.PersonaResolutionFailed:
                    case OrchestrationError.ArtifactProcessingFailed:
                    case OrchestrationError.RuntimeExecutionFailed:
                    case OrchestrationError.UnknownError:
                        return Problem(
                            detail: $"Orchestration failed with error: {result.ErrorValue}", 
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Orchestration Error"
                        );

                    case OrchestrationError.OperationCancelled:
                        return StatusCode(StatusCodes.Status499ClientClosedRequest, 
                            new AdapterResponse(false, "Operation was cancelled.", ErrorMessage: result.ErrorValue.ToString()));

                    case OrchestrationError.NotFound: // Example if you had a NotFound error
                        return NotFound(new AdapterResponse(false, "Resource not found.", ErrorMessage: result.ErrorValue.ToString()));

                    default:
                        _logger.LogError("Unhandled OrchestrationError: {Error} for ConversationId: {ConversationId}", result.ErrorValue, request.ConversationId);
                        return Problem(
                            detail: "An unexpected orchestration error occurred.", 
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Unexpected Orchestration Error"
                        );
                }
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
