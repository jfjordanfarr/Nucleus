---
title: "MCP Tool: RAGPipeline Server Architecture"
description: "Detailed architecture for the Nucleus_RAGPipeline_McpServer, responsible for advanced Retrieval Augmented Generation (RAG) including hybrid search, 4R ranking, and orchestration of calls to KnowledgeStore and LlmOrchestration MCPs."
version: 1.0
date: 2025-05-27
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
    - ./KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md # Critical dependency
    - ./LlmOrchestration/ARCHITECTURE_MCPTOOL_LLM_ORCHESTRATION.md # For query expansion, re-ranking, embedding generation
    - ../../Domain/RAGLogic/NAMESPACE_DOMAIN_RAGLOGIC.md # Planned: Defines core RAG algorithms & strategies
    - ../../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md
    - ../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md # For DTO definitions
---

# MCP Tool: RAGPipeline Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide M365 Persona Agents with sophisticated retrieval capabilities over the knowledge stored by the `Nucleus_KnowledgeStore_McpServer`.
*   **Capabilities Encapsulated:**
    *   Implementation of advanced search strategies (e.g., hybrid search combining vector similarity and keyword/metadata filters).
    *   Application of the "4R Ranking System" (Recency, Relevancy, Richness, Reputation) to retrieved candidates.
    *   Orchestration of queries to the `Nucleus_KnowledgeStore_McpServer`.
*   **Contribution to M365 Agents:** Enables agents to receive highly relevant and ranked contextual information to inform their LLM prompts and generate more accurate and grounded responses.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `RAGPipeline.SearchAndRank`
*   **Description:** Takes a user query, tenant/persona context, and ranking preferences, then performs an advanced search and ranking process, returning a list of relevant `PersonaKnowledgeEntry` summaries or references.
*   **Input (DTO):** `SearchAndRankRequest { string TenantId, string? PersonaId, string UserQuery, float[]? UserQueryVector, int TopKInitialRetrieve, int TopKFinalRank, RankingPreferences RankingPrefs, KnowledgeSearchFilters? Filters }`.
    *   `UserQueryVector`: Optional. If not provided by the calling agent, this tool can orchestrate its generation by calling `Nucleus_LlmOrchestration_McpServer.GenerateEmbeddings`.
    *   `RankingPreferences`: Specifies weights for 4R factors.
    *   `KnowledgeSearchFilters`: Includes date ranges, source artifact types, etc.
    *   *(DTOs are defined in `Nucleus.Shared.Kernel.Abstractions.Models` or a similar shared kernel project).*
*   **Output (DTO):** `SearchAndRankResponse { List<RankedKnowledgeResult> Results, string? ErrorMessage }`.
    *   `RankedKnowledgeResult`: Includes `PersonaKnowledgeEntry` (or its summary/ID), the overall rank score, and individual 4R scores.
    *   *(DTOs are defined in `Nucleus.Shared.Kernel.Abstractions.Models` or a similar shared kernel project).*

## 3. Core Internal Logic & Components

*   **Query Understanding/Expansion (Optional):**
    *   May include minor query pre-processing (e.g., stop word removal, synonym expansion).
    *   For advanced query understanding or expansion requiring LLM capabilities, this tool **should make an MCP call to `Nucleus_LlmOrchestration_McpServer`** (e.g., `LlmOrchestration.ExpandQuery` or `LlmOrchestration.RephraseQuery`) rather than making direct `IChatClient` calls.
*   **Embedding Generation (if `UserQueryVector` is missing):**
    *   If the `UserQueryVector` is not provided in the request, this tool will call `Nucleus_LlmOrchestration_McpServer.GenerateEmbeddings` with the `UserQuery` to obtain the necessary vector.
*   **Interaction with `Nucleus_KnowledgeStore_McpServer`:**
    *   Makes MCP calls to `KnowledgeStore.SearchPersonaKnowledgeByVector` using the (potentially newly generated) `UserQueryVector` (if provided) or by generating one from `UserQuery` (potentially via an embedding model call, perhaps orchestrated by `LlmOrchestration_McpServer`).
    *   Makes MCP calls to `KnowledgeStore.SearchArtifactMetadata` and/or `KnowledgeStore.SearchPersonaKnowledgeByFilters` using `Filters` to fetch candidates based on metadata or keywords for hybrid search.
    *   Retrieves an initial set of candidates (e.g., `TopKInitialRetrieve`).
*   **4R Ranking Logic (Primary focus of `Nucleus.Domain.RAGLogic`):**
    *   This MCP server incorporates logic from the `Nucleus.Domain.RAGLogic` project (conceptual location for these algorithms).
    *   **Relevancy:** Calculated based on vector similarity scores from `KnowledgeStore.SearchPersonaKnowledgeByVector`, potentially augmented by keyword matches or LLM-based semantic similarity if query expansion was used.
    *   **Recency:** Calculated based on timestamps associated with `PersonaKnowledgeEntry` or underlying `ArtifactMetadata` (e.g., creation date, last modified date).
    *   **Richness:** Assessed based on the density of information, length, presence of summaries, or other indicators of content quality within the `PersonaKnowledgeEntry` or its associated artifact.
    *   **Reputation:** Could be based on explicit user feedback (future), frequency of access, or pre-defined source authority levels.
    *   Applies configured weights from `RankingPreferences` to combine these scores into a final rank for each candidate.
    *   Selects the `TopKFinalRank` results.
*   **Hybrid Search Orchestration:** Merges and de-duplicates results from vector search and filter-based searches before applying the full 4R ranking.
*   **Result Aggregation and Formatting:** Prepares the `SearchAndRankResponse` DTO with the ranked results and their scores.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault (for secrets like API keys for LLMs if used, or connection details for other MCP tools if not discoverable via a service registry).
    *   Azure App Configuration (for static configurations like default ranking weights, LLM model IDs for query expansion).
*   **External Services/LLMs:**
    *   None directly. All LLM interactions (for query expansion, re-ranking, or embedding generation) are orchestrated via `Nucleus_LlmOrchestration_McpServer`.
*   **Other Nucleus MCP Tools:**
    *   **Critically depends on `Nucleus_KnowledgeStore_McpServer`** for all underlying data retrieval.
    *   **Depends on `Nucleus_LlmOrchestration_McpServer`** for:
        *   Generating query embeddings if not provided by the caller.
        *   Performing LLM-based query expansion or re-ranking, if such features are implemented.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions` (for DTOs and common models).
    *   `Nucleus.Domain.RAGLogic` (conceptual project containing core algorithms for 4R ranking and hybrid search logic).
    *   `Nucleus.Mcp.Client` (standardized client for making authenticated MCP calls to other Nucleus MCP Tools).

## 5. Security Model

*   **Authentication of Callers:** All incoming MCP requests must be authenticated by validating Azure AD tokens. The server checks token signature, audience, and issuer.
*   **Authorization within the Tool:**
    *   `TenantId` and `PersonaId` from the `SearchAndRankRequest` are strictly used to scope all downstream calls to `Nucleus_KnowledgeStore_McpServer` and any other services. This ensures that an M365 Persona Agent can only retrieve and rank data within its authorized tenant and persona context.
*   **Authentication to Dependencies:**
    *   Uses its Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access Azure Key Vault and Azure App Configuration.
    *   Uses its Managed Identity to make authenticated MCP calls to `Nucleus_KnowledgeStore_McpServer` and `Nucleus_LlmOrchestration_McpServer` by acquiring tokens for their respective App Registrations / service principals, ensuring a secure service-to-service communication mesh.

## 6. Data Handling & Persistence (If Applicable)

*   This tool is primarily **stateless in terms of its own direct persistence**.
*   It retrieves data from `Nucleus_KnowledgeStore_McpServer`, processes it (ranks, filters), and returns the results.
*   It **does not write any data back to the `KnowledgeStore_McpServer` or any other persistent store.** The ranked results are provided to the calling M365 Persona Agent, which then decides how to use this information (e.g., for LLM prompt generation, or potentially to create new `PersonaKnowledgeEntry` records via a separate call to `KnowledgeStore_McpServer` if the interaction leads to new insights).

## 7. Deployment & Configuration Considerations

*   **Deployment:** Typically deployed as an Azure Container App or Azure App Service.
*   **Key Configurations (managed via Azure App Configuration and Key Vault):**
    *   Endpoints and authentication details (e.g., App ID URI) for `Nucleus_KnowledgeStore_McpServer`.
    *   Endpoints and authentication details for `Nucleus_LlmOrchestration_McpServer`.
    *   Default weights and parameters for the 4R ranking algorithm (e.g., default `RankingPreferences`).
    *   Settings for `TopKInitialRetrieve` and `TopKFinalRank` defaults.
    *   *(Note: Configuration for direct LLM client (`IChatClient`) is not needed here as LLM interactions are delegated to `Nucleus_LlmOrchestration_McpServer`)*.

## 8. Future Considerations / Potential Enhancements

*   **More sophisticated query understanding and decomposition:** Using LLMs to break down complex queries into sub-queries.
*   **User-configurable ranking profiles:** Allowing users or personas to save and apply different sets of `RankingPreferences`.
*   **Integration of real-time data sources:** Potentially augmenting knowledge store results with information from live feeds or tools (though this might be better handled at the M365 Agent level).
*   **Explainable AI for Ranking:** Providing reasons or contributing factors for why certain results ranked higher.
*   **Feedback Loop:** Incorporating implicit or explicit user feedback on search results to dynamically adjust ranking parameters (long-term).
