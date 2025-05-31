---
title: "ARCHIVED - Client Adapter - Discord (Superseded by Microsoft 365 Agents SDK Strategy)"
description: "ARCHIVED: This document detailed the Discord client adapter. Nucleus will now rely on the Microsoft 365 Agents SDK for channel integrations. Discord integration is pending M365 SDK channel support. Retained for historical context."
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
> This document describes a previous architectural approach for a custom Discord adapter that has been **superseded** by the strategic shift to the Microsoft 365 Agents SDK. Nucleus will not be developing a bespoke Discord adapter.
>
> Future Discord integration will depend on the availability and capabilities of a Discord channel connector provided through the Microsoft 365 Agents SDK (likely via Azure Bot Service channels).
>
> The information below is for **historical reference only** and does not reflect the current architecture or plans for Nucleus.

# ARCHIVED - Client Adapter: Discord (Historical Context)


## 1. Overview (Historical Context)

Enables interaction with Nucleus Personas within the Discord platform (Servers/Guilds, Channels, Threads).


## 2. Key Features & Considerations (Historical Context)

Following key features and considerations were identified for the Discord client adapter:

*   **Rich Interaction Model:** Discord's support for embeds, buttons, and select menus allows for rich message interactions.
*   **Message Content Intents:** Discord's privileged intents are required to access message content in events.
*   **Threaded Conversations:** Discord threads provide a way to have sub-conversations, which should be mapped to Nucleus conversation models.
*   **File Sharing:** Discord's file sharing capabilities need to be integrated for artifact handling.
*   **Rate Limits:** Discord imposes rate limits on message sending and other actions, which must be respected by the adapter.
*   **Ephemeral Messages:** Consideration for messages that are temporary and disappear after a short time.
*   **Integration with Discord's OAuth2:** For user authentication and authorization.


## 3. Authentication (Historical Context)

Requires a Discord Bot Token obtained from the Discord Developer Portal. Permissions (Scopes and Privileged Intents like Message Content) must be configured appropriately for the bot.


## 4. Event Handling (Historical Context)

The adapter's bot logic registers event handlers to receive incoming events from the Discord Gateway.

### 4.1. Message Events (Historical Context)

For message events, the adapter extracts standard information from the message object:

*   Author ID, Name (`message.author.id`, `message.author.name`)
*   Channel ID (`message.channel.id`)
*   Guild ID (if applicable) (`message.guild.id`)
*   Message Content (`message.content`, `message.attachments`)
*   Message ID (`message.id`)
*   Timestamp (`message.created_at`)

To enable implicit activation for replies, the adapter checks if the incoming message is a reply by inspecting the `message.reference` attribute.


### 4.2. File Share Events (Historical Context)

When a message with attachments is received, the adapter extracts the direct download URLs for each attachment.


### 4.3. Slash Commands (Historical Context)

Slash commands are registered commands that trigger specific bot actions. The adapter must handle these commands and map them to appropriate Nucleus API interactions.


## 5. Interaction with Nucleus API (Historical Context)

The adapter maps the extracted information from Discord events to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.

1.  **Construct API Request:** The adapter maps the extracted information to the `InteractionRequest` DTO.
2.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint with the populated `InteractionRequest` object as the body.
3.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result, the adapter translates this back into a Discord message and sends it to the originating channel.
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter sends an acknowledgement and needs a mechanism to receive the final result later and send it to the user.


## 6. `ArtifactMetadata` Mapping (Historical Context)

Mapping of Discord messages, channels, and threads to Nucleus `ArtifactMetadata` records.

*   **Messages:**
    *   Each Message gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: Generated using Discord's unique IDs.
    *   `sourceUri`: Constructed message link.
    *   `displayName`: First N characters of message content.
    *   `sourceCreatedAt`, `sourceLastModifiedAt`: Timestamps provided by the Discord API.
    *   `sourceCreatedByUserId`: Discord User ID.
    *   `originatingContext`: Could store `{ "guildId": "...", "channelId": "...", "threadId": "..." }`.
*   **Containers (Guilds, Channels, Threads):**
    *   Each can be represented by an `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Discord`.
    *   `sourceIdentifier`: e.g., `discord://guild/GUILD_ID`, `discord://channel/CHANNEL_ID`, `discord://thread/THREAD_ID`.
    *   `displayName`: Guild/Channel/Thread name.


## 7. Deployment (Historical Context)

Guidance on deploying the Discord adapter, considering factors like:

*   Hosting environment (e.g., Azure, AWS, on-premises)
*   Scalability considerations
*   Security configurations (e.g., securing the Discord Bot Token)
*   Monitoring and logging setup


## 8. Limitations and Future Considerations (Historical Context)

*   **Rate Limits:** Discord's rate limits on message sending and other actions.
*   **Message Content Intents:** Need for privileged intents to access message content.
*   **Threading Model:** Mapping Discord's threading model to Nucleus conversations.
*   **File Size Limits:** Discord's limits on file sizes for attachments.
*   **Ephemeral Messages:** Handling of temporary messages that disappear.
*   **OAuth2 Integration:** For user authentication and authorization.
*   **Future of Discord Integration:** Dependent on Microsoft 365 Agents SDK capabilities.
