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
    private const string BackgroundTaskClientName = "sbbackgroundtasks";
    private const string BackgroundTaskQueueName = "nucleus-background-tasks"; // Match registration

    private readonly ILogger<ServiceBusBackgroundTaskQueue> _logger;
    private readonly IAzureClientFactory<ServiceBusClient> _serviceBusClientFactory; // Inject the factory
    private readonly string _queueName = BackgroundTaskQueueName; // Use constant

    // Flag to prevent multiple dispose calls
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusBackgroundTaskQueue"/> class.
    /// </summary>
    /// <param name="serviceBusClientFactory">The factory to create Azure SDK clients.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceBusClientFactory or logger is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the named Service Bus client cannot be resolved.</exception>
    public ServiceBusBackgroundTaskQueue(
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
        ILogger<ServiceBusBackgroundTaskQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceBusClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceBusClientFactory = serviceBusClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DequeuedMessage<NucleusIngestionRequest>?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        ServiceBusReceivedMessage? receivedMessage = null;
        ServiceBusReceiver? receiver = null; // Keep receiver reference
        ServiceBusClient? serviceBusClient = null; // Keep client reference

        try
        {
            _logger.LogDebug("Attempting to receive message from queue '{QueueName}'.", _queueName);

            // Resolve the named client using the factory
            serviceBusClient = _serviceBusClientFactory.CreateClient(BackgroundTaskClientName);
            _logger.LogDebug("Service Bus client '{ClientName}' created successfully.", BackgroundTaskClientName);

            // Create a receiver
            receiver = serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
            _logger.LogDebug("Service Bus receiver created successfully for queue '{QueueName}'. Waiting for message...", _queueName);

            // Attempt to receive a message (wait time can be configured if needed)
            // TimeSpan.Zero means it won't wait if the queue is empty
            receivedMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), cancellationToken);

            if (receivedMessage == null)
            {
                _logger.LogTrace("No message received from queue '{QueueName}'.", _queueName);
                await DisposeReceiverAsync(receiver); // Dispose receiver if no message
                await DisposeClientAsync(serviceBusClient); // Dispose client if no message
                return null;
            }

            _logger.LogInformation("Message received from queue '{QueueName}'. SequenceNumber: {SequenceNumber}, CorrelationId: {CorrelationId}",
                _queueName, receivedMessage.SequenceNumber, receivedMessage.CorrelationId ?? "N/A");

            // Deserialize the message body
            NucleusIngestionRequest? request = JsonSerializer.Deserialize<NucleusIngestionRequest>(receivedMessage.Body);

            if (request == null)
            {
                _logger.LogError("Failed to deserialize message body for SequenceNumber: {SequenceNumber}. Message will be dead-lettered.", receivedMessage.SequenceNumber);
                // Dead-letter the message if deserialization fails
                await DeadLetterMessageAsync(receivedMessage, "DeserializationError", "Failed to deserialize message body.", CancellationToken.None); // Use separate token
                await DisposeReceiverAsync(receiver); // Dispose receiver after dead-letter
                await DisposeClientAsync(serviceBusClient); // Dispose client
                return null; // Indicate failure to dequeue valid message
            }

            _logger.LogDebug("Message deserialized successfully for SequenceNumber: {SequenceNumber}.", receivedMessage.SequenceNumber);

            // IMPORTANT: Do NOT complete the message here. Return it with context.
            // The receiver and client will be disposed by the caller indirectly via CompleteAsync/AbandonAsync
            // or explicitly if processing fails before calling those.
            return new DequeuedMessage<NucleusIngestionRequest>(request, receivedMessage); 

        }
        catch (ServiceBusException sbe) when (sbe.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogCritical(sbe, "CRITICAL: Service Bus Queue '{QueueName}' not found. Ensure the queue exists and the connection is correct.", _queueName);
            await DisposeReceiverAsync(receiver); // Attempt cleanup
            await DisposeClientAsync(serviceBusClient);
            // Rethrow or handle as appropriate for the application lifecycle
            throw; 
        }
        catch (OperationCanceledException) 
        { 
             _logger.LogInformation("Dequeue operation cancelled for queue '{QueueName}'.", _queueName);
             await DisposeReceiverAsync(receiver);
             await DisposeClientAsync(serviceBusClient);
             return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while dequeuing from Service Bus queue '{QueueName}'.", _queueName);
            // If we got a message but an error occurred before returning, try to abandon it.
            if (receivedMessage != null)
            {
                _logger.LogWarning("Attempting to abandon message {SequenceNumber} due to dequeue error.", receivedMessage.SequenceNumber);
                // We need a separate receiver instance here potentially, as the original might be invalid
                // Or better, handle this within AbandonAsync logic itself
                // For now, log and rely on lock expiry for retry
                // await AbandonMessageAsync(receivedMessage, ex, CancellationToken.None); // Requires refactor or separate receiver
            }
            await DisposeReceiverAsync(receiver);
            await DisposeClientAsync(serviceBusClient);
            return null; // Indicate failure
        }
        // Note: Receiver and client are NOT disposed here if a message is successfully returned.
        // They are implicitly kept alive via the returned ServiceBusReceivedMessage context.
        // Disposal responsibility shifts to CompleteAsync/AbandonAsync.
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        ServiceBusSender? sender = null;
        ServiceBusClient? serviceBusClient = null;

        try
        {
            // Resolve the named client using the factory
            serviceBusClient = _serviceBusClientFactory.CreateClient(BackgroundTaskClientName);
            _logger.LogDebug("Service Bus client '{ClientName}' created successfully.", BackgroundTaskClientName);

            // Create a sender
            sender = serviceBusClient.CreateSender(_queueName);
            _logger.LogDebug("Service Bus sender created successfully for queue '{QueueName}'.", _queueName);

            // Serialize the work item
            string json = JsonSerializer.Serialize(workItem);

            // Create a new message
            ServiceBusMessage message = new ServiceBusMessage(json);

            // Send the message
            await sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Message sent to queue '{QueueName}'.", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while enqueuing to Service Bus queue '{QueueName}'.", _queueName);
        }
        finally
        {
            await DisposeSenderAsync(sender);
            await DisposeClientAsync(serviceBusClient);
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
        await CompleteMessageAsyncInternal(message, cancellationToken);
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

        await AbandonMessageAsyncInternal(message, reason, description, cancellationToken);
    }

    // --- Internal Helper Methods --- 

    // Modified Helper method to complete message and log errors
    private async Task CompleteMessageAsyncInternal(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        ServiceBusReceiver? receiver = null; // Keep receiver reference
        ServiceBusClient? serviceBusClient = null;
        try
        {
            // Resolve the named client using the factory within the method scope
            serviceBusClient = _serviceBusClientFactory.CreateClient(BackgroundTaskClientName);
            _logger.LogDebug("Service Bus client created successfully. Creating receiver for completion.");

            // Create a receiver for the configured queue
            // Note: Ideally, use the SAME receiver that got the message, but that requires significant refactoring.
            // Creating a new one works but is less efficient.
            receiver = serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
            _logger.LogDebug("Transient Service Bus receiver created successfully for queue '{QueueName}' to complete message.", _queueName);

            await receiver.CompleteMessageAsync(message, cancellationToken);
            _logger.LogInformation("Message {SequenceNumber} completed successfully on queue '{QueueName}'.", message.SequenceNumber, _queueName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to complete message {SequenceNumber} from queue '{QueueName}'. It might be processed again or eventually dead-lettered.", message.SequenceNumber, _queueName);
            // Consider implications: maybe try dead-lettering if completion fails?
        }
        finally
        {
            await DisposeReceiverAsync(receiver); // Dispose the transient receiver
            await DisposeClientAsync(serviceBusClient);
        }
    }

    // NEW Helper method to abandon message
    private async Task AbandonMessageAsyncInternal(ServiceBusReceivedMessage message, string reason, string? description, CancellationToken cancellationToken)
    {
        ServiceBusReceiver? receiver = null; // Keep receiver reference
        ServiceBusClient? serviceBusClient = null;
        try
        {
            // Resolve the named client using the factory within the method scope
            serviceBusClient = _serviceBusClientFactory.CreateClient(BackgroundTaskClientName);
            _logger.LogDebug("Service Bus client created successfully. Creating receiver for abandonment.");

            // Create a transient receiver
            receiver = serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
            _logger.LogDebug("Transient Service Bus receiver created successfully for queue '{QueueName}' to abandon message.", _queueName);

            await receiver.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);
            _logger.LogWarning("Message {SequenceNumber} explicitly abandoned on queue '{QueueName}'. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to abandon message {SequenceNumber} from queue '{QueueName}'. Reason: {Reason}. It might be processed again or eventually dead-lettered.", 
                message.SequenceNumber, _queueName, reason);
        }
        finally
        {
            await DisposeReceiverAsync(receiver);
            await DisposeClientAsync(serviceBusClient);
        }
    }

    // Helper method to dead-letter message
    private async Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string reason, string? description, CancellationToken cancellationToken)
    {
        ServiceBusReceiver? receiver = null;
        ServiceBusClient? serviceBusClient = null;
        try
        {
            // Resolve the named client using the factory within the method scope
            serviceBusClient = _serviceBusClientFactory.CreateClient(BackgroundTaskClientName);
            _logger.LogDebug("Service Bus client created successfully. Creating receiver for dead-lettering.");

            // Create a transient receiver for the configured queue
            receiver = serviceBusClient.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });
            _logger.LogDebug("Transient Service Bus receiver created successfully for queue '{QueueName}' to dead-letter message.", _queueName);

            await receiver.DeadLetterMessageAsync(message, reason, description, cancellationToken);
            _logger.LogWarning("Message {SequenceNumber} dead-lettered on queue '{QueueName}'. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to dead-letter message {SequenceNumber} on queue '{QueueName}' after error. Reason: {Reason}", message.SequenceNumber, _queueName, reason);
        }
        finally
        {
            await DisposeReceiverAsync(receiver);
            await DisposeClientAsync(serviceBusClient);
        }
    }

    // --- Disposal Helpers ---

    private async ValueTask DisposeReceiverAsync(ServiceBusReceiver? receiver)
    {
        if (receiver != null)
        {
            try
            {
                await receiver.DisposeAsync();
                _logger.LogTrace("ServiceBusReceiver disposed.");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusReceiver."); }
        }
    }

    private async ValueTask DisposeSenderAsync(ServiceBusSender? sender)
    {
        if (sender != null)
        {
            try
            {
                await sender.DisposeAsync();
                _logger.LogTrace("ServiceBusSender disposed.");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusSender."); }
        }
    }

    private async ValueTask DisposeClientAsync(ServiceBusClient? client)
    {
        if (client != null)
        {
            try
            {
                _logger.LogTrace("Disposing ServiceBusClient (obtained from factory).");
                await client.DisposeAsync(); 
            }
            catch (Exception ex) { _logger.LogError(ex, "Error disposing ServiceBusClient."); }
        }
    }


    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        // Prevent multiple dispose calls
        if (_disposed) return ValueTask.CompletedTask; // Return completed task if already disposed

        _logger.LogInformation("Disposing ServiceBusBackgroundTaskQueue instance for queue '{QueueName}'...", _queueName);

        // Note: We don't hold long-lived clients/receivers at the class level anymore
        // due to the factory pattern. Disposal happens within methods or via helper calls.
        // If a more permanent receiver/client pattern is adopted later, disposal logic here would change.

        // Mark as disposed
        _disposed = true;
        _logger.LogInformation("ServiceBusBackgroundTaskQueue instance disposal complete.");

        // Suppress finalization. Not strictly necessary for sealed classes but good practice.
        GC.SuppressFinalize(this);
        
        return ValueTask.CompletedTask; // Return completed task
    }
}
