---
title: Client Adapter - Local
description: Describes the Nucleus Local Adapter library, designed for in-process, programmatic interaction with the Nucleus API services by other local components.
version: 1.3
date: 2025-05-07
parent: ../05_ARCHITECTURE_CLIENTS.md
grand_parent: ../00_ARCHITECTURE_OVERVIEW.md
see_also:
  - title: "Namespace: Nucleus.Infrastructure.Adapters.Local"
    link: "../Namespaces/NAMESPACE_ADAPTERS_LOCAL.md"
---

# Client Adapter: Local (`Nucleus.Infrastructure.Adapters.Local`)

## Overview

This document describes the `Nucleus.Infrastructure.Adapters.Local` class library. **Crucially, this adapter functions as an in-process client, enabling direct programmatic interaction with services within the `Nucleus.Services.Api` or other tightly coupled local components.**

Its primary purpose is to provide a clean, integrated way for the `Nucleus.Services.Api` itself (or other local services) to initiate interactions or operations that would typically flow through the standard adapter model, but without the overhead of network communication or separate process execution. This is useful for internal system tasks, scheduled jobs, or scenarios where a "local agent" needs to interact with the core system logic.

It translates internal requests or method calls into the appropriate DTOs (like `AdapterRequest`) if necessary and interacts directly with the relevant application or domain services. It **does not** function as a standalone executable or provide a user-facing interface. It is a library intended for consumption by other parts of the Nucleus system.

## Interaction Flow & Service Invocation

The Local Adapter facilitates direct, in-process communication:

1.  **Initiation:** An internal service or component within `Nucleus.Services.Api` (or a similar local host) decides to initiate an interaction.
2.  **Construct `AdapterRequest` (or similar DTO):** The calling code constructs an `AdapterRequest` DTO (or a more specialized DTO if appropriate for direct service calls).
    *   Sets `PlatformType` to `"Local"`.
    *   `ConversationId` and `UserId` might be system-generated or passed from the originating context.
    *   Populates `QueryText` or other relevant data.
    *   Constructs `ArtifactReference` objects for any local file paths or in-memory data identifiers.
3.  **Direct Service Invocation / In-Process Request:** The adapter's logic directly calls the appropriate service methods within the `Nucleus.Services.Api`'s application or domain layers, or sends an in-process request that mimics an external API call.
    *   This bypasses HTTP communication, directly utilizing the registered services via dependency injection.
4.  **Receive Response:** The adapter receives a response DTO (e.g., `AdapterResponse`) or direct return values from the invoked services.
    *   This response is then passed back to the initiating internal component.

## Generated Artifact Handling

The Local Adapter's interaction with artifacts depends on the specific use case:

*   **Sending Artifact References:** If interacting with services that expect `ArtifactReference` objects (e.g., for file-based data), the adapter will construct these references. The path resolution and content reading would still typically be handled by the core services (like `LocalFileArtifactProvider`) to maintain consistency.
*   **In-Memory Data:** For purely in-process operations, the adapter might handle or pass around in-memory data directly if the target services support this, potentially without creating formal `ArtifactReference` objects for temporary data.

## Implementation Details

The core logic will reside within the `Nucleus.Infrastructure.Adapters.Local` project:

*   **Service Registration:** An extension method (e.g., `AddLocalAdapterServices`) to register its services with the host's dependency injection container.
*   **Interaction Service(s):** Classes responsible for orchestrating the interaction with the core API services, preparing requests, and processing responses.
*   **DTOs:** Utilizes shared DTOs from `Nucleus.Abstractions` (`AdapterRequest`, `AdapterResponse`, etc.).

## Limitations

*   Purely an in-process library; not a standalone executable.
*   Relies on being hosted within another process (e.g., `Nucleus.Services.Api`).
*   Not designed for direct user interaction.
*   Interaction scope is limited to the services available within its host process.

## Local Adapter: Context Simulation Scheme

This document outlines the conventions and mechanisms for simulating local conversation contexts, persona-specific data, and associated file storage when interacting with the Nucleus system via the `LocalAdapter`. This scheme is primarily intended for development, testing (e.g., using PowerShell scripts), and scenarios where the `LocalAdapter` is used for in-process interactions.

The `LocalAdapter` itself does not directly manage or create physical directories or files. Instead, it relies on the information provided within the `AdapterRequest` DTO, which is then processed by the `IOrchestrationService` and subsequently by components like `IPersonaResolver`, `IPersonaRuntime`, and `IArtifactProvider` (specifically `LocalFileArtifactProvider` for file-based artifacts).

### 1. Key `AdapterRequest` Fields for Context

The following fields in the `AdapterRequest` DTO are crucial for defining the context:

*   `PlatformType`: Should be set to `"Local"` (or `PlatformType.Local.ToString()`).
*   `PersonaId` (Optional but Recommended for Testing): Specifies the target persona for the interaction. If provided, it helps in scoping data and selecting persona-specific configurations.
*   `ConversationId`: A string identifying a specific conversation or session. For local testing, this can be a descriptive name or a UUID.
*   `UserId`: A string identifying the user initiating the interaction within the local context.
*   `TenantId` (Optional): If multi-tenancy simulation is required, this field can be used. For simpler local testing, it might be a constant or omitted.
*   `Artifacts`: An `IEnumerable<ArtifactReference>` where each `ArtifactReference` can point to local files using `file:///` URIs in its `Uri` property.

### 2. Directory Structure Convention (Conceptual)

While not strictly enforced by the `LocalAdapter` itself, a recommended conceptual directory structure for organizing local test data and artifacts is as follows. This structure would primarily be utilized by the `LocalFileArtifactProvider` or similar mechanisms during testing.

```
<BaseLocalDataPath>/
├── <PersonaId_1>/
│   ├── conversations/
│   │   ├── <ConversationId_A>/
│   │   │   ├── files/           # Artifacts associated with this specific interaction/conversation
│   │   │   │   └── example_document.txt
│   │   │   └── interaction_log.json # Potential future use for local interaction audit
│   │   └── <ConversationId_B>/
│   │       └── ...
│   ├── knowledge/             # Persona-specific knowledge base files (if managed locally)
│   │   └── custom_instructions.md
│   └── persona_config.json    # Local override or definition for persona configuration (testing)
├── <PersonaId_2>/
│   └── ...
└── shared_artifacts/            # For artifacts not specific to a persona or conversation
    └── shared_template.docx
```

*   **`<BaseLocalDataPath>`**: A root directory on the local filesystem configured for Nucleus local data (e.g., `D:\NucleusTestData\`). This path would typically be a configuration setting for the `LocalFileArtifactProvider`.
*   **`<PersonaId>`**: A directory named after the `PersonaId` from the `AdapterRequest`. This allows for persona-specific data isolation.
*   **`conversations/<ConversationId>`**: Subdirectories for each unique `ConversationId`, allowing conversation-specific artifact storage.
*   **`files/`**: A subdirectory within a conversation to store files explicitly referenced in an `AdapterRequest` for that conversation.

**Note:** This structure is a *recommendation* for organizing test data. The `LocalFileArtifactProvider` would be configured with a base path and would resolve `file:///` URIs relative to that base or use absolute paths as provided.

#### Identifier-Based Path Construction and Data Isolation

The `AdapterRequest` DTO contains several key identifiers (`TenantId`, `PersonaId`, `ConversationId`, `UserId`) that are critical for routing, context, and data isolation. When the `LocalAdapter` is used for testing, particularly with the `LocalFileArtifactProvider`, these identifiers can inform how local file paths are constructed or interpreted for storing and retrieving data like conversation artifacts, logs, or persona-specific files.

*   **`BaseLocalDataPath`**:
    *   This is the foundational root directory on the local filesystem (e.g., `D:\NucleusTestData\`).
    *   It's typically a configuration setting supplied to the `LocalFileArtifactProvider`.
    *   All dynamically constructed paths for local file storage related to simulated interactions would be relative to this base path.

*   **`TenantId`**:
    *   If a `TenantId` is provided in the `AdapterRequest` and the `LocalFileArtifactProvider` (or similar mechanism) is designed to be tenant-aware for path generation, the `TenantId` should form the first level of hierarchy immediately under the `BaseLocalDataPath`.
    *   Example: `<BaseLocalDataPath>/<TenantId>/...`
    *   If `TenantId` is not provided, or if the local simulation setup does not require tenant-level segregation in the file system, this path segment would be omitted, and paths would start with the next relevant identifier (e.g., `PersonaId`).
    *   This allows for simulating multi-tenant data isolation in the local file structure.

*   **`PersonaId`**:
    *   The `PersonaId` is essential for isolating all data related to a specific persona.
    *   It typically forms the next path segment after any `TenantId` segment (or directly under `BaseLocalDataPath` if no `TenantId` segment is used).
    *   Example: `<BaseLocalDataPath>/[<TenantId>/]<PersonaId>/...`
    *   This segment would be the root for persona-specific subdirectories like `conversations/`, `knowledge/`, or `persona_config.json` as shown in the conceptual tree.

*   **`ConversationId`**:
    *   The `ConversationId` uniquely identifies an interaction stream or session.
    *   It's used to create a dedicated subdirectory for artifacts, logs, and other data specific to that conversation.
    *   Example path: `.../<PersonaId>/conversations/<ConversationId>/`
    *   This ensures that files related to different conversations, even for the same persona, are kept separate.

*   **`UserId`**:
    *   The `UserId` in `AdapterRequest` identifies the user initiating or involved in the interaction. It's crucial for auditing, attribution in logs (e.g., within an `interaction_log.json` file associated with a `ConversationId`), and potentially for applying user-specific logic or permissions within the application.
    *   However, for the purpose of structuring shared conversation artifacts in the local file system simulation, `UserId` is generally **not** used to create an additional subdirectory level *within* a specific `<ConversationId>` path. Artifacts belonging to a conversation are typically considered shared among participants of that conversation (scoped by `ConversationId`).
    *   If a persona required storage for user-specific files *outside* the context of a shared conversation (e.g., private user notes related to a persona), a different path scheme such as `.../<PersonaId>/users/<UserId>/private_data/` might be conceptualized. This is distinct from the shared conversation artifact storage.

This approach ensures that file-based data can be organized logically, reflecting the contextual identifiers, thereby facilitating proper data isolation and straightforward retrieval during local testing and development.

### 2.4. Scope Enforcement and Data Governance Testing

While the `LocalAdapter` itself primarily facilitates passing `AdapterRequest` objects to the `IOrchestrationService`, its design, in conjunction with the conceptual local file structure and `PersonaConfiguration`, is critical for enabling robust testing of data governance and scope enforcement. The actual enforcement logic resides in downstream services (e.g., `IActivationChecker`, `IPersonaRuntime`, `IArtifactProvider` implementations driven by persona configuration).

**Key Principles:**

1.  **Configuration-Driven Access:**
    *   A `PersonaConfiguration` (retrieved by `IPersonaConfigurationProvider`) will define the legitimate operational scopes for a persona. This can include:
        *   Allowed `TenantId`(s).
        *   Allowed `ConversationId`(s) or patterns (e.g., all conversations within a specific Teams channel if the persona represents a bot for that channel).
        *   Rules for accessing specific artifact types or paths.
    *   For example, a persona might be configured to operate only within `TenantId: "ContosoCorp"` and only for `ConversationId: "TeamChannel123"`.

2.  **`LocalAdapter` as a Test Conduit:**
    *   Test scripts utilizing the `LocalAdapter` can craft `AdapterRequest` objects with various combinations of `TenantId`, `PersonaId`, `ConversationId`, `UserId`, and `ArtifactReference` URIs.
    *   These requests simulate real-world scenarios, including attempts to access data or operate in contexts that should be outside the persona's configured scope.

3.  **Downstream Enforcement Verification:**
    *   **Activation Checks (`IActivationChecker`):** Tests can verify that a persona activation is denied if the `AdapterRequest`'s context (e.g., `TenantId`, `ConversationId`) does not match the persona's allowed scopes in its `PersonaConfiguration`.
    *   **Artifact Access (`IArtifactProvider` via `IPersonaRuntime`):** Tests can verify that attempts to read or write artifacts (e.g., using `file:///` URIs via `LocalFileArtifactProvider`) are denied if the target path or context is outside the persona's configured access rights. For instance, Persona A should not be able to access files in Persona B's designated local storage path, or files from a `ConversationId` it's not authorized for, even within the same `TenantId`.
    *   **Cross-Tenant Isolation:** Critical tests must prove that a persona configured for `TenantId: "TenantA"` cannot, under any circumstances, access data or resources associated with `TenantId: "TenantB"`.

4.  **Simulating Platform Primitives for Test Scenarios:**
    *   **Teams:** `TenantId` maps to M365 Tenant. `ConversationId` maps to a channel ID or chat ID. Tests can simulate a Teams bot persona being restricted to specific channels or tenants.
    *   **Slack:** `TenantId` could map to a Slack Workspace/Enterprise Grid Org ID. `ConversationId` maps to a channel ID or DM ID. Tests can simulate a Slack App persona with similar restrictions.
    *   **Discord:** `TenantId` could map to a Discord Server (Guild) ID. `ConversationId` maps to a channel ID. Tests can simulate a Discord bot persona.

By designing test cases that explicitly attempt to violate these configured boundaries and asserting that the system correctly prevents such violations, the `LocalAdapter` environment can provide strong assurances about the data governance capabilities of the Nucleus platform. The local file structure, as described, provides a tangible way to set up and verify these scoped interactions during testing.

### 3. Artifact Handling with `file:///` URIs

When submitting an `AdapterRequest` via the `LocalAdapter` with `ArtifactReference` objects:

1.  The `Uri` property of an `ArtifactReference` should be a valid absolute `file:///` path (e.g., `file:///D:/NucleusTestData/PersonaAlpha/conversations/conv123/files/input.txt`).
2.  The `LocalAdapter` passes these `ArtifactReference` objects to the `IOrchestrationService` as part of the `AdapterRequest`.
3.  When the `IPersonaRuntime` or other core services need to access the content of these artifacts, they will use an `IArtifactProvider`.
4.  The `LocalFileArtifactProvider`, if registered and appropriate for `file:///` URIs, will be responsible for:
    *   Resolving the `file:///` URI to an absolute file path.
    *   Reading the file content.
    *   Returning it as an `ArtifactContent` object.

**Example `ArtifactReference` in `AdapterRequest` (C# code for constructing the request):**

```csharp
var request = new AdapterRequest
{
    PlatformType = PlatformType.Local.ToString(),
    PersonaId = "TestPersona001",
    ConversationId = "MyPowerShellSession_123",
    UserId = "LocalTestUser",
    QueryText = "Analyze this document.",
    Artifacts = new List<ArtifactReference>
    {
        new ArtifactReference
        {
            ProviderId = NucleusConstants.ArtifactProviders.LocalFile, // Hint for LocalFileArtifactProvider
            Uri = "file:///C:/Path/To/Your/Test/Documents/sample_report.docx",
            OriginalFileName = "sample_report.docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        }
    }
};
```

### 4. Persona Configuration for Local Testing

For `LocalAdapter` testing, persona configurations can be managed via `CosmosDbPersonaConfigurationProvider` (if testing against a live/emulator DB) or potentially an `InMemoryPersonaConfigurationProvider` (if available and configured for pure unit/integration tests without DB dependency).

If a `persona_config.json` file is used within the conceptual directory structure (e.g., `<BaseLocalDataPath>/<PersonaId>/persona_config.json`), a specialized `IArtifactProvider` or a custom setup in test harnesses would be needed to load this configuration and make it available to the `IPersonaConfigurationProvider` being used.

### 5. Summary of Responsibilities

*   **Test Script / Calling Code:** Responsible for constructing the `AdapterRequest` with correct `PersonaId`, `ConversationId`, `UserId`, and fully qualified `file:///` URIs for artifacts.
*   **`LocalAdapter`:** Passes the `AdapterRequest` (containing `ArtifactReference` objects) to the `IOrchestrationService`. It **does not** directly invoke `IArtifactProvider` implementations for artifact resolution or content fetching; this responsibility lies with downstream core services (e.g., `IPersonaRuntime` and its collaborators) after orchestration.
*   **`IOrchestrationService` & Core Logic:** Orchestrates the processing flow, eventually leading to artifact processing by appropriate services.
*   **`LocalFileArtifactProvider` (and other `IArtifactProvider` implementations):** Resolves `file:///` URIs (or other URI schemes) and reads file content when requested by core services during the interaction processing lifecycle.

This scheme allows the `LocalAdapter` to remain a thin client, focusing on in-process request submission, while leveraging existing artifact provider mechanisms for data access in a way that is testable and simulates real-world interactions.
