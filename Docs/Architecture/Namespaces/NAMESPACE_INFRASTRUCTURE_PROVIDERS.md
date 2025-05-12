---
title: Namespace - Nucleus.Infrastructure.Providers
description: Defines shared infrastructure components for providing access to external data or resources, and for initial content processing like extraction.
version: 1.1
date: 2025-05-08
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Providers

**Relative Path:** `src/Nucleus.Infrastructure/Providers/Nucleus.Infrastructure.Providers.csproj`

## 1. Purpose

This project consolidates implementations of provider interfaces defined in `Nucleus.Abstractions`. Providers are responsible for interacting with external systems or data sources to retrieve information needed by the core application (e.g., the content of an artifact referenced by a client) and for performing initial content processing tasks like extracting text from various document formats.

Placing providers in this dedicated infrastructure project ensures they are decoupled from specific client adapters (like Console or Teams) and can be readily consumed by the central `Nucleus.Services.Api` or other core services.

## 2. Key Components

This project is organized into sub-namespaces based on the type of provider.

*   **Artifact Providers (`Nucleus.Infrastructure.Providers.Artifacts`):**
    *   `IArtifactProvider` Implementations:
        *   `ConsoleArtifactProvider.cs`: ([../../../src/Nucleus.Infrastructure/Providers/Artifacts/ConsoleArtifactProvider.cs](../../../src/Nucleus.Infrastructure/Providers/Artifacts/ConsoleArtifactProvider.cs)) - Fetches content for artifacts referenced via local file system paths.
    *   **(Future Providers):** Implementations for other artifact sources (e.g., SharePoint, OneDrive, Web URLs) would reside in this sub-namespace.

*   **Content Extraction Providers (`Nucleus.Infrastructure.Providers.ContentExtraction`):**
    *   `IContentExtractor` Implementations & Helpers:
        *   `HtmlContentExtractor.cs`: ([../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/HtmlContentExtractor.cs](../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/HtmlContentExtractor.cs)) - Extracts text content from HTML documents.
        *   `PlainTextContentExtractor.cs`: ([../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/PlainTextContentExtractor.cs](../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/PlainTextContentExtractor.cs)) - Extracts text content from plain text files.
        *   `MimeTypeHelper.cs`: ([../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/MimeTypeHelper.cs](../../../src/Nucleus.Infrastructure/Providers/ContentExtraction/MimeTypeHelper.cs)) - Utility for working with MIME types, often used by content extractors.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References interfaces like `IArtifactProvider`, `IContentExtractor` and shared models like `ArtifactReference`, `ArtifactContent`, `ContentExtractionResult`).

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (Uses these providers via Dependency Injection to resolve artifact content).

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [IArtifactProvider Interface](../../../src/Nucleus.Abstractions/IArtifactProvider.cs)
*   [01_ARCHITECTURE_PROCESSING.md](../01_ARCHITECTURE_PROCESSING.md) (Describes the role of artifact providers in ingestion)
