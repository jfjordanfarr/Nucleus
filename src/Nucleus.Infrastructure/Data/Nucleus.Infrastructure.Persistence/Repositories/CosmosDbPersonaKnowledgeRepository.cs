using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
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
/// Cosmos DB implementation for storing and retrieving PersonaKnowledgeEntry records.
/// Corresponds to architecture doc: ARCHITECTURE_PERSISTENCE_REPOSITORIES.md
/// </summary>
public class CosmosDbPersonaKnowledgeRepository : IPersonaKnowledgeRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly Container _container;
    private readonly ILogger<CosmosDbPersonaKnowledgeRepository> _logger;

    public CosmosDbPersonaKnowledgeRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> cosmosDbOptions, ILogger<CosmosDbPersonaKnowledgeRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var settings = cosmosDbOptions?.Value ?? throw new ArgumentNullException(nameof(cosmosDbOptions), "CosmosDbSettings cannot be null.");

        _databaseName = settings.DatabaseName;
        _containerName = settings.KnowledgeContainerName;

        if (string.IsNullOrWhiteSpace(_databaseName))
        {
            throw new ArgumentException("Cosmos DB DatabaseName cannot be null or empty in settings.", nameof(cosmosDbOptions));
        }
        if (string.IsNullOrWhiteSpace(_containerName))
        {
            throw new ArgumentException("Cosmos DB KnowledgeContainerName cannot be null or empty in settings.", nameof(cosmosDbOptions));
        }

        _container = _cosmosClient.GetContainer(_databaseName, _containerName);
        _logger.LogInformation("CosmosDbPersonaKnowledgeRepository initialized for Database '{DatabaseName}', Container '{ContainerName}'.", _databaseName, _containerName);
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

    public async Task<PersonaKnowledgeEntry> SaveAsync(PersonaKnowledgeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

        // ID is now a required init-only property, set during object creation.
        // If an ID needs to be generated here, the model definition or creation logic needs adjustment.
        // Assuming Id is correctly set before calling SaveAsync.
        // Example (if Id generation was needed here, which contradicts 'init'):
        // var entryToSave = string.IsNullOrWhiteSpace(entry.Id)
        //     ? entry with { Id = Guid.NewGuid().ToString() }
        //     : entry;
        // _logger.LogDebug("Ensured ID '{Id}' for PersonaKnowledgeEntry.", entryToSave.Id);

        // Ensure TenantId is present for partition key
        if (string.IsNullOrWhiteSpace(entry.TenantId))
        {
             _logger.LogError("Attempted to save PersonaKnowledgeEntry with null or empty TenantId. ID: {Id}", entry.Id);
             throw new ArgumentException("TenantId must be provided to save PersonaKnowledgeEntry.", nameof(entry.TenantId));
        }

        try
        {
            // Upsert the original entry, assuming Id is correctly initialized
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
            // Log a warning if attempting to delete an item that doesn't exist
            _logger.LogWarning("Attempted to delete a non-existent knowledge entry with ID '{Id}', PartitionKey '{PartitionKey}'. Charge: {Charge} RU.", id, partitionKey, ex.RequestCharge);
            // Depending on requirements, might not need to re-throw here.
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "CosmosException occurred while deleting knowledge entry with ID '{Id}', PartitionKey '{PartitionKey}'. Status Code: {StatusCode}, Charge: {Charge} RU.", id, partitionKey, ex.StatusCode, ex.RequestCharge);
            throw; // Re-throw other exceptions
        }
    }
}
