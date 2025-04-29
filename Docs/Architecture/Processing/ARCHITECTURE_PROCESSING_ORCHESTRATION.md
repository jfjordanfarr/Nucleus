---
title: Architecture - Processing Orchestration Overview
description: Describes the high-level concepts and responsibilities for orchestrating the flow of user interactions and processing tasks within Nucleus, coordinated via the API service.
version: 1.5
date: 2025-05-01
parent: ../01_ARCHITECTURE_PROCESSING.md
---

# Nucleus: Processing Orchestration Overview

## 1. Introduction

This document provides a high-level overview of the **Orchestration** sub-domain within the overall [Processing Architecture](../01_ARCHITECTURE_PROCESSING.md). Orchestration is concerned with managing the *flow* of work required to handle user interactions and related background processing tasks.

While the [Ingestion](./ARCHITECTURE_PROCESSING_INGESTION.md) components focus on transforming raw artifacts into usable representations (ephemerally), and [Personas](../02_ARCHITECTURE_PERSONAS.md) focus on analyzing content and generating responses, Orchestration bridges the gap. It coordinates the sequence of events, routes requests based on **activation rules evaluated within the API service**, and manages the execution context.

Key goals of the orchestration layer include:
*   **Reliability:** Ensuring interactions are processed correctly and consistently.
*   **Scalability:** Handling varying loads of concurrent interactions through efficient API routing and asynchronous processing.
*   **Decoupling:** Separating concerns between triggering, routing, execution, and response delivery.
*   **Observability:** Providing visibility into the flow of work.

## 2. Core Responsibilities

The Orchestration layer encompasses several key responsibilities:

*   **API Request Handling & Hydration:**
    *   Receiving interaction requests exclusively via the central API endpoint (`POST /api/v1/interactions`).
    *   Interpreting the incoming `AdapterRequest` DTO and hydrating the initial message context. This includes `PlatformType`, platform-specific identifiers, and the list of `ArtifactReference` objects pointing to user content in external storage.
    *   Potentially invoking an [`IPersonaResolver`](../../../src/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs) service early to map platform identifiers to a canonical Nucleus Persona ID, if applicable, to aid routing.
*   **API Interaction Activation & Routing:**
    *   Determining if an incoming interaction received by the API service warrants processing based on configured activation rules (e.g., mentions, scope, user) via [`IActivationChecker`](../../../src/Nucleus.Abstractions/Orchestration/IActivationChecker.cs) (implemented by [`ActivationChecker`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/ActivationChecker.cs)).
    *   If activated, routing the task to the appropriate handler: either a synchronous service for quick responses or an asynchronous task queue for background processing.
    *   This centralized API-based process replaces the previous decentralized salience check model.
    *   **Details:** See [API Activation & Routing](./Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md).
*   **Interaction Context Creation (Session Initiation):**
    *   *Following successful API activation and routing*, the API service (specifically its internal orchestration logic) initiates the processing context.
    *   This involves creating the `InteractionContext` object, populating it with the `InteractionId`, request details, resolved Persona ID (using [`IPersonaResolver`](../../../src/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs) if needed), and preparing for hand-off to the synchronous or asynchronous handler.
    *   **Details:** See [Session Initiation](./Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md).
*   **Interaction Lifecycle Management:**
    *   Executing the defined steps for processing an interaction within its context.
    *   **Fetching Artifact Content:** If a Persona requires the content of an artifact during processing, the orchestrator uses the details within the relevant `ArtifactReference` object from the `AdapterRequest`. It resolves the appropriate `IArtifactProvider` implementation (based on the `ReferenceType`) and calls `IArtifactProvider.GetContentAsync(artifactReference)` to ephemerally fetch the content stream directly from the user's source system. **Nucleus never accepts or stores raw artifact content directly.**
    *   **Sending Responses/Notifications:** When sending messages outwards, the orchestrator determines the target (user or persona) and the **target `PlatformType`** (usually the origin, or specified for persona-to-persona comms). It resolves the correct [`IPlatformNotifier`](../../../src/Nucleus.Abstractions/IPlatformNotifier.cs) implementation via DI using this `PlatformType` and uses the Persona Profile (or request context) to get the specific platform identifiers (e.g., Teams User ID, Email Address) needed by the notifier.
    *   Coordinating calls to other services like [`IPersona`](../../../src/Nucleus.Abstractions/IPersona.cs), [`IRepository`](../../../src/Nucleus.Abstractions/Data/IRepository.cs), [`IChatClient`](../../../src/Nucleus.Abstractions/IChatClient.cs) as needed.
    *   **Details:** See [Interaction Processing Lifecycle](./Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md).
*   **State Management:**
    *   Managing any necessary state related to the *flow* of complex or long-running operations (Note: distinct from the ephemeral interaction scratchpad managed within `PersonaInteractionContext`).
*   **Error Handling & Resilience:**
    *   Implementing strategies for handling failures during orchestration steps (e.g., retries, compensating actions, logging, alerting).

## 3. Relationship to Other Components

*   **Client Adapters:** Provide initial triggers (`AdapterRequest`) containing only `ArtifactReference` objects. Receive final responses via [`IPlatformNotifier`](../../../src/Nucleus.Abstractions/IPlatformNotifier.cs).
*   **API Endpoint (`/api/v1/interactions`):** The single entry point triggering orchestration.
*   **[`Nucleus.Services.Api`](../../../src/Nucleus.Services/Nucleus.Services.Api):** The central hub for receiving interactions, performing activation checks, and initiating synchronous or asynchronous processing flows using [`IOrchestrationService`](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) (implemented by [`OrchestrationService`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs)).
*   **Activation Rule Engine:** Logic within the API service that determines if an interaction is relevant.
*   **Internal Task Queue & Workers:** Used for handling long-running asynchronous tasks decoupled from the API request, initiated *by* the API service.
*   **[`PersonaManager`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/PersonaManager.cs) Instances:** Components potentially used to manage the *state* and *lifecycle* of active interaction sessions for specific Personas after the initial API activation and routing.
*   **Personas ([`IPersona`](../../../src/Nucleus.Abstractions/IPersona.cs)):** Execute domain-specific logic (analysis, query handling).
*   **Artifact Provider ([`IArtifactProvider`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs)):** The central interface used by the API/Orchestrator to fetch artifact content based on an `ArtifactReference`.
*   **Platform Services ([`IPlatformAttachmentFetcher`](../../../src/Nucleus.Abstractions/IPlatformAttachmentFetcher.cs), [`IPlatformNotifier`](../../../src/Nucleus.Abstractions/IPlatformNotifier.cs)):** Concrete implementations resolved by `IArtifactProvider` or the orchestrator based on `PlatformType`/`ReferenceType` for platform-specific interactions.
*   **Persona Resolver ([`IPersonaResolver`](../../../src/Nucleus.Abstractions/Orchestration/IPersonaResolver.cs)):** Service used to map between platform identities and canonical Persona IDs.
*   **Persona Profile Store:** Provides the data needed by the `IPersonaResolver` and for looking up target platform identifiers.
*   **Processing Components (Ingestion):** Orchestration invokes necessary ephemeral ingestion/processing steps.
*   **Compute Runtime:** The orchestration logic runs within the chosen compute environment.

## 4. Future Considerations

As the system matures (e.g., towards [Phase 4 Maturity Requirements](../../Requirements/04_REQUIREMENTS_PHASE4_MATURITY.md#32-workflow-orchestration)), the orchestration layer may evolve to use more sophisticated tools like dedicated workflow engines (e.g., Azure Durable Functions, Dapr Workflows) to handle more complex, stateful, or long-running processes beyond the simple background task model.