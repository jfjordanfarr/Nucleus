namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Service responsible for mapping platform-specific identifiers to canonical Nucleus Persona IDs.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public interface IPersonaResolver
{
    /// <summary>
    /// Attempts to resolve the canonical Nucleus Persona ID based on the provided interaction context.
    /// </summary>
    /// <param name="context">The interaction context containing platform type and identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canonical Persona ID if resolved; otherwise, null.</returns>
    Task<string?> ResolvePersonaIdAsync(InteractionContext context, CancellationToken cancellationToken = default);
}
