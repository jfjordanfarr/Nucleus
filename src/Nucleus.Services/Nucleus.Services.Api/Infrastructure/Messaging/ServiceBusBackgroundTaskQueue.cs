// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

/// <summary>
/// Configuration settings for the Service Bus background task queue.
/// </summary>
public class ServiceBusQueueSettings
{
    /// <summary>
    /// The name of the Service Bus queue used for background tasks.
    /// Defaults to "nucleus-background-tasks".
    /// </summary>
    public string QueueName { get; set; } = "nucleus-background-tasks";
}

/// <summary>
/// Implements the <see cref="IBackgroundTaskQueue"/> using Azure Service Bus.
/// Requires the Service Bus connection string to be configured (typically injected by Aspire).
/// </summary>
public class ServiceBusBackgroundTaskQueue : IBackgroundTaskQueue, IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceiver _receiver;
    private readonly ILogger<ServiceBusBackgroundTaskQueue> _logger;
    private readonly string _queueName;

    public ServiceBusBackgroundTaskQueue(
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusQueueSettings> settings,
        ILogger<ServiceBusBackgroundTaskQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queueName = settings?.Value?.QueueName ?? throw new ArgumentNullException(nameof(settings), "ServiceBusQueueSettings or its QueueName is null.");
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));

        _sender = _serviceBusClient.CreateSender(_queueName);
        _receiver = _serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });

        _logger.LogInformation("ServiceBusBackgroundTaskQueue initialized for queue '{QueueName}' using injected ServiceBusClient.", _queueName);
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            var messageBody = JsonSerializer.Serialize(request);
            var message = new ServiceBusMessage(messageBody)
            {
                CorrelationId = request.CorrelationId?.ToString()
            };

            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogDebug("Queued work item to Service Bus queue '{QueueName}'. CorrelationId: {CorrelationId}", _queueName, request.CorrelationId ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue work item to Service Bus queue '{QueueName}'. CorrelationId: {CorrelationId}", _queueName, request.CorrelationId ?? "N/A");
            throw; // Re-throw to signal failure
        }
    }

    /// <inheritdoc />
    public async ValueTask<NucleusIngestionRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Wait indefinitely until a message is available or cancellation is requested.
            // BackgroundService cancellation token will propagate here.
            ServiceBusReceivedMessage receivedMessage = await _receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);

            // If cancellationToken is signaled during ReceiveMessageAsync, it throws OperationCanceledException

            var messageBody = receivedMessage.Body.ToString();
            var workItem = JsonSerializer.Deserialize<NucleusIngestionRequest>(messageBody);

            if (workItem == null)
            {
                _logger.LogError("Failed to deserialize message body from Service Bus queue '{QueueName}'. MessageId: {MessageId}", _queueName, receivedMessage.MessageId);
                await _receiver.DeadLetterMessageAsync(receivedMessage, "DeserializationFailure", "Could not deserialize message body.", cancellationToken);
                throw new InvalidOperationException("Failed to deserialize message from Service Bus.");
            }

            _logger.LogDebug("Dequeued work item from Service Bus queue '{QueueName}'. CorrelationId: {CorrelationId}, MessageId: {MessageId}", _queueName, workItem.CorrelationId ?? "N/A", receivedMessage.MessageId);

            // Complete the message AFTER processing should ideally occur.
            // Since IBackgroundTaskQueue doesn't support this, we complete immediately after dequeuing successfully.
            // This assumes the QueuedInteractionProcessorService will handle the work item correctly.
            await _receiver.CompleteMessageAsync(receivedMessage, cancellationToken);

            return workItem;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dequeue operation cancelled for Service Bus queue '{QueueName}'.", _queueName);
            throw; // Re-throw standard cancellation exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while dequeuing from Service Bus queue '{QueueName}'.", _queueName);
            // If an error occurred *before* processing (e.g., connection issue during receive), 
            // the message lock might expire and it will become visible again. If it happened after receive 
            // but before complete, we might abandon or let the lock expire depending on the exception.
            // For simplicity here, we just log and re-throw. Consider abandoning the message in specific cases.
            throw; // Re-throw other exceptions
        }
    }

    /// <summary>
    /// Disposes the Service Bus client and related resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing ServiceBusBackgroundTaskQueue resources for queue '{QueueName}'.", _queueName);
        // Dispose sender and receiver first, then the client
        if (_sender != null)
        {
            try { await _sender.DisposeAsync(); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing ServiceBusSender for queue '{QueueName}'.", _queueName); }
        }
        if (_receiver != null)
        {
            try { await _receiver.DisposeAsync(); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing ServiceBusReceiver for queue '{QueueName}'.", _queueName); }
        }
        if (_serviceBusClient != null)
        {
            try { await _serviceBusClient.DisposeAsync(); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing ServiceBusClient."); }
        }
        GC.SuppressFinalize(this);
    }
}
