---
title: Client Adapter - Console
description: Describes a basic interaction surface with Nucleus personas, tailored for accelerated local development and providing a lightweight simulation environment for core adapter interactions.
version: 1.1
date: 2025-04-13
---

# Client Adapter: Console


## Overview

Describes a basic interaction surface with Nucleus personas, tailored for accelerated local development and providing a lightweight simulation environment for core adapter interactions.

## Auth

No specific authentication model is required for the basic console adapter. It runs under the local user's context.

## Generated Artifact Handling

While the console itself isn't a persistent store, it interacts with the **user's local filesystem** to handle generated artifacts.

*   **Configured Local Directory:** Generated artifacts (e.g., files, `viz.html`) are saved to a configurable local directory (e.g., `./.nucleus/console_artifacts/` within the workspace, potentially gitignored). This provides developers with easy access to outputs.
*   **Storage Interface:** The adapter still uses the standard `IFileStorage` abstraction, but its implementation writes/reads directly to/from this configured local path.
*   **`ArtifactMetadata`:** `ArtifactMetadata` records are generated as usual, but the `sourceUri` might be a `file://` URI pointing to the local path, and `sourceSystemType` would indicate `LocalFilesystem` or similar.

## Messaging

The console uses a simple linear sequence of inputs and outputs.

*   **Platform Representation:** A sequence of user inputs and application outputs.
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   Each line of user input and each distinct block of Nucleus output can be treated as a separate artifact.
    *   `sourceSystemType`: Set to `Console`.
    *   `sourceIdentifier`: Generated based on a session ID and sequence number (e.g., `console://session-uuid/input-0`, `console://session-uuid/output-1`).
    *   `sourceUri`: Not applicable.
    *   `displayName`: First N characters of the input/output text.
    *   `sourceCreatedAt`: Timestamp of the input/output event.

## Conversations

While lacking the rich threading of platforms like Teams or Slack, the console adapter can **simulate basic conversation context** for development and testing purposes.

*   **Session Context:** Each run of the console application can be considered a session. Input/output sequences within a session can be linked.
*   **(Optional) Local State:** A simple local mechanism (e.g., session IDs maintained in memory, a lightweight SQLite database, or simple state files within the configured local directory) could be used to group interactions within a session or even across short-lived sessions, allowing simulation of `replyTo` or simple threading logic for testing Personas.
*   **Nucleus `ArtifactMetadata` Mapping:**
    *   `parentSourceIdentifier`: Could link to a conceptual "session artifact" or a previous turn within the session simulation.
    *   `replyToSourceIdentifier`, `threadSourceIdentifier`: Can be populated based on the simulated local state if implemented.

## Attachments

Standard console I/O does not have a native concept of attachments like other communication platforms.

*   **Handling:** File paths provided as input arguments are the primary way to reference external files.

## Rich Presentations and Embedded Hypermedia

Presentation capabilities are limited to standard console text output. The adapter **cannot render** rich HTML, interactive visualizations, or images directly.

*   **Artifact Referencing:** However, when a Persona generates a rich artifact (like a `viz.html` file stored locally via the Generated Artifact Handling mechanism described above), the console adapter **can present a reference** to it (e.g., printing the local file path or a `file://` hyperlink) allowing the developer to open and view it manually.
*   **Simulation Focus:** The primary goal is to test the Persona's ability to *request* and *generate* the rich artifact payload, not the final rendering within the console itself.

## Limitations

Presentation capabilities are limited to standard console text output.

*   **Limitations:** No support for interactive elements, complex formatting (beyond basic ANSI colors/styles), or embedded visualizations like those described in [ARCHITECTURE_PROCESSING_DATAVIZ.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md:0:0-0:0).
