using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.IntegrationTests.Infrastructure;

/// <summary>
/// A null implementation of IArtifactMetadataRepository for use in integration tests
/// where database interaction is not desired or available.
/// </summary>
public class NullArtifactMetadataRepository : IArtifactMetadataRepository
{
    public Task<ArtifactMetadata?> GetByIdAsync(string id, string partitionKey)
    {
        // Always return null as if the item doesn't exist
        return Task.FromResult<ArtifactMetadata?>(null);
    }

    // Corrected signature to match interface
    public Task<ArtifactMetadata?> GetBySourceIdentifierAsync(string sourceIdentifier)
    {
        // Always return null as if the item doesn't exist
        return Task.FromResult<ArtifactMetadata?>(null);
    }

    public Task<IEnumerable<ArtifactMetadata>> GetAllAsync(string partitionKey)
    {
        // Always return an empty list
        return Task.FromResult(Enumerable.Empty<ArtifactMetadata>());
    }

    public Task<ArtifactMetadata> SaveAsync(ArtifactMetadata metadata)
    {
        // Return the metadata as if it was saved, but do nothing
        // Ensure required properties like Id are handled if needed for subsequent test steps
        // For simplicity, we just return the input metadata.
        return Task.FromResult(metadata);
    }

    public Task DeleteAsync(string id, string partitionKey)
    {
        // Do nothing, just return a completed task
        return Task.CompletedTask;
    }
}
