title: "Requirements: MVP - Core API & Initial Console Client"
description: Minimum requirements for the Nucleus OmniRAG core API service and its validation via an initial Console Application client.
version: 1.3
date: 2025-04-22

# Requirements: MVP - Core API & Initial Console Client

**Version:** 1.3
**Date:** 2025-04-22

## 1. Goal

To establish and validate the core backend architecture (`Nucleus.ApiService`) for Nucleus OmniRAG, including persona integration (`BootstrapperPersona`) and basic data flow (query, simple ingestion placeholder). This MVP utilizes a **Console Application (`Nucleus.Console`)** as the **initial reference client** to interact with and test the API, prioritizing development velocity and synergy with agentic development workflows. The local development environment using **.NET Aspire** with emulated services remains critical.

See also:
*   [Project Mandate](00_PROJECT_MANDATE.md)
*   [API Architecture Overview](../../Architecture/Api/ARCHITECTURE_API_OVERVIEW.md)
*   [Testing Strategy](../../Architecture/09_ARCHITECTURE_TESTING.md)

## 2. Scope

*   **Core Deliverable:** `Nucleus.ApiService` ASP.NET Core Web API providing foundational endpoints.
*   **Initial Client:** `Nucleus.Console` .NET Console Application used for testing and validation.
*   **Interaction:** API endpoints for querying and basic ingestion triggering, consumed initially by the Console App CLI.
*   **Backend:** `Nucleus.ApiService` ASP.NET Core Web API providing endpoints for the Console App.
*   **Processing:** Basic query handling by a single `BootstrapperPersona`. Placeholder for ingestion flow.
*   **Data Storage:** Basic storage for persona knowledge entries (Cosmos DB emulator).
*   **Environment:** Local development using .NET Aspire, orchestrating the Console App, API, and the essential emulated Azure service (Cosmos DB). Processing uses ephemeral container storage, not external blobs.

## 3. Requirements

### 3.1. Local Development Environment (Aspire)

*   **REQ-MVP-ENV-001:** The `.NET Aspire` AppHost (`Nucleus.AppHost`) MUST successfully launch and orchestrate the following essential components:
    *   `Nucleus.Console` project.
    *   `Nucleus.ApiService` project.
    *   Azure Cosmos DB Emulator container.
*   **REQ-MVP-ENV-002:** Aspire MUST correctly inject necessary connection strings and service discovery information (e.g., API base URL, Cosmos connection) into both `Nucleus.ApiService` and `Nucleus.Console` via configuration/environment variables.
*   **REQ-MVP-ENV-003:** Developers MUST be able to run the entire MVP stack locally using a single command (e.g., `dotnet run` in the AppHost directory).

### 3.2. Admin Experience (Configuration)

*   **REQ-MVP-ADM-001:** An Administrator (developer during MVP) MUST be able to configure necessary connection details/credentials for external services not managed by Aspire emulators, primarily:
    *   Configured AI Model Provider API Key and Endpoint (e.g., Google Gemini). **(PARTIAL - Key configured; endpoint implicit in SDK)**
*   **REQ-MVP-ADM-002:** Configuration MUST be manageable through standard .NET mechanisms (e.g., `appsettings.json`, user secrets, environment variables). **(COMPLETE)**

### 3.3. API Consumption (Example: Console Application `Nucleus.Console`)

*   **REQ-MVP-CON-001:** An initial client (e.g., `Nucleus.Console`) MUST provide a clear interface for invoking API endpoints (e.g., using `System.CommandLine`).
*   **REQ-MVP-CON-002:** The client MUST be able to invoke the `/api/query` endpoint:
    *   Sends user query text (e.g., via a `query` command like `nucleus query "What is the capital of France?"`).
    *   Displays the response received from the API clearly formatted.
*   **REQ-MVP-CON-003:** The client MUST be able to invoke the placeholder `/api/ingest` endpoint:
    *   Sends a file path or identifier (e.g., via an `ingest` command like `nucleus ingest ./myfile.txt`).
    *   Displays a confirmation message based on the API response (e.g., "Ingestion request acknowledged for ./myfile.txt").
*   **REQ-MVP-CON-004:** The client MUST be able to invoke the placeholder `/api/status` endpoint:
    *   (E.g., via a `status` command).
    *   Displays basic status information received from the API.
*   **REQ-MVP-CON-005:** The client MUST handle and display errors gracefully if API calls fail (e.g., connection errors, API error responses).
*   **REQ-MVP-CON-006:** The client MUST correctly use the API base address provided by Aspire configuration.

### 3.4. Backend API (`Nucleus.ApiService`)

*   **REQ-MVP-API-001:** The API MUST expose a `/api/query` endpoint (e.g., `POST /api/v1/query`). **(COMPLETE)**
    *   Accepts a query object (e.g., containing query text).
    *   Injects and calls the `HandleQueryAsync` method of the appropriate registered Persona (initially `BootstrapperPersona`).
    *   Returns the persona's response.
*   **REQ-MVP-API-002:** The API MUST expose a placeholder `/api/ingest` endpoint (e.g., `POST /api/v1/ingest`).
    *   Accepts ingestion request details (e.g., file path/identifier, potentially user context).
    *   Logs the request or performs minimal validation for MVP.
    *   Returns a success/acknowledgement response.
*   **REQ-MVP-API-003:** The API MUST expose a placeholder `/api/status` endpoint (e.g., `GET /api/v1/status`).
    *   Returns basic status information (e.g., API health, loaded personas).
*   **REQ-MVP-API-004:** The API MUST correctly register and inject the `BootstrapperPersona` and other necessary services (Logging, Configuration, `IPersonaKnowledgeRepository`, `IEmbeddingGenerator`). **(PARTIAL - Persona, Logging, Config injected. Repository/Embeddings TBD)**
*   **REQ-MVP-API-005:** The API MUST correctly use connection strings/configurations provided by Aspire/environment for accessing emulated/external services (Cosmos DB, AI Provider). **(PARTIAL - AI Provider config used. Cosmos DB TBD)**
*   **REQ-MVP-API-006:** The API MUST include basic health checks (`/healthz`).

### 3.5. System Behavior (Core Logic & Processing)

*   **REQ-MVP-SYS-001:** The `BootstrapperPersona` MUST implement `HandleQueryAsync` to: **(COMPLETE)**
    *   Receive the query text from the API.
    *   Interact with the configured AI Model (via `IGenerativeAI`) to generate a response.
    *   Return the generated response.
*   **REQ-MVP-SYS-002:** (Stretch Goal/Minimal) After handling a query, the system SHOULD:
    *   Generate embeddings for the query and/or response (`IEmbeddingGenerator`).
    *   Store a basic `PersonaKnowledgeEntry` record in the Cosmos DB emulator (`IPersonaKnowledgeRepository`) capturing the interaction.
*   **REQ-MVP-SYS-003:** The system MUST implement basic `IPersonaKnowledgeRepository` using the Cosmos DB .NET SDK against the emulator.
*   **REQ-MVP-SYS-004:** The system MUST implement a basic `IEmbeddingGenerator` using the configured AI provider's SDK.
*   **REQ-MVP-SYS-005:** Error handling MUST be implemented in the API and Persona logic to catch common failures (e.g., AI API errors, database connection issues) and return appropriate error responses to the Console App.
