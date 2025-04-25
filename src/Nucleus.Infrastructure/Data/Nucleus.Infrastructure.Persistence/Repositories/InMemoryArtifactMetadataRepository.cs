// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Repositories;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Data.Persistence.Repositories;

/// <summary>
/// Simple in-memory implementation of <see cref="IArtifactMetadataRepository"/> for testing and development.
/// </summary>
public class InMemoryArtifactMetadataRepository : IArtifactMetadataRepository
{
    // NOTE: PartitionKey is ignored in this in-memory implementation.
    private readonly ConcurrentDictionary<string, ArtifactMetadata> _store = new();

    /// <inheritdoc />
    public Task<ArtifactMetadata> SaveAsync(ArtifactMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (string.IsNullOrWhiteSpace(metadata.Id))
        {
            metadata = metadata with { Id = Guid.NewGuid().ToString() }; // Generate ID if missing
        }

        // Simulate timestamp update
        var now = DateTimeOffset.UtcNow;
        // CreatedAtNucleus is set by default in the record; only update ModifiedAtNucleus.
        metadata = metadata with { ModifiedAtNucleus = now };

        _store[metadata.Id] = metadata; // Add or update
        return Task.FromResult(metadata); // Return the potentially updated metadata
    }

    /// <inheritdoc />
    public Task<ArtifactMetadata?> GetByIdAsync(string id, string partitionKey)
    {
        // Partition key is ignored in this implementation
        _store.TryGetValue(id, out var metadata);
        return Task.FromResult(metadata);
    }

    /// <inheritdoc />
    public Task<ArtifactMetadata?> GetBySourceIdentifierAsync(string sourceIdentifier)
    {
        // This is inefficient for a real database, but okay for in-memory.
        // Assumes SourceIdentifier is unique enough for testing purposes.
        var metadata = _store.Values.FirstOrDefault(m => m.SourceIdentifier == sourceIdentifier);
        return Task.FromResult(metadata);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, string partitionKey)
    {
        // Partition key is ignored in this implementation
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
