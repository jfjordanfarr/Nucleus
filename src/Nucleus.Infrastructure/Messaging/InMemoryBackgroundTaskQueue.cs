using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Messaging
{
    /// <summary>
    /// An in-memory implementation of <see cref="IBackgroundTaskQueue"/> for local development and testing.
    /// </summary>
    public class InMemoryBackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ILogger<InMemoryBackgroundTaskQueue> _logger;
        private readonly ConcurrentQueue<NucleusIngestionRequest> _workItems = new();
        // Consider a separate queue for dead-lettered items if complex retry/abandon logic is needed.
        // private readonly ConcurrentQueue<(NucleusIngestionRequest item, Exception? lastException)> _deadLetterQueue = new();

        public InMemoryBackgroundTaskQueue(ILogger<InMemoryBackgroundTaskQueue> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("[InMemoryQueue] Initialized.");
        }

        /// <inheritdoc />
        public ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            _workItems.Enqueue(request);
            _logger.LogDebug("[InMemoryQueue] Queued work item with CorrelationId: {CorrelationId}", (request.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", ""));
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public Task<DequeuedMessage<NucleusIngestionRequest>?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_workItems.TryDequeue(out var workItem))
            {
                _logger.LogDebug("[InMemoryQueue] Dequeued work item with CorrelationId: {CorrelationId}", (workItem.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", ""));
                // For the in-memory queue, the workItem itself can serve as the MessageContext.
                // If more complex state per dequeued message is needed (e.g., delivery count),
                // a wrapper object could be used here and for the ConcurrentQueue's generic type.
                return Task.FromResult<DequeuedMessage<NucleusIngestionRequest>?>(new DequeuedMessage<NucleusIngestionRequest>(workItem, workItem));
            }

            _logger.LogTrace("[InMemoryQueue] Queue is empty. No work item dequeued.");
            return Task.FromResult<DequeuedMessage<NucleusIngestionRequest>?>(null);
        }

        /// <inheritdoc />
        public Task CompleteAsync(object messageContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messageContext);
            cancellationToken.ThrowIfCancellationRequested();

            if (messageContext is NucleusIngestionRequest workItem)
            {
                _logger.LogDebug("[InMemoryQueue] Marking work item as complete with CorrelationId: {CorrelationId}", (workItem.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", ""));
                // In a simple concurrent queue, dequeuing effectively removes it.
                // If we needed to track active messages, we'd remove it from an "active messages" collection here.
                // For now, this is largely a NOP other than logging for the in-memory version.
            }
            else
            {
                _logger.LogWarning("[InMemoryQueue] CompleteAsync called with unexpected messageContext type: {ContextType}. Expected {ExpectedType}.", 
                    (messageContext.GetType().FullName ?? "N/A").Replace("\n", "").Replace("\r", ""), 
                    (typeof(NucleusIngestionRequest).FullName ?? "N/A").Replace("\n", "").Replace("\r", ""));
                // Consider throwing ArgumentException if strict type checking is crucial for robust error handling by the consumer.
                // throw new ArgumentException($"Invalid messageContext type: {messageContext.GetType().FullName}. Expected {typeof(NucleusIngestionRequest).FullName}.", nameof(messageContext));
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task AbandonAsync(object messageContext, Exception? exception = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messageContext);
            cancellationToken.ThrowIfCancellationRequested();

            if (messageContext is NucleusIngestionRequest workItem)
            {
                // Basic abandon: re-queue the item.
                // For more sophisticated handling, consider: 
                // 1. Max delivery/retry count (would need to store this, perhaps by wrapping NucleusIngestionRequest in another class in the queue, and using that as the MessageContext).
                // 2. Moving to a dead-letter queue after N attempts (e.g., _deadLetterQueue.Enqueue((workItem, exception));).
                // 3. Exponential backoff (would require delaying re-queueing, more complex for a simple in-memory queue).
                _logger.LogWarning(exception, "[InMemoryQueue] Abandoning work item with CorrelationId: {CorrelationId}. Re-queueing.", (workItem.CorrelationId ?? "N/A").Replace("\n", "").Replace("\r", ""));
                _workItems.Enqueue(workItem); // Simple re-queue
            }
            else
            {
                _logger.LogWarning("[InMemoryQueue] AbandonAsync called with unexpected messageContext type: {ContextType}. Expected {ExpectedType}. Work item not re-queued.", 
                    (messageContext.GetType().FullName ?? "N/A").Replace("\n", "").Replace("\r", ""), 
                    (typeof(NucleusIngestionRequest).FullName ?? "N/A").Replace("\n", "").Replace("\r", ""));
                // Consider throwing ArgumentException for similar reasons as in CompleteAsync.
                // throw new ArgumentException($"Invalid messageContext type: {messageContext.GetType().FullName}. Expected {typeof(NucleusIngestionRequest).FullName}.", nameof(messageContext));
            }
            return Task.CompletedTask;
        }
    }
}
