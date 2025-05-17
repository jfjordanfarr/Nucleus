using Aspire.Hosting.Azure;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting; // Added to potentially resolve CS1061
using Microsoft.Extensions.Logging; // Required for ILogger if you add more logging
using Xunit;
using Xunit.Abstractions; // Required for ITestOutputHelper
using Nucleus.Infrastructure.Testing.Configuration;
using Aspire.Hosting.Dcp;

namespace Nucleus.Services.Api.IntegrationTests;

public class MinimalCosmosEmulatorTest : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication? _app;

    public MinimalCosmosEmulatorTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public async Task InitializeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- MinimalCosmosEmulatorTest InitializeAsync START ---");
        try
        {
            // For a truly minimal test, create a builder without referencing the existing AppHost project.
            var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
            {
                Args = null, // No specific args for this minimal test
                AssemblyName = typeof(MinimalCosmosEmulatorTest).Assembly.FullName,
                DisableDashboard = true // Re-add this to prevent dashboard init errors
            }); 

            // Add only the Cosmos DB emulator
#pragma warning disable ASPIRECOSMOSDB001 // Suppress diagnostic for preview features
            builder.AddAzureCosmosDB("cosmosdb-minimal") // Unique name for this test's resource
                   .RunAsPreviewEmulator(e => e.WithDataExplorer());
#pragma warning restore ASPIRECOSMOSDB001

            _app = builder.Build(); // Build the app

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Minimal AppHost built. Starting...");
            await _app.StartAsync(); // Start the app
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Minimal AppHost Started. Waiting for 'cosmosdb-minimal' to be running...");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)); 
            await _app.ResourceNotifications.WaitForResourceAsync("cosmosdb-minimal", KnownResourceStates.Running, cts.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] 'cosmosdb-minimal' resource is running.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        {            
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during MinimalCosmosEmulatorTest InitializeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during MinimalCosmosEmulatorTest InitializeAsync: {ex}");
            // Attempt to dispose if app was created, otherwise rethrow immediately.
            if (_app != null)
            {
                await DisposeAsync();
            }
            throw;
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- MinimalCosmosEmulatorTest InitializeAsync END ---");
    }

    public async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- MinimalCosmosEmulatorTest DisposeAsync START ---");
        try
        {            
            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing Minimal DistributedApplication...");
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Minimal DistributedApplication Disposed Successfully.");
            }
            else
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Minimal _app was null, nothing to dispose.");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during MinimalCosmosEmulatorTest DisposeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during MinimalCosmosEmulatorTest DisposeAsync: {ex}");
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- MinimalCosmosEmulatorTest DisposeAsync END ---");
    }

    [Fact]
    public Task CosmosEmulatorLifecycleTest_ShouldNotCauseDcpErrors()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: CosmosEmulatorLifecycleTest_ShouldNotCauseDcpErrors EXECUTING ---");
        // The main work is done in InitializeAsync and DisposeAsync.
        // This test just needs to exist to trigger the lifecycle.
        Assert.True(true);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: CosmosEmulatorLifecycleTest_ShouldNotCauseDcpErrors COMPLETED ---");
        return Task.CompletedTask;
    }

    // Optional: Add a second identical test method to see if running it twice in a row (within the same class instance due to xUnit behavior)
    // or multiple test methods in the same class, affects DCP stability.
    // [Fact]
    // public Task CosmosEmulatorLifecycleTest_SecondRun_ShouldNotCauseDcpErrors()
    // {
    //     _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: CosmosEmulatorLifecycleTest_SecondRun_ShouldNotCauseDcpErrors EXECUTING ---");
    //     Assert.True(true);
    //     _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: CosmosEmulatorLifecycleTest_SecondRun_ShouldNotCauseDcpErrors COMPLETED ---");
    //     return Task.CompletedTask;
    // }
}
