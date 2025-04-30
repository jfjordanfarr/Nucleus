using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.IntegrationTests.Infrastructure;

/// <summary>
/// A null implementation of IArtifactMetadataRepository for use in integration tests
/// where database interaction is not desired or available.
/// </summary>
public class NullArtifactMetadataRepository : IArtifactMetadataRepository
{
    /// <summary>
    /// Tracks the number of times UpsertAsync was called.
    /// Reset this before tests that need to check it.
    /// </summary>
    private int _upsertAsyncCalledCount = 0;

    public int UpsertAsyncCalledCount => _upsertAsyncCalledCount;

    /// <summary>
    /// Resets the counter for UpsertAsync calls.
    /// </summary>
    public void Reset()
    {
        _upsertAsyncCalledCount = 0;
    }

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

    /// <summary>
    /// Gets all artifact metadata asynchronously. Returns an empty list for the null implementation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An empty list of artifact metadata.</returns>
    public static Task<IEnumerable<ArtifactMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<ArtifactMetadata>());
    }

    public Task UpsertAsync(ArtifactMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Interlocked.Increment(ref _upsertAsyncCalledCount); // Use Interlocked for potential future parallel tests
        // No actual persistence
        return Task.CompletedTask;
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
