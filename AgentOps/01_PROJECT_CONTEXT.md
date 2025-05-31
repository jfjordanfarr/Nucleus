---
title: AgentOps - Nucleus Project Context
description: High-level project context including vision, goals, tech stack, and links to detailed architecture.
version: 3.2
date: 2025-05-28
---

# Nucleus: Project Context (Microsoft 365 Agents SDK / MCP / .NET 9 / Aspire / Azure Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the Nucleus project, now centered on the **Microsoft 365 Agents SDK and Model Context Protocol (MCP)**, and utilizing .NET 9, .NET Aspire, and Azure Cosmos DB. Refer to `../Docs/` for full details and the Project Mandate (`../Docs/ProjectExecutionPlan/00_REQUIREMENTS_PROJECT_MANDATE.md`) for motivation. **The primary source for agent behavior and tool usage guidelines is `.github/copilot-instructions.md` in the project root.**

## Vision & Goal

Build the Nucleus infrastructure for knowledge work enhanced by contextual AI Personas, integrated into users' existing workflows. See the [Project Mandate](../Docs/ProjectExecutionPlan/00_REQUIREMENTS_PROJECT_MANDATE.md) and the root [README.md](../README.md) for the full vision. The initial goal focuses on establishing foundational **Nucleus M365 Persona Agent applications and backend Nucleus MCP Tool/Server applications** using .NET Aspire; see [Phase 1 Requirements](../Docs/ProjectExecutionPlan/01_REQUIREMENTS_PHASE1_MVP_FOUNDATION.md).

## Key Technologies

*   **Microsoft 365 Agents SDK:** Core framework for building Nucleus M365 Persona Agent applications.
*   **Model Context Protocol (MCP):** Standard for interaction between M365 Agents and backend Nucleus MCP Tool/Server applications.
*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** .NET Aspire (Managing the Nucleus Solution)
*   **Cloud Platform:** Microsoft Azure (Primary target for hosting)
*   **Primary Backend (Knowledge Store):** **Azure Cosmos DB (NoSQL API w/ Integrated Vector Search)** - Stores `PersonaKnowledgeEntry` documents.
*   **Primary Backend (Metadata Store):** **Azure Cosmos DB** - Stores `ArtifactMetadata` objects alongside knowledge entries (potentially separate container).
*   **Key Azure Services:** Cosmos DB, Service Bus, Functions (v4+ Isolated Worker - for later phases), Key Vault.
*   **AI Provider Strategy:** Multi-provider support (Azure OpenAI, Google Gemini, OpenRouter.AI) via **`Microsoft.Extensions.AI`** abstractions (`IChatClient`, `ITextEmbeddingGenerator`). See [AI Integration Strategy](../Docs/Architecture/CoreNucleus/05_AI_INTEGRATION_STRATEGY.md) for details.
*   **Platform Integration (M365 Agents):** **Microsoft 365 Agents SDK** / Microsoft Graph API (Teams, M365 Copilot, etc.)...
*   **Development:** Git, VS Code / Windsurf, .NET SDK 9.x, NuGet, **DotNet Aspire** (9.2+), xUnit, Moq/NSubstitute, TDD focus.
*   **AI Abstractions:** **`Microsoft.Extensions.AI`** (`IChatClient`, `ITextEmbeddingGenerator`) is the primary abstraction for LLM interaction.
*   **Infrastructure-as-Code (Optional/Later):** Bicep / Terraform.

## Architecture Snapshot

The system is a distributed system of **Nucleus M365 Persona Agent applications** and backend **Nucleus MCP Tool/Server applications**, orchestrated by .NET Aspire. See the [System Architecture Overview](../Docs/Architecture/NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md) (and the [Executive Summary](../Docs/Architecture/CoreNucleus/00_SYSTEM_EXECUTIVE_SUMMARY.md)) for component diagrams and relationships.

*   **Ephemeral Processing Model:** Nucleus processes data transiently within a session scope, avoiding persistent storage of intermediate or generated content. See the [Ephemeral Processing principle in .github/copilot-instructions.md](../.github/copilot-instructions.md) for details.
*   **Target Deployment:** Primarily **Azure Container Apps (ACA)** for the distributed M365 Agents and MCP Tools. See the [Deployment Overview](../Docs/Architecture/Deployment/01_DEPLOYMENT_OVERVIEW.md) for details.

## Data Flow (Typical M365 Agent Interaction)

The typical data flow involves M365 Agent interactions triggering calls to backend MCP Tools and persona logic execution within the agent. See the [Processing Orchestration Overview](../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md) or [Ephemeral Processing Pipeline](../Docs/Architecture/CoreNucleus/04_EPHEMERAL_PROCESSING_PIPELINE.md) for a detailed flow diagram.

## Deployment Model (Target)

The primary target deployment uses Azure Container Apps and supporting Azure services. See the [Deployment Overview](../Docs/Architecture/Deployment/01_DEPLOYMENT_OVERVIEW.md) for details.

## Non-Goals (Explicit Initial Focus)

*   Building complex Web UIs (Blazor, etc.) initially.

## Current Project Structure Overview (Aspire-based)

See the [Namespace and Folder Structure](../Docs/Architecture/DevelopmentLifecycle/01_NAMESPACES_FOLDERS.md) for the conceptual codebase structure.
