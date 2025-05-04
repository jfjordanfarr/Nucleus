// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Collections.Generic;

namespace Nucleus.Infrastructure.Testing.Repositories;

/// <summary>
/// Test implementation of <see cref="IPersonaConfigurationProvider"/> that uses a Cosmos DB emulator.
/// </summary>
/// <remarks>
/// This provider assumes that the <see cref="CosmosClient"/> instance injected into it
/// is correctly configured to point to a running Cosmos DB emulator, typically managed
/// by the test host environment (e.g., Aspire).
/// It also assumes the emulator has the necessary database and container created and potentially seeded.
/// </remarks>
public class EmulatorCosmosDbPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private readonly CosmosClient _emulatorCosmosClient;
    private readonly CosmosDbSettings _cosmosDbSettings;
    private readonly ILogger<EmulatorCosmosDbPersonaConfigurationProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmulatorCosmosDbPersonaConfigurationProvider"/> class.
    /// </summary>
    /// <param name="emulatorCosmosClient">The Cosmos DB client connected to the emulator.</param>
    /// <param name="cosmosDbOptions">The Cosmos DB configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    public EmulatorCosmosDbPersonaConfigurationProvider(
        CosmosClient emulatorCosmosClient, // Expecting the client connected to the emulator
        IOptions<CosmosDbSettings> cosmosDbOptions,
        ILogger<EmulatorCosmosDbPersonaConfigurationProvider> logger)
    {
        _emulatorCosmosClient = emulatorCosmosClient ?? throw new ArgumentNullException(nameof(emulatorCosmosClient));
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
        _logger.LogInformation("EmulatorCosmosDbPersonaConfigurationProvider initialized. Using Database: {DatabaseName}, Container: {ContainerName}", 
            _cosmosDbSettings.DatabaseName, _cosmosDbSettings.PersonaContainerName);
    }

    /// <inheritdoc />
    public async Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            _logger.LogWarning("[Emulator] Attempted to retrieve persona configuration with null or empty personaId.");
            return null;
        }

        try
        {
            var container = _emulatorCosmosClient.GetContainer(_cosmosDbSettings.DatabaseName, _cosmosDbSettings.PersonaContainerName);
            
            _logger.LogDebug("[Emulator] Attempting to read persona configuration for PersonaId '{PersonaId}' from container '{ContainerName}'.", 
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
                _logger.LogInformation("[Emulator] Successfully retrieved persona configuration for PersonaId '{PersonaId}'.", personaId);
                return response.Resource;
            }
            else
            {
                _logger.LogWarning("[Emulator] Received unexpected status code {StatusCode} when retrieving persona configuration for PersonaId '{PersonaId}'.", 
                    response.StatusCode, 
                    personaId);
                return null;
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "[Emulator] Persona configuration with PersonaId '{PersonaId}' not found in Cosmos DB container '{ContainerName}'. Ensure the emulator is seeded correctly.", 
                personaId, 
                _cosmosDbSettings.PersonaContainerName);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "[Emulator] Cosmos DB error retrieving persona configuration for PersonaId '{PersonaId}': {StatusCode}", 
                personaId, 
                ex.StatusCode);
            return null; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Emulator] Unexpected error retrieving persona configuration for PersonaId '{PersonaId}'.", personaId);
            return null;
        }
    }

    public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // For the emulator/test context, returning an empty list might be sufficient
        // Or, if needed, load from a test data source.
        _logger.LogWarning("EmulatorCosmosDbPersonaConfigurationProvider.GetAllConfigurationsAsync returning empty list. Implement loading if needed for tests.");
        return Task.FromResult(Enumerable.Empty<PersonaConfiguration>());
    }
}
