using Nucleus.Abstractions.Models;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Repositories;

/// <summary>
/// Defines the contract for a repository responsible for managing ArtifactMetadata records.
/// Implementations will typically interact with the ArtifactMetadataContainer in Cosmos DB.
/// See: ../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md#8-next-steps
/// See: ../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md#7-next-steps
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
