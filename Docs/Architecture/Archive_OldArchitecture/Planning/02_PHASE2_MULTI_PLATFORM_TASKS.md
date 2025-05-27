---
title: "ARCHIVED - Phase 2: API Extension & Multi-Platform Client Adapters Tasks (Superseded by M365 Agent & MCP Tools Integration)"
description: "ARCHIVED: Detailed tasks for implementing the Nucleus API extensions and client adapters needed to support interactions from multiple platforms (Teams, Slack, Discord). This phase is superseded by the M365 Agent and MCP Tools integration strategy."
version: 1.3
date: 2025-05-25
---

# ARCHIVED - Phase 2: API Extension & Multi-Platform Client Adapters Tasks

**This document is ARCHIVED and SUPERSEDED by the M365 Agent & MCP Tools integration strategy outlined in the current [Roadmap](./00_ROADMAP.md) and the new Phase 2 planning document: [`02_PHASE2_M365_AGENT_ENHANCED_TASKS.md`](./02_PHASE2_M365_AGENT_ENHANCED_TASKS.md).**

**Original Epic:** [`EPIC-MULTI-PLATFORM`](./00_ROADMAP.md#phase-2-multi-platform-integration) (Note: Roadmap has been updated)
**Original Requirements:** [`02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`](../Requirements/02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md) (Note: Requirements document has been updated and retitled)

This document details the specific tasks required to complete Phase 2, focusing on extending the API to handle platform interactions and building the client adapters.

---

## `ISSUE-MP-API-01`: Enhance API for Platform Interactions

*Primary location: `Nucleus.Services.Api`*

*   [ ] **TASK-P2-API-01:** Enhance DTOs for platform interaction (`InteractionRequest`, `ArtifactReference`, `PlatformContext`, etc.) to include platform-specific details. (Ref Code: `Nucleus.Core/Models/Api/`)
*   [ ] **TASK-P2-API-02:** Enhance the unified interaction endpoint (`POST /api/v1/interactions`):
    *   [ ] Add logic to authenticate adapter requests (e.g., API keys, JWT).
    *   [ ] If `ArtifactReference` indicates a platform source (Teams, Slack, etc.), select and invoke the appropriate `IArtifactProvider` implementation (e.g., `TeamsGraphArtifactProvider`).
    *   [ ] The `IArtifactProvider` uses platform-specific APIs (e.g., MS Graph) and securely stored credentials to fetch content ephemerally.
    *   [ ] Integrate fetched ephemeral content with the existing processing pipeline (`IContentExtractor`, `IPersona`).
    *   [ ] Return `InteractionResponse` (sync result or Job ID).
*   [ ] **TASK-P2-API-03:** Enhance job status endpoint (`GET /api/v1/jobs/{jobId}/status`) if needed for platform interactions.
*   [ ] **TASK-P2-API-04:** Implement mechanism for notifying adapters of async job completion (e.g., Webhook callback system, requires adapters to register endpoints).
*   [ ] **TASK-P2-API-05:** Ensure API endpoints handle platform-specific context securely and associate it correctly with stored `ArtifactMetadata`.
*   [ ] **TASK-P2-API-06:** Define `IArtifactProvider` interface (if not already done in P1 extension). (Ref Code: `Nucleus.Abstractions/Interfaces/`)
*   [ ] **TASK-P2-API-07:** Implement platform-specific `IArtifactProvider`s (e.g., `TeamsGraphArtifactProvider`, `SlackApiArtifactProvider`, `DiscordAttachmentProvider`) within `Nucleus.Infrastructure` or dedicated provider projects.
    *   These providers will encapsulate logic to call platform APIs (e.g., MS Graph, Slack Web API, Discord API) using securely configured credentials.

## `ISSUE-MP-ADAPT-TEAMS-01`: Implement Teams Client Adapter

*Primary location: `Nucleus.Adapters.Teams`*

*   [ ] **TASK-P2-TEA-01:** Set up Teams Bot project using Bot Framework SDK.
*   [ ] **TASK-P2-TEA-02:** Register Azure AD application and configure Bot registration for Teams.
*   [ ] **TASK-P2-TEA-03:** Implement Bot logic to handle incoming Teams events (messages, mentions, file shares).
*   [ ] **TASK-P2-TEA-04:** Implement translation layer: Map Teams events/data to API DTOs:
    *   Extract user query/intent.
    *   If files are shared, **create `ArtifactReference` objects** containing Teams-specific identifiers (e.g., DriveItem ID, WebUrl, Site ID, File Name, MIME Type).
    *   Package into `InteractionRequest`.
*   [ ] **TASK-P2-TEA-05:** Implement authenticated HTTP calls to the `Nucleus.Services.Api` unified interaction endpoint (`POST /api/v1/interactions`) and status endpoint.
*   [ ] **TASK-P2-TEA-06:** Implement logic to render API responses (sync data, status updates, async completion notifications) into Teams messages (Text, Adaptive Cards).
*   [ ] **TASK-P2-TEA-07:** (If using webhooks for async results) Implement endpoint within the adapter to receive callbacks from the `ApiService` and register it.
*   [ ] **TASK-P2-TEA-08:** Handle Teams-specific authentication aspects required *by the adapter* itself (e.g., validating incoming requests from Bot Framework).

## `ISSUE-MP-ADAPT-SLACK-01`: Implement Slack Client Adapter

*Primary location: `Nucleus.Adapters.Slack`*

*   [ ] **TASK-P2-SLA-01:** Set up Slack App project (using preferred SDK/approach - Events API, Socket Mode).
*   [ ] **TASK-P2-SLA-02:** Configure Slack App permissions and event subscriptions.
*   [ ] **TASK-P2-SLA-03:** Implement logic to handle incoming Slack events (messages, mentions, file shares, slash commands).
*   [ ] **TASK-P2-SLA-04:** Implement translation layer: Map Slack events/data to API DTOs:
    *   Extract user query/intent.
    *   If files are shared, **create `ArtifactReference` objects** containing Slack-specific identifiers (e.g., File ID, URL Private, File Type).
    *   Package into `InteractionRequest`.
*   [ ] **TASK-P2-SLA-05:** Implement authenticated HTTP calls to `Nucleus.Services.Api` unified interaction endpoint (`POST /api/v1/interactions`) and status endpoint.
*   [ ] **TASK-P2-SLA-06:** Implement logic to render API responses into Slack messages (Text, Block Kit).
*   [ ] **TASK-P2-SLA-07:** (If using webhooks) Implement endpoint to receive callbacks from `ApiService`.
*   [ ] **TASK-P2-SLA-08:** Handle Slack request verification/authentication required by the adapter.

## `ISSUE-MP-ADAPT-DISCORD-01`: Implement Discord Client Adapter

*Primary location: `Nucleus.Adapters.Discord`*

*   [ ] **TASK-P2-DIS-01:** Set up Discord Bot project (using preferred library for Gateway interaction).
*   [ ] **TASK-P2-DIS-02:** Create Discord Application and Bot User, configure permissions.
*   [ ] **TASK-P2-DIS-03:** Implement logic to connect to Discord Gateway and handle relevant events (message creation, mentions, slash commands, attachments).
*   [ ] **TASK-P2-DIS-04:** Implement translation layer: Map Discord events/data to API DTOs:
    *   Extract user query/intent.
    *   If attachments are present, **create `ArtifactReference` objects** containing Discord attachment URLs, filenames, MIME types.
    *   Package into `InteractionRequest`.
*   [ ] **TASK-P2-DIS-05:** Implement authenticated HTTP calls to `Nucleus.Services.Api` unified interaction endpoint (`POST /api/v1/interactions`) and status endpoint.
*   [ ] **TASK-P2-DIS-06:** Implement logic to render API responses into Discord messages (Text, Embeds).
*   [ ] **TASK-P2-DIS-07:** (If using webhooks) Implement endpoint to receive callbacks from `ApiService`.
*   [ ] **TASK-P2-DIS-08:** Handle Discord Bot token authentication required by the adapter.

## `ISSUE-MP-PROCESS-01`: Enhance Content Extraction (API Responsibility)

*   [ ] **TASK-P2-EXT-01:** Review/Enhance `IContentExtractor` implementations within `Nucleus.Processing` to handle diverse content types retrieved ephemerally via `IArtifactProvider` (e.g., process Markdown common across platforms).

## `ISSUE-MP-UI-01`: Update Admin UI (Low Priority)

*(Dependent on `ArtifactMetadata` updates in API)*

*   [ ] **TASK-P2-UI-01:** Ensure `ArtifactMetadata` (saved by API) includes source platform/context info derived from `InteractionRequest` / `ArtifactReference`.
*   [ ] **TASK-P2-UI-02:** Update Admin UI (if applicable) to display source platform for processed items based on stored metadata.
*   [ ] **TASK-P2-UI-03:** Consider adding filtering by source platform in Admin UI.

## `ISSUE-MP-AUTH-01`: Secure Credential Handling

*   [ ] **TASK-P2-AUTH-01:** Ensure secure storage (e.g., Key Vault) for credentials used *by Adapters* to connect to platforms (Bot tokens, App Secrets).
*   [ ] **TASK-P2-AUTH-02:** Ensure secure storage for credentials used *by the `ApiService`'s IArtifactProvider implementations* to call platform APIs for ephemeral content retrieval (e.g., MS Graph App Secret, Slack Bot Token with file read scope, Discord Bot Token).
*   [ ] **TASK-P2-AUTH-03:** Implement/verify secure configuration loading for these credentials in Adapters and `ApiService`/Infrastructure.

---
