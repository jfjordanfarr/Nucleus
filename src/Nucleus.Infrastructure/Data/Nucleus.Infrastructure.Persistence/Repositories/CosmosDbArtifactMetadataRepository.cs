using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Repositories;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Data.Persistence.Repositories;

/// <summary>
/// Cosmos DB implementation of the <see cref="IArtifactMetadataRepository"/>.
/// Manages CRUD operations for <see cref="ArtifactMetadata"/> in Azure Cosmos DB.
/// </summary>
/// <seealso cref="../../../../../../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md"/>
/// <seealso cref="../../../../../Docs/Architecture/Hosting/Azure/ARCHITECTURE_HOSTING_AZURE_COSMOSDB.md"/>
/// <seealso cref="../../../../../Docs/Architecture/Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md"/>
/// <seealso cref="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
public sealed class CosmosDbArtifactMetadataRepository : IArtifactMetadataRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbArtifactMetadataRepository> _logger;

    public CosmosDbArtifactMetadataRepository(
        CosmosClient cosmosClient, 
        IOptions<CosmosDbSettings> cosmosDbSettingsOptions, 
        ILogger<CosmosDbArtifactMetadataRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(cosmosDbSettingsOptions?.Value);

        var settings = cosmosDbSettingsOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
        {
            throw new ArgumentException("CosmosDB DatabaseName is not configured.", nameof(settings.DatabaseName));
        }
        if (string.IsNullOrWhiteSpace(settings.MetadataContainerName))
        {
            throw new ArgumentException("CosmosDB MetadataContainerName is not configured.", nameof(settings.MetadataContainerName));
        }

        var database = cosmosClient.GetDatabase(settings.DatabaseName);
        _container = database.GetContainer(settings.MetadataContainerName);
    }

    /// <inheritdoc />
    public async Task<ArtifactMetadata?> GetByIdAsync(string id, string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        _logger.LogDebug("Attempting to retrieve artifact metadata by ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);

        try
        {
            ItemResponse<ArtifactMetadata> response = await _container.ReadItemAsync<ArtifactMetadata>(id, new PartitionKey(partitionKey));
            _logger.LogInformation("Successfully retrieved artifact metadata by ID: {Id}", id);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Artifact metadata not found for ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error retrieving artifact metadata by ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);
            throw; // Re-throw the exception to be handled by upper layers
        }
    }

    /// <inheritdoc />
    public async Task<ArtifactMetadata?> GetBySourceIdentifierAsync(string sourceIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);
        _logger.LogDebug("Attempting to retrieve artifact metadata by SourceIdentifier: {SourceIdentifier}", sourceIdentifier);

        // This query might span partitions if SourceIdentifier is not the partition key or part of it.
        // Consider performance implications for large datasets.
        // We assume SourceIdentifier is unique across partitions for this implementation.

        var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.sourceIdentifier = @sourceIdentifier")
            .WithParameter("@sourceIdentifier", sourceIdentifier);

        try
        {
            using FeedIterator<ArtifactMetadata> feedIterator = _container.GetItemQueryIterator<ArtifactMetadata>(queryDefinition);
            
            if (feedIterator.HasMoreResults)
            {
                FeedResponse<ArtifactMetadata> response = await feedIterator.ReadNextAsync();
                foreach (var item in response)
                {
                    // Assuming SourceIdentifier is globally unique, return the first match.
                    _logger.LogInformation("Successfully retrieved artifact metadata by SourceIdentifier: {SourceIdentifier}", sourceIdentifier);
                    return item;
                }
            }

            _logger.LogWarning("Artifact metadata not found for SourceIdentifier: {SourceIdentifier}", sourceIdentifier);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error retrieving artifact metadata by SourceIdentifier: {SourceIdentifier}", sourceIdentifier);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ArtifactMetadata> SaveAsync(ArtifactMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        _logger.LogDebug("Attempting to save artifact metadata with ID: {Id}, PartitionKey: {PartitionKey}", metadata.Id, metadata.PartitionKey);

        try
        {
            // Update modification timestamp before saving
            metadata.ModifiedAtNucleus = DateTimeOffset.UtcNow;
            // Consider setting ModifiedByUserId if available in context

            ItemResponse<ArtifactMetadata> response = await _container.UpsertItemAsync(metadata, new PartitionKey(metadata.PartitionKey));
            _logger.LogInformation("Successfully saved artifact metadata with ID: {Id}. Status Code: {StatusCode}", metadata.Id, response.StatusCode);
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error saving artifact metadata with ID: {Id}, PartitionKey: {PartitionKey}", metadata.Id, metadata.PartitionKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        _logger.LogDebug("Attempting to delete artifact metadata by ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);

        try
        {
            ItemResponse<ArtifactMetadata> response = await _container.DeleteItemAsync<ArtifactMetadata>(id, new PartitionKey(partitionKey));
            _logger.LogInformation("Successfully deleted artifact metadata with ID: {Id}. Status Code: {StatusCode}", id, response.StatusCode);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Arguably, deleting a non-existent item is not an error for the caller.
            _logger.LogWarning("Attempted to delete non-existent artifact metadata with ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error deleting artifact metadata by ID: {Id}, PartitionKey: {PartitionKey}", id, partitionKey);
            throw;
        }
    }
}
