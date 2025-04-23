---
title: Architecture - Orchestration Routing & Salience (v1.2)
description: Details the mechanism for routing incoming messages to relevant active Persona sessions via Persona Managers, considering platform context and multi-platform concepts.
version: 1.2
date: 2025-04-22
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus OmniRAG: Orchestration Routing & Salience

## 1. Introduction

This document details how an incoming user message, **identified by its source `PlatformType` and platform-specific identifiers**, is assessed for **salience** against potentially relevant, active Persona interaction contexts ("sessions") managed within the Nucleus system. It focuses on routing the message to an *existing* session if appropriate.

This process relies on **decentralized session management**, where dedicated [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) components handle the state and salience checks for the specific Persona types they are responsible for.

This builds upon the [Interaction Processing Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md) and is a key part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

The primary goals are:
*   **Efficient Hydration:** Fetch message content from the source platform only once.
*   **Accurate Routing:** Deliver the message only to the [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) (and subsequently, the specific [`PersonaInteractionContext`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Interfaces/IPersonaInteractionContext.cs:0:0-0:0)) where it is contextually relevant.
*   **Scalability:** Support potentially many concurrent active sessions distributed across [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0)s.
*   **Context Management:** Maintain necessary session state ephemerally within each session's scratchpad.

**The process for determining if a message should initiate a *new* session (if no existing session claims it) is detailed in [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md).**

## 2. Core Components

*   **`InteractionRouter`:** A central orchestration service (or logic block) triggered by incoming messages (e.g., from [`NucleusIngestionRequest`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs:0:0-0:0)). Responsible for hydrating the message (**including `PlatformType`, identifiers, `ContentSourceUri`**), potentially resolving the canonical Persona ID via [`IPersonaResolver`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs:0:0-0:0), and coordinating salience checks.
*   **[`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) (Multiple Instances):**
    *   A component responsible for managing the lifecycle and interactions for **a specific type of Persona across all its supported platforms**.
    *   Each `PersonaManager` maintains the state of *its own* active [`PersonaInteractionContext`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Interfaces/IPersonaInteractionContext.cs:0:0-0:0) sessions (e.g., in an in-memory collection mapped by `SessionId`, potentially referencing `PlatformType` and canonical Persona ID associated with the session).
    *   It listens for requests from the `InteractionRouter` containing the hydrated message **and platform context**.
    *   It performs the salience check for incoming messages against the active sessions **associated with the relevant canonical Persona ID (if resolved) and potentially matching the `PlatformType`**.
*   **[`IPersonaResolver`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs:0:0-0:0) (Service):** Used by the `InteractionRouter` to map incoming platform-specific user/conversation identifiers to a canonical Nucleus Persona ID, based on stored Persona Profiles.
*   **`EphemeralMarkdownScratchpad`:** A temporary file (or in-memory structure) per active session holding its conversation history, current state, draft responses, and metadata (**including `PlatformType`, platform IDs, and canonical Persona ID**). Managed indirectly via the [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) and [`PersonaInteractionContext`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Interfaces/IPersonaInteractionContext.cs:0:0-0:0).
*   **`InternalEventBus` / Request-Response:** Mechanism for the `InteractionRouter` to communicate with [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) instances (broadcast or targeted requests).

## 3. Proposed Routing Flow (Salience Check for Existing Sessions)

```mermaid
graph TD
    A[InteractionRouter Receives NucleusIngestionRequest] --> Hydrate[Hydrate Context (PlatformType, IDs, ContentUri)]
    Hydrate --> Resolve[Invoke IPersonaResolver (Optional Early)]
    Resolve --> B[Rule-Based Filtering]
    B -- Filtered --> X[Discard]
    B -- Not Filtered --> E[Request Salience Check from Relevant PersonaManagers]
    subgraph PersonaManager 1 (e.g., Professional)
      E --> PM1_Rcv[Receive Request (Msg, PlatformCtx, CanonicalId?)]
      PM1_Rcv --> PM1_Chk[Check Salience vs Own Active Sessions (Match CanonicalId? Match Platform?)]
      PM1_Chk -- Salient --> PM1_Sig[Signal Salience, Update Scratchpad]
      PM1_Sig --> PM1_Trig[Trigger Persona Processing]
      PM1_Chk -- Not Salient --> PM1_Disc[Signal Not Claimed]
    end
    subgraph PersonaManager N (e.g., Educator)
        E --> PMN_Rcv[Receive Request (Msg, PlatformCtx, CanonicalId?)]
        PMN_Rcv --> PMN_Chk[Check Salience vs Own Active Sessions (Match CanonicalId? Match Platform?)]
        PMN_Chk -- Salient --> PMN_Sig[Signal Salience, Update Scratchpad]
        PMN_Sig --> PMN_Trig[Trigger Persona Processing]
        PMN_Chk -- Not Salient --> PMN_Disc[Signal Not Claimed]
    end
    PM1_Sig --> Wait[Gather Responses]
    PMN_Sig --> Wait
    PM1_Disc --> Wait
    PMN_Disc --> Wait
    Wait --> Q[Did any Manager signal salience?]
    Q -- Yes --> Existing[Route to Claiming Manager/Session]
    Q -- No --> NewSession[Proceed to New Session Initiation Flow]
```

**Flow Description:**

1.  **Receive & Hydrate:** The `InteractionRouter` receives the [`NucleusIngestionRequest`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs:0:0-0:0) (containing `PlatformType`, identifiers, `ContentSourceUri`).
2.  **Resolve Persona (Optional Early):** The `InteractionRouter` *may* invoke the [`IPersonaResolver`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs:0:0-0:0) immediately to get the canonical Persona ID. This can help target the salience check request more effectively (e.g., only query the [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) for the resolved Persona).
3.  **Rule-Based Filtering (Optional):** Apply quick, deterministic filters.
4.  **Request Salience Check:** The `InteractionRouter` sends a request (e.g., via `InternalEventBus` or direct calls) to relevant [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) instances. This request includes the message content, `PlatformType`, platform identifiers, and potentially the resolved canonical Persona ID.
5.  **Manager Salience Check:** Each notified [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) checks the incoming message against its active sessions:
    *   It filters sessions based on the canonical Persona ID (if provided/resolved).
    *   It applies its salience logic (LLM check, keyword match, etc.) against the filtered sessions, considering the message content and conversation history (from the session's scratchpad).
    *   The salience check might also consider if the incoming `PlatformType` matches the session's original platform, although a Persona session might span multiple platforms conceptually.
6.  **Signal Salience (If Matched):** If a [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) finds a salient session:
    *   It signals back to the `InteractionRouter` that the message has been claimed (including the `SessionId`).
    *   It updates the corresponding `EphemeralMarkdownScratchpad` **with the new message and potentially updated metadata (e.g., `LastUpdateTime`, `CurrentPlatformContext` = incoming `PlatformType`)**.
    *   It triggers the actual [`IPersona`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/IPersona.cs:0:0-0:0) processing logic for that session context (as detailed in [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)).
    *   **(Assumption):** A single message is typically claimed by at most one session. Conflict resolution might be needed if multiple claim.
7.  **Signal Not Claimed:** If a [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) checks all its relevant active sessions and finds no salient match, it signals back "not claimed".
8.  **New Session Decision Point:** The `InteractionRouter` waits for responses.
    *   If **any** [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) signaled salience, the message is routed to that existing session.
    *   If **no** [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) signaled salience, proceed to [Session Initiation](./ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md).

## 4. Ephemeral Markdown Scratchpad Structure

This temporary file acts as the short-term memory for an active session. Its structure remains the same as previously defined:

```markdown
---
SessionId: unique-session-guid
CanonicalPersonaId: professional-colleague # Resolved Persona ID
UserId: "platform-specific-user-id" # Platform ID of the user who started session
PlatformUserIdMap: # Map of user's ID across platforms involved in this session
  teams: "user-id-on-teams"
  email: "user@example.com"
ConversationId: "platform-specific-convo-id"
OriginPlatformType: "Teams" # Platform where session originated
CurrentPlatformContext: "Email" # Platform of the *last* message processed
PersonaTypeId: Professional_v1 # Concrete implementation type
CurrentProcessingTaskId: task-guid-for-last-message # Tracks ongoing work
LastUpdateTime: timestamp
---
```

## 5. Handling Corrections & Updates

Managing subsequent messages that modify a request already in progress:

1.  **Task Tracking:** When a Persona begins processing, the `CurrentProcessingTaskId` in the relevant scratchpad is set.
2.  **Signal on Update:** If a *new* message (e.g., correction) arrives and is deemed salient by a [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) for one of its active sessions:
    *   The [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) checks the `CurrentProcessingTaskId` in that session's scratchpad.
    *   If a task is active, the [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) must:
        *   Update the scratchpad content with the correction.
        *   **Signal** the running task (identified by `CurrentProcessingTaskId`) to **abort or restart**. (Requires `CancellationToken` or similar mechanism passed to the Persona task).
        *   Initiate a *new* processing task using the updated scratchpad.

## 6. LLM Caching Considerations (e.g., Gemini)

(Content unchanged - Caching benefits are primarily during main Persona processing, less so for the manager-level salience check unless the check itself uses an LLM heavily).

## 7. Trade-offs

*   **Pros:** Single hydration, highly decoupled salience logic, aligns with ephemeral principles, event-driven/request-response scalability, **incorporates platform context and canonical identity**.
*   **Cons:** Increased complexity (managers, resolver, platform context handling), potential overhead if resolver is slow or broadcasting is inefficient, requires robust manager registration/discovery. Coordination for claiming messages needs careful implementation.
