using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the context of a single interaction being processed by the Nucleus system.
/// It encapsulates the original request and resolved metadata necessary for downstream processing.
/// </summary>
/// <remarks>
/// This context is typically created by the OrchestrationService after an interaction passes activation checks.
/// </remarks>
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md"/>
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
    /// The extracted content from any artifacts associated with the original request.
    /// This list will be empty if no artifacts were present or if extraction failed for all of them.
    /// </summary>
    public IReadOnlyList<ExtractedArtifact> ExtractedContents { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionContext"/> class.
    /// </summary>
    /// <param name="originalRequest">The original adapter request.</param>
    /// <param name="platformType">The source platform of the interaction.</param>
    /// <param name="resolvedPersonaId">The resolved canonical Persona ID (string, can be null).</param>
    /// <param name="extractedContents">The results of content extraction for associated artifacts.</param>
    public InteractionContext(
        AdapterRequest originalRequest, 
        PlatformType platformType, 
        string? resolvedPersonaId,
        IReadOnlyList<ExtractedArtifact> extractedContents)
    {
        OriginalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
        PlatformType = platformType;
        ResolvedPersonaId = resolvedPersonaId;
        ExtractedContents = extractedContents ?? throw new ArgumentNullException(nameof(extractedContents));
    }
}
