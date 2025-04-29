// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the possible outcomes of the orchestration process for an interaction.
/// </summary>
public enum OrchestrationStatus
{
    /// <summary>
    /// The interaction was successfully processed synchronously, and the response is available immediately.
    /// </summary>
    ProcessedSync,
    /// <summary>
    /// The interaction was accepted and queued for asynchronous processing.
    /// </summary>
    AcceptedAsync,
    /// <summary>
    /// The interaction was received but did not meet the criteria for activation and was not processed further.
    /// </summary>
    NotActivated,
    /// <summary>
    /// An error occurred during the orchestration process.
    /// </summary>
    Error
}

/// <summary>
/// Encapsulates the result of the orchestration process, including the status and any resulting response.
/// </summary>
/// <param name="Status">The final status of the orchestration attempt.</param>
/// <param name="Response">The AdapterResponse generated, if applicable (e.g., for sync processing or error details).</param>
public record OrchestrationResult(OrchestrationStatus Status, AdapterResponse? Response);

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
public interface IOrchestrationService
{
    /// <summary>
    /// Processes an incoming interaction request, handling activation checks, routing, and initiating processing (sync or async).
    /// </summary>
    /// <param name="request">The adapter request containing interaction details.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, yielding an OrchestrationResult.</returns>
    Task<OrchestrationResult> ProcessInteractionAsync(
        AdapterRequest request,
        CancellationToken cancellationToken = default);

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
}
