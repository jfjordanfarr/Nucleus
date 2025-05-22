// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Results; // Added for Result<TSuccess, TError>
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
    public async Task<Result<AdapterResponse, OrchestrationError>> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
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
                interactionId.Replace("\n", "").Replace("\r", ""), 
                request.PlatformType, 
                (request.UserId ?? "N/A").Replace("\n", "").Replace("\r", ""), 
                (request.ConversationId ?? "N/A").Replace("\n", "").Replace("\r", ""));

            // 1. Check for Persona Activation
            _logger.LogDebug("Checking activation for interaction {InteractionId}", interactionId.Replace("\n", "").Replace("\r", ""));
            var configurations = await _personaConfigurationProvider.GetAllConfigurationsAsync(cancellationToken);
            var activationResult = await _activationChecker.CheckActivationAsync(request, configurations, cancellationToken);

            if (!activationResult.ShouldActivate || string.IsNullOrEmpty(activationResult.PersonaId))
            {
                _logger.LogInformation("Interaction {InteractionId} did not meet activation criteria or PersonaId is missing.",
                    interactionId.Replace("\n", "").Replace("\r", ""));
                activity?.SetStatus(ActivityStatusCode.Ok, "Interaction Ignored or PersonaId Missing");
                return Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.ActivationCheckFailed);
            }

            _logger.LogInformation("Interaction {InteractionId} activated persona {PersonaId}",
                interactionId.Replace("\n", "").Replace("\r", ""), 
                (activationResult.PersonaId ?? "N/A").Replace("\n", "").Replace("\r", ""));
            activity?.SetTag("nucleus.persona_id", activationResult.PersonaId);

            // 2. Queue for Background Processing
            _logger.LogDebug("Queueing interaction {InteractionId} for background processing.", interactionId.Replace("\n", "").Replace("\r", ""));

            var ingestionRequest = new NucleusIngestionRequest(
                PlatformType: request.PlatformType,
                OriginatingUserId: request.UserId ?? "N/A",
                OriginatingConversationId: request.ConversationId ?? "N/A",
                OriginatingReplyToMessageId: request.ReplyToMessageId,
                OriginatingMessageId: request.MessageId,
                ResolvedPersonaId: activationResult.PersonaId ?? "N/A", // PersonaId is checked for null/empty above, but defensive here
                TimestampUtc: DateTimeOffset.UtcNow,
                QueryText: request.QueryText,
                ArtifactReferences: request.ArtifactReferences,
                CorrelationId: interactionId,
                Metadata: request.Metadata,
                TenantId: request.TenantId
            );

            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(ingestionRequest, cancellationToken);

            var sanitizedInteractionId = interactionId.ToString().Replace("\n", " ").Replace("\r", " ");
            var sanitizedPersonaId = activationResult.PersonaId?.ToString().Replace("\n", " ").Replace("\r", " ") ?? "<unknown_persona>";

            _logger.LogInformation("Interaction {InteractionId} successfully queued for persona {PersonaId}.", sanitizedInteractionId, sanitizedPersonaId);
            activity?.SetStatus(ActivityStatusCode.Ok, "Interaction Queued");

            var successResponse = new AdapterResponse(
                Success: true,
                ResponseMessage: $"Request for persona '{sanitizedPersonaId}' received and queued for processing. Interaction ID: {sanitizedInteractionId}"
            );
            return Result<AdapterResponse, OrchestrationError>.Success(successResponse);
        }
        catch (ArgumentNullException ex)
        {
            var sanitizedErrorMessage = ex.Message.Replace("\n", " ").Replace("\r", " ");
            var sanitizedParamName = ex.ParamName?.Replace("\n", " ").Replace("\r", " ") ?? "unknown";
            var sanitizedInteractionId = interactionId.ToString().Replace("\n", " ").Replace("\r", " ");
            _logger.LogError(ex, "ArgumentNull error during orchestration for interaction {InteractionId}: {ErrorMessage} (Parameter: {ParameterName})", sanitizedInteractionId, sanitizedErrorMessage, sanitizedParamName);
            activity?.SetStatus(ActivityStatusCode.Error, $"ArgumentNull: {ex.ParamName}");
            if (activity != null) { activity.AddException(ex); }
            return Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.InvalidRequest);
        }
        catch (Exception ex)
        {
            var sanitizedErrorMessage = ex.Message.Replace("\n", " ").Replace("\r", " ");
            var sanitizedInteractionIdGeneralEx = interactionId.ToString().Replace("\n", " ").Replace("\r", " ");
            _logger.LogError(ex, "Unhandled error during orchestration for interaction {InteractionId}: {ErrorMessage}", sanitizedInteractionIdGeneralEx, sanitizedErrorMessage);
            activity?.SetStatus(ActivityStatusCode.Error, sanitizedErrorMessage);
            if (activity != null)
            {
                activity.AddException(ex);
            }
            return Result<AdapterResponse, OrchestrationError>.Failure(OrchestrationError.UnknownError);
        }
    }
}