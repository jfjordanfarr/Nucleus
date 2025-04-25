# Phase 2: API Extension & Multi-Platform Client Adapters Tasks

**Epic:** [`EPIC-MULTI-PLATFORM`](./00_ROADMAP.md#phase-2-api-extension--multi-platform-integration)
**Requirements:** [`02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`](../Requirements/02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md)

This document details the specific tasks required to complete Phase 2, focusing on extending the API and building thin client adapters.

---

## `ISSUE-MP-API-01`: Implement API Endpoints for Platform Integration

*Primary location: `Nucleus.Services.Api`*

*   [ ] **TASK-P2-API-01:** Design and document DTOs for platform interaction requests/responses (including file references, platform context, status info).
*   [ ] **TASK-P2-API-02:** Implement API endpoint for initiating ingestion from platforms (e.g., `/api/v1/ingest/platform`).
    *   [ ] Include logic to authenticate adapter requests.
    *   [ ] Implement file retrieval logic using platform references (e.g., MS Graph API for Teams file IDs/links) based on received context and stored credentials.
    *   [ ] Integrate with `IFileStorage` and initiate backend processing (sync/async).
    *   [ ] Return appropriate response (e.g., 202 Accepted with Job ID).
*   [ ] **TASK-P2-API-03:** Implement API endpoint for general platform queries/interactions (e.g., `/api/v1/interactions`).
    *   [ ] Handle incoming queries with platform context.
    *   [ ] Integrate with core processing logic (LLM interaction, etc.).
    *   [ ] Return synchronous response or initiate async job.
*   [ ] **TASK-P2-API-04:** Implement API endpoint for querying job status (e.g., `/api/v1/jobs/{jobId}/status`).
*   [ ] **TASK-P2-API-05:** Implement mechanism for notifying adapters of async job completion/failure (e.g., Webhook callback system, requires adapters to register endpoints).
*   [ ] **TASK-P2-API-06:** Ensure API endpoints handle platform-specific context securely and associate it correctly with internal artifacts/jobs.
*   [ ] **TASK-P2-API-07:** Implement necessary configuration handling for platform API credentials used by the `ApiService` (e.g., Graph API secrets).

## `ISSUE-MP-ADAPT-TEAMS-01`: Implement Teams Client Adapter

*Primary location: `Nucleus.Adapters.Teams`*

*   [ ] **TASK-P2-TEA-01:** Set up Teams Bot project using Bot Framework SDK.
*   [ ] **TASK-P2-TEA-02:** Register Azure AD application and configure Bot registration for Teams.
*   [ ] **TASK-P2-TEA-03:** Implement Bot logic to handle incoming Teams events (messages, mentions, file shares).
*   [ ] **TASK-P2-TEA-04:** Implement translation layer: Map Teams events/data to API DTOs (e.g., extracting user query, file references, context).
*   [ ] **TASK-P2-TEA-05:** Implement authenticated HTTP calls to the `Nucleus.Services.Api` endpoints (`/ingest/platform`, `/interactions`, `/jobs/.../status`).
*   [ ] **TASK-P2-TEA-06:** Implement logic to render API responses (sync data, status updates, async completion notifications) into Teams messages (Text, Adaptive Cards).
*   [ ] **TASK-P2-TEA-07:** (If using webhooks for async results) Implement endpoint within the adapter to receive callbacks from the `ApiService` and register it.
*   [ ] **TASK-P2-TEA-08:** Handle Teams-specific authentication aspects required *by the adapter* itself (e.g., validating incoming requests from Bot Framework).

## `ISSUE-MP-ADAPT-SLACK-01`: Implement Slack Client Adapter

*Primary location: `Nucleus.Adapters.Slack`*

*   [ ] **TASK-P2-SLA-01:** Set up Slack App project (using preferred SDK/approach - Events API, Socket Mode).
*   [ ] **TASK-P2-SLA-02:** Configure Slack App permissions and event subscriptions.
*   [ ] **TASK-P2-SLA-03:** Implement logic to handle incoming Slack events (messages, mentions, file shares, slash commands).
*   [ ] **TASK-P2-SLA-04:** Implement translation layer: Map Slack events/data to API DTOs.
*   [ ] **TASK-P2-SLA-05:** Implement authenticated HTTP calls to `Nucleus.Services.Api` endpoints.
*   [ ] **TASK-P2-SLA-06:** Implement logic to render API responses into Slack messages (Text, Block Kit).
*   [ ] **TASK-P2-SLA-07:** (If using webhooks) Implement endpoint to receive callbacks from `ApiService`.
*   [ ] **TASK-P2-SLA-08:** Handle Slack request verification/authentication required by the adapter.

## `ISSUE-MP-ADAPT-DISCORD-01`: Implement Discord Client Adapter

*Primary location: `Nucleus.Adapters.Discord`*

*   [ ] **TASK-P2-DIS-01:** Set up Discord Bot project (using preferred library for Gateway interaction).
*   [ ] **TASK-P2-DIS-02:** Create Discord Application and Bot User, configure permissions.
*   [ ] **TASK-P2-DIS-03:** Implement logic to connect to Discord Gateway and handle relevant events (message creation, mentions, slash commands).
*   [ ] **TASK-P2-DIS-04:** Implement translation layer: Map Discord events/data to API DTOs.
*   [ ] **TASK-P2-DIS-05:** Implement authenticated HTTP calls to `Nucleus.Services.Api` endpoints.
*   [ ] **TASK-P2-DIS-06:** Implement logic to render API responses into Discord messages (Text, Embeds).
*   [ ] **TASK-P2-DIS-07:** (If using webhooks) Implement endpoint to receive callbacks from `ApiService`.
*   [ ] **TASK-P2-DIS-08:** Handle Discord Bot token authentication required by the adapter.

## `ISSUE-MP-PROCESS-01`: Enhance Content Extraction (Minor Update)

*   [ ] **TASK-P2-EXT-01:** Review `IContentExtractor` implementations (e.g., Markdown) to ensure compatibility with formats potentially passed *through* the API from different platforms. (Core extraction logic remains within API service boundary).

## `ISSUE-MP-UI-01`: Update Web App UI

*(Dependent on `ArtifactMetadata` updates in API)*

*   [ ] **TASK-P2-UI-01:** Update `ArtifactMetadata` definition (in Domain/Api DTOs) to include source platform/context info provided by adapters.
*   [ ] **TASK-P2-UI-02:** Update Admin UI (if applicable) to display source platform for ingested items/status based on updated metadata.
*   [ ] **TASK-P2-UI-03:** (Low Priority) Consider adding filtering by source platform.

## `ISSUE-MP-AUTH-01`: Secure Credential Handling

*   [ ] **TASK-P2-AUTH-01:** Ensure secure storage (e.g., Key Vault) for credentials used *by Adapters* to connect to platforms (Bot tokens, App Secrets).
*   [ ] **TASK-P2-AUTH-02:** Ensure secure storage for credentials used *by the `ApiService`* to call platform APIs (e.g., Graph API tokens/secrets).
*   [ ] **TASK-P2-AUTH-03:** Implement/verify secure configuration loading for these credentials in both Adapters and the ApiService.

---
