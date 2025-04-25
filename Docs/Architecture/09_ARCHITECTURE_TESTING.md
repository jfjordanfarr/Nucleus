---
title: Architecture - Testing Strategy
description: Outlines the testing philosophy, levels, environments, and specific strategies for ensuring the quality and reliability of the Nucleus OmniRAG platform.
version: 2.2
date: 2025-04-25
---

[<- System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md)

# Nucleus OmniRAG: Testing Strategy

This document defines the testing strategy for the Nucleus OmniRAG platform, ensuring components function correctly individually and integrate seamlessly. It builds upon the concepts introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md), the [API-First Architecture](./10_ARCHITECTURE_API.md), and complements specific component designs like [Processing](./01_ARCHITECTURE_PROCESSING.md), [Personas](./02_ARCHITECTURE_PERSONAS.md), [Storage](./03_ARCHITECTURE_STORAGE.md), [Database](./04_ARCHITECTURE_DATABASE.md), and [Clients](./05_ARCHITECTURE_CLIENTS.md).

## 1. Testing Philosophy

Our testing philosophy emphasizes pragmatism, focusing on validating core logic and key integration points without creating excessive, brittle test suites. We favor simplicity and effectiveness, letting each testing layer shine without trying to do more than it's good at. Key tenets include:

1.  **Focus on Core Logic:** Prioritize robust unit tests for fundamental abstractions, domain logic ([`Nucleus.Domain.Processing`](../../../src/Domain/Nucleus.Domain.Processing/Nucleus.Domain.Processing.csproj)), and utility classes within [`Nucleus.Abstractions`](../../../src/Abstractions/Nucleus.Abstractions/Nucleus.Abstractions.csproj). These provide the most value for ensuring foundational stability.
2.  **Integration Confidence:** Employ integration tests to verify interactions between major components (e.g., API Service <-> Database, Processor <-> AI Services).
3.  **API-Centric E2E Validation:** Utilize **direct API calls** as the primary tool for End-to-End (E2E) testing during development and for automated checks. Test scripts (e.g., PowerShell, Python) or dedicated C# test projects using `HttpClient` provide a scriptable way to simulate client interactions and test the full flow from API request to response (or asynchronous job completion).
4.  **Adapters as Clients:** Treat client adapters ([`Nucleus.Adapters.Console`](../../../src/Adapters/Nucleus.Adapters.Console/Nucleus.Adapters.Console.csproj), [`Nucleus.Adapters.Teams`](../../../src/Adapters/Nucleus.Adapters.Teams/Nucleus.Adapters.Teams.csproj), etc.) as consumers of the API. Their E2E tests focus on their specific translation logic *and* their ability to correctly interact with the `Nucleus.Services.Api`.
5.  **Avoid Brittle UI Tests:** Given the focus on backend processing and API interactions, avoid investing heavily in complex UI automation unless a specific UI client becomes a core part of the system.
6.  **Leverage AI for Testing:** Explore opportunities to use AI (including Cascade) to generate test data, formulate complex test queries against the API, and potentially automate aspects of E2E API testing.
7.  **Documentation as Test Specification:** Treat architecture documents like this one as living specifications. Tests should validate the behaviors and interactions described herein.
8.  **Simplicity and Ergonomics:** Favor standard, potentially built-in .NET testing tools where practicable. Aim for a smaller, tighter suite of very good tests over a philosophy of broad code coverage with countless tests.

## 2. Levels of Testing

We employ standard testing levels adapted to the Nucleus API-First architecture:

### 2.1. Unit Tests

*   **Scope:** Individual classes, methods, or small clusters of tightly coupled classes.
*   **Location:** Corresponding `.Tests` projects (e.g., [`Nucleus.Domain.Processing.Tests`](../../../tests/Domain/Nucleus.Domain.Processing.Tests/Nucleus.Domain.Processing.Tests.csproj)).
*   **Purpose:** Verify logic correctness in isolation. Dependencies are typically mocked or stubbed using standard mocking techniques/libraries.
*   **Examples:**
    *   Testing parsing logic within a specific [`IContentExtractor`](./Processing/ARCHITECTURE_PROCESSING_INTERFACES.md#1-icontentextractor) implementation.
    *   Validating state transitions within an orchestration service.
    *   Testing helper/utility functions.
    *   Verifying input validation in API endpoints or command handlers (mocking underlying services).

### 2.2. Integration Tests

*   **Scope:** Interaction between multiple components, often involving external dependencies or infrastructure.
*   **Location:** Dedicated `.IntegrationTests` projects or within standard `.Tests` projects where appropriate.
*   **Purpose:** Verify that components collaborate correctly according to their contracts. May use real dependencies or test doubles (e.g., Cosmos DB Emulator, in-memory caches, Testcontainers).
*   **Examples:**
    *   Testing [`IPersonaKnowledgeRepository`](../../../src/Abstractions/Nucleus.Abstractions/Repositories/IPersonaKnowledgeRepository.cs) implementation against the Cosmos DB Emulator.
    *   Testing an API endpoint handler with its *real* service dependencies connecting to test doubles for databases/external systems.
    *   Testing the interaction between an orchestration service and a message queue.

### 2.3. End-to-End (E2E) Tests

*   **Scope:** The entire system or significant subsystems, tested from an external perspective, primarily via the [`Nucleus.Services.Api`](../../../src/Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj).
*   **Location:** Dedicated test suites (e.g., PowerShell scripts, Python scripts, C# projects using `HttpClient` or test frameworks like `Microsoft.AspNetCore.Mvc.Testing`).
*   **Purpose:** Validate complete user scenarios and interaction flows through the primary API interface. Ensure all components integrate correctly to deliver the expected outcome.
*   **Primary Tool:** Direct HTTP calls to the API service endpoints.
*   **Important:** E2E tests **must** interact with the system *through* the defined `Nucleus.Services.Api` endpoints. They should **not** bypass the API to call internal services directly, as this would invalidate the purpose of testing the integrated system via its public contract.
*   **Examples:**
    *   Sending an 'ingest' request to the API (`/api/v1/interactions/ingest`) with sample data and verifying:
        *   Successful API response (e.g., HTTP 202 Accepted with a `jobId` for async).
        *   Correct `ArtifactMetadata` created in the database.
        *   (For async) Ability to poll a status endpoint (`/api/v1/jobs/{jobId}`) and eventually see completion.
    *   Sending a 'query' request to the API (`/api/v1/interactions/query`) and verifying:
        *   Correct context retrieval (potentially verified via logs or intermediate state).
        *   Plausible AI-generated response returned synchronously or via job completion.
    *   *Using Client Adapters:* Running an adapter (like `Nucleus.Adapters.Console.exe ingest <file>`) and verifying the *adapter's* output and the resulting *API interaction's* side effects (database entries, etc.). This tests the adapter *and* the API flow it triggers.

## 3. Testing Environments

*   **Local Development:** Developers run unit and integration tests frequently. E2E tests target local instances of services (often via [.NET Aspire AppHost](../../../Nucleus.AppHost/Nucleus.AppHost.csproj)).
*   **CI/CD Pipelines:** Automated test runners execute all test levels, prioritizing API endpoint validation. E2E tests may use ephemeral environments or containers to spin up the API and dependencies.
*   **Staging/Production:** Smoke tests and health checks target the deployed API endpoints.

## 4. Testing Specific Components & Flows

### 4.1. Content Extraction & Synthesis (`IContentExtractor`, Synthesizers)

*   **Unit Tests:** Mock input streams/file contents and verify the extractor produces the expected raw text or intermediate structure.
*   **Integration Tests:** Test extractors against real sample files (PDF, DOCX, etc.) stored in the test project. Verify correct text extraction.

### 4.2. Knowledge Repository

*   **Unit Tests:** Test repository logic with in-memory or mocked stores.
*   **Integration Tests:** Perform CRUD operations against the Cosmos DB Emulator. Verify data consistency, indexing (vector search), and query correctness. Test repository methods thoroughly.

### 4.3. Orchestration Logic

*   **Unit Tests:** Test orchestration and routing logic in isolation. Mock downstream services.
*   **Integration Tests:** Test end-to-end orchestration via API endpoints using real or test double dependencies.

### 4.4. API Service (`Nucleus.Services.Api`)

*   **Unit Tests:** Test individual endpoint handlers/Minimal API delegates. Mock service dependencies. Verify input validation, correct service calls, and response mapping.
*   **Integration Tests:** Test endpoint logic with real dependencies (e.g., database, queue) in a controlled environment.
*   **E2E Tests:** Directly exercise the API endpoints using scripts or C# test projects. Validate ingestion, querying, and async job flows. See Section 5.

### 4.5. Client Adapters (e.g., Console, Teams)

*   **Role:** Adapters are pure **API clients**. Their primary responsibility is translation: Platform Event -> `AdapterRequest`, and `AdapterResponse` -> Platform Response.
*   **Unit Tests:** Verify the translation logic within the adapter: does it correctly parse platform-specific input (like a Teams `Activity` or console arguments) and construct the appropriate `AdapterRequest`? Does it correctly interpret an `AdapterResponse` (or `OrchestrationResult`) and format it for the platform?
*   **Integration Tests:** These tests primarily focus on the adapter's interaction with the **Nucleus API**. They should mock the *platform* side (e.g., simulate an incoming Teams message) and use tools like `HttpClient` to send the resulting `AdapterRequest` to a running instance of the `Nucleus.Services.Api` (potentially with mocked downstream dependencies like AI services or databases). The tests then verify that the adapter correctly handles the API's response.
*   **Refactoring `Console.Tests`:** The existing [`Nucleus.Adapters.Console.Tests`](../../../tests/Adapters/Nucleus.Adapters.Console.Tests/Nucleus.Adapters.Console.Tests.csproj) project will be refactored. Instead of testing the old console application logic, it will become an **API Integration Test Suite**, using `HttpClient` to directly call the `Nucleus.Services.Api` endpoints, simulating various scenarios.

## 5. API End-to-End Testing Workflow

The primary E2E validation mechanism is direct interaction with the `Nucleus.Services.Api` via HTTP. This can be accomplished using PowerShell scripts, Python scripts, or C# test projects. The Console Adapter may be used as a reference client, but its role is now illustrative rather than central.

```mermaid
graph TD
    subgraph Test Environment
        A[Test Runner (e.g., PowerShell Script, C# Test, Python Script)]
        B[Running Nucleus Stack (AppHost or Direct Services)]
        C[Sample Input Files]
        D[Cosmos DB Emulator]
        E[Configured AI Services (or Mocks)]
    end

    subgraph Test Execution
        A -- Sends HTTP Request --> F["Nucleus.Services.Api Endpoint"]
        F -- Interacts --> B
        B -- Interacts --> D & E
    end

    subgraph Verification
        A -- Asserts --> G["API Response (e.g., AI response, status, jobId)"]
        A -- Asserts --> H["Database State (e.g., PersonaKnowledgeEntry existence/content)"]
        A -- Asserts --> I["File System State (e.g., ephemeral file cleanup)"]
    end
```

**Steps:**

1.  **Setup:** Start the Nucleus backend (AppHost recommended). Ensure Cosmos DB Emulator is running and AI services are configured (or mocked appropriately for specific tests). Prepare sample input files.
2.  **Execute:** The test runner sends HTTP requests to specific API endpoints (e.g., `/api/interaction/ingest`, `/api/interaction/query`, `/api/interaction/dataviz`) with appropriate payloads. This can be done using:
    *   **PowerShell Scripts:** Like the updated [`test_ingest_agent_api.ps1`](../../../tests/Adapters/Nucleus.Adapters.Console.Tests/test_ingest_agent_api.ps1) and [`test_query_agent_api.ps1`](../../../tests/Adapters/Nucleus.Adapters.Console.Tests/test_query_agent_api.ps1).
    *   **C# Test Projects:** Using `HttpClient` (as planned for the refactored `Console.Tests`).
    *   Other tools (e.g., Postman, Python scripts).
    The original Console Adapter application can still be run manually as a reference client, but automated E2E testing focuses on direct API calls.
3.  **Verify:**
    *   **API Response:** Assert expected status codes, response bodies (e.g., direct `AdapterResponse` for sync, `OrchestrationResult` with `jobId` for async).
    *   **Database State:** Connect to the Cosmos DB Emulator and verify that expected `ArtifactMetadata` and `PersonaKnowledgeEntry` documents were created/updated correctly. Check embedding values if necessary.
    *   **(Optional) File System/Logs:** Check if temporary files were cleaned up, examine service logs for errors.

## 6. Testing AI Integration (`IChatClient`, `IEmbeddingGenerator`)

Verifying integration with external AI services is critical:

1.  **Configuration:** Ensure API keys/endpoints are correctly loaded (e.g., via `secrets.json` for local testing).
2.  **Connectivity:** Simple integration tests that make a basic call (e.g., generate embedding for "test", get a simple chat completion) to verify authentication and basic API reachability.
3.  **E2E Validation:** Use API endpoint E2E tests (Section 5). Provide input requiring AI analysis (e.g., summarization, sentiment analysis) and assert that a plausible AI-generated response is returned. This implicitly tests both `IEmbeddingGenerator` (for retrieval context) and `IChatClient` (for final response generation).
4.  **Monitoring/Logging:** Ensure adequate logging around AI service calls to diagnose failures during testing or in production.

## 7. Future Considerations (Phase 2+)

*   **UI Testing:** Strategies for testing graphical clients (Web, Desktop, Mobile) will be needed.
*   **Platform Adapter Testing:** Testing interactions with specific platforms (Teams Bot Framework, Email servers) will require specialized tools and potentially sandboxed environments.
*   **Performance Testing:** Measuring API response times, processing throughput under load.
*   **Security Testing:** Penetration testing, vulnerability scanning.
*   **Chaos Testing:** Injecting failures to test resilience.

This testing strategy provides a framework for ensuring Nucleus OmniRAG quality. It prioritizes validating core functionality and integration points using pragmatic approaches, with direct API testing as the backbone and the Console Adapter as a reference client for adapter-specific flows.
