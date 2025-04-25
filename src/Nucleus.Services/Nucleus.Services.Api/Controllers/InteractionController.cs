using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly ILogger<InteractionController> _logger;
    private readonly IOrchestrationService _orchestrationService;

    public InteractionController(ILogger<InteractionController> logger, IOrchestrationService orchestrationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
    }

    // TODO: Add endpoint for regular interactions (/interaction?)

    /// <summary>
    /// Endpoint for explicit ingestion requests.
    /// </summary>
    /// <param name="request">Request containing artifacts/data to ingest.</param>
    /// <returns>Status indication.</returns>
    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] AdapterRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        try
        {
            _logger.LogInformation("Received ingestion request. Platform: {Platform}, User: {UserId}, Conversation: {ConversationId}, MessageId: {MessageId}",
                request.PlatformType,
                request.UserId,
                request.ConversationId,
                request.MessageId ?? "(null)");

            // Validate the request model (basic checks)
            if (string.IsNullOrWhiteSpace(request.PlatformType) ||
                string.IsNullOrWhiteSpace(request.ConversationId) ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.QueryText))
            {
                _logger.LogWarning("Ingestion request validation failed: Missing PlatformType, ConversationId, UserId, or QueryText.");
                return BadRequest("PlatformType, ConversationId, UserId, and QueryText are required.");
            }

            // Call the orchestration service to handle the request
            var result = await _orchestrationService.ProcessInteractionAsync(request, HttpContext.RequestAborted);

            // Map OrchestrationResult to IActionResult
            return MapOrchestrationResultToAction(result);

        }
        catch (ArgumentException argEx) // Catch specific validation errors if thrown by service
        {
            _logger.LogWarning(argEx, "Invalid arguments during ingestion processing for ConversationId: {ConversationId}", request.ConversationId);
            return BadRequest(new AdapterResponse(Success: false, ResponseMessage: "Invalid request data.", ErrorMessage: argEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing ingestion for ConversationId: {ConversationId}", request.ConversationId);
            // Return a generic 500 for unexpected errors
            return StatusCode(500, new AdapterResponse(Success: false, ResponseMessage: "An unexpected internal error occurred.", ErrorMessage: ex.Message));
        }
    }

    /// <summary>
    /// Endpoint for regular interaction/query requests.
    /// </summary>
    /// <param name="request">Request containing the query and context.</param>
    /// <returns>Status indication or query response.</returns>
    [HttpPost("query")] // Define the route for POST /api/interaction/query
    public async Task<IActionResult> Query([FromBody] AdapterRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        try
        {
            _logger.LogInformation("Received query request. Platform: {Platform}, User: {UserId}, Conversation: {ConversationId}, MessageId: {MessageId}",
                request.PlatformType,
                request.UserId,
                request.ConversationId,
                request.MessageId ?? "(null)");

            // Validate the request model (basic checks) - adjust if query has different requirements
            if (string.IsNullOrWhiteSpace(request.PlatformType) ||
                string.IsNullOrWhiteSpace(request.ConversationId) ||
                string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.QueryText))
            {
                _logger.LogWarning("Query request validation failed: Missing PlatformType, ConversationId, UserId, or QueryText.");
                return BadRequest("PlatformType, ConversationId, UserId, and QueryText are required.");
            }

            // Call the orchestration service to handle the request
            var result = await _orchestrationService.ProcessInteractionAsync(request, HttpContext.RequestAborted);

            // Map OrchestrationResult to IActionResult using the existing helper method
            return MapOrchestrationResultToAction(result);
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid arguments during query processing for ConversationId: {ConversationId}", request.ConversationId);
            return BadRequest(new AdapterResponse(Success: false, ResponseMessage: "Invalid request data.", ErrorMessage: argEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing query for ConversationId: {ConversationId}", request.ConversationId);
            // Return a generic 500 for unexpected errors
            return StatusCode(500, new AdapterResponse(Success: false, ResponseMessage: "An unexpected internal error occurred.", ErrorMessage: ex.Message));
        }
    }

    /// <summary>
    /// Maps the result of the orchestration service to an appropriate HTTP action result.
    /// </summary>
    /// <param name="result">The result from the orchestration service.</param>
    /// <returns>An IActionResult (e.g., Ok, Accepted, BadRequest, StatusCode).</returns>
    private IActionResult MapOrchestrationResultToAction(OrchestrationResult result)
    {
        _logger.LogDebug("Mapping OrchestrationResult. Status: {Status}, Response Provided: {HasResponse}", result.Status, result.Response != null);

        switch (result.Status)
        {
            case OrchestrationStatus.ProcessedSync:
                if (result.Response != null)
                {
                    // For sync, map success/failure to Ok/BadRequest based on the response flag
                    return result.Response.Success
                        ? Ok(result.Response)
                        : BadRequest(result.Response);
                }
                else
                {
                    _logger.LogError("OrchestrationStatus.ProcessedSync returned without an AdapterResponse.");
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new AdapterResponse(false, "Internal Server Error: Sync processing finished without a response.", "Missing Response"));
                }

            case OrchestrationStatus.AcceptedAsync:
                // Return HTTP 202 Accepted, include response body if provided (might contain job ID etc.)
                return Accepted(result.Response ?? new AdapterResponse(true, "Request accepted for asynchronous processing."));

            case OrchestrationStatus.NotActivated:
                // Interaction was intentionally not processed, return Ok with no body.
                // This avoids potential serialization issues with ResponseBodyPipeWriter in tests.
                return Ok();

            case OrchestrationStatus.Error:
                // Return 500 Internal Server Error, include response body if provided (contains error details)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    result.Response ?? new AdapterResponse(false, "An internal orchestration error occurred.", "Unspecified Error"));

            default:
                _logger.LogError("Unhandled OrchestrationStatus encountered: {Status}", result.Status);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new AdapterResponse(false, $"Internal Server Error: Unhandled orchestration status '{result.Status}'.", "Unhandled Status"));
        }
    }
}
