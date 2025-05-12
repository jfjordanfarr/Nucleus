using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Adapters.Local
{
    /// <summary>
    /// Defines the contract for a local adapter client, enabling direct in-process interaction
    /// with the Nucleus API service or its core functionalities.
    /// This is intended for scenarios where Nucleus is used as a library or an embedded service
    /// rather than a standalone, network-accessed API.
    /// </summary>
    /// <remarks>
    /// See <seealso href="../../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md">Local Adapter Architecture</seealso> for architectural details.
    /// See <seealso href="../../../../../Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md">API Client Interaction Patterns</seealso> for general API interaction patterns.
    /// </remarks>
    public interface ILocalAdapterClient
    {
        /// <summary>
        /// Submits an interaction request directly to the Nucleus processing pipeline.
        /// </summary>
        /// <param name="request">The <see cref="AdapterRequest"/> containing the details of the interaction.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an
        /// <see cref="AdapterResponse"/> with the outcome of the interaction.
        /// If the interaction is processed asynchronously by the backend, the initial response
        /// might indicate acceptance (e.g., via JobId in <see cref="AdapterResponse.PlatformSpecificData"/>),
        /// and further polling or a callback mechanism (if designed) would be needed to get the final result.
        /// </returns>
        Task<AdapterResponse> SubmitInteractionAsync(AdapterRequest request);

        /// <summary>
        /// Persists the details of an interaction request for auditing purposes.
        /// This method should ensure that sensitive information is handled securely and logged appropriately.
        /// </summary>
        /// <param name="request">The <see cref="AdapterRequest"/> to persist for auditing.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PersistInteractionAsync(AdapterRequest request, System.Threading.CancellationToken cancellationToken = default);
    }
}
