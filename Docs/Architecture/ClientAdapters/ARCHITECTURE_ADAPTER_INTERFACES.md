---
title: Architecture - Common Client Adapter Interfaces
description: Defines conceptual C# interfaces and base classes representing common patterns and abstractions expected across different Nucleus Client Adapters (e.g., Console, Teams).
version: 1.1
date: 2025-04-22
---

# Architecture: Common Client Adapter Interfaces

This document defines conceptual C# interfaces and base classes representing common patterns and abstractions expected across different Nucleus Client Adapters (e.g., Console, Teams), as outlined in the main [Client Architecture](../05_ARCHITECTURE_CLIENTS.md). These promote consistency and allow the core Nucleus API/services to interact with adapters in a standardized way where appropriate.

Implementations detailing how these interfaces are mapped to specific platforms can be found in:
*   [Console Adapter Interfaces](./Console/ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md)
*   [Teams Adapter Interfaces](./Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md)

## 1. `IPlatformMessage`

Represents a single, discrete message or event received from the client platform that might trigger Nucleus processing.

**Purpose:** To abstract the details of a specific platform message (e.g., Teams message, Console command line input) into a common structure usable by the Nucleus core.

**Conceptual Definition:**

```csharp
/// <summary>
/// Represents a single message or event received from the client platform.
/// </summary>
public interface IPlatformMessage
{
    /// <summary>
    /// Unique identifier for the message within the platform (e.g., Teams message ID, a generated UUID for console input).
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// Identifier for the conversation or thread the message belongs to (e.g., Teams channel ID, chat ID, "console_session").
    /// </summary>
    string ConversationId { get; }

    /// <summary>
    /// Identifier for the user who sent the message (e.g., AAD Object ID, local username).
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// The primary textual content of the message (e.g., text body, command line arguments).
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Timestamp when the message was created or received on the platform.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// URIs or identifiers pointing to any attached files or resources relevant to this message
    /// (e.g., `file://` URIs for console, SharePoint/OneDrive URIs/IDs for Teams).
    /// These are pointers to the *source* artifacts managed by the platform.
    /// </summary>
    IEnumerable<string> SourceArtifactUris { get; }

    // Potentially other platform-specific metadata accessible via methods or properties.
    // T GetMetadata<T>(string key);
}
```

## 2. `IPersonaInteractionContext`

Represents the context of a single user-initiated interaction or "micro-conversation" with a Persona, potentially spanning multiple messages over a short period (e.g., 5-60 minutes of activity). It's the primary object passed from the adapter into the Nucleus core for processing a specific task.

**Purpose:** To encapsulate all necessary information and capabilities required to process a user's interaction, including user identity, the originating message(s), and access to platform-specific resources like file I/O within the security context of that interaction. This aligns with the ephemeral processing model.

**Conceptual Definition:**

```csharp
/// <summary>
/// Encapsulates the context of a single user interaction with a Persona.
/// Provides access to interaction details and platform-specific resources for the duration of the task.
/// </summary>
public interface IPersonaInteractionContext : IDisposable // Potentially disposable for resource cleanup
{
    /// <summary>
    /// Unique identifier for this specific interaction context.
    /// </summary>
    string InteractionId { get; } // Renamed from RequestId

    /// <summary>
    /// Identifier for the user initiating the interaction.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Identifier for the originating platform (e.g., "Teams", "Console").
    /// </summary>
    string PlatformId { get; }

    /// <summary>
    /// The primary message(s) that initiated or are part of this interaction context.
    /// Could be a single message or potentially a sequence.
    /// </summary>
    IEnumerable<IPlatformMessage> TriggeringMessages { get; }

    /// <summary>
    /// Provides access to read source files referenced within this interaction context
    /// (e.g., files attached to triggering messages).
    /// </summary>
    ISourceFileReader SourceFileReader { get; }

    /// <summary>
    /// Provides access to write generated output artifacts back to the platform's
    /// designated storage location for this interaction context (e.g., Team's .Nucleus folder,
    /// local console output directory).
    /// </summary>
    IOutputWriter OutputWriter { get; }

    // May include methods for:
    // - Sending reply messages back to the platform (e.g., SendReplyAsync(string content))
    // - Accessing interaction-scoped configuration or services
    // - Tracking interaction duration/metrics
}
```

## 3. `ISourceFileReader`

An interface, accessed via `IPersonaInteractionContext`, responsible for retrieving the content of source artifacts referenced by their URIs.

**Purpose:** To abstract the platform-specific mechanism for accessing file content (e.g., Microsoft Graph API calls for Teams, local `File.OpenRead` for Console) during interaction processing.

**Conceptual Definition:**

```csharp
/// <summary>
/// Provides read access to source artifacts referenced within a persona interaction context.
/// Implementation is platform-specific (e.g., Graph API, local filesystem).
/// </summary>
public interface ISourceFileReader
{
    /// <summary>
    /// Checks if a source artifact exists and is accessible at the given URI within the current interaction context.
    /// </summary>
    /// <param name="sourceUri">The platform-specific URI or identifier of the source artifact.</param>
    /// <returns>True if accessible, false otherwise.</returns>
    Task<bool> SourceExistsAsync(string sourceUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream to the content of the specified source artifact.
    /// </summary>
    /// <param name="sourceUri">The platform-specific URI or identifier of the source artifact.</param>
    /// <returns>A readable Stream.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the artifact does not exist or is inaccessible.</exception>
    Task<Stream> OpenReadSourceAsync(string sourceUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets basic metadata about the source artifact (e.g., name, size, last modified date).
    /// Actual metadata available will vary by platform.
    /// </summary>
    /// <param name="sourceUri">The platform-specific URI or identifier of the source artifact.</param>
    /// <returns>A dictionary or specific metadata object.</returns>
    Task<SourceArtifactMetadata> GetSourceMetadataAsync(string sourceUri, CancellationToken cancellationToken = default);
}

// Helper class for metadata
public record SourceArtifactMetadata(string Name, long? Size, DateTimeOffset? LastModified);
```

## 4. `IOutputWriter`

An interface, accessed via `IPersonaInteractionContext`, responsible for writing generated artifacts (including AI replies and associated media) back to the platform's designated storage area.

**Purpose:** To abstract the platform-specific mechanism for saving generated outputs (e.g., Microsoft Graph API uploads to SharePoint for Teams, local `File.Create` for Console).

**Conceptual Definition:**

```csharp
/// <summary>
/// Provides write access to save generated output artifacts within a persona interaction context's
/// designated output location (e.g., Team's .Nucleus folder, console output dir).
/// Implementation is platform-specific.
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    /// Writes the provided content stream as a generated output artifact.
    /// </summary>
    /// <param name="personaId">The ID of the persona generating the output.</param>
    /// <param name="outputName">A suggested name for the output artifact (e.g., "Q1_Summary.md", "analysis_chart.png"). The implementation may modify this for uniqueness or conventions.</param>
    /// <param name="contentStream">The stream containing the artifact content.</param>
    /// <param name="mimeType">Optional MIME type of the content.</param>
    /// <returns>The platform-specific URI or identifier of the created artifact.</returns>
    Task<string> WriteOutputAsync(
        string personaId,
        string outputName,
        Stream contentStream,
        string? mimeType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a textual reply, potentially grouping it with other outputs from the same persona interaction.
    /// This might save the text as a file (e.g., .md) in the designated output location.
    /// </summary>
    /// <param name="personaId">The ID of the persona generating the reply.</param>
    /// <param name="replyContent">The textual content of the reply.</param>
    /// <param name="suggestedFileName">A suggested base name for the file if saved (e.g., "chat_reply").</param>
    /// <returns>The platform-specific URI or identifier if saved as a distinct artifact, otherwise null.</returns>
    Task<string?> WriteTextReplyAsync(
        string personaId,
        string replyContent,
        string suggestedFileName = "reply",
        CancellationToken cancellationToken = default);

    // Could potentially include methods for creating directories if needed by the platform structure.
    // Task EnsureOutputDirectoryExistsAsync(string personaId, CancellationToken cancellationToken = default);
}

---
