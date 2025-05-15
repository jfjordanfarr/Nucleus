// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection; // For CreateScope, GetRequiredService
using Microsoft.Extensions.Hosting; // For BackgroundService
using Microsoft.Extensions.Logging; // For ILogger
using Nucleus.Abstractions; // For IArtifactProvider
using Nucleus.Abstractions.Adapters; // For IPlatformNotifier, IPlatformAttachmentFetcher
using Nucleus.Abstractions.Models; // For NucleusIngestionRequest, AdapterRequest, ArtifactReference, ArtifactContent, PlatformType
using Nucleus.Abstractions.Models.ApiContracts; // For NucleusIngestionRequest
using Nucleus.Abstractions.Models.Configuration; // For PersonaConfiguration
using Nucleus.Abstractions.Orchestration; // For IBackgroundTaskQueue, InteractionContext, ExtractedArtifact, DequeuedMessage, ProcessingStatus
using Nucleus.Domain.Personas.Core.Interfaces; // For IPersonaRuntime, IPersonaConfigurationProvider
using Nucleus.Abstractions.Extraction; // For IContentExtractor
using Nucleus.Infrastructure.Providers.ContentExtraction; // For MimeTypeHelper
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // For JsonSerializer
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Background service responsible for processing interactions dequeued from the <see cref="IBackgroundTaskQueue"/>.
/// It reconstructs context, invokes the appropriate <see cref="IPersonaRuntime"/>, and handles results.
/// </summary>
/// <seealso cref="../../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md#31-service-queuedinteractionprocessorservice">Nucleus Processing Architecture - QueuedInteractionProcessorService</seealso>
/// <seealso href="../../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md">Processing Orchestration Overview</seealso>
/// <seealso href="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md">API Activation & Asynchronous Routing</seealso>
/// <seealso href="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
public class QueuedInteractionProcessorService : BackgroundService
{
    private readonly ILogger<QueuedInteractionProcessorService> _logger;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IArtifactProvider> _artifactProviders;

    public QueuedInteractionProcessorService(
        ILogger<QueuedInteractionProcessorService> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IServiceProvider serviceProvider,
        IEnumerable<IArtifactProvider> artifactProviders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queued Processing Service is starting.");

        while (!cancellationToken.IsCancellationRequested)
        {
            object? messageContext = null; // Store context for completion/abandonment
            NucleusIngestionRequest? requestPayload = null; // Store the deserialized request

            try
            {
                var dequeuedMessage = await _backgroundTaskQueue.DequeueAsync(cancellationToken);

                if (dequeuedMessage != null)
                {
                    messageContext = dequeuedMessage.MessageContext; // Store the context
                    requestPayload = dequeuedMessage.Payload; // Already deserialized by the queue

                    if (requestPayload == null)
                    {
                        _logger.LogError("[BackgroundQueue:{MessageContext}] Dequeued message payload was null. Abandoning message.", messageContext);
                        await _backgroundTaskQueue.AbandonAsync(messageContext, new ArgumentNullException(nameof(dequeuedMessage.Payload), "Dequeued message payload was null."), cancellationToken);
                        continue;
                    }

                    _logger.LogInformation("[BackgroundQueue:{MessageContext}] Dequeued request with CorrelationId: {CorrelationId}. Processing...", 
                        messageContext, requestPayload.CorrelationId ?? "N/A");
                    
                    // Process the request within a scope
                    await ProcessRequestAsync(requestPayload, messageContext, cancellationToken);

                    // If ProcessRequestAsync completes without throwing, the message completion/abandonment is handled within it.
                }
                else
                {
                    // No message dequeued, wait a bit before trying again to avoid tight loop on empty queue
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Queued Processing Service is stopping due to cancellation request.");
                break; // Exit the loop if cancellation is requested
            }
            catch (Exception ex)
            {
                // This catch block handles exceptions during DequeueAsync or if ProcessRequestAsync re-throws
                // (though ProcessRequestAsync is designed to handle its own errors and message lifecycle).
                _logger.LogError(ex, "[BackgroundQueue:{MessageContext}] Unhandled error in ExecuteAsync loop. Attempting to abandon message if context available.", messageContext ?? "UnknownContext");
                if (messageContext != null)
                {
                    try
                    {
                        // Attempt to abandon with the original exception if possible, otherwise create a new one.
                        await _backgroundTaskQueue.AbandonAsync(messageContext, ex, cancellationToken);
                        _logger.LogWarning("[BackgroundQueue:{MessageContext}] Message abandoned due to unhandled exception in ExecuteAsync loop.", messageContext);
                    }
                    catch (Exception abandonEx)
                    {
                        _logger.LogCritical(abandonEx, "[BackgroundQueue:{MessageContext}] CRITICAL: Failed to abandon message after an error in ExecuteAsync. Message might be stuck.", messageContext);
                    }
                }
                // Wait a bit before continuing to prevent rapid error logging if the issue is persistent (e.g., queue connection problem)
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        _logger.LogInformation("Queued Processing Service has stopped.");
    }

    // Modified to accept NucleusIngestionRequest directly and the messageLockToken (messageContext)
    internal async Task ProcessRequestAsync(NucleusIngestionRequest request, object messageContext, CancellationToken cancellationToken)
    {
        // Create a new DI scope for this request processing
        // This ensures that scoped services (like DbContext or other unit-of-work services) are handled correctly.
        using var scope = _serviceProvider.CreateScope();
        var scopedServiceProvider = scope.ServiceProvider;

        // Resolve services from the scoped provider
        var logger = scopedServiceProvider.GetRequiredService<ILogger<QueuedInteractionProcessorService>>(); // Scoped logger
        var personaConfigurationProvider = scopedServiceProvider.GetRequiredService<IPersonaConfigurationProvider>();
        var personaRuntime = scopedServiceProvider.GetRequiredService<IPersonaRuntime>();
        // IPlatformNotifier will be resolved later, based on platform type
        // IArtifactProvider and IContentExtractor are resolved by PersonaRuntime as needed
        // CORRECTED COMMENT: IArtifactProvider is used here in QIPS, IContentExtractor in PersonaRuntime

        try
        {
            logger.LogInformation("[BackgroundQueue:{MessageContext}] Processing request for CorrelationId: {CorrelationId}, User: {UserId}, Persona: {PersonaId}", 
                messageContext, request.CorrelationId, request.OriginatingUserId, request.ResolvedPersonaId);

            // 1. Load Persona Configuration
            var personaConfig = await personaConfigurationProvider.GetConfigurationAsync(request.ResolvedPersonaId ?? string.Empty, cancellationToken);
            if (personaConfig == null)
            {
                logger.LogError("[BackgroundQueue:{MessageContext}] Persona configuration not found for PersonaId: {PersonaId}. Abandoning message.", 
                    messageContext, request.ResolvedPersonaId);
                await _backgroundTaskQueue.AbandonAsync(messageContext, new ArgumentException($"Persona configuration not found for {request.ResolvedPersonaId}"), cancellationToken);
                return;
            }

            // Check if the persona is disabled
            if (!personaConfig.IsEnabled)
            {
                var disabledMessage = $"Persona configuration {request.ResolvedPersonaId} is disabled. Aborting processing for message {request.OriginatingMessageId ?? "unknown"}.";
                logger.LogWarning("[BackgroundQueue:{MessageContext}] {DisabledMessage}", messageContext, disabledMessage);
                await _backgroundTaskQueue.AbandonAsync(messageContext, new InvalidOperationException($"Persona configuration {request.ResolvedPersonaId} is disabled."), cancellationToken);
                return;
            }

            logger.LogDebug("[BackgroundQueue:{MessageContext}] Loaded PersonaConfiguration for {PersonaId}.", messageContext, personaConfig.PersonaId);

            // Log with the resolved PersonaId from the configuration for consistency
            using var personaScope = logger.BeginScope(new Dictionary<string, object>
            {
                ["PersonaId"] = personaConfig.PersonaId
            });

            logger.LogInformation("[BackgroundQueue:{MessageContext}] Processing dequeued request for Persona: {PersonaName} ({PersonaId_Config}) using Platform: {PlatformType}", 
                messageContext, personaConfig.DisplayName, personaConfig.PersonaId, request.PlatformType);

            // 2. Construct InteractionContext
            //    Fetch artifact content first, then create AdapterRequest and InteractionContext.

            var fetchedArtifactContents = new List<ArtifactContent>();
            if (request.ArtifactReferences != null && request.ArtifactReferences.Any())
            {
                logger.LogInformation("[BackgroundQueue:{MessageContext}] {ArtifactCount} artifact references found. Attempting to fetch content.", 
                    messageContext, request.ArtifactReferences.Count);

                foreach (var artifactRef in request.ArtifactReferences)
                {
                    if (artifactRef == null || string.IsNullOrWhiteSpace(artifactRef.ReferenceType) || string.IsNullOrWhiteSpace(artifactRef.ReferenceId))
                    {
                        logger.LogWarning("[BackgroundQueue:{MessageContext}] Skipping invalid artifact reference (null, or missing ReferenceType/ReferenceId).", messageContext);
                        continue;
                    }

                    logger.LogDebug("[BackgroundQueue:{MessageContext}] Attempting to find provider for Artifact ReferenceId: {ArtifactRefId}, Type: {ArtifactRefType}", 
                        messageContext, artifactRef.ReferenceId, artifactRef.ReferenceType);

                    var provider = _artifactProviders.FirstOrDefault(p => 
                        p.SupportedReferenceTypes.Any(spt => 
                            string.Equals(spt, artifactRef.ReferenceType, StringComparison.OrdinalIgnoreCase)));

                    if (provider != null)
                    {
                        logger.LogDebug("[BackgroundQueue:{MessageContext}] Found provider for Artifact ReferenceId: {ArtifactRefId}. Type: {ProviderType}. Fetching content.", 
                            messageContext, artifactRef.ReferenceId, provider.GetType().Name);
                        try
                        {
                            var artifactContent = await provider.GetContentAsync(artifactRef, cancellationToken);
                            if (artifactContent != null)
                            {
                                fetchedArtifactContents.Add(artifactContent);
                                logger.LogInformation("[BackgroundQueue:{MessageContext}] Successfully fetched content for Artifact ReferenceId: {ArtifactRefId}. ContentType: {ContentType}, Length: {Length}", 
                                    messageContext, artifactRef.ReferenceId, artifactContent.ContentType, artifactContent.ContentStream?.Length ?? -1);
                            }
                            else
                            {
                                logger.LogWarning("[BackgroundQueue:{MessageContext}] Provider {ProviderType} returned null content for Artifact ReferenceId: {ArtifactRefId}.", 
                                    messageContext, provider.GetType().Name, artifactRef.ReferenceId);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "[BackgroundQueue:{MessageContext}] Error fetching content for Artifact ReferenceId: {ArtifactRefId} using provider {ProviderType}.", 
                                messageContext, artifactRef.ReferenceId, provider.GetType().Name);
                            // Optionally, decide if this error should lead to message abandonment earlier.
                            // For now, we collect what we can and proceed.
                        }
                    }
                    else
                    {
                        logger.LogWarning("[BackgroundQueue:{MessageContext}] No IArtifactProvider found for ReferenceType: {ReferenceType} (ArtifactRefId: {ArtifactRefId}). Skipping artifact.", 
                            messageContext, artifactRef.ReferenceType, artifactRef.ReferenceId);
                    }
                }
            }
            else
            {
                logger.LogDebug("[BackgroundQueue:{MessageContext}] No artifact references found in the request.", messageContext);
            }

            // Create the AdapterRequest for the InteractionContext from the NucleusIngestionRequest
            var adapterRequestForContext = new AdapterRequest(
                PlatformType: request.PlatformType,
                ConversationId: request.OriginatingConversationId,
                UserId: request.OriginatingUserId,
                QueryText: request.QueryText ?? string.Empty,
                MessageId: request.OriginatingMessageId,
                ReplyToMessageId: request.OriginatingReplyToMessageId,
                ArtifactReferences: request.ArtifactReferences?.Select(a => new ArtifactReference(a.ReferenceId, a.ReferenceType, a.SourceUri, string.Empty /* TenantId - TODO: Review this, NucleusIngestionRequest has no TenantId */, a.FileName, a.MimeType)).ToList(),
                Metadata: request.Metadata,
                PersonaId: personaConfig.PersonaId,
                TenantId: null,
                TimestampUtc: request.TimestampUtc,
                InteractionType: null
            );

            // Now create the InteractionContext
            var interactionContext = new InteractionContext(
                originalRequest: adapterRequestForContext,
                platformType: adapterRequestForContext.PlatformType, // Use PlatformType from the created AdapterRequest
                resolvedPersonaId: personaConfig.PersonaId, // Use the ID from the loaded persona configuration
                rawArtifacts: fetchedArtifactContents, // Contains successfully fetched artifacts
                processedArtifacts: new List<ExtractedArtifact>() // Initialize with empty list
            );

            logger.LogDebug("[BackgroundQueue:{MessageContext}] InteractionContext created. ResolvedPersonaId: {ResolvedPersonaId}, Artifacts fetched: {ArtifactCount}", 
                messageContext, interactionContext.ResolvedPersonaId, interactionContext.RawArtifacts.Count);

            // 3. Invoke Persona Runtime
            logger.LogInformation("[BackgroundQueue:{MessageContext}] Invoking PersonaRuntime for PersonaId: {PersonaId}",
                messageContext, interactionContext.ResolvedPersonaId);

            var (adapterResponse, personaStatus) = await personaRuntime.ExecuteAsync(personaConfig, interactionContext, cancellationToken);

            logger.LogInformation("[BackgroundQueue:{MessageContext}] PersonaRuntime execution completed. Status: {Status}, Success: {Success}, Message: '{ResponseMessage}'", 
                messageContext, personaStatus, adapterResponse.Success, adapterResponse.ResponseMessage?.Substring(0, Math.Min(adapterResponse.ResponseMessage.Length, 100)));

            // 4. Handle Response - Send notification if needed
            if (adapterResponse.Success && !string.IsNullOrWhiteSpace(adapterResponse.ResponseMessage))
            {
                try
                {
                    // Resolve IPlatformNotifier based on the original request's platform type
                    var platformNotifiers = scopedServiceProvider.GetServices<IPlatformNotifier>();
                    var notifier = platformNotifiers.FirstOrDefault(n => n.SupportedPlatformType.Equals(request.PlatformType.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (notifier != null)
                    {
                        logger.LogInformation("[BackgroundQueue:{MessageContext}] Sending notification via {PlatformType} notifier. ConversationId: {ConversationId}, ReplyTo: {ReplyToId}", 
                            messageContext, request.PlatformType, request.OriginatingConversationId, request.OriginatingReplyToMessageId ?? request.OriginatingMessageId);
                        
                        (bool sentSuccess, string? sentMessageId, string? error) = await notifier.SendNotificationAsync(
                            request.OriginatingConversationId,
                            adapterResponse.ResponseMessage,
                            request.OriginatingReplyToMessageId ?? request.OriginatingMessageId, // Use ReplyTo if available, else original message for context
                            cancellationToken);

                        if (sentSuccess)
                        {
                            logger.LogInformation("[BackgroundQueue:{MessageContext}] Notification sent successfully. SentMessageId: {SentMessageId}", messageContext, sentMessageId ?? "N/A");
                            // Optionally, update AdapterResponse with SentMessageId if it's a mutable property or re-create if needed.
                        }
                        else
                        {
                            logger.LogError("[BackgroundQueue:{MessageContext}] Failed to send notification. Error: {Error}", messageContext, error ?? "Unknown notifier error.");
                            // Decide if this failure should cause the message to be abandoned or if it's a non-critical error.
                            // For now, we'll log and complete, assuming the core persona work was done.
                        }
                    }
                    else
                    {
                        logger.LogWarning("[BackgroundQueue:{MessageContext}] No IPlatformNotifier found for PlatformType: {PlatformType}. Cannot send response.", messageContext, request.PlatformType);
                    }
                }
                catch (Exception notifierEx)
                {
                    logger.LogError(notifierEx, "[BackgroundQueue:{MessageContext}] Error during platform notification.", messageContext);
                    // Non-critical for message completion, core persona work done.
                }
            }

            // 5. Message Lifecycle Management based on PersonaExecutionStatus
            if (personaStatus == PersonaExecutionStatus.Success || 
                personaStatus == PersonaExecutionStatus.Filtered || 
                personaStatus == PersonaExecutionStatus.NoActionTaken)
            {
                await _backgroundTaskQueue.CompleteAsync(messageContext, cancellationToken);
                logger.LogInformation("[BackgroundQueue:{MessageContext}] Message completed as persona action resulted in status: {Status}.", messageContext, personaStatus);
            }
            else // Includes Failed, ErrorInResourceFetch, ErrorInExtraction, ErrorInPersonaLogic
            {
                logger.LogWarning("[BackgroundQueue:{MessageContext}] Persona execution finished with non-success status {Status}. Message: {ErrorMessage}. Abandoning message.",
                    messageContext, personaStatus, adapterResponse.ErrorMessage ?? "N/A");
                
                // Create an exception to pass to AbandonAsync if AdapterResponse has an error message
                Exception? abandonException = null;
                if (!string.IsNullOrWhiteSpace(adapterResponse.ErrorMessage))
                {
                    abandonException = new Exception($"Persona execution failed: {adapterResponse.ErrorMessage} (Status: {personaStatus})");
                }
                else if (personaStatus == PersonaExecutionStatus.Failed)
                {
                    abandonException = new Exception($"Persona execution failed with status {personaStatus} but no specific error message was provided in AdapterResponse.");
                }

                await _backgroundTaskQueue.AbandonAsync(messageContext, abandonException, cancellationToken); 
                logger.LogWarning("[BackgroundQueue:{MessageContext}] Message abandoned due to persona execution status: {Status}.", messageContext, personaStatus);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[BackgroundQueue:{MessageContext}] Unhandled error during processing of request. Attempting to abandon message.", messageContext);
            try
            {
                await _backgroundTaskQueue.AbandonAsync(messageContext, ex, cancellationToken);
                logger.LogWarning("[BackgroundQueue:{MessageContext}] Message abandoned due to unhandled exception during processing.", messageContext);
            }
            catch (Exception abandonEx)
            {
                logger.LogCritical(abandonEx, "[BackgroundQueue:{MessageContext}] CRITICAL: Failed to abandon message after processing error. Message might be stuck.", messageContext);
            }
            // Do not re-throw from ProcessRequestAsync; error is logged, message abandoned. Loop in ExecuteAsync will continue.
        }
    }
}
