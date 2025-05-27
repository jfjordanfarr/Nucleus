---
title: "MCP Tool: ContentProcessing Server Architecture"
description: "Detailed architecture for the Nucleus_ContentProcessing_McpServer, focused on extracting structured text and components from various document formats, preparing them for LLM-based analysis or synthesis via LlmOrchestration_McpServer."
version: 1.1
date: 2025-05-27
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
    - ../../CoreNucleus/04_EPHEMERAL_PROCESSING_PIPELINE.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_PLAINTEXT.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_PDF.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_MULTIMEDIA.md
    - ../../Processing/Ingestion/ARCHITECTURE_INGESTION_FILECOLLECTIONS.md
    - ../../CoreNucleus/06_ABSTRACTIONS_DTOs_INTERFACES.md # for IContentExtractor
    - ./LlmOrchestration/ARCHITECTURE_MCPTOOL_LLM_ORCHESTRATION.md # Key dependency for synthesis
---

# MCP Tool: ContentProcessing Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide M365 Persona Agents (or other MCP tools) with a robust service for converting various file types (e.g., PDF, Office documents) into a canonical, ephemeral, and structured textual representation suitable for further processing, analysis, or LLM-driven synthesis.
*   **Capabilities Encapsulated:**
    *   Abstraction of different `IContentExtractor` implementations for specific file formats to extract raw text, structural information, and identify non-textual components (e.g., images, tables).
    *   Logic for synthesizing a coherent Markdown document from multiple extracted components. This is achieved by **internally orchestrating a call to `Nucleus_LlmOrchestration_McpServer`**, thereby centralizing direct LLM interaction.
*   **Contribution to M365 Agents:** Offloads the complex and potentially resource-intensive task of file content extraction and structuring. It prepares content in a way that agents can easily consume or pass to other services (like `Nucleus_LlmOrchestration_McpServer`) for more advanced AI tasks like summarization, Q&A, or detailed analysis.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `ContentProcessing.ExtractStructuredTextFromStream`
*   **Description:** Takes a file content stream and its MIME type. Returns structured textual components, references to non-textual elements, and metadata. This output is suitable for direct use by an agent or as input to other MCP tools (e.g., for synthesis by `Nucleus_LlmOrchestration_McpServer`).
*   **Input (DTO):** `ExtractTextRequest { Stream FileContentStream, string MimeType, string TenantId, string OriginalFileNameHint, Dictionary<string, string>? ProcessingOptions }`. `ProcessingOptions` could include hints like "ocr:true" for PDFs.
*   **Output (DTO):** `ExtractStructuredTextResponse { List<TextComponent> Components, string? OverallStructureHint, Dictionary<string, object>? ExtractedMetadata, string? ErrorMessage }`.
    *   `TextComponent` could be `{ Type: "text_block"|"image_placeholder"|"table_placeholder", Content: string (for text_block, or a reference/description for placeholders), Order: int, Metadata: Dictionary<string,string>? }`.
    *   Placeholders for images/tables would signal to the agent that these elements exist and might require separate processing (e.g., calling `LlmOrchestration.DescribeImage` if the agent deems it necessary).

### 2.2. `ContentProcessing.SynthesizeMarkdownFromComponents`
*   **Description:** Takes a collection of textual and structural components (typically from `ExtractStructuredTextFromStream`) and a synthesis instruction, then **internally calls `Nucleus_LlmOrchestration_McpServer.GenerateChatCompletion`** to create a single, coherent Markdown document.
*   **Input (DTO):** `SynthesizeMarkdownRequest { string TenantId, string PersonaId, string OriginalContainerName, List<TextComponent> Components, string SynthesisInstruction, LlmRequestSettings? LlmSettingsForOrchestration }`. `LlmSettingsForOrchestration` would be passed through to the `LlmOrchestration_McpServer` call.
*   **Output (DTO):** `SynthesizeMarkdownResponse { string SynthesizedMarkdown, string? ErrorMessage }`.

## 3. Core Internal Logic & Components

*   **`IContentExtractor` Resolution:** Maintains a registry of `IContentExtractor` implementations (injected via DI) and resolves the appropriate one based on the `MimeType` from the request.
*   **Extraction Logic:** Invokes the `ExtractStructuredTextAsync` method of the resolved `IContentExtractor` to produce `List<TextComponent>`.
*   **Synthesis Orchestration (for `SynthesizeMarkdownFromComponents`):**
    *   Constructs a suitable prompt for the `Nucleus_LlmOrchestration_McpServer` using the provided `Components` and `SynthesisInstruction`.
    *   Uses an injected `Nucleus.Mcp.Client.IMcpClient` (or a specifically typed client for `LlmOrchestration`) to make an MCP call to `Nucleus_LlmOrchestration_McpServer.GenerateChatCompletion`.
    *   Passes through `LlmSettingsForOrchestration` to the `LlmOrchestration_McpServer` call.
*   **Markdown Generation/Structuring (Post-Extraction):** Ensures that the output from extractors is well-structured into `TextComponent` objects. The final Markdown from synthesis is directly from `LlmOrchestration_McpServer`.
*   **Temporary Storage Handling:** This service aims to be stateless. If unpacking of archives is handled here, it must manage temporary ephemeral streams carefully. The primary design favors receiving individual component streams if synthesis is required, or a single stream for extraction.

## 4. Dependencies

*   **Azure Services:**
    *   Azure Key Vault (for secrets like the App ID URI or client secret for calling `LlmOrchestration_McpServer`, if applicable, though Managed Identity is preferred for service-to-service).
    *   Azure App Configuration (for static service configurations).
*   **Other Nucleus MCP Tools:**
    *   **Critically depends on `Nucleus_LlmOrchestration_McpServer`** for the `SynthesizeMarkdownFromComponents` operation.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions` (for DTOs like `ExtractTextRequest`, `ExtractStructuredTextResponse`, `TextComponent`, and the `IContentExtractor` interface).
    *   Specific `IContentExtractor` implementations (e.g., from a `Nucleus.Infrastructure.ContentExtractors` project).
    *   `Nucleus.Mcp.Client` (for making authenticated MCP calls to `Nucleus_LlmOrchestration_McpServer`).
*   **Third-party Libraries:**
    *   May be used within specific `IContentExtractor` implementations (e.g., PdfPig for PDFs, OpenXML SDK for Office documents). These are indirect dependencies.

## 5. Security Model

*   **Authentication of Callers:** All incoming MCP requests must be authenticated by validating Azure AD tokens.
*   **Authorization within the Tool:**
    *   `TenantId` from the request DTOs is used for logical data scoping and context.
*   **Authentication to Dependencies:**
    *   Uses its Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access Azure Key Vault and Azure App Configuration.
    *   Uses its Managed Identity to make authenticated MCP calls to `Nucleus_LlmOrchestration_McpServer` by acquiring tokens for its App Registration / service principal.

## 6. Data Handling & Persistence (If Applicable)

*   This tool is fundamentally **stateless and ephemeral**.
*   It processes input streams and returns structured textual components or synthesized Markdown (via `LlmOrchestration_McpServer`).
*   It **does not persist any data itself**.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Typically deployed as an Azure Container App or Azure App Service.
*   **Key Configurations (managed via Azure App Configuration and Key Vault):**
    *   Configuration for the endpoint and authentication details (e.g., App ID URI) for `Nucleus_LlmOrchestration_McpServer`.
    *   Configuration for different `IContentExtractor` implementations (if they have specific settings).
    *   Processing timeout settings for long-running extraction or calls to `LlmOrchestration_McpServer`.
    *   Resource allocation for the container.

## 8. Future Considerations / Potential Enhancements

*   **Support for a wider range of file formats** by adding new `IContentExtractor` implementations.
*   **More sophisticated error handling and resilience:** e.g., partial content extraction for corrupted files.
*   **Configurable "quality vs. speed" trade-offs** for extraction.
*   **Batch processing capabilities** for multiple streams.
*   **Enhanced metadata extraction:** Deriving more structured metadata from content during the extraction phase.