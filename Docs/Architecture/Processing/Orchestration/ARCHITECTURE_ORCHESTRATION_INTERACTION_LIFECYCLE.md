---
title: Architecture - Interaction Lifecycle (Activation & Queued Execution)
description: Details the process for handling user interactions via the Nucleus API, including initial activation checks and subsequent asynchronous execution via the background task queue, leveraging IPersonaRuntime within potentially multi-persona environments.
version: 4.9
date: 2025-05-07
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus: Interaction Processing Lifecycle (Activation & Queued Execution)

## 1. Introduction

This document describes the end-to-end lifecycle for processing user interactions initiated via the `Nucleus.Services.Api`, adhering to the [API-First principle](../../10_ARCHITECTURE_API.md). It outlines a model for a system that may host multiple, distinctly scoped Personas, incorporating:

1.  **Activation Check:** An initial step within the API to determine if an incoming interaction warrants processing based on configured rules (e.g., explicit mentions like `@{PersonaName}`, specific scopes, user lists) and to identify the target Persona.
2.  **Queued Execution:** Subsequent processing is *always* handled asynchronously via the internal background task queue (`nucleus-background-tasks`) for activated interactions, ensuring operations are correctly scoped to the resolved Persona.

## 2. API Request Received (Common Starting Point)

All potential interactions begin when a Client Adapter sends an **HTTP request** to a designated `Nucleus.Services.Api` endpoint (e.g., `POST /api/v1/interactions`).

*   **Authentication & Authorization:** Standard checks are performed.
*   **Request Parsing & Validation:** Request data (message content/metadata, user info, channel/context info, crucially `ArtifactReference` list, and identifiers necessary for Persona resolution like originating bot ID or channel ID) is extracted and validated.

## 3. Activation Check (Within API)

Before any significant processing occurs, the API handler performs an **activation check** based on centrally configured rules:

*   **Rule Evaluation:** The interaction details (including `TenantId` and any data from `AdapterRequest` useful for identifying the target Persona) are compared against rules such as:
    *   Presence of a specific mention (e.g., `@{PersonaName}`).
    *   Originating from a pre-configured scope (channel, team, chat) where a specific Persona should listen unconditionally.
    *   Originating from a specific user a Persona is configured to monitor.
    *   The originating bot/adapter identifier implying a specific target Persona.
    *   Other custom logic defined for the Persona or system-wide.
*   **Persona Resolution:** A key outcome of this step is the resolution of the target `PersonaId` (or multiple, if applicable and supported).
*   **Decision:**
    *   **If NO activation rule matches OR no target Persona can be resolved:** The API logs the check (optional) and returns a non-committal success response (e.g., `HTTP 200 OK` or `HTTP 204 No Content`). No further Nucleus processing occurs for this interaction.
    *   **If an activation rule DOES match AND a PersonaId is resolved:** The interaction is deemed relevant, and processing proceeds to the queuing phase, scoped to the resolved `PersonaId`.

## 4. Interaction Queuing (Post-Activation)

For all activated interactions, the API handler prepares and enqueues a message for background processing:

1.  **Reference Message Creation:**
    *   The API service gathers the essential **references and identifiers** needed for the background worker to reconstruct the context. This **must not** include raw message content or file content, adhering to the Zero Trust principle ([Security Architecture](../../06_ARCHITECTURE_SECURITY.md#L1-163)). The resolved `PersonaId` is critical here as it dictates all subsequent scoped access.
    *   Essential data includes:
        *   `InteractionId` (unique identifier for this specific interaction event).
        *   `TenantId`.
        *   Relevant Context IDs (e.g., `UserId`, `SessionId`, `PlatformContextId`).
        *   List of `ArtifactReference` objects from the original request.
        *   Resolved `PersonaId`(s) targeted by the activation.
        *   Correlation ID.
2.  **Enqueueing the Task:**
    *   The reference message is serialized and placed onto the `nucleus-background-tasks` Azure Service Bus Queue using the `IBackgroundTaskQueue` abstraction.
    *   The message includes necessary metadata for routing and processing.
3.  **API Response:**
    *   The API handler immediately returns `HTTP 202 Accepted` to the Client Adapter, potentially including a unique `jobId` (if status tracking is implemented) to allow the client to poll for results if needed (though direct notification via `IPlatformNotifier` is preferred).

## 5. Background Processing Lifecycle (Asynchronous)

(Handles all activated interactions via the queue)

1.  **Dequeuing (by `QueuedInteractionProcessorService`):**
    *   A background service (`QueuedInteractionProcessorService`, typically running as a hosted service) continuously monitors the `nucleus-background-tasks` queue using `IBackgroundTaskQueue.DequeueAsync()`. 
    *   `DequeueAsync` attempts to retrieve the next available message.
    *   If successful, it returns a `DequeuedMessage<NucleusIngestionRequest>` object which contains both the deserialized `NucleusIngestionRequest` (the reference message created in Step 4) and the raw `ServiceBusReceivedMessage` context necessary for later completion or abandonment.

2.  **Context Reconstruction & Content Fetching (by `QueuedInteractionProcessorService`):**
    *   Using the `NucleusIngestionRequest` payload (which includes the `TenantId` and `PersonaId`), the service reconstructs the necessary `InteractionContext`.
    *   This involves using the `ArtifactReference` list and `IArtifactProvider` implementations (like `FileArtifactProvider`, `TeamsAttachmentProvider`, etc.) to fetch the *actual* content of mentioned files or attachments. **Access via `IArtifactProvider` must be scoped and constrained by the permissions and knowledge boundaries defined in the `PersonaConfiguration` associated with the resolved `PersonaId` and `TenantId`.**
    *   The fetched content (`ArtifactContent` including streams and metadata) is added to the `InteractionContext`.

3.  **Persona Execution (Invoking `IPersonaRuntime`):**
    *   The `QueuedInteractionProcessorService` resolves the `IPersonaRuntime` service.
    *   It loads the relevant `PersonaConfiguration` using `IPersonaConfigurationProvider` based on the `TenantId` and `ResolvedPersonaId` from the request. This configuration dictates the Persona's behavior, accessible knowledge (e.g., specific SharePoint sites, CosmosDB containers/partitions), and permissions.
    *   It invokes `IPersonaRuntime.ExecuteAsync()`, passing the loaded (and therefore scoped) `PersonaConfiguration` and the fully reconstructed `InteractionContext`.
    *   The `PersonaRuntime` executes the appropriate `IAgenticStrategyHandler` based on the persona's configuration.
    *   The persona logic operates on the ephemerally fetched content and context, adhering to the scopes defined in its configuration to ensure data isolation between different Personas.

4.  **Execution Result & Message Completion (by `QueuedInteractionProcessorService`):**
    *   `IPersonaRuntime.ExecuteAsync()` returns a tuple containing the `AdapterResponse` and the `PersonaExecutionStatus`.
    *   The `QueuedInteractionProcessorService` examines the `PersonaExecutionStatus`:
        *   **Success / Filtered / NoActionTaken:** These statuses indicate the interaction was processed successfully from the queue's perspective. The service calls `IBackgroundTaskQueue.CompleteAsync()`, passing the message context from the original `DequeuedMessage`, to permanently remove the message from the queue (ACK).
        *   **Failed / Unknown:** These statuses indicate a failure during persona execution. The service calls `IBackgroundTaskQueue.AbandonAsync()`, passing the message context, to make the message visible again on the queue for potential retries or dead-lettering (NACK).
    *   Any exceptions caught during steps 1-4 (e.g., failure to fetch content, runtime exceptions) will also typically lead to the message being abandoned via `AbandonAsync` in the service's error handling.
    *   The `AdapterResponse` payload (if `Success` was true) contains the result generated by the persona. If a response needs to be sent back to the originating platform, the `PersonaRuntime` or its strategy handler is responsible for invoking the appropriate `IPlatformNotifier` (This is **not** handled directly by the `QueuedInteractionProcessorService`).
    *   Artifacts generated or modified by the persona (e.g., summaries, analysis results) should be saved to persistent storage (e.g., via `IArtifactMetadataRepository`, ensuring data is stored in a way that respects persona/tenant isolation, such as using persona-specific containers or partition keys) by the `PersonaRuntime` or its strategy handler during execution.

## 6. Capturing User Feedback (Ranking Signal)

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

## 7. Related Core Components

The interaction lifecycle described involves several key components:

*   **API Interaction Controller:** Entry point for all interactions, receives requests from Client Adapters.
    *   Implementation: [`InteractionController`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs)
*   **Orchestration Service:** Coordinates the overall flow, including activation and queuing.
    *   Interface: [`IOrchestrationService`](../../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs)
    *   Implementation: [`OrchestrationService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs)
*   **Activation Checker:** Determines if an interaction should activate a Persona based on configured rules.
    *   Interface: [`IActivationChecker`](../../../../src/Nucleus.Abstractions/Orchestration/IActivationChecker.cs)
    *   Implementation (Example): [`ActivationChecker`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs)
*   **Persona Resolver:** Identifies the target `PersonaId` based on the incoming request and platform context.
    *   Interface: [`IPersonaResolver`](../../../../src/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs)
    *   Implementation (Example): [`DefaultPersonaResolver`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/DefaultPersonaResolver.cs)
*   **Persona Runtime:** Handles execution based on configuration.
    *   Interface: [`IPersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs)
    *   Implementation: [`PersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs)
*   **Agentic Strategy Handler:** Implements specific persona logic.
    *   Interface: [`IAgenticStrategyHandler`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IAgenticStrategyHandler.cs)
    *   Implementation (Example): [`EchoAgenticStrategyHandler`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Strategies/EchoAgenticStrategyHandler.cs)
*   **Persona Configuration:** Provides persona settings, including knowledge scopes and operational parameters. It is critical for multi-persona isolation.
    *   Interface: [`IPersonaConfigurationProvider`](../../../../src/Nucleus.Abstractions/Models/Configuration/IPersonaConfigurationProvider.cs)
    *   Implementation (Example): [`InMemoryPersonaConfigurationProvider`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Configuration/InMemoryPersonaConfigurationProvider.cs)
*   **Artifact Handling:** Manages access to external content, **operating within the scopes defined by the active Persona's configuration.**
    *   Interface: [`IArtifactProvider`](../../../../src/Nucleus.Abstractions/IArtifactProvider.cs)
    *   Implementation (Example): `LocalFileArtifactProvider` (Conceptual example for handling local file system paths)
*   **Metadata Storage:** Persists information about artifacts, **ensuring data is segregated or partitioned appropriately for multi-tenant/multi-persona scenarios.**
    *   Interface: [`IArtifactMetadataRepository`](../../../../src/Nucleus.Abstractions/Repositories/IArtifactMetadataRepository.cs)
    *   Implementations:
        *   [`CosmosDbArtifactMetadataRepository`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs)
        *   [`InMemoryArtifactMetadataRepository`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/InMemoryArtifactMetadataRepository.cs)
*   **Asynchronous Processing:** Handles background tasks.
    *   Interface (Queue): [`IBackgroundTaskQueue`](../../../../src/Nucleus.Abstractions/Orchestration/IBackgroundTaskQueue.cs) (*Defines queue operations like Enqueue, Dequeue, Complete, Abandon*)
    *   Implementation (Queue): [`ServiceBusBackgroundTaskQueue`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusBackgroundTaskQueue.cs) (*Azure Service Bus implementation*)
    *   Implementation (Processor): [`QueuedInteractionProcessorService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs) (*Hosted service that dequeues, orchestrates processing via PersonaRuntime, and manages message lifecycle [ACK/NACK]*)
    *   Interface (Notification): [`IPlatformNotifier`](../../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs) (*Used by PersonaRuntime/Strategies to send results*)

This refined model separates the initial *activation decision* (including Persona resolution) from the subsequent *queued execution*, ensuring all processing happens asynchronously via background workers, adhering to security principles by passing only references, enforcing persona-specific scopes for data access, and incorporating valuable user feedback loops.