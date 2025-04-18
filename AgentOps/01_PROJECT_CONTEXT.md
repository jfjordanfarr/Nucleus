---
title: AgentOps - Nucleus Project Context
description: High-level project context including vision, goals, tech stack, and links to detailed architecture.
version: 3.1 # Refactored based on DRY pass
date: 2025-04-18
---

# Nucleus: Project Context (.NET 9 / Aspire / Azure Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the Nucleus project using .NET 9, Aspire, and Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/Requirements/00_PROJECT_MANDATE.md`) for motivation. **The primary source for agent behavior and tool usage guidelines is `.windsurfrules` in the project root.**

**Version:** 3.1
**Date:** 2025-04-18

## Vision & Goal

Build the Nucleus infrastructure for knowledge work enhanced by contextual AI Personas, integrated into users' existing workflows. See the [Project Mandate](../../Docs/Requirements/00_PROJECT_MANDATE.md) and the root [README.md](../../README.md) for the full vision. The initial goal focuses on the core backend API and services using .NET Aspire; see [Phase 1 Requirements](../../Docs/Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md).

## Key Technologies

*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** Nucleus (.NET Solution managed by DotNet Aspire)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Cosmos DB** - Stores `ArtifactMetadata` objects alongside knowledge entries (potentially separate container).
*   **Key Azure Services:** Cosmos DB, **Azure OpenAI Service / Google Gemini AI**, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider:** Google Gemini AI (Primary, integrated via `Mscc.GenerativeAI`). See [AI Integration Architecture](../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md) for details. Azure OpenAI Service (Secondary/Future).
*   **Platform Integration (Phase 2+):** Microsoft Bot Framework SDK / Graph API (Teams), Slack Bolt/API, Discord.NET/API, Email Processing (e.g., MailKit/MimeKit).
*   **Development:** Git, VS Code / Windsurf, .NET SDK 9.x, NuGet, **DotNet Aspire** (9.2+), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** `Mscc.GenerativeAI.IGenerativeAI` (for Gemini interaction), `Microsoft.Extensions.AI` (Potential future use).
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot

The system uses .NET Aspire for orchestration and includes core components like `Nucleus.ApiService`, `Nucleus.Personas.*`, `Nucleus.Processing.*`, and `Nucleus.Infrastructure`. See the [System Architecture Overview](../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md) for component diagrams and relationships.

*   **Ephemeral Processing Model:** Nucleus processes data transiently within a session scope, avoiding persistent storage of intermediate or generated content. See the [Ephemeral Processing principle in .windsurfrules](../.windsurfrules) for details.
*   **Target Deployment:** Initial target is **Azure Container Apps (ACA) as a 'Modular Monolith'**. See the [Deployment Architecture Overview](../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md) for details and alternatives.

## Data Flow (Typical API Request)

The typical data flow involves API requests triggering persona logic, interaction with AI services, and retrieval/storage of metadata in Cosmos DB. See the [Processing Architecture](../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md) for a detailed flow diagram.

## Deployment Model (Target)

The primary target deployment uses Azure Container Apps and supporting Azure services. See the [Deployment Architecture Overview](../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md) for details.

## Non-Goals (Explicit Initial Focus)

*   Building complex Web UIs (Blazor, etc.) initially.
*   Implementing production-ready platform adapters (Email, Teams, etc.) initially.
*   Implementing complex cross-persona orchestration workflows initially.

## Key Links & References (Planned & Existing)

*   **Agent Rules:** `.windsurfrules` (Project Root)
*   **Project Mandate:** `/docs/Requirements/00_PROJECT_MANDATE.md`
*   **Phase 1 Tasks:** `/docs/Planning/01_PHASE1_MVP_TASKS.md`
*   **Architecture Docs:** `/docs/Architecture/` (Refer relevant sections, noting P1 scope)
*   **Core Abstractions:** `/src/Nucleus.Abstractions/`
*   **Core Models:** `/src/Nucleus.Core/Models/`
*   **Infrastructure:** `/src/Nucleus.Infrastructure/`
*   **Personas:** `/src/Nucleus.Personas/`

## Current Project Structure Overview (Aspire-based)

See the [System Architecture Overview](../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md#5-high-level-codebase-structure-conceptual) for the conceptual codebase structure.

```
Nucleus.sln
├── AgentOps/                  # Documentation for AI agent collaboration
├── Docs/                      # Project documentation (requirements, architecture, etc.)
├── {Individual C# Project Directories (i.e. AppHost, ServiceDefaults, ApiService, etc.)}
├── .gitignore
└── README.md
```

## Current High-Level Goals

*   Establish robust core service structure within the Aspire framework.
*   Implement reliable Cosmos DB persistence for `ArtifactMetadata` and `PersonaKnowledgeEntry`.
*   Integrate Google Gemini for core AI capabilities within Personas.
*   Develop initial Persona logic (e.g., Bootstrapper, potentially EduFlow basics).
*   Ensure comprehensive test coverage for core components.