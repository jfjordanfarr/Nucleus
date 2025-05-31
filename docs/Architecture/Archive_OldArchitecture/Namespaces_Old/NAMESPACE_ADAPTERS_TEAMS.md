<!-- 
**THIS DOCUMENT IS ARCHIVED**

This document is no longer actively maintained and is preserved for historical context only. 
Refer to the main architectural documents for current information.
-->

<!-- filepath: /workspaces/Nucleus/Docs/Architecture/DevelopmentLifecycle/Namespaces/NAMESPACE_ADAPTERS_TEAMS.md -->
---
title: Namespace - Nucleus.Adapters.Teams
description: Describes the Teams Adapter project, providing a Microsoft Teams bot interface for interacting with the Nucleus API.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Adapters.Teams

**Relative Path:** `src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj`

## 1. Purpose

This project implements a Microsoft Teams bot that serves as a client adapter for the Nucleus system. It handles incoming messages and events from Teams, interacts with the Microsoft Graph API (e.g., for file access via OneDrive/SharePoint), translates user interactions into `AdapterRequest` objects, calls the `Nucleus.Services.Api`, and sends responses back to the Teams user.

## 2. Key Components

*   **`TeamsBot` / `ActivityHandler`:** The core bot logic class inheriting from the Bot Framework SDK's `ActivityHandler` to process incoming Teams activities (messages, mentions, file consents).
*   **API Client Logic:** Code to call the `Nucleus.Services.Api` endpoints.
*   **Graph Client Logic:** Code to interact with Microsoft Graph API for user/file context.
*   **State Management:** Logic to handle conversation state if needed.
*   **Authentication/Authorization:** Setup for Teams app authentication.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References shared DTOs like `AdapterRequest`, `ArtifactReference`).
*   Microsoft Bot Framework SDK (`Microsoft.Bot.Builder`, `Microsoft.Bot.Builder.Integration.AspNet.Core`).
*   Microsoft Graph SDK (`Microsoft.Graph`).
*   **Implicit Dependency:** Relies on the `Nucleus.Services.Api` being accessible via HTTP.

## 4. Dependents

*   This project is deployed as a service (e.g., Azure Bot Service) and is not a direct dependency for other projects in the solution.

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [05_ARCHITECTURE_CLIENTS.md](../05_ARCHITECTURE_CLIENTS.md)
*   [ARCHITECTURE_ADAPTERS_TEAMS.md](../ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md)
*   [10_ARCHITECTURE_API.md](../10_ARCHITECTURE_API.md)
