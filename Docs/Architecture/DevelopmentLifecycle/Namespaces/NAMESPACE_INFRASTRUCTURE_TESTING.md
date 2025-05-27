---
title: "Namespace - Nucleus.Infrastructure.Testing"
description: "Defines the structure and purpose of the Nucleus.Infrastructure.Testing project, housing test doubles and supporting classes for infrastructure testing."
version: 1.1
date: 2025-05-29
parent: ../01_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Testing

**Relative Path:** `tests/Nucleus.Infrastructure.Testing/Nucleus.Infrastructure.Testing.csproj`

## 1. Purpose

The `Nucleus.Infrastructure.Testing` project and its corresponding namespace serve as a dedicated location for shared test support infrastructure within the Nucleus solution. Its primary goal is to centralize test doubles (mocks, fakes, stubs) and other testing utilities that are specifically useful for testing infrastructure components or for providing in-memory alternatives to real infrastructure for higher-level tests.

This ensures:

*   **Clarity:** Production code remains free of test-specific implementations.
*   **Maintainability:** Test utilities are organized logically in one place.
*   **Reusability:** Common test doubles can be shared across multiple infrastructure test projects (e.g., `Nucleus.Infrastructure.Data.Persistence.Tests`, `Nucleus.Infrastructure.ExternalServices.Tests`) and even system integration tests.

This project is referenced by unit and integration test projects for the infrastructure layer and potentially by the system integration test project (`Nucleus.System.IntegrationTests`) when in-memory infrastructure alternatives are needed.

## 2. Key Components & Structure

The internal structure mirrors relevant parts of the `src/Nucleus.Infrastructure` or `src/Nucleus.Shared.Kernel.Abstractions` directories where the corresponding interfaces or base classes are defined, promoting consistency.

*   **`./Configuration/`**: Contains in-memory or mock implementations of configuration providers.
    *   `InMemoryPersonaConfigurationProvider.cs`: Provides an in-memory implementation of `IPersonaConfigurationProvider` (interface likely from `Nucleus.Shared.Kernel.Abstractions`).
*   **`./Messaging/`** (Previously Queues):
    *   `InMemoryBackgroundTaskQueue.cs`: Provides an in-memory implementation of `IBackgroundTaskQueue` (interface from `Nucleus.Shared.Kernel.Abstractions.Messaging`).
*   **`./Persistence/`** (Previously Repositories):
    *   `InMemoryArtifactRepository.cs` (Illustrative, replaces `InMemoryArtifactMetadataRepository`): Implements `IArtifactRepository` (from `Nucleus.Shared.Kernel.Abstractions.Data`).
    *   `InMemoryPersonaKnowledgeRepository.cs`: Implements `IPersonaKnowledgeRepository` (from `Nucleus.Shared.Kernel.Abstractions.Data`).
*   **`./ExternalServices/`** (New section):
    *   `MockHttpMessageHandler.cs`: Utility for mocking HTTP responses for testing external service clients.
    *   `FakeFileArtifactProvider.cs`: An in-memory `IArtifactProvider` for testing file access logic without hitting a real file system.

## 3. Dependencies

*   **`Nucleus.Shared.Kernel.Abstractions`**: This project references `Nucleus.Shared.Kernel.Abstractions` to access the interfaces (like `IArtifactRepository`, `IPersonaKnowledgeRepository`, `IBackgroundTaskQueue`, `IPersonaConfigurationProvider`, etc.) that its test doubles implement.
*   Potentially `Microsoft.AspNetCore.Http.Abstractions` or similar for mocking HTTP contexts if needed for external service client testing.

## 4. Usage

Test projects (e.g., `Nucleus.Infrastructure.Data.Persistence.Tests`, `Nucleus.Infrastructure.ExternalServices.Tests`, `Nucleus.System.IntegrationTests`) add a project reference to `Nucleus.Infrastructure.Testing`. During test setup (e.g., in `Startup.cs` for integration tests using `WebApplicationFactory`, or directly in unit test constructors/setup methods), these in-memory implementations are registered with the Dependency Injection container, overriding the default production implementations or providing controlled test environments.

```csharp
// Example usage in an infrastructure test project (Simplified)
services.AddSingleton<IArtifactRepository, InMemoryArtifactRepository>();
services.AddSingleton<IPersonaKnowledgeRepository<object>, InMemoryPersonaKnowledgeRepository<object>>();
services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
services.AddSingleton<IPersonaConfigurationProvider, InMemoryPersonaConfigurationProvider>();
// ... other registrations
```

## 5. Related Documents

*   [01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [02_TESTING_STRATEGY.md](../02_TESTING_STRATEGY.md)
*   [NAMESPACE_INFRASTRUCTURE_DATA_PERSISTENCE.md](./NAMESPACE_INFRASTRUCTURE_DATA_PERSISTENCE.md)
*   [NAMESPACE_INFRASTRUCTURE_EXTERNAL_SERVICES.md](./NAMESPACE_INFRASTRUCTURE_EXTERNAL_SERVICES.md)
*   [NAMESPACE_INFRASTRUCTURE_MESSAGING.md](./NAMESPACE_INFRASTRUCTURE_MESSAGING.md)
