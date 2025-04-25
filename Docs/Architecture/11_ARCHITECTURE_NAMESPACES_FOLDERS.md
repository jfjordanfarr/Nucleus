---
title: Architecture - Namespaces and Folder Structure
description: Defines the standard namespace and folder structure for the Nucleus project, following .NET Aspire conventions and Clean Architecture principles.
version: 1.1
date: 2025-04-25
parent: ./00_ARCHITECTURE_OVERVIEW.md
---

# Nucleus: Namespace and Folder Structure

## 1. Introduction

This document defines the standard organization for the Nucleus codebase. The goals are:

*   **Clarity & Consistency:** Provide a predictable structure that is easy to navigate and understand.
*   **Maintainability & Extensibility:** Facilitate adding new features and components without major structural changes.
*   **Minimize Path Hallucinations:** Adhere to established conventions to improve predictability for developers and AI assistants.
*   **Alignment with Best Practices:** Follow idiomatic .NET Aspire project layout and Clean Architecture layering principles.

## 2. Top-Level Structure

The project root follows the standard .NET Aspire convention:

```
./
├── Aspire/                   # Aspire orchestration and defaults
│   ├── Nucleus.AppHost/          # Aspire AppHost project
│   └── Nucleus.ServiceDefaults/  # Aspire ServiceDefaults project
├── Docs/                     # Project documentation
│   └── Architecture/
├── src/                      # Core application source code
│   ├── Nucleus.Abstractions/
│   ├── Nucleus.Domain/
│   ├── Nucleus.Application/
│   ├── Nucleus.Infrastructure/
│   └── Nucleus.Services/
├── tests/                    # Test projects
│   ├── Integration/          # Integration test projects
│   │   └── Nucleus.Services.Api.IntegrationTests/
│   └── (Unit/EndToEnd planned)
├── .gitignore
├── Nucleus.sln
└── README.md
```

*   **`Aspire/`**: Contains projects specific to .NET Aspire's development-time orchestration (`AppHost`) and shared runtime configurations (`ServiceDefaults`).
*   **`Docs/`**: Contains all project documentation, including architecture specifications.
*   **`src/`**: Contains all core source code for the Nucleus application, organized by architectural layer.
*   **`tests/`**: Contains all automated test projects. Currently focused on Integration tests, with Unit and End-to-End tests planned for the future.

## 3. `src/` Layer Structure & Responsibilities

The `src/` directory is organized according to Clean Architecture layers:

*   **`Nucleus.Abstractions/`**:
    *   **Purpose:** Defines core interfaces, DTOs (Data Transfer Objects), enums, exceptions, and base types shared across multiple layers. Contains no implementation logic.
    *   **Dependencies:** None (or minimal external framework abstractions like `Microsoft.Extensions.Logging.Abstractions`).
*   **`Nucleus.Domain/`**:
    *   **Purpose:** Contains the core business logic, entities, aggregates, value objects, domain events, and core behavioral definitions. Represents the heart of the application and should be independent of infrastructure concerns.
    *   **Dependencies:** `Nucleus.Abstractions`.
    *   **Sub-folders / Projects:** Organized logically, e.g.:
        *   `Nucleus.Domain.Processing/`: Core processing logic.
        *   `Personas/Nucleus.Personas.Core/`: Base implementations for Personas.
*   **`Nucleus.Application/`**:
    *   **Purpose:** Orchestrates use cases by coordinating Domain entities and Infrastructure services. Defines application-specific interfaces (e.g., `IOrchestrationService`) potentially implemented by Infrastructure. Contains application logic but not business rules (those are in Domain). **(Currently a placeholder directory).**
    *   **Dependencies:** `Nucleus.Abstractions`, `Nucleus.Domain`.
*   **`Nucleus.Infrastructure/`**:
    *   **Purpose:** Contains implementations for external concerns: data access, external service clients, message queue interactions, caching, and platform adapters. Implements interfaces defined in `Application` or `Abstractions`.
    *   **Dependencies:** `Nucleus.Abstractions`, `Nucleus.Application`. May also depend on `Nucleus.Services.Api` *specifically for Adapters* following the API-First principle.
    *   **Sub-folders / Projects:** Organized by concern, e.g.:
        *   `Adapters/Nucleus.Adapters.Console/`: Console client adapter.
        *   `Adapters/Nucleus.Adapters.Teams/`: Teams client adapter.
        *   `Data/Nucleus.Infrastructure.Persistence/`: Data persistence implementations (repositories).
*   **`Nucleus.Services/`**:
    *   **Purpose:** The entry point / hosting layer. Contains API projects (`Nucleus.Services.Api`), background worker services, etc. Responsible for exposing application functionality and handling hosting concerns (ASP.NET Core setup, DI configuration).
    *   **Dependencies:** `Nucleus.Abstractions`, `Nucleus.Application`, `Nucleus.Infrastructure`. (Note: `Nucleus.Services.Api` depends on `Nucleus.ServiceDefaults` located in `Aspire/`).

## 4. Naming Conventions

*   **Namespaces:** Follow the folder structure precisely. Example: `Nucleus.Infrastructure.Adapters.Teams`.
*   **Project Files:** `.csproj` files should match the primary namespace and folder name. Example: `src/Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj`.

## 5. Dependency Rules

*   **Direction:** Dependencies flow inwards: `Services` -> `Infrastructure` -> `Application` -> `Domain`. `Abstractions` can be referenced by any layer except `Domain` (which only references `Abstractions`).
*   **API-First:** Client Adapters located in `Infrastructure` explicitly depend on and call `Nucleus.Services.Api`. This is an intentional pattern.
*   **No Circular Dependencies:** Direct circular dependencies between layers (e.g., `Domain` depending on `Application`) are forbidden.

## 6. Dependency Graph

```mermaid
graph LR
    %% Top Level Structure
    subgraph Root
        APP_HOST[Aspire/Nucleus.AppHost]
        SVC_DEFAULTS[Aspire/Nucleus.ServiceDefaults]
        SRC[src/]
        TESTS[tests/]
    end

    %% Tests Layer
    subgraph tests/
        T_Integration[Integration/Nucleus.Services.Api.IntegrationTests]
        %% T_Unit(Unit)
        %% T_EndToEnd(EndToEnd)
    end

    %% src/ Layer Breakdown
    subgraph src/
        subgraph Services
            S_Api[Nucleus.Services.Api]
        end

        subgraph Infrastructure
            direction LR
            subgraph Data
                I_Persistence[Nucleus.Infrastructure.Persistence]
            end
            subgraph Adapters
                 I_Console[Nucleus.Adapters.Console]
                 I_Teams[Nucleus.Adapters.Teams]
            end
        end

        subgraph Application
            A[Nucleus.Application (Placeholder)]
        end

        subgraph Domain
             subgraph Processing
                 D_Processing[Nucleus.Domain.Processing]
             end
             subgraph Personas
                 D_Personas[Nucleus.Personas.Core]
             end
        end

        subgraph Abstractions
            Abs[Nucleus.Abstractions]
        end
    end

    %% Core Application Dependencies
    S_Api --> A;
    S_Api --> I_Persistence;
    S_Api --> I_Console; %% Via DI for potential shared services, not direct calls
    S_Api --> I_Teams;   %% Via DI for potential shared services, not direct calls
    S_Api --> D_Processing; %% OrchestrationService lives here
    S_Api --> D_Personas; %% PersonaManager lives here
    S_Api --> Abs;

    I_Persistence --> A; %% Implements App/Abstractions interfaces
    I_Persistence --> Abs;
    I_Console --> Abs;
    I_Teams --> Abs;

    A --> D_Processing;
    A --> D_Personas;
    A --> Abs;

    D_Processing --> Abs;
    D_Personas --> Abs;

    %% API-First Adapter Dependency (Specific to Adapters)
    I_Console -- Calls API --> S_Api;
    I_Teams -- Calls API --> S_Api;

    %% Aspire Dependencies
    APP_HOST --> S_Api; %% AppHost references the services it hosts
    APP_HOST --> SVC_DEFAULTS;
    S_Api --> SVC_DEFAULTS; %% Services use shared defaults

    %% Test Dependencies
    %% T_Unit --> A;
    %% T_Unit --> D_Processing;
    T_Integration --> S_Api;
    %% T_Integration --> I_Persistence;
    %% T_EndToEnd --> S_Api; %% Typically tests the API surface

    %% Style API First links
    linkStyle 13 stroke:red,stroke-width:2px,stroke-dasharray: 5 5;
    linkStyle 14 stroke:red,stroke-width:2px,stroke-dasharray: 5 5;

```

## 7. Related Documents

*   [00_ARCHITECTURE_OVERVIEW.md](./00_ARCHITECTURE_OVERVIEW.md)
*   [10_ARCHITECTURE_API.md](./10_ARCHITECTURE_API.md)
*   [05_ARCHITECTURE_CLIENTS.md](./05_ARCHITECTURE_CLIENTS.md)
