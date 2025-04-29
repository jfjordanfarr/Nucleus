---
title: "Phase 3: API Enhancements & Advanced Personas Tasks"
description: "Detailed tasks for implementing Nucleus Phase 3 enhancements, focusing on sophisticated personas, advanced metadata extraction, 4 R ranking, caching, and configuration."
version: 1.3
date: 2025-04-27
---

# Phase 3: API Enhancements & Advanced Personas Tasks

**Epic:** [`EPIC-ENHANCEMENTS`](./00_ROADMAP.md#phase-3-enhancements--sophistication)
**Requirements:** [`03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md`](../Requirements/03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md)

This document details the specific tasks required to complete Phase 3, primarily focused on the `Nucleus.Services.Api` and its supporting components.

---

## `ISSUE-ENH-PERSONA-01`: Develop Sophisticated Personas (within ApiService)

*   [ ] **TASK-P3-PER-E01:** Design specialized Persona capability requirements (e.g., for Education, Code Assistance):
    *   Define specific analytical goals and desired structured output models (e.g., `EduFlowAnalysis`, `CodeCompanionAnalysis` C# records).
    *   Define agentic workflow steps (e.g., initial analysis of ephemeral content, use of `IRetrievalService` for related metadata/knowledge, synthesis steps).
    *   Define prompts for analysis and query handling specific to the capability.
*   [ ] **TASK-P3-PER-E02:** Implement new `IPersona` classes incorporating agentic steps:
    *   Leverage ephemeral context provided by the API's interaction pipeline (content from `IArtifactProvider`).
    *   Call `IRetrievalService` to fetch relevant *metadata* (`ArtifactMetadata`, `PersonaKnowledgeEntry`) based on initial analysis or query.
    *   Synthesize responses using both ephemeral context and retrieved metadata/knowledge.
    *   Handle internal tool use if applicable (e.g., calling specialized services).
*   [ ] **TASK-P3-PER-GEN01:** Enhance the mechanism within the `Nucleus.Services.Api` interaction pipeline to select/route to the appropriate persona based on request context or configuration.

## `ISSUE-ENH-PROCESS-01`: Implement Advanced Metadata Extraction (within ApiService)

*   [ ] **TASK-P3-META-01:** Research and choose service/library for entity extraction (e.g., Azure AI Language Service - Named Entity Recognition) - applied to **ephemeral content** during processing.
*   [ ] **TASK-P3-META-02:** Research and choose service/library for key phrase extraction (e.g., Azure AI Language Service) - applied to **ephemeral content**.
*   [ ] **TASK-P3-META-03:** Research and implement method for calculating 'Richness' metric (e.g., based on word count, structural complexity of ephemeral content) - applied to **ephemeral content**.
*   [ ] **TASK-P3-META-04:** Update `ArtifactMetadata` model to include fields for extracted entities, key phrases, 'Richness' score, and 'Reputation' score (initially 0, updatable later).
*   [ ] **TASK-P3-META-05:** Integrate metadata extraction calls into the core processing pipeline within `Nucleus.Services.Api` (after ephemeral content retrieval via `IArtifactProvider`, before persona invocation).
*   [ ] **TASK-P3-META-06:** Implement service adapters/clients for chosen metadata extraction services (e.g., Azure AI Language).
*   [ ] **TASK-P3-META-07:** Ensure extracted metadata (Entities, Key Phrases, Richness) is stored via `IArtifactMetadataRepository` when saving the artifact record.

## `ISSUE-ENH-PROCESS-02`: Refine Knowledge/Metadata Schema (within Domain/Data)

*   [ ] **TASK-P3-SCHEMA-01:** Analyze storage needs for new personas and **4 R ranking** (Recency, Relevancy, Richness, Reputation).
*   [ ] **TASK-P3-SCHEMA-02:** Update `ArtifactMetadata` and `PersonaKnowledgeEntry` C# record definitions to ensure all necessary fields for filtering and ranking are present (Confirm fields from `TASK-P3-META-04` and add `LastAccessed` or similar for Recency).
*   [ ] **TASK-P3-SCHEMA-03:** Update `IArtifactMetadataRepository` & `IPersonaKnowledgeRepository` interfaces and adapters (`CosmosDb*Repository`) to handle any schema changes (including indexing for ranking fields).
*   [ ] **TASK-P3-SCHEMA-04:** Consider potential data migration needs for existing entries if schema is breaking.

## `ISSUE-ENH-QUERY-01`: Enhance **Metadata** Retrieval with 4 R Ranking (within ApiService/Data)

*   [ ] **TASK-P3-QRY-01:** Update `IRetrievalService` interface to support richer filtering parameters (dates, metadata fields, etc.) and return ranked lists of `ArtifactMetadata` and/or `PersonaKnowledgeEntry`.
*   [ ] **TASK-P3-QRY-02:** Implement `RankedRetrievalService` (or enhance `BasicRetrievalService`) to:
    *   Perform initial candidate retrieval from Cosmos DB based on query (vector search on `PersonaKnowledgeEntry.Embeddings`) AND metadata filters (on `ArtifactMetadata` / `PersonaKnowledgeEntry`).
    *   Implement the **4 R Ranking algorithm** on the candidate set:
        *   **Recency:** Score based on `ArtifactMetadata.Timestamp` / `LastAccessed`.
        *   **Relevancy:** Score based on vector similarity from initial search.
        *   **Richness:** Score based on `ArtifactMetadata.RichnessScore`.
        *   **Reputation:** Score based on `ArtifactMetadata.ReputationScore`.
    *   Combine scores using a defined weighting strategy.
    *   Return the top-ranked list of `ArtifactMetadata` / `PersonaKnowledgeEntry`.
*   [ ] **TASK-P3-QRY-03:** Integrate the `RankedRetrievalService` into agentic personas (`TASK-P3-PER-E02`) to fetch relevant metadata before deciding which artifacts need full ephemeral content retrieval via `IArtifactProvider`.
*   [ ] **TASK-P3-QRY-04:** Implement mechanism to update 'Reputation' score on `ArtifactMetadata` based on user feedback or usage patterns (potentially requires new API endpoint or feedback loop).

## `ISSUE-ENH-CONFIG-01`: Implement Robust Configuration Management (within ApiService/Adapters)

*   [ ] **TASK-P3-CONF-01:** Define configuration schema for personas (e.g., specific API keys, default prompts, feature flags).
*   [ ] **TASK-P3-CONF-02:** Define configuration schema for ingestion sources (credentials, target queues, enabled status).
*   [ ] **TASK-P3-CONF-03:** Implement loading of configuration using `Microsoft.Extensions.Options` pattern.
*   [ ] **TASK-P3-CONF-04:** Integrate configuration reading into Personas, Adapters, and Functions.
*   [ ] **TASK-P3-CONF-05:** Expose necessary configuration settings (read-only view) via `Nucleus.Services.Api` endpoints if needed for an Admin UI (Phase 4).
*   [ ] **TASK-P3-CONF-06:** Ensure secure handling of sensitive configuration values (link to Key Vault).

## `ISSUE-ENH-CACHE-01`: Implement Caching (within ApiService)

*   [ ] **TASK-P3-CACHE-01:** Define `ICacheManagementService` interface. (Ref Code: `Nucleus.Abstractions/Interfaces/` - TBD)
*   [ ] **TASK-P3-CACHE-02:** Implement adapter for chosen cache provider (e.g., `AzureRedisCacheService` or `InMemoryCacheService` for testing). (Ref Code: `Nucleus.Infrastructure/Services/Caching/` - TBD)
*   [ ] **TASK-P3-CACHE-03:** Integrate caching for LLM provider calls (via `Microsoft.Extensions.AI` or custom logic).
*   [ ] **TASK-P3-CACHE-04:** Consider caching for frequently accessed metadata or retrieval results.

## `ISSUE-ENH-TEST-01`: Establish Automated Testing Framework

*   [ ] **TASK-P3-TEST-01:** Set up Unit Test projects for core libraries (`Nucleus.Core`, `Nucleus.Processing`, `Nucleus.Personas`).
*   [ ] **TASK-P3-TEST-02:** Implement unit tests for key logic (extractors, persona base classes, repository interfaces mocks).
*   [ ] **TASK-P3-TEST-03:** Set up Integration Test project for `Nucleus.Services.Api`.
*   [ ] **TASK-P3-TEST-04:** Implement integration tests for API endpoints using `WebApplicationFactory` (testing interaction flows, basic metadata saving, persona invocation).
*   [ ] **TASK-P3-TEST-05:** Configure CI pipeline (GitHub Actions) to run tests on push/PR.

---
