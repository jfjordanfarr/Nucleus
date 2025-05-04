---
title: Namespace - Nucleus.Infrastructure.Providers
description: Defines shared infrastructure components for providing access to external data or resources, like artifact content.
version: 1.0
date: 2025-05-03
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Providers

**Relative Path:** `src/Nucleus.Infrastructure/Providers/Nucleus.Infrastructure.Providers.csproj`

## 1. Purpose

This project consolidates implementations of provider interfaces defined in `Nucleus.Abstractions`. Providers are responsible for interacting with external systems or data sources to retrieve information needed by the core application, such as the content of an artifact referenced by a client.

Placing providers in this dedicated infrastructure project ensures they are decoupled from specific client adapters (like Console or Teams) and can be readily consumed by the central `Nucleus.Services.Api` or other core services.

## 2. Key Components

*   **`IArtifactProvider` Implementations:**
    *   `ConsoleArtifactProvider.cs`: ([../../../src/Nucleus.Infrastructure/Providers/ConsoleArtifactProvider.cs](../../../src/Nucleus.Infrastructure/Providers/ConsoleArtifactProvider.cs)) - Fetches content for artifacts referenced via local file system paths (typically originated by the Console adapter).
*   **(Future Providers):** Implementations for other artifact sources (e.g., SharePoint, OneDrive, Web URLs) would reside here.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References interfaces like `IArtifactProvider` and shared models like `ArtifactReference`, `ArtifactContent`).

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (Uses these providers via Dependency Injection to resolve artifact content).

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [IArtifactProvider Interface](../../../src/Nucleus.Abstractions/IArtifactProvider.cs)
*   [01_ARCHITECTURE_PROCESSING.md](../01_ARCHITECTURE_PROCESSING.md) (Describes the role of artifact providers in ingestion)
