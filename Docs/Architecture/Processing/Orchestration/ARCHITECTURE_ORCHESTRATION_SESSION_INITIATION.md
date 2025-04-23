---
title: Architecture - Orchestration Session Initiation (v1.3)
description: Details the logic for Persona Managers determining when to initiate a new Persona Interaction Context (session) based on platform context, and preventing duplicate creation via Cosmos DB atomic operations.
version: 1.3
date: 2025-04-22
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Nucleus OmniRAG: Orchestration Session Initiation

## 1. Introduction

This document outlines the process for initiating a *new* [`PersonaInteractionContext`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Interfaces/IPersonaInteractionContext.cs:0:0-0:0) (session) when an incoming message is not claimed by any existing active session. This responsibility is **decentralized to the individual [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) components**, coordinated by the central `InteractionRouter`, **considers the incoming message's platform context**, and utilizes Cosmos DB atomic operations to ensure consistency and prevent duplicate session creation.

This process follows the initial message hydration and the check for salience against *existing* sessions described in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md), and is part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

Key Goals:
*   **Persona-Specific Initiation:** Allow each [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) to decide if a message is appropriate for the Persona type it manages.
*   **Context-Sensitivity:** Avoid initiating new sessions if a message is just a simple follow-up claimed by an existing session.
*   **Efficiency:** Avoid unnecessary broad intent classification; managers evaluate based on their specific focus.
*   **Consistency:** Prevent multiple managers or instances from creating duplicate sessions for the same trigger using database atomicity.

## 2. Initial Salience Check (Existing Sessions)

As detailed in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md):

1.  The `InteractionRouter` hydrates the incoming message (`PlatformType`, identifiers, etc.).
2.  The `InteractionRouter` may resolve the canonical Persona ID using [`IPersonaResolver`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs:0:0-0:0).
3.  Optional rule-based pre-filtering occurs.
4.  The `InteractionRouter` requests salience checks from relevant [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0)s, providing the hydrated message and platform context.
5.  Each [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) checks the message's salience against the **active sessions it currently manages** (considering platform context and canonical ID).
6.  If any [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) claims the message for an existing session, that manager handles the processing, and the new session initiation flow described below is **skipped**.

## 3. Decision Point for New Session Creation

The `InteractionRouter` proceeds to initiate the *new session creation phase* only if the following condition is met:

1.  **No Existing Session Claim:** After a reasonable timeout following the initial broadcast (Step 3 above), *no* [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) has signaled that it claimed the message as salient for one of its existing sessions.

## 4. Preventing Duplicate Creation (Manager-Led Cosmos DB Conditional Create)

If no existing session claims the message, the `InteractionRouter` coordinates the attempt to create a new one:

```mermaid
graph TD
    A[InteractionRouter Receives Msg + PlatformCtx] --> Resolve[Invoke IPersonaResolver (If not done earlier)]
    Resolve --> B[Check Salience vs Existing Sessions (Routing Flow)]
    B -- Message Claimed by Existing Session --> D[Route to Claiming Manager or Session]
    B -- No Salience Claim --> E[Broadcast to Managers: Evaluate for New Session (Msg, PlatformCtx, CanonicalId?)]
    subgraph PersonaManager 1 (e.g., Professional)
        E --> PM1_Eval[Receive Eval Request]
        PM1_Eval --> PM1_Decide[Decide: Should I Handle New Session? (Based on Msg, PlatformCtx, Activation Rules)]
        PM1_Decide -- Yes --> PM1_Cosmos[Attempt Cosmos DB Placeholder Create (Use Platform-Specific Key)]
        PM1_Decide -- No --> PM1_Abort[Abort Initiation]
        PM1_Cosmos -- Success --> PM1_Create[Create Scratchpad (Incl PlatformCtx), Register Session]
        PM1_Create --> PM1_Trig[Trigger Initial Persona Processing]
        PM1_Cosmos -- Conflict --> PM1_Abort
    end
    subgraph PersonaManager N (e.g., Educator)
        E --> PMN_Eval[Receive Eval Request]
        PMN_Eval --> PMN_Decide[Decide: Should I Handle New Session? (Based on Msg, PlatformCtx, Activation Rules)]
        PMN_Decide -- Yes --> PMN_Cosmos[Attempt Cosmos DB Placeholder Create (Use Platform-Specific Key)]
        PMN_Decide -- No --> PMN_Abort[Abort Initiation]
        PMN_Cosmos -- Success --> PMN_Create[Create Scratchpad (Incl PlatformCtx), Register Session]
        PMN_Create --> PMN_Trig[Trigger Initial Persona Processing]
        PMN_Cosmos -- Conflict --> PMN_Abort
    end
    D --> Z[End Flow]
    PM1_Trig --> Z
    PMN_Trig --> Z
    PM1_Abort --> Z
    PMN_Abort --> Z
```

**Detailed Steps:**

1.  **Resolve Persona (If Needed):** If not done during the routing phase, the `InteractionRouter` invokes [`IPersonaResolver`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs:0:0-0:0) to get the canonical Persona ID based on the incoming platform identifiers.
2.  **Initiation Broadcast:** The `InteractionRouter` broadcasts a request (containing the hydrated message, `PlatformType`, platform IDs, and potentially the canonical Persona ID) for [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0)s to evaluate it for *new session initiation*.
3.  **Manager Evaluation:** Each [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) receives the request and applies its logic to determine if the message content, **platform context (`PlatformType`, user ID), resolved canonical ID (if applicable)**, and its own activation rules warrant creating a new session for its Persona type.
4.  **Attempt Atomic Claim (by Manager):** If a [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) decides "Yes":
    *   **Derive Unique Placeholder ID:** Generate the deterministic, unique document ID incorporating the platform context (e.g., `placeholder:msg:{PlatformType}:{channel_id}:{message_id}`). This ensures uniqueness across platforms for the same message trigger.
    *   **Attempt Cosmos DB Conditional Create:** The [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) executes `CreateItemAsync` to create the placeholder document with the derived, platform-specific ID.
5.  **Create Succeeded (e.g., HTTP 201 - Claimed by this Manager):**
    *   This [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) has claimed responsibility.
    *   **Create Session Resources:**
        *   Generate a new unique `SessionId`.
        *   Create the `EphemeralMarkdownScratchpad`, **populating initial metadata including `PlatformType`, platform IDs, and resolved `CanonicalPersonaId`**. 
        *   Register the new `SessionId` and its metadata/pointers within its own internal state.
    *   **Trigger Initial Processing:** Initiate the first processing step by invoking the relevant [`IPersona`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/IPersona.cs:0:0-0:0) implementation, passing the new context.
6.  **Create Failed (e.g., HTTP 409 Conflict - Claimed by Another):**
    *   Another manager claimed the placeholder first.
    *   This [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) aborts its new session creation process.

## 5. Placeholder Document

The placeholder document ID now includes the platform type for uniqueness:

```json
{
  "id": "placeholder:msg:Teams:channel123:messageABC", // Platform specific ID
  "pk": "placeholder:msg:Teams:channel123", // Example partition key
  "messageTimestamp": "2025-04-13T20:55:33Z",
  "canonicalPersonaId": "professional-colleague", // Resolved ID if available
  "platformUserId": "teams-user-guid", // Platform specific ID
  "status": "claimed", 
  "claimingInstanceId": "aca-instance-xyz", 
  "initiatingPersonaTypeId": "Professional_v1", 
  "createdSessionId": null,
  "ttl": 3600
}
```

## 6. Failure Handling

*   **Cosmos DB Availability:** The [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0) attempting the create should handle transient errors with appropriate retry logic.
*   **Resource Creation Failure (Post-Claim):** If the *claiming [`PersonaManager`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Orchestration/IPersonaManager.cs:0:0-0:0)* fails during scratchpad creation or internal registration *after* successfully creating the placeholder, this is an internal error within that manager. Ideally, it should attempt cleanup (delete placeholder) or rely on monitoring/alerting. The placeholder prevents others from retrying.

## 7. Trade-offs

*   **Pros:** Decentralizes initiation logic; **incorporates platform context into decisions**; avoids central bottleneck; maintains atomicity via DB.
*   **Cons:** Requires second broadcast/request round; multiple managers evaluate; relies on managers implementing DB check correctly; slightly more complex coordination.
