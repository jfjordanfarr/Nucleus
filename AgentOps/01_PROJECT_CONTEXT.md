---
title: AgentOps - Nucleus Project Context
description: High-level project context including vision, goals, tech stack, and links to detailed architecture.
version: 3.1 # Refactored based on DRY pass
date: 2025-04-18
---

# Nucleus: Project Context (.NET 9 / Aspire / Azure Cosmos DB Backend)

**Attention AI Assistant:** This document provides high-level context for the Nucleus project using .NET 9, Aspire, and Azure Cosmos DB. Refer to `/docs/` for full details and the Project Mandate (`/docs/Requirements/00_PROJECT_MANDATE.md`) for motivation. **The primary source for agent behavior and tool usage guidelines is `.windsurfrules` in the project root.**

## Vision & Goal

Build the Nucleus infrastructure for knowledge work enhanced by contextual AI Personas, integrated into users' existing workflows. See the [Project Mandate](../../Docs/Requirements/00_PROJECT_MANDATE.md) and the root [README.md](../../README.md) for the full vision. The initial goal focuses on the core backend API and services using .NET Aspire; see [Phase 1 Requirements](../../Docs/Requirements/01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md).

## Key Technologies

*   **Language:** C# (using .NET 9.0)
*   **Core Framework:** .NET Aspire (Managing the Nucleus Solution)
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

## Desired Directory Structure With Justification for Each File or Directory

This section outlines the target directory structure after refactoring, including the justification for each component based on the rationalization effort. It should mirror the 'Current Directory Structure' section above.

Nucleus/ 
├── .devcontainer/         # **Purpose:** Defines the configuration for running the development environment within a Docker container.
│   └── devcontainer.json  # **Purpose:** Contains specific settings for the dev container.
├── .github/
│   └── workflows/         # **Purpose:** Contains GitHub Actions workflow definitions.
├── .vs/
├── .vscode/
│   └── launch.json
├── AgentOps/              # **Purpose:** Contains files specifically supporting the AI Agent (Cascade) development workflow.
│   ├── Archive/           # **Purpose:** Stores historical documents from previous agent development sessions.
│   │   ├── STORY_01_NavigatingEvolvingAILibraries.md
│   │   ├── STORY_02_MCP_Server_Integration_Lessons.md
│   │   ├── STORY_03_LinterIntegration.md
│   ├── Logs/
│   ├── Scripts/           # **Purpose:** Contains utility scripts used to assist the agent or developer.
│   │   ├── codebase_to_markdown.py
│   │   ├── csharp_code_analyzer.csx
│   │   └── tree_gitignore.py # **Purpose:** Python script to generate a directory tree view respecting .gitignore.
│   ├── 00_START_HERE_METHODOLOGY.md # **Purpose:** Outlines the methodology and rules for AI-assisted development.
│   ├── 01_PROJECT_CONTEXT.md     # **Purpose:** Provides high-level project context for the AI agent (this file).
│   ├── 02_CURRENT_SESSION_STATE.md # **Purpose:** Captures the state of the current agent development session.
│   ├── 03_PROJECT_PLAN_KANBAN.md   # **Purpose:** Kanban-style board in Markdown for tracking tasks.
│   ├── 04_AGENT_TO_AGENT_CONVERSATION.md # **Purpose:** Log/scratchpad for meta-level communication between sessions.
│   └── README.md          # **Purpose:** Overview of the AgentOps directory.
├── Docs/                  # **Purpose:** Contains all human-readable project documentation.
│   ├── Architecture/      # **Purpose:** Detailed system architecture design documents.
│   │   ├── ClientAdapters/  # **Purpose:** Architecture for adapters connecting to external client platforms.
│   │   │   ├── Console/
│   │   │   │   └── ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md
│   │   │   ├── Teams/
│   │   │   │   ├── ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md # **Purpose:** Details fetching file attachments via Graph API for Teams.
│   │   │   │   └── ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
│   │   │   ├── ARCHITECTURE_ADAPTER_INTERFACES.md # **Purpose:** Defines common interfaces across client adapters.
│   │   │   ├── ARCHITECTURE_ADAPTERS_CONSOLE.md
│   │   │   ├── ARCHITECTURE_ADAPTERS_DISCORD.md
│   │   │   ├── ARCHITECTURE_ADAPTERS_EMAIL.md
│   │   │   ├── ARCHITECTURE_ADAPTERS_SLACK.md
│   │   │   └── ARCHITECTURE_ADAPTERS_TEAMS.md
│   │   ├── Deployment/      # **Purpose:** Architecture related to application deployment.
│   │   │   ├── Hosting/
│   │   │   │   ├── ARCHITECTURE_HOSTING_AZURE.md
│   │   │   │   ├── ARCHITECTURE_HOSTING_CLOUDFLARE.md
│   │   │   │   └── ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md
│   │   │   ├── ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md
│   │   │   └── ARCHITECTURE_DEPLOYMENT_CICD_OSS.md
│   │   ├── Personas/        # **Purpose:** Architecture defining AI 'Personas'.
│   │   │   ├── Bootstrapper/
│   │   │   ├── Educator/
│   │   │   │   ├── Pedagogical_And_Tautological_Trees_Of_Knowledge/
│   │   │   │   │   └── Age*.md
│   │   │   │   ├── ARCHITECTURE_EDUCATOR_KNOWLEDGE_TREES.md
│   │   │   │   ├── ARCHITECTURE_EDUCATOR_REFERENCE.md
│   │   │   │   └── NumeracyAndTimelinesWebappConcept.md
│   │   │   ├── Professional/
│   │   │   │   └── ARCHITECTURE_AZURE_DOTNET_HELPDESK.md
│   │   │   ├── ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md
│   │   │   ├── ARCHITECTURE_PERSONAS_EDUCATOR.md
│   │   │   └── ARCHITECTURE_PERSONAS_PROFESSIONAL.md
│   │   ├── Processing/      # **Purpose:** Architecture detailing the data processing pipeline.
│   │   │   ├── Dataviz/
│   │   │   │   ├── Examples/
│   │   │   │   │   ├── dataviz.html
│   │   │   │   │   └── EXAMPLE_OUTPUT_*.html
│   │   │   │   └── ARCHITECTURE_DATAVIZ_TEMPLATE.md
│   │   │   ├── Ingestion/
│   │   │   │   ├── ARCHITECTURE_INGESTION_FILECOLLECTIONS.md
│   │   │   │   ├── ARCHITECTURE_INGESTION_MULTIMEDIA.md
│   │   │   │   ├── ARCHITECTURE_INGESTION_PDF.md
│   │   │   │   └── ARCHITECTURE_INGESTION_PLAINTEXT.md
│   │   │   ├── Orchestration/
│   │   │   │   ├── ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md
│   │   │   │   ├── ARCHITECTURE_ORCHESTRATION_ROUTING.md
│   │   │   │   └── ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
│   │   │   ├── ARCHITECTURE_PROCESSING_DATAVIZ.md
│   │   │   ├── ARCHITECTURE_PROCESSING_INGESTION.md
│   │   │   ├── ARCHITECTURE_PROCESSING_INTERFACES.md
│   │   │   └── ARCHITECTURE_PROCESSING_ORCHESTRATION.md
│   │   ├── Storage/
│   │   ├── 00_ARCHITECTURE_OVERVIEW.md # **Purpose:** Top-level system architecture overview.
│   │   ├── 01_ARCHITECTURE_PROCESSING.md
│   │   ├── 02_ARCHITECTURE_PERSONAS.md
│   │   ├── 03_ARCHITECTURE_STORAGE.md
│   │   ├── 04_ARCHITECTURE_DATABASE.md
│   │   ├── 05_ARCHITECTURE_CLIENTS.md
│   │   ├── 06_ARCHITECTURE_SECURITY.md
│   │   ├── 07_ARCHITECTURE_DEPLOYMENT.md
│   │   └── 08_ARCHITECTURE_AI_INTEGRATION.md
│   ├── HelpfulMarkdownFiles/ # **Purpose:** Supplementary markdown files for development context.
│   │   ├── Library-References/
│   │   │   ├── AzureAI.md
│   │   │   ├── AzureCosmosDBDocumentation.md
│   │   │   ├── DotnetAspire.md
│   │   │   ├── MicrosoftExtensionsAI.md
│   │   │   └── Mscc.GenerativeAI.Microsoft-Reference.md
│   │   ├── Aspire-AI-Template-Extended-With-Gemini.md
│   │   ├── Nucleus Teams Adapter Report.md
│   │   ├── Secure-Bot-Framework-Azure-Deployment.md
│   │   ├── Slack-Email-Discord-Adapter-Report.md
│   │   └── Windsurf Dev Container Integration Feasibility_.md
│   ├── Planning/          # **Purpose:** Project planning, roadmap, and task breakdowns.
│   │   ├── 00_ROADMAP.md
│   │   ├── 01_PHASE1_MVP_TASKS.md
│   │   ├── 02_PHASE2_MULTI_PLATFORM_TASKS.md
│   │   ├── 03_PHASE3_ENHANCEMENTS_TASKS.md
│   │   └── 04_PHASE4_MATURITY_TASKS.md
│   └── Requirements/      # **Purpose:** Formal project requirements documents.
│       ├── 00_PROJECT_MANDATE.md # **Purpose:** Defines the overall project goal and mandate.
│       ├── 01_REQUIREMENTS_PHASE1_MVP_CONSOLE.md
│       ├── 02_REQUIREMENTS_PHASE2_MULTI_PLATFORM.md
│       ├── 03_REQUIREMENTS_PHASE3_ENHANCEMENTS.md
│       └── 04_REQUIREMENTS_PHASE4_MATURITY.md
├── Nucleus.AppHost/       # **Purpose:** The .NET Aspire Application Host project.
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── Nucleus.AppHost.csproj
│   └── Program.cs         # **Purpose:** Main entry point for the .NET Aspire AppHost.
├── src/                   # **Purpose:** Contains the core application source code.
│   ├── Abstractions/      # **Purpose:** Parent directory for abstraction projects.
│   │   └── Nucleus.Abstractions/ # **Purpose:** Project defining shared interfaces, DTOs, core models, and repository contracts.
│   │       ├── Models/
│   │       │   └── ArtifactMetadata.cs
│   │       ├── Repositories/
│   │       │   ├── IArtifactMetadataRepository.cs
│   │       │   └── IPersonaKnowledgeRepository.cs
│   │       ├── IMessageQueuePublisher.cs
│   │       ├── IOrchestrationService.cs
│   │       ├── IPersona.cs
│   │       ├── IPlatformAttachmentFetcher.cs
│   │       ├── IPlatformNotifier.cs
│   │       ├── Nucleus.Abstractions.csproj
│   │       ├── NucleusIngestionRequest.cs
│   │       └── PlatformAttachmentReference.cs
│   ├── Adapters/            # **Purpose:** Parent directory for projects implementing adapters to external platforms.
│   │   ├── Nucleus.Adapters.Console/ # **Purpose:** Project implementing the CLI adapter.
│   │   │   ├── Services/
│   │   │   │   ├── ConsoleArtifactProvider.cs # **Purpose:** Service for accessing local files via console.
│   │   │   │   └── NucleusApiServiceAgent.cs # **Purpose:** Agent to communicate with the backend API from console.
│   │   │   ├── appsettings.json
│   │   │   ├── Nucleus.Console.csproj
│   │   │   └── Program.cs     # **Purpose:** Main entry point for the CLI application.
│   │   └── Nucleus.Adapters.Teams/ # **Purpose:** Project implementing the Microsoft Teams adapter.
│   │       ├── GraphClientService.cs # **Purpose:** Service encapsulating Microsoft Graph API interactions.
│   │       ├── Nucleus.Adapters.Teams.csproj
│   │       ├── TeamsAdapterBot.cs # **Purpose:** Implements the Bot Framework ActivityHandler for Teams.
│   │       ├── TeamsAdapterConfiguration.cs
│   │       ├── TeamsGraphFileFetcher.cs # **Purpose:** Implements IPlatformAttachmentFetcher for Teams.
│   │       └── TeamsNotifier.cs # **Purpose:** Implements IPlatformNotifier for Teams.
│   ├── DataAccess/          # **Purpose:** Parent directory for data persistence logic projects.
│   ├── Domain/              # **Purpose:** Parent directory for core business logic projects.
│   │   └── Nucleus.Domain.Processing/ # **Purpose:** Project containing central domain services (Orchestration, Dataviz).
│   │       ├── Resources/
│   │       │   └── Dataviz/
│   │       │       ├── dataviz_plotly_script.py
│   │       │       ├── dataviz_script.js
│   │       │       ├── dataviz_styles.css
│   │       │       ├── dataviz_template.html
│   │       │       └── dataviz_worker.js
│   │       ├── Services/
│   │       │   └── DatavizHtmlBuilder.cs # **Purpose:** Service for constructing HTML data visualizations.
│   │       ├── Nucleus.Processing.csproj # **Purpose:** C# project file (Consider renaming: Nucleus.Domain.Core).
│   │       ├── OrchestrationService.cs # **Purpose:** Implementation of IOrchestrationService.
│   │       └── ServiceCollectionExtensions.cs
│   ├── Features/            # **Purpose:** Parent directory for cross-cutting feature projects.
│   ├── Personas/            # **Purpose:** Parent directory for AI 'Persona' projects.
│   │   └── Nucleus.Personas.Core/ # **Purpose:** Project containing base persona logic.
│   │       ├── BootstrapperPersona.cs
│   │       ├── EmptyAnalysisData.cs
│   │       └── Nucleus.Personas.Core.csproj
│   └── Services/            # **Purpose:** Parent directory for hosting service projects (APIs, workers).
│       ├── Nucleus.ServiceDefaults/ # **Purpose:** Standard Aspire Service Defaults project.
│       │   ├── Extensions.cs
│       │   └── Nucleus.ServiceDefaults.csproj
│       └── Nucleus.Services.Api/ # **Purpose:** Primary backend API service project (ASP.NET Core).
│           ├── Configuration/
│           │   └── GeminiOptions.cs
│           ├── Controllers/
│           │   └── InteractionController.cs
│           ├── Diagnostics/
│           │   └── ServiceBusSmokeTestService.cs # **Purpose:** Hosted service for Azure Service Bus connectivity test.
│           ├── Infrastructure/
│           │   └── Messaging/
│           │       ├── AzureServiceBusPublisher.cs # **Purpose:** Implements IMessageQueuePublisher using Azure Service Bus.
│           │       └── ServiceBusQueueConsumerService.cs # **Purpose:** Background service consuming messages from Azure Service Bus.
│           ├── Properties/
│           │   └── launchSettings.json
│           ├── AdapterWithErrorHandler.cs # **Purpose:** Custom Bot Framework Adapter with error handling.
│           ├── appsettings.Development.json
│           ├── appsettings.json
│           ├── Nucleus.ApiService.csproj # **Purpose:** C# project file (Consider renaming: Nucleus.Services.Api).
│           └── Program.cs     # **Purpose:** Main entry point for the ASP.NET Core API service.
├── tests/                 # **Purpose:** Contains all test projects.
│   └── Adapters/            # **Purpose:** Test projects for the Adapters layer.
│       └── Nucleus.Adapters.Console.Tests/ # **Purpose:** Tests for the Console Adapter.
│           └── TestData/
│               └── test_context.txt
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .windsurfrules         # **Purpose:** Contains specific instructions and guidelines (Memories) for the AI agent.
├── LICENSE.txt            # **Purpose:** Contains the full text of the open-source license (MIT License).
├── Nucleus.sln            # **Purpose:** The Visual Studio Solution file.
└── README.md              # **Purpose:** The main entry point documentation for the repository.
