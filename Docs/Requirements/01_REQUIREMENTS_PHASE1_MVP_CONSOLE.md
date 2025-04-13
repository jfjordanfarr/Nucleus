title: "Requirements: MVP - Console Application Interaction"
description: Minimum requirements for the initial Console Application interaction model for Nucleus OmniRAG, focusing on rapid iteration.
version: 1.0
date: 2025-04-11

# Requirements: MVP - Console Application Interaction

**Version:** 1.0
**Date:** 2025-04-11

## 1. Goal

To establish the core interaction loop for Nucleus OmniRAG using a **Console Application (`Nucleus.Console`)** as the primary client. This MVP focuses on validating the fundamental backend architecture (`Nucleus.Api`), persona integration (`BootstrapperPersona`), and basic data flow (query, simple ingestion placeholder) in a way that **maximizes development velocity and synergy with agentic development workflows**. The local development environment using **.NET Aspire** with emulated services is critical.

## 2. Scope

*   **Client:** `Nucleus.Console` .NET Console Application.
*   **Interaction:** Command-line interface (CLI) for querying and basic ingestion triggering.
*   **Backend:** `Nucleus.Api` ASP.NET Core Web API providing endpoints for the Console App.
*   **Processing:** Basic query handling by a single `BootstrapperPersona`. Placeholder for ingestion flow.
*   **Data Storage:** Basic storage for persona knowledge entries (Cosmos DB emulator).
*   **Environment:** Local development using .NET Aspire, orchestrating the Console App, API, and emulated Azure services (Cosmos DB, Azurite, Service Bus - though queue usage is minimal in this phase).

## 3. Requirements

### 3.1. Local Development Environment (Aspire)

*   **REQ-MVP-ENV-001:** The `.NET Aspire` AppHost (`Nucleus.AppHost`) MUST successfully launch and orchestrate the following:
    *   `Nucleus.Console` project.
    *   `Nucleus.Api` project.
    *   Azure Cosmos DB Emulator container.
    *   Azurite Storage Emulator container (for Blobs/Queues, even if not heavily used initially).
    *   Service Bus Emulator container (even if not heavily used initially).
*   **REQ-MVP-ENV-002:** Aspire MUST correctly inject necessary connection strings and service discovery information (e.g., API base URL) into both `Nucleus.Api` and `Nucleus.Console` via configuration/environment variables.
*   **REQ-MVP-ENV-003:** Developers MUST be able to run the entire MVP stack locally using a single command (e.g., `dotnet run` in the AppHost directory).

### 3.2. Admin Experience (Configuration)

*   **REQ-MVP-ADM-001:** An Administrator (developer during MVP) MUST be able to configure necessary connection details/credentials for external services not managed by Aspire emulators, primarily:
    *   Configured AI Model Provider API Key and Endpoint (e.g., Google Gemini).
*   **REQ-MVP-ADM-002:** Configuration MUST be manageable through standard .NET mechanisms (e.g., `appsettings.json`, user secrets, environment variables).

### 3.3. Console Application (`Nucleus.Console`)

*   **REQ-MVP-CON-001:** The Console App MUST provide a clear command-line interface (using `System.CommandLine` or similar).
*   **REQ-MVP-CON-002:** The Console App MUST support a `query` command:
    *   Accepts user query text as an argument (e.g., `nucleus query "What is the capital of France?"`).
    *   Calls the `/api/query` endpoint on `Nucleus.Api`.
    *   Displays the response received from the API clearly formatted on the console.
*   **REQ-MVP-CON-003:** The Console App MUST support an `ingest` command (placeholder functionality for MVP):
    *   Accepts a file path or identifier as an argument (e.g., `nucleus ingest ./myfile.txt`).
    *   Calls a placeholder `/api/ingest` endpoint on `Nucleus.Api` (The API might just log receipt for MVP).
    *   Displays a confirmation message on the console (e.g., "Ingestion request sent for ./myfile.txt").
*   **REQ-MVP-CON-004:** The Console App MUST support a `status` command (placeholder functionality for MVP):
    *   Calls a placeholder `/api/status` endpoint on `Nucleus.Api`.
    *   Displays basic status information received from the API (e.g., "API reachable. Bootstrapper Persona loaded.").
*   **REQ-MVP-CON-005:** The Console App MUST handle and display errors gracefully if API calls fail (e.g., connection errors, API error responses).
*   **REQ-MVP-CON-006:** The Console App MUST correctly use the API base address provided by Aspire configuration.

### 3.4. Backend API (`Nucleus.Api`)

*   **REQ-MVP-API-001:** The API MUST expose a `/api/query` endpoint (e.g., `POST`).
    *   Accepts a query object (e.g., containing query text).
    *   Injects and calls the `HandleQueryAsync` method of the registered `BootstrapperPersona`.
    *   Returns the persona's response.
*   **REQ-MVP-API-002:** The API MUST expose a placeholder `/api/ingest` endpoint (e.g., `POST`).
    *   Accepts ingestion request details (e.g., file path/identifier).
    *   Logs the request or performs minimal validation for MVP.
    *   Returns a success/acknowledgement response.
*   **REQ-MVP-API-003:** The API MUST expose a placeholder `/api/status` endpoint (e.g., `GET`).
    *   Returns basic status information (e.g., API health, loaded personas).
*   **REQ-MVP-API-004:** The API MUST correctly register and inject the `BootstrapperPersona` and other necessary services (Logging, Configuration, `IPersonaKnowledgeRepository`, `IEmbeddingGenerator`).
*   **REQ-MVP-API-005:** The API MUST correctly use connection strings/configurations provided by Aspire/environment for accessing emulated/external services (Cosmos DB, AI Provider).
*   **REQ-MVP-API-006:** The API MUST include basic health checks (`/healthz`).

### 3.5. System Behavior (Core Logic & Processing)

*   **REQ-MVP-SYS-001:** The `BootstrapperPersona` MUST implement `HandleQueryAsync` to:
    *   Receive the query text from the API.
    *   Interact with the configured AI Model (via `IChatCompletionService` or similar) to generate a response.
    *   Return the generated response.
*   **REQ-MVP-SYS-002:** (Stretch Goal/Minimal) After handling a query, the system SHOULD:
    *   Generate embeddings for the query and/or response (`IEmbeddingGenerator`).
    *   Store a basic `PersonaKnowledgeEntry` record in the Cosmos DB emulator (`IPersonaKnowledgeRepository`) capturing the interaction.
*   **REQ-MVP-SYS-003:** The system MUST implement basic `IPersonaKnowledgeRepository` using the Cosmos DB .NET SDK against the emulator.
*   **REQ-MVP-SYS-004:** The system MUST implement a basic `IEmbeddingGenerator` using the configured AI provider's SDK.
*   **REQ-MVP-SYS-005:** Error handling MUST be implemented in the API and Persona logic to catch common failures (e.g., AI API errors, database connection issues) and return appropriate error responses to the Console App.
