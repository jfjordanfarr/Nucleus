---
title: Architecture - API Interaction Lifecycle (Activation & Hybrid Execution)
description: Details the process for handling user interactions via the Nucleus API, including initial activation checks (mentions, rules) and subsequent synchronous or asynchronous execution.
version: 4.2
date: 2025-04-25
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus OmniRAG: API Interaction Processing Lifecycle (Activation & Hybrid Execution)

## 1. Introduction

This document describes the end-to-end lifecycle for processing user interactions initiated via the `Nucleus.Services.Api`, adhering to the [API-First principle](../ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md#api-first-architecture). It outlines a model incorporating:

1.  **Activation Check:** An initial step within the API to determine if an incoming interaction warrants processing based on configured rules (e.g., explicit mentions like `@{PersonaName}`, specific scopes, user lists).
2.  **Hybrid Execution:** Subsequent processing via either:
    *   **Synchronous Processing:** For quick tasks handled directly.
    *   **Asynchronous Processing:** For long-running tasks handled by background workers via an internal task queue.

This approach simplifies triggering while maintaining flexibility and robustness for task execution.

## 2. API Request Received (Common Starting Point)

All potential interactions begin when a Client Adapter sends an **HTTP request** to a designated `Nucleus.Services.Api` endpoint (e.g., `POST /api/interactions`).

*   **Authentication & Authorization:** Standard checks are performed.
*   **Request Parsing & Validation:** Request data (message content/metadata, user info, channel/context info) is extracted and validated.

## 3. Activation Check (Within API)

Before any significant processing occurs, the API handler performs an **activation check** based on centrally configured rules:

*   **Rule Evaluation:** The interaction details are compared against rules such as:
    *   Presence of a specific mention (e.g., `@{PersonaName}`).
    *   Originating from a pre-configured scope (channel, team, chat) where the Persona should listen unconditionally.
    *   Originating from a specific user the Persona is configured to monitor.
    *   Other custom logic defined for the Persona or system-wide.
*   **Decision:**
    *   **If NO activation rule matches:** The API logs the check (optional) and returns a non-committal success response (e.g., `HTTP 200 OK` or `HTTP 204 No Content`). No further Nucleus processing occurs for this interaction.
    *   **If an activation rule DOES match:** The interaction is deemed relevant, and processing proceeds to the execution phase.

## 4. Execution Path Decision (Post-Activation)

For activated interactions, the API handler determines the appropriate execution path based on the nature of the request (inferred from endpoint, parameters, or initial analysis):

*   **Synchronous Path Chosen:** For requests expected to complete quickly.
*   **Asynchronous Path Chosen:** For tasks expected to be long-running or resource-intensive.

## 5. Synchronous Interaction Lifecycle ("Fast Path")

(Activated interactions suitable for immediate response)

1.  **Persona Resolution:** Resolve the appropriate Persona ID based on the interaction context using the configured `IPersonaResolver`.
2.  **Context Establishment:** Gather necessary context (user, session, history, request details, resolved Persona ID).
3.  **Invoke Core Processing Logic:** Call the shared internal processing method (`ProcessInteractionCoreAsync` in `OrchestrationService`) *synchronously*, passing the established context.
    *   This core logic retrieves the specific `IPersonaManager` based on the resolved Persona ID.
    *   It then invokes the manager's processing method (e.g., `InitiateNewSessionAsync`).
4.  **Ephemeral Processing (within Manager):** Perform work using temporary structures (e.g., internal plan document).
5.  **Response Generation (by Manager):** Create the result.
6.  **Conditional Artifact Generation ("Show Your Work"):**
    *   Check the configuration of the active Persona for a `ShowYourWork: true` setting.
    *   If true, package the internal plan/reasoning document (from step 4) as an artifact.
    *   Coordinate with the appropriate storage mechanism (e.g., calling Graph API via context provided by the adapter for Teams SharePoint storage at `.Nucleus/Personas/{PersonaId}/ShowYourWork/`) to persist this artifact. Store relevant metadata (e.g., artifact URI) if needed for the final response.
7.  **HTTP Response:** Return the primary result (generated in step 5, and potentially metadata about the 'Show Your Work' artifact) directly in the HTTP response body.

## 6. Asynchronous Interaction Lifecycle ("Slow Path")

(Activated interactions requiring background processing)

1.  **Persona Resolution & Task Queuing:**
    *   The API handler resolves the appropriate Persona ID based on the interaction context using `IPersonaResolver`.
    *   It packages the required information (interaction details, context, **resolved Persona ID**) into a task message (`NucleusIngestionRequest`).
    *   This message is placed onto an **internal task queue** (e.g., `IBackgroundTaskQueue`) dedicated to distributing *activated* workloads to background workers.
    *   The API handler immediately returns `HTTP 202 Accepted` to the Client Adapter, including potentially a unique `jobId` (if status tracking is implemented).
2.  **Background Worker Processing:**
    *   Dedicated worker services (`QueuedInteractionProcessorService`) pick tasks (`NucleusIngestionRequest`) from the queue.
    *   **Context Reconstruction:** Extract the interaction details and the **resolved Persona ID** from the task message.
    *   **Invoke Core Processing Logic:** Call the *same shared internal processing method* (`ProcessInteractionCoreAsync`) as the synchronous path, passing the reconstructed context and resolved Persona ID.
        *   This core logic retrieves the specific `IPersonaManager` based on the resolved Persona ID.
        *   It then invokes the manager's processing method (e.g., `InitiateNewSessionAsync`).
    *   **Secure Data Fetching (if needed, by Manager):** Fetch external artifact content directly from source storage (e.g., Graph API).
    *   **Core Logic Execution (within Manager):** Perform the main processing (RAG, tool use, etc.), potentially generating an internal plan document.
    *   **Ephemeral Processing (within Manager):** Use temporary state during execution.
    *   **Conditional Artifact Generation ("Show Your Work"):**
        *   Check the configuration of the active Persona (using the resolved Persona ID) for a `ShowYourWork: true` setting.
        *   If true, package the internal plan/reasoning document (from core logic execution) as an artifact.
        *   Persist this artifact using the appropriate mechanism for the context (e.g., using Graph API for Teams SharePoint storage at `.Nucleus/Personas/{PersonaId}/ShowYourWork/`).
3.  **Result Handling & Client Notification:**
    *   Worker stores the primary result (or pointer) and potentially metadata about the saved 'Show Your Work' artifact, associated with the original interaction or `jobId`.
    *   Client Adapter retrieves the result via **Polling** (`GET /api/jobs/{jobId}/status`, `GET /api/jobs/{jobId}/result`), **Webhook**, or other configured mechanism (e.g., **WebSocket/SignalR**, or a direct notification via `IPlatformNotifier` if applicable).

## 7. Ephemeral Scratchpad (Conceptual)

Remains relevant during the *active processing phase* (within the `IPersonaManager` for both sync and async) to hold temporary state for the activated task.

## 8. Client Adapter Responsibility

Client adapters interact *only* with the API endpoints. They are responsible for:
*   Sending interaction details to the API (e.g., `POST /api/interactions`).
*   Handling the immediate API response:
    *   If non-activating (`200 OK`/`204 No Content`), no further action needed.
    *   If synchronous result (`200 OK` with body), present it to the user.
    *   If asynchronous acknowledgment (`202 Accepted`), store the `jobId` (if provided) and implement a mechanism (polling, webhook receiver) to retrieve and present the final result later.

This refined model separates the initial *activation decision* from the subsequent *execution strategy*, providing a clearer and potentially simpler architecture for handling interactions.