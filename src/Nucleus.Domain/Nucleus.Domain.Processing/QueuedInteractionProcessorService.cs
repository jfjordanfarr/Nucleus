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
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
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
        var scopedProvider = scope.ServiceProvider;

        IOrchestrationService? orchestrationService = null; 
        IEnumerable<IPlatformNotifier>? notifiers = null;
        
        try
        {
            // Resolve necessary services from the scope
            orchestrationService = scopedProvider.GetRequiredService<IOrchestrationService>();
            notifiers = scopedProvider.GetRequiredService<IEnumerable<IPlatformNotifier>>();
            var logger = scopedProvider.GetRequiredService<ILogger<QueuedInteractionProcessorService>>(); // Use injected logger
            
            logger.LogInformation("Processing queued request for ConversationId {ConversationId}", request.OriginatingConversationId);

            // Call the new method designed for queued processing
            OrchestrationResult? result = await orchestrationService.HandleQueuedInteractionAsync(request, cancellationToken);

            // If processing yielded a response message, send it back via the appropriate notifier
            if (result?.Response != null && !string.IsNullOrWhiteSpace(result.Response.ResponseMessage))
            {
                logger.LogInformation("Processing complete for ConversationId {ConversationId}. Attempting to send response notification.", request.OriginatingConversationId);

                var notifier = notifiers.FirstOrDefault(n => 
                    n.SupportedPlatformType.Equals(request.PlatformType, StringComparison.OrdinalIgnoreCase));

                if (notifier == null)
                {
                    logger.LogError("No IPlatformNotifier found for PlatformType: {PlatformType}. Cannot send response for ConversationId: {ConversationId}", 
                        request.PlatformType, request.OriginatingConversationId);
                } 
                else
                {    
                    logger.LogDebug("Found notifier {NotifierType} for PlatformType {PlatformType}. Sending notification...", 
                        notifier.GetType().Name, request.PlatformType);
                        
                    var notificationResult = await notifier.SendNotificationAsync(
                        conversationId: request.OriginatingConversationId,
                        messageText: result.Response.ResponseMessage,
                        replyToMessageId: request.OriginatingMessageId, // Reply to the original message that triggered the async flow
                        cancellationToken: cancellationToken);

                    if (notificationResult.Success)
                    {
                        logger.LogInformation("Successfully sent notification for ConversationId {ConversationId}. Sent Message ID: {SentMessageId}", 
                            request.OriginatingConversationId, notificationResult.SentMessageId ?? "N/A");
                    }
                    else
                    {
                        logger.LogError("Failed to send notification for ConversationId {ConversationId}. Error: {Error}", 
                            request.OriginatingConversationId, notificationResult.Error ?? "Unknown error");
                    }
                }
            }
            else
            {
                 logger.LogInformation("Processing complete for ConversationId {ConversationId}. No response message generated or processing failed before response generation.", 
                    request.OriginatingConversationId);
            }
 
            // Logging below is now handled within HandleQueuedInteractionAsync or ExecuteInteractionProcessingAsync
            /*
            logger.LogInformation("Finished processing queued request for ConversationId {ConversationId}. Status: {Status}, Response Success: {Success}", 
                request.ConversationId, 
                result.Status, 
                result.Response?.Success ?? false); // Log basic outcome
            */
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error occurred while processing queued request for ConversationId {ConversationId}.", 
                request.OriginatingConversationId); 
            // Consider adding retry logic or moving to a dead-letter queue here
        }
        finally
        {
            // Optional: Log scope disposal or other cleanup if needed
            _logger.LogDebug("Disposing scope for request processing of ConversationId {ConversationId}", request.OriginatingConversationId);
        }
    }
}
