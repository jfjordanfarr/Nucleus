---
title: "Nucleus Teams Adapter: Graph File Fetcher"
description: "Architecture details for the component responsible for fetching file attachments from Microsoft Teams using the Microsoft Graph API."
version: 1.0
date: 2025-04-19
---

## Overview

This document describes the architecture and functionality of the `TeamsGraphFileFetcher` component within the Nucleus Teams Adapter. Its primary responsibility is to implement the `IPlatformAttachmentFetcher` interface, enabling the download of file attachments shared within Microsoft Teams conversations.

## Core Functionality

-   **Implements:** `Nucleus.Abstractions.IPlatformAttachmentFetcher`
-   **Purpose:** To retrieve the content stream of file attachments referenced in Teams messages.
-   **Mechanism:** Leverages the `GraphClientService` to interact with the Microsoft Graph API. Specifically, it uses Graph endpoints related to accessing DriveItems within a specific Drive.
-   **Identifiers:** Requires both the `DriveId` and the `DriveItemId` (passed as the `PlatformSpecificId` in the `PlatformAttachmentReference`) to locate and download the correct file. The `DriveId` must be supplied within the `PlatformContext` of the attachment reference.

## Dependencies

-   **`GraphClientService`:** Provides the authenticated Microsoft Graph client instance and helper methods for API calls (e.g., `DownloadDriveItemContentStreamAsync`).
-   **`Nucleus.Abstractions`:** Defines the `IPlatformAttachmentFetcher` interface and related models like `PlatformAttachmentReference`.
-   **Microsoft Graph API:** The underlying service used to access Teams file data. Requires appropriate application permissions (e.g., `Files.Read.All`, `Sites.Read.All`).

## Implementation Details

-   **Error Handling:** Includes checks for valid input references, the presence of required context (`DriveId`), and handles exceptions during Graph API calls.
-   **Metadata:** Attempts to return the correct filename and content type, prioritizing information returned by Graph over hints provided in the `PlatformAttachmentReference`.

## Code Links

-   **Implementation:** [../../../src/Adapters/Nucleus.Adapters.Teams/TeamsGraphFileFetcher.cs](cci:7://file:///d:/Projects/Nucleus/src/Adapters/Nucleus.Adapters.Teams/TeamsGraphFileFetcher.cs)
-   **Interface:** [../../../src/Abstractions/Nucleus.Abstractions/IPlatformAttachmentFetcher.cs](cci:7://file:///d:/Projects/Nucleus/src/Abstractions/Nucleus.Abstractions/IPlatformAttachmentFetcher.cs)

## Related Documents

-   [ARCHITECTURE_ADAPTERS_TEAMS.md](../ARCHITECTURE_ADAPTERS_TEAMS.md) (Parent Overview)
-   [ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md](./ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md) (Sibling Interfaces)
