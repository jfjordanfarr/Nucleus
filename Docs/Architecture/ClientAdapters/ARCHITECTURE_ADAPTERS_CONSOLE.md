---
title: Client Adapter - Console
description: Describes a basic interaction surface with Nucleus personas, tailored for accelerated local development and providing a lightweight simulation environment for core adapter interactions.
version: 1.6
date: 2025-04-24
---

# Client Adapter: Console

## Overview

Describes a basic command-line interaction surface for Nucleus. **Crucially, under the API-First architecture, the Console Adapter functions *exclusively* as a client to the `Nucleus.Services.Api`.** It translates user commands and inputs into specific HTTP requests targeted at the API service and renders the responses received from the API back to the console.

Its primary purpose is for local development and testing, providing a way to directly interact with the API endpoints for core functionalities like querying and ingestion. It serves as the Minimum Viable Product (MVP) client described in the main [Client Architecture document](../05_ARCHITECTURE_CLIENTS.md).

This adapter interacts with the API based on contracts implicitly defined by the API endpoints, rather than necessarily implementing the shared interfaces from [ARCHITECTURE_ADAPTER_INTERFACES.md](./ARCHITECTURE_ADAPTER_INTERFACES.md) directly (as those might be geared towards adapters with more platform integration).

## Auth

No specific authentication model is required for the basic console adapter beyond potential network-level security for accessing the API service. It runs under the local user's context.

## Interaction Handling & API Forwarding

As a pure client to the `Nucleus.Services.Api`, the Console Adapter's primary role is translating user commands into API requests.

1.  **Parse Command Line:** The application uses a command-line parsing library (e.g., `System.CommandLine`) to interpret user input, arguments, and options (e.g., `nucleus query "Analyze this data" --persona Coder`, `nucleus ingest --path ./data.csv`).
2.  **Extract Context:** Based on the parsed command, the adapter extracts:
    *   The intended action (query, ingest, etc.).
    *   The primary content (query text, file path for ingestion).
    *   Any relevant parameters (target persona, output flags).
    *   User context is implicitly the local machine user.
    *   Session context might be generated per run or passed via arguments.
3.  **Detect Reply Context:** The standard console environment lacks the threaded conversation or direct message reply features of chat platforms. Therefore, the Console Adapter **does not** populate reply-specific fields (like `RepliedToPlatformMessageId`) in the API request. Each command is treated as a distinct interaction initiation.
4.  **Construct API Request:** The adapter maps the extracted information to the appropriate DTO (e.g., `InteractionRequest`, `IngestionRequest`) defined by the `Nucleus.Services.Api`.
    *   For path-based ingestion, this involves constructing the `IngestionRequestPath` DTO as defined in [API Ingestion Endpoints](../Api/ARCHITECTURE_API_INGESTION.md).
5.  **Forward to API:** The adapter makes an authenticated HTTP request (e.g., `POST /api/v1/interactions`, `POST /api/v1/ingest/path`) to the central Nucleus API endpoint with the populated DTO.
    *   See [API Client Interaction Pattern](../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md) for the `/interactions` endpoint.
    *   See [API Ingestion Endpoints](../Api/ARCHITECTURE_API_INGESTION.md) for the `/ingest/path` endpoint.
6.  **Handle API Response:**
    *   **Synchronous:** If the API returns an immediate result (HTTP 200 OK + body), the adapter parses the response. Textual content is printed to the console. If the response indicates binary data or a file, it may be saved locally based on configuration or flags (see 'Generated Artifact Handling').
    *   **Asynchronous:** If the API returns HTTP 202 Accepted, the adapter typically prints a confirmation message (e.g., "Ingestion job submitted with ID: {jobId}"). It might then:
        *   Exit.
        *   Provide a separate command to check job status (`nucleus status --jobId {jobId}`).
        *   (Less common for console) Implement polling within the original command to wait for completion.

## Generated Artifact Handling

**The Console Adapter interacts with the local filesystem only to save outputs received *from* the API.** It does not perform any independent artifact processing or management, nor does it read local files for uploading.

*   **Ingestion (Sending):** When a user initiates an ingestion command (e.g., `nucleus ingest --path ./myfile.txt`):
    1.  The adapter parses the command and extracts the file path.
    2.  It constructs an API request (e.g., `POST /api/v1/ingest/path`) containing the *file path* and any other relevant metadata (like target persona, user context).
    3.  It **does not** read the file content locally.
    4.  It sends this request to the `Nucleus.Services.Api`.
    5.  The `ApiService` receives the request containing the file path. It is the **API Service's responsibility** to resolve this path (potentially relative to a configured base path or requiring specific permissions) and read the file content for the actual ingestion logic (processing, data extraction, metadata storage, etc.).
*   **Generated Outputs (Handling):** Outputs generated by personas (e.g., summaries, analysis results, visualizations) are received *from the Nucleus API* as part of query responses.
    *   **Terminal Display:** By default, textual outputs from the API response are displayed directly in the terminal.
    *   **Local File Persistence (Optional):**
        *   If the API response includes binary data or indicates a generated file, the adapter **can** support saving this received data to a configurable local directory (e.g., `./.nucleus/console_outputs/` relative to the working directory, which should be added to `.gitignore`).
        *   This behavior might be enabled via a configuration setting or a command-line flag (e.g., `--save-output`).
        *   This is useful for non-textual artifacts received from the API (e.g., images, PDFs, HTML files) or for easily reviewing complex textual outputs.
        *   Files could be named using conventions like `{Timestamp}_{OutputName}.ext` based on details from the API response.
    *   **Explicit Output Path:** Commands may still support an explicit `--output <file_path>` argument to override the default save location for a specific execution.
*   **No Backend Persistent Storage Management:** The Console Adapter itself does not manage persistent storage. It relies entirely on the `Nucleus.Services.Api` for state management and persistence.

## Messaging

The console uses a simple linear sequence of inputs and outputs, translated into API calls.

*   **Platform Representation:** A sequence of user inputs and application outputs rendered from API responses.
*   **Nucleus `ArtifactMetadata` Mapping:** Metadata describing the interaction (e.g., source system, timestamps) is constructed *by the adapter* before sending requests to the API. The API response may contain further metadata about generated artifacts.
    *   `sourceSystemType`: Set to `Console` by the adapter.
    *   Identifiers (`sourceIdentifier`, session IDs) are generated by the adapter to correlate requests and responses within a single run.
    *   Other metadata fields are populated by the adapter based on the command/input.

## Conversations

Conversation context and session management are primarily the responsibility of the `Nucleus.Services.Api`. The console adapter simply passes necessary identifiers (like a session ID generated at startup or for a specific interaction) within its API requests.

*   **Session Context:** Each run of the console application typically starts a new logical session, identified by a unique ID sent with API requests.
*   **No Local State:** The adapter does not maintain its own conversational state between runs. It relies on the API to manage context based on the provided session/interaction identifiers.

## Attachments

Standard console I/O does not have a native concept of attachments.

*   **Handling:** File paths provided to the `ingest` command are sent *as paths* within the API request to the `Nucleus.Services.Api`. The **API Service** is responsible for resolving the path and accessing the file content. The Console Adapter itself **does not** read the file content for ingestion purposes.
*   See [API Ingestion Endpoints](../Api/ARCHITECTURE_API_INGESTION.md) for the specific API contract used.

## Rich Presentations and Embedded Hypermedia

Presentation capabilities are limited to standard console text output. The adapter **cannot render** rich HTML, interactive visualizations, or images directly.

*   **Artifact Referencing:** If the `ApiService` generates a rich artifact (e.g., an HTML visualization) and the API response includes a way to retrieve it (e.g., another API endpoint, or the content itself), the console adapter's role is limited to:
    *   Displaying textual representations or summaries received from the API.
    *   Saving the received artifact content to a local file (as described in Generated Artifact Handling).
    *   Presenting a reference (like the saved file path or a `file://` hyperlink) allowing the developer to open and view it manually.
*   **Simulation Focus:** The adapter facilitates testing the API's ability to *generate* and *return* rich artifact data, not rendering it.

## Limitations

Presentation capabilities are limited to standard console text output received from the API.

*   **Limitations:** No support for interactive elements, complex formatting (beyond basic ANSI colors/styles), or embedded visualizations like those described in [ARCHITECTURE_PROCESSING_DATAVIZ.md](../Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md). The adapter merely displays what the API provides or saves received files.
