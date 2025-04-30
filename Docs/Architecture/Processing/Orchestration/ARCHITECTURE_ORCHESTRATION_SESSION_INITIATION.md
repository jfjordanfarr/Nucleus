---
title: Architecture - Orchestration Session Initiation (API-First)
description: Details how the Nucleus API Service initiates interaction processing context via the OrchestrationService after a successful Activation Check.
version: 2.1
date: 2025-04-30
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Orchestration Session Initiation (API-First)

## 1. Introduction

Under the API-First architecture, the concept of "session initiation" is significantly simplified and centralized within the `Nucleus.Services.Api`'s `OrchestrationService`.

This document outlines the process that occurs *after* an incoming `InteractionRequest` (received via `POST /interactions`) has successfully passed the **Activation Check** performed by the `OrchestrationService`, as detailed in [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md).

There is no longer a decentralized broadcast or complex claim mechanism involving multiple components competing for initiation.

## 2. Context Creation Post-Activation

Once the `OrchestrationService` determines that an incoming `InteractionRequest` should be processed (i.e., it passes the Activation Check):

1.  **Unique Interaction ID:** The `OrchestrationService` generates a unique `InteractionId` (e.g., a GUID) to track this specific processing instance.
2.  **Context Object Creation:** A central context object (`InteractionContext`) is created. This object encapsulates:
    *   The generated `InteractionId`.
    *   The details from the original `InteractionRequest` (User ID, Content, Platform Context, Source `ArtifactReference`s, etc.).
    *   The resolved Persona ID (determined during Activation Check or via `IPersonaResolver`).
    *   Placeholders for fetched/extracted content (`ExtractedContents`).
    *   Any other relevant metadata needed for processing.
3.  **Persona Configuration Loading:** The `OrchestrationService` uses `IPersonaConfigurationProvider` to load the `PersonaConfiguration` for the resolved Persona ID.
4.  **Ephemeral Content Fetching & Context Population:** The `OrchestrationService` proceeds with context ranking and selective ephemeral fetching using `IArtifactProvider` (as described in the [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)), populating the `InteractionContext.ExtractedContents`.
5.  **(Optional) Ephemeral Storage:** If intermediate reasoning needs to be persisted (e.g., `ShowYourWork`), the `IAgenticStrategyHandler` (invoked later by `IPersonaRuntime`) is responsible for requesting its persistence via `IArtifactProvider.SaveAsync`.
6.  **Hand-off to Handler/Runtime:** The `OrchestrationService` then passes the fully populated `InteractionContext` and the loaded `PersonaConfiguration` to the appropriate handler:
    *   **Synchronous:** Directly calls `IPersonaRuntime.ExecuteAsync`.
    *   **Asynchronous:** Packages necessary data (`NucleusIngestionRequest` containing request details and references) and places it onto the task queue (e.g., Azure Service Bus) for a background worker. The worker will repeat steps 1-4 before calling `IPersonaRuntime.ExecuteAsync`.

## 3. Key Changes from Previous Model

*   **Centralized:** Context creation and initiation logic resides within the `OrchestrationService`.
*   **Simplified:** No broadcast, salience checks for *initiation*, or complex distributed locking required.
*   **Implicit:** Context creation is an implicit step within the `OrchestrationService`'s processing flow following successful activation.

## 4. Related Documents

*   [API Architecture Overview](../10_ARCHITECTURE_API.md)
*   [API Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)

## 5. Related Components and Documents

- **Core Implementation:**
  - [`OrchestrationService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs): Central service managing the interaction lifecycle, including context setup.
  - [`OrchestrationService.ProcessInteractionAsync`](cci:1://file:///d:/Projects/Nucleus/src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs:59:4-167:5): The primary method handling incoming requests.
- **Key Data Structures:**
  - [`InteractionContext`](../../../../src/Nucleus.Abstractions/Orchestration/InteractionContext.cs): Represents the context object created post-activation.
  - [`NucleusIngestionRequest`](../../../../src/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs): Used for queueing asynchronous processing tasks.
- **Core Runtime:**
  - [`IPersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs): Interface for the central runtime.
  - [`PersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs): Implementation of the runtime.
- **Related Orchestration Concepts:**
  - [Overall Orchestration](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
  - [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
  - [Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)
