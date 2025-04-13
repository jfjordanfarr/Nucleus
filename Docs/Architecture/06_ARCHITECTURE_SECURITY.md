---
title: Nucleus OmniRAG Security Architecture & Data Governance
description: Outlines the security principles, data handling strategies, authentication mechanisms, and responsibilities for the Nucleus OmniRAG platform.
version: 1.1
date: 2025-04-13
---

# Nucleus OmniRAG: Security Architecture & Data Governance

**Version:** 1.1
**Date:** 2025-04-13

## 1. Introduction

Security and responsible data governance are paramount for the Nucleus OmniRAG platform, especially given its interaction with potentially sensitive user information and its deployment in both individual (Cloud-Hosted) and team/organizational (Self-Hosted) contexts. This document outlines the core security principles, data boundaries, authentication/authorization strategies, and responsibilities.

## 2. Data Governance & Boundaries

Nucleus is designed to process user data, not to be its primary custodian.

*   **Primary Data Source:** Original user files (documents, images, etc.) **always reside in the user's designated cloud storage** (e.g., Personal OneDrive/GDrive for Individuals, Team SharePoint/Shared Drives for Teams). Nucleus interacts with these sources via API, respecting the permissions granted by the user/organization.
*   **Nucleus ArtifactMetadata:**
    *   Stored within the **Nucleus Database (Cosmos DB)**.
    *   Contain metadata *about* the source file (identifiers, hashes, timestamps, content type, processing status, originating context, etc.).
    *   **Policy:** ArtifactMetadata should avoid storing sensitive PII directly from the source document. Fields like `displayName` are acceptable.
*   **Nucleus Database (Cosmos DB):**
    *   Stores `PersonaKnowledgeEntry` documents.
    *   Contains: **Derived knowledge, salient text snippets, and vector embeddings** (derived data), persona-specific analysis (`TAnalysisData`), `sourceIdentifier` linking back to the ArtifactMetadata, timestamps.
    *   **PII Consideration:** Text previews or `TAnalysisData` fields *might* contain sensitive information derived from the source. Personas generating this data should be designed with potential sensitivity in mind. Redaction/scrubbing strategies are persona-specific implementation details.
*   **Chat Logs:**
    *   Inbound chat messages are inspected for salience and may be processed by Nucleus components (Functions/Workers) to generate responses.
    *   Salient portions of chat may be bundled into an artifact and placed into the user's long-term storage as an artifact that could be later retrieved for specifics.
    *   The Nucleus backend **does not persist raw chat conversation logs**.
*   **Data Flow:** Data fetched from user storage is processed transiently by Nucleus components (Functions/Workers). Only derived metadata, embeddings, previews, and analysis are persisted in the Nucleus Database. Original content is not duplicated or stored by Nucleus; while Self-Hosted deployments might integrate external organizational caching, the core Nucleus system remains ephemeral regarding source content.

### Data Flow & Security Boundaries Diagram

```mermaid
graph TD
    subgraph "User's Domain / Source System"
        direction LR
        U[fa:fa-user User]
        CS[fa:fa-cloud User Cloud Storage <br/> (e.g., SharePoint, OneDrive) <br/> **(Source of Truth for Artifacts)**]
        U -- Interacts --> CS
    end

    subgraph "Nucleus Domain (Hosted @ WWR / Cloud)"
        direction LR
        subgraph "Security & Config"
            KV[fa:fa-key Azure Key Vault <br/> (API Keys, DB Strings, OAuth Secrets)]
        end
        subgraph "Interaction & API Layer"
            Teams[fa:fa-users Teams Client / Other UI]
            Adapter[fa:fa-plug Nucleus Adapter/API <br/> (e.g., Container App)]
        end
        subgraph "Processing & Logic (Ephemeral)"
            Proc[fa:fa-cogs Processing Service <br/> (e.g., Container App/Functions) <br/> **(Handles Transient Sensitive Data)**]
            Persona[fa:fa-brain Persona Modules]
            AI[fa:fa-microchip AI Service <br/> (e.g., Azure OpenAI)]
        end
        subgraph "Persistent Nucleus Storage"
            Cosmos[fa:fa-database Azure Cosmos DB <br/> **(ArtifactMetadata & PersonaKnowledgeEntry - NO Raw Content)**]
        end
    end

    %% Interactions
    U -- 1. Query/Trigger via UI --> Teams
    Teams -- 2. Request --> Adapter
    Adapter -- 3. Reads Secrets --> KV
    Adapter -- 4. Uses OAuth Token --> CSAPI{Cloud Storage API}
    CSAPI -- 5. Fetches Source Data --> Adapter %% Data fetched on demand %%
    Adapter -- 6. Initiates Processing --> Proc
    Proc -- 7. Handles Ephemeral Data --> Persona
    Persona -- 8. Calls for Analysis --> AI
    AI -- 9. Returns Analysis --> Persona
    Persona -- 10. Returns Derived Knowledge --> Proc
    Proc -- 11. Reads/Writes Metadata & Derived Knowledge --> Cosmos
    Proc -- 12. Reads Secrets (e.g., AI Key) --> KV
    Proc -- 13. Sends Response --> Adapter
    Adapter -- 14. Returns Result --> Teams
    Teams -- 15. Displays to --> U

```

**Explanation:** This diagram illustrates the core security boundaries and data flow:
*   The **User's Domain** holds the original sensitive artifacts in their designated cloud storage.
*   The **Nucleus Domain** interacts with the User's Domain via APIs, using securely managed OAuth tokens obtained via user consent.
*   Sensitive configuration secrets (API keys, connection strings) are stored securely in **Azure Key Vault** (or equivalent).
*   Incoming source data is processed **ephemerally** within the Processing Service. This is the only place within the Nucleus domain where sensitive source data exists, and only transiently during active processing.
*   Persistent storage within Nucleus (**Cosmos DB**) is strictly limited to non-sensitive `ArtifactMetadata` and derived `PersonaKnowledgeEntry` data (snippets, structured analysis, vectors). **No raw source content is persisted here.**
*   Access to user data is governed by the permissions granted during the OAuth flow, respecting the source system's controls.

### Where Sensitive Information is Okay

The artifacts uploaded in conversation by users for use by a Nucleus persona, as well as any artifacts that Nucleus personas themselves generate (analyses, reports, interactive visualizations, notes, tasks, etc.) are stored in the user's designated cloud storage (e.g., OneDrive, GDrive, Sharepoint, etc.). In terms of persistent storage, this is the only location that is acceptable. We do not want to house sensitive information in Nucleus.

However, **transient** sensitive data is acceptable. This may come in the form of:
- In-memory session data (i.e. embeddings, responses, etc.)
- Transient data during processing (e.g., temporary files, in-progress calculations, etc.)
- Prompt/response caches provided by AI/Inference providers (so long as the inference provider has appropriate policies to prevent data retention)

### Where Sensitive Information is Not Okay

**The database!**
It is a simultaneous optimization and security feature to **not** house raw files or sensitive data in Cosmos. Instead, personas write metadata about/impressions of the various inputs that an artifact ID has been generated for (i.e. message, attachment, etc.). 

One additional benefit that this secure design allows is for tagging of files with the specific type of content they contain, should a future persona attempt to retrieve its raw contents once more. This can further enhance searches in the future. 

## 3. Authentication & Authorization (AuthN/AuthZ)

A multi-layered approach handles user identity and access to resources.

*   **User Authentication (Nucleus Login):**
    *   **Hosted Model:** Primarily uses Azure AD B2C or direct Social Sign-in (Google/Microsoft/Steam via OpenID Connect) to establish the user's identity within the Nucleus application. Steam identity may be particularly relevant for verticals like EduFlow distributed via desktop applications.
    *   **Self-Hosted Model:** Integrates with the organization's existing Identity Provider (Entra ID, Okta, etc.) via standard protocols (OpenID Connect, SAML).
    *   **Account Linking:** When supporting multiple identity providers (e.g., Google, Microsoft, Steam), a mechanism must exist to allow users to link these external identities to a single, unified Nucleus user profile. This ensures a consistent user experience regardless of the login method used.
*   **Service Authorization (Cloud Storage Access):**
    *   Requires a **separate, explicit OAuth 2.0 consent flow** initiated by the user *after* logging into Nucleus (e.g., "Connect Google Drive"). This applies regardless of the primary login method (including Steam).
    *   Nucleus requests specific, least-privilege scopes (e.g., read-only access if sufficient).
*   **Token Management:**
    *   **Hosted Model (Transitory Access):**
        *   **MUST NOT** request the `offline_access` scope during the storage OAuth flow.
        *   Obtains short-lived **access tokens** only.
        *   Stores access tokens securely, associated with the active backend user session (e.g., encrypted server-side cache).
        *   Tokens are discarded when the user session ends.
    *   **Self-Hosted Model (Optional Persistent Access):**
        *   Persistent background access requires an **explicit configuration flag** (`EnableBackgroundStorageAccess: true`).
        *   If enabled, **MAY** request the `offline_access` scope during storage OAuth flow.
        *   If requested and granted, receives a **refresh token** in addition to the access token.
        *   **CRITICAL:** The hosting organization is **solely responsible** for storing refresh tokens with extreme security (e.g., Azure Key Vault, HashiCorp Vault, HSM-backed encryption). Nucleus code provides the mechanism but relies on secure infrastructure configuration.
        *   Background workers use the refresh token to obtain new access tokens as needed.
*   **RBAC Delegation:** Nucleus operates within the permissions granted through the OAuth consent flow. Access control to specific files/folders within the user/team storage is **governed by the underlying storage provider's RBAC rules**, not by Nucleus itself.

## 4. Secrets Management

All sensitive configuration values must be managed securely.

*   **Secrets Include:** AI Service API Keys, Database Connection Strings, Message Queue Connection Strings, OAuth Client IDs/Secrets, Token Signing Keys, Refresh Tokens (Self-Hosted).
*   **Mandatory Practice:** Use secure secret management solutions appropriate for the deployment environment:
    *   **Cloud-Hosted:** Azure Key Vault, integrated with App Services/Functions via Managed Identity.
    *   **Self-Hosted:** Azure Key Vault, HashiCorp Vault, Kubernetes Secrets (with appropriate backend), secure environment variables.
*   **Policy:** **NO hardcoded secrets** in source code or configuration files checked into version control.

## 5. Infrastructure Security

Secure configuration of underlying infrastructure is essential.

*   **Network Security:** Employ standard practices like network segmentation, firewalls, and potentially Private Endpoints (especially for Self-Hosted access to PaaS services like Cosmos DB/Service Bus).
*   **Service Configuration:** Apply least-privilege principles to service identities (e.g., Managed Identities), database access rules, storage account security settings (network rules, access keys), API security (authentication, rate limiting).

## 6. AI & Prompt Security

Leverage platform capabilities and best practices.

*   **Provider Safeguards:** Rely primarily on the **built-in safety features and content filters** provided by the chosen AI service providers (e.g., Azure OpenAI Content Filters, Google Gemini Safety Settings). Configure these appropriately according to policy and use case.
*   **Input Handling:** While provider filters are the main defense, basic input sanitization on user prompts can add a layer of defense against simple injection attempts.
*   **Data Minimization:** Personas should be designed to only send necessary context (**retrieved snippets/derived data**, query) to the AI provider, minimizing exposure of potentially sensitive data.

## 7. Compliance & Auditing

*   **Audit Logs (Future):** While not detailed in V1.0, future iterations, particularly for team/enterprise use, may require robust audit logging of user actions, data access, and administrative changes. The architecture should facilitate adding such capabilities later (e.g., logging interactions via the API layer, processing events).

This document serves as the guiding framework for security decisions within the Nucleus OmniRAG project. It will be updated as the architecture evolves.
