---
title: Architecture - Testing Strategy
description: Outlines the testing philosophy, levels, environments, and specific strategies for ensuring the quality and reliability of the Nucleus OmniRAG platform, adopting a layered approach.
version: 2.8
date: 2025-04-29
---

[<- System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md)

# Nucleus OmniRAG: Testing Strategy

**Version:** 2.8
**Date:** 2025-04-29

This document outlines the multi-layered testing strategy for Nucleus OmniRAG, designed to ensure quality and reliability by leveraging the best tools for different validation needs within the .NET Aspire ecosystem.

## 1. Testing Philosophy

Our testing philosophy emphasizes pragmatism, focusing on validating core logic and key integration points without creating excessive, brittle test suites. We favor simplicity and effectiveness, letting each testing layer shine without trying to do more than it's good at. Key tenets include:

1.  **Focus on Core Logic:** Prioritize robust unit tests for fundamental abstractions, domain logic ([`Nucleus.Domain.Processing`](../../../src/Nucleus.Domain/Nucleus.Domain.Processing/Nucleus.Domain.Processing.csproj)), and utility classes within [`Nucleus.Abstractions`](../../../src/Nucleus.Abstractions/Nucleus.Abstractions.csproj). These provide the most value for ensuring foundational stability.
2.  **Integration Confidence:** Employ integration tests to verify interactions between major components (e.g., API Service <-> Database, Processor <-> AI Services).
3.  **API-Centric E2E Validation:** Utilize **direct API calls** as the primary tool for End-to-End (E2E) testing during development and for automated checks. Test scripts (e.g., PowerShell, Python) or dedicated C# test projects using `.NET Aspire's testing support (Aspire.Hosting.Testing)` provide a scriptable way to simulate client interactions and test the full flow from API request to response (or asynchronous job completion).
    *   **Implementation Example:** [`ApiIntegrationTests.cs`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/ApiIntegrationTests.cs)
4.  **Adapters as Clients:** Treat client adapters ([`Nucleus.Adapters.Console`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Nucleus.Infrastructure.Adapters.Console.csproj), [`Nucleus.Adapters.Teams`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj)) as consumers of the API. Their E2E tests focus on their specific translation logic *and* their ability to correctly interact with the `Nucleus.Services.Api`.
5.  **Avoid Brittle UI Tests:** Given the focus on backend processing and API interactions, avoid investing heavily in complex UI automation unless a specific UI client becomes a core part of the system.
6.  **Leverage AI for Testing:** Explore opportunities to use AI (including Cascade) to generate test data, formulate complex test queries against the API, and potentially automate aspects of E2E API testing.
7.  **Documentation as Test Specification:** Treat architecture documents like this one as living specifications. Tests should validate the behaviors and interactions described herein.
8.  **Simplicity and Ergonomics:** Favor standard, potentially built-in .NET testing tools where practicable. Aim for a smaller, tighter suite of very good tests over a philosophy of broad code coverage with countless tests.

## 2. Testing Layers & Tools

We adopt a layered testing strategy, recognizing that different types of validation require different tools and approaches. Our primary test framework across layers is **xUnit.net**, chosen for its modern design, popularity, and strong integration with the .NET ecosystem.

### 2.1. Layer 1: Unit Tests

*   **Scope:** Individual classes, methods, and logic units within a single project/service, tested in isolation.
*   **Location:** Ideally, test projects colocated with the source project (e.g., a hypothetical `Nucleus.Domain.Processing.Tests`). *(Note: This project was not found during the last search)*.
*   **Tools:** **xUnit.net**, Mocking libraries (e.g., `Moq`, `NSubstitute`).
*   **Purpose:** Verify the correctness of specific algorithms, state transitions, utility functions, and input validation. Dependencies are typically mocked.
*   **Examples:** Testing parsing logic in `IContentExtractor`, state transitions in `OrchestrationService`, helper functions.

### 2.2. Layer 2: Service Integration Tests

*   **Scope:** Testing a single service (e.g., `Nucleus.Services.Api`) interacting with its direct *external* dependencies (databases, caches, message queues).
*   **Location:** Dedicated integration test projects (e.g., [`Nucleus.Services.Api.IntegrationTests`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj)).
*   **Tools:**
    *   **xUnit.net** as the test runner.
    *   **`Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>`:** Hosts the service under test *in-memory*. Critically, this approach **allows modifying the service's internal Dependency Injection (DI) container** using features of `WebApplicationFactory`. This capability is essential for replacing specific internal components (e.g., database access, external API clients) with test doubles (mocks, stubs) when necessary for focused testing.
    *   **Testcontainers for .NET:** Highly recommended for providing *real* external dependencies (e.g., Cosmos DB Emulator, Redis) running in ephemeral Docker containers. This ensures high-fidelity testing against actual dependency behavior.
*   **Purpose:** Verify the service's interaction logic with its immediate infrastructure dependencies. Allows testing repository logic against a real database, ensuring correct message queue interactions, etc., while potentially mocking downstream *services*.
*   **Examples:** Testing `CosmosDbPersonaKnowledgeRepository` against a Testcontainer-managed Cosmos DB emulator; testing API endpoint logic that interacts with the database, potentially mocking the `IOrchestrationService` call.

### 2.3. Layer 3: System Integration Tests (.NET Aspire AppHost)

*   **Scope:** Testing the interaction of multiple services orchestrated by the `.NET Aspire AppHost` ([`Nucleus.AppHost`](../../../Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj)). This verifies the overall system configuration and inter-service communication paths.
*   **Location:** Dedicated integration test projects (e.g., [`Nucleus.Services.Api.IntegrationTests`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj)).
*   **Tools:** **xUnit.net**, `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>`, `Aspire.Hosting.Testing` (or direct `HttpClient` usage against AppHost-exposed endpoints).
*   **Purpose:** Verify that services can start, communicate, and handle requests within the Aspire-managed environment. Focuses on connectivity, configuration loading, and basic request/response flows across service boundaries.
*   **Dependencies:** Uses real dependencies (or emulators like the Cosmos DB Emulator) provided by the AppHost.

### 2.3.1. Conditional Resource Registration for System Tests

*   **Challenge:** System Integration Tests (Layer 3) launch the actual `AppHost`, which typically configures production-like resources (e.g., cloud databases, message queues). For testing, it's often desirable to use local emulators, containers (via Testcontainers directly in AppHost), or fakes to improve speed, reduce cost, and isolate tests.
*   **Solution:** Since tests in this layer *cannot* modify the internal DI of running services, the `AppHost` itself must conditionally register different resources based on the execution context.
*   **Mechanism:** The standard approach is to use environment variables. The test project using `Aspire.Hosting.Testing` can set an environment variable (e.g., `ASPIRE_ENVIRONMENT=Development` or `ASPIRE_ENVIRONMENT=Testing`) before building the `DistributedApplication`. The `AppHost`'s `Program.cs` then checks this variable:
    ```csharp
    // In Nucleus.AppHost/Program.cs
    var builder = DistributedApplication.CreateBuilder(args);
    var isTestEnvironment = builder.Environment.IsDevelopment(); // Or check a specific custom variable

    if (isTestEnvironment)
    {
        // Register local/test resources
        builder.AddRedisContainer("cache");
        builder.AddRabbitMQContainer("messaging");
        // Potentially use Azure Storage Emulator via AddExecutable or specific Testcontainers
    }
    else
    {
        // Register production/staging resources
        builder.AddAzureRedis("cache");
        builder.AddAzureServiceBus("messaging");
        builder.AddAzureStorage("storage");
    }

    // Add projects that consume these resources
    var apiService = builder.AddProject<Projects.Nucleus_Services_Api>("nucleusapi")
                           .WithReference(builder.GetResource("cache")) // Gets the right one based on the if/else
                           .WithReference(builder.GetResource("messaging"));
    ```
*   **Benefit:** This allows Layer 3 tests to run against a fully orchestrated environment using test-appropriate dependencies without needing to modify the services under test.

### 2.4. Layer 4: End-to-End (E2E) UI Tests (Future)

*   **Scope:** Testing the complete user experience through a graphical user interface (Web UI, etc.).
*   **Location:** Dedicated E2E test project.
*   **Tools:**
    *   **xUnit.net** as the test runner.
    *   **Playwright for .NET:** Recommended framework for reliable browser automation.
    *   Ideally integrated with **`Aspire.Hosting.Testing`**: Launch the full stack (including UI and backend) locally using the test host, get the UI's URL, and point Playwright to it.
*   **Purpose:** Validate user workflows from the UI perspective, ensuring the front-end interacts correctly with the backend APIs.

## 3. Testing Environments

*   **Local Development:** Developers run unit and service/system integration tests frequently using the tools described above (xUnit, WAF, Testcontainers, Aspire.Hosting.Testing).
*   **CI/CD Pipelines:** Automated test runners execute all applicable test layers. System integration tests (`Aspire.Hosting.Testing`) are crucial here to validate the orchestrated application. E2E UI tests (Playwright) run against either a locally hosted stack (via `Aspire.Hosting.Testing`) or a deployed test environment.
*   **Staging/Production:** Smoke tests and health checks target deployed API endpoints.

## 4. Testing Specific Components & Flows

This section now maps to the layers defined above:

*   **Content Extraction, Synthesis, Repository Logic, etc.:** Primarily tested via **Unit Tests** (Layer 1) and **Service Integration Tests** (Layer 2, using WAF + Testcontainers for database interaction).
*   **Orchestration Logic:** Tested via **Unit Tests** (mocking dependencies) and **System Integration Tests** (Layer 3, verifying interaction flows between API and workers via queues/databases configured in AppHost).
*   **API Service Endpoints:** Tested across multiple layers:
    *   **Unit Tests** (Layer 1): Test endpoint handler logic in isolation, mocking service dependencies.
    *   **Service Integration Tests** (Layer 2): Test endpoints interacting with real dependencies (e.g., database via Testcontainers), potentially mocking other *services*.
    *   **System Integration Tests** (Layer 3): Test endpoints as part of the full Aspire-orchestrated system, verifying reachability and basic request/response flows. ([`ApiIntegrationTests.cs`](../../../tests/Integration/Nucleus.Services.Api.IntegrationTests/ApiIntegrationTests.cs))
*   **Client Adapters ([`Nucleus.Adapters.Console`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Nucleus.Infrastructure.Adapters.Console.csproj), [`Nucleus.Adapters.Teams`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj)):**
    *   **Unit Tests** (Layer 1): Verify the translation logic (Platform Event <-> AdapterRequest/Response).
    *   **System Integration Tests** (Layer 3): Could potentially be extended to include adapters running within the AppHost, verifying their ability to call the API service within the orchestrated environment.

## 5. E2E API Testing (System Integration Focus)

While E2E often implies UI, we use the term here to refer to **System Integration Tests** (Layer 3) that validate full flows via the API endpoints of the Aspire-orchestrated application.

**Workflow:**

1.  **Setup:** Use `Aspire.Hosting.Testing` (or `WebApplicationFactory` pointing to the AppHost-managed service) to start the necessary parts of the application. For full system tests, start the [`Nucleus.AppHost`](../../../Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj). Ensure dependencies defined in AppHost (like Cosmos Emulator container) are running. Use `ResourceNotificationService` (if using `Aspire.Hosting.Testing`) or other mechanisms to wait for target services (e.g., `apiservice`) to be ready.
2.  **Execute:** Use `HttpClient` obtained from the testing infrastructure to send requests to API endpoints.
3.  **Verify:** Assert API responses, check database state (connecting to the DB instance managed by AppHost), check message queues, etc.

```mermaid
graph TD
    subgraph Test Environment (System Integration)
        A[xUnit Test Runner]
        B(Aspire.Hosting.Testing)
        C[AppHost Process]
        D[API Service Process]
        E[Worker Service Process]
        F[Dependency Containers (e.g., DB Emulator via AppHost)]
        G[ResourceNotificationService]
    end

    subgraph Test Execution
        A -- Uses --> B
        B -- Starts --> C
        C -- Starts --> D & E & F
        A -- Waits via --> G
        A -- Gets HttpClient --> B
        A -- HTTP Request --> D
        D -- Interacts --> E & F
    end

    subgraph Verification
        A -- Asserts --> H["API Response (Status, Body)"]
        A -- Asserts --> I["Database State (via connection string from B)"]
        A -- Asserts --> J["Queue State (if applicable)"]
    end
```

## 6. Testing AI Integration (`IChatClient`, `IEmbeddingGenerator`)

Verifying integration with external AI services is critical:

1.  **Configuration:** Ensure API keys/endpoints are correctly loaded (e.g., via `secrets.json` for local testing).
2.  **Connectivity:** Simple integration tests that make a basic call (e.g., generate embedding for "test", get a simple chat completion) to verify authentication and basic API reachability.
3.  **E2E Validation:** Use API endpoint E2E tests (Section 5). Provide input requiring AI analysis (e.g., summarization, sentiment analysis) and assert that a plausible AI-generated response is returned. This implicitly tests both `IEmbeddingGenerator` (for retrieval context) and `IChatClient` (for final response generation).
4.  **Monitoring/Logging:** Ensure adequate logging around AI service calls to diagnose failures during testing or in production.

## 7. Future Considerations

*   **E2E UI Testing:** Implement Layer 4 using **Playwright** if/when a web UI is added.
*   **Testcontainers Adoption:** Fully integrate **Testcontainers** for managing dependencies in Service Integration Tests (Layer 2).
*   **Observability in Tests:** Leverage Aspire's logging/tracing/metrics within test runs for better diagnostics.
*   **Architecture Testing:** Consider adding tests using **NetArchTest** to enforce architectural rules.
*   **Performance Testing:** Measuring API response times, processing throughput under load.
*   **Security Testing:** Penetration testing, vulnerability scanning.
*   **Chaos Testing:** Injecting failures to test resilience.

This layered testing strategy provides a robust framework for ensuring Nucleus OmniRAG quality by leveraging the strengths of different tools, aligned with .NET Aspire best practices.
