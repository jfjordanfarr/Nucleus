---
title: Architecture - Personas & Verticals
description: Details the architecture for implementing specialized AI assistants (Personas/Verticals) within the Nucleus OmniRAG platform.
version: 1.1
date: 2025-04-07
---

# Nucleus OmniRAG: Persona/Vertical Architecture

**Version:** 1.1
**Date:** 2025-04-07

This document details the architecture for implementing specialized AI assistants, referred to as "Personas" or "Verticals," within the Nucleus OmniRAG platform, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md).

## 1. Core Concept: Personas as Specialized Agents

Personas are distinct, configurable AI agents designed to address specific domains or user needs (e.g., education, business knowledge, personal finance). They encapsulate domain-specific logic, analysis capabilities, and interaction patterns, leveraging the core platform's infrastructure for data ingestion, processing (see [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md)), storage (see [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)), and retrieval from the [Database](./04_ARCHITECTURE_DATABASE.md).

## 2. The `IPersona` Interface

The foundation of the persona system is the `IPersona` interface (likely defined in `Nucleus.Abstractions` or a dedicated `Nucleus.Personas.Abstractions` project). This interface defines the essential contract for all personas, ensuring they can integrate consistently with the platform.

```csharp
// Example Interface (Subject to refinement)
public interface IPersona<TAnalysisData> where TAnalysisData : class
{
    // Identifier for the persona
    string PersonaId { get; }

    // Display name for the persona
    string DisplayName { get; }

    // Description of the persona's purpose and capabilities
    string Description { get; }

    // Performs persona-specific analysis on the standardized content (e.g., Markdown)
    // provided by the upstream processing pipeline, identifying relevant sections
    // and producing structured analysis results.
    Task<PersonaAnalysisResult<TAnalysisData>> AnalyzeContentAsync(ContentItem content, CancellationToken cancellationToken = default);

    // Handles a user query directed at this persona, leveraging retrieval
    // and AI generation to provide a contextual answer.
    Task<PersonaQueryResult> HandleQueryAsync(UserQuery query, CancellationToken cancellationToken = default);

    // (Optional) Method for persona-specific configuration loading
    Task ConfigureAsync(IConfigurationSection configurationSection);
}

// Supporting types (Examples)
// Represents the input for persona analysis, containing the standardized content.
public record ContentItem(string SourceIdentifier, string StandardizedContent, ArtifactMetadata Metadata, Dictionary<string, object>? AdditionalContext = null);
public record PersonaAnalysisResult<TAnalysisData>(TAnalysisData StructuredAnalysis, string RelevantTextSnippetOrSummary) where TAnalysisData : class;
public record UserQuery(string QueryText, string UserId, Dictionary<string, object> Context);
public record PersonaQueryResult(string ResponseText, List<string> SourceReferences);
```

Key responsibilities defined by `IPersona`:
*   **Identification:** Provide unique ID and descriptive info.
*   **Content Analysis:** Process the **standardized content (e.g., Markdown)** provided by the [Processing Pipeline](./01_ARCHITECTURE_PROCESSING.md), identify relevant sections *within that content*, and generate structured insights stored as `PersonaKnowledgeEntry` records in the [Database](./04_ARCHITECTURE_DATABASE.md).
*   **Query Handling:** Respond to user queries (originating from [Clients](./05_ARCHITECTURE_CLIENTS.md)) within its domain.
*   **Configuration:** Load persona-specific settings.

## 3. Hybrid Project Structure

To promote modularity and separation of concerns, personas are implemented using a hybrid project structure:

*   **`Nucleus.Personas` (Parent Project):**
    *   Contains shared utilities, base classes, or common logic applicable across multiple personas.
    *   May reference `Nucleus.Abstractions` and `Nucleus.Core`.
*   **Individual Persona Projects (e.g., `Nucleus.Personas.EduFlow`, `Nucleus.Personas.BusinessAssistant`):**
    *   Each project implements a specific `IPersona`.
    *   Contains persona-specific models (e.g., `LearningEvidenceAnalysis`), prompts, configuration, and logic.
    *   References the parent `Nucleus.Personas` project and potentially other core/infrastructure projects as needed.

This structure allows new personas to be added relatively independently.

## 4. Initial & Planned Verticals/Personas

Based on the [Project Mandate](./00_PROJECT_MANDATE.md) and existing project structure, the following are initial or planned personas:

### 4.1 Business Knowledge Assistant (Initial Vertical)

*   **Target Deployment:** Self-Hosted Instance (Persistent background processing).
*   **Goal:** Provide a reliable, access-controlled internal Q&A assistant grounded *only* in authorized company information sources.
*   **Key Functionality:**
    *   Implements `IPersona`.
    *   Primarily focused on the `HandleQueryAsync` aspect, retrieving relevant content from authorized sources based on the user query and potentially user permissions.
    *   Uses RAG (Retrieval Augmented Generation) to synthesize answers based *only* on retrieved content.
    *   Interacts with artifacts ingested via persistent connections configured in the self-hosted environment (SharePoint, configured object storage, etc.).
    *   May involve simpler analysis during ingestion compared to other personas, focusing more on accurate retrieval during query time.
    *   Leverages message queues for asynchronous ingestion and processing triggers.

### 4.2 EduFlow OmniEducator (Initial Vertical)

*   **Target Deployment:** Cloud-Hosted Service (Ephemeral user sessions).
*   **Goal:** Observe, document, and provide insights on authentic learning activities (e.g., analyzing Scratch projects, documents, web browsing related to a project).
*   **Key Functionality:**
    *   Implements `IPersona`.
    *   Defines specific analysis schemas (e.g., `LearningEvidenceAnalysis` record) for structuring insights.
    *   Uses AI (via `IChatClient`/Semantic Kernel) with tailored prompts to generate educational observations based on salient content.
    *   Focuses on processing artifacts accessed via ephemeral user tokens (Google Drive, potentially browser extensions).
    *   Analysis results are stored in the backend DB associated with the user's session/temporary identifier.

### 4.3 Other Planned Personas (Likely Cloud-Hosted Targets)

These represent other areas identified for potential persona development, primarily targeting individual users:

*   **HealthSpecialist:** Intended to process personal health data (e.g., fitness tracker logs, health records accessed via user permission) and provide health-related insights or summaries.
*   **PersonalAssistant:** Designed to manage personal tasks, appointments, deadlines, potentially integrating with calendars or task lists via user permission.
*   _(Others like PersonalFinance, WorldModeler could also be considered)_.

## 5. Interaction with Platform Services

Personas are clients of the core platform services, accessing them via dependency injection. The primary interaction patterns are:

*   **Content Analysis (`AnalyzeContentAsync`):** For analyzing new content, personas **receive the standardized content (e.g., Markdown) within the `ContentItem` directly from the processing pipeline**. They do not need to fetch the content themselves for this initial analysis step.
*   **Query Handling (`HandleQueryAsync`):** When responding to user queries, personas may need to access additional information. This involves:
    *   **Retrieval Service (`IRetrievalService` - Name TBC):** Querying the vector database (Knowledge DB) for relevant `PersonaKnowledgeEntry` records based on the user query.
    *   **Storage (`IFileStorage`) / Source APIs:** In some cases, if the retrieved context is insufficient or the persona needs to verify information against the original source, it *might* interact directly with `IFileStorage` or platform-specific APIs (like Microsoft Graph) to access original artifact content, **always respecting user permissions and context**. This is considered a secondary access pattern during querying.
*   **Database (`IRepository<T>`):** Reading/writing persona-specific analysis results (`PersonaKnowledgeEntry`), potentially accessing configuration or shared context.
*   **AI Client (`IChatClient`, `IEmbeddingGenerator` from `Microsoft.Extensions.AI`):** Interacting with LLMs for analysis generation, query answering, and potentially function calling. Note: `IEmbeddingGenerator` is used by the *pipeline*, not typically directly by the persona during analysis.
*   **Message Queue (Publisher/Subscriber):** Potentially publishing events (e.g., `AnalysisComplete`) or subscribing to relevant platform events (e.g., `NewContentAvailable`) - more common in self-hosted or complex workflow scenarios.

_Note: While personas analyze **standardized content** to identify relevant sections, they do NOT directly generate embeddings - this is the responsibility of the Processing Pipeline after receiving the persona's analysis results._

## 6. Persona-Specific Analysis Schemas

A key aspect is the ability for personas to generate structured analysis outputs.

*   **C# Records:** Define specific C# record types within the persona's project (e.g., `Nucleus.Personas.EduFlow.Models.LearningEvidenceAnalysis`).
*   **Semantic Kernel / Function Calling:** Leverage Semantic Kernel and LLM function calling (or JSON mode) to instruct the AI model to generate outputs conforming to the JSON schema derived from these records (as outlined in Memory `b62d4c46`).
*   **Implementation:** Personas use `IChatClient` from `Microsoft.Extensions.AI` (not a custom `IAiClient` abstraction) for LLM interactions, potentially wrapped in Semantic Kernel for structured output generation.
*   **Persistence:** These structured results are stored alongside the relevant text snippet in the database, allowing for targeted querying and aggregation later.

## 7. Configuration

Persona behavior can be configured via standard .NET mechanisms (`appsettings.json`, environment variables, Aspire configuration):

*   Persona-specific API keys or endpoints (if using unique external services).
*   Prompts and templates.
*   Feature flags or behavior toggles.
*   Thresholds (e.g., salience scores).

The `IPersona.ConfigureAsync` method provides a hook for personas to load their specific configuration sections.

## 8. Next Steps

1.  **Refine `IPersona` Interface:** Update the interface to standardize on `AnalyzeContentAsync` for content processing, ensuring it accepts **standardized content (e.g., Markdown via `ContentItem.StandardizedContent`)** and returns both structured analysis and identified relevant text.
2.  **Create Project Structure:** Set up the `Nucleus.Personas` parent project and initial `Nucleus.Personas.EduFlow` and `Nucleus.Personas.BusinessAssistant` projects. (Partially done, needs BusinessAssistant project).
3.  **Implement Base Logic:** Create base classes or shared utilities in `Nucleus.Personas` if needed.
4.  **Refactor EduFlow Implementation:** Update to use `IChatClient` from `Microsoft.Extensions.AI` (replacing `IAiClient`), implement the refined interface, and ensure it works directly with **standardized content**.
5.  **Develop Business Assistant:** Implement initial query handling logic using `IRetrievalService` and basic RAG.
6.  **DI Registration:** Ensure personas are discoverable and registered correctly in the application's DI container.
7.  **Integration Tests:** Test persona interaction with core services (mocked and potentially integrated).

---

_This architecture document provides the blueprint for personas, emphasizing their role in directly analyzing **standardized content**, identifying relevant sections, and generating structured insights without relying on generic pre-chunking or direct handling of diverse raw formats._
