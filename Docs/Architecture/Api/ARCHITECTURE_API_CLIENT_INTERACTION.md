---
title: Architecture - API Client Interaction Pattern
description: Defines how Nucleus Client Adapters should interact with the Nucleus API Service, including request construction, response handling (sync/async), and polling.
version: 0.2
date: 2025-04-25
parent: ../10_ARCHITECTURE_API.md
---

# API Client Interaction Pattern

## 1. Introduction

This document details the standard pattern Client Adapters (e.g., Teams, Console, Email) **must** follow when interacting with the central `Nucleus.Services.Api`. It acts as the bridge between platform-specific logic within an adapter and the core processing capabilities exposed by the API.

This pattern specifically covers message-based interactions submitted via the primary `/interactions` endpoint. Dedicated ingestion endpoints (e.g., for path-based file ingestion) may have slightly different request structures, detailed in [ARCHITECTURE_API_INGESTION.md](./ARCHITECTURE_API_INGESTION.md).

Adherence to this pattern ensures consistency and leverages the API-First architecture effectively.

## 2. Constructing the `InteractionRequest`

*   **Purpose:** Translating a platform-specific event (message, command) into the standardized `InteractionRequest` DTO.
*   **Key Fields to Populate:**
    *   `PlatformType` (e.g., "Teams", "Console", "Email").
    *   `UserId` (Platform-specific user identifier).
    *   `ConversationId` (Platform-specific channel/chat/session identifier).
    *   `MessageId` (Platform-specific message identifier).
    *   `Content` (Primary textual content).
    *   `Timestamp`.
    *   `SourceArtifactUris` (List of platform URIs for attachments/references).
    *   **`PlatformContext` (CRITICAL):** Dictionary or structured object containing platform-specific metadata crucial for Activation Checks, especially reply context:
        *   Teams: `ReplyToActivityId` (from `Conversation.Id` or `ReplyToId`).
        *   Discord: `ReferenceMessageId`.
        *   Slack: `ThreadTs`.
        *   Email: `InReplyToMessageId`, `ReferencesHeader`.
*   **API Endpoint:** `POST /api/v1/interactions`
*   **Content Type:** `application/json`

## 3. Handling API Responses

Adapters must handle different HTTP status codes returned by `POST /interactions`:

*   **HTTP 200 OK (Synchronous Completion):**
    *   The API processed the request synchronously and successfully.
    *   The response body contains the result (e.g., `InteractionResponse` DTO).
    *   Adapter translates this result back to the platform format (e.g., send reply message).
*   **HTTP 202 Accepted (Asynchronous Accepted):**
    *   The API accepted the request for asynchronous processing.
    *   The response body contains the `jobId`.
    *   The adapter **must** store this `jobId` (potentially associated with the original platform context) and initiate the polling process (see Section 4).
*   **HTTP 204 No Content (Activation Check Failed / No Action):**
    *   The API received the request, but the Activation Check determined no action was required.
    *   The adapter should typically do nothing further.
*   **HTTP 4xx/5xx (Errors):**
    *   Handle standard HTTP errors (e.g., 400 Bad Request, 401 Unauthorized, 500 Internal Server Error).
    *   Log the error, potentially notify the user/admin on the platform.

## 4. Polling for Asynchronous Job Status

If the initial response was HTTP 202:

*   **API Endpoint:** `GET /api/v1/interactions/{jobId}/status`
*   **Frequency:** Use a reasonable polling interval with backoff (e.g., start at 2s, increase to 5s, 10s, max out at 30s?). Exact strategy TBD.
*   **Handling Status Responses (`AsyncJobStatus` DTO):**
    *   `Queued` / `Processing`: Continue polling.
    *   `Completed`: Stop polling and proceed to retrieve the result (see Section 5). The response may contain a `resultUrl`.
    *   `Failed`: Stop polling. Log the error details provided in the response. Notify the user on the platform that the request failed.

## 5. Retrieving Asynchronous Job Results

Once the status is `Completed`:

*   **API Endpoint:** `GET /api/v1/interactions/{jobId}/result`
*   **Handling Result Response:**
    *   The response body contains the final output (e.g., `InteractionResponse` DTO, potentially other formats indicated by `Content-Type`).
    *   Adapter translates this result back to the platform format (e.g., send reply message, upload generated file).

## 6. Error Handling Considerations

*   Distinguish between API errors (e.g., API unavailable, invalid request) and platform errors (e.g., failed to send Teams message).
*   Implement appropriate logging and potentially user feedback mechanisms for different error types.

## 7. Translating Results Back to Platform

*   This is the reverse of Step 2.
*   Take the `InteractionResponse` (or other result format) from the API (received synchronously or via async result retrieval).
*   Format it appropriately for the target platform (e.g., create a Teams Activity, format console output, compose an email).
*   Handle potential artifact links or generated files as required by the platform.

## 8. Related Documents

*   [API Architecture Overview](../10_ARCHITECTURE_API.md)
*   [API Asynchronous Processing Pattern](../10_ARCHITECTURE_API.md#6-asynchronous-processing-pattern)
*   [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)
*   *(Link to API DTO Definitions document)*
