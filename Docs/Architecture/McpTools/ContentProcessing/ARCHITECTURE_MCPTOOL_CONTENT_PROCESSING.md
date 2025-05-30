---
title: "MCP Tool: ContentProcessing Server Architecture"
description: "Detailed architecture for the Nucleus_ContentProcessing_McpServer, focused on extracting structured text and components from various document formats, preparing them for LLM-based analysis or synthesis via LlmOrchestration_McpServer."
version: 1.2
date: 2025-05-28
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
    - ../../CoreNucleus/04_EPHEMERAL_PROCESSING_PIPELINE.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_PLAINTEXT.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_PDF.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_MULTIMEDIA.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_FILECOLLECTIONS.md
    - ../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md # for IContentExtractor
    - ../LlmOrchestration/ARCHITECTURE_MCPTOOL_LLM_ORCHESTRATION.md # Key dependency for synthesis
---

# MCP Tool: ContentProcessing Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide M365 Persona Agents (or other MCP tools) with a robust service for converting various file types (e.g., PDF, Office documents, plain text, multimedia transcripts) into a canonical, ephemeral, and structured textual representation suitable for further processing, analysis, or LLM-driven synthesis. This server acts as a specialized content transformation engine within the Nucleus ecosystem.
*   **Capabilities Encapsulated:**
    *   Abstraction of different `IContentExtractor` implementations for specific file formats (e.g., `PlainTextExtractor`, `PdfExtractor`, `MultimediaTranscriptExtractor`) to extract raw text, structural information (like headings, paragraphs, lists), and identify non-textual components (e.g., images, tables, embedded objects).
    *   Logic for synthesizing a coherent Markdown document from multiple extracted components. This is achieved by **internally orchestrating a call to `Nucleus_LlmOrchestration_McpServer`**, thereby centralizing direct LLM interaction for this synthesis step.
    *   Handling of file collections (e.g., ZIP archives) by iterating through their contents and processing each supported file type individually, potentially aggregating the results or providing a manifest of processed items.
*   **Contribution to M365 Agents:** Offloads the complex and potentially resource-intensive task of file content extraction and structuring. It prepares content in a way that agents can easily consume or pass to other services (like `Nucleus_LlmOrchestration_McpServer` or `Nucleus_KnowledgeStore_McpServer`) for more advanced AI tasks like summarization, Q&A, detailed analysis, or knowledge entry creation.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `ContentProcessing.ExtractStructuredTextFromStream`
*   **Description:** Takes a file content stream and its MIME type (or a filename hint for type detection). Returns structured textual components, references to non-textual elements, and metadata. This output is suitable for direct use by an agent or as input to other MCP tools (e.g., for synthesis by `Nucleus_LlmOrchestration_McpServer` or for creating `PersonaKnowledgeEntry` objects by the agent before storing via `Nucleus_KnowledgeStore_McpServer`).
*   **Input (DTO):** `ExtractTextRequest { Stream FileContentStream, string MimeType, string TenantId, string PersonaId, string OriginalFileNameHint, Dictionary<string, string>? ProcessingOptions }`.
    *   `ProcessingOptions` could include hints like `"ocr:true"` for PDFs, `"language:en-US"` for transcription, or `"max_component_size:1000"` for breaking down very large text blocks.
*   **Output (DTO):** `ExtractStructuredTextResponse { string ArtifactId, List<TextComponent> Components, string? OverallStructureHint, Dictionary<string, object>? ExtractedMetadata, string? ErrorMessage }`.
    *   `ArtifactId`: A unique identifier for this extraction job or the source artifact, useful for correlation.
    *   `TextComponent` could be `{ string ComponentId, ComponentType Type, string Content, int Order, Dictionary<string,string>? Metadata, string? SourceHint }`.
        *   `ComponentType`: e.g., `"paragraph"`, `"heading_1"`, `"list_item"`, `"image_placeholder"`, `"table_placeholder"`, `"code_block"`.
        *   `Content`: For text types, the actual text. For placeholders, a description or reference (e.g., "Image: Figure 1.png", "Table: Sales Data Q1").
        *   `SourceHint`: e.g., "Page 3, Paragraph 2" or "image1.jpg".
    *   Placeholders for images/tables would signal to the agent that these elements exist and might require separate processing (e.g., calling `LlmOrchestration.DescribeImage` or `LlmOrchestration.AnalyzeTable` if the agent deems it necessary).

### 2.2. `ContentProcessing.SynthesizeMarkdownFromComponents`
*   **Description:** Takes a collection of textual and structural components (typically from `ExtractStructuredTextFromStream`) and a synthesis instruction, then **internally calls `Nucleus_LlmOrchestration_McpServer.GenerateChatCompletion`** to create a single, coherent Markdown document. This is useful for creating summaries or transforming extracted content into a specific narrative format.
*   **Input (DTO):** `SynthesizeMarkdownRequest { string TenantId, string PersonaId, string OriginalContainerName, string ArtifactId, List<TextComponent> Components, string SynthesisInstruction, LlmRequestSettings? LlmSettingsForOrchestration }`.
    *   `ArtifactId`: Correlates with the `ExtractStructuredTextResponse`.
*   **Output (DTO):** `SynthesizeMarkdownResponse { string ArtifactId, string SynthesizedMarkdown, string? ErrorMessage }`.

### 2.3. `ContentProcessing.ProcessFileCollectionFromStream` (New)
*   **Description:** Takes a stream representing a file collection (e.g., a ZIP archive) and its MIME type. It iterates through the files within the collection, calling `ExtractStructuredTextFromStream` for each supported file. Returns a manifest of processed files and their individual extraction results or errors.
*   **Input (DTO):** `ProcessFileCollectionRequest { Stream FileCollectionStream, string MimeType, string TenantId, string PersonaId, string OriginalCollectionNameHint, Dictionary<string, string>? ProcessingOptionsPerFile }`.
*   **Output (DTO):** `ProcessFileCollectionResponse { string CollectionArtifactId, List<FileProcessingResult> FileResults, string? ErrorMessage }`.
    *   `FileProcessingResult`: `{ string FileName, string MimeType, ExtractStructuredTextResponse? ExtractionResult, string? ErrorMessage }`.

## 3. Core Internal Logic & Components

*   **`IContentExtractor` Resolution:** Maintains a registry of `IContentExtractor` implementations (injected via DI using a factory pattern or named registrations) and resolves the appropriate one based on the `MimeType` or file extension from the request.
    *   Example Extractors: `PlainTextContentExtractor`, `PdfPigContentExtractor`, `OpenXmlContentExtractor`, `AzureAiVisionImageExtractor` (for OCR from images if directly supported, or via LlmOrchestration), `AzureAiSpeechTranscriptExtractor`.
*   **Extraction Logic:** Invokes the `ExtractStructuredTextAsync` method of the resolved `IContentExtractor` to produce `List<TextComponent>`. This includes handling potential errors from individual extractors and populating the `ErrorMessage` in the response if necessary.
*   **Synthesis Orchestration (for `SynthesizeMarkdownFromComponents`):**
    *   Constructs a suitable prompt for the `Nucleus_LlmOrchestration_McpServer` using the provided `Components` and `SynthesisInstruction`. This prompt engineering is a key part of this tool's intelligence.
    *   Uses an injected `Nucleus.Mcp.Client.IMcpClient` (or a specifically typed client for `LlmOrchestration`) to make an MCP call to `Nucleus_LlmOrchestration_McpServer.GenerateChatCompletion`.
    *   Passes through `LlmSettingsForOrchestration` to the `LlmOrchestration_McpServer` call.
*   **File Collection Processing Logic (for `ProcessFileCollectionFromStream`):**
    *   Uses a library (e.g., `System.IO.Compression` for ZIP files) to iterate through entries in the archive stream.
    *   For each entry, determines its MIME type (if possible from name or magic bytes).
    *   Invokes `ExtractStructuredTextFromStream` logic for each supported file entry.
    *   Aggregates results and errors into the `ProcessFileCollectionResponse`.
*   **Temporary Storage Handling:** This service aims to be stateless. If unpacking of archives is handled here, it must manage temporary ephemeral streams carefully, ensuring they are disposed of correctly. The primary design favors receiving individual component streams if synthesis is required, or a single stream for extraction. For large file collections, streaming directly from the archive to individual extractors without fully unpacking to disk is preferred.
*   **Error Handling and Resilience:** Implements robust error handling for issues like unsupported file types, corrupted files, or timeouts during extraction or LLM calls. Provides clear error messages in responses.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault (for secrets like the App ID URI or client secret for calling `LlmOrchestration_McpServer`, if applicable, though Managed Identity is preferred for service-to-service).
    *   Azure App Configuration (for static service configurations, feature flags, extractor settings).
    *   (Potentially) Azure Blob Storage for temporary staging of very large files if streaming proves insufficient, though this should be avoided to maintain ephemeral nature.
*   **Other Nucleus MCP Tools:**
    *   **Critically depends on `Nucleus_LlmOrchestration_McpServer`** for the `SynthesizeMarkdownFromComponents` operation and potentially for advanced OCR or image/table analysis if not handled by dedicated extractors.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions` (for DTOs like `ExtractTextRequest`, `ExtractStructuredTextResponse`, `TextComponent`, `ProcessFileCollectionRequest`, `ProcessFileCollectionResponse`, and the `IContentExtractor` interface).
    *   Specific `IContentExtractor` implementations (e.g., from a `Nucleus.Infrastructure.ContentExtractors` project, which might wrap libraries like PdfPig, OpenXML SDK, Azure AI SDKs).
    *   `Nucleus.Mcp.Client` (for making authenticated MCP calls to `Nucleus_LlmOrchestration_McpServer`).
    *   `Nucleus.Shared.Common` (for utility functions, error types).
*   **Third-party Libraries:**
    *   May be used within specific `IContentExtractor` implementations (e.g., PdfPig for PDFs, OpenXML SDK for Office documents, Azure.AI.Vision.ImageAnalysis, Azure.AI.Speech). These are indirect dependencies encapsulated within the extractor implementations.
    *   `System.IO.Compression` for archive handling.

## 5. Security Model

*   **Authentication of Callers:** All incoming MCP requests must be authenticated by validating Azure AD tokens (via `Microsoft.Identity.Web` or similar in an ASP.NET Core context). The caller must have appropriate application permissions (defined in this MCP tool's App Registration) to invoke its operations.
*   **Authorization within the Tool:**
    *   `TenantId` and `PersonaId` from the request DTOs are used for logging, context, and potentially for routing to specific configurations if needed, but not typically for direct data access authorization as this tool is ephemeral.
    *   Role-based access can be implemented if certain operations are deemed more sensitive (e.g., only specific agents can trigger very resource-intensive processing options).
*   **Authentication to Dependencies:**
    *   Uses its Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access Azure Key Vault and Azure App Configuration.
    *   Uses its Managed Identity to make authenticated MCP calls to `Nucleus_LlmOrchestration_McpServer` by acquiring tokens for its App Registration / service principal, targeting the `LlmOrchestration_McpServer`'s App ID URI.
*   **Input Sanitization:** While not its primary role, basic checks on input parameters (e.g., MIME types, option values) should be performed. The core content extractors are responsible for safely handling potentially malformed file streams.

## 6. Data Handling & Persistence

*   This tool is fundamentally **stateless and ephemeral** regarding user content.
*   It processes input streams and returns structured textual components or synthesized Markdown (via `LlmOrchestration_McpServer`).
*   It **does not persist any user file content or extracted data itself** beyond the lifetime of a request.
*   Temporary files or streams created during processing (e.g., unpacking archives) must be managed carefully and disposed of promptly and securely.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Typically deployed as an Azure Container App or Azure App Service, configured for scalability based on expected load.
*   **Key Configurations (managed via Azure App Configuration and Key Vault):**
    *   Configuration for the endpoint and authentication details (e.g., App ID URI) for `Nucleus_LlmOrchestration_McpServer`.
    *   Configuration for different `IContentExtractor` implementations (e.g., API keys for cloud-based extractors if not using Managed Identity, feature flags for specific extractor behaviors).
    *   Processing timeout settings for long-running extraction or calls to `LlmOrchestration_McpServer`.
    *   Resource allocation for the container (CPU, memory), especially important given the potential for intensive file processing.
    *   Retry policies for calls to dependent services.
    *   Logging levels and sinks.
    *   Allowed MIME types and maximum file sizes.

## 8. Future Considerations / Potential Enhancements

*   **Support for a wider range of file formats** by adding new `IContentExtractor` implementations (e.g., specialized CAD file text, more audio/video formats).
*   **More sophisticated error handling and resilience:** e.g., partial content extraction for corrupted files, with clear indicators of what was successful.
*   **Configurable "quality vs. speed" trade-offs** for extraction (e.g., deep OCR vs. quick text layer, basic table extraction vs. detailed cell-level analysis).
*   **Direct integration with `Nucleus_FileAccess_McpServer`**: Instead of receiving a stream, it could receive an `ArtifactReference` and call `FileAccess` to get the stream, simplifying the M365 Agent's role. This would require careful consideration of data flow and security.
*   **Caching of extraction results for identical files (if checksums match and content is deemed static):** This would require a small, time-limited cache and careful invalidation logic, potentially using a distributed cache like Redis if scaled out. This deviates slightly from pure statelessness but could offer performance benefits.
*   **Enhanced metadata extraction:** Deriving more structured metadata from content during the extraction phase (e.g., author, creation date, detected language, key entities) and including it in the `ExtractedMetadata` field.
*   **Pluggable post-processing steps:** Allow for a pipeline of transformations on extracted components before they are returned or sent for synthesis.
*   **Versioned DTOs:** As the tool evolves, ensure backward compatibility or clear versioning of request/response DTOs.