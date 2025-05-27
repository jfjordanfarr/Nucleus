---
title: "Namespace - Nucleus.Shared.Kernel"
description: "Describes the shared kernel project containing core, reusable logic for executing Nucleus Persona behaviors within M365 Persona Agent applications."
version: 4.0 # Updated version
date: 2025-05-27 # Updated date
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Shared.Kernel

**Suggested Relative Path:** `src/Nucleus.Shared/Nucleus.Shared.Kernel/Nucleus.Shared.Kernel.csproj`

## 1. Purpose

This project (`Nucleus.Shared.Kernel`) contains the core, reusable, platform-agnostic domain logic for executing Nucleus Persona behaviors. This includes the `PersonaRuntime` (implementing `IPersonaRuntime`) and `IAgenticStrategyHandler` implementations. This 'kernel' is used by each distinct Nucleus M365 Persona Agent application (e.g., `Nucleus.Agent.EduFlow`). It orchestrates calls to AI services (`IChatClient` via `Microsoft.Extensions.AI`) and prepares calls to backend Nucleus MCP Tools based on `PersonaConfiguration`.

## 2. Key Components

*   **`PersonaRuntime` (Service/Engine):** The central component that loads a `PersonaConfiguration` and executes the defined agentic strategy by invoking the appropriate `IAgenticStrategyHandler`.
*   **`IAgenticStrategyHandler` Implementations:** Concrete strategy handlers responsible for specific types of agentic behavior, for example:
    *   `SimpleRagStrategyHandler`
    *   `ToolUsingStrategyHandler`
    *   (Other future strategy handlers)

## 3. Dependencies

*   `Nucleus.Core.Abstractions` (for `IPersonaRuntime`, `IAgenticStrategyHandler`, `PersonaConfiguration`, and `Microsoft.Extensions.AI` interfaces like `IChatClient` which may be re-exported or directly referenced from Abstractions).
*   Potentially `Microsoft.SemanticKernel.Core` if Semantic Kernel is used directly within this kernel for advanced planning or prompt templating, though the primary LLM interaction is intended via `IChatClient`.

## 4. Dependents

*   All `Nucleus.Agent.<PersonaName>.dll` projects (e.g., `src/Nucleus.Agents/Nucleus.Agent.EduFlow/Nucleus.Agent.EduFlow.csproj`). These M365 Persona Agent applications consume the `PersonaRuntime` and strategy handlers from this shared kernel.

## 5. Related Documents

*   [../01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [../../CoreNucleus/01_PERSONA_CONCEPTS.md](../../CoreNucleus/01_PERSONA_CONCEPTS.md)
*   [../../Agents/01_M365_AGENTS_OVERVIEW.md](../../Agents/01_M365_AGENTS_OVERVIEW.md)
*   [../../Agents/02_AGENT_CONFIGURATION.md](../../Agents/02_AGENT_CONFIGURATION.md) (Note: This should likely be renamed/refocused to `PERSONA_CONFIGURATION.md` to align with terminology)
