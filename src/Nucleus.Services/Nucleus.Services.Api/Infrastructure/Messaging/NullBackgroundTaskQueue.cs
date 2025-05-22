// Copyright (c) Jordan Farr. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

#pragma warning disable CA1812 // Class is instantiated via DI
/// <summary>
/// A null implementation of <see cref="IBackgroundTaskQueue"/> that performs no operations.
/// Used when messaging infrastructure (like Azure Service Bus) is not configured.
/// </summary>
/// <seealso href="../../../../../Docs/Architecture/09_ARCHITECTURE_TESTING.md" />
public sealed class NullBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ILogger<NullBackgroundTaskQueue> _logger;

    public NullBackgroundTaskQueue(ILogger<NullBackgroundTaskQueue> logger)
    {
        _logger = logger;
        _logger.LogInformation("NullBackgroundTaskQueue initialized. Background tasks will not be queued.");
    }

    /// <inheritdoc />
    public ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Attempted to enqueue work item via NullBackgroundTaskQueue (Service Bus not configured). CorrelationId: {CorrelationId}", (request.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", ""));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DequeuedMessage<NucleusIngestionRequest>?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Null implementation: Always return null as there's nothing to dequeue.
        _logger.LogDebug("Attempted to dequeue from NullBackgroundTaskQueue (Service Bus not configured).");
        return Task.FromResult<DequeuedMessage<NucleusIngestionRequest>?>(null);
    }

    /// <inheritdoc />
    public Task CompleteAsync(object messageContext, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempted to complete work item via NullBackgroundTaskQueue for messageContext '{messageContext}'. No action taken.", (messageContext?.ToString() ?? "N/A").Replace("\n", "").Replace("\r", ""));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AbandonAsync(object messageContext, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(exception, "Attempted to abandon work item via NullBackgroundTaskQueue for messageContext '{messageContext}'. No action taken.", (messageContext?.ToString() ?? "N/A").Replace("\n", "").Replace("\r", ""));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Attempted to complete work item via NullBackgroundTaskQueue for CorrelationId '{CorrelationId}'. No action taken.", (request.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", "")); // Use CorrelationId
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AbandonWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogWarning("Attempted to abandon work item via NullBackgroundTaskQueue for CorrelationId '{CorrelationId}'. No action taken.", (request.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", "")); // Use CorrelationId
        return Task.CompletedTask;
    }
}
#pragma warning restore CA1812
