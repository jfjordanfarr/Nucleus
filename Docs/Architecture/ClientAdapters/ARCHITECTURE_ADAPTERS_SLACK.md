---
title: Client Adapter - Slack
description: Describes a client adapter which enables the interaction with Nucleus personas in Slack
version: 1.4
date: 2025-04-27
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Client Adapter: Slack


## Overview

Enables interaction with Nucleus Personas within a Slack workspace. This adapter adheres to the principles outlined in the main [Client Architecture Overview](../05_ARCHITECTURE_CLIENTS.md) and implements concepts from the [Common Adapter Interfaces](./ARCHITECTURE_ADAPTER_INTERFACES.md).

## Auth

Requires a Slack Bot Token obtained from the Slack App administration interface. Necessary permissions (scopes) must be granted, potentially including privileged intents depending on required functionality.

## Interaction Handling & API Forwarding

In line with the API-First principle, the Slack Adapter serves as a bridge between the Slack APIs (Events API, Web API, potentially RTM) and the central `Nucleus.Services.Api`.

1.  **Receive Message Event:** The adapter receives incoming message events (e.g., `message.channels`, `message.groups`, `message.im`) via the configured method (typically Slack Events API subscription).
2.  **Extract Basic Context:** The adapter parses the event payload to extract key information:
    *   User ID (`event.user`)
    *   Channel ID (`event.channel`)
    *   Team ID (`event.team` or from context)
    *   Message Content (`event.text`, `event.files`, `event.blocks`)
    *   Message Timestamp (`event.ts` - serves as the unique message ID)
    *   Event Timestamp (`event.event_ts`)
3.  **Detect Reply Context (Threads):** To enable implicit activation for replies within threads ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](./../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter checks for thread context:
    *   It inspects the event payload for a `thread_ts` field.
    *   If `thread_ts` exists *and* is different from the message's own `ts`, the message is a reply within a thread.
    *   The `thread_ts` value identifies the timestamp (`ts`) of the *original root message* that started the thread.
4.  **Construct API Request:** The adapter maps the extracted information to the `InteractionRequest` DTO used by the `Nucleus.Services.Api`.
    *   This includes user, channel/team, and message content details.
    *   Crucially, if the message is identified as a thread reply (i.e., `thread_ts` is present and differs from `ts`), the `thread_ts` value is populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformThreadId` or `RepliedToPlatformMessageId`).
5.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the `InteractionRequest` DTO.
6.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result (HTTP 200 OK + body), the adapter translates this back into a Slack message (text, Blocks, file upload) and sends it using the Slack Web API (e.g., `chat.postMessage`), potentially specifying the `thread_ts` to reply in the correct thread.
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter might send an acknowledgement (e.g., using `chat.postEphemeral` or a reaction). It requires a mechanism (TBD - polling `/interactions/{jobId}/status`, or webhooks) to get the final result later and post it to the appropriate channel/thread.

## Generated Artifact Handling

*   **Conversations & Files:** Slack stores message history and uploaded files persistently based on workspace plan/settings.
*   **Generated Artifacts (Outputs):** Personas generate artifacts (files, reports, visualization images/data). The flow is:
    1.  The `Nucleus.Services.Api` generates the artifact content.
    2.  The API response (`InteractionResponse`) sent back to the Slack Adapter includes this generated content (or a way to retrieve it).
    3.  The **Slack Adapter** receives the response and uses the Slack API (`files.upload` or newer methods) to **upload the received artifact content** into the relevant Slack channel/thread.
    4.  Upon successful upload, the Slack API provides details about the uploaded file (e.g., file ID, `permalink`).
    5.  The adapter (potentially via a follow-up call or async job completion) informs the `Nucleus.Services.Api` of the successful upload and provides the Slack file identifiers.
    6.  The `Nucleus.Services.Api` then creates the canonical `ArtifactMetadata` record for the generated artifact (the uploaded file), using the provided Slack identifiers for `sourceIdentifier` and `sourceUri`.

## Messaging

Handles individual messages within Slack channels or threads.

*   **Platform Representation:** Message objects, composed of text and/or structured 'Blocks'. Can include files.
*   **Nucleus `ArtifactMetadata` Mapping (for Messages):**
    *   Each message gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Slack`.
    *   `sourceIdentifier`: Generated using Slack's unique IDs (e.g., `slack://message/CHANNEL_ID/MESSAGE_TIMESTAMP`). Example: `slack://message/C67890/p1678886400.123456`.
    *   `sourceUri`: Slack permalink for the message (e.g., `https://yourteam.slack.com/archives/C67890/p1678886400.123456`).
    *   `displayName`: First N chars of message text.
    *   `sourceCreatedAt`, `sourceLastModifiedAt`: Timestamps from the Slack API (message `ts`).
    *   `sourceCreatedByUserId`: Slack User ID.
    *   `originatingContext`: Could store `{ "teamId": "...", "channelId": "...", "threadTs": "..." }`.

## Conversations

Manages the context of Workspaces, Channels, and Threads.

*   **Platform Representation:** Workspace (Team), Channel (Public/Private), Thread (sub-conversation initiated from a message).
*   **Nucleus `ArtifactMetadata` Mapping (for Containers):**
    *   Workspaces, Channels, and potentially Thread *root messages* can be represented by `ArtifactMetadata` records.
    *   `sourceSystemType`: Set to `Slack`.
    *   `sourceIdentifier`: e.g., `slack://team/T12345`, `slack://channel/C67890`, potentially `slack://thread/CHANNEL_ID/THREAD_ROOT_TS`.
    *   `displayName`: Workspace/Channel name.
*   **Relationships (`ArtifactMetadata`):**
    *   `parentSourceIdentifier`:
        *   Channel: Workspace `sourceIdentifier`.
        *   Message in Channel (not in thread): Channel `sourceIdentifier`.
        *   Thread's root message: Channel `sourceIdentifier`.
        *   Message replying within a Thread: The *thread root message's* `sourceIdentifier` (identified by `thread_ts` field in the message).
    *   `replyToSourceIdentifier`: Not explicitly a Slack concept like email, but could potentially identify the message being replied to if it's the start of a thread (using `thread_ts`).
    *   `threadSourceIdentifier`: Set to the *thread root message's* `sourceIdentifier` (e.g., `slack://message/CHANNEL_ID/THREAD_ROOT_TS`) for all messages within that thread (including the root message itself).

## Attachments (Inputs/Ingestion)

Handles files uploaded by users in Slack messages.

*   **Platform Representation:** File objects included in the `files` array of an incoming message event.
*   **Adapter Action:** When a message event contains files (`event.files`), the adapter extracts a reference for each file.
    *   This is typically the **Slack File ID** (`file.id`). The adapter may also need to retrieve a temporary download URL using the Slack Web API (`files.info` and the bot token, respecting `url_private_download`).
*   **API Request:** The adapter includes these **file identifiers or private download URLs** in the `SourceArtifactUris` field of the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.
*   **API Service Action:** The **`Nucleus.Services.Api`** receives these references. It uses the File ID or URL (along with the bot token if necessary for private URLs) to fetch the actual file content directly from Slack for ingestion and processing.
*   **Nucleus `ArtifactMetadata` Mapping (for Source File):**
    *   Each File processed gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Slack`.
    *   `sourceIdentifier`: Slack File ID (e.g., `slack://file/FABCDEF`).
    *   `sourceUri`: Slack permalink for the file (`file.permalink`).
    *   `parentSourceIdentifier`: The `sourceIdentifier` of the message the file was attached to.
    *   `displayName`: Filename (`file.name`).

## Rich Presentations and Embedded Hypermedia

Slack offers rich presentation options via Block Kit.

*   **Slack Blocks:** The primary method. Personas should generate structured output suitable for formatting as Block Kit JSON (text, images, dividers, context, buttons, menus, etc.).
*   **Markdown:** Slack uses `mrkdwn`, a variant of Markdown, within text blocks.
*   **Interactive Components:** Buttons, select menus, date pickers etc., can be included in Block Kit messages, enabling user interaction (requires bot interaction handling).
*   **Embedded Visualizations:** Complex interactive visualizations ([ARCHITECTURE_PROCESSING_DATAVIZ.md](../Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md)) are best handled by:
    1.  Generating as a static image (PNG) and uploading via `files.upload`, displayed inline.
    2.  Generating an interactive HTML file, uploading it via `files.upload`, and providing a link/button in the message.
    3.  Direct embedding within Slack messages is not feasible for complex JS-based visualizations.

## References

*   For details on initial feasibility and design considerations across Slack, Email, and Discord adapters, see: [Slack, Email, and Discord Adapter Report](../../HelpfulMarkdownFiles/Slack-Email-Discord-Adapter-Report.md)
