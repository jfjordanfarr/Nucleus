# Nucleus OmniRAG: Storage Architecture

**Version:** 1.6
**Date:** 2025-04-13

This document outlines the architecture for managing **artifacts** and their associated **metadata** within the Nucleus OmniRAG system. A fundamental principle is that Nucleus **does not maintain its own persistent artifact storage**. Instead, it interacts with artifacts directly within the user's chosen source systems (e.g., Microsoft Teams/SharePoint, Slack, Email Servers) via platform-specific adapters, respecting existing permissions. Nucleus's own persistent storage (Cosmos DB) is reserved exclusively for **metadata** (`ArtifactMetadata`, `PersonaKnowledgeEntry`) derived from or describing these external artifacts.

## 1. Core Principles

*   **User Source System is Authoritative:** The original artifact (user-provided *or* Nucleus-generated) resides and is managed within the user's designated source system (SharePoint, Slack Files, Google Drive, etc.). Nucleus interacts with these systems via adapters (see `05_ARCHITECTURE_CLIENTS.md`).
*   **Metadata in Nucleus DB:** Essential metadata about the source artifact (`ArtifactMetadata`) and persona-specific analysis (`PersonaKnowledgeEntry`) are stored in Nucleus's own database layer (Cosmos DB, see `04_ARCHITECTURE_DATABASE.md`). This metadata contains pointers (`sourceUri`) to the actual artifact in the user's system.
*   **No Intermediate Storage:** Nucleus avoids creating its own persistent store for artifacts. Temporary, in-memory streams or extremely short-lived local caches might be used during processing, but **no intermediate persistence** (like Azure Blob Storage or dedicated file shares) should be relied upon.
*   **Permission-Based Access:** Nucleus accesses artifacts in the source system using the permissions granted to its corresponding bot or application identity within that platform (e.g., Teams bot permissions to read/write files in a channel).
*   **Support Diverse Sources:** The architecture must accommodate artifacts from various locations accessible via APIs (cloud drives, collaboration platforms, email attachments).

## 2. Key Metadata Structure: `ArtifactMetadata`

The `ArtifactMetadata` record is the central object persisted in the `ArtifactMetadataContainer` within Cosmos DB for *every* unique artifact Nucleus interacts with. It represents Nucleus's understanding of the artifact's properties and context, derived primarily from the source system via Adapters.

**Conceptual Fields (Illustrative - Not Exhaustive):**

*   **Identification & Core:**
    *   `id`: (string) Unique Cosmos DB document ID. *Primary Key.*
    *   `sourceIdentifier`: (string) A stable, unique logical identifier *within Nucleus* for this artifact (e.g., `spo://tenant.sharepoint.com/sites/TeamSite/Shared%20Documents/Report.docx?ver=1`, `msteams://channel/19:xxx@thread.tacv2/messageid/12345`, `slack://ws/T123/C456/p1678886400.123456`). *Logically Unique Key, Indexed.*
    *   `sourceUri`: (string) The direct URI or locator used by the Platform Adapter to access the artifact content in the source system.
    *   `sourceSystemType`: (string enum) e.g., `SharePoint`, `OneDrive`, `MSTeams`, `Slack`, `Web`, `LocalFile`, `Email`, `NucleusGenerated`.
    *   `displayName`: (string) The human-readable name (e.g., filename, message snippet, page title).
    *   `mimeType`: (string | null) e.g., `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `text/plain`, `message/rfc822`.
    *   `sizeBytes`: (long | null) Size in bytes, if applicable.

*   **Relationships & Context (`Where` / `How` Part 1):**
    *   `parentSourceIdentifier`: (string | null) `sourceIdentifier` of the container artifact (e.g., folder, channel, message for an attachment).
    *   `replyToSourceIdentifier`: (string | null) `sourceIdentifier` of the message this artifact is a reply to.
    *   `threadSourceIdentifier`: (string | null) `sourceIdentifier` of the root message of the conversation thread this artifact belongs to.
    *   `referencedSourceIdentifiers`: (List<string> | null) `sourceIdentifier`s of other artifacts explicitly mentioned or linked within this one (e.g., linked files, @mentioned artifacts).
    *   `compositeArtifactId`: (string | null) Identifier if this artifact is part of a larger logical unit (e.g., a multi-part email, a slide in a presentation represented individually).
    *   `originatingContext`: (object | null) Additional source-specific context (e.g., `{ "channelId": "19:xxx@thread.tacv2", "teamId": "uuid" }` for Teams).

*   **Authorship & Ownership (`Who`):**
    *   `sourceCreatedByUserId`: (string | null) Identifier of the user who created the artifact in the source system.
    *   `sourceLastModifiedByUserId`: (string | null) Identifier of the user who last modified it in the source system.
    *   `ownerUserId`: (string | null) Identifier of the user considered the 'owner' within Nucleus context (might align with `sourceCreatedByUserId` or be assigned).

*   **Timestamps (`When`):**
    *   `sourceCreatedAt`: (DateTimeOffset | null) Timestamp from the source system.
    *   `sourceLastModifiedAt`: (DateTimeOffset | null) Timestamp from the source system.
    *   `timestampIngested`: (DateTimeOffset) When Nucleus first created this metadata record.
    *   `timestampLastProcessed`: (DateTimeOffset | null) When any persona last successfully processed this artifact.
    *   `timestampLastAccessed`: (DateTimeOffset | null) When the content was last fetched via an adapter (optional, for cache/staleness checks).

*   **Content & Purpose (`What` / `Why` - Often Inferred/Extracted):**
    *   `contentHash`: (string | null) Hash of the content (e.g., SHA256) for change detection.
    *   `extractedTextLength`: (int | null) Length of text extracted during processing.
    *   `summary`: (string | null) A brief, objective summary extracted/generated by a foundational process (not persona-specific).
    *   `keywords`: (List<string> | null) Extracted keywords.
    *   `potentialPurposeTags`: (List<string> | null) Inferred tags about the artifact's likely purpose (e.g., 'report', 'meeting_notes', 'user_query').

*   **Generation Info (`How` Part 2 - For Nucleus-Generated Artifacts):**
    *   `generationMethod`: (string | null) e.g., `persona_generated`, `system_process`.
    *   `generatingPersonaName`: (string | null) If `generationMethod` is `persona_generated`, the name of the persona (e.g., `ProfessionalColleague_v1`).
    *   `originatingSourceIdentifier`: (string | null) The `sourceIdentifier` of the artifact (e.g., user message) that triggered the generation of this artifact.

*   **Processing State:**
    *   `overallProcessingState`: (string enum) e.g., `Pending`, `Processing`, `Processed`, `Failed`, `Skipped`.
    *   `personaProcessingStatus`: (Dictionary<string, PersonaProcessingState>) Tracks the status per target persona (e.g., `{"EduFlow_v1": {"state": "Processed", "timestamp": "...", "pkeId": "guid"}, "Health_v2": {"state": "Pending"}}`). Includes status, timestamp, errors, and optionally the ID of the generated `PersonaKnowledgeEntry`.

*   **Access Control:**
    *   `accessControlList`: (List<ACLEntry> | null) Denormalized list of users/groups and their permissions derived from the source system (can be complex).
    *   `sensitivityLabel`: (string | null) e.g., 'Confidential', 'Public' (from source system).

**(Note:** The exact fields, types, and nullability will be refined during C# model implementation and depend on Adapter capabilities.)

## 3. Handling Persona-Generated Artifacts

A core principle is that Nucleus **does not manage its own persistent artifact storage layer**. This applies equally to artifacts generated *by* Personas (e.g., reports, summaries, code, data visualizations) in response to user requests.

1.  **Generation:** A Persona generates the content for a new artifact.
2.  **Storage via Adapters:** The Nucleus Processing Pipeline takes the generated content and uses the appropriate **Platform Adapter** (e.g., `SharePointAdapter`, `MSTeamsAdapter`) to save this content *back into the user's designated source system* (e.g., a specific SharePoint library, the Files tab of a Teams channel, a user's OneDrive).
3.  **Metadata Creation:** Upon successful saving, the Adapter returns the necessary details (like the `sourceUri`, `displayName`, `sizeBytes`, etc.) of the newly created artifact in the source system.
4.  **`ArtifactMetadata` Record:** Nucleus creates a standard `ArtifactMetadata` record for this newly generated artifact in the `ArtifactMetadataContainer`.
5.  **Linking Generation Context:** This new record's `ArtifactMetadata` is populated with:
    *   `sourceSystemType`: The target system (e.g., `SharePoint`).
    *   `sourceUri`: The URI where the generated artifact now resides.
    *   `generationMethod`: Set to `persona_generated`.
    *   `generatingPersonaName`: The name of the persona that produced the content.
    *   `originatingSourceIdentifier`: The `sourceIdentifier` of the user's request or context that led to this generation.

This approach ensures:
*   User data (including generated artifacts) remains within their managed environment.
*   Generated artifacts are discoverable and manageable using the source system's native tools.
*   Nucleus maintains a consistent metadata view across ingested and generated artifacts.

## 4. Core Operations

*   **Ingestion/Update:** When a new artifact is detected or an existing one changes in a source system:
    *   The corresponding Adapter notifies the Processing Pipeline.
    *   The Processing Pipeline updates the `ArtifactMetadata` record in Cosmos DB.
    *   The Processing Pipeline triggers the appropriate Personas for analysis.

*   **Persona Analysis:** Personas analyze the artifact content (fetched via Adapters) and generate `PersonaKnowledgeEntry` data.
    *   The `PersonaKnowledgeEntry` data is stored in Cosmos DB.
    *   The `ArtifactMetadata` record is updated with the analysis results.

*   **Query Handling:** When a user submits a query:
    *   The Query Handler retrieves relevant `ArtifactMetadata` and `PersonaKnowledgeEntry` records from Cosmos DB.
    *   The Query Handler uses the retrieved data to generate a response.

## 5. Integration with Other Architectures

*   **Processing Pipeline (`01_ARCHITECTURE_PROCESSING.md`):**
    *   Receives triggers (e.g., webhook from Teams) indicating a new/updated artifact in the source system.
    *   Creates or updates the `ArtifactMetadata` document in Cosmos DB.
    *   Uses platform adapters to fetch the artifact content stream from the `sourceUri` for analysis.
    *   Updates processing status fields in the `ArtifactMetadata` document.
*   **Database Layer (`04_ARCHITECTURE_DATABASE.md`):**
    *   Is the primary persistence layer for `ArtifactMetadata` and `PersonaKnowledgeEntry`.
    *   Leverages Cosmos DB features for indexing (including vector indexing on embeddings within `PersonaKnowledgeEntry`), querying, and scaling.
*   **Persona Layer (`02_ARCHITECTURE_PERSONAS.md`):**
    *   Receives artifact content streams (fetched by the pipeline) for analysis.
    *   Produces `PersonaKnowledgeEntry` data (including analysis and embeddings) stored in Cosmos DB.
    *   During query handling, retrieves relevant `PersonaKnowledgeEntry` documents from Cosmos DB. May use the associated `sourceIdentifier` to request the pipeline/adapter layer to fetch fresh snippets or verify information from the original artifact via its `sourceUri` if needed.
*   **Client/Adapter Layer (`05_ARCHITECTURE_CLIENTS.md`):**
    *   Provides the concrete implementations for interacting with source systems (Graph API, Slack API, etc.) to read/write artifacts using appropriate authentication and permissions.
    *   Handles triggers/webhooks from source systems.

## 6. Security Considerations

*   **Authentication & Authorization:** Nucleus adapters must securely authenticate with source systems (e.g., OAuth for Graph API, Slack Bot Tokens). Access to artifacts is governed by the permissions granted to the Nucleus application/bot identity *within the source system*.
*   **Data Minimization:** Nucleus only stores metadata in its database. The potentially sensitive content remains within the user's controlled environment.
*   **Cosmos DB Security:** Standard database security practices apply (network restrictions, access keys/RBAC, encryption at rest/transit).
*   **Secrets Management:** Securely manage API keys, OAuth client secrets, bot tokens needed by adapters (e.g., Azure Key Vault, Aspire configuration).
*   **PII/Sensitive Data:** While original content stays external, the metadata itself (`displayName`, `tags`, user IDs, context) might contain PII. Apply appropriate data handling policies.

## 7. Composite Artifact Handling

When a source artifact represents a collection (e.g., a folder, an email with multiple attachments), the `ArtifactMetadata` system uses the optional `composite...` fields in Cosmos DB to link them:

1.  A **parent** `ArtifactMetadata` document can be created for the container concept (e.g., the folder, the email itself), assigned a unique `compositeArtifactId`, and role `Parent`.
2.  **Child** `ArtifactMetadata` documents are created for each item within the container (files in folder, attachments), inheriting the same `compositeArtifactId`, linking back via `parentSourceIdentifier`, and assigned role `Child`.
3.  This allows personas, when analyzing a child artifact, to query Cosmos DB using the `compositeArtifactId` to retrieve metadata for related items, understanding the full context.

## 8. Next Steps

1.  **Implement `IArtifactMetadataRepository`:** Create a repository interface and Cosmos DB implementation for CRUD operations on `ArtifactMetadata` documents.
2.  **Finalize `sourceIdentifier` Strategy:** Define the precise algorithm for generating stable logical `sourceIdentifier`s for different `SourceSystemType`s, ensuring uniqueness and allowing correlation even if the `sourceUri` changes slightly (e.g., SharePoint version updates).
3.  **Integrate with Processing Pipeline:** Ensure the pipeline correctly interacts with the `IArtifactMetadataRepository` and uses platform adapters to fetch content via `sourceUri`.
4.  **Develop Platform Adapters:** Build out the necessary adapters in the Client Layer (`05_ARCHITECTURE_CLIENTS.md`) for key target source systems.
5.  **Refine Metadata Schema:** Continuously refine the `ArtifactMetadata` fields based on persona needs and extraction capabilities.

---

_This architecture clarifies that Nucleus leverages user source systems for artifact storage via adapters and uses its own database (Cosmos DB) solely for persisting rich metadata (`ArtifactMetadata`) and derived persona knowledge._
