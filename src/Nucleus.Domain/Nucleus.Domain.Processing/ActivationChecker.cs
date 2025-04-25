// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Basic implementation of activation checking.
/// Activates if the query text contains "@Nucleus".
/// </summary>
public class ActivationChecker : IActivationChecker
{
    private readonly ILogger<ActivationChecker> _logger;
    // TODO: Inject configuration for activation rules (e.g., mention string)

    public ActivationChecker(ILogger<ActivationChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<bool> ShouldActivateAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Basic rule: Check for "@Nucleus" mention (case-insensitive)
        // TODO: Enhance with configurable rules, user checks, platform context, etc.
        bool isActivated = request.QueryText?.Contains("@Nucleus", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isActivated)
        {
            _logger.LogInformation("Activation Check PASSED for ConversationId {ConversationId} due to '@Nucleus' mention.", request.ConversationId);
        }
        else
        {
            _logger.LogDebug("Activation Check FAILED for ConversationId {ConversationId}. Mention '@Nucleus' not found.", request.ConversationId);
        }

        return Task.FromResult(isActivated);
    }
}
