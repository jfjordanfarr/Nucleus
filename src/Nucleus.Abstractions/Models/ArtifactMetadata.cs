using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Represents the core metadata associated with an artifact managed by Nucleus.
/// This record is persisted in the central ArtifactMetadataContainer in Cosmos DB.
/// See: ../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md#2-key-metadata-structure-artifactmetadata
/// See: ../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md#3-artifactmetadatacontainer-schema
/// </summary>
public record ArtifactMetadata
{
    /// <summary>
    /// The unique identifier for the metadata record within Nucleus (typically a GUID).
    /// This is the primary key in Cosmos DB.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// A stable, logical identifier representing the source artifact across potential URI changes.
    /// Derived from source system properties.
    /// Example: `teams:channelId:messageId:attachmentId`
    /// See: ../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md#21-identity-and-identifiers
    /// </summary>
    [JsonPropertyName("sourceIdentifier")]
    public required string SourceIdentifier { get; init; }

    /// <summary>
    /// The type of the system where the original artifact resides.
    /// </summary>
    [JsonPropertyName("sourceSystemType")]
    public required SourceSystemType SourceSystemType { get; init; }

    /// <summary>
    /// The specific URI pointing to the artifact in its source system.
    /// This might be temporary or change over time (e.g., SharePoint versioned links).
    /// </summary>
    [JsonPropertyName("sourceUri")]
    public required System.Uri SourceUri { get; init; }

    /// <summary>
    /// The ID of the tenant owning or associated with this artifact.
    /// Used as part of the partition key in Cosmos DB.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    /// <summary>
    /// The ID of the user who ingested or owns the artifact context.
    /// Used as part of the partition key in Cosmos DB.
    /// </summary>
    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    /// <summary>
    /// The original filename of the artifact, if applicable.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; init; }

    /// <summary>
    /// The MIME type of the artifact (e.g., "application/pdf", "text/plain").
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>
    /// The size of the artifact in bytes.
    /// </summary>
    [JsonPropertyName("sizeBytes")]
    public long? SizeBytes { get; init; }

    /// <summary>
    /// Timestamp when the artifact was created in the source system.
    /// </summary>
    [JsonPropertyName("createdAtSource")]
    public DateTimeOffset? CreatedAtSource { get; init; }

    /// <summary>
    /// Timestamp when the artifact was last modified in the source system.
    /// </summary>
    [JsonPropertyName("modifiedAtSource")]
    public DateTimeOffset? ModifiedAtSource { get; init; }

    /// <summary>
    /// Timestamp when this metadata record was first created in Nucleus.
    /// </summary>
    [JsonPropertyName("createdAtNucleus")]
    public DateTimeOffset CreatedAtNucleus { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when this metadata record was last updated in Nucleus.
    /// </summary>
    [JsonPropertyName("modifiedAtNucleus")]
    public DateTimeOffset ModifiedAtNucleus { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The ID of the user who last modified this metadata record.
    /// </summary>
    [JsonPropertyName("modifiedByUserId")]
    public string? ModifiedByUserId { get; set; }

    /// <summary>
    /// The name or title of the artifact.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// The author or creator of the artifact in the source system.
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; init; }

    /// <summary>
    /// A brief summary or description of the artifact's content.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Tags or keywords associated with the artifact.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Information about the specific platform context (e.g., Channel ID, Message ID).
    /// </summary>
    [JsonPropertyName("platformContext")]
    public Dictionary<string, string> PlatformContext { get; init; } = [];

    /// <summary>
    /// IDs of the Personas that have analyzed this artifact.
    /// Used to link to PersonaKnowledgeEntry records.
    /// </summary>
    [JsonPropertyName("analyzedByPersonaIds")]
    public IList<string> AnalyzedByPersonaIds { get; init; } = [];

    /// <summary>
    /// Vector embedding of the artifact's summary or title for similarity searches.
    /// </summary>
    [JsonPropertyName("summaryEmbedding")]
    public IReadOnlyList<float>? SummaryEmbedding { get; set; }

    /// <summary>
    /// The model used to generate the summaryEmbedding.
    /// </summary>
    [JsonPropertyName("summaryEmbeddingModel")]
    public string? SummaryEmbeddingModel { get; set; }

    /// <summary>
    /// The partition key value used for Cosmos DB.
    /// Typically derived from tenantId or userId.
    /// See: ../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md#6-partitioning-strategy
    /// </summary>
    [JsonIgnore] // Ignored during serialization, calculated on the fly if needed.
    public string PartitionKey => TenantId ?? UserId ?? throw new InvalidOperationException("Either TenantId or UserId must be set for PartitionKey.");

}

/// <summary>
/// Enumerates the known source systems for artifacts.
/// See: ../../Docs/Architecture/03_ARCHITECTURE_STORAGE.md#22-source-system-information
/// </summary>
public enum SourceSystemType
{
    Unknown,
    MicrosoftTeams,
    Slack,
    Email,
    FileSystem,
    SharePoint,
    WebUrl,
    // Add other relevant source systems as needed
}
