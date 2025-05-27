---
title: "Nucleus - Development Phases (Post-M365 Agent SDK Pivot)"
description: "Outlines the planned development phases for Nucleus, now centered on building Microsoft 365 Persona Agents and backend Model Context Protocol (MCP) Tool/Server applications."
version: 3.0
date: 2025-05-27
see_also:
  - title: "Project Mandate & Vision"
    link: "./00_REQUIREMENTS_PROJECT_MANDATE.md"
---

# Nucleus - Development Phases (Post-M365 Agent SDK Pivot)

This document outlines the planned development phases for Nucleus, structured similarly to JIRA Epics and Issues. It aligns with the refactored requirements reflecting the adoption of the **Microsoft 365 Agents SDK** and **Model Context Protocol (MCP)**.

---

## Phase 1: MVP - Core M365 Agent & Backend MCP Tool Foundation

**Epic:** `EPIC_PHASE1_MVP_FOUNDATION` - Establish the minimum viable **Nucleus M365 Persona Agent application** (e.g., `BootstrapperNucleusAgent`) and the essential backend **Nucleus MCP Tool/Server applications** (`Nucleus_KnowledgeStore_McpServer` for Cosmos DB, `Nucleus_FileAccess_McpServer` for local file `IArtifactProvider`). Validate basic interaction via M365 Agents Playground or Teams, and ensure MCP tool communication. Set up foundational architecture for this distributed system using .NET Aspire for local orchestration.

*See also: [Requirements: Phase 1 - MVP Foundation](./01_REQUIREMENTS_PHASE1_MVP_FOUNDATION.md)*

*   **Issue:** `ISSUE-MVP-SETUP-001`: Establish Core Project Structure for M365 Agents & MCP Tools (New `src/Nucleus.Agents/*`, `src/Nucleus.McpTools/*` projects).
*   **Issue:** `ISSUE-MVP-ABSTRACTIONS-001`: Refine `Nucleus.Abstractions` (Review DTOs, Interfaces for M365/MCP context).
*   **Issue:** `ISSUE-MVP-AGENT-BOOTSTRAP-001`: Develop `BootstrapperNucleusAgent` M365 Agent application (basic `Activity` handling, `IPersonaRuntime` integration with `BootstrapperPersonaConfiguration`).
*   **Issue:** `ISSUE-MVP-MCPTOOL-KNOWLEDGESTORE-001`: Develop `Nucleus_KnowledgeStore_McpServer` (MCP Tool exposing `IArtifactMetadataRepository` & `IPersonaKnowledgeRepository` for Cosmos DB).
*   **Issue:** `ISSUE-MVP-MCPTOOL-FILEACCESS-001`: Develop `Nucleus_FileAccess_McpServer` (MCP Tool exposing `IArtifactProvider` for local files initially).
*   **Issue:** `ISSUE-MVP-AGENT-MCP-INTEG-001`: Implement MCP client logic in `BootstrapperNucleusAgent` to call `KnowledgeStoreMCP` and `FileAccessMCP`.
*   **Issue:** `ISSUE-MVP-ASPIRE-SETUP-001`: Configure `.NET Aspire AppHost` to orchestrate the `BootstrapperNucleusAgent`, backend MCP Tools, and Cosmos DB emulator locally.
*   **Issue:** `ISSUE-MVP-CONFIG-STATIC-001`: Implement static configuration loading (Azure App Config/Key Vault via Aspire) for Agents & MCP Tools (LLM keys, MCP endpoints).
*   **Issue:** `ISSUE-MVP-INFRA-AZURE-001`: Define basic Azure IaC (Bicep via `azd`) for deploying one M365 Agent, core MCP Tools, and Cosmos DB.

---

## Phase 2: Enhanced M365 Agent Capabilities & M365 Channel Integration

**Epic:** `EPIC_PHASE2_ENHANCEMENTS` - Fully integrate a lead Nucleus M365 Persona Agent (e.g., `EduFlowNucleusAgent`) with Microsoft Teams and M365 Copilot. Implement robust asynchronous processing and proactive replies. Enhance backend MCP Tools.

*See also: [Requirements: Phase 2 - Enhancements](./02_REQUIREMENTS_PHASE2_ENHANCEMENTS.md)*

*   **Issue:** `ISSUE-M365-AGENT-EDUFLOW-001`: Develop `EduFlowNucleusAgent` M365 Agent application.
*   **Issue:** `ISSUE-M365-AGENT-TEAMS-COPILOT-001`: Deploy and validate `EduFlowNucleusAgent` in Microsoft Teams and M365 Copilot (if feasible for custom engine agents).
*   **Issue:** `ISSUE-M365-AGENT-FILEHANDLER-001`: Implement robust M365 file handling: M365 Agent gets file info from SDK, passes `ArtifactReference` to `Nucleus_FileAccess_McpServer` (which uses Graph API `IArtifactProvider`).
*   **Issue:** `ISSUE-M365-AGENT-ASYNC-001`: Implement `IBackgroundTaskQueue` (Azure Service Bus) integration for M365 Agents to offload long tasks.
*   **Issue:** `ISSUE-M365-AGENT-PROACTIVE-001`: Implement proactive replies from background workers via M365 Agents (using `ConversationReference`).
*   **Issue:** `ISSUE-M365-MCPTOOL-FILEACCESS-GRAPH-001`: Enhance `Nucleus_FileAccess_McpServer` with `IArtifactProvider` for Microsoft Graph (SharePoint/OneDrive).
*   **Issue:** `ISSUE-M365-MCPTOOL-CONTENTPROC-001`: Develop `Nucleus_ContentProcessing_McpServer` (for `IContentExtractor` logic and Markdown synthesis from complex types).
*   **Issue:** `ISSUE-M365-AUTH-ENTRA-ID-001`: Implement Microsoft Entra Agent ID for M365 Agents and secure Agent-to-MCP-Tool authentication using Entra ID tokens.

---

## Phase 3: Advanced Persona Intelligence & Backend Sophistication

**Epic:** `EPIC_PHASE3_ADVANCED_INTELLIGENCE` - Develop sophisticated agentic behaviors within M365 Persona Agents (leveraging `IPersonaRuntime` and `IAgenticStrategyHandler`), implement advanced RAG (4R ranking via `Nucleus_RAGPipeline_McpServer`), enhance dynamic `PersonaConfiguration` via `Nucleus_PersonaBehaviourConfig_McpServer`, and add caching/testing.

*See also: [Requirements: Phase 3 - Advanced Intelligence](./03_REQUIREMENTS_PHASE3_ADVANCED_INTELLIGENCE.md)*

*   **Issue:** `ISSUE-ADV-AGENT-LOGIC-001`: Implement advanced agentic strategies (`MultiStepReasoning`, `ToolUsing` via MCP) within `IPersonaRuntime` for M365 Agents.
*   **Issue:** `ISSUE-ADV-MCPTOOL-RAG-001`: Develop `Nucleus_RAGPipeline_McpServer` implementing 4R ranking and hybrid search, calling `KnowledgeStoreMCP`.
*   **Issue:** `ISSUE-ADV-MCPTOOL-PERSONACONFIG-DB-001`: Develop `Nucleus_PersonaBehaviourConfig_McpServer` for dynamic `PersonaConfiguration` (prompts, etc.) from Cosmos DB.
*   **Issue:** `ISSUE-ADV-AGENT-DYNAMIC-CONFIG-001`: Enable M365 Agents to load and use dynamic behavioral configurations via `PersonaBehaviourConfigMCP`.
*   **Issue:** `ISSUE-ADV-METADATA-EXTRACTION-001`: Implement advanced metadata extraction (Entities, Keywords) as capabilities within an MCP Tool or M365 Agent logic using configured LLMs.
*   **Issue:** `ISSUE-ADV-CACHE-001`: Implement `ICacheManagementService` for caching LLM calls or MCP Tool responses within M365 Agents or MCP Tools.
*   **Issue:** `ISSUE-ADV-TESTING-SYSTEM-001`: Establish comprehensive system integration testing for the distributed system using .NET Aspire (`Aspire.Hosting.Testing`).
*   **Issue:** `ISSUE-ADV-MULTI-LLM-CONFIG-001`: Ensure M365 Agents and relevant MCP Tools can be configured for different LLM providers (Azure OpenAI, Gemini, OpenRouter).

---

## Phase 4: Platform Maturity & Enterprise Readiness

**Epic:** `EPIC_PHASE4_PLATFORM_MATURITY` - Focus on reliability, scalability, security hardening (Entra Agent ID), observability of the distributed system, advanced workflow orchestration for background tasks, richer M365 Agent UI, and enterprise admin features.

*See also: [Requirements: Phase 4 - Platform Maturity](./04_REQUIREMENTS_PHASE4_PLATFORM_MATURITY.md)*

*   **Issue:** `ISSUE-MAT-OBSERVABILITY-001`: Implement comprehensive distributed tracing and monitoring across M365 Agents, MCP Tools, Service Bus, and Cosmos DB.
*   **Issue:** `ISSUE-MAT-SECURITY-HARDENING-001`: Conduct security review of M365 Agents, MCP Tools, Entra Agent ID usage, and inter-service authentication. Implement hardening.
*   **Issue:** `ISSUE-MAT-PERF-SCALE-001`: Performance analysis and optimization for M365 Agents and MCP Tools. Configure scaling for Azure Container Apps/App Services.
*   **Issue:** `ISSUE-MAT-WORKFLOW-ORCH-001`: Implement advanced workflow orchestration (e.g., Azure Durable Functions triggered by Service Bus worker) for complex, multi-step asynchronous backend tasks involving multiple MCP tool calls.
*   **Issue:** `ISSUE-MAT-AGENT-UI-RICH-001`: Implement enhanced M365 platform interactions (Adaptive Cards in Teams, etc.) by M365 Persona Agents.
*   **Issue:** `ISSUE-MAT-ADMIN-FEATURES-001`: Develop admin capabilities (e.g., via a dedicated Admin M365 Agent or a simple web UI calling admin-focused MCP Tools) for managing `PersonaConfiguration` (dynamic parts), monitoring, user management (if applicable beyond Entra ID).
*   **Issue:** `ISSUE-MAT-ISV-PUBLISHING-001`: Investigate and implement steps for publishing Nucleus M365 Persona Agents to Microsoft Agent Store/AppSource (multi-tenant app registration, validation).
*   **Issue:** `ISSUE-MAT-DOCS-FINAL-001`: Finalize all developer and user documentation for the pivoted architecture.
*   **Issue:** `ISSUE-MAT-BACKUP-RECOVERY-DB-001`: Define & Test Backup/Recovery for Nucleus Cosmos DB.
*   **Issue:** `ISSUE-MAT-DEPLOY-AUTOMATION-FULL-001`: Achieve comprehensive deployment automation (IaC via `azd` and Bicep) for the entire distributed Nucleus platform.

---
