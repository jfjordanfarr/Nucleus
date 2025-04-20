namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the result of evaluating whether an IPersonaManager should initiate a new session for an interaction.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public class NewSessionEvaluationResult
{
    /// <summary>
    /// Indicates whether the manager has decided to attempt initiating a new session.
    /// </summary>
    public bool ShouldInitiate { get; init; }

    /// <summary>
    /// If ShouldInitiate is true, indicates whether the attempt to claim the session
    /// (e.g., via atomic database operation) was successful.
    /// </summary>
    public bool ClaimSuccessful { get; init; }

    /// <summary>
    /// If initiation was attempted and successful, the ID of the newly created session.
    /// </summary>
    public string? CreatedSessionId { get; init; }

    /// <summary>
    /// Creates a result indicating the manager decided not to initiate a new session.
    /// </summary>
    public static NewSessionEvaluationResult DoNotInitiate() => new() { ShouldInitiate = false };

    /// <summary>
    /// Creates a result indicating the manager attempted initiation, but the claim failed (e.g., conflict).
    /// </summary>
    public static NewSessionEvaluationResult InitiationAttemptedClaimFailed() => new() { ShouldInitiate = true, ClaimSuccessful = false };

    /// <summary>
    /// Creates a result indicating the manager successfully initiated and claimed a new session.
    /// </summary>
    /// <param name="sessionId">The ID of the newly created session.</param>
    public static NewSessionEvaluationResult InitiationSuccessful(string sessionId) => new() { ShouldInitiate = true, ClaimSuccessful = true, CreatedSessionId = sessionId };
}
