---
title: "Requirements: Phase 1 - MVP Foundation for M365 Agents & MCP Tools"
description: "Defines the Minimum Viable Product requirements for establishing Nucleus M365 Agent and core MCP Tool functionality."
version: 1.0
date: 2025-05-27
parent: ./00_TASKS_ROADMAP.md
see_also:
  - title: "Project Mandate"
    link: "./00_REQUIREMENTS_PROJECT_MANDATE.md"
  - title: "Phase 1 Tasks"
    link: "./01_TASKS_PHASE1_MVP_FOUNDATION.md"
  - title: "M365 Agents Overview"
    link: "../Architecture/Agents/01_M365_AGENTS_OVERVIEW.md"
  - title: "MCP Tools Overview"
    link: "../Architecture/McpTools/01_MCP_TOOLS_OVERVIEW.md"
  - title: "KnowledgeStore MCP Tool Architecture"
    link: "../Architecture/McpTools/KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md"
  - title: "FileAccess MCP Tool Architecture"
    link: "../Architecture/McpTools/FileAccess/ARCHITECTURE_MCPTOOL_FILE_ACCESS.md"
---

# Requirements: Phase 1 - MVP Foundation for M365 Agents & MCP Tools

**Version:** 1.0
**Date:** 2025-05-27

## 1. Goal

To establish the foundational components for Project Nucleus based on the Microsoft 365 Agents SDK and Model Context Protocol (MCP). This includes developing a basic `BootstrapperNucleusAgent` (M365 Agent), essential backend MCP Tools for knowledge storage (`Nucleus_KnowledgeStore_McpServer`) and local file access (`Nucleus_FileAccess_McpServer`), and validating their core interaction.

## 2. Scope

### In Scope for Phase 1 MVP:

*   **`BootstrapperNucleusAgent` (M365 Agent Application) Development:**
    *   Basic `Activity` handling (e.g., echo command, simple query processing).
    *   Integration with `IPersonaRuntime` using a static, basic `BootstrapperPersonaConfiguration`.
    *   Basic MCP client logic to call `Nucleus_KnowledgeStore_McpServer` and `Nucleus_FileAccess_McpServer`.
    *   Ability to send simple text responses back to the M365 platform.
*   **`Nucleus_KnowledgeStore_McpServer` (MCP Tool) Development:**
    *   Exposing MCP operations for basic Create, Read, Update, Delete (CRUD) on `ArtifactMetadata` and `PersonaKnowledgeEntry` objects.
    *   Integration with Azure Cosmos DB (local emulator for development, basic Azure deployment for testing).
*   **`Nucleus_FileAccess_McpServer` (MCP Tool) Development:**
    *   Exposing an MCP operation to retrieve content based on an `ArtifactReference`.
    *   Initial implementation will use a `LocalFileArtifactProvider` to access files from a local file system path (for testing and development).
*   **.NET Aspire AppHost Setup:**
    *   Configuration for local orchestration of the `BootstrapperNucleusAgent`, `Nucleus_KnowledgeStore_McpServer`, `Nucleus_FileAccess_McpServer`, and the Cosmos DB emulator.
*   **Static Configuration Loading:**
    *   Implementation of static configuration loading (e.g., via Azure App Configuration and Key Vault, leveraged by .NET Aspire) for essential settings such as:
        *   LLM API keys for the `BootstrapperNucleusAgent`.
        *   Endpoints for the `Nucleus_KnowledgeStore_McpServer` and `Nucleus_FileAccess_McpServer`.
        *   Cosmos DB connection string for `Nucleus_KnowledgeStore_McpServer`.
*   **Basic Azure Infrastructure as Code (IaC):**
    *   Initial Bicep templates (managed via `azd`) for deploying the MVP components to Azure (App Service/Container Apps for agent and tools, basic Cosmos DB instance).

### Explicitly Out of Scope for Phase 1 MVP:

*   Development of complex, feature-rich M365 Persona Agents (e.g., `EduFlowNucleusAgent`, `BusinessKnowledgeAssistantAgent`).
*   Full M365 channel integration beyond basic `Activity` handling in Teams (e.g., proactive messaging, rich Adaptive Cards, M365 Copilot Studio plugins).
*   Robust asynchronous processing pipelines using Azure Service Bus for background tasks initiated by the agent.
*   Advanced RAG capabilities (e.g., `Nucleus_RAGPipeline_McpServer` with 4R ranking).
*   Dynamic `PersonaConfiguration` management (e.g., `Nucleus_PersonaBehaviourConfig_McpServer`).
*   Advanced `IArtifactProvider` implementations for `Nucleus_FileAccess_McpServer` (e.g., Microsoft Graph API for SharePoint/OneDrive, web content).
*   Development of `Nucleus_ContentProcessing_McpServer` for complex content extraction or synthesis.
*   Full Microsoft Entra ID integration for complex authentication scenarios between agent and MCP tools (beyond basic application registration for the agent and service-to-service auth if simple).
*   Advanced observability, monitoring, and alerting.
*   Comprehensive automated testing suite (focus on manual E2E for MVP).

## 3. Requirements

*(Existing REQ-* items from the source document will be reviewed, and only those essential and adapted for Phase 1 MVP will be retained below. Others will be considered deferred.)*

*   **REQ-MVP-AGENT-001:** The `BootstrapperNucleusAgent` MUST be registerable with Azure Bot Service to enable basic interaction within Microsoft Teams.
*   **REQ-MVP-AGENT-002:** The `BootstrapperNucleusAgent` MUST handle basic incoming message `Activity` objects from Teams (e.g., a user typing a command).
*   **REQ-MVP-AGENT-003:** The `BootstrapperNucleusAgent` MUST integrate with `IPersonaRuntime` and use a predefined, static `BootstrapperPersonaConfiguration`.
*   **REQ-MVP-AGENT-004:** The `BootstrapperNucleusAgent` MUST be able to make basic MCP calls to the `Nucleus_FileAccess_McpServer` to request file content (via local file path) and to the `Nucleus_KnowledgeStore_McpServer` to read/write metadata and knowledge entries.
*   **REQ-MVP-AGENT-005:** The `BootstrapperNucleusAgent` MUST be able to send simple text-based responses back to the invoking M365 platform (Teams).
*   **REQ-MVP-AGENT-006:** The `BootstrapperNucleusAgent` MUST securely load and use an LLM API key for any direct LLM interactions it performs (if any within its simple MVP scope).

*   **REQ-MVP-MCP-KS-001:** The `Nucleus_KnowledgeStore_McpServer` MUST expose MCP operations for creating, reading, updating, and deleting `ArtifactMetadata`.
*   **REQ-MVP-MCP-KS-002:** The `Nucleus_KnowledgeStore_McpServer` MUST expose MCP operations for creating, reading, updating, and deleting `PersonaKnowledgeEntry` items.
*   **REQ-MVP-MCP-KS-003:** The `Nucleus_KnowledgeStore_McpServer` MUST connect to and interact with an Azure Cosmos DB instance (emulator locally, provisioned instance in Azure).
*   **REQ-MVP-MCP-KS-004:** The `Nucleus_KnowledgeStore_McpServer` MUST securely load and use its Cosmos DB connection string.

*   **REQ-MVP-MCP-FA-001:** The `Nucleus_FileAccess_McpServer` MUST expose an MCP operation that accepts an `ArtifactReference` (specifying a local file path for MVP) and returns the content of the specified file.
*   **REQ-MVP-MCP-FA-002:** The `Nucleus_FileAccess_McpServer` MUST implement a `LocalFileArtifactProvider` to fulfill these requests.

*   **REQ-MVP-CORE-001:** Core abstractions including `ArtifactReference` (supporting local file paths), `ArtifactMetadata`, `PersonaKnowledgeEntry`, `IPersonaRuntime`, `LocalFileArtifactProvider`, and basic repository interfaces for Cosmos DB interactions MUST be defined and implemented sufficiently for MVP needs.
*   **REQ-MVP-CORE-002:** All MVP components (`BootstrapperNucleusAgent`, `Nucleus_KnowledgeStore_McpServer`, `Nucleus_FileAccess_McpServer`) MUST use secure configuration mechanisms (e.g., .NET user secrets locally, Azure Key Vault via Aspire in Azure) for managing secrets like API keys and connection strings.

*   **REQ-MVP-DEVOPS-001:** The MVP components (`BootstrapperNucleusAgent`, `Nucleus_KnowledgeStore_McpServer`, `Nucleus_FileAccess_McpServer`, Cosmos DB emulator) MUST be orchestrable locally using a .NET Aspire AppHost project.
*   **REQ-MVP-DEVOPS-002:** Basic Azure IaC (Bicep) scripts MUST be created to allow deployment of the `BootstrapperNucleusAgent`, `Nucleus_KnowledgeStore_McpServer`, `Nucleus_FileAccess_McpServer`, and a basic Cosmos DB instance to Azure.

## 4. Implementation Strategy

1.  **Setup Core Project Structures:** Create new projects for `Nucleus.Agents.Bootstrapper`, `Nucleus.McpTools.KnowledgeStore`, and `Nucleus.McpTools.FileAccess` if they don't exist.
2.  **Define/Refine Core Abstractions:** Ensure `Nucleus.Abstractions` (or a similar shared kernel project) contains the necessary interfaces and DTOs (`ArtifactReference`, `ArtifactMetadata`, `PersonaKnowledgeEntry`, `IPersonaRuntime`, `IArtifactProvider`, repository interfaces) for MVP functionality.
3.  **Develop `Nucleus_KnowledgeStore_McpServer`:** Implement the MCP tool with basic CRUD operations for `ArtifactMetadata` and `PersonaKnowledgeEntry`, integrating with Cosmos DB.
4.  **Develop `Nucleus_FileAccess_McpServer`:** Implement the MCP tool with the `LocalFileArtifactProvider` to serve content from local files.
5.  **Develop `BootstrapperNucleusAgent`:**
    *   Integrate with the M365 Agents SDK for basic `Activity` handling.
    *   Implement `IPersonaRuntime` with a simple, static configuration.
    *   Implement MCP client logic to call the `KnowledgeStore` and `FileAccess` MCP tools.
6.  **Configure .NET Aspire AppHost:** Add projects to the AppHost for local orchestration, including the Cosmos DB emulator.
7.  **Implement Static Configuration:** Ensure all components can load necessary configurations (API keys, endpoints, connection strings) securely.
8.  **Basic End-to-End Testing:** Manually test the flow: user sends a message in Teams -> `BootstrapperNucleusAgent` receives it -> Agent calls `FileAccessMCP` (for a test file) -> Agent calls `KnowledgeStoreMCP` (to log metadata/knowledge) -> Agent sends a response to Teams.
9.  **Develop Basic Azure IaC:** Create initial Bicep templates for Azure deployment.

## 5. Non-Goals (This Phase)

*   Full Microsoft Graph integration for file access (`Nucleus_FileAccess_McpServer`).
*   Advanced content processing or synthesis (`Nucleus_ContentProcessing_McpServer`).
*   Sophisticated RAG pipeline (`Nucleus_RAGPipeline_McpServer`).
*   Dynamic persona behavior configuration (`Nucleus_PersonaBehaviourConfig_McpServer`).
*   Asynchronous task offloading using Azure Service Bus.
*   Proactive messaging capabilities for the agent.
*   Rich UI interactions (e.g., Adaptive Cards beyond simple text).
*   Comprehensive automated testing (unit, integration, system).
*   Advanced security hardening and complex Entra ID authentication flows.
*   Multi-LLM provider support beyond what's configured for the `BootstrapperNucleusAgent`.
*   Scalability and performance optimization beyond basic functionality.

## 6. Key Dependencies & Linkages

*   **Microsoft 365 Agents SDK:** For building the `BootstrapperNucleusAgent`.
*   **Model Context Protocol (MCP) Specification:** For designing the MCP Tools.
*   **`.NET Aspire`:** For local development orchestration and potentially for simplifying Azure deployment patterns.
*   **`Azure Cosmos DB`:** As the backend database for `Nucleus_KnowledgeStore_McpServer`.
*   **`Azure App Configuration` / `Azure Key Vault`:** For managing static configuration.
*   **`Bicep` / `Azure Developer CLI (azd)`:** For Azure IaC.
*   **Project Documents:**
    *   `./00_TASKS_ROADMAP.md`
    *   `./00_REQUIREMENTS_PROJECT_MANDATE.md`
    *   `./01_TASKS_PHASE1_MVP_FOUNDATION.md`
    *   `../Architecture/Agents/01_M365_AGENTS_OVERVIEW.md`
    *   `../Architecture/McpTools/01_MCP_TOOLS_OVERVIEW.md`
    *   `../Architecture/McpTools/KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md`
    *   `../Architecture/McpTools/FileAccess/ARCHITECTURE_MCPTOOL_FILE_ACCESS.md`
*   **`Microsoft.Extensions.AI`:** If the `BootstrapperNucleusAgent` needs to make direct LLM calls.
