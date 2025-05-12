using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models; // For PlatformAttachmentReference, SourceSystemType and other models
using Nucleus.Abstractions.Models.ApiContracts; // For API specific models
using Nucleus.Abstractions.Repositories;
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
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Infrastructure.Data.Persistence.Repositories; // Added for CosmosDb repo
using Nucleus.Infrastructure.Data.Persistence; // Corrected namespace for CosmosDb provider
using System.Net;
using System.Threading;
using System.Collections.Generic; // For List in NucleusIngestionRequest

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
            var defaultSettings = new CosmosDbSettings();
            _artifactMetadataContainerName = defaultSettings.MetadataContainerName;
            _personaKnowledgeContainerName = defaultSettings.KnowledgeContainerName;
            _personaConfigContainerName = defaultSettings.PersonaContainerName;

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Targeting Cosmos DB: Database='{_databaseName}', MetadataContainer='{_artifactMetadataContainerName}'");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Targeting Cosmos DB: Database='{_databaseName}', MetadataContainer='{_artifactMetadataContainerName}'");

            // Create CancellationTokenSources for timeouts
            using var ctsCosmos = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var ctsServiceBus = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Increased from 3 min
            using var ctsApi = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // Increased from 1 min

            // Start waiting for resources in parallel
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for Cosmos DB & Service Bus Emulators in parallel...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting for Cosmos DB & Service Bus Emulators in parallel...");

            var cosmosTask = _app.ResourceNotifications.WaitForResourceAsync("cosmosdb", KnownResourceStates.Running, ctsCosmos.Token);
            var serviceBusTask = _app.ResourceNotifications.WaitForResourceAsync("sbemulatorns", KnownResourceStates.Running, ctsServiceBus.Token);

            // Await both tasks concurrently
            await Task.WhenAll(cosmosTask, serviceBusTask);

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Cosmos DB & Service Bus Emulators are running.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Cosmos DB & Service Bus Emulators are running.");

            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting for Nucleus API Service...");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting for Nucleus API Service...");
            await _app.ResourceNotifications.WaitForResourceAsync("nucleusapi", KnownResourceStates.Running, ctsApi.Token);
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Nucleus API Service is running.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Nucleus API Service is running.");

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
            if (_cosmosClient != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing CosmosClient...");
                _cosmosClient.Dispose();
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] CosmosClient disposed.");
            }

            if (_app != null)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Disposing DistributedApplication...");
                await _app.DisposeAsync(); // Reverted: Removed token and try/catch
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] DistributedApplication Disposed Successfully.");
                Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: DistributedApplication Disposed Successfully.");
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

    [Fact]
    public async Task PostInteraction_ShouldPersistArtifactMetadataAsync()
    {
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: PostInteraction_ShouldPersistArtifactMetadataAsync START ---");
        Assert.NotNull(_httpClient); // Ensure _httpClient is initialized

        var interactionId = $"test-interaction-{Guid.NewGuid()}";
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var userId = $"test-user-{Guid.NewGuid()}";
        var conversationId = $"test-conversation-{Guid.NewGuid()}";
        var personaId = "test-persona-metadata-saver"; // Matches the persona configured in InitializeAsync

        // Prepare the request payload
        var request = new NucleusIngestionRequest(
            PlatformType: PlatformType.Test, // CS0117: Use SmokeTest instead of TestPlatform
            OriginatingUserId: userId,
            OriginatingConversationId: conversationId,
            OriginatingReplyToMessageId: null,
            OriginatingMessageId: $"test-message-{Guid.NewGuid()}",
            ResolvedPersonaId: personaId, 
            TimestampUtc: DateTimeOffset.UtcNow,
            QueryText: "This is a test query for metadata persistence.",
            ArtifactReferences: new List<ArtifactReference>
            {
                new ArtifactReference(
                    ReferenceId: "test-artifact-id-1",
                    ReferenceType: "text_plain", // Example reference type
                    SourceUri: new Uri("http://example.com/artifact1.txt"),
                    TenantId: tenantId,
                    FileName: "artifact1.txt",
                    MimeType: "text/plain"
                )
            },
            CorrelationId: interactionId, // Using interactionId as CorrelationId for tracing
            Metadata: new Dictionary<string, string> 
            { 
                { "TestMetadataKey", "TestMetadataValue" },
                { "InteractionId", interactionId }, // Pass InteractionId via metadata
                { "TenantId", tenantId } // Pass TenantId via metadata
            }
        );

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Posting NucleusIngestionRequest to /interactions endpoint with InteractionId: {interactionId}, TenantId: {tenantId}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Posting NucleusIngestionRequest with InteractionId: {interactionId}, TenantId: {tenantId}");

        // Act: Post to the /interactions endpoint
        // The endpoint now expects NucleusIngestionRequest directly
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/interactions", request, CancellationToken.None);
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] PostAsJsonAsync response status: {response.StatusCode}");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: PostAsJsonAsync response status: {response.StatusCode}");

        response.EnsureSuccessStatusCode(); // Throw if not a success code.

        // Allow some time for background processing (e.g., Service Bus message handling)
        // TODO: Replace with a more robust polling mechanism or event-driven wait when available.
        var processingDelay = TimeSpan.FromSeconds(15); // Adjusted delay, might need tuning
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Waiting {processingDelay.TotalSeconds} seconds for background processing...");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Waiting {processingDelay.TotalSeconds} seconds for background processing...");
        await Task.Delay(processingDelay);

        // Assert: Verify data in Cosmos DB
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Verifying ArtifactMetadata in Cosmos DB. Database='{_databaseName}', Container='{_artifactMetadataContainerName}'");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Verifying ArtifactMetadata in Cosmos DB. Database='{_databaseName}', Container='{_artifactMetadataContainerName}'");
        
        var metadataContainer = _cosmosClient.GetContainer(_databaseName, _artifactMetadataContainerName);
        ArtifactMetadata? persistedMetadata = null;
        string artifactIdToQuery = string.Empty; // Will be derived from the artifact reference

        // We need to determine the ID of the artifact that would have been created.
        // Assuming the first artifact reference is the one we're interested in.
        if (request.ArtifactReferences?.Count > 0 && request.ArtifactReferences[0] is ArtifactReference paRef)
        {
            // The ArtifactMetadata.Id is typically a new GUID generated during processing.
            // However, the SourceIdentifier would be the PlatformSpecificId of the attachment.
            // We need to query by a known unique property if the ID is not predictable.
            // For this test, let's assume we can find it via SourceIdentifier and other context.
            // A more robust test might involve a predictable ID or a query mechanism.
            // For now, we'll try to read by the *interactionId* if that's how it's stored as the document ID.
            // The previous test code was reading ArtifactMetadata by `interactionId` as document ID and `tenantId` as PK.
            // Let's stick to that for now. The Id field in ArtifactMetadata is initialized with a new Guid usually.
            // If the test setup implies InteractionId becomes the ArtifactMetadata.Id, we use that.

            // The original test used 'interactionId' as the ID for ReadItemAsync.
            // This implies that the MetadataSavingStrategyHandler or some part of the pipeline
            // is using the CorrelationId (which we set to interactionId) as the ID for the ArtifactMetadata document.
            artifactIdToQuery = interactionId; 
        }
        else
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] No ArtifactReference found in the request. Cannot determine artifact ID to query.");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: No ArtifactReference found. Cannot determine artifact ID to query.");
            Assert.Fail("Test setup error: No ArtifactReference provided.");
        }

        try
        {
            // ArtifactMetadata uses its own 'Id' field as the document ID and TenantId as partition key.
            // The previous test was using 'interactionId' for the ReadItemAsync call.
            // If the 'Id' of ArtifactMetadata is indeed set to the 'interactionId' from the request's CorrelationId, this will work.
            ItemResponse<ArtifactMetadata> itemResponse = await metadataContainer.ReadItemAsync<ArtifactMetadata>(artifactIdToQuery, new PartitionKey(tenantId));
            persistedMetadata = itemResponse.Resource;
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ReadItemAsync status: {itemResponse.StatusCode}. RU charge: {itemResponse.RequestCharge}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: ReadItemAsync status: {itemResponse.StatusCode}.");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ArtifactMetadata with Id {artifactIdToQuery} and PK {tenantId} not found. CosmosException: {ex.Message}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: ArtifactMetadata with Id {artifactIdToQuery} and PK {tenantId} not found.");
            // Listing items for debugging if not found
            try
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Listing items in container '{_artifactMetadataContainerName}' with PK '{tenantId}' for debugging:");
                var query = new QueryDefinition("SELECT * FROM c");
                using FeedIterator<ArtifactMetadata> feed = metadataContainer.GetItemQueryIterator<ArtifactMetadata>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId) });
                while (feed.HasMoreResults)
                {
                    FeedResponse<ArtifactMetadata> queryResponse = await feed.ReadNextAsync();
                    foreach (ArtifactMetadata item in queryResponse)
                    {
                        _outputHelper.WriteLine($"  Found item: Id={item.Id}, SourceIdentifier={item.SourceIdentifier}, FileName={item.FileName}");
                    }
                }
            }
            catch (Exception listEx)
            {
                _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception while listing items for debug: {listEx.Message}");
            }
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] Exception while reading from Cosmos DB: {ex}");
            Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: Exception while reading from Cosmos DB: {ex}");
            throw; // Re-throw unexpected exceptions
        }

        Assert.NotNull(persistedMetadata); 
        // CS1061: 'ArtifactMetadata' no definition for 'OriginalFileName' -> Use FileName
        // Assert.Equal(request.AdapterRequest.QueryText, persistedMetadata.OriginalFileName); -> AdapterRequest is not part of NucleusIngestionRequest, compare with request.QueryText if that's the intent.
        // For an attachment, the OriginalFileName in metadata would likely be the FileName from the ArtifactReference.
        if (request.ArtifactReferences?.Count > 0 && request.ArtifactReferences[0] is ArtifactReference paRefForAssert)
        {
             Assert.Equal(paRefForAssert.FileName, persistedMetadata.FileName); 
        }
        else
        {
            Assert.Null(persistedMetadata.FileName); // Or Assert.Fail if a file name was expected.
        }

        // CS1061: 'ArtifactMetadata' no definition for 'SourcePlatform' -> Use SourceSystemType
        // The type of request.PlatformType is PlatformType enum. SourceSystemType is also an enum.
        // This assumes a direct mapping or that they are compatible. This might need adjustment.
        Assert.Equal((SourceSystemType)request.PlatformType, persistedMetadata.SourceSystemType);
        
        // CS1061: 'NucleusIngestionRequest' no definition for 'UserId' -> Use OriginatingUserId from NucleusIngestionRequest
        Assert.Equal(request.OriginatingUserId, persistedMetadata.UserId);
        
        // CS1061: 'ArtifactMetadata' no definition for 'ConversationId' -> This property doesn't exist on ArtifactMetadata.
        // If conversation context is needed, it should be part of the metadata dictionary or another field.
        // Removing this assertion as the model doesn't support it directly.
        // Assert.Equal(request.OriginatingConversationId, persistedMetadata.ConversationId);

        Assert.Equal(interactionId, persistedMetadata.Id); // Ensure the document ID matches the expected interactionId (if that's the design)
        Assert.Equal(tenantId, persistedMetadata.TenantId);
        Assert.NotEqual(default(DateTimeOffset), persistedMetadata.CreatedAtNucleus); 
        Assert.True(persistedMetadata.CreatedAtNucleus < DateTimeOffset.UtcNow.AddSeconds(5) && persistedMetadata.CreatedAtNucleus > DateTimeOffset.UtcNow.AddSeconds(-60)); 
        // Assert.NotNull(persistedMetadata.ProcessingLog); // CS1061: ProcessingLog does not exist on ArtifactMetadata
        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] ArtifactMetadata successfully verified in Cosmos DB for InteractionId: {interactionId}.");
        Console.WriteLine($"[{DateTime.UtcNow:O}] CONSOLE: ArtifactMetadata successfully verified in Cosmos DB for InteractionId: {interactionId}.");

        _outputHelper.WriteLine($"[{DateTime.UtcNow:O}] --- Test: PostInteraction_ShouldPersistArtifactMetadataAsync END ---");
    }
}
