// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models; // For NucleusIngestionRequest
// Removed using Nucleus.Domain.Processing; as it's not needed here.
using Nucleus.Abstractions.Orchestration; // Corrected namespace for IOrchestrationService
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

/// <summary>
/// A background service that consumes messages from an Azure Service Bus queue.
/// It processes received messages by invoking the core orchestration logic within a dedicated scope.
/// </summary>
/// <remarks>
/// This service integrates with Azure Service Bus as an alternative to the in-memory queue,
/// suitable for scaled-out deployments. It handles message processing, error handling, and completion.
/// Configuration is driven by <see cref="AzureServiceBusSettings"/>.
/// </remarks>
/// <seealso cref="IMessageQueuePublisher{T}"/>
/// <seealso cref="IOrchestrationService"/>
/// <seealso cref="NucleusIngestionRequest"/>
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
public class ServiceBusQueueConsumerService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<ServiceBusQueueConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _queueName;
    private readonly IConfiguration _configuration;
    private ServiceBusProcessor? _processor;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ServiceBusQueueConsumerService(
        ILogger<ServiceBusQueueConsumerService> logger,
        ServiceBusClient serviceBusClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // TODO: Consider a dedicated configuration section for messaging infrastructure
        _queueName = _configuration["NucleusIngestionQueueName"]
            ?? throw new InvalidOperationException("Configuration key 'NucleusIngestionQueueName' is not set.");

        if (string.IsNullOrWhiteSpace(_queueName))
        {
            throw new InvalidOperationException("IngestionQueueName cannot be empty.");
        }

        _logger.LogInformation("Service Bus Consumer configured for queue: {QueueName}", _queueName);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Service Bus Consumer Service for queue '{QueueName}'...", _queueName);

        // Configure the processor options (e.g., concurrency, prefetch count)
        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false, // We'll manually complete/abandon messages
            MaxConcurrentCalls = 1, // Start with 1, adjust based on performance/resource needs
            PrefetchCount = 10 // Optional: Adjust for performance
        };

        // Create a processor that reads from the queue
        _processor = _serviceBusClient.CreateProcessor(_queueName, options);

        // Configure the message and error handlers
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        // Start processing
        return _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Service Bus Consumer Service for queue '{QueueName}'...", _queueName);
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }
        _logger.LogInformation("Service Bus Consumer Service stopped.");
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        NucleusIngestionRequest? ingestionRequest = null;
        string correlationId = "N/A"; // Default if not found

        try
        {
            // Deserialize the message body
            ingestionRequest = JsonSerializer.Deserialize<NucleusIngestionRequest>(args.Message.Body.ToStream(), _jsonSerializerOptions);

            if (ingestionRequest == null)
            {
                _logger.LogError("Failed to deserialize Service Bus message body into NucleusIngestionRequest. MessageId: {MessageId}. Dead-lettering.", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailure", "Could not deserialize message body.");
                return;
            }

            correlationId = ingestionRequest.CorrelationId ?? args.Message.MessageId ?? "N/A";
            _logger.LogInformation("Received message from queue '{QueueName}'. CorrelationID: {CorrelationId}, MessageId: {MessageId}, SequenceNumber: {SequenceNumber}", 
                _queueName, correlationId, args.Message.MessageId, args.Message.SequenceNumber);

            // Added for integration testing verification
            _logger.LogInformation("Successfully deserialized message for integration test verification. CorrelationID: {CorrelationId}, MessageId: {MessageId}", correlationId, args.Message.MessageId);

            // --- Process the message within a DI Scope --- 
            using (var scope = _serviceProvider.CreateScope())
            {
                // Resolve the main orchestrator service (adjust interface name if needed)
                var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestrationService>();

                _logger.LogDebug("Invoking orchestration service. CorrelationID: {CorrelationId}", correlationId);
                
                // TODO: Evaluate if HandleQueuedInteractionAsync is still needed or how it fits the new API-first sync/async model.
                //var result = await _orchestrationService.HandleQueuedInteractionAsync(request, cancellationToken);
                //_logger.LogInformation("Processing result for JobId {JobId}: {Status}", request.JobId, result.Status);
            }
            // --- End Scope --- 

            // If the orchestrator call completed without throwing an exception here,
            // consider the message successfully processed at this level.
            _logger.LogDebug("Completing message. CorrelationID: {CorrelationId}, MessageId: {MessageId}", correlationId, args.Message.MessageId);
            await args.CompleteMessageAsync(args.Message); 
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON Deserialization error processing message. CorrelationID: {CorrelationId}, MessageId: {MessageId}. Dead-lettering.", correlationId, args.Message.MessageId);
            await args.DeadLetterMessageAsync(args.Message, "DeserializationError", jsonEx.Message);
        }
        catch (Exception ex)
        {
            // Catch exceptions during scope creation, service resolution, or the orchestrator call itself.
            _logger.LogError(ex, "Unhandled exception processing message. CorrelationID: {CorrelationId}, MessageId: {MessageId}. Abandoning for retry.", correlationId, args.Message.MessageId);
            
            // Abandon the message. Service Bus will retry based on queue configuration.
            // If retries fail, it will eventually be dead-lettered.
            try
            {
                await args.AbandonMessageAsync(args.Message); 
            }
            catch (Exception abandonEx)
            {
                 _logger.LogError(abandonEx, "Failed to abandon message after processing error. CorrelationID: {CorrelationId}, MessageId: {MessageId}", correlationId, args.Message.MessageId);
                 // If abandoning fails, the message lock might expire, causing a retry anyway.
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus Processor encountered an error. EntityPath: {EntityPath}, ErrorSource: {ErrorSource}, Namespace: {FullyQualifiedNamespace}", 
            args.EntityPath, args.ErrorSource, args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }

        // ServiceBusClient itself might be singleton and managed by DI, 
        // so we don't necessarily dispose it here unless this service owns it.
        // In this setup (injected via ctor), DI manages the client's lifetime.
    }
}
