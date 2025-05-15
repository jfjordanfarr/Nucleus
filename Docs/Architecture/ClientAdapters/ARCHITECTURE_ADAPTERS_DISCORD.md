---
title: Client Adapter - Discord
description: Describes a client adapter which enables the interaction with Nucleus personas in Discord
version: 1.4
date: 2025-05-15
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Client Adapter: Discord


## Overview

Enables interaction with Nucleus Personas within the Discord platform (Servers/Guilds, Channels, Threads).


## Auth

Requires a Discord Bot Token obtained from the Discord Developer Portal. Permissions (Scopes and Privileged Intents like Message Content) must be configured appropriately for the bot.


## Interaction Handling & API Forwarding

Following the API-First principle, the Discord Adapter translates between the Discord API (likely via a library like `discord.py` or `DSharpPlus`) and the central `Nucleus.Services.Api`.

1.  **Receive Message Event:** The adapter's bot logic registers an event handler (e.g., `on_message`) to receive incoming message objects from the Discord Gateway.
2.  **Extract Basic Context:** The adapter extracts standard information from the message object:
    *   Author ID, Name (`message.author.id`, `message.author.name`)
    *   Channel ID (`message.channel.id`)
    *   Guild ID (if applicable) (`message.guild.id`)
    *   Message Content (`message.content`, `message.attachments`)
    *   Message ID (`message.id`)
    *   Timestamp (`message.created_at`)
3.  **Detect Reply Context:** To enable implicit activation for replies ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](./../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter specifically checks if the incoming message is a reply:
    *   It inspects the `message.reference` attribute.
    *   If `message.reference` is not null, it indicates a reply.
    *   The adapter extracts the `message_id` from `message.reference.message_id`. This is the ID of the message being replied to.
4.  **Construct API Request:** The adapter maps the extracted information to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.
    *   This includes mapping the user, channel/guild, and message content.
    *   Crucially, if a `message.reference.message_id` was extracted, it is populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformMessageId`).
5.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the populated `InteractionRequest` object as the body.
6.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result (HTTP 200 OK + body), the adapter translates this back into a Discord message (text, embed, file upload) and sends it to the originating channel using the Discord API.
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter might send an acknowledgement (e.g., a thinking emoji reaction or a simple message). It then needs a mechanism (TBD - polling `/interactions/{jobId}/status`, or webhooks) to receive the final result later and send it to the user.


## Persistent Storage & Artifact Handling

*   **Conversations:** Discord stores message history persistently according to server/channel settings.
*   **Generated Artifacts (Outputs):** Personas can generate artifacts (files). The flow for handling these is:
    1.  The `Nucleus.Services.Api` generates the artifact content (e.g., a text file, image, HTML visualization).
    2.  The API response (`InteractionResponse`) sent back to the Discord Adapter includes this generated content (or a means to retrieve it).
    3.  The **Discord Adapter** receives the response and uses the Discord API (e.g., `channel.send(file=...)`) to **upload the received artifact content** into the relevant Discord channel/thread.
    4.  Upon successful upload, the Discord API provides details about the uploaded file (e.g., a message ID containing the file, a direct attachment URL).
    5.  The adapter (potentially via a follow-up call or as part of async job completion handling) informs the `Nucleus.Services.Api` of the successful upload and provides the necessary Discord identifiers (URL, message ID, attachment ID).
    6.  The `Nucleus.Services.Api` then creates the canonical `ArtifactMetadata` record for the generated artifact, using the provided Discord identifiers for `sourceIdentifier` and `sourceUri`.
    This ensures the artifact resides natively within the Discord platform, while Nucleus maintains the authoritative metadata record.


## Messaging

Handles individual messages within Discord channels or threads.

*   **Platform Representation:** Message objects containing text, attachments, embeds, author, timestamp, etc.
*   **Nucleus `ArtifactMetadata` Mapping (for Messages):**
    *   Each Message gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: Generated using Discord's unique IDs (e.g., `discord://message/CHANNEL_ID/MESSAGE_ID`).
    *   `sourceUri`: Constructed message link (e.g., `https://discord.com/channels/GUILD_ID/CHANNEL_ID/MESSAGE_ID`).
    *   `displayName`: First N characters of message content.
    *   `sourceCreatedAt`, `sourceLastModifiedAt`: Timestamps provided by the Discord API.
    *   `sourceCreatedByUserId`: Discord User ID.
    *   `originatingContext`: Could store `{ "guildId": "...", "channelId": "...", "threadId": "..." }`.


## Conversations

Manages the context of Guilds, Channels, and Threads.

*   **Platform Representation:** Guild (Server), Channel (Text/Voice), Thread (sub-conversation).
*   **Nucleus `ArtifactMetadata` Mapping (for Containers):**
    *   Guilds, Channels, and Threads can each be represented by an `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: e.g., `discord://guild/GUILD_ID`, `discord://channel/CHANNEL_ID`, `discord://thread/THREAD_ID`.
    *   `displayName`: Guild/Channel/Thread name.
*   **Relationships (`ArtifactMetadata`):**
    *   `parentSourceIdentifier`:
        *   Channel: Guild's `sourceIdentifier`.
        *   Message in Channel: Channel's `sourceIdentifier`.
        *   Thread: Channel's `sourceIdentifier` (or initiating message's `sourceIdentifier`).
        *   Message in Thread: Thread's `sourceIdentifier`.
    *   `replyToSourceIdentifier`: Message's `sourceIdentifier` if it's a direct Discord reply.
    *   `threadSourceIdentifier`: Set to the Thread's `sourceIdentifier` for all messages within that thread.


## Attachments (Inputs/Ingestion)

Handles files attached to incoming Discord messages.

*   **Platform Representation:** `Attachment` objects linked to a `Message` object in the Discord API. Each attachment typically has properties like `id`, `filename`, and `url`.
*   **Adapter Action:** When a message event contains attachments (`message.attachments`), the adapter extracts the direct download **URL** for each attachment.
*   **API Request:** The adapter includes these **URLs** in the `SourceArtifactUris` field of the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.
*   **API Service Action:** The **`Nucleus.Services.Api`** receives these URLs and is responsible for using them to download the actual file content directly from Discord's CDN for ingestion and processing.
*   **Nucleus `ArtifactMetadata` Mapping (Optional):** While the primary focus is on ingesting the content, `ArtifactMetadata` can be created for the source Discord attachment itself:
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: e.g., `discord://attachment/MESSAGE_ID/ATTACHMENT_ID`.
    *   `parentSourceIdentifier`: The `sourceIdentifier` of the containing Discord message.


## Rich Presentations and Embedded Hypermedia

Discord offers several ways to present information richly.

*   **Markdown:** Supports a flavor of Markdown for text formatting.
*   **Embeds:** Discord's primary mechanism for rich, structured messages. Ideal for presenting summaries, links, small tables, or previews. Personas should generate content suitable for Embeds.
*   **Buttons & Select Menus:** Interactive components can be added to messages for user actions (requires bot interaction logic).
*   **Custom Apps/Bots & Visualizations:**
    *   While Nucleus acts as a bot, complex UIs might require dedicated Discord Apps.
    *   Interactive data visualizations ([ARCHITECTURE_PROCESSING_DATAVIZ.md](../Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md)) are best presented by generating the viz, saving it as an image/HTML file, uploading it back to Discord, and providing a link/preview embed, rather than direct embedding.


## References

*   For details on initial feasibility and design considerations across Slack, Email, and Discord adapters, see: [Slack, Email, and Discord Adapter Report](../../HelpfulMarkdownFiles/Slack-Email-Discord-Adapter-Report.md)
