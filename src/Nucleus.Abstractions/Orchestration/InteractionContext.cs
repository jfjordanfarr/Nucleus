using Nucleus.Abstractions.Models;
using System;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the context of a specific interaction after initial processing,
/// including the original request, the resolved persona, and platform type.
/// </summary>
public class InteractionContext
{
    /// <summary>
    /// The original request received from the adapter.
    /// </summary>
    public AdapterRequest OriginalRequest { get; }

    /// <summary>
    /// The source platform of the interaction.
    /// </summary>
    public PlatformType PlatformType { get; }

    /// <summary>
    /// The resolved canonical Persona ID (string) for the sender, if available.
    /// This might be null if the resolver couldn't identify the sender.
    /// </summary>
    public string? ResolvedPersonaId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionContext"/> class.
    /// </summary>
    /// <param name="originalRequest">The original adapter request.</param>
    /// <param name="platformType">The source platform of the interaction.</param>
    /// <param name="resolvedPersonaId">The resolved canonical Persona ID (string, can be null).</param>
    public InteractionContext(AdapterRequest originalRequest, PlatformType platformType, string? resolvedPersonaId)
    {
        OriginalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
        PlatformType = platformType;
        ResolvedPersonaId = resolvedPersonaId;
    }
}
