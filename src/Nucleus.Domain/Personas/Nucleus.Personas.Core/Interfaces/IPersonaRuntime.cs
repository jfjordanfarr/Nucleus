// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions; // Re-added missing using directive
using Nucleus.Abstractions.Models.ApiContracts; // Added using directive for ApiContracts
using Nucleus.Abstractions.Models; // Contains AdapterResponse
using Nucleus.Abstractions.Models.Configuration; // Contains PersonaConfiguration
using Nucleus.Abstractions.Orchestration; // Contains InteractionContext
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Personas.Core.Interfaces;

/// <summary>
/// Defines the contract for the Persona Runtime engine, responsible for executing
/// a specific persona configuration within a given interaction context.
/// </summary>
/// <remarks>
/// This interface represents the core execution logic for a configured persona.
/// Implementations will handle loading the configuration, selecting the appropriate
/// agentic strategy, and orchestrating the steps involved in generating a response.
/// See: Docs/Architecture/02_ARCHITECTURE_PERSONAS.md
/// See: Docs/Architecture/Namespaces/NAMESPACE_PERSONAS_CORE.md
/// </remarks>
public interface IPersonaRuntime
{
    /// <summary>
    /// Executes the agentic process defined by the persona configuration for the given interaction.
    /// </summary>
    /// <param name="personaConfig">The configuration defining the persona's behavior, strategies, and parameters.</param>
    /// <param name="interactionContext">Contextual information about the interaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a tuple containing the response from the persona and its execution status.</returns>
    Task<(AdapterResponse Response, PersonaExecutionStatus Status)> ExecuteAsync(
        PersonaConfiguration personaConfig,
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default);
}