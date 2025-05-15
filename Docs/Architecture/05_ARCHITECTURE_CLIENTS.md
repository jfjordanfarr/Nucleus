---
title: Architecture - Client Applications & Adapters
description: Outlines the architecture for client applications and platform adapters interacting with the Nucleus API, featuring the Local Adapter as a primary internal client mechanism.
version: 4.3
date: 2025-05-14
parent: ./00_ARCHITECTURE_OVERVIEW.md
see_also:
  - title: "Local Adapter Architecture"
    link: "./ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md"
  - title: "Teams Adapter Architecture"
    link: "./ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md"
  - title: "Discord Adapter Architecture"
    link: "./ClientAdapters/ARCHITECTURE_ADAPTERS_DISCORD.md"
  - title: "Email Adapter Architecture"
    link: "./ClientAdapters/ARCHITECTURE_ADAPTERS_EMAIL.md"
  - title: "API Architecture"
    link: "./10_ARCHITECTURE_API.md"
---

# Nucleus: Client Architecture


This document outlines the architecture for client applications and adapters interacting with the Nucleus backend API, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It details the primary interaction mechanism for an internal client – the **`Nucleus.Infrastructure.Adapters.Local` library** – and outlines future plans for integrating with collaboration platforms, emphasizing that **all interactions occur via the central API using `ArtifactReference` objects for file context where applicable**.

## 1. Core Principles

*   **API-First Interaction:** All clients and adapters, whether internal libraries (like `Nucleus.Infrastructure.Adapters.Local`) or integrated Platform Adapters (like `Nucleus.Adapters.Teams`), **must** interact with the backend via the defined `Nucleus.Services.Api` endpoints or its direct services (see [API Architecture](./10_ARCHITECTURE_API.md) and [Deployment Architecture](./07_ARCHITECTURE_DEPLOYMENT.md)). Adapters act as translators between the platform/tool/internal service and the API's core logic.
*   **Reference-Based Context:** When dealing with external artifacts (files, messages), clients and adapters provide context by including [`ArtifactReference`](../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) objects within their API requests (typically in an [`AdapterRequest`](../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterRequest.cs) payload). **Clients do not typically send raw file content directly to the top-level API endpoints if a reference mechanism is more appropriate.**
*   **Clear Feedback (for user-facing adapters):** User-facing adapters should provide clear status updates and results to the user.
*   **Extensibility:** The core API design supports various client and adapter types.
*   **Internal Integration Focus (LocalAdapter):** The initial internal client adapter prioritizes enabling efficient, in-process interaction for system tasks and services within the `Nucleus.Services.Api` host.

## 2. Primary Internal Client: Local Adapter (`Nucleus.Infrastructure.Adapters.Local`)

The primary internal client interface for Nucleus is a .NET Class Library, implemented in the [`Nucleus.Infrastructure.Adapters.Local`](../../src/Nucleus.Infrastructure/Adapters/Nucleus.Infrastructure.Adapters.Local/) project ([see `ARCHITECTURE_ADAPTERS_LOCAL.md`](./ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md)). This library provides a direct, programmatic, and efficient way for services within `Nucleus.Services.Api` or other tightly coupled local components to interact with core system logic without network overhead.

**It is NOT intended as a standalone user-facing application or a developer CLI tool.** It acts as a lightweight in-process mechanism for `Nucleus.Services.Api` or other authorized internal services.

**Key Goals:**
*   Provide a programmatic interface for triggering core Nucleus operations (ingestion, interaction) from within the same process as `Nucleus.Services.Api`.
*   Facilitate internal system tasks, scheduled jobs, or workflows that require interaction with Nucleus core logic.
*   Enable efficient, low-latency communication for tightly coupled local components.
*   Serve as a direct integration point for internal system components requiring Nucleus functionalities.

**Technology Stack:**
*   **.NET Class Library:** The core project type.
*   **`Microsoft.Extensions.DependencyInjection`:** For integration with the host's service container and resolving dependencies.
*   **`System.Text.Json`:** For serializing/deserializing DTOs if used in a request/response pattern internally.

**Interaction Pattern:**
1.  An internal service or component within the `Nucleus.Services.Api` host (or a similar authorized local host) requires interaction with Nucleus core logic.
2.  This service resolves an instance of a service provided by the `Nucleus.Infrastructure.Adapters.Local` library via dependency injection.
3.  The internal service calls methods on the `LocalAdapter`'s service, passing necessary data (potentially constructing an `AdapterRequest` or a more specialized DTO).
4.  The `LocalAdapter` service then directly invokes the appropriate application or domain services within `Nucleus.Services.Api` or its dependencies.
5.  A response (e.g., an `AdapterResponse` DTO or direct return value) is returned through the `LocalAdapter` service to the calling internal component.

This pattern bypasses external HTTP communication for these internal interactions.

## 3. Future Clients: Platform Integration (Phase 2+)

While the `LocalAdapter` serves internal programmatic needs, the long-term vision involves integrating Nucleus directly into users' existing workflows via platform-specific bots and adapters (e.g., `Nucleus.Adapters.Teams`).

**Goals:**
*   **Seamless User Experience:** Integrate Nucleus functionality naturally within the target platform (Microsoft Teams, Slack, etc.).
*   **Platform-Specific Interactions:** Leverage platform features (e.g., Adaptive Cards in Teams, Slack Blocks) for rich input and output.

**Common Adapter Responsibilities (for user-facing platform adapters):**
*   **Authentication & Authorization:** Securely identify users and verify their permissions according to platform and Nucleus policies.
*   **Event Handling:** Process incoming events from the platform (e.g., new messages, file uploads, bot mentions).
*   **Ingestion/Analysis Trigger:** When a user interaction involves an artifact (e.g., sharing a file in Teams), the adapter constructs an `AdapterRequest` containing relevant user/context info and one or more [`ArtifactReference`](../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) objects pointing to the artifact(s). It sends this request to the API (e.g., `POST /api/v1/interactions`). The **`Nucleus.Services.Api` then orchestrates** the analysis, using the provided references to ephemerally fetch content via `IArtifactProvider` as needed for [processing](./01_ARCHITECTURE_PROCESSING.md).
*   **Querying:** Send user queries (within an `AdapterRequest`) to the relevant API endpoints for [persona](./02_ARCHITECTURE_PERSONAS.md) processing.
*   **Status Checks:** Future adapters could allow users to query the status of ongoing [processing](./01_ARCHITECTURE_PROCESSING.md) via dedicated API endpoints.
*   **Response Formatting:** Format Nucleus API responses (like [`AdapterResponse`](../../src/Nucleus.Abstractions/Models/ApiContracts/AdapterResponse.cs)) into platform-native messages (e.g., Teams Adaptive Cards, Slack Blocks, plain text).
*   **Feedback Mechanisms:** Provide clear feedback to the user about command acceptance, processing status, and final results.
*   **Artifact Handling (Reference Construction):** The adapter's primary role regarding artifacts is to **construct accurate [`ArtifactReference`](../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) objects** that the `Nucleus.Services.Api` can use (via `IArtifactProvider`) to locate and ephemerally retrieve the content from the user's source system or specified path. The adapter does *not* transfer file bytes to the API. (See specific adapter documents like [Local Adapter](./ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md) and [Teams Adapter](./ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)).
*   **Error Handling:** Gracefully handle platform-specific errors and Nucleus API errors, providing informative messages to the user.
*   **Administrative Capabilities:** Future adapters will also need to surface administrative functionalities (monitoring, user management, persona configuration, reporting) as defined in [Phase 4 Maturity Requirements](../Requirements/04_REQUIREMENTS_PHASE4_MATURITY.md#33-enterprise-readiness--admin-features) (REQ-P4-ADM-001 to ADM-004). This might involve dedicated admin commands or interfaces within the client platform (e.g., specific Adaptive Cards in Teams for authorized administrators), interacting with dedicated administrative API endpoints.

---

_This architecture document focuses on the client-side interaction with the Nucleus API, emphasizing the API-first and reference-based approach for all client types._
