---
title: "Archived: API Interaction Processing Lifecycle (Activation & Queued Execution)"
description: "This document is archived. Original content described the lifecycle of interaction processing in the deprecated API-first architecture."
version: 1.4
date: 2025-05-25
published: false
---

# ARCHIVED: API Interaction Processing Lifecycle (Activation & Queued Execution)

**This document has been archived as of 2025-05-25.**

The content previously in this file described the detailed lifecycle of interaction processing, including API-based activation, queueing, and background worker execution, within the context of the deprecated monolithic `Nucleus.Services.Api` and its associated orchestration services.

This model has been superseded by the new architecture centered around **Microsoft 365 Persona Agents** and **Model Context Protocol (MCP) Tool/Server applications**.

For current architectural details, please refer to:

*   [Architecture Overview](../../00_ARCHITECTURE_OVERVIEW.md)
*   [Processing Architecture Overview](../01_ARCHITECTURE_PROCESSING.md)
*   [Processing Orchestration Overview](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
*   [Personas Architecture](../../02_ARCHITECTURE_PERSONAS.md)
*   [Clients Architecture](../../05_ARCHITECTURE_CLIENTS.md)
*   [Abstractions Architecture](../../12_ARCHITECTURE_ABSTRACTIONS.md)

Key changes include:

*   **Orchestration:** Now primarily handled by M365 Persona Agents.
*   **Processing Logic:** Encapsulated within discrete MCP Tool/Server applications.
*   **Communication:** M365 Agents invoke MCP Tools.

The detailed step-by-step flow involving a central API, `IActivationChecker`, `IOrchestrationService`, and `IBackgroundTaskQueue` as previously described here is no longer the primary model for Nucleus interaction processing.