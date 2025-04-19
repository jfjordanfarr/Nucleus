// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Processing; // Assuming core processing logic lives here
using System.Threading.Tasks;
using System;

namespace Nucleus.ApiService.Controllers;

/// <summary>
/// Handles standardized interaction requests from various client adapters.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InteractionController : ControllerBase
{
    private readonly ILogger<InteractionController> _logger;
    // TODO: Inject the primary Nucleus processing/orchestration service here
    // private readonly IOrchestrationService _orchestrationService;

    public InteractionController(ILogger<InteractionController> logger /*, IOrchestrationService orchestrationService */)
    {
        _logger = logger;
        // _orchestrationService = orchestrationService;
    }

    /// <summary>
    /// Processes a generic interaction request from a client adapter.
    /// </summary>
    /// <param name="request">The adapter request containing session info, input text, and optional artifacts.</param>
    /// <returns>An AdapterResponse with the processing result or error information.</returns>
    [HttpPost]
    public async Task<ActionResult<AdapterResponse>> ProcessInteraction([FromBody] AdapterRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("Received null AdapterRequest in ProcessInteraction.");
            return BadRequest(new AdapterResponse("Request body cannot be null.", IsError: true, ErrorMessage: "Null request body"));
        }

        _logger.LogInformation("Processing interaction for SessionId: {SessionId}. Input: '{InputText}'. ContextId: '{ContextId}' Artifacts: {ArtifactCount}",
            request.SessionId,
            request.InputText,
            request.ContextSourceIdentifier ?? "None",
            request.Artifacts?.Count ?? 0);

        try
        {
            // --- Placeholder for calling the core processing logic --- 
            _logger.LogInformation("TODO: Invoke core processing logic with AdapterRequest...");

            // Example: Simulate processing and create a dummy response
            await Task.Delay(100); // Simulate async work
            var responseText = $"API received input: '{request.InputText}' for session {request.SessionId}. Processing not yet implemented.";
            var sources = request.Artifacts?.Select(a => $"Processed: {a.Type}:{a.Identifier}").ToList();

            var response = new AdapterResponse(responseText, sources);
            // --- End Placeholder ---

            _logger.LogInformation("Interaction processed successfully for SessionId: {SessionId}", request.SessionId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interaction for SessionId: {SessionId}. Error: {ErrorMessage}", request.SessionId, ex.Message);
            return StatusCode(500, new AdapterResponse("An internal error occurred while processing your request.", IsError: true, ErrorMessage: ex.Message));
        }
    }
}
