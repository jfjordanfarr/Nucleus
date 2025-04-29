---
title: Architecture - Orchestration Session Initiation (API-First)
description: Details how the Nucleus API Service initiates interaction processing context after a successful Activation Check.
version: 2.0
date: 2025-04-27
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Orchestration Session Initiation (API-First)

## 1. Introduction

Under the API-First architecture, the concept of "session initiation" is significantly simplified and centralized within the `Nucleus.Services.Api`.

This document outlines the process that occurs *after* an incoming `InteractionRequest` (received via `POST /interactions`) has successfully passed the **Activation Check** performed by the API Service, as detailed in [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md).

There is no longer a decentralized broadcast or complex claim mechanism involving multiple `PersonaManager` components competing for initiation.

## 2. Context Creation Post-Activation

Once the `ApiService` determines that an incoming `InteractionRequest` should be processed (i.e., it passes the Activation Check):

1.  **Unique Interaction ID:** The `ApiService` (likely via its internal `OrchestrationService`) generates a unique `InteractionId` (e.g., a GUID) to track this specific processing instance.
2.  **Context Object Creation:** A central context object (e.g., `InteractionContext`) is created. This object encapsulates:
    *   The generated `InteractionId`.
    *   The details from the original `InteractionRequest` (User ID, Content, Platform Context, Source Artifacts, etc.).
    *   The resolved Persona ID (if determined during Activation Check or resolved subsequently).
    *   Any other relevant metadata needed for processing.
3.  **(Optional) Ephemeral Storage:** If the interaction requires temporary storage (e.g., for intermediate LLM reasoning steps if `ShowYourWork` is enabled), the `ApiService` is responsible for creating the necessary storage container (e.g., an Ephemeral Markdown Scratchpad in a designated location, potentially linked via the `InteractionContext`).
4.  **Hand-off to Handler:** The `ApiService` then passes this fully populated `InteractionContext` to the appropriate handler based on whether the task is synchronous or asynchronous:
    *   **Synchronous:** Passed directly to the responsible synchronous processing logic.
    *   **Asynchronous:** Packaged and placed onto the task queue (e.g., Azure Service Bus) for a background worker to pick up. The `InteractionContext` data is included in the queue message.

## 3. Key Changes from Previous Model

*   **Centralized:** Initiation is solely the responsibility of the `ApiService` after activation.
*   **Simplified:** No broadcast, salience checks for *initiation*, or complex distributed locking (like the Cosmos DB placeholder) is required.
*   **Implicit:** Session initiation is an implicit step within the API's processing flow following successful activation.

## 4. Related Documents

*   [API Architecture Overview](../10_ARCHITECTURE_API.md)
*   [API Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)

## 5. Related Components and Documents

- **Core Implementation:**
  - [`OrchestrationService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs): Central service managing the interaction lifecycle, including context setup (though currently divergent from this document's description).
  - [`OrchestrationService.ProcessInteractionAsync`](cci:1://file:///d:/Projects/Nucleus/src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs:59:4-167:5): The primary method handling incoming requests.
- **Key Data Structures:**
  - [`InteractionContext`](../../../../src/Nucleus.Abstractions/Orchestration/InteractionContext.cs): Represents the context object ideally created post-activation.
  - [`NucleusIngestionRequest`](../../../../src/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs): Used for queueing asynchronous processing tasks.
- **Related Orchestration Concepts:**
  - [Overall Orchestration](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
  - [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
  - [Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)
