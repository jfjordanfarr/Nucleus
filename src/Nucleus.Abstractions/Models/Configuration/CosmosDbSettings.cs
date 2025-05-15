namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Configuration settings for connecting to Azure Cosmos DB.
/// <seealso cref="../../../../Docs/Architecture/12_ARCHITECTURE_ABSTRACTIONS.md"/>
/// </summary>
public class CosmosDbSettings
{
    /// <summary>
    /// Gets or sets the connection string for the Cosmos DB account.
    /// This is typically retrieved from configuration (e.g., appsettings.json or environment variables).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of the database to use within the Cosmos DB account.
    /// Defaults to "NucleusDb" if not specified.
    /// </summary>
    public string DatabaseName { get; set; } = "NucleusDb";

    /// <summary>
    /// Gets or sets the name of the container used for storing ArtifactMetadata.
    /// Defaults to "ArtifactMetadata" if not specified.
    /// </summary>
    public string MetadataContainerName { get; set; } = "ArtifactMetadata";

    /// <summary>
    /// Gets or sets the name of the container used for storing PersonaKnowledgeEntry items.
    /// Defaults to "PersonaKnowledgeEntries" if not specified.
    /// </summary>
    public string KnowledgeContainerName { get; set; } = "PersonaKnowledgeEntries";

    /// <summary>
    /// Gets or sets the name of the container used for storing PersonaConfiguration items.
    /// Defaults to "Personas" if not specified.
    /// </summary>
    public string PersonaContainerName { get; set; } = "Personas";

    // Add any other relevant settings, e.g., throughput configuration, consistency level, etc.
}
