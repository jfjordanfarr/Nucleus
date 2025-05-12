---
title: Processing Architecture - Shared Interfaces
description: Defines common C# interfaces used across various stages of the Nucleus processing pipeline, coordinated via the API service, supporting multi-persona scoped operations.
version: 1.5
date: 2025-05-08
parent: ../01_ARCHITECTURE_PROCESSING.md
---

# Processing Architecture: Shared Interfaces

This document defines common C# interfaces shared by different components within the Nucleus processing architecture, promoting consistency and modularity. It complements the main [Processing Architecture document](../01_ARCHITECTURE_PROCESSING.md) and is related to other interfaces such as `IArtifactProvider`, `IChatClient`, and `IPlatformNotifier`.

In a multi-persona, multi-tenant environment, these interfaces (especially those involved in orchestration and data handling) are designed to operate within specific scopes defined by `TenantId` and the resolved `PersonaId`.

## 1. `IContentExtractor`

This interface provides a standard way to handle the *initial parsing* and content retrieval from different source artifact types before further processing or synthesis.

*   **Code:** [../../src/Nucleus.Abstractions/Extraction/IContentExtractor.cs](../../src/Nucleus.Abstractions/Extraction/IContentExtractor.cs)
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
    /// <returns>A result containing extracted text and potentially other metadata. See ContentExtractionResult in Nucleus.Abstractions.Orchestration.</returns>
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

## 2. `IOrchestrationService`

This interface defines the central coordinating service for processing incoming requests within the `Nucleus.Services.Api`. It's responsible for managing the overall flow for tasks like ingestion or querying. **In the API-First architecture, implementations of this service are typically invoked by the API's request handlers (e.g., ASP.NET Core Controllers) after initial request validation, authentication, and activation checks (`IActivationChecker`) which include resolving the target `PersonaId`.** It orchestrates steps like resolving the persona (`IPersonaResolver`), fetching artifact content (via [`IArtifactProvider`](../Abstractions/ARCHITECTURE_ABSTRACTIONS_PROVIDER.md) operating under the resolved Persona's scope), invoking specific processors (e.g., content extraction, chunking, embedding, LLM interaction using `IChatClient` from the `Microsoft.Extensions.AI` library or similar AI abstractions), managing interaction context (scoped by `TenantId` and `PersonaId`), and potentially triggering notifications (via `IPlatformNotifier`) or responses.

*(Note: The service handles both initial interaction processing (`ProcessInteractionAsync`) which includes activation checks and sync/async routing for a specific `TenantId` and resolved `PersonaId`, and the subsequent processing of queued asynchronous requests (`HandleQueuedInteractionAsync`) which are already scoped by `TenantId` and `PersonaId` from the queue message.)*

*   **Code:** [../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs](../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs)
*   **Related Architecture:**
    *   [Ingestion Overview](./ARCHITECTURE_PROCESSING_INGESTION.md)
    *   [Orchestration Overview](./ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   [API Interaction Lifecycle](./Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md)

```csharp
using Nucleus.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents the possible outcomes of the orchestration process for an interaction.
/// </summary>
public enum OrchestrationStatus { /* ... See C# file for details ... */ }

/// <summary>
/// Encapsulates the result of the orchestration process.
/// </summary>
public record OrchestrationResult(OrchestrationStatus Status, AdapterResponse? Response);

/// <summary>
/// Defines the contract for the central orchestration service responsible for
/// processing interactions received by the API service. Operations are implicitly
/// or explicitly scoped by TenantId and the resolved PersonaId.
/// </summary>
/// <remarks>
/// Handles activation (including Persona resolution), routing (sync/async), and delegates to specific processing logic.
/// </remarks>
public interface IOrchestrationService
{
    /// <summary>
    /// Processes an incoming interaction request, handling activation checks (including Persona resolution),
    /// routing, and initiating processing (sync or async) for a given Tenant and resolved Persona.
    /// The AdapterRequest should contain necessary information for Tenant and Persona resolution (e.g., TenantId, originating bot ID).
    /// </summary>
    Task<OrchestrationResult> ProcessInteractionAsync(
        AdapterRequest request, // Contains TenantId and cues for PersonaId resolution
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an interaction request that has been dequeued for asynchronous processing.
    /// The NucleusIngestionRequest already contains the resolved TenantId and PersonaId.
    /// </summary>
    Task<OrchestrationResult?> HandleQueuedInteractionAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default);
}

## 3. `IPlatformNotifier`

*   **Code:** [../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs](../../../src/Nucleus.Abstractions/Adapters/IPlatformNotifier.cs)
*   **Purpose:** Defines an interface for services that can send notifications (messages, acknowledgements) back to a specific platform (e.g., Teams, Slack, Console).
*   **Key Responsibilities:**

```csharp
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines a contract for sending notifications or responses back to the originating platform.
/// </summary>
public interface IPlatformNotifier
{
    /// <summary>
    /// Gets the platform type this notifier supports.
    /// </summary>
    string SupportedPlatformType { get; }

    /// <summary>
    /// Asynchronously sends a notification message back to the originating platform context.
    /// </summary>
    Task<(bool Success, string? SentMessageId, string? Error)> SendNotificationAsync(
        string conversationId,
        string messageText,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sends a simple acknowledgement or typing indicator.
    /// </summary>
    Task<bool> SendAcknowledgementAsync(
        string conversationId,
        string? replyToMessageId = null,
        CancellationToken cancellationToken = default);
}

---
**(More interfaces may be added here as the processing pipeline evolves)**
