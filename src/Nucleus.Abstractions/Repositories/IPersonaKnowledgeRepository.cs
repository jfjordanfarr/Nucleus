using Nucleus.Abstractions.Models; // Kept for potential future use or if needed by generics implicitly.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Repositories;

/// <summary>
/// Defines the contract for storing and retrieving persona-specific knowledge entries.
/// These entries represent the processed output or analysis derived from artifacts by a specific persona.
/// Implementations will typically interact with a persistent data store (e.g., Cosmos DB).
/// This interface works with PersonaKnowledgeEntry records which use System.Text.Json.JsonElement for the AnalysisData property,
/// allowing flexible, configuration-driven schemas for analysis results.
/// </summary>
/// <seealso cref="Models.PersonaKnowledgeEntry"/>
/// <seealso cref="Models.ConfidenceLevel"/>
/// <seealso cref="../../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
/// <seealso cref="../../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md"/>
/// <seealso cref="../../../../../Docs/Architecture/02_ARCHITECTURE_PERSONAS.md"/>
/// <seealso cref="../../../../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md"/>
/// <seealso cref="../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md#332-ipersonaknowledgerepositorycs"/>
public interface IPersonaKnowledgeRepository
{
    /// <summary>
    /// Retrieves a single PersonaKnowledgeEntry record by its unique ID within its container.
    /// </summary>
    /// <param name="id">The unique ID of the knowledge entry.</param>
    /// <param name="partitionKey">The partition key (e.g., artifactId, tenantId, or userId) required for the Cosmos DB query.</param>
    /// <returns>The found PersonaKnowledgeEntry record, or null if not found.</returns>
    Task<PersonaKnowledgeEntry?> GetByIdAsync(string id, string partitionKey);

    /// <summary>
    /// Retrieves all PersonaKnowledgeEntry records associated with a specific artifact ID.
    /// </summary>
    /// <param name="artifactId">The ID of the artifact whose knowledge entries are to be retrieved.</param>
    /// <param name="partitionKey">The partition key strategy might require passing artifactId, tenantId or userId here depending on container setup.</param>
    /// <returns>An enumerable collection of PersonaKnowledgeEntry records.</returns>
    Task<IEnumerable<PersonaKnowledgeEntry>> GetByArtifactIdAsync(string artifactId, string partitionKey);

    /// <summary>
    /// Creates or updates a PersonaKnowledgeEntry record in the repository.
    /// Implementations should handle setting relevant timestamps.
    /// </summary>
    /// <param name="entry">The PersonaKnowledgeEntry record to save.</param>
    /// <returns>The saved PersonaKnowledgeEntry record.</returns>
    Task<PersonaKnowledgeEntry> SaveAsync(PersonaKnowledgeEntry entry);

    /// <summary>
    /// Deletes a PersonaKnowledgeEntry record by its unique ID.
    /// </summary>
    /// <param name="id">The unique ID of the knowledge entry to delete.</param>
    /// <param name="partitionKey">The partition key required for the Cosmos DB delete operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string id, string partitionKey);

    // Potential future methods for querying based on analysis data properties or vector search
    // Task<IEnumerable<PersonaKnowledgeEntry>> FindByAnalysisPropertyAsync(string propertyName, object value, string partitionKey);
    // Task<IEnumerable<(PersonaKnowledgeEntry Entry, double Score)>> FindSimilarAsync(float[] embedding, int topK, string partitionKey);
}

/* DELETED: Duplicate Record Definitions Removed - See Nucleus.Abstractions.Models */
