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
}
