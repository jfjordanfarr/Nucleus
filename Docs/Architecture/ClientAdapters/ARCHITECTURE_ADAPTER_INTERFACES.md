---
title: Architecture - Client Adapter Interaction Pattern
description: Defines the standard interaction pattern for Nucleus Client Adapters (e.g., Console, Teams) within the API-First architecture.
version: 2.1
date: 2025-04-27
parent: ../05_ARCHITECTURE_CLIENTS.md
---

# Architecture: Client Adapter Interaction Pattern

This document describes the standard interaction pattern expected for Nucleus Client Adapters (e.g., Console, Teams) within the strict [API-First Architecture](../00_ARCHITECTURE_OVERVIEW.md#1-core-principles) defined for the Nucleus platform. It replaces previous concepts of common adapter interfaces passed *into* a core system.

As outlined in the main [Client Architecture](../05_ARCHITECTURE_CLIENTS.md), adapters function purely as **translators** and **API clients**. Their primary responsibilities are:
1.  Receiving events/messages from their specific platform.
2.  Translating these platform-specific messages into standardized Data Transfer Objects (DTOs) defined by the `Nucleus.Services.Api`.
3.  Calling the appropriate endpoints on the `Nucleus.Services.Api`.
4.  Receiving response DTOs from the API service.
5.  Translating these response DTOs back into platform-specific replies or actions.
6.  Sending the replies/actions back to the originating platform.

All core logic, including authentication/authorization checks, accessing source artifacts, processing data, interacting with AI, and writing output artifacts, resides **exclusively within the `Nucleus.Services.Api`** and its underlying services.

Implementations detailing how specific adapters follow this pattern can be found in:
*   [Console Adapter Architecture](./Console/ARCHITECTURE_ADAPTERS_CONSOLE.md)
*   [Teams Adapter Architecture](./Teams/ARCHITECTURE_ADAPTERS_TEAMS.md)

## 1. `IPlatformMessage` (Conceptual Input)

Conceptually, adapters start by receiving a platform-specific message or event. While the exact structure varies (e.g., Teams `Activity`, console input string), we can represent the essential information abstractly as an `IPlatformMessage`.

**Purpose:** To identify the key pieces of information an adapter needs to extract from a platform message to construct an API request.

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

Adapters **do not implement** interfaces like `IPersonaInteractionContext`, `ISourceFileReader`, or `IOutputWriter` to be passed into a core system. Instead, they follow a standard pattern as API clients:

1.  **Receive Platform Message:** The adapter's listener receives a native message (e.g., HTTP request from Teams Bot Framework, console input read).
2.  **Extract Key Information:** The adapter parses the platform message to extract data conceptually similar to `IPlatformMessage` (MessageId, ConversationId, UserId, Content, SourceArtifactUris, Timestamp).
3.  **Translate to API Request DTO:** The adapter constructs a request DTO defined by the `Nucleus.Services.Api` (see [API Architecture](../10_ARCHITECTURE_API.md) and its sub-documents). This DTO encapsulates the necessary information for the API to process the request.
4.  **Call `Nucleus.Services.Api` Endpoint:** The adapter makes an authenticated HTTP call (e.g., POST `/api/interactions`) to the central API service, sending the request DTO.
5.  **Receive API Response DTO:** The adapter receives a response DTO from the API service, indicating success, failure, or asynchronous job submission.
6.  **Translate to Platform Reply:** The adapter translates the information in the response DTO into a format suitable for the client platform (e.g., constructing a Teams `Activity` reply, formatting console output).
7.  **Send Platform Reply:** The adapter uses platform-specific mechanisms (e.g., Bot Framework Connector Client, `Console.WriteLine`) to send the reply back to the user or channel.

This pattern ensures that adapters remain thin translators and all core logic is centralized and secured within the `Nucleus.Services.Api`, adhering to the API-First principle.

*(Previous interface definitions like `IPersonaInteractionContext`, `ISourceFileReader`, and `IOutputWriter` are deprecated as they represent an older architectural model where adapters held more core logic responsibility.)*

---
