// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Implements the <see cref="IActivationChecker"/> interface to determine if an interaction
/// should activate a Persona based on predefined rules.
/// </summary>
/// <remarks>
/// TODO: Enhance with configurable rules, user checks, platform context, etc.
/// Currently, it performs a basic check for a specific mention.
/// </remarks>
/// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md">Interaction Processing Lifecycle - Activation Check</seealso>
public class ActivationChecker : IActivationChecker
{
    private readonly ILogger<ActivationChecker> _logger;
    // TODO: Inject configuration for activation rules (e.g., mention string)

    public ActivationChecker(ILogger<ActivationChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<ActivationResult> CheckActivationAsync(
        AdapterRequest request,
        IEnumerable<PersonaConfiguration> configurations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(configurations);

        // Basic rule: Check for "@Nucleus" mention (case-insensitive)
        // TODO: Enhance with configurable rules, user checks, platform context, etc.
        // This simple version just checks the mention and picks the *first* matching config.
        // A real implementation needs to evaluate PersonaConfiguration.ActivationTriggers and ContextScope.
        bool mentionFound = request.QueryText?.Contains("@Nucleus", StringComparison.OrdinalIgnoreCase) ?? false;

        if (mentionFound)
        {
            // For this basic implementation, just return the first configuration found.
            var firstConfig = configurations.FirstOrDefault();
            if (firstConfig != null)
            {
                _logger.LogInformation("Activation Check PASSED for ConversationId {ConversationId} due to '@Nucleus' mention. Activating Persona: {PersonaId}", 
                    request.ConversationId, firstConfig.PersonaId);
                return Task.FromResult(new ActivationResult(true, firstConfig.PersonaId, firstConfig));
            }
            else
            {
                _logger.LogWarning("Activation Check PASSED for ConversationId {ConversationId} due to '@Nucleus' mention, but NO persona configurations were provided.", request.ConversationId);
            }
        }
        else
        {
            _logger.LogDebug("Activation Check FAILED for ConversationId {ConversationId}. Mention '@Nucleus' not found.", request.ConversationId);
        }

        // If no mention or no configurations, return non-activated result
        return Task.FromResult(new ActivationResult(false));
    }
}
