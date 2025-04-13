---
title: Persona: Educator ('EduFlow OmniEducator')
description: Describes a pre-build persona for Nucleus OmniRAG that is focused on educational use cases, including classroom chats, one-on-one precision teaching, personalized learning experiences, and rich reporting capabilities.
version: 1.0
date: 2025-04-13
---


# Persona: Educator ('EduFlow OmniEducator')

## 1. Vision & Purpose

The Educator persona, initially conceived as EduFlow OmniEducator, is the flagship persona for Nucleus OmniRAG. It stems from the need to create a safer, more personalized, and authentic educational experience, particularly recognizing learning within digital creation and self-directed projects. Traditional educational models often fail to capture or support this intrinsic drive.

This persona aims to:

*   **Observe Authenticity:** Capture and understand learning as it happens naturally (e.g., coding projects, game modding, digital art).
*   **Document Process:** Analyze *how* learning occurs (capabilities, processes, cognitive depth) using its "Learning Facets" schema, alongside *what* subjects are touched upon.
*   **Build Understanding:** Create a dynamic, private knowledge base for each learner, mapping their unique trajectory.
*   **Provide Insight:** Enable querying of this knowledge base for meaningful progress reports and contextually relevant support.
*   **Foster Engagement:** Potentially leverage Nucleus capabilities (like dynamic content generation via tools) to create tailored micro-learning experiences.

It transforms ephemeral moments of creation into tangible evidence of learning, supporting learners, educators, and parents.

## 2. Typical Request Flow (Learning Artifact Analysis - Slow Path)

**Purpose:** Illustrates the asynchronous analysis of a submitted learning artifact (e.g., a screen recording of a coding session).

```mermaid
sequenceDiagram
    participant LearnerApp as Learner Interface / App
    participant ACA_Instance as Azure Container App Instance (API / Ingestion / Processing)
    participant TempStorage as Temporary Raw Data Storage
    participant KnowledgeDB as Cosmos DB (IngestionRecord, ArtifactMetadata & PersonaKnowledgeEntries)
    participant EducatorPersona as Educator Persona Module (within ACA)
    participant AIService as AI Model (e.g., Google Gemini)

    LearnerApp->>+ACA_Instance: Submits Artifact (API Request)
    activate ACA_Instance
    ACA_Instance->>ACA_Instance: Establish Session Context (In-Memory)
    ACA_Instance->>+TempStorage: Store Raw Artifact (gets Temp Pointer)
    ACA_Instance->>+KnowledgeDB: Create IngestionRecord (IngestionID, SourceType, Temp Pointer)
    ACA_Instance->>ACA_Instance: Schedule Background Task (IngestionID, Session Context Ref)
    ACA_Instance-->>-LearnerApp: Return API Response (e.g., Accepted 202)
    deactivate ACA_Instance

    %% --- Background Task Execution --- %%
    activate ACA_Instance #LightSkyBlue
    ACA_Instance->>+KnowledgeDB: Read IngestionRecord (using IngestionID)
    ACA_Instance->>+TempStorage: Fetch Raw Artifact Data (using Temp Pointer)
    ACA_Instance->>+KnowledgeDB: Create/Update ArtifactMetadata (Initial - Status: Processing)
    ACA_Instance->>+EducatorPersona: Initiate Analysis (Salience Check, pass Raw Data/Metadata/Context)
    Note right of EducatorPersona: Persona determines if artifact is relevant based on data and session context.
    EducatorPersona-->>-ACA_Instance: Confirms Salience
    ACA_Instance->>+EducatorPersona: Process Artifact (Extract Text/Features, Use AI)
    EducatorPersona->>+AIService: Analyze Content (Apply Learning Facets, Identify Skills/Domains)
    AIService-->>-EducatorPersona: Return Analysis Results (Structured Data)
    EducatorPersona-->>-ACA_Instance: Return Derived Knowledge (e.g., LearningEvidenceAnalysis)
    ACA_Instance->>+AIService: Generate Embeddings for Key Snippets/Analysis
    AIService-->>-ACA_Instance: Return Embeddings
    ACA_Instance->>+KnowledgeDB: Write PersonaKnowledgeEntry (Analysis, Embeddings)
    ACA_Instance->>+KnowledgeDB: Update ArtifactMetadata (Processing Complete)
    ACA_Instance->>-TempStorage: Delete Raw Artifact Data
    deactivate ACA_Instance #LightSkyBlue

    %% Optional Notification Back to Learner App can be added here (e.g., via SignalR/WebSockets) %%
```

**Explanation:** A learner submits an artifact via an API call to the main Azure Container App (ACA) instance. The ACA handler establishes session context, creates an Ingestion Record, stores raw data temporarily, and schedules an in-process background task. The API responds quickly. The background task then executes within the same ACA instance, fetches the raw data using the Ingestion ID, accesses session context if needed, and invokes the Educator Persona. The Persona checks relevance (salience) and, if salient, orchestrates the analysis using AI services (like Gemini). The derived knowledge and embeddings are stored as a `PersonaKnowledgeEntry` in Cosmos DB. Finally, temporary data is cleaned up.

## 3. Interaction Example (Query - Fast Path)

(To be defined - would show querying the KnowledgeDB for insights about learner progress based on analyzed artifacts).

## Knowledge Framework: Pedagogical and Tautological Trees

The Educator persona operates with a sophisticated understanding of child development and learning, codified within two distinct knowledge frameworks:

1.  **Pedagogical Tree:** Maps learning activities and artifacts to specific subject domains and genuine skill acquisition (e.g., mastering algebraic concepts, understanding historical timelines, developing artistic techniques).
2.  **Tautological Tree:** Maps the same activities to underlying cognitive, social-emotional, and physical developmental processes (e.g., logical reasoning, collaborative problem-solving, fine motor control), often aligning with formal educational standards or reporting requirements.

This dual-lens approach allows the Educator to analyze learning comprehensively, supporting authentic skill development while also facilitating necessary documentation and reporting.

For a detailed explanation of this framework and links to the age-specific knowledge breakdowns (Ages 5-18), please see:

*   **[Educator Persona Knowledge Trees](./Educator/ARCHITECTURE_EDUCATOR_KNOWLEDGE_TREES.md)**

---

*Previous detailed tree definitions removed and consolidated into the linked document.*
