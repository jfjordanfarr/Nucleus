using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Infrastructure.Testing.Repositories;
using Nucleus.Domain.Personas.Core; // For EmptyAnalysisData
using Xunit.Abstractions; // For ITestOutputHelper
using Microsoft.Azure.Cosmos; // For CosmosClient
using Aspire.Hosting.Testing; // Add Aspire Testing namespace
using Aspire.Hosting; // Add Aspire Hosting namespace for DistributedApplication
using Aspire.Hosting.ApplicationModel; // Added for ResourceStates
using System.Net.Http.Json; // For PostAsJsonAsync
using Xunit;
using Microsoft.Extensions.DependencyInjection; // For GetRequiredService
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Infrastructure.Data.Persistence.Repositories; // Added for CosmosDb repo
using Nucleus.Infrastructure.Data.Persistence; // Corrected namespace for CosmosDb provider
using System.Net;
using System.Threading;

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>
/// Integration tests for the Nucleus API service using Aspire Test Host.
/// Implements IAsyncLifetime for proper setup and teardown of the Aspire DistributedApplication.
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication _app = null!;
    private HttpClient _apiClient = null!;
    private IServiceProvider _serviceProvider = null!;

    // Constructor remains simple, just storing the output helper
    public ApiIntegrationTests(ITestOutputHelper outputHelper) 
    {
        _outputHelper = outputHelper;
    }

    // Moved initialization logic here from constructor and separate method
    public async Task InitializeAsync() // Part of IAsyncLifetime
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InitializeAsync START ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- InitializeAsync START ---");
        try
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Initializing Aspire AppHost for integration tests...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Initializing Aspire AppHost for integration tests...");

            // Ensure AppHost isn't initialized multiple times if tests share context (though not the case here)
            if (_app != null)
            {
                return;
            }

            // Attempt to set logging levels for emulators by setting env vars in the test process
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Setting Logging environment variables for potential emulator inheritance...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Setting Logging environment variables for potential emulator inheritance...");
            Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft", "Warning");
            Environment.SetEnvironmentVariable("Logging__LogLevel__System", "Warning");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building DistributedApplication...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Building DistributedApplication...");
            _app = await appHost.BuildAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Built. Starting AppHost...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: DistributedApplication Built. Starting AppHost...");

            // Start the application asynchronously.
            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Started. Waiting for resources to be running...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: AppHost Started. Waiting for resources to be running...");

            // Create CancellationTokenSources for timeouts
            using var ctsCosmos = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var ctsServiceBus = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            using var ctsApi = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            // Wait for resources to be running using CancellationTokens for timeouts
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for Cosmos DB Emulator...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting for Cosmos DB Emulator...");
            await _app.ResourceNotifications.WaitForResourceAsync("cosmosdb", KnownResourceStates.Running, ctsCosmos.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Cosmos DB Emulator is running.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Cosmos DB Emulator is running.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for Service Bus Emulator...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting for Service Bus Emulator...");
            await _app.ResourceNotifications.WaitForResourceAsync("servicebus", KnownResourceStates.Running, ctsServiceBus.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Service Bus Emulator is running.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Service Bus Emulator is running.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for Nucleus API Service...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting for Nucleus API Service...");
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running, ctsApi.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API Service is running.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Nucleus API Service is running.");

            // Resolve dependencies from the test host's service provider
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Resolving dependencies from Aspire test host...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Resolving dependencies from Aspire test host...");
            _serviceProvider = _app.Services;

            // Create HttpClient for the API service
            _apiClient = _app.CreateHttpClient("nucleusapi");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] HttpClient created for Nucleus API: BaseAddress={_apiClient.BaseAddress}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: HttpClient created for Nucleus API: BaseAddress={_apiClient.BaseAddress}");

            // DO NOT resolve application-specific services like repositories here.
            // _app.Services provides access to resources managed by the test host (e.g., HttpClient factory),
            // not the internal DI container of the 'nucleusapi' service itself.
            // Tests should interact with the API service via HTTP calls.
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during InitializeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during InitializeAsync: {ex}");
            // Ensure DisposeAsync is called even if InitializeAsync fails
            await DisposeAsync();
            throw; // Re-throw the exception to fail the test setup
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InitializeAsync END ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- InitializeAsync END ---");
    }

    // Dispose method from IAsyncLifetime
    public async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- DisposeAsync START ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- DisposeAsync START ---");
        try
        {
            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Disposing DistributedApplication...");
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed.");
                Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: DistributedApplication Disposed.");
            }
            else
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] _app was null, nothing to dispose.");
                Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: _app was null, nothing to dispose.");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during DisposeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during DisposeAsync: {ex.ToString()}"); // Log full exception
            // Don't throw from DisposeAsync if possible, but log aggressively.
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- DisposeAsync END ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- DisposeAsync END ---");
    }

    [Fact(Timeout = 60000)] // Timeout set to 60 seconds
    public async Task BasicHealthCheck_ShouldReturnOk()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: BasicHealthCheck_ShouldReturnOk START ---");
        // Arrange
        Assert.NotNull(_apiClient); // Ensure HttpClient is ready

        // Act
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending request to /health...");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Sending request to /health...");
        var response = await _apiClient.GetAsync("/health");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received response: StatusCode={response.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Received response: StatusCode={response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: BasicHealthCheck_ShouldReturnOk END ---");

    }

    // Add other tests (like CanProcessInteractionAndPersistMetadata) back here as needed.

}
