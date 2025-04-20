// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Implementation of the core processing orchestration service.
/// This service acts as the central router, coordinating with <see cref="IPersonaResolver"/>
/// and multiple <see cref="IPersonaManager"/> instances to handle incoming requests.
/// It first checks if a message is salient to an existing session via managers, and if not,
/// asks managers to evaluate if they should initiate a new session.
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly ILogger<OrchestrationService> _logger;
    private readonly IPersonaResolver _personaResolver;
    private readonly IEnumerable<IPersonaManager> _personaManagers;

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaResolver personaResolver,
        IEnumerable<IPersonaManager> personaManagers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver));
        _personaManagers = personaManagers ?? throw new ArgumentNullException(nameof(personaManagers));
    }

    /// <inheritdoc />
    public async Task ProcessIngestionRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("[Orchestration] Processing request. CorrelationID: {CorrelationId}, Platform: {PlatformType}, User: {UserId}, Conversation: {ConversationId}",
            request.CorrelationId, request.PlatformType, request.OriginatingUserId, request.OriginatingConversationId);

        // Construct AdapterRequest from NucleusIngestionRequest
        // TODO: Map AttachmentReferences and AdditionalPlatformContext appropriately
        var adapterRequest = new AdapterRequest(
            PlatformType: request.PlatformType,
            ConversationId: request.OriginatingConversationId,
            UserId: request.OriginatingUserId,
            QueryText: request.TriggeringText ?? string.Empty, // Use TriggeringText for QueryText
            MessageId: request.OriginatingMessageId,
            ReplyToMessageId: null, // Not available in NucleusIngestionRequest
            ArtifactReferences: null, // TODO: Map from request.AttachmentReferences
            Metadata: request.AdditionalPlatformContext // Direct mapping for now
        );

        try
        {
            // Use properties from the constructed adapterRequest for logging
            _logger.LogInformation("Processing ingestion request for conversation {ConversationId}, user {UserId}", adapterRequest.ConversationId, adapterRequest.UserId);

            // 1. Resolve Persona ID
            // Use the constructed adapterRequest
            string? resolvedPersonaId = await _personaResolver.ResolvePersonaAsync(adapterRequest, cancellationToken);
            var interactionContext = new InteractionContext(adapterRequest, resolvedPersonaId); // Use adapterRequest and provide resolvedPersonaId

            // 2. Check Salience with Active Persona Managers
            // TODO: Consider parallelizing calls to persona managers if performance becomes an issue.
            string? salientSessionId = null; // Changed name for clarity
            IPersonaManager? salientManager = null; // Track which manager claimed salience

            foreach (var manager in _personaManagers)
            {
                // Use ResolvedPersonaId (corrected name)
                _logger.LogTrace("Checking salience with manager {ManagerType} for persona {PersonaId}", manager.GetType().Name, interactionContext.ResolvedPersonaId);
                var salienceResult = await manager.CheckSalienceAsync(interactionContext, cancellationToken);

                if (salienceResult.IsSalient)
                {
                    // Use ResolvedPersonaId (corrected name)
                    _logger.LogInformation("Request deemed salient by manager {ManagerType} for session {SessionId} and persona {PersonaId}",
                                       manager.GetType().Name, salienceResult.SessionId, interactionContext.ResolvedPersonaId);
                    salientSessionId = salienceResult.SessionId;
                    salientManager = manager;
                    break; // Assuming first salient manager handles the request
                }
            }

            // 3. Process based on Salience
            if (salientManager != null && !string.IsNullOrEmpty(salientSessionId))
            {
                // TODO: 3a. Request is Salient - Route to the existing session via the salient manager
                _logger.LogInformation("Routing salient request to existing session {SessionId} managed by {ManagerType}", salientSessionId, salientManager.GetType().Name);
                // Placeholder: Actual routing logic would involve calling a method on salientManager
                // await salientManager.ProcessSalientInteractionAsync(interactionContext, salientSessionId, cancellationToken);
                // For now, just return a placeholder success
                return;
            }
            else
            {
                // 4. Not Salient - Evaluate for New Session Initiation
                _logger.LogInformation("Request not salient to existing sessions. Evaluating for new session initiation.");
                IPersonaManager? initiatingManager = null;
                NewSessionEvaluationResult? initiationResult = null;

                foreach (var manager in _personaManagers)
                {
                    // Use ResolvedPersonaId (corrected name)
                    _logger.LogTrace("Evaluating new session initiation with manager {ManagerType} for persona {PersonaId}", manager.GetType().Name, interactionContext.ResolvedPersonaId);
                    var evaluationResult = await manager.EvaluateForNewSessionAsync(interactionContext, cancellationToken);

                    // Use ShouldInitiate (corrected name)
                    if (evaluationResult.ShouldInitiate)
                    {
                        // Use ResolvedPersonaId (corrected name)
                        _logger.LogInformation("Manager {ManagerType} indicates new session should be initiated for persona {PersonaId}", manager.GetType().Name, interactionContext.ResolvedPersonaId);
                        initiatingManager = manager;
                        initiationResult = evaluationResult;
                        break; // Assuming first manager willing to initiate handles it
                    }
                }

                // 5. Initiate New Session or Reject
                if (initiatingManager != null && initiationResult != null)
                {
                    // TODO: 5a. Initiate New Session - Call manager's initiation logic
                    // Use NewSessionId (corrected name)
                    _logger.LogInformation("Initiating new session {NewSessionId} via manager {ManagerType}", initiationResult.NewSessionId ?? "(ID TBD)", initiatingManager.GetType().Name);
                    // Placeholder: Actual initiation logic
                    // var newSession = await initiatingManager.InitiateNewSessionAsync(interactionContext, cancellationToken);
                    // For now, just return a placeholder success based on the evaluation
                    return;
                }
                else
                {
                    // 5b. No manager wants to initiate - Reject
                    _logger.LogInformation("No persona manager initiated a new session for this request.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // Use properties from the constructed adapterRequest for logging
            _logger.LogError(ex, "Unhandled exception during ingestion processing for conversation {ConversationId}", adapterRequest.ConversationId);
            // Return type is Task, so we just log and let the exception potentially propagate or be handled upstream if necessary.
            // If specific failure signaling is needed without exceptions, the interface/return type would need adjustment.
            return;
        }
    }
}
