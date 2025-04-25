---
title: Client Adapter - Discord
description: Describes a client edapter which enables the interaction with Nucleus personas in Discord
version: 1.3
date: 2025-04-23
---

# Client Adapter: Discord


## Overview

Enables interaction with Nucleus Personas within the Discord platform (Servers/Guilds, Channels, Threads).


## Auth

Requires a Discord Bot Token obtained from the Discord Developer Portal. Permissions (Scopes and Privileged Intents like Message Content) must be configured appropriately for the bot.


## Interaction Handling & API Forwarding

Following the API-First principle ([Memory: 21ba96d2](cci:memory/21ba96d2-36ea-4a88-8b6b-ed0fb4d8dd07)), the Discord Adapter translates between the Discord API (likely via a library like `discord.py` or `DSharpPlus`) and the central `Nucleus.Services.Api`.

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


## Persistent Storage

*   **Conversations:** Discord stores message history persistently according to server/channel settings.
*   **Generated Artifacts:** Personas can generate artifacts (files). The Discord Adapter **must** use the Discord API to upload these files back into the relevant channel/thread. Nucleus then creates `ArtifactMetadata` for the uploaded file using the `sourceIdentifier` and `sourceUri` provided by the Discord API upon successful upload. Local storage by Nucleus is avoided for generated content destined for Discord.


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


## Attachments

Handles files and rich content previews associated with messages.

*   **Platform Representation:** Attachment or Embed objects linked to a Message.
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   Attachments/Embeds can optionally have their own `ArtifactMetadata` (especially if they are files to be processed).
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: e.g., `discord://attachment/MESSAGE_ID/ATTACHMENT_ID`.
    *   `parentSourceIdentifier`: Message's `sourceIdentifier`.


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
