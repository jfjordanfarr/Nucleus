using Nucleus.Abstractions.Models.Client; // Assuming NucleusIngestionRequest is here or similar
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the context of an incoming interaction to be processed.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public class InteractionContext
{
    /// <summary>
    /// The original request that triggered this interaction.
    /// </summary>
    public NucleusIngestionRequest OriginalRequest { get; init; } = null!;

    /// <summary>
    /// The platform type from which the interaction originated.
    /// </summary>
    public string PlatformType => OriginalRequest.PlatformType;

    /// <summary>
    /// Platform-specific identifiers (e.g., User ID, Conversation ID).
    /// Extracted from OriginalRequest.PlatformContext.
    /// </summary>
    public IReadOnlyDictionary<string, string> PlatformIdentifiers { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// The URI pointing to the source content, if applicable (e.g., file upload).
    /// </summary>
    public Uri? ContentSourceUri => OriginalRequest.ContentSourceUri;

    /// <summary>
    /// The message content (e.g., text query).
    /// </summary>
    public string MessageContent => OriginalRequest.MessageContent ?? string.Empty;

    /// <summary>
    /// The resolved canonical Nucleus Persona ID, if available.
    /// Set by the IPersonaResolver.
    /// </summary>
    public string? CanonicalPersonaId { get; set; }

    // Constructor or factory method might be useful here to parse PlatformIdentifiers from PlatformContext
    public InteractionContext(NucleusIngestionRequest request)
    {
        OriginalRequest = request;
        // TODO: Implement logic to parse OriginalRequest.PlatformContext into PlatformIdentifiers if needed
        // For now, assume PlatformContext is directly usable or identifiers are passed separately.
        // Example placeholder:
        if (request.PlatformContext != null)
        {
            // This is a simplified example; actual parsing logic depends on PlatformContext structure
            // PlatformIdentifiers = request.PlatformContext.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
        }
    }
}
