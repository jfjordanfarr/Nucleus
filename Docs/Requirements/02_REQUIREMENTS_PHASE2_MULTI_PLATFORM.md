---
title: "Requirements: Phase 2 - Multi-Platform Bot Integration (Teams, Discord, Slack)"
description: Requirements for integrating Nucleus OmniRAG Personas as interactive bots within Microsoft Teams, Discord, and Slack.
version: 1.0
date: 2025-04-08
---

# Requirements: Phase 2 - Multi-Platform Bot Integration

**Version:** 1.0
**Date:** 2025-04-08

## 1. Goal

To extend the Nucleus OmniRAG platform by integrating Personas as interactive bots across multiple collaboration platforms: **Microsoft Teams, Discord, and Slack**. This phase enables users to interact with Personas directly within their preferred platform's chats and channels, initiate processing of shared files, and query the status of ongoing asynchronous tasks, all through a unified backend interface (`IPlatformAdapter`).

## 2. Scope

*   **Platforms:** Microsoft Teams, Discord, Slack.
*   **Interaction:** Bot mentions (@PersonaName or platform equivalent), direct messages, file sharing within supported platform contexts (channels, DMs, etc.).
*   **Functionality:** Initiating analysis of platform-shared files, responding to direct queries, providing status updates on asynchronous jobs.
*   **Foundation:** Builds upon the core backend infrastructure established in the MVP Email Integration ([01_REQUIREMENTS_MVP_EMAIL.md](./01_REQUIREMENTS_MVP_EMAIL.md)). The **Teams adapter will be implemented first** to validate the core patterns, followed closely by Discord and Slack implementations to ensure the `IPlatformAdapter` abstraction is robust.

## 3. Requirements

### 3.1. Admin Experience (Bot Setup & Configuration)

*   **REQ-P2-ADM-001:** An Administrator MUST be able to register and deploy the Nucleus Persona(s) as Bot Applications within their respective platforms (Microsoft 365 Tenant, Discord Server(s), Slack Workspace(s)).
*   **REQ-P2-ADM-002:** The Bot Application registrations MUST include necessary platform-specific permissions/scopes to:
    *   Read messages where installed/mentioned.
    *   Receive user mentions.
    *   Read basic user profile information (for identification).
    *   Access files shared where the bot has access (using platform APIs).
    *   Post messages/responses (text, cards, embeds, blocks).
*   **REQ-P2-ADM-003:** Configuration for each Platform Adapter (e.g., App IDs, secrets, tokens) MUST be manageable alongside other backend configurations (REQ-MVP-ADM-003).

### 3.2. End-User Experience (Platform Interaction - Applies to Teams, Discord, Slack)

*   **REQ-P2-USR-001:** A User MUST be able to add/invite/install the Nucleus Persona bot within their platform environment, subject to policies.
*   **REQ-P2-USR-002:** A User MUST be able to initiate analysis by sharing a supported file where the bot is present and explicitly mentioning the bot (e.g., "@EduFlow analyze this").
*   **REQ-P2-USR-003:** The bot MUST acknowledge the request natively in the platform chat (e.g., "Okay, analyzing `[FileName]` with EduFlow. I'll notify you here when done."). This triggers the *same backend asynchronous processing pipeline* as the Email MVP.
*   **REQ-P2-USR-004:** Upon successful completion of asynchronous processing, the bot MUST post a notification message in the originating chat/channel, mentioning the initiating user, containing the analysis summary (using platform-appropriate formatting).
*   **REQ-P2-USR-005:** Upon processing failure, the bot MUST post a failure notification in the originating chat/channel, mentioning the initiating user.
*   **REQ-P2-USR-006:** A User MUST be able to query the status of an ongoing asynchronous processing job by mentioning the bot (e.g., "@EduFlow status on `[FileName]`?").
*   **REQ-P2-USR-007:** In response to a status query, the bot MUST provide an informative status update within the chat. This response SHOULD include:
    *   Confirmation of the job.
    *   Current processing stage.
    *   Estimated progress (if feasible).
    *   Reassurance about resource consumption (if applicable/available).
    *   (Optional) Offer further actions (e.g., intermediate results), formatted appropriately for the platform.
*   **REQ-P2-USR-008:** A User MUST be able to ask the Persona general questions by mentioning it. The bot should leverage its AI model and potentially retrieved knowledge to respond within the platform context.

### 3.3. System Behavior (Platform Adapters & Backend)

*   **REQ-P2-SYS-001:** A generic `IPlatformAdapter` interface MUST be defined, abstracting common bot functionalities (receiving messages/events, sending messages, getting file streams, user identification).
*   **REQ-P2-SYS-002:** Concrete implementations (`TeamsAdapter`, `DiscordAdapter`, `SlackAdapter`) MUST be created, inheriting from `IPlatformAdapter`.
*   **REQ-P2-SYS-003:** Each adapter MUST handle incoming events from its respective platform API and translate them into a common internal format/event model where possible.
*   **REQ-P2-SYS-004:** Each adapter MUST parse incoming messages/events to identify user intent (analysis request, status query, general question) and extract relevant context (user ID, channel/chat ID, message ID, file references).
*   **REQ-P2-SYS-005:** For analysis requests, the responsible adapter MUST:
    *   Securely retrieve the shared file content via `IPlatformAdapter` methods (implemented using platform-specific APIs).
    *   Save the file to `IFileStorage`.
    *   Create `ArtifactMetadata` (linking to generic platform context: AdapterType, UserID, ConversationID, MessageID, etc.).
    *   **Trigger an in-process background task** within `Nucleus.Api` to initiate the asynchronous processing pipeline, passing necessary context (e.g., `IngestionId`).
    *   Post the acknowledgement message back via `IPlatformAdapter` methods.
*   **REQ-P2-SYS-006:** The backend processing service (running as a background task within the API host) MUST operate on the generic context stored in `ArtifactMetadata`.
*   **REQ-P2-SYS-007:** A notification service/mechanism MUST use the stored `AdapterType` and context in `ArtifactMetadata` to route completion/failure messages back to the correct `IPlatformAdapter` implementation for sending.
*   **REQ-P2-SYS-008:** The status query mechanism MUST allow querying `ArtifactMetadata` based on generic context and provide status details suitable for any adapter to format and return.
*   **REQ-P2-SYS-009:** The mechanism for handling general queries MAY involve a common API endpoint (`Nucleus.Api`) that interacts with retrieval/AI services and uses `IPlatformAdapter` to send the response back to the originating platform.

## 4. Implementation Strategy

1.  Define `IPlatformAdapter` interface.
2.  Implement `TeamsAdapter` first, refining `IPlatformAdapter` and backend interactions.
3.  Implement `DiscordAdapter` and `SlackAdapter` concurrently or sequentially, leveraging the established `IPlatformAdapter` patterns and ensuring minimal changes are needed to the core backend logic.

## 5. Non-Goals (Phase 2)

*   Full conversational memory across long interactions.
*   Proactive bot participation without explicit mentions.
*   Advanced platform-specific UI elements beyond basic formatted messages/responses.
