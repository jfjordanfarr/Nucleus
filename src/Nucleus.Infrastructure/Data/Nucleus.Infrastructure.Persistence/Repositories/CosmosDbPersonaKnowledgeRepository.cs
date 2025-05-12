using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Data.Persistence.Repositories;

/// <summary>
/// Implements the <see cref="IPersonaKnowledgeRepository"/> for Azure Cosmos DB.
/// </summary>
public class CosmosDbPersonaKnowledgeRepository : IPersonaKnowledgeRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbPersonaKnowledgeRepository> _logger;

    public CosmosDbPersonaKnowledgeRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> cosmosDbSettingsOptions,
        ILogger<CosmosDbPersonaKnowledgeRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentNullException.ThrowIfNull(cosmosDbSettingsOptions?.Value);

        var settings = cosmosDbSettingsOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
        {
            throw new ArgumentException("CosmosDB DatabaseName is not configured.", nameof(settings.DatabaseName));
        }
        if (string.IsNullOrWhiteSpace(settings.KnowledgeContainerName))
        {
            throw new ArgumentException("CosmosDB KnowledgeContainerName is not configured.", nameof(settings.KnowledgeContainerName));
        }

        var database = cosmosClient.GetDatabase(settings.DatabaseName);
        _container = database.GetContainer(settings.KnowledgeContainerName);
    }

    // Partition key is TenantId for PersonaKnowledge
    private PartitionKey GetPartitionKey(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogError("TenantId cannot be null or empty when creating partition key for PersonaKnowledge.");
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        }
        return new PartitionKey(tenantId);
    }

    /// <inheritdoc />
    public async Task<PersonaKnowledgeEntry?> GetByIdAsync(string id, string partitionKey) // partitionKey here is TenantId
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("GetByIdAsync called with null or empty ID.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            _logger.LogWarning("GetByIdAsync called with null or empty partitionKey (TenantId).");
            return null;
        }

        try
        {
            ItemResponse<PersonaKnowledgeEntry> response = await _container.ReadItemAsync<PersonaKnowledgeEntry>(id, GetPartitionKey(partitionKey));
            _logger.LogDebug("Retrieved knowledge entry with ID '{Id}' and partition key '{PartitionKey}'. Charge: {Charge} RU.", id, partitionKey, response.RequestCharge);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Knowledge entry with ID '{Id}' and partition key '{PartitionKey}' not found. Charge: {Charge} RU.", id, partitionKey, ex.RequestCharge);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "CosmosException occurred while getting knowledge entry by ID '{Id}', PartitionKey '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", id, partitionKey, ex.StatusCode, ex.RequestCharge);
            throw; // Re-throw exceptions that aren't NotFound
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PersonaKnowledgeEntry>> GetByArtifactIdAsync(string artifactId, string partitionKey) // partitionKey here is TenantId
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            _logger.LogWarning("GetByArtifactIdAsync called with null or empty artifactId.");
            return Enumerable.Empty<PersonaKnowledgeEntry>();
        }
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            _logger.LogWarning("GetByArtifactIdAsync called with null or empty partitionKey (TenantId).");
            return Enumerable.Empty<PersonaKnowledgeEntry>();
        }

        var query = new QueryDefinition("SELECT * FROM c WHERE c.ArtifactId = @artifactId")
            .WithParameter("@artifactId", artifactId);

        var queryRequestOptions = new QueryRequestOptions
        {
            PartitionKey = GetPartitionKey(partitionKey)
        };

        var results = new List<PersonaKnowledgeEntry>();
        double totalCharge = 0;

        try
        {
            using FeedIterator<PersonaKnowledgeEntry> feedIterator = _container.GetItemQueryIterator<PersonaKnowledgeEntry>(query, requestOptions: queryRequestOptions);

            while (feedIterator.HasMoreResults)
            {
                FeedResponse<PersonaKnowledgeEntry> response = await feedIterator.ReadNextAsync();
                results.AddRange(response);
                totalCharge += response.RequestCharge;
                _logger.LogDebug("Query page for ArtifactId '{ArtifactId}', PartitionKey '{PartitionKey}' returned {Count} results. Charge: {Charge} RU.", artifactId, partitionKey, response.Count, response.RequestCharge);
            }
            _logger.LogInformation("Retrieved {Count} knowledge entries for ArtifactId '{ArtifactId}', PartitionKey '{PartitionKey}'. Total charge: {TotalCharge} RU.", results.Count, artifactId, partitionKey, totalCharge);
            return results;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "CosmosException occurred while querying knowledge entries by ArtifactId '{ArtifactId}', PartitionKey '{PartitionKey}'. Status Code: {StatusCode}", artifactId, partitionKey, ex.StatusCode);
            throw; // Re-throw exceptions
        }
    }

    /// <inheritdoc />
    public async Task<PersonaKnowledgeEntry> SaveAsync(PersonaKnowledgeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        // Ensure TenantId is present for partition key
        if (string.IsNullOrWhiteSpace(entry.TenantId))
        {
             _logger.LogError("Attempted to save PersonaKnowledgeEntry with null or empty TenantId. ID: {Id}", entry.Id);
             throw new ArgumentException("TenantId must be provided to save PersonaKnowledgeEntry.", nameof(entry.TenantId));
        }

        try
        {
            ItemResponse<PersonaKnowledgeEntry> response = await _container.UpsertItemAsync(entry, GetPartitionKey(entry.TenantId));
            _logger.LogInformation("Saved knowledge entry with ID '{Id}' and partition key '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", entry.Id, entry.TenantId, response.StatusCode, response.RequestCharge);
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "CosmosException occurred while saving knowledge entry with ID '{Id}', PartitionKey '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", entry.Id, entry.TenantId, ex.StatusCode, ex.RequestCharge);
            throw; // Re-throw exceptions
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, string partitionKey) // partitionKey here is TenantId
    {
         if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("DeleteAsync called with null or empty ID.");
            return; // Or throw?
        }
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            _logger.LogWarning("DeleteAsync called with null or empty partitionKey (TenantId).");
            return; // Or throw?
        }

        try
        {
            ItemResponse<PersonaKnowledgeEntry> response = await _container.DeleteItemAsync<PersonaKnowledgeEntry>(id, GetPartitionKey(partitionKey));
            _logger.LogInformation("Deleted knowledge entry with ID '{Id}' and partition key '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", id, partitionKey, response.StatusCode, response.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Attempted to delete a non-existent knowledge entry with ID '{Id}', PartitionKey '{PartitionKey}'. Charge: {Charge} RU.", id, partitionKey, ex.RequestCharge);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "CosmosException occurred while deleting knowledge entry with ID '{Id}', PartitionKey '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", id, partitionKey, ex.StatusCode, ex.RequestCharge);
            throw; // Re-throw other exceptions
        }
    }
}
