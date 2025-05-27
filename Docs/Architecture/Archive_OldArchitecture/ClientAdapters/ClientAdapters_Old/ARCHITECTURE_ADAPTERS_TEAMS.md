---
title: "ARCHIVED - Client Adapter - Teams (Superseded by M365 Agent & MCP Architecture)"
description: "ARCHIVED: This document detailed the Microsoft Teams client adapter, now superseded by the Microsoft 365 Agents SDK and Model Context Protocol (MCP) architecture. It is retained for historical context."
version: 1.0
date: 2025-05-25
see_also:
    - "./ARCHITECTURE_ADAPTER_INTERFACES.md"
    - "../05_ARCHITECTURE_CLIENTS.md"
    - "../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md" # Added link to new architecture
    - "../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md" # Added link to new architecture
---

> [!WARNING]
> **ARCHIVED DOCUMENT**
>
> This document describes a previous architectural approach that has been **superseded** by the adoption of the Microsoft 365 Agents SDK and the Model Context Protocol (MCP). The information below is for **historical reference only** and does not reflect the current architecture of Nucleus.
>
> Please refer to `../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md` and `../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md` for the current architectural direction.

# ARCHIVED - Client Adapter: Microsoft Teams (Historical Context)

The Client Adapter for Microsoft Teams served as a crucial component in the Nucleus architecture, enabling interaction between Nucleus personas and the Teams environment. This document provides an overview of the key features, authentication mechanisms, event handling, and interaction with the Nucleus API as it was originally designed.

## 1. Overview (Historical Context)

The Teams Client Adapter (`Nucleus.Adapters.Teams`) was designed to bridge the core Nucleus platform and the Microsoft Teams environment. It allowed users to interact with Nucleus Personas through familiar Teams interfaces like chat messages, channel conversations, and Adaptive Cards. The adapter leveraged the Microsoft Bot Framework SDK and Microsoft Graph API to handle communication, file access, and presentation within Teams.

As a client to the `Nucleus.Services.Api`, this adapter followed the interaction pattern described in [ARCHITECTURE_ADAPTER_INTERFACES.md](./ARCHITECTURE_ADAPTER_INTERFACES.md) (now archived), translating between Teams/Bot Framework activities and API calls. It did not implement core logic itself.

Implementation details for common interfaces are further specified in `ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md` (now archived).

## 2. Key Features (Historical Context)

Some of the key features of the Teams Client Adapter included:

- **Activity Processing:** The adapter processed various types of activities from Teams, such as messages, message reactions, and file shares.
- **API Translation:** It translated Teams activities into `InteractionRequest` objects for the Nucleus API.
- **Response Handling:** The adapter handled responses from the Nucleus API and translated them back into appropriate Teams messages or notifications.

## 3. Authentication (Historical Context)

The authentication mechanisms used by the Teams Client Adapter included:

- **Bot Authentication:** Relied on standard Bot Framework authentication mechanisms (App ID/Password or Managed Identity) configured during bot registration in Azure Bot Service / Azure AD.
- **User Authentication:** User identity was typically derived from the Teams context provided in incoming activities.
- **Graph API Access (for References):** The adapter required Graph API access to obtain unique identifiers or metadata for Teams entities (users, channels, messages, files) to be included as references in the `InteractionRequest`. Direct content retrieval via Graph API by the adapter was minimized; content access was primarily the responsibility of `Nucleus.Services.Api` based on references provided by the adapter. Permissions like `Sites.Selected`, `Files.ReadWrite` (for creating upload sessions if API delegates this), `User.Read`, `ChannelMessage.Read.All` might be needed for the adapter to gather these references. See implementation in [`GraphClientService.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs).

## 4. Event Handling (Historical Context)

Event handling was a critical aspect of the Teams Client Adapter, enabling it to respond to various events from the Teams environment.

### 4.1. Message Events (Historical Context)

For message events, the adapter performed the following steps:

1. **Receive Activity:** The adapter's bot logic (e.g., implementing `IBot` or using `ActivityHandler` in [`TeamsAdapterBot.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs)) received an incoming `Activity` object from the Bot Framework for events like `message`, `messageReaction`, etc.
2. **Extract Basic Context:** The adapter extracted standard information from the `Activity`:
    - User ID, Name (`activity.From.Id`, `activity.From.Name`)
    - Timestamp (`activity.Timestamp`)
3. **Detect Reply Context:** To enable implicit activation for replies ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](../../../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter specifically checked if the incoming message was part of a thread/reply chain:
    - It inspected the `Activity.Conversation.Id`. For threaded replies in channels, this ID often follows the pattern `19:CHANNEL_ID@thread.tacv2;messageid=PARENT_ACTIVITY_ID` or similar variants.
    - While `Activity.ReplyToId` exists, relying on parsing the `Conversation.Id` for the parent `messageid` is often more reliable for capturing the *thread* context.
4. **Construct API Request:** The adapter mapped the extracted information and any artifact references (see [File Attachments](#file-attachments)) to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.
    - This included mapping the user, channel/chat, message content, and attachment references.
    - Crucially, if a parent `messageid` was extracted in the previous step, it was populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformMessageId`).
5. **Forward to API:** The adapter made an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the populated `InteractionRequest` object as the body.
6. **Handle API Response:**
    - **Synchronous:** If the API returned an immediate result (HTTP 200 OK + body), the adapter translated this back into a Teams message (text, Adaptive Card) and sent it using `TurnContext.SendActivityAsync`. Notification logic might be delegated to [`TeamsNotifier.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsNotifier.cs).
    - **Errors:** The adapter logged the error and sent an informative message back to the user.

### 4.2. File Share Events (Historical Context)

File share events were handled by:

- **Receiving File Share Activity:** The adapter received `Activity` objects with `eventType` set to `fileShare`.
- **Extracting File Information:** It extracted file information from the activity, such as file ID, name, and URL.
- **Mapping to API Request:** The file information was mapped to the `InteractionRequest` to be sent to the Nucleus API.
- **Notifying Users:** Upon successful file processing, the adapter notified users in Teams about the file availability or any actions taken.

## 5. Interaction with Nucleus API (Historical Context)

The interaction with the Nucleus API was a core responsibility of the Teams Client Adapter. It involved:

- **Translating Activities to API Requests:** The adapter translated incoming Teams activities into `InteractionRequest` objects for the Nucleus API.
- **Forwarding Requests:** It forwarded these requests to the appropriate Nucleus API endpoints (e.g., `POST /api/v1/interactions`).
- **Handling API Responses:** The adapter handled responses from the API, translating them back into Teams messages or notifications.

## 6. Deployment (Historical Context)

Deployment of the Teams Client Adapter involved:

- **Azure Bot Service Registration:** The bot was registered in the Azure Bot Service, where App ID and Password (or Managed Identity) were configured.
- **Azure AD Permissions:** Necessary API permissions were granted in Azure AD for Graph API access.
- **Continuous Deployment Setup:** A CI/CD pipeline was typically set up for automated deployments.

## 7. Limitations and Future Considerations (Historical Context)

Some limitations and considerations for the Teams Client Adapter included:

- **Tight Coupling with Bot Framework SDK:** The adapter was tightly coupled with the Bot Framework SDK, which limited flexibility.
- **Monolithic API Dependency:** As a pure client to the `Nucleus.Services.Api`, any changes in the API required corresponding changes in the adapter.
- **Limited Error Handling:** Error handling was basic, with limited mechanisms for retry or fallback.

Future considerations for a potential re-architecture or replacement of the adapter could involve:

- **Decoupling from Bot Framework SDK:** Exploring a more decoupled architecture that does not rely on the Bot Framework SDK.
- **Enhanced Error Handling:** Implementing more robust error handling, including retry and fallback mechanisms.
- **Direct Graph API Integration:** Considering direct integration with Graph API for certain functionalities, reducing dependency on the Nucleus API.

## File Attachments (Historical Context)

Handling file attachments from Teams messages involved several steps, adhering to the API-First principle:

- For security considerations specific to deploying Bot Framework applications in Azure, refer to: [Secure Bot Framework Azure Deployment](../../HelpfulMarkdownFiles/Secure-Bot-Framework-Azure-Deployment.md)