---
title: "Ingestion Architecture: Multimedia Files (Post-M365 Agent SDK Pivot)"
description: "Describes the conversion of multimedia files into canonical ephemeral textual representations by Nucleus M365 Persona Agents using backend MCP Tools and multimodal LLMs."
version: 1.3
date: 2025-05-26
parent: ../ARCHITECTURE_PROCESSING_INGESTION.md
---

# Ingestion Architecture: Multimedia Files (Image, Audio, Video) (Post-M365 Agent SDK Pivot)

## 1. Role and Overview

The processing of multimedia files (images, audio, video) within the Nucleus ecosystem, now driven by **Nucleus M365 Persona Agents** and backend **Nucleus MCP Tool/Server applications**, adheres to the core ingestion principles outlined in [ARCHITECTURE_PROCESSING_INGESTION.md](../ARCHITECTURE_PROCESSING_INGESTION.md). The goal is to convert these non-textual artifacts into a **rich, contextually grounded, ephemeral textual representation** (typically Markdown) suitable for nuanced analysis by Persona Agents.

This process leverages advanced multimodal Large Language Models (LLMs), such as Google Gemini or Azure OpenAI's multimodal capabilities, configured for and invoked by the M365 Persona Agent or a specialized `Nucleus_ContentProcessing_McpServer`.

## 2. Core Strategy: Context-Grounded LLM Conversion via M365 Agents & MCP Tools

Instead of relying solely on traditional methods, Nucleus uses LLMs as a "common sense engine" to generate text that captures the salient aspects of the multimedia content *within the context* it was provided. This is orchestrated by the M365 Persona Agent, which may delegate specific conversion tasks to MCP Tools like `Nucleus_ContentProcessing_McpServer`.

## 3. Processing Steps (Revised Flow)

1.  **M365 Agent Receives Multimedia Reference & Context:**
    *   A **Nucleus M365 Persona Agent** (or a background worker acting on its behalf) receives an `Activity` or identifies a multimedia artifact (e.g., image, audio file) via an `ArtifactReference` (e.g., from a Teams message or a file share event).
    *   The Agent gathers crucial grounding context: source metadata (filename, timestamps from `ArtifactReference`), conversational context from the M365 platform, and any explicit user goals or task directives.
2.  **Ephemeral Content Retrieval (via `Nucleus_FileAccess_McpServer` (MCP Tool)):**
    *   The M365 Agent makes an MCP call to the **`Nucleus_FileAccess_McpServer`** (MCP Tool), passing the `ArtifactReference`.
    *   The `Nucleus_FileAccess_McpServer` (MCP Tool) (using internal `IArtifactProvider` logic and the M365 Agent's security context) ephemerally fetches the multimedia content stream from the user's M365 source system.
3.  **Prepare LLM Prompt & Multimedia Data for Conversion:**
    *   The M365 Agent (or the `Nucleus_FileAccess_McpServer` (MCP Tool) / `Nucleus_ContentProcessing_McpServer` (MCP Tool) if the task is delegated) prepares a prompt for the configured multimodal LLM (e.g., Gemini, Azure OpenAI).
    *   This prompt combines the grounding context (as text) with the multimedia data itself (the stream).
4.  **LLM Invocation (via `IChatClient` or MCP Tool):**
    *   The M365 Agent or the designated MCP Tool (e.g., `Nucleus_ContentProcessing_McpServer` (MCP Tool)) calls the appropriate multimodal LLM API (using its configured `IChatClient` or the MCP Tool's internal LLM client) with the prepared prompt and multimedia data. API features like Context Caching are leveraged where applicable. The M365 Agent typically performs this step directly using its `IChatClient` for describing multimedia content.
5.  **Receive Textual Representation:** The LLM returns a generated text string (e.g., a detailed image description, a structured meeting summary from audio, a scene description from video).
6.  **Dispatch Text for Markdown Conversion (Potentially via `Nucleus_ContentProcessing_McpServer` (MCP Tool) or Agent Logic):**
    *   The generated raw text string is then processed to create the canonical ephemeral Markdown.
    *   This might involve the M365 Agent sending this text to the `Nucleus_ContentProcessing_McpServer` (MCP Tool) (if it has specific Markdown conversion logic for LLM outputs) or the Agent itself performing this conversion using internal logic or another LLM call for structuring, as detailed in the [Plaintext Processor](./ARCHITECTURE_INGESTION_PLAINTEXT.md) architecture.
7.  **Return Ephemeral Markdown to M365 Agent:** The final ephemeral Markdown representation is made available to the M365 Persona Agent (or the background worker).
8.  **Context for `ArtifactMetadata`:** The M365 Agent, having orchestrated this, will later ensure the `Nucleus_KnowledgeStore_McpServer` (MCP Tool) updates the `ArtifactMetadata` with:
    *   Confirmation of successful conversion.
    *   Indication of source type (image, audio, video) and that the textual representation is AI-generated.
    *   The original `ArtifactReference` to the multimedia file is preserved.

## 4. Key Principles & Considerations (Reinforced)

*   **Maximizes Context:** The quality of the generated text heavily depends on the richness of the grounding context provided to the LLM by the M365 Agent.
*   **Leverages LLM Strengths:** Offloads complex multimedia interpretation to LLMs, orchestrated by Agents/MCP Tools.
*   **No Native Chunking / Ephemeral Full Representation:** The process results in a *full* ephemeral Markdown representation. **Nucleus explicitly rejects mechanical, upfront chunking.** The M365 Persona Agent analyzes this full representation to derive salient information for `PersonaKnowledgeEntry` records.
*   **Ephemeral Processing:** All intermediate data (fetched streams, raw LLM text output) and the final Markdown are transient and discarded after the processing task completes.
*   **Potential for Iteration:** Future versions might involve iterative refinement with the LLM, orchestrated by the M365 Agent or an MCP Tool.
*   **Cost/Latency:** Relies on the performance and cost-effectiveness of the chosen multimodal LLM provider. Caching (at LLM provider level or via custom MCP Tool caching) is vital.
*   **Video Processing:** Video might be handled by sampling frames (as images) and/or processing the audio track, with results potentially combined by an LLM, orchestrated by the M365 Agent or `Nucleus_ContentProcessing_McpServer`.

## 5. Output (Revised)

The final output of this orchestrated process is a **canonical, ephemeral Markdown string**. This Markdown is consumed by the **Nucleus M365 Persona Agent** for its analysis, from which *it* will derive salient information and insights to be stored in `PersonaKnowledgeEntry` records (via the `Nucleus_KnowledgeStore_McpServer` (MCP Tool)).

This approach ensures that valuable information within multimedia artifacts is transformed into a usable textual format, deeply integrated with the surrounding context, and aligned with Nucleus's anti-chunking and intelligence-first philosophy within the M365 Agent and MCP architecture.
