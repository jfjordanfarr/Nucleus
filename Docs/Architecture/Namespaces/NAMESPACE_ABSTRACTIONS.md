---
title: Namespace - Nucleus.Abstractions
description: Defines core interfaces, DTOs, and base types shared across the Nucleus application, forming the foundation for decoupling layers.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Abstractions

**Relative Path:** `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj`

## 1. Purpose

This project defines the fundamental contracts (interfaces), data structures (DTOs, records, enums), and shared constants used throughout the Nucleus solution. It serves as the core decoupling mechanism between different architectural layers.

## 2. Key Components

*   **Interfaces:** Defines contracts for services and repositories (e.g., `IOrchestrationService`, `IArtifactMetadataRepository`, `IPersona`).
*   **DTOs/Models:** Defines data transfer objects used for API requests/responses and internal communication (e.g., `AdapterRequest`, `ArtifactReference`, `ArtifactMetadata`).
*   **Enums:** Shared enumerations (e.g., `PlatformType`).
*   **Constants:** Shared constant values (`NucleusConstants.cs`).

## 3. Dependencies

*   Minimal external dependencies (typically only .NET BCL and potentially logging abstractions).
*   **Crucially, this project has NO dependencies on other Nucleus projects.**

## 4. Dependents

Virtually all other projects within the `src/` directory depend on `Nucleus.Abstractions`.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [Interfaces Document (Placeholder)]()
*   [Data Models Document (Placeholder)]()
