---
title: "ARCHIVED - External Client Adapter API Interaction (Superseded by M365 Agent & MCP Architecture)"
description: "ARCHIVED: This document outlined the interaction patterns for external client adapters with the Nucleus API, now superseded by the Microsoft 365 Agents SDK and Model Context Protocol (MCP) architecture. It is retained for historical context."
version: 1.0
date: 2025-05-25
see_also:
    - "../05_ARCHITECTURE_CLIENTS.md"
    - "../Api/10_ARCHITECTURE_API.md"
    - "../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md" # Added link to new architecture
    - "../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md" # Added link to new architecture
---

> [!WARNING]
> **ARCHIVED DOCUMENT**
>
> This document describes a previous architectural approach that has been **superseded** by the adoption of the Microsoft 365 Agents SDK and the Model Context Protocol (MCP). The information below is for **historical reference only** and does not reflect the current architecture of Nucleus.
>
> Please refer to `../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md` and `../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md` for the current architectural direction.

# ARCHIVED - External Client Adapter API Interaction (Historical Context)

**THIS DOCUMENT IS ARCHIVED AND SUPERSEDED BY THE MICROSOFT 365 AGENTS SDK AND MODEL CONTEXT PROTOCOL (MCP) ARCHITECTURE.**

The interaction patterns and conceptual interfaces described herein were relevant for custom client adapters making direct calls to a monolithic `Nucleus.Services.Api`.

With the current architecture:
*   **Microsoft 365 Persona Agents** (built with the M365 Agents SDK) handle user interactions within platforms like Teams. They receive `Activity` objects from the M365 platform, which serve a more comprehensive and standardized role than the conceptual `IPlatformMessage` described below.
*   These M365 Persona Agents then act as **MCP clients**, making calls to backend **Nucleus MCP Tool/Server applications** for specific functionalities.
*   Notification and response delivery are handled by the M365 Agents SDK's built-in capabilities (e.g., `turnContext.SendActivityAsync`, `ChannelAdapter.ContinueConversationAsync`), replacing concepts like `IPlatformNotifier`.

This document is retained for historical context and to understand the evolution of the Nucleus architecture.

---

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
*   [Teams Adapter Architecture](./ARCHITECTURE_ADAPTERS_TEAMS.md) (Now also archived)

## 1. Core Concepts (Historical Context)

This section provides an overview of the key concepts and components involved in the interaction between external client adapters and the Nucleus API, as understood in the previous architectural context.

### 1.1. `IPlatformMessage` (Conceptual - Historical Context)

Conceptually, external adapters start by receiving a platform-specific message or event. While the exact structure varies (e.g., Teams `Activity`, Slack event payload), this section provides an abstract representation of the essential information an adapter needs to extract.

**Purpose:** To serve as a **guide** for identifying the key pieces of information an adapter needs to extract from a platform message to construct a well-formed HTTP request to the `Nucleus.Services.Api` (typically resulting in an `AdapterRequest` DTO, as detailed in [API Client Interaction](../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)). It is **not** a C# interface that external adapters are expected to literally implement and pass within the Nucleus system.

**Conceptual Definition (Historical):**

```csharp
/// <summary>
/// Represents a single message or event received from the client platform.
/// </summary>
public interface IPlatformMessage
{
    // ... (Content of historical IPlatformMessage, now superseded by M365 Activity object)
    // string MessageId { get; }
    // string ConversationId { get; }
    // string UserId { get; }
    // string UserName { get; }
    // string TenantId { get; } // Optional, but important for multi-tenant systems
    // string Content { get; }
    // IReadOnlyList<string> SourceArtifactUris { get; }
    // DateTimeOffset Timestamp { get; }
    // string RepliedToPlatformMessageId { get; } // For threaded replies
    // T GetMetadata<T>(string key);
}
```

## 2. Interaction Flow (Historical Context)

This section outlines the typical flow of interaction between an external client adapter and the Nucleus API, from the receipt of an event in the external platform to the delivery of a response.

### 2.1. Event in External Platform (Historical Context)

An event or message is generated in the external platform (e.g., a new message in Slack, an email received).

### 2.2. Adapter Receives Event (Historical Context)

The external client adapter receives the event through its platform-specific listener (e.g., an HTTP endpoint for webhooks, an email inbox).

### 2.3. Adapter Normalizes to `IPlatformMessage` (Historical Context)

The adapter extracts relevant information from the raw event data, mapping it to the conceptual `IPlatformMessage` structure. This step is crucial for translating platform-specific details into a standardized format understood by the Nucleus API.

### 2.4. Adapter Sends to Nucleus API (Historical Context)

The adapter constructs an HTTP request to the `Nucleus.Services.Api`, typically a `POST` request to the `/api/v1/interactions` endpoint, including the normalized data in the request body as an `AdapterRequest` DTO.

### 2.5. Nucleus Processes and Responds (Historical Context)

The Nucleus API processes the request, performing actions such as data retrieval, AI interaction, and artifact management. It then sends an HTTP response back to the adapter, containing an `AdapterResponse` DTO.

### 2.6. Adapter Delivers Response to Platform (Historical Context)

Upon receiving the response from the Nucleus API, the adapter translates the `AdapterResponse` DTO back into a format suitable for the external platform. It then sends this response using the platform's native mechanisms (e.g., Slack SDK, SMTP client).

## 3. Authentication and Security (Historical Context)

Details the authentication and security measures relevant to the interaction between external client adapters and the Nucleus API, including token-based authentication, encryption, and data privacy considerations.

## 4. Error Handling (Historical Context)

Describes the error handling mechanisms that should be implemented within external client adapters to manage and respond to errors effectively, ensuring robustness and reliability in API interactions.
