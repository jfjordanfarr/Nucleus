using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Manages the lifecycle and interaction processing for a specific type of Persona.
/// Responsible for determining if an interaction belongs to an existing session (salience)
/// and if a new session should be started for this persona type.
/// </summary>
public interface IPersonaManager
{
    /// <summary>
    /// Gets the unique identifier for the type of Persona managed by this instance (e.g., "MeetingCopilot_v1").
    /// </summary>
    string ManagedPersonaTypeId { get; }

    /// <summary>
    /// Checks if the incoming interaction is relevant (salient) to any *active* sessions managed by this persona manager.
    /// </summary>
    /// <param name="context">The context of the interaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the interaction is salient and, if so, which session it belongs to.</returns>
    Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a new session should be initiated by this persona manager for the given interaction,
    /// assuming no existing session claimed it during the salience check phase.
    /// </summary>
    /// <param name="context">The context of the interaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether a new session should be initiated.</returns>
    Task<NewSessionEvaluationResult> EvaluateForNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an interaction that has been determined to be salient to an existing session.
    /// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
    /// </summary>
    /// <param name="context">The context of the interaction.</param>
    /// <param name="sessionId">The ID of the existing session the interaction belongs to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessSalientInteractionAsync(InteractionContext context, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a new session based on the interaction context and performs initial processing.
    /// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
    /// </summary>
    /// <param name="context">The context of the interaction triggering session initiation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous session initiation and processing operation. May contain information about the newly created session if applicable.</returns>
    Task InitiateNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default);
}
