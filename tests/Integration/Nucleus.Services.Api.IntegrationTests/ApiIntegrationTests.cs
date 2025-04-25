using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nucleus.Abstractions.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection; // Added for GetRequiredService
using Nucleus.Abstractions.Repositories; // Added for IArtifactMetadataRepository
using System; // Added for Uri
using System.IO; // Added for Path
using Microsoft.AspNetCore.Hosting; // Needed for IWebHostBuilder
using Microsoft.Extensions.DependencyInjection.Extensions; // Needed for TryAddSingleton
using Azure.Messaging.ServiceBus; // Needed for ServiceBusMessage
using Nucleus.Services.Api.Infrastructure.Messaging; // Needed for NullMessageQueuePublisher
using Nucleus.Abstractions; // Corrected namespace for IMessageQueuePublisher
using Microsoft.Extensions.Logging; // Needed for ILogger
using Nucleus.Services.Api.IntegrationTests.Infrastructure; // ADDED for NullArtifactMetadataRepository

namespace Nucleus.Services.Api.IntegrationTests;

[TestClass]
public class ApiIntegrationTests
{
    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void Setup(TestContext context)
    {
#pragma warning disable CA2000 // Factory is disposed in ClassCleanup
        _factory = new WebApplicationFactory<Program>()
#pragma warning restore CA2000
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove potentially problematic Azure/Hosted Service registrations
                    var azureClientService = services.FirstOrDefault(d => d.ServiceType.FullName?.Contains("Microsoft.Extensions.Azure") ?? false);
                    if (azureClientService != null) { services.Remove(azureClientService); }

                    var hostedService = services.FirstOrDefault(d => d.ImplementationType == typeof(ServiceBusQueueConsumerService));
                    if (hostedService != null) { services.Remove(hostedService); }

                    // Explicitly remove any existing publishers first to be absolutely sure
                    var existingPublisher1 = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageQueuePublisher<ServiceBusMessage>));
                    if (existingPublisher1 != null) { services.Remove(existingPublisher1); }
                    var existingPublisher2 = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageQueuePublisher<NucleusIngestionRequest>));
                    if (existingPublisher2 != null) { services.Remove(existingPublisher2); }

                    // Remove the real ArtifactMetadataRepository registration
                    var realMetadataRepo = services.FirstOrDefault(d => d.ServiceType == typeof(IArtifactMetadataRepository));
                    if (realMetadataRepo != null) { services.Remove(realMetadataRepo); }

                    // Register Null publishers for testing
                    services.AddSingleton<IMessageQueuePublisher<ServiceBusMessage>>(sp => 
                        new NullMessageQueuePublisher<ServiceBusMessage>(sp.GetRequiredService<ILogger<NullMessageQueuePublisher<ServiceBusMessage>>>()));
                    services.AddSingleton<IMessageQueuePublisher<NucleusIngestionRequest>>(sp => 
                        new NullMessageQueuePublisher<NucleusIngestionRequest>(sp.GetRequiredService<ILogger<NullMessageQueuePublisher<NucleusIngestionRequest>>>()));
                    
                    // Register the Null ArtifactMetadataRepository for testing
                    services.AddSingleton<IArtifactMetadataRepository, NullArtifactMetadataRepository>();
                });
            });
    }

    [TestMethod]
    public async Task IngestEndpoint_WithValidLocalFileRequest_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateClient(); // Get client directly from factory

        string filePath = "d:\\Projects\\Nucleus\\_LocalData\\test_doc.txt"; // Use double backslashes
        string fileName = Path.GetFileName(filePath);
        string mimeType = "text/plain"; // Assuming based on extension
        string conversationId = Guid.NewGuid().ToString();
        Uri fileUri = new Uri(filePath); // Create a file URI

        var artifactReference = new ArtifactReference(
            ReferenceId: filePath,
            ReferenceType: "local_file_path",
            SourceUri: fileUri, // Add the missing SourceUri
            TenantId: "TestTenant", // Add missing TenantId
            FileName: fileName,
            MimeType: mimeType
        );

        var request = new AdapterRequest(
            PlatformType: "TestClient", // Indicate the source is a test
            ConversationId: conversationId,
            UserId: "TestUser",
            QueryText: $"Ingest test file: {fileName}",
            ArtifactReferences: new List<ArtifactReference> { artifactReference }
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/interaction/ingest", request);

        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode, $"API request failed with status code {response.StatusCode}. Details: {await response.Content.ReadAsStringAsync()}");
        // Removed: No body expected for NotActivated
        // var result = await response.Content.ReadFromJsonAsync<AdapterResponse?>();
        // Assert.IsNotNull(result);
        // Assert.IsTrue(result.Success, $"API indicated failure: {result.ErrorMessage}");
    }

    // TODO: Add separate test case for ingestion *with activation* where metadata persistence IS expected.

    [TestMethod]
    public async Task QueryEndpoint_WithSimpleQuery_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateClient(); // Get client directly from factory

        string conversationId = Guid.NewGuid().ToString();
        var request = new AdapterRequest(
            PlatformType: "TestClient",
            ConversationId: conversationId,
            UserId: "TestUser",
            QueryText: "What is the capital of France?",
            ArtifactReferences: null // No artifacts for this query
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/interaction/query", request);

        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode, $"API request failed with status code {response.StatusCode}. Details: {await response.Content.ReadAsStringAsync()}");
        // Removed: No body expected for NotActivated
        // var result = await response.Content.ReadFromJsonAsync<AdapterResponse?>();
        // Assert.IsNotNull(result);
        // Assert.IsTrue(result.Success, $"API indicated failure: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task QueryEndpoint_WithContextFromFile_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateClient(); // Get client directly from factory

        string conversationId = Guid.NewGuid().ToString();
        string contextFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "test_context.txt"); // Relative path
        
        // Ensure the test context file exists
        Assert.IsTrue(File.Exists(contextFilePath), $"Test context file not found at: {contextFilePath}");

        string fileName = Path.GetFileName(contextFilePath);
        string mimeType = "text/plain"; 

        var artifactReference = new ArtifactReference(
            ReferenceId: contextFilePath, // Use the file path as ID for local files
            ReferenceType: "local_file_path", // Use the correct type for local files
            SourceUri: new Uri(contextFilePath),
            TenantId: "TestTenant",
            FileName: fileName,
            MimeType: mimeType
        );

        var request = new AdapterRequest(
            PlatformType: "TestClient",
            ConversationId: conversationId,
            UserId: "TestUserWithContext",
            QueryText: "Based *only* on the provided text context, what fruits are mentioned?", // Query related to test_context.txt content
            ArtifactReferences: new List<ArtifactReference> { artifactReference } // Include the artifact
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/interaction/query", request);

        // Assert
        try
        {
            Assert.IsTrue(response.IsSuccessStatusCode, $"API request failed with status code {response.StatusCode}. Details: {await response.Content.ReadAsStringAsync()}");
        }
        catch (HttpRequestException ex)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Fail($"API request failed with status code {response.StatusCode}. Details: {ex.Message}\nResponse Content: {errorContent}");
        }
        
        // Removed: No body expected for NotActivated
        // var result = await response.Content.ReadFromJsonAsync<AdapterResponse?>();
        // Assert.IsNotNull(result);
        // Assert.IsTrue(result.Success, $"API indicated failure: {result.ErrorMessage}");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose(); // Ensure the factory is disposed
    }
}
