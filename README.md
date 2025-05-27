---
title: "Nucleus Project README"
description: "Root README for the Nucleus project, a platform for contextual AI Personas."
version: 2.0
date: 2025-05-27
---

# Nucleus: Contextual AI Personas for M365 & Beyond

**Nucleus is an open-source platform designed to empower individuals and teams by transforming disparate digital information into actionable, contextual knowledge through specialized AI assistants ("Personas") that operate within Microsoft 365 and other collaboration environments.**

It provides a robust, flexible, and secure foundation for Retrieval-Augmented Generation (RAG) that emphasizes **platform integration**, **persona-driven intelligence**, and **user data sovereignty**. Nucleus enables the development of "Virtual Colleagues" that understand context, respect permissions, and integrate seamlessly into your daily workflows.

## Development Status

⚠️ **Please Note:** Nucleus is currently under **active development and undergoing a significant architectural evolution** towards M365 Persona Agents and Model Context Protocol (MCP) Tools. Expect rapid changes, potential instability, and ongoing refinement. This project is developed in the open, serving as a live demonstration of agentic AI development methodologies.

## Vision

We envision a future where knowledge work and learning are augmented by reliable, context-aware, and specialized AI assistants or "Personas," tailored to specific needs and data ecosystems. These Personas will operate as **first-class citizens within Microsoft 365 (Teams, Outlook, SharePoint)** and other collaboration platforms, providing intelligent assistance directly within users' existing workflows.

[Read the full Project Mandate](./Docs/Requirements/00_PROJECT_MANDATE.md) for a deeper understanding of our mission.

## Core Architectural Pillars

Nucleus is built upon several key architectural pillars:

1.  **M365 Persona Agents:**
    *   Personas are realized as **Microsoft Graph-connected applications** (e.g., Teams Bots, Outlook Add-ins) that act as "Virtual Colleagues."
    *   They leverage **Microsoft Graph APIs** to understand context (conversations, files, calendars) and operate within the user's M365 environment, respecting existing permissions.
    *   Focus on **Resource-Specific Consent (RSC)** for granular, context-aware data access in Teams.

2.  **Model Context Protocol (MCP) Tools:**
    *   Nucleus Personas will expose their capabilities as **MCP Tools**. This allows them to be invoked by other AI agents or systems that understand the MCP specification.
    *   Enables interoperability and composability of AI functionalities.
    *   [Learn more about MCP Tools (Conceptual Link - to be updated with actual link when available)]()

3.  **Platform Integration First:**
    *   Beyond M365, Personas can be adapted for other platforms like Slack, Discord, and Email, maintaining a consistent interaction model.

4.  **Persona-Driven Intelligence (Anti-Chunking):**
    *   Specialized AI Personas (e.g., `EduFlowOmniEducator`, `BusinessKnowledgeAssistant`) analyze content contextually.
    *   We **reject mechanical document chunking**. Instead, Personas intelligently extract relevant information and generate structured analyses (`ArtifactMetadata`, `PersonaKnowledgeEntry`).

5.  **Ephemeral Processing & User Data Sovereignty:**
    *   Source artifacts (user files, emails) remain in the user's designated systems (e.g., SharePoint, OneDrive, local machine for console interactions).
    *   Nucleus processes data transiently and stores only derived metadata and knowledge in its database (Azure Cosmos DB). Original content is not persisted by Nucleus.

6.  **Secure and Scalable Backend:**
    *   Built on **.NET 8.0** and **DotNet Aspire** for orchestration.
    *   Utilizes Azure services (Cosmos DB for NoSQL + Vector Search, Azure Service Bus).
    *   Employs `Microsoft.Extensions.AI` for AI model abstractions (initially Google Gemini).

7.  **Agentic Development Focus:**
    *   Documentation and code are tightly cross-linked to support AI-assisted development workflows.
    *   The `/AgentOps` directory contains operational plans and session states for AI development agents.

## Getting Started

### Prerequisites:

*   **.NET 8.0 SDK** (Verify with `dotnet --version`)
*   **Docker Desktop** (or a compatible OCI runtime) for running emulators and containerized services.
*   **Azure Developer CLI (`azd`)** (Recommended for Azure deployments and consistent environment setup).
*   **Access to an AI Provider** (e.g., Google Gemini API Key, Azure OpenAI endpoint).
*   **(Optional) Microsoft 365 Developer Tenant** for building and testing M365 Persona Agents.

### Setup & Configuration:

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/jfjordanfarr/Nucleus.git
    cd Nucleus
    ```

2.  **Initialize with Azure Developer CLI (Recommended):**
    *   If you have `azd` installed, you can initialize the environment and provision Azure resources (or configure local emulators) by running:
        ```bash
        azd init
        azd env new <your-environment-name>
        # Follow prompts to select template (if any) and configure environment.
        # azd up (to provision and deploy to Azure, or configure local resources)
        ```
    *   `azd` helps manage environment variables and secrets.

3.  **Manual Secret Configuration (if not using `azd`):**
    *   Set up necessary API keys (e.g., AI provider) and connection strings (e.g., Cosmos DB, Service Bus) via .NET User Secrets for the `Nucleus.AppHost` project and relevant service projects.
    *   Example for `Nucleus.AppHost`:
        ```bash
        cd Aspire/Nucleus.AppHost
        dotnet user-secrets init
        dotnet user-secrets set "Azure:CosmosDb:ConnectionString" "your_cosmosdb_connection_string_or_emulator_string"
        dotnet user-secrets set "AI:Google:ApiKey" "your_gemini_api_key"
        # Add other secrets as required by components.
        cd ../.. 
        ```
    *   Refer to individual component documentation (linked from Namespace documents) for specific required secrets.

### Running Locally with .NET Aspire:

1.  **Navigate to the AppHost Directory:**
    ```bash
    cd Aspire/Nucleus.AppHost
    ```

2.  **Run the Application:**
    ```bash
    dotnet run
    ```
    This command starts:
    *   The .NET Aspire Dashboard (typically `http://localhost:19888`).
    *   All defined services and projects within the Aspire application (e.g., `Nucleus.Core.AgentRuntime`, `Nucleus.Adapters.Console`, `Nucleus.Infrastructure.ExternalServices`).
    *   Emulators for services like Azure Cosmos DB and Azure Service Bus (if configured in `Program.cs`).

3.  **Interact (Initial MVP - Console Adapter):**
    *   The `Nucleus.Adapters.Console` application (launched by the AppHost) provides an initial command-line interface for interaction.
    *   Open a new terminal or use the Aspire Dashboard to view logs and access services.
    *   Example commands (syntax may evolve):
        ```bash
        # (Assuming the console adapter exposes a CLI or listens for input)
        # This part will be refined as the Console Adapter matures.
        # For now, check the output of the Nucleus.Adapters.Console service in the Aspire Dashboard.
        ```

## High-Level Project Structure

The Nucleus codebase is organized into several key top-level directories and solution folders, reflecting its modular architecture.

*   **`/Aspire/Nucleus.AppHost`**: The .NET Aspire application host project. Orchestrates all services, emulators, and projects for local development. This is the **primary entry point for running the system locally.**
*   **`/Aspire/Nucleus.ServiceDefaults`**: Contains shared configurations, extensions, and conventions for services participating in the Aspire ecosystem (e.g., health checks, OpenTelemetry setup).

*   **`/src`**: Contains all the source code for Nucleus, organized by architectural layers and concerns.
    *   **`/src/Nucleus.SharedKernel`**: (`Nucleus.SharedKernel.csproj`) Core abstractions, interfaces, and data transfer objects (DTOs) shared across the entire system. Includes `IPersona`, `ArtifactMetadata`, `PersonaKnowledgeEntry`, MCP-related interfaces, etc.
    *   **`/src/Nucleus.Core.AgentRuntime`**: (`Nucleus.Core.AgentRuntime.csproj`) The heart of the agentic capabilities. Manages Persona lifecycles, orchestrates MCP Tool execution, and handles core RAG logic (retrieval, synthesis).
    *   **`/src/Nucleus.Core.RagLogic`**: (`Nucleus.Core.RagLogic.csproj`) Contains the specialized logic for intelligent content extraction, analysis, and knowledge representation, distinct from generic chunking.
    *   **`/src/Nucleus.Adapters.Console`**: (`Nucleus.Adapters.Console.csproj`) A command-line interface (CLI) adapter for interacting with Nucleus. Serves as an initial testbed and direct interaction point.
    *   **`/src/Nucleus.Adapters.MicrosoftGraph`**: (`Nucleus.Adapters.MicrosoftGraph.csproj`) (Future) Adapter for integrating with Microsoft 365 via Microsoft Graph API (Teams bots, Outlook add-ins).
    *   **`/src/Nucleus.Infrastructure.DataPersistence`**: (`Nucleus.Infrastructure.DataPersistence.csproj`) Implements data access logic, primarily for Azure Cosmos DB (NoSQL API + Vector Search).
    *   **`/src/Nucleus.Infrastructure.ExternalServices`**: (`Nucleus.Infrastructure.ExternalServices.csproj`) Clients and connectors for external services like AI models (Google Gemini, Azure OpenAI) and other third-party APIs.
    *   **`/src/Nucleus.Infrastructure.Messaging`**: (`Nucleus.Infrastructure.Messaging.csproj`) Implements message queue interactions, primarily for Azure Service Bus.

*   **`/Docs`**: Contains all project documentation.
    *   **`/Docs/Requirements`**: Project mandate, use cases, and functional/non-functional requirements.
        *   [`00_PROJECT_MANDATE.md`](./Docs/Requirements/00_PROJECT_MANDATE.md)
    *   **`/Docs/Architecture`**: Detailed architectural documents, C4 models, and design decisions.
        *   [`00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md`](./Docs/Architecture/00_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md): The primary architectural overview.
        *   **`/Docs/Architecture/DevelopmentLifecycle`**: Documents related to the development process.
            *   [`01_NAMESPACES_FOLDERS.md`](./Docs/Architecture/DevelopmentLifecycle/01_NAMESPACES_FOLDERS.md): Definitive guide to codebase structure, project naming, and namespace conventions.
            *   [`02_TESTING_STRATEGY.md`](./Docs/Architecture/DevelopmentLifecycle/02_TESTING_STRATEGY.md): Detailed testing strategy.
    *   **`/Docs/Meta`**: Documents about the documentation itself.

*   **`/AgentOps`**: Files supporting AI-assisted development (Methodology, Context, State, Plan). This is meta-repository content for the agentic development process.

*   **`/tests`**: Contains automated test projects, mirroring the `/src` structure.
    *   **`/tests/Nucleus.Core.AgentRuntime.Tests`**: Unit and integration tests for the agent runtime.
    *   **`/tests/Nucleus.Adapters.Console.Tests`**: Tests for the console adapter.
    *   **`/tests/Nucleus.System.IntegrationTests`**: End-to-end integration tests for the system, often utilizing Testcontainers and `WebApplicationFactory` where applicable.

For a definitive guide on project structure, naming conventions, and namespaces, please refer to the [Namespaces and Folder Structure document](./Docs/Architecture/DevelopmentLifecycle/01_NAMESPACES_FOLDERS.md).

## Testing Strategy

Nucleus employs a comprehensive, multi-layered testing strategy to ensure reliability and maintainability. Our approach is guided by the principles outlined in the [Testing Strategy document](./Docs/Architecture/DevelopmentLifecycle/02_TESTING_STRATEGY.md).

Key aspects include:

*   **Unit Tests:** Focused on individual components and classes in isolation. Located within test projects that mirror the source project structure (e.g., `Nucleus.Core.AgentRuntime.Tests` for `Nucleus.Core.AgentRuntime`).
*   **Integration Tests:** Verify interactions between components.
    *   **Service-Level Integration Tests:** For services like `Nucleus.Core.AgentRuntime`, these tests may involve in-memory providers or test doubles for external dependencies.
    *   **Infrastructure Integration Tests:** Test persistence and messaging components against real (or emulated via Testcontainers) dependencies like Cosmos DB and Service Bus. These are often found in projects like `Nucleus.Infrastructure.DataPersistence.Tests`.
*   **System Integration Tests (`/tests/Nucleus.System.IntegrationTests`):**
    *   These tests validate flows across multiple services orchestrated by .NET Aspire.
    *   They leverage `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory` for testing API endpoints (if applicable to a service) and Testcontainers for managing external dependencies like databases and message queues in a controlled environment.
*   **Test-Driven Development (TDD):** Encouraged for new feature development.

### Running Tests:

Execute all tests from the **solution root directory** (the directory containing `Nucleus.sln` and the `Aspire` directory) using:

```bash
dotnet test
```

This command will discover and run tests from all test projects in the solution.

For more detailed information on the testing philosophy, setup, specific test types, and how to run targeted tests, please refer to the [Testing Strategy document](./Docs/Architecture/DevelopmentLifecycle/02_TESTING_STRATEGY.md).

## Contributing

This project is being developed actively, often with AI assistance, leading to rapid iteration. While the development is public and open-source, we are **not actively seeking contributions via Pull Requests from the general public at this early stage** due to the high pace of architectural change and the specialized nature of the agentic development process.

However, feedback, bug reports, and feature suggestions are highly valuable! Please feel free to:
*   Open an issue on the [GitHub Issues](https://github.com/jfjordanfarr/Nucleus/issues) page.
*   Engage in discussions if a discussion forum is established.

As the architecture stabilizes, we will provide clearer guidelines for community contributions.

## License

This project is licensed under the [MIT License](./LICENSE.txt).