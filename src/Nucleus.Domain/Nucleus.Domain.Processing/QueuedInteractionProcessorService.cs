// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// A background service responsible for processing interactions dequeued from a background task queue.
/// This service runs independently of the API request lifecycle, handling long-running or asynchronous tasks.
/// </summary>
/// <remarks>
/// It continuously monitors the <see cref="IBackgroundTaskQueue"/> for new work items (<see cref="NucleusIngestionRequest"/>).
/// When an item is dequeued, it creates a new dependency injection scope, resolves the necessary services
/// (like <see cref="IOrchestrationService"/> and <see cref="IPlatformNotifier"/>), and processes the interaction.
/// </remarks>
/// <seealso href="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// <seealso href="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
public class QueuedInteractionProcessorService : BackgroundService
{
    private readonly ILogger<QueuedInteractionProcessorService> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;

    public QueuedInteractionProcessorService(
        ILogger<QueuedInteractionProcessorService> logger,
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ServiceName} is starting.", nameof(QueuedInteractionProcessorService));

        // Cast to specific type to access DequeueAsync (or update IBackgroundTaskQueue interface)
        if (_taskQueue is not InMemoryBackgroundTaskQueue concreteQueue)
        {
            _logger.LogError("Background task queue is not the expected InMemoryBackgroundTaskQueue type.");
            return; // Cannot proceed
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for an item to become available
                var ingestionRequest = await concreteQueue.DequeueAsync(stoppingToken);
                _logger.LogInformation("Dequeued request for async processing. ConversationId: {ConversationId}, MessageId: {MessageId}",
                    ingestionRequest.OriginatingConversationId, ingestionRequest.OriginatingMessageId);

                // Process the item within a dedicated scope
                await ProcessRequestAsync(ingestionRequest, stoppingToken);

            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("{ServiceName} is stopping due to cancellation request.", nameof(QueuedInteractionProcessorService));
                break; // Exit the loop if cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while dequeuing or processing a background task.");
                // Optional: Delay before retrying the loop to prevent fast failure cycles
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
            }
        }

        _logger.LogInformation("{ServiceName} has stopped.", nameof(QueuedInteractionProcessorService));
    }

    private async Task ProcessRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services (like DbContexts or specific request handlers)
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueuedInteractionProcessorService>>();
        var orchestrationService = scope.ServiceProvider.GetRequiredService<IOrchestrationService>();
        var notifier = scope.ServiceProvider.GetService<Abstractions.IPlatformNotifier>(); // Notifier might not always be configured

        logger.LogInformation("[BackgroundQueue] Dequeued interaction. Request ID (if available): {CorrelationId}, Platform: {PlatformType}, User: {UserId}",
            request.CorrelationId ?? "N/A",
            request.PlatformType,
            request.OriginatingUserId);

        OrchestrationResult? result = null;
        try
        {
            // Construct AdapterRequest from NucleusIngestionRequest
            var adapterRequest = new AdapterRequest(
                PlatformType: request.PlatformType, // Pass the string directly
                ConversationId: request.OriginatingConversationId,
                UserId: request.OriginatingUserId,
                ReplyToMessageId: request.OriginatingReplyToMessageId,
                MessageId: request.OriginatingMessageId,
                QueryText: request.QueryText ?? string.Empty, // Handle potential null
                ArtifactReferences: request.ArtifactReferences,
                Metadata: request.Metadata
            );

            // Call OrchestrationService with the constructed AdapterRequest
            result = await orchestrationService.ProcessInteractionAsync(adapterRequest, cancellationToken);

            logger.LogInformation("[BackgroundQueue] Interaction processing completed. Status: {Status}, PersonaId: {PersonaId}, CorrelationId: {CorrelationId}",
                result.Status,
                result.ResolvedPersonaId ?? "N/A",
                request.CorrelationId ?? "N/A");

            // Check if the OrchestrationResult itself contains a response to send immediately
            // (e.g., if processing failed early or was ignored within ProcessInteractionAsync)
            // OR if the status indicates success and the contained response is the final one.
            if (result.AdapterResponse != null && result.Status != OrchestrationStatus.Queued) // Don't notify if it was just queued again (shouldn't happen here)
            {
                if (notifier != null && notifier.SupportedPlatformType.Equals(request.PlatformType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("[BackgroundQueue] Sending final response via notifier for Platform: {Platform}, ConversationId: {ConversationId}",
                        request.PlatformType, request.OriginatingConversationId);
                    // Send the response contained within the result
                    await notifier.SendNotificationAsync(
                        request.OriginatingConversationId,
                        result.AdapterResponse.ResponseMessage,
                        request.OriginatingReplyToMessageId, // Use ReplyToId if available
                        cancellationToken);
                }
                else
                {
                    logger.LogWarning("[BackgroundQueue] No suitable notifier found or configured for platform {PlatformType}. Cannot send final response.", request.PlatformType);
                }
            }
            // If result.AdapterResponse is null, it implies successful persona execution handled its own notification, or it failed silently.
            // Logging within OrchestrationService/PersonaRuntime should cover those cases.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[BackgroundQueue] Unhandled exception processing dequeued interaction. CorrelationId: {CorrelationId}", request.CorrelationId ?? "N/A");
            // Optionally, attempt to notify about the failure if possible and not already handled
            if (notifier != null && notifier.SupportedPlatformType.Equals(request.PlatformType, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var failureResponse = new AdapterResponse(false, "An unexpected error occurred while processing your request.", ex.Message);
                    await notifier.SendNotificationAsync(
                        request.OriginatingConversationId,
                        failureResponse.ResponseMessage,
                        request.OriginatingReplyToMessageId,
                        cancellationToken);
                }
                catch (Exception notifyEx)
                {
                    logger.LogError(notifyEx, "[BackgroundQueue] Failed to send failure notification after unhandled processing exception.");
                }
            }
        }
    }
}
