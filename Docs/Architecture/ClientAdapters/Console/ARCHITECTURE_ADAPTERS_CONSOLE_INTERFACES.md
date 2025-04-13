---
title: Architecture - Console Adapter Interface Mapping
description: Details how the common client adapter interfaces (IPersonaInteractionContext, IPlatformMessage, etc.) are implemented within the Console adapter using System.CommandLine and local filesystem I/O.
version: 1.0
date: 2025-04-13
---

# Console Adapter: Interface Mapping

This document details the specific implementation patterns for the common client adapter interfaces defined in `../ARCHITECTURE_ADAPTER_INTERFACES.md` within the context of the [Console adapter (`Nucleus.Console`)](../ARCHITECTURE_ADAPTERS_CONSOLE.md), primarily leveraging `System.CommandLine` and standard .NET filesystem/console APIs.

## 1. `IPlatformMessage` Implementation

Corresponds to a single parsed command invocation via `System.CommandLine`.

*   **`MessageId`**: A unique ID generated for this specific command invocation (e.g., `Guid.NewGuid().ToString()`), as console input doesn't have an inherent persistent ID.
*   **`ConversationId`**: Can be considered a constant like `"console_session"` or potentially derived from the current working directory or a session identifier if multi-session support is added later. For simplicity, `"console_session"` is likely sufficient initially.
*   **`UserId`**: Maps to the local system's current user principal name or username (e.g., `Environment.UserName`).
*   **`Content`**: Represents the core arguments or text provided to the command after parsing (e.g., the `<prompt>` value in `nucleus query --persona <name> "<prompt>"`). Command names/options themselves are handled by `System.CommandLine` before this stage.
*   **`Timestamp`**: `DateTimeOffset.UtcNow` at the time the command is processed.
*   **`SourceArtifactUris`**: Derived from command-line arguments specifying local file paths (e.g., the `<filepath>` in `nucleus ingest --path <filepath>`). These should be resolved to absolute `file://` URIs.

## 2. `IPersonaInteractionContext` Implementation

Represents the scope of processing a single command execution. It's typically created within the command's handler delegate provided to `System.CommandLine`.

*   **`InteractionId`**: The same unique ID generated for the corresponding `IPlatformMessage`.
*   **`UserId`**: Sourced from `Environment.UserName`.
*   **`PlatformId`**: Hardcoded as `"Console"`.
*   **`TriggeringMessages`**: Contains the `IPlatformMessage` wrapper around the current command invocation details.
*   **`SourceFileReader`**: An instance of a `ConsoleSourceFileReader` (see below).
*   **`OutputWriter`**: An instance of a `ConsoleOutputWriter` (see below). Requires access to configuration specifying the output directory (if configured).
*   **Disposal (`Dispose`)**: Might clean up temporary resources if any were created. Generally less complex than the Teams context.
*   **Additional Capabilities**: Might hold references to console output mechanisms (like `Spectre.Console`'s `IAnsiConsole`) or command-specific parsed options.

## 3. `ISourceFileReader` Implementation (`ConsoleSourceFileReader`)

Uses standard .NET `System.IO` APIs.

*   **`SourceExistsAsync(string sourceUri, ...)`**: Parses the `file://` URI, converts it to a local path, and uses `File.Exists(path)`. Asynchronous wrapper might be trivial (`Task.FromResult(File.Exists(path))`).
*   **`OpenReadSourceAsync(string sourceUri, ...)`**: Parses the `file://` URI, converts it to a local path, and returns `File.OpenRead(path)`. Requires error handling for `FileNotFoundException`, `IOException`, etc. Wraps the synchronous call in `Task.FromResult`.
*   **`GetSourceMetadataAsync(string sourceUri, ...)`**: Parses the `file://` URI, uses `FileInfo` to get the `Name`, `Length`, and `LastWriteTimeUtc`. Wraps in `Task.FromResult`.

## 4. `IOutputWriter` Implementation (`ConsoleOutputWriter`)

Uses standard .NET `System.IO` APIs for file writing and console APIs for display.

*   **`WriteOutputAsync(string personaId, string outputName, Stream contentStream, ...)`**:
    1.  Checks configuration for the designated output directory (e.g., `./.nucleus/console_outputs/`) and whether saving is enabled.
    2.  If enabled, constructs the full output path (e.g., `./.nucleus/console_outputs/{personaId}_{timestamp}_{outputName}`). Ensures the directory exists (`Directory.CreateDirectory`).
    3.  Uses `File.Create(path)` to get a `FileStream` and copies the `contentStream` into it asynchronously.
    4.  Returns the absolute `file://` URI of the created file.
    5.  Also prints a message to the console indicating the file was saved (e.g., "Output saved to: <path>").
*   **`WriteTextReplyAsync(string personaId, string replyContent, ...)`**:
    *   **Option 1 (Primary):** Writes the `replyContent` directly to the standard console output stream (e.g., using `Console.WriteLine` or `Spectre.Console`). Returns `null`.
    *   **Option 2 (If Saving Enabled):** In addition to console output, *may* also use `WriteOutputAsync` logic to save the `replyContent` as a file (e.g., `.txt` or `.md`) in the configured output directory. Returns the `file://` URI if saved.
