// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection; // Using standard IServiceProvider for keyed access
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration; // Required for AgenticStrategyParametersBase
using Nucleus.Abstractions.Orchestration;
using Nucleus.Domain.Personas.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Personas.Core;

/// <summary>
/// Runtime engine responsible for executing a specific persona configuration within a given interaction context.
/// It selects and delegates to the appropriate <see cref="IAgenticStrategyHandler"/> based on the configuration.
/// Implements <see cref="IPersonaRuntime"/>.
/// </summary>
public class PersonaRuntime : IPersonaRuntime
{
    // Use IServiceProvider, GetKeyedService is an extension method on it in .NET 8+
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PersonaRuntime> _logger;

    // Inject IServiceProvider
    public PersonaRuntime(IServiceProvider serviceProvider, ILogger<PersonaRuntime> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AdapterResponse> ExecuteAsync(
        PersonaConfiguration personaConfig,
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default)
    {
        if (personaConfig == null) throw new ArgumentNullException(nameof(personaConfig));
        if (interactionContext == null) throw new ArgumentNullException(nameof(interactionContext));

        // --- Updated Section ---
        if (personaConfig.AgenticStrategy == null)
        {
            _logger.LogError("PersonaConfiguration {PersonaId} is missing the AgenticStrategy configuration.", personaConfig.PersonaId);
            return new AdapterResponse(Success: false, ResponseMessage: "Internal configuration error: Agentic strategy configuration missing.", ErrorMessage: "AgenticStrategy missing");
        }

        // 1. Identify the strategy key string
        var strategyKey = personaConfig.AgenticStrategy.StrategyKey;
        if (string.IsNullOrWhiteSpace(strategyKey))
        {
            _logger.LogError("PersonaConfiguration {PersonaId} does not specify a valid StrategyKey in AgenticStrategy.", personaConfig.PersonaId);
            return new AdapterResponse(Success: false, ResponseMessage: "Internal configuration error: Agentic strategy key not specified.", ErrorMessage: "StrategyKey missing or empty");
        }

        _logger.LogInformation("Executing persona '{PersonaId}' using strategy '{StrategyKey}'.", personaConfig.PersonaId, strategyKey);

        try
        {
            // 2. Resolve the appropriate IAgenticStrategyHandler using the string key
            var handler = _serviceProvider.GetKeyedService<IAgenticStrategyHandler>(strategyKey);

            if (handler == null)
            {
                _logger.LogError("No {HandlerInterface} registered for key '{StrategyKey}'. Ensure handler is registered with DI using this key.",
                    nameof(IAgenticStrategyHandler), strategyKey);
                return new AdapterResponse(Success: false, ResponseMessage: $"Internal configuration error: No handler found for strategy '{strategyKey}'.", ErrorMessage: "Handler not found");
            }

            // Verify the resolved handler's key matches the requested key (optional sanity check)
            if (handler.StrategyKey != strategyKey)
            {
                 _logger.LogWarning("Resolved handler's key '{HandlerKey}' does not match requested key '{RequestedKey}'. Check DI registration.",
                    handler.StrategyKey, strategyKey);
                // Decide whether to proceed or return an error
            }

            // 3. Extract the specific strategy parameters object (now typed as base class)
            var strategyParameters = personaConfig.AgenticStrategy.Parameters;

            // 4. Invoke the handler's HandleAsync method
            var response = await handler.HandleAsync(personaConfig, strategyParameters, interactionContext, cancellationToken);
            // --- End Updated Section ---

            _logger.LogInformation("Strategy '{StrategyKey}' for persona '{PersonaId}' completed with Success={Success}.", strategyKey, personaConfig.PersonaId, response.Success);
            return response;
        }
        catch (InvalidCastException castEx) // Catch potential errors if handler casts parameters incorrectly
        {
             _logger.LogError(castEx, "Error casting strategy parameters for strategy '{StrategyKey}' in persona '{PersonaId}'. Check handler implementation and configuration.",
                strategyKey, personaConfig.PersonaId);
             return new AdapterResponse(Success: false, ResponseMessage: "Internal configuration error during parameter processing.", ErrorMessage: $"Parameter type mismatch for strategy {strategyKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing strategy '{StrategyKey}' for persona '{PersonaId}'.", strategyKey, personaConfig.PersonaId);
            return new AdapterResponse(Success: false, ResponseMessage: "An unexpected error occurred during processing.", ErrorMessage: ex.Message);
        }
    }
}