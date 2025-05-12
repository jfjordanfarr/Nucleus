---
title: Architecture - External Client Adapter API Interaction
description: Defines the standard API interaction pattern for new, external Nucleus Client Adapters (e.g., Slack, Discord) interfacing with Nucleus.Services.Api.
version: 2.2
date: 2025-05-08
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Architecture: External Client Adapter - API Interaction Pattern

This document describes the standard interaction pattern expected for **new, external Nucleus Client Adapters** (e.g., for platforms like Slack, Discord, Email) when interfacing with the Nucleus platform. Its primary purpose is to guide developers of such adapters on how to:

1.  Structure HTTP requests to the `Nucleus.Services.Api`.
2.  Interpret the DTOs and responses from the `Nucleus.Services.Api`.

It adheres to the strict [API-First Architecture](../00_ARCHITECTURE_OVERVIEW.md#1-core-principles) defined for Nucleus. **Crucially, this document does *not* define shared C# interfaces that external adapters would implement for direct, in-process interaction with the core Nucleus system.** That architectural model has been superseded by the API-first approach. For internal, in-process client needs, the `Nucleus.Infrastructure.Adapters.Local` library serves this purpose, and its interaction patterns are considered internal to the Nucleus ecosystem.

As outlined in the main [Client Architecture](../05_ARCHITECTURE_CLIENTS.md), external adapters function purely as **translators** and **API clients**. Their primary responsibilities are:
1.  Receiving events/messages from their specific platform.
2.  Translating these platform-specific messages into standardized Data Transfer Objects (DTOs) required by the `Nucleus.Services.Api` (as defined in [API Architecture](../10_ARCHITECTURE_API.md)).
3.  Calling the appropriate HTTP endpoints on the `Nucleus.Services.Api`.
4.  Receiving response DTOs from the API service via HTTP.
5.  Translating these response DTOs back into platform-specific replies or actions.
6.  Sending the replies/actions back to the originating platform using platform-native SDKs or APIs.

All core logic, including authentication/authorization checks, accessing source artifacts, processing data, interacting with AI, and writing output artifacts, resides **exclusively within the `Nucleus.Services.Api`** and its underlying services.

Implementations detailing how specific adapters follow this pattern can be found in:
*   [Teams Adapter Architecture](./Teams/ARCHITECTURE_ADAPTERS_TEAMS.md)

## 1. `IPlatformMessage` (Conceptual Input for API Request Construction)

Conceptually, external adapters start by receiving a platform-specific message or event. While the exact structure varies (e.g., Teams `Activity`, Slack event payload), this section provides an abstract representation of the essential information an adapter needs to extract.

**Purpose:** To serve as a **guide** for identifying the key pieces of information an adapter needs to extract from a platform message to construct a well-formed HTTP request to the `Nucleus.Services.Api` (typically resulting in an `AdapterRequest` DTO, as detailed in [API Client Interaction](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)). It is **not** a C# interface that external adapters are expected to literally implement and pass within the Nucleus system.

**Conceptual Definition:**

```csharp
/// <summary>
/// Represents a single message or event received from the client platform.
/// </summary>
public interface IPlatformMessage
{
    /// <summary>
    /// Unique identifier for the message within the platform (e.g., Teams message ID, a generated UUID for console input).
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// Identifier for the conversation or thread the message belongs to (e.g., Teams channel ID, chat ID, "console_session").
    /// </summary>
    string ConversationId { get; }

    /// <summary>
    /// Identifier for the user who sent the message (e.g., AAD Object ID, local username).
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// The primary textual content of the message (e.g., text body, command line arguments).
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Timestamp when the message was created or received on the platform.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// URIs or identifiers pointing to any attached files or resources relevant to this message
    /// (e.g., `file://` URIs for console, SharePoint/OneDrive URIs/IDs for Teams).
    /// These are pointers to the *source* artifacts managed by the platform.
    /// </summary>
    IEnumerable<string> SourceArtifactUris { get; }

    // Potentially other platform-specific metadata accessible via methods or properties.
    // T GetMetadata<T>(string key);
}
```

## 2. Adapter API Interaction Pattern

External Client Adapters **do not implement** C# interfaces like `IPersonaInteractionContext`, `ISourceFileReader`, or `IOutputWriter` to be passed into the core Nucleus system. Instead, they follow a standard pattern as remote API clients:

1.  **Receive Platform Message:** The adapter's listener receives a native message (e.g., HTTP webhook from Slack, email via an email server).
2.  **Extract Key Information:** The adapter parses the platform message to extract data conceptually similar to the elements described in the `IPlatformMessage` section (MessageId, ConversationId, UserId, Content, SourceArtifactUris, Timestamp, etc.).
3.  **Translate to API Request DTO:** The adapter constructs an API request DTO (e.g., `AdapterRequest`) as defined by the `Nucleus.Services.Api` (see [API Architecture](../10_ARCHITECTURE_API.md) and its sub-documents, particularly [API Client Interaction](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)). This DTO encapsulates the necessary information for the API to process the request.
4.  **Call `Nucleus.Services.Api` Endpoint:** The adapter makes an authenticated HTTP call (e.g., `POST /api/v1/interactions`) to the central API service, sending the request DTO in the request body.
5.  **Receive API Response DTO:** The adapter receives an HTTP response containing a DTO from the API service, indicating success, failure, or asynchronous job submission status (e.g., `AdapterResponse`).
6.  **Translate to Platform Reply:** The adapter translates the information in the response DTO into a format suitable for the client platform (e.g., constructing a Slack message payload, formatting an email reply).
7.  **Send Platform Reply:** The adapter uses platform-specific mechanisms (e.g., Slack SDK, SMTP client) to send the reply back to the user or channel.

This pattern ensures that external adapters remain thin translators, and all core logic is centralized and secured within the `Nucleus.Services.Api`, adhering to the API-First principle.

*(Previous C# interface definitions like `IPersonaInteractionContext`, `ISourceFileReader`, and `IOutputWriter` are deprecated as they represent an older architectural model where adapters held more core logic responsibility and interacted more directly with the system's internals. This model is no longer applicable for external client adapters.)*

### 2. `IPlatformNotifier`

*   **Purpose:** Defines an interface for services that can send notifications (messages, acknowledgements) back to a specific platform (e.g., Teams, Slack, Console).
*   **Key Responsibilities:**
    *   Sending textual messages to a specified conversation/channel/user on the platform.
    *   Optionally sending messages as replies to existing messages.
    *   Sending acknowledgement signals (e.g., "typing" indicators, read receipts if supported by the platform and desired).
*   **Source Code:** [`IPlatformNotifier`](../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs)
*   **Implementations:** Each client adapter that supports receiving asynchronous responses from Nucleus (e.g., after a long-running background task) *may* provide an implementation of this interface. For example, `TeamsNotifier`, `ConsoleNotifier`.
    *   For platforms where notifications are not applicable or handled differently (e.g., a direct API call that returns an immediate HTTP response), a `NullPlatformNotifier` can be used.

---
