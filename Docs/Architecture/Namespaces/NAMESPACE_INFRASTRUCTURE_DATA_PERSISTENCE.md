---
title: Namespace - Nucleus.Infrastructure.Persistence
description: Describes the infrastructure project responsible for data persistence using Azure Cosmos DB.
version: 1.0
date: 2025-04-28
parent: ../11_ARCHITECTURE_NAMESPACES_FOLDERS.md
---

# Project: Nucleus.Infrastructure.Persistence

**Relative Path:** `src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Nucleus.Infrastructure.Data.Persistence.csproj`

## 1. Purpose

This project implements the data access logic for the Nucleus application, specifically interacting with the chosen database provider (Azure Cosmos DB). It belongs to the Infrastructure Layer and is responsible for translating domain or application requests into concrete database operations.

## 2. Key Components

*   **Repository Implementations:** Concrete implementations of repository interfaces defined in `Nucleus.Abstractions` (e.g., `CosmosArtifactMetadataRepository` implementing `IArtifactMetadataRepository`).
*   **Cosmos DB Client Setup:** Configuration and initialization of the `CosmosClient`.
*   **Data Models/Entities:** May contain internal data models specific to the database schema if they differ significantly from domain models (though often mapping occurs within the repository).
*   **Database Context:** (If using EF Core Cosmos Provider) DbContext setup.

## 3. Dependencies

*   `src/Nucleus.Abstractions/Nucleus.Abstractions.csproj` (References interfaces to implement and potentially shared DTOs/Models)
*   Azure Cosmos DB SDK (`Microsoft.Azure.Cosmos`)
*   Potentially `src/Nucleus.Application/` if it defines specific persistence interfaces.

## 4. Dependents

*   `src/Nucleus.Services/Nucleus.Services.Api/Nucleus.Services.Api.csproj` (Registers and injects repository implementations via DI).

## 5. Related Documents

*   [11_ARCHITECTURE_NAMESPACES_FOLDERS.md](../11_ARCHITECTURE_NAMESPACES_FOLDERS.md)
*   [04_ARCHITECTURE_DATABASE.md](../04_ARCHITECTURE_DATABASE.md)
*   [03_ARCHITECTURE_STORAGE.md](../03_ARCHITECTURE_STORAGE.md) (Related concepts of artifacts vs. metadata)
