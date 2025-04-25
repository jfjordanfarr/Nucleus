---
title: Client Adapter - Email
description: Describes a client adapter which enables the interaction with Nucleus personas in Email
version: 1.3
date: 2025-04-24
---

# Client Adapter: Email


## Overview

Enables interaction with Nucleus Personas via email (e.g., reading monitored mailboxes, sending replies).

> **Warning:** This document is currently **underdeveloped** regarding the specific mechanisms for **monitoring incoming emails** (e.g., Microsoft Graph Change Notifications vs. polling) and the **activation strategies** used to determine which emails should trigger processing by the Nucleus API. These critical aspects require further definition.


## Auth

Depends on the chosen protocol:
*   **IMAP/SMTP:** Requires mailbox credentials (username/password or App Password).
*   **Microsoft Graph API:** Requires Azure AD App Registration with appropriate permissions (e.g., `Mail.ReadWrite`, `Mail.Send`) and authentication (client credentials or managed identity).


## Generated Artifact Handling

*   **Conversations:** Email messages are stored persistently on the email server (e.g., Exchange, Gmail, IMAP server).
*   **Generated Artifacts:** Personas can generate artifacts (files, reports). The Email Adapter **must** use the appropriate email protocol (e.g., MS Graph API, SMTP/IMAP) to:
    *   Option 1: Send a *new email* containing the generated artifact as an attachment (potentially replying to the triggering email if contextually appropriate).
    *   Option 2 (If using Graph API for M365): Potentially save the artifact to the user's OneDrive and include a link in a reply email.
*   Nucleus then creates `ArtifactMetadata` for the newly sent email or the uploaded file, using identifiers returned by the email/storage system.


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


## Attachments

Handles files embedded within messages.

*   **Platform Representation:** Files embedded within a message MIME structure.
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   Each distinct attachment gets its own `ArtifactMetadata` record.
    *   `sourceSystemType`: Set to `Email`.
    *   `sourceIdentifier`: e.g., `email://<MESSAGE_ID_HASH_OR_RAW>/attachment/CONTENT_ID_OR_INDEX`.
    *   `parentSourceIdentifier`: The `sourceIdentifier` of the containing email message.
    *   `displayName`: Attachment filename.


## Rich Presentations and Embedded Hypermedia

Email presentation capabilities vary by client.

*   **HTML:** Email bodies can contain rich HTML formatting.
*   **Interactivity:** Limited. Some clients support basic forms or actions (e.g., Outlook Actionable Messages), but general interactivity is minimal.
*   **Embedded Visualizations:** Not directly supported. Interactive data visualizations would need to be rendered as static images (PNG) embedded in the HTML body, or saved as files (HTML, SVG, PNG) and attached, with a link provided in the email body.

## References

*   For details on initial feasibility and design considerations across Slack, Email, and Discord adapters, see: [Slack, Email, and Discord Adapter Report](../../HelpfulMarkdownFiles/Slack-Email-Discord-Adapter-Report.md)
