using System;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.ApiContracts; // Keep if QueueTestRequest is defined here or in a sub-namespace
using Nucleus.Abstractions.Orchestration; // Keep if IBackgroundTaskQueue is used directly (not in this version)
using Xunit;
using Xunit.Abstractions;
using System.Net.Http.Json;
using Nucleus.Abstractions; // Added for NucleusConstants
using Xunit.Sdk; // Added for Skip.If

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>  
/// Integration tests targeting the IBackgroundTaskQueue using the InMemoryBackgroundTaskQueue implementation.  
/// Relies on the Development environment configuration in Program.cs.  
/// </summary>  
[Trait("Category", "Integration-InMemoryQueue")] // Categorize these tests
[Trait("Category", "Integration")]
public class InMemoryMessagingTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication? _app; // Changed to nullable
    private HttpClient? _apiClient; // Changed to nullable

    // Static property to check if integration tests are enabled via environment variable.
    private static bool ShouldSkipIntegrationTests => 
        !string.Equals(Environment.GetEnvironmentVariable(NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled), "true", StringComparison.OrdinalIgnoreCase);

    public InMemoryMessagingTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public async Task InitializeAsync() // Part of IAsyncLifetime
    {
        // Use Skip.If directly here. If this condition is met, InitializeAsync won't proceed.
        Skip.If(ShouldSkipIntegrationTests, $"Skipping InMemoryMessagingTests. Set {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}=true to enable.");

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InMemoryMessagingTests InitializeAsync START ---");
        try
        { 
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building DistributedApplication...");
            _app = await appHost.BuildAsync();

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Starting DistributedApplication...");
            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Started. Waiting for nucleusapi resource...");

            // Wait for the API service
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API resource is running.");

            _apiClient = _app.CreateHttpClient("nucleusapi");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] HttpClient created for nucleusapi.");

            // Resolve the IBackgroundTaskQueue from the running service
            var serviceProvider = _app.Services.GetRequiredService<IServiceProvider>(); // Get the host's service provider
            // Note: This gets the queue from the *test host's* perspective, might need adjustment
            // if we need the queue *within* the running nucleusapi service container.
            // For now, let's assume the Development environment uses the same registration accessible here.
            // A better approach might be to use the HttpClient to call an endpoint that triggers queueing.

            // **Alternative/Correct Way**: Get service provider specific to the API resource
            // This capability might not be directly exposed by the Testing Host easily.
            // Let's defer resolving the queue directly and focus on tests that *use* the queue via API interaction first.

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] InMemoryMessagingTests InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        { 
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during InMemoryMessagingTests InitializeAsync: {ex}");
            await DisposeAsync(); // Ensure cleanup on failure
            throw;
        }
         _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InMemoryMessagingTests InitializeAsync END ---");
    }

    public async Task DisposeAsync() // Part of IAsyncLifetime
    {
        if (ShouldSkipIntegrationTests && _app == null) // Don't run dispose if tests were skipped and app wasn't initialized
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InMemoryMessagingTests DisposeAsync SKIPPED (tests were not run) ---");
            return;
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InMemoryMessagingTests DisposeAsync START ---");
        try
        {
            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed.");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during InMemoryMessagingTests DisposeAsync: {ex}");
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InMemoryMessagingTests DisposeAsync END ---");
    }

    [SkippableFact(Timeout = 30000)] // Changed from Fact to SkippableFact
    public async Task QueueItemViaApi_ShouldSucceed()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: QueueItemViaApi_ShouldSucceed START ---");
        Assert.NotNull(_apiClient); // apiClient will be null if InitializeAsync was skipped

        // Arrange
        var request = new QueueTestRequest { TestData = "Hello from InMemoryMessagingTests!" };
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Arranged QueueTestRequest: {request.TestData}");

        // Act
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Posting to /api/v1/ingest/queue-test...");
        HttpResponseMessage response = null!;
        try
        {
            response = await _apiClient.PostAsJsonAsync("/api/v1/ingest/queue-test", request);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] API Response Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during API call: {ex}");
            Assert.Fail($"API call failed with exception: {ex.Message}");
        }
        
        // Assert
        Assert.NotNull(response);
        // Expecting 202 Accepted as per our IngestEndpoints logic for queueing
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Assertion successful: API returned {response.StatusCode}.");

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: QueueItemViaApi_ShouldSucceed END ---");
    }

    // TODO: Add tests that verify the QueuedInteractionProcessorService processes items from the InMemory queue (might require access to logs or state).
}
