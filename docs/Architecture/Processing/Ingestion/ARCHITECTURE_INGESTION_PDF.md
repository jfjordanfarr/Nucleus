---
title: "Ingestion Architecture - PDF Files (M365/MCP)"
description: "Outlines the hybrid ingestion strategy for PDF files, balancing direct multimodal LLM processing with fallback to external pre-processing services for complex cases, orchestrated by M365 Agents and utilizing MCP Tools."
version: 1.2
date: 2025-05-26
parent: ../ARCHITECTURE_PROCESSING_INGESTION.md
---

# Architecture: PDF Ingestion (M365/MCP)

This document outlines the ingestion strategy for Portable Document Format (PDF) files within the Nucleus architecture, now orchestrated by **Microsoft 365 Persona Agents** and utilizing backend **MCP Tools** (Model Context Protocol Tools/Servers).

PDFs represent a unique challenge due to their complex, fixed-layout nature, potentially containing a mix of structured text, raster images (requiring OCR), vector graphics, forms, and annotations. They are not inherently structured as archives of simple components like Office Open XML formats.

The strategy aims to balance the core principles of simplicity and leveraging LLMs with the practical need to reliably extract content from this often-difficult format.

## 1. Core Challenge

Directly applying the "bundle components + LLM synthesis" pattern used for [File Collections](./ARCHITECTURE_INGESTION_FILECOLLECTIONS.md) (like `.docx`) is not feasible for PDFs because:

*   PDFs lack a standardized, easily extractable internal component structure comparable to OOXML.
*   Content extraction often requires specialized parsing libraries and Optical Character Recognition (OCR).
*   Layout information is critical to meaning but hard to capture reliably.

## 2. Processing Strategy: Hybrid Approach (M365 Agent & MCP Tool Centric)

Nucleus employs a two-pronged, configurable approach determined by the **Nucleus M365 Persona Agent** (or a background worker acting on its behalf) after retrieving the PDF content stream via an MCP call to the **`Nucleus_FileAccess_McpServer`** (MCP Tool).

**Attempt 1: Direct Multimodal LLM Processing (Preferred, via M365 Agent or `Nucleus_ContentProcessing_McpServer`)**

*   **Trigger:** The M365 Persona Agent determines the PDF's size/complexity (e.g., page count) is *below* a configured threshold.
*   **Mechanism:** The M365 Persona Agent (using its `IChatClient`) or by making an MCP call to the **`Nucleus_ContentProcessing_McpServer`** (MCP Tool), sends the retrieved PDF content `Stream` directly to a capable multimodal LLM (e.g., Google Gemini).
*   **Prompt:** The LLM is instructed to:
    *   Analyze the PDF structure and layout.
    *   Extract text content.
    *   Perform OCR on embedded images if necessary and possible.
    *   Interpret the overall document.
    *   Generate a **single, coherent Markdown document** that best represents the original PDF's content and structure.
*   **Outcome:** If the LLM successfully processes the PDF and returns satisfactory Markdown:
    *   **Next Step:** The LLM-generated Markdown is passed by the M365 Persona Agent to the **`Nucleus_ContentProcessing_McpServer`** (MCP Tool, specifically its plaintext processing capabilities, see [ARCHITECTURE_INGESTION_PLAINTEXT.md](./ARCHITECTURE_INGESTION_PLAINTEXT.md)) for final ephemeral representation generation, or the Agent handles this directly.

**Attempt 2: External Pre-processing (Fallback, via `Nucleus_ContentProcessing_McpServer`)**

*   **Trigger:** The M365 Persona Agent determines that:
    *   The PDF's size/complexity exceeds the configured threshold for direct LLM processing, OR
    *   The direct LLM attempt (Attempt 1) fails, times out, or produces clearly inadequate results.
*   **Mechanism:** The M365 Persona Agent invokes the **`Nucleus_ContentProcessing_McpServer`** (MCP Tool), which in turn calls a dedicated, **external** pre-processing service, passing the retrieved PDF content `Stream`.
    *   *Example Implementation (within `Nucleus_ContentProcessing_McpServer` or its delegate):* An Azure Function App container running a library like `Pandoc`, `PyMuPDF`, `pdfminer.six`, or a commercial PDF SDK.
*   **Pre-processor Task:** The external service (called by `Nucleus_ContentProcessing_McpServer`) performs the heavy lifting:
    *   Parses the PDF structure.
    *   Extracts text blocks, preserving reading order where possible.
    *   Performs OCR on images.
    *   Converts the extracted content into a simplified intermediate format, typically **basic Markdown or plain text**.
*   **Outcome:** The external pre-processor returns the extracted text/Markdown content.
    *   **Next Step:** This intermediate output is passed by the M365 Persona Agent to the **`Nucleus_ContentProcessing_McpServer`** (MCP Tool, specifically its plaintext processing capabilities, see [ARCHITECTURE_INGESTION_PLAINTEXT.md](./ARCHITECTURE_INGESTION_PLAINTEXT.md)) for potential minor cleanup and final ephemeral representation generation.

## 3. Integration with Core Pipeline (M365 Agent & MCP Tool Centric)

Crucially, regardless of whether the PDF is processed via direct LLM or an external pre-processor, the final step is **always** the M365 Persona Agent ensuring the resulting text/Markdown is processed by the **`Nucleus_ContentProcessing_McpServer`** (MCP Tool, for plaintext to final Markdown conversion). This ensures:

*   The core ingestion flow remains consistent, orchestrated by the M365 Agent.
*   The final **ephemeral Markdown representation** consumed by M365 Persona Agents is always generated via the `Nucleus_ContentProcessing_McpServer`'s plaintext capabilities.
*   Downstream processes (retrieval, analysis by the M365 Persona Agent) interact with a uniform data type.

## 4. Configuration and Considerations

*   **Thresholds:** The decision boundary (file size, page count) between Attempt 1 and Attempt 2 must be configurable (e.g., in the M365 Persona Agent's configuration or the `Nucleus_ContentProcessing_McpServer`'s settings).
*   **Error Handling:** Robust error handling is needed for both paths (LLM failures, pre-processor failures, corrupted PDFs) within the M365 Persona Agent and the involved MCP Tools.
*   **External Dependency:** Acknowledges the introduction of an optional, external pre-processing service (likely invoked by `Nucleus_ContentProcessing_McpServer`), requiring separate deployment, maintenance, and cost considerations. This is deemed a necessary complexity trade-off specifically for robust PDF handling.
*   **Pre-processor Choice:** The specific library/tool used in the external pre-processor (managed by `Nucleus_ContentProcessing_McpServer`) can be chosen based on required features, performance, and licensing.

## 5. Key Principles Summary

This hybrid strategy adheres to the core principles:

*   **Simplicity First:** The M365 Persona Agent attempts the simplest path (direct LLM via its own `IChatClient` or `Nucleus_ContentProcessing_McpServer`) when feasible.
*   **Leverage LLM:** Uses the LLM's multimodal capabilities for direct interpretation when possible.
*   **Pragmatism:** The M365 Persona Agent orchestrates fallback to a specialized external tool (via `Nucleus_ContentProcessing_McpServer`) for cases exceeding current LLM limitations or requiring deeper parsing.
*   **Architectural Separation:** Keeps the specialized, complex PDF parsing logic *external* or within a dedicated MCP Tool (`Nucleus_ContentProcessing_McpServer`), not directly in the M365 Persona Agent.
*   **Canonical Markdown:** Ensures the final ephemeral output consumed by M365 Persona Agents is standardized Markdown generated via the `Nucleus_ContentProcessing_McpServer`'s plaintext capabilities.
