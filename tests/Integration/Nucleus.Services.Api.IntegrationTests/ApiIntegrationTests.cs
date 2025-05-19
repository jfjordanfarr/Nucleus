using System;
using System.Collections.Generic; // For List
using System.Net;
using System.Net.Http;
using System.Net.Http.Json; // For PostAsJsonAsync
using System.Threading.Tasks;
using Aspire.Hosting; // Add Aspire Hosting namespace for DistributedApplication
using Aspire.Hosting.ApplicationModel; // Added for ResourceStates, if available, or check alternative
using Aspire.Hosting.Testing; // Add Aspire Testing namespace
using Microsoft.Azure.Cosmos; // For CosmosClient
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // For GetRequiredService
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration; 
using Nucleus.Abstractions.Repositories;
using System.Text.Json; 
using Nucleus.Infrastructure.Data.Persistence; 
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk; // Required for Skip.If

namespace Nucleus.Services.Api.IntegrationTests;

/// <summary>
/// Integration tests for the Nucleus API service using Aspire Test Host.
/// Implements IAsyncLifetime for proper setup and teardown of the Aspire DistributedApplication.
/// </summary>
/// <seealso href="../../../../Docs/Architecture/09_ARCHITECTURE_TESTING.md" />
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private CosmosClient? _cosmosClient;
    private string _databaseName = null!;
    private string _artifactMetadataContainerName = null!;
    private string _personaKnowledgeContainerName = null!;
    private string _personaConfigContainerName = null!;

    // Static property to check if integration tests are enabled via environment variable.
    // Uses NucleusConstants for the environment variable name.
    private static bool ShouldSkipIntegrationTests => 
        !string.Equals(Environment.GetEnvironmentVariable(NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled), "true", StringComparison.OrdinalIgnoreCase);

    // New private record to hold common test data
    private record TestIngestionData(
        string TenantId,
        string UserId,
        string ConversationId,
        string PersonaId,
        string AttachmentFileName,
        string AttachmentSourceUri,
        ArtifactReference TestArtifact,
        NucleusIngestionRequest IngestionRequest
    );

    // New helper method to set up common test data
    private static TestIngestionData SetupTestIngestionData(
        string baseName,
        string? queryText = null, 
        string personaId = "test-persona-metadata-saver")
    {
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var userId = $"test-user-{Guid.NewGuid()}";
        var conversationId = $"test-conversation-{Guid.NewGuid()}";
        var attachmentFileName = $"{baseName}-{Guid.NewGuid().ToString().Substring(0, 8)}.txt";
        var attachmentSourceUri = $"http://example.com/{attachmentFileName}";

        var testArtifact = CreateTestArtifactReference(
            tenantId: tenantId,
            referenceId: attachmentSourceUri,
            fileName: attachmentFileName,
            mimeType: "text/plain"
        );

        var ingestionRequest = CreateTestNucleusIngestionRequest(
            platformType: PlatformType.Unknown,
            originatingUserId: userId,
            originatingConversationId: conversationId,
            timestampUtc: DateTimeOffset.UtcNow,
            queryText: queryText ?? $"Test query for {baseName}",
            artifactReferences: new List<ArtifactReference> { testArtifact },
            resolvedPersonaId: personaId,
            originatingMessageId: conversationId
        );

        return new TestIngestionData(
            TenantId: tenantId,
            UserId: userId,
            ConversationId: conversationId,
            PersonaId: personaId,
            AttachmentFileName: attachmentFileName,
            AttachmentSourceUri: attachmentSourceUri,
            TestArtifact: testArtifact,
            IngestionRequest: ingestionRequest
        );
    }

    // Helper method to create a valid ArtifactReference instance
    private static ArtifactReference CreateTestArtifactReference(
        string tenantId,
        string referenceId, // This will often be the SourceUri as a string
        string? fileName = null,
        string referenceType = "file", 
        string? mimeType = "application/octet-stream")
    {
        // Ensure SourceUri is a valid Uri
        if (!Uri.TryCreate(referenceId, UriKind.Absolute, out var sourceUri))
        {
            // If referenceId is not a valid absolute URI, try to construct one, 
            // assuming it might be a relative path or just a name.
            // This is a fallback and might need adjustment based on typical 'referenceId' values.
            sourceUri = new Uri($"http://example.com/{referenceId}");
        }

        var actualFileName = fileName ?? Path.GetFileName(sourceUri.LocalPath);
        if (string.IsNullOrEmpty(actualFileName) && Uri.IsWellFormedUriString(referenceId, UriKind.Absolute))
        {
            actualFileName = Path.GetFileName(new Uri(referenceId).LocalPath);
        }
        actualFileName ??= "unknown_file"; // Default if still null

        return new ArtifactReference(
            ReferenceId: referenceId, // The original referenceId string
            ReferenceType: referenceType,
            SourceUri: sourceUri,     // The parsed and validated Uri
            TenantId: tenantId,
            FileName: actualFileName,
            MimeType: mimeType
        );
    }

    // Helper method to create a valid NucleusIngestionRequest instance
    private static NucleusIngestionRequest CreateTestNucleusIngestionRequest(
        PlatformType platformType,
        string originatingUserId,
        string originatingConversationId,
        DateTimeOffset timestampUtc,
        string? resolvedPersonaId = null,
        string? queryText = null,
        List<ArtifactReference>? artifactReferences = null,
        string? originatingMessageId = null,
        string? originatingReplyToMessageId = null,
        string? correlationId = null,
        Dictionary<string, string>? metadata = null)
    {
        return new NucleusIngestionRequest(
            PlatformType: platformType, // Corrected from PlatformType.Test to a valid enum member if necessary
            OriginatingUserId: originatingUserId,
            OriginatingConversationId: originatingConversationId,
            OriginatingReplyToMessageId: originatingReplyToMessageId,
            OriginatingMessageId: originatingMessageId ?? originatingConversationId, // Ensure not null, default to conversationId
            ResolvedPersonaId: resolvedPersonaId,
            TimestampUtc: timestampUtc,
            QueryText: queryText,
            ArtifactReferences: artifactReferences ?? new List<ArtifactReference>(),
            CorrelationId: correlationId ?? Guid.NewGuid().ToString(),
            Metadata: metadata
        );
    }

    // Constructor remains simple, just storing the output helper
    public ApiIntegrationTests(ITestOutputHelper outputHelper) 
    {
        _outputHelper = outputHelper;
    }

    // Moved initialization logic here from constructor and separate method
    public async Task InitializeAsync() // Part of IAsyncLifetime
    {
        // Use Skip.If directly here. If this condition is met, InitializeAsync won't proceed, effectively skipping all tests in this class.
        Skip.If(ShouldSkipIntegrationTests, $"Skipping all ApiIntegrationTests. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InitializeAsync START ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- InitializeAsync START ---");

        // Define a test-specific persona configuration using MetadataSavingStrategyHandler
        var testPersonaConfigsJson = @"[
            {
                ""PersonaId"": ""test-persona-metadata-saver"",
                ""Description"": ""A test persona that uses the MetadataSavingStrategyHandler to persist artifact metadata."",
                ""IsEnabled"": true,
                ""IsDefault"": true, // Make this the default to ensure it's picked up by tests without explicit persona selection
                ""AgenticStrategies"": [
                    {
                        ""StrategyKey"": ""MetadataSaver"", // Must match the StrategyKey of MetadataSavingStrategyHandler
                        ""IsEnabled"": true,
                        ""Order"": 1,
                        ""Parameters"": {}
                    }
                ]
            }
        ]";
        Environment.SetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON", testPersonaConfigsJson);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] NUCLEUS_TEST_PERSONA_CONFIGS_JSON environment variable set for InMemoryPersonaConfigurationProvider.");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: NUCLEUS_TEST_PERSONA_CONFIGS_JSON environment variable set.");

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

            // Signal AppHost to use simple console logging for its services
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Setting NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE=true for AppHost services.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Setting NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE=true for AppHost services.");
            Environment.SetEnvironmentVariable("NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE", "true");

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Nucleus_AppHost>();

            // Configure services for the test host before building the app
            // appHost implements IHostApplicationBuilder, so we use its Services and Configuration properties directly.
            var configuration = appHost.Configuration;

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Building DistributedApplication...");
            _app = await appHost.BuildAsync();

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Starting DistributedApplication...");
            await _app.StartAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication started.");

            // Attempt 3: Directly use _app.GetConnectionStringAsync
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Attempting to get 'cosmosdb' connection string via _app.GetConnectionStringAsync...");
            var cosmosConnectionString = await _app.GetConnectionStringAsync("cosmosdb");

            if (string.IsNullOrEmpty(cosmosConnectionString))
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! CRITICAL ERROR: Cosmos DB connection string 'cosmosdb' not found using _app.GetConnectionStringAsync AFTER app start.");
                Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! CRITICAL ERROR: Cosmos DB connection string 'cosmosdb' not found using _app.GetConnectionStringAsync AFTER app start.");
                throw new InvalidOperationException("Cosmos DB connection string 'cosmosdb' not found via _app.GetConnectionStringAsync after app start. Ensure it's correctly exposed by the AppHost.");
            }

            var safeLogConnectionString = cosmosConnectionString.Length > 50 ? cosmosConnectionString.Substring(0, 50) + "..." : cosmosConnectionString;
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Successfully retrieved CosmosDB connection string after app start. Prefix: {safeLogConnectionString}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Successfully retrieved CosmosDB connection string after app start. Prefix: {safeLogConnectionString}");

            _cosmosClient = new CosmosClient(cosmosConnectionString, new CosmosClientOptions
            {
                // Optionally, configure CosmosClientOptions here
            });
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] CosmosClient instance created successfully.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Creating HttpClient for nucleusapi service...");
            _httpClient = _app.CreateHttpClient("nucleusapi");
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] HttpClient created for Nucleus API: BaseAddress={_httpClient.BaseAddress}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: HttpClient created for Nucleus API: BaseAddress={_httpClient.BaseAddress}");

            // Determine Database and Container names
            // The API service, when run via AppHost, uses its appsettings.Development.json.
            // So, the database name will be 'NucleusEmulatorDb'.
            _databaseName = "NucleusEmulatorDb"; 
            var defaultSettings = new Nucleus.Abstractions.Models.Configuration.CosmosDbSettings();
            _artifactMetadataContainerName = defaultSettings.MetadataContainerName;
            _personaKnowledgeContainerName = defaultSettings.KnowledgeContainerName;
            _personaConfigContainerName = defaultSettings.PersonaContainerName;

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Targeting Cosmos DB: Database='{_databaseName}', MetadataContainer='{_artifactMetadataContainerName}'");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Targeting Cosmos DB: Database='{_databaseName}', MetadataContainer='{_artifactMetadataContainerName}'");

            // Ensure the database and containers exist
            await EnsureCosmosDbResourcesAsync();
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! CRITICAL ERROR during InitializeAsync: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! CRITICAL ERROR during InitializeAsync: {ex}");
            // Attempt to clean up if app was partially started
            if (_app != null) // Simplified check
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Attempting to stop partially started app due to initialization error...");
                // await _app.StopAsync(); // StopAsync might also not be available or might have different semantics.
                                        // For now, focusing on other errors. DisposeAsync should handle cleanup.
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] App stop attempt (or skip if StopAsync is unavailable).");
            }
            throw; // Re-throw the exception to fail the test initialization
        }
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- InitializeAsync END ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- InitializeAsync END ---");
    }
    
    private async Task EnsureCosmosDbResourcesAsync()
    {
        if (ShouldSkipIntegrationTests) return;
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Ensuring Cosmos DB database '{_databaseName}' and containers exist...");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Ensuring Cosmos DB database '{_databaseName}' and containers exist...");

        if (_cosmosClient == null)
        {
            var errorMessage = "CosmosClient is null in EnsureCosmosDbResourcesAsync. This should not happen if InitializeAsync completed successfully.";
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] !!! CRITICAL ERROR: {errorMessage}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: !!! CRITICAL ERROR: {errorMessage}");
            throw new InvalidOperationException(errorMessage);
        }

        DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
        Database database = databaseResponse.Database;
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Database '{_databaseName}' ensured. Status: {databaseResponse.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Database '{_databaseName}' ensured. Status: {databaseResponse.StatusCode}");

        // Ensure ArtifactMetadata container
        ContainerResponse metadataContainerResponse = await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_artifactMetadataContainerName, "/tenantId")); // Partition key path
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Container '{_artifactMetadataContainerName}' ensured. Status: {metadataContainerResponse.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Container '{_artifactMetadataContainerName}' ensured. Status: {metadataContainerResponse.StatusCode}");

        // Ensure PersonaKnowledge container
        ContainerResponse knowledgeContainerResponse = await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_personaKnowledgeContainerName, "/tenantId")); // Partition key path
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Container '{_personaKnowledgeContainerName}' ensured. Status: {knowledgeContainerResponse.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Container '{_personaKnowledgeContainerName}' ensured. Status: {knowledgeContainerResponse.StatusCode}");
        
        // Ensure PersonaConfiguration container
        ContainerResponse personaConfigContainerResponse = await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_personaConfigContainerName, "/personaId")); // Partition key path for persona configs
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Container '{_personaConfigContainerName}' ensured. Status: {personaConfigContainerResponse.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Container '{_personaConfigContainerName}' ensured. Status: {personaConfigContainerResponse.StatusCode}");
    }

    public async Task DisposeAsync() // Part of IAsyncLifetime
    {
        if (ShouldSkipIntegrationTests) return; // Don't run dispose if tests were skipped
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- DisposeAsync START ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- DisposeAsync START ---");
        if (_app != null)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
            await _app.DisposeAsync();
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication disposed.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: DistributedApplication disposed.");
        }
        _cosmosClient?.Dispose();
        _httpClient?.Dispose();
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- DisposeAsync END ---");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: --- DisposeAsync END ---");
    }

    [SkippableFact]
    public async Task BasicHealthCheck_ShouldReturnOk()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: BasicHealthCheck_ShouldReturnOk START ---");
        // Arrange
        Assert.NotNull(_httpClient); // Ensure HttpClient is ready

        // Act
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending request to /health...");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Sending request to /health...");
        var response = await _httpClient.GetAsync("/health");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received response: StatusCode={response.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Received response: StatusCode={response.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: BasicHealthCheck_ShouldReturnOk END ---");

    }

    [SkippableFact]
    public async Task PostIngestionRequest_WithValidData_ShouldReturnAccepted()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient); 

        var testData = SetupTestIngestionData("acceptance");

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending POST request to /api/ingestion with TenantId: {testData.TenantId}, ConversationId: {testData.ConversationId}");
        var response = await _httpClient.PostAsJsonAsync("/api/ingestion", testData.IngestionRequest);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received response with StatusCode: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [SkippableFact]
    public async Task PostIngestionRequest_WithValidData_ShouldPersistArtifactMetadata()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient);
        Assert.NotNull(_cosmosClient);

        var testData = SetupTestIngestionData("persist-metadata", "Test metadata persistence");

        var response = await _httpClient.PostAsJsonAsync("/api/ingestion", testData.IngestionRequest);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for 5 seconds for metadata to be processed and persisted...");
        await Task.Delay(TimeSpan.FromSeconds(5));

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Querying Cosmos DB for persisted ArtifactMetadata. TenantId: {testData.TenantId}");
        
        var persistedMetadata = await GetItemFromCosmosDbAsync<ArtifactMetadata>(
            _cosmosClient, 
            _databaseName, 
            _artifactMetadataContainerName, 
            testData.TenantId, 
            $"SELECT * FROM c WHERE c.tenantId = '{testData.TenantId}' AND c.userId = '{testData.UserId}' AND c.fileName = '{testData.AttachmentFileName}'",
            _outputHelper,
            isQuery: true
        );
        
        Assert.NotNull(persistedMetadata);
        Assert.Equal(testData.TenantId, persistedMetadata.TenantId);
        Assert.Equal(testData.UserId, persistedMetadata.UserId);
        Assert.Equal(testData.AttachmentFileName, persistedMetadata.FileName);
        Assert.Equal(SourceSystemType.Unknown, persistedMetadata.SourceSystemType);
        Assert.NotNull(persistedMetadata.AnalyzedByPersonaIds);
        Assert.Contains(testData.PersonaId, persistedMetadata.AnalyzedByPersonaIds);

        // Cleanup
        await CleanupCosmosDbAsync<ArtifactMetadata>(_cosmosClient, _databaseName, _artifactMetadataContainerName, testData.TenantId, persistedMetadata.Id, _outputHelper);
    }

    [SkippableFact]
    public async Task PostIngestionRequest_WithValidData_ShouldPersistPersonaKnowledge()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient);
        Assert.NotNull(_cosmosClient);

        var testData = SetupTestIngestionData("persist-knowledge", "Test knowledge persistence");
        var expectedStrategyId = "MetadataSaver"; // Matches the StrategyKey in the test persona config

        var response = await _httpClient.PostAsJsonAsync("/api/ingestion", testData.IngestionRequest);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for 10 seconds for metadata and knowledge to be processed and persisted...");
        await Task.Delay(TimeSpan.FromSeconds(10)); 

        var relatedArtifactMetadata = await GetItemFromCosmosDbAsync<ArtifactMetadata>(
            _cosmosClient,
            _databaseName,
            _artifactMetadataContainerName,
            testData.TenantId,
            $"SELECT * FROM c WHERE c.tenantId = '{testData.TenantId}' AND c.userId = '{testData.UserId}' AND c.fileName = '{testData.AttachmentFileName}' ORDER BY c.createdAtNucleus DESC",
            _outputHelper,
            isQuery: true
        );

        Assert.NotNull(relatedArtifactMetadata);
        var artifactId = relatedArtifactMetadata.Id; 
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Found related ArtifactMetadata with ID: {artifactId} for knowledge check.");

        var expectedKnowledgeId = $"{testData.TenantId}_{artifactId}_{testData.PersonaId}_{expectedStrategyId}";
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Expected PersonaKnowledgeEntry ID: {expectedKnowledgeId}");

        var persistedKnowledge = await GetItemFromCosmosDbAsync<PersonaKnowledgeEntry>(
            _cosmosClient, 
            _databaseName, 
            _personaKnowledgeContainerName, 
            testData.TenantId, 
            expectedKnowledgeId, 
            _outputHelper,
            isQuery: false 
        );
        
        Assert.NotNull(persistedKnowledge);
        Assert.Equal(expectedKnowledgeId, persistedKnowledge.Id);
        Assert.Equal(testData.TenantId, persistedKnowledge.TenantId);
        Assert.Equal(testData.UserId, persistedKnowledge.UserId);
        Assert.Equal(testData.ConversationId, persistedKnowledge.InteractionId);
        Assert.Equal(testData.PersonaId, persistedKnowledge.PersonaId);
        Assert.Equal(artifactId, persistedKnowledge.ArtifactId);
        Assert.Equal(expectedStrategyId, persistedKnowledge.StrategyId);
        Assert.NotNull(persistedKnowledge.AnalysisData);
        
        using var jsonDoc = JsonDocument.Parse(persistedKnowledge.AnalysisData.Value.GetRawText());
        Assert.True(jsonDoc.RootElement.TryGetProperty("metadataPersisted", out var property) && property.GetBoolean());
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] PersonaKnowledgeEntry AnalysisData verified.");

        // Cleanup
        await CleanupCosmosDbAsync<PersonaKnowledgeEntry>(_cosmosClient, _databaseName, _personaKnowledgeContainerName, testData.TenantId, expectedKnowledgeId, _outputHelper);
        await CleanupCosmosDbAsync<ArtifactMetadata>(_cosmosClient, _databaseName, _artifactMetadataContainerName, testData.TenantId, artifactId, _outputHelper);
    }

    [SkippableFact]
    public async Task GetArtifactMetadata_WithValidId_ShouldReturnOkAndMetadata()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient);
        Assert.NotNull(_cosmosClient);

        var testData = SetupTestIngestionData("get-metadata", "Test for GetArtifactMetadata");

        var postResponse = await _httpClient.PostAsJsonAsync("/api/ingestion", testData.IngestionRequest);
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for 5 seconds for metadata to be processed...");
        await Task.Delay(TimeSpan.FromSeconds(5));

        var persistedIngestedMetadata = await GetItemFromCosmosDbAsync<ArtifactMetadata>(
            _cosmosClient,
            _databaseName,
            _artifactMetadataContainerName,
            testData.TenantId,
            $"SELECT * FROM c WHERE c.tenantId = '{testData.TenantId}' AND c.userId = '{testData.UserId}' AND c.fileName = '{testData.AttachmentFileName}'",
            _outputHelper,
            isQuery: true
        );
        Assert.NotNull(persistedIngestedMetadata);
        var artifactIdToGet = persistedIngestedMetadata.Id;
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ArtifactMetadata persisted with ID: {artifactIdToGet}. Attempting to retrieve it via API.");

        var getResponse = await _httpClient.GetAsync($"/api/artifacts/{artifactIdToGet}/metadata?tenantId={testData.TenantId}");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] GET /api/artifacts/{artifactIdToGet}/metadata response: {getResponse.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedMetadata = await getResponse.Content.ReadFromJsonAsync<ArtifactMetadata>();
        Assert.NotNull(retrievedMetadata);
        Assert.Equal(artifactIdToGet, retrievedMetadata.Id);
        Assert.Equal(testData.TenantId, retrievedMetadata.TenantId);
        Assert.Equal(testData.UserId, retrievedMetadata.UserId);
        Assert.Equal(testData.AttachmentFileName, retrievedMetadata.FileName);
        Assert.Equal(SourceSystemType.Unknown, retrievedMetadata.SourceSystemType);

        await CleanupCosmosDbAsync<ArtifactMetadata>(_cosmosClient, _databaseName, _artifactMetadataContainerName, testData.TenantId, artifactIdToGet, _outputHelper);
    }

    [SkippableFact]
    public async Task GetPersonaKnowledge_WithValidIds_ShouldReturnOkAndKnowledge()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient);
        Assert.NotNull(_cosmosClient);

        var testData = SetupTestIngestionData("get-knowledge", "Test for GetPersonaKnowledge");
        var expectedStrategyId = "MetadataSaver";

        var postResponse = await _httpClient.PostAsJsonAsync("/api/ingestion", testData.IngestionRequest);
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for 10 seconds for metadata and knowledge to be processed...");
        await Task.Delay(TimeSpan.FromSeconds(10));

        var persistedIngestedMetadata = await GetItemFromCosmosDbAsync<ArtifactMetadata>(
            _cosmosClient,
            _databaseName,
            _artifactMetadataContainerName,
            testData.TenantId,
            $"SELECT * FROM c WHERE c.tenantId = '{testData.TenantId}' AND c.userId = '{testData.UserId}' AND c.fileName = '{testData.AttachmentFileName}'",
            _outputHelper,
            isQuery: true
        );
        Assert.NotNull(persistedIngestedMetadata);
        var artifactIdForKnowledge = persistedIngestedMetadata.Id;
        var expectedKnowledgeId = $"{testData.TenantId}_{artifactIdForKnowledge}_{testData.PersonaId}_{expectedStrategyId}";
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ArtifactMetadata ID: {artifactIdForKnowledge}. Expected PersonaKnowledge ID: {expectedKnowledgeId}. Attempting to retrieve it via API.");

        var getResponse = await _httpClient.GetAsync($"/api/artifacts/{artifactIdForKnowledge}/knowledge/{testData.PersonaId}/{expectedStrategyId}?tenantId={testData.TenantId}");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] GET /api/artifacts/.../knowledge/... response: {getResponse.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedKnowledge = await getResponse.Content.ReadFromJsonAsync<PersonaKnowledgeEntry>();
        Assert.NotNull(retrievedKnowledge);
        Assert.Equal(expectedKnowledgeId, retrievedKnowledge.Id);
        Assert.Equal(testData.TenantId, retrievedKnowledge.TenantId);
        Assert.Equal(testData.UserId, retrievedKnowledge.UserId);
        Assert.Equal(testData.PersonaId, retrievedKnowledge.PersonaId);
        Assert.Equal(artifactIdForKnowledge, retrievedKnowledge.ArtifactId);
        Assert.Equal(expectedStrategyId, retrievedKnowledge.StrategyId);
        Assert.NotNull(retrievedKnowledge.AnalysisData);

        await CleanupCosmosDbAsync<PersonaKnowledgeEntry>(_cosmosClient, _databaseName, _personaKnowledgeContainerName, testData.TenantId, expectedKnowledgeId, _outputHelper);
        await CleanupCosmosDbAsync<ArtifactMetadata>(_cosmosClient, _databaseName, _artifactMetadataContainerName, testData.TenantId, artifactIdForKnowledge, _outputHelper);
    }

    [SkippableFact]
    public async Task GetPersonaConfiguration_ShouldReturnConfiguredPersona()
    {
        Skip.If(ShouldSkipIntegrationTests, $"Skipping test. Set {NucleusConstants.EnvironmentVariables.IntegrationTestsEnabled}=true to enable.");
        Assert.NotNull(_httpClient);

        var personaIdToGet = "test-persona-metadata-saver"; 

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Sending GET request to /api/personas/configurations/{personaIdToGet}");
        var response = await _httpClient.GetAsync($"/api/personas/configurations/{personaIdToGet}");
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Received response with StatusCode: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<PersonaConfiguration>();

        Assert.NotNull(config);
        Assert.Equal(personaIdToGet, config.PersonaId);
        Assert.True(config.IsEnabled);
        // Removed: Assert.True(config.IsDefault); // IsDefault is not a property of PersonaConfiguration
        Assert.NotNull(config.AgenticStrategy); 
        Assert.Equal("MetadataSaver", config.AgenticStrategy.StrategyKey); 
    }

    // Helper method to clean up Cosmos DB items - can be marked static
    private static async Task CleanupCosmosDbAsync(CosmosClient client, string dbName, string containerName, string tenantId, string itemId)
    {
        try
        {
            var container = client.GetContainer(dbName, containerName);
            await container.DeleteItemAsync<object>(itemId, new PartitionKey(tenantId));
            // _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Cleaned up item {itemId} from {containerName} for tenant {tenantId}.");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Item doesn't exist, which is fine for cleanup
            // _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Item {itemId} not found in {containerName} for tenant {tenantId} during cleanup (already deleted or never existed).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Cosmos DB cleanup for item {itemId} in {containerName}: {ex.Message}");
            // _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Error during Cosmos DB cleanup for item {itemId} in {containerName}: {ex.Message}");
        }
    }

    // Helper method to retrieve an item from Cosmos DB - can be marked static
    private static async Task<T?> GetItemFromCosmosDbAsync<T>(CosmosClient cosmosClient, string databaseName, string containerName, string partitionKey, string itemIdOrQuery, ITestOutputHelper outputHelper, bool isQuery = false) where T : class
    {
        if (ShouldSkipIntegrationTests) return null;
        var container = cosmosClient.GetContainer(databaseName, containerName);
        T? item = null;

        if (isQuery)
        {
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Querying Cosmos DB. Container: {containerName}, PartitionKey: {partitionKey}, Query: {itemIdOrQuery}");
            var query = new QueryDefinition(itemIdOrQuery);
            using FeedIterator<T> feed = container.GetItemQueryIterator<T>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            if (feed.HasMoreResults)
            {
                FeedResponse<T> response = await feed.ReadNextAsync();
                item = response.FirstOrDefault(); // Taking the first item from the query response
                outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Query executed. Found item: {item != null}. RU: {response.RequestCharge}");
            }
            else
            {
                outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Query executed. No items found.");
            }
        }
        else // Direct read by ID
        {
            var itemId = itemIdOrQuery; // In this case, it's an item ID
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Reading item from Cosmos DB. Container: {containerName}, ID: {itemId}, PartitionKey: {partitionKey}");
            try
            {
                ItemResponse<T> response = await container.ReadItemAsync<T>(itemId, new PartitionKey(partitionKey));
                item = response.Resource;
                outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Item read. Status: {response.StatusCode}, RU: {response.RequestCharge}");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Item with ID '{itemId}' not found in container '{containerName}'.");
                item = null;
            }
            catch (Exception ex)
            {
                outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception reading item '{itemId}' from container '{containerName}': {ex.Message}");
                throw; // Re-throw other exceptions
            }
        }
        return item;
    }

    // Static helper method to clean up (delete) an item from Cosmos DB
    private static async Task CleanupCosmosDbAsync<T>(CosmosClient cosmosClient, string databaseName, string containerName, string partitionKey, string itemId, ITestOutputHelper outputHelper)
    {
        if (ShouldSkipIntegrationTests) return;
        outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Cleaning up item. Container: {containerName}, ID: {itemId}, PartitionKey: {partitionKey}");
        var container = cosmosClient.GetContainer(databaseName, containerName);
        try
        {
            await container.DeleteItemAsync<T>(itemId, new PartitionKey(partitionKey));
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Item '{itemId}' deleted successfully from '{containerName}'.");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Item '{itemId}' not found during cleanup in '{containerName}'. No action needed.");
        }
        catch (Exception ex)
        {
            outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Error deleting item '{itemId}' from '{containerName}': {ex.Message}");
            // Decide if this should throw or just log
        }
    }
}
