---
title: Nucleus Security Architecture & Data Governance
description: Outlines the security principles, data handling strategies, authentication mechanisms, and responsibilities for the Nucleus platform.
version: 1.7
date: 2025-05-06
---

# Nucleus: Security Architecture & Data Governance

## 1. Introduction

Security and responsible data governance are paramount for the Nucleus platform, especially given its interaction with potentially sensitive user information and its deployment in both individual (Cloud-Hosted) and team/organizational (Self-Hosted) contexts, as outlined in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). This document outlines the core security principles, data boundaries, authentication/authorization strategies, and responsibilities.

## 2. Data Governance & Boundaries

Nucleus is designed to process user data, not to be its primary custodian. The core principle is **Zero Trust for User File Content**: The Nucleus backend infrastructure (API Service, databases, queues) **MUST NEVER** store, persist, or have direct access to the raw byte content of user files.

*   **Primary Data Source:** Original user files (documents, images, etc.) **always reside exclusively in the user's designated, secure cloud storage** (e.g., Personal OneDrive/GDrive for Individuals, Team SharePoint/Shared Drives for Teams - see [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)).
*   **API File Interaction:**
    *   The Nucleus API Service (`Nucleus.Services.Api`) **DOES NOT support direct file uploads**. This capability has been explicitly removed to minimize attack surface and enforce the zero-trust principle.
    *   The API interacts with files **only** through secure `ArtifactReference` objects provided within API request payloads. These references point to content in the user's controlled storage.
    *   The [`IArtifactProvider`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs) implementations are responsible for resolving these references and retrieving content *ephemerally* when requested by a persona **during asynchronous processing within a background worker**.
*   **Nucleus ArtifactMetadata:**
    *   Stored within the **Nucleus [Database (Cosmos DB)](./04_ARCHITECTURE_DATABASE.md)**.
    *   Contains sanitized metadata *about* the source file (identifiers, hashes, timestamps, content type, [processing](./01_ARCHITECTURE_PROCESSING.md) status, originating context, etc.). Crucially, this metadata serves as a **Secure Index** for personas to understand available knowledge without accessing raw content.
    *   **Policy:** ArtifactMetadata **must not** store sensitive PII directly from the source document. Fields like `displayName` are acceptable.
*   **Nucleus Database (Cosmos DB):**
    *   Stores [`PersonaKnowledgeEntry`](./04_ARCHITECTURE_DATABASE.md) documents.
    *   Contains: **Derived knowledge, salient text snippets, and vector embeddings** (derived data), [persona-specific](./02_ARCHITECTURE_PERSONAS.md) analysis (`TAnalysisData`), `sourceIdentifier` linking back to the ArtifactMetadata, timestamps.
    *   **PII Consideration:** Text previews or `TAnalysisData` fields *might* contain sensitive information derived from the source. [Personas](./02_ARCHITECTURE_PERSONAS.md) generating this data must be designed with potential sensitivity in mind. Redaction/scrubbing strategies are persona-specific implementation details.
*   **Chat Logs:**
    *   Inbound chat messages are inspected for salience and may be processed by Nucleus components to generate responses.
    *   Salient portions of chat may be bundled into an artifact and placed into the user's long-term storage as an artifact that could be later retrieved for specifics.
    *   The Nucleus backend **does not persist raw chat conversation logs**.
*   **Data Flow:** User-provided `ArtifactReference` objects are passed to the API. When required during processing, the relevant `IArtifactProvider` uses the reference to fetch content *transiently* from the user's [storage](./03_ARCHITECTURE_STORAGE.md). This content flows ephemerally through Nucleus components. Only derived metadata, embeddings, previews, and analysis are persisted in the Nucleus [Database](./04_ARCHITECTURE_DATABASE.md). Original content is never stored by Nucleus.

### Data Flow & Security Boundaries Diagram

```mermaid
graph TD
    subgraph User Domain
        direction LR
        U[User]
        CS[User Cloud Storage]
        U -- Interacts --> CS
    end

    subgraph Nucleus Domain
        direction LR
        KV[Key Vault]
        Teams[Teams Client]
        Adapter[Nucleus Adapter/API]
        Proc[Processing/Orchestration (Worker)]
        Persona[Persona Modules]
        AI[AI Service]
        Cosmos[Cosmos DB]
        Api[Nucleus.Services.Api]
    end

    U -- Query or Trigger --> Teams
    Teams -- Request --> Adapter
    Adapter -- Authenticate --> Api
    Api -- Retrieves Secrets --> KV
    Api -- Uses OAuth Token --> CS
    Api -- Queues Task --> Proc
    Proc -- Processes Data --> Persona
    Persona -- Uses AI --> AI
    Proc -- Stores Metadata --> Cosmos
```

**Explanation:** This diagram illustrates the core security boundaries and data flow:
*   The **User's Domain** holds the original sensitive artifacts in their designated cloud storage.
*   The **Nucleus Domain** contains the application components:
    *   [Adapters/API](./05_ARCHITECTURE_CLIENTS.md) handle user interaction and external requests.
    *   [Processing/Orchestration](./01_ARCHITECTURE_PROCESSING.md) components (primarily **background workers**) handle the analysis and generation of derived data (knowledge entries, embeddings).
    *   [`Nucleus.Services.Api`](../../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs) orchestrates interactions, performs activation checks, **queues activated tasks**, manages OAuth flows (if applicable for storage providers), retrieves secrets, and handles `ArtifactReference` objects.
    *   The [Database (Cosmos DB)](./04_ARCHITECTURE_DATABASE.md) stores only derived metadata and knowledge.
    *   Key Vault secures credentials.
    *   **Interactions:**
        *   Adapters authenticate primarily with their respective platforms (e.g., Teams Bot authentication).
        *   The **`Nucleus.Services.Api`** authenticates with Key Vault to retrieve secrets for core backend services (Database, AI, etc.).
        *   The **API service orchestrates** interactions with external storage *via `ArtifactReference` resolution*. It may direct adapters on how to interact (e.g., providing specific tokens or parameters obtained securely) or handle the interaction directly via [`IArtifactProvider`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs) implementations within the context of the [`OrchestrationService`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs).
        *   Data flows ephemerally through processing and persona layers only when content retrieval is explicitly requested based on an `ArtifactReference`.
    *   Access to user data is governed by the permissions granted during the OAuth consent flow, respecting the source system's controls.

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
    *   **Cloud-Hosted:** Users authenticate via a primary identity provider (e.g., Microsoft Entra ID - formerly Azure AD, Google Identity) using OpenID Connect (OIDC).
    *   **Self-Hosted:** Configurable to use the organization's preferred IdP (Entra ID, Okta, etc.) via OIDC or potentially SAML.
*   **Storage Provider Authentication (OAuth 2.0):**
    *   To access files in the user's cloud storage (OneDrive, SharePoint, GDrive), Nucleus initiates a separate OAuth 2.0 authorization code flow, specific to that storage provider.
    *   User interaction to grant consent may be triggered via the [Client/Adapter](./05_ARCHITECTURE_CLIENTS.md) layer.
    *   However, the **`Nucleus.Services.Api` manages or validates the core OAuth flow** (e.g., handling the redirect URI, receiving the authorization code, exchanging it for tokens securely).
    *   The API service ensures specific, least-privilege scopes are requested (e.g., read-only access if sufficient, details in [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)).
    *   Tokens obtained are managed centrally by the API service.
*   **Token Management:**
    *   **Hosted Model (Transitory Access):**
        *   **MUST NOT** request the `offline_access` scope during the storage OAuth flow.
        *   Obtains short-lived **access tokens** only.
        *   Tokens are cached securely (e.g., encrypted in memory or a secure cache like Redis if scaled out) by the API service for the duration of the user's session or token lifetime.
        *   **No refresh tokens** are stored or used in this model.
    *   **Self-Hosted Model (Persistent Access):**
        *   **MAY** request the `offline_access` scope (configurable) to obtain **refresh tokens**.
        *   Refresh tokens **MUST** be stored encrypted at rest (e.g., in Azure Key Vault or equivalent).
        *   The **API service's background workers** (or potentially the API service itself when initiating tasks) securely use the refresh token to obtain new access tokens as needed.
*   **RBAC Delegation:** Nucleus operates within the permissions granted through the OAuth consent flow. Access control to specific files/folders within the user/team storage is **governed by the underlying storage provider's RBAC rules** ([Storage Architecture](./03_ARCHITECTURE_STORAGE.md)), not by Nucleus itself.

## 4. Secrets Management

All sensitive configuration values must be managed securely.

*   **Secrets Include:** [AI Service](./02_ARCHITECTURE_PERSONAS.md) API Keys, [Database](./04_ARCHITECTURE_DATABASE.md) Connection Strings, Message Queue Connection Strings, OAuth Client IDs/Secrets, Token Signing Keys, Refresh Tokens (Self-Hosted).
*   **Secure Storage:** Use secure secret management solutions appropriate for the [deployment](./07_ARCHITECTURE_DEPLOYMENT.md) environment:
    *   **Cloud-Hosted:** Azure Key Vault, integrated with App Services/Functions via Managed Identity.
    *   **Self-Hosted:** Azure Key Vault, HashiCorp Vault, Kubernetes Secrets (with appropriate backend), secure environment variables.
*   **Primary Consumer:** The **[`Nucleus.Services.Api`](../../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs)** is the primary consumer of secrets for core backend services (Database, AI, Queues, OAuth Client Secrets for storage providers). Adapters should only require secrets specific to their platform integration (e.g., Teams Bot ID/Password, Slack Bot Token), which should also be stored securely.
*   **Policy:** **NO hardcoded secrets** in source code or configuration files checked into version control.

## 5. Infrastructure Security

Secure configuration of underlying infrastructure (detailed in [Deployment Architecture](./07_ARCHITECTURE_DEPLOYMENT.md)) is essential.

*   **Network Security:** Employ standard practices like network segmentation, firewalls, and potentially Private Endpoints (especially for Self-Hosted access to PaaS services like [Cosmos DB](./04_ARCHITECTURE_DATABASE.md)/Service Bus).
*   **Service Configuration:** Apply least-privilege principles to service identities (e.g., Managed Identities), database access rules, storage account security settings (network rules, access keys), API security (authentication, rate limiting).

## 6. AI & Prompt Security

Leverage platform capabilities and best practices.

*   **Provider Safeguards:** Rely primarily on the **built-in safety features and content filters** provided by the chosen AI service providers (e.g., Azure OpenAI Content Filters, Google Gemini Safety Settings). Configure these appropriately according to policy and use case.
*   **Input Handling:** While provider filters are the main defense, basic input sanitization on user prompts can add a layer of defense against simple injection attempts.
*   **Data Minimization:** [Personas](./02_ARCHITECTURE_PERSONAS.md) should be designed to only send necessary context to the AI provider. The **Secure Metadata Index** helps identify relevant artifacts, and **Rich Ephemeral Context Retrieval** allows personas to fetch *only* the required full content transiently, minimizing unnecessary data exposure to the LLM.

## 7. Compliance & Auditing

*   **Audit Logs (Future):** While not detailed in V1.0, future iterations, particularly for team/enterprise use, may require robust audit logging of user actions, data access, and administrative changes. The architecture should facilitate adding such capabilities later (e.g., logging interactions via the [API layer](./07_ARCHITECTURE_DEPLOYMENT.md), [processing events](./01_ARCHITECTURE_PROCESSING.md)).

This document serves as the guiding framework for security decisions within the Nucleus project. It will be updated as the architecture evolves.
