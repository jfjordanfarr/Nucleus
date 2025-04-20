namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the result of evaluating whether a new session should be initiated by an IPersonaManager.
/// </summary>
public class NewSessionEvaluationResult
{
    /// <summary>
    /// Gets a value indicating whether a new session should be initiated.
    /// </summary>
    public bool ShouldInitiate { get; private set; }

    /// <summary>
    /// Gets the ID of the newly initiated session, if applicable.
    /// This might be null even if ShouldInitiate is true if the initiation process
    /// happens asynchronously or the ID is determined later.
    /// </summary>
    public string? NewSessionId { get; private set; }

    // Private constructor to force use of factory methods
    private NewSessionEvaluationResult() { }

    /// <summary>
    /// Creates a result indicating a new session should be initiated.
    /// </summary>
    /// <param name="newSessionId">Optional: The ID of the newly created session, if known immediately.</param>
    /// <returns>A new NewSessionEvaluationResult instance.</returns>
    public static NewSessionEvaluationResult InitiateNewSession(string? newSessionId = null)
    {
        return new NewSessionEvaluationResult { ShouldInitiate = true, NewSessionId = newSessionId };
    }

    /// <summary>
    /// Creates a result indicating a new session should not be initiated.
    /// </summary>
    /// <returns>A new NewSessionEvaluationResult instance.</returns>
    public static NewSessionEvaluationResult DoNotInitiate()
    {
        return new NewSessionEvaluationResult { ShouldInitiate = false, NewSessionId = null };
    }
}
