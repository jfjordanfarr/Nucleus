---
title: Processing Architecture - Shared Interfaces
description: Defines common C# interfaces used by Nucleus MCP Tool/Server applications, often invoked by M365 Persona Agents.
version: 2.0
date: 2025-05-25
parent: ../01_ARCHITECTURE_PROCESSING.md
---

# Processing Architecture: Shared Interfaces

This document defines common C# interfaces shared by different components within the Nucleus processing architecture, promoting consistency and modularity, particularly for **Nucleus MCP Tool/Server applications**. These tools are typically invoked by **Microsoft 365 Persona Agents** acting as orchestrators.

It complements the main [Processing Architecture document](../01_ARCHITECTURE_PROCESSING.md).

## 1. `IContentExtractor`

This interface provides a standard way for **Nucleus MCP Tool/Server applications** to handle the *initial parsing* and content retrieval from different source artifact types before further processing or synthesis. An M365 Persona Agent might invoke an MCP Tool that utilizes an `IContentExtractor` implementation.

*   **Code:** `Nucleus.Abstractions/Extraction/IContentExtractor.cs` (Conceptual path; actual location TBD in new structure)
*   **Related Architecture:** [ARCHITECTURE_PROCESSING_INGESTION.md](./ARCHITECTURE_PROCESSING_INGESTION.md)

```csharp
/// <summary>
/// Interface for services that can extract textual content (and potentially other data)
/// from a source artifact stream.
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// Checks if this extractor supports the given MIME type.
    /// </summary>
    bool SupportsMimeType(string mimeType);

    /// <summary>
    /// Extracts content from the provided stream.
    /// </summary>
    /// <param name="sourceStream">The stream containing the artifact content.</param>
    /// <param name="mimeType">The MIME type of the content.</param>
    /// <param name="sourceUri">Optional URI of the source for context.</param>
    /// <returns>A result containing extracted text and potentially other metadata. See ContentExtractionResult.</returns>
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, string? sourceUri = null);
}

/// <summary>
/// Holds the result of content extraction.
/// </summary>
public record ContentExtractionResult(
    string ExtractedText,
    Dictionary<string, object>? AdditionalMetadata = null, // e.g., page numbers, structural info
    string? SourceUri = null,
    Exception? Exception = null);
```

## 2. `IArtifactStorageProvider` (Conceptual - Replaces aspects of old Orchestration)

This conceptual interface represents the capabilities of a **Nucleus MCP Tool/Server application** (e.g., `Nucleus_KnowledgeStore_McpServer`) to manage the storage and retrieval of `ArtifactMetadata` and `PersonaKnowledgeEntry` objects. M365 Persona Agents would interact with such an MCP Tool to persist or fetch processed information.

*   **Code:** TBD (Likely within a `Nucleus.Mcp.KnowledgeStore.Abstractions` or similar project)
*   **Related Architecture:**
    *   [Storage Architecture](../../03_ARCHITECTURE_STORAGE.md)
    *   [Database Architecture](../../04_ARCHITECTURE_DATABASE.md)

```csharp
// Conceptual interface - details to be refined
using Nucleus.Abstractions.Models; // Assuming ArtifactMetadata, PersonaKnowledgeEntry are here or moved
using System.Threading.Tasks;

namespace Nucleus.Mcp.KnowledgeStore.Abstractions; // Example namespace

/// <summary>
/// Defines the contract for a service that provides access to the Nucleus knowledge store,
/// managing ArtifactMetadata and PersonaKnowledgeEntry objects.
/// This would be implemented by an MCP Tool/Server.
/// </summary>
public interface IArtifactStorageProvider
{
    /// <summary>
    /// Stores artifact metadata.
    /// </summary>
    Task StoreArtifactMetadataAsync(ArtifactMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves artifact metadata.
    /// </summary>
    Task<ArtifactMetadata?> GetArtifactMetadataAsync(string artifactId, string personaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a persona knowledge entry.
    /// </summary>
    Task StorePersonaKnowledgeAsync<T>(PersonaKnowledgeEntry<T> entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a persona knowledge entry.
    /// </summary>
    Task<PersonaKnowledgeEntry<T>?> GetPersonaKnowledgeAsync<T>(string entryId, string personaId, CancellationToken cancellationToken = default);

    // Other methods for querying, deleting, etc.
}
```

## 3. `IMcpToolInvoker` (Conceptual - Resides in M365 Agent)

This conceptual interface would reside within an **M365 Persona Agent** project. It defines how the agent invokes various backend **Nucleus MCP Tool/Server applications**. This replaces the direct, in-process invocation of services like the old `IOrchestrationService`.

*   **Code:** TBD (Likely within an `M365.Ageny.MyPersona.Core` or similar project)
*   **Related Architecture:**
    *   [Processing Orchestration](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md) (as it will describe how Agents use these invokers)
    *   [Abstractions](../../12_ARCHITECTURE_ABSTRACTIONS.md) (for MCP details)

```csharp
// Conceptual interface - details to be refined
using Microsoft.Bot.Builder; // Example, if using Bot Framework for the Agent
using System.Threading.Tasks;

namespace M365.Agent.MyPersona.Core; // Example namespace

/// <summary>
/// Defines the contract for a component within an M365 Persona Agent
/// responsible for invoking backend Nucleus MCP Tool/Server applications.
/// </summary>
public interface IMcpToolInvoker
{
    /// <summary>
    /// Invokes a specified MCP Tool with the given request payload.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request payload for the MCP Tool.</typeparam>
    /// <typeparam name="TResponse">The expected type of the response from the MCP Tool.</typeparam>
    /// <param name="toolName">The identifier of the MCP Tool to invoke (e.g., "Nucleus_ContentExtractor_McpServer", "Nucleus_KnowledgeStore_McpServer").</param>
    /// <param name="requestPayload">The request data for the tool.</param>
    /// <param name="turnContext">The current turn context from the M365 Agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the MCP Tool.</returns>
    Task<TResponse?> InvokeToolAsync<TRequest, TResponse>(
        string toolName,
        TRequest requestPayload,
        ITurnContext turnContext, // Or other relevant M365 Agent context
        CancellationToken cancellationToken = default);
}
```

## 4. `IPlatformMessageRelay` (Conceptual - Replaces `IPlatformNotifier`)

This conceptual interface would be part of an **M365 Persona Agent**. It's responsible for sending messages back to the host platform (e.g., Teams, Outlook) using the M365 Agent SDK's capabilities. This replaces the older `IPlatformNotifier` which was designed for a monolithic backend.

*   **Code:** TBD (Likely within an `M365.Agent.MyPersona.Core` or similar project)
*   **Related Architecture:**
    *   [Clients Architecture](../../05_ARCHITECTURE_CLIENTS.md) (as M365 Agents are the clients)

```csharp
// Conceptual interface - details to be refined
using Microsoft.Bot.Builder; // Example, if using Bot Framework for the Agent
using System.Threading.Tasks;

namespace M365.Agent.MyPersona.Core; // Example namespace

/// <summary>
/// Defines a contract for sending messages and notifications back to the
/// originating platform via the M365 Agent SDK.
/// </summary>
public interface IPlatformMessageRelay
{
    /// <summary>
    /// Asynchronously sends a message back to the originating platform conversation.
    /// </summary>
    Task SendMessageAsync(
        ITurnContext turnContext, // M365 Agent's turn context
        string messageText,
        string? replyToActivityId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a typing indicator.
    /// </summary>
    Task SendTypingIndicatorAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken = default);

    // Potentially other methods for sending cards, adaptive cards, etc.
}
```

---
**(More interfaces will be defined or refined as the M365 Agent and MCP Tool architecture evolves, particularly focusing on the specific contracts exposed by MCP Tools and consumed by M365 Agents.)**
