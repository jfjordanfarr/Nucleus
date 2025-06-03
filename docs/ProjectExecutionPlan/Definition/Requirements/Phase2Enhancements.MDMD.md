---
title: "Requirements: Phase 2 - M365 Agent Enhancements & Channel Integration"
description: "Defines requirements for enhancing Nucleus M365 Agents, integrating fully with M365 channels, implementing robust asynchronous processing, and expanding core MCP Tool capabilities."
version: 1.0
date: 2025-05-27
parent: ./00_TASKS_ROADMAP.md
see_also:
  - ./00_REQUIREMENTS_PROJECT_MANDATE.md
  - ./01_REQUIREMENTS_PHASE1_MVP_FOUNDATION.md
  - ./02_TASKS_PHASE2_ENHANCEMENTS.md
  - ../../Architecture/Agents/01_M365_AGENTS_OVERVIEW.md
  - ../../Architecture/McpTools/01_MCP_TOOLS_OVERVIEW.md
  - ../../Architecture/McpTools/FileAccess/ARCHITECTURE_MCPTOOL_FILE_ACCESS.md
  - ../../Architecture/McpTools/ContentProcessing/ARCHITECTURE_MCPTOOL_CONTENT_PROCESSING.md
status: "Draft"
---

## 1. Goal

The primary goal of Phase 2 is to build upon the Minimum Viable Product (MVP) foundation by developing a feature-rich M365 Persona Agent (with `EduFlowNucleusAgent` as the lead example), fully integrating it with Microsoft Teams channels and potentially exploring M365 Copilot integration. This phase will also implement robust asynchronous processing for long-running tasks using Azure Service Bus and will enhance backend Model Context Protocol (MCP) Tools. Specifically, the `FileAccessMCP` will be augmented with Microsoft Graph API support for accessing SharePoint and OneDrive files, and a new `ContentProcessingMCP` will be introduced to handle complex file type extraction and Markdown synthesis.

## 2. Scope

### In Scope

*   **M365 Agent Enhancements (`EduFlowNucleusAgent` as lead example):**
    *   Full Microsoft Teams channel integration, including handling messages, mentions, and file sharing events within channels.
    *   Deployment and validation within the M365 Copilot environment (exploratory, contingent on custom engine agent capabilities and support at the time of implementation).
    *   Robust handling of M365 file attachments received from Teams, processed via `FileAccessMCP` utilizing its new Graph provider.
    *   Implementation of asynchronous task offloading to Azure Service Bus, leveraging an `IBackgroundTaskQueue` abstraction for tasks exceeding a configurable duration.
    *   Development of proactive messaging capabilities, allowing the agent to initiate messages to users or channels (using stored `ConversationReference`s) upon completion of background tasks or other triggers.
    *   Utilization of Adaptive Cards for presenting complex information, requesting structured input, and enhancing user interaction within Microsoft Teams.
*   **MCP Tool Enhancements/Additions:**
    *   `Nucleus_FileAccess_McpServer` (`Nucleus.McpTools.FileAccess` project):
        *   Addition of a `GraphArtifactProvider` to enable ephemeral access to files stored in SharePoint Online and OneDrive for Business, using the M365 Agent's security context.
    *   `Nucleus_ContentProcessing_McpServer` (`Nucleus.McpTools.ContentProcessing` project - New):
        *   Development of a new MCP tool dedicated to content extraction and transformation.
        *   Integration of `IContentExtractor` implementations for various common file types (e.g., initial support for DOCX, PDF focusing on text extraction).
        *   Capability to synthesize a standardized Markdown document from extracted textual components, potentially by orchestrating calls to `Nucleus_LlmOrchestration_McpServer` for summarization or reformatting if needed.
*   **Security:**
    *   Implementation of Microsoft Entra Agent ID for Nucleus M365 Persona Agents to securely identify themselves when accessing M365 services like Microsoft Graph.
    *   Implementation of secure authentication between M365 Agents and Nucleus MCP Tools using Azure AD tokens (OAuth 2.0 client credentials flow or equivalent).
*   **Asynchronous Processing Infrastructure:**
    *   Integration with Azure Service Bus as the message broker for `IBackgroundTaskQueue`.
    *   Development of a background worker service capable of dequeuing and processing tasks.
*   **Configuration:**
    *   Refinement and expansion of agent and MCP tool configuration mechanisms to support the enhanced capabilities introduced in this phase.

### Out of Scope (for Phase 2)

*   Advanced RAG capabilities (e.g., the full 4R ranking algorithm: Recency, Relevancy, Richness, Reputation).
*   Dynamic loading or updating of `PersonaConfiguration` from a database; configuration will primarily remain static or file-based for this phase.
*   Introduction of other new MCP tools beyond `ContentProcessingMCP` (e.g., tools for Calendar, Tasks, Email interactions are deferred).
*   Full, production-grade M365 Copilot plugin development if custom engine agent integration proves too complex or immature within the phase timeline.

## 3. Requirements

### M365 Agent (`EduFlowNucleusAgent`)

*   **`REQ-P2-AGENT-001`:** The `EduFlowNucleusAgent` MUST be deployable and fully functional within Microsoft Teams. It must be capable of receiving and processing messages where it is mentioned, participating in channel conversations, and responding to file sharing events within those channels.
*   **`REQ-P2-AGENT-002`:** The `EduFlowNucleusAgent` MUST integrate with the `Nucleus_FileAccess_McpServer`, specifically utilizing its `GraphArtifactProvider`, to ephemerally access and process files shared by users in Microsoft Teams (from SharePoint/OneDrive).
*   **`REQ-P2-AGENT-003`:** The `EduFlowNucleusAgent` MUST utilize an `IBackgroundTaskQueue` abstraction, implemented with Azure Service Bus, to offload content processing or other long-running tasks (defined as tasks exceeding a configurable threshold, e.g., 5 seconds) to a separate background worker process.
*   **`REQ-P2-AGENT-004`:** Upon completion of a background task, the `EduFlowNucleusAgent` MUST be able to send proactive messages (e.g., notifications, results) to the originating user or channel using stored `ConversationReference`s.
*   **`REQ-P2-AGENT-005`:** The `EduFlowNucleusAgent` MUST utilize Adaptive Cards to present complex information, solicit structured input from users, and provide a richer interactive experience within Microsoft Teams.
*   **`REQ-P2-AGENT-006` (Stretch Goal):** The `EduFlowNucleusAgent` SHOULD be testable and demonstrate basic functionality within the M365 Copilot environment, assuming the M365 Agent SDK supports such custom engine integrations effectively during this phase.

### `Nucleus_FileAccess_McpServer`

*   **`REQ-P2-MCPFA-001`:** The `Nucleus_FileAccess_McpServer` MUST implement a `GraphArtifactProvider` capable of ephemerally accessing file content and metadata from SharePoint Online and OneDrive for Business. This provider must operate using the security context of the calling M365 Agent (via its Microsoft Entra Agent ID and appropriate Graph API permissions).

### `Nucleus_ContentProcessing_McpServer` (New)

*   **`REQ-P2-MCPCP-001`:** The `Nucleus_ContentProcessing_McpServer` MUST expose MCP operations that accept `ArtifactMetadata` (pointing to content, potentially already fetched by `FileAccessMCP`) or raw content streams, and return structured content, primarily as a standardized Markdown string.
*   **`REQ-P2-MCPCP-002`:** The `Nucleus_ContentProcessing_McpServer` MUST integrate and manage multiple `IContentExtractor` implementations. Initial support should target common enterprise file types such as DOCX and PDF, focusing on robust plain text extraction.
*   **`REQ-P2-MCPCP-003`:** The `Nucleus_ContentProcessing_McpServer` MUST be capable of synthesizing a single, coherent Markdown document from multiple extracted textual components or sections. This may involve simple concatenation or invoking `Nucleus_LlmOrchestration_McpServer` for more advanced summarization or formatting tasks if complex synthesis is required.

### Security

*   **`REQ-P2-SEC-001`:** Nucleus M365 Persona Agents (e.g., `EduFlowNucleusAgent`) MUST use their provisioned Microsoft Entra Agent ID for authenticating to Microsoft Graph and other M365 services, adhering to the principle of least privilege.
*   **`REQ-P2-SEC-002`:** All HTTP-based communication between Nucleus M365 Persona Agents and Nucleus MCP Tools (e.g., `FileAccessMCP`, `ContentProcessingMCP`, `KnowledgeStoreMCP`) MUST be secured using Azure AD-issued OAuth 2.0 bearer tokens. Agents will act as clients, and MCP Tools will act as protected resources, validating tokens.

### Asynchronous Processing

*   **`REQ-P2-ASYNC-001`:** A dedicated background worker service (e.g., an Azure Function with a Service Bus trigger, or a continuously running .NET service) MUST be implemented to listen to and process messages from the Azure Service Bus queue associated with the `IBackgroundTaskQueue`.
*   **`REQ-P2-ASYNC-002`:** The background worker service MUST be capable of invoking relevant MCP Tools (e.g., `FileAccessMCP` to fetch content if only a reference was queued, `ContentProcessingMCP` for extraction, `KnowledgeStoreMCP` to save results) as part of executing the offloaded task logic.
*   **`REQ-P2-ASYNC-003`:** Upon successful completion or failure of a task, the background worker service MUST be able to signal the originating M365 Agent (e.g., via another queue or a direct callback mechanism if feasible and secure) to trigger a proactive reply to the user or channel.

## 4. Implementation Strategy (High-Level)

1.  **Develop `EduFlowNucleusAgent`:** Enhance the M365 Agent from Phase 1 (or create a new one inheriting base capabilities) to include Teams channel interactions, Adaptive Card usage, and `IBackgroundTaskQueue` integration.
2.  **Implement Graph API Provider:** Add the `GraphArtifactProvider` to the existing `Nucleus_FileAccess_McpServer`, including necessary Graph SDK integration and permission handling.
3.  **Develop `ContentProcessingMCP`:** Create the new `Nucleus_ContentProcessing_McpServer` project, define its MCP interface, and implement initial `IContentExtractor`s (e.g., for DOCX, PDF text extraction) and Markdown synthesis logic.
4.  **Integrate Azure Service Bus:** Set up Azure Service Bus, implement the `IBackgroundTaskQueue` with it, and develop the background worker service to process queued tasks.
5.  **Implement Entra ID Security:** Configure Entra ID App Registrations for agents and MCP tools. Implement token acquisition in agents and token validation in MCP tools.
6.  **End-to-End Testing:** Rigorously test E2E scenarios involving file sharing in Teams, asynchronous processing via Service Bus, content extraction, knowledge storage, and proactive notifications.

## 5. Non-Goals (This Phase)

*   **Advanced RAG Capabilities:** The full implementation of the 4R (Recency, Relevancy, Richness, Reputation) ranking algorithm for retrieval is deferred. Basic retrieval from `KnowledgeStoreMCP` will suffice.
*   **Dynamic Persona Configuration:** `PersonaConfiguration` will continue to be loaded from static sources (e.g., configuration files). Loading persona configurations dynamically from a database is not in scope for this phase.
*   **Additional New MCP Tools:** Development of other new MCP tools beyond `ContentProcessingMCP` (e.g., for interacting with Calendar, Tasks, or Email) is deferred to future phases.
*   **Full M365 Copilot Plugin Suite:** While initial exploration is a stretch goal, developing a comprehensive suite of M365 Copilot plugins with deep integration is beyond Phase 2.

## 6. Key Dependencies & Linkages

*   **Phase 1 MVP Components:** This phase builds directly upon the agent, MCP tools, and orchestration established in Phase 1.
*   **Microsoft Graph API:** Essential for accessing M365 resources like files in SharePoint and OneDrive.
*   **Azure Service Bus:** Required for implementing the asynchronous task processing queue.
*   **Microsoft Entra ID:** For securing agent and MCP tool communications.
*   **M365 Agent SDK:** Continued reliance on the Microsoft 365 Agent SDK for agent development.
*   **Relevant Architecture Documents:**
    *   `Docs/Architecture/Agents/01_M365_AGENTS_OVERVIEW.md`
    *   `Docs/Architecture/McpTools/01_MCP_TOOLS_OVERVIEW.md`
    *   `Docs/Architecture/McpTools/FileAccess/ARCHITECTURE_MCPTOOL_FILE_ACCESS.md`
    *   `Docs/Architecture/McpTools/ContentProcessing/ARCHITECTURE_MCPTOOL_CONTENT_PROCESSING.md`
    *   `Docs/Architecture/Security/ (relevant security architecture documents)`
