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
using System.Text.Json;
using Xunit.Sdk; // For Skip
using Nucleus.Abstractions; // For NucleusConstants
using Nucleus.Infrastructure.Testing.Configuration; // For TestConfigurationManager

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>
/// Integration tests specifically targeting Azure Service Bus functionality using the Aspire Test Host.
/// Manages the lifecycle of the distributed application and provides a ServiceBusClient.
/// </summary>
[Trait("Category", "Integration-ServiceBus")]
[Trait("Category", "Integration")]
public class ServiceBusMessagingTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication? _app; // Made nullable
    private ServiceBusClient? _serviceBusClient; // Made nullable
    private readonly bool _skipTests;

    private static bool AreServiceBusTestsEnabled()
    {
        var integrationTestsEnabled = Environment.GetEnvironmentVariable(NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled);
        var serviceBusEnabled = Environment.GetEnvironmentVariable(NucleusConstants.NucleusEnvironmentVariables.AzureServiceBusEnabled);

        return (string.Equals(integrationTestsEnabled, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(integrationTestsEnabled, "1")) &&
               (string.Equals(serviceBusEnabled, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(serviceBusEnabled, "1"));
    }

    public ServiceBusMessagingTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _skipTests = !AreServiceBusTestsEnabled();

        if (_skipTests)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Skipping Service Bus tests: Either {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled} or {NucleusConstants.NucleusEnvironmentVariables.AzureServiceBusEnabled} is not set to 'true'.");
        }
    }

    public async Task InitializeAsync()
    {
        if (_skipTests) return;

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests InitializeAsync START ---");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled} and {NucleusConstants.NucleusEnvironmentVariables.AzureServiceBusEnabled} are 'true'. Proceeding with Service Bus test initialization.");

        try
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Initializing Aspire AppHost for Service Bus tests...");
            
            Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__System", "Warning");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building DistributedApplication...");
            _app = await appHost.BuildAsync();
            
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Starting DistributedApplication...");
            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Started. Waiting for resources to be running...");

            using var ctsSb = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _app.ResourceNotifications.WaitForResourceAsync("servicebus", KnownResourceStates.Running, ctsSb.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Service Bus resource is running.");

            using var ctsApi = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running, ctsApi.Token); 
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API resource is running.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Getting Service Bus connection string...");
            var connectionString = await _app.GetConnectionStringAsync("sbemulatorns");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Service Bus connection string was null or empty.");
            }
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Service Bus connection string obtained.");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Delaying for 2 seconds before creating ServiceBusClient...");
            await Task.Delay(2000);

            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            };

            _serviceBusClient = new ServiceBusClient(connectionString, clientOptions);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ServiceBusClient created manually.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ServiceBusMessagingTests InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during ServiceBusMessagingTests InitializeAsync: {ex}");
            await DisposeAsync();
            throw;
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests InitializeAsync END ---");
    }

    public async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests DisposeAsync START ---");
        if (_serviceBusClient != null)
        {
            try
            {
                await _serviceBusClient.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ServiceBusClient disposed.");
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception during ServiceBusClient Dispose: {ex.Message}");
            }
            finally
            {
                _serviceBusClient = null;
            }
        }
        else
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] _serviceBusClient was null, no need to dispose.");
        }

        if (_app != null)
        {
            try
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed.");
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during DistributedApplication DisposeAsync: {ex.Message}");
            }
            finally
            {
                _app = null;
            }
        }
        else
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] _app was null, nothing to dispose.");
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- ServiceBusMessagingTests DisposeAsync END ---");
    }

    [SkippableFact(Timeout = 60000)]
    public async Task SendMessage_ShouldSucceed()
    {
        Skip.If(_skipTests, $"Service Bus tests skipped: {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled} or {NucleusConstants.NucleusEnvironmentVariables.AzureServiceBusEnabled} not set to \'true\'.");

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SendMessage_ShouldSucceed START ---");
        Assert.NotNull(_serviceBusClient); 

        const string queueName = "nucleus-ingestion-requests";
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Preparing to send message to queue: {queueName}");

        await using var sender = _serviceBusClient.CreateSender(queueName);
        
        // Inlined structure for the message
        var testMessagePayload = new { InteractionId = Guid.NewGuid(), Content = "Hello from integration test!" };
        var messageBody = JsonSerializer.SerializeToUtf8Bytes(testMessagePayload);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(), // Keep a unique message ID for the ServiceBusMessage itself
            CorrelationId = testMessagePayload.InteractionId.ToString() // Use the InteractionId from our payload for correlation
        };

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending message with ID: {serviceBusMessage.MessageId}, CorrelationID: {serviceBusMessage.CorrelationId}");
        await sender.SendMessageAsync(serviceBusMessage);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Message sent successfully.");

        // Basic assertion: no exception means success for sending.
        // More advanced tests could involve a receiver or checking queue metrics if available through emulator.
        Assert.True(true, "Message sending did not throw an exception.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SendMessage_ShouldSucceed END ---");
    }
}
