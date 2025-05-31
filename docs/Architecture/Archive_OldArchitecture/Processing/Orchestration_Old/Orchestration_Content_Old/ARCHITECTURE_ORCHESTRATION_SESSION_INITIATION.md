---
title: "Archived: API-Driven Session Initiation for Queued Interactions"
description: "This document is archived. Original content described session initiation in the deprecated API-first architecture."
version: 1.3
date: 2025-05-25
published: false
---

# ARCHIVED: API-Driven Session Initiation for Queued Interactions

**This document has been archived as of 2025-05-25.**

The content previously in this file described how the `Nucleus.Services.Api` initiated interaction "sessions" by preparing context and queueing tasks for background workers in the deprecated monolithic architecture.

This model has been superseded by the new architecture centered around **Microsoft 365 Persona Agents** and **Model Context Protocol (MCP) Tool/Server applications**.

For current architectural details, please refer to:

*   [Architecture Overview](../../00_ARCHITECTURE_OVERVIEW.md)
*   [Processing Architecture Overview](../01_ARCHITECTURE_PROCESSING.md)
*   [Processing Orchestration Overview](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   [Personas Architecture](../../02_ARCHITECTURE_PERSONAS.md)
*   [Clients Architecture](../../05_ARCHITECTURE_CLIENTS.md)
*   [Abstractions Architecture](../../12_ARCHITECTURE_ABSTRACTIONS.md)

Key changes include:

*   **Interaction Context:** Managed by the M365 Persona Agent using the M365 Agent SDK's capabilities.
*   **Task Initiation:** M365 Persona Agents directly invoke MCP Tools with the necessary context for each specific task.

The concept of a central API service creating a generic `NucleusIngestionRequest` to be placed on a queue for a general-purpose worker is no longer the primary model. Instead, M365 Agents manage the interaction lifecycle and make targeted calls to specialized MCP Tools.
