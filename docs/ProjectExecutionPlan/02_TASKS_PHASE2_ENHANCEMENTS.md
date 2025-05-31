---
title: "Phase 2 Tasks: M365 Agent Enhancements & Channel Integration"
description: "Detailed tasks for Nucleus Phase 2, focusing on enhancing the M365 Agent with full Teams channel integration, Microsoft Graph-powered file access, a new Content Processing MCP Tool, asynchronous task processing, and Entra ID security."
version: 2.0
date: 2025-05-27
parent: ./00_TASKS_ROADMAP.md
epic: ./00_TASKS_ROADMAP.md#epic_phase2_enhancements
requirements: ./02_REQUIREMENTS_PHASE2_ENHANCEMENTS.md
status: "Defined"
---

## Phase 2 Tasks: M365 Agent Enhancements & Channel Integration

This document outlines the specific development tasks required to deliver the Phase 2 Enhancements. These tasks are derived from the [Phase 2 Requirements](./02_REQUIREMENTS_PHASE2_ENHANCEMENTS.md) and are aligned with the [Project Roadmap](./00_TASKS_ROADMAP.md#epic_phase2_enhancements).

---

### `ISSUE-P2-AGENT-ENH-01`: Enhanced M365 Agent Capabilities (`EduFlowNucleusAgent`)

*   **Title:** Enhanced M365 Agent Capabilities (`EduFlowNucleusAgent`)
*   **Description:** Implement advanced features for the lead M365 Agent for Phase 2, `EduFlowNucleusAgent`, focusing on deeper Teams integration, asynchronous operations, and richer user interactions.
*   **Primary Location:** `Nucleus.Agents.EduFlow` (New or Enhanced Project for P2 Lead Agent)
*   **Depends On:** Phase 1 MVP Agent, `ISSUE-P2-ASYNC-INFRA-001`
*   **Effort:** High
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-AGENT-01:** Implement robust conversation management using M365 Agent SDK state capabilities for `EduFlowNucleusAgent`.
    *   [ ] **TASK-P2-AGENT-02:** Enhance error handling and provide clear user feedback within Teams for `EduFlowNucleusAgent` operations.
    *   [ ] **TASK-P2-AGENT-03:** Design `EduFlowNucleusAgent` to orchestrate calls to multiple MCP Tools as required by its persona logic (e.g., `FileAccessMCP`, `ContentProcessingMCP`, `KnowledgeStoreMCP`).
    *   [ ] **TASK-P2-AGENT-04:** Implement Adaptive Card usage in `EduFlowNucleusAgent` for presenting complex information and soliciting structured input (REQ-P2-AGENT-005).
    *   [ ] **TASK-P2-AGENT-05:** Refine and document configuration for `EduFlowNucleusAgent`, including persona-specific settings, MCP tool endpoints, and feature flags.
    *   [ ] **TASK-P2-AGENT-06:** Implement full Microsoft Teams channel integration for `EduFlowNucleusAgent`, enabling it to handle messages, mentions, and file sharing events within channels (REQ-P2-AGENT-001).
    *   [ ] **TASK-P2-AGENT-07 (Stretch):** Deploy and validate basic `EduFlowNucleusAgent` functionality within the M365 Copilot environment (REQ-P2-AGENT-006).
    *   [ ] **TASK-P2-AGENT-08:** Integrate `IBackgroundTaskQueue` (Azure Service Bus implementation) into `EduFlowNucleusAgent` for offloading long-running tasks like content processing (REQ-P2-AGENT-003).
    *   [ ] **TASK-P2-AGENT-09:** Implement proactive messaging capabilities in `EduFlowNucleusAgent` using stored `ConversationReference`s to notify users/channels upon completion of background tasks (REQ-P2-AGENT-004).

---

### `ISSUE-P2-AGENT-EDUFLOW-LOGIC-001`: Implement Core `EduFlowNucleusAgent` Persona Logic

*   **Title:** Implement Core `EduFlowNucleusAgent` Persona Logic
*   **Description:** Develop the specific business logic and intelligence for the `EduFlowNucleusAgent` persona, focusing on its educational assistance capabilities.
*   **Primary Location:** `Nucleus.Agents.EduFlow` (and its dependent core persona logic libraries, e.g., `Nucleus.Domain.Personas.EduFlow`)
*   **Depends On:** `ISSUE-P2-AGENT-ENH-01`, `ISSUE-P2-MCPCP-DEV-001`
*   **Effort:** Medium
*   **Priority:** High
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-PER-01:** Design and implement summarization capabilities within `EduFlowNucleusAgent` for processed content (potentially calling `ContentProcessingMCP` or `Nucleus_LlmOrchestration_McpServer`).
    *   [ ] **TASK-P2-PER-02:** Design and implement basic Q&A capabilities within `EduFlowNucleusAgent` against processed content and knowledge retrieved from `KnowledgeStoreMCP`.

---

### `ISSUE-P2-MCPFA-GRAPH-001`: Enhance `FileAccessMCP` with Graph Provider

*   **Title:** Enhance `Nucleus_FileAccess_McpServer` with `GraphArtifactProvider`
*   **Description:** Augment the `FileAccessMCP` to support accessing files from SharePoint Online and OneDrive for Business using the Microsoft Graph API.
*   **Primary Location:** `Nucleus.McpTools.FileAccess`, `Nucleus.Infrastructure.FileProviders.Platform`
*   **Depends On:** Phase 1 `FileAccessMCP`
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-MCPFA-GR-01:** Implement `GraphArtifactProvider` within `Nucleus.Infrastructure.FileProviders.Platform` (or a similar shared location) for SharePoint/OneDrive, handling Graph API calls and authentication using the agent's context (REQ-P2-MCPFA-001).
    *   [ ] **TASK-P2-MCPFA-GR-02:** Integrate the `GraphArtifactProvider` into the `Nucleus_FileAccess_McpServer` as a new artifact source.
    *   [ ] **TASK-P2-MCPFA-GR-03:** Write comprehensive unit and integration tests for the `GraphArtifactProvider` and its integration into the MCP tool.

---

### `ISSUE-P2-MCPCP-DEV-001`: Develop `Nucleus_ContentProcessing_McpServer`

*   **Title:** Develop `Nucleus_ContentProcessing_McpServer`
*   **Description:** Create the new MCP Tool responsible for extracting content from various file types and synthesizing standardized Markdown.
*   **Primary Location:** `Nucleus.McpTools.ContentProcessing` (New Project), `Nucleus.Infrastructure.ContentExtractors`
*   **Depends On:** Phase 1 MCP Tool template/abstractions
*   **Effort:** High
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-MCPCP-INIT-01:** Initialize the `Nucleus.McpTools.ContentProcessing` project based on the standard MCP tool architecture.
    *   [ ] **TASK-P2-MCPCP-OPS-01:** Define and implement MCP operations (e.g., `ExtractStructuredTextFromStream`, `SynthesizeMarkdownFromComponents`) according to `REQ-P2-MCPCP-001`.
    *   [ ] **TASK-P2-MCPCP-EXTRACT-01:** Integrate initial `IContentExtractor` implementations (e.g., for DOCX, PDF basic text extraction) from `Nucleus.Infrastructure.ContentExtractors` (REQ-P2-MCPCP-002).
    *   [ ] **TASK-P2-MCPCP-SYNTH-01:** Implement Markdown synthesis logic. This may involve simple concatenation or orchestrating calls to `Nucleus_LlmOrchestration_McpServer` for more complex summarization or formatting (REQ-P2-MCPCP-003).
    *   [ ] **TASK-P2-MCPCP-TEST-01:** Write comprehensive unit and integration tests for the `ContentProcessingMCP` tool.

---

### `ISSUE-P2-SEC-ENTRA-ID-001`: Implement Agent-to-MCP Tool Security

*   **Title:** Implement Entra ID based Agent-to-MCP Tool Security
*   **Description:** Secure communication between M365 Agents and Nucleus MCP Tools using Azure AD tokens.
*   **Primary Location:** M365 Agents (e.g., `Nucleus.Agents.EduFlow`), MCP Tools (e.g., `Nucleus.McpTools.FileAccess`, `Nucleus.McpTools.ContentProcessing`), Azure Portal (App Registrations)
*   **Depends On:** All agent and MCP tool projects.
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-SEC-APPS-01:** Configure Azure AD App Registrations for MCP Tools to expose them as protected APIs (REQ-P2-SEC-002).
    *   [ ] **TASK-P2-SEC-AGENTCLIENT-01:** Implement token acquisition logic (e.g., using MSAL.NET) in M365 Agents (acting as clients) to obtain Azure AD tokens for calling MCP Tools (REQ-P2-SEC-002).
    *   [ ] **TASK-P2-SEC-MCPVALIDATE-01:** Implement Azure AD token validation middleware or logic in all Nucleus MCP Tools to protect their endpoints (REQ-P2-SEC-002).

---

### `ISSUE-P2-ASYNC-INFRA-001`: Implement Asynchronous Processing Infrastructure

*   **Title:** Implement Asynchronous Processing Infrastructure
*   **Description:** Set up and integrate Azure Service Bus for background task processing, including a dedicated worker service.
*   **Primary Location:** `Nucleus.Infrastructure.Messaging` (New or Enhanced Project), `Nucleus.BackgroundWorker.ServiceBus` (New Project), M365 Agents (e.g., `Nucleus.Agents.EduFlow`)
*   **Depends On:** `ISSUE-P2-AGENT-ENH-01`
*   **Effort:** High
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-ASYNC-SB-01:** Set up Azure Service Bus namespace and queue(s) required for asynchronous task processing (REQ-P2-ASYNC-001).
    *   [ ] **TASK-P2-ASYNC-QUEUEIMPL-01:** Implement the `IBackgroundTaskQueue` interface using the Azure Service Bus SDK within `Nucleus.Infrastructure.Messaging` (or a similar shared infrastructure project).
    *   [ ] **TASK-P2-ASYNC-WORKER-01:** Develop the `Nucleus.BackgroundWorker.ServiceBus` project, a dedicated service (e.g., .NET Generic Host service or Azure Function) that listens to the Service Bus queue, dequeues tasks, and processes them by invoking relevant MCP Tools (REQ-P2-ASYNC-001, REQ-P2-ASYNC-002).
    *   [ ] **TASK-P2-ASYNC-PROACTIVE-01:** Integrate the background worker service with the originating M365 Agent (e.g., `EduFlowNucleusAgent`) to trigger proactive replies upon task completion or failure (REQ-P2-ASYNC-003).

---

### `ISSUE-P2-TESTING-01`: Enhanced Testing & Validation for Phase 2

*   **Title:** Enhanced Testing & Validation for Phase 2
*   **Description:** Define and execute tests for all new and enhanced components in Phase 2.
*   **Primary Location:** `tests/` (Unit, Integration, E2E)
*   **Depends On:** All other Phase 2 development tasks.
*   **Effort:** High
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-TEST-AGENT-01:** Write unit and integration tests for `EduFlowNucleusAgent`, covering Teams channel interactions, Adaptive Card usage, async task offloading, and proactive messaging.
    *   [ ] **TASK-P2-TEST-MCPFA-GRAPH-01:** Write unit and integration tests for the `GraphArtifactProvider` in `FileAccessMCP`.
    *   [ ] **TASK-P2-TEST-MCPCP-01:** Write unit and integration tests for the new `ContentProcessingMCP`, including its extractors and synthesis logic.
    *   [ ] **TASK-P2-TEST-SECURITY-01:** Test Entra ID secured communication between `EduFlowNucleusAgent` and MCP Tools.
    *   [ ] **TASK-P2-TEST-ASYNC-01:** Test the end-to-end asynchronous processing flow, including task queuing, worker processing, and proactive replies.
    *   [ ] **TASK-P2-TEST-E2E-01:** Define and execute manual and/or automated E2E test scenarios covering key Phase 2 user stories (e.g., sharing a DOCX in Teams, agent processes it asynchronously, extracts content, stores knowledge, and notifies user with a summary card).

---

### `ISSUE-P2-CONFIG-ASPIRE-01`: Aspire Integration & Configuration for Phase 2

*   **Title:** Aspire Integration & Configuration for Phase 2 Services
*   **Description:** Ensure all new and modified services for Phase 2 are correctly orchestrated by .NET Aspire for local development.
*   **Primary Location:** `Aspire/Nucleus.AppHost`, `Aspire/Nucleus.ServiceDefaults`
*   **Depends On:** All new service/tool projects.
*   **Effort:** Medium
*   **Priority:** High
*   **Status:** To Do
*   **Tasks:**
    *   [ ] **TASK-P2-ASPIRE-01:** Add new projects (`Nucleus.Agents.EduFlow`, `Nucleus.McpTools.ContentProcessing`, `Nucleus.BackgroundWorker.ServiceBus`) to the `Nucleus.AppHost` for Aspire orchestration.
    *   [ ] **TASK-P2-ASPIRE-02:** Configure service discovery, environment variables, and necessary Azure service connections (e.g., Service Bus connection string) for all Phase 2 services within the Aspire AppHost and ServiceDefaults.
    *   [ ] **TASK-P2-ASPIRE-03:** Ensure secure handling of secrets (e.g., Service Bus connection strings, Entra ID app client secrets) for local development using user secrets, and document placeholders for Azure Key Vault integration for deployed environments.

---
