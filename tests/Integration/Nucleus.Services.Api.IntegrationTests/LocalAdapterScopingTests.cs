using System;
using System.Net.Http;
using System.Net.Http.Json; // Added for ReadFromJsonAsync/PostAsJsonAsync extension methods
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models; // Added for AdapterRequest, ArtifactReference, AdapterResponse etc.
using Nucleus.Abstractions.Models.ApiContracts; // Added for AdapterRequest and AdapterResponse
using Nucleus.Infrastructure.Testing.Configuration;
using Nucleus.Infrastructure.Testing.Utilities;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging; // Added for logging
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>
/// Integration tests for the LocalAdapter focusing on persona scoping and data governance.
/// These tests verify that the LocalAdapter correctly handles requests based on tenant and conversation IDs,
/// ensuring data isolation and adherence to configured persona scopes.
/// </summary>
/// <seealso href="../../../../Docs/Architecture/Adapters/02_ARCHITECTURE_ADAPTERS_LOCAL.md" />
/// <seealso href="../../../../Docs/Architecture/09_ARCHITECTURE_TESTING.md" />
public class LocalAdapterScopingTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication? _app;
    private HttpClient? _apiClient;
    private TestFileSystemManager? _fileSystemManager;
    private ILogger<LocalAdapterScopingTests>? _logger;

    public LocalAdapterScopingTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public async Task InitializeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests InitializeAsync START ---");
        try
        {
            // Define and set the test persona configurations environment variable
            var testPersonaConfigsJson = """
            [
                {
                    \"PersonaId\": \"test-persona-1\",
                    \"TenantId\": \"test-tenant-local-adapter\",
                    \"DisplayName\": \"Test Persona 1\",
                    \"Description\": \"A persona for testing basic local adapter scoping.\",
                    \"IsEnabled\": true,
                    \"AllowedConversationSpaces\": [\"test-conversation-local-adapter\", \"general\"],
                    \"AssociatedKnowledgeUris\": [],
                    \"AssociatedArtifactStoreUris\": []
                }
            ]
            """;
            Environment.SetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON", testPersonaConfigsJson);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] NUCLEUS_TEST_PERSONA_CONFIGS_JSON environment variable set.");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();
            _app = await appHost.BuildAsync();

            // Get a logger from the app's services to pass to TestFileSystemManager
            // This assumes the test host's DI container can resolve ILogger<TestFileSystemManager>
            // If not, we might need to create a simpler logger instance manually for the manager.
            var appServices = _app.Services;
            _logger = appServices.GetRequiredService<ILogger<LocalAdapterScopingTests>>();
            var fileSystemManagerLogger = appServices.GetRequiredService<ILogger<TestFileSystemManager>>();

            _fileSystemManager = new TestFileSystemManager();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] TestFileSystemManager initialized.");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Test Artifacts Path: {_fileSystemManager.BaseTestPath}");

            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Started. Waiting for resources to be running...");

            using var ctsApi = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running, ctsApi.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API Service is running.");

            _apiClient = _app.CreateHttpClient("nucleusapi");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] HttpClient created for Nucleus API: BaseAddress={_apiClient.BaseAddress}");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] InitializeAsync COMPLETED SUCCESSFULLY.");
        }
        catch (Exception ex)
        {            
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during InitializeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during InitializeAsync: {ex}");
            await DisposeAsync();
            throw;
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests InitializeAsync END ---");
    }

    public async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests DisposeAsync START ---");
        try
        {
            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed Successfully.");
            }
            else
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] _app was null, nothing to dispose.");
            }

            _fileSystemManager?.Dispose(); // Changed from CleanupTestArtifacts() to Dispose()
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Test artifacts cleaned up by TestFileSystemManager.");

            // Clear the environment variable
            Environment.SetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON", null);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] NUCLEUS_TEST_PERSONA_CONFIGS_JSON environment variable cleared.");
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! EXCEPTION during DisposeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! EXCEPTION during DisposeAsync: {ex}");
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests DisposeAsync END ---");
    }

    [Fact(Timeout = 60000)] // Timeout set to 60 seconds
    public async Task ApiHealthCheck_ShouldReturnOk()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: ApiHealthCheck_ShouldReturnOk START ---");
        Assert.NotNull(_apiClient); // Ensure HttpClient is ready

        // Act
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending request to /health...");
        var response = await _apiClient.GetAsync("/health");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received response: StatusCode={response.StatusCode}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: ApiHealthCheck_ShouldReturnOk END ---");
    }

    [Fact(Timeout = 60000)]
    public async Task SubmitInteraction_WithConfiguredPersona_ShouldSucceed()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SubmitInteraction_WithConfiguredPersona_ShouldSucceed START ---");

        var conversationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();

        var request = new AdapterRequest(
            PlatformType: PlatformType.Local,
            ConversationId: conversationId,
            UserId: "test-user-local-adapter-scoped",
            QueryText: $"Hello, this is a test interaction for @test-persona-1", // Target the configured persona
            MessageId: messageId,
            ReplyToMessageId: null,
            ArtifactReferences: new List<ArtifactReference>(), // Ensure non-null list
            Metadata: new Dictionary<string, string>(),       // Ensure non-null dictionary
            InteractionType: "UserMessage"
        );

        _outputHelper.WriteLine($"Constructed AdapterRequest for ConversationId: {request.ConversationId}, Query: {request.QueryText}");

        // Act
        _outputHelper.WriteLine("Sending POST request to api/Interaction/query...");
        HttpResponseMessage? response = null;
        AdapterResponse? adapterResponse = null;
        string? responseContent = null;

        try
        {
            response = await _apiClient!.PostAsJsonAsync("api/Interaction/query", request);
            _outputHelper.WriteLine($"Received response with StatusCode: {response.StatusCode}");
            responseContent = await response.Content.ReadAsStringAsync();
            _outputHelper.WriteLine($"Response content: {responseContent}");
            response.EnsureSuccessStatusCode(); // Throw an exception for bad status codes
            adapterResponse = await response.Content.ReadFromJsonAsync<AdapterResponse>();
        }
        catch (HttpRequestException e)
        {
            _outputHelper.WriteLine($"HttpRequestException: {e.Message}");
            if (response != null)
            {
                _outputHelper.WriteLine($"Response status code: {response.StatusCode}");
                _outputHelper.WriteLine($"Response content on error: {responseContent ?? "<empty>"}");
            }
            throw; // Re-throw to fail the test
        }
        catch (JsonException e)
        {
            _outputHelper.WriteLine($"JsonException during deserialization: {e.Message}");
            _outputHelper.WriteLine($"Response content that failed to deserialize: {responseContent ?? "<empty>"}");
            throw; // Re-throw to fail the test
        }

        // Assert
        _outputHelper.WriteLine("Asserting response...");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        adapterResponse.Should().NotBeNull();
        adapterResponse!.Success.Should().BeTrue();
        // Because we are using EchoStrategy, the response message should contain the original query text.
        adapterResponse!.ResponseMessage.Should().NotBeNullOrEmpty()
            .And.Contain(request.QueryText, "because the EchoStrategy should include the original query in its response.");
        adapterResponse!.ErrorMessage.Should().BeNullOrEmpty();

        _outputHelper.WriteLine("Test SubmitInteraction_WithConfiguredPersona_ShouldSucceed completed successfully.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: SubmitInteraction_WithConfiguredPersona_ShouldSucceed END ---");
    }
}
