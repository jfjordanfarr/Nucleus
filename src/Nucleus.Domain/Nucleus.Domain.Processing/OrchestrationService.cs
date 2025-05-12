// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration; 
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Generic; 
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Trace;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Service responsible for orchestrating the processing of interactions,
/// including activation checks, queuing, and coordinating with persona runtimes.
/// </summary>
/// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md">Interaction Processing Lifecycle</seealso>
public class OrchestrationService : IOrchestrationService
{
    private static readonly ActivitySource ActivitySource = new("Nucleus.Domain.Processing.OrchestrationService");

    private readonly ILogger<OrchestrationService> _logger;
    private readonly IActivationChecker _activationChecker; 
    private readonly IBackgroundTaskQueue _backgroundTaskQueue; 
    private readonly IPersonaConfigurationProvider _personaConfigurationProvider; 

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IActivationChecker activationChecker, 
        IBackgroundTaskQueue backgroundTaskQueue, 
        IPersonaConfigurationProvider personaConfigurationProvider
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activationChecker = activationChecker ?? throw new ArgumentNullException(nameof(activationChecker)); 
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue)); 
        _personaConfigurationProvider = personaConfigurationProvider ?? throw new ArgumentNullException(nameof(personaConfigurationProvider)); 
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var activity = ActivitySource.StartActivity(nameof(ProcessInteractionAsync));
        activity?.SetTag("nucleus.platform_type", request.PlatformType);
        activity?.SetTag("nucleus.conversation_id", request.ConversationId);
        activity?.SetTag("nucleus.user_id", request.UserId);
        activity?.SetTag("nucleus.message_id", request.MessageId);

        // Derive a unique ID for tracing, preferring the adapter's message ID.
        string interactionId = request.MessageId ?? Guid.NewGuid().ToString(); 
        activity?.SetTag("nucleus.interaction_id", interactionId);

        try
        {
            _logger.LogInformation(
                "Processing interaction {InteractionId} from {PlatformType} user {UserId} in conversation {ConversationId}",
                interactionId, request.PlatformType, request.UserId, request.ConversationId);

            // 1. Check for Persona Activation
            _logger.LogDebug("Checking activation for interaction {InteractionId}", interactionId);
            var configurations = await _personaConfigurationProvider.GetAllConfigurationsAsync(cancellationToken);
            var activationResult = await _activationChecker.CheckActivationAsync(request, configurations, cancellationToken); 

            if (!activationResult.ShouldActivate)
            {
                _logger.LogInformation("Interaction {InteractionId} did not meet activation criteria.",
                    interactionId);
                activity?.SetStatus(ActivityStatusCode.Ok, "Interaction Ignored");
                return new OrchestrationResult(OrchestrationStatus.Ignored);
            }

            _logger.LogInformation("Interaction {InteractionId} activated persona {PersonaId}",
                interactionId, activationResult.PersonaId);
            activity?.SetTag("nucleus.persona_id", activationResult.PersonaId);

            // 2. Queue for Background Processing
            _logger.LogDebug("Queueing interaction {InteractionId} for background processing.", interactionId);

            var ingestionRequest = new NucleusIngestionRequest(
                PlatformType: request.PlatformType,
                OriginatingUserId: request.UserId,
                OriginatingConversationId: request.ConversationId,
                OriginatingReplyToMessageId: request.ReplyToMessageId, 
                OriginatingMessageId: request.MessageId,         
                ResolvedPersonaId: activationResult.PersonaId, 
                TimestampUtc: DateTimeOffset.UtcNow,            
                QueryText: request.QueryText,
                ArtifactReferences: request.ArtifactReferences,
                CorrelationId: interactionId,                    
                Metadata: request.Metadata                        
            );

            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(ingestionRequest, cancellationToken);

            _logger.LogInformation("Interaction {InteractionId} successfully queued.", interactionId);
            activity?.SetStatus(ActivityStatusCode.Ok, "Interaction Queued");

            return new OrchestrationResult(OrchestrationStatus.Queued, activationResult.PersonaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during orchestration for interaction {InteractionId}", interactionId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            if (activity != null)
            {
                activity.AddException(ex);
            }
            return new OrchestrationResult(
                OrchestrationStatus.UnhandledError,
                null, 
                null, 
                ex.Message,
                ex);
        }
    }
}