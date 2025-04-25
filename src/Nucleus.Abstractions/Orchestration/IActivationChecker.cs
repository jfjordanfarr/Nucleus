// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the contract for a service that checks if an incoming interaction
/// meets the criteria required to activate processing.
/// </summary>
public interface IActivationChecker
{
    /// <summary>
    /// Checks if the given interaction request should trigger processing.
    /// </summary>
    /// <param name="request">The incoming interaction request from the adapter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the interaction should activate processing, false otherwise.</returns>
    Task<bool> ShouldActivateAsync(AdapterRequest request, CancellationToken cancellationToken = default);
}
