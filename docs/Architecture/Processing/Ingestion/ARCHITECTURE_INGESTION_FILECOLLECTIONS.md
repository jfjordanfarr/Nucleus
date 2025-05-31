---
title: "Ingestion Architecture: File Collections (Post-M365 Agent SDK Pivot)"
description: "Describes how container artifacts (e.g., zip, docx) are unpacked by MCP Tools, their components identified and processed, and finally synthesized into a single ephemeral textual representation by an LLM, orchestrated by a Nucleus M365 Persona Agent."
version: 1.4
date: 2025-05-26
parent: ../ARCHITECTURE_PROCESSING_INGESTION.md
---

# Ingestion Architecture: File Collections (e.g., Zip Archives, Docx) (Post-M365 Agent SDK Pivot)

## 1. Role and Overview

This document describes the processing of artifacts that are containers holding multiple individual files, such as Zip archives (`.zip`) or Office Open XML formats like `.docx`. The primary responsibility is to **unpack** the container and **identify** its constituent files. This logic is typically encapsulated within a backend **`Nucleus_FileAccess_McpServer`** or a specialized **`Nucleus_ContentProcessing_McpServer`** (MCP Tools).

The **Nucleus M365 Persona Agent** (or a background worker acting on its behalf) orchestrates the subsequent processing. It receives component information from the MCP Tool, directs further processing of these components (e.g., image description via an LLM call, text extraction), collects the resulting textual representations, and finally uses an LLM (via its configured `IChatClient`) to synthesize a single, coherent Markdown document representing the original container.

This ensures adherence to the overall ingestion principles ([ARCHITECTURE_PROCESSING_INGESTION.md](../ARCHITECTURE_PROCESSING_INGESTION.md)), ultimately yielding a faithful **ephemeral textual representation** derived from all components, ready for Persona Agent analysis.

## 2. Processing Flow (M365 Agent & MCP Tool Centric)

1.  **M365 Agent Initiates Processing:** A **Nucleus M365 Persona Agent** (or a background worker acting on its behalf) identifies a container file (e.g., a `.zip` or `.docx`) via an `ArtifactReference`. It makes an MCP call to the **`Nucleus_FileAccess_McpServer`** (MCP Tool) with this reference.
2.  **MCP Tool Unpacks & Identifies Components:** The `Nucleus_FileAccess_McpServer` (MCP Tool) (using internal `IArtifactProvider` and `IContentExtractor` logic, and the agent's security context):
    *   Retrieves the content stream for the container artifact.
    *   Unpacks the container's contents into a temporary, accessible location (e.g., ephemeral in-memory streams or temporary local files within the MCP tool's secure execution scope).
    *   Iterates through the extracted items.
    *   For each item, it determines its type (e.g., `text/xml`, `image/png`) and its path within the container.
    *   It **returns a list of identified components and their ephemeral content streams/handles** back to the calling M365 Agent. Each entry includes:
        *   Component Type (MIME type or inferred type)
        *   Relative Path within the container
        *   An ephemeral handle to the unpacked content (e.g., a byte array, an in-memory stream, or a temporary access URI scoped to the MCP tool's session).
3.  **M365 Agent Orchestrates Component Processing & Textualization:** The M365 Agent receives the component list and their ephemeral content. For each component:
    *   Based on the **Component Type**, it determines the appropriate textualization strategy:
        *   For plain text components (e.g., XML files within a DOCX), it reads the text directly.
        *   For image components, it makes a call to a configured multimodal LLM (via its `IChatClient`) to get a textual description.
        *   For other binary components, it might invoke another specialized MCP Tool (e.g., `Nucleus_ContentProcessing_McpServer`) or use internal logic if applicable.
    *   The Agent collects all these textual representations (e.g., raw XML content, image descriptions).
4.  **M365 Agent Synthesizes Final Markdown (via LLM):** The M365 Agent takes the **bundle** of textual components.
    *   It constructs a prompt for a large context window LLM (e.g., Gemini, Azure OpenAI, as configured for the Agent via `IChatClient`).
    *   The prompt includes an instruction like "Synthesize a single coherent Markdown document representing the original container artifact `[Container Name]` from these components: [component1_text, component2_description, ...]", providing the textualized parts.
    *   The LLM generates the final synthesized Markdown.
5.  **Return Ephemeral Markdown to Agent Core Logic:** The synthesized Markdown string is now available to the M365 Agent's core `IPersonaRuntime` logic for analysis, knowledge extraction, and persistence of `PersonaKnowledgeEntry` records (via `Nucleus_KnowledgeStore_McpServer` (MCP Tool)).
6.  **Cleanup:** All temporary unpacked files, intermediate component data, and ephemeral streams are discarded by the MCP Tool and the M365 Agent at the end of the processing task for that interaction.

## 3. Key Principles & Considerations

*   **M365 Agent as Orchestrator:** The M365 Persona Agent is central to managing the lifecycle of processing complex, multi-part artifacts, making calls to backend MCP Tools.
*   **MCP Tools for Specialized Tasks:** `Nucleus_FileAccess_McpServer` (and potentially `Nucleus_ContentProcessing_McpServer`) (MCP Tools) handle the "dirty work" of file unpacking and basic content extraction.
*   **LLM for Synthesis & Description:** The M365 Agent leverages its configured LLM (via `IChatClient`) for describing non-text components (like images) and for the final, complex task of synthesizing a coherent document from disparate parts. This synthesis step is typically performed by the M365 Agent itself using its `IChatClient`.
*   **Strictly Ephemeral:** All intermediate data (unpacked files, component streams, textualized parts) and the final synthesized Markdown (before Persona analysis) are transient and exist only for the duration of the processing task.
*   **Error Handling:** The M365 Agent and MCP Tools must handle errors at each stage (unpacking, component processing, synthesis).
*   **Resource Management:** MCP Tools manage resources for unpacked files; the M365 Agent manages memory for collected textual parts.

## 4. Example: Processing a Word Document (.docx)

Illustrating the M365 Agent & MCP Tool flow:

1.  **Agent Initiates:** M365 Agent gets `ArtifactReference` for `MyReport.docx`, calls `Nucleus_FileAccess_McpServer` (MCP Tool).
2.  **MCP Tool Unpacks:** `Nucleus_FileAccess_McpServer` (MCP Tool) unpacks `.docx`, identifies components, and returns a list to the Agent:
    *   { Type: `application/xml`, Path: `word/document.xml`, EphemeralContent: `[stream_or_bytes_1]` }
    *   { Type: `application/xml`, Path: `word/styles.xml`, EphemeralContent: `[stream_or_bytes_2]` }
    *   { Type: `image/png`, Path: `word/media/image1.png`, EphemeralContent: `[stream_or_bytes_3]` }
    *   ...
3.  **Agent Processes Components:**
    *   Reads text from `stream_or_bytes_1` (document.xml) -> `doc_xml_content`.
    *   Reads text from `stream_or_bytes_2` (styles.xml) -> `styles_xml_content`.
    *   Sends `stream_or_bytes_3` (image1.png) to its LLM (via `IChatClient`) for description -> `description1_text`.
4.  **Agent Synthesizes Markdown:** Agent constructs prompt for its LLM (via `IChatClient`): "Synthesize Markdown for `MyReport.docx` from: { `doc_xml_content`, `styles_xml_content`, `description1_text` }..."
5.  **LLM Returns Synthesized Markdown:** Agent receives the final Markdown string.
6.  **Agent Analysis & Knowledge Storage:** The Agent's `IPersonaRuntime` analyzes this ephemeral Markdown, extracts salient info, and calls `Nucleus_KnowledgeStore_McpServer` (MCP Tool) to save `ArtifactMetadata` and `PersonaKnowledgeEntry`.
7.  **Cleanup:** All ephemeral data is discarded.

**Outcome:** The single `.docx` artifact is now represented by its `ArtifactMetadata` and relevant `PersonaKnowledgeEntry` records. The intelligence for understanding and synthesizing the document's content (including image descriptions) resides with the M365 Agent and its configured LLM, using ephemeral data provided by MCP Tools. This approach avoids dedicated Office parsers, leveraging the Agent's LLM for final representation generation.
