---
title: Namespace - Nucleus.ServiceDefaults (Aspire)
description: Describes the .NET Aspire ServiceDefaults project, which provides shared configurations, extensions, and best practices for all services within the Nucleus solution, enhancing observability, resilience, and consistency.
version: 1.1
date: 2025-05-27
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.ServiceDefaults

**Relative Path:** `Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj`

## 1. Purpose

This project, part of the .NET Aspire ecosystem, plays a crucial role in establishing common configurations, service registrations, and operational best practices across all services within the Nucleus solution. Its primary goal is to ensure consistency, improve observability (metrics, tracing, logging), enhance resilience (health checks, retries), and simplify the development of cloud-native applications.

By centralizing these cross-cutting concerns, `Nucleus.ServiceDefaults` helps reduce boilerplate code in individual service projects and promotes a standardized approach to building and deploying services managed by the `Nucleus.AppHost`.

## 2. Key Components

*   **`Extensions.cs` (or similar utility classes):**
    *   **`AddServiceDefaults()` (and related extension methods):** This is the core method that configures a standard set of services for an `IHostApplicationBuilder`. This typically includes:
        *   **OpenTelemetry Integration:** Sets up distributed tracing and metrics collection, configured to export to a defined endpoint (e.g., Aspire Dashboard, Azure Monitor).
        *   **Default Health Checks:** Registers standard health check endpoints (e.g., `/health`, `/alive`) that can be used by orchestration platforms or load balancers.
        *   **Service Discovery Configuration:** Facilitates how services discover and communicate with each other, often integrated with the Aspire AppHost.
        *   **Standardized Logging Configuration:** May include enhancements to logging, such as structured logging formats or specific log event enrichments.
    *   Other utility extensions for common service patterns (e.g., HTTP client resilience policies via Polly).
*   **Instrumentation Subfolder/Classes:** May contain custom instrumentation or telemetry initializers if specific application-level metrics or traces are needed beyond the defaults.
*   **Configuration Files/Models (Optional):** While primarily code-based, it might include shared `appsettings.json` snippets or strongly-typed configuration models for settings that are common across all services (e.g., default telemetry export settings).

## 3. Dependencies

*   `Microsoft.Extensions.Hosting.Abstractions`
*   `Microsoft.Extensions.Http.Resilience` (for Polly integration)
*   `Microsoft.Extensions.ServiceDiscovery`
*   `OpenTelemetry.Exporter.OpenTelemetryProtocol`
*   `OpenTelemetry.Extensions.Hosting`
*   `OpenTelemetry.Instrumentation.AspNetCore`
*   `OpenTelemetry.Instrumentation.Http`
*   `OpenTelemetry.Instrumentation.Runtime`
*   This project typically has **no direct dependencies on other Nucleus-specific projects** to maintain its role as a foundational, shared library.

## 4. Dependents

*   `Nucleus.AppHost` (Uses these defaults when orchestrating and launching services, ensuring all managed services benefit from the shared configurations).
*   All executable service projects within the Nucleus solution should reference `Nucleus.ServiceDefaults` and call its extension methods (e.g., `AddServiceDefaults()`) in their `Program.cs` to apply the shared configurations. Examples include:
    *   `Nucleus.Core.AgentRuntime` (if it's built as a runnable service/API)
    *   Any future API gateway or specialized microservices.
    *   Adapter projects if they are hosted as separate services (less common for adapters like Console, but possible for web-facing adapters).

## 5. Related Documents

*   `../01_NAMESPACES_FOLDERS.md`
*   `../Aspire/01_ARCHITECTURE_ASPIRE_OVERVIEW.md` (Overview of .NET Aspire in Nucleus)
*   `../Aspire/02_ARCHITECTURE_ASPIRE_APP_HOST.md` (Details on the AppHost that consumes these defaults)
*   [.NET Aspire Service Defaults Documentation](https://learn.microsoft.com/dotnet/aspire/service-defaults) (Official Microsoft documentation)
