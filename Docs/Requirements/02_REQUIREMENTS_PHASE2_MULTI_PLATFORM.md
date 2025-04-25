---
title: "Requirements: Phase 2 - API Extension for Multi-Platform Integration (Teams First)"
description: Requirements for extending the Nucleus.Services.Api to support interactions from multiple collaboration platforms (Teams, Discord, Slack) via dedicated client adapters.
version: 1.1
date: 2025-04-24
---

# Requirements: Phase 2 - API Extension for Multi-Platform Integration

**Version:** 1.1
**Date:** 2025-04-24

## 1. Goal

To extend the **`Nucleus.Services.Api`** to support interactions originating from multiple collaboration platforms, starting with **Microsoft Teams**, followed by **Discord** and **Slack**. This involves defining necessary API endpoints and DTOs, and implementing thin, platform-specific **Client Adapters** that translate platform events into API calls and render API responses back to the user within their native platform.

## 2. Scope

*   **Platforms:** Microsoft Teams (initial focus), Discord, Slack.
*   **API Extension:** Defining and implementing endpoints within `Nucleus.Services.Api` to handle requests originating from these platforms, including ingestion initiation (via file references), queries, and status checks.
*   **Client Adapters:** Building dedicated adapters (`Nucleus.Adapters.Teams`, etc.) that act solely as clients to the `Nucleus.Services.Api`. They handle platform-specific protocols (e.g., Bot Framework) and translate data.
*   **Interaction:** Bot mentions (@PersonaName or platform equivalent), direct messages, file sharing within supported platform contexts.
*   **Functionality:** Enabling users to initiate analysis of platform-shared files (via references), respond to direct queries, provide status updates on asynchronous jobs, all orchestrated through the central API.
*   **Foundation:** Builds upon the core API infrastructure established in Phase 1 ([01_REQUIREMENTS_PHASE1_MVP_API.md](./01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md)).

## 3. Requirements

### 3.1. Admin Experience (Bot Setup & Configuration)

*   **REQ-P2-ADM-001:** An Administrator MUST be able to register and deploy the Nucleus Persona(s) as Bot Applications within their respective platforms (Microsoft 365 Tenant, Discord Server(s), Slack Workspace(s)).
*   **REQ-P2-ADM-002:** The Bot Application registrations MUST include necessary platform-specific permissions/scopes allowing the **`Nucleus.Services.Api`** (acting on behalf of the bot) to:
    *   Receive messages/events via the platform adapter.
    *   Read basic user profile information (for identification).
    *   Access files shared where the bot has access (using platform APIs, potentially requiring credentials configured in the `ApiService`).
    *   Allow the adapter to post messages/responses (text, cards, embeds, blocks) based on API results.
*   **REQ-P2-ADM-003:** Configuration for each Platform Adapter (e.g., App IDs, secrets, tokens needed *by the adapter* for platform communication) and any credentials needed *by the `ApiService`* for platform API calls (e.g., Graph API for Teams) MUST be securely manageable.

### 3.2. End-User Experience (Platform Interaction - Applies to Teams, Discord, Slack)

*   **REQ-P2-USR-001:** A User MUST be able to add/invite/install the Nucleus Persona bot within their platform environment, subject to policies.
*   **REQ-P2-USR-002:** A User MUST be able to initiate analysis by sharing a supported file where the bot is present and explicitly mentioning the bot (e.g., "@EduFlow analyze this"). The adapter sends relevant identifiers (e.g., file ID, share link, message context) to the API.
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
*   **REQ-P2-SYS-004:** Adapters MUST extract relevant platform context (user ID, channel/chat ID, message ID, file *references/identifiers*) and include it in the API requests.
*   **REQ-P2-SYS-005:** The **`Nucleus.Services.Api`** MUST provide endpoints to handle requests from platform adapters, including:
    *   An endpoint to initiate ingestion based on a file *reference* (e.g., `/api/v1/ingest/platform`). The API service is responsible for using this reference and configured credentials to securely retrieve the file content via platform-specific APIs (e.g., Microsoft Graph for Teams).
    *   An endpoint for general queries/interactions (e.g., `/api/v1/interactions`).
    *   An endpoint to query the status of asynchronous jobs (e.g., `/api/v1/jobs/{jobId}/status`).
*   **REQ-P2-SYS-006:** The **`Nucleus.Services.Api`** is responsible for:
    *   Authenticating requests from adapters.
    *   Retrieving source content based on platform references.
    *   Saving content to `IFileStorage` (if needed).
    *   Creating/managing `ArtifactMetadata`.
    *   Initiating and managing synchronous or asynchronous processing tasks.
    *   Interacting with LLMs and other backend services.
    *   Storing results and status.
*   **REQ-P2-SYS-007:** A mechanism MUST exist for the adapter to receive asynchronous results/notifications from the `ApiService` (e.g., API pushing updates via webhooks registered by the adapter, or adapter polling status endpoints).
*   **REQ-P2-SYS-008:** API responses (for sync requests, status queries, or async notifications) MUST provide sufficient information for the adapter to construct and send appropriate messages back to the user on the source platform.

## 4. Implementation Strategy

1.  **Define API Endpoints & DTOs:** Specify the contracts for `/api/v1/ingest/platform`, `/api/v1/interactions`, `/api/v1/jobs/{jobId}/status` and any necessary callback/webhook mechanisms within the API architecture documentation ([Docs/Architecture/Api](../../Architecture/Api/)).
2.  **Implement API Endpoints:** Build the core logic within `Nucleus.Services.Api` to handle these endpoints, including platform file retrieval (initially for Teams/Graph API) and processing orchestration.
3.  **Implement `TeamsAdapter`:** Build the Teams adapter as a pure API client using the Bot Framework SDK, translating events to API calls and responses back to Teams messages.
4.  **Implement `DiscordAdapter` / `SlackAdapter`:** Build subsequent adapters following the same client pattern, potentially identifying common adapter client logic that could be shared.

## 5. Non-Goals (Phase 2)

*   Full conversational memory across long interactions.
*   Proactive bot participation without explicit mentions.
*   Advanced platform-specific UI elements beyond basic formatted messages/responses.
