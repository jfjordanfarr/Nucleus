// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using System.Collections.Generic;

namespace Nucleus.Infrastructure.Data.Persistence.Configuration;

/// <summary>
/// Implements <see cref="IPersonaConfigurationProvider"/> using Azure Cosmos DB as the backing store.
/// </summary>
/// <remarks>
/// Assumes that PersonaConfiguration documents are stored in the container specified in <see cref="CosmosDbSettings.PersonaContainerName"/>
/// and that the document's 'id' field corresponds to the <see cref="PersonaConfiguration.PersonaId"/>.
/// </remarks>
public class CosmosDbPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _cosmosDbSettings;
    private readonly ILogger<CosmosDbPersonaConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbPersonaConfigurationProvider"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client instance.</param>
    /// <param name="cosmosDbOptions">The Cosmos DB configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    public CosmosDbPersonaConfigurationProvider(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> cosmosDbOptions,
        ILogger<CosmosDbPersonaConfigurationProvider> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _cosmosDbSettings = cosmosDbOptions?.Value ?? throw new ArgumentNullException(nameof(cosmosDbOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_cosmosDbSettings.DatabaseName))
        {
            throw new ArgumentException("Cosmos DB DatabaseName cannot be null or empty.", nameof(cosmosDbOptions));
        }
        if (string.IsNullOrWhiteSpace(_cosmosDbSettings.PersonaContainerName))
        {
            throw new ArgumentException("Cosmos DB PersonaContainerName cannot be null or empty.", nameof(cosmosDbOptions));
        }
    }

    /// <inheritdoc />
    public async Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            _logger.LogWarning("Attempted to get persona configuration with null or empty personaId.");
            return null;
        }

        try
        {
            var container = GetPersonaContainer();
            // Assuming personaId is also the partition key for optimal point reads.
            var response = await container.ReadItemAsync<PersonaConfiguration>(personaId, new PartitionKey(personaId), cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully retrieved persona configuration for PersonaId: {PersonaId}", personaId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Persona configuration with PersonaId: {PersonaId} not found.", personaId);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error retrieving persona configuration with PersonaId: {PersonaId} from Cosmos DB.", personaId);
            throw; // Re-throw to allow higher-level error handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving persona configuration with PersonaId: {PersonaId}.", personaId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var configurations = new List<PersonaConfiguration>();
        try
        {
            var container = GetPersonaContainer();
            var query = new QueryDefinition("SELECT * FROM c");

            _logger.LogInformation("Retrieving all persona configurations from container: {ContainerName}", _cosmosDbSettings.PersonaContainerName);

            using (var feedIterator = container.GetItemQueryIterator<PersonaConfiguration>(query))
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync(cancellationToken);
                    configurations.AddRange(response.Resource);
                    _logger.LogDebug("Retrieved {Count} persona configurations in this batch. Total so far: {TotalCount}", response.Resource.Count(), configurations.Count);
                }
            }
            _logger.LogInformation("Successfully retrieved {TotalCount} persona configurations.", configurations.Count);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error retrieving all persona configurations from Cosmos DB container: {ContainerName}.", _cosmosDbSettings.PersonaContainerName);
            // Depending on requirements, you might return an empty list or re-throw
            // For now, re-throwing to make issues visible during development.
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving all persona configurations from Cosmos DB container: {ContainerName}.", _cosmosDbSettings.PersonaContainerName);
            throw;
        }
        return configurations;
    }

    private Container GetPersonaContainer()
    {
        return _cosmosClient.GetContainer(_cosmosDbSettings.DatabaseName, _cosmosDbSettings.PersonaContainerName);
    }
}
