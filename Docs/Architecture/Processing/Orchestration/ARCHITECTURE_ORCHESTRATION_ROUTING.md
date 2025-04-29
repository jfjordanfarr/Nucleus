---
title: Architecture - API Activation & Routing
description: Details the process for activating interactions received via the API and routing them to appropriate synchronous or asynchronous handlers.
version: 2.3
date: 2025-04-27
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus: API Activation & Routing

## 1. Introduction

This document outlines the two key stages involved after an interaction request is received by the `Nucleus.Services.Api`, following the [API Interaction Processing Lifecycle (v4.0)](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md) and adhering to the overall [API-First Architecture](../../../10_ARCHITECTURE_API.md):

1.  **Activation Check:** Determining if the interaction warrants processing based on configured rules. This relies on interaction details provided by the client adapter as described in [API Client Interaction Patterns](../../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md).
2.  **Post-Activation Routing:** Directing an *activated* interaction to the correct internal handler, either for synchronous processing or asynchronous background execution.

This centralized approach within the API service replaces the previous decentralized salience broadcast model.

## 2. Core Components

*   **API Endpoint Handler:** (e.g., ASP.NET Core Controller Action) Receives the initial HTTP request, performs authentication/authorization, and orchestrates the activation and routing process.
*   **Activation Rule Engine:** Logic (likely a dedicated service injected into the handler) responsible for evaluating incoming interaction metadata against configured activation rules.
*   **Configuration Store:** Source for activation rules (e.g., `appsettings.json`, database, dedicated configuration service).
*   **Synchronous Handlers:** Internal services responsible for processing specific types of requests directly within the API request cycle (e.g., simple query service, metadata service).
*   **Internal Task Queue:** Message queue (e.g., RabbitMQ, Azure Service Bus) used to decouple the API handler from long-running background tasks.
*   **Background Worker Services:** Services that listen to the task queue, dequeue messages, and execute the long-running processing logic.

## 3. Activation Check Flow

The API Endpoint Handler receives the interaction details (user, platform context, message content/metadata, **potentially including platform-specific reply identifiers if the Client Adapter detected a reply**) and invokes the Activation Rule Engine.

```mermaid
graph TD
    A[API Handler Receives Interaction Request] --> B(Parse Request Data + Reply Context?);
    B -- Interaction Details --> C{Activation Rule Engine};
    C -- Configured Rules / Reply Context --> D{Evaluate Rules (Mention?, Scope?, User?, Direct Reply?)};
    D -- Yes (Activate) --> E[Proceed to Post-Activation Routing];
    D -- No (Ignore) --> F[Return HTTP 200 OK / 204 No Content];
    E --> G[Activated Task Details];
```

**Rule Evaluation Logic:**

*   The engine checks the interaction against a prioritized list or set of rules associated with configured Personas or system behaviors.
*   **Implicit Activation for Replies:** Before evaluating standard rules (mentions, scopes), the engine checks if the interaction is identified as a direct reply to a message recently sent by Nucleus. This check relies on platform-specific context provided in the API request (extracted by the Client Adapter):
    *   **Teams:** Matching `Conversation.Id` containing a `messageid` corresponding to a recent Nucleus `ActivityId`.
    *   **Discord:** Presence of a `message.reference.message_id` corresponding to a recent Nucleus message ID.
    *   **Slack:** Presence of a `thread_ts` corresponding to the `ts` of a recent Nucleus message.
    *   If a direct reply is detected, the interaction is implicitly activated, bypassing other rules.
*   **Standard Activation Rules (Examples):**
    *   Does the message text contain `@{PersonaName}`?
    *   Does the interaction originate from a channel/chat ID configured for unconditional monitoring?
    *   Does the interaction originate from a user ID on a specific watchlist?
    *   Is the interaction type a specific system command?
*   The first matching rule typically determines activation (and potentially the target Persona/action).
*   Configuration needs to allow defining these rules (e.g., in `appsettings.json` or a database). Example structure:

```json
"ActivationRules": {
  "Default_v1": [
    { "Type": "Mention", "Pattern": "@Default" },
    { "Type": "Scope", "Platform": "Teams", "ScopeId": "channel-id-123" }
  ],
  "Educator_v1": [
    { "Type": "Mention", "Pattern": "@Educator" },
    { "Type": "User", "Platform": "*", "UserId": "boss-user-id" }
  ]
}
```

## 4. Post-Activation Routing

Once an interaction is activated, the API Handler determines the execution path and routes the task accordingly.

```mermaid
graph TD
    A[Activated Task Details] --> B{Determine Execution Path (Sync vs Async)};
    B -- Synchronous --> C[Identify Target Sync Handler];
    C -- Task --> D[Invoke Synchronous Service];
    D -- Result --> E[Return HTTP Response];

    B -- Asynchronous --> F[Identify Target Async Task Type/Queue];
    F -- Task --> G[Construct Task Message];
    G --> H(Publish to Internal Task Queue);
    H --> I[Return HTTP 202 Accepted + JobId];
```

**Routing Logic:**

*   The decision between sync/async might be based on:
    *   The specific API endpoint hit initially.
    *   The nature of the activated task (e.g., ingestion is typically async, simple status check is sync).
    *   Parameters in the request indicating complexity or size.
*   **Synchronous Routing:** The handler resolves (e.g., via DI) and invokes the appropriate internal service interface based on the task type.
*   **Asynchronous Routing:**
    *   The handler identifies the correct *type* of task message and the target *queue* (if multiple queues exist for different priorities or worker types).
    *   It packages all necessary context (user info, interaction details, target persona, artifact references) into the message body.
    *   It publishes the message to the designated internal queue.

## 5. Handling Corrections & Updates (Async Context)

If a user provides a correction or update to a task already submitted for asynchronous processing:

1.  **New Activation:** The correction arrives as a *new* interaction request and goes through the Activation Check.
2.  **Context Linking:** Activation Rules or subsequent logic needs to identify that this new interaction relates to an existing, possibly in-progress, asynchronous job (e.g., based on user, conversation context, or explicit reference to the previous interaction).
3.  **Action:**
    *   **Option A (Cancel & Replace):** If the original job can be safely cancelled, the API could potentially signal cancellation (if the job hasn't started or can be interrupted) via the `jobId` and queue a new task with the corrected information.
    *   **Option B (Append/Update - Complex):** If the job is designed to handle updates, the new interaction could be routed (possibly via a specific 'update' task type) to the relevant worker or state store associated with the `jobId`. This is significantly more complex to implement reliably.
    *   **Option C (Inform User):** The API simply processes the correction as a *new* request, potentially informing the user that it will supersede or follow the previous one. (Simplest approach).

The chosen approach depends on the desired user experience and the interruptibility/updatability of the background tasks.

## 6. Trade-offs

*   **Pros:**
    *   **Simplified Activation:** Centralized rules are easier to manage and understand than distributed salience checks.
    *   **Clear Responsibility:** API handles activation; workers handle execution.
    *   **Performance:** Avoids broadcast overhead for non-relevant messages.
    *   **Flexibility:** Supports both quick synchronous and robust asynchronous execution paths.
*   **Cons:**
    *   **Centralization:** API activation becomes a potential bottleneck if rule evaluation is extremely complex (though likely faster than broadcast).
    *   **Configuration Management:** Requires a robust way to define and manage activation rules.
    *   **State Management (Async):** Retrieving results/status for async jobs requires careful handling of `jobId`s and potentially polling or callback mechanisms by the client adapters.

## 7. Related Core Components

The activation and routing logic described above involves the following key interfaces and implementations:

-   **API Entry Point:**
    -   `InteractionController` ([`Nucleus.Services.Api/Controllers/InteractionController.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs)): Receives the initial `AdapterRequest`.
-   **Orchestration Hub:**
    -   `IOrchestrationService` ([`Nucleus.Abstractions/Orchestration/IOrchestrationService.cs`](../../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs)): Defines the orchestration contract.
    -   `OrchestrationService` ([`Nucleus.Domain/Processing/OrchestrationService.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs)): Implements the core orchestration, activation checks, and routing.
-   **Activation Check:**
    -   `IActivationChecker` ([`Nucleus.Abstractions/Orchestration/IActivationChecker.cs`](../../../../src/Nucleus.Abstractions/Orchestration/IActivationChecker.cs)): Defines the contract for activation checks.
    -   `ActivationChecker` ([`Nucleus.Domain/Processing/ActivationChecker.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs)): A basic implementation of the activation check.
-   **Asynchronous Processing (Queueing):**
    -   `IBackgroundTaskQueue` ([`Nucleus.Abstractions/IBackgroundTaskQueue.cs`](../../../../src/Nucleus.Abstractions/IBackgroundTaskQueue.cs)): Interface for the internal queue.
    -   `InMemoryBackgroundTaskQueue` ([`Nucleus.Domain/Processing/InMemoryBackgroundTaskQueue.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/InMemoryBackgroundTaskQueue.cs)): In-memory implementation of the queue.
    -   `QueuedInteractionProcessorService` ([`Nucleus.Domain/Processing/QueuedInteractionProcessorService.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs)): Background service processing items from `IBackgroundTaskQueue`.
-   **Asynchronous Processing (External Message Queue):**
    -   `IMessageQueuePublisher<T>` ([`Nucleus.Abstractions/IMessageQueuePublisher.cs`](../../../../src/Nucleus.Abstractions/IMessageQueuePublisher.cs)): Interface for publishing to an external queue.
    -   `NullMessageQueuePublisher<T>` ([`Nucleus.Services.Api/Infrastructure/Messaging/NullMessageQueuePublisher.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/NullMessageQueuePublisher.cs)): A null implementation for development/testing.
    -   `ServiceBusQueueConsumerService` ([`Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs)): Background service consuming messages from Azure Service Bus (example external queue consumer).

---

[<- Back to Orchestration Overview](ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
