---
title: Namespace - Nucleus.ServiceDefaults
description: Describes the .NET Aspire ServiceDefaults project providing shared configurations for Nucleus services.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.ServiceDefaults

**Relative Path:** `Aspire/Nucleus.ServiceDefaults/Nucleus.ServiceDefaults.csproj`

## 1. Purpose

This project centralizes common configurations and service registrations recommended by .NET Aspire for building resilient and observable cloud-native applications. It ensures consistency across different services within the Nucleus solution.

## 2. Key Components

*   **`Extensions.cs`:** Contains extension methods (e.g., `AddServiceDefaults()`) that configure standard services like:
    *   OpenTelemetry for distributed tracing and metrics.
    *   Default health checks endpoints.
    *   Service discovery configurations.
*   **Configuration Files:** May include shared `appsettings.json` snippets or configuration models relevant across services.

## 3. Dependencies

*   Relies heavily on .NET Aspire and OpenTelemetry NuGet packages.
*   Has no dependencies on other Nucleus projects.

## 4. Dependents

*   `Aspire/Nucleus.AppHost/Nucleus.AppHost.csproj` (Uses defaults for orchestration)
*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (Applies shared configurations)
*   Likely any future service projects.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [07_ARCHITECTURE_DEPLOYMENT.md](../07_ARCHITECTURE_DEPLOYMENT.md)
*   [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/service-defaults)
