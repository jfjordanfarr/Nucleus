---
title: Client Adapter - Teams
description: Describes a Bot Framework SDK client adapter to bring Nucleus personas into Microsoft Teams.
version: 1.7
date: 2025-04-24
---

# Client Adapter: Teams

## Overview

The Teams Client Adapter (`Nucleus.Adapters.Teams`) serves as the bridge between the core Nucleus OmniRAG platform and the Microsoft Teams environment, as introduced in the main [Client Architecture document](../05_ARCHITECTURE_CLIENTS.md). It enables users to interact with Nucleus Personas through familiar Teams interfaces like chat messages, channel conversations, and potentially Adaptive Cards. The adapter leverages the Microsoft Bot Framework SDK and Microsoft Graph API to handle communication, file access, and presentation within Teams.

As a pure client to the `Nucleus.Services.Api`, this adapter follows the interaction pattern described in [ARCHITECTURE_ADAPTER_INTERFACES.md](./ARCHITECTURE_ADAPTER_INTERFACES.md), translating between Teams/Bot Framework activities and API calls. It does not implement core logic itself.

## Auth

*   **Bot Authentication:** Relies on standard Bot Framework authentication mechanisms (App ID/Password or Managed Identity) configured during bot registration in Azure Bot Service / Azure AD.
*   **User Authentication:** User identity is typically derived from the Teams context provided in incoming activities.
*   **Graph API Access:** Requires separate Azure AD App Registration with appropriate Microsoft Graph API permissions (e.g., `Sites.Selected`, `Files.ReadWrite`, `User.Read`, `ChannelMessage.Send`) granted via admin consent. Authentication uses client credentials flow or Managed Identity.

## Interaction Handling & API Forwarding

As per the API-First principle ([Memory: 21ba96d2](cci:memory/21ba96d2-36ea-4a88-8b6b-ed0fb4d8dd07)), the Teams Adapter acts as a translator between the Bot Framework SDK and the central `Nucleus.Services.Api`.

1.  **Receive Activity:** The adapter's bot logic (e.g., implementing `IBot` or using `ActivityHandler`) receives an incoming `Activity` object from the Bot Framework for events like `message`, `messageReaction`, etc.
2.  **Extract Basic Context:** The adapter extracts standard information from the `Activity`:
    *   User ID, Name (`activity.From.Id`, `activity.From.Name`)
    *   Conversation ID, Type (`activity.Conversation.Id`, `activity.Conversation.ConversationType` - e.g., 'channel', 'personal')
    *   Tenant ID (`activity.Conversation.TenantId`)
    *   Message Content (`activity.Text`, `activity.Attachments`)
    *   Activity ID (`activity.Id`)
    *   Timestamp (`activity.Timestamp`)
3.  **Detect Reply Context:** To enable implicit activation for replies ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](./../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter specifically checks if the incoming message is part of a thread/reply chain:
    *   It inspects the `Activity.Conversation.Id`. For threaded replies in channels, this ID often follows the pattern `19:CHANNEL_ID@thread.tacv2;messageid=PARENT_ACTIVITY_ID` or similar variants.
    *   It parses this string to extract the `messageid` value, which corresponds to the `Activity.Id` of the message being replied to.
    *   While `Activity.ReplyToId` exists, relying on parsing the `Conversation.Id` for the parent `messageid` is often more reliable for capturing the *thread* context.
4.  **Construct API Request:** The adapter maps the extracted information to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.
    *   This includes mapping the user, channel/chat, message content, and attachment references (see [File Attachments](#file-attachments)).
    *   Crucially, if a parent `messageid` was extracted in the previous step, it is populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformMessageId`).
5.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the populated `InteractionRequest` object as the body.
6.  **Handle API Response:**
    *   **Synchronous (`HTTP 200 OK`):** The adapter receives the response DTO (e.g., `InteractionResponse`), which may contain text replies, structured data, or references to generated artifacts.
    *   **Asynchronous (`HTTP 202 Accepted`):** The adapter receives a `jobId` and potentially an initial acknowledgment message.
    *   **Errors (`HTTP 4xx/5xx`):** The adapter logs the error and sends an informative message back to the user.

## Generated Artifact Handling

Nucleus integrates with the **native storage mechanisms** of Microsoft Teams, primarily SharePoint Online for channel files and OneDrive for Business for chat files. Nucleus itself **does not persist raw user content** within its own managed storage (See Memory `08b60bec`). Instead, it interacts with the Team's designated storage location.

*   **Storing Outputs:** When the `Nucleus.Services.Api` generates output artifacts (e.g., summaries, reports, visualizations, logs) intended for persistent storage within the Team's context, the **`ApiService` itself writes these artifacts** to the appropriate location (typically the Team's SharePoint document library) using its own Graph permissions. The **API response then includes references** (e.g., SharePoint URLs, Graph Item IDs) to these already-stored artifacts. The adapter's role is simply to present these references to the user (e.g., as links in a message or an Adaptive Card), not to perform the write operation itself.

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

*   **Platform Representation:** Files uploaded to Teams chats/channels, stored in SharePoint or OneDrive.
*   **Handling:** When a message activity contains attachments, the adapter extracts references to these attachments (e.g., Graph Download URL, SharePoint Item ID/URL via `activity.Attachments[n].Content.downloadUrl` or by querying Graph using IDs).
*   **Adapter Action:** The adapter includes these **references** (URIs or structured identifiers) in the `SourceArtifactUris` field of the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.
*   **API Service Action:** The **`Nucleus.Services.Api`** receives these references and is responsible for using them (along with appropriate authentication) to fetch the actual file content directly from the source (e.g., SharePoint/OneDrive via Graph API) for ingestion and processing.

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