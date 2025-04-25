---
title: Architecture - Nucleus API Service
description: Overview of the API-First design principles, core responsibilities, endpoint structure, security model, and interaction patterns for the Nucleus API service.
version: 0.3
date: 2025-04-25
parent: ./00_ARCHITECTURE_OVERVIEW.md # Assuming a top-level overview exists
---

# Nucleus API Service Architecture

## 1. Introduction & Principles

*   Purpose of the API Service (Central interaction point)
*   API-First Principle recap (Adapters as translators, logic centralized)
*   Design Goals (Statelessness, RESTful patterns, Security, Scalability, Observability)
*   Key Consumers (Client Adapters, Internal Services, Testing Harnesses)

## 2. Core Responsibilities

*   Authentication & Authorization
*   Interaction Ingestion & Initial Validation
*   Activation Check (Link to [API Activation & Routing](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md))
*   Routing to Synchronous Handlers
*   Queuing for Asynchronous Processing
*   Providing Status/Results for Asynchronous Jobs (Mechanism TBD - polling, webhooks?)
*   Serving Core Data/Metadata (e.g., Persona listing - if applicable via API)
*   Enforcing Security Boundaries

## 3. Endpoint Design & Structure

*   Base URL Path (e.g., `/api/v1/`)
*   Resource Naming Conventions (e.g., `/interactions`, `/personas`, `/artifacts`?)
*   Common HTTP Verbs (POST for interactions, GET for status/data, etc.)
*   Example Endpoint Definitions (High-level):
    *   `POST /interactions` (Primary ingestion endpoint)
    *   `GET /interactions/{jobId}/status` (For async tasks)
    *   `GET /personas` (Example data endpoint)
*   Versioning Strategy

## 4. Data Formats & Contracts

*   Standard Request/Response format (e.g., JSON)
*   Key Data Transfer Objects (DTOs) - High level description. Precise definitions and usage patterns are detailed in specific contract documents:
    *   **Client Message Interactions:** Handled primarily by the `POST /api/v1/interactions` endpoint using the `InteractionRequest` DTO. See:
        *   [API Client Interaction Pattern](./Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)
    *   **Direct Content Ingestion:** Handled by dedicated `/ingest/...` endpoints (e.g., `POST /api/v1/ingest/path`). See:
        *   [API Ingestion Endpoints](./Api/ARCHITECTURE_API_INGESTION.md)
    *   **Common DTOs (Examples):**
        *   `InteractionRequest` (Input for message interactions)
        *   `IngestionRequestPath` (Input for path ingestion)
        *   `InteractionResponse` (Synchronous result)
        *   `AsyncJobStatus` (Response from status endpoint)
*   Error Handling (Standard HTTP status codes, error response body structure)

## 5. Authentication & Authorization

*   Authentication Mechanism (e.g., API Keys, OAuth, Managed Identity)
*   Authorization Strategy (How access is controlled - role-based, scope-based?)
*   Security Considerations (Input validation, rate limiting, etc.)

## 6. Asynchronous Processing Pattern

When an API request (e.g., `POST /interactions`) triggers processing expected to be long-running, the API adopts an asynchronous pattern to avoid holding the client connection open.

*   **Job Submission Response:**
    *   The API immediately responds with `HTTP 202 Accepted`.
    *   The response body **must** include a unique `jobId` (e.g., a GUID string) that the client will use to track the job's progress.
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
    *   **Response Structure (`AsyncJobStatus` DTO - Conceptual):**
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
    *   The response body of this endpoint contains the final output of the job (e.g., the `InteractionResponse` DTO, generated file content, etc.). The `Content-Type` header indicates the format.
    *   This separation allows the status endpoint to remain lightweight and prevents large results from being repeatedly sent during polling.

*   **(Optional) Callback/Webhook Mechanism:**
    *   As an alternative or addition to polling, a webhook mechanism could be implemented.
    *   Clients could register a callback URL during the initial request.
    *   The API service (or a dedicated notification worker) would send an HTTP POST request to the client's URL when the job completes or fails.
    *   This reduces polling traffic but requires the client adapter to expose a publicly accessible endpoint.

## 7. Related Documents

*   [API Interaction Lifecycle](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [API Activation & Routing](./Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md)
*   [Client Adapters Overview](./ClientAdapters/ARCHITECTURE_CLIENT_ADAPTERS.md) (Needs update)
*   [Testing Strategy](./Testing/ARCHITECTURE_TESTING.md) (Needs update)