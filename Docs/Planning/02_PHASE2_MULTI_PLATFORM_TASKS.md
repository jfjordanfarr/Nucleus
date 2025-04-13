# Phase 2: Multi-Platform Integration Tasks

**Epic:** [`EPIC-MULTI-PLATFORM`](./00_ROADMAP.md#phase-2-multi-platform-integration)
**Requirements:** [`02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md`](../Requirements/02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md)

This document details the specific tasks required to complete Phase 2.

---

## `ISSUE-MP-INGEST-00-EMAIL`: Implement Email Ingestion Adapter

*   [ ] **TASK-P2-ING-E01:** Define `IPlatformAdapter` interface (ensure support for async sources like email) - *(Verify definition from P1 if not already done)*.
*   [ ] **TASK-P2-ING-E02:** Choose ingestion mechanism (e.g., Azure Function with Timer trigger + MailKit, Logic App connector). Document decision.
*   [ ] **TASK-P2-ING-E03:** Implement core `EmailAdapter` (`Nucleus.Adapters.Email`) logic: Connect, fetch new emails, handle basic authentication (app password/OAuth token).
*   [ ] **TASK-P2-ING-E04:** Implement email parsing: Extract sender, recipients, subject, date, plain text body, HTML body.
*   [ ] **TASK-P2-ING-E05:** Implement attachment handling: Identify attachments, prepare for storage.
*   [ ] **TASK-P2-ING-E06:** Implement hand-off to processing: Call internal service/API endpoint in `Nucleus.Api` to **trigger in-process background task** for the ingested email (metadata + attachment pointers).
*   [ ] **TASK-P2-ING-E07:** Implement basic error handling and logging for the ingestion service.
*   [ ] **TASK-P2-ING-E08:** Configure secure storage for email credentials (Key Vault).
*   [ ] **TASK-P2-ING-E09:** Implement mechanism to prevent re-processing of the same email (e.g., track Message-IDs).
*   [ ] **TASK-P2-ING-E10:** Implement mechanism to distinguish content submissions vs. direct queries if needed (e.g., subject line prefix, specific recipient). (Revisit P1 MVP assumption).

## `ISSUE-MP-INGEST-01`: Implement Teams Ingestion Adapter

*   [ ] **TASK-P2-ING-T01:** Research Teams integration options (Graph API webhooks, potentially bots if query integration requires it).
*   [ ] **TASK-P2-ING-T02:** Register Azure AD application for Graph API access.
*   [ ] **TASK-P2-ING-T03:** Implement `TeamsAdapter` inheriting `IPlatformAdapter` (`Nucleus.Adapters.Teams`).
*   [ ] **TASK-P2-ING-T04:** Implement webhook endpoint (e.g., Azure Function) to receive Teams notifications/messages.
*   [ ] **TASK-P2-ING-T05:** Implement message parsing for Teams format (text, mentions, potential attachments/links).
*   [ ] **TASK-P2-ING-T06:** Implement authentication/authorization for webhook calls.
*   [ ] **TASK-P2-ING-T07:** Integrate with internal processing: Call `Nucleus.Api` service/endpoint to **trigger in-process background task**.
*   [ ] **TASK-P2-ING-T08:** Update processing pipeline (background task logic) to handle Teams-originated artifacts/queries.

## `ISSUE-MP-INGEST-02`: Implement Slack Ingestion Adapter

*   [ ] **TASK-P2-ING-S01:** Research Slack integration options (Events API, Socket Mode).
*   [ ] **TASK-P2-ING-S02:** Create Slack App and configure permissions/events.
*   [ ] **TASK-P2-ING-S03:** Implement `SlackAdapter` inheriting `IPlatformAdapter` (`Nucleus.Adapters.Slack`).
*   [ ] **TASK-P2-ING-S04:** Implement endpoint (e.g., Azure Function) or background service for receiving Slack events.
*   [ ] **TASK-P2-ING-S05:** Implement message parsing for Slack format (mrkdwn, user mentions, attachments/files).
*   [ ] **TASK-P2-ING-S06:** Implement request verification/authentication for Slack events.
*   [ ] **TASK-P2-ING-S07:** Integrate with internal processing: Call `Nucleus.Api` service/endpoint to **trigger in-process background task**.
*   [ ] **TASK-P2-ING-S08:** Update processing pipeline (background task logic) to handle Slack-originated artifacts/queries.

## `ISSUE-MP-INGEST-03`: Implement Discord Ingestion Adapter

*   [ ] **TASK-P2-ING-D01:** Research Discord integration options (Bot User, Gateway API).
*   [ ] **TASK-P2-ING-D02:** Create Discord Application and Bot User.
*   [ ] **TASK-P2-ING-D03:** Implement `DiscordAdapter` inheriting `IPlatformAdapter` (`Nucleus.Adapters.Discord`).
*   [ ] **TASK-P2-ING-D04:** Implement background service or Function to connect to Discord Gateway.
*   [ ] **TASK-P2-ING-D05:** Implement message parsing for Discord format (Markdown, mentions, attachments).
*   [ ] **TASK-P2-ING-D06:** Handle Gateway events (message creation, etc.).
*   [ ] **TASK-P2-ING-D07:** Integrate with internal processing: Call `Nucleus.Api` service/endpoint to **trigger in-process background task**.
*   [ ] **TASK-P2-ING-D08:** Update processing pipeline (background task logic) to handle Discord-originated artifacts/queries.

## `ISSUE-MP-PROCESS-01`: Enhance Content Extraction

*   [ ] **TASK-P2-EXT-01:** Implement `MarkdownExtractor` (handle variants potentially needed for Slack/Discord).
*   [ ] **TASK-P2-EXT-02:** Enhance attachment handling in processing pipeline to correctly associate files from different platforms.
*   [ ] **TASK-P2-EXT-03:** Update `IContentExtractor` selection logic to accommodate new platform types/formats.

## `ISSUE-MP-QUERY-01`: Integrate Query into Teams

*   [ ] **TASK-P2-QRY-T01:** Design Teams interaction model (Bot command, Message Extension, Adaptive Card?).
*   [ ] **TASK-P2-QRY-T02:** Implement Teams Bot logic (if required) to handle user queries.
*   [ ] **TASK-P2-QRY-T03:** Integrate Bot/Extension with `Nucleus.Api` (new endpoint needed for platform queries).
*   [ ] **TASK-P2-QRY-T04:** Implement logic to call the appropriate `IPersona.HandleQueryAsync` based on Teams context.
*   [ ] **TASK-P2-QRY-T05:** Implement response formatting for Teams (Adaptive Cards?).

## `ISSUE-MP-QUERY-02`: Integrate Query into Slack

*   [ ] **TASK-P2-QRY-S01:** Design Slack interaction model (Slash command, Bot mention?).
*   [ ] **TASK-P2-QRY-S02:** Implement Slack command handler or Bot event handler.
*   [ ] **TASK-P2-QRY-S03:** Integrate handler with `Nucleus.Api` query endpoint.
*   [ ] **TASK-P2-QRY-S04:** Implement logic to call `IPersona.HandleQueryAsync` based on Slack context.
*   [ ] **TASK-P2-QRY-S05:** Implement response formatting for Slack (Block Kit?).

## `ISSUE-MP-QUERY-03`: Integrate Query into Discord

*   [ ] **TASK-P2-QRY-D01:** Design Discord interaction model (Slash command, Bot mention?).
*   [ ] **TASK-P2-QRY-D02:** Implement Discord command handler or Bot event handler.
*   [ ] **TASK-P2-QRY-D03:** Integrate handler with `Nucleus.Api` query endpoint.
*   [ ] **TASK-P2-QRY-D04:** Implement logic to call `IPersona.HandleQueryAsync` based on Discord context.
*   [ ] **TASK-P2-QRY-D05:** Implement response formatting for Discord (Embeds?).

## `ISSUE-MP-UI-01`: Update Web App UI

*   [ ] **TASK-P2-UI-01:** Update `ArtifactMetadata` model/storage to include source platform.
*   [ ] **TASK-P2-UI-02:** Update Admin UI (from P1) to display source platform for ingested items/status.
*   [ ] **TASK-P2-UI-03:** (Low Priority for P2) Consider adding basic filtering by source platform if feasible.

## `ISSUE-MP-AUTH-01`: Implement Platform Authentication

*   [ ] **TASK-P2-AUTH-01:** Implement OAuth flow or secure token handling for Teams adapter.
*   [ ] **TASK-P2-AUTH-02:** Implement OAuth flow or secure token handling for Slack adapter.
*   [ ] **TASK-P2-AUTH-03:** Implement Bot token handling securely for Discord adapter.
*   [ ] **TASK-P2-AUTH-04:** Store platform credentials/tokens securely (Key Vault).

---
