// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Data.Persistence;

/// <summary>
/// Provides persona configurations loaded directly from the application's IConfiguration source (e.g., appsettings.json).
/// </summary>
/// <remarks>
/// This implementation relies on the options pattern, expecting a `List&lt;PersonaConfiguration&gt;` 
/// to be bound to the configuration section defined by `NucleusConstants.ConfigurationKeys.PersonasSection`.
/// </remarks>
public class ConfigurationPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private readonly IOptions<List<PersonaConfiguration>> _personaOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPersonaConfigurationProvider"/> class.
    /// </summary>
    /// <param name="personaOptions">The options snapshot containing the persona configurations.</param>
    public ConfigurationPersonaConfigurationProvider(IOptions<List<PersonaConfiguration>> personaOptions)
    {
        _personaOptions = personaOptions ?? throw new ArgumentNullException(nameof(personaOptions));
    }

    // Note: The original interface seems to only require GetConfigurationAsync(string, CancellationToken).
    // The GetPersonaConfigurationsAsync might be from a previous iteration or a different interface branch.
    // Keeping it for now, but the primary implementation is GetConfigurationAsync based on the build error.

    /// 
    /// This method might be redundant if the interface only defines GetConfigurationAsync(id).
    /// Keeping it commented out unless specifically needed later.
    // public Task<IEnumerable<PersonaConfiguration>> GetPersonaConfigurationsAsync()
    // {
    //     // Return the configurations directly from the options snapshot.
    //     // Ensure the value is not null; return empty list if it is (though options binding usually ensures non-null).
    //     var configurations = _personaOptions.Value ?? Enumerable.Empty<PersonaConfiguration>();
    //     return Task.FromResult(configurations);
    // }

    /// <inheritdoc />
    public Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        // Find the persona configuration by ID in the options list.
        var persona = _personaOptions.Value?.FirstOrDefault(p => 
            p.PersonaId.Equals(personaId, StringComparison.OrdinalIgnoreCase));

        // Return the found persona configuration, or null if not found.
        return Task.FromResult(persona);
    }

    /// <inheritdoc />
    public Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // Return the configurations directly from the options snapshot.
        // Ensure the value is not null; return empty list if it is (though options binding usually ensures non-null).
        var configurations = _personaOptions.Value ?? Enumerable.Empty<PersonaConfiguration>();
        return Task.FromResult(configurations);
    }
}
