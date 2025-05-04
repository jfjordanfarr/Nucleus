// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions;
// using Nucleus.Abstractions.Models.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Testing.Configuration; // Updated Namespace

/// <summary>
/// A temporary, in-memory implementation of <see cref="IPersonaConfigurationProvider"/>.
/// THIS IS INTENTIONALLY LEFT EMPTY AND SHOULD NOT BE USED.
/// Rely on emulators/containers provided by Aspire for integration testing.
/// </summary>
/// <remarks>
/// This class is kept temporarily to satisfy type resolution during the transition phase.
/// It does not implement the required interface members and will cause build errors
/// until either removed completely or updated.
/// See: [Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md:0:0-0:0)
/// </remarks>
public class InMemoryPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private Dictionary<string, PersonaConfiguration> _configurations = new Dictionary<string, PersonaConfiguration>();

    // Intentionally left empty. Implementations were removed as part of the shift
    // towards using emulators (CosmosDB, RabbitMQ/NATS) for integration tests.
    // This class will likely be removed entirely once integration tests are fully
    // refactored to use Aspire's emulator/container infrastructure.

    // Corrected stub matching the interface signature:
    public Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        _ = personaId; // Avoid unused parameter warning
        _ = cancellationToken; // Avoid unused parameter warning
        throw new NotImplementedException("InMemoryPersonaConfigurationProvider is deprecated. Use emulator-backed configurations for testing.");
    }

    public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // Return all stored configurations
        return Task.FromResult(_configurations.Values.AsEnumerable());
    }

    // Method to add or update a persona configuration in the dictionary
    public void AddOrUpdateConfiguration(PersonaConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config); // Add null check for config
        _configurations[config.PersonaId] = config; // Use PersonaId instead of Id
    }
}
