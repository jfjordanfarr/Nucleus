---
title: Architecture - Orchestration Session Initiation (API-First)
description: Details how the Nucleus API Service initiates interaction processing context (including resolved PersonaId) via the OrchestrationService after a successful Activation Check.
version: 2.2
date: 2025-05-07
parent: ../ARCHITECTURE_PROCESSING_ORCHESTRATION.md
---

# Orchestration Session Initiation (API-First)

## 1. Introduction

Under the API-First architecture, the concept of "session initiation" is significantly simplified and centralized within the `Nucleus.Services.Api`'s `OrchestrationService`. This initiation step is crucial in a multi-persona environment as it establishes the specific `PersonaId` that will govern the scope of all subsequent operations for the interaction.

This document outlines the process that occurs *after* an incoming `InteractionRequest` (received via `POST /interactions`) has successfully passed the **Activation Check** performed by the `OrchestrationService`, which includes resolving the target `PersonaId`, as detailed in [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md).

There is no longer a decentralized broadcast or complex claim mechanism involving multiple components competing for initiation.

## 2. Context Creation Post-Activation

Once the `OrchestrationService` determines that an incoming `InteractionRequest` should be processed (i.e., it passes the Activation Check and a target `PersonaId` is resolved):

1.  **Unique Interaction ID:** The `OrchestrationService` generates a unique `InteractionId` (e.g., a GUID) to track this specific processing instance.
2.  **Context Object Creation:** A central context object (`InteractionContext`) is created. This object encapsulates:
    *   The generated `InteractionId`.
    *   The `TenantId` from the request.
    *   The details from the original `InteractionRequest` (User ID, Content, Platform Context, Source `ArtifactReference`s, etc.).
    *   The **resolved `PersonaId`** (determined during Activation Check).
    *   Placeholders for fetched/extracted content (`ExtractedContents`).
    *   Any other relevant metadata needed for processing.
3.  **Persona Configuration Loading:** The `OrchestrationService` uses `IPersonaConfigurationProvider` to load the `PersonaConfiguration` for the resolved `TenantId` and `PersonaId`. This configuration is paramount for defining the operational scope (e.g., accessible knowledge bases, permissions) of the Persona.
4.  **Ephemeral Content Fetching & Context Population:** The `OrchestrationService` proceeds with context ranking and selective ephemeral fetching using `IArtifactProvider` (as described in the [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)). **All artifact access is performed under the constraints and permissions defined in the loaded `PersonaConfiguration` to ensure data isolation.** This populates the `InteractionContext.ExtractedContents`.
5.  **(Optional) Ephemeral Storage:** If intermediate reasoning needs to be persisted (e.g., `ShowYourWork`), the `IAgenticStrategyHandler` (invoked later by `IPersonaRuntime`) is responsible for requesting its persistence via `IArtifactProvider.SaveAsync`, again respecting the Persona's scope.
6.  **Hand-off to Handler/Runtime:** The `OrchestrationService` then prepares the necessary data for the background worker:
    *   **Asynchronous (Standard Path):** Packages necessary data (`NucleusIngestionRequest` containing request details, `TenantId`, `PersonaId`, and references) and places it onto the task queue (e.g., Azure Service Bus) for a background worker. The worker will repeat steps like loading `PersonaConfiguration` (step 3) and fetching content (step 4) strictly according to the received `PersonaId` and `TenantId` before calling `IPersonaRuntime.ExecuteAsync`.
    *   **(Hypothetical Synchronous Path - Not Standard for Activated API Interactions):** If a synchronous path were ever used, it would directly call `IPersonaRuntime.ExecuteAsync` with the populated `InteractionContext` and the loaded `PersonaConfiguration`.

## 3. Key Changes from Previous Model

*   **Centralized & Persona-Scoped:** Context creation and initiation logic resides within the `OrchestrationService` and is intrinsically tied to the resolved `PersonaId` from the activation phase.
*   **Simplified:** No broadcast, salience checks for *initiation*, or complex distributed locking required.
*   **Implicit:** Context creation is an implicit step within the `OrchestrationService`'s processing flow following successful activation and Persona resolution.

## 4. Related Documents

*   [API Architecture Overview](../10_ARCHITECTURE_API.md)
*   [API Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
*   [API Activation & Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)

## 5. Related Components and Documents

- **Core Implementation:**
  - [`OrchestrationService`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs): Central service managing the interaction lifecycle, including context setup scoped by the resolved Persona.
  - [`OrchestrationService.ProcessInteractionAsync`](../../../../src/Nucleus.Domain/Nucleus.Domain.Processing/OrchestrationService.cs#L59-L167): The primary method handling incoming requests, including Persona resolution.
- **Key Data Structures:**
  - [`InteractionContext`](../../../../src/Nucleus.Abstractions/Orchestration/InteractionContext.cs): Represents the context object created post-activation, containing the resolved `PersonaId`.
  - [`NucleusIngestionRequest`](../../../../src/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs): Used for queueing asynchronous processing tasks, carries `TenantId` and `PersonaId`.
- **Persona Configuration Provider:**
  - Interface: [`IPersonaConfigurationProvider`](../../../../src/Nucleus.Abstractions/Models/Configuration/IPersonaConfigurationProvider.cs)
- **Core Runtime:**
  - [`IPersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/Interfaces/IPersonaRuntime.cs): Interface for the central runtime, operates based on a specific `PersonaConfiguration`.
  - [`PersonaRuntime`](../../../../src/Nucleus.Domain/Personas/Nucleus.Personas.Core/PersonaRuntime.cs): Implementation of the runtime.
- **Related Orchestration Concepts:**
  - [Overall Orchestration](../ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
  - [Interaction Lifecycle](./ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)
  - [Routing](./ARCHITECTURE_ORCHESTRATION_ROUTING.md)
