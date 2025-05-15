---
title: Architecture - Nucleus API Service
description: Overview of the API-First design principles, core responsibilities, endpoint structure, security model, and reference-based interaction patterns for the Nucleus API service.
version: 0.7
date: 2025-05-15
parent: ./00_ARCHITECTURE_OVERVIEW.md
---

# Nucleus API Service Architecture

**Version:** 0.7
**Date:** 2025-05-15

## 1. Introduction & Principles

*   Purpose of the API Service (Central interaction point for all backend functionality)
*   API-First Principle recap ([Adapters](./05_ARCHITECTURE_CLIENTS.md) translate platform events/commands into standardized API calls; core logic is centralized)
*   **Reference-Based Interaction:** The API exclusively uses `ArtifactReference` objects to interact with user content stored in external systems. **Direct file uploads are not supported.** (See [Storage Architecture](./03_ARCHITECTURE_STORAGE.md))
*   Design Goals (Statelessness, RESTful patterns, Security, Scalability, Observability)
*   Key Consumers ([Client Adapters](./05_ARCHITECTURE_CLIENTS.md), Internal Services, Testing Harnesses)

## 2. Core Responsibilities

*   Authentication & Authorization
*   Interaction Ingestion & Initial Validation
*   Activation Check (Link to [API Activation & Routing](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md))
*   Queuing Activated Interactions for Asynchronous Processing
*   Providing Status/Results for Asynchronous Jobs (via polling endpoints)
*   Serving Core Data/Metadata (e.g., Persona listing - if applicable via API)
*   Enforcing Security Boundaries

## 3. Endpoint Design & Structure

*   Base URL Path (e.g., `/api/v1/`)
*   Resource Naming Conventions (e.g., `/interactions`, `/personas`)
*   Common HTTP Verbs (POST for interactions, GET for status/data)
*   Example Endpoint Definitions (High-level):
    *   `POST /interactions` (Primary endpoint for all interaction processing requests)
        *   **Implementation:** [`InteractionController.cs`](../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs)
    *   `GET /interactions/{jobId}/status` (For async task status)
    *   `GET /interactions/{jobId}/result` (For async task results)
    *   `GET /personas` (Example metadata/configuration endpoint)
*   Versioning Strategy (e.g., `/v1/` in path)

## 4. Data Formats & Contracts

*   Standard Request/Response format: JSON
*   Key Data Transfer Objects (DTOs) - Precise definitions reside in `Nucleus.Abstractions`. High-level descriptions:
    *   **`AdapterRequest`:** The primary input DTO for the `POST /interactions` endpoint. Contains all necessary context from the client adapter, including user information, the prompt/command, and a list of `ArtifactReference` objects pointing to relevant external content. See:
        *   [API Client Interaction Pattern](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
        *   [`AdapterRequest`](../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterRequest.cs) (Source Code)
    *   **`ArtifactReference`:** Contained within `AdapterRequest`. Provides details needed by the API service's `IArtifactProvider` to locate and ephemerally fetch content from the user's source system. See:
        *   [`ArtifactReference`](../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) (Source Code)
    *   **`InteractionResponse` (typically `AdapterResponse`):** The DTO containing the final results delivered once an asynchronous interaction job completes successfully. Contains processing output, status, and potentially references to generated artifacts. See:
        *   [`AdapterResponse`](../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterResponse.cs) (Source Code)
    *   **`AsyncJobStatus` (Conceptual DTO Structure):** Response from the status polling endpoint (`GET /interactions/{jobId}/status`), indicating the job's current state (`Queued`, `Processing`, `Completed`, `Failed`). Its structure is detailed in Section 6.
*   Error Handling (Standard HTTP status codes, structured JSON error response body)

## 5. Authentication & Authorization

*   Authentication Mechanism (e.g., API Keys, OAuth, Managed Identity)
*   Authorization Strategy (How access is controlled - role-based, scope-based?)
*   Security Considerations (Input validation, rate limiting, etc.)

## 6. Asynchronous Processing Pattern

When an API request (e.g., `POST /interactions`) results in an activated interaction requiring processing, the API adopts an asynchronous pattern.

*   **Job Submission Response:**
    *   The API immediately responds with `HTTP 202 Accepted` after successfully validating the request and queueing the interaction task.
    *   The response body **must** include a unique `jobId` (e.g., a GUID string) that the client will use to track the job's progress. This is often returned as a `JobIdResponse`. See:
        *   [`JobIdResponse`](../../src/Nucleus.Abstractions/Models/ApiContracts/JobIdResponse.cs) (Source Code)
    *   Optionally, the response **may** include a `Location` header pointing to the status endpoint (e.g., `Location: /api/v1/interactions/{jobId}/status`).
    ```json
    // Example HTTP 202 Response Body
    {
      "jobId": "b7e1f4a0-8c1d-4a7f-9d8b-3e2f1a0c9b8d"
    }
    ```

*   **Status Polling Endpoint:**
    *   Clients periodically poll `GET /interactions/{jobId}/status` to check the job's state.
    *   The response body contains the current status and potentially error details or a link to the result.
    *   **Possible Status Values:**
        *   `Queued`: The job is waiting in the queue for a worker.
        *   `Processing`: A worker has picked up the job and is actively processing it.
        *   `Completed`: The job finished successfully.
        *   `Failed`: The job encountered an error during processing.
    *   **Response Structure (`AsyncJobStatus` DTO - Conceptual Structure):**
        ```json
        // Example for Queued/Processing
        {
          "jobId": "b7e1f4a0-8c1d-4a7f-9d8b-3e2f1a0c9b8d",
          "status": "Processing", // or "Queued"
          "lastUpdateTime": "2025-04-24T10:30:00Z"
        }

        // Example for Completed
        {
          "jobId": "b7e1f4a0-8c1d-4a7f-9d8b-3e2f1a0c9b8d",
          "status": "Completed",
          "lastUpdateTime": "2025-04-24T10:35:15Z",
          // Optionally include a direct link to the result endpoint
          "resultUrl": "/api/v1/interactions/b7e1f4a0-8c1d-4a7f-9d8b-3e2f1a0c9b8d/result"
        }

        // Example for Failed
        {
          "jobId": "b7e1f4a0-8c1d-4a7f-9d8b-3e2f1a0c9b8d",
          "status": "Failed",
          "lastUpdateTime": "2025-04-24T10:32:45Z",
          "error": {
            "code": "ProcessingError",
            "message": "An error occurred during data analysis.",
            "details": "Specific error details or stack trace (optional)"
          }
        }
        ```

*   **Result Retrieval Endpoint:**
    *   Once the status is `Completed`, the client fetches the actual result using `GET /interactions/{jobId}/result`.
    *   The response body of this endpoint contains the final output of the job (e.g., the `InteractionResponse` DTO, typically `AdapterResponse`, generated file content, etc.). The `Content-Type` header indicates the format.
    *   This separation allows the status endpoint to remain lightweight and prevents large results from being repeatedly sent during polling.

*   **(Optional) Callback/Webhook Mechanism:**
    *   As an alternative or addition to polling, a webhook mechanism could be implemented.
    *   Clients could register a callback URL during the initial request.
    *   The API service (or a dedicated notification worker) would send an HTTP POST request to the client's URL when the job completes or fails.
    *   This reduces polling traffic but requires the client adapter to expose a publicly accessible endpoint.

## 7. Related Documents

*   [API Client Interaction Pattern](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
*   [API Interaction Lifecycle](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [API Activation & Routing](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)
*   [Client Adapters Overview](./05_ARCHITECTURE_CLIENTS.md)
*   [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)
*   [Security Architecture](./06_ARCHITECTURE_SECURITY.md)
*   [Testing Strategy](./09_ARCHITECTURE_TESTING.md)