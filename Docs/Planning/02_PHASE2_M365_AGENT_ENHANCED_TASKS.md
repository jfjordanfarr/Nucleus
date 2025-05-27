---
title: "Phase 2: M365 Agent Enhanced Capabilities & Backend Persistence Tasks"
description: "Detailed tasks for Nucleus Phase 2, focusing on enhancing the M365 Agent with more sophisticated MCP Tool interactions, implementing backend data persistence for metadata and knowledge, and initial persona development beyond simple echo."
version: 1.0
date: 2025-05-25
---

# Phase 2: M365 Agent Enhanced Capabilities & Backend Persistence Tasks

**Epic:** [`EPIC-M365-ENHANCED`](./00_ROADMAP.md#phase-2-m365-agent-enhanced-capabilities--backend-persistence)
**Requirements:** [`02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`](../Requirements/02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md) (Renamed to "Requirements: M365 Agent & MCP Tool Integration" - covers foundational needs)

This document details the specific tasks required to complete Phase 2, building upon the M365 Agent MVP.

---

## `ISSUE-P2-AGENT-ENH-01`: Enhanced M365 Agent Capabilities

*Primary Location: `Nucleus.Adapters.M365Agent`*

*   [ ] **TASK-P2-AGENT-01:** Implement more robust conversation management within the agent (e.g., handling multi-turn interactions, maintaining basic conversation state if needed before full semantic memory).
*   [ ] **TASK-P2-AGENT-02:** Improve error handling and user feedback mechanisms in Teams (e.g., clearer messages for tool failures, API errors).
*   [ ] **TASK-P2-AGENT-03:** Agent should be able to invoke multiple MCP tools if necessary (design for future, implement one more as example).
*   [ ] **TASK-P2-AGENT-04:** Explore and implement basic Adaptive Card usage for richer responses in Teams.
*   [ ] **TASK-P2-AGENT-05:** Refine agent configuration for easier management of API endpoints, tool invocation, etc.

## `ISSUE-P2-MCPTOOL-CALENDAR-01`: Develop MCP Tool for User's Calendar Context (Example of a new tool)

*Primary Location: `Nucleus.MCP.Tools.Calendar` (New Project)*

*   [ ] **TASK-P2-MCP-C-01:** Initialize new .NET project for the Calendar MCP Tool (`Nucleus.MCP.Tools.Calendar`).
*   [ ] **TASK-P2-MCP-C-02:** Implement an MCP Tool that can be invoked by the M365 Agent.
*   [ ] **TASK-P2-MCP-C-03:** Tool Focus: Accessing User's Calendar Information (e.g., upcoming meetings).
    *   [ ] Define tool input: User query (e.g., "what are my meetings today?").
    *   [ ] Implement logic to use Microsoft Graph API to fetch calendar data for the invoking user.
        *   Handle authentication using the M365 Agent's context/identity.
        *   Ensure appropriate permissions (e.g., `Calendars.ReadBasic`).
    *   [ ] Define tool output: Standardized representation of calendar events (e.g., list of events with time, subject, attendees in Markdown or structured data).
*   [ ] **TASK-P2-MCP-C-04:** Implement error handling (permissions, API errors).
*   [ ] **TASK-P2-MCP-C-05:** Write unit tests for the Calendar MCP Tool.
*   [ ] **TASK-P2-AGENT-06:** Integrate invocation of the Calendar MCP Tool into the M365 Agent based on user intent.

## `ISSUE-P2-BACKEND-PERSIST-01`: Implement Backend Data Persistence

*Primary Location: `Nucleus.Services.Api`, `Nucleus.Data`, `Nucleus.Infrastructure`*

*   [ ] **TASK-P2-API-DB-01:** `Nucleus.Core/Models`: Finalize C# record structures for `ArtifactMetadata` and `PersonaKnowledgeEntry` based on initial analysis needs.
*   [ ] **TASK-P2-DB-REPO-01:** Define `IArtifactMetadataRepository` and `IPersonaKnowledgeRepository` interfaces. (Ref Code: `Nucleus.Abstractions/Interfaces/Data/`)
*   [ ] **TASK-P2-DB-COSMOS-01:** Implement Cosmos DB repositories (`CosmosDbArtifactMetadataRepository`, `CosmosDbPersonaKnowledgeRepository`) in `Nucleus.Infrastructure`.
    *   [ ] Configure Cosmos DB connection (emulator for local dev, Azure for deployed).
    *   [ ] Implement basic CRUD operations for both repositories.
    *   [ ] Define partition key strategy (e.g., by UserId or TenantId for `PersonaKnowledgeEntry`, by `ArtifactId` or `SourcePlatform` for `ArtifactMetadata`).
*   [ ] **TASK-P2-API-SAVE-01:** Modify `Nucleus.Services.Api` `/api/v1/process` endpoint flow:
    *   After content extraction and initial persona processing (e.g., by a `SaliencePersona` or `AnalysisPersona`):
        *   Create `ArtifactMetadata` record (e.g., with source info, timestamp, initial tags from persona).
        *   Create `PersonaKnowledgeEntry` record (e.g., with persona's initial analysis, extracted text snippets, placeholder for embeddings).
        *   Save both records using the respective repositories.
*   [ ] **TASK-P2-API-EMBED-01:** Define `IEmbeddingGenerationService` interface. (Ref Code: `Nucleus.Abstractions/Interfaces/AI/`)
*   [ ] **TASK-P2-API-EMBED-02:** Implement a basic embedding service client (e.g., using `Microsoft.Extensions.AI.Embeddings` with a configured provider like Azure OpenAI Embeddings or a local model via Ollama if feasible for dev). (Ref Code: `Nucleus.Infrastructure/AI/`)
*   [ ] **TASK-P2-API-EMBED-03:** After saving initial `PersonaKnowledgeEntry`, asynchronously generate embeddings for its text snippets and update the entry.
    *   This might involve a simple background task or a message queue trigger for this phase.

## `ISSUE-P2-PERSONA-DEV-01`: Initial Persona Development (Beyond Echo)

*Primary Location: `Nucleus.Personas`*

*   [ ] **TASK-P2-PER-01:** Design and implement a `SummarizationPersona`:
    *   Input: Extracted text content (Markdown).
    *   Logic: Calls an LLM (via a new `ILanguageModelService` abstraction using `Microsoft.Extensions.AI`) to summarize the content.
    *   Output: Summary text, to be stored in `PersonaKnowledgeEntry.AnalysisData`.
*   [ ] **TASK-P2-PER-02:** Design and implement a `BasicQAPersona`:
    *   Input: User query and extracted text content.
    *   Logic: Calls an LLM with the content and query to generate an answer.
    *   Output: Answer text.
*   [ ] **TASK-P2-API-PERSONA-ROUTE-01:** Enhance `Nucleus.Services.Api` to allow selection of a persona (e.g., based on a header or query parameter for now, or a simple hardcoded default like `SummarizationPersona`).

## `ISSUE-P2-TESTING-01`: Enhanced Testing & Validation

*   [ ] **TASK-P2-TEST-UNIT-01:** Write unit tests for new repository implementations (using mocks for Cosmos DB client).
*   [ ] **TASK-P2-TEST-UNIT-02:** Write unit tests for new personas.
*   [ ] **TASK-P2-TEST-INT-01:** Implement integration tests for the `Nucleus.Services.Api` `/api/v1/process` endpoint, verifying data persistence in Cosmos DB (using emulator).
*   [ ] **TASK-P2-TEST-E2E-01:** Expand manual E2E tests to verify summarization and basic QA with file content from Teams, and calendar queries.

## `ISSUE-P2-CONFIG-ASPIRE-01`: Aspire Integration & Configuration

*Primary Location: Nucleus Solution Level, `Nucleus.AppHost`*

*   [ ] **TASK-P2-ASPIRE-01:** Ensure all services (`M365Agent`, `MCP.Tools.*`, `Services.Api`) are part of the .NET Aspire AppHost for local development orchestration.
*   [ ] **TASK-P2-ASPIRE-02:** Configure service discovery and environment variables for inter-service communication (e.g., Agent calling API, API calling MCP Tools if that pattern emerges, API using Cosmos DB).
*   [ ] **TASK-P2-ASPIRE-03:** Integrate Cosmos DB emulator into the Aspire manifest.
*   [ ] **TASK-P2-ASPIRE-04:** Ensure configuration for LLM providers (API keys, endpoints for embeddings and language models) is handled securely (e.g., user secrets, Key Vault for deployed environments).

---
