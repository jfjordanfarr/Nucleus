// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration; 

/// <summary>
/// Defines the contract for a service that provides Persona configurations.
/// This is crucial for loading the correct operational parameters and knowledge scopes for a resolved Persona.
/// </summary>
/// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md">Orchestration Session Initiation - Persona Configuration Loading</seealso>
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#31-ipersonaconfigurationprovidercs"/>
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