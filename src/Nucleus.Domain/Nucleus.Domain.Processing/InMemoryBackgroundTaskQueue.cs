// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Provides an in-memory implementation of a background task queue based on <see cref="Channel{T}"/>.
/// Suitable for single-instance deployments or testing environments.
/// </summary>
/// <remarks>
/// This implementation allows decoupling of task submission (enqueueing) from task execution (dequeueing),
/// facilitating asynchronous processing patterns within the application.
/// </remarks>
/// <seealso cref="IBackgroundTaskQueue"/>
/// <seealso cref="NucleusIngestionRequest"/>
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
public class InMemoryBackgroundTaskQueue : IBackgroundTaskQueue
{
    // Use NucleusIngestionRequest as the channel type
    private readonly Channel<NucleusIngestionRequest> _queue;
    private readonly ILogger<InMemoryBackgroundTaskQueue> _logger;

    // Consider making capacity configurable
    private const int QueueCapacity = 100;

    public InMemoryBackgroundTaskQueue(ILogger<InMemoryBackgroundTaskQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Bounded channel to prevent excessive memory usage if the worker falls behind.
        // Full mode determines behavior when capacity is reached (Wait = block, DropWrite = discard).
        var options = new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait // Wait for space if the queue is full
        };
        // Create channel with NucleusIngestionRequest
        _queue = Channel.CreateBounded<NucleusIngestionRequest>(options);
        _logger.LogInformation("In-memory background task queue initialized with capacity {Capacity}.", QueueCapacity);
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Attempt to write the item to the channel.
            // This will wait if the queue is full (due to BoundedChannelFullMode.Wait).
            await _queue.Writer.WriteAsync(request, cancellationToken);
            _logger.LogInformation("Queued interaction for async processing. ConversationId: {ConversationId}, MessageId: {MessageId}",
                request.OriginatingConversationId, request.OriginatingMessageId); // Use properties from NucleusIngestionRequest
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Queueing operation cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing background work item for ConversationId: {ConversationId}", request.OriginatingConversationId);
            // Consider re-throwing or handling differently based on requirements
            throw; 
        }
    }

    /// <inheritdoc />
    public async ValueTask<NucleusIngestionRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        // Wait until an item is available to read
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        _logger.LogInformation("Dequeued interaction for async processing. ConversationId: {ConversationId}, MessageId: {MessageId}",
             workItem.OriginatingConversationId, workItem.OriginatingMessageId);
        return workItem;
    }

    // Method for the background worker to check if items are available without waiting
    public bool TryPeek(out NucleusIngestionRequest? workItem)
    {
        return _queue.Reader.TryPeek(out workItem);
    }
}
