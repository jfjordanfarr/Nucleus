---
title: Persona - Bootstrapper
description: Describes the basic Bootstrapper persona for Nucleus OmniRAG, serving as a foundation or fallback.
version: 0.1
date: 2025-05-17
---

# Persona: Bootstrapper

## 1. Purpose

The Bootstrapper persona represents the most fundamental level of interaction within the Nucleus OmniRAG system. It is designed to be simple and serve potentially as:

*   **Initial Setup/Testing:** Provides a basic interaction layer for verifying core system functionality (ingestion, storage, basic retrieval) without complex domain logic.
*   **Fallback Behavior:** Could act as a default persona if a more specialized one is not specified or applicable.
*   **Foundation:** Represents the core `IPersona` implementation from which more complex personas can inherit or delegate.

It typically involves minimal analysis during ingestion and focuses on direct retrieval or simple query handling.

## 2. Typical Request Flow (Simple Query)

**Purpose:** Illustrates a basic query flow with minimal processing.

```mermaid
sequenceDiagram
    participant UserInterface as User Interface
    participant QueryAPI as Nucleus Query API
    participant BootstrapperPersona as Bootstrapper Persona Module
    participant KnowledgeDB as Cosmos DB (ArtifactMetadata & Minimal PKEs)
    participant AIService as AI Model (Optional)

    UserInterface->>+QueryAPI: Sends Basic Query
    QueryAPI->>+BootstrapperPersona: Handle Query Request
    BootstrapperPersona->>+KnowledgeDB: Retrieve Relevant ArtifactMetadata or simple PKEs
    KnowledgeDB-->>-BootstrapperPersona: Return Basic Data/Text Snippets
    %% Optional: Simple AI summarization/formatting %%
    BootstrapperPersona->>+AIService: (Optional) Format/Summarize Snippets
    AIService-->>-BootstrapperPersona: Formatted Response
    BootstrapperPersona-->>-QueryAPI: Return Response
    QueryAPI-->>-UserInterface: Display Response

```

**Explanation:** A user sends a simple query. The Bootstrapper persona directly retrieves potentially relevant metadata or very basic stored knowledge entries (perhaps just text snippets). It might perform minimal formatting, possibly using an AI service, before returning the result. Deep analysis or complex synthesis is generally avoided.

## 3. Key Characteristics

*   **Simplicity:** Minimal domain logic and analysis.
*   **Directness:** Focuses on basic retrieval and interaction.
*   **Foundation:** Implements the core `IPersona` contract.
*   **Low Overhead:** Requires minimal configuration and processing resources compared to specialized personas.
