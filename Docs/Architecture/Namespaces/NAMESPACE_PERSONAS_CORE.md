---
title: Namespace - Nucleus.Personas.Core
description: Describes the core domain logic project containing the Persona Runtime engine for executing persona configurations.
version: 2.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Domain.Personas.Core

**Relative Path:** `src/Nucleus.Domain/Personas/Nucleus.Domain.Personas.Core/Nucleus.Domain.Personas.Core.csproj`

## 1. Purpose

This project contains the core domain logic related to Personas – the specialized, task-oriented AI agents within the Nucleus system. It forms a crucial part of the Domain Layer, most notably housing the **Persona Runtime engine** responsible for executing specific `PersonaConfiguration` definitions and managing the agentic processing loops.

## 2. Key Components

*   **`PersonaRuntime` (Service/Engine):** The central component that loads a `PersonaConfiguration` and executes the defined agentic strategy (e.g., Simple RAG, Multi-Step Reasoning, Tool-Using) by orchestrating interactions with AI services (`IChatClient`), data repositories, and potentially tools.
*   **Agentic Strategy Implementations:** Concrete implementations of the different strategies defined in `AgenticStrategyType` (e.g., classes handling the logic for `SimpleRag`, `MultiStepReasoning`).
*   **(Potential) `PersonaConfigurationLoader`:** Logic responsible for retrieving `PersonaConfiguration` data (though this might also live closer to the API service depending on final design).
*   **(Deprecated Concepts):** The `IPersona` interface and specific C# classes implementing it directly are no longer used for core persona logic, replaced by the configuration + runtime model.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References shared interfaces like `IChatClient`, `IRepository`, DTOs like `PersonaConfiguration`, `InteractionContext`, constants)
*   Potentially `src/Nucleus.Domain.Processing/Nucleus.Domain.Processing.csproj` if it uses services from there (e.g., for certain orchestration sub-steps if not handled by the API layer).

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (The API service invokes the `PersonaRuntime` after activating an interaction and loading the appropriate configuration).

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [02_ARCHITECTURE_PERSONAS.md](../02_ARCHITECTURE_PERSONAS.md)
*   [ARCHITECTURE_PERSONAS_CONFIGURATION.md](../Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md)
*   [ARCHITECTURE_PROCESSING_ORCHESTRATION.md](../Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
