// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration; // Required for AgenticStrategyParametersBase
using Nucleus.Abstractions.Orchestration;
using Nucleus.Domain.Personas.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Personas.Core.Strategies;

/// <summary>
/// Concrete parameter class for the Echo strategy (currently empty).
/// Inherits from <see cref="AgenticStrategyParametersBase"/>.
/// </summary>
public class EchoStrategyParameters : AgenticStrategyParametersBase { }

/// <summary>
/// A simple agentic strategy handler that echoes back the user's query text.
/// Useful for basic testing of the PersonaRuntime and handler delegation.
/// Implements <see cref="IAgenticStrategyHandler"/> and uses the key "EchoStrategy".
/// </summary>
public class EchoAgenticStrategyHandler : IAgenticStrategyHandler
{
    private readonly ILogger<EchoAgenticStrategyHandler> _logger;

    public EchoAgenticStrategyHandler(ILogger<EchoAgenticStrategyHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string StrategyKey => "EchoStrategy"; // Keep StrategyKey implementation

    /// <inheritdoc/>
    public Task<AdapterResponse> HandleAsync(
        PersonaConfiguration personaConfig,
        AgenticStrategyParametersBase? strategyParameters, // Updated type
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default)
    {
        if (interactionContext == null) throw new ArgumentNullException(nameof(interactionContext));
        if (personaConfig == null) throw new ArgumentNullException(nameof(personaConfig));

        // Optional: Validate or use parameters if they were defined and passed
        // EchoStrategyParameters? echoParams = strategyParameters as EchoStrategyParameters;
        // if (strategyParameters != null && echoParams == null)
        // {
        //    _logger.LogWarning("EchoStrategy received non-null parameters of unexpected type: {ParameterType}", strategyParameters.GetType().Name);
        //    // Potentially return an error or ignore
        // }

        _logger.LogInformation("Executing EchoStrategy ({StrategyKey}) for persona '{PersonaId}'. Echoing query.", StrategyKey, personaConfig.PersonaId);

        // Simple echo logic
        var responseMessage = $"Echo from {personaConfig.DisplayName ?? personaConfig.PersonaId}: {interactionContext.OriginalRequest.QueryText}";

        // Construct the successful AdapterResponse
        var response = new AdapterResponse(
            Success: true,
            ResponseMessage: responseMessage
        );

        return Task.FromResult(response); // Return a completed task
    }
}
