<!-- 
**THIS DOCUMENT IS ARCHIVED**

This document is no longer actively maintained and is preserved for historical context only. 
Refer to the main architectural documents for current information.
-->

<!-- filepath: /workspaces/Nucleus/Docs/Architecture/DevelopmentLifecycle/Namespaces/NAMESPACE_SERVICES_SHARED.md -->
---
title: "Namespace - Nucleus.Services.Shared (DEPRECATED)"
description: "[DEPRECATED] Formerly defined shared service components. Its responsibilities have been refactored."
version: 1.1
date: 2025-05-08
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Namespace: Nucleus.Services.Shared (DEPRECATED)

## 1. Status: DEPRECATED

**This project and namespace (`Nucleus.Services.Shared`) are deprecated and are targeted for removal in a future refactoring effort.**

Its original purpose was to house reusable components and interfaces, primarily for content extraction. These responsibilities have been refactored into more appropriate, specialized projects:

*   **Abstractions for content extraction** (like `IContentExtractor` and `ContentExtractionResult`) are now located in the [**`Nucleus.Abstractions`**](../NAMESPACE_ABSTRACTIONS.md) project, specifically under the `Nucleus.Abstractions.Extraction` and `Nucleus.Abstractions.Orchestration` namespaces respectively.
*   **Concrete implementations of content extractors** (like `PlainTextContentExtractor` and `HtmlContentExtractor`) are now located in the [**`Nucleus.Infrastructure.Providers`**](../NAMESPACE_INFRASTRUCTURE_PROVIDERS.md) project, under the `Nucleus.Infrastructure.Providers.ContentExtraction` namespace.

This change aligns with clearer separation of concerns, placing abstractions in `Nucleus.Abstractions` and their concrete, potentially infrastructure-aware, implementations in `Nucleus.Infrastructure` sub-projects.

## 2. Original Purpose and Scope (Historical)

The `Nucleus.Services.Shared` project and namespace originally contained reusable components, interfaces, and implementations that provided common functionalities. Its primary focus was on content extraction services.

Key characteristics (historical):
*   Cross-Cutting Concerns: Housed logic applicable across multiple architectural layers or services.
*   Infrastructure Agnosticism: Components here were intended to not depend directly on specific database technologies or external APIs.
*   Shared Abstractions & Implementations: Defined `IContentExtractor` and its common implementations.

## 3. Key Components (Moved)

The following key components were previously part of this namespace but have been relocated:

*   **`Extraction/`**
    *   `IContentExtractor`: Now in `Nucleus.Abstractions.Extraction`.
    *   `ContentExtractionResult`: Now in `Nucleus.Abstractions.Orchestration`.
    *   `PlainTextContentExtractor`, `HtmlContentExtractor`: Now in `Nucleus.Infrastructure.Providers.ContentExtraction`.

This project should no longer contain significant components.

## 4. Dependencies (Historical Context / Current Status)

*   **Original Dependencies:** Depended on `Nucleus.Abstractions`.
*   **Original Dependents:** Was depended on by `Nucleus.Domain.Processing` and `Nucleus.Services.Api`.
*   **Current Status:** After refactoring, other projects should no longer depend on `Nucleus.Services.Shared` for the functionalities that have been moved. The project itself should have minimal to no dependencies if it's slated for complete removal.

## 5. Design Principles (Historical)

*   Keep components focused and cohesive.
*   Avoid introducing dependencies on higher-level layers (like specific Adapters or the API service).
*   Ensure interfaces are well-defined and implementations are testable.