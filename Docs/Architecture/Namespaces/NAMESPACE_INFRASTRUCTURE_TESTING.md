---
title: Architecture - Namespace - Infrastructure.Testing
description: Defines the structure and purpose of the Nucleus.Infrastructure.Testing namespace and project, which houses test doubles and supporting classes for integration and unit testing.
version: 1.0
date: 2025-04-30
---

# Architecture: `Nucleus.Infrastructure.Testing` Namespace & Project

**Parent:** [./11_ARCHITECTURE_NAMESPACES_FOLDERS.md](./11_ARCHITECTURE_NAMESPACES_FOLDERS.md)

## 1. Purpose

The `Nucleus.Infrastructure.Testing` project and its corresponding namespace serve as a dedicated location for test support infrastructure within the Nucleus solution. Its primary goal is to isolate test doubles (mocks, fakes, stubs) and other testing utilities from the production codebase (`src/`). This separation ensures:

*   **Clarity:** Production code remains free of test-specific implementations.
*   **Maintainability:** Test utilities are organized logically in one place.
*   **Build Integrity:** Test infrastructure does not accidentally get included in production deployments.

This project is referenced primarily by integration test projects (e.g., `Nucleus.Services.Api.IntegrationTests`) but can also be used by unit test projects if needed.

## 2. Key Components & Structure

The internal structure mirrors relevant parts of the `src/` directory where the corresponding interfaces or base classes are defined, promoting consistency.

*   **`./Configuration/`**: Contains in-memory or mock implementations of configuration providers (e.g., `IConfigurationProvider`, `IPersonaConfigurationProvider`).
    *   `InMemoryPersonaConfigurationProvider.cs`: ([../tests/Infrastructure.Testing/Configuration/InMemoryPersonaConfigurationProvider.cs](../tests/Infrastructure.Testing/Configuration/InMemoryPersonaConfigurationProvider.cs))
*   **`./Queues/`**: Houses in-memory implementations of background task queues.
    *   `InMemoryBackgroundTaskQueue.cs`: ([../tests/Infrastructure.Testing/Queues/InMemoryBackgroundTaskQueue.cs](../tests/Infrastructure.Testing/Queues/InMemoryBackgroundTaskQueue.cs))
*   **`./Repositories/`**: Includes in-memory implementations of data repositories defined in `Nucleus.Abstractions`.
    *   `InMemoryArtifactMetadataRepository.cs`: ([../tests/Infrastructure.Testing/Repositories/InMemoryArtifactMetadataRepository.cs](../tests/Infrastructure.Testing/Repositories/InMemoryArtifactMetadataRepository.cs))
    *   `InMemoryPersonaKnowledgeRepository.cs`: ([../tests/Infrastructure.Testing/Repositories/InMemoryPersonaKnowledgeRepository.cs](../tests/Infrastructure.Testing/Repositories/InMemoryPersonaKnowledgeRepository.cs))

## 3. Dependencies

*   **`Nucleus.Abstractions`**: This project references `Nucleus.Abstractions` to access the interfaces (like `IArtifactMetadataRepository`, `IPersonaKnowledgeRepository`, etc.) that its test doubles implement.

## 4. Usage

Integration test projects add a project reference to `Nucleus.Infrastructure.Testing`. During test setup (e.g., in `WebApplicationFactory` configuration), these in-memory implementations are registered with the Dependency Injection container, overriding the default production implementations.

```csharp
// Example usage in ApiIntegrationTests.cs (Simplified)
services.AddSingleton<IArtifactMetadataRepository, InMemoryArtifactMetadataRepository>();
services.AddSingleton<IPersonaKnowledgeRepository<object>, InMemoryPersonaKnowledgeRepository<object>>();
services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
// ... other registrations
```
