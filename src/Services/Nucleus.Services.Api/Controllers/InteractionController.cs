// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models; 
using Nucleus.Abstractions.Orchestration; 
using System.Threading.Tasks;
using System;
using System.Collections.Generic; 
using System.Linq;
using Nucleus.Abstractions;

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
    private readonly IOrchestrationService _orchestrationService;

    public InteractionController(ILogger<InteractionController> logger, IOrchestrationService orchestrationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
    }

    /// <summary>
    /// Processes a generic interaction request from a client adapter.
    /// </summary>
    /// <param name="request">The adapter request containing session info, input text, and optional artifacts.</param>
    /// <returns>An AdapterResponse with the processing result or error information.</returns>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessInteraction([FromBody] AdapterRequest request)
    {
        _logger.LogInformation("Received interaction request for Platform: {PlatformType}, Conversation: {ConversationId}", 
            request.PlatformType, request.ConversationId);

        if (request == null)
        {
            return BadRequest("Request body cannot be null.");
        }

        try
        {
            // Map AdapterRequest to NucleusIngestionRequest
            var ingestionRequest = new NucleusIngestionRequest(
                PlatformType: request.PlatformType,
                OriginatingUserId: request.UserId, // Map UserId to OriginatingUserId
                OriginatingConversationId: request.ConversationId, // Map ConversationId to OriginatingConversationId
                TimestampUtc: DateTimeOffset.UtcNow, // Use current time
                // Convert List<ArtifactReference> to List<PlatformAttachmentReference>
                AttachmentReferences: request.ArtifactReferences?.Select(ar => new PlatformAttachmentReference(
                    PlatformSpecificId: ar.ArtifactId,
                    FileName: null, // Not available in AdapterRequest
                    ContentType: null, // Not available in AdapterRequest
                    SizeBytes: null, // Add null for missing SizeBytes
                    DownloadUrlHint: null, // Not available in AdapterRequest
                    PlatformContext: ar.OptionalContext
                    )).ToList() ?? [], // Ensure non-null list
                OriginatingMessageId: request.MessageId, // Map MessageId to OriginatingMessageId
                TriggeringText: request.QueryText, // Map QueryText to TriggeringText
                CorrelationId: Guid.NewGuid().ToString(), // Generate a correlation ID
                AdditionalPlatformContext: request.Metadata
            );

            // Delegate processing to the orchestration service
            await _orchestrationService.ProcessIngestionRequestAsync(ingestionRequest);

            _logger.LogInformation("Successfully queued interaction for processing. Conversation: {ConversationId}", 
                request.ConversationId);
                
            // TODO: In the future, this might return a more specific response based on initial validation or sync processing.
            // For now, we acknowledge receipt.
            return Ok(new AdapterResponse(Success: true, ResponseMessage: "Request received and queued for processing."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interaction for Conversation: {ConversationId}", request.ConversationId);
            // Return an AdapterResponse indicating failure
            return StatusCode(500, new AdapterResponse(Success: false, ResponseMessage: "An internal error occurred.", ErrorMessage: ex.Message));
        }
    }
}
