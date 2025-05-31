---
title: Architecture - Processing Orchestration Overview
description: Describes how Microsoft 365 Persona Agents orchestrate interactions by invoking backend Nucleus MCP Tool/Server applications.
version: 2.0
date: 2025-05-25
parent: ../01_ARCHITECTURE_PROCESSING.md
---

# Nucleus: Processing Orchestration Overview

## 1. Introduction

This document provides a high-level overview of the **Orchestration** sub-domain within the overall [Processing Architecture](../01_ARCHITECTURE_PROCESSING.md). In the revised Nucleus architecture, orchestration is primarily the responsibility of **Microsoft 365 Persona Agents**. These agents manage the *flow* of work required to handle user interactions by invoking various backend **Nucleus Model Context Protocol (MCP) Tool/Server applications**.

While [Ingestion](./ARCHITECTURE_PROCESSING_INGESTION.md) components (now often MCP Tools) focus on transforming raw artifacts, and [Personas](../02_ARCHITECTURE_PERSONAS.md) (embodied as M365 Agents) define the logic and user experience, Orchestration describes how the M365 Agent coordinates calls to these backend MCP Tools to fulfill a user's request or process an event.

Key goals of this agent-centric orchestration model include:
*   **Reliability & Resilience:** Leveraging the M365 Agent SDK's capabilities and the robustness of individual MCP Tools.
*   **Scalability:** M365 Agents can scale according to platform capabilities, and MCP Tools can be scaled independently based on demand.
*   **Decoupling & Modularity:** Clear separation of concerns between the agent's conversational logic and the specialized functions of backend MCP Tools.
*   **Observability:** Tracing and logging can be implemented within the M365 Agent and across MCP Tool invocations.

## 2. Core Responsibilities of M365 Persona Agents in Orchestration

The M365 Persona Agent, built using the Microsoft 365 Agents SDK, takes on the central coordinating role:

*   **Interaction Handling & Intent Recognition:**
    *   Receiving interaction events from the host platform (e.g., Teams message, Outlook add-in event).
    *   Using the M365 Agent SDK to understand user intent, parse commands, and extract relevant entities from the user's input and the surrounding context (e.g., attached files, email content).
*   **MCP Tool Discovery & Invocation:**
    *   Identifying the necessary Nucleus MCP Tool/Server(s) required to fulfill the user's intent (e.g., a content extraction tool, a knowledge store tool, an AI analysis tool).
    *   Using an `IMcpToolInvoker` (or similar mechanism as defined in [Processing Interfaces](./ARCHITECTURE_PROCESSING_INTERFACES.md)) to make requests to the appropriate MCP Tools.
    *   Passing necessary data to the MCP Tools, which might include artifact references (e.g., SharePoint URIs, message attachment IDs), user queries, or persona-specific configuration identifiers.
*   **Workflow Management:**
    *   Sequencing calls to multiple MCP Tools if necessary (e.g., extract content from a file, then send it to an AI analysis tool, then store the results).
    *   Managing the state of the interaction within the agent itself, potentially using the M365 Agent SDK's state management capabilities.
*   **Response Generation & Delivery:**
    *   Aggregating responses from various MCP Tool calls.
    *   Formatting the final response for the user.
    *   Using the M365 Agent SDK (via a component like `IPlatformMessageRelay` from [Processing Interfaces](./ARCHITECTURE_PROCESSING_INTERFACES.md)) to deliver the response back to the user on the host platform.
*   **Error Handling & Fallbacks:**
    *   Managing errors from MCP Tool invocations (e.g., retries, user notifications, graceful degradation).

## 3. Relationship to Other Components

*   **Microsoft 365 Persona Agents:** The primary orchestrators. They contain the conversational logic and the intelligence to decide which MCP Tools to call and in what order.
    *   **Related Architecture:** [Clients Architecture](../../05_ARCHITECTURE_CLIENTS.md), [Personas Architecture](../../02_ARCHITECTURE_PERSONAS.md)
*   **Nucleus MCP Tool/Server Applications:** Backend services that perform specific tasks (e.g., `Nucleus_ContentExtractor_McpServer`, `Nucleus_KnowledgeStore_McpServer`, `Nucleus_AiAnalysis_McpServer`). They expose MCP-compliant endpoints.
    *   **Related Architecture:** [Abstractions Architecture](../../12_ARCHITECTURE_ABSTRACTIONS.md), [Deployment Architecture](../../07_ARCHITECTURE_DEPLOYMENT.md)
*   **Shared Interfaces (`IMcpToolInvoker`, `IContentExtractor`, `IArtifactStorageProvider`, etc.):** Define the contracts for how agents invoke tools and how tools perform their work.
    *   **Related Architecture:** [Processing Interfaces](./ARCHITECTURE_PROCESSING_INTERFACES.md)
*   **M365 Platform (Teams, Outlook, etc.):** Provides the user interface, event triggers, and the runtime environment for the M365 Persona Agents.
*   **Nucleus Database:** Accessed by specific MCP Tools (e.g., `Nucleus_KnowledgeStore_McpServer`) to persist and retrieve `ArtifactMetadata` and `PersonaKnowledgeEntry` objects.
    *   **Related Architecture:** [Database Architecture](../../04_ARCHITECTURE_DATABASE.md)
*   **AI Inference Providers (e.g., Azure OpenAI, Google Gemini):** Used by specific MCP Tools (e.g., `Nucleus_AiAnalysis_McpServer`) for tasks like summarization, Q&A, or other intelligent processing.
    *   **Related Architecture:** [AI Integration Architecture](../../08_ARCHITECTURE_AI_INTEGRATION.md)

## 4. Deprecated Concepts (from previous API-First Architecture)

The following concepts related to orchestration in the previous monolithic API architecture are now deprecated or significantly changed:

*   **Central `IOrchestrationService`:** Replaced by the distributed orchestration logic within M365 Persona Agents invoking MCP Tools.
*   **API-based Activation (`IActivationChecker`):** Activation logic now resides within the M365 Persona Agent, guided by how it's configured to respond to platform events.
*   **Central Background Task Queue (`IBackgroundTaskQueue`) for all interactions:** While individual MCP Tools might use internal queues for their own asynchronous tasks, the primary orchestration is driven by the agent.
*   **Direct API invocation for all processing steps:** Replaced by agent-to-tool communication via MCP.
*   Detailed sub-documents like `ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md`, `ARCHITECTURE_ORCHESTRATION_ROUTING.md`, and `ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md` are being **archived** as their specific approaches are no longer representative of the M365 Agent + MCP model.

## 5. Future Considerations

As the M365 Agents SDK and MCP evolve, the orchestration capabilities within agents may become more sophisticated. For very complex, long-running, or stateful cross-tool workflows, future iterations might explore:
*   Leveraging features within the M365 Agents SDK for more advanced workflow patterns.
*   The use of external orchestration services (e.g., Azure Logic Apps, Durable Functions) that an M365 Agent could trigger if a process becomes too complex to manage internally.

This shift to agent-centric orchestration aligns Nucleus with modern distributed application patterns and the capabilities of the Microsoft 365 ecosystem.