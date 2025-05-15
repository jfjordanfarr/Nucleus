---
title: Namespace - Nucleus.Infrastructure.Adapters.Local
description: Describes the Local Adapter project, providing an in-process client for local interactions with the Nucleus API services.
version: 1.1
date: 2025-05-15
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Adapters.Local

**Relative Path:** `src/Nucleus.Infrastructure/Adapters/Nucleus.Infrastructure.Adapters.Local/Nucleus.Infrastructure.Adapters.Local.csproj`

## 1. Purpose

This project provides a local, in-process client adapter for interacting with the Nucleus system. It acts as an "Adapter" in the Infrastructure Layer, allowing direct programmatic interaction with the `Nucleus.Services.Api` from within the same process or a tightly coupled local process. Unlike the previous `ConsoleAdapter`, this adapter is not intended as a standalone executable CLI but rather as a library to be consumed by other services, primarily `Nucleus.Services.Api` for specific local use cases.

## 2. Key Components

*   **Service Registration Logic:** (e.g., `AddLocalAdapterServices` extension method) for integrating with the `Nucleus.Services.Api` dependency injection container.
*   **API Client Logic:** Code responsible for constructing and directly invoking methods or services within `Nucleus.Services.Api` or making requests to its in-process endpoints if applicable. This might involve direct method calls or a simplified internal request/response mechanism.
*   **Interaction Handlers:** Classes responsible for specific types of local interactions or tasks.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References shared DTOs like `AdapterRequest`, `AdapterResponse`, `Interaction`, `ArtifactReference`).
*   Potentially `Nucleus.Domain.Processing.csproj` or other domain/application layer projects if direct service invocation is used.

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (This adapter is primarily consumed by the API service for local scenarios).

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [05_ARCHITECTURE_CLIENTS.md](../05_ARCHITECTURE_CLIENTS.md)
*   [ARCHITECTURE_ADAPTERS_LOCAL.md](../ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md)
*   [10_ARCHITECTURE_API.md](../Api/10_ARCHITECTURE_API.md)
