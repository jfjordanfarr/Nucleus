---
title: "Tasks: Phase 1 - MVP Foundation for M365 Agents & MCP Tools"
description: "Defines the core development tasks for establishing the Minimum Viable Product (MVP) of Nucleus, focusing on the M365 Agent Bootstrapper and essential Model Context Protocol (MCP) Tools for File Access and Knowledge Storage."
version: 2.0
date: 2025-05-27
parent: ./00_TASKS_ROADMAP.md
epic: ../../00_TASKS_ROADMAP.md#epic_phase1_mvp_foundation
requirements: ./01_REQUIREMENTS_PHASE1_MVP_FOUNDATION.md
see_also:
  - ../../Architecture/Agents/01_M365_AGENTS_OVERVIEW.md
  - ../../Architecture/McpTools/01_MCP_TOOLS_OVERVIEW.md
  - ../../Architecture/McpTools/KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md
  - ../../Architecture/McpTools/FileAccess/ARCHITECTURE_MCPTOOL_FILE_ACCESS.md
status: "Defined"
---

## Phase 1 Tasks: MVP Foundation - M365 Agents & MCP Tools

This document outlines the specific development tasks required to deliver the Phase 1 MVP. These tasks are derived from the [Phase 1 MVP Requirements](./01_REQUIREMENTS_PHASE1_MVP_FOUNDATION.md) and are aligned with the [Project Roadmap](../00_TASKS_ROADMAP.md#epic_phase1_mvp_foundation).

The primary goal of this phase is to establish a foundational, end-to-end working system that demonstrates:
1.  A bootstrapper M365 Agent capable of basic operation.
2.  Essential MCP Tools for local file access and knowledge storage (using Cosmos DB Emulator).
3.  The interaction between the Agent and these MCP Tools.
4.  Orchestration via DotNet Aspire for local development.

---

### `ISSUE-MVP-ASPIRE-SETUP-001`: Infrastructure: DotNet Aspire Orchestration Setup

*   **Title:** Infrastructure: DotNet Aspire Orchestration Setup
*   **Description:** Establish the DotNet Aspire application host and service defaults projects to orchestrate the various Nucleus services for local development. This is critical for managing dependencies and simplifying the developer experience.
*   **Primary Location:** `Nucleus.AppHost`, `Nucleus.ServiceDefaults`
*   **Depends On:** None
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Create `Nucleus.AppHost` project for Aspire orchestration.
    2.  Create `Nucleus.ServiceDefaults` project for common service configurations (logging, telemetry placeholders, health checks).
    3.  Configure `Nucleus.Agents.Bootstrapper`, `Nucleus.McpTools.FileAccess`, and `Nucleus.McpTools.KnowledgeStore` as resources within the `Program.cs` of `Nucleus.AppHost`.
    4.  Ensure basic service discovery and inter-service communication setup for local development (e.g., environment variables for service endpoints injected by Aspire).
    5.  Verify that all services can be launched and managed via the Aspire Dashboard.

---

### `ISSUE-MVP-AGENT-BOOTSTRAPPER-001`: M365 Agent: Bootstrapper Nucleus Agent Implementation

*   **Title:** M365 Agent: Bootstrapper Nucleus Agent Implementation
*   **Description:** Develop the initial M365 Agent, `BootstrapperNucleusAgent`. This agent will serve as the entry point for user interactions (simulated for MVP) and will orchestrate calls to MCP Tools. It will implement the core `IAgent` interface from the M365 Agent SDK.
*   **Primary Location:** `Nucleus.Agents.Bootstrapper` (New Project)
*   **Depends On:** `ISSUE-MVP-ASPIRE-SETUP-001`
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Create the `Nucleus.Agents.Bootstrapper` project.
    2.  Define `BootstrapperNucleusAgent` class implementing `Microsoft.Teams.AI.Agent.IAgent<AgentOptions>` (or equivalent M365 Agent SDK base).
    3.  Implement basic `IPersonaRuntime` integration for agent startup, command handling (e.g., a simple 'ping', 'status', or 'process-file <path>' command), and graceful shutdown.
    4.  Integrate with `Microsoft.Extensions.Hosting` for service lifecycle management within the Aspire ecosystem.
    5.  Setup basic configuration loading for agent settings (e.g., from `appsettings.json`).

---

### `ISSUE-MVP-MCPTOOL-FILEACCESS-001`: MCP Tool: Local File Access Provider

*   **Title:** MCP Tool: Local File Access Provider
*   **Description:** Develop the `FileAccessMCP` tool, responsible for reading content and metadata from local files. This tool will implement the `IArtifactProvider` interface and will be invoked by the `BootstrapperNucleusAgent`. For MVP, this focuses on local file system access, not platform-specific APIs (like Microsoft Graph).
*   **Primary Location:** `Nucleus.McpTools.FileAccess` (New Project)
*   **Depends On:** `ISSUE-MVP-ASPIRE-SETUP-001`
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Create the `Nucleus.McpTools.FileAccess` project.
    2.  Define `LocalFileArtifactProvider` class implementing `Nucleus.Abstractions.Models.Mcp.IArtifactProvider`.
    3.  Implement methods to read file content and basic metadata (name, path, size, last modified) from a local file path specified in an `ArtifactMetadata` input.
    4.  Ensure it handles basic plain text file types (e.g., `.txt`, `.md`) and provides the content as a `RawContentArtifact`.
    5.  Implement basic error handling for scenarios like file not found or access denied.
    6.  Define the MCP service endpoint and integrate with Aspire for service discovery.

---

### `ISSUE-MVP-MCPTOOL-KNOWLEDGESTORE-001`: MCP Tool: KnowledgeStore (Cosmos DB Emulator)

*   **Title:** MCP Tool: KnowledgeStore (Cosmos DB Emulator)
*   **Description:** Develop the `KnowledgeStoreMCP` tool, responsible for persisting and retrieving `ArtifactMetadata` and `PersonaKnowledgeEntry` objects. For MVP, this will use the Azure Cosmos DB Emulator for local development.
*   **Primary Location:** `Nucleus.McpTools.KnowledgeStore` (New Project)
*   **Depends On:** `ISSUE-MVP-ASPIRE-SETUP-001`
*   **Effort:** High
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Create the `Nucleus.McpTools.KnowledgeStore` project.
    2.  Define `KnowledgeStoreProvider` class implementing `Nucleus.Abstractions.Models.Mcp.IKnowledgeProvider`.
    3.  Implement methods for CRUD (Create, Read, Update, Delete) operations on `ArtifactMetadata` and `PersonaKnowledgeEntry<string>` (using a simple string for `Content` in `PersonaKnowledgeEntry` for MVP).
    4.  Configure and integrate with the Azure Cosmos DB Emulator (NoSQL API). This includes setting up the database, containers, and partition keys.
    5.  Implement basic indexing strategies for `ArtifactMetadata` (e.g., by `ArtifactId`, `SourceUri`) and `PersonaKnowledgeEntry` (e.g., by `EntryId`, `ArtifactId`).
    6.  Define the MCP service endpoint and integrate with Aspire for service discovery.
    7.  Ensure appropriate data models and serialization for Cosmos DB.

---

### `ISSUE-MVP-AGENT-MCP-INTEG-001`: Agent-MCP Integration: Bootstrapper Agent with MCP Tools

*   **Title:** Agent-MCP Integration: Bootstrapper Agent with MCP Tools
*   **Description:** Implement the communication and orchestration logic within the `BootstrapperNucleusAgent` to invoke the `FileAccessMCP` and `KnowledgeStoreMCP` tools. This involves making HTTP calls to the MCP services discovered via Aspire.
*   **Primary Location:** `Nucleus.Agents.Bootstrapper`
*   **Depends On:** `ISSUE-MVP-AGENT-BOOTSTRAPPER-001`, `ISSUE-MVP-MCPTOOL-FILEACCESS-001`, `ISSUE-MVP-MCPTOOL-KNOWLEDGESTORE-001`
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Implement typed HTTP client services (e.g., using `IHttpClientFactory`) within `BootstrapperNucleusAgent` to communicate with `FileAccessMCP` and `KnowledgeStoreMCP`.
    2.  Implement logic within `BootstrapperNucleusAgent` (e.g., in a command handler) to:
        *   Receive a local file path as input.
        *   Construct an `ArtifactMetadata` object for the file.
        *   Invoke `FileAccessMCP` to retrieve the file's content.
        *   (Placeholder for MVP) Create a simple `PersonaKnowledgeEntry<string>` based on the file content (e.g., "Processed file: [filename]").
        *   Invoke `KnowledgeStoreMCP` to save both the `ArtifactMetadata` and the `PersonaKnowledgeEntry`.
    3.  Define simple Data Transfer Objects (DTOs) or use existing models for requests and responses between the agent and MCP tools if not directly using `ArtifactMetadata` or `PersonaKnowledgeEntry` over the wire.
    4.  Implement basic error handling and logging for MCP tool invocations.

---

### `ISSUE-MVP-CONFIG-STATIC-001`: Configuration: Static Configuration for MVP Services

*   **Title:** Configuration: Static Configuration for MVP Services
*   **Description:** Implement basic static configuration (`appsettings.json`) for the MVP services, allowing essential parameters to be set without recompilation.
*   **Primary Location:** `Nucleus.Agents.Bootstrapper`, `Nucleus.McpTools.KnowledgeStore`, `Nucleus.McpTools.FileAccess`
*   **Depends On:** `ISSUE-MVP-AGENT-BOOTSTRAPPER-001`, `ISSUE-MVP-MCPTOOL-FILEACCESS-001`, `ISSUE-MVP-MCPTOOL-KNOWLEDGESTORE-001`
*   **Effort:** Low
*   **Priority:** Medium
*   **Status:** To Do
*   **Tasks:**
    1.  Implement `appsettings.json` and corresponding `IOptions` pattern configuration for `BootstrapperNucleusAgent` (e.g., default persona ID, basic prompts or behaviors if any).
    2.  Implement `appsettings.json` and `IOptions` pattern configuration for `KnowledgeStoreMCP` (e.g., Cosmos DB emulator endpoint URI, primary key, database name, container names).
    3.  Implement `appsettings.json` and `IOptions` pattern configuration for `FileAccessMCP` (if any specific settings are needed, e.g., allowed file extensions - though likely not for MVP).
    4.  Ensure configurations are loaded correctly at service startup and utilized by the respective services.
    5.  Document the configurable settings in each project's README.

---

### `ISSUE-MVP-INFRA-AZURE-001`: Infrastructure: Define Azure Resource Placeholders (Post-MVP)

*   **Title:** Infrastructure: Define Azure Resource Placeholders (Post-MVP)
*   **Description:** Document the placeholder Azure resources that will be needed for a deployed environment beyond the local emulator-based MVP. This includes outlining the types of services and initial thoughts on their configuration.
*   **Primary Location:** `Docs/Deployment/Azure/PHASE1_MVP_AzureResources.md` (New Document)
*   **Depends On:** None
*   **Effort:** Low
*   **Priority:** Low
*   **Status:** To Do
*   **Tasks:**
    1.  Create the `Docs/Deployment/Azure/PHASE1_MVP_AzureResources.md` document.
    2.  Document the Azure services required for a basic cloud deployment corresponding to the MVP components:
        *   Azure Cosmos DB (for `KnowledgeStoreMCP`)
        *   Azure App Service or Azure Container Apps (for hosting the Agent and MCP services)
        *   (Consideration) Azure Service Bus (even if not used in MVP, note its potential role for future asynchronous processing).
        *   (Consideration) Azure Key Vault (for managing secrets).
    3.  Outline a basic Bicep template structure or list of resources to be provisioned for these services (detailed Bicep to be developed post-MVP).
    4.  Note any key configuration considerations for these services in a cloud environment (e.g., networking, scaling tiers).

---

### `ISSUE-MVP-TESTING-01`: Testing: Manual End-to-End MVP Scenario

*   **Title:** Testing: Manual End-to-End MVP Scenario
*   **Description:** Define and manually execute a basic end-to-end (E2E) test scenario to verify the core functionality of the MVP. This involves simulating a user trigger and observing the interaction between the agent and MCP tools.
*   **Primary Location:** `Docs/Testing/MVP_E2E_TestPlan.md` (New Document)
*   **Depends On:** All other MVP development tasks.
*   **Effort:** Medium
*   **Priority:** Critical
*   **Status:** To Do
*   **Tasks:**
    1.  Create the `Docs/Testing/MVP_E2E_TestPlan.md` document.
    2.  Define a detailed manual E2E test scenario:
        *   **Setup:** Start all services using DotNet Aspire. Ensure Cosmos DB Emulator is running and accessible. Prepare a sample `.txt` file in a known local directory.
        *   **Action:** Simulate a trigger to the `BootstrapperNucleusAgent` (e.g., via a direct HTTP call if a simple test endpoint is exposed by the agent, or by invoking a method if running in a test harness) with the path to the sample `.txt` file.
        *   **Verification:**
            *   Check logs of `BootstrapperNucleusAgent` for successful invocation and interaction with MCP tools.
            *   Check logs of `FileAccessMCP` for successful file read.
            *   Check logs of `KnowledgeStoreMCP` for successful data persistence.
            *   Inspect the Cosmos DB Emulator to verify that `ArtifactMetadata` and a corresponding `PersonaKnowledgeEntry<string>` for the sample file have been created correctly.
            *   Verify the agent provides a confirmation of completion (e.g., log output).
    3.  Manually execute the E2E test steps.
    4.  Document the actual outcomes, including any issues encountered and their resolution.

---

### `ISSUE-MVP-DOCS-01`: Documentation: Core MVP Component Readmes & Setup Guide

*   **Title:** Documentation: Core MVP Component Readmes & Setup Guide
*   **Description:** Create or update essential documentation for the MVP components, including README files for each new project and a consolidated local development setup guide.
*   **Primary Location:** Project Roots (`Nucleus.Agents.Bootstrapper`, `Nucleus.McpTools.FileAccess`, `Nucleus.McpTools.KnowledgeStore`), `Docs/Development/`
*   **Depends On:** All other MVP development tasks.
*   **Effort:** Medium
*   **Priority:** High
*   **Status:** To Do
*   **Tasks:**
    1.  Create/Update `README.md` for `Nucleus.Agents.Bootstrapper`:
        *   Purpose of the project.
        *   Key features (MVP scope).
        *   How to build and run (as part of Aspire).
        *   Configuration settings.
    2.  Create/Update `README.md` for `Nucleus.McpTools.FileAccess`:
        *   Purpose of the project.
        *   API endpoint(s) it exposes.
        *   How to build and run (as part of Aspire).
        *   Configuration settings (if any).
    3.  Create/Update `README.md` for `Nucleus.McpTools.KnowledgeStore`:
        *   Purpose of the project.
        *   API endpoint(s) it exposes.
        *   Cosmos DB Emulator setup requirements.
        *   Data models persisted.
        *   How to build and run (as part of Aspire).
        *   Configuration settings.
    4.  Create a `Docs/Development/LOCAL_DEVELOPMENT_SETUP.md` guide:
        *   Prerequisites (SDKs, Docker for Emulator, etc.).
        *   Steps to clone the repository.
        *   Instructions for starting the Azure Cosmos DB Emulator.
        *   Instructions for launching all services using DotNet Aspire (`dotnet run --project Nucleus.AppHost`).
        *   Link to the MVP E2E Test Plan.
        *   Basic troubleshooting tips.

---
