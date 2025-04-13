---
title: Client Adapter - Slack
description: Describes a client adapter which enables the interaction with Nucleus personas in Slack
version: 1.1
date: 2025-04-13
---

# Client Adapter: Slack


## Overview

Enables interaction with Nucleus Personas within a Slack workspace.

## Auth

Requires a Slack Bot Token obtained from the Slack App administration interface. Necessary permissions (scopes) must be granted, potentially including privileged intents depending on required functionality.

## Generated Artifact Handling

*   **Conversations & Files:** Slack stores message history and uploaded files persistently based on workspace plan/settings.
*   **Generated Artifacts:** Personas generate artifacts (files, reports, visualization images/data). The Slack Adapter **must** use the Slack API (`files.upload` or newer methods) to upload these artifacts back into the relevant channel or thread. Nucleus then creates `ArtifactMetadata` for the uploaded file using the identifiers (`id`, `permalink`) returned by the Slack API.

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

## Attachments

Handles files uploaded to Slack.

*   **Platform Representation:** File objects, associated with channels or messages.
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   Each File gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Slack`.
    *   `sourceIdentifier`: Slack File ID (e.g., `slack://file/FABCDEF`).
    *   `sourceUri`: Slack permalink for the file.
    *   `parentSourceIdentifier`:
        *   File uploaded directly to channel: Channel `sourceIdentifier`.
        *   File attached to a message: Message `sourceIdentifier`.
    *   `displayName`: Filename.

## Rich Presentations and Embedded Hypermedia

Slack offers rich presentation options via Block Kit.

*   **Slack Blocks:** The primary method. Personas should generate structured output suitable for formatting as Block Kit JSON (text, images, dividers, context, buttons, menus, etc.).
*   **Markdown:** Slack uses `mrkdwn`, a variant of Markdown, within text blocks.
*   **Interactive Components:** Buttons, select menus, date pickers etc., can be included in Block Kit messages, enabling user interaction (requires bot interaction handling).
*   **Embedded Visualizations:** Complex interactive visualizations ([ARCHITECTURE_PROCESSING_DATAVIZ.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md:0:0-0:0)) are best handled by:
    1.  Generating as a static image (PNG) and uploading via `files.upload`, displayed inline.
    2.  Generating an interactive HTML file, uploading it via `files.upload`, and providing a link/button in the message.
    3.  Direct embedding within Slack messages is not feasible for complex JS-based visualizations.
