using System;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the result of a salience check performed by an IPersonaManager.
/// </summary>
public class SalienceCheckResult
{
    /// <summary>
    /// Gets a value indicating whether the interaction was deemed salient to an existing session.
    /// </summary>
    public bool IsSalient { get; private set; }

    /// <summary>
    /// Gets the ID of the session to which the interaction is salient, if applicable.
    /// </summary>
    public string? SessionId { get; private set; }

    // Private constructor to force use of factory methods
    private SalienceCheckResult() { }

    /// <summary>
    /// Creates a result indicating the interaction is salient to the specified session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to which the interaction is salient.</param>
    /// <returns>A new SalienceCheckResult instance.</returns>
    public static SalienceCheckResult Salient(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or whitespace when interaction is salient.", nameof(sessionId));
        }
        return new SalienceCheckResult { IsSalient = true, SessionId = sessionId };
    }

    /// <summary>
    /// Creates a result indicating the interaction is not salient to any existing session.
    /// </summary>
    /// <returns>A new SalienceCheckResult instance.</returns>
    public static SalienceCheckResult NotSalient()
    {
        return new SalienceCheckResult { IsSalient = false, SessionId = null };
    }
}
