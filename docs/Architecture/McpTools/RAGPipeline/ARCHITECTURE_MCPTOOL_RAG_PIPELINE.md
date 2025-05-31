---
title: "MCP Tool: RAGPipeline Server Architecture"
description: "Detailed architecture for the Nucleus_RAGPipeline_McpServer, responsible for advanced Retrieval Augmented Generation (RAG) including hybrid search, 4R ranking, and orchestration of calls to KnowledgeStore and LlmOrchestration MCPs."
version: 1.1 # Incremented version
date: 2025-05-28 # Updated date
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
    - title: "MCP Tool: KnowledgeStore Server Architecture"
      link: ./KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md # Critical dependency
    - title: "MCP Tool: LlmOrchestration Server Architecture"
      link: ./LlmOrchestration/ARCHITECTURE_MCPTOOL_LLM_ORCHESTRATION.md # For query expansion, re-ranking, embedding generation
    # - title: "Domain: RAG Logic"
    #   link: ../../Domain/RAGLogic/NAMESPACE_DOMAIN_RAGLOGIC.md # Planned: Defines core RAG algorithms & strategies - Keep commented until exists
    - title: "Core: AI Integration Strategy"
      link: ../../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md
    - title: "Core: Abstractions, DTOs, and Interfaces"
      link: ../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md # For DTO definitions
    - title: "Foundations: MCP and M365 Agents SDK Primer"
      link: ../../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md
    - title: "M365 Agents Overview"
      link: ../../Agents/01_M365_AGENTS_OVERVIEW.md
---

# MCP Tool: RAGPipeline Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide M365 Persona Agents with sophisticated, context-aware, and ranked retrieval capabilities over the knowledge managed by the `Nucleus_KnowledgeStore_McpServer`.
*   **Capabilities Encapsulated:**
    *   Implementation of advanced search strategies, including **hybrid search** (combining dense vector similarity with sparse keyword/metadata filters) and potentially **multi-modal search** in future iterations.
    *   Application of the **"4R Ranking System"** (Recency, Relevancy, Richness, Reputation) to retrieved candidates, with configurable weights.
    *   Orchestration of queries to the `Nucleus_KnowledgeStore_McpServer` for candidate retrieval.
    *   Orchestration of calls to `Nucleus_LlmOrchestration_McpServer` for tasks like query embedding generation (if not provided by the caller) and potential LLM-based query expansion or re-ranking.
*   **Contribution to M365 Agents:** Enables agents to receive highly relevant and reliably ranked contextual information. This allows agents to construct more effective prompts for their internal LLMs, leading to more accurate, grounded, and nuanced responses to user interactions.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `RAGPipeline.SearchAndRank`
*   **Description:** The primary operation of this MCP tool. It takes a user query (text and/or vector), tenant/persona context, filtering criteria, and ranking preferences. It then performs an advanced search and ranking process, returning a list of relevant `PersonaKnowledgeEntry` summaries or references, ordered by their calculated relevance.
*   **Input (DTO):** `SearchAndRankRequest`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.RAGPipeline
    public class SearchAndRankRequest
    {
        public required string TenantId { get; init; }
        public string? PersonaId { get; init; } // Optional, for persona-specific ranking or filtering
        public required string UserQueryText { get; init; }
        public float[]? UserQueryVector { get; init; } // Optional, if pre-computed by the agent
        public int TopKInitialRetrieve { get; init; } = 50; // How many candidates to fetch initially
        public int TopKFinalRank { get; init; } = 10; // How many top candidates to return after ranking
        public RankingPreferences? RankingPrefs { get; init; } // Weights for 4R factors, persona-specific overrides
        public KnowledgeSearchFilters? Filters { get; init; } // Date ranges, source types, tags, etc.
        public bool ExpandQueryWithLlm { get; init; } = false; // Flag to enable LLM-based query expansion
    }
    ```
*   **Output (DTO):** `SearchAndRankResponse`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.RAGPipeline
    public class SearchAndRankResponse
    {
        public required IReadOnlyList<RankedKnowledgeResult> Results { get; init; }
        public string? DebugInformation { get; init; } // Optional, for diagnostics
        public string? ErrorMessage { get; init; }
    }

    public class RankedKnowledgeResult
    {
        public required string PersonaKnowledgeEntryId { get; init; }
        public required ArtifactMetadataSummary AssociatedArtifact { get; init; }
        public required string Snippet { get; init; } // Relevant text snippet from the PersonaKnowledgeEntry
        public float OverallRankScore { get; init; }
        public FourRScores? IndividualScores { get; init; }
    }

    public class FourRScores
    {
        public float Relevancy { get; init; }
        public float Recency { get; init; }
        public float Richness { get; init; }
        public float Reputation { get; init; }
    }
    ```

## 3. Core Internal Logic & Components

*   **Request Validation:** Ensures all required fields in `SearchAndRankRequest` are present and valid.
*   **Query Embedding Generation (Conditional):**
    *   If `UserQueryVector` is not provided in the `SearchAndRankRequest`, this tool **must** orchestrate its generation.
    *   It achieves this by making an MCP call to `Nucleus_LlmOrchestration_McpServer.GenerateEmbeddings` using the `UserQueryText`.
*   **Query Expansion (Optional & Conditional):**
    *   If `ExpandQueryWithLlm` is true, this tool will make an MCP call to `Nucleus_LlmOrchestration_McpServer` (e.g., a conceptual `LlmOrchestration.ExpandOrRephraseQuery` operation) using the `UserQueryText`.
    *   The expanded query terms or alternative phrasings can then be used to augment keyword searches or generate multiple embedding vectors for a broader initial retrieval.
*   **Candidate Retrieval (Interaction with `Nucleus_KnowledgeStore_McpServer`):**
    *   **Vector Search:** Makes an MCP call to `KnowledgeStore.SearchPersonaKnowledgeByVector` using the (potentially newly generated or provided) `UserQueryVector` and `Filters` to retrieve an initial set of candidates (up to `TopKInitialRetrieve`).
    *   **Keyword/Metadata Search (for Hybrid Search):** Makes MCP calls to `KnowledgeStore.SearchPersonaKnowledgeByFilters` (or a similar operation) using keywords extracted from `UserQueryText` (and potentially expanded query terms) along with `Filters`.
    *   The results from vector and keyword searches are merged and de-duplicated to form a comprehensive candidate pool.
*   **4R Ranking Logic (Leveraging `Nucleus.Domain.RAGLogic`):**
    *   This MCP server incorporates and applies the ranking algorithms defined within the conceptual `Nucleus.Domain.RAGLogic` project/namespace.
    *   **Relevancy:** Primarily calculated from vector similarity scores (cosine similarity from `KnowledgeStore.SearchPersonaKnowledgeByVector`). Can be augmented by keyword match scores (e.g., BM25 if supported by the KnowledgeStore or calculated here) and semantic similarity from LLM-based re-ranking (if `Nucleus_LlmOrchestration_McpServer` is used for this).
    *   **Recency:** Calculated based on timestamps from `PersonaKnowledgeEntry.Timestamp` or `AssociatedArtifact.LastModifiedDate` / `CreationDate`. A decay function might be applied.
    *   **Richness:** Assessed based on factors like the length/density of `PersonaKnowledgeEntry.ContentSummary` or `Snippet`, presence of structured data, or other indicators of information quality within the `PersonaKnowledgeEntry`.
    *   **Reputation:** Could be derived from `AssociatedArtifact.SourceReputationScore` (a planned metadata field), explicit user feedback (future), frequency of access, or pre-defined authority levels for different data sources.
    *   The `RankingPreferences` from the request (or defaults) provide weights for each of the 4R factors. These are used to compute a weighted `OverallRankScore` for each candidate.
    *   The candidates are sorted by `OverallRankScore`, and the top `TopKFinalRank` results are selected.
*   **Result Aggregation and Formatting:** Constructs the `SearchAndRankResponse` DTO, populating it with the `RankedKnowledgeResult` list, including snippets, scores, and associated artifact summaries.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault: For secure storage and retrieval of secrets (e.g., API keys for dependent MCP tool authentication if not using pure AAD, though AAD with Managed Identities is preferred).
    *   Azure App Configuration: For managing static configurations (e.g., default ranking weights, default `TopK` values, dependent MCP tool URIs if not using service discovery).
    *   Azure Application Insights: For logging, monitoring, and distributed tracing.
*   **External Services/LLMs:**
    *   None directly. All LLM interactions (for query embedding, expansion, or re-ranking) are strictly orchestrated via `Nucleus_LlmOrchestration_McpServer`.
*   **Other Nucleus MCP Tools:**
    *   **`Nucleus_KnowledgeStore_McpServer` (Critical):** For all underlying data retrieval (vector search, metadata filtering).
    *   **`Nucleus_LlmOrchestration_McpServer` (Critical):** For generating query embeddings (if not provided by the caller) and for any LLM-based query expansion or re-ranking features.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions`: For DTOs (e.g., `SearchAndRankRequest`, `SearchAndRankResponse`, `RankedKnowledgeResult`), common models, and interfaces.
    *   `Nucleus.Domain.RAGLogic` (Conceptual): This namespace/project will contain the core algorithms and business logic for the 4R ranking system and hybrid search strategies. The `RAGPipeline` MCP tool will be a primary consumer of this logic.
    *   `Nucleus.Mcp.Client`: Standardized client library for making authenticated and resilient MCP calls to other Nucleus MCP Tools (handles token acquisition, retries, etc.).
    *   `Microsoft.Extensions.Http.Resilience`: For implementing resilient HTTP calls to dependent MCP services.

## 5. Security Model

*   **Authentication of Callers (M365 Agents):**
    *   All incoming MCP requests from M365 Persona Agents **must** be authenticated.
    *   This server validates Azure AD tokens (obtained by the M365 Agent for this MCP tool's App Registration) ensuring the token's signature, audience (this MCP tool's App ID URI), and issuer are correct.
    *   App roles or scopes defined in this MCP tool's App Registration can be used for coarse-grained authorization if needed (e.g., `RAGPipeline.SearchAccess`).
*   **Authorization within the Tool (Data Scoping):**
    *   The `TenantId` from the validated token (or explicitly from the `SearchAndRankRequest`, cross-validated with token claims) **must** be used to scope all downstream calls to `Nucleus_KnowledgeStore_McpServer`.
    *   The optional `PersonaId` from the request, if present, further refines the scope, ensuring that an M365 Persona Agent can only retrieve and rank data within its authorized tenant and specific persona context.
*   **Authentication to Dependencies (Other MCP Tools):**
    *   This MCP server uses its own Azure AD Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access Azure Key Vault and Azure App Configuration.
    *   It uses its Managed Identity to acquire Azure AD tokens for the `Nucleus_KnowledgeStore_McpServer` and `Nucleus_LlmOrchestration_McpServer` App Registrations (specifically, for their App ID URIs/scopes). This ensures a secure, auditable service-to-service communication mesh based on least privilege.

## 6. Data Handling & Persistence

*   This MCP tool is primarily **stateless** regarding its own direct, long-term persistence.
*   It retrieves data from `Nucleus_KnowledgeStore_McpServer` and orchestrates calls to `Nucleus_LlmOrchestration_McpServer`.
*   It processes this data in-memory (e.g., for ranking, filtering) and returns the results in the `SearchAndRankResponse`.
*   It **does not write any `PersonaKnowledgeEntry` or `ArtifactMetadata` back to the `Nucleus_KnowledgeStore_McpServer` or any other persistent store.**
*   The ranked results are provided to the calling M365 Persona Agent. The agent then decides how to use this information (e.g., for LLM prompt generation, displaying to the user, or potentially initiating new knowledge creation via a separate call to `Nucleus_ContentProcessing_McpServer` or `Nucleus_KnowledgeStore_McpServer` if the interaction leads to new insights).
*   **Caching (Optional Short-Term):** May implement short-term in-memory caching for frequently requested, identical queries or for intermediate results like embeddings if performance analysis indicates a benefit, but this is not a primary persistence mechanism.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Deployed as a .NET Aspire application component, typically hosted as an Azure Container App or Azure App Service, configured for auto-scaling based on load.
*   **Key Configurations (Managed via .NET Aspire, Azure App Configuration, and Azure Key Vault):**
    *   Service discovery names or explicit endpoints for `Nucleus_KnowledgeStore_McpServer` and `Nucleus_LlmOrchestration_McpServer`.
    *   Authentication details for dependent MCPs (e.g., their App ID URIs for token acquisition).
    *   Default weights and parameters for the 4R ranking algorithm (e.g., default `RankingPreferences` if not supplied in the request).
    *   Default values for `TopKInitialRetrieve` and `TopKFinalRank`.
    *   Configuration for resilience policies (retries, timeouts) for calls to dependent MCPs.
    *   Logging levels and Application Insights connection string.
    *   *(Note: Direct LLM client (`IChatClient`) configuration is NOT needed here, as all LLM interactions are delegated to `Nucleus_LlmOrchestration_McpServer`)*.

## 8. Future Considerations / Potential Enhancements

*   **Advanced Query Decomposition:** Using `Nucleus_LlmOrchestration_McpServer` to break down complex user queries into multiple sub-queries, then aggregating results from each.
*   **LLM-based Re-ranking:** After initial 4R ranking, use `Nucleus_LlmOrchestration_McpServer` to perform a final re-ranking pass on the top N candidates based on deeper semantic understanding of the query and candidate snippets.
*   **User-Configurable Ranking Profiles:** Allow M365 Persona Agents (or end-users via agent settings) to define and save different sets of `RankingPreferences` for various tasks or information needs.
*   **Explainable AI for Ranking:** Augment `RankedKnowledgeResult` with details on why certain results ranked higher (e.g., which 4R factors contributed most significantly).
*   **Feedback Loop Integration:** Design mechanisms to receive implicit (e.g., click-through on a result) or explicit (e.g., "this was helpful") feedback from M365 Persona Agents. This feedback could be stored (perhaps in a new `FeedbackStore_McpServer` or within `KnowledgeStore_McpServer` linked to `PersonaKnowledgeEntry`) and used to fine-tune ranking models or update `Reputation` scores over time.
*   **Support for Multi-Modal Search:** Extend capabilities to include searching based on image or audio embeddings if `Nucleus_KnowledgeStore_McpServer` and `Nucleus_LlmOrchestration_McpServer` support multi-modal embeddings.
*   **Personalized Ranking Adjustments:** Dynamically adjust 4R weights based on individual user preferences or interaction history, potentially managed by a `Nucleus_UserProfile_McpServer`.
