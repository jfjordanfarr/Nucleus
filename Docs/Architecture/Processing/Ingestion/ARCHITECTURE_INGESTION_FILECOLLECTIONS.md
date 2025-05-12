---
title: Ingestion Architecture - File Collections
description: Describes how container artifacts (e.g., zip, docx) are unpacked, their components identified, processed via appropriate processors coordinated by the orchestrator, and finally synthesized into a single textual representation.
version: 1.2
date: 2025-04-28
parent: ../ARCHITECTURE_PROCESSING_INGESTION.md
---

# Ingestion Architecture: File Collections (e.g., Zip Archives, Docx)

## 1. Role and Overview

The File Collections processor handles artifacts that represent containers holding multiple individual files, such as Zip archives (`.zip`) or Office Open XML formats like `.docx`. Its primary responsibility is to **unpack** the container and **identify** its constituent files.

The [**Orchestration Service**](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) coordinates the subsequent processing. It takes the list of identified components from this processor, routes each component to the appropriate ingestion processor ([Plaintext](./ARCHITECTURE_INGESTION_PLAINTEXT.md), [Multimedia](./ARCHITECTURE_INGESTION_MULTIMEDIA.md), etc.), collects the resulting textual representations, and finally invokes the **Plaintext Processor** to synthesize a single, coherent Markdown document representing the original container.

This ensures adherence to the overall ingestion principles ([ARCHITECTURE_PROCESSING_INGESTION.md](../ARCHITECTURE_PROCESSING_INGESTION.md)), ultimately yielding a faithful ephemeral textual representation derived from all components.

## 2. Processing Flow (Orchestrator-Centric)

**Note:** While this document refers to specific conceptual processors like "FileCollections Processor", "Plaintext Processor", and "Multimedia Processor", a codebase search did not find dedicated interfaces or classes matching these exact names. The described logic might be implemented within other components or is planned for future development.

1.  **Receive Collection & Initiate:** The [**Orchestration Service**](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) receives an `ArtifactReference` for a container file (e.g., a `.zip` or `.docx`). It retrieves the content stream via [`IArtifactProvider.GetContentAsync(collectionRef)`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs) and updates the artifact's metadata status to 'Processing'.
2.  **Invoke FileCollections Processor (Unpack & Identify):** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) passes the stream to the **FileCollections Processor**. This processor:
    *   Unpacks the container's contents into a temporary, accessible location (e.g., ephemeral blob storage, temporary file system).
    *   Iterates through the extracted items.
    *   For each item, it determines its type (e.g., `text/xml`, `image/png`) and its path within the container.
    *   It **returns a list of identified components** back to the Orchestration Service. Each entry includes:
        *   Component Type (MIME type or inferred type)
        *   Relative Path within the container
        *   A handle/reference to the temporary unpacked content (e.g., a temporary URI or stream handle).
3.  **Orchestrate Component Processing:** The [**Orchestration Service**](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) receives the component list. For each component:
    *   Based on the **Component Type**, it determines the appropriate processor.
    *   It prepares the necessary context (including the handle to the temporary content and any relevant metadata like relative path).
    *   It invokes the target processor (e.g., **Multimedia Processor** for images, **Plaintext Processor** for XML/text) asynchronously.
4.  **Collect Component Results:** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) waits for all invoked component processors to complete. Each processor returns its result, which is typically a **text string** (e.g., the description from the Multimedia processor, the raw content from a simple text file read by the Plaintext processor).
5.  **Prepare Synthesis Bundle:** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) gathers all the returned text strings from the component processing steps into a **bundle**. This bundle might include structured information about the source of each text string (e.g., original file path within the container).
6.  **Invoke Plaintext Processor (Synthesis):** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) sends the complete **bundle** to the **Plaintext Processor** with a specific instruction (e.g., "Synthesize a single coherent Markdown document representing the original container artifact `[Container Name]` from these components, preserving structure where possible.").
7.  **Receive Synthesized Markdown:** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) receives the final, synthesized Markdown string from the Plaintext Processor.
8.  **Finalize Metadata & Cleanup:** The [**Orchestration Service**](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs):
    *   Updates the *original container's* [ArtifactMetadata](../../../../Nucleus.Abstractions/Models/ArtifactMetadata.cs) record, marking processing as complete and indicating that the synthesized Markdown representation is available (e.g., via a signal or by storing a handle/reference, *not* the content itself in metadata).
    *   Initiates cleanup of the temporary unpacked files and intermediate component data.

## 3. Key Principles & Considerations

*   **Separation of Concerns:** The FileCollections processor focuses solely on unpacking and identifying. The Orchestrator handles the complexity of routing, parallel processing, result collection, and final synthesis invocation.
*   **Orchestration is Key:** The [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs) is central to managing the lifecycle of processing complex, multi-part artifacts.
*   **Component Processing:** Relies on other specialized processors (Multimedia, Plaintext) to handle individual component types.
*   **Delegated Synthesis:** The final, complex task of synthesizing a coherent document from disparate parts is delegated to the Plaintext processor's LLM.
*   **Ephemeral Output:** The ultimate result is the synthesized ephemeral Markdown, referenced by the original container's metadata.
*   **Error Handling:** The Orchestrator must handle errors at each stage (unpacking, component processing, synthesis).
*   **Resource Management:** The Orchestrator and temporary storage mechanism need to manage resources for unpacked files and intermediate results.

## 4. Example: Processing a Word Document (.docx)

Illustrating the orchestrator-centric flow:

1.  **Receipt & Initiate:** Orchestrator receives `.docx` `ArtifactReference`, gets stream via [`IArtifactProvider`](../../../src/Nucleus.Abstractions/IArtifactProvider.cs), updates metadata to 'Processing'.
2.  **Invoke Unpack/Identify:** Orchestrator sends stream to FileCollections Processor.
3.  **Return Components:** FileCollections Processor unpacks to temp location and returns list to Orchestrator:
    *   { Type: `application/xml`, Path: `word/document.xml`, Handle: `temp_uri_1` }
    *   { Type: `application/xml`, Path: `word/styles.xml`, Handle: `temp_uri_2` }
    *   { Type: `image/png`, Path: `word/media/image1.png`, Handle: `temp_uri_3` }
    *   { Type: `image/jpg`, Path: `word/media/image2.jpg`, Handle: `temp_uri_4` }
    *   ...
4.  **Orchestrate Components:** Orchestrator sees the list:
    *   Invokes Plaintext Processor for `word/document.xml` (using `temp_uri_1`).
    *   Invokes Plaintext Processor for `word/styles.xml` (using `temp_uri_2`).
    *   Invokes Multimedia Processor for `image1.png` (using `temp_uri_3`).
    *   Invokes Multimedia Processor for `image2.jpg` (using `temp_uri_4`).
5.  **Collect Results:** Orchestrator awaits results:
    *   Plaintext Proc -> `doc_xml_content` string.
    *   Plaintext Proc -> `styles_xml_content` string.
    *   Multimedia Proc -> `description1_text` string.
    *   Multimedia Proc -> `description2_text` string.
6.  **Prepare & Dispatch Bundle:** Orchestrator bundles (`doc_xml_content`, `styles_xml_content`, `description1_text`, `description2_text`, ...) with context.
7.  **Invoke Synthesis:** Orchestrator sends bundle to Plaintext Processor: "Synthesize Markdown for Word doc `MyReport.docx` from these components..."
8.  **Receive Synthesized Markdown:** Orchestrator gets the final Markdown string.
9.  **Finalize Metadata & Cleanup:** Orchestrator updates `MyReport.docx` metadata (status: Complete, result available) and cleans up temporary files (`temp_uri_1` etc.).

**Outcome:** The single `.docx` artifact is now represented by its `ArtifactMetadata`, indicating the availability of the synthesized ephemeral Markdown content. This synthesized content, intelligently generated by an LLM from the document's structure and content (including image descriptions), becomes the basis for Persona analysis and knowledge extraction (`PersonaKnowledgeEntry`).

This LLM-driven synthesis, coordinated by the [Orchestration Service](../../../src/Nucleus.Abstractions/Orchestration/IOrchestrationService.cs), avoids dedicated Office parsers, leveraging the Plaintext processor's LLM for final representation generation.
