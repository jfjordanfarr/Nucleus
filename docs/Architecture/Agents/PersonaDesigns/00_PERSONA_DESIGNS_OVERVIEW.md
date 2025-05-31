---
title: "Nucleus Persona Designs Overview"
description: "Overview of the different Nucleus Persona designs, detailing their specific roles, capabilities, and target user groups within the M365 Agent and MCP Tool architecture."
version: 1.0
date: 2025-05-29
parent: "../01_M365_AGENTS_OVERVIEW.md"
see_also:
  - title: "M365 Agents Overview"
    link: "../01_M365_AGENTS_OVERVIEW.md"
  - title: "Bootstrapper Persona Design"
    link: "./ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md"
  - title: "Educator Persona Design"
    link: "./ARCHITECTURE_PERSONAS_EDUCATOR.md"
  - title: "Professional Persona Design"
    link: "./ARCHITECTURE_PERSONAS_PROFESSIONAL.md"
---

# Nucleus Persona Designs Overview

## 1. Introduction

This document provides an overview and central navigation point for the various **Nucleus Persona Designs**. Each persona represents a specialized AI assistant, implemented as a **Nucleus M365 Persona Agent application**, tailored to specific domains, user needs, and interaction patterns. These agents leverage the common Nucleus backend infrastructure of **MCP (Model Context Protocol) Tool applications** for core functionalities like data access, AI processing, and knowledge management.

The purpose of defining distinct persona designs is to:

*   Clearly articulate the intended role and capabilities of each specialized agent.
*   Provide a blueprint for their technical implementation, including specific configurations, prompts, and interactions with MCP Tools.
*   Facilitate understanding of how different personas address unique user requirements within the broader Nucleus ecosystem.

## 2. Core Persona Design Documents

The following documents detail the architecture and design of individual Nucleus Personas:

*   **[Bootstrapper Persona](./ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md):**
    *   **Role:** A foundational persona responsible for initial setup, configuration, and potentially guiding users through the deployment or customization of other personas or Nucleus services. It acts as a meta-persona or an administrative assistant for the Nucleus system itself.
    *   **Key Capabilities:** System diagnostics, configuration management, user guidance for Nucleus platform features.

*   **[EduFlow OmniEducator Persona](./ARCHITECTURE_PERSONAS_EDUCATOR.md):**
    *   **Role:** An educational assistant designed to support learners and educators by observing learning activities, providing insights based on pedagogical frameworks (like Learning Facets), and facilitating access to educational resources.
    *   **Key Capabilities:** Analysis of learning artifacts, application of Learning Facet schemas, personalized feedback, progress tracking.
    *   **Sub-Architectures:**
        *   [Educator Reference Implementation Details](./Educator/ARCHITECTURE_EDUCATOR_REFERENCE.md)
        *   [Educator Knowledge Trees](./Educator/ARCHITECTURE_EDUCATOR_KNOWLEDGE_TREES.md)

*   **[Professional Persona (IT Helpdesk Example)](./ARCHITECTURE_PERSONAS_PROFESSIONAL.md):**
    *   **Role:** A versatile persona template adaptable for various professional and enterprise scenarios, exemplified by an IT Helpdesk assistant. It focuses on providing contextual information, answering queries, and potentially automating tasks within a business environment.
    *   **Key Capabilities:** Knowledge retrieval from internal KBs, answering domain-specific questions, integration with enterprise systems (e.g., ticketing via MCP Tools).
    *   **Sub-Architectures:**
        *   [Azure/.NET IT Helpdesk Reference Implementation](./Professional/ARCHITECTURE_AZURE_DOTNET_HELPDESK.md)

## 3. Common Principles Across Persona Designs

While each persona is specialized, they all adhere to the core architectural principles of the Nucleus platform:

*   **M365 Agent Implementation:** Built using the Microsoft 365 Agents SDK.
*   **MCP Tool Consumption:** Rely on backend Nucleus MCP Tool applications for data, AI, and other services.
*   **Configurability:** Defined by `PersonaConfiguration` which includes prompts, LLM settings, and MCP tool bindings.
*   **User-Centricity:** Designed to provide value within specific user workflows and contexts.

This overview serves as a starting point for exploring the diverse range of AI-driven assistance that Nucleus aims to provide through its specialized M365 Persona Agents.
