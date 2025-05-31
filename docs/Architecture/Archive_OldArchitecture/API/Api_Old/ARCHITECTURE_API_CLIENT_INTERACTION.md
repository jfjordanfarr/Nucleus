---
title: "ARCHIVED - API Client Interaction Pattern (Superseded by M365 Agent & MCP Architecture)"
description: "ARCHIVED: This document described the original API client interaction patterns, now superseded by the Microsoft 365 Agents SDK and Model Context Protocol (MCP) architecture. It is retained for historical context."
version: 1.0
date: 2025-05-25
see_also:
    - "../05_ARCHITECTURE_CLIENTS.md"
    - "../10_ARCHITECTURE_API.md"
    - "../../Requirements/REQUIREMENTS_INTERACTION_MODEL.md"
    - "../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md" # Added link to new architecture
    - "../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md" # Added link to new architecture
---

> [!WARNING]
> **ARCHIVED DOCUMENT**
>
> This document describes a previous architectural approach that has been **superseded** by the adoption of the Microsoft 365 Agents SDK and the Model Context Protocol (MCP). The information below is for **historical reference only** and does not reflect the current architecture of Nucleus.
>
> Please refer to `../McpApplication/ARCHITECTURE_MCP_APPLICATIONS_OVERVIEW.md` and `../McpTools/ARCHITECTURE_MCP_TOOLS_OVERVIEW.md` for the current architectural direction.

# ARCHIVED - API Client Interaction Pattern (Historical Context)

**THIS DOCUMENT IS ARCHIVED AND SUPERSEDED BY THE MICROSOFT 365 AGENTS SDK AND MODEL CONTEXT PROTOCOL (MCP) ARCHITECTURE.**

The interaction patterns described herein were relevant for custom client adapters making direct calls to a monolithic `Nucleus.Services.Api`.

With the current architecture:
*   **Microsoft 365 Persona Agents** (built with the M365 Agents SDK) handle user interactions within platforms like Teams. They receive `Activity` objects from the M365 platform.
*   These M365 Persona Agents then act as **MCP clients**, making calls to backend **Nucleus MCP Tool/Server applications** for specific functionalities (e.g., knowledge retrieval, LLM interaction, file processing).

This document is retained for historical context and to understand the evolution of the Nucleus architecture.

--- 

## 1. Core Principles (Historical Context)

This document details the standard pattern Client Adapters (e.g., Teams, Local, Email) **must** follow when interacting with the central `Nucleus.Services.Api`. It acts as the bridge between platform-specific logic within an adapter and the core processing capabilities exposed by the API.

This pattern covers all interactions submitted via the primary `POST /api/v1/interactions` endpoint, utilizing the `AdapterRequest` DTO which includes `ArtifactReference` objects for context.

Adherence to this pattern ensures consistency and leverages the API-First architecture effectively.

## 2. Interaction Flow (Historical Context)

### 2.1. User Initiates Action in Client Application (Historical Context)

*   The user performs an action in the client application (e.g., sends a message, triggers a command).
*   The client application captures this event, gathering all necessary context (user, channel, message content).

### 2.2. Client Application Sends Request to Nucleus API (Historical Context)

*   **Purpose:** Translating a platform-specific event (message, command, file share) into the standardized `AdapterRequest` DTO.
*   **Key Fields to Populate (Refer to [`AdapterRequest.cs`](../../../src/Nucleus.Abstractions/Models/AdapterRequest.cs) for definitive structure):**
    *   `PlatformType` (e.g., "Teams", "Local", "Email").
    *   `UserId` (Platform-specific user identifier).
    *   `ConversationId` (Platform-specific channel/chat/session identifier).
    *   `MessageId` (Platform-specific message identifier, if applicable).
    *   `Prompt` (Primary textual content or command from the user).
    *   `Timestamp`.
*   **API Endpoint:** `POST /api/v1/interactions`
*   **Content Type:** `application/json`

### 2.3. Nucleus API Processes Request (Historical Context)

*   The Nucleus API receives the request and validates the `AdapterRequest` DTO.
*   If valid, the request is queued for processing (asynchronous), and a `jobId` is generated.
*   If invalid, an error response is returned (e.g., 400 Bad Request).

### 2.4. Nucleus API Sends Response to Client Application (Historical Context)

*   **HTTP 202 Accepted (Asynchronous Accepted):**
    *   The API successfully validated the request and accepted it for asynchronous processing by queueing the interaction task.
    *   The adapter **must** store this `jobId` (potentially associated with the original platform context) and initiate the polling process (see Section 4).
*   **HTTP 204 No Content (Activation Check Failed / No Action):**
    *   The API received the request, but the Activation Check determined no action was required.
    *   The adapter should typically do nothing further.
*   **HTTP 4xx/5xx (Errors):**
    *   Handle standard HTTP errors (e.g., 400 Bad Request for invalid input, 401 Unauthorized, 500 Internal Server Error).
    *   Log the error, potentially notify the user/admin on the platform.

### 2.5. Client Application Presents Information to User (Historical Context)

*   Once the status is `Completed` (after polling), the client application retrieves the result.
*   The response body contains the final output (e.g., `InteractionResponse` DTO, potentially other formats indicated by `Content-Type`).
*   The client application formats and presents this information to the user, handling any artifacts or generated files as necessary.

## 3. Key API Endpoints (Conceptual - Historical Context)

*   `POST /api/v1/interactions`: Submit a new interaction.
*   `GET /api/v1/interactions/{jobId}/status`: Check the status of a queued/processing interaction.
*   `GET /api/v1/interactions/{jobId}/result`: Retrieve the result of a completed interaction.

## 4. Authentication and Authorization (Historical Context)

*   All requests to the Nucleus API must be authenticated.
*   Typically, this involves including an `Authorization` header with a valid token.
*   The API will validate the token and determine the associated user/context.

## 5. Error Handling and Resilience (Historical Context)

*   Distinguish between API errors (e.g., API unavailable, invalid request) and platform errors (e.g., failed to send Teams message).
*   Implement appropriate logging and potentially user feedback mechanisms for different error types.
