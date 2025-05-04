---
title: Namespace - Nucleus.AppHost
description: Describes the .NET Aspire AppHost project responsible for orchestrating the development and testing environment, including service discovery and emulated resources.
version: 1.1
date: 2025-05-01
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.AppHost

**Relative Path:** `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj`

## 1. Purpose

This project is the entry point for the .NET Aspire orchestration layer during development and system integration testing. It uses the `IDistributedApplicationBuilder` to define and configure the services, databases, and other resources that make up the Nucleus application. It simplifies local development by managing service discovery, environment variables, and containerized dependencies (like the Cosmos DB Emulator and Azure Service Bus Emulator).

**Crucially, it also serves as the foundation for `Aspire.Hosting.Testing`, allowing the `Nucleus.Services.Api.IntegrationTests` project to orchestrate the entire application stack for high-fidelity testing.**

## 2. Key Components

*   **`Program.cs`:** The main application entry point where the `DistributedApplication.CreateBuilder()` is called and services/resources are added to the builder.
*   **Aspire Builder Extensions:** Methods like `AddProject<T>()`, `AddAzureCosmosDBEmulator(...)`, `AddAzureServiceBus(...)` (potentially using emulated connection strings), `WithReference(...)` are used to define the application composition.
*   **Conditional Resource Registration:** Logic (e.g., using environment variables or `#if` directives) might be present to configure resources differently for development vs. testing scenarios.

## 3. Dependencies

*   `Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj` (References shared Aspire configurations)
*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (References the API service it needs to host)
*   Other potential service projects as they are added.

## 4. Dependents

*   This project is primarily a development-time and test-time tool.
*   It is directly referenced and orchestrated by `tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj` for system integration testing.
*   It is not directly depended upon by the core application logic (`src/`) projects.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [09_ARCHITECTURE_TESTING.md](../09_ARCHITECTURE_TESTING.md)
*   [NAMESPACE_API_INTEGRATION_TESTS.md](./NAMESPACE_API_INTEGRATION_TESTS.md)
*   [07_ARCHITECTURE_DEPLOYMENT.md](../07_ARCHITECTURE_DEPLOYMENT.md)
*   [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
