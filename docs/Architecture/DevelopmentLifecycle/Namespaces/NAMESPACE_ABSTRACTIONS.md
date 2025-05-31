---
title: "Architectural Strategy for Abstractions in Nucleus"
description: "Explains the strategy for defining and using abstractions (interfaces, base classes) within the Nucleus project, emphasizing the role of Nucleus.Shared.Kernel and layer-specific abstractions."
version: 3.0
date: 2025-05-27
parent: ../01_NAMESPACES_FOLDERS.md
see_also:
    - ../../CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md
    - ../../00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md
    - ./NAMESPACE_SHARED_KERNEL.md # Placeholder for the renamed NAMESPACE_PERSONAS_CORE.md
---

# Architectural Strategy for Abstractions in Nucleus

## 1. Purpose and Philosophy

This document outlines the strategy for defining and utilizing abstractions (primarily interfaces, but also abstract base classes where appropriate) within the Nucleus project. The goal is to achieve loose coupling, high cohesion, testability, and maintainability across the codebase.

Historically, a dedicated `Nucleus.Core.Abstractions` project might have been considered. However, with the refined architecture, the primary location for truly universal, cross-cutting abstractions is the `Nucleus.Shared.Kernel` project. Beyond this, abstractions are typically defined closer to their usage or by the layer that owns the contract.

## 2. Core Abstractions in `Nucleus.Shared.Kernel`

The `Nucleus.Shared.Kernel` project (formerly `Nucleus.Domain.Personas.Core`, and documented in `NAMESPACE_SHARED_KERNEL.md`) is the bedrock for fundamental abstractions that are used throughout the entire Nucleus ecosystem. These include:

*   **`IPersona`**: The core interface defining a Persona's capabilities.
*   **`ArtifactMetadata`**: The model for describing source artifacts.
*   **`PersonaKnowledgeEntry<T>`**: The model for persona-specific knowledge.
*   Core `Result` types for operation outcomes.
*   Other foundational interfaces and base types essential for the interoperability of all Nucleus components.

These abstractions are designed to be stable and have minimal dependencies. Refer to `NAMESPACE_SHARED_KERNEL.md` (once renamed from `NAMESPACE_PERSONAS_CORE.md`) for detailed information on these specific components.

## 3. Layer-Specific Abstractions

While `Nucleus.Shared.Kernel` provides the global contracts, individual architectural layers or components often define their own abstractions relevant to their specific responsibilities. This promotes the Interface Segregation Principle and avoids bloating the shared kernel with overly specific interfaces.

### 3.1. Domain Layer (`Nucleus.Domain.RAGLogic`)

*   **Purpose**: This layer encapsulates the core business logic for Retrieval Augmented Generation, content processing, and analysis.
*   **Abstraction Strategy**:
    *   May define interfaces for specific processing steps, analysis strategies, or data extraction services internal to its operation.
    *   Example: `IContentSegmenter`, `ITextAnalyzer`, `IEmbeddingGeneratorProvider` (though the latter might also be an infrastructure concern if it directly involves external SDKs).
*   **Implementation**: Implemented by concrete classes within `Nucleus.Domain.RAGLogic`.

### 3.2. Infrastructure Layer (`Nucleus.Infrastructure.<Provider>`)

*   **Purpose**: Implements interactions with external systems (databases, message queues, AI models, file systems).
*   **Abstraction Strategy**:
    *   Often implements interfaces defined in `Nucleus.Shared.Kernel` or `Nucleus.Domain.RAGLogic`. For example, an `IRepository<T>` interface from `Kernel` might be implemented by `CosmosDbRepository<T>` in `Nucleus.Infrastructure.CosmosDb`.
    *   May define its own internal interfaces for provider-specific helper services.
    *   For AI model interactions, it will implement interfaces from `Microsoft.Extensions.AI.Abstractions` (e.g., `IChatCompletionService`, `ITextEmbeddingGenerationService`), which are referenced by `Nucleus.Shared.Kernel` or consumed directly by `Nucleus.Domain.RAGLogic`.
*   **Implementation**: Concrete classes within specific provider projects (e.g., `Nucleus.Infrastructure.CosmosDb`, `Nucleus.Infrastructure.Ai.Google`).

### 3.3. Application Layer (Agents, MCP Tools, Background Workers)

*   **`Nucleus.Agent.<PersonaName>`**:
    *   **Abstraction Strategy**: Defines interfaces for platform-specific adapters or interaction handlers if needed, but primarily consumes abstractions from `Nucleus.Shared.Kernel` and `Nucleus.Mcp.Client`.
*   **`Nucleus.McpTool.<ToolName>`**:
    *   **Abstraction Strategy**: Defines the service contract (e.g., API endpoints, gRPC service definitions) that it exposes. Internally, it uses services that might implement interfaces from `Nucleus.Domain.RAGLogic` or `Nucleus.Shared.Kernel`.
*   **`Nucleus.Mcp.Client`**:
    *   **Abstraction Strategy**: Provides typed client interfaces for interacting with MCP Tools. These interfaces mirror the contracts exposed by the MCP Tools. Example: `IKnowledgeStoreMcpClient`.
*   **`Nucleus.BackgroundWorker.ServiceBus`**:
    *   **Abstraction Strategy**: Defines interfaces for message handlers (e.g., `IMessageHandler<TMessage>`) that are then implemented by classes responsible for processing specific message types from the queue.

## 4. Key Principles for Abstractions

*   **Dependency Inversion Principle (DIP):** High-level modules should not depend on low-level modules. Both should depend on abstractions. Abstractions should not depend on details. Details should depend on abstractions.
    *   `Nucleus.Shared.Kernel` provides the most fundamental abstractions.
    *   Domain and Application layers define contracts that Infrastructure layers implement.
*   **Interface Segregation Principle (ISP):** Clients should not be forced to depend on interfaces they do not use. Define fine-grained interfaces specific to client needs.
*   **Liskov Substitution Principle (LSP):** Subtypes must be substitutable for their base types. Ensure implementations correctly fulfill the contract of the interface.
*   **Define Abstractions Close to Their Clients:** While `Nucleus.Shared.Kernel` is for universal abstractions, more specialized abstractions should be defined in the layer or component that is the primary client or owner of that contract.

## 5. Dependencies and Dependents

*   **`Nucleus.Shared.Kernel`**: Contains core abstractions. Has no Nucleus project dependencies. Is a dependency for almost all other projects.
*   Other layers define abstractions as needed and depend on `Nucleus.Shared.Kernel`. Implementations of these abstractions reside within the same layer or in a lower-level layer (e.g., Infrastructure implementing Domain abstractions).

## 6. Related Documents

*   [../01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [./NAMESPACE_SHARED_KERNEL.md](./NAMESPACE_SHARED_KERNEL.md) (Placeholder for the renamed `NAMESPACE_PERSONAS_CORE.md`)
*   [../../CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md](../../CoreNucleus/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md)
*   [../../Agents/01_M365_AGENTS_OVERVIEW.md](../../Agents/01_M365_AGENTS_OVERVIEW.md)
*   [../../McpTools/01_MCP_TOOLS_OVERVIEW.md](../../McpTools/01_MCP_TOOLS_OVERVIEW.md)
