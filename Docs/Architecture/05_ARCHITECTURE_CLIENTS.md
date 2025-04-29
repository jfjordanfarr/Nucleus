---
title: Architecture - Client Applications & Adapters
description: Outlines the architecture for clients (MVP Console App) and future platform adapters interacting with the Nucleus API via reference-based requests.
version: 3.5
date: 2025-04-27
parent: ./00_ARCHITECTURE_OVERVIEW.md
---

# Nucleus: Client Architecture

**Version:** 3.5
**Date:** 2025-04-27

This document outlines the architecture for client applications interacting with the Nucleus backend API, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It details the primary interaction mechanism for the Minimum Viable Product (MVP) – a command-line interface (CLI) application – and outlines future plans for integrating with collaboration platforms, emphasizing that **all interactions occur via the central API using `ArtifactReference` objects for file context**.

## 1. Core Principles

*   **API-First Interaction:** All clients, whether standalone applications (like `Nucleus.Console`) or integrated Platform Adapters (like `Nucleus.Adapters.Teams`), **must** interact exclusively with the backend via the defined `Nucleus.Services.Api` endpoints (see [API Architecture](./10_ARCHITECTURE_API.md) and [Deployment Architecture](./07_ARCHITECTURE_DEPLOYMENT.md)). Adapters act as translators between the platform and the API.
*   **Reference-Based Context:** Clients provide context about external artifacts (files, messages) by including [`ArtifactReference`](../../../Nucleus.Abstractions/Models/ArtifactReference.cs) objects within their API requests (typically in an [`AdapterRequest`](../../../Nucleus.Abstractions/Models/AdapterRequest.cs) payload). **Clients do not send raw file content to the API.**
*   **Clear Feedback:** Provide clear status updates and output to the user for all operations.
*   **Extensibility:** The core API design supports various client types.
*   **Development Focus (MVP):** The initial client prioritizes enabling rapid development, testing, and direct interaction for developers and agents.

## 2. MVP Client: Nucleus Console Application (`Nucleus.Console`)

The primary client interface for the Nucleus MVP is a .NET Console Application, implemented in the [`Nucleus.Console`](../../../Nucleus.Console/) project ([see `Program.cs`](../../../Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Program.cs)). This approach provides a direct, scriptable, and efficient way to interact with the backend API, facilitating development, testing, and agent-driven operations.

**Key Goals:**
*   Provide a command-line interface for core Nucleus operations ([ingestion](./01_ARCHITECTURE_PROCESSING.md), [querying](./02_ARCHITECTURE_PERSONAS.md), [status checks](./01_ARCHITECTURE_PROCESSING.md)).
*   Act as a direct client to the [`Nucleus.Api`](./07_ARCHITECTURE_DEPLOYMENT.md).
*   Display responses and status information clearly in the terminal.
*   Serve as the initial integration point for developers and AI assistants working on the project.

**Technology Stack:**
*   **.NET 9 Console Application:** The core project type.
*   **`System.CommandLine`:** For robust command parsing, argument handling, and help generation.
*   **`Microsoft.Extensions.Http` (`IHttpClientFactory`):** For making resilient HTTP requests to the `Nucleus.Api`.
*   **`Spectre.Console` (Optional but Recommended):** For richer terminal output (tables, spinners, formatted text).
*   **`Microsoft.Extensions.DependencyInjection`:** For managing dependencies like `HttpClient`.

**Interaction Pattern:**
1.  User invokes `Nucleus.Console` with a command and arguments (e.g., `nucleus interact --path "./docs/my_document.pdf"`).
2.  `System.CommandLine` parses the input.
3.  The corresponding command handler is executed.
4.  The handler constructs an [`AdapterRequest`](../../../Nucleus.Abstractions/Models/AdapterRequest.cs) with an [`ArtifactReference`](../../../Nucleus.Abstractions/Models/ArtifactReference.cs) for the specified path.
5.  The handler uses `HttpClient` (obtained via `IHttpClientFactory`, often via a dedicated service like [`NucleusApiServiceAgent`](../../../Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Services/NucleusApiServiceAgent.cs)) to make a request to the `Nucleus.Api` interaction endpoint.
6.  The handler receives the API response (e.g., JSON data like [`AdapterResponse`](../../../Nucleus.Abstractions/Models/AdapterResponse.cs), status codes).
7.  The handler formats and displays the results to the console (potentially using `Spectre.Console`).

**Example Command Structure (Conceptual):**

```bash
# Trigger analysis for a local file
nucleus interact --path "./docs/my_document.pdf"

# Trigger analysis for a URI
nucleus interact --uri "https://example.com/article.html"

# Query a specific persona
nucleus interact --persona FinanceExpert --prompt "Summarize the Q1 financial report highlights."

# List available personas
nucleus personas list

# Check status of processing artifacts
nucleus status list [--status Processing]

# Get details for a specific artifact
nucleus status get --id <artifact-guid>
```

**Displaying Output:**
*   **Success:** Display relevant information from the API response (e.g., artifact ID after ingestion, query response text). Use clear formatting (potentially tables for lists).
*   **Progress:** For long-running operations initiated via the console (if any), use spinners or progress indicators (`Spectre.Console`).
*   **Errors:** Display clear error messages based on API status codes or exception details. Include correlation IDs if available.

## 3. Future Clients: Platform Integration (Phase 2+)

While the Console App serves the MVP, the long-term vision involves integrating Nucleus directly into users' existing workflows via platform-specific bots and adapters.

**Goals:**
*   Bring [persona](./02_ARCHITECTURE_PERSONAS.md) interaction directly into Teams, Slack, Discord, Email, etc.
*   Leverage platform UI, notifications, and file sharing.
*   Enable natural language interaction with personas.

**Components (Planned for Phase 2+):**
*   **Platform Adapters (`Nucleus.Adapters.*`):** Implementations specific to each platform (Teams, Slack, Email) responsible for:
    *   Receiving messages/events.
    *   Translating them into Nucleus [API](./07_ARCHITECTURE_DEPLOYMENT.md) calls or messages.
    *   Handling platform [authentication](./06_ARCHITECTURE_SECURITY.md) and user mapping.
    *   Formatting and sending responses back to the platform.
*   **Common Adapter Interface (`IPlatformAdapter`):** A standard interface for adapters, defined in [Adapter Interfaces](../ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md).
*   **API Enhancements:** Potential additions to the [API](./07_ARCHITECTURE_DEPLOYMENT.md) to better support bot-specific needs (e.g., managing conversation state).

**Technology Stack (Potential):**
*   Microsoft Bot Framework SDK
*   Slack Bolt / Slack APIs
*   Discord.NET / Discord APIs
*   MailKit / Azure Communication Services / Graph API (for Email)

## 5. Common Adapter Patterns & Responsibilities (Phase 2+)

While implementations will vary based on the target platform, adapters should generally adhere to the following patterns and responsibilities defined in the [Adapter Interfaces document](../ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md):

*   **Initialization & Configuration:** Load necessary platform SDKs, authenticate with the platform (see [Security](./06_ARCHITECTURE_SECURITY.md)), and retrieve Nucleus [API](./10_ARCHITECTURE_API.md) endpoint/authentication details.
*   **User/Tenant Mapping:** Establish a reliable mapping between platform user/team identifiers and Nucleus user/tenant contexts (relevant to [Security](./06_ARCHITECTURE_SECURITY.md)).
*   **Event Handling:** Listen for relevant platform events (e.g., new messages, file shares, commands).
*   **Command Parsing:** Interpret user commands or specific interaction triggers (e.g., @mentions).
*   **API Interaction (Reference-Based):** Translate platform events and user commands into appropriate [`Nucleus.Api`](./10_ARCHITECTURE_API.md) calls, typically constructing an [`AdapterRequest`](../../../Nucleus.Abstractions/Models/AdapterRequest.cs) payload.
    *   **Ingestion/Analysis Trigger:** When a user interaction involves an artifact (e.g., sharing a file in Teams, referencing a local path in Console), the adapter constructs an `AdapterRequest` containing relevant user/context info and one or more [`ArtifactReference`](../../../Nucleus.Abstractions/Models/ArtifactReference.cs) objects pointing to the artifact(s) in the user's storage. It sends this request to the API (e.g., `POST /api/v1/interactions`). The **`Nucleus.Services.Api` then orchestrates** the analysis, using the provided references to ephemerally fetch content via `IArtifactProvider` as needed for [processing](./01_ARCHITECTURE_PROCESSING.md).
    *   **Querying:** Send user queries (within an `AdapterRequest`) to the relevant API endpoints for [persona](./02_ARCHITECTURE_PERSONAS.md) processing.
    *   **Status Checks:** Allow users to query the status of ongoing [processing](./01_ARCHITECTURE_PROCESSING.md) via dedicated API endpoints.
*   **Response Formatting:** Format Nucleus API responses (like [`AdapterResponse`](../../../Nucleus.Abstractions/Models/AdapterResponse.cs)) into platform-native messages (e.g., Teams Adaptive Cards, Slack Blocks, plain text).
*   **Feedback Mechanisms:** Provide clear feedback to the user about command acceptance, processing status, and final results.
*   **Artifact Handling (Reference Construction):** The adapter's primary role regarding artifacts is to **construct accurate [`ArtifactReference`](../../../Nucleus.Abstractions/Models/ArtifactReference.cs) objects** that the `Nucleus.Services.Api` can use (via `IArtifactProvider`) to locate and ephemerally retrieve the content from the user's source system. The adapter does *not* transfer file bytes to the API. (See specific adapter documents like [Console](../ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md) and [Teams](../ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)).
*   **Error Handling:** Gracefully handle platform-specific errors and Nucleus API errors, providing informative messages to the user.
*   **Administrative Capabilities:** Future adapters will also need to surface administrative functionalities (monitoring, user management, persona configuration, reporting) as defined in [Phase 4 Maturity Requirements](../Requirements/04_REQUIREMENTS_PHASE4_MATURITY.md#33-enterprise-readiness--admin-features) (REQ-P4-ADM-001 to ADM-004). This might involve dedicated admin commands or interfaces within the client platform (e.g., specific Adaptive Cards in Teams for authorized administrators), interacting with dedicated administrative API endpoints.

## 6. Next Steps

1.  **Initialize `Nucleus.Console` Project:** Set up the basic project structure ([`Nucleus.Console`](../../../Nucleus.Console/)) with required NuGet packages (`System.CommandLine`, `Microsoft.Extensions.Http`).
2.  **Configure `HttpClient`:** Use `IServiceCollection` and `IHttpClientFactory` to configure the client for accessing the [`Nucleus.Api`](./10_ARCHITECTURE_API.md) (base address, default headers if needed).
3.  **Implement Initial Commands:** Start with basic commands:
    *   `nucleus status ping`: A simple command to verify API connectivity.
    *   `nucleus interact --path <filepath>`: Implement triggering analysis via the interaction endpoint, constructing an `AdapterRequest` with an `ArtifactReference` for the local path.
    *   `nucleus interact --uri <uri>`: Similar to above, but for a URI reference.
    *   `nucleus interact --persona <name> --prompt "<prompt>"`: Implement the basic query workflow via the interaction endpoint.
4.  **Define API DTOs:** Ensure necessary Data Transfer Objects (DTOs) like [`AdapterRequest`](../../../Nucleus.Abstractions/Models/AdapterRequest.cs), [`AdapterResponse`](../../../Nucleus.Abstractions/Models/AdapterResponse.cs), and [`ArtifactReference`](../../../Nucleus.Abstractions/Models/ArtifactReference.cs), shared between the API and Console client, are defined (likely in [`Nucleus.Abstractions`](../../../Nucleus.Abstractions/)).
5.  **Develop Command Handlers:** Write the logic within the console app ([`Nucleus.Console`](../../../Nucleus.Console/)) to parse arguments, construct the `AdapterRequest`, call the API, and display results.
6.  **(Ongoing)** Refine command structure and output formatting as API capabilities expand.

---

_This architecture document focuses on the client-side interaction with the Nucleus API, emphasizing the API-first and reference-based approach for all client types._
