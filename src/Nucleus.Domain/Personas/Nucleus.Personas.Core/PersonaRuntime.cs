// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Domain.Personas.Core.Interfaces;
using System.Diagnostics;

namespace Nucleus.Domain.Personas.Core;

/// <summary>
/// Default implementation of the persona runtime.
/// Handles loading configuration, selecting the appropriate agentic strategy,
/// and executing it within the interaction context.
/// </summary>
/// <seealso cref="Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md"/>
/// <seealso cref="Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
public class PersonaRuntime : IPersonaRuntime
{
    private readonly ILogger<PersonaRuntime> _logger;
    private readonly IEnumerable<IAgenticStrategyHandler> _handlers;

    public PersonaRuntime(ILogger<PersonaRuntime> logger, IEnumerable<IAgenticStrategyHandler> handlers)
    {
        _logger = logger;
        _handlers = handlers;
    }

    /// <inheritdoc />
    public async Task<(AdapterResponse Response, PersonaExecutionStatus Status)> ExecuteAsync(
        PersonaConfiguration personaConfig,
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personaConfig);
        ArgumentNullException.ThrowIfNull(interactionContext);

        var stopwatch = Stopwatch.StartNew();
        // Use ConversationId and MessageId for logging correlation
        _logger.LogInformation("Executing persona {PersonaId} with strategy {StrategyKey} for conversation {ConversationId}, message {MessageId}.",
            personaConfig.PersonaId, 
            personaConfig.AgenticStrategy?.StrategyKey ?? "None", 
            interactionContext.OriginalRequest.ConversationId, 
            interactionContext.OriginalRequest.MessageId ?? "(no message ID)");

        if (personaConfig.AgenticStrategy == null || string.IsNullOrEmpty(personaConfig.AgenticStrategy.StrategyKey))
        {
            _logger.LogWarning("Persona {PersonaId} has no AgenticStrategy configured. Returning default empty response. Conversation: {ConversationId}, Message: {MessageId}",
                personaConfig.PersonaId, 
                interactionContext.OriginalRequest.ConversationId, 
                interactionContext.OriginalRequest.MessageId ?? "(no message ID)");
            // Return failure using constructor
            return (new AdapterResponse(false, "No strategy configured."), PersonaExecutionStatus.Failed);
        }

        var strategyKey = personaConfig.AgenticStrategy.StrategyKey;
        var handler = _handlers.FirstOrDefault(h => h.StrategyKey.Equals(strategyKey, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            _logger.LogWarning("No agentic strategy handler found for key {StrategyKey} configured for persona {PersonaId}. Returning error response. Conversation: {ConversationId}, Message: {MessageId}",
                strategyKey, 
                personaConfig.PersonaId, 
                interactionContext.OriginalRequest.ConversationId, 
                interactionContext.OriginalRequest.MessageId ?? "(no message ID)");
            // Return failure using constructor
            return (new AdapterResponse(false, $"Configuration error: Strategy handler '{strategyKey}' not found.", $"Handler for strategy '{strategyKey}' is not registered or available."), PersonaExecutionStatus.Failed);
        }

        try
        {
            // Cast the parameters to the expected type
            var strategyParameters = personaConfig.AgenticStrategy.Parameters as AgenticStrategyParametersBase;
            
            // Log if casting resulted in null when parameters were expected (optional, depends on strategy needs)
            if (personaConfig.AgenticStrategy.Parameters != null && strategyParameters == null)
            {
                _logger.LogWarning("Strategy parameters for {StrategyKey} in persona {PersonaId} could not be cast to AgenticStrategyParametersBase. Type was {ParameterType}. Proceeding with null parameters.",
                    strategyKey, personaConfig.PersonaId, personaConfig.AgenticStrategy.Parameters.GetType().FullName);
            }

            var response = await handler.HandleAsync(
                personaConfig,
                strategyParameters, // Pass the potentially null casted parameters
                interactionContext,
                cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Persona {PersonaId} execution completed in {ElapsedMilliseconds}ms for conversation {ConversationId}, message {MessageId}.",
                personaConfig.PersonaId, stopwatch.ElapsedMilliseconds, interactionContext.OriginalRequest.ConversationId, interactionContext.OriginalRequest.MessageId ?? "(no message ID)");

            // Determine the status based on the strategy response
            // TODO: Refine status mapping - how do we determine Filtered/NoActionTaken from the strategy?
            // For now, map Success=true to Success, Success=false to Failed.
            var status = response.Success ? PersonaExecutionStatus.Success : PersonaExecutionStatus.Failed;

            return (response, status);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing agentic strategy {StrategyKey} for persona {PersonaId} (Conversation: {ConversationId}, Message: {MessageId}) after {ElapsedMilliseconds}ms.",
                strategyKey, 
                personaConfig.PersonaId, 
                interactionContext.OriginalRequest.ConversationId, 
                interactionContext.OriginalRequest.MessageId ?? "(no message ID)", 
                stopwatch.ElapsedMilliseconds);

            // Return an error response using the correct AdapterResponse constructor
            return (new AdapterResponse(
                Success: false, 
                ResponseMessage: "An error occurred while processing your request.", 
                ErrorMessage: $"Error in strategy '{strategyKey}': {ex.Message}"
            ), PersonaExecutionStatus.Failed);
        }
    }
}