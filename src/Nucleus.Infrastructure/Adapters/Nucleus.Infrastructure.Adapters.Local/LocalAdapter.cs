// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Adapters.Local; // Added for ILocalAdapterClient
using Nucleus.Abstractions.Models; // Added for AdapterRequest, AdapterResponse
using Nucleus.Abstractions.Models.ApiContracts; // Added for AdapterRequest, AdapterResponse
using Nucleus.Abstractions.Orchestration;
using System;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Adapters.Local
{
    /// <summary>
    /// Provides a local, in-process adapter client for submitting interactions directly to the Nucleus core.
    /// This adapter is typically used by internal services or test harnesses that run within the same
    /// application domain as the Nucleus API or core processing logic.
    /// </summary>
    public class LocalAdapter : ILocalAdapterClient
    {
        private readonly ILogger<LocalAdapter> _logger;
        private readonly IOrchestrationService _orchestrationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalAdapter"/> class.
        /// </summary>
        /// <param name="logger">The logger for this adapter.</param>
        /// <param name="orchestrationService">The orchestration service for processing interactions.</param>
        public LocalAdapter(ILogger<LocalAdapter> logger, IOrchestrationService orchestrationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        }

        /// <summary>
        /// Submits an interaction request for processing via the local adapter.
        /// </summary>
        /// <param name="request">The adapter request to process.</param>
        /// <returns>An <see cref="AdapterResponse"/> indicating the outcome of the submission attempt.</returns>
        public async Task<AdapterResponse> SubmitInteractionAsync(AdapterRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("SubmitInteractionAsync called with a null request.");
                return new AdapterResponse(false, "Request cannot be null.", ErrorMessage: "AdapterRequest was null.");
            }

            _logger.LogInformation(
                "LocalAdapter: Submitting interaction. InteractionId={InteractionId}, TenantId={TenantId}, PersonaId={PersonaId}, UserId={UserId}, ConversationId={ConversationId}, PlatformType={PlatformType}",
                request.MessageId ?? "N/A",
                request.TenantId ?? "N/A",
                request.PersonaId ?? "N/A",
                request.UserId ?? "N/A",
                request.ConversationId ?? "N/A",
                request.PlatformType);

            try
            {
                OrchestrationResult orchestrationResult = await _orchestrationService.ProcessInteractionAsync(request, System.Threading.CancellationToken.None);

                switch (orchestrationResult.Status)
                {
                    case OrchestrationStatus.Queued:
                        _logger.LogInformation("Interaction {InteractionId} successfully queued by orchestration service. Job/Persona ID: {JobId}",
                            request.MessageId ?? "N/A",
                            orchestrationResult.ResolvedPersonaId ?? "N/A");
                        return new AdapterResponse(
                            Success: true,
                            ResponseMessage: "Interaction submitted for asynchronous processing.",
                            SentMessageId: orchestrationResult.ResolvedPersonaId
                        );

                    case OrchestrationStatus.ValidationFailed:
                    case OrchestrationStatus.ActivationFailed:
                    case OrchestrationStatus.PersonaResolutionFailed:
                    case OrchestrationStatus.ContentExtractionFailed:
                    case OrchestrationStatus.RateLimited:
                    case OrchestrationStatus.UnhandledError:
                        _logger.LogWarning("Orchestration service indicated failure for interaction {InteractionId}: Status {Status}, Error: {Error}",
                            request.MessageId ?? "N/A",
                            orchestrationResult.Status,
                            orchestrationResult.ErrorMessage ?? "No specific error message provided.");
                        if (orchestrationResult.AdapterResponse != null)
                        {
                            return orchestrationResult.AdapterResponse;
                        }
                        return new AdapterResponse(
                            Success: false,
                            ResponseMessage: $"Interaction processing failed: {orchestrationResult.Status}",
                            ErrorMessage: orchestrationResult.ErrorMessage ?? $"Orchestration resulted in {orchestrationResult.Status} without a specific error message."
                        );

                    case OrchestrationStatus.ProcessingIgnored:
                        _logger.LogInformation("Interaction {InteractionId} was ignored by the orchestration service as per activation rules or persona configuration. Status: {Status}",
                            request.MessageId ?? "N/A",
                            orchestrationResult.Status);
                        if (orchestrationResult.AdapterResponse != null)
                        {
                            return orchestrationResult.AdapterResponse; // Likely indicates non-salience
                        }
                        return new AdapterResponse(
                            Success: true, // Technically successful hand-off, but ignored.
                            ResponseMessage: $"Interaction ignored by processing rules: {orchestrationResult.Status}",
                            GeneratedArtifactReference: null // Pass along if available, might be null
                        );

                    default:
                        _logger.LogError("Unhandled OrchestrationStatus '{Status}' for interaction {InteractionId}. Error: {Error}",
                            orchestrationResult.Status,
                            request.MessageId ?? "N/A",
                            orchestrationResult.ErrorMessage ?? "N/A");
                        return new AdapterResponse(
                            Success: false,
                            ResponseMessage: "An unexpected orchestration outcome occurred.",
                            ErrorMessage: $"Unhandled orchestration status: {orchestrationResult.Status}. Details: {orchestrationResult.ErrorMessage ?? "N/A"}"
                        );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in LocalAdapter.SubmitInteractionAsync while processing interaction {InteractionId} for User {UserId}.",
                    request.MessageId ?? "N/A",
                    request.UserId ?? "N/A");
                return new AdapterResponse(
                    Success: false,
                    ResponseMessage: "A critical error occurred while submitting the interaction.",
                    ErrorMessage: ex.Message
                );
            }
        }

        /// <summary>
        /// Persists the details of an interaction request, typically for auditing or secure logging.
        /// </summary>
        /// <param name="request">The <see cref="AdapterRequest"/> to persist.</param>
        /// <param name="cancellationToken">A <see cref="System.Threading.CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PersistInteractionAsync(AdapterRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                _logger.LogWarning("{MethodName} called with a null request.", nameof(PersistInteractionAsync));
                // Returning completed task as the interface is Task, not Task<bool> or similar.
                // The null request won't be processed further by the caller (e.g. InteractionController) which should handle it.
                return Task.CompletedTask;
            }

            // This method provides secure console logging for auditing interaction requests.
            // It logs key non-sensitive identifiers and metadata from the AdapterRequest.
            // Sensitive fields like QueryText or RawMessage are explicitly NOT logged here
            // to prevent accidental exposure, aligning with security best practices.
            // Future OTel instrumentation can pick up these structured console logs.
            _logger.LogInformation(
                "LocalAdapter: Interaction audit log. InteractionId={InteractionId}, TimestampUtc={TimestampUtc}, TenantId={TenantId}, PersonaId={PersonaId}, UserId={UserId}, ConversationId={ConversationId}, PlatformType={PlatformType}, InteractionType={InteractionType}",
                request.MessageId ?? "N/A",      // Serves as the unique ID for this interaction event
                request.TimestampUtc,            // Time of the original request
                request.TenantId ?? "N/A",
                request.PersonaId ?? "N/A",
                request.UserId ?? "N/A",
                request.ConversationId ?? "N/A",
                request.PlatformType,
                request.InteractionType);        // Type of interaction (e.g., Message, Command)

            // No actual persistence to a database is performed here; it's a logging operation.
            return Task.CompletedTask;
        }
    }
}
