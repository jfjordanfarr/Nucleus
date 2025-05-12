using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Adapters.Local;
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

        // Persist the interaction securely before further processing
        await _localAdapterClient.PersistInteractionAsync(request, cancellationToken);

        _logger.LogInformation("Received interaction request for ConversationId: {ConversationId}", request.ConversationId);

        try
        {
            // Extract user information if available (e.g., from JWT)
            // For now, we might pass a default or null user
            // string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            // _logger.LogInformation("Processing request for User ID: {UserId}", userId);

            // The 'files' parameter is removed, pass null or an empty collection if the interface still requires it temporarily.
            var result = await _orchestrationService.ProcessInteractionAsync(request, cancellationToken); // Pass request and cancellationToken

            _logger.LogInformation("Invoking orchestration service...");
            _logger.LogInformation("Orchestration result: {Status}", result.Status);

            // Map orchestration result to HTTP response
            return result.Status switch
            {
                // Immediate success with a response
                OrchestrationStatus.Success when result.AdapterResponse != null =>
                    Ok(result.AdapterResponse), // Use correct enum: Success, Access response via AdapterResponse

                // Accepted for asynchronous processing (HTTP 202)
                OrchestrationStatus.Queued =>
                    Accepted(), // Use correct enum: Queued

                // Interaction ignored (e.g., didn't meet activation criteria)
                OrchestrationStatus.Ignored =>
                    Ok(), // Use correct enum: Ignored - Return 200 OK, as it's not an error, just no action needed.

                // Failure cases - return Problem details
                OrchestrationStatus.Failed or
                OrchestrationStatus.PersonaResolutionFailed or
                OrchestrationStatus.ActivationCheckFailed or
                OrchestrationStatus.ArtifactProcessingFailed or
                OrchestrationStatus.RuntimeExecutionFailed =>
                    Problem(detail: result.ErrorMessage ?? "Orchestration failed.", statusCode: StatusCodes.Status500InternalServerError),

                // Unhandled error case
                OrchestrationStatus.UnhandledError =>
                    Problem(detail: result.ErrorMessage ?? "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError),

                // Default/Unexpected status - should ideally not happen
                _ => Problem(detail: $"Unexpected orchestration status: {result.Status}", statusCode: StatusCodes.Status500InternalServerError)
            };
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
