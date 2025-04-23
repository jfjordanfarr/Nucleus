---
title: Architecture - Personas & Multi-Platform Interaction
description: Details the architecture for Personas, including multi-platform identities, interaction patterns, and persona-to-persona communication.
version: 1.5
date: 2025-04-22
---

# Nucleus OmniRAG: Persona Architecture

**Version:** 1.5
**Date:** 2025-04-22

This document details the architecture for implementing specialized AI assistants, referred to as "Personas" or "Verticals," within the Nucleus OmniRAG platform, as introduced in the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It covers their core concept, structure, configuration, and crucially, how they operate across multiple communication platforms.

*   **Related Architecture:**
    *   [Overall System Architecture](./00_ARCHITECTURE_OVERVIEW.md)
    *   [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md)
    *   [Client Architecture](./05_ARCHITECTURE_CLIENTS.md) (and specific adapters in `../ClientAdapters/`)
    *   [Processing Orchestration](./Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
    *   [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)
    *   [Database](./04_ARCHITECTURE_DATABASE.md)

## 1. Core Concept: Personas as Specialized Agents

Personas are distinct, configurable AI agents designed to address specific domains or user needs (e.g., education, business knowledge, personal finance). They encapsulate domain-specific logic, analysis capabilities, and interaction patterns, leveraging the core platform's infrastructure for data ingestion, processing (see [Processing Architecture](./01_ARCHITECTURE_PROCESSING.md)), storage (see [Storage Architecture](./03_ARCHITECTURE_STORAGE.md)), and retrieval from the [Database](./04_ARCHITECTURE_DATABASE.md).

A key design principle is that a Persona exists as an abstraction *above* specific communication platforms. The same "Professional Colleague" persona, with its unique knowledge and capabilities, should be accessible via Teams, Email, Slack, etc., if configured.

## 2. The `IPersona` Interface

The foundation of the persona system is the `IPersona<TAnalysisData>` interface, which serves as the essential contract for all personas. It ensures consistent integration with the core platform for tasks like content analysis and query handling.

**Authoritative Definition:** The precise C# definition of `IPersona<TAnalysisData>` and its related types (e.g., `ContentItem`, `PersonaAnalysisResult`, `UserQuery`, `PersonaQueryResult`) resides within the `Nucleus.Abstractions` project (specifically, likely in [`IPersona.cs`](../../../Nucleus.Abstractions/IPersona.cs)). Refer to the source code and its XML documentation for the exact method signatures and type definitions.

Key responsibilities defined conceptually by `IPersona`:
*   **Identification:** Provide unique ID and descriptive info.
*   **Content Analysis:** Process the **standardized content (e.g., Markdown)** provided by the [Processing Pipeline](./01_ARCHITECTURE_PROCESSING.md), identify relevant sections *within that content*, and generate structured insights stored as `PersonaKnowledgeEntry` records in the [Database](./04_ARCHITECTURE_DATABASE.md).
*   **Query Handling:** Respond to user queries (originating from [Clients](./05_ARCHITECTURE_CLIENTS.md)) within its domain.
*   **Configuration:** Load persona-specific settings.

## 3. Persona Profile & Multi-Platform Identities

To bridge the gap between the abstract Persona and concrete platform accounts, each Persona requires a profile that links its canonical identity to its representation on various platforms.

*   **Canonical Persona ID:** A unique, persistent identifier for the Persona within Nucleus (e.g., `professional-colleague`). This ID is used internally for routing, storage association, etc.
*   **Platform Identities:** A collection mapping the Persona to specific user accounts or addresses on supported platforms. Each entry includes:
    *   `PlatformType`: An enum identifying the platform (e.g., `Teams`, `Email`, `Discord`).
    *   Platform-Specific Identifiers: The necessary ID(s) for that platform (e.g., `UserId`, `TenantId` for Teams; `Address` for Email; `UserId` for Discord).
*   **Storage:** This profile information must be stored persistently and be accessible by the [`OrchestrationService`](./Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md). Potential stores include:
    *   Configuration files (`appsettings.json`, dedicated persona config files).
    *   A dedicated database table (e.g., in the [Operational Database](./04_ARCHITECTURE_DATABASE.md)).
*   **Resolution:** A mechanism (e.g., an `IPersonaResolver` service) is needed to map an incoming request's platform-specific identifiers (`PlatformType`, `OriginatingUserId`) to the corresponding Canonical Persona ID, and vice-versa for outgoing messages.

**Example Persona Profile Structure (Conceptual JSON):**

```json
{
  "PersonaId": "professional-colleague",
  "DisplayName": "Professional Colleague",
  "BasePrompt": "You are a helpful professional assistant...",
  "KnowledgeFilterTags": ["work", "projects"],
  "PlatformIdentities": [
    { "PlatformType": "Teams", "UserId": "uuid-for-teams-user", "TenantId": "contoso.onmicrosoft.com" },
    { "PlatformType": "Email", "Address": "colleague@contoso.com" },
    { "PlatformType": "Discord", "UserId": "discord-snowflake-id" }
    // Add other platforms as needed
  ],
  "Capabilities": ["web-search", "calendar-query"]
}
```

## 4. Hybrid Project Structure

To promote modularity and separation of concerns, personas are implemented using a hybrid project structure:

*   **`Nucleus.Personas.Core` (Parent Project):**
    *   Contains shared utilities, base classes, or common logic applicable across multiple personas.
    *   May reference `Nucleus.Abstractions` and `Nucleus.Domain.Processing`.
*   **Individual Persona Projects (e.g., `Nucleus.Personas.EduFlow`, `Nucleus.Personas.BusinessAssistant`):**
    *   Each project implements a specific `IPersona`.
    *   Contains persona-specific models (e.g., `LearningEvidenceAnalysis`), prompts, configuration, and logic.

This structure allows new personas to be added relatively independently.

## 5. Initial & Planned Verticals/Personas

Based on the [Project Mandate](../Requirements/00_PROJECT_MANDATE.md) and existing project structure, the following are initial or planned persona categories:

### 5.1 Professional Colleague (Initial Vertical)

*   **Detailed Architecture:** [./Personas/ARCHITECTURE_PERSONAS_PROFESSIONAL.md](./Personas/ARCHITECTURE_PERSONAS_PROFESSIONAL.md)

*   **Goal:** Simulates a professional colleague to provide insights, assistance, and perform tasks within a specific work context (e.g., IT Helpdesk, domain-specific knowledge Q&A).
*   **Target Deployment:** Often self-hosted or within enterprise environments (e.g., integrated with Teams) where access to internal knowledge bases is required.
*   **Key Functionality:**
    *   Implements `IPersona`.
    *   Leverages RAG against authorized company/domain-specific knowledge sources.
    *   May integrate with specific enterprise tools or APIs via client adapters.

### 5.2 EduFlow OmniEducator (Initial Vertical)

*   **Detailed Architecture:** [./Personas/ARCHITECTURE_PERSONAS_EDUCATOR.md](./Personas/ARCHITECTURE_PERSONAS_EDUCATOR.md)

*   **Target Deployment:** Cloud-Hosted Service (Ephemeral user sessions).
*   **Goal:** Observe, document, and provide insights on authentic learning activities (e.g., analyzing Scratch projects, documents, web browsing related to a project).
*   **Key Functionality:**
    *   Implements `IPersona`.
    *   Defines specific analysis schemas (e.g., `LearningEvidenceAnalysis` record) for structuring insights.
    *   Uses AI (via `IChatClient`/Semantic Kernel) with tailored prompts to generate educational observations based on salient content.
    *   Focuses on processing artifacts accessed via ephemeral user tokens (Google Drive, potentially browser extensions).
    *   Analysis results are stored in the backend DB associated with the user's session/temporary identifier.

### 5.3 Other Planned Personas (Likely Cloud-Hosted Targets)

These represent other areas identified for potential persona development, primarily targeting individual users:

*   **HealthSpecialist:** Intended to process personal health data (e.g., fitness tracker logs, health records accessed via user permission) and provide health-related insights or summaries.
*   **PersonalAssistant:** Designed to manage personal tasks, appointments, deadlines, potentially integrating with calendars or task lists via user permission.
*   _(Others like PersonalFinance, WorldModeler could also be considered)_.

## 6. Interaction with Core Services & Multi-Platform Flow

Personas interact with core platform services via DI. The [`OrchestrationService`](./Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md) plays a central role in managing the flow, especially across platforms.

*   **Content Analysis (`AnalyzeContentAsync`):** As stated before, personas receive **standardized content** (e.g., Markdown) within the `ContentItem` from the processing pipeline for initial analysis.

*   **Query Handling (`HandleQueryAsync`):** Involves retrieval from the Knowledge DB (`IRetrievalService`, `IPersonaKnowledgeRepository`) and interaction with LLMs (`IChatClient`).

*   **Fetching Original Artifacts (Targeted Fetching):**
    *   When the `OrchestrationService` receives a `NucleusIngestionRequest`, the request contains the `PlatformType` and `ContentSourceUri` of the original interaction.
    *   To fetch the original artifact content (if needed during query handling, *not* for initial analysis), the Orchestrator uses the **specific `PlatformType` from the request** to resolve the correct [`IPlatformAttachmentFetcher`](../../../Nucleus.Abstractions/IPlatformAttachmentFetcher.cs) implementation via DI.
    *   It then calls the fetcher with the `ContentSourceUri`.
    *   **Crucially, there is NO broadcast attempt across all adapters to find the original artifact.** Its source platform is known.

*   **Sending Outgoing Notifications (Targeted Send):**
    *   When a Persona, via the `OrchestrationService`, needs to send a notification or message:
    *   **Primary Case (Response):** The message is typically directed back to the originating user on the originating platform. The Orchestrator uses the context from the `NucleusIngestionRequest` (or session state) to identify the correct target `PlatformType` and user details.
    *   **Persona-to-Persona Case:** As described in Section 7, the initiating persona specifies the target persona and the desired platform for communication.
    *   **Mechanism:** In either case, the Orchestrator identifies the target canonical Persona ID (or user ID) and the specific target `PlatformType`.
    *   It looks up the target's corresponding `PlatformIdentity` (e.g., Teams user ID, Email address) in the Persona Profile (or original request context).
    *   It resolves the correct [`IPlatformNotifier`](../../../Nucleus.Abstractions/IPlatformNotifier.cs) implementation via DI using the target `PlatformType`.
    *   It invokes `SendNotificationAsync` on the specific notifier, providing the correct platform-specific target details.
    *   While the *mechanism* allows looking up multiple platform identities for a target, the typical flow involves sending to a *single, specific* platform per required message (either responding to the origin or sending a directed cross-persona message).

*   **Other Services:** Access to `IFileStorage`, `IRepository<T>`, `IChatClient`, `IEmbeddingGenerator` (pipeline), `IMessageQueuePublisher` remains as previously described, used as needed by the Persona logic or orchestration flow.

## 7. Persona-to-Persona Communication

Communication *between* Personas leverages the standard platform messaging mechanisms and the Nucleus ingestion pipeline, treating Personas like users on the platform:

1.  **Initiation:** Persona A's logic determines it needs to communicate with Persona B.
2.  **Addressing:** Persona A formulates a message containing a standard platform mention or address for Persona B (e.g., `@CreativeAssistant`, `creative-assistant@contoso.com`). The specific address/mention used must correspond to Persona B's identity *on the chosen communication platform*.
3.  **Platform Selection:** Persona A's logic (potentially guided by the Orchestrator or Session context) determines the most appropriate platform and conversation context to send the message.
4.  **Sending via Notifier:** Persona A instructs the `OrchestrationService` to send the message. The Orchestrator resolves the correct `IPlatformNotifier` for the chosen platform and invokes `SendNotificationAsync`, sending the message *as Persona A* (using its platform identity) to *Persona B's* platform identity.
5.  **Reception:** The Platform Adapter for the chosen platform (e.g., Teams Adapter) receives this message just like any other user message.
6.  **Ingestion:** The Adapter creates a *new* `NucleusIngestionRequest`, identifying Persona A (`OriginatingUserId`) and the target conversation. Platform mentions might be part of the message content.
7.  **Orchestration & Routing:** The request flows through the queue to the `OrchestrationService`. The Orchestrator determines the intended recipient (Persona B, based on the message content or conversation context) and routes the request to Persona B's `HandleQueryAsync` (or equivalent) method.

This closes the loop, allowing Personas to interact using the established platform communication channels and the Nucleus processing pipeline.

## 8. Persona-Specific Analysis Schemas

A key aspect is the ability for personas to generate structured analysis outputs.

*   **C# Records:** Define specific C# record types within the persona's project (e.g., `Nucleus.Personas.EduFlow.Models.LearningEvidenceAnalysis`).
*   **Semantic Kernel / Function Calling:** Leverage Semantic Kernel and LLM function calling (or JSON mode) to instruct the AI model to generate outputs conforming to the JSON schema derived from these records (as outlined in Memory `b62d4c46`).
*   **Implementation:** Personas use `IChatClient` from `Microsoft.Extensions.AI` (not a custom `IAiClient` abstraction) for LLM interactions, potentially wrapped in Semantic Kernel for structured output generation.
*   **Persistence:** These structured results are stored alongside the relevant text snippet in the database, allowing for targeted querying and aggregation later.

## 9. Configuration

Persona behavior can be configured via standard .NET mechanisms (`appsettings.json`, environment variables, Aspire configuration):

*   Persona-specific API keys or endpoints (if using unique external services).
*   Prompts and templates.
*   Feature flags or behavior toggles.
*   Thresholds (e.g., salience scores).

The `IPersona.ConfigureAsync` method provides a hook for personas to load their specific configuration sections.

## 10. Next Steps

1.  **Refine `IPersona` Interface:** Ensure consistency with analysis flow.
2.  **Implement Persona Profile Store:** Decide on and implement storage (config/DB).
3.  **Implement Persona Resolver:** Create service to map between canonical and platform IDs.
4.  **Update OrchestrationService:** Implement profile lookup, targeted fetching logic, and **targeted notification sending logic**.
5.  **Implement DI for Platform Services:** Ensure `IPlatformAttachmentFetcher` and `IPlatformNotifier` can be resolved by `PlatformType` (e.g., keyed services or factory pattern).
6.  **Create Project Structure:** Continue setting up persona projects.
7.  **Implement Personas:** Refactor EduFlow, develop Business Assistant.
8.  **DI Registration:** Ensure personas and related services are registered.
9.  **Integration Tests:** Test multi-platform flows.

---

_This architecture document provides the blueprint for personas, emphasizing multi-platform operation, targeted fetching, **targeted notification sending**, and standardized persona-to-persona communication._

### Key Services and Abstractions (Relevant to Multi-Platform)

*   **`IPlatformAttachmentFetcher`**: Used by Orchestrator for **targeted** fetching of original artifacts based on `PlatformType` from `NucleusIngestionRequest`.
*   **`IPlatformNotifier`**: Used by Orchestrator for sending outgoing messages/notifications, typically to a **single targeted platform** (origin or persona-to-persona target) based on Persona Profile lookups and context.
*   **`IPersonaResolver` (Proposed)**: Service to map between canonical Persona IDs and platform-specific identities.
*   **Persona Profile Store (Conceptual)**: Persistent storage for the mapping defined above.
*   **`IOrchestrationService`**: Central component managing the multi-platform logic (resolving services, looking up profiles, coordinating fetch/notify operations).

(Other services like `IArtifactMetadataService`, `IContentExtractor`, `IPersona`, `IChatClient`, `IEmbeddingGenerator`, `IPersonaKnowledgeRepository` remain relevant as described previously).
