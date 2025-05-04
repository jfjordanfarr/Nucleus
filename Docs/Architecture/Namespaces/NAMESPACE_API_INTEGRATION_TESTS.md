---
title: Namespace - Nucleus.Services.Api.IntegrationTests
description: Describes the System Integration test project for the Nucleus API service, utilizing .NET Aspire's testing infrastructure.
version: 1.1
date: 2025-05-01
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Services.Api.IntegrationTests

**Relative Path:** `tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj`

## 1. Purpose

This project contains **System Integration tests (Layer 3)** targeting the `Nucleus.Services.Api` within the context of the entire application orchestrated by the [`Nucleus.AppHost`](../../../Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj). It leverages the **`Aspire.Hosting.Testing`** infrastructure to:

*   Build and run the `Nucleus.AppHost` process.
*   Start dependent services (like `Nucleus.Services.Api`) and resources (like the **Cosmos DB Emulator** and **Service Bus Emulator**).
*   Wait for resources to become available.
*   Obtain configured `HttpClient` instances to make real HTTP requests to the API service endpoints running within the orchestrated environment.
*   Resolve dependencies directly from the running API service's `IServiceProvider` for verification purposes (e.g., checking database state).

This approach provides high confidence in the integrated system's behavior.

## 2. Key Components

*   **Test Classes:** Classes containing test methods implementing `IAsyncLifetime` for setup/teardown (e.g., `ApiIntegrationTests`).
*   **`DistributedApplicationTestingBuilder` Usage:** Code within `InitializeAsync` to build and start the `DistributedApplication` representing the `Nucleus.AppHost`.
*   **Resource Waiting Logic:** Using `DistributedApplication.ResourceNotifications.WaitForResourceAsync` to ensure services and emulators are running before tests execute.
*   **`HttpClient` Creation:** Using `DistributedApplication.CreateHttpClient` to get clients for interacting with services.
*   **Dependency Resolution (for Verification):** Using `DistributedApplication.Services.GetRequiredService<TProject>().Services` to access the service provider of a running service to resolve components like repositories.
*   **Test Methods:** Individual tests exercising specific API endpoints and verifying responses and side effects (e.g., data persistence in emulated Cosmos DB).
*   **Test Utilities/Helpers:** Custom code to support test setup (e.g., creating test request objects) or assertions.

## 3. Dependencies

*   `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (The AppHost project being orchestrated).
*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (The primary service under test).
*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (For DTOs, interfaces).
*   `src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Data.Persistence.csproj` (For repository interfaces/implementations used in verification).
*   **Testing Frameworks:**
    *   `xUnit` (Test runner and assertion library).
    *   `Aspire.Hosting.Testing` (Core Aspire testing infrastructure).
    *   `Aspire.Hosting` (Core Aspire hosting types).
    *   `Microsoft.AspNetCore.Mvc.Testing` (Provides underlying testing infrastructure used by Aspire).
    *   `Aspire.Microsoft.Azure.Cosmos` (Aspire integration for Cosmos DB client configuration).
    *   `Azure.Messaging.ServiceBus` (Needed for Service Bus types if interacting directly).
    *   `Moq` (For potential mocking in lower-level tests, though less common for Layer 3).

## 4. Dependents

*   This project is a test project and is not depended upon by application code.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [09_ARCHITECTURE_TESTING.md](../09_ARCHITECTURE_TESTING.md)
*   [10_ARCHITECTURE_API.md](../10_ARCHITECTURE_API.md)
*   [NAMESPACE_APP_HOST.md](./NAMESPACE_APP_HOST.md)
