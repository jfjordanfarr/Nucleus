// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Manages the lifecycle and interaction processing for a specific type of Persona.
/// <seealso cref="../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </summary>
public interface IPersonaManager
{
    /// <summary>
    /// Gets the unique identifier for the type of Persona managed by this instance (e.g., "MeetingCopilot_v1").
    /// </summary>
    string ManagedPersonaTypeId { get; }

    /// <summary>
    /// Gets the configuration settings associated with this specific persona instance.
    /// </summary>
    PersonaConfiguration Configuration { get; }

    /// <summary>
    /// Checks if the incoming interaction is relevant (salient) to any *active* sessions managed by this persona manager.
    /// </summary>
    /// <param name="context">The context of the interaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the interaction is salient and, if so, which session it belongs to.</returns>
    Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a new session based on the interaction context and performs initial processing.
    /// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
    /// </summary>
    /// <param name="context">The context of the interaction triggering session initiation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous session initiation and processing operation, yielding an AdapterResponse.</returns>
    Task<AdapterResponse> InitiateNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default);
}
