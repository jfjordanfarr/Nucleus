---
title: Namespace - Nucleus.Adapters.Console
description: Describes the Console Adapter project, providing a command-line interface for interacting with the Nucleus API.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Adapters.Console

**Relative Path:** `src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/Nucleus.Infrastructure.Adapters.Console.csproj`

## 1. Purpose

This project provides a command-line interface (CLI) client for interacting with the Nucleus system. It acts as an "Adapter" in the Infrastructure Layer, translating console input/output into API calls to the `Nucleus.Services.Api`.

## 2. Key Components

*   **`Program.cs`:** Main entry point for the console application.
*   **API Client Logic:** Code responsible for constructing and sending HTTP requests to the Nucleus API endpoints (likely using `HttpClient`).
*   **Input/Output Handling:** Logic for reading user input from the console and displaying responses.
*   **Argument Parsing:** Potentially uses a library (like `System.CommandLine`) to parse command-line arguments.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References shared DTOs like `AdapterRequest`, `ArtifactReference`).
*   **Implicit Dependency:** Relies on the `Nucleus.Services.Api` being accessible via HTTP.

## 4. Dependents

*   This project is typically run directly by a user or script and is not a dependency for other projects in the solution.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [05_ARCHITECTURE_CLIENTS.md](../05_ARCHITECTURE_CLIENTS.md)
*   [10_ARCHITECTURE_API.md](../10_ARCHITECTURE_API.md)
