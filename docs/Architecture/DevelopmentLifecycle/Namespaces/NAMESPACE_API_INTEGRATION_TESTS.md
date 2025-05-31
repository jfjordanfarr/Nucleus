---
title: "Namespace - Nucleus.System.IntegrationTests"
description: "Describes the system-wide integration test project, utilizing .NET Aspire's testing infrastructure to orchestrate the entire Nucleus application via Nucleus.AppHost."
version: 1.0
date: 2025-05-29
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.System.IntegrationTests

**Relative Path:** `tests/Nucleus.System.IntegrationTests/Nucleus.System.IntegrationTests.csproj`

## 1. Purpose

This project contains **System Integration tests (Layer 3)** targeting the entire Nucleus application, orchestrated by the [`Nucleus.AppHost`](../../Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj). It leverages the **`Aspire.Hosting.Testing`** infrastructure to:

*   Build and run the `Nucleus.AppHost` process, which in turn starts all defined services and resources.
*   Start all dependent services (e.g., `Nucleus.Core.RAGLogic`, `Nucleus.Core.AgentRuntime`, `Nucleus.Infrastructure.Data.Persistence`, `Nucleus.Agents.M365`, `Nucleus.Tools.Mcp`) and resources (like the **Cosmos DB Emulator** and **Service Bus Emulator**).
*   Wait for resources and services to become available.
*   Obtain configured `HttpClient` instances to make real HTTP requests to API service endpoints (if applicable, e.g., if an API gateway or specific service endpoints are exposed by `Nucleus.AppHost`).
*   Resolve dependencies directly from the running services' `IServiceProvider` for verification purposes (e.g., checking database state after a RAG logic operation, or verifying agent runtime state).
*   Test interactions between different services within the orchestrated environment, including those communicating via the Model Context Protocol (MCP).

This approach provides high confidence in the integrated system's behavior, ensuring that different components work together as expected.

## 2. Key Components

*   **Test Classes:** Classes containing test methods implementing `IAsyncLifetime` for setup/teardown (e.g., `SystemIntegrationTests`, `McpInteractionTests`).
*   **`DistributedApplicationTestingBuilder` Usage:** Code within `InitializeAsync` to build and start the `DistributedApplication` representing the `Nucleus.AppHost`.
*   **Resource Waiting Logic:** Using `DistributedApplication.ResourceNotifications.WaitForResourceAsync` to ensure all services and emulators are running before tests execute.
*   **`HttpClient` Creation (if applicable):** Using `DistributedApplication.CreateHttpClient` to get clients for interacting with any exposed HTTP services.
*   **Direct Service Provider Access (for Verification):** Using `DistributedApplication.Services.GetRequiredService<TProject>().Services` to access the service provider of a running service (e.g., `Nucleus.Core.RAGLogic`, `Nucleus.Core.AgentRuntime`) to resolve components like repositories, service clients, or to inspect internal state.
*   **Test Methods:** Individual tests exercising specific system-level workflows, interactions between services, and verifying responses and side effects (e.g., data persistence in emulated Cosmos DB after an agent interaction, correct MCP message routing and handling).
*   **Test Utilities/Helpers:** Custom code to support test setup (e.g., creating test request objects, seeding data in emulators) or assertions.
*   **Model Context Protocol (MCP) Test Utilities:** Specific helpers for constructing, sending (potentially via direct service invocation if not over HTTP), and validating MCP requests and responses, ensuring compliance with the MCP schema and expected agent/tool behaviors across the system.

## 3. Dependencies

*   `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (The AppHost project being orchestrated).
*   All core service projects that are part of the `Nucleus.AppHost` and whose interactions are being tested:
    *   `src/Nucleus.Core.RAGLogic/Nucleus.Core.RAGLogic.csproj`
    *   `src/Nucleus.Core.AgentRuntime/Nucleus.Core.AgentRuntime.csproj`
    *   `src/Nucleus.Infrastructure.Data.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj`
    *   `src/Nucleus.Agents.M365/Nucleus.Agents.M365.csproj`
    *   `src/Nucleus.Tools.Mcp/Nucleus.Tools.Mcp.csproj`
    *   (And any other relevant service projects)
*   `src/Nucleus.Shared.Kernel.Abstractions/Nucleus.Shared.Kernel.Abstractions.csproj` (For DTOs, interfaces, including MCP contracts).
*   **Testing Frameworks:**
    *   `xUnit` (Test runner and assertion library).
    *   `Aspire.Hosting.Testing` (Core Aspire testing infrastructure).
    *   `Aspire.Hosting` (Core Aspire hosting types).
    *   `Microsoft.AspNetCore.Mvc.Testing` (If HTTP interactions are tested).
*   **Emulators/Test Resources defined in AppHost:** (e.g., `Aspire.Microsoft.Azure.Cosmos.Emulator`, `Aspire.Azure.Messaging.ServiceBus.Emulator`)

## 4. Dependents

*   This project is a test project and is not depended upon by application code.

## 5. Related Documents

*   [01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [02_TESTING_STRATEGY.md](../02_TESTING_STRATEGY.md) (Replaces `09_ARCHITECTURE_TESTING.md`)
*   [NAMESPACE_APP_HOST.md](./NAMESPACE_APP_HOST.md)
*   [MODEL_CONTEXT_PROTOCOL.md](../../Protocols/MODEL_CONTEXT_PROTOCOL.md) (For MCP details)
