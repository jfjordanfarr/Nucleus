---
title: Client Adapter - Teams
description: Describes a Bot Framework SDK client adapter to bring Nucleus personas into Microsoft Teams.
version: 1.9
date: 2025-05-08
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Client Adapter: Teams

## Overview

The Teams Client Adapter (`Nucleus.Adapters.Teams`) serves as the bridge between the core Nucleus platform and the Microsoft Teams environment, as introduced in the main [Client Architecture document](../05_ARCHITECTURE_CLIENTS.md). It enables users to interact with Nucleus Personas through familiar Teams interfaces like chat messages, channel conversations, and potentially Adaptive Cards. The adapter leverages the Microsoft Bot Framework SDK and Microsoft Graph API to handle communication, file access, and presentation within Teams.

As a pure client to the `Nucleus.Services.Api`, this adapter follows the interaction pattern described in [ARCHITECTURE_ADAPTER_INTERFACES.md](./ARCHITECTURE_ADAPTER_INTERFACES.md), translating between Teams/Bot Framework activities and API calls. It does not implement core logic itself.

Implementation details for common interfaces are further specified in [ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md](./Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md).

## Auth

*   **Bot Authentication:** Relies on standard Bot Framework authentication mechanisms (App ID/Password or Managed Identity) configured during bot registration in Azure Bot Service / Azure AD.
*   **User Authentication:** User identity is typically derived from the Teams context provided in incoming activities.
*   **Graph API Access (for References):** The adapter may require Graph API access to obtain unique identifiers or metadata for Teams entities (users, channels, messages, files) to be included as references in the `InteractionRequest`. Direct content retrieval via Graph API by the adapter should be minimized; content access is primarily the responsibility of `Nucleus.Services.Api` based on references provided by the adapter. Permissions like `Sites.Selected`, `Files.ReadWrite` (for creating upload sessions if API delegates this), `User.Read`, `ChannelMessage.Read.All` might be needed for the adapter to gather these references. See implementation in [`GraphClientService.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs).

## Interaction Handling & API Forwarding

As per the API-First principle, the Teams Adapter acts as a translator between the Bot Framework SDK and the central `Nucleus.Services.Api`.

1.  **Receive Activity:** The adapter's bot logic (e.g., implementing `IBot` or using `ActivityHandler` in [`TeamsAdapterBot.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs)) receives an incoming `Activity` object from the Bot Framework for events like `message`, `messageReaction`, etc.
2.  **Extract Basic Context:** The adapter extracts standard information from the `Activity`:
    *   User ID, Name (`activity.From.Id`, `activity.From.Name`)
    *   Conversation ID, Type (`activity.Conversation.Id`, `activity.Conversation.ConversationType` - e.g., 'channel', 'personal')
    *   Tenant ID (`activity.Conversation.TenantId`)
    *   Message Content (`activity.Text`, `activity.Attachments`)
    *   Activity ID (`activity.Id`)
    *   Timestamp (`activity.Timestamp`)
3.  **Detect Reply Context:** To enable implicit activation for replies ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](../../../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter specifically checks if the incoming message is part of a thread/reply chain:
    *   It inspects the `Activity.Conversation.Id`. For threaded replies in channels, this ID often follows the pattern `19:CHANNEL_ID@thread.tacv2;messageid=PARENT_ACTIVITY_ID` or similar variants.
    *   It parses this string to extract the `messageid` value, which corresponds to the `Activity.Id` of the message being replied to.
    *   While `Activity.ReplyToId` exists, relying on parsing the `Conversation.Id` for the parent `messageid` is often more reliable for capturing the *thread* context.
4.  **Construct API Request:** The adapter maps the extracted information and any artifact references (see [File Attachments](#file-attachments)) to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.
    *   This includes mapping the user, channel/chat, message content, and attachment references.
    *   Crucially, if a parent `messageid` was extracted in the previous step, it is populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformMessageId`).
5.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the populated `InteractionRequest` object as the body.
6.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result (HTTP 200 OK + body), the adapter translates this back into a Teams message (text, Adaptive Card) and sends it using `TurnContext.SendActivityAsync`. Notification logic might be delegated to [`TeamsNotifier.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsNotifier.cs).
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter should send an acknowledgement (e.g., using `SendAcknowledgementAsync` from `TeamsNotifier`). It requires a mechanism (like the `IPlatformNotifier` interface implemented by `TeamsNotifier`) to receive the final result later and post it to the appropriate conversation.
    *   **Errors:** The adapter logs the error and sends an informative message back to the user.

## File Attachments

Handling file attachments from Teams messages involves several steps, adhering to the API-First principle:

1.  **Receive Attachment Info:** The Teams `Activity` object contains an `Attachments` collection. For files, each `Attachment` object provides:
    *   `ContentUrl`: A URL that can be used to download the file (often temporary or requiring Bot Framework authentication).
    *   `Name`: The original filename.
    *   `ContentType`: The MIME type of the file.

2.  **Obtain Stable Reference (If Necessary):**
    *   The `ContentUrl` provided by the Bot Framework might be temporary. For durable access, especially if `Nucleus.Services.Api` needs to fetch the content later, the adapter might need to use the Microsoft Graph API to get a more stable reference (e.g., a SharePoint drive item ID, a persistent sharing link, or a Graph download URL).
    *   This step requires the adapter to have appropriate Graph API permissions (e.g., `ChannelMessage.Read.All`, `ChatMessage.Read.All` to access message details, then permissions to access the underlying file storage like `Sites.Read.All` or `Files.Read.All` scopes, depending on where the file is stored).
    *   **Crucially, the adapter's primary role here is to obtain a *reference* (URI or structured identifier) that `Nucleus.Services.Api` can use, not to download the content itself.**

3.  **Construct `ArtifactReference`:** For each file, the adapter creates an `ArtifactReference` object (part of the `InteractionRequest` DTO) and populates it with:
    *   `Uri`: The stable URI or structured identifier (e.g., `graph://users/{userId}/messages/{messageId}/attachments/{attachmentId}`, or a Graph API direct download link if that's the agreed contract with the API).
    *   `ProviderId`: Potentially a hint like `"MSGraph"` or similar, if the API uses this to select the correct `IArtifactProvider`.
    *   `OriginalFileName`: The `Name` from the attachment info.
    *   `MimeType`: The `ContentType` from the attachment info.

4.  **Send to API:** The `InteractionRequest` (containing these `ArtifactReference` objects) is sent to `Nucleus.Services.Api`.

5.  **API Handles Content Retrieval:** `Nucleus.Services.Api` receives the `InteractionRequest`. When it needs the content of an artifact:
    *   It inspects the `ArtifactReference` (e.g., its URI and/or `ProviderId`).
    *   It uses an appropriate `IArtifactProvider` implementation (e.g., a `GraphArtifactProvider`) configured within the API to resolve the reference and fetch the actual file content directly from the source (e.g., SharePoint/OneDrive via Graph API).
    *   The adapter **does not** download the file content and send it to the API. It only sends the reference.

This approach centralizes data fetching logic within `Nucleus.Services.Api`, aligns with API-First, and allows the API to manage security, caching, and transformation of artifacts more effectively.

## Generated Artifact Handling

Nucleus integrates with the **native storage mechanisms** of Microsoft Teams, primarily SharePoint Online for channel files and OneDrive for Business for chat files. Nucleus itself **does not persist raw user content** within its own managed storage, adhering to zero-trust principles regarding user data. Instead, it interacts with the Team's designated storage location.

*   **Storing Outputs:** When the `Nucleus.Services.Api` generates output artifacts (e.g., summaries, reports, visualizations, logs) intended for persistent storage within the Team's context, the **`ApiService` itself writes these artifacts** to the appropriate location (typically the Team's SharePoint document library) using its own Graph capabilities. The **API response then includes references** (e.g., SharePoint URLs, Graph Item IDs) to these already-stored artifacts. The adapter's role is simply to present these references to the user (e.g., as links in a message or an Adaptive Card), not to perform the write operation itself.

    The API service determines the path, typically following this structure:
    *   **`.Nucleus/`**: Hidden root directory for Nucleus data.
        *   **`Personas/`**: Root directory for outputs generated by specific personas.
            *   **`{PersonaId}/`**: Directory specific to a persona (e.g., `EduFlowOmniEducator`).
                *   **`Outputs/`**: Contains the actual generated artifacts (files, visualizations).
                    *   **Example Path (determined by API):** `.Nucleus/Personas/CoderPersona/Outputs/Interaction_123/20250424T105000Z_AnalysisReport.md`
                *   **`Logs/` (or `ShowYourWork/`):**
                    *   If a Persona is configured to output logs or intermediate reasoning artifacts (e.g., "Show Your Work" markdown), the API response will contain this content and specify a target path within this subdirectory.
                    *   **Example Path (determined by API):** `.Nucleus/Personas/CoderPersona/Logs/Interaction_123/20250424T105000Z_Trace.md`

The `Nucleus.Services.Api` remains the source of truth for metadata ([Database Architecture](../04_ARCHITECTURE_DATABASE.md)). The adapter's role is solely to write the output artifacts as directed by the API response.

## Messaging

*   **Receiving:** Handles incoming Activity objects (e.g., `message`, `invoke`) via Bot Framework. Parses messages for user intent, @mentions, context, and attached files.
*   **Sending:** Sends messages back using `TurnContext.SendActivityAsync`. Can send plain text, mentions, and attachments (Adaptive Cards, File Consent Cards).
*   **Platform Representation:** Message objects within a channel or chat. Can contain text, @mentions, files, Adaptive Cards.
*   **Nucleus `ArtifactMetadata` Mapping (for Messages):**
    *   Each Message gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `MSTeams`.
    *   `sourceIdentifier`: Generated using Graph API IDs (e.g., `msteams://message/CHANNEL_ID/MESSAGE_ID` or `msteams://message/CHAT_ID/MESSAGE_ID`).
    *   `sourceUri`: Graph API endpoint URL or Teams deep link (e.g., `https://teams.microsoft.com/l/message/CHANNEL_ID/MESSAGE_ID?...`).
    *   `displayName`: First N chars of message text.
    *   `sourceCreatedAt`, `sourceLastModifiedAt`: Timestamps from Graph API.
    *   `sourceCreatedByUserId`: Azure AD User ID.
    *   `originatingContext`: Could store `{ "teamId": "...", "channelId": "...", "chatId": "...", "messageId": "..." }`.

## Conversations

*   Leverages Bot Framework conversation management (`ConversationReference`, state) for context.
*   **Platform Representation:** Team (M365 Group), Channel (within Team), Chat (1:1 or Group), Reply/Thread (in channel messages).
*   **Nucleus `ArtifactMetadata` Mapping (for Containers):**
    *   Teams, Channels, and Chats can each be represented by `ArtifactMetadata` records.
    *   `sourceSystemType`: Set to `MSTeams`.
    *   `sourceIdentifier`: Graph API IDs (e.g., `msteams://team/TEAM_ID`, `msteams://channel/CHANNEL_ID`, `msteams://chat/CHAT_ID`).
    *   `displayName`: Team/Channel/Chat name.
*   **Relationships (`ArtifactMetadata`):**
    *   `parentSourceIdentifier`:
        *   Channel: Team `sourceIdentifier`.
        *   Message in Channel (root): Channel `sourceIdentifier`.
        *   Reply in Channel Thread: Root message's `sourceIdentifier`.
        *   Message in Chat: Chat `sourceIdentifier`.
    *   `replyToSourceIdentifier`: For a reply message in a channel thread, this links to the *root message's* `sourceIdentifier`.
    *   `threadSourceIdentifier`: For channel messages, this would typically be the *root message's* `sourceIdentifier` for all messages in that thread.

## Attachments

Handles files uploaded by users in Teams messages. See also [ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md](./Teams/ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md) for details on how file *content* is retrieved.

*   **Platform Representation:** Files uploaded to Teams chats/channels, stored in SharePoint or OneDrive.
*   **Handling:** When a message activity contains attachments, the adapter extracts references to these attachments (e.g., Graph Download URL, SharePoint Item ID/URL via `activity.Attachments[n].Content.downloadUrl` or by querying Graph using IDs).
*   **Adapter Action:** The adapter includes these **references** (URIs or structured identifiers) in the `SourceArtifactUris` field of the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.
*   **API Service Action:** The **`Nucleus.Services.Api`** receives these references and is responsible for using them (along with appropriate authentication) to fetch the actual file content directly from the source (e.g., SharePoint/OneDrive via Graph API) for ingestion and processing.

## API Contract

The Teams Adapter interacts with the `Nucleus.Services.Api` using the DTOs defined in `Nucleus.Abstractions.Models.ApiContracts` (e.g., `AdapterRequest`, `AdapterResponse`, `ArtifactReference`).

## Key Code Files

*   [`TeamsAdapterBot.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs): Core logic for handling incoming activities from Teams and interacting with the `Nucleus.Services.Api`.
*   [`GraphClientService.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs): Manages communication with the Microsoft Graph API for tasks like obtaining file references or user/channel information.
*   [`TeamsNotifier.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsNotifier.cs): Handles sending messages and notifications back to the Teams user or channel.
*   [`TeamsAdapterConfiguration.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterConfiguration.cs): Defines the configuration settings for the Teams adapter.

## Future Considerations

*   **Enhanced Error Handling:** Implement more sophisticated error handling and logging mechanisms to improve the adapter's robustness and debuggability.
*   **Security Enhancements:** Continuously review and enhance the adapter's security posture, ensuring alignment with the latest security best practices and compliance requirements.
*   **Adaptive Card Enhancements:** Explore opportunities to leverage Adaptive Cards more extensively, potentially incorporating features like card actions, inputs, and templating to enrich the user experience.

## Rich Presentations and Embedded Hypermedia

A key capability of the Teams Adapter is presenting rich, interactive content generated by Personas, specifically **Pyodide-based Data Visualizations**.

### Pyodide Visualization Delivery Mechanism

This describes how visualizations, ***generated by the `Nucleus.Services.Api` backend***, are presented in Teams:

1.  **API Generates & Stores HTML:** The `Nucleus.Services.Api` executes the Persona's code, generating a self-contained HTML file (`viz.html`). The **API service then uploads this `viz.html` file** to the designated output location in the Team's SharePoint library (e.g., within the `.Nucleus` structure) using its Graph capabilities.
2.  **API Response with Reference:**
    *   The API response (`InteractionResponse`) sent back to the adapter includes a **reference** to the stored visualization, typically the persistent SharePoint URL (`webUrl`) or Graph Item ID of the `viz.html` file.
3.  **Cache URL Reference:**
    *   The adapter caches the received **SharePoint URL** locally (e.g., using `IMemoryCache`) keyed by a unique identifier (e.g., a generated `vizId` perhaps derived from the interaction or Graph ID). This avoids needing to reconstruct or look up the URL later.
4.  **Task Module Invocation:**
    *   Creates an Adaptive Card to send to the user/channel.
    *   The card includes:
        *   Contextual information about the visualization (e.g., "Persona generated a visualization: [Title]").
        *   An `Action.Execute` button (e.g., "View Interactive").
        *   The button's `data` payload includes `{"msteams": {"type": "task/fetch"}, "vizId": "UNIQUE_VIZ_ID"}` (using the ID from step 3).
        *   (Optional) An `Action.OpenUrl` button linking directly to the artifact stored in SharePoint (using the `webUrl` obtained from the API response or cache).
    *   Sends the Adaptive Card using `TurnContext.SendActivityAsync`.
5.  **Task Module Fetch Handling (`OnTeamsTaskModuleFetchAsync`):**
    *   The adapter implements the `OnTeamsTaskModuleFetchAsync` method (or equivalent Bot Framework v4 handler).
    *   When the user clicks the "View Interactive" button, this method is invoked.
    *   It parses the `vizId` from the incoming `taskModuleRequest.Data`.
    *   It validates the `vizId`.
    *   It constructs the URL pointing to an endpoint hosted *within this Teams Adapter application* (e.g., `https://<your-bot-service-domain>/api/renderViz?id={vizId}`).
    *   It returns a `TaskModuleResponse` containing a `TaskModuleContinueResponse` with the **constructed adapter URL**, desired dimensions (`height`, `width`), and `title` for the Task Module.
6.  **HTML Serving Endpoint (`/api/renderViz`):**
    *   The Teams Adapter ASP.NET Core application hosts a minimal API endpoint (e.g., mapped via `app.MapGet` or a dedicated controller).
    *   This endpoint receives GET requests with the `id` query parameter (`vizId`).
    *   It retrieves the cached **SharePoint URL** associated with the `vizId` from the cache (from step 3).
    *   If the URL is found:
        *   **Option A (Redirect):** It returns an **HTTP 302 Found** redirect response, pointing the Task Module's iframe directly to the SharePoint URL. This is simpler for the adapter but relies on SharePoint correctly setting `frame-ancestors` headers.
        *   **Option B (Iframe):** It returns a minimal HTML page (`Content-Type: text/html`) containing only an `<iframe>` whose `src` attribute is set to the SharePoint URL. This gives the adapter more control over the immediate container but adds a layer of nesting.
    *   The chosen option must ensure necessary **Content Security Policy (CSP)** headers are handled appropriately, either by SharePoint (Option A) or set by the adapter's minimal HTML page (Option B), allowing `frame-ancestors 'teams.microsoft.com'` etc.
    *   If the URL is not found in the cache, it returns `NotFound` or an appropriate error page.
7.  **Rendering:** Teams opens the Task Module, loads the URL from the adapter (`/api/renderViz`).
    *   Depending on Option A or B in step 6, the Task Module iframe is either redirected to SharePoint or renders the adapter's minimal iframe page, which in turn loads the `viz.html` from SharePoint.
    *   The JavaScript within the `viz.html` (served from SharePoint) then loads Pyodide, executes the Python script, and renders the interactive visualization within the Task Module context.
8.  **Export:** Export functionality (PNG, SVG, HTML) is embedded within the `viz.html` JavaScript (part of the content generated by the API service) and operates entirely client-side within the Task Module context.

This mechanism allows the Teams Adapter to seamlessly present complex, interactive Python visualizations generated by Personas directly within the Teams user interface, leveraging Task Modules as the hosting container.

*   **Embedded Visualization Handling Summary:** Interactive visualizations ([ARCHITECTURE_PROCESSING_DATAVIZ.md](../Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md)) are primarily delivered as self-contained HTML files, stored in SharePoint, and rendered within **Task Modules** via a serving endpoint hosted by the Teams Adapter Bot. Simpler representations might involve static images (PNG) embedded in Adaptive Cards.

## References

*   For a detailed report on the initial Teams adapter implementation and considerations, see: [Nucleus Teams Adapter Report](../../HelpfulMarkdownFiles/Nucleus Teams Adapter Report.md)
*   For security considerations specific to deploying Bot Framework applications in Azure, refer to: [Secure Bot Framework Azure Deployment](../../HelpfulMarkdownFiles/Secure-Bot-Framework-Azure-Deployment.md)