# Nucleus OmniRAG: Processing Architecture

**Version:** 1.3
**Date:** 2025-04-08

This document outlines the architecture of the processing components in the Nucleus OmniRAG system. It focuses on **artifact ingestion, content extraction, persona-driven analysis, and the storage of resulting knowledge entries** used for intelligent retrieval.

## 1. Philosophy: Persona-Driven Meaning Extraction

A central tenet of the Nucleus OmniRAG architecture is that interpreting meaning from diverse artifacts is best achieved through specialized AI Personas. Key principles guiding our approach:

1.  **No One-Size-Fits-All Interpretation**: Different artifacts, domains, and user goals require different analytical perspectives.
2.  **Persona-Centric Analysis**: Value is maximized when Personas analyze artifacts within their domain context, extracting relevant insights and summaries rather than relying on generic pre-chunking.
3.  **Contextual Relevance**: Personas determine what constitutes a relevant snippet or summary based on the artifact content and the persona's purpose.
4.  **Focus on Knowledge, Not Just Text**: The goal is to store structured knowledge (`PersonaKnowledgeEntry`) derived by personas, not just fragmented text.
5.  **Extensibility**: The architecture supports adding new personas and content extractors to handle evolving needs and artifact types.

## 2. Artifact Content Extraction

Before personas can analyze an artifact, its content needs to be extracted into a usable format, typically raw text.

### 2.1 Abstraction: `IContentExtractor`

An `IContentExtractor` interface provides a standard way to handle different file types:

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
    /// <returns>A result containing extracted text and potentially other metadata.</returns>
    Task<ContentExtractionResult> ExtractContentAsync(Stream sourceStream, string mimeType, string? sourceUri = null);
}

/// <summary>
/// Holds the result of content extraction.
/// </summary>
public record ContentExtractionResult(
    string ExtractedText,
    Dictionary<string, object>? AdditionalMetadata = null // e.g., page numbers, structural info
);
```

### 2.2 Implementation

Implementations will exist for common types (PDF, DOCX, TXT, HTML, etc.) and potentially specialized formats. OCR might be integrated for image-based documents. The processing pipeline will select the appropriate extractor based on the `mimeType` specified in the `ArtifactMetadata`.

### 2.3 Handling Complex and Multimodal Content (Planned)

While initial implementations may focus on standard text-based documents, the architecture must accommodate more complex scenarios, particularly for use cases like Weltman IT (e.g., images embedded in documents, complex Excel sheets, scanned PDFs). Strategies include:

*   **Image Processing (OCR):**
    *   **Technology:** Utilize services like **Azure AI Vision** (specifically its Read API / OCR capabilities) to extract text from images (e.g., PNG, JPG, TIFF) or image-based PDFs.
    *   **Integration:** An `IContentExtractor` implementation could detect image types or image-heavy PDFs, route them to Azure AI Vision, and return the extracted text.
    *   **Considerations:** Cost, API limits, language support, handling layouts.
*   **Document Structure Analysis (Tables, Forms):**
    *   **Technology:** Employ services like **Azure AI Document Intelligence** to understand document structure, extract tables, key-value pairs, and form data, preserving layout information where possible.
    *   **Integration:** Specific `IContentExtractor` implementations for formats like PDF or DOCX could leverage Document Intelligence for richer extraction than simple text scraping, potentially adding structural information to the `ContentExtractionResult.AdditionalMetadata`.
    *   **Considerations:** Model training/customization needs, cost, specific schema requirements.
*   **Multimodal AI Analysis:**
    *   **Technology:** For artifacts containing both text and significant images (e.g., diagrams, screenshots), leverage multimodal LLMs (like GPT-4 Vision or Gemini Pro Vision models accessed via `IChatClient`) capable of understanding both modalities.
    *   **Integration:** This might involve passing image references or descriptions alongside text to the `IPersona.AnalyzeContentAsync` method within the `ContentItem.AdditionalContext`, or having a specialized extractor generate image descriptions first.
    *   **Considerations:** Model availability, cost, prompt engineering complexity.

**Note:** Detailed implementation plans and selection of specific technologies for these advanced extraction methods are required and will depend on specific format requirements and cost-benefit analysis.

## 3. Processing Pipeline Flow

The end-to-end flow for processing a new artifact involves several steps, likely orchestrated by a message queue or a workflow engine:

1.  **Ingestion Trigger:** An event signals a new artifact is available (e.g., file upload, URL submission). The trigger includes the `sourceUri`, `userId`, `sourceType`, initial `displayName`, etc.
2.  **Metadata Creation:**
    *   The pipeline calls `IArtifactMetadataService.CreateOrUpdateMetadataAsync`.
    *   This service generates the unique `sourceIdentifier`, populates initial fields (timestamps, user info, source details), sets `overallProcessingState` to `Pending` or `InProgress`, and saves the initial `ArtifactMetadata` object to the configured storage location (see `03_ARCHITECTURE_STORAGE.md`).
3.  **Content Acquisition:**
    *   The pipeline fetches the actual artifact content stream using the `sourceUri` from the `ArtifactMetadata` (leveraging an appropriate `IFileStorage` abstraction if needed).
4.  **Content Extraction:**
    *   The pipeline selects the correct `IContentExtractor` based on the `mimeType` from `ArtifactMetadata`.
    *   It calls `ExtractContentAsync` to get the raw text (and any other relevant data).
    *   If extraction fails, `overallProcessingState` in `ArtifactMetadata` is updated to `Failed`, and the process may terminate for this artifact.
5.  **Persona Analysis Loop:**
    *   The pipeline determines the set of *target personas* for this artifact (based on configuration, user selection, or context).
    *   For each `personaName` in the target list:
        *   Update `personaProcessingStatus[personaName]` to `Processing` in `ArtifactMetadata` (and save changes).
        *   Create a `ContentItem` containing the `ArtifactMetadata`, extracted raw text, and any other necessary context.
        *   Invoke the appropriate `IPersona.AnalyzeContentAsync` method, passing the `ContentItem`.
        *   The `IPersona` implementation:
            *   Performs its domain-specific analysis directly on the raw text (no generic pre-chunking).
            *   Determines the `relevantTextSnippetOrSummary` (a concise piece of text representing the core insight or relevant section for that persona).
            *   Generates the structured `analysis` object (the concrete `TAnalysisData` type for that persona, e.g., `EduFlowAnalysis`).
            *   Returns both the `analysis` and `relevantTextSnippetOrSummary` to the pipeline.
        *   **The Processing Pipeline** (not the Persona):
            *   Receives the results (analysis object, relevant snippet).
            *   Calls the registered `IEmbeddingGenerator` to generate the `snippetEmbedding` for the `relevantTextSnippetOrSummary`.
            *   Optionally calculates `analysisSummaryEmbedding` if applicable.
        *   On Success:
            *   The pipeline calls `IPersonaKnowledgeRepository<TAnalysisData>.AddOrUpdateEntryAsync` to save the complete `PersonaKnowledgeEntry` to the persona-specific Cosmos DB container (see `04_ARCHITECTURE_DATABASE.md`).
            *   Updates `personaProcessingStatus[personaName]` to `Processed` in `ArtifactMetadata`.
            *   If the analysis generated a reply for the user (e.g., in the Email scenario), the pipeline publishes a `ReplyReadyEvent` to the message queue containing the reply content and routing information. The appropriate Platform Adapter will subscribe to this event to send the reply.
        *   On Failure/Skip:
            *   Update `personaProcessingStatus[personaName]` to `Failed` or `Skipped` in `ArtifactMetadata`.
6.  **Finalize Metadata:**
    *   After looping through all target personas, the pipeline determines the final `overallProcessingState` (e.g., `Completed`, `CompletedWithFailures`).
    *   Update the `ArtifactMetadata` one last time with the final state and potentially a completion timestamp.

## 4. Embedding Generation

Embeddings are crucial for semantic search during retrieval. They are generated *after* a persona has identified the most relevant piece of text.

### 4.1 Abstraction Layer

Nucleus OmniRAG leverages the standard `Microsoft.Extensions.AI` abstractions:

```csharp
// Defined in Microsoft.Extensions.AI.Abstractions
public interface IEmbeddingGenerator<TData, TEmbedding>
{
    IReadOnlyList<int>? GetEmbeddingDimensions(string? modelId = null);
    Task<TEmbedding> GenerateEmbeddingAsync(TData data, CancellationToken cancellationToken = default, string? modelId = null, EmbeddingOptions? options = null);
    // ... other methods
}
```

### 4.2 Integration

*   An implementation of `IEmbeddingGenerator<string, Embedding<float>>` (e.g., using Google Gemini, Azure OpenAI) is registered in the DI container (see `Nucleus.Infrastructure`).
*   This generator is used **by the Processing Pipeline** (not the persona) to create embeddings for:
    *   `PersonaKnowledgeEntry.relevantTextSnippetOrSummary` -> stored as `snippetEmbedding`.
    *   Optionally, a derived summary from `PersonaKnowledgeEntry.analysis` -> stored as `analysisSummaryEmbedding`.
*   These embeddings are stored within the `PersonaKnowledgeEntry` document in Cosmos DB.

## 5. Retrieval Flow

Retrieval leverages the structured knowledge and embeddings stored by personas:

1.  User submits a query relevant to a specific persona's domain (e.g., asking EduFlow about student progress).
2.  The application identifies the target persona (`personaName`).
3.  The query text is processed by an `IRetrievalService` implementation.
4.  The service calls the registered `IEmbeddingGenerator` to generate an embedding vector for the query.
5.  The service uses the `IPersonaKnowledgeRepository<TAnalysisData>` for the target persona.
6.  It performs a vector similarity search within the persona's specific Cosmos DB container (`{personaName}KnowledgeContainer`) against the `snippetEmbedding` (or `analysisSummaryEmbedding`, depending on the strategy).
The search likely includes metadata filters (e.g., `userId`, `tags` from related `ArtifactMetadata`).
7.  The repository returns ranked, relevant `PersonaKnowledgeEntry` documents.
8.  These entries (containing the persona's analysis and the relevant snippet) are used as context for generating a final response via an AI chat client, potentially after fetching the full `ArtifactMetadata` using the `sourceIdentifier` for more context.

## 6. Configuration

*   **Content Extractors:** Configuration might specify preferred extractors or settings for specific MIME types.
*   **AI Providers:** Standard configuration for embedding generators and chat clients (API keys, endpoints, model IDs) via `appsettings.json`, environment variables, or a configuration provider like Azure App Configuration/Aspire.
*   **Database:** Connection strings and database/container names for Cosmos DB.
*   **Storage:** Configuration for accessing the storage mechanism where artifacts and `ArtifactMetadata` reside.
*   **Target Personas:** Configuration defining which personas should process which types of artifacts or based on user context.

## 7. Storage Architecture Alignment

This processing architecture aligns with the storage and database models:

*   **Storage Layer (`03_ARCHITECTURE_STORAGE.md`):** Stores original artifacts (or references via `sourceUri`) and the primary `ArtifactMetadata` object (containing overall status, `personaProcessingStatus`, links, etc.).
*   **Database Layer (`04_ARCHITECTURE_DATABASE.md`):** Stores `PersonaKnowledgeEntry` documents in persona-specific Cosmos DB containers. Each entry contains the persona's analysis (`analysis`), the relevant text snippet (`relevantTextSnippetOrSummary`), and associated embeddings (`snippetEmbedding`, `analysisSummaryEmbedding`). The `sourceIdentifier` links back to the `ArtifactMetadata` in the Storage Layer.

## 8. Next Steps

1.  **Implement `IContentExtractor`:** Create initial implementations for key types (PDF, DOCX, TXT, HTML).
2.  **Implement `IArtifactMetadataService`:** Build the service for managing `ArtifactMetadata` in storage.
3.  **Implement `IPersonaKnowledgeRepository`:** Create the repository for interacting with Cosmos DB containers.
4.  **Develop Orchestration Logic:** Design and implement the pipeline flow (e.g., using Azure Functions, Durable Functions, or a message queue like Azure Service Bus).
5.  **Refactor `IPersona` Interface/Implementations:** Refactor the interface to use `AnalyzeContentAsync` and update implementations to work directly with raw content (not dependent on pre-chunking).
6.  **Implement Reply Event System:** Create the message types and subscription patterns for async reply handling.
7.  **Implement `IRetrievalService`:** Build the service querying `PersonaKnowledgeEntry` data.
8.  **Testing:** Implement comprehensive integration tests covering the entire pipeline.

---

### Key Services and Abstractions

*   **`IArtifactMetadataService`**: Manages CRUD operations for `ArtifactMetadata` in the central Storage repository.
*   **`IContentExtractor`**: Interface for services that extract raw text/structured content from various artifact MIME types (PDF, DOCX, HTML, etc.). Implementations handle specific formats.
*   **`IPersona`**: The core interface defining a persona's analytical capabilities, primarily through `AnalyzeContentAsync`.
*   **`IChatClient` (from `Microsoft.Extensions.AI`)**: The standard abstraction for interacting with LLMs for chat completions. Implementations will handle provider-specific details, including context caching integration.
*   **`IEmbeddingGenerator` (from `Microsoft.Extensions.AI`)**: The standard abstraction for generating text embeddings.
*   **`IPersonaKnowledgeRepository`**: Interface for services managing the storage and retrieval of `PersonaKnowledgeEntry` documents in the persona-specific data stores (Cosmos DB).
*   **`ICacheManagementService` (Planned for Phase 2+)**: Abstraction responsible for interacting with the underlying AI provider's prompt/context caching mechanisms. It handles creating, retrieving, and potentially managing the lifecycle (TTL) of cached content linked to a `SourceIdentifier`.
