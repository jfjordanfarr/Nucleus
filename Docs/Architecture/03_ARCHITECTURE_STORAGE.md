# Nucleus OmniRAG: Storage Architecture

**Version:** 1.5
**Date:** 2025-04-08

This document outlines the architecture for storing original source **artifacts** and their primary metadata within the Nucleus OmniRAG system. It focuses on using a structured **metadata object (`ArtifactMetadata`)** within the chosen storage backend (e.g., Azure Blob Storage, local file system) to track source information, decoupling it from the persona-specific analysis stored in the Database Layer (Cosmos DB).

## 1. Core Principles

*   **Preserve Original Source:** The primary goal is to retain the original source artifact (or a stable reference to it, like a URL) exactly as provided by the user or system.
*   **Metadata Co-location:** Essential metadata about the source artifact and its processing status should be easily discoverable and associated with the source reference.
*   **Separation from Database:** The storage layer holds the *raw* or *original* content and its primary metadata (`ArtifactMetadata`), while the Database Architecture (`04_ARCHITECTURE_DATABASE.md`) focuses on *indexed, analyzed data* (persona knowledge entries).
*   **Support Diverse Sources:** Accommodate artifacts from various locations (local uploads, cloud drives, web URLs).

## 2. Storage Mechanisms

Depending on the hosting model (Cloud-Hosted SaaS vs. Self-Hosted), different mechanisms might be used:

*   **Cloud-Hosted (Primary): Azure Blob Storage:**
    *   Highly scalable, durable, and cost-effective object storage.
    *   Ideal for storing uploaded files and associated **`ArtifactMetadata`**.
    *   Supports features like lifecycle management, access tiers, and secure access control (SAS tokens, Managed Identity).
*   **Self-Hosted (Alternative): Local Filesystem / Network Share:**
    *   Simpler for on-premises deployments.
    *   Requires careful management of permissions, backups, and scalability.
*   **Web URLs:** For web sources, the `sourceUri` itself acts as the pointer. The storage layer's role is primarily to store the **`ArtifactMetadata`** associated with that URL.

## 3. Proposed Structure: The Artifact Metadata Approach

To manage metadata consistently across storage types, we define a core `ArtifactMetadata` object. This object is stored in a predictable location associated with the source artifact.

*   **Source Identification:** A unique, stable identifier (`sourceIdentifier`) is derived from the source. This could be:
    *   For Blobs: A unique ID incorporating container, path, and potentially version/hash (e.g., `blob://user-id/container/path/to/file.docx?versionId=xyz`).
    *   For Web URLs: A normalized, potentially hashed URL (e.g., `web://hash(https://www.example.com/page)`).
    *   For Local Files: A unique identifier incorporating machine and path.
    The exact generation strategy depends on the `SourceType`.
*   **Metadata Storage Location:** The `ArtifactMetadata` object is stored:
    *   In Azure Blob Storage: As blob metadata on the source blob itself, or potentially as a separate JSON file in a dedicated metadata container/path linked by the `sourceIdentifier` (e.g., `.metadata/<hashed_sourceIdentifier>.json`). Storing as blob metadata might have size limits.
    *   On Filesystems: As a hidden file (e.g., `._artifactId.meta.json`) alongside the source, or in a centralized hidden directory (e.g., `.nucleus_metadata/<hashed_sourceIdentifier>.json`).
    The key is a reliable mechanism to retrieve the `ArtifactMetadata` given the `sourceIdentifier`.

### 3.1 C# Model: `ArtifactMetadata`

This C# record defines the structure for the primary metadata associated with each ingested artifact.

```csharp
/// <summary>
/// Represents the type of the source artifact.
/// </summary>
public enum SourceType
{
    Unknown = 0,
    AzureBlob = 1,
    LocalFile = 2,
    NetworkShare = 3,
    WebUrl = 4,
    OneDrive = 5,
    GoogleDrive = 6,
    SharePoint = 7
    // ... other types as needed
}

/// <summary>
/// Represents the overall processing state of the artifact.
/// </summary>
public enum ProcessingState
{
    Pending = 0,      // Waiting to be processed
    InProgress = 1,   // Actively being processed
    Completed = 2,    // All targeted personas finished successfully
    CompletedWithFailures = 3, // Some personas failed, but processing is done
    Failed = 4,       // Unrecoverable failure during processing
    Skipped = 5       // Processing was intentionally skipped
}

/// <summary>
/// Represents the processing state of a specific persona for this artifact.
/// </summary>
public enum PersonaProcessingState
{
    Pending = 0,    // Not yet attempted by this persona
    Processing = 1, // Currently being processed by this persona
    Processed = 2,  // Successfully processed, knowledge entry created
    Skipped = 3,    // Persona determined artifact not relevant/applicable
    Failed = 4      // Persona encountered an error processing this artifact
}

/// <summary>
/// Defines the role of an artifact within a composite group (e.g., files from an archive).
/// </summary>
public enum CompositeArtifactRole
{
    Standalone = 0, // Not part of a composite group
    Parent = 1,     // The original container artifact (e.g., the .zip file)
    Child = 2,      // A file extracted from a Parent
    Entrypoint = 3  // A specific file within a composite group designated as the primary (e.g., index.html)
}

/// <summary>
/// Core metadata record stored for each ingested source artifact.
/// This is the single source of truth for information *about* the artifact itself.
/// </summary>
public record ArtifactMetadata(
    // --- Core Identification & Linking ---
    string sourceIdentifier, // Unique, stable identifier derived from the source URI/path
    string sourceUri,        // The actual pointer/URI to the artifact (blob path, file path, URL)
    SourceType sourceType,   // Type of the source artifact
    string userId,           // Identifier of the user who ingested/owns this artifact

    // --- Basic File Info ---
    string displayName,      // User-friendly name for the artifact
    string? mimeType = null, // Detected MIME type
    long? sourceSizeBytes = null, // Size in bytes
    string? sourceHash = null,    // Optional hash (e.g., MD5, SHA256) of the original content

    // --- Timestamps ---
    DateTimeOffset timestampIngested,       // When the artifact was first ingested by the system
    DateTimeOffset timestampLastUpdated,    // When this metadata record was last updated
    DateTimeOffset? timestampSourceModified = null, // Optional: Last modified time of the original source

    // --- Processing Status ---
    ProcessingState overallProcessingState, // High-level status of processing for this artifact
    // Tracks the status for each *targeted* persona
    Dictionary<string, PersonaProcessingState> personaProcessingStatus,

    // --- Relationships & Context ---
    string? compositeArtifactId = null, // Optional: GUID identifying a logical group (e.g., unzipped archive)
    CompositeArtifactRole compositeArtifactRole = CompositeArtifactRole.Standalone,
    string? parentSourceIdentifier = null, // Optional: Link back to the parent artifact's sourceIdentifier
    Dictionary<string, string>? originatingContext = null, // Optional: Key-value pairs providing context (e.g., "chatSessionId": "xyz")
    List<string>? tags = null, // Optional: User or system-defined tags

    // --- Hosting/Tenant Info ---
    string? tenantId = null // Optional: For partitioning in self-hosted scenarios
);
```

### 3.2 Example `ArtifactMetadata` (JSON Representation)

This is how the `ArtifactMetadata` might look when serialized to JSON for storage.

```json
// Example: Stored as metadata/blob_mycontainer_user1_abc_mydoc.docx.json
{
  "sourceIdentifier": "blob://mycontainer/user1/abc/mydoc.docx?versionId=123",
  "sourceUri": "https://mystorage.blob.core.windows.net/mycontainer/user1/abc/mydoc.docx",
  "sourceType": "AzureBlob",
  "userId": "user-guid-123",
  "displayName": "My Important Document.docx",
  "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "sourceSizeBytes": 123456,
  "sourceHash": "d41d8cd98f00b204e9800998ecf8427e", // Example MD5
  "timestampIngested": "2025-04-08T10:00:00Z",
  "timestampLastUpdated": "2025-04-08T10:05:00Z",
  "timestampSourceModified": "2025-04-06T11:30:00Z",
  "overallProcessingState": "CompletedWithFailures",
  "personaProcessingStatus": {
    "EduFlow_v1": "Processed",
    "Health_v1": "Skipped",
    "PersonalAssistant_v1": "Failed"
  },
  "compositeArtifactId": null,
  "compositeArtifactRole": "Standalone",
  "parentSourceIdentifier": null,
  "originatingContext": {
    "uploadSource": "WebApp",
    "correlationId": "req-abc-123"
  },
  "tags": ["project-alpha", "research"],
  "tenantId": null
}
```

*   **(Optional) Other Artifacts:** The storage location *could* also store intermediate processing results if needed (e.g., extracted raw text), but the primary artifact is the `ArtifactMetadata` object itself.

## 4. Integration with Other Architectures

*   **Processing (`01_ARCHITECTURE_PROCESSING.md`):**
    *   The pipeline is responsible for creating/updating the `ArtifactMetadata` object in storage.
    *   It derives the `sourceIdentifier` and manages the `overallProcessingState` and `personaProcessingStatus` fields based on pipeline steps and persona results.
*   **Database (`04_ARCHITECTURE_DATABASE.md`):**
    *   The database (Cosmos DB `PersonaAnalysisContainer`s) stores the `sourceIdentifier` field.
    *   This `sourceIdentifier` acts as the foreign key linking a persona's analysis record back to the primary source metadata stored in the `ArtifactMetadata` object.
    *   Retrieving metadata like `displayName` requires fetching and parsing the `ArtifactMetadata` from storage using the `sourceIdentifier`.
*   **Personas (`02_ARCHITECTURE_PERSONAS.md`):**
    *   Personas primarily interact with their own Cosmos DB containers.
    *   If a persona needs full source metadata (beyond the `sourceIdentifier`), the application layer would fetch the `ArtifactMetadata` from storage.

## 5. Security Considerations

*   **Access Control:** Ensure appropriate permissions are set on the storage locations (Blob containers, filesystem directories) to prevent unauthorized access or modification of **`ArtifactMetadata`** objects and original files.
*   **Secrets Management:** Securely manage connection strings, SAS tokens, or keys required to access storage.
*   **PII/Sensitive Data:** The original files may contain sensitive data. Access to these should be strictly controlled. The **`ArtifactMetadata`** should avoid storing sensitive data itself, except for potentially the `displayName` or `tags` if user-provided.

## 6. Composite Artifact Handling

When a source results in multiple related files (e.g., unzipping an archive), the **`ArtifactMetadata`** system uses the optional `composite...` fields to link them:

1.  A **parent** `ArtifactMetadata` is created for the original container (e.g., the `.zip` file), assigned a unique `compositeArtifactId`, and role `Parent`.
2.  **Child** `ArtifactMetadata` objects are created for each extracted file, inheriting the same `compositeArtifactId`, linking back via `parentSourceIdentifier`, and assigned role `Child`.
3.  This allows the system or personas, when encountering a child artifact's metadata, to query for all related metadata using the `compositeArtifactId` to understand the full context.

## 7. Next Steps

1.  **Implement Metadata Service:** Create a service/repository (`IArtifactMetadataService`) responsible for reliably generating `sourceIdentifier`s and reading/writing/updating `ArtifactMetadata` objects in the chosen storage backend (initially Azure Blob Storage).
2.  **Finalize `sourceIdentifier` Strategy:** Define the precise algorithm for generating stable `sourceIdentifier`s for different `SourceType`s, ensuring uniqueness and idempotency.
3.  **Integrate with Processing Pipeline:** Ensure the pipeline correctly interacts with the `IArtifactMetadataService`.
4.  **Update Application Logic:** Ensure code retrieving persona knowledge from Cosmos DB can use the `sourceIdentifier` to fetch the full `ArtifactMetadata` from storage when needed.
5.  **Consider Metadata Storage Strategy:** Evaluate pros/cons of storing metadata alongside the blob vs. a separate metadata store, especially regarding performance and potential size limits.

---

_This architecture defines how original source artifacts and their primary metadata (`ArtifactMetadata`) are managed, relying on a structured object stored within the chosen storage backend._

### Benefits of the `ArtifactMetadata` System

*   **Identification:** The `sourceIdentifier` provides a stable, unique reference.
*   **Decoupling:** Keeps primary source metadata separate from persona-specific analysis in Cosmos DB.
*   **Rich Context:** Provides comprehensive information about the source, its origin, relationships, and processing status.
*   **Context for Multi-File Artifacts:** Enables understanding of composite artifacts.
*   **Context for Retrieval:** The `originatingContext` helps retrieval processes.
*   **Granular Processing Tracking:** Enables tracking per persona via `personaProcessingStatus`.
*   **Flexibility:** Supports various source types.

### Field Descriptions (Selected)

*   **`sourceIdentifier` vs `sourceUri`:** `sourceIdentifier` is the logical, stable key used *within* Nucleus OmniRAG. `sourceUri` is the actual, potentially transient, pointer to the artifact in the external storage.
*   **`overallProcessingState`:** Tracks the high-level status.
*   **`personaProcessingStatus`:** Tracks status per *targeted* persona.
*   **`composite...` fields:** Define relationships for multi-file artifacts.
*   **`originatingContext`:** Provides context on how the artifact was introduced.
*   **Timestamps:** Track ingestion, updates, and optionally original source modification.
