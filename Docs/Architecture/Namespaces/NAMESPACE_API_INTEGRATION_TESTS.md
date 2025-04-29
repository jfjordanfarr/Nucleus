---
title: Namespace - Nucleus.Services.Api.IntegrationTests
description: Describes the integration test project for the Nucleus API service.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Services.Api.IntegrationTests

**Relative Path:** `tests/Integration/Nucleus.Services.Api.IntegrationTests/Nucleus.Services.Api.IntegrationTests.csproj`

## 1. Purpose

This project contains integration tests specifically targeting the `Nucleus.Services.Api`. It uses the `WebApplicationFactory` from `Microsoft.AspNetCore.Mvc.Testing` to host the API in-memory, allowing tests to make real HTTP requests to the API endpoints and verify their behavior, including interactions with backend services (which are often mocked or replaced with test doubles).

## 2. Key Components

*   **Test Classes:** Classes containing test methods (e.g., `ApiIntegrationTests`).
*   **`WebApplicationFactory` Setup:** Configuration of the test host, potentially overriding service registrations for testing purposes (e.g., replacing `IArtifactMetadataRepository` with `NullArtifactMetadataRepository`).
*   **Test Methods:** Individual tests exercising specific API endpoints and scenarios.
*   **Test Utilities/Helpers:** Any custom code to support test setup or assertions.

## 3. Dependencies

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (The project under test).
*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (For DTOs used in requests/responses).
*   Testing frameworks (`MSTest`, `Microsoft.AspNetCore.Mvc.Testing`).

## 4. Dependents

*   This project is a test project and is not depended upon by application code.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [09_ARCHITECTURE_TESTING.md](../09_ARCHITECTURE_TESTING.md)
*   [10_ARCHITECTURE_API.md](../10_ARCHITECTURE_API.md)
