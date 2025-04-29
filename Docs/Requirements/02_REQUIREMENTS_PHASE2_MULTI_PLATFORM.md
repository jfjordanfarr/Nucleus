---
title: "Requirements: Phase 2 - Multi-Platform Integration (Teams First)"
description: Requirements for supporting Nucleus interactions from multiple collaboration platforms (Teams, Discord, Slack) via dedicated client adapters and the core API.
version: 1.2
date: 2025-04-27
---

# Requirements: Phase 2 - Multi-Platform Integration

**Version:** 1.2
**Date:** 2025-04-27

## 1. Goal

To enable the **`Nucleus.Services.Api`** to support interactions originating from multiple collaboration platforms, starting with **Microsoft Teams**, followed by **Discord** and **Slack**. This involves refining API endpoints and DTOs for the unified interaction model, and implementing thin, platform-specific **Client Adapters** that translate platform events into API calls (passing appropriate `ArtifactReference` objects) and render API responses back to the user within their native platform.

## 2. Scope

*   **Platforms:** Microsoft Teams (initial focus), Discord, Slack.
*   **API Extension:** Defining and implementing endpoints within `Nucleus.Services.Api` to handle requests originating from these platforms, including ingestion initiation (via file references), queries, and status checks, primarily through the unified `/api/v1/interactions` endpoint.
*   **Client Adapters:** Building dedicated adapters (`Nucleus.Adapters.Teams`, etc.) that act solely as clients to the `Nucleus.Services.Api`. They handle platform-specific protocols (e.g., Bot Framework) and translate data.
*   **Interaction:** Bot mentions (@PersonaName or platform equivalent), direct messages, file sharing within supported platform contexts.
*   **Functionality:** Enabling users to initiate analysis of platform-shared files (via references), respond to direct queries, provide status updates on asynchronous jobs, all orchestrated through the central API.
*   **Foundation:** Builds upon the core API infrastructure established in Phase 1 ([01_REQUIREMENTS_PHASE1_MVP_API.md](./01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md)).

## 3. Requirements

### 3.1. Admin Experience (Bot Setup & Configuration)

*   **REQ-P2-ADM-001:** An Administrator MUST be able to register and deploy the Nucleus Persona(s) as Bot Applications within their respective platforms (Microsoft 365 Tenant, Discord Server(s), Slack Workspace(s)).
*   **REQ-P2-ADM-002:** The Bot Application registrations MUST include necessary platform-specific permissions/scopes allowing the **Platform Adapter** to receive events and potentially access file metadata/references, and potentially allowing the **`IArtifactProvider` implementation** (used by the `ApiService`) to securely access file content when requested via an `ArtifactReference`:
    *   Receive messages/events via the platform adapter.
    *   Read basic user profile information (for identification).
    *   Access file metadata or generate secure references (`ArtifactReference`) for files shared where the bot has access.
    *   Allow the adapter to post messages/responses (text, cards, embeds, blocks) based on API results.
*   **REQ-P2-ADM-003:** Configuration for each Platform Adapter (e.g., App IDs, secrets, tokens needed *by the adapter* for platform communication) and any credentials needed *by the `IArtifactProvider` implementations* for platform API calls (e.g., delegated permissions via Graph API for Teams) MUST be securely manageable within the `ApiService` configuration.

### 3.2. End-User Experience (Platform Interaction - Applies to Teams, Discord, Slack)

*   **REQ-P2-USR-001:** A User MUST be able to add/invite/install the Nucleus Persona bot within their platform environment, subject to policies.
*   **REQ-P2-USR-002:** A User MUST be able to initiate analysis by sharing a supported file where the bot is present and explicitly mentioning the bot (e.g., "@EduFlow analyze this"). The adapter constructs an appropriate `ArtifactReference` (containing platform file ID, share link, message context, etc.) and sends it to the API's `/api/v1/interactions` endpoint.
*   **REQ-P2-USR-003:** The bot MUST acknowledge the request natively in the platform chat (e.g., "Okay, I'll ask EduFlow to analyze `[FileName]`. I'll notify you here when done."). This acknowledgement is triggered after the adapter receives a successful response (e.g., HTTP 202 Accepted) from the `ApiService` call.
*   **REQ-P2-USR-004:** Upon successful completion of asynchronous processing, the bot MUST post a notification message in the originating chat/channel, mentioning the initiating user, containing the analysis summary. This is triggered by the `ApiService` indicating completion (e.g., via webhook callback to the adapter, or the adapter polling a status endpoint).
*   **REQ-P2-USR-005:** Upon processing failure, the bot MUST post a failure notification similarly.
*   **REQ-P2-USR-006:** A User MUST be able to query the status of an ongoing asynchronous processing job by mentioning the bot (e.g., "@EduFlow status on `[FileName]`?"). The adapter forwards this query to a dedicated status endpoint on the `ApiService`.
*   **REQ-P2-USR-007:** In response to a status query API call, the adapter MUST receive status information from the `ApiService` and render an informative update within the chat.
*   **REQ-P2-USR-008:** A User MUST be able to ask the Persona general questions by mentioning it. The adapter forwards the query text and context to the `ApiService` interaction endpoint.

### 3.3. System Behavior (API Service & Client Adapters)

*   **REQ-P2-SYS-001:** Dedicated client adapters (`TeamsAdapter`, `DiscordAdapter`, `SlackAdapter`) MUST be implemented.
*   **REQ-P2-SYS-002:** Each adapter MUST handle incoming events/requests from its respective platform API/SDK.
*   **REQ-P2-SYS-003:** Each adapter MUST translate incoming platform events into appropriate DTOs and make authenticated HTTP calls to defined endpoints on the `Nucleus.Services.Api`.
*   **REQ-P2-SYS-004:** Adapters MUST extract relevant platform context (user ID, channel/chat ID, message ID, file `ArtifactReference` objects) and include it in the API requests to `/api/v1/interactions`.
*   **REQ-P2-SYS-005:** The **`Nucleus.Services.Api`** MUST provide endpoints to handle requests from platform adapters, primarily:
    *   The unified endpoint for general queries/interactions (`POST /api/v1/interactions`). This endpoint accepts `ArtifactReference` objects from adapters to initiate processing.
    *   An endpoint to query the status of asynchronous jobs (e.g., `GET /api/v1/jobs/{jobId}/status`).
*   **REQ-P2-SYS-006:** The **`Nucleus.Services.Api`** is responsible for:
    *   Authenticating requests from adapters.
    *   Orchestrating content retrieval via `IArtifactProvider` implementations, passing the `ArtifactReference` received from the adapter. **The API service itself does not directly retrieve platform file content.**
    *   Saving temporary content to `IFileStorage` (if needed during processing).
    *   Creating/managing `ArtifactMetadata`.
    *   Initiating and managing synchronous or asynchronous processing tasks (e.g., placing work on a queue).
    *   Interacting with LLMs and other backend services.
    *   Storing results and status.
*   **REQ-P2-SYS-007:** A mechanism MUST exist for the adapter to receive asynchronous results/notifications from the `ApiService` (e.g., API pushing updates via webhooks registered by the adapter, or adapter polling status endpoints).
*   **REQ-P2-SYS-008:** API responses (for sync requests, status queries, or async notifications) MUST provide sufficient information for the adapter to construct and send appropriate messages back to the user on the source platform.

## 4. Implementation Strategy

1.  **Refine API Endpoints & DTOs:** Ensure the contracts for `/api/v1/interactions` and `/api/v1/jobs/{jobId}/status` can handle platform adapter requests, including `ArtifactReference` objects, within the API architecture documentation ([Docs/Architecture/Api](../../Architecture/Api/)). Define any necessary callback/webhook mechanisms.
2.  **Implement API Endpoint Logic:** Enhance the core logic within `Nucleus.Services.Api` for the `/api/v1/interactions` endpoint to handle `ArtifactReference` objects originating from platforms and orchestrate processing, including invoking the correct `IArtifactProvider`.
3.  **Implement `IArtifactProvider` for Teams:** Create an implementation (e.g., `GraphArtifactProvider`) capable of resolving Teams/SharePoint `ArtifactReference` objects using configured credentials/permissions (e.g., Graph API).
4.  **Implement `TeamsAdapter`:** Build the Teams adapter as a pure API client using the Bot Framework SDK, translating events (including file shares) into `ArtifactReference` objects for API calls, and rendering API responses back to Teams messages.
5.  **Implement `DiscordAdapter` / `SlackAdapter`:** Build subsequent adapters and corresponding `IArtifactProvider` implementations following the same client/provider pattern, potentially identifying common logic.

## 5. Non-Goals (Phase 2)

*   Full conversational memory across long interactions.
*   Proactive bot participation without explicit mentions.
*   Advanced platform-specific UI elements beyond basic formatted messages/responses.
