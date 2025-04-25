// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the contract for a service that queues work items (like NucleusIngestionRequest)
/// for asynchronous background processing.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a NucleusIngestionRequest for background processing.
    /// </summary>
    /// <param name="request">The ingestion request to process asynchronously.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task representing the queuing operation. May potentially return a Job ID in the future.</returns>
    ValueTask QueueBackgroundWorkItemAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to dequeue a work item for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dequeued NucleusIngestionRequest.</returns>
    ValueTask<NucleusIngestionRequest> DequeueAsync(CancellationToken cancellationToken);
}
