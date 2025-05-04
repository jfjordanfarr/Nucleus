---
title: Architecture - Namespaces and Folder Structure
description: Defines the standard namespace and folder structure for the Nucleus project, following .NET Aspire conventions and Clean Architecture principles.
version: 2.2
date: 2025-05-03
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

## 3. `src/` Layer Structure

The `src/` directory is organized according to Clean Architecture principles, generally flowing from `Abstractions` -> `Domain` -> `Application` -> `Infrastructure` / `Services`. Detailed responsibilities for each project are documented in the specific files linked in the [Project Breakdown](#4-project-breakdown) section below.

## 4. Project Breakdown

This section lists the individual projects within the Nucleus solution and links to their detailed architecture documents.

*   **[`Nucleus.Abstractions`](./Namespaces/NAMESPACE_ABSTRACTIONS.md)** (`src/Nucleus.Abstractions/`)
    *   Defines core interfaces, DTOs, and base types shared across the application.
*   **[`Nucleus.Domain.Processing`](./Namespaces/NAMESPACE_DOMAIN_PROCESSING.md)** (`src/Nucleus.Domain/Nucleus.Domain.Processing/`)
    *   Core domain logic for interaction processing and orchestration.
*   **[`Nucleus.Personas.Core`](./Namespaces/NAMESPACE_PERSONAS_CORE.md)** (`src/Nucleus.Domain/Personas/Nucleus.Personas.Core/`)
    *   Core domain logic for Personas, including the **Persona Runtime engine** responsible for executing configurations and agentic strategies.
*   **(Placeholder for `Nucleus.Application`)** *(Currently empty)*
*   **[`Nucleus.Infrastructure.Data.Persistence`](./Namespaces/NAMESPACE_INFRASTRUCTURE_DATA_PERSISTENCE.md)** (`src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/`)
    *   Data persistence implementation (Cosmos DB Repositories).
*   **[`Nucleus.Infrastructure.Providers`](./Namespaces/NAMESPACE_INFRASTRUCTURE_PROVIDERS.md)** (`src/Nucleus.Infrastructure/Providers/`)
    *   Implementations for accessing external data/resources (e.g., Artifact Providers).
*   **[`Nucleus.Adapters.Console`](./Namespaces/NAMESPACE_ADAPTERS_CONSOLE.md)** (`src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Console/`)
    *   Console client adapter.
*   **[`Nucleus.Adapters.Teams`](./Namespaces/NAMESPACE_ADAPTERS_TEAMS.md)** (`src/Nucleus.Infrastructure/Adapters/Nucleus.Adapters.Teams/`)
    *   Microsoft Teams client adapter (Bot).
*   **[`Nucleus.Services.Api`](./Namespaces/NAMESPACE_SERVICES_API.md)** (`src/Nucleus.Services/Nucleus.Services.Api/`)
    *   Main backend HTTP API service.
*   **[`Nucleus.AppHost`](./Namespaces/NAMESPACE_APP_HOST.md)** (`Aspire/Nucleus.AppHost/`)
    *   .NET Aspire AppHost for development orchestration.
*   **[`Nucleus.ServiceDefaults`](./Namespaces/NAMESPACE_SERVICE_DEFAULTS.md)** (`Aspire/Nucleus.ServiceDefaults/`)
    *   Shared configurations (Telemetry, Health Checks) for Aspire services.
*   **(Placeholder for `Nucleus.Services.Api.IntegrationTests`)** (`tests/Integration/Nucleus.Services.Api.IntegrationTests/`)

## 5. Naming Conventions

*   **Namespaces:** Follow the folder structure precisely. Example: `Nucleus.Infrastructure.Adapters.Teams`.
*   **Project Files:** `.csproj` files should match the primary namespace and folder name. Example: `src/Infrastructure/Adapters/Nucleus.Adapters.Teams/Nucleus.Infrastructure.Adapters.Teams.csproj`.

## 6. Dependency Rules

*   **Direction:** Dependencies flow inwards: `Services` -> `Infrastructure` -> `Application` -> `Domain`. `Abstractions` can be referenced by any layer except `Domain` (which only references `Abstractions`).
*   **API-First:** Client Adapters located in `Infrastructure` explicitly depend on and call `Nucleus.Services.Api`. This is an intentional pattern.
*   **No Circular Dependencies:** Direct circular dependencies between layers (e.g., `Domain` depending on `Application`) are forbidden.

## 7. Dependency Graph

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
                I_Data_Persistence[Nucleus.Infrastructure.Data.Persistence]
            end
            subgraph Providers
                 I_Providers[Nucleus.Infrastructure.Providers]
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
    S_Api --> I_Data_Persistence;
    S_Api --> I_Providers;
    S_Api --> I_Console; %% Via DI for potential shared services, not direct calls
    S_Api --> I_Teams;   %% Via DI for potential shared services, not direct calls
    S_Api --> D_Processing; %% OrchestrationService lives here
    S_Api --> D_Personas; %% PersonaManager lives here
    S_Api --> Abs;

    I_Data_Persistence --> A; %% Implements App/Abstractions interfaces
    I_Data_Persistence --> Abs;
    I_Providers --> Abs;
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
    %% T_Integration --> I_Data_Persistence;
    %% T_EndToEnd --> S_Api; %% Typically tests the API surface

    %% Style API First links
    linkStyle 13 stroke:red,stroke-width:2px,stroke-dasharray: 5 5;
    linkStyle 14 stroke:red,stroke-width:2px,stroke-dasharray: 5 5;

```

## 8. Testing (`tests/`)

*   **`tests/Integration/`**: Contains integration test projects, currently focused on API integration tests (`Nucleus.Services.Api.IntegrationTests`). See [NAMESPACE_API_INTEGRATION_TESTS.md](./Namespaces/NAMESPACE_API_INTEGRATION_TESTS.md) (Placeholder).
*   **`tests/Unit/`**: Planned for unit tests, currently not implemented.
*   **`tests/EndToEnd/`**: Planned for end-to-end tests, currently not implemented.
*   **`Nucleus.Services.Api.IntegrationTests`**: Integration tests specifically targeting the `Nucleus.Services.Api` project, often involving spinning up the web host and making real HTTP requests.
    *   *Details*: [./11_ARCHITECTURE_NAMESPACES_FOLDERS/TESTS_API_INTEGRATION.md](./11_ARCHITECTURE_NAMESPACES_FOLDERS/TESTS_API_INTEGRATION.md)
*   **`Nucleus.Infrastructure.Testing`**: Contains shared test doubles (mocks, fakes, stubs) and helper classes used by various test projects. This ensures test infrastructure is separate from production code.
    *   *Details*: [./12_NAMESPACE_INFRASTRUCTURE_TESTING.md](./12_NAMESPACE_INFRASTRUCTURE_TESTING.md)

## 9. Related Documents

*   [00_ARCHITECTURE_OVERVIEW.md](./00_ARCHITECTURE_OVERVIEW.md)
*   [10_ARCHITECTURE_API.md](./10_ARCHITECTURE_API.md)
*   [05_ARCHITECTURE_CLIENTS.md](./05_ARCHITECTURE_CLIENTS.md)
