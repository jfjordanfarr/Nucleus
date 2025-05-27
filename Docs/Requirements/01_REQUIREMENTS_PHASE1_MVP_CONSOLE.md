---
title: "ARCHIVED - Requirements - Phase 1: MVP Console (Superseded by M365 Agent & MCP Architecture)"
description: "This document is ARCHIVED. The MVP Console concept has been superseded by the Microsoft 365 Agent SDK and Model Context Protocol (MCP) architecture."
version: 0.1
date: 2023-10-26
---

> [!WARNING]
> **This document is ARCHIVED and is no longer relevant to the current Nucleus project direction.**
> The "MVP Console" approach has been superseded by the Microsoft 365 Agent SDK and Model Context Protocol (MCP) architecture.
> For current requirements, please refer to `00_PROJECT_MANDATE.md` and other active requirement documents.

## ARCHIVED CONTENT

### 1. Overview

This document outlined the requirements for the Phase 1 MVP Console for the Nucleus project, which have been superseded by the Microsoft 365 Agent SDK and Model Context Protocol (MCP) architecture. The MVP Console was intended as a lightweight, local development tool to facilitate rapid prototyping and testing of Nucleus capabilities in conjunction with the Microsoft 365 Copilot and related technologies.

### 2. Goals

The primary goals for the MVP Console were:

*   To provide a simplified, local environment for developing and testing Nucleus features.
*   To enable quick iteration on user interactions and system responses.
*   To serve as a proof-of-concept for integrating various components of the Nucleus ecosystem.

### 3. Requirements

#### 3.1. General

*   **REQ-MVP-CNSL-001:** The MVP Console MUST be a .NET 6.0+ application.
*   **REQ-MVP-CNSL-002:** The application structure MUST follow the Clean Architecture guidelines.
*   **REQ-MVP-CNSL-003:** The solution MUST include the following projects:
    *   `Nucleus.Console`: The main console application.
    *   `Nucleus.Api`: The API project for handling interactions.
    *   `Nucleus.Domain`: The domain model and business logic.
    *   `Nucleus.Infrastructure`: The infrastructure and data access layer.
*   **REQ-MVP-CNSL-004:** The console application MUST support the following commands:
    *   `help`: Display available commands.
    *   `exit`: Exit the application.
    *   `query <text>`: Submit a query to the system.
    *   `ingest <file>`: Ingest a file for processing.
    *   `status`: Display the current status of the system.

#### 3.2. API

*   **REQ-MVP-API-001:** The API MUST expose a unified `/api/v1/interactions` endpoint (e.g., `POST /api/v1/interactions`).
    *   Accepts a standard interaction request object (containing user info, session context, query text, list of `ArtifactReference` objects, etc.).
    *   Routes the request to the appropriate handler based on context/payload (e.g., activating a Persona for a query, initiating processing for an artifact reference).
    *   For MVP queries, injects and calls the `HandleInteractionAsync` method of the appropriate registered Persona (initially `BootstrapperPersona`).
    *   Returns the persona's response or an acknowledgement (e.g., for asynchronous processing triggers).
*   **REQ-MVP-API-002:** The API MUST expose a placeholder `/api/status` endpoint (e.g., `GET /api/v1/status`).
    *   Returns basic status information (e.g., API health, loaded personas).
*   **REQ-MVP-API-003:** The API MUST correctly register and inject the `BootstrapperPersona` and other necessary services (Logging, Configuration, `IPersonaKnowledgeRepository`, `IEmbeddingGenerator`, `IArtifactProvider`). **(PARTIAL - Persona, Logging, Config injected. Repository/Embeddings/Provider TBD)**
*   **REQ-MVP-API-004:** The API MUST correctly use connection strings/configurations provided by Aspire/environment for accessing emulated/external services (Cosmos DB, AI Provider). **(PARTIAL - AI Provider config used. Cosmos DB TBD)**
*   **REQ-MVP-API-005:** The API MUST include basic health checks (`/healthz`).

#### 3.3. System Behavior (Core Logic & Processing)

*   **REQ-MVP-SYS-001:** The `BootstrapperPersona` MUST implement `HandleInteractionAsync` to: **(COMPLETE)**
    *   Receive the interaction context (including query text) from the API.
    *   Interact with the configured AI Model (via `IGenerativeAI`) to generate a response.
    *   Return the generated response.
*   **REQ-MVP-SYS-002:** (Stretch Goal/Minimal) After handling a query, the system SHOULD:
    *   Generate embeddings for the query and/or response (`IEmbeddingGenerator`).
    *   Store a basic `PersonaKnowledgeEntry` record in the Cosmos DB emulator (`IPersonaKnowledgeRepository`) capturing the interaction.
*   **REQ-MVP-SYS-003:** The system MUST implement basic `IPersonaKnowledgeRepository` using the Cosmos DB .NET SDK against the emulator.
*   **REQ-MVP-SYS-004:** The system MUST implement a basic `IEmbeddingGenerator` using the configured AI provider's SDK.
*   **REQ-MVP-SYS-005:** Error handling MUST be implemented in the API and Persona logic to catch common failures (e.g., AI API errors, database connection issues) and return appropriate error responses through the API to the calling client.

### 4. Archival Notice

This document is archived and no longer maintained. The information contained herein is superseded by other documentation and is provided here for reference only.
