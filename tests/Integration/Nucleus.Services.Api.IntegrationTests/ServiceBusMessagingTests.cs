using System;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Nucleus.Services.Api.IntegrationTests.Models; // Added for TestInteractionMessage
using System.Text.Json; // Added for JsonSerializer

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>
/// Integration tests specifically targeting Azure Service Bus functionality using the Aspire Test Host.
/// Manages the lifecycle of the distributed application and provides a ServiceBusClient.
/// </summary>
public class ServiceBusMessagingTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication _app = null!;
    private ServiceBusClient _serviceBusClient = null!;

    public ServiceBusMessagingTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public async Task InitializeAsync() // Part of IAsyncLifetime
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests InitializeAsync START ---");
        try
        {
            if (_app != null) return; // Already initialized

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Initializing Aspire AppHost for Service Bus tests...");
            
            // Set logging environment variables for emulators (inherited by child processes)
            Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__System", "Warning");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building DistributedApplication...");
            _app = await appHost.BuildAsync();
            
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Starting DistributedApplication...");
            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Started. Waiting for resources to be running...");

            // Wait for the Service Bus resource specifically
            await _app.ResourceNotifications.WaitForResourceAsync("servicebus", KnownResourceStates.Running);
             _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Service Bus resource is running.");

             // Wait for the API service to ensure the consumer is likely started
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running); 
             _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API resource is running.");

            // Get the Service Bus connection string
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Getting Service Bus connection string...");
            var connectionString = await _app.GetConnectionStringAsync("sbemulatorns"); // Retrieve the connection string for the specific 'amqp' endpoint
            _outputHelper.WriteLine($"*** Service Bus Connection String Retrieved: {connectionString}"); // Log the connection string provided by Aspire
            _outputHelper.WriteLine($"Retrieved Service Bus Connection String: {connectionString}"); // Re-add logging
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Service Bus connection string was null or empty.");
            }
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Service Bus connection string obtained.");

            // EXPERIMENT: Add a small delay to allow emulator internals to stabilize
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Delaying for 2 seconds before creating ServiceBusClient...");
            await Task.Delay(2000);

            // Configure client options for emulator compatibility
            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            };

            // Create the ServiceBusClient manually
            _serviceBusClient = new ServiceBusClient(connectionString, clientOptions);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ServiceBusClient created manually.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ServiceBusMessagingTests InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during ServiceBusMessagingTests InitializeAsync: {ex}");
            await DisposeAsync(); // Ensure cleanup on failure
            throw;
        }
         _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests InitializeAsync END ---");
    }

    public async Task DisposeAsync() // Part of IAsyncLifetime
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests DisposeAsync START ---");
        try
        {
            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                await _app.DisposeAsync();
                 _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed.");
            }
            else
            {
                 _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] _app was null, nothing to dispose.");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during ServiceBusMessagingTests DisposeAsync: {ex}");
            // Log aggressively, but don't throw from DisposeAsync
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests DisposeAsync END ---");
    }

    [Fact(Timeout = 60000)] // 60 second timeout
    public async Task SendMessage_ShouldSucceed()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SendMessage_ShouldSucceed START ---");
        Assert.NotNull(_serviceBusClient); // Ensure client is initialized

        const string queueName = "nucleus-ingestion-requests";
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Preparing to send message to queue: {queueName}");

        // Arrange
        var testMessage = new TestInteractionMessage(Guid.NewGuid(), "Hello from integration test!");
        var messageBody = JsonSerializer.SerializeToUtf8Bytes(testMessage);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            CorrelationId = testMessage.MessageId.ToString(), // Use MessageId as CorrelationId
            MessageId = Guid.NewGuid().ToString() // Service Bus requires a unique MessageId
        };

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Created test message. CorrelationID: {serviceBusMessage.CorrelationId}, SB MessageId: {serviceBusMessage.MessageId}");

        ServiceBusSender? sender = null;
        try
        {
            // Act
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Creating ServiceBusSender for queue '{queueName}'...");
            sender = _serviceBusClient.CreateSender(queueName);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending message...");
            await sender.SendMessageAsync(serviceBusMessage);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Message sent successfully.");

            // Assert (implicitly successful if no exception is thrown)
            Assert.True(true, "Message sending completed without exceptions.");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during SendMessage_ShouldSucceed: {ex}");
            Assert.Fail($"Message sending failed: {ex.Message}");
        }
        finally
        {
            if (sender != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing ServiceBusSender...");
                await sender.DisposeAsync();
            }
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SendMessage_ShouldSucceed END ---");
        }
    }
}
