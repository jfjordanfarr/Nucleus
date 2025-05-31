---
title: "Client Adapter: Email (via M365 Agents SDK & Graph API)"
description: "Describes how Nucleus M365 Persona Agents will interact with Email, primarily leveraging Microsoft 365 Agents SDK channel capabilities (e.g., Outlook channel) or direct Microsoft Graph API calls."
version: 2.0
date: 2025-05-25
see_also:
    - "../05_ARCHITECTURE_CLIENTS.md"
    - "../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md"
    - "../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md"
    - "../Security/ARCHITECTURE_SECURITY_M365_INTEGRATION.md" # Assuming a doc on M365 security
---

# Client Adapter: Email (via M365 Agents SDK & Graph API)

## 1. Overview & Strategic Shift

With the adoption of the Microsoft 365 Agents SDK, Nucleus's approach to email integration has fundamentally shifted. Instead of a custom IMAP/SMTP adapter, email interaction will primarily occur through **Nucleus M365 Persona Agents**. These agents will leverage:

1.  **M365 Agents SDK Channel Capabilities:** Utilizing built-in or Azure Bot Service channels (e.g., the Outlook channel) that the M365 Agents SDK can consume.
2.  **Direct Microsoft Graph API Calls:** Enabling M365 Agents to interact with user mailboxes (e.g., monitored mailboxes) using their Entra Agent ID for permissions, for more tailored scenarios.

This strategy deprecates the need for Nucleus to manage low-level email protocols and credentials directly for its primary interaction model, instead relying on the robust infrastructure and security model of the Microsoft 365 ecosystem.

## 2. Authentication

Authentication for email interactions will be centered around **Microsoft Graph API authentication mechanisms**:

*   **Managed Identities / Entra Agent ID:** Nucleus M365 Persona Agents, when running in Azure, will use their Managed Identity (which translates to an Entra Agent ID/Service Principal) to securely access Microsoft Graph APIs with appropriately scoped permissions (e.g., `Mail.Read`, `Mail.ReadWrite`, `Mail.Send`).
*   **User Delegated Permissions (Less Common for Agents):** While Graph supports delegated permissions, for autonomous agent scenarios, application permissions granted to the Agent's identity are more typical.

Direct IMAP/SMTP credential management by Nucleus is no longer the primary model for agent-based interactions.

## 3. Interaction Handling & Flow

The flow of email interaction involves the M365 Agent acting as the intermediary between the user's mailbox and Nucleus backend MCP Tools.

### 3.1. Receiving Emails

An M365 Agent can become aware of new emails through several mechanisms:

*   **M365 SDK Channel (e.g., Outlook Channel):** If the agent is connected to an Azure Bot Service channel for Outlook, incoming emails can be delivered to the agent as an `Activity` object.
*   **Graph API Notifications (Webhooks):** The agent (or a supporting Nucleus service) can subscribe to Microsoft Graph API change notifications for specific mailboxes or folders. When a new email arrives, a notification is sent to a webhook endpoint, triggering the agent.
*   **Graph API Polling (Less Ideal):** The agent could periodically poll mailboxes via the Graph API, though this is less efficient than notifications.

### 3.2. Processing Received Emails

Once an M365 Agent receives an email (as an `Activity` or a Graph API `Message` object):

1.  **Parsing:** The agent parses the email to extract key information:
    *   Sender (e.g., `message.from`, `message.sender`)
    *   Recipients (e.g., `message.toRecipients`)
    *   Subject (e.g., `message.subject`)
    *   Body (e.g., `message.body.content`, `message.uniqueBody.content`)
    *   Tenant ID, User ID (from agent context or Graph response)
    *   Attachments (e.g., `message.attachments`, `message.hasAttachments`)
2.  **Contextualization:** The agent gathers relevant context, such as conversation history (if part of a thread, using `message.conversationId`) and user information.
3.  **Artifact Reference Creation (for Attachments):** For each attachment, the agent (or a designated MCP Tool like `Nucleus_FileAccess_McpServer`) creates an `ArtifactReference` using Microsoft Graph item IDs (e.g., `attachment.id`, and potentially drive item IDs if attachments are saved to OneDrive first).
4.  **MCP Call to Nucleus Tools:** The M365 Agent makes one or more calls to relevant backend Nucleus MCP Tools (e.g., `Nucleus_ContentAnalysis_McpTool`, `Nucleus_KnowledgeQuery_McpTool`) via the Model Context Protocol. The request would include the extracted email content, `ArtifactReference`s for attachments, and other necessary context.

### 3.3. Sending Replies/New Emails

After processing and potentially interacting with backend MCP Tools, the M365 Agent can send emails using:

*   **M365 Agents SDK:** If the originating interaction came through an SDK channel, the agent can use the SDK's reply mechanisms (e.g., creating a reply `Activity`).
*   **Microsoft Graph API `SendMail` Action:** The agent can construct and send new emails or replies directly using the `/sendMail` Graph API endpoint, leveraging its Entra Agent ID for authorization.

## 4. Input/Ingestion: Email Content & Attachments

Email content and attachments are primarily handled as Microsoft Graph API objects:

*   **`Message` Object:** Represents the email itself, providing access to headers, body, recipients, and attachment collections.
*   **`Attachment` Object:** Represents an email attachment. Can be a `fileAttachment`, `itemAttachment`, or `referenceAttachment`.
    *   The M365 Agent (or the `Nucleus_FileAccess_McpServer` acting on its behalf using the agent's context/permissions) would use Graph API calls to fetch attachment content if needed for analysis by an MCP Tool.

**`ArtifactMetadata` Mapping:**
Conceptual mapping of email properties to `ArtifactMetadata` remains similar to previous designs, but identifiers will now be based on Microsoft Graph IDs:

*   `SourceSystem`: "MicrosoftGraphEmail"
*   `SourceItemId`: Graph `Message` ID (e.g., `message.id` or `message.internetMessageId`)
*   `ParentSourceItemId`: Graph `Message` ID of a parent email in a thread (if applicable, using `message.conversationId` to link).
*   `Name`: Email Subject.
*   `MimeType`: From `message.body.contentType` (for body) or `attachment.contentType`.
*   `Author`: From `message.sender` or `message.from`.
*   `Timestamp`: From `message.receivedDateTime` or `message.sentDateTime`.

For attachments, separate `ArtifactMetadata` entries would be created, linking back to the parent email's `SourceItemId`.

## 5. Generated Artifact Handling

When Nucleus Personas (via M365 Agents and MCP Tools) generate artifacts (e.g., reports, summaries):

1.  **Storage:** The M365 Agent, using Graph API, can save these generated files to a user's OneDrive or a designated SharePoint location.
2.  **Notification/Delivery:**
    *   Send an email (via Graph API `SendMail`) with a link to the generated file stored in OneDrive/SharePoint.
    *   Attach the file directly to an email (if size permits and appropriate) using Graph API.

## 6. Rich Presentations & Actionable Messages

*   **HTML Emails:** M365 Agents can send richly formatted HTML emails via Graph API, enhancing the presentation of information.
*   **Actionable Messages:** For more interactive scenarios, M365 Agents can send Outlook Actionable Messages. This allows embedding actions (e.g., buttons that trigger further Graph API calls or MCP Tool invocations) directly within the email, creating a more dynamic experience.

## 7. Security Considerations

*   **Permissions:** Adherence to the principle of least privilege is critical. The M365 Agent's Entra ID should only be granted the necessary Graph API permissions (e.g., `Mail.Read`, `Mail.Send` for specific mailboxes if possible, rather than tenant-wide).
*   **Data Handling:** Sensitive information extracted from emails must be handled according to Nucleus's overall security and data governance policies, even during ephemeral processing by MCP Tools.
*   Refer to `../Security/ARCHITECTURE_SECURITY_M365_INTEGRATION.md` for broader M365 security integration details.

This approach aligns email integration with the broader M365 strategy, leveraging Microsoft's infrastructure for communication, security, and identity, while allowing Nucleus to focus on the intelligence provided by its Personas and MCP Tools.
