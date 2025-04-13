# Architecture: PDF Ingestion

**Version:** 1.0
**Date:** 2025-04-13

This document outlines the ingestion strategy for Portable Document Format (PDF) files within the Nucleus OmniRAG architecture. PDFs represent a unique challenge due to their complex, fixed-layout nature, potentially containing a mix of structured text, raster images (requiring OCR), vector graphics, forms, and annotations. They are not inherently structured as archives of simple components like Office Open XML formats.

The strategy aims to balance the core principles of simplicity and leveraging LLMs with the practical need to reliably extract content from this often-difficult format.

## 1. Core Challenge

Directly applying the "bundle components + LLM synthesis" pattern used for File Collections (like `.docx`) is not feasible for PDFs because:

*   PDFs lack a standardized, easily extractable internal component structure comparable to OOXML.
*   Content extraction often requires specialized parsing libraries and Optical Character Recognition (OCR).
*   Layout information is critical to meaning but hard to capture reliably.

## 2. Processing Strategy: Hybrid Approach

Nucleus employs a two-pronged, configurable approach based on the PDF's characteristics (size, complexity) and the capabilities of available LLMs:

**Attempt 1: Direct Multimodal LLM Processing (Preferred)**

*   **Trigger:** PDF artifact detected, and its size/complexity (e.g., page count) is *below* a configured threshold.
*   **Mechanism:** The raw PDF file bytes (or potentially rendered images of its pages) are sent directly to a capable multimodal LLM (e.g., Google Gemini).
*   **Prompt:** The LLM is instructed to:
    *   Analyze the PDF structure and layout.
    *   Extract text content.
    *   Perform OCR on embedded images if necessary and possible.
    *   Interpret the overall document.
    *   Generate a **single, coherent Markdown document** that best represents the original PDF's content and structure.
*   **Outcome:** If the LLM successfully processes the PDF and returns satisfactory Markdown:
    *   **Next Step:** The LLM-generated Markdown is passed directly to the standard **Plaintext Processor** ([ARCHITECTURE_INGESTION_PLAINTEXT.md](./ARCHITECTURE_INGESTION_PLAINTEXT.md)) for final storage.

**Attempt 2: External Pre-processing (Fallback)**

*   **Trigger:** PDF artifact detected, and:
    *   Its size/complexity exceeds the configured threshold for direct LLM processing, OR
    *   The direct LLM attempt (Attempt 1) fails, times out, or produces clearly inadequate results.
*   **Mechanism:** The ingestion pipeline invokes a dedicated, **external** pre-processing service. This service is specifically designed for robust PDF parsing.
    *   *Example Implementation:* An Azure Function App container running a library like `Pandoc`, `PyMuPDF`, `pdfminer.six`, or a commercial PDF SDK.
*   **Pre-processor Task:** The external service performs the heavy lifting:
    *   Parses the PDF structure.
    *   Extracts text blocks, preserving reading order where possible.
    *   Performs OCR on images.
    *   Converts the extracted content into a simplified intermediate format, typically **basic Markdown or plain text**.
*   **Outcome:** The external pre-processor returns the extracted text/Markdown content.
    *   **Next Step:** This intermediate output is passed to the standard **Plaintext Processor** ([ARCHITECTURE_INGESTION_PLAINTEXT.md](./ARCHITECTURE_INGESTION_PLAINTEXT.md)) for potential minor cleanup and final storage as canonical Markdown.

## 3. Integration with Core Pipeline

Crucially, regardless of whether the PDF is processed via direct LLM or an external pre-processor, the final step is **always** passing the resulting text/Markdown to the **Plaintext Processor**. This ensures:

*   The core ingestion flow remains consistent.
*   The final artifact stored and linked in `ArtifactMetadata` is always the canonical Markdown representation.
*   Downstream processes (retrieval, analysis) interact with a uniform data type.

## 4. Configuration and Considerations

*   **Thresholds:** The decision boundary (file size, page count) between Attempt 1 and Attempt 2 must be configurable.
*   **Error Handling:** Robust error handling is needed for both paths (LLM failures, pre-processor failures, corrupted PDFs).
*   **External Dependency:** Acknowledges the introduction of an optional, external pre-processing service, requiring separate deployment, maintenance, and cost considerations. This is deemed a necessary complexity trade-off specifically for robust PDF handling.
*   **Pre-processor Choice:** The specific library/tool used in the external pre-processor can be chosen based on required features, performance, and licensing.

## 5. Key Principles Summary

This hybrid strategy adheres to the core principles:

*   **Simplicity First:** Attempts the simplest path (direct LLM) when feasible.
*   **Leverage LLM:** Uses the LLM's multimodal capabilities for direct interpretation when possible.
*   **Pragmatism:** Falls back to a specialized external tool for cases exceeding current LLM limitations or requiring deeper parsing.
*   **Architectural Separation:** Keeps the specialized, complex PDF parsing logic *external* to the core Nucleus ingestion processors.
*   **Canonical Markdown:** Ensures the final persisted output is always standardized Markdown via the Plaintext Processor.
