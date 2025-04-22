// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider and GetKeyedService
using Nucleus.Abstractions.Models; // Used for AdapterRequest, NucleusIngestionRequest, InteractionContext etc.
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
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
    private readonly IServiceProvider _serviceProvider;

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaResolver personaResolver,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task ProcessIngestionRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("[Orchestration] Processing request. CorrelationID: {CorrelationId}, Platform: {PlatformType}, User: {UserId}, Conversation: {ConversationId}",
            request.CorrelationId, request.PlatformType, request.OriginatingUserId, request.OriginatingConversationId);

        // Parse the PlatformType string into the enum
        if (!Enum.TryParse<PlatformType>(request.PlatformType, ignoreCase: true, out var platformTypeEnum))
        {
            _logger.LogWarning("Invalid PlatformType string '{PlatformTypeString}' received in ingestion request. CorrelationID: {CorrelationId}. Cannot route interaction.", request.PlatformType, request.CorrelationId);
            // If we can't parse the platform, we cannot determine the right persona or adapter logic.
            // Handle this as a fundamental failure for this request.
            return; // Stop processing this request.
        }

        // Extract artifact references (placeholder)
        List<ArtifactReference>? artifactReferences = await ExtractArtifactReferencesAsync(request, cancellationToken);

        // Construct AdapterRequest correctly using the parsed enum and other request properties
        var adapterRequest = new AdapterRequest(
            request.PlatformType,                 // PlatformType (string)
            request.OriginatingConversationId,  // ConversationId (string)
            request.OriginatingUserId,            // UserId (string)
            request.TriggeringText ?? string.Empty, // QueryText (string) - Ensure non-null
            request.OriginatingMessageId,       // MessageId (string?)
            null,                               // ReplyToMessageId (string?) - No equivalent in NucleusIngestionRequest
            artifactReferences                  // ArtifactReferences (List<...>?) 
            // Metadata (Dictionary<string, string>?) - Defaulting to null
        );

        try
        {
            // Use properties from the constructed adapterRequest for logging
            _logger.LogInformation("Processing ingestion request for conversation {ConversationId}, user {UserId}", adapterRequest.ConversationId, adapterRequest.UserId);

            // 1. Resolve Persona ID
            // Pass the parsed PlatformType enum and the constructed AdapterRequest to the resolver
            string? resolvedPersonaId = await _personaResolver.ResolvePersonaIdAsync(platformTypeEnum, adapterRequest, cancellationToken);

            _logger.LogInformation("[Orchestration] Attempting to resolve IPersonaManager with key: '{ResolvedKey}'. CorrelationID: {CorrelationId}", resolvedPersonaId ?? "(null)", request.CorrelationId);

            // Construct InteractionContext using the original request, parsed platform type enum, and resolved persona ID
            var interactionContext = new InteractionContext(adapterRequest, platformTypeEnum, resolvedPersonaId);

            // If no persona could be resolved, we cannot proceed.
            if (string.IsNullOrEmpty(resolvedPersonaId))
            {
                _logger.LogWarning("No persona resolved for request. CorrelationID: {CorrelationId}. Cannot route interaction.", request.CorrelationId);
                return; // Or handle as appropriate (e.g., send default non-committal response)
            }

            // 2. Resolve the specific Persona Manager using the resolved ID as the key
            var personaManager = _serviceProvider.GetKeyedService<IPersonaManager>(resolvedPersonaId);

            if (personaManager == null)
            {
                _logger.LogWarning("No IPersonaManager registered for resolved key: {ResolvedPersonaId}. CorrelationID: {CorrelationId}. Cannot route interaction.", resolvedPersonaId, request.CorrelationId);
                return; // Or handle as appropriate
            }

            _logger.LogInformation("Resolved Persona Manager '{ManagerType}' for key '{ResolvedPersonaId}'.", personaManager.GetType().Name, resolvedPersonaId);

            // 3. Check Salience with the resolved Persona Manager
            var salienceResult = await personaManager.CheckSalienceAsync(interactionContext, cancellationToken);

            // 4. Process based on Salience
            if (salienceResult.IsSalient)
            {
                // 4a. Request is Salient - Route to the existing session via the resolved manager
                _logger.LogInformation("Routing salient request to existing session {SessionId} managed by {ManagerType}", salienceResult.SessionId, personaManager.GetType().Name);
                await personaManager.ProcessSalientInteractionAsync(interactionContext, salienceResult.SessionId!, cancellationToken); // SessionId is non-null if IsSalient
                return;
            }
            else
            {
                // 5. Not Salient - Evaluate for New Session Initiation with the resolved manager
                _logger.LogInformation("Request not salient. Evaluating for new session initiation with manager {ManagerType} for persona {PersonaId}", personaManager.GetType().Name, interactionContext.ResolvedPersonaId);
                var evaluationResult = await personaManager.EvaluateForNewSessionAsync(interactionContext, cancellationToken);

                // 6. Initiate New Session or Reject
                if (evaluationResult.ShouldInitiate)
                {
                    // 6a. Initiate New Session - Call manager's initiation logic
                    _logger.LogInformation("Initiating new session {NewSessionId} via manager {ManagerType}", evaluationResult.NewSessionId ?? "(ID TBD)", personaManager.GetType().Name);
                    await personaManager.InitiateNewSessionAsync(interactionContext, cancellationToken);
                    return;
                }
                else
                {
                    // 6b. Manager decided not to initiate - Log
                    _logger.LogInformation("Persona manager {ManagerType} did not initiate a new session for this request.", personaManager.GetType().Name);
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

    // Placeholder method for extracting artifact references from the ingestion request.
    // TODO: Implement logic to parse TriggeringText, MessageId, etc. to find potential references.
    private async Task<List<ArtifactReference>?> ExtractArtifactReferencesAsync(NucleusIngestionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Placeholder: ExtractArtifactReferencesAsync called for CorrelationID {CorrelationId}. Currently returns null.", request.CorrelationId);
        // In the future, this method could:
        // 1. Analyze request.TriggeringText for file paths, URLs, etc.
        // 2. Query a message store using request.OriginatingMessageId if applicable (e.g., for Teams attachments).
        // 3. Construct ArtifactReference objects based on findings.
        await Task.CompletedTask; // To make the method async without real async work yet.
        return null; // Return null for now, as AdapterRequest constructor accepts nullable list.
    }
}
