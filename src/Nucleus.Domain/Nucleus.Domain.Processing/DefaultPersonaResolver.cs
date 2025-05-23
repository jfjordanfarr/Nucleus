using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Default implementation that maps a fixed set of external IDs to a specific persona.
/// TODO: Make this configurable.
/// Returns a hardcoded default Persona ID if no specific resolution logic applies.
/// <seealso cref="../../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md#defaultpersonaresolver-class">Nucleus Processing Architecture - DefaultPersonaResolver</seealso>
/// </summary>
/// <remarks>
/// Corresponds to: [IPersonaResolver](../../../Abstractions/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs)
/// See architecture doc: [ARCHITECTURE_ORCHESTRATION_PERSONA_RESOLUTION.md](../../../../../Docs/Architecture/Orchestration/ARCHITECTURE_ORCHESTRATION_PERSONA_RESOLUTION.md)
/// See: ../../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_IMPLEMENTATIONS.md
/// <seealso cref="IPersonaResolver"/>
/// <seealso cref="../../../../Docs/Architecture/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </remarks>
/// <summary>
/// Provides a default, simplistic implementation of <see cref="IPersonaResolver"/>.
/// This implementation currently returns a hardcoded default Persona ID, suitable for initial development or single-persona deployments.
/// </summary>
/// <remarks>
/// See <see cref="IPersonaResolver"/> for more details on the role of persona resolution.
/// This resolver is part of the [Processing Architecture](../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md).
/// For context on what a "Persona" is, see [Persona Architecture](../../../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md).
/// In a multi-persona or more dynamic environment, a more sophisticated implementation would be required,
/// potentially looking up mappings in a configuration source or database.
/// </remarks>
public class DefaultPersonaResolver : IPersonaResolver
{
    private readonly ILogger<DefaultPersonaResolver> _logger;
    private const string DefaultPersonaId = "Default_v1"; // Define the default ID

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPersonaResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <seealso href="d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md">Interaction Processing Lifecycle - Persona Resolution</seealso>
    public DefaultPersonaResolver(ILogger<DefaultPersonaResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    // Reverting implementation to non-FQN, keeping interface as FQN.
    public Task<string?> ResolvePersonaIdAsync(PlatformType platformType, AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Log the attempt using request properties
        // Note: CorrelationId, SessionId, TurnId are part of NucleusIngestionRequest, not AdapterRequest passed here.
        _logger.LogDebug("DefaultPersonaResolver resolving persona for Platform: {Platform}, User: {User}, Conversation: {Conversation}, MessageId: {MessageId}.",
            platformType,      // Use platformType parameter
            request.UserId,    // Corrected from OriginatingUserId
            request.ConversationId, // Corrected from OriginatingConversationId
            request.MessageId ?? "N/A"); // Added MessageId for context, removed CorrelationId

        // TODO: Implement actual logic here based on platformType, request properties, etc.
        // For now, always return the hardcoded default.

        _logger.LogInformation("DefaultPersonaResolver returning default Persona ID: {DefaultPersonaId}", DefaultPersonaId);
        return Task.FromResult<string?>(DefaultPersonaId);
    }
}
