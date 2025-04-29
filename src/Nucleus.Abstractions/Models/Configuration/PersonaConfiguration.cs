// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Defines the preferred processing mode for interactions handled by a persona.
/// </summary>
public enum ProcessingPreference
{
    /// <summary>
    /// Prefer synchronous processing if possible, but allow fallback to asynchronous.
    /// (Default behavior if not specified).
    /// </summary>
    PreferSync = 0,

    /// <summary>
    /// Prefer asynchronous processing, but allow fallback to synchronous if necessary/trivial.
    /// </summary>
    PreferAsync = 1,

    /// <summary>
    /// Always process interactions asynchronously, regardless of complexity.
    /// </summary>
    ForceAsync = 2
}

/// <summary>
/// Represents the configuration settings for a specific Nucleus Persona, defining how the
/// generic Persona Runtime/Engine should execute interactions associated with this PersonaId.
/// Loaded typically from configuration sources like appsettings.json during service startup.
/// </summary>
/// <seealso cref="../../../../../Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md"/>
public class PersonaConfiguration
{
    #region Core Identification (2.1)

    /// <summary>
    /// Unique identifier for this specific persona configuration (e.g., "Bootstrapper", "Educator_Grade5", "Helpdesk_IT").
    /// Used by the Orchestration Router and Persona Runtime to select and load the correct configuration.
    /// Required.
    /// </summary>
    public string PersonaId { get; set; } = string.Empty;

    /// <summary>
    /// A user-friendly name for the persona.
    /// Required.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// A brief description of the persona's purpose and capabilities.
    /// Optional.
    /// </summary>
    public string? Description { get; set; }

    #endregion

    #region Operational Settings (2.2)

    /// <summary>
    /// If true, the Persona Runtime's interaction processing will generate an internal reasoning/planning artifact ('ShowYourWork').
    /// The Client Adapter is responsible for persisting this artifact.
    /// Optional, Defaults to true.
    /// </summary>
    /// <remarks>
    /// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md#conditional-artifact-generation-show-your-work
    /// </remarks>
    public bool ShowYourWork { get; set; } = true; // Default changed to true based on doc

    #endregion

    #region Activation Rules (2.3)

    /// <summary>
    /// Specifies events or conditions that can activate the persona.
    /// (e.g., "@mention", "DirectMessage", "SpecificKeyword").
    /// Used by the Orchestration Router.
    /// Optional. If null or empty, activation might rely solely on ContextScope or default routing.
    /// </summary>
    public List<string>? ActivationTriggers { get; set; } = new();

    /// <summary>
    /// Defines the context (e.g., channels, users) where the persona is active.
    /// Structure depends on the Client Adapter platform (e.g., "TeamsChannelIds": ["id1", "id2"], "UserIds": ["id3"]).
    /// Used by the Orchestration Router.
    /// Optional.
    /// </summary>
    public Dictionary<string, object>? ContextScope { get; set; } = new();

    #endregion

    #region Capability Settings (2.4)

    /// <summary>
    /// Configuration for the Language Learning Model (LLM) used by the persona.
    /// Required.
    /// </summary>
    public LlmConfiguration LlmConfiguration { get; set; } = new();

    /// <summary>
    /// List of tool IDs or names the Persona Runtime is permitted to use when executing this configuration.
    /// Requires a separate tool registration/definition mechanism and Runtime support for tool execution.
    /// Optional.
    /// </summary>
    public List<string>? EnabledTools { get; set; } = new();

    /// <summary>
    /// Defines how the persona accesses user knowledge artifacts and dedicated knowledge containers.
    /// Required.
    /// </summary>
    public KnowledgeScope KnowledgeScope { get; set; } = new();

    #endregion

    #region Prompt Configuration (2.5)

    /// <summary>
    /// The primary instruction or meta-prompt defining the persona's role, tone, capabilities, constraints,
    /// and how it should process the user query and context.
    /// Required.
    /// </summary>
    public string SystemMessage { get; set; } = string.Empty;

    /// <summary>
    /// Additional guidelines for formatting or structuring the final response
    /// (e.g., "Always cite sources using markdown links.", "Use bullet points for lists.").
    /// Optional.
    /// </summary>
    public string? ResponseGuidelines { get; set; }

    #endregion

    #region Agentic Strategy Configuration (2.6)

    /// <summary>
    /// Defines the high-level execution strategy the Persona Runtime should employ.
    /// </summary>
    public AgenticStrategyConfiguration AgenticStrategy { get; set; } = new();

    #endregion

    #region Custom Properties (2.7)

    /// <summary>
    /// A flexible key-value store for persona-specific configuration not covered by the standard schema.
    /// The Persona Runtime implementation needs to be aware of how to interpret and use these properties.
    /// Example: {"PedagogicalTreeRef": "trees/pedagogical_v1.json"}
    /// Optional.
    /// </summary>
    public Dictionary<string, string>? CustomProperties { get; set; }

    #endregion

    // Future fields can be added here
}

/// <summary>
/// Configuration specific to the Language Learning Model (LLM).
/// </summary>
public class LlmConfiguration
{
    /// <summary>
    /// AI Service provider (e.g., "GoogleGemini", "AzureOpenAI").
    /// Required.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Identifier for the primary chat/reasoning model (e.g., "gemini-1.5-pro-latest").
    /// Required.
    /// </summary>
    public string ChatModelId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier for the text embedding model (e.g., "text-embedding-004").
    /// Required.
    /// </summary>
    public string EmbeddingModelId { get; set; } = string.Empty;

    /// <summary>
    /// Sampling temperature for generation.
    /// Optional.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for the LLM response.
    /// Optional.
    /// </summary>
    public int? MaxOutputTokens { get; set; }
}

/// <summary>
/// Defines the scope and strategy for accessing user knowledge artifacts and persona knowledge containers.
/// </summary>
public class KnowledgeScope
{
    /// <summary>
    /// Strategy for accessing user knowledge (e.g., AllUserArtifacts, SpecificCollectionIds, MetadataOnly, NoUserArtifacts).
    /// Required.
    /// </summary>
    public KnowledgeScopeStrategy Strategy { get; set; } = KnowledgeScopeStrategy.NoUserArtifacts; // Default to safest

    /// <summary>
    /// List of specific artifact collection IDs to use when Strategy is SpecificCollectionIds.
    /// Optional.
    /// </summary>
    public List<string>? CollectionIds { get; set; } = new();

    /// <summary>
    /// Specifies the ID of a dedicated Persona Knowledge Container (Cosmos DB container)
    /// to query for PersonaKnowledgeEntry records.
    /// If null or empty, only ArtifactMetadata is searched.
    /// Optional.
    /// </summary>
    /// <remarks>See: Docs/Architecture/04_ARCHITECTURE_DATABASE.md#4-personaidknowledgecontainer-schema</remarks>
    public string? TargetKnowledgeContainerId { get; set; }

    /// <summary>
    /// Maximum number of full documents to retrieve ephemerally based on metadata search
    /// to form the context window for the LLM.
    /// Optional, Defaults to 10.
    /// </summary>
    /// <remarks>See: Docs/Architecture/06_ARCHITECTURE_SECURITY.md#6-ai--prompt-security</remarks>
    public int MaxContextDocuments { get; set; } = 10;
}

/// <summary>
/// Defines the high-level execution strategy the Persona Runtime should employ.
/// </summary>
public class AgenticStrategyConfiguration
{
    /// <summary>
    /// The unique key identifying the specific IAgenticStrategyHandler implementation to use.
    /// Required. This key is used for dependency injection resolution.
    /// Examples: "SimpleRagStrategy", "MultiStepReasoningStrategy", "EchoStrategy".
    /// </summary>
    public string StrategyKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional configuration parameters specific to the selected StrategyKey.
    /// Must be an instance of a class derived from AgenticStrategyParametersBase.
    /// The specific type required depends on the IAgenticStrategyHandler implementation.
    /// Handlers are responsible for casting this to their expected parameter type.
    /// </summary>
    public AgenticStrategyParametersBase? Parameters { get; set; } // Changed from Dictionary to base class

    /// <summary>
    /// Used by iterative strategies (e.g., MultiStepReasoning, ToolUsing) to limit execution loops.
    /// Optional. Defaults to 1.
    /// </summary>
    public int MaxIterations { get; set; } = 1;

    // Future: May include strategy-specific parameters directly if a common structure emerges.
}

/// <summary>
/// Enum defining strategies for accessing user knowledge.
/// </summary>
public enum KnowledgeScopeStrategy
{
    /// <summary>
    /// The persona does not access any user artifacts or specific persona knowledge entries.
    /// </summary>
    NoUserArtifacts,

    /// <summary>
    /// The persona can potentially access all artifacts associated with the user context.
    /// (Use with caution - ensure appropriate filtering happens during retrieval).
    /// Also queries the default ArtifactMetadata container, not specific PersonaKnowledgeContainers.
    /// </summary>
    AllUserArtifacts,

    /// <summary>
    /// The persona can only access artifacts belonging to specific collections identified in CollectionIds.
    /// Also queries the default ArtifactMetadata container, filtered by these collections.
    /// </summary>
    SpecificCollectionIds,

    /// <summary>
    /// The persona only queries metadata (ArtifactMetadata and/or PersonaKnowledgeEntry in the TargetKnowledgeContainerId)
    /// but does *not* trigger ephemeral content retrieval based on ArtifactReferences found in that metadata.
    /// Useful for personas that operate solely on indexed knowledge or summaries.
    /// </summary>
    MetadataOnly
}

/// <summary>
/// Enum defining the high-level agentic execution strategy for the Persona Runtime.
/// The corresponding handler implementation should be registered with DI using the enum member's string representation as the key.
/// </summary>
public enum AgenticStrategyType
{
    /// <summary>
    /// Basic Retrieve-Augment-Generate. Performs metadata search, potentially retrieves full content ephemerally,
    /// synthesizes response in a single primary LLM call.
    /// Key: "SimpleRag"
    /// </summary>
    SimpleRag,

    /// <summary>
    /// Allows for internal planning and potentially multiple LLM calls or retrieval steps to answer complex queries.
    /// Requires sophisticated Runtime implementation.
    /// Key: "MultiStepReasoning"
    /// </summary>
    MultiStepReasoning,

    /// <summary>
    /// Enables the Runtime to plan and execute configured EnabledTools as part of the interaction.
    /// Requires sophisticated Runtime implementation and tool definitions.
    /// Key: "ToolUsing"
    /// </summary>
    ToolUsing,

    /// <summary>
    /// Simple strategy for testing that echoes the input query.
    /// Key: "Echo"
    /// </summary>
    Echo // Added Echo strategy type
}
