// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Configuration; 
using System.Collections.Generic; 

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the contract for a service that checks if an incoming interaction request
/// should trigger activation based on configured rules.
/// </summary>
/// <remarks>
/// This component is central to the API's initial request handling, determining whether
/// to proceed with synchronous or asynchronous processing or to ignore the interaction.
/// </remarks>
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
public interface IActivationChecker
{
    /// <summary>
    /// Checks if any persona should be activated based on the incoming request and available persona configurations.
    /// </summary>
    /// <param name="request">The incoming adapter request.</param>
    /// <param name="configurations">An enumerable of all available persona configurations.</param>
    /// <param name="cancellationToken">A token for cancelling the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <see cref="ActivationResult"/>
    /// indicating whether a persona should activate, and if so, which one and its configuration.
    /// </returns>
    Task<ActivationResult> CheckActivationAsync(
        AdapterRequest request,
        IEnumerable<PersonaConfiguration> configurations,
        CancellationToken cancellationToken = default);
}
