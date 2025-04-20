namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the result of a salience check against existing sessions by an IPersonaManager.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// </summary>
public class SalienceCheckResult
{
    /// <summary>
    /// Indicates whether the message was claimed by an existing session managed by this manager.
    /// </summary>
    public bool IsSalient { get; init; }

    /// <summary>
    /// If IsSalient is true, the ID of the session that claimed the message.
    /// </summary>
    public string? ClaimingSessionId { get; init; }

    /// <summary>
    /// Creates a result indicating the message was not salient to any existing session.
    /// </summary>
    public static SalienceCheckResult NotSalient() => new() { IsSalient = false };

    /// <summary>
    /// Creates a result indicating the message was salient and claimed by a specific session.
    /// </summary>
    /// <param name="sessionId">The ID of the claiming session.</param>
    public static SalienceCheckResult Salient(string sessionId) => new() { IsSalient = true, ClaimingSessionId = sessionId };
}
