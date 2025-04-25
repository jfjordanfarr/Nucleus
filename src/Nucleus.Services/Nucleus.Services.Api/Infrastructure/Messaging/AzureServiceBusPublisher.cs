// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

/// <summary>
/// Implements <see cref="IMessageQueuePublisher{T}"/> using Azure Service Bus.
/// Handles serialization of the payload and publishing to a specified queue or topic.
/// See: ../../../../Docs/Architecture/Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md
/// See: ../../../../Docs/Architecture/Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
public class AzureServiceBusPublisher<T> : IMessageQueuePublisher<T>
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<AzureServiceBusPublisher<T>> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public AzureServiceBusPublisher(ServiceBusClient serviceBusClient, ILogger<AzureServiceBusPublisher<T>> logger)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync(T messagePayload, string queueOrTopicName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messagePayload, nameof(messagePayload));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueOrTopicName, nameof(queueOrTopicName));

        ServiceBusSender? sender = null;
        try
        {
            // Create a sender for the specific queue or topic
            // Consider caching senders if performance becomes an issue, but creation is relatively lightweight.
            sender = _serviceBusClient.CreateSender(queueOrTopicName);

            // Serialize the payload to JSON
            string messageBody = JsonSerializer.Serialize(messagePayload, _jsonSerializerOptions);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json"
                // Optionally set MessageId, SessionId, CorrelationId etc. if needed
                // MessageId = Guid.NewGuid().ToString(),
            };

            _logger.LogInformation("Publishing message of type {MessageType} to Service Bus entity '{ServiceBusEntityName}'", typeof(T).Name, queueOrTopicName);

            // Send the message
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation("Successfully published message of type {MessageType} to Service Bus entity '{ServiceBusEntityName}'", typeof(T).Name, queueOrTopicName);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to serialize message payload of type {MessageType} for Service Bus entity '{ServiceBusEntityName}'.", typeof(T).Name, queueOrTopicName);
            // Re-throw or handle as appropriate for your error strategy
            throw new Exception($"Failed to serialize message payload for Service Bus entity '{queueOrTopicName}'.", jsonEx);
        }
        catch (ServiceBusException sbEx)
        {
            _logger.LogError(sbEx, "Failed to publish message of type {MessageType} to Service Bus entity '{ServiceBusEntityName}'. Reason: {Reason}",
                typeof(T).Name, queueOrTopicName, sbEx.Reason.ToString());
            // Re-throw or handle based on the Reason (e.g., MessagingEntityNotFound, ServiceBusy)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while publishing message of type {MessageType} to Service Bus entity '{ServiceBusEntityName}'.", typeof(T).Name, queueOrTopicName);
            throw;
        }
        finally
        {
            // Ensure the sender is disposed asynchronously
            if (sender != null)
            {
                await sender.DisposeAsync();
            }
        }
    }
}
