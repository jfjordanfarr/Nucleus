// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Adapters.Local; // Added for ILocalAdapterClient
using Nucleus.Abstractions.Models; // Added for AdapterRequest, AdapterResponse
using Nucleus.Abstractions.Models.ApiContracts; // Added for AdapterRequest, AdapterResponse
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Results; // Added for Result<TSuccess, TError> and OrchestrationError
using System;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Adapters.Local
{
    /// <summary>
    /// Provides a local, in-process adapter client for submitting interactions directly to the Nucleus core.
    /// This adapter is typically used by internal services or test harnesses that run within the same
    /// application domain as the Nucleus API or core processing logic.
    /// </summary>
    /// <seealso cref="../../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md"/>
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
                request.PersonaId ?? "N/A", // This might be null if not specified by the caller of LocalAdapter
                request.UserId ?? "N/A",
                request.ConversationId ?? "N/A",
                request.PlatformType);

            try
            {
                Result<AdapterResponse, OrchestrationError> orchestrationResult = await _orchestrationService.ProcessInteractionAsync(request, System.Threading.CancellationToken.None);

                if (orchestrationResult.IsSuccess)
                {
                    _logger.LogInformation("Interaction {InteractionId} successfully processed by orchestration service. Response: {ResponseMessage}",
                        request.MessageId ?? "N/A",
                        orchestrationResult.SuccessValue.ResponseMessage);
                    return orchestrationResult.SuccessValue;
                }
                else // IsFailure
                {
                    _logger.LogWarning("Orchestration service indicated failure for interaction {InteractionId}: Error: {Error}",
                        request.MessageId ?? "N/A",
                        orchestrationResult.ErrorValue);

                    // Map OrchestrationError to an AdapterResponse
                    string errorMessage = $"Orchestration failed: {orchestrationResult.ErrorValue}";
                    string responseMessage = "Interaction processing failed."; // Generic message for failure

                    // Potentially customize messages based on specific OrchestrationError values
                    switch (orchestrationResult.ErrorValue)
                    {
                        case OrchestrationError.InvalidRequest:
                            responseMessage = "The request was invalid.";
                            break;
                        case OrchestrationError.ActivationCheckFailed:
                            responseMessage = "Interaction did not meet activation criteria.";
                            break;
                        // Add other cases as needed
                    }

                    return new AdapterResponse(
                        Success: false,
                        ResponseMessage: responseMessage,
                        ErrorMessage: errorMessage
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during LocalAdapter.SubmitInteractionAsync for interaction {InteractionId}.", request.MessageId ?? "N/A");
                return new AdapterResponse(
                    Success: false,
                    ResponseMessage: "An unexpected error occurred while submitting the interaction.",
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
                (request.MessageId ?? "N/A").Replace("\n", "").Replace("\r", ""),      // Serves as the unique ID for this interaction event
                request.TimestampUtc,            // Time of the original request
                (request.TenantId ?? "N/A").Replace("\n", "").Replace("\r", ""),
                (request.PersonaId ?? "N/A").Replace("\n", "").Replace("\r", ""),
                (request.UserId ?? "N/A").Replace("\n", "").Replace("\r", ""),
                (request.ConversationId ?? "N/A").Replace("\n", "").Replace("\r", ""),
                request.PlatformType,
                request.InteractionType);        // Type of interaction (e.g., Message, Command)

            // No actual persistence to a database is performed here; it's a logging operation.
            return Task.CompletedTask;
        }
    }
}
