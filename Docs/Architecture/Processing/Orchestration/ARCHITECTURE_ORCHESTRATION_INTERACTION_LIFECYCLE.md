---
title: Architecture - API Interaction Lifecycle (Activation & Hybrid Execution)
description: Details the process for handling user interactions via the Nucleus API, including initial activation checks (mentions, rules) and subsequent synchronous or asynchronous execution, incorporating context ranking.
version: 4.4
date: 2025-04-27
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus: API Interaction Processing Lifecycle (Activation & Hybrid Execution)

## 1. Introduction

This document describes the end-to-end lifecycle for processing user interactions initiated via the `Nucleus.Services.Api`, adhering to the [API-First principle](../../10_ARCHITECTURE_API.md). It outlines a model incorporating:

1.  **Activation Check:** An initial step within the API to determine if an incoming interaction warrants processing based on configured rules (e.g., explicit mentions like `@{PersonaName}`, specific scopes, user lists).
2.  **Hybrid Execution:** Subsequent processing via either:
    *   **Synchronous Processing:** For quick tasks handled directly.
    *   **Asynchronous Processing:** For long-running tasks handled by background workers via an internal task queue.

This approach simplifies triggering while maintaining flexibility and robustness for task execution.

## 2. API Request Received (Common Starting Point)

All potential interactions begin when a Client Adapter sends an **HTTP request** to a designated `Nucleus.Services.Api` endpoint (e.g., `POST /api/v1/interactions`).

*   **Authentication & Authorization:** Standard checks are performed.
*   **Request Parsing & Validation:** Request data (message content/metadata, user info, channel/context info, crucially `ArtifactReference` list) is extracted and validated.

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
2.  **Context Establishment & Ephemeral Content Fetching:** Gather necessary context (user, session, history, request details, resolved Persona ID). 
    *   **Metadata Search (Implicit):** The `IPersonaManager` first queries the secure metadata index ([`IMetadataService`](../../Storage/ARCHITECTURE_STORAGE_METADATA.md)) based on the interaction context and query to identify potentially relevant artifacts known to the system.
    *   **Context Ranking (4 R System):** If multiple relevant artifacts are identified via metadata, the `IPersonaManager` applies a ranking algorithm to prioritize which ones are most salient for the current task. This **"4 R Ranking System"** typically considers:
        1.  **Recency:** Last modified/created date of the artifact or related message.
        2.  **Relevancy:** Semantic and keyword similarity score between the artifact's metadata/summary and the user's query.
        3.  **Richness:** A metric indicating the substance or completeness of the artifact (e.g., unique word count, length).
        4.  **Reputation:** User-provided signals like explicit votes (+1/-1), endorsements, or feedback associated with the artifact metadata.
    *   **Selective Ephemeral Fetching:** Based on the ranking and persona configuration (e.g., `MaxContextDocuments` from [Persona Configuration](../../Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md)), the `IPersonaManager` requests the full content of the *top-ranked* artifacts ephemerally using `IArtifactProvider.GetContentAsync` with the relevant `ArtifactReference`(s) from the original request or metadata search results. *Only the necessary, highly-ranked content is fetched.* 
3.  **Invoke Core Processing Logic:** Call the shared internal processing method (`ProcessInteractionCoreAsync` in `OrchestrationService`) *synchronously*, passing the established context and any fetched ephemeral content.
    *   This core logic retrieves the specific `IPersonaManager` based on the resolved Persona ID.
    *   It then invokes the manager's processing method (e.g., `InitiateNewSessionAsync`), providing the necessary context and content.
4.  **Persona Processing (within Manager):** The Persona Manager performs its work using the provided data and temporary structures (e.g., internal plan document), generating a primary result and potentially content for a 'Show Your Work' artifact.
5.  **Response Generation & Artifact Handling:**
    *   The `OrchestrationService` receives the primary result and any 'Show Your Work' content back from the `IPersonaManager`.
    *   **Conditional Artifact Saving ("Show Your Work"):**
        *   The configuration for `ShowYourWork` should **default to `true`** for most Personas.
        *   If true, use the appropriate `IArtifactProvider.SaveAsync` implementation (passing the 'Show Your Work' content and context like Persona ID, interaction details) to persist the artifact in the designated location (e.g., `.Nucleus/Personas/{PersonaId}/ShowYourWork/` in the user's storage). The `IArtifactProvider` handles platform specifics (e.g., Graph API).
        *   **Rationale:** Saving these artifacts by default is crucial. It enhances the knowledge base by creating pointers (`ArtifactMetadata`) in CosmosDB to detailed reasoning stored securely in user storage. This allows future interactions to leverage past work, improving accuracy not just in responses but also in analyzing and refining tool usage patterns.
        *   Store relevant metadata (e.g., saved artifact URI) if needed for the final response.
6.  **HTTP Response:** Return the primary result (from the Manager) and potentially metadata about the saved 'Show Your Work' artifact directly in the HTTP response body. Where feasible (depending on platform capabilities surfaced by the Client Adapter), the final message sent to the user should include a link or attachment pointing to the saved 'Show Your Work' artifact.

## 6. Asynchronous Interaction Lifecycle ("Background Task")

(Activated interactions suitable for background processing)

1.  **Enqueue Task:**
    *   The API handler validates the request and activation as per steps 2 & 3.
    *   It gathers minimal necessary context (request details, user info, resolved Persona ID, `ArtifactReference` list) and places it onto a persistent queue (e.g., Azure Queue Storage, Redis Stream) managed by `IQueuedInteractionPublisher`.
    *   The API handler immediately returns `HTTP 202 Accepted` to the Client Adapter, including potentially a unique `jobId` (if status tracking is implemented).
2.  **Background Worker Processing:**
    *   Dedicated worker services (`QueuedInteractionProcessorService`) pick tasks from the queue.
    *   The worker re-hydrates context, performs **Context Ranking and Selective Ephemeral Fetching** similar to the synchronous path (using the "4 R Ranking System" described in Section 5, Step 2), and invokes the core processing logic (`ProcessInteractionCoreAsync`) to execute the `IPersonaManager`'s task.
    *   **Response Generation & Artifact Handling (by Worker/Orchestrator):**
        *   The worker service (or `ProcessInteractionCoreAsync`) receives the primary result and any 'Show Your Work' content back from the `IPersonaManager`.
        *   **Conditional Artifact Saving ("Show Your Work"):**
            *   Configuration should **default to `ShowYourWork: true`**.
            *   If true, use `IArtifactProvider.SaveAsync` (passing content and context) to persist the artifact in the designated location. `IArtifactProvider` handles platform specifics.
            *   **Rationale:** As with the synchronous path, saving this enhances the knowledge base, improves future reasoning, and aids in analyzing tool usage.
3.  **Result Handling & Client Notification:**
    *   Worker stores the primary result (or pointer) and potentially metadata about the saved 'Show Your Work' artifact, associated with the original interaction or `jobId`.
    *   Client Adapter retrieves the result via **Polling** (`GET /api/jobs/{jobId}/status`, `GET /api/jobs/{jobId}/result`), **Webhook**, or other configured mechanism (e.g., **WebSocket/SignalR**, or a direct notification via `IPlatformNotifier` if applicable).
    *   The final message sent to the user (via `IPlatformNotifier` or handled by the Adapter upon result retrieval) should, where feasible, include a link or attachment pointing to the saved 'Show Your Work' artifact.

## 7. Ephemeral Scratchpad (Conceptual)

Remains relevant during the *active processing phase* (within the `IPersonaManager` for both sync and async) to hold temporary state for the activated task.

## 8. Client Adapter Responsibility

Client adapters interact *only* with the API endpoints. They are responsible for:
*   Sending interaction details (including `ArtifactReference` list) to the API (e.g., `POST /api/v1/interactions`).
*   Handling the immediate API response:
    *   If non-activating (`200 OK`/`204 No Content`), no further action needed.
    *   If synchronous result (`200 OK` with body), present it to the user.
    *   If asynchronous acknowledgment (`202 Accepted`), store the `jobId` (if provided) and implement a mechanism (polling, webhook receiver) to retrieve and present the final result later.
*   **(See Section 9) Monitoring for and reporting user feedback reactions.**

## 9. Capturing User Feedback (Ranking Signal)

To continuously improve Persona performance and response quality, capturing explicit user feedback is essential.

1.  **Feedback Detection (Client Adapter):**
    *   The Client Adapter (e.g., for Microsoft Teams) monitors for specific user reactions on messages *sent by Nucleus Personas*. The primary reactions to monitor are thumbs-up (👍) and thumbs-down (👎).
    *   The Adapter needs to associate the reaction with the specific Nucleus message ID (e.g., `ActivityId` in Teams) it corresponds to.
2.  **Feedback Reporting (Adapter -> API):**
    *   When a relevant reaction is detected, the Client Adapter constructs a dedicated feedback request.
    *   This request is sent to a specific API endpoint (e.g., `POST /api/v1/feedback`).
    *   The request body must include:
        *   Identifier for the original Nucleus message/interaction (e.g., the `ActivityId` or a related `jobId`).
        *   The type of feedback (e.g., `Positive`, `Negative`).
        *   Platform context (User ID, etc.).
3.  **Metadata Update (API Service):**
    *   The API Service receives the feedback request.
    *   It locates the relevant `ArtifactMetadata` record(s) associated with the original interaction/response.
    *   It updates the metadata (or a related feedback entity/field) to store this ranking signal (e.g., incrementing a `PositiveFeedbackCount` or `NegativeFeedbackCount`, or storing timestamped feedback events).
    *   This feedback data becomes a valuable signal for future analysis, fine-tuning, and potentially influencing retrieval or generation strategies.

## 10. Related Core Components

The interaction lifecycle described involves several key components:

*   **Orchestration Service:** Coordinates the overall flow.
    *   Interface: [`IOrchestrationService`](../../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs)
    *   Implementation: [`OrchestrationService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs)
*   **Persona Management:** Handles persona-specific logic and context.
    *   Interface: [`IPersonaManager`](../../../../src/Nucleus.Abstractions/Orchestration/IPersonaManager.cs)
    *   Implementation: [`DefaultPersonaManager`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/DefaultPersonaManager.cs)
*   **Artifact Handling:** Manages access to external content.
    *   Interface: [`IArtifactProvider`](../../../../src/Nucleus.Abstractions/IArtifactProvider.cs)
    *   Implementation (Example): [`ConsoleArtifactProvider`](../../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/ConsoleArtifactProvider.cs)
*   **Metadata Storage:** Persists information about artifacts.
    *   Interface: [`IArtifactMetadataRepository`](../../../../src/Nucleus.Abstractions/Repositories/IArtifactMetadataRepository.cs)
    *   Implementations:
        *   [`CosmosDbArtifactMetadataRepository`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs)
        *   [`InMemoryArtifactMetadataRepository`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/InMemoryArtifactMetadataRepository.cs)
*   **Asynchronous Processing:** Handles background tasks.
    *   Implementation: [`QueuedInteractionProcessorService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs) (*Handles dequeuing and processing*)
    *   Interface (Notification): [`IPlatformNotifier`](../../../../src/Nucleus.Abstractions/Orchestration/IPlatformNotifier.cs) (*Used by worker to send results*)

This refined model separates the initial *activation decision* from the subsequent *execution strategy*, ensuring proper separation of concerns for data handling and incorporating valuable user feedback loops.