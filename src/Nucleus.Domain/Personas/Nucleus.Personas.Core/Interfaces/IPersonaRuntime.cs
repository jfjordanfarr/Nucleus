// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <param name="interactionContext">The context of the current user interaction, including request details and resolved artifacts.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>An <see cref="AdapterResponse"/> containing the persona's output, success status, and any generated artifact references.</returns>
    Task<AdapterResponse> ExecuteAsync(
        PersonaConfiguration personaConfig,
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default);
}