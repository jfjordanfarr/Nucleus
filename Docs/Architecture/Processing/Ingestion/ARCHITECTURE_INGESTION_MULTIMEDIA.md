---
title: Ingestion Architecture - Multimedia Files
description: Describes the conversion of multimedia files, grounded against substantial background context, into a canonical, faithful, and complete textual representation.
version: 1.1
date: 2025-04-27
parent: ../ARCHITECTURE_PROCESSING_INGESTION.md
---

# Ingestion Architecture: Multimedia Files (Image, Audio, Video)

## 1. Role and Overview

The Multimedia File processor handles non-textual artifacts like images, audio files, and potentially video. It adheres to the core ingestion principles ([ARCHITECTURE_PROCESSING_INGESTION.md](../ARCHITECTURE_PROCESSING_INGESTION.md)) by converting these formats into a **rich, contextually grounded textual representation** suitable for downstream LLM analysis and retrieval.

This process leverages advanced multimodal Large Language Models (LLMs), such as Google Gemini, capable of interpreting visual and auditory information directly.

## 2. Core Strategy: Context-Grounded LLM Conversion

Instead of relying solely on traditional methods (e.g., basic OCR, simple transcription), this processor uses LLMs as a "common sense engine" to generate text that captures the salient aspects of the multimedia content *within the context* it was provided.

## 3. Processing Steps

1.  **Receive Multimedia & Context:** Accepts the multimedia artifact stream and crucial grounding context, typically provided by the orchestrating service (e.g., `OrchestrationService`) which has retrieved the stream via `IArtifactProvider.GetContentAsync` based on an `ArtifactReference`. This context may include:
    *   Source Metadata: Filename, timestamps, creator, related artifact IDs (from `ArtifactReference`).
    *   Conversational Context: Relevant parts of the user interaction where the artifact was shared, explicit user goals.
    *   Task Directives: Specific instructions like "Describe the trends in this chart image," or "Summarize the key decisions in this meeting audio."
2.  **Prepare LLM Prompt:** Constructs a prompt for the multimodal LLM, combining the grounding context (as text) with the multimedia data itself (the stream).
3.  **LLM Invocation:** Calls the appropriate multimodal LLM API (e.g., Gemini) with the prepared prompt and multimedia data. Leverage API features like **Context Caching** where applicable for efficiency.
4.  **Receive Textual Representation:** The LLM returns a generated text string (e.g., a detailed image description, a structured meeting summary from audio, a scene description from video).
5.  **Dispatch Text to Plaintext Processor:** Pass the generated text string downstream to the **Plaintext Processor** ([ARCHITECTURE_INGESTION_PLAINTEXT.md](./ARCHITECTURE_INGESTION_PLAINTEXT.md)) for conversion into the canonical ephemeral Markdown format.
6.  **Provide Context for Metadata:** Contributes information relevant to the eventual update of the [`ArtifactMetadata`](../../../../Nucleus.Abstractions/Models/ArtifactMetadata.cs) record performed by the orchestrator. This includes:
    *   Confirmation of successful conversion.
    *   Metadata indicating the source type (image, audio, video) and that the resulting text is a generated representation.
    *   Any relevant structured data extracted or inferred by the LLM during conversion.
    *   (The orchestrator retains the original `ArtifactReference` linking to the source multimedia file in user storage).

## 4. Key Principles & Considerations

*   **Maximizes Context:** The quality of the generated text heavily depends on the richness of the grounding context provided to the LLM.
*   **Leverages LLM Strengths:** Offloads the complex task of multimedia interpretation to the LLM, benefiting from its common-sense reasoning.
*   **No Native Chunking:** The generated text is passed to the Plaintext Processor to create the full ephemeral Markdown, avoiding premature chunking. Subsequent Persona analysis operates on this full representation.
*   **Potential for Iteration:** Future versions might involve iterative refinement with the LLM if the initial generated text is insufficient.
*   **Cost/Latency:** Relies on the performance and cost-effectiveness of the chosen multimodal LLM provider. Caching is vital.
*   **Video Processing:** Video might be handled by sampling frames (as images) and/or processing the audio track, potentially combining results via another LLM call.

## 5. Output

The output of this processor is the raw generated **text string**. This string is then processed by the [Plaintext Processor](./ARCHITECTURE_INGESTION_PLAINTEXT.md) to create the final ephemeral Markdown representation used by Personas.

This approach ensures that valuable information within multimedia artifacts is transformed into a usable textual format, deeply integrated with the surrounding context. The resulting ephemeral Markdown allows Personas to analyze the content, extract knowledge, and store relevant insights in `PersonaKnowledgeEntry` records for future use.
