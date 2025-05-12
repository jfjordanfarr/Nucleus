using Nucleus.Abstractions.Models;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Repositories;

/// <summary>
/// Defines the contract for storing and retrieving artifact metadata.
/// This metadata includes information about artifacts processed by Nucleus, 
/// but crucially *not* the artifact content itself.
/// <seealso cref="Models.ArtifactMetadata"/>
/// <seealso cref="../../../Docs/Architecture/Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md"/>
/// <seealso cref="../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// <seealso cref="../../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
/// <seealso cref="../../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md"/>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#331-iartifactmetadatarepositorycs"/>
/// </summary>
public interface IArtifactMetadataRepository
{
    /// <summary>
    /// Retrieves a single ArtifactMetadata record by its unique Nucleus ID.
    /// </summary>
    /// <param name="id">The unique ID of the metadata record.</param>
    /// <param name="partitionKey">The partition key (e.g., tenantId or userId) required for the Cosmos DB query.</param>
    /// <returns>The found ArtifactMetadata record, or null if not found.</returns>
    Task<ArtifactMetadata?> GetByIdAsync(string id, string partitionKey);

    /// <summary>
    /// Retrieves a single ArtifactMetadata record by its logical source identifier.
    /// Note: Implementations should handle potential partition key scans if the source identifier alone isn't sufficient for partitioning.
    /// </summary>
    /// <param name="sourceIdentifier">The logical source identifier of the artifact.</param>
    /// <returns>The found ArtifactMetadata record, or null if not found.</returns>
    Task<ArtifactMetadata?> GetBySourceIdentifierAsync(string sourceIdentifier);

    /// <summary>
    /// Creates or updates an ArtifactMetadata record in the repository.
    /// Implementations should handle setting ModifiedAtNucleus timestamps.
    /// </summary>
    /// <param name="metadata">The ArtifactMetadata record to save.</param>
    /// <returns>The saved ArtifactMetadata record, potentially updated with new timestamps or an ID if newly created.</returns>
    Task<ArtifactMetadata> SaveAsync(ArtifactMetadata metadata);

    /// <summary>
    /// Deletes an ArtifactMetadata record by its unique Nucleus ID.
    /// </summary>
    /// <param name="id">The unique ID of the metadata record to delete.</param>
    /// <param name="partitionKey">The partition key (e.g., tenantId or userId) required for the Cosmos DB delete operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string id, string partitionKey);
}
