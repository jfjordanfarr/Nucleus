// Copyright (c) Jordan Farr. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

#pragma warning disable CA1812 // Class is instantiated via DI
/// <summary>
/// A null implementation of <see cref="IBackgroundTaskQueue"/> that performs no operations.
/// Used when messaging infrastructure (like Azure Service Bus) is not configured.
/// </summary>
internal sealed class NullBackgroundTaskQueue : IBackgroundTaskQueue
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
        // Null implementation: Discard the item immediately.
        // Log that we are discarding the item, including a request identifier if available.
        _logger.LogWarning("Service Bus is not configured. Discarding background work item for PlatformType '{PlatformType}', Conversation '{ConversationId}', CorrelationId '{CorrelationId}'.",
            request.PlatformType, request.OriginatingConversationId, request.CorrelationId ?? "N/A"); // Use CorrelationId
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member (expected for null object pattern).
    public ValueTask<NucleusIngestionRequest?> DequeueAsync(CancellationToken cancellationToken)
    {
        // Null implementation: Always return null as there's nothing to dequeue.
        _logger.LogDebug("Attempted to dequeue from NullBackgroundTaskQueue (Service Bus not configured).");
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
        // Rationale: This is the explicit behavior of the Null Object pattern here.
        return ValueTask.FromResult<NucleusIngestionRequest?>(null);
#pragma warning restore CS8613
    }
#pragma warning restore CS8613

    /// <inheritdoc />
    public ValueTask CompleteWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempted to complete work item via NullBackgroundTaskQueue for CorrelationId '{CorrelationId}'. No action taken.", request.CorrelationId ?? "N/A"); // Use CorrelationId
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask AbandonWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Attempted to abandon work item via NullBackgroundTaskQueue for CorrelationId '{CorrelationId}'. No action taken.", request.CorrelationId ?? "N/A"); // Use CorrelationId
        return ValueTask.CompletedTask;
    }
}
#pragma warning restore CA1812
