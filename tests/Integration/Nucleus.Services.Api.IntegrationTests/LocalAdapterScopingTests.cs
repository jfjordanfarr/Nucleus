using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Infrastructure.Testing.Utilities;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Nucleus.Abstractions; // For NucleusConstants
using Xunit.Sdk; // For Skip

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
    private readonly bool _skipTests;

    private static bool IsIntegrationTestsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1");
    }

    public LocalAdapterScopingTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _skipTests = !IsIntegrationTestsEnabled();
        if (_skipTests)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] LocalAdapterScopingTests: Integration tests are DISABLED by environment variable {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}. Skipping tests.");
        }
    }

    public async Task InitializeAsync()
    {
        if (_skipTests) return;

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests InitializeAsync START ---");
        try
        {
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

            _fileSystemManager = new TestFileSystemManager();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] TestFileSystemManager initialized.");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Test Artifacts Path: {_fileSystemManager.BaseTestPath}");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();
            _app = await appHost.BuildAsync();
            
            var appServices = _app.Services;
            _logger = appServices.GetRequiredService<ILogger<LocalAdapterScopingTests>>();

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
    }

    public async Task DisposeAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests DisposeAsync START ---");
        Environment.SetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON", null);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] NUCLEUS_TEST_PERSONA_CONFIGS_JSON environment variable cleared.");

        if (_app != null)
        {
            try
            {
                await _app.DisposeAsync();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost Disposed.");
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception during AppHost Dispose: {ex.Message}");
            }
            finally
            {
                _app = null;
            }
        }
        else
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] AppHost was null, no need to dispose.");
        }

        _apiClient?.Dispose();
        _apiClient = null;
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] HttpClient disposed.");

        if (_fileSystemManager != null)
        {
            try
            {
                _fileSystemManager.Dispose();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] TestFileSystemManager disposed (environment cleaned up).");
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception during TestFileSystemManager dispose: {ex.Message}");
            }
            finally
            {
                _fileSystemManager = null;
            }
        }
        else
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] TestFileSystemManager was null, no cleanup needed.");
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- LocalAdapterScopingTests DisposeAsync COMPLETE ---");
    }

    private async Task<AdapterResponse?> SendAdapterRequestAsync(AdapterRequest request)
    {
        Skip.If(_skipTests, $"Integration tests are disabled via {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}.");
        Assert.NotNull(_apiClient);

        var response = await _apiClient.PostAsJsonAsync("/api/v1/adapter/local/handle", request);
        // response.EnsureSuccessStatusCode(); // Do not ensure success here, as some tests expect failure
        return await response.Content.ReadFromJsonAsync<AdapterResponse>();
    }

    [SkippableFact]
    public async Task HandleInteraction_ValidRequest_ValidPersona_ShouldSucceed()
    {
        Skip.If(_skipTests, $"Integration tests are disabled via {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}.");
        Assert.NotNull(_fileSystemManager);
        const string tenantId = "test-tenant-local-adapter";
        const string conversationId = "test-conversation-local-adapter";
        const string fileName = "testfile.txt";

        var testFilePath = await _fileSystemManager.CreateTestArtifactAsync($"{tenantId}/{conversationId}", fileName, "Hello from LocalAdapter test!");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Created test file: {testFilePath}");

        var request = new AdapterRequest(
            PlatformType: PlatformType.Local,
            ConversationId: conversationId,
            UserId: "test-user-local",
            QueryText: "Test message for persona-1",
            TenantId: tenantId,
            PersonaId: "test-persona-1",
            InteractionType: "UserMessage",
            ArtifactReferences: new List<ArtifactReference>
            {
                new ArtifactReference(
                    ReferenceId: testFilePath,
                    ReferenceType: "file",
                    SourceUri: new Uri(testFilePath), // For local files, URI can be file path
                    TenantId: tenantId,
                    FileName: fileName, // Corrected from OriginalFileName
                    MimeType: "text/plain"
                )
            }
        );

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending AdapterRequest: {JsonSerializer.Serialize(request)}");
        var adapterResponse = await SendAdapterRequestAsync(request);

        adapterResponse.Should().NotBeNull();
        adapterResponse!.Success.Should().BeTrue("because the request is valid and persona is configured correctly.");
        adapterResponse.ResponseMessage.Should().Contain("Interaction handled successfully by persona: test-persona-1");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received AdapterResponse: {JsonSerializer.Serialize(adapterResponse)}");
    }

    [SkippableFact]
    public async Task HandleInteraction_InvalidPersona_ShouldFailGracefully()
    {
        Skip.If(_skipTests, $"Integration tests are disabled via {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}.");
        Assert.NotNull(_fileSystemManager);
        const string tenantId = "test-tenant-local-adapter";
        const string conversationId = "test-conversation-local-adapter";
        const string fileName = "anotherfile.txt";

        var testFilePath = await _fileSystemManager.CreateTestArtifactAsync($"{tenantId}/{conversationId}", fileName, "Content for invalid persona test.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Created test file for invalid persona test: {testFilePath}");

        var request = new AdapterRequest(
            PlatformType: PlatformType.Local,
            ConversationId: conversationId,
            UserId: "test-user-local",
            QueryText: "Test message for non-existent-persona",
            TenantId: tenantId,
            PersonaId: "non-existent-persona",
            InteractionType: "UserMessage",
            ArtifactReferences: new List<ArtifactReference>
            {
                new ArtifactReference(
                    ReferenceId: testFilePath,
                    ReferenceType: "file",
                    SourceUri: new Uri(testFilePath),
                    TenantId: tenantId,
                    FileName: fileName, // Corrected from OriginalFileName
                    MimeType: "text/plain"
                )
            }
        );
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending AdapterRequest (invalid persona): {JsonSerializer.Serialize(request)}");
        var adapterResponse = await SendAdapterRequestAsync(request);

        adapterResponse.Should().NotBeNull();
        adapterResponse!.Success.Should().BeFalse("because the persona ID is not configured.");
        adapterResponse.ResponseMessage.Should().Contain("Persona 'non-existent-persona' not found or not enabled for tenant 'test-tenant-local-adapter'.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received AdapterResponse (invalid persona): {JsonSerializer.Serialize(adapterResponse)}");
    }

    [SkippableFact]
    public async Task HandleInteraction_PersonaNotAllowedInConversation_ShouldFailGracefully()
    {
        Skip.If(_skipTests, $"Integration tests are disabled via {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}.");
        Assert.NotNull(_fileSystemManager);
        const string tenantId = "test-tenant-local-adapter";
        const string conversationId = "restricted-conversation";
        const string fileName = "restricted.txt";

        var testFilePath = await _fileSystemManager.CreateTestArtifactAsync($"{tenantId}/{conversationId}", fileName, "Content for restricted conversation test.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Created test file for restricted conversation test: {testFilePath}");

        var request = new AdapterRequest(
            PlatformType: PlatformType.Local,
            ConversationId: conversationId,
            UserId: "test-user-local",
            QueryText: "Test message for persona-1 in restricted conversation",
            TenantId: tenantId,
            PersonaId: "test-persona-1",
            InteractionType: "UserMessage",
            ArtifactReferences: new List<ArtifactReference>
            {
                new ArtifactReference(
                    ReferenceId: testFilePath,
                    ReferenceType: "file",
                    SourceUri: new Uri(testFilePath),
                    TenantId: tenantId,
                    FileName: fileName, // Corrected from OriginalFileName
                    MimeType: "text/plain"
                )
            }
        );
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending AdapterRequest (restricted conversation): {JsonSerializer.Serialize(request)}");
        var adapterResponse = await SendAdapterRequestAsync(request);

        adapterResponse.Should().NotBeNull();
        adapterResponse!.Success.Should().BeFalse("because the persona is not allowed in this conversation space.");
        adapterResponse.ResponseMessage.Should().Contain("Persona 'test-persona-1' is not allowed in conversation space 'restricted-conversation' for tenant 'test-tenant-local-adapter'.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received AdapterResponse (restricted conversation): {JsonSerializer.Serialize(adapterResponse)}");
    }

    [SkippableFact]
    public async Task HandleInteraction_NoArtifacts_ShouldStillProcessMessage()
    {
        Skip.If(_skipTests, $"Integration tests are disabled via {NucleusConstants.NucleusEnvironmentVariables.IntegrationTestsEnabled}.");
        const string tenantId = "test-tenant-local-adapter";
        const string conversationId = "general";

        var request = new AdapterRequest(
            PlatformType: PlatformType.Local,
            ConversationId: conversationId,
            UserId: "test-user-local",
            QueryText: "Test message with no artifacts for persona-1",
            TenantId: tenantId,
            PersonaId: "test-persona-1",
            InteractionType: "UserMessage",
            ArtifactReferences: new List<ArtifactReference>()
        );

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending AdapterRequest (no artifacts): {JsonSerializer.Serialize(request)}");
        var adapterResponse = await SendAdapterRequestAsync(request);

        adapterResponse.Should().NotBeNull();
        adapterResponse!.Success.Should().BeTrue("because a message without artifacts is a valid interaction.");
        adapterResponse.ResponseMessage.Should().Contain("Interaction handled successfully by persona: test-persona-1");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received AdapterResponse (no artifacts): {JsonSerializer.Serialize(adapterResponse)}");
    }
}
