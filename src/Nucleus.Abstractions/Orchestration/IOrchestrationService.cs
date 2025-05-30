// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Results; // Add this using directive

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the central service responsible for orchestrating the processing of incoming interactions.
/// This acts as the primary entry point into the Nucleus backend logic after the initial API reception.
/// It handles activation checks, determines processing mode (sync/async), resolves personas, and routes requests.
/// </summary>
/// <remarks>
/// This service embodies the core API-First principle of the Nucleus architecture.
/// </remarks>
/// <seealso cref="Models.AdapterRequest"/>
/// <seealso cref="Models.NucleusIngestionRequest"/>
/// <seealso cref="OrchestrationResult"/>
/// <seealso cref="../../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
/// <seealso cref="../../../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
/// <seealso cref="../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md"/>
/// <seealso cref="../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INGESTION.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#323-iorchestrationservicecs"/>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes an incoming interaction request, handling activation checks, routing, and initiating processing (sync or async).
    /// </summary>
    /// <param name="request">The adapter request containing interaction details.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, yielding a Result containing either an AdapterResponse on success or an OrchestrationError on failure.</returns>
    Task<Result<AdapterResponse, OrchestrationError>> ProcessInteractionAsync(
        AdapterRequest request,
        CancellationToken cancellationToken = default);

    // Removed HandleQueuedInteractionAsync as queue handling will be done by a dedicated service.
    /*
    /// <summary>
    /// Handles an interaction request that has been dequeued from the background task queue for asynchronous processing.
    /// This method bypasses the initial activation checks and sync/async decision logic.
    /// </summary>
    /// <param name="request">The NucleusIngestionRequest dequeued from the background task queue.</param>
    /// <param name="cancellationToken">Cancellation token passed down from the background service.</param>
    /// <returns>
    /// A Task representing the completion of the asynchronous processing, yielding an <see cref="OrchestrationResult"/>
    /// containing the outcome. The result might be null if processing fails critically before a result can be determined.
    /// The <see cref="AdapterResponse"/> within the result should contain the message intended for the end-user.
    /// </returns>
    Task<OrchestrationResult?> HandleQueuedInteractionAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);
    */
}
