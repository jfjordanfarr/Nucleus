---
title: Architecture - API Activation & Asynchronous Routing
description: Details the process for activating interactions received via the API (resolving the target Persona) and routing them to the asynchronous background task queue via the OrchestrationService.
version: 2.6
date: 2025-05-07
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus: API Activation & Asynchronous Routing

## 1. Introduction

This document outlines the two key stages involved after an interaction request is received by the `Nucleus.Services.Api`, following the [Interaction Processing Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md) and adhering to the overall [API-First Architecture](../../../10_ARCHITECTURE_API.md):

1.  **Activation Check & Persona Resolution:** Determining if the interaction warrants processing based on configured rules and identifying the target `PersonaId`. This relies on interaction details provided by the client adapter (e.g., originating bot ID, channel context) as described in [API Client Interaction Patterns](../../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md).
2.  **Post-Activation Routing:** Directing an *activated* interaction (now associated with a specific `PersonaId`) **exclusively** to the asynchronous background task queue (`IBackgroundTaskQueue`) via the central `OrchestrationService`.

This centralized approach within the API service supports hosting multiple, distinctly scoped Personas by ensuring requests are routed to the appropriate context.

## 2. Core Components

*   **API Endpoint Handler:** (e.g., `InteractionController`) Receives the initial `AdapterRequest`, performs authentication/authorization, and invokes the `IOrchestrationService`.
*   **Orchestration Service (`IOrchestrationService`):** Central component responsible for handling the interaction lifecycle, including activation checks (which incorporate Persona resolution), context setup, and **routing activated tasks (scoped to a Persona) to the background queue**.
*   **Activation Checker (`IActivationChecker`):** Logic (likely used internally by `OrchestrationService`) responsible for evaluating incoming interaction metadata against configured activation rules to determine if processing is warranted and for which Persona.
*   **Configuration Store:** Source for activation rules (e.g., `appsettings.json`, database), which can be defined per Persona or system-wide.
*   **Background Task Queue (`IBackgroundTaskQueue`):** The sole mechanism for handling activated interactions. Implemented using Azure Service Bus (via `ServiceBusBackgroundTaskQueue` and Aspire's named client registration) to decouple the API handler from the actual processing.
*   **Background Worker Service (`QueuedInteractionProcessorService`):** Service (running as a hosted service) that listens to the `IBackgroundTaskQueue`, dequeues messages (which include `TenantId` and `PersonaId`), and invokes the `OrchestrationService` (or directly the `IPersonaRuntime`) to execute the processing logic within the correct Persona's scope.

## 3. Activation Check Flow

The API Endpoint Handler receives the interaction details (user, `TenantId`, platform context including originating bot/channel identifiers, message content/metadata, **potentially including platform-specific reply identifiers if the Client Adapter detected a reply**) and invokes the `IOrchestrationService`.

```mermaid
graph TD
    A[API Handler Receives Interaction Request] --> B(Parse Request Data, incl. TenantId & Persona resolution cues);
    B -- Interaction Details --> C{Orchestration Service};
    C -- Configured Rules (potentially Persona-specific) / Reply Context --> D{Evaluate Rules & Resolve PersonaId (Mention?, Scope?, User?, Bot ID?, Direct Reply?)};
    D -- Yes (Activate for Resolved PersonaId) --> E[Proceed to Post-Activation Routing];
    D -- No (Ignore or Unresolved Persona) --> F[Return HTTP 200 OK / 204 No Content];
    E --> G[Activated Task Details with PersonaId];
```

**Rule Evaluation Logic:**

*   The `OrchestrationService` checks the interaction against a prioritized list or set of rules. These rules can be associated with specific configured Personas or be system-wide behaviors. The `AdapterRequest` must provide sufficient information (e.g., target bot ID from Teams, specific channel) to help the `OrchestrationService` select the relevant set of rules or directly identify the target Persona.
*   **Implicit Activation for Replies:** Before evaluating standard rules, the `OrchestrationService` checks if the interaction is identified as a direct reply to a message recently sent by Nucleus. This check relies on platform-specific context provided in the API request.
    *   If a direct reply is detected, the interaction is implicitly activated, and the target `PersonaId` is inferred from the original message context.
*   **Standard Activation Rules (Examples):** These rules help determine both if activation should occur and which `PersonaId` is the target.
    *   Does the message text contain `@{PersonaName}` where `PersonaName` maps to a known `PersonaId`?
    *   Does the interaction originate from a channel/chat ID configured for unconditional monitoring by a specific `PersonaId`?
    *   Does the interaction originate from a user ID on a specific watchlist for a `PersonaId`?
    *   Does the originating bot identifier in the `AdapterRequest` map directly to a `PersonaId`?
    *   Is the interaction type a specific system command intended for a particular Persona?
*   The first matching rule typically determines activation and the target `PersonaId`.
*   Configuration needs to allow defining these rules, potentially scoped by `TenantId` and associated with `PersonaName` or `PersonaId`. Example structure:

```json
"ActivationRules": {
  "Tenant_XYZ": {
    "ITPersona_v1": [
      { "Type": "Mention", "Pattern": "@ITHelp" },
      { "Type": "SourceBotId", "Platform": "Teams", "BotId": "it-support-bot-app-id" },
      { "Type": "Scope", "Platform": "Teams", "ScopeId": "it-support-channel-id" }
    ],
    "BusinessPersona_v1": [
      { "Type": "Mention", "Pattern": "@BizInsights" },
      { "Type": "SourceBotId", "Platform": "Teams", "BotId": "biz-insights-bot-app-id" }
    ]
  }
}
```

## 4. Post-Activation Routing (Always Asynchronous)

Once an interaction is activated for a specific `PersonaId`, the `OrchestrationService` **always** routes the task to the `IBackgroundTaskQueue` for asynchronous processing.

```mermaid
graph TD
    A[Activated Task Details with PersonaId] --> B{OrchestrationService prepares Reference Message};
    B -- Reference Message (IDs, ArtifactRefs, TenantId, PersonaId) --> C[Enqueue via IBackgroundTaskQueue (Service Bus)];
    C --> D(API Returns HTTP 202 Accepted);
    subgraph Background Processing (Scoped to PersonaId)
        E[QueuedInteractionProcessorService Dequeues Message]
        E --> F[Hydrate Context & Fetch Content Ephemerally (scoped by PersonaConfiguration)]
        F --> G[Invoke IPersonaRuntime (with specific PersonaConfiguration)]
        G --> H[Process Interaction]
        H --> I[Send Result via IPlatformNotifier]
    end
    C -.-> E;
```

**Routing Logic:**

1.  **Activated Task with Persona Context:** The `OrchestrationService` confirms the task is activated and has an associated `TenantId` and `PersonaId`.
2.  **Reference Message Creation:** It gathers the necessary references and identifiers (`InteractionId`, Context IDs, `ArtifactReference` list, `TenantId`, `PersonaId`) into a message suitable for the queue, ensuring no raw content is included (as detailed in the [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)).
3.  **Enqueueing:** It uses the injected `IBackgroundTaskQueue` implementation to publish the reference message to the configured Azure Service Bus queue (`nucleus-background-tasks`).
4.  **API Response:** The API endpoint handler returns `HTTP 202 Accepted` immediately.

There is **no synchronous execution path** for activated interactions initiated via the main API endpoints. This ensures that all substantive processing occurs within the controlled, scoped environment of a background worker dedicated to the resolved Persona.

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
    *   **Targeted Activation:** Centralized, persona-aware rules allow for precise activation, reducing unnecessary processing for non-target personas.
    *   **Clear Responsibility & Scoping:** API handles activation and queuing for a specific persona; workers handle execution within that persona's defined boundaries.
    *   **Performance:** Avoids broadcast overhead for non-relevant messages.
    *   **Scalability & Resilience:** Leverages Azure Service Bus for robust, scalable background task handling.
    *   **Decoupling:** API responsiveness is maintained as processing is offloaded.
*   **Cons:**
    *   **Centralization:** API activation becomes a potential bottleneck if rule evaluation is extremely complex (though likely faster than broadcast).
    *   **Configuration Management:** Requires a robust way to define and manage activation rules, potentially on a per-persona basis.
    *   **Latency:** All activated tasks incur the latency of the queueing mechanism, even if they *could* theoretically complete quickly (this is a deliberate trade-off for consistency and decoupling).
    *   **State Management:** Retrieving results/status requires careful handling of `jobId`s and potentially polling or callback mechanisms by the client adapters, coordinated with the `IPlatformNotifier` mechanism.

## 7. Related Core Components

The activation and routing logic described above involves the following key interfaces and implementations:

-   **API Entry Point:**
    -   `InteractionController` ([`Nucleus.Services.Api/Controllers/InteractionController.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Controllers/InteractionController.cs)): Receives the initial `AdapterRequest`.
-   **Orchestration Hub:**
    -   `IOrchestrationService` ([`Nucleus.Abstractions/Orchestration/IOrchestrationService.cs`](../../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs)): Defines the orchestration contract.
    -   `OrchestrationService` ([`Nucleus.Domain/Processing/OrchestrationService.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs)): Implements the core orchestration, **persona-aware activation checks**, and **routing to the queue**.
-   **Activation Check:**
    -   `IActivationChecker` ([`Nucleus.Abstractions/Orchestration/IActivationChecker.cs`](../../../../src/Nucleus.Abstractions/Orchestration/IActivationChecker.cs)): Defines the contract for activation checks, which must support Persona resolution.
    -   `ActivationChecker` ([`Nucleus.Domain/Processing/ActivationChecker.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs)): A basic implementation of the activation check, used by `OrchestrationService`, adaptable for multi-persona rule evaluation.
-   **Asynchronous Processing (Queueing & Execution):**
    -   `IBackgroundTaskQueue` ([`Nucleus.Abstractions/IBackgroundTaskQueue.cs`](../../../../src/Nucleus.Abstractions/IBackgroundTaskQueue.cs)): Interface for the background task queue.
    -   `ServiceBusBackgroundTaskQueue` ([`Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusBackgroundTaskQueue.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/ServiceBusBackgroundTaskQueue.cs)): Implementation using Azure Service Bus (registered via `WebApplicationBuilderExtensions`).
    -   `QueuedInteractionProcessorService` ([`Nucleus.Domain/Processing/QueuedInteractionProcessorService.cs`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/QueuedInteractionProcessorService.cs)): Background service processing items dequeued from `IBackgroundTaskQueue`.
    -   `IPlatformNotifier` ([`Nucleus.Abstractions/Adapters/IPlatformNotifier.cs`](../../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs): Interface used by the worker to send results back to the originating platform.

---

[<- Back to Orchestration Overview](ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
