using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Defines the contract for resolving a canonical Persona ID based on
/// platform-specific identifiers provided in an incoming request.
/// <seealso cref="../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </summary>
/// <remarks>
/// See architecture doc: [ARCHITECTURE_ORCHESTRATION_PERSONA_RESOLUTION.md](../../../../Docs/Architecture/Orchestration/ARCHITECTURE_ORCHESTRATION_PERSONA_RESOLUTION.md)
/// See: ../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md
/// See: ../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// </remarks>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#324-ipersonaresolvercs"/>
public interface IPersonaResolver
{
    /// <summary>
    /// Resolves the canonical Persona ID for the given request context.
    /// </summary>
    /// <param name="platformType">The source platform of the request.</param>
    /// <param name="request">The original request data containing platform identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved Persona ID (string) or null if resolution fails.</returns>
    /// <seealso href="../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md">Interaction Processing Lifecycle - Persona Resolution</seealso>
    Task<string?> ResolvePersonaIdAsync(PlatformType platformType, AdapterRequest request, CancellationToken cancellationToken = default);
}
