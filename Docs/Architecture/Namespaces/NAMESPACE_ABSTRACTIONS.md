---
title: Namespace - Nucleus.Abstractions
description: Defines core interfaces, DTOs, and base types shared across the Nucleus application, forming the foundation for decoupling layers.
version: 1.2
date: 2025-05-08
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Abstractions

**Relative Path:** `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj`

## 1. Purpose

This project defines the fundamental contracts (interfaces), data structures (DTOs, records, enums), and shared constants used throughout the Nucleus solution. It serves as the core decoupling mechanism between different architectural layers.

## 2. Key Components

*   **Interfaces:** Defines contracts for services and repositories. Found primarily in the root and sub-folders like `Orchestration/`, `Repositories/`, `Extraction/`, and `Adapters/`.
    *   Examples: [`IOrchestrationService`](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs), [`IArtifactMetadataRepository`](../../../src/Nucleus.Abstractions/Repositories/IArtifactMetadataRepository.cs), [`IArtifactProvider`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs), [`IAgenticStrategyHandler`](../Personas/Nucleus.Personas.Core/Interfaces/IAgenticStrategyHandler.cs), [`IPlatformNotifier`](../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs).
    *   **Extraction Specific:**
        *   [`IContentExtractor`](../../../src/Nucleus.Abstractions/Extraction/IContentExtractor.cs): Interface for services that extract textual content from artifact streams. Located in `Nucleus.Abstractions.Extraction`.
*   **DTOs/Models:** Defines data transfer objects used for API requests/responses and internal communication. Found primarily in the `Models/` and `Orchestration/` folders.
    *   Examples: [`AdapterRequest`](../../../src/Nucleus.Abstractions/Models/AdapterRequest.cs), [`ArtifactReference`](../../../src/Nucleus.Abstractions/Models/ArtifactReference.cs), [`ArtifactMetadata`](../../../src/Nucleus.Abstractions/Models/ArtifactMetadata.cs).
    *   **Orchestration Specific:**
        *   [`ContentExtractionResult`](../../../src/Nucleus.Abstractions/Orchestration/ContentExtractionResult.cs): DTO representing the outcome of a content extraction operation. Located in `Nucleus.Abstractions.Orchestration`.
*   **Enums:** Shared enumerations (e.g., [`PlatformType`](../../../src/Nucleus.Abstractions/Models/PlatformType.cs)). Found in `Models/`.
*   **Constants:** Shared constant values ([`NucleusConstants.cs`](../../../src/Nucleus.Abstractions/NucleusConstants.cs)).

## 3. Dependencies

*   Minimal external dependencies (typically only .NET BCL and potentially logging abstractions).
*   **Crucially, this project has NO dependencies on other Nucleus projects.**

## 4. Dependents

Virtually all other projects within the `src/` directory depend on `Nucleus.Abstractions`.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [01_PROJECT_CONTEXT.md](../../../AgentOps/01_PROJECT_CONTEXT.md#nucleusabstractions)
*   (Consider adding links to specific processing/persona architecture docs that heavily use these abstractions)
