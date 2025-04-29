// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration; // Required for AgenticStrategyParametersBase
using Nucleus.Abstractions.Orchestration;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Personas.Core.Interfaces;

/// <summary>
/// Defines the contract for handlers responsible for executing a specific agentic strategy
/// as defined within a PersonaConfiguration.
/// </summary>
/// <remarks>
/// The PersonaRuntime will select and invoke the appropriate handler based on the
/// strategy specified in the persona's configuration using the <see cref="StrategyKey"/>.
/// Implementations will contain the core logic for different processing approaches (e.g., simple retrieval,
/// multi-step reasoning, function calling).
/// Handlers should be registered with DI using their corresponding StrategyKey.
/// See: Docs/Architecture/02_ARCHITECTURE_PERSONAS.md
/// See: Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md
/// </remarks>
public interface IAgenticStrategyHandler
{
    /// <summary>
    /// Gets the unique key or name identifying the strategy this handler implements.
    /// This key must match the strategy identifier used in PersonaConfiguration.
    /// </summary>
    string StrategyKey { get; } // Keep StrategyKey

    /// <summary>
    /// Executes the specific agentic strategy.
    /// </summary>
    /// <param name="personaConfig">The full configuration for the persona being executed.</param>
    /// <param name="strategyParameters">
    /// Specific configuration parameters for this strategy, extracted from <see cref="AgenticStrategyConfiguration.Parameters"/>.
    /// Implementations should safely cast this to their expected concrete type derived from <see cref="AgenticStrategyParametersBase"/>.
    /// This can be null if no specific parameters are configured or required for the strategy.
    /// </param>
    /// <param name="interactionContext">The context of the current user interaction.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>An <see cref="AdapterResponse"/> containing the strategy's output.</returns>
    Task<AdapterResponse> HandleAsync(
        PersonaConfiguration personaConfig,
        AgenticStrategyParametersBase? strategyParameters, // Changed from object to AgenticStrategyParametersBase?
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default);
}