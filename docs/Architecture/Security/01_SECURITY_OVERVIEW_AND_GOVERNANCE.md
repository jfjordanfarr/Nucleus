---
title: "Nucleus Security Overview and Data Governance"
description: "Defines the comprehensive security architecture and data governance framework for the Nucleus platform, emphasizing Microsoft 365 Agent integration, Model Context Protocol (MCP) security, and Zero Trust principles."
version: 1.1
date: 2025-05-26
parent: ../00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md
see_also:
    - ../CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md
    - ../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md
    - ../Deployment/01_DEPLOYMENT_OVERVIEW.md
---

# Nucleus: Security Architecture & Data Governance

## 1. Introduction

Security and responsible data governance are paramount for the Nucleus platform, especially given its interaction with potentially sensitive user information. This document outlines the core security principles, data boundaries, authentication/authorization strategies, and responsibilities, updated to reflect the adoption of the **Microsoft 365 Agents SDK**, **Model Context Protocol (MCP)**, and **Microsoft Entra Agent ID**. Nucleus is designed for both cloud-hosted offerings and self-hosted deployments by organizations.

## 2. Data Governance & Boundaries

Nucleus is designed to process user data, not to be its primary custodian. The core principle is **Zero Trust for User File Content**: The Nucleus backend infrastructure (MCP Tool/Server applications, databases, queues) **MUST NEVER** store, persist, or have direct access to the raw byte content of user files from their primary storage.

*   **Primary Data Source:** Original user files (documents, images, etc.) **always reside exclusively in the user's designated, secure cloud storage** (e.g., Microsoft 365 SharePoint/OneDrive, or other user-controlled systems). Nucleus M365 Persona Agents are informed about these files via the M365 platform context.
*   **Ephemeral File Interaction (via `Nucleus_FileAccess_McpServer`):**
    *   Nucleus M365 Persona Agents **DO NOT directly download or upload files** for processing.
    *   When a Persona Agent requires file content for analysis, it makes an MCP call to a dedicated **`Nucleus_FileAccess_McpServer`**. This call includes an `ArtifactReference` object (constructed by the agent from platform SDK information).
    *   The `Nucleus_FileAccess_McpServer` (which internally uses `IArtifactProvider` logic) resolves this reference and retrieves content *ephemerally* directly from the user's source storage (e.g., via Microsoft Graph API, using the M365 Agent's delegated or application permissions via its Entra Agent ID).
    *   This ephemerally fetched content is processed by the M365 Agent (or other Nucleus MCP tools) and **discarded after the interaction/processing is complete.**
*   **Nucleus ArtifactMetadata:**
    *   Stored within the **Nucleus Database (Cosmos DB)**, accessed via the `Nucleus_KnowledgeStore_McpServer`.
    *   Contains sanitized metadata *about* the source file (identifiers, hashes, timestamps, content type, processing status, originating context, etc.). Crucially, this metadata serves as a **Secure Index** for Personas to understand available knowledge without accessing raw content.
    *   **Policy:** ArtifactMetadata **must not** store sensitive PII directly from the source document. Fields like `displayName` are acceptable.
*   **Nucleus Database (Cosmos DB) for `PersonaKnowledgeEntry`:**
    *   Stores `PersonaKnowledgeEntry` documents via the `Nucleus_KnowledgeStore_McpServer`.
    *   Contains: **Derived knowledge, salient text snippets, and vector embeddings** (derived data), persona-specific analysis (`AnalysisData` as `JsonElement?`), `sourceIdentifier` linking back to `ArtifactMetadata`, timestamps.
    *   **PII Consideration:** Text previews or `AnalysisData` fields *might* contain sensitive information derived from the source. Nucleus M365 Persona Agents generating this data (and the LLMs they use) must be designed with potential sensitivity in mind. Redaction/scrubbing strategies are persona-specific configuration and prompt engineering details.
    *   **Data Processing by AI Providers:** When ephemeral content is sent to an LLM (e.g., Azure OpenAI, Google Gemini, or via OpenRouter.AI) for analysis by a Nucleus M365 Agent or an MCP Tool:
        *   The choice of AI provider can be configured (see [AI Integration Architecture](../../CoreNucleus/05_AI_INTEGRATION_STRATEGY.md)).
        *   **Data Sovereignty & LLM Choice:** For enterprises with strict data residency or provider preferences (e.g., requiring all AI processing within their Azure tenant via Azure OpenAI), Nucleus Personas can be configured to use these specific providers. This is a key configuration point for enterprise deployments.
        *   Data sent to any LLM is ephemeral in the context of Nucleus's processing; Nucleus does not persist the raw content after the LLM interaction. However, the chosen LLM provider's data handling policies apply to the data *while they are processing it*.
*   **Chat Logs (Platform Managed):**
    *   Inbound and outbound chat messages between the user and the Nucleus M365 Persona Agent are managed and persisted by the respective communication platform (e.g., Microsoft Teams, M365 Copilot).
    *   Nucleus M365 Agents process the incoming `Activity` but do not persist the raw chat logs themselves within Nucleus's own database. Salient information extracted from chats might be stored as `PersonaKnowledgeEntry` records.

### Data Flow & Security Boundaries Diagram (Revised Conceptual)

```mermaid
graph TD
    subgraph User Domain
        direction LR
        U[User]
        UserM365Storage[User Microsoft 365 Storage <br/> (SharePoint, OneDrive)]
        U -- Interacts --> UserM365Storage
    end

    subgraph Microsoft_365_Platform_and_Azure_Bot_Service ["Microsoft 365 Platform & Azure Bot Service"]
        TeamsCopilot[MS Teams / M365 Copilot]
        AzureBotSvc[Azure Bot Service]
    end

    subgraph "Nucleus Deployment (ISV or Self-Hosted Azure Tenant)"
        direction LR

        subgraph Nucleus_M365_Persona_Agents ["Nucleus M365 Persona Agents (e.g., ACA/AppService)"]
            M365Agent["EduFlow Nucleus M365 Agent <br/> (Entra Agent ID)"]
        end

        subgraph Nucleus_Backend_MCP_Tools ["Backend Nucleus MCP Tools/Servers (e.g., ACA/AppService)"]
            FileAccessMCP["Nucleus_FileAccess_McpServer <br/> (Uses IArtifactProvider, Entra ID AuthN/AuthZ)"]
            KnowledgeStoreMCP["Nucleus_KnowledgeStore_McpServer <br/> (Entra ID AuthN/AuthZ)"]
            LLMOrchestrationMCP["(Optional) Nucleus_LLM_Orchestration_McpServer <br/> (Manages IChatClient interactions, Entra ID AuthN/AuthZ)"]
        end

        subgraph Nucleus_Shared_Infrastructure ["Nucleus Shared Infrastructure (Azure)"]
            NucleusCosmosDB[Nucleus Cosmos DB <br/> (ArtifactMetadata, PersonaKnowledge)]
            NucleusServiceBus[Nucleus Service Bus <br/> (Async Tasks)]
            NucleusKeyVault[Nucleus Azure Key Vault <br/> (Stores LLM API Keys, MCP Tool Secrets)]
        end

        subgraph External_AI_Services ["Configurable External AI Services"]
            AzureOpenAI["Azure OpenAI Service"]
            GoogleGemini["Google Gemini API"]
            OpenRouter["OpenRouter.AI Hub"]
        end
    end

    %% User Interaction
    U -- Interacts via --> TeamsCopilot

    %% M365 Flow
    TeamsCopilot -- Activity --> AzureBotSvc
    AzureBotSvc -- Activity --> M365Agent

    %% Agent to MCP Tools
    M365Agent -- MCP Call (ArtifactReference) --> FileAccessMCP
    M365Agent -- MCP Call (Save/Query Knowledge) --> KnowledgeStoreMCP
    M365Agent -- Uses LLM via --> LLMOrchestrationMCP %% Option 1: Centralized LLM MCP
    M365Agent -- Directly Calls (via IChatClient) --> AzureOpenAI %% Option 2a: Direct, if configured
    M365Agent -- Directly Calls (via IChatClient) --> GoogleGemini %% Option 2b: Direct, if configured
    M365Agent -- Directly Calls (via IChatClient) --> OpenRouter %% Option 2c: Direct, if configured

    %% MCP Tool Interactions
    FileAccessMCP -- Ephemeral Fetch using Agent's context/perms --> UserM365Storage
    KnowledgeStoreMCP -- Reads/Writes --> NucleusCosmosDB
    LLMOrchestrationMCP -- Calls specific --> AzureOpenAI
    LLMOrchestrationMCP -- Calls specific --> GoogleGemini
    LLMOrchestrationMCP -- Calls specific --> OpenRouter

    %% Secrets & Config
    M365Agent -- Gets Secrets (LLM Keys, MCP Tool Endpoints/Keys) --> NucleusKeyVault
    FileAccessMCP -- Gets Secrets (e.g. if it needs its own for Graph) --> NucleusKeyVault
    KnowledgeStoreMCP -- Gets Secrets (Cosmos Connection) --> NucleusKeyVault
    LLMOrchestrationMCP -- Gets Secrets (LLM Keys) --> NucleusKeyVault

    %% Async Flow
    M365Agent -- Enqueues Task Ref --> NucleusServiceBus
    NucleusServiceBus --> BgWorker[Background Worker <br/> (Calls MCP Tools)]
    BgWorker -- MCP Calls --> FileAccessMCP
    BgWorker -- MCP Calls --> KnowledgeStoreMCP
    BgWorker -- MCP Calls --> LLMOrchestrationMCP
    M365Agent <-- Proactive Msg via AzureBotSvc --- BgWorker


    %% Data Flow Clarification
    style UserM365Storage fill:#DCDCDC,stroke:#333,stroke-width:2px
    style NucleusCosmosDB fill:#DCDCDC,stroke:#333,stroke-width:2px
    note right of UserM365Storage : User's source files ONLY live here.
    note right of NucleusCosmosDB : Nucleus ONLY stores metadata & derived knowledge.
```

### Where Sensitive Information is Okay (Ephemeral Processing)

The artifacts uploaded by users (via M365 platforms) for use by a Nucleus Persona Agent, as well as any artifacts Nucleus Personas themselves generate (and save back to the user's M365 storage), are stored in the **user's designated cloud storage** (OneDrive, SharePoint). In terms of persistent storage, this is the only location for raw user content. We do not want to house sensitive raw user files in Nucleus's own backend.

However, **transient, ephemeral** sensitive data is acceptable during active processing:
*   In-memory within the M365 Persona Agent or backend MCP Tools during a single interaction/analysis.
*   Ephemeral data streams passed between MCP tools or to an LLM provider.
*   Prompt/response data sent to/from any configured LLM provider (Azure OpenAI, Google Gemini, OpenRouter.AI). The data handling policies of the *chosen LLM provider* apply during their processing. Nucleus itself does not persist this ephemeral LLM interaction data unless specific derived knowledge is extracted and stored in `PersonaKnowledgeEntry`.

### Where Sensitive Information is Not Okay (Nucleus Persistent Storage)

**The Nucleus-managed database (Cosmos DB)!**
It is a simultaneous optimization and security feature to **not** house raw user files or extensive sensitive data derived directly from them in Cosmos DB. Instead, Persona Agents (via the `Nucleus_KnowledgeStore_McpServer`) write `ArtifactMetadata` about source artifacts and `PersonaKnowledgeEntry` records containing *derived insights, summaries, or sanitized snippets*.

## 3. Authentication & Authorization (AuthN/AuthZ)

A multi-layered approach handles identity and access:

*   **User Authentication (to M365 Platform):** Handled by the Microsoft 365 platform (e.g., user logs into Teams). The Nucleus M365 Agent receives user context via the M365 Agents SDK.
*   **Nucleus M365 Persona Agent Authentication & Identity:**
    *   Each deployed Nucleus M365 Persona Agent has a **Microsoft Entra Agent ID**. This identity is used for:
        *   Authenticating to the Azure Bot Service.
        *   Authenticating to backend Nucleus MCP Tool/Server applications (e.g., acquiring tokens for them).
        *   Potentially being granted permissions by customer admins to access Microsoft Graph API (e.g., for file metadata or content access by the `Nucleus_FileAccess_McpServer` acting on behalf of the agent).
*   **M365 Agent to Nucleus MCP Tool Authentication:**
    *   Backend Nucleus MCP Tool/Servers expose secure MCP endpoints (e.g., HTTPS).
    *   Nucleus M365 Persona Agents authenticate to these MCP tools using their Entra Agent ID (Managed Identity) to acquire an Azure AD token scoped for the target MCP Tool's App ID URI.
    *   MCP Tools validate these tokens.
*   **Nucleus MCP Tool to Azure Resource Authentication:**
    *   Backend Nucleus MCP Tools (e.g., `Nucleus_KnowledgeStore_McpServer`, `Nucleus_FileAccess_McpServer`) use their own **Managed Identities** to authenticate to Azure resources like Cosmos DB, Key Vault, and to Microsoft Graph if they make direct calls (though file access is preferably centralized in `Nucleus_FileAccess_McpServer`).
*   **Storage Provider Access (via `Nucleus_FileAccess_McpServer`):**
    *   The `Nucleus_FileAccess_McpServer` (using its `IArtifactProvider` logic) accesses files in the user's M365 storage (SharePoint, OneDrive).
    *   This access is performed using the **calling Nucleus M365 Persona Agent's security context/permissions (Entra Agent ID)**. The M365 Agent passes necessary context (e.g., user identity, file reference from platform) to the `FileAccessMCP` tool. The `FileAccessMCP` tool might then make Graph API calls impersonating the agent or using the agent's direct permissions if appropriately consented.
    *   This ensures that file access adheres to the permissions granted to the specific Nucleus Persona Agent by the organization/user.
*   **RBAC Delegation & Data Scoping:**
    *   Access to user files in M365 is governed by Microsoft Graph permissions granted to the Nucleus M365 Agent's Entra Agent ID.
    *   Access to data within Nucleus's Cosmos DB by MCP Tools is governed by Azure RBAC roles on the database for the MCP Tool's Managed Identity, AND critically by **application-level enforcement of `tenantId`** (and `userId`/`personaId` where applicable) in all database queries and operations performed by the `Nucleus_KnowledgeStore_McpServer`.

## 4. Secrets Management

All sensitive configuration values must be managed securely in **Azure Key Vault**.

*   **Secrets Include:**
    *   API Keys for **external LLM providers** (Google Gemini, OpenRouter.AI, or even specific Azure OpenAI keys if not using Managed Identity for direct Azure OpenAI calls).
    *   Connection string for Nucleus Cosmos DB (used by `Nucleus_KnowledgeStore_McpServer`).
    *   Connection string for Nucleus Service Bus (used by M365 Agents to enqueue, and by background workers to dequeue).
    *   Any secrets required by specific `IArtifactProvider` implementations within `Nucleus_FileAccess_McpServer` if they cannot use the agent's direct Graph permissions.
    *   Secrets for MCP Tool-to-Tool communication if not using Entra ID tokens.
*   **Secure Storage & Access:**
    *   Azure Key Vault is the central store.
    *   **Nucleus M365 Persona Agents** use their Managed Identity (Entra Agent ID) to access secrets they directly need (e.g., LLM API keys if calling LLMs directly, Service Bus connection string).
    *   **Backend Nucleus MCP Tool/Server applications** use their Managed Identities to access secrets they need (e.g., `Nucleus_KnowledgeStore_McpServer` gets Cosmos DB connection string; `Nucleus_LLM_Orchestration_McpServer` might get LLM keys).
    *   Configuration of these Key Vault URIs is managed via Azure App Configuration, injected by .NET Aspire.
*   **Policy:** **NO hardcoded secrets.**

## 5. Infrastructure Security

Secure configuration of underlying infrastructure (see [Deployment Architecture](../../Deployment/01_DEPLOYMENT_OVERVIEW.md)) for M365 Agents and backend MCP Tools is essential.

*   **Network Security:**
    *   Employ VNet integration for M365 Agents and MCP Tools hosted in Azure (e.g., Azure Container Apps, App Service).
    *   Use Azure Application Gateway/APIM as a secure public ingress for M365 Agent messaging endpoints if required for channel connectivity.
    *   Route all outbound egress traffic from agents and MCP tools through a central Azure Firewall.
    *   Utilize Private Endpoints for M365 Agents and MCP Tools to access Azure PaaS dependencies (Cosmos DB, Key Vault, App Configuration, Service Bus) securely within the VNet.
*   **Service Configuration:** Apply least-privilege principles to all Managed Identities (for M365 Agents and MCP Tools). Grant specific Azure RBAC roles on target resources.

## 6. AI & Prompt Security

Leverage platform capabilities and best practices.

*   **Provider Safeguards:**
    *   Rely primarily on the **built-in safety features and content filters** provided by the *chosen and configured* AI service providers (e.g., Azure OpenAI Content Filters, Google Gemini Safety Settings, or any available via OpenRouter.AI).
    *   Nucleus M365 Persona Agents (or a `Nucleus_LLM_Orchestration_McpServer`) must be configured to enable and use these provider-specific safety mechanisms appropriately.
*   **Input Handling:** Basic input sanitization on user prompts (before sending to any LLM) can add a layer of defense.
*   **Data Minimization:** Nucleus M365 Persona Agents should be designed to only send necessary context (ephemerally fetched content + metadata from `Nucleus_KnowledgeStore_McpServer`) to the configured LLM provider.
*   **Dynamic Prompt Security (Cosmos DB stored prompts):**
    *   If behavioral prompts are stored in Cosmos DB and potentially updated by agents:
        *   Implement strict validation and sanitization on any input that could modify these stored prompts to prevent prompt injection.
        *   Use versioning and approval workflows for significant prompt changes.
        *   Audit all prompt modifications.
        *   To mitigate risks associated with dynamic prompt modification, Nucleus M365 Persona Agents and their configuration mechanisms must implement:
            *   **Input Validation and Sanitization:** Rigorously validate and sanitize any external data (especially user-provided feedback) used to modify stored prompts.
            *   **Strict Schema and Modification Limits:** Define and enforce strict schemas for prompt structures and limit the scope of automated or user-driven changes.
            *   **Human-in-the-Loop Approval:** For significant or sensitive prompt changes, implement approval workflows.
            *   **Segregation of Duties & Least Privilege:** The mechanism updating prompts should operate with minimal necessary permissions.
            *   **Contextual Awareness in Prompts:** Design prompts to be robust against attempts to override core instructions.
            *   **Output Filtering:** Apply content safety filters to LLM outputs.
            *   **No Secrets in Prompts:** Ensure prompts never contain API keys or other static sensitive data; these belong in Azure Key Vault.
            *   **Data Minimization for LLM Context:** Provide LLMs only with necessary information.
            Adherence to OWASP LLM Top 10 guidelines is crucial.

## 7. Compliance & Auditing

*   **Audit Logs:** Robust audit logging is critical across the distributed system:
    *   M365 Persona Agents log key interaction events.
    *   Backend MCP Tools log their operations, including data access (with `tenantId`) and calls to external services (like LLMs).
    *   Cosmos DB Change Feed for auditing changes to `ArtifactMetadata`, `PersonaKnowledgeEntry`, and dynamic `PersonaConfiguration` data.
    *   Azure Firewall logs for network traffic.
    *   Logs should be correlated (e.g., using a shared `CorrelationId` initiated by the M365 Agent).
*   **Microsoft Purview:** Investigate integration with Microsoft Purview for data governance and compliance monitoring, especially if Nucleus M365 Agents are surfaced within M365 Copilot or handle sensitive enterprise data.

This document serves as the guiding framework for security decisions within the refactored Nucleus project. It will be updated as the architecture and Microsoft's agent ecosystem evolve.
