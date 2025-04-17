---
title: Architecture - Orchestration Routing & Salience (v1.1)
description: Details the mechanism for routing incoming messages to relevant active Persona sessions via Persona Managers.
version: 1.1
date: 2025-04-16

---

# Nucleus OmniRAG: Orchestration Routing & Salience

**Version:** 1.1
**Date:** 2025-04-16

## 1. Introduction

This document details how an incoming user message is assessed for **salience** against potentially relevant, active Persona interaction contexts ("sessions") managed within the Nucleus system. It focuses on routing the message to an *existing* session if appropriate.

This process relies on **decentralized session management**, where dedicated `PersonaManager` components handle the state and salience checks for the specific Persona types they are responsible for.

This builds upon the [Interaction Processing Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md) and is a key part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

The primary goals are:
*   **Efficient Hydration:** Fetch message content from the source platform only once.
*   **Accurate Routing:** Deliver the message only to the `PersonaManager` (and subsequently, the specific `PersonaInteractionContext`) where it is contextually relevant.
*   **Scalability:** Support potentially many concurrent active sessions distributed across `PersonaManagers`.
*   **Context Management:** Maintain necessary session state ephemerally within each session's scratchpad.

**The process for determining if a message should initiate a *new* session (if no existing session claims it) is detailed in [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md).**

## 2. Core Components

*   **`InteractionRouter`:** A central orchestration service (or logic block) triggered by incoming messages (e.g., from Pub/Sub). Responsible for hydrating the message and broadcasting it for salience checks and potential new session initiation.
*   **`PersonaManager` (Multiple Instances):**
    *   A component responsible for managing the lifecycle and interactions for **a specific type of Persona** (e.g., `BootstrapperPersonaManager`, `EduFlowPersonaManager`).
    *   Each `PersonaManager` maintains the state of *its own* active `PersonaInteractionContext` sessions (e.g., in an in-memory collection holding session IDs and references to their `EphemeralMarkdownScratchpad`).
    *   It listens for broadcasted messages from the `InteractionRouter`.
    *   It performs the salience check for incoming messages against the active sessions it manages.
*   **`EphemeralMarkdownScratchpad`:** A temporary file (or in-memory structure) per active session holding its conversation history, current state, draft responses, and metadata. Managed indirectly via the `PersonaManager` and `PersonaInteractionContext`.
*   **`InternalEventBus`:** An in-process or distributed mechanism (like MediatR or a lightweight Pub/Sub) used by the `InteractionRouter` to broadcast the hydrated message to all registered `PersonaManager` instances.

## 3. Proposed Routing Flow (Salience Check for Existing Sessions)

```mermaid
graph TD
    A[InteractionRouter Receives & Hydrates Message] --> B{Rule-Based Filtering};
    B -- Filtered --> X(Discard);
    B -- Not Filtered --> E[InternalEventBus: Broadcast Hydrated Message Event];

    subgraph Persona Manager 1 (e.g., Bootstrapper)
        E --> PM1_Rcv(Receive Event);
        PM1_Rcv --> PM1_Chk{Check Salience vs Own Active Sessions};
        PM1_Chk -- Salient (Session S1) --> PM1_Sig[Signal Salience, Update Scratchpad S1];
        PM1_Sig --> PM1_Trig[Trigger Persona S1 Processing];
        PM1_Chk -- Not Salient --> PM1_Disc(Discard/Timeout);
    end

    subgraph Persona Manager N (e.g., EduFlow)
        E --> PMN_Rcv(Receive Event);
        PMN_Rcv --> PMN_Chk{Check Salience vs Own Active Sessions};
        PMN_Chk -- Salient (Session S2) --> PMN_Sig[Signal Salience, Update Scratchpad S2];
        PMN_Sig --> PMN_Trig[Trigger Persona S2 Processing];
        PMN_Chk -- Not Salient --> PMN_Disc(Discard/Timeout);
    end

    PM1_Sig --> Wait;
    PMN_Sig --> Wait;
    PM1_Disc --> Wait;
    PMN_Disc --> Wait;

    subgraph Check for New Session Trigger
        Wait{Wait for Salience Signals/Timeout from Managers} --> Q{Did *any* Manager signal salience?};
        Q -- Yes --> R(Message Claimed by Existing Session);
        Q -- No --> S[Proceed to New Session Initiation Logic];
    end

```

**Flow Description:**

1.  **Receive & Hydrate:** The `InteractionRouter` receives the trigger (e.g., Pub/Sub message) and hydrates it with necessary context (user ID, channel ID, message content, etc.) potentially interacting with the relevant Client Adapter.
2.  **Rule-Based Filtering (Optional):** Apply quick, deterministic filters (e.g., message length, ignore bot mentions) to potentially discard trivial messages early.
3.  **Broadcast Event:** If not filtered, the `InteractionRouter` publishes the hydrated message and metadata via the `InternalEventBus`.
4.  **Manager Receives:** Each registered `PersonaManager` receives the event.
5.  **Manager-Level Salience Check:** Each `PersonaManager` iterates through the active sessions *it currently manages* for its specific Persona type. For each active session, it performs a salience check (e.g., comparing message context to the session's `EphemeralMarkdownScratchpad`). This check might involve simple heuristics or potentially a lightweight LLM call comparing the new message to the scratchpad summary/history.
6.  **Signal Salience & Process:** If a `PersonaManager` determines the message is salient for one of its sessions:
    *   It signals back to the `InteractionRouter` (or a coordinator) that the message has been claimed (potentially including the Session ID).
    *   It updates the corresponding `EphemeralMarkdownScratchpad`.
    *   It triggers the actual `IPersona` processing logic for that session context.
    *   **(Assumption):** A single message is typically claimed by at most one session across all managers. Logic might be needed to handle rare edge cases if multiple sessions *could* claim it.
7.  **Timeout/Discard (If Not Salient):** If a `PersonaManager` checks all its active sessions and finds no salient match, it signals back "not claimed" or simply times out.
8.  **New Session Decision Point:** The `InteractionRouter` waits for a defined period or until all `PersonaManagers` have responded/timed out.
    *   If **any** `PersonaManager` signaled salience (Step 6), the message is considered handled by an existing session.
    *   If **no** `PersonaManager` signaled salience, the `InteractionRouter` proceeds to the logic defined in [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md) to attempt initiation of a new session (which likely involves broadcasting again to the managers).

## 4. Ephemeral Markdown Scratchpad Structure

This temporary file acts as the short-term memory for an active session. Its structure remains the same as previously defined:

```markdown
---
SessionId: unique-session-guid
UserId: user-platform-id
ConversationId: platform-convo-id
PersonaTypeId: persona-type-id # e.g., Bootstrapper_v1
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
(Populated when a message is deemed salient by this session's manager)
The full text of the message being processed.

## Persona Draft / Analysis State
(The Persona reads from and writes to this section during processing)

## Retrieved Knowledge Snippets
(Populated by RAG process during Persona execution)
- Snippet 1 Source: [doc_id]
- Snippet 1 Content: ...
```

## 5. Handling Corrections & Updates

Managing subsequent messages that modify a request already in progress:

1.  **Task Tracking:** When a Persona begins processing, the `CurrentProcessingTaskId` in the relevant scratchpad is set.
2.  **Signal on Update:** If a *new* message (e.g., correction) arrives and is deemed salient by a `PersonaManager` for one of its active sessions:
    *   The `PersonaManager` checks the `CurrentProcessingTaskId` in that session's scratchpad.
    *   If a task is active, the `PersonaManager` must:
        *   Update the scratchpad content with the correction.
        *   **Signal** the running task (identified by `CurrentProcessingTaskId`) to **abort or restart**. (Requires `CancellationToken` or similar mechanism passed to the Persona task).
        *   Initiate a *new* processing task using the updated scratchpad.

## 6. LLM Caching Considerations (e.g., Gemini)

(Content unchanged - Caching benefits are primarily during main Persona processing, less so for the manager-level salience check unless the check itself uses an LLM heavily).

## 7. Trade-offs

*   **Pros:** Single hydration, highly decoupled salience logic (each manager only knows its own sessions), aligns with ephemeral principles, event-driven scalability, removes need for complex central session registry synchronization.
*   **Cons:** Increased application complexity (multiple managers, event bus), potential overhead of broadcasting to all managers (though likely negligible), requires robust mechanism for managers to register/discover themselves with the `InteractionRouter`. Coordination for claiming messages (ensuring only one session processes it) needs careful implementation.
