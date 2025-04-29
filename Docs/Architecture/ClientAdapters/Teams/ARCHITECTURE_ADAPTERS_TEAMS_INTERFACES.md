---
title: Architecture - Teams Adapter Interface Mapping
description: Details how the Teams adapter maps Bot Framework Activity data to the InteractionRequest DTO for the Nucleus API Service in the API-First model.
version: 1.2
date: 2025-04-27
parent: ../ARCHITECTURE_ADAPTERS_TEAMS.md
---

# Teams Adapter: Interface Mapping

**Note:** Under the API-First architecture, the Teams adapter acts purely as a translator between the Bot Framework and the `Nucleus.Services.Api`. It **does not** directly implement interfaces like `ISourceFileReader` or `IOutputWriter` for file content I/O using Graph. Those responsibilities belong to the central `ApiService`.

This document focuses on how the Teams adapter maps information from a Bot Framework `Activity` object into the `InteractionRequest` DTO sent to the `Nucleus.Services.Api` (as defined in [API Client Interaction Pattern](../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)).

The primary implementation of this mapping logic resides within the [`TeamsAdapterBot.OnMessageActivityAsync`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs#L66) method.

## 1. `IPlatformMessage` Implementation

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

## 2. `IPersonaInteractionContext` Implementation (Adapter Context - Deprecated Concept)

The concept of an adapter-level `IPersonaInteractionContext` holding direct `ISourceFileReader` and `IOutputWriter` instances for Graph I/O is **deprecated** under the API-First model. The adapter's role is simplified:

1.  Receive the `Activity` within the `ITurnContext`.
2.  Create an `InteractionRequest` DTO.
3.  Map fields from the `Activity` and `ITurnContext` to the `InteractionRequest` DTO (as described in Section 1).
4.  Call the `Nucleus.Services.Api` with the `InteractionRequest`.
5.  Receive the response from the API.
6.  Use the `ITurnContext` (e.g., `TurnContext.SendActivityAsync`) to send the API's response back to the Teams user.

## 3. Handling API Responses

*   **Synchronous (`HTTP 200 OK`):** The adapter receives the response DTO from the API and formats it into one or more `Activity` objects sent back via `TurnContext.SendActivityAsync`.
*   **Asynchronous (`HTTP 202 Accepted`):** The adapter receives the `jobId`. How the adapter handles polling/waiting for the final result is TBD and part of the adapter's implementation detail (it might send an initial ack, then use proactive messaging or require the user to check status).
*   **No Action (`HTTP 204 No Content`):** The adapter typically does nothing.
*   **Errors (`HTTP 4xx/5xx`):** The adapter should log the error and potentially send an informative error message back to the user via `TurnContext.SendActivityAsync`.
