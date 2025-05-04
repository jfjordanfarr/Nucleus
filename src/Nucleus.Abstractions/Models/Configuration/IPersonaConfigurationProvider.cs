// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration; 

/// <summary>
/// Defines the contract for retrieving Persona configurations.
/// Implementations are responsible for fetching the appropriate <see cref="PersonaConfiguration"/>
/// based on a persona identifier, potentially from sources like databases, files, or external services.
/// </summary>
public interface IPersonaConfigurationProvider
{
    /// <summary>
    /// Asynchronously retrieves the configuration for a specific persona.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona whose configuration is requested.</param>
    /// <param name="cancellationToken">A token for cancelling the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the
    /// <see cref="PersonaConfiguration"/> if found; otherwise, null.
    /// </returns>
    Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all available persona configurations.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an enumerable
    /// of all <see cref="PersonaConfiguration"/> instances.
    /// </returns>
    Task<IEnumerable<PersonaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);
}