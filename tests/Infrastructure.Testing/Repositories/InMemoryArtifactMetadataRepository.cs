// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Testing.Repositories; // Updated Namespace

// USER TODO (2025-05-07): This class appears to be deprecated and largely unused in favor of emulator-backed repositories.
// Consider if this file is still necessary or if it can be removed to simplify the testing infrastructure.
/// <summary>
/// In-memory implementation of the <see cref="IArtifactMetadataRepository"/>.
/// THIS IS INTENTIONALLY LEFT EMPTY AND SHOULD NOT BE USED.
/// Rely on emulators (e.g., CosmosDB Emulator) provided by Aspire for integration testing.
/// </summary>
/// <remarks>
/// This class is kept temporarily to satisfy type resolution during the transition phase.
/// It does not implement the required interface members and will cause build errors
/// until either removed completely or updated.
/// See: [Docs/Architecture/04_ARCHITECTURE_DATABASE.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/04_ARCHITECTURE_DATABASE.md:0:0-0:0)
/// </remarks>
/// <seealso href="../../../../Docs/Architecture/09_ARCHITECTURE_TESTING.md" />
public class InMemoryArtifactMetadataRepository : IArtifactMetadataRepository
{
    // Intentionally left empty. Implementation removed as part of the shift
    // towards using emulators (CosmosDB) for integration tests.
    // This class will likely be removed entirely once integration tests are fully
    // refactored to use Aspire's emulator/container infrastructure.

    // Corrected stubs matching the interface signatures indicated by build errors:

    public Task<ArtifactMetadata?> GetByIdAsync(string id, string partitionKey)
    {
        _ = id; // Avoid unused parameter warning
        _ = partitionKey; // Avoid unused parameter warning
        throw new NotImplementedException("InMemoryArtifactMetadataRepository is deprecated. Use emulator-backed repositories for testing.");
    }

    public Task<ArtifactMetadata?> GetBySourceIdentifierAsync(string sourceIdentifier)
    {
        _ = sourceIdentifier; // Avoid unused parameter warning
        throw new NotImplementedException("InMemoryArtifactMetadataRepository is deprecated. Use emulator-backed repositories for testing.");
    }

    public Task<ArtifactMetadata> SaveAsync(ArtifactMetadata metadata)
    {
        _ = metadata; // Avoid unused parameter warning
        throw new NotImplementedException("InMemoryArtifactMetadataRepository is deprecated. Use emulator-backed repositories for testing.");
    }

    public Task DeleteAsync(string id, string partitionKey)
    {
        _ = id; // Avoid unused parameter warning
        _ = partitionKey; // Avoid unused parameter warning
        throw new NotImplementedException("InMemoryArtifactMetadataRepository is deprecated. Use emulator-backed repositories for testing.");
    }
}
