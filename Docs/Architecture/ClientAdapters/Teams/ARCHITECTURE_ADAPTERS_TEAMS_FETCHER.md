---
title: "Nucleus Teams Adapter: Graph File Fetcher (Reference Gathering)"
description: "Architecture details for the component responsible for fetching file attachments from Microsoft Teams using the Microsoft Graph API."
version: 1.1
date: 2025-04-24
---

## Overview

**Note:** Under the API-First architecture ([Memory: 21ba96d2](cci:memory/21ba96d2-36ea-4a88-8b6b-ed0fb4d8dd07)), the responsibility for **fetching** file content using Graph has moved to the central `Nucleus.Services.Api`. Direct file fetching by client adapters like the Teams Adapter is **deprecated**.

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

-   **`GraphClientService`:** Provides the authenticated Microsoft Graph client instance and helper methods for API calls (e.g., `DownloadDriveItemContentStreamAsync`).
-   **`Nucleus.Abstractions`:** Defines the `IPlatformAttachmentFetcher` interface and related models like `PlatformAttachmentReference`. *(Note: `IPlatformAttachmentFetcher` may be deprecated or refactored in the adapter context)*.
-   **Microsoft Graph API:** The underlying service used to access Teams file data. Requires appropriate application permissions (e.g., `Files.Read.All`, `Sites.Read.All`).

## Implementation Details

-   **Error Handling:** Includes checks for valid attachment references and handles exceptions during Graph API calls *for metadata/reference retrieval*.
-   **Metadata:** Attempts to return the correct filename and content type, prioritizing information returned by Graph over hints provided in the `PlatformAttachmentReference`.
    *   This metadata is included in the `InteractionRequest` sent to the API service.

## Code Links

-   **Implementation:** [../../../src/Adapters/Nucleus.Adapters.Teams/TeamsGraphFileFetcher.cs](cci:7://file:///d:/Projects/Nucleus/src/Adapters/Nucleus.Adapters.Teams/TeamsGraphFileFetcher.cs) *(Requires refactoring to align with reference gathering role)*
-   **Interface:** [../../../src/Abstractions/Nucleus.Abstractions/IPlatformAttachmentFetcher.cs](cci:7://file:///d:/Projects/Nucleus/src/Abstractions/Nucleus.Abstractions/IPlatformAttachmentFetcher.cs)
    *(Note: This interface as implemented by adapters may be deprecated/refactored)*

## Related Documents

-   [ARCHITECTURE_ADAPTERS_TEAMS.md](../ARCHITECTURE_ADAPTERS_TEAMS.md) (Parent Overview)
-   [ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md](./ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md) (Sibling Interfaces)
