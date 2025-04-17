---
title: Architecture - Orchestration Session Initiation (v1.2)
description: Details the logic for Persona Managers determining when to initiate a new Persona Interaction Context (session) and preventing duplicate creation via Cosmos DB atomic operations.
version: 1.2
date: 2025-04-16

---

# Nucleus OmniRAG: Orchestration Session Initiation

**Version:** 1.2
**Date:** 2025-04-16

## 1. Introduction

This document outlines the process for initiating a *new* `PersonaInteractionContext` (session) when an incoming message is not claimed by any existing active session. This responsibility is **decentralized to the individual `PersonaManager` components**, coordinated by the central `InteractionRouter`, and utilizes Cosmos DB atomic operations to ensure consistency and prevent duplicate session creation.

This process follows the initial message hydration and the check for salience against *existing* sessions described in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md), and is part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

Key Goals:
*   **Persona-Specific Initiation:** Allow each `PersonaManager` to decide if a message is appropriate for the Persona type it manages.
*   **Context-Sensitivity:** Avoid initiating new sessions if a message is just a simple follow-up claimed by an existing session.
*   **Efficiency:** Avoid unnecessary broad intent classification; managers evaluate based on their specific focus.
*   **Consistency:** Prevent multiple managers or instances from creating duplicate sessions for the same trigger using database atomicity.

## 2. Initial Salience Check (Existing Sessions)

As detailed in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md):

1.  The `InteractionRouter` hydrates the incoming message.
2.  Optional rule-based pre-filtering occurs.
3.  The `InteractionRouter` broadcasts the hydrated message via the `InternalEventBus` to all registered `PersonaManagers`.
4.  Each `PersonaManager` checks the message's salience against the **active sessions it currently manages**.
5.  If any `PersonaManager` claims the message for an existing session, that manager handles the processing, and the new session initiation flow described below is **skipped** for this message.

## 3. Decision Point for New Session Creation

The `InteractionRouter` proceeds to initiate the *new session creation phase* only if the following condition is met:

1.  **No Existing Session Claim:** After a reasonable timeout following the initial broadcast (Step 3 above), *no* `PersonaManager` has signaled that it claimed the message as salient for one of its existing sessions.

## 4. Preventing Duplicate Creation (Manager-Led Cosmos DB Conditional Create)

If no existing session claims the message, the `InteractionRouter` coordinates the attempt to create a new one:

```mermaid
graph TD
    A[InteractionRouter Receives & Hydrates Message] --> B(Broadcast to Managers: Check Salience vs EXISTING Sessions);
    B --> C{Wait for Salience Signals/Timeout};
    C -- Message Claimed by Existing Session --> D(Route to Claiming Manager/Session);
    C -- No Salience Claim --> E{InteractionRouter: Broadcast to Managers: EVALUATE FOR NEW Session};

    subgraph Persona Manager 1 (e.g., Bootstrapper)
        E --> PM1_Eval(Receive Eval Request);
        PM1_Eval --> PM1_Decide{Decide: Should Bootstrapper Handle NEW Session?};
        PM1_Decide -- Yes --> PM1_Cosmos[Attempt Cosmos DB Placeholder Create];
        PM1_Decide -- No --> PM1_Abort(Abort Initiation);
        PM1_Cosmos -- Success (201 Created) --> PM1_Create[Create Scratchpad, Register Session Internally];
        PM1_Create --> PM1_Trig(Trigger Initial Persona Processing);
        PM1_Cosmos -- Conflict (409) --> PM1_Abort;
    end

    subgraph Persona Manager N (e.g., EduFlow)
        E --> PMN_Eval(Receive Eval Request);
        PMN_Eval --> PMN_Decide{Decide: Should EduFlow Handle NEW Session?};
        PMN_Decide -- Yes --> PMN_Cosmos[Attempt Cosmos DB Placeholder Create];
        PMN_Decide -- No --> PMN_Abort(Abort Initiation);
        PMN_Cosmos -- Success (201 Created) --> PMN_Create[Create Scratchpad, Register Session Internally];
        PMN_Create --> PMN_Trig(Trigger Initial Persona Processing);
        PMN_Cosmos -- Conflict (409) --> PMN_Abort;
    end

    D --> Z(End Flow);
    PM1_Trig --> Z;
    PMN_Trig --> Z;
    PM1_Abort --> Z;
    PMN_Abort --> Z;

```

**Detailed Steps:**

1.  **Initiation Broadcast:** The `InteractionRouter` broadcasts the hydrated message again via the `InternalEventBus`, this time signaling a request for `PersonaManagers` to evaluate it for *new session initiation*.
2.  **Manager Evaluation:** Each `PersonaManager` receives the request and applies its own logic to determine if the message content, user context, channel, etc., warrants creating a new session for the specific Persona type it manages. (e.g., Does it look like a request for the Bootstrapper? Does it mention keywords relevant to EduFlow?).
3.  **Attempt Atomic Claim (by Manager):** If a `PersonaManager` decides "Yes", it proceeds to attempt the atomic claim:
    *   **Derive Unique Placeholder ID:** Generate the deterministic, unique document ID based on the incoming message context (e.g., `placeholder:msg:<platform>:<channel_id>:<message_id>`). This must be identical across all managers attempting to claim the same message.
    *   **Attempt Cosmos DB Conditional Create:** The `PersonaManager` executes `CreateItemAsync` in the designated Cosmos DB container, attempting to create the placeholder document with the derived ID.
4.  **Create Succeeded (e.g., HTTP 201 - Claimed by this Manager):**
    *   This specific `PersonaManager` has successfully claimed responsibility for the new session.
    *   **Create Session Resources:**
        *   Generate a new unique `SessionId`.
        *   Create the `EphemeralMarkdownScratchpad` file/structure for the new session.
        *   Register the new `SessionId` and its metadata/pointers **within its own internal state management** (e.g., its in-memory dictionary of active sessions).
    *   **Trigger Initial Processing:** Initiate the first processing step by invoking the relevant `IPersona` implementation associated with this manager, passing the new context.
    *   **(Optional):** Update the placeholder document with the actual `SessionId` or processing status.
5.  **Create Failed (e.g., HTTP 409 Conflict - Claimed by Another):**
    *   Another `PersonaManager` (or potentially another instance of the same manager type in a scaled-out scenario) successfully created the placeholder first.
    *   This `PersonaManager` aborts its new session creation process.

## 5. Placeholder Document

The placeholder document remains simple, but adding the initiating Persona type could be useful for traceability:

```json
{
  "id": "placeholder:msg:teams:channel123:messageABC",
  "pk": "placeholder:msg:teams:channel123", // Example partition key
  "messageTimestamp": "2025-04-13T20:55:33Z",
  "status": "claimed", // Indicates a manager succeeded in creating it
  "claimingInstanceId": "aca-instance-xyz", // Instance where the successful manager ran
  "initiatingPersonaTypeId": "Bootstrapper_v1", // Which type of manager claimed it
  "createdSessionId": null, // Could be updated later by the manager
  "ttl": 3600 // Optional: Auto-delete after 1 hour
}
```

## 6. Failure Handling

*   **Cosmos DB Availability:** The `PersonaManager` attempting the create should handle transient errors with appropriate retry logic.
*   **Resource Creation Failure (Post-Claim):** If the *claiming `PersonaManager`* fails during scratchpad creation or internal registration *after* successfully creating the placeholder, this is an internal error within that manager. Ideally, it should attempt cleanup (delete placeholder) or rely on monitoring/alerting. The placeholder prevents others from retrying.

## 7. Trade-offs

*   **Pros:** Decentralizes initiation logic to the components (Managers) that understand the Personas best; avoids central bottleneck for deciding *which* Persona should handle a new interaction; maintains atomicity via DB.
*   **Cons:** Requires a second broadcast round; multiple managers might perform evaluation logic unnecessarily if one claims quickly; relies on managers implementing the Cosmos DB check correctly; slightly more complex coordination flow than a fully centralized model.
