// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Infrastructure.Data.Persistence;

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
            _logger.LogWarning("Attempted to retrieve persona configuration with null or empty personaId.");
            return null;
        }

        try
        {
            var container = _cosmosClient.GetContainer(_cosmosDbSettings.DatabaseName, _cosmosDbSettings.PersonaContainerName);
            
            _logger.LogDebug("Attempting to read persona configuration for PersonaId '{PersonaId}' from container '{ContainerName}'.", 
                personaId, 
                _cosmosDbSettings.PersonaContainerName);

            // Assumption: The document 'id' in Cosmos DB matches the personaId.
            // Partition key is assumed to be the same as the id for efficient point reads.
            var response = await container.ReadItemAsync<PersonaConfiguration>(
                id: personaId,
                partitionKey: new PartitionKey(personaId), 
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully retrieved persona configuration for PersonaId '{PersonaId}'.", personaId);
                return response.Resource;
            }
            else // Should not happen with ReadItemAsync if item exists, as it throws on NotFound by default
            {
                _logger.LogWarning("Received unexpected status code {StatusCode} when retrieving persona configuration for PersonaId '{PersonaId}'.", 
                    response.StatusCode, 
                    personaId);
                return null;
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Persona configuration with PersonaId '{PersonaId}' not found in Cosmos DB container '{ContainerName}'.", 
                personaId, 
                _cosmosDbSettings.PersonaContainerName);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB error retrieving persona configuration for PersonaId '{PersonaId}': {StatusCode}", 
                personaId, 
                ex.StatusCode);
            // Depending on policy, might re-throw or return null
            return null; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving persona configuration for PersonaId '{PersonaId}'.", personaId);
            // Depending on policy, might re-throw or return null
            return null;
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement logic to retrieve all configurations from Cosmos DB.
        _logger.LogWarning("Retrieving all persona configurations from Cosmos DB is not yet implemented.");
        throw new NotImplementedException();
    }
}
