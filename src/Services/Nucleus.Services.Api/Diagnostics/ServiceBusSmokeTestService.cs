// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.ApiService.Diagnostics;

/// <summary>
/// An IHostedService that performs a simple smoke test by sending a message
/// via IMessageQueuePublisher shortly after the application starts.
/// This helps verify the publisher configuration and basic queue connectivity.
/// IMPORTANT: This should generally only be enabled in Development environments.
/// </summary>
public class ServiceBusSmokeTestService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ServiceBusSmokeTestService> _logger;
    private readonly IConfiguration _configuration;

    public ServiceBusSmokeTestService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime appLifetime,
        ILogger<ServiceBusSmokeTestService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Hook into the ApplicationStarted event to ensure all services
        // (including the ServiceBusQueueConsumerService) are running.
        _appLifetime.ApplicationStarted.Register(async () => await RunSmokeTestAsync(cancellationToken));
        return Task.CompletedTask;
    }

    private async Task RunSmokeTestAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{ServiceName}] Application started. Running Service Bus smoke test...", nameof(ServiceBusSmokeTestService));

        // Short delay to give the consumer a chance to fully initialize its listener
        // Might not be strictly necessary but can help avoid race conditions in local dev.
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        // Need to create a scope to resolve scoped/transient services correctly.
        // IMessageQueuePublisher is Singleton, but dependencies might not be.
        // Good practice anyway.
        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        try
        {
            var publisher = scopedProvider.GetRequiredService<IMessageQueuePublisher<NucleusIngestionRequest>>();

            // Get queue name from configuration
            string queueName = _configuration["NucleusIngestionQueueName"]
                ?? throw new InvalidOperationException("Configuration key 'NucleusIngestionQueueName' is not set.");

            // Create the test request using the correct constructor
            var testRequest = new NucleusIngestionRequest(
                PlatformType: "SmokeTest", // Required: Use a specific type for tests
                OriginatingUserId: "smoke-test-user", // Required
                OriginatingConversationId: "smoke-test-conversation", // Required
                TimestampUtc: DateTimeOffset.UtcNow, // Required: Use DateTimeOffset
                AttachmentReferences: [], // Required: Provide an empty list for this test
                OriginatingMessageId: Guid.NewGuid().ToString(), // Optional
                TriggeringText: "This is a smoke test message.", // Optional: Use TriggeringText
                CorrelationId: Guid.NewGuid().ToString(), // Optional
                AdditionalPlatformContext: null // Optional: Explicitly add null
            );

            _logger.LogInformation("[{ServiceName}] Publishing smoke test message with CorrelationID: {CorrelationId} to Queue: {QueueName}...",
                nameof(ServiceBusSmokeTestService), testRequest.CorrelationId, queueName);

            // Call PublishAsync and handle potential exceptions
            await publisher.PublishAsync(testRequest, queueName, cancellationToken);

            _logger.LogInformation("[{ServiceName}] Smoke test message published successfully to Queue: {QueueName}. CorrelationID: {CorrelationId}",
                nameof(ServiceBusSmokeTestService), queueName, testRequest.CorrelationId);

        }
        catch (InvalidOperationException configEx) // Catch specific config error
        {
            _logger.LogError(configEx, "[{ServiceName}] Configuration error: {ErrorMessage}", nameof(ServiceBusSmokeTestService), configEx.Message);
        }
        catch (Exception ex) // Catch general publishing errors
        {
            _logger.LogError(ex, "[{ServiceName}] An exception occurred publishing the smoke test message.", nameof(ServiceBusSmokeTestService));
        }

        _logger.LogInformation("[{ServiceName}] Service Bus smoke test complete.", nameof(ServiceBusSmokeTestService));

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No resources to clean up
        return Task.CompletedTask;
    }
}
