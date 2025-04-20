namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Manages the lifecycle and interaction salience for a specific type of Persona across its supported platforms.
/// Implementations are typically registered per Persona type.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public interface IPersonaManager
{
    /// <summary>
    /// Gets the canonical Persona Type ID that this manager is responsible for (e.g., "Professional_v1").
    /// Used by the InteractionRouter to potentially target requests.
    /// </summary>
    string ManagedPersonaTypeId { get; }

    /// <summary>
    /// Checks if the incoming interaction is salient (contextually relevant) to any *existing* active sessions
    /// managed by this instance.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SalienceCheckResult"/> indicating if the message was claimed and by which session.</returns>
    Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a new session should be initiated for the Persona type managed by this instance,
    /// based on the incoming interaction context and activation rules.
    /// If evaluation is positive, this method attempts the atomic claim (e.g., Cosmos DB create) to prevent duplicates.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="NewSessionEvaluationResult"/> indicating the outcome of the evaluation and claim attempt.</returns>
    Task<NewSessionEvaluationResult> EvaluateForNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default);

    // Note: Additional methods might be needed for managing session state internally,
    // but they might not need to be part of the public interface used by the InteractionRouter.
}
