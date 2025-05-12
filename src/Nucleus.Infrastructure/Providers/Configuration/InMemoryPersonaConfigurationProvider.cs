using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using System.Collections.Concurrent;

namespace Nucleus.Infrastructure.Providers.Configuration;

/// <summary>
/// An in-memory implementation of <see cref="IPersonaConfigurationProvider"/> for testing purposes.
/// This provider allows injecting a specific set of persona configurations into the application
/// when it's running, typically controlled by an environment variable during integration tests.
/// </summary>
public class InMemoryPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private readonly ILogger<InMemoryPersonaConfigurationProvider> _logger;
    private readonly ConcurrentDictionary<string, PersonaConfiguration> _configurations;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPersonaConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="initialConfigurations">An optional collection of persona configurations to seed the provider.</param>
    public InMemoryPersonaConfigurationProvider(
        ILogger<InMemoryPersonaConfigurationProvider> logger,
        IEnumerable<PersonaConfiguration>? initialConfigurations = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurations = new ConcurrentDictionary<string, PersonaConfiguration>();

        if (initialConfigurations != null)
        {
            foreach (var config in initialConfigurations)
            {
                if (string.IsNullOrWhiteSpace(config.PersonaId))
                {
                    _logger.LogWarning("Attempted to add a persona configuration with a null or empty PersonaId. Skipping.");
                    continue;
                }
                if (!_configurations.TryAdd(config.PersonaId, config))
                {
                    _logger.LogWarning("Attempted to add a persona configuration with a duplicate PersonaId '{PersonaId}'. Skipping.", config.PersonaId);
                }
            }
            _logger.LogInformation("Initialized InMemoryPersonaConfigurationProvider with {Count} configurations.", _configurations.Count);
        }
        else
        {
            _logger.LogInformation("Initialized InMemoryPersonaConfigurationProvider with no initial configurations.");
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all persona configurations from memory. Count: {Count}", _configurations.Values.Count);
        return Task.FromResult<IEnumerable<PersonaConfiguration>>(_configurations.Values.ToList());
    }

    /// <inheritdoc />
    public Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            _logger.LogWarning("Attempted to retrieve persona configuration with null or empty personaId.");
            return Task.FromResult<PersonaConfiguration?>(null);
        }

        if (_configurations.TryGetValue(personaId, out var configuration))
        {
            _logger.LogDebug("Retrieved persona configuration for PersonaId '{PersonaId}' from memory.", personaId);
            return Task.FromResult<PersonaConfiguration?>(configuration);
        }

        _logger.LogDebug("Persona configuration for PersonaId '{PersonaId}' not found in memory.", personaId);
        return Task.FromResult<PersonaConfiguration?>(null);
    }

    /// <summary>
    /// Adds or updates a persona configuration in the in-memory store.
    /// This method is specific to the in-memory provider for test setup.
    /// </summary>
    /// <param name="configuration">The persona configuration to add or update.</param>
    public void AddOrUpdateConfiguration(PersonaConfiguration configuration)
    {
        if (configuration == null)
        {
            _logger.LogWarning("Attempted to add or update a null persona configuration.");
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.PersonaId))
        {
            _logger.LogWarning("Attempted to add or update a persona configuration with a null or empty PersonaId. Skipping.");
            return;
        }

        _configurations.AddOrUpdate(configuration.PersonaId, configuration, (key, existingVal) => configuration);
        _logger.LogInformation("Added or updated persona configuration for PersonaId '{PersonaId}' in memory.", configuration.PersonaId);
    }

    /// <summary>
    /// Clears all persona configurations from the in-memory store.
    /// This method is specific to the in-memory provider for test setup/teardown.
    /// </summary>
    public void ClearConfigurations()
    {
        _configurations.Clear();
        _logger.LogInformation("Cleared all persona configurations from in-memory store.");
    }
}
