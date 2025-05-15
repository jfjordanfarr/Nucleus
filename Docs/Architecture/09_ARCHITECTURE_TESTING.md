---
title: Architecture - Testing Strategy
description: Outlines the testing philosophy, levels, environments, and specific strategies for ensuring the quality and reliability of the Nucleus platform, adopting a layered approach centered around .NET Aspire.
version: 3.3
date: 2025-05-15
---

[<- System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md)

# Nucleus: Testing Strategy

**Version:** 3.3
**Date:** 2025-05-15

This document outlines the multi-layered testing strategy for Nucleus, designed to ensure quality and reliability by leveraging the best tools for different validation needs within the .NET Aspire ecosystem.

## 1. Testing Philosophy

Our testing philosophy emphasizes pragmatism, focusing on validating core logic and key integration points without creating excessive, brittle test suites. We favor simplicity and effectiveness, letting each testing layer shine without trying to do more than it's good at. Key tenets include:

1.  **Focus on Core Logic:** Prioritize robust unit tests for fundamental abstractions, domain logic ([`Nucleus.Domain.Processing`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/Nucleus.Domain.Processing.csproj)), and utility classes within [`Nucleus.Abstractions`](../../../src/Nucleus.Abstractions/Nucleus.Abstractions.csproj). These provide the most value for ensuring foundational stability.
2.  **Component Interaction Confidence:** Employ *service integration tests* (Layer 2) primarily for verifying direct interactions between collaborating *components* within a service or closely related services, potentially using `WebApplicationFactory` or similar techniques *without* full AppHost orchestration, often mocking external dependencies.
3.  **System Integration Confidence (Primary Focus):** Utilize **.NET Aspire's testing support (`Aspire.Hosting.Testing`)** as the primary tool for System Integration testing (Layer 3). This approach orchestrates the *entire relevant subset* of the application defined in the [`Nucleus.AppHost`](../../../Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj), including services and their dependencies (like **emulated Cosmos DB and Service Bus**). Tests interact with the system primarily via **direct API calls** to the orchestrated API service.
    *   **Implementation Example:** [`ApiIntegrationTests.cs`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/ApiIntegrationTests.cs)
4.  **Adapters:** 
    *   External client adapters (e.g., [`Nucleus.Adapters.Teams`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj)) are treated as consumers of the API. Their integration tests focus on their specific translation logic *and* their ability to correctly interact with the `Nucleus.Services.Api` (potentially within the Layer 3 AppHost environment).
    *   Internal adapters like [`Nucleus.Infrastructure.Adapters.Local`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Infrastructure.Adapters.Local/Nucleus.Infrastructure.Adapters.Local.csproj) are generally tested as integrated components within the services that use them, or via focused unit/integration tests if they contain complex, separable logic.
5.  **Avoid Brittle UI Tests:** Given the focus on backend processing and API interactions, avoid investing heavily in complex UI automation unless a specific UI client becomes a core part of the system.
6.  **Leverage AI for Testing:** Explore opportunities to use AI (including Cascade) to generate test data, formulate complex test queries against the API, and potentially automate aspects of Layer 3 system testing by interacting with the API endpoints.
7.  **Documentation as Test Specification:** Treat architecture documents like this one as living specifications. Tests should validate the behaviors and interactions described herein.
8.  **Security and Data Governance Validation:** Actively design tests, particularly at the repository and integration layers, to prove that the data access controls, tenant isolation, and persona-specific data boundaries outlined in the [Security Architecture & Data Governance](./06_ARCHITECTURE_SECURITY.md) document are strictly enforced. This includes verifying that personas cannot access data outside their configured scope and that tenant data remains isolated.
9.  **Simplicity and Ergonomics:** Favor standard, potentially built-in .NET testing tools where practicable. Aim for a smaller, tighter suite of very good tests over a philosophy of broad code coverage with countless tests.

## 2. Testing Layers & Tools

We adopt a layered testing strategy, recognizing that different types of validation require different tools and approaches. Our primary test framework across layers is **xUnit.net**, chosen for its modern design, popularity, and strong integration with the .NET ecosystem.

To support these layers, especially Layers 1 and 2, we utilize shared test infrastructure. The [`Nucleus.Infrastructure.Testing`](../../../tests/Infrastructure.Testing) project, detailed in [NAMESPACE_INFRASTRUCTURE_TESTING.md](./Namespaces/NAMESPACE_INFRASTRUCTURE_TESTING.md), provides common test doubles like in-memory repositories (e.g., `InMemoryArtifactMetadataRepository`), fake queue implementations (`InMemoryBackgroundTaskQueue`), and mock configuration providers. This helps create consistent and controlled test environments, distinct from the domain-specific unit tests found in `Nucleus.Domain.Tests`.

### 2.1. Layer 1: Unit Tests

*   **Scope:** Individual classes, methods, and logic units within a single project/service, tested in isolation.
*   **Purpose:** Verify the correctness of specific algorithms, state transitions, utility functions, and input validation. Dependencies are typically mocked using utilities from `Nucleus.Infrastructure.Testing` or standard mocking libraries.
*   **Location:** Test projects colocated or logically grouped with the source project (e.g., `Nucleus.Domain.Processing.Tests`, whose structure and purpose are further defined in [NAMESPACE_DOMAIN_TESTS.md](./Namespaces/NAMESPACE_DOMAIN_TESTS.md)).
*   **Tools:** **xUnit.net**, Mocking libraries (e.g., `Moq`, `NSubstitute`), and test doubles from [`Nucleus.Infrastructure.Testing`](../../../tests/Infrastructure.Testing).

### 2.2. Layer 2: Service Integration Tests

*   **Scope:** Interaction between components *within* a single service or between tightly coupled services, often focusing on infrastructure concerns like repository logic or specific middleware behavior, *without* full AppHost orchestration.
*   **Purpose:** Verify that components within a service collaborate correctly, or that a service interacts correctly with a *specific* infrastructure component (e.g., database schema mapping), often using techniques like `WebApplicationFactory` (for API projects) or direct instantiation with real or in-memory dependencies.
*   **Location:** Dedicated integration test projects (e.g., potentially parts of [`Nucleus.Services.Api.IntegrationTests`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj), although this project now primarily focuses on Layer 3).
*   **Tools:**
    *   **xUnit.net** as the test runner.
    *   `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>` for in-memory testing of ASP.NET Core services.
    *   Potentially **Testcontainers** for managing individual external dependencies if not using AppHost orchestration (less common now).
    *   In-memory databases or fakes (e.g., EF Core In-Memory Provider, although less realistic than emulators).

### 2.3. Layer 3: System Integration Tests (.NET Aspire AppHost)

*   **Scope:** Testing the interactions between multiple services and their *real* dependencies (or high-fidelity emulators) as orchestrated by the .NET Aspire **AppHost**.
*   **Purpose:** Verify end-to-end flows through the system, starting from an API call and potentially involving multiple services, databases (via **Cosmos DB Emulator**), and message queues (via **Azure Service Bus Emulator**). This layer provides the highest confidence in the integrated system's behavior during development.
*   **Key Technology:** **`Aspire.Hosting.Testing`** NuGet package.
*   **Emulator Versions:** Requires .NET Aspire 9.1+ for Service Bus Emulator support and 9.2+ for Cosmos DB Data Explorer visualization (though the emulator works in earlier 9.x versions).
*   **Location:** Dedicated integration test projects designed to orchestrate the AppHost, such as [`Nucleus.Services.Api.IntegrationTests`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj).
*   **Tools:**
    *   **xUnit.net** (implementing `IAsyncLifetime` for setup/teardown).
    *   **`DistributedApplicationTestingBuilder`** to build and run the `Nucleus.AppHost`.
    *   **`DistributedApplication.ResourceNotificationService`** to wait for resources (services, emulators) to reach a running state.
    *   **`DistributedApplication.CreateHttpClient()`** to get an `HttpClient` configured to talk to a specific service in the AppHost.
    *   **`DistributedApplication.Services.GetRequiredService<TProject>()`** to access the `IServiceProvider` of a running service (allowing resolution of repositories, etc., for verification).
    *   **Azure SDK Clients** (e.g., `CosmosClient`, `ServiceBusClient`) configured automatically by Aspire to connect to the emulators.

### 2.4. Layer 4: End-to-End (E2E) UI Tests (Future)

*   **Scope:** Testing the complete system from the perspective of an end-user interacting with a specific UI (e.g., Web App, Teams App).
*   **Purpose:** Verify that user journeys are functioning correctly through the UI, interacting with the backend API.
*   **Location:** Separate E2E test project(s).
*   **Tools:** UI automation frameworks (e.g., **Playwright**, Selenium).
*   **Status:** Future consideration, only if a dedicated UI becomes a core component.

## 3. Testing Environments

*   **Local Development:** All layers (Unit, Service Integration, System Integration via Aspire AppHost with emulators) are run locally by developers and in CI pre-merge checks.
*   **CI/CD Pipeline:** Automated execution of Unit, Service Integration, and System Integration tests.
*   **Staging/Production:** Smoke tests and health checks target deployed API endpoints.

## 4. Testing Specific Components & Flows

This section maps components to the primary testing layers:

*   **Domain Logic (`Nucleus.Domain.*`)**: Primarily **Unit Tests** (Layer 1).
*   **Repositories (`Nucleus.Infrastructure.Data.*`)**: Can be tested via **Service Integration Tests** (Layer 2, perhaps against an in-memory DB or Testcontainer) *and* implicitly via **System Integration Tests** (Layer 3, against the AppHost-managed emulator).
*   **API Controllers (`Nucleus.Services.Api`)**: Primarily **System Integration Tests** (Layer 3) making HTTP calls via `Aspire.Hosting.Testing`.
*   **Background Services (`QueuedInteractionProcessorService`)**: Tested implicitly via **System Integration Tests** (Layer 3) by queuing work (via API call) and verifying the expected side effects (e.g., data persisted, external calls made).
*   **Client Adapters (`Nucleus.Adapters.*`)**: Adapter-specific logic via **Unit Tests** (Layer 1). Full interaction with the API via **System Integration Tests** (Layer 3), potentially running the adapter within the AppHost if feasible, or by simulating adapter calls to the API endpoints.

## 5. System Integration Testing Workflow (Layer 3 Focus)

This describes the typical workflow for tests using `Aspire.Hosting.Testing`:

1.  **Setup (`InitializeAsync`):**
    *   Use `DistributedApplicationTestingBuilder.CreateAsync<TAppHostProject>()` to get a builder for the AppHost.
    *   Use `builder.BuildAsync()` to create the `DistributedApplication` instance.
    *   Start the application using `await app.StartAsync()`.
    *   Obtain `HttpClient` instances for services using `app.CreateHttpClient("serviceName")`.
    *   Resolve necessary dependency instances (e.g., database contexts connected to emulators) from the AppHost's service provider if needed for verification (`app.Services.GetRequiredService<T>()`).
2.  **Execute (Test Method):**
    *   Use the obtained `HttpClient` to send requests to the API service endpoints (`POST /api/v1/interactions`, `GET /health`, etc.).
3.  **Verify (Test Method):**
    *   **Initial API Response:** For asynchronous operations like `POST /api/v1/interactions`, assert the immediate API response is `HTTP 202 Accepted` and potentially validate the returned `jobId` format.
    *   **Asynchronous Outcome:** Verify the expected *result* of the background processing:
        *   Check the state of the database emulator (e.g., `CosmosClient` connected to the emulator) to confirm expected data was created/modified.
        *   *(Optional)* Check the state of the message queue emulator (Service Bus) if verifying specific queuing behaviors.
        *   *(Optional)* Implement polling logic within the test against the API's job status/result endpoints (`GET /api/v1/interactions/{jobId}/status`, `GET /api/v1/interactions/{jobId}/result`) to wait for completion and assert the final result DTO.
    *   **Other API Responses:** For synchronous endpoints (like `/health`), assert the expected status code (e.g., `HTTP 200 OK`) and content directly.
4.  **Teardown (`DisposeAsync`):**
    *   Call `app.DisposeAsync()` to gracefully shut down the AppHost and all its resources.

```mermaid
graph TD
    subgraph Test Project (e.g., ApiIntegrationTests)
        A[xUnit Test Runner]
        B(Aspire.Hosting.Testing Infrastructure)
        H[HttpClient]
        K[Resolved Dependencies e.g., IRepository]
    end

    subgraph Orchestrated by Aspire AppHost
        C[AppHost Process]
        D[API Service Process]
        E[Worker Service Process (Optional)]
        F[Cosmos DB Emulator]
        G[Service Bus Emulator]
        I[ResourceNotifier]
        J[Service Discovery]
    end

    subgraph Test Setup (InitializeAsync)
        A -- Uses --> B
        B -- Builds & Starts --> C
        C -- Manages --> D
        C -- Manages --> E
        C -- Manages --> F
        C -- Manages --> G
        C -- Manages --> I
        C -- Manages --> J
        B -- Waits via --> I
        B -- Gets Client via --> J
        B -- Resolves Dependencies --> D
        A -- Receives --> H
        A -- Receives --> K
    end

    subgraph Test Execution & Verification
        A -- Uses --> H
        H -- HTTP Request --> D
        D -- Interacts --> F
        D -- Interacts --> G
        D -- Interacts --> E
        A -- Uses --> K
        K -- Verifies State --> F
        A -- Asserts --> L[API Response]
        A -- Asserts --> M[Emulator DB State]
    end

    subgraph Test Teardown (DisposeAsync)
        A -- Calls Dispose --> B
        B -- Stops --> C
    end
```

## 6. Testing AI Integration (`IChatClient`, `IEmbeddingGenerator`)

Verifying integration with external AI services within the Aspire testing model:

1.  **Configuration:** Ensure API keys/endpoints for *real* AI services are correctly loaded via user secrets (`secrets.json`), which the AppHost automatically incorporates for services during development/testing.
2.  **Connectivity (Optional Layer 2):** Simple service integration tests (Layer 2) could directly instantiate clients (`IChatClient`) with configured credentials and make basic calls to verify authentication/reachability without full system orchestration.
3.  **E2E Validation (Layer 3):** Use System Integration tests (Section 5). Provide input via API calls that requires AI analysis (e.g., summarize an artifact). Assert that a plausible AI-generated response is returned or that expected data derived from AI processing (e.g., embeddings stored in metadata) is present. This implicitly tests the AI client integration within the context of the running service.
4.  **Monitoring/Logging:** Ensure adequate logging around AI service calls within the API/processing services to diagnose failures during test runs.

## 7. Future Considerations

*   **E2E UI Testing:** Implement Layer 4 using **Playwright** if/when a web UI is added.
*   **Refined Service Integration (Layer 2):** Further clarify the role and scope of Layer 2 tests now that Layer 3 handles full system orchestration with emulators.
*   **Observability in Tests:** Leverage Aspire's integrated logging/tracing/metrics within test runs (accessible via the Aspire Dashboard during test execution) for better diagnostics.
*   **Architecture Testing:** Consider adding tests using **NetArchTest** to enforce architectural rules.
*   **Performance Testing:** Measuring API response times, processing throughput under load (potentially leveraging Layer 3 setup).
*   **Security Testing:** Penetration testing, vulnerability scanning.
*   **Chaos Testing:** Injecting failures into the AppHost-managed resources to test resilience.

This layered testing strategy provides a robust framework for ensuring Nucleus quality by leveraging the strengths of different tools, centered around the orchestration capabilities of .NET Aspire.
