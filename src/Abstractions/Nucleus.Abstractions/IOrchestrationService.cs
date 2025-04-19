// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines the contract for the central service responsible for orchestrating
/// the processing of incoming ingestion requests.
/// This includes session management, persona selection, routing, and invoking specific processing steps.
/// See: ../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md
/// See: ../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// See: ../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md
/// </summary>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes a standardized ingestion request received from a platform adapter.
    /// </summary>
    /// <param name="request">The details of the ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessIngestionRequestAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);
}
