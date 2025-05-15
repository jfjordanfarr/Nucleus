// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
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
/// Implements the <see cref="IBackgroundTaskQueue"/> using Azure Service Bus for reliable messaging.
/// </summary>
/// <remarks>
/// This service is responsible for sending interaction requests to a Service Bus queue for asynchronous processing.
/// It utilizes the <see cref="ServiceBusSender"/> provided via DI from Aspire's component model.
/// </remarks>
/// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md">API Activation & Asynchronous Routing</seealso>
public sealed class ServiceBusBackgroundTaskQueue : IBackgroundTaskQueue, IAsyncDisposable
{
    // Constants
    private const string BackgroundTaskQueueName = "nucleus-background-tasks"; // Match registration


    private readonly ILogger<ServiceBusBackgroundTaskQueue> _logger;
    private readonly string _queueName = BackgroundTaskQueueName;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceiver _receiver;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusBackgroundTaskQueue"/> class.
    /// </summary>
    /// <param name="serviceBusClient">The Service Bus client.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceBusClient or logger is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the named Service Bus client cannot be resolved.</exception>
    public ServiceBusBackgroundTaskQueue(
        ServiceBusClient serviceBusClient,
        ILogger<ServiceBusBackgroundTaskQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceBusClient);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _logger.LogInformation("Initializing ServiceBusBackgroundTaskQueue with QueueName: {QueueName}", _queueName);

        _serviceBusClient = serviceBusClient;
        _sender = _serviceBusClient.CreateSender(_queueName);
        _receiver = _serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
    }

    /// <inheritdoc />
    public async Task<DequeuedMessage<NucleusIngestionRequest>?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting to receive message from queue '{QueueName}'.", _queueName);
            var receivedMessage = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (receivedMessage == null)
            {
                _logger.LogTrace("No message received from queue '{QueueName}'.", _queueName);
                return null;
            }
            _logger.LogInformation("Message received from queue '{QueueName}'. SequenceNumber: {SequenceNumber}, CorrelationId: {CorrelationId}",
                _queueName, receivedMessage.SequenceNumber, receivedMessage.CorrelationId ?? "N/A");
            NucleusIngestionRequest? request = JsonSerializer.Deserialize<NucleusIngestionRequest>(receivedMessage.Body);
            if (request == null)
            {
                _logger.LogError("Failed to deserialize message body for SequenceNumber: {SequenceNumber}. Message will be dead-lettered.", receivedMessage.SequenceNumber);
                await DeadLetterMessageAsync(receivedMessage, "DeserializationError", "Failed to deserialize message body.", CancellationToken.None);
                return null;
            }
            _logger.LogDebug("Message deserialized successfully for SequenceNumber: {SequenceNumber}.", receivedMessage.SequenceNumber);
            return new DequeuedMessage<NucleusIngestionRequest>(request, receivedMessage);
        }
        catch (ServiceBusException sbe) when (sbe.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogCritical(sbe, "CRITICAL: Service Bus Queue '{QueueName}' not found. Ensure the queue exists and the connection is correct.", _queueName);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dequeue operation cancelled for queue '{QueueName}'.", _queueName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while dequeuing from Service Bus queue '{QueueName}'.", _queueName);
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        try
        {
            string json = JsonSerializer.Serialize(workItem);
            ServiceBusMessage message = new ServiceBusMessage(json);
            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Message sent to queue '{QueueName}'.", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while enqueuing to Service Bus queue '{QueueName}'.", _queueName);
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync(object messageContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageContext);
        if (messageContext is not ServiceBusReceivedMessage message)
        {
            throw new ArgumentException($"messageContext must be of type {nameof(ServiceBusReceivedMessage)}.", nameof(messageContext));
        }

        _logger.LogInformation("Attempting to complete message {SequenceNumber} for queue '{QueueName}'.", message.SequenceNumber, _queueName);
        try
        {
            await _receiver.CompleteMessageAsync(message, cancellationToken);
            _logger.LogInformation("Message {SequenceNumber} completed successfully on queue '{QueueName}'.", message.SequenceNumber, _queueName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to complete message {SequenceNumber} from queue '{QueueName}'. It might be processed again or eventually dead-lettered.", message.SequenceNumber, _queueName);
        }
    }

    /// <inheritdoc />
    public async Task AbandonAsync(object messageContext, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageContext);
        if (messageContext is not ServiceBusReceivedMessage message)
        {
            throw new ArgumentException($"messageContext must be of type {nameof(ServiceBusReceivedMessage)}.", nameof(messageContext));
        }

        var reason = exception?.GetType().Name ?? "ProcessingFailure";
        var description = exception?.Message;

        _logger.LogWarning(exception, "Attempting to abandon message {SequenceNumber} for queue '{QueueName}'. Reason: {Reason}", 
            message.SequenceNumber, _queueName, reason);

        try
        {
            await _receiver.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);
            _logger.LogWarning("Message {SequenceNumber} explicitly abandoned on queue '{QueueName}'. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to abandon message {SequenceNumber} from queue '{QueueName}'. Reason: {Reason}. It might be processed again or eventually dead-lettered.", message.SequenceNumber, _queueName, reason);
        }
    }

    // --- Internal Helper Methods --- 



    // Helper method to dead-letter message
    private async Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string reason, string? description, CancellationToken cancellationToken)
    {
        try
        {
            await _receiver.DeadLetterMessageAsync(message, reason, description, cancellationToken);
            _logger.LogWarning("Message {SequenceNumber} dead-lettered on queue '{QueueName}'. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to dead-letter message {SequenceNumber} on queue '{QueueName}' after error. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
    }




    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _logger.LogInformation("Disposing ServiceBusBackgroundTaskQueue instance for queue '{QueueName}'...", _queueName);
        try { await _sender.DisposeAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusSender."); }
        try { await _receiver.DisposeAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusReceiver."); }
        try { await _serviceBusClient.DisposeAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusClient."); }
        _disposed = true;
        _logger.LogInformation("ServiceBusBackgroundTaskQueue instance disposal complete.");
        GC.SuppressFinalize(this);
    }
}
