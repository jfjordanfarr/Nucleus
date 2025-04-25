---
title: Architecture - API Ingestion Endpoints
description: Defines the API endpoints and contracts for submitting content to Nucleus for ingestion, separate from standard message-based interactions.
version: 0.1
date: 2025-04-24
parent: ../10_ARCHITECTURE_API.md
---

# API Ingestion Endpoints

## 1. Introduction

While the primary `/api/v1/interactions` endpoint handles message-based interactions (including attachments referenced by URI), Nucleus also provides dedicated endpoints for initiating the ingestion of content directly, such as files specified by path.

These endpoints allow clients like the Console Adapter or administrative tools to submit specific content for processing by the Nucleus ingestion pipeline (detailed in [ARCHITECTURE_PROCESSING_INGESTION.md](../Processing/ARCHITECTURE_PROCESSING_INGESTION.md)).

## 2. Endpoint: Path-Based Ingestion

*   **Endpoint:** `POST /api/v1/ingest/path`
*   **Method:** `POST`
*   **Purpose:** Allows a client (e.g., Console Adapter via `nucleus ingest --path ...`) to request the ingestion of a single file specified by a local or network path that is **accessible to the `Nucleus.Services.Api` service process**. The API service reads the file directly.
*   **Content Type:** `application/json`

### Request Body (`IngestionRequestPath` DTO - Conceptual)

```json
{
  "FilePath": "/path/to/accessible/file.txt", // Required: Absolute path accessible by the API service
  "PlatformType": "Console", // Required: Identifier for the client type
  "UserId": "local_user_sid_or_name", // Required: Identifier for the submitting user context
  "SessionId": "optional-console-session-guid", // Optional: Identifier for the client session
  "TargetPersonaId": "optional-persona-id", // Optional: Hint for specific persona processing
  "Metadata": { // Optional: Key-value pairs for extra context
    "SourceCommand": "nucleus ingest --path /path/to/file.txt --persona Coder",
    "ClientVersion": "1.0.0"
  }
}
```

### API Service Responsibilities

1.  **Authentication/Authorization:** Verify the client is permitted to request ingestion.
2.  **Validation:**
    *   Ensure `FilePath`, `PlatformType`, and `UserId` are present.
    *   Validate the `FilePath` format (though accessibility is checked next).
3.  **Path Resolution & Access Check:** Attempt to access the file specified by `FilePath` using the service's permissions. Fail if the path is invalid, the file doesn't exist, or the service lacks read permissions.
4.  **Initiate Processing:** If the file is accessible, read its content (or stream it) and initiate the appropriate internal ingestion pipeline (e.g., [Plain Text Ingestion](../Processing/Ingestion/ARCHITECTURE_INGESTION_PLAINTEXT.md), [PDF Ingestion](../Processing/Ingestion/ARCHITECTURE_INGESTION_PDF.md), etc.) based on file type or other factors.
5.  **Response:** Because file ingestion can be time-consuming, this endpoint **always** follows the asynchronous processing pattern.

### Response Handling

*   **HTTP 202 Accepted:**
    *   Indicates the request was validated, the file path appears accessible (initial check), and the ingestion job has been successfully queued.
    *   Response Body: `{ "jobId": "<unique-job-guid>" }`
    *   The client uses this `jobId` to poll the status endpoint.
*   **HTTP 400 Bad Request:**
    *   Missing required fields (`FilePath`, `PlatformType`, `UserId`).
    *   Invalid `FilePath` format.
    *   File specified by `FilePath` does not exist or is inaccessible by the API service.
    *   Response body should contain error details.
*   **HTTP 401 Unauthorized / HTTP 403 Forbidden:**
    *   Authentication failed or the client is not authorized for ingestion.
*   **HTTP 500 Internal Server Error:**
    *   Unexpected error during validation, queuing, or initial file access.

### Status & Result Retrieval

Clients use the standard asynchronous pattern endpoints with the returned `jobId`:

*   **Status:** `GET /api/v1/interactions/{jobId}/status`
*   **Result:** `GET /api/v1/interactions/{jobId}/result`
    *   The result payload for a successful ingestion might contain metadata about the created Nucleus artifact(s) or simply confirm completion.

*Note: Consideration could be given to using dedicated `/ingestion/{jobId}/status` and `/ingestion/{jobId}/result` endpoints if the processing pipeline or result structure significantly differs from standard interactions, but using the common `/interactions/...` endpoints promotes consistency.* 

## 3. Endpoint: URI-Based Ingestion (Future)

*   **Endpoint:** `POST /api/v1/ingest/uri` (Placeholder)
*   **Purpose:** To support ingestion from generic URIs (e.g., `http://`, `https://`, cloud storage signed URLs). The API service would be responsible for fetching content from the URI.
*   **Details:** TBD.

## 4. Related Documents

*   [API Architecture Overview](../10_ARCHITECTURE_API.md)
*   [API Client Interaction Pattern](./ARCHITECTURE_API_CLIENT_INTERACTION.md)
*   [Processing - Ingestion Overview](../Processing/ARCHITECTURE_PROCESSING_INGESTION.md)
