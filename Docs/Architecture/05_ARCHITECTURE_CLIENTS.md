---
title: Architecture - Client Applications & Adapters
description: Outlines the architecture for clients (MVP Console App) and future platform adapters interacting with the Nucleus API.
version: 3.0
date: 2025-04-11
---

# Nucleus OmniRAG: Client Architecture

**Version:** 3.0
**Date:** 2025-04-11

This document outlines the architecture for client applications interacting with the Nucleus OmniRAG backend API, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It details the primary interaction mechanism for the Minimum Viable Product (MVP) – a command-line interface (CLI) application – and outlines future plans for integrating with collaboration platforms.

## 1. Core Principles

*   **Direct API Interaction:** Clients primarily interact with the backend via the defined `Nucleus.Api` endpoints (see [Deployment Architecture](./07_ARCHITECTURE_DEPLOYMENT.md)).
*   **Clear Feedback:** Provide clear status updates and output to the user for all operations.
*   **Extensibility:** The core API design should support various client types.
*   **Development Focus (MVP):** The initial client prioritizes enabling rapid development, testing, and direct interaction for developers and agents.

## 2. MVP Client: Nucleus Console Application (`Nucleus.Console`)

The primary client interface for the Nucleus OmniRAG MVP is a .NET Console Application. This approach provides a direct, scriptable, and efficient way to interact with the backend API, facilitating development, testing, and agent-driven operations.

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
1.  User invokes `Nucleus.Console` with a command and arguments (e.g., `nucleus ingest ./myfile.pdf`).
2.  `System.CommandLine` parses the input.
3.  The corresponding command handler is executed.
4.  The handler uses `HttpClient` (obtained via `IHttpClientFactory`) to make one or more calls to the `Nucleus.Api`.
5.  The handler receives the API response (e.g., JSON data, status codes).
6.  The handler formats and displays the results to the console (potentially using `Spectre.Console`).

**Example Command Structure (Conceptual):**

```bash
# Ingest a local file
nucleus ingest --path "./docs/my_document.pdf" [--persona GeneralAnalyst]

# Ingest content from a URI
nucleus ingest --uri "https://example.com/article.html"

# Query a specific persona
nucleus query --persona FinanceExpert "Summarize the Q1 financial report highlights."

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

While the Console App serves the MVP, the long-term vision involves integrating Nucleus OmniRAG directly into users' existing workflows via platform-specific bots and adapters.

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

## 4. Key Considerations

*   **API Contract:** The [`Nucleus.Api`](./07_ARCHITECTURE_DEPLOYMENT.md) serves as the stable contract for all clients. Changes must be managed carefully.
*   **Authentication:** The Console App might initially rely on simple API keys or local development bypasses. Production scenarios (including future bots) will require robust [authentication](./06_ARCHITECTURE_SECURITY.md) (e.g., JWT, OAuth).
*   **Configuration:** The Console App will need a mechanism (e.g., `appsettings.json`, environment variables) to know the `Nucleus.Api` base URL.

## 5. Common Adapter Patterns & Responsibilities (Phase 2+)

While implementations will vary based on the target platform, adapters should generally adhere to the following patterns and responsibilities defined in the [Adapter Interfaces document](../ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md):

*   **Initialization & Configuration:** Load necessary platform SDKs, authenticate with the platform (see [Security](./06_ARCHITECTURE_SECURITY.md)), and retrieve Nucleus [API](./07_ARCHITECTURE_DEPLOYMENT.md) endpoint/authentication details.
*   **User/Tenant Mapping:** Establish a reliable mapping between platform user/team identifiers and Nucleus user/tenant contexts (relevant to [Security](./06_ARCHITECTURE_SECURITY.md)).
*   **Event Handling:** Listen for relevant platform events (e.g., new messages, file uploads, commands).
*   **Command Parsing:** Interpret user commands or specific interaction triggers (e.g., @mentions).
*   **API Interaction:** Translate platform events and user commands into appropriate [`Nucleus.Api`](./07_ARCHITECTURE_DEPLOYMENT.md) calls.
    *   **Ingestion:** Trigger artifact [ingestion](./01_ARCHITECTURE_PROCESSING.md) via the API, potentially uploading content directly or providing URLs.
    *   **Querying:** Send user queries to the relevant [persona](./02_ARCHITECTURE_PERSONAS.md) endpoints.
    *   **Status Checks:** Allow users to query the status of ongoing [processing](./01_ARCHITECTURE_PROCESSING.md).
*   **Response Formatting:** Format Nucleus API responses into platform-native messages (e.g., Teams Adaptive Cards, Slack Blocks, plain text).
*   **Feedback Mechanisms:** Provide clear feedback to the user about command acceptance, processing status, and final results.
*   **Artifact Handling:** Manage the upload/download of artifacts between the platform storage (e.g., SharePoint, Slack files) and the Nucleus backend, respecting the defined [storage architecture](./03_ARCHITECTURE_STORAGE.md) for the platform (see specific adapter documents like [Console](../ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md) and [Teams](../ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)).
*   **Error Handling:** Gracefully handle platform-specific errors and Nucleus API errors, providing informative messages to the user.

## 6. Next Steps

1.  **Initialize `Nucleus.Console` Project:** Set up the basic project structure with required NuGet packages (`System.CommandLine`, `Microsoft.Extensions.Http`).
2.  **Configure `HttpClient`:** Use `IServiceCollection` and `IHttpClientFactory` to configure the client for accessing the [`Nucleus.Api`](./07_ARCHITECTURE_DEPLOYMENT.md) (base address, default headers if needed).
3.  **Implement Initial Commands:** Start with basic commands:
    *   `nucleus status ping`: A simple command to verify API connectivity.
    *   `nucleus ingest --path <filepath>`: Implement the file [ingestion](./01_ARCHITECTURE_PROCESSING.md) workflow.
    *   `nucleus query --persona <name> "<prompt>"`: Implement the basic [query](./02_ARCHITECTURE_PERSONAS.md) workflow.
4.  **Define API DTOs:** Ensure necessary Data Transfer Objects (DTOs) shared between the API and Console client are defined (likely in `Nucleus.Abstractions`).
5.  **Develop Command Handlers:** Write the logic within the console app to parse arguments, call the API, and display results.
6.  **(Ongoing)** Refine command structure and output formatting as API capabilities expand.

---
