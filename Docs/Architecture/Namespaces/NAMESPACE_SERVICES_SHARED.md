---
title: Namespace - Nucleus.Services.Shared
description: Defines shared service components and abstractions used across multiple layers, focusing on infrastructure-agnostic tasks.
version: 1.0
date: 2025-04-29
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Namespace: Nucleus.Services.Shared

## 1. Purpose and Scope

The `Nucleus.Services.Shared` project and namespace contain reusable components, interfaces, and implementations that provide common functionalities needed by various parts of the Nucleus application, particularly those that bridge domain logic and infrastructure concerns without introducing direct dependencies on specific infrastructure implementations.

Key characteristics:
*   **Cross-Cutting Concerns:** Houses logic applicable across multiple architectural layers or services.
*   **Infrastructure Agnosticism:** Components here should ideally not depend directly on specific database technologies, external APIs (unless abstracting them), or UI frameworks.
*   **Shared Abstractions & Implementations:** May define interfaces (`IContentExtractor`) and potentially default or common implementations (`PlainTextContentExtractor`, `HtmlContentExtractor`).

## 2. Key Components

*   **`Extraction/`**
    *   `IContentExtractor`: Interface for extracting textual content from various source streams based on content type.
    *   `ContentExtractionResult`: DTO representing the outcome of an extraction operation.
    *   `PlainTextContentExtractor`, `HtmlContentExtractor`: Concrete implementations for specific content types.
*   **(Future components)**: Could include shared utilities for caching, validation, or other common service-level tasks.

## 3. Dependencies

*   **Depends On:** `Nucleus.Abstractions` (for base models, constants).
*   **Depended On By:** `Nucleus.Domain.Processing`, potentially `Nucleus.Application` (if created), `Nucleus.Services.Api` (for DI registration).

## 4. Design Principles

*   Keep components focused and cohesive.
*   Avoid introducing dependencies on higher-level layers (like specific Adapters or the API service).
*   Ensure interfaces are well-defined and implementations are testable.