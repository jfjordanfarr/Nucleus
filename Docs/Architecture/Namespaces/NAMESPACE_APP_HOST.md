---
title: Namespace - Nucleus.AppHost
description: Describes the .NET Aspire AppHost project responsible for orchestrating the development environment and service discovery.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.AppHost

**Relative Path:** `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj`

## 1. Purpose

This project is the entry point for the .NET Aspire orchestration layer during development. It uses the `IDistributedApplicationBuilder` to define and configure the services, databases, and other resources that make up the Nucleus application. It simplifies local development by managing service discovery, environment variables, and containerized dependencies (like the Cosmos DB Emulator).

## 2. Key Components

*   **`Program.cs`:** The main application entry point where the `DistributedApplication.CreateBuilder()` is called and services are added to the builder.
*   **Aspire Builder Extensions:** Methods like `AddProject<T>()`, `AddAzureCosmosDB(...)`, `WithReference(...)` are used to define the application composition.

## 3. Dependencies

*   `Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj` (References shared Aspire configurations)
*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (References the API service it needs to host)
*   Other potential service projects as they are added.

## 4. Dependents

This project is primarily a development-time tool and is not directly depended upon by the core application logic (`src/`) projects.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [07_ARCHITECTURE_DEPLOYMENT.md](../07_ARCHITECTURE_DEPLOYMENT.md)
*   [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
