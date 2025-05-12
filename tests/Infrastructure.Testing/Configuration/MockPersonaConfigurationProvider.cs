using Nucleus.Abstractions.Models.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Testing.Configuration // Adjusted namespace
{
    /// <summary>
    /// A mock implementation of <see cref="IPersonaConfigurationProvider"/> for testing purposes.
    /// This allows tests to inject specific persona configurations.
    /// </summary>
    public class MockPersonaConfigurationProvider : IPersonaConfigurationProvider
    {
        private readonly Dictionary<string, PersonaConfiguration> _configurations = new Dictionary<string, PersonaConfiguration>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockPersonaConfigurationProvider"/> class.
        /// </summary>
        public MockPersonaConfigurationProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockPersonaConfigurationProvider"/> class
        /// with a predefined set of persona configurations.
        /// </summary>
        /// <param name="initialConfigurations">A collection of persona configurations to prepopulate the mock provider.</param>
        public MockPersonaConfigurationProvider(IEnumerable<PersonaConfiguration> initialConfigurations)
        {
            foreach (var config in initialConfigurations ?? Enumerable.Empty<PersonaConfiguration>())
            {
                if (config != null && !string.IsNullOrEmpty(config.PersonaId))
                {
                    _configurations[config.PersonaId] = config;
                }
            }
        }

        /// <summary>
        /// Adds or updates a persona configuration in the mock provider.
        /// </summary>
        /// <param name="configuration">The persona configuration to add or update.</param>
        public void AddOrUpdateConfiguration(PersonaConfiguration configuration)
        {
            if (configuration != null && !string.IsNullOrEmpty(configuration.PersonaId))
            {
                _configurations[configuration.PersonaId] = configuration;
            }
        }

        /// <summary>
        /// Removes a persona configuration from the mock provider.
        /// </summary>
        /// <param name="personaId">The ID of the persona configuration to remove.</param>
        public void RemoveConfiguration(string personaId)
        {
            if (!string.IsNullOrEmpty(personaId))
            {
                _configurations.Remove(personaId);
            }
        }

        /// <summary>
        /// Clears all persona configurations from the mock provider.
        /// </summary>
        public void ClearConfigurations()
        {
            _configurations.Clear();
        }

        /// <inheritdoc />
        public Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(personaId))
            {
                return Task.FromResult<PersonaConfiguration?>(null);
            }

            _configurations.TryGetValue(personaId, out var configuration);
            return Task.FromResult<PersonaConfiguration?>(configuration);
        }

        /// <inheritdoc />
        public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_configurations.Values.AsEnumerable());
        }
    }
}
