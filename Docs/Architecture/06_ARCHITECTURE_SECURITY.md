---
title: Nucleus OmniRAG Security Architecture & Data Governance
description: Outlines the security principles, data handling strategies, authentication mechanisms, and responsibilities for the Nucleus OmniRAG platform.
version: 1.0
date: 2025-04-07
---

# Nucleus OmniRAG: Security Architecture & Data Governance

**Version:** 1.0
**Date:** 2025-04-07

## 1. Introduction

Security and responsible data governance are paramount for the Nucleus OmniRAG platform, especially given its interaction with potentially sensitive user information and its deployment in both individual (Cloud-Hosted) and team/organizational (Self-Hosted) contexts. This document outlines the core security principles, data boundaries, authentication/authorization strategies, and responsibilities.

## 2. Data Governance & Boundaries

Nucleus is designed to process user data, not to be its primary custodian.

*   **Primary Data Source:** Original user files (documents, images, etc.) **always reside in the user's designated cloud storage** (e.g., Personal OneDrive/GDrive for Individuals, Team SharePoint/Shared Drives for Teams). Nucleus interacts with these sources via API, respecting the permissions granted by the user/organization.
*   **Nucleus ArtifactMetadata:**
    *   Stored within Nucleus-managed storage (e.g., Azure Blob Storage, potentially co-located with user data in self-hosted scenarios).
    *   Contain metadata *about* the source file (identifiers, hashes, timestamps, content type, processing status, originating context, etc.).
    *   **Policy:** ArtifactMetadata should avoid storing sensitive PII directly from the source document. Fields like `displayName` are acceptable.
*   **Nucleus Database (Cosmos DB):**
    *   Stores `PersonaKnowledgeEntry` documents.
    *   Contains: Embeddings (derived data), text previews (potentially truncated snippets), persona-specific analysis (`TAnalysisData`), `sourceIdentifier` linking back to the ArtifactMetadata, timestamps.
    *   **PII Consideration:** Text previews or `TAnalysisData` fields *might* contain sensitive information derived from the source. Personas generating this data should be designed with potential sensitivity in mind. Redaction/scrubbing strategies are persona-specific implementation details.
*   **Chat Logs:**
    *   Active/recent chat history is stored **locally in the user's browser (IndexedDB)** for performance and privacy during the session.
    *   Long-term archival is **user-initiated via a "Save Chat" action**, which saves the transcript as a file to the **user's designated cloud storage**.
    *   The Nucleus backend **does not persist raw chat conversation logs**.
*   **Data Flow:** Data fetched from user storage is processed transiently by Nucleus components (Functions/Workers). Only derived metadata, embeddings, previews, and analysis are persisted in the Nucleus Database. Original content is not duplicated or stored by Nucleus in the Cloud-Hosted model; optional caching/storage might be configurable in Self-Hosted models.

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
*   **Data Minimization:** Personas should be designed to only send necessary context (retrieved chunks, query) to the AI provider, minimizing exposure of potentially sensitive data.

## 7. Compliance & Auditing

*   **Audit Logs (Future):** While not detailed in V1.0, future iterations, particularly for team/enterprise use, may require robust audit logging of user actions, data access, and administrative changes. The architecture should facilitate adding such capabilities later (e.g., logging interactions via the API layer, processing events).

This document serves as the guiding framework for security decisions within the Nucleus OmniRAG project. It will be updated as the architecture evolves.
