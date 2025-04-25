---
title: Architecture - Persona Configuration
description: Defines the structure and available settings for configuring Nucleus Personas.
version: 1.0
date: 2025-04-24
parent: ../06_ARCHITECTURE_PERSONAS.md
---

# Persona Configuration

## 1. Overview

This document outlines the structure and parameters used to configure individual Nucleus Personas. Configuration defines a Persona's behavior, capabilities, activation triggers, and operational settings.

Configuration might be stored in various ways (e.g., JSON files, database entries) but should adhere to the logical structure defined here.

## 2. Configuration Schema (Conceptual)

Each Persona configuration includes the following key properties:

### 2.1 Core Identification

*   `PersonaId`: (String, Required) A unique identifier for the persona (e.g., `Coder`, `DataAnalyst`).
*   `DisplayName`: (String, Required) A user-friendly name for the persona.
*   `Description`: (String, Optional) A brief description of the persona's purpose and capabilities.

### 2.2 Operational Settings

*   **`ShowYourWork`**: (Boolean, Optional, Default: `false`)
    *   If `true`, the Persona will save its internal reasoning/planning artifact upon completing an interaction.
    *   This artifact is typically saved to a platform-specific location (e.g., `.Nucleus/Personas/{PersonaId}/ShowYourWork/` in Teams SharePoint) for auditability and transparency.
    *   Referenced in: [Interaction Lifecycle](../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md#conditional-artifact-generation-show-your-work)

### 2.3 Activation Rules

*   *(Placeholder: Define structure for activation rules, e.g., mentions, channel scope, user scope - see [ARCHITECTURE_ORCHESTRATION_ROUTING.md](../Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md))*.

### 2.4 Capability Settings

*   *(Placeholder: Define LLM/Model configuration, e.g., provider, model name, temperature, max tokens)*.
*   *(Placeholder: Define allowed/enabled Tools or Plugins)*.
*   *(Placeholder: Define grounding data sources or knowledge bases)*.

## 3. Configuration Management

*(Placeholder: Describe how configurations are loaded, updated, and potentially scoped - e.g., global defaults vs. context-specific overrides)*.
