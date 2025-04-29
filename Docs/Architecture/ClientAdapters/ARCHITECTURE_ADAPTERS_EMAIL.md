---
title: Client Adapter - Email
description: Describes a client adapter which enables the interaction with Nucleus personas via Email
version: 1.4
date: 2025-04-27
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Client Adapter: Email


## Overview

Enables interaction with Nucleus Personas via email (e.g., reading monitored mailboxes, sending replies).

> **Warning:** This document is currently **underdeveloped** regarding the specific mechanisms for **monitoring incoming emails** (e.g., Microsoft Graph Change Notifications vs. polling) and the **activation strategies** used to determine which emails should trigger processing by the Nucleus API. These critical aspects require further definition.


## Auth

Depends on the chosen protocol:
*   **IMAP/SMTP:** Requires mailbox credentials (username/password or App Password).
*   **Microsoft Graph API:** Requires Azure AD App Registration with appropriate permissions (e.g., `Mail.ReadWrite`, `Mail.Send`) and authentication (client credentials or managed identity).


## Interaction Handling & API Forwarding

**(Note: Specific mechanism for detecting incoming emails is TBD - e.g., MS Graph Change Notifications, periodic polling via Graph/IMAP)**

Following the API-First principle, the Email Adapter translates between email protocols/APIs and the central `Nucleus.Services.Api`.

1.  **Receive/Detect Email:** The adapter identifies a new relevant email in the monitored mailbox(es) using the chosen mechanism.
2.  **Extract Basic Context:** The adapter parses the email (e.g., using MIME libraries or Graph API data structures) to extract:
    *   Sender (`From`), Recipients (`To`, `Cc`)
    *   `Subject`
    *   Body (Text and/or HTML)
    *   Email Headers (specifically `Message-ID`, `In-Reply-To`, `References`)
    *   Attachment identifiers (e.g., filenames, Content-IDs, Graph Attachment IDs)
    *   Timestamp (`Date` header)
3.  **Detect Reply Context:** To enable implicit activation or context chaining ([ARCHITECTURE_ORCHESTRATION_ROUTING.md](./../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)), the adapter specifically checks email headers:
    *   It inspects the `In-Reply-To` and `References` headers.
    *   If present, these indicate the email is part of a thread.
    *   The adapter extracts the `Message-ID` from the `In-Reply-To` header (if available) as the direct parent.
4.  **Construct API Request:** The adapter maps the extracted information to the `InteractionRequest` DTO defined by the `Nucleus.Services.Api`.
    *   This includes mapping the sender as the user, subject/body as content, and attachment references (see [Attachments](#attachments-inputsingestion)).
    *   Crucially, if a parent `Message-ID` was extracted from `In-Reply-To`, it is populated into a dedicated field in the `InteractionRequest` (e.g., `RepliedToPlatformMessageId`). The full thread context might be passed separately if needed.
5.  **Forward to API:** The adapter makes an authenticated HTTP POST request to the central Nucleus API endpoint (e.g., `POST /api/v1/interactions`) with the populated `InteractionRequest` object as the body.
6.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result (HTTP 200 OK + body), the adapter translates this back into an email (e.g., a reply to the original sender) potentially including generated content/attachments, and sends it using the configured email protocol/API.
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter might send an initial acknowledgement email. It then needs a mechanism (TBD - polling `/interactions/{jobId}/status`, or webhooks) to receive the final result later and send it as a new email/reply.


## Generated Artifact Handling

*   **Conversations:** Email messages are stored persistently on the email server (e.g., Exchange, Gmail, IMAP server).
*   **Generated Artifacts (Outputs):** Personas can generate artifacts (files, reports). The flow for handling these is:
    1.  The `Nucleus.Services.Api` generates the artifact content (e.g., a text file, image, HTML visualization).
    2.  The API response (`InteractionResponse`) sent back to the Email Adapter includes this generated content (or a means to retrieve it).
    3.  The **Email Adapter** receives the response and uses the appropriate email protocol (e.g., MS Graph API, SMTP with MIME libraries) to compose and send a **new email** (or reply) containing the generated content, potentially as attachments.
    4.  Upon successful sending (or saving to OneDrive if using Option 2 below), the email system/Graph API provides details about the sent message (e.g., its new `Message-ID`) or uploaded file.
    5.  The adapter (potentially via a follow-up call or async job completion) informs the `Nucleus.Services.Api` of the successful delivery and provides the relevant identifiers (e.g., the new email's `Message-ID`).
    6.  The `Nucleus.Services.Api` then creates the canonical `ArtifactMetadata` record for the generated artifact (the sent email or the attached file), using the provided identifiers.
*   **Delivery Options:**
    *   Option 1: Send a *new email* (or reply) with the generated artifact as an attachment.
    *   Option 2 (If using Graph API for M365): The API Service could potentially save the artifact to the user's OneDrive (requiring Graph permissions) and the Adapter sends an email containing a link to the OneDrive file.


## Messaging

Handles individual email messages.

*   **Platform Representation:** The core unit is the Message, containing headers (From, To, Subject, Date, Message-ID, In-Reply-To, References), body (plain text and/or HTML), and attachments.
*   **Nucleus `ArtifactMetadata` Mapping (for Messages):**
    *   Each email message gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Email`.
    *   `sourceIdentifier`: Derived from the unique `Message-ID` header (e.g., `email://<MESSAGE_ID_HASH_OR_RAW>`).
    *   `sourceUri`: Not directly applicable for Nucleus access; requires API calls using message ID. Could store API-specific ID (e.g., Graph message ID).
    *   `displayName`: Email Subject line.
    *   `sourceCreatedAt`: From the `Date` header.
    *   `sourceLastModifiedAt`: Not typically applicable for emails once sent.
    *   `sourceCreatedByUserId`: From the `From` header (email address).
    *   `originatingContext`: Could store mailbox ID, folder name, etc.


## Conversations

Handles email threading.

*   **Platform Representation:** Threading is not an explicit object but is reconstructed using the `In-Reply-To` and `References` headers.
*   **Nucleus `ArtifactMetadata` Mapping (Relationships):**
    *   `replyToSourceIdentifier`: Derived from the `In-Reply-To` header, mapping the referenced Message-ID to its corresponding `sourceIdentifier` in Nucleus (requires lookup).
    *   `threadSourceIdentifier`: Can be derived by analyzing the `References` header chain to find the root message's `sourceIdentifier` (complex, requires lookup and potentially heuristics).


## Attachments (Inputs/Ingestion)

Handles files embedded within incoming email messages.

*   **Platform Representation:** Files embedded within a message MIME structure, accessible via email protocols or APIs (like MS Graph).
*   **Adapter Action:** When an incoming email contains attachments, the adapter extracts identifiers for each attachment.
    *   If using MS Graph, this would typically be the `Attachment Id` provided by the Graph API for the specific message.
    *   If using IMAP/SMTP, the adapter might need to assign a temporary identifier or use the `Content-ID` header if present.
*   **API Request:** The adapter includes these **identifiers** (e.g., MS Graph Attachment IDs) in the `SourceArtifactUris` field of the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.
*   **API Service Action:** The **`Nucleus.Services.Api`** receives these identifiers. If they are Graph IDs, the API service uses the Graph API (with appropriate permissions) to fetch the attachment content directly from the email message. For other protocols, the API might require the adapter to provide a temporary access mechanism (TBD - requires careful design).
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   Each distinct attachment processed can get its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Email`.
    *   `sourceIdentifier`: e.g., `email://<MESSAGE_ID_HASH_OR_RAW>/attachment/<GRAPH_ATTACHMENT_ID_OR_CONTENT_ID>`.
    *   `parentSourceIdentifier`: The `sourceIdentifier` of the containing email message.
    *   `displayName`: Attachment filename.


## Rich Presentations and Embedded Hypermedia

Email presentation capabilities vary by client.

*   **HTML:** Email bodies can contain rich HTML formatting.
*   **Interactivity:** Limited. Some clients support basic forms or actions (e.g., Outlook Actionable Messages), but general interactivity is minimal.
*   **Embedded Visualizations:** Not directly supported. Interactive data visualizations would need to be rendered as static images (PNG) embedded in the HTML body, or saved as files (HTML, SVG, PNG) and attached, with a link provided in the email body.


## References

*   For details on initial feasibility and design considerations across Slack, Email, and Discord adapters, see: [Slack, Email, and Discord Adapter Report](../../HelpfulMarkdownFiles/Slack-Email-Discord-Adapter-Report.md)
