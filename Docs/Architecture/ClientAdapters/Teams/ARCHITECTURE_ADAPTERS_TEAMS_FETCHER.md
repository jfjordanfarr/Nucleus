---
title: "Nucleus Teams Adapter: Graph File Fetcher (Reference Gathering)"
description: "Architecture details for the component responsible for gathering file attachment references from Microsoft Teams for API processing."
version: 1.2
date: 2025-04-27
parent: ../ARCHITECTURE_ADAPTERS_TEAMS.md
---

## Overview

**Note:** Under the API-First architecture, the responsibility for **fetching** file content using Graph has moved to the central `Nucleus.Services.Api`. Direct file fetching by client adapters like the Teams Adapter is **deprecated**.

This document now describes the *modified* role of the component within the Teams Adapter, which is focused on **gathering attachment references** (like Drive IDs, Item IDs, or potentially temporary download URLs) from Teams platform events. These references are then passed to the `Nucleus.Services.Api` via the `InteractionRequest` DTO (see [API Client Interaction Pattern](../Api/ARCHITECTURE_API_CLIENT_INTERACTION.md)) for the actual content retrieval by the service.

## Core Functionality

-   **Implements:** (Potentially a refactored interface like `IPlatformAttachmentReferenceProvider` - TBD, or logic integrated directly into main adapter flow)
-   **Purpose:** To extract necessary identifiers (Drive ID, Item ID, potentially download URL) for file attachments referenced in Teams messages.
-   **Mechanism:** Leverages the `GraphClientService` *within the adapter's context* primarily to query metadata or obtain references/download URLs for attachments, **not** to download the full content stream.
-   **Identifiers:** 
    *   The adapter extracts these identifiers (e.g., from the `Activity.Attachments` data provided by the Bot Framework).
    *   These identifiers are packaged into the `InteractionRequest` DTO sent to the `Nucleus.Services.Api`.

## API Service Responsibility

*   The `Nucleus.Services.Api` receives the `InteractionRequest` containing the attachment references.
*   The API Service uses its *own* `GraphClientService` instance (with appropriate service-level permissions and authentication) to fetch the actual file content based on the provided references.

## Dependencies

-   **`GraphClientService`:** Provides the authenticated Microsoft Graph client instance needed *within the adapter* if temporary download URLs or specific metadata needs resolving before creating the `ArtifactReference`. Found in [`GraphClientService.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/GraphClientService.cs).
-   **`Nucleus.Abstractions`:** Defines models like [`ArtifactReference`](../../../src/Nucleus.Abstractions/Models/ArtifactReference.cs) which is used to package the references for the API.
-   **Microsoft Graph API:** The underlying service used to access Teams file data.

*Self-Note: The interface [`IPlatformAttachmentFetcher`](../../../../src/Nucleus.Abstractions/Adapters/IPlatformAttachmentFetcher.cs) defines the contract for actually **fetching content**. This is implemented and used by the **API Service**, not the Teams Adapter itself.*

## Implementation Details

-   **Error Handling:** Includes checks for valid attachment references and handles exceptions during Graph API calls *for metadata/reference retrieval*.
-   **Metadata:** Attempts to return the correct filename and content type, prioritizing information returned by Graph over hints provided in the `PlatformAttachmentReference`.
    *   This metadata is included in the `InteractionRequest` sent to the API service.

## Code Links

-   **Reference Gathering Implementation:** Logic integrated into [`TeamsAdapterBot.cs`](../../../src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/TeamsAdapterBot.cs), specifically within the `ExtractAttachmentReferences` static method.
-   **Core DTO:** Uses [`ArtifactReference`](../../../src/Nucleus.Abstractions/Models/ArtifactReference.cs).

## Related Documents

-   [ARCHITECTURE_ADAPTERS_TEAMS.md](../ARCHITECTURE_ADAPTERS_TEAMS.md) (Parent Overview)
-   [ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md](./ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md) (Sibling Interfaces)
