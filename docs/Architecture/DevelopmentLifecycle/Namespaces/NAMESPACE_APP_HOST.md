---
title: "Namespace - Nucleus.AppHost (.NET Aspire Orchestrator)"
description: "Describes the .NET Aspire AppHost project (`Nucleus.AppHost`) responsible for orchestrating the development, testing, and deployment environments for Nucleus services, including Agents, MCP Tools, and Background Workers."
version: 3.0
date: 2025-05-27
parent: ../01_NAMESPACES_FOLDERS.md
see_also:
    - ../02_TESTING_STRATEGY.md
    - ../../Deployment/01_DEPLOYMENT_OVERVIEW.md # Assuming this covers Aspire deployment
    - "https://learn.microsoft.com/en-us/dotnet/aspire/"
---

# Project: Nucleus.AppHost

**Relative Path:** `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj`

## 1. Purpose

The `Nucleus.AppHost` project is the .NET Aspire application host, serving as the central orchestrator for the various services that constitute the Nucleus platform. Its primary responsibilities include:

*   **Development Environment Setup:** Simplifies local development by defining, configuring, and launching all necessary Nucleus services (Agents, MCP Tools, Background Workers), along with their dependencies (e.g., databases, message queues, emulators).
*   **Service Discovery:** Provides mechanisms for services to discover and communicate with each other within the orchestrated environment.
*   **Configuration Management:** Manages environment variables and connection strings for the orchestrated services.
*   **Resource Emulation:** Facilitates the use of emulated resources (e.g., Azure Cosmos DB emulator, Azure Service Bus emulator) for local development and testing, reducing reliance on cloud resources.
*   **Integration Testing Foundation:** Serves as the foundation for `Aspire.Hosting.Testing`, enabling high-fidelity integration tests by orchestrating the entire application stack or relevant subsets thereof.
*   **Deployment Manifest Generation:** Can be used to generate deployment manifests for various target environments, streamlining the deployment process.

This project is crucial for maintaining a consistent and manageable development experience and for enabling robust integration testing across the distributed Nucleus architecture.

## 2. Key Components

*   **`Program.cs`:** The main application entry point where `DistributedApplication.CreateBuilder()` is invoked. This file contains the logic for adding and configuring all Nucleus projects and their required resources.
*   **Service Project References:** Adds references to all orchestrable Nucleus service projects, such as:
    *   `Nucleus.Agent.<PersonaName>` projects
    *   `Nucleus.McpTool.<ToolName>` projects
    *   `Nucleus.BackgroundWorker.ServiceBus`
*   **Resource Definitions:** Uses .NET Aspire builder extensions to declare and configure resources:
    *   `builder.AddProject<TProject>()`: To include other .NET projects as services.
    *   `builder.AddAzureCosmosDB(...)` or `builder.AddPostgres(...)`, etc.: To add database resources, often configured to use emulators locally.
    *   `builder.AddAzureServiceBus(...)`: To add message queue resources, often configured for local emulators.
    *   `builder.AddContainer(...)`: To include custom containers.
*   **Service Discovery Configuration:** Implicitly handled by Aspire, but specific configurations might be applied.
*   **Conditional Logic:** May employ conditional logic (e.g., based on environment variables or preprocessor directives like `#if DEBUG`) to adjust configurations for different scenarios (e.g., local development vs. integration testing vs. preparing for production deployment).

## 3. Dependencies

*   **`Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj`**: References the shared .NET Aspire service defaults project, which provides common configurations, health checks, and telemetry setup for all Aspire-managed services.
*   **All Orchestrable Service Projects**: Directly references all Nucleus projects that are intended to be run and managed by Aspire, for example:
    *   `src/Nucleus.Agent.EduFlow/Nucleus.Agent.EduFlow.csproj`
    *   `src/Nucleus.McpTool.KnowledgeStore/Nucleus.McpTool.KnowledgeStore.csproj`
    *   `src/Nucleus.BackgroundWorker.ServiceBus/Nucleus.BackgroundWorker.ServiceBus.csproj`
    *   *(And so on for all other agents, MCP tools, etc.)*
*   **Relevant .NET Aspire SDK Packages**: `Aspire.Hosting`, etc.

## 4. Dependents

*   **Development/Test Time Tool:** `Nucleus.AppHost` is primarily a tool for development, testing, and deployment orchestration.
*   **Integration Test Projects:** Directly referenced and utilized by integration test projects (e.g., `tests/Integration/Nucleus.AppHost.Tests.Integration/` or specific service integration tests that use `Aspire.Hosting.Testing`) to set up the necessary service environment for tests.
*   **Deployment Tools/Scripts:** The output or manifests generated by `Nucleus.AppHost` might be consumed by deployment scripts or CI/CD pipelines.
*   It is **not** a runtime dependency for the individual deployed services themselves (e.g., `Nucleus.Agent.EduFlow.dll` does not depend on `Nucleus.AppHost.dll` in a production deployment).

## 5. Related Documents

*   [../01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [../02_TESTING_STRATEGY.md](../02_TESTING_STRATEGY.md)
*   [../../Deployment/01_DEPLOYMENT_OVERVIEW.md](../../Deployment/01_DEPLOYMENT_OVERVIEW.md) (should cover Aspire deployment aspects)
*   [Official .NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
