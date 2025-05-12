// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the contract for a background task queue used to process interactions asynchronously.
/// Ensures that long-running tasks do not block the primary API request threads.
/// </summary>
/// <seealso href="../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md">Processing Orchestration Overview</seealso>
/// <seealso href="../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md">API Activation & Asynchronous Routing</seealso>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#322-ibackgroundtaskqueuecs"/>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a NucleusIngestionRequest for background processing.
    /// </summary>
    /// <param name="request">The ingestion request to process asynchronously.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to dequeue a work item from the background queue.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a
    /// <see cref="DequeuedMessage{NucleusIngestionRequest}"/> if a work item was available, or null if the queue was empty or the operation was cancelled.
    /// The <see cref="DequeuedMessage{T}"/> contains the payload and the necessary context to complete or abandon the message.
    /// </returns>
    Task<DequeuedMessage<NucleusIngestionRequest>?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a dequeued message as successfully processed.
    /// </summary>
    /// <param name="messageContext">The message context object obtained from <see cref="DequeueAsync"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageContext"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="messageContext"/> is not of the expected type for the underlying implementation.</exception>
    /// <remarks>
    /// Implementations should handle potential transient errors during completion and log appropriately.
    /// </remarks>
    Task CompleteAsync(object messageContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandons a dequeued message, making it available for reprocessing.
    /// Optionally logs associated exception information.
    /// </summary>
    /// <param name="messageContext">The message context object obtained from <see cref="DequeueAsync"/>.</param>
    /// <param name="exception">Optional exception that caused the processing failure, used for logging or dead-letter reason.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageContext"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="messageContext"/> is not of the expected type for the underlying implementation.</exception>
    /// <remarks>
    /// Implementations should handle potential transient errors during abandonment and log appropriately.
    /// Depending on the broker's configuration, abandoned messages might eventually be dead-lettered after sufficient retries.
    /// </remarks>
    Task AbandonAsync(object messageContext, Exception? exception = null, CancellationToken cancellationToken = default);
}
