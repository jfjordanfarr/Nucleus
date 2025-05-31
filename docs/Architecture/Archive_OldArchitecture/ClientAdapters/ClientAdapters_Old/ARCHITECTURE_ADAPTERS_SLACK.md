---
title: "ARCHIVED - Client Adapter - Slack (Superseded by Microsoft 365 Agents SDK Strategy)"
description: "ARCHIVED: This document detailed the Slack client adapter. Nucleus will now rely on the Microsoft 365 Agents SDK for channel integrations. Slack integration is pending M365 SDK channel support. Retained for historical context."
version: 1.0
date: 2025-05-25
see_also:
    - "./ARCHITECTURE_ADAPTER_INTERFACES.md"
    - "../05_ARCHITECTURE_CLIENTS.md"
    - "../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md"
    - "../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md"
---

> [!WARNING]
> **ARCHIVED DOCUMENT**
>
> This document describes a previous architectural approach for a custom Slack adapter that has been **superseded** by the strategic shift to the Microsoft 365 Agents SDK. Nucleus will not be developing a bespoke Slack adapter.
>
> Future Slack integration will depend on the availability and capabilities of a Slack channel connector provided through the Microsoft 365 Agents SDK (likely via Azure Bot Service channels).
>
> The information below, including details on Slack APIs (Events API, Socket Mode), Slack Bolt for .NET, Block Kit, and specific `ArtifactMetadata` mapping, is for **historical reference only** and does not reflect the current architecture or plans for Nucleus.

# ARCHIVED - Client Adapter: Slack (Historical Context)

## 1. Overview (Historical Context)

Enables interaction with Nucleus Personas within a Slack workspace. This adapter adheres to the principles outlined in the main [Client Architecture Overview](../05_ARCHITECTURE_CLIENTS.md) and implements concepts from the [Common Adapter Interfaces](./ARCHITECTURE_ADAPTER_INTERFACES.md).

## 2. Key Features & Considerations (Historical Context)

### 2.1. Slack APIs: Events API vs. Socket Mode (Historical Context)

- **Events API:** A robust way to receive real-time events from Slack. Requires setting up a public endpoint and subscribing to events in the Slack app configuration.
- **Socket Mode:** An alternative to the Events API, allowing the app to receive events over a WebSocket connection. Useful for development or environments where exposing a public endpoint is challenging.

### 2.2. Slack Bolt for .NET (Historical Context)

A framework for building Slack apps in .NET, simplifying the process of handling events, commands, and interactions. It provides a structured way to define app behavior and integrates with the Slack API.

## 3. Authentication (Historical Context)

Requires a Slack Bot Token obtained from the Slack App administration interface. Necessary permissions (scopes) must be granted, potentially including privileged intents depending on required functionality.

## 4. Event Handling (Historical Context)

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

## 5. Interaction with Nucleus API (Historical Context)

The adapter maps the extracted information to the `InteractionRequest` DTO used by the `Nucleus.Services.Api`. This includes user, channel/team, and message content details. If the message is identified as a thread reply, the `thread_ts` value is populated into a dedicated field in the `InteractionRequest`.

## 6. `ArtifactMetadata` Mapping (Historical Context)

Slack-specific `ArtifactMetadata` mapping for messages and files includes:

- **Messages:**
  - `sourceSystemType`: Set to `Slack`.
  - `sourceIdentifier`: Generated using Slack's unique IDs.
  - `sourceUri`: Slack permalink for the message.
  - `displayName`: First N chars of message text.
  - `sourceCreatedAt`, `sourceLastModifiedAt`: Timestamps from the Slack API.
  - `sourceCreatedByUserId`: Slack User ID.
  - `originatingContext`: Could store `{ "teamId": "...", "channelId": "...", "threadTs": "..." }`.

- **Files:**
  - `sourceSystemType`: Set to `Slack`.
  - `sourceIdentifier`: Slack File ID.
  - `sourceUri`: Slack permalink for the file.
  - `parentSourceIdentifier`: The `sourceIdentifier` of the message the file was attached to.
  - `displayName`: Filename.

## 7. Rich Presentations with Block Kit (Historical Context)

Slack offers rich presentation options via Block Kit.

-   **Slack Blocks:** The primary method. Personas should generate structured output suitable for formatting as Block Kit JSON (text, images, dividers, context, buttons, menus, etc.).
-   **Markdown:** Slack uses `mrkdwn`, a variant of Markdown, within text blocks.
-   **Interactive Components:** Buttons, select menus, date pickers etc., can be included in Block Kit messages, enabling user interaction (requires bot interaction handling).
-   **Embedded Visualizations:** Complex interactive visualizations ([ARCHITECTURE_PROCESSING_DATAVIZ.md](../Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md)) are best handled by:
    1.  Generating as a static image (PNG) and uploading via `files.upload`, displayed inline.
    2.  Generating an interactive HTML file, uploading it via `files.upload`, and providing a link/button in the message.
    3.  Direct embedding within Slack messages is not feasible for complex JS-based visualizations.

## 8. Deployment (Historical Context)

Deployment considerations for the Slack adapter included:

- Hosting the adapter service (e.g., Azure App Service, AWS, on-premises).
- Configuring Slack app settings (OAuth, event subscriptions, command URLs).
- Ensuring network accessibility (firewall, NAT, SSL/TLS).
- Setting up monitoring and logging (Application Insights, Slack logs).

## 9. Limitations and Future Considerations (Historical Context)

-   **Rate Limits:** Slack API rate limits apply. Strategies for handling rate limits include exponential backoff, queuing requests, and optimizing API usage.
-   **Message Formatting:** Complex message layouts may require adjustments to fit within Slack's Block Kit limitations.
-   **Threading Model:** Slack's threading model (using `thread_ts`) must be carefully managed to ensure correct message replies and thread initiation.
-   **File Handling:** Files must be explicitly uploaded and linked to messages. Considerations for file size, type, and upload limits are necessary.
-   **Interactive Components:** Usage of buttons, menus, and other interactive elements requires careful planning of the interaction flow and state management.
-   **Deprecation of Legacy APIs:** Slack may deprecate older APIs or features. Keeping the integration up-to-date with Slack's API changes is essential.
-   **Compliance and Security:** Ensure compliance with data protection regulations (e.g., GDPR) and implement necessary security measures (e.g., data encryption, access controls).
