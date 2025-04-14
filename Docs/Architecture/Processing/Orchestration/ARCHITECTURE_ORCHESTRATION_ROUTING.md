---
title: Architecture - Orchestration Routing & Salience
description: Details the proposed mechanism for routing incoming messages to relevant active Persona sessions and managing session context.
version: 1.0
date: 2025-04-13
---

# Nucleus OmniRAG: Orchestration Routing & Salience

**Version:** 1.0
**Date:** 2025-04-13

## 1. Introduction

This document addresses the complex challenge of routing incoming user messages to potentially multiple relevant, active Persona interaction contexts ("sessions") within the Nucleus system. It details how a message is dequeued, hydrated, assessed for salience against active sessions, and how session context is managed ephemerally.

This builds upon the [Interaction Processing Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md) and is a key part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

The primary goals are:
*   **Efficient Hydration:** Fetch message content from the source platform only once.
*   **Accurate Routing:** Deliver the message only to Persona sessions where it is contextually relevant.
*   **Scalability:** Support potentially many concurrent active sessions.
*   **Context Management:** Maintain necessary session state ephemerally.

**The process for determining if a message should initiate a *new* session (if no existing session claims it) and preventing duplicate creation is detailed in [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md).**

## 2. Core Components

*   **`InteractionDispatcher`:** A central orchestration service triggered by messages on its Pub/Sub subscription.
*   **`SessionRegistry`:** 
    *   **Initial Implementation:** An **in-memory collection** (e.g., a thread-safe dictionary managed by a singleton service) within the application instance (e.g., ACA). It tracks active Persona sessions (User + Persona + Conversation) primarily by storing `SessionId` and potentially minimal routing metadata (e.g., ConversationID) and/or identifiers pointing to the location of the session's ephemeral state.
    *   **Future Scaling Options:** Depending on deployment target and scale requirements, this could be externalized to a distributed cache (e.g., Redis) or leverage platform-specific features (e.g., Cloudflare Durable Objects, Azure Durable Entities) if deployment flexibility is constrained.
    *   **Sensitivity:** The registry holds *identifiers* and routing hints, not the full sensitive session context contained within the scratchpad.
*   **`EphemeralMarkdownScratchpad`:** A temporary file per active session holding its conversation history, current state, draft responses, and metadata.
*   **`InternalEventBus`:** An in-process or local cluster eventing mechanism (e.g., MediatR, Dapr Pub/Sub) for broadcasting hydrated messages.

## 3. Proposed Routing Flow

```mermaid
graph TD
    A[Dispatcher Receives & Hydrates Message] --> B{Rule-Based Filtering};
    B -- Filtered --> X(Discard - No Action for New Session);
    B -- Not Filtered --> E[InternalEventBus: Broadcast Hydrated Message Event];
    E --> F(Active Session 1: Receive Event);
    E --> G(Active Session N: Receive Event);
    F --> H{Session 1: Perform Salience Check (vs Scratchpad)};
    G --> I{Session N: Perform Salience Check (vs Scratchpad)};
    H -- Salient --> J[Session 1: Signal Salience, Update Scratchpad];
    I -- Salient --> K[Session N: Signal Salience, Update Scratchpad];
    J --> L[Session 1: Trigger Persona Processing];
    K --> M[Session N: Trigger Persona Processing];
    H -- Not Salient --> N(Session 1: Discard/Timeout);
    I -- Not Salient --> O(Session N: Discard/Timeout);
    subgraph Check for New Session Trigger
        P{Wait for Salience Signals/Timeout}
        P --> Q{Did *any* session signal salience?}
        Q -- Yes --> R(No New Session Needed)
        Q -- No --> S(Proceed to Session Initiation Logic)
    end
    F --> P;
    G --> P;
```

1.  **Receive & Hydrate:** The `InteractionDispatcher` receives a message identifier from its Pub/Sub subscription and hydrates the content via the appropriate `IClientAdapter`.
2.  **Rule-Based Filtering (Optional):** Apply quick, deterministic filters (e.g., message length, keywords) to potentially discard trivial messages early.
3.  **Broadcast Event:** If not filtered, the dispatcher publishes the hydrated message and metadata via the `InternalEventBus` to all active session handlers.
4.  **Session-Level Salience Check:** Each subscribed active session handler receives the event.
    *   It loads its current context (from its `EphemeralMarkdownScratchpad`).
    *   It performs a salience check: Is this new message relevant to *this specific session's* context?
    *   *Method:* Can range from simple rules to LLM-based comparisons, leveraging the session's rich context.
5.  **Signal Salience & Update (If Salient):**
    *   If a session deems the message salient, it **signals** this back (e.g., publishes a confirmation event, updates a shared marker).
    *   It updates its `EphemeralMarkdownScratchpad`.
    *   It triggers its `IPersona` processing.
6.  **Timeout/Discard (If Not Salient):** If a session determines the message is not salient, it effectively discards the event (potentially after a short processing timeout).
7.  **New Session Decision Point:** The `InteractionDispatcher` (or a coordinating component) waits for a defined period or until all potential sessions have responded/timed out.
    *   If **any** session signaled salience (Step 5), no further action is needed regarding new session creation for this message.
    *   If **no** session signaled salience, the dispatcher proceeds to the logic defined in [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md) to attempt atomic creation of a new session via Cosmos DB.

**Note:** This flow prioritizes letting existing sessions claim relevant messages based on their context, including simple follow-ups, before considering the creation of a new session.

## 4. Ephemeral Markdown Scratchpad Structure

This temporary file acts as the short-term memory for an active session. A possible structure:

```markdown
---
SessionId: unique-session-guid
UserId: user-platform-id
ConversationId: platform-convo-id
PersonaId: persona-name
CurrentProcessingTaskId: task-guid-for-last-message # Tracks ongoing work
LastUpdateTime: timestamp
---

## Interaction Goal/Summary
(Optional: LLM-generated summary of the micro-conversation's objective)

## Conversation History (Recent & Token-Limited)
User (Timestamp): Previous relevant message...
Persona (Timestamp): Previous relevant response...
...

## Current User Message (Timestamp)
(Populated when a message is deemed salient by this session)
The full text of the message being processed.

## Persona Draft / Analysis State
(The Persona reads from and writes to this section during processing)

## Retrieved Knowledge Snippets
(Populated by RAG process during Persona execution)
- Snippet 1 Source: [doc_id]
- Snippet 1 Content: ...
```

## 5. Handling Corrections & Updates

Managing subsequent messages that modify a request already in progress requires careful coordination:

1.  **Task Tracking:** When a Persona begins processing based on the scratchpad, the `CurrentProcessingTaskId` in the scratchpad metadata is set.
2.  **Signal on Update:** If a *new* message (e.g., correction) arrives and is deemed salient by a session handler:
    *   The handler checks the `CurrentProcessingTaskId` in its scratchpad.
    *   If a task is active for the message being corrected, the handler must:
        *   Update the scratchpad content with the correction.
        *   **Signal** the running task (identified by `CurrentProcessingTaskId`) to **abort or restart**. This requires a mechanism like `CancellationToken` or a dedicated signaling channel monitored by the Persona task.
        *   Initiate a *new* processing task using the updated scratchpad.

## 6. LLM Caching Considerations (e.g., Gemini)

While prompt caching can reduce costs for repeated identical inputs, its direct benefit for the *salience check* phase might be limited if checking one new message against many *different* session contexts. The most significant caching benefit is likely during the *main Persona processing step*, where the potentially large and relatively stable content of the `EphemeralMarkdownScratchpad` (especially history) is passed to the LLM.

## 7. Trade-offs

*   **Pros:** Single hydration, decoupled salience logic, aligns with ephemeral principles, event-driven scalability. **Using an initial in-memory `SessionRegistry` significantly reduces infrastructure complexity and dependencies.**
*   **Cons:** Increased application complexity (session management, event bus, scratchpad format), potential cost of per-session salience checks (if using LLMs), complexity in handling updates/corrections robustly. **An in-memory registry's state is lost on instance restart and may require sticky sessions or externalization for certain large-scale, cross-instance coordination scenarios.**
