---
title: "Phase 1: M365 Agent & MCP Tools MVP - Core Integration Tasks"
description: "Detailed tasks for implementing the Minimum Viable Product (MVP) of Nucleus, focusing on core Microsoft 365 Agent SDK integration, Model Context Protocol (MCP) tool development for Teams, and foundational backend services."
version: 1.0
date: 2025-05-25
---

# Phase 1: M365 Agent & MCP Tools MVP - Core Integration Tasks

**Epic:** [`EPIC-MVP-M365-MCP`](./00_ROADMAP.md#phase-1-mvp---m365-agent--mcp-tools-integration)
**Requirements:** [`02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`](../Requirements/02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md) (Note: This was the closest match after re-titling, effectively the new Phase 1 requirements)

This document details the specific tasks required to complete the M365 Agent & MCP Tools MVP.

---

## `ISSUE-MVP-AGENTSDK-01`: Core M365 Agent SDK Integration & Teams Bot

*Primary Location: `Nucleus.Adapters.M365Agent` (New Project)*

*   [ ] **TASK-P1-AGENT-01:** Initialize new .NET project for the M365 Agent (`Nucleus.Adapters.M365Agent`).
*   [ ] **TASK-P1-AGENT-02:** Integrate the Microsoft 365 Agent SDK.
*   [ ] **TASK-P1-AGENT-03:** Configure the agent to operate as a Microsoft Teams Bot.
    *   [ ] Set up Azure Bot registration.
    *   [ ] Configure necessary permissions for Teams (e.g., message read, file access via RSC).
*   [ ] **TASK-P1-AGENT-04:** Implement basic message handling to receive user queries from Teams.
*   [ ] **TASK-P1-AGENT-05:** Implement basic response mechanism to send simple text replies back to Teams.
*   [ ] **TASK-P1-AGENT-06:** Set up local development and debugging environment for the M365 Agent with Teams (e.g., using Dev Tunnels or ngrok).
*   [ ] **TASK-P1-AGENT-07:** Define basic application configuration (e.g., `appsettings.json`) for the agent.

## `ISSUE-MVP-MCPTOOL-TEAMS-01`: Develop MCP Tool for Teams File Context

*Primary Location: `Nucleus.MCP.Tools.Teams` (New Project)*

*   [ ] **TASK-P1-MCP-T-01:** Initialize new .NET project for the Teams MCP Tool (`Nucleus.MCP.Tools.Teams`).
*   [ ] **TASK-P1-MCP-T-02:** Implement an MCP Tool that can be invoked by the M365 Agent.
*   [ ] **TASK-P1-MCP-T-03:** Tool Focus: Accessing File Content from Teams.
    *   [ ] Define tool input: `ArtifactReference` (or similar) specifying a file in Teams (e.g., from a message attachment, SharePoint link).
    *   [ ] Implement logic to use Microsoft Graph API to fetch file content ephemerally based on the input reference.
        *   Handle authentication using the M365 Agent's context/identity.
        *   Ensure only file *content* is retrieved, not persisted by the tool.
    *   [ ] Define tool output: Standardized Markdown representation of the file content (or a reference to it if too large, TBD).
*   [ ] **TASK-P1-MCP-T-04:** Implement error handling for file access issues (permissions, file not found, etc.).
*   [ ] **TASK-P1-MCP-T-05:** Write unit tests for the MCP Tool's core logic.

## `ISSUE-MVP-BACKEND-API-01`: Foundational Backend API Service

*Primary Location: `Nucleus.Services.Api` (Existing, to be refactored/enhanced)*

*   [ ] **TASK-P1-API-01:** Define a minimal API endpoint (e.g., `/api/v1/process`) that the M365 Agent can call.
    *   Input: User query, context (e.g., conversation ID), and potentially content from MCP Tool.
    *   Output: Processed result or analysis.
*   [ ] **TASK-P1-API-02:** Implement basic request validation.
*   [ ] **TASK-P1-API-03:** Set up `Nucleus.Services.Api` to be invokable by the M365 Agent (e.g., ensure it's hosted and accessible).
*   [ ] **TASK-P1-API-04:** Define `IContentExtractor` interface (if not already suitable from previous plans). (Ref Code: `Nucleus.Abstractions/Interfaces/`)
*   [ ] **TASK-P1-API-05:** Implement a basic `MarkdownContentExtractor` for processing Markdown provided by the MCP Tool. (Ref Code: `Nucleus.Processing/Extractors/`)
*   [ ] **TASK-P1-API-06:** Define `IPersona` interface (if not already suitable). (Ref Code: `Nucleus.Abstractions/Interfaces/`)
*   [ ] **TASK-P1-API-07:** Implement a simple `EchoPersona` that takes extracted content and returns it, perhaps with a prefix. (Ref Code: `Nucleus.Personas/`)
    *   This persona will be used for initial end-to-end testing.
*   [ ] **TASK-P1-API-08:** Integrate `IContentExtractor` and `IPersona` into the `/api/v1/process` endpoint flow.

## `ISSUE-MVP-ORCHESTRATION-01`: Agent-Tool-API Orchestration (Initial)

*Primarily within `Nucleus.Adapters.M365Agent` and `Nucleus.Services.Api`*

*   [ ] **TASK-P1-ORCH-01:** M365 Agent: When a user query involving a file is received:
    *   [ ] Invoke the `Nucleus.MCP.Tools.Teams` file access tool.
    *   [ ] Receive standardized Markdown (or reference) from the tool.
*   [ ] **TASK-P1-ORCH-02:** M365 Agent: Call the `Nucleus.Services.Api` `/api/v1/process` endpoint.
    *   [ ] Pass the user query and the Markdown content from the MCP tool.
*   [ ] **TASK-P1-ORCH-03:** M365 Agent: Receive the response from the API.
*   [ ] **TASK-P1-ORCH-04:** M365 Agent: Send the API's response back to the user in Teams.
*   [ ] **TASK-P1-ORCH-05:** Implement basic error handling and logging for this flow.

## `ISSUE-MVP-DB-SETUP-01`: Minimal Database Setup (No Data Persistence Yet)

*Primary Location: `Nucleus.Data` (Existing, for schema definition)*

*   [ ] **TASK-P1-DB-01:** Define initial C# record structures for `ArtifactMetadata` and `PersonaKnowledgeEntry` (even if not persisted in MVP). (Ref Code: `Nucleus.Core/Models/`)
    *   Focus on fields relevant for future use, but no database interaction in this phase.
*   [ ] **TASK-P1-DB-02:** No actual database setup or repository implementation required for MVP. This is to prepare for Phase 2.

## `ISSUE-MVP-TESTING-01`: Basic End-to-End Testing

*   [ ] **TASK-P1-TEST-01:** Manually test the flow: User sends a message with a file in Teams -> M365 Agent invokes MCP Tool -> MCP Tool gets file content -> Agent calls API -> API processes with EchoPersona -> Agent sends response to Teams.
*   [ ] **TASK-P1-TEST-02:** Document setup steps for local E2E testing.

## `ISSUE-MVP-DOCS-01`: Initial Developer Documentation

*   [ ] **TASK-P1-DOCS-01:** Create README files for the new `Nucleus.Adapters.M365Agent` and `Nucleus.MCP.Tools.Teams` projects.
*   [ ] **TASK-P1-DOCS-02:** Document the basic MVP architecture and data flow in `Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md` (or a new M365-specific architecture doc).

---
