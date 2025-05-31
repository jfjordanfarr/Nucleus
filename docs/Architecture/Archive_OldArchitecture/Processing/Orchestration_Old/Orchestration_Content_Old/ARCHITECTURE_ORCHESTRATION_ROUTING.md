---
title: "Archived: API Activation & Asynchronous Routing"
description: "This document is archived. Original content described API-based activation and routing in the deprecated API-first architecture."
version: 1.3
date: 2025-05-25
published: false
---

# ARCHIVED: API Activation & Asynchronous Routing

**This document has been archived as of 2025-05-25.**

The content previously in this file described the mechanisms for interaction activation (e.g., using `IActivationChecker`, `IPersonaResolver`) and subsequent asynchronous routing to a background queue, all managed by the central `Nucleus.Services.Api` in the deprecated monolithic architecture.

This model has been superseded by the new architecture centered around **Microsoft 365 Persona Agents** and **Model Context Protocol (MCP) Tool/Server applications**.

For current architectural details, please refer to:

*   [Architecture Overview](../../00_ARCHITECTURE_OVERVIEW.md)
*   [Processing Architecture Overview](../01_ARCHITECTURE_PROCESSING.md)
*   [Processing Orchestration Overview](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   [Personas Architecture](../../02_ARCHITECTURE_PERSONAS.md)
*   [Clients Architecture](../../05_ARCHITECTURE_CLIENTS.md)
*   [Abstractions Architecture](../../12_ARCHITECTURE_ABSTRACTIONS.md)

Key changes include:

*   **Activation Logic:** Now resides within individual M365 Persona Agents, which decide how to respond to platform events.
*   **Routing:** M365 Persona Agents directly invoke specific MCP Tools as needed, rather than relying on a central API to route to a generic queue.

The concepts of a central API controller performing activation checks and pushing all activated interactions to a single queue for later processing by generic workers are no longer representative of the current Nucleus design.
