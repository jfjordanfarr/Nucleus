---
title: "ARCHIVED - Nucleus Teams Adapter: Graph File Fetcher (Reference Gathering) - Superseded by M365 Agent SDK & MCP Server"
description: "ARCHIVED: This document described reference gathering for Teams file attachments. This functionality is now superseded by M365 Agent SDK file handling and the Nucleus_FileAccess_McpServer (IArtifactProvider) architecture."
version: 1.3
date: 2025-05-26
parent: ../ARCHITECTURE_ADAPTERS_TEAMS.md
---

# ARCHIVED - Nucleus Teams Adapter: Graph File Fetcher (Reference Gathering)

**This document is ARCHIVED and SUPERSEDED by M365 Agent SDK file handling and `Nucleus_FileAccess_McpServer` (which implements `IArtifactProvider`) architecture.**

Key insights regarding Teams file attachment data interpretation for `ArtifactReference` creation should be migrated to:
*   Design notes for the **Nucleus M365 Persona Agent for Teams** (how it processes incoming `Activity.Attachments`).
*   Implementation details of the `GraphArtifactProvider` within the `Nucleus_FileAccess_McpServer`.

The `IPlatformAttachmentFetcher.cs` interface is deprecated.

## Original Overview (Now Obsolete)

**Note:** Under the API-First architecture, the responsibility for **fetching** file content using Graph has moved to the central `Nucleus.Services.Api`. Direct file fetching by client adapters like the Teams Adapter is **deprecated**.

This document now describes the *modified* role of the component within the Teams Adapter, which is focused on **gathering attachment references** (like Drive IDs, Item IDs, or potentially temporary download URLs) from Teams platform events. These references are then passed to the `Nucleus.Services.Api` via the `InteractionRequest` DTO (see [API Client Interaction Pattern](../../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)) for the actual content retrieval by the service.

## Original Core Functionality (Now Obsolete)

-   **Implements:** (Potentially a refactored interface like `IPlatformAttachmentReferenceProvider` - TBD, or logic integrated directly into main adapter flow)
-   **Purpose:** To extract necessary identifiers (Drive ID, Item ID, potentially download URL) for file attachments referenced in Teams messages.
-   **Mechanism:** Leverages the `GraphClientService` *within the adapter's context* primarily to query metadata or obtain references/download URLs for attachments, **not** to download the full content stream.
-   **Identifiers:** 
    *   The adapter extracts these identifiers (e.g., from the `Activity.Attachments` data provided by the Bot Framework).
    *   These identifiers are packaged into the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.

## Original API Service Responsibility (Now Handled by MCP Server)

*   The `Nucleus.Services.Api` receives the `InteractionRequest` containing the attachment references.
*   The API Service uses its *own* `GraphClientService` instance (with appropriate service-level permissions and authentication) to fetch the actual file content based on the provided references.

## Original Dependencies (Context Shifted)

-   **`GraphClientService`:** Provides the authenticated Microsoft Graph client instance needed *within the adapter* if temporary download URLs or specific metadata needs resolving before creating the `ArtifactReference`. Found in [`GraphClientService.cs`](../../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs).
-   **`Nucleus.Abstractions`:** Defines models like [`ArtifactReference`](../../../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) which is used to package the references for the API.
-   **Microsoft Graph API:** The underlying service used to access Teams file data.

*Self-Note: The interface [`IPlatformAttachmentFetcher`](../../../../src/Nucleus.Abstractions/Adapters/IPlatformAttachmentFetcher.cs) defines the contract for actually **fetching content**. This is implemented and used by the **API Service**, not the Teams Adapter itself.*

## Original Implementation Details (Context Shifted)

-   **Error Handling:** Includes checks for valid attachment references and handles exceptions during Graph API calls *for metadata/reference retrieval*.
-   **Metadata:** Attempts to return the correct filename and content type, prioritizing information returned by Graph over hints provided in the `PlatformAttachmentReference`.
    *   This metadata is included in the `InteractionRequest` sent to the API service.

## Original Code Links (Context Shifted)

-   **Reference Gathering Implementation:** Logic integrated into [`TeamsAdapterBot.cs`](../../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs), specifically within the `ExtractAttachmentReferences` static method.
-   **Core DTO:** Uses [`ArtifactReference`](../../../../src/Nucleus.Abstractions/Models/ArtifactReference.cs).

## Original Related Documents

-   [ARCHITECTURE_ADAPTERS_TEAMS.md](../ARCHITECTURE_ADAPTERS_TEAMS.md) (Parent Overview)
-   [ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md](./ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md) (Sibling Interfaces)
