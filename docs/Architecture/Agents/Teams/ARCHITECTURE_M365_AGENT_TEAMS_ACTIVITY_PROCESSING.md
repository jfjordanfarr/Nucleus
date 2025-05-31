---
title: "M365 Agent for Teams: Activity Processing & Backend Interaction"
description: "Details how the Nucleus M365 Persona Agent for Microsoft Teams processes incoming Bot Framework Activity objects, extracts key data, and prepares for interactions with backend MCP Tools and services."
version: 1.0
date: 2025-05-26
parent: ../../ARCHITECTURE_CLIENTS.md # Or a new ../ARCHITECTURE_AGENTS.md if created
---

# M365 Agent for Teams: Activity Processing & Backend Interaction

This document outlines how the Nucleus M365 Persona Agent, specifically when operating within Microsoft Teams, processes incoming `Activity` objects received via the Microsoft 365 Agents SDK. It details the extraction of relevant information and how this information is used to prepare for calls to backend Model Context Protocol (MCP) Tools and other Nucleus services.

This replaces the logic previously described in the archived `ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md` document, adapting it to the M365 Agent SDK context.

## 1. Receiving and Interpreting `Activity` Objects

The Nucleus M365 Persona Agent for Teams receives `Activity` objects from the M365 platform via the M365 Agents SDK. The agent's core logic (e.g., in its `Program.cs` or a dedicated activity handler class) is responsible for parsing these activities.

Key information extracted from the `Activity` includes:

*   **`Activity.Type`**: Determines the nature of the activity (e.g., `ActivityTypes.Message`, `ActivityTypes.MessageUpdate`, `ActivityTypes.MessageReaction`). The agent will primarily focus on `ActivityTypes.Message` for user queries and commands.
*   **`Activity.From.AadObjectId`**: The Azure AD Object ID of the user who sent the message. Crucial for user identification and context.
*   **`Activity.From.Name`**: Display name of the user.
*   **`Activity.Recipient.Name`**: The name of the bot/agent as it was addressed (useful if the agent has multiple aliases).
*   **`Activity.Conversation.Id`**: Identifier for the conversation (e.g., 1:1 chat ID, group chat ID, channel ID).
*   **`Activity.Conversation.ConversationType`**: Type of conversation (e.g., "personal", "channel", "groupChat").
*   **`Activity.Conversation.TenantId`**: The Azure AD Tenant ID where the interaction is occurring.
*   **`Activity.Text`**: The textual content of the message. This needs to be parsed for user intent, queries, and mentions of the agent.
    *   Mentions of the agent (e.g., `@NucleusAgent`) should be stripped from the core query content.
*   **`Activity.Attachments`**: A list of attachments included in the message. This is critical for file handling.
    *   For file attachments, the agent needs to extract identifiers to pass to the `Nucleus_FileAccess_McpServer`.
    *   Relevant properties within an attachment object (e.g., `attachment.Content.uniqueId`, `attachment.Content.downloadUrl`, `attachment.Name`, `attachment.ContentType`) are used to construct an `ArtifactReference` DTO for the MCP tool.
*   **`Activity.Value`**: Often contains data from Adaptive Card submissions or other interactive components.
*   **`Activity.ChannelData`**: Contains Teams-specific information, such as `channelData.TeamsChannelId`, `channelData.Team.Id`, `channelData.EventType`.
*   **`Activity.Id`**: The unique ID of the incoming activity.
*   **`Activity.ReplyToId`**: If this activity is a reply to a previous one, this ID can be used for context.
*   **`Activity.Timestamp`**: Timestamp of the activity.

## 2. Constructing `ArtifactReference` for MCP File Access Tool

When `Activity.Attachments` indicate a file has been shared, the M365 Agent for Teams is responsible for creating an `ArtifactReference` DTO (or a similar model defined by the `Nucleus_FileAccess_McpServer` contract). This DTO will contain the necessary information for the MCP tool to locate and fetch the file content using Microsoft Graph.

Example mapping to `ArtifactReference` fields:

*   **`ArtifactReference.ProviderHint`**: Could be set to "TeamsGraph" or similar.
*   **`ArtifactReference.ReferenceId`**: Populated with a stable Graph identifier, e.g., `DriveItem.Id` (often found in `attachment.Content.uniqueId` or derivable from other attachment properties).
*   **`ArtifactReference.FileName`**: From `attachment.Name`.
*   **`ArtifactReference.MimeType`**: From `attachment.ContentType`.
*   **`ArtifactReference.SourceUrl`**: Potentially `attachment.ContentUrl` if it's a direct, temporary download link, or a constructed Graph API URL if needed by the MCP tool.
*   **`ArtifactReference.Context`**: Could include `Conversation.Id`, `ChannelData.TeamsChannelId`, `From.AadObjectId` to provide context to the MCP tool for logging or fine-grained access checks if the tool implements them.

## 3. Preparing for MCP Tool Calls

Based on the parsed `Activity` and the user's intent (derived from `Activity.Text` or `Activity.Value`), the M365 Agent will decide which backend MCP Tool(s) to call.

For each MCP Tool call, the agent will:

1.  **Instantiate the MCP Tool Client**: Using the M365 Agents SDK mechanisms for discovering and invoking registered MCP Tools.
2.  **Prepare the Input DTO**: Construct the specific input DTO required by the target MCP Tool. This DTO will be populated with:
    *   Data directly from the `Activity` (e.g., user query, file references).
    *   Information from the agent's own state or configuration (e.g., `PersonaConfiguration`, `ITurnState`).
    *   Contextual information like `UserAadObjectId`, `ConversationId`, `TenantId`.
3.  **Invoke the Tool**: Make the call to the MCP Tool.

## 4. Handling MCP Tool Responses

The M365 Agent will receive responses from MCP Tools. These responses could be:

*   **Synchronous Data**: Direct results from the tool (e.g., a summary, a list of files, confirmation of an action).
*   **Error Information**: If the tool call failed.

The agent is responsible for:

1.  **Interpreting the Response**: Understanding the data or error returned by the tool.
2.  **Formatting for Teams**: Converting the tool's response into a user-friendly format for Microsoft Teams (e.g., plain text, Markdown, Adaptive Cards).
3.  **Sending the Reply**: Using the `ITurnContext.SendActivityAsync` (or similar M365 SDK methods) to send the formatted response back to the user in Teams.

## 5. Interaction with Other Backend Services (e.g., `Nucleus.Services.Api`)

If the M365 Agent needs to interact with other backend Nucleus services (like a central API for persistence, complex persona logic, or semantic memory retrieval as outlined in planning documents), it will follow a similar pattern:

1.  **Construct Request DTO**: Prepare the DTO required by the target API endpoint.
2.  **Make HTTP Call**: Use an `HttpClient` (properly managed, perhaps via `IHttpClientFactory`) to call the backend API.
3.  **Handle Response**: Process the API response and format it for the user in Teams.

This interaction is distinct from MCP Tool calls and would be used for capabilities not encapsulated within an MCP Tool.

## 6. Asynchronous Operations and Proactive Messaging

For long-running operations initiated by user requests:

1.  The M365 Agent might offload the task to a background queue (as per Phase 4 planning).
2.  It should send an immediate acknowledgment to the user in Teams (e.g., "I'm working on that, I'll let you know when it's ready.").
3.  The agent must save the necessary conversation reference (`Activity.GetConversationReference()` or M365 SDK equivalent) to be able to send a proactive message later.
4.  Once the background task completes, a separate process (e.g., a worker service) will use the saved conversation reference to notify the M365 Agent, which then delivers the result proactively to the user in the original Teams context.

This document provides a foundational understanding. Specific implementation details will evolve as the M365 Agent and MCP Tools are developed.
