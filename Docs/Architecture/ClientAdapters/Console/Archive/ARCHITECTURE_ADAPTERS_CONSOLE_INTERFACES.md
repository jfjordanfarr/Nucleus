---
title: Architecture - Console Adapter API Mapping
description: Details how Console Adapter commands map to specific HTTP requests against the Nucleus API Service in the API-First model.
version: 2.0
date: 2025-04-23
---

# Console Adapter: API Mapping (API-First)

**Note:** In the API-First architecture, the Console Adapter (`Nucleus.Console`) functions *exclusively* as a client to the `Nucleus.Services.Api`. It **does not** directly implement the shared adapter interfaces (`IPlatformMessage`, `IPersonaInteractionContext`, `ISourceFileReader`, `IOutputWriter`) defined in `../ARCHITECTURE_ADAPTER_INTERFACES.md`. Those interfaces are implemented and utilized *within* the `Nucleus.Services.Api`.

This document outlines how standard Console Adapter commands translate into HTTP requests made to the API Service.

## Console Command -> API Request Mapping

The adapter uses `System.CommandLine` for parsing and typically interacts with the API via the `NucleusApiServiceAgent` (which wraps `HttpClient`).

### 1. `ingest --path <local_file_path>`

*   **Action:** Reads the specified local file content.
*   **HTTP Method:** `POST`
*   **API Endpoint:** `/api/ingest/content` (Example endpoint)
*   **Request Body:** JSON payload containing `FileName` and `ContentBase64` (file content encoded as Base64).
*   **Adapter Responsibility:** Read local file, encode content, construct request, call API.
*   **API Responsibility:** Receive content, handle ingestion logic.

### 2. `query "<prompt>"`

*   **Action:** Sends a query prompt.
*   **HTTP Method:** `POST`
*   **API Endpoint:** `/api/query` (Example endpoint)
*   **Request Body:** JSON payload containing `QueryText` and `SessionId`.
*   **Adapter Responsibility:** Construct request with prompt and session ID, call API.
*   **API Responsibility:** Process query, interact with Personas/LLM, manage context, return response.

## API Response -> Console Output Mapping

*   **Textual Responses:** Text results from the API's JSON response are written to the standard console output.
*   **File/Binary Responses:** If the API returns binary data, the adapter saves it to a local file (if configured) and prints the file path to the console.

## Obsolete Interface Implementations

The following interfaces are **no longer implemented** by the Console Adapter in the API-First model:

*   `IPlatformMessage`
*   `IPersonaInteractionContext`
*   `ISourceFileReader`
*   `IOutputWriter`

Their functionality is now handled internally by the `Nucleus.Services.Api`.
