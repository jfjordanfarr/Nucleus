---
title: Architecture - Orchestration Session Initiation
description: Details the logic for determining when to initiate a new Persona Interaction Context (session) using contextual salience and preventing duplicate creation via Cosmos DB atomic operations.
version: 1.1
date: 2025-04-13
---

# Nucleus OmniRAG: Orchestration Session Initiation

**Version:** 1.1
**Date:** 2025-04-13

## 1. Introduction

This document outlines the critical process within the `InteractionDispatcher` for deciding whether an incoming message warrants the creation of a *new* `PersonaInteractionContext` (session). It relies on salience checks performed by *existing* sessions and uses Cosmos DB atomic operations to prevent duplicate session creation when multiple application instances might process the same trigger.

This process follows the initial message hydration and routing described in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md) and is part of the overall [Processing Orchestration Overview](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md).

Key Goals:
*   **Accuracy:** Initiate new sessions primarily based on lack of relevance to existing contexts.
*   **Context-Sensitivity:** Allow existing sessions to correctly claim follow-up messages (e.g., simple "yes"/"no" replies) based on their state.
*   **Efficiency:** Avoid unnecessary upfront LLM calls for broad intent classification.
*   **Consistency:** Prevent multiple instances from creating duplicate sessions for the same underlying trigger using database atomicity.

## 2. Filtering and Broadcast for Salience

To avoid unnecessary processing, the `InteractionDispatcher` employs initial filtering and then relies on existing sessions:

1.  **Rule-Based Pre-filtering (Optional):** Apply quick, computationally cheap checks after message hydration:
    *   Is the message extremely short?
    *   Does it match a configurable list of common non-actionable phrases (e.g., "thanks", "ok", "good night")?
    *   Does metadata indicate it's from a bot or system process known to be irrelevant?
    *   If filtered here, the message processing (for *new* session creation) stops. It might still be relevant to *existing* sessions.

2.  **Broadcast for Existing Session Salience:** If not filtered, the hydrated message is broadcast internally via the `InternalEventBus` as described in [Routing & Salience](./ARCHITECTURE_ORCHESTRATION_ROUTING.md). Existing active sessions evaluate the message against their current context (scratchpad) to determine if it's salient to them.
    *   Sessions that deem the message salient will signal this back (mechanism TBD - could be via the event bus or updating a shared marker) and proceed to update their scratchpad and trigger their own processing.

## 3. Decision Point for New Session Creation

The `InteractionDispatcher` instance handling the message proceeds to consider creating a *new* session only if the following condition is met:

1.  **No Existing Session Claim:** After a reasonable timeout following the internal broadcast, *no* existing session has signaled that it claimed the message as salient to its context.

If this condition is true, the dispatcher proceeds to the atomic creation attempt using Cosmos DB.

## 4. Preventing Duplicate Creation (Cosmos DB Conditional Create)

To ensure only one instance successfully creates the new session, even if multiple instances reach the decision point concurrently for the same trigger, a conditional create operation in Cosmos DB is used:

```mermaid
graph TD
    A[Dispatcher Receives & Hydrates Message] --> B{Rule-Based Filtering};
    B -- Filtered --> X(Discard - No Action for New Session);
    B -- Not Filtered --> E[Broadcast Hydrated Message to Active Sessions (Internal Event Bus)];
    E --> F{Salience Check by Active Sessions (Wait/Timeout)};
    subgraph New Session Decision Point
        direction LR
        G{Did *any* existing session claim salience?} --> H{Attempt New Session Creation (via Cosmos DB)};
        G -- Yes --> I(Route to existing salient session(s) - No New Session);
    end
    F --> G; % Connect salience check result to decision point
    H --> J{Derive Unique Placeholder ID (from Message Context)};
    J --> K[Attempt Cosmos DB CreateItemAsync(placeholder ID)];
    K -- Success (201 Created) --> L{Claim Success: Create Scratchpad & Register Session};
    K -- Conflict (409 Conflict) --> M(Claim Failed: Another instance succeeded - Abort);
    L --> N[Trigger Initial Persona Processing];
```

**Detailed Steps:**

1.  **Derive Unique Placeholder ID:** Generate a deterministic, unique document ID based on the incoming message context. This ID must be identical across all instances processing the same original message. Examples:
    *   `placeholder:msg:<platform>:<channel_id>:<message_id>`
    *   `placeholder:msg:<platform>:<user_id>:<conversation_id>:<message_timestamp_hash>`

2.  **Attempt Cosmos DB Conditional Create:** Execute `CreateItemAsync` (or equivalent SDK method) in the designated Cosmos DB container (e.g., `SessionPlaceholders` or `ArtifactMetadata`) attempting to create a minimal placeholder document with the derived ID. **Crucially, this operation inherently fails if a document with that ID already exists.**

3.  **Create Succeeded (e.g., HTTP 201):**
    *   This instance has successfully claimed responsibility for the new session.
    *   **Create Session Resources:**
        *   Generate a new unique `SessionId`.
        *   Create the `EphemeralMarkdownScratchpad` file.
        *   Add the new `SessionId` and its metadata/pointers to the in-memory `SessionRegistry`.
    *   **Trigger Initial Processing:** Initiate the first processing step for the new session.
    *   **(Optional):** Update the placeholder document with the actual `SessionId` or processing status.

4.  **Create Failed (e.g., HTTP 409 Conflict):**
    *   Another instance successfully created the placeholder first.
    *   This instance aborts the new session creation process.

## 5. Placeholder Document

The placeholder document itself can be very simple, primarily serving as the lock/existence check:

```json
{
  "id": "placeholder:msg:teams:channel123:messageABC",
  "pk": "placeholder:msg:teams:channel123", // Example partition key
  "messageTimestamp": "2025-04-13T20:55:33Z",
  "status": "claimed", // Could be updated later
  "claimingInstanceId": "aca-instance-xyz",
  "createdSessionId": null, // Could be updated later
  "ttl": 3600 // Optional: Auto-delete after 1 hour
}
```

## 6. Failure Handling

*   **Cosmos DB Availability:** Handle transient errors during the `CreateItemAsync` call with appropriate retry logic.
*   **Resource Creation Failure (Post-Claim):** If scratchpad/registry steps fail *after* successfully creating the placeholder, ideally the placeholder should be deleted or marked as failed to allow another instance to potentially retry (though this adds complexity). Simpler might be to rely on monitoring/alerting for such failures.
