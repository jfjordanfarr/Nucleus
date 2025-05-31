---
title: "Namespace - Core Logic & Shared Kernel Unit Tests"
description: "Details the unit test projects for Nucleus Core Logic (RAGLogic) and the Shared Kernel."
version: 1.3
date: 2025-05-29
parent: ../01_NAMESPACES_FOLDERS.md
---

# Projects: Core Logic & Shared Kernel Unit Tests

## 1. Overview

This document describes the unit test projects targeting the core business logic within the `Nucleus.Core.RAGLogic` project and the foundational elements within the `Nucleus.Shared.Kernel` project.

The primary goal of these projects is to ensure the correctness and reliability of individual units of business logic, services, domain entities, and shared abstractions in isolation.

## 2. Purpose and Scope

*   **Purpose:** To verify the functional correctness of components within `Nucleus.Core.RAGLogic` and `Nucleus.Shared.Kernel` through automated unit tests (Layer 1 & Layer 2 tests).
*   **Scope:**
    *   **`Nucleus.Core.RAGLogic.Tests`:** Testing public methods of services, validators, mappers, and other logic within the `Nucleus.Core.RAGLogic` namespace. Focus on RAG pipeline components, content processing, analysis logic, and interaction with (mocked) data persistence and AI model services.
    *   **`Nucleus.Shared.Kernel.Tests`:** Testing domain entities, value objects, core abstractions (interfaces, base classes), helper utilities, and validation logic within the `Nucleus.Shared.Kernel` namespace.
    *   Mocking external dependencies (e.g., repositories, infrastructure services, AI models) to ensure tests are isolated and fast.
*   **Out of Scope:**
    *   System integration testing (covered by `Nucleus.System.IntegrationTests`).
    *   End-to-end testing.
    *   Testing private methods directly (these should be tested via their public contract).

## 3. Project Structure

Unit tests will be organized into distinct test projects, mirroring the structure of the code under test:

*   **`Nucleus.Core.RAGLogic.Tests`**
    *   **Location:** `tests/Nucleus.Core.RAGLogic.Tests/Nucleus.Core.RAGLogic.Tests.csproj`
    *   **Purpose:** Unit tests for components in `Nucleus.Core.RAGLogic`.
*   **`Nucleus.Shared.Kernel.Tests`**
    *   **Location:** `tests/Nucleus.Shared.Kernel.Tests/Nucleus.Shared.Kernel.Tests.csproj`
    *   **Purpose:** Unit tests for components in `Nucleus.Shared.Kernel`.

*   **Common Test Configuration:**
    *   **Test Framework:** xUnit.net
    *   **Mocking Framework:** Moq
    *   **Assertion Library:** FluentAssertions
    *   **Organization:** Test files are typically named after the class they are testing, with a `Tests` suffix (e.g., `RAGPipelineServiceTests.cs`, `ArtifactMetadataTests.cs`). Tests within these files are organized by the method being tested and the scenario under test.

## 4. Key Components & Test Targets

*   **From `Nucleus.Core.RAGLogic` (tested in `Nucleus.Core.RAGLogic.Tests`):**
    *   RAG pipeline services and individual pipeline steps.
    *   Content extraction and processing logic.
    *   Persona-specific analysis and interpretation services.
    *   Interaction with (mocked) `IArtifactRepository`, `IPersonaKnowledgeRepository`.
    *   Interaction with (mocked) AI inference services (`Microsoft.Extensions.AI`).
    *   Validators for input and output data.
    *   Mappers between DTOs and domain entities.
*   **From `Nucleus.Shared.Kernel` (tested in `Nucleus.Shared.Kernel.Tests`):**
    *   Domain entities (e.g., `ArtifactMetadata`, `PersonaKnowledgeEntry`) - behavior, validation rules.
    *   Value objects - equality, validation.
    *   Core abstractions and interfaces (e.g., `IPersona`, `IContentProcessor`) - ensuring any default implementations or utility methods are correct.
    *   Shared helper utilities and extension methods.
    *   Cross-cutting concerns implemented within the kernel (e.g., custom validation attributes).

## 5. Dependencies

*   **Production Code (for `Nucleus.Core.RAGLogic.Tests`):**
    *   `src/Nucleus.Core.RAGLogic/Nucleus.Core.RAGLogic.csproj`
    *   `src/Nucleus.Shared.Kernel/Nucleus.Shared.Kernel.csproj` (implicitly, via RAGLogic)
    *   `src/Nucleus.Shared.Kernel.Abstractions/Nucleus.Shared.Kernel.Abstractions.csproj`
*   **Production Code (for `Nucleus.Shared.Kernel.Tests`):**
    *   `src/Nucleus.Shared.Kernel/Nucleus.Shared.Kernel.csproj`
    *   `src/Nucleus.Shared.Kernel.Abstractions/Nucleus.Shared.Kernel.Abstractions.csproj`
*   **Test Frameworks & Libraries:**
    *   `xUnit`
    *   `Moq`
    *   `FluentAssertions`
*   **Microsoft AI Extensions (for mocking in `Nucleus.Core.RAGLogic.Tests`):**
    *   `Microsoft.Extensions.AI.Abstractions`

## 6. Dependents

*   These projects are test projects and are not depended upon by application code.

## 7. Related Documents

*   [01_NAMESPACES_FOLDERS.md](../01_NAMESPACES_FOLDERS.md)
*   [02_TESTING_STRATEGY.md](../02_TESTING_STRATEGY.md)
*   [NAMESPACE_CORE_RAGLOGIC.md](./NAMESPACE_CORE_RAGLOGIC.md)
*   [NAMESPACE_SHARED_KERNEL.md](./NAMESPACE_SHARED_KERNEL.md)
*   [NAMESPACE_SHARED_KERNEL_ABSTRACTIONS.md](./NAMESPACE_SHARED_KERNEL_ABSTRACTIONS.md)
