---
title: "Phase 3: Advanced MCP Tools & Semantic Capabilities Tasks"
description: "Detailed tasks for Nucleus Phase 3, focusing on developing advanced MCP Tools, implementing semantic search and memory for the M365 Agent, and refining backend services for richer persona interactions."
version: 1.0
date: 2025-05-26
---

# Phase 3: Advanced MCP Tools & Semantic Capabilities Tasks

**Epic:** [`EPIC-MCP-ADVANCED-SEMANTIC`](./00_ROADMAP.md#phase-3-advanced-mcp-tools--semantic-capabilities)
**Requirements:** [`03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md`](../Requirements/03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md) (Renamed to "Requirements: Advanced Backend Capabilities & Persona Refinement")

This document details the specific tasks required to complete Phase 3, focusing on enhancing the intelligence and capabilities of the Nucleus M365 Agent and its backend.

---

## `ISSUE-P3-MCPTOOL-EMAIL-01`: Develop MCP Tool for User's Email (Outlook)

*Primary Location: `Nucleus.MCP.Tools.Email` (New Project)*

*   [ ] **TASK-P3-MCP-E-01:** Initialize new .NET project for the Email MCP Tool (`Nucleus.MCP.Tools.Email`).
*   [ ] **TASK-P3-MCP-E-02:** Implement an MCP Tool for accessing user's Outlook email content via Microsoft Graph.
    *   [ ] Define tool input: Query (e.g., "summarize my unread emails from today," "find emails from Contoso about Project X").
    *   [ ] Implement logic to use Microsoft Graph API to search/fetch email data.
        *   Handle authentication using the M365 Agent's context/identity.
        *   Ensure appropriate permissions (e.g., `Mail.Read`).
    *   [ ] Define tool output: Standardized representation of email content (summaries, snippets, links).
*   [ ] **TASK-P3-MCP-E-03:** Implement robust filtering and pagination for email queries.
*   [ ] **TASK-P3-MCP-E-04:** Write unit tests for the Email MCP Tool.
*   [ ] **TASK-P3-AGENT-EMAIL-01:** Integrate invocation of the Email MCP Tool into the M365 Agent based on user intent.

## `ISSUE-P3-MCPTOOL-TASKS-01`: Develop MCP Tool for User's ToDo/Planner

*Primary Location: `Nucleus.MCP.Tools.Tasks` (New Project)*

*   [ ] **TASK-P3-MCP-TSK-01:** Initialize new .NET project for the Tasks MCP Tool (`Nucleus.MCP.Tools.Tasks`).
*   [ ] **TASK-P3-MCP-TSK-02:** Implement an MCP Tool for accessing user's Microsoft ToDo or Planner tasks via Microsoft Graph.
    *   [ ] Define tool input: Query (e.g., "what are my tasks due this week?", "create a task to follow up with Fabrikam").
    *   [ ] Implement logic to use Microsoft Graph API to read/create/update tasks.
        *   Handle authentication using the M365 Agent's context/identity.
        *   Ensure appropriate permissions (e.g., `Tasks.ReadWrite`, `Group.ReadWrite.All` for Planner).
    *   [ ] Define tool output: Confirmation of actions, list of tasks.
*   [ ] **TASK-P3-MCP-TSK-03:** Write unit tests for the Tasks MCP Tool.
*   [ ] **TASK-P3-AGENT-TASKS-01:** Integrate invocation of the Tasks MCP Tool into the M365 Agent.

## `ISSUE-P3-SEMANTIC-MEMORY-01`: Implement Semantic Memory & Retrieval for Agent

*Primary Location: `Nucleus.Services.Api`, `Nucleus.Adapters.M365Agent`*

*   [ ] **TASK-P3-API-RETRIEVE-01:** `Nucleus.Services.Api`: Develop a new API endpoint (e.g., `/api/v1/retrieve`) for semantic search over persisted `PersonaKnowledgeEntry` items.
    *   Input: User query text, UserID/TenantID, optional filters (date, source).
    *   Logic: Performs vector search on `PersonaKnowledgeEntry.Embeddings` and combines with metadata filtering.
    *   Output: Ranked list of relevant `PersonaKnowledgeEntry` summaries or snippets.
*   [ ] **TASK-P3-API-RETRIEVE-02:** Implement the 4R ranking (Recency, Relevancy, Richness, Reputation - if `Richness` and `Reputation` are available) for search results in the API.
*   [ ] **TASK-P3-AGENT-SEMANTIC-01:** `Nucleus.Adapters.M365Agent`: Before calling domain-specific MCP tools or LLMs for a response, the agent should first call the `/api/v1/retrieve` endpoint with the current user query to fetch relevant past knowledge.
*   [ ] **TASK-P3-AGENT-SEMANTIC-02:** The agent should then incorporate this retrieved knowledge into the context provided to subsequent MCP tool calls or LLM prompts to provide more informed and personalized responses.
*   [ ] **TASK-P3-AGENT-SEMANTIC-03:** Store conversation history snippets (user query + agent response) as `PersonaKnowledgeEntry` items via the `/api/v1/process` endpoint to build up the semantic memory over time.
    *   Ensure these are distinguishable from document-derived knowledge (e.g., via `ArtifactMetadata.SourceType`).

## `ISSUE-P3-PERSONA-ADV-01`: Advanced Persona Logic & Tool Orchestration

*Primary Location: `Nucleus.Personas`, `Nucleus.Services.Api`*

*   [ ] **TASK-P3-PER-ORCH-01:** Refine `IPersona` implementations (e.g., `EduFlowOmniEducatorPersona`, `BusinessKnowledgeAssistantPersona`) to intelligently decide when to use semantic memory vs. calling specific MCP Tools vs. direct LLM calls.
    *   This involves developing more sophisticated prompt engineering or a basic planning step within the persona logic.
*   [ ] **TASK-P3-PER-ORCH-02:** Personas should be able to synthesize information from multiple sources (semantic memory + MCP tool output + current query context) to generate comprehensive answers.
*   [ ] **TASK-P3-API-LLM-01:** Enhance `ILanguageModelService` to support more advanced LLM features if needed (e.g., function calling if the chosen LLM supports it and it simplifies tool integration from the API side).
*   [ ] **TASK-P3-CONFIG-PERSONA-01:** Implement configuration options for personas (e.g., preferred tools, retrieval settings, specific prompts) loaded via `Microsoft.Extensions.Options`.

## `ISSUE-P3-BACKEND-ENH-01`: Backend Enhancements (Logging, Monitoring, Security)

*Primary Location: `Nucleus.Services.Api`, `Nucleus.Infrastructure`, Aspire AppHost*

*   [ ] **TASK-P3-LOG-01:** Implement structured logging with correlation IDs across M365 Agent, MCP Tools (if they call other services), and `Nucleus.Services.Api`.
*   [ ] **TASK-P3-MONITOR-01:** Set up basic monitoring and dashboards in Azure Application Insights (or chosen APM) for API endpoints, MCP tool invocations (if instrumented), and database performance (Cosmos DB RU/s).
*   [ ] **TASK-P3-SEC-01:** Review and harden authentication/authorization for API endpoints and between Agent/MCP Tools.
    *   Ensure MCP Tools validate callers (e.g., expected M365 Agent App ID).
*   [ ] **TASK-P3-SEC-02:** Ensure sensitive configuration (API keys, connection strings) is stored securely (e.g., Azure Key Vault) and accessed via managed identities where possible.

## `ISSUE-P3-TESTING-01`: Comprehensive Testing

*   [ ] **TASK-P3-TEST-MCP-01:** Write unit and integration tests for all new MCP Tools.
*   [ ] **TASK-P3-TEST-API-RETRIEVE-01:** Write integration tests for the `/api/v1/retrieve` endpoint.
*   [ ] **TASK-P3-TEST-AGENT-SEMANTIC-01:** Develop test cases (manual or automated if feasible) for verifying semantic memory recall and its impact on agent responses.
*   [ ] **TASK-P3-TEST-PERSONA-01:** Write unit tests for new persona decision-making logic.

---
