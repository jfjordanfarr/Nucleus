---
title: "ARCHIVED - Teams Adapter Interface Mapping - Superseded by M365 Agent SDK & MCP Patterns"
description: "ARCHIVED: This document detailed how the old Teams adapter mapped Bot Framework Activity data to an InteractionRequest DTO. This is superseded by M365 Agent SDK direct Activity handling and MCP Tool interaction patterns."
version: 1.3
date: 2025-05-26
parent: ../ARCHITECTURE_ADAPTERS_TEAMS.md
---

# ARCHIVED - Teams Adapter: Interface Mapping

**This document is ARCHIVED and SUPERSEDED by M365 Agent SDK direct `Activity` handling and MCP Tool interaction patterns.**

Valuable information regarding `Activity` object field mapping (e.g., `Activity.Conversation.TenantId`, `Activity.Attachments`) should be migrated to a new document: `Docs/Architecture/Agents/Teams/ARCHITECTURE_M365_AGENT_TEAMS_ACTIVITY_PROCESSING.md`. This new document will detail how a Nucleus M365 Agent for Teams processes incoming activities and prepares for backend interactions.

## Original 1. `IPlatformMessage` Implementation (Now Obsolete)

Corresponds primarily to a Bot Framework `Activity` object received by the bot.

*   **`MessageId`**: Maps to `Activity.Id`.
*   **`ConversationId`**: Maps to `Activity.Conversation.Id`. This could represent a channel (`teamsChannelId`), a group chat, or a 1:1 chat.
*   **`UserId`**: Maps to `Activity.From.AadObjectId` (or potentially `Activity.From.Id` if AAD mapping isn't available/required for a specific scenario).
*   **`Content`**: Maps primarily to `Activity.Text`. Mentions targeting the Nucleus bot (`@NucleusPersonaName`) should be stripped or handled appropriately during parsing.
*   **`Timestamp`**: Maps to `Activity.Timestamp` or `Activity.LocalTimestamp`.
*   **`SourceArtifactUris`**: Derived from `Activity.Attachments`.
    *   For file uploads (`application/vnd.microsoft.teams.file.download.info`), the adapter extracts information needed to construct a **Microsoft Graph reference URI** (e.g., combining Drive ID and Item ID) or potentially uses a temporary `contentUrl` if suitable for the API service's fetching mechanism. This reference URI is included in the `InteractionRequest.SourceArtifactUris` list.
    *   For inline images or other content, appropriate identifiers/URIs are extracted and added to `SourceArtifactUris`.
*   **`PlatformContext`**: Populated with relevant context from the `Activity`, especially `Activity.ReplyToId` (if present) to support the API's Activation Check, and potentially `Activity.Conversation.TenantId`, `Activity.ChannelData`, etc.

## Original 2. `IPersonaInteractionContext` Implementation (Adapter Context - Deprecated Concept)

The concept of an adapter-level `IPersonaInteractionContext` holding direct `ISourceFileReader` and `IOutputWriter` instances for Graph I/O is **deprecated** under the API-First model. The adapter's role is simplified:

1.  Receive the `Activity` within the `ITurnContext`.
2.  Create an `InteractionRequest` DTO.
3.  Map fields from the `Activity` and `ITurnContext` to the `InteractionRequest` DTO (as described in Section 1).
4.  Call the `Nucleus.Services.Api` with the `InteractionRequest`.
5.  Receive the response from the API.
6.  Use the `ITurnContext` (e.g., `TurnContext.SendActivityAsync`) to send the API's response back to the Teams user.

## Original 3. Handling API Responses (Now MCP Tool Responses)

*   **Synchronous (`HTTP 200 OK`):** The adapter receives the response DTO from the API and formats it into one or more `Activity` objects sent back via `TurnContext.SendActivityAsync`.
*   **Asynchronous (`HTTP 202 Accepted`):** The adapter receives the `jobId`. How the adapter handles polling/waiting for the final result is TBD and part of the adapter's implementation detail (it might send an initial ack, then use proactive messaging or require the user to check status).
*   **No Action (`HTTP 204 No Content`):** The adapter typically does nothing.
*   **Errors (`HTTP 4xx/5xx`):** The adapter should log the error and potentially send an informative error message back to the user via `TurnContext.SendActivityAsync`.
