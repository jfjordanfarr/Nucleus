---
title: "Requirements: M365 Agent & MCP Tool Integration"
description: "Requirements for Nucleus to function as a backend for Microsoft 365 Agents and as a Model Context Protocol (MCP) Tool/Server, enabling platform-integrated AI Personas."
version: 2.0
date: 2025-05-25
---

# Requirements: Microsoft 365 Agent & Model Context Protocol (MCP) Integration

**Version:** 2.0  
**Date:** 2025-05-25

## 1. Goal

To refactor Nucleus to operate as a robust backend system supporting **Microsoft 365 Agents** and to expose its capabilities as a **Model Context Protocol (MCP) Tool/Server**. This enables Nucleus Personas to be invoked by M365 Agents within the Microsoft ecosystem (Teams, Outlook, etc.) and allows other MCP-compatible agents to leverage Nucleus's specialized knowledge processing and retrieval functionalities. This phase focuses on the core backend services, MCP compliance, and the necessary interfaces for M365 Agent integration.

## 2. Scope

*   **Microsoft 365 Agent Integration:**
    *   Enabling Nucleus Personas to be discoverable and invocable by M365 Agents.
    *   Handling requests from M365 Agents, including context (e.g., conversation history, document references from M365 services like SharePoint, OneDrive).
    *   Returning responses and structured data to M365 Agents for presentation in the host application (Teams, Outlook, etc.).
*   **Model Context Protocol (MCP) Server/Tool Implementation:**
    *   Exposing Nucleus's core functionalities (content analysis, knowledge retrieval, persona-specific logic) via MCP-compliant endpoints.
    *   Defining MCP Tool capabilities that represent Nucleus Persona actions.
    *   Handling MCP requests, processing them through the Nucleus backend, and returning MCP responses.
*   **Nucleus Backend Services (`Nucleus.Core`, `Nucleus.Services.Core`, `Nucleus.Services.McpServer`):**
    *   Refining and extending core services for ingestion, processing, storage (Cosmos DB), and AI interaction (Gemini via `Microsoft.Extensions.AI`).
    *   Implementing the `Nucleus.Services.McpServer` project to host MCP endpoints.
    *   Ensuring secure and scalable operation within Azure (App Service, Functions, Service Bus, Cosmos DB).
*   **Artifact Handling:**
    *   Processing `ArtifactReference` objects that point to content within the M365 ecosystem (e.g., SharePoint files, OneDrive documents, Teams messages) or other locations accessible via MCP context.
    *   Utilizing `IArtifactProvider` implementations (e.g., for Microsoft Graph) to resolve these references securely and ephemerally.
*   **Persona Invocation:**
    *   Allowing M365 Agents or MCP clients to specify and invoke particular Nucleus Personas (`IPersona` implementations) for specialized processing.

## 3. Requirements

### 3.1. M365 Agent Integration

*   **REQ-M365-INT-001:** Nucleus MUST provide a mechanism for its Personas to be registered or declared in a way that Microsoft 365 Agents can discover and invoke them (e.g., via an App Manifest for Teams, or other M365 extensibility points).
*   **REQ-M365-INT-002:** Nucleus (specifically, a dedicated M365 integration layer or the MCP Server if M365 Agents use MCP) MUST be able to receive and parse invocation requests from M365 Agents. These requests will include:
    *   Identifier of the target Nucleus Persona.
    *   User query or command.
    *   Contextual information provided by the M365 Agent (e.g., conversation history, references to documents in SharePoint/OneDrive, email content).
*   **REQ-M365-INT-003:** Nucleus MUST process the M365 Agent's request by:
    *   Identifying the target `IPersona`.
    *   Resolving any `ArtifactReference` objects (e.g., M365 Graph URIs) using appropriate `IArtifactProvider` implementations to fetch content ephemerally.
    *   Invoking the Persona's logic (e.g., `AnalyzeContentAsync`, `HandleInteractionAsync`).
    *   Utilizing its AI capabilities (e.g., Google Gemini) and internal knowledge store (Cosmos DB).
*   **REQ-M365-INT-004:** Nucleus MUST return a response to the M365 Agent in a format it expects (e.g., structured JSON, Adaptive Card payload, or MCP response if applicable). The response should be suitable for rendering within the M365 host application.
*   **REQ-M365-INT-005:** All interactions with M365 services (e.g., accessing files via Graph API) MUST adhere to Microsoft's security and permission models, using appropriate authentication and authorization (e.g., OAuth, managed identities). User data sovereignty and Zero Trust principles are paramount.

### 3.2. Model Context Protocol (MCP) Server/Tool Implementation

*   **REQ-MCP-SRV-001:** Nucleus MUST implement an MCP Server (e.g., as part of `Nucleus.Services.McpServer`) that exposes its functionalities as one or more MCP Tools.
*   **REQ-MCP-SRV-002:** The MCP Server MUST correctly implement the required MCP endpoints (e.g., for tool discovery, execution, status checks if applicable).
*   **REQ-MCP-SRV-003:** Nucleus Personas and their specific actions (e.g., "analyze document with EduFlow," "query knowledge with BusinessAssistant") MUST be definable as MCP Tools with clear schemas for their inputs and outputs.
*   **REQ-MCP-SRV-004:** The MCP Server MUST be able to receive MCP tool invocation requests, parse their parameters, and map them to the corresponding Nucleus Persona and action.
*   **REQ-MCP-SRV-005:** The MCP Server MUST orchestrate the execution of the request using Nucleus backend services (core logic, `IArtifactProvider` for context, `IPersona` invocation, AI calls, database access).
*   **REQ-MCP-SRV-006:** The MCP Server MUST return results to the MCP client in the standard MCP format, including success/failure status and any output data.
*   **REQ-MCP-SRV-007:** The MCP Server SHOULD support asynchronous tool execution patterns if defined by the MCP specification and relevant to Nucleus's long-running tasks.

### 3.3. Nucleus Backend Services & Core Logic

*   **REQ-NUC-CORE-001:** The `Nucleus.Core` and `Nucleus.Services.Core` projects MUST provide robust and extensible implementations of:
    *   `IPersona` interface and base classes.
    *   `IArtifactProvider` interface and implementations (especially for M365 Graph and potentially generic HTTP resources).
    *   `IFileStorage` for ephemeral storage during processing.
    *   Services for interacting with the AI model (e.g., Google Gemini via `Microsoft.Extensions.AI`).
    *   Services for database interaction (Cosmos DB for `ArtifactMetadata` and `PersonaKnowledgeEntry`).
    *   Message queueing (Azure Service Bus) for decoupling and managing asynchronous tasks.
*   **REQ-NUC-CORE-002:** The system MUST securely manage configuration, including API keys for AI services, Cosmos DB connection strings, Service Bus connection strings, and any M365 integration credentials, using Azure Key Vault or similar.
*   **REQ-NUC-CORE-003:** The system MUST implement comprehensive logging and telemetry (e.g., Azure Application Insights) for monitoring, debugging, and performance analysis.
*   **REQ-NUC-CORE-004:** The system MUST be designed for stateless operation where possible, particularly for API endpoints and function apps, to ensure scalability.
*   **REQ-NUC-CORE-005:** The `ArtifactReference` model MUST be robust enough to carry necessary information for `IArtifactProvider` implementations to resolve content from various sources, including M365 (Graph URIs, item IDs) and potentially other MCP-provided contexts.

### 3.4. Administration & Deployment

*   **REQ-ADM-DEP-001:** The Nucleus backend (including the MCP Server) MUST be deployable to Azure using .NET Aspire orchestration or IaC scripts (e.g., Bicep/ARM templates).
*   **REQ-ADM-DEP-002:** Administrators MUST be able to configure connection strings, AI service endpoints/keys, and other critical settings securely.
*   **REQ-ADM-DEP-003:** Mechanisms for registering and managing Nucleus Personas available to M365 Agents or via MCP MUST be provided (e.g., configuration files, admin API).

## 4. Implementation Strategy

1.  **MCP Core Implementation:**
    *   Develop the `Nucleus.Services.McpServer` project.
    *   Implement core MCP request handling, tool definition, and response generation.
    *   Define initial MCP Tools representing core Nucleus Persona actions (e.g., a generic "analyze" tool, a "query" tool).
2.  **M365 Integration Research & Prototyping:**
    *   Investigate the specific mechanisms for M365 Agents to discover and call external tools/services (e.g., if they use MCP directly, or require a specific adapter/manifest).
    *   Prototype the connection between an M365 Agent (e.g., a sample Teams bot acting as an agent) and the Nucleus MCP Server.
3.  **Refine Core Services:**
    *   Ensure `IPersona`, `IArtifactProvider` (especially for Graph API), and other core services can support the data and context provided by M365 Agents and MCP.
    *   Solidify `ArtifactReference` to handle M365 resource identifiers.
4.  **Develop `GraphArtifactProvider`:** Implement a robust `IArtifactProvider` for Microsoft Graph to resolve file/item references from SharePoint, OneDrive, Teams, etc., using appropriate authentication.
5.  **End-to-End Testing:**
    *   Test invocation of Nucleus Personas from a sample M365 Agent (or test harness).
    *   Test invocation of Nucleus MCP Tools from a generic MCP client.
6.  **Deployment & Configuration:**
    *   Develop .NET Aspire configurations or IaC scripts for deploying the full backend stack to Azure.
    *   Establish secure configuration management.

## 5. Non-Goals (This Phase)

*   Building a custom UI for Nucleus outside of M365 host applications.
*   Direct integration with platforms other than those supported by M365 Agents or MCP clients (e.g., standalone Slack, Discord adapters are superseded by this M365/MCP focus).
*   Implementing all conceivable Nucleus Personas; focus is on the infrastructure to support them.

## 6. Key Dependencies & Linkages

*   **Microsoft 365 Agent SDK/Platform:** The capabilities and requirements of M365 Agents will heavily influence integration points.
*   **Model Context Protocol (MCP) Specification:** Adherence to the MCP spec is critical for interoperability.
*   **`00_PROJECT_MANDATE.md`:** Overarching project goals and vision.
*   **Architecture Documents (especially `00_ARCHITECTURE_OVERVIEW.md`, `01_ARCHITECTURE_PROCESSING.md`, `02_ARCHITECTURE_INTERACTION_FLOW.md`):** Technical design for the Nucleus backend.
*   **`Microsoft.Extensions.AI`:** For interaction with LLMs.
*   **Azure Services:** Cosmos DB, Service Bus, App Service/Functions, Key Vault, Application Insights.
