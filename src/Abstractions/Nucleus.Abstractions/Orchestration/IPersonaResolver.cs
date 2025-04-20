using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Resolves platform-specific identifiers to a canonical Nucleus Persona ID.
/// </summary>
public interface IPersonaResolver
{
    /// <summary>
    /// Attempts to resolve the sender's identity from an incoming request to a canonical Persona ID.
    /// </summary>
    /// <param name="request">The incoming adapter request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canonical Persona ID (string) if resolved; otherwise, null.</returns>
    Task<string?> ResolvePersonaAsync(AdapterRequest request, CancellationToken cancellationToken = default);
}
