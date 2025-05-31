---
title: "Processing Architecture: Ingestion (Post-M365 Agent SDK Pivot)"
description: "Describes the orchestrated ingestion process, converting diverse artifact types into canonical ephemeral full textual representations for nuanced analysis by Nucleus M365 Persona Agents via backend MCP Tools."
version: 1.4
date: 2025-05-26
parent: ../../CoreNucleus/04_EPHEMERAL_PROCESSING_PIPELINE.md
---

# Processing Architecture: Ingestion (Post-M365 Agent SDK Pivot)

## 1. Overview

The Nucleus ingestion pipeline, now orchestrated by **Nucleus M365 Persona Agents** interacting with backend **Nucleus MCP Tool/Server applications**, is responsible for processing artifacts referenced by users. The primary goal is to convert diverse artifact types into a canonical, **ephemeral full textual representation** (typically Markdown) where feasible. This process prioritizes preserving the complete context of the original artifact, preparing it for nuanced, LLM-driven analysis by Persona Agents and for the *intelligent extraction of salient information* during subsequent retrieval and reasoning phases, rather than performing traditional, indiscriminate pre-chunking.

Nucleus strategically employs Large Language Models (LLMs) with extensive context windows (e.g., 1M+ tokens via configured providers like Google Gemini, Azure OpenAI, or OpenRouter.AI) *during* this ephemeral processing stage for tasks requiring common-sense reasoning, complex transformations, or rich descriptions of non-textual content.

## 2. Core Principles (Anti-Chunking Philosophy Reinforced)

*   **Create Full Ephemeral Textual Representations:** Maximize the capture of information from diverse sources into a canonical Markdown format *ephemerally* during the active processing of an interaction. This full representation provides the richest possible context for Persona Agent analysis.
*   **Leverage Large Context LLMs for Sophisticated Conversion:** Utilize LLMs (as configured for the M365 Persona Agent or relevant MCP Tool) for common-sense processing, summarization to extract essence, description generation (for multimedia), and synthesis of components into a coherent whole, all within the ephemeral processing scope.
*   **No Premature or Indiscriminate Chunking:** The system generates the full Markdown representation first (ephemerally). **Nucleus explicitly rejects mechanical, upfront chunking of entire documents into arbitrary segments for vectorization.** Instead, the full ephemeral representation is made available to the Persona Agent.
*   **No Direct Embedding of Full Ephemeral Content:** Vector embeddings are generated *by the Persona Agent's logic* (or a dedicated RAG MCP Tool it calls) based on **derived knowledge and intelligently identified salient segments or summaries** from the ephemeral full text, not by embedding the entire generated Markdown.
*   **Metadata is King for Discovery:** Persistently store rich [`ArtifactMetadata`](../../src/Nucleus.Abstractions/Models/ArtifactMetadata.cs) (pointers to source, status, structural cues) and [`PersonaKnowledgeEntry`](../../src/Nucleus.Abstractions/Models/PersonaKnowledgeEntry.cs) records (structured knowledge, intelligently extracted salient information/summaries, and their corresponding vector embeddings) in Cosmos DB via the `Nucleus_KnowledgeStore_McpServer`. This metadata and targeted knowledge form the primary index for retrieval.
*   **Strictly Ephemeral Processing Scope:** All intermediate representations (like extracted text from various file components, or the synthesized full Markdown) exist *only* within the memory/ephemeral storage scope of a single user interaction or background processing task. Nucleus **does not persistently store this generated full textual content**. It relies on its ability to re-fetch and re-process fresh source data via the `Nucleus_FileAccess_McpServer` (using `IArtifactProvider`) when a deep dive into the full content is required by a Persona Agent during a new interaction. This ensures data freshness and minimizes privacy risks associated with storing large processed versions of user content.
*   **Processors (as MCP Tools or Internal Logic) Produce Ephemeral Outputs for Persona Agents:** Components like the `Nucleus_ContentProcessing_McpServer` (handling specific file types) or internal logic within the `Nucleus_FileAccess_McpServer` generate ephemeral representations (Markdown, descriptions) that are consumed immediately by the calling Nucleus M365 Persona Agent (or the background worker acting on its behalf) within the same session/interaction.

## 3. High-Level Flow (Revised for M365 Agents & MCP Tools)

1.  **M365 Platform Interaction & `ArtifactReference` Creation:**
    *   A user interacts on an M365 platform (e.g., Teams, M365 Copilot), potentially sharing or referencing files.
    *   The **Nucleus M365 Persona Agent** receives an `Activity` from the M365 Agents SDK.
    *   The Agent parses the `Activity` and constructs `ArtifactReference` objects pointing to the artifacts in user-controlled M365 storage (e.g., SharePoint, OneDrive).
2.  **M365 Agent Orchestration & Ephemeral Content Retrieval (via MCP Tools):**
    *   The M365 Agent's logic (driven by `IPersonaRuntime` and its `PersonaConfiguration`) determines if artifact content is needed.
    *   If so, the Agent makes an MCP call to the **`Nucleus_FileAccess_McpServer`**, passing the `ArtifactReference`(s).
    *   The `Nucleus_FileAccess_McpServer` (using internal `IArtifactProvider` logic and the M365 Agent's Entra Agent ID context for permissions) ephemerally fetches the content stream(s) directly from the user's M365 source system.
3.  **Ephemeral Content Transformation & Synthesis (via MCP Tools or Agent Logic):**
    *   The fetched stream(s) are then processed to create a full textual representation:
        *   The `Nucleus_FileAccess_McpServer` might directly use internal `IContentExtractor`s for simple types.
        *   For more complex types (multimedia, file collections, PDFs requiring OCR), it might make further MCP calls to a specialized **`Nucleus_ContentProcessing_McpServer`**.
        *   This processing (e.g., describing images, transcribing audio, unpacking archives and synthesizing components using an LLM as described in specific ingestion docs) results in an **ephemeral Markdown** string (or other structured text).
    *   This final ephemeral Markdown is returned to the calling M365 Agent (or the background worker if the task was offloaded).
4.  **Persona Agent Analysis (on Full Ephemeral Representation):**
    *   The Nucleus M365 Persona Agent (or its background worker) now has the **full ephemeral textual representation** of the artifact(s).
    *   The `IPersonaRuntime` (guided by `PersonaConfiguration`) analyzes this full ephemeral representation to understand its meaning, extract structured data, and identify *salient information or summaries* relevant to the Persona's purpose or the current interaction.
5.  **Knowledge Extraction & Persistence (Targeted, Not Full Content):**
    *   The Persona Agent makes an MCP call to the **`Nucleus_KnowledgeStore_McpServer`** to:
        *   Save/update the `ArtifactMetadata` record for the source artifact.
        *   Save a `PersonaKnowledgeEntry` containing the *derived structured data, salient information/summaries, and vector embeddings of that specific salient information*, not the entire ephemeral Markdown.
6.  **Response Handling & Cleanup:**
    *   The M365 Agent formulates and sends its response to the user via the M365 SDK.
    *   All ephemeral representations (fetched content streams, intermediate outputs, full synthesized Markdown) are discarded at the end of the interaction's processing scope.
7.  **(Query Phase - Reinforcing Anti-Chunking):**
    *   When a user queries a Nucleus M365 Persona Agent:
        *   The Agent (via MCP calls to `Nucleus_KnowledgeStore_McpServer` and/or `Nucleus_RAGPipeline_McpServer`) primarily uses the persisted `PersonaKnowledgeEntry` records (with their targeted embeddings of salient info) and `ArtifactMetadata` to find relevant artifacts.
        *   If deeper context is needed for response synthesis, the Agent can then choose to re-fetch the *full ephemeral content* of top-ranked artifacts via the `Nucleus_FileAccess_McpServer` for that specific query, enabling nuanced understanding without relying on pre-chunked, potentially out-of-context data.

## 4. Performance, Cost, and Caching (for Ephemeral Processing LLM Calls)

**[CONTENT LARGELY KEPT - Principles are sound, LLM calls happen within Agents or MCP Tools]**

The strategy of processing full documents (ephemerally) or synthesizing complex textual representations using LLMs relies heavily on:
*   **Large Context Windows (1M+ Tokens):** The availability of LLMs that can handle very large amounts of text (e.g., Google's Gemini models, future Azure OpenAI offerings) is crucial. This allows the entire textual content of even large documents or complex synthesized bundles to be processed in a single pass, preserving overall context.
*   **Low Cost:** The per-token cost for these large context models must be economically viable for processing potentially many documents. Ongoing advancements in model efficiency and pricing are key enablers.
*   **Context Caching (LLM Provider Level):** Some LLM providers offer context caching. If an initial segment of a prompt (e.g., a large system message or a set of instructions) is reused across multiple calls with only appended new data, the provider might only charge for the new tokens. This can significantly reduce costs when processing multiple similar items or when iteratively building up a complex synthesis.

This combination enables the high-quality, context-preserving ephemeral processing central to Nucleus's intelligent analysis.

## 5. Specific Format Processors (Now Logic within MCP Tools)

Details for handling specific formats (now primarily as logic within the `Nucleus_FileAccess_McpServer` or a dedicated `Nucleus_ContentProcessing_McpServer`) are documented separately:

*   [Plaintext Files](./Ingestion/ARCHITECTURE_INGESTION_PLAINTEXT.md) **[TO BE MODIFIED]**
*   [PDF Files](./Ingestion/ARCHITECTURE_INGESTION_PDF.md) **[TO BE MODIFIED]**
*   [Multimedia Files (Image, Audio, Video)](./Ingestion/ARCHITECTURE_INGESTION_MULTIMEDIA.md) **[TO BE MODIFIED]**
*   [File Collections (e.g., Zip, Docx)](./Ingestion/ARCHITECTURE_INGESTION_FILECOLLECTIONS.md) **[TO BE MODIFIED]**
