# Phase 3: Enhancements & Sophistication Tasks

**Epic:** [`EPIC-ENHANCEMENTS`](./00_ROADMAP.md#phase-3-enhancements--sophistication)
**Requirements:** [`03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md`](../Requirements/03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md)

This document details the specific tasks required to complete Phase 3.

---

## `ISSUE-ENH-PERSONA-01`: Develop Sophisticated Personas

*   [ ] **TASK-P3-PER-E01:** Design `EduFlowPersona`:
    *   Define specific learning objectives/concepts it should extract.
    *   Define the `EduFlowAnalysis` C# record model for structured output.
    *   Define prompts for analysis and query handling specific to educational content.
*   [ ] **TASK-P3-PER-E02:** Implement `EduFlowPersona` logic (`AnalyzeContentAsync`, `HandleQueryAsync`).
*   [ ] **TASK-P3-PER-C01:** Design `CodeCompanionPersona`:
    *   Define types of code analysis (e.g., summaries, potential issues, language identification).
    *   Define the `CodeCompanionAnalysis` C# record model.
    *   Define prompts for code-related analysis and queries.
*   [ ] **TASK-P3-PER-C02:** Implement `CodeCompanionPersona` logic (`AnalyzeContentAsync`, `HandleQueryAsync`).
*   [ ] **TASK-P3-PER-GEN01:** Implement mechanism for the processing pipeline/API to select/route to the appropriate persona based on context or user request.

## `ISSUE-ENH-PROCESS-01`: Implement Advanced Metadata Extraction

*   [ ] **TASK-P3-META-01:** Research and choose service/library for entity extraction (e.g., Azure AI Language Service - Named Entity Recognition).
*   [ ] **TASK-P3-META-02:** Research and choose service/library for key phrase extraction (e.g., Azure AI Language Service).
*   [ ] **TASK-P3-META-03:** Update `ArtifactMetadata` model to include fields for entities, key phrases, etc.
*   [ ] **TASK-P3-META-04:** Integrate metadata extraction calls into the core processing pipeline (after content extraction).
*   [ ] **TASK-P3-META-05:** Implement service adapters/clients for chosen metadata extraction services.
*   [ ] **TASK-P3-META-06:** Ensure extracted metadata is stored via `IArtifactMetadataService`.

## `ISSUE-ENH-PROCESS-02`: Refine `PersonaKnowledgeEntry` Schema

*   [ ] **TASK-P3-SCHEMA-01:** Analyze storage needs for new personas (`EduFlow`, `CodeCompanion`).
*   [ ] **TASK-P3-SCHEMA-02:** Update the `PersonaKnowledgeEntry` C# record definition to accommodate potentially diverse structured analysis data (verify generic approach from P1 is sufficient or needs refinement).
*   [ ] **TASK-P3-SCHEMA-03:** Update `IPersonaKnowledgeRepository` adapter (`CosmosDbPersonaKnowledgeRepository`) to handle any schema changes.
*   [ ] **TASK-P3-SCHEMA-04:** Consider potential data migration needs for existing entries (if schema is backward-incompatible - low risk if generics work).

## `ISSUE-ENH-QUERY-01`: Enhance Knowledge Retrieval

*   [ ] **TASK-P3-QRY-01:** Update `IRetrievalService` interface to support filtering parameters (source platform, date range, persona ID, specific metadata fields).
*   [ ] **TASK-P3-QRY-02:** Update `BasicRetrievalService` implementation to:
    *   Accept filter parameters.
    *   Modify Cosmos DB query (e.g., add WHERE clauses alongside vector search) to apply filters.
*   [ ] **TASK-P3-QRY-03:** Update relevant API endpoint(s) in `Nucleus.Api` to accept and pass filter parameters to `IRetrievalService`.
*   [ ] **TASK-P3-QRY-04:** (Optional) Investigate/prototype hybrid search (keyword + vector) capabilities in Cosmos DB.

## `ISSUE-ENH-CONFIG-01`: Implement Robust Configuration Management

*   [ ] **TASK-P3-CONF-01:** Define configuration schema for personas (e.g., specific API keys, default prompts, feature flags).
*   [ ] **TASK-P3-CONF-02:** Define configuration schema for ingestion sources (credentials, target queues, enabled status).
*   [ ] **TASK-P3-CONF-03:** Implement loading of configuration using `Microsoft.Extensions.Options` pattern.
*   [ ] **TASK-P3-CONF-04:** Integrate configuration reading into Personas, Adapters, and Functions.
*   [ ] **TASK-P3-CONF-05:** Expose configuration settings via `Nucleus.Api` endpoints for the Admin UI.
*   [ ] **TASK-P3-CONF-06:** Ensure secure handling of sensitive configuration values (link to Key Vault).

## `ISSUE-ENH-CACHE-01`: Implement Caching

*   [ ] **TASK-P3-CACHE-01:** Define `ICacheManagementService` interface (align with architecture doc).
*   [ ] **TASK-P3-CACHE-02:** Research and select underlying cache mechanism (e.g., Gemini Cache API, potentially Azure Cache for Redis if cross-provider needed later).
*   [ ] **TASK-P3-CACHE-03:** Implement adapter for chosen cache mechanism (e.g., `GeminiCacheService`).
*   [ ] **TASK-P3-CACHE-04:** Integrate `ICacheManagementService` calls into `IPersona` implementations (e.g., Bootstrapper, potentially others) where appropriate.
*   [ ] **TASK-P3-CACHE-05:** Add configuration for cache service (API keys, TTL defaults).

## `ISSUE-ENH-TEST-01`: Establish Automated Testing Framework

*   [ ] **TASK-P3-TEST-01:** Set up Unit Test projects for Adapters, API, WebApp (e.g., `tests/Nucleus.Adapters.Email.Tests`, `tests/Nucleus.Api.Tests`).
*   [ ] **TASK-P3-TEST-02:** Choose mocking framework (e.g., Moq, NSubstitute).
*   [ ] **TASK-P3-TEST-03:** Write initial unit tests for key services and logic implemented in P1-P3.
*   [ ] **TASK-P3-TEST-04:** Set up Integration Test project (`tests/Nucleus.IntegrationTests`).
*   [ ] **TASK-P3-TEST-05:** Integrate Testcontainers (or similar) for emulating dependencies (Cosmos DB, Service Bus) in integration tests. *(Aligns with KANBAN TASK-ID-006 / existing backlog item)*
*   [ ] **TASK-P3-TEST-06:** Write initial integration tests for core workflows (e.g., ingest-process-store, basic query).
*   [ ] **TASK-P3-TEST-07:** Integrate test execution into CI pipeline (GitHub Actions / ADO).

---
