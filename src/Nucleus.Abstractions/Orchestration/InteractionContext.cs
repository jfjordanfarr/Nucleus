using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the context of a single interaction being processed by the Nucleus system.
/// It encapsulates the original request and resolved metadata necessary for downstream processing.
/// </summary>
/// <remarks>
/// This context is typically created by the OrchestrationService after an interaction passes activation checks,
/// and is enriched by the QueuedInteractionProcessorService with fetched artifact data.
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
    /// This ID is crucial for scoping all subsequent operations as described in session initiation.
    /// </summary>
    /// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md">Orchestration Session Initiation - Context Creation</seealso>
    public string? ResolvedPersonaId { get; }

    /// <summary>
    /// The raw content of any artifacts associated with the original request, fetched by <see cref="IArtifactProvider"/>s.
    /// This list contains the artifacts before content extraction.
    /// </summary>
    public IReadOnlyList<ArtifactContent> RawArtifacts { get; }

    /// <summary>
    /// The processed results from artifact fetching and content extraction for artifacts associated with the original request.
    /// This list will be empty if no artifacts were present or if processing failed for all of them.
    /// </summary>
    public IReadOnlyList<ExtractedArtifact> ProcessedArtifacts { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionContext"/> class.
    /// </summary>
    /// <param name="originalRequest">The original adapter request.</param>
    /// <param name="platformType">The source platform of the interaction.</param>
    /// <param name="resolvedPersonaId">The resolved canonical Persona ID (string, can be null).</param>
    /// <param name="rawArtifacts">The raw content of artifacts fetched by providers.</param>
    /// <param name="processedArtifacts">The results of content extraction for associated artifacts.</param>
    /// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md">Orchestration Session Initiation - Context Creation</seealso>
    public InteractionContext(
        AdapterRequest originalRequest, 
        PlatformType platformType, 
        string? resolvedPersonaId,
        IReadOnlyList<ArtifactContent> rawArtifacts, 
        IReadOnlyList<ExtractedArtifact> processedArtifacts)
    {
        OriginalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
        PlatformType = platformType;
        ResolvedPersonaId = resolvedPersonaId;
        RawArtifacts = rawArtifacts ?? throw new ArgumentNullException(nameof(rawArtifacts));
        ProcessedArtifacts = processedArtifacts ?? throw new ArgumentNullException(nameof(processedArtifacts));
    }
}
