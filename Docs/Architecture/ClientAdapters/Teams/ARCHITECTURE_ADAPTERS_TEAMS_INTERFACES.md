---
title: Architecture - Teams Adapter Interface Mapping
description: Details how the common client adapter interfaces (IPersonaInteractionContext, IPlatformMessage, etc.) are implemented within the Teams adapter using Bot Framework SDK and Microsoft Graph.
version: 1.0
date: 2025-04-13
---

# Teams Adapter: Interface Mapping

This document details the specific implementation patterns for the common client adapter interfaces defined in `../ARCHITECTURE_ADAPTER_INTERFACES.md` within the context of the [Microsoft Teams adapter (`Nucleus.Adapters.Teams`)](../ARCHITECTURE_ADAPTERS_TEAMS.md), leveraging the Bot Framework SDK and Microsoft Graph API.

## 1. `IPlatformMessage` Implementation

Corresponds primarily to a Bot Framework `Activity` object received by the bot.

*   **`MessageId`**: Maps to `Activity.Id`.
*   **`ConversationId`**: Maps to `Activity.Conversation.Id`. This could represent a channel (`teamsChannelId`), a group chat, or a 1:1 chat.
*   **`UserId`**: Maps to `Activity.From.AadObjectId`.
*   **`Content`**: Maps to `Activity.Text`. Mentions (`@NucleusPersonaName`) need to be stripped. If the activity represents a command (e.g., via Adaptive Card submission or specific text patterns), this content might need parsing.
*   **`Timestamp`**: Maps to `Activity.Timestamp` or `Activity.LocalTimestamp`.
*   **`SourceArtifactUris`**: Derived from `Activity.Attachments`.
    *   For file uploads (`application/vnd.microsoft.teams.file.download.info`), the attachment's `contentUrl` might provide a short-lived download URL, but more robustly, metadata within the attachment or context allows constructing a **Microsoft Graph URI** pointing to the file in SharePoint (channel) or OneDrive (chat). This Graph URI (e.g., referencing DriveItem ID and path) is the preferred representation for persistence in `ArtifactMetadata`.
    *   For inline images or other content, appropriate identifiers/URIs are extracted.

## 2. `IPersonaInteractionContext` Implementation

Represents the scope of processing a specific `Activity` (or potentially a sequence related by `Activity.Conversation.Id` and `Activity.ReplyToId`). It's typically created within the bot's `OnTurnAsync` handler or a specific command handler.

*   **`InteractionId`**: A unique ID generated for this turn/interaction (e.g., `Guid.NewGuid().ToString()`). Could potentially incorporate `Activity.Id`.
*   **`UserId`**: Sourced from `Activity.From.AadObjectId`.
*   **`PlatformId`**: Hardcoded as `"Teams"`.
*   **`TriggeringMessages`**: Contains the `IPlatformMessage` wrapper(s) around the relevant `Activity` object(s).
*   **`SourceFileReader`**: An instance of a `TeamsSourceFileReader` (see below). Requires access to a `GraphServiceClient` authenticated for the user or the application, scoped appropriately based on the interaction context (channel/chat).
*   **`OutputWriter`**: An instance of a `TeamsOutputWriter` (see below). Also requires access to a `GraphServiceClient`.
*   **Disposal (`Dispose`)**: Should release any resources, potentially including cancelling long-running Graph API calls if applicable.
*   **Additional Capabilities**: Might hold references to the `ITurnContext` for sending replies (`TurnContext.SendActivityAsync`) or accessing bot-specific state/services.

## 3. `ISourceFileReader` Implementation (`TeamsSourceFileReader`)

Relies heavily on the Microsoft Graph API.

*   **`SourceExistsAsync(string sourceUri, ...)`**: Uses `GraphServiceClient` to query Graph based on the `sourceUri` (which should be a Graph-compatible identifier like a DriveItem path or ID) to check for the item's existence and accessibility within the context's permissions.
*   **`OpenReadSourceAsync(string sourceUri, ...)`**: Uses `GraphServiceClient.Drives[driveId].Items[itemId].Content.Request().GetAsync()` or similar Graph calls to download the file content as a `Stream`. Requires appropriate Graph permissions (`Files.Read`, `Sites.Read.All`, etc.).
*   **`GetSourceMetadataAsync(string sourceUri, ...)`**: Uses `GraphServiceClient` to retrieve the `DriveItem` metadata (name, size, lastModifiedDateTime, etc.) corresponding to the `sourceUri`.

## 4. `IOutputWriter` Implementation (`TeamsOutputWriter`)

Also relies on the Microsoft Graph API to write files back to the `.Nucleus` structure in the appropriate SharePoint site (Team default library) or potentially OneDrive for Business (less common for shared outputs).

*   **`WriteOutputAsync(string personaId, string outputName, Stream contentStream, ...)`**:
    1.  Determines the target SharePoint site/library based on the interaction context (e.g., `Activity.ChannelData.Team.Id`).
    2.  Constructs the target path within the `.Nucleus` structure (e.g., `/{TeamLibrary}/.Nucleus/Personas/{personaId}/GeneratedOutputs/{outputName}`).
    3.  Uses `GraphServiceClient.Drives[driveId].Root.ItemWithPath(path).Content.Request().PutAsync<DriveItem>(contentStream)` or similar Graph API calls to upload the file. Requires appropriate Graph permissions (`Files.ReadWrite`, `Sites.ReadWrite.All`, etc.).
    4.  Returns the Graph ID or persistent URL of the created `DriveItem`.
*   **`WriteTextReplyAsync(string personaId, string replyContent, ...)`**:
    *   **Option 1 (Primary):** Sends the `replyContent` back to the user via `ITurnContext.SendActivityAsync()`. This is the standard bot reply mechanism. Does *not* typically save a separate file for the chat message itself. Returns `null`.
    *   **Option 2 (If Explicit File Needed):** Could potentially use `WriteOutputAsync` to save the `replyContent` as a `.md` file (e.g., `chat_reply_{timestamp}.md`) in the `.Nucleus` output folder, in addition to sending it via `SendActivityAsync`. Returns the Graph ID/URL if saved. This might be useful for logging or complex replies.
