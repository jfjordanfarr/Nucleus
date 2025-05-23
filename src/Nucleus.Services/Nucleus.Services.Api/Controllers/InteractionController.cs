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
        // ModelState.IsValid will now check for null/empty ConversationId and UserId due to annotations on AdapterRequest.
        // It also implicitly checks if 'request' itself is null and returns a 400 if so, 
        // but we can keep an explicit null check for clarity or custom logging if desired.
        if (request == null) // This check is somewhat redundant if ASP.NET Core model binding handles it, but good for explicit logging.
        {
            _logger.LogWarning("Received null request body.");
            return new BadRequestObjectResult(new AdapterResponse(false, "Request body cannot be null.", ErrorMessage: "Request body cannot be null."));
        }

        // Explicitly check PlatformType as it's not well-covered by simple annotations for enum defaults.
        if (request.PlatformType == PlatformType.Unknown)
        {
            _logger.LogWarning("Received request with Unknown PlatformType.");
            return new BadRequestObjectResult(new AdapterResponse(false, "PlatformType cannot be Unknown.", ErrorMessage: "PlatformType cannot be Unknown."));
        }

        // Combined validation for QueryText and ArtifactReferences
        if (string.IsNullOrEmpty(request.QueryText) && (request.ArtifactReferences == null || !request.ArtifactReferences.Any()))
        {
            _logger.LogWarning("Received request with null or empty QueryText and no ArtifactReferences.");
            return new BadRequestObjectResult(new AdapterResponse(false, "QueryText cannot be null or empty if no ArtifactReferences are provided.", ErrorMessage: "QueryText cannot be null or empty if no ArtifactReferences are provided."));
        }

        // Check ModelState after custom checks for PlatformType and QueryText/ArtifactReferences
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for AdapterRequest: {ModelStateErrors}", 
                JsonSerializer.Serialize(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage), _jsonSerializerOptions).SanitizeLogInput());
            // Return BadRequestObjectResult with the ModelStateDictionary itself or a ValidationProblemDetails constructed from it.
            // This is more aligned with how [ApiController] typically handles model validation errors.
            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState)
            {
                Title = "Invalid request data.",
                Detail = "One or more validation errors occurred."
                // Instance can be set to HttpContext.Request.Path if needed for more detail
            });
        }

        // The null checks for ConversationId and UserId are now handled by ModelState.IsValid
        // if (string.IsNullOrEmpty(request.ConversationId))
        // {
        //     _logger.LogWarning("Received request with null or empty ConversationId.");
        //     return BadRequest(new AdapterResponse(false, "ConversationId cannot be null or empty.", ErrorMessage: "ConversationId cannot be null or empty."));
        // }
        // if (string.IsNullOrEmpty(request.UserId))
        // {
        //     _logger.LogWarning("Received request with null or empty UserId.");
        //     return BadRequest(new AdapterResponse(false, "UserId cannot be null or empty.", ErrorMessage: "UserId cannot be null or empty."));
        // }

        await _localAdapterClient.PersistInteractionAsync(request, cancellationToken);

        _logger.LogInformation("Received interaction request for ConversationId: {ConversationId}", request.ConversationId.SanitizeLogInput("unknown-conversation"));

        try
        {
            _logger.LogInformation("Invoking orchestration service for ConversationId: {ConversationId}...", request.ConversationId.SanitizeLogInput("unknown-conversation"));
            Result<AdapterResponse, OrchestrationError> result = await _orchestrationService.ProcessInteractionAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Orchestration successful for ConversationId: {ConversationId}. Response: {ResponseMessage}", 
                    request.ConversationId.SanitizeLogInput("unknown-conversation"), result.SuccessValue?.ResponseMessage.SanitizeLogInput("<empty_response>"));
                return Ok(result.SuccessValue);
            }
            else
            {
                var errorType = result.ErrorValue;
                _logger.LogError("Orchestration failed for ConversationId: {ConversationId}. Error: {ErrorType}",
                    request.ConversationId.SanitizeLogInput("unknown-conversation"), 
                    errorType.ToString().SanitizeLogInput());

                switch (errorType)
                {
                    case OrchestrationError.InvalidRequest:
                        return new BadRequestObjectResult(new AdapterResponse(false, "The request was invalid.", ErrorMessage: errorType.ToString()));
                    case OrchestrationError.ActivationCheckFailed:
                        // This case is handled by the OrchestrationService returning a Success=true AdapterResponse
                        // with a specific message. If it were to return an error, this would be the place.
                        // For now, assume the test expectation of OkObjectResult with specific content is met by OrchestrationService.
                        // If OrchestrationService changes to return an error for this, this case needs adjustment.
                        // Based on current tests, OrchestrationService returns Success=true for ActivationCheckFailed.
                        // This path in the controller implies result.IsSuccess was false, which contradicts current test setup for ActivationCheckFailed.
                        // For robustness, if it *did* come here as an error:
                        return Ok(new AdapterResponse(true, "Interaction did not meet activation criteria and was ignored.", ErrorMessage: errorType.ToString()));
                    case OrchestrationError.NotFound:
                        return NotFound(new AdapterResponse(false, "Resource not found.", ErrorMessage: errorType.ToString()));
                    case OrchestrationError.OperationCancelled:
                        return StatusCode(StatusCodes.Status499ClientClosedRequest, new AdapterResponse(false, "Operation was cancelled.", ErrorMessage: errorType.ToString()));
                    case OrchestrationError.PersonaResolutionFailed:
                    case OrchestrationError.ArtifactProcessingFailed:
                    case OrchestrationError.RuntimeExecutionFailed:
                    case OrchestrationError.UnknownError: // Explicitly handle UnknownError
                        return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                        {
                            Title = "Orchestration Error",
                            Status = StatusCodes.Status500InternalServerError,
                            Detail = $"Orchestration failed with error: {errorType}"
                        });
                    default: // For any other unhandled OrchestrationError
                        return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                        {
                            Title = "Unexpected Orchestration Error",
                            Status = StatusCodes.Status500InternalServerError,
                            Detail = "An unexpected orchestration error occurred."
                        });
                }
            }
        }
        catch (ArgumentException argEx) // Catches ArgumentNullException as well
        {
            var sanitizedArgExMessage = argEx.Message.SanitizeLogInput();
            _logger.LogWarning(argEx, "Invalid arguments during interaction processing for ConversationId: {ConversationId}: {ErrorMessage}. Parameter: {ParamName}", 
                request.ConversationId.SanitizeLogInput("unknown-conversation"), 
                sanitizedArgExMessage, 
                (argEx as ArgumentNullException)?.ParamName.SanitizeLogInput() ?? "N/A");
            return new BadRequestObjectResult(new AdapterResponse(false, "Invalid request data.", sanitizedArgExMessage));
        }
        catch (Exception ex) // Catch-all for other unexpected errors
        {
            _logger.LogError(ex, "Unexpected error processing interaction for ConversationId: {ConversationId}", request.ConversationId.SanitizeLogInput("unknown-conversation"));
            return StatusCode(StatusCodes.Status500InternalServerError, new AdapterResponse(false, "An unexpected error occurred.", ErrorMessage: "An unexpected error occurred processing your request."));
        }
    }
}
