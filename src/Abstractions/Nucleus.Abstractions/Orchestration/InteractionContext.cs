using Nucleus.Abstractions.Models;
using System;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Encapsulates the context for an interaction being processed by the OrchestrationService.
/// </summary>
public class InteractionContext
{
    /// <summary>
    /// The original request received from the adapter.
    /// </summary>
    public AdapterRequest OriginalRequest { get; }

    /// <summary>
    /// The resolved canonical Persona ID (string) for the sender, if available.
    /// This might be null if the resolver couldn't identify the sender.
    /// </summary>
    public string? ResolvedPersonaId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionContext"/> class.
    /// </summary>
    /// <param name="originalRequest">The original adapter request.</param>
    /// <param name="resolvedPersonaId">The resolved canonical Persona ID (string, can be null).</param>
    public InteractionContext(AdapterRequest originalRequest, string? resolvedPersonaId)
    {
        OriginalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
        ResolvedPersonaId = resolvedPersonaId;
    }
}
