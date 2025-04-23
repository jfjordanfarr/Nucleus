---
title: Client Adapter - Discord
description: Describes a client edapter which enables the interaction with Nucleus personas in Discord
version: 1.2
date: 2025-04-22
---

# Client Adapter: Discord


## Overview

Enables interaction with Nucleus Personas within the Discord platform (Servers/Guilds, Channels, Threads).


## Auth

Requires a Discord Bot Token obtained from the Discord Developer Portal. Permissions (Scopes and Privileged Intents like Message Content) must be configured appropriately for the bot.


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
