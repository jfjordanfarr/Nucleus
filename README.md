# Nucleus OmniRAG

**Nucleus OmniRAG is a platform designed to empower individuals and teams by transforming disparate digital information into actionable, contextual knowledge through specialized AI assistants ("Personas").**

It provides a robust, flexible, and secure foundation for Retrieval-Augmented Generation (RAG) that respects user data ownership and adapts to different needs and deployment models.

## Vision

We envision a future where knowledge work and learning are augmented by reliable, context-aware, and specialized AI assistants or "Personas," tailored to specific needs and data ecosystems, seamlessly integrated into users' existing workflows (Microsoft Teams, Slack, Discord, Email).

[Read the full Project Mandate](./Docs/Requirements/00_PROJECT_MANDATE.md)

## Key Features & Principles

*   **Platform Integration First:** Personas operate primarily as bots/apps within existing platforms (Teams, Slack, etc.).
*   **Persona-Driven Intelligence:** Specialized AI Personas (e.g., Educator, Professional) analyze content contextually, avoiding generic chunking.
*   **Contextual Retrieval:** Combines vector search with structured metadata for accurate information retrieval.
*   **Ephemeral Processing:** Prioritizes processing data transiently to enhance security and ensure data freshness. Nucleus avoids persisting intermediate transformed content.
*   **User Data Sovereignty:** Source artifacts remain in the user's designated systems; Nucleus stores derived metadata and knowledge (`ArtifactMetadata`, `PersonaKnowledgeEntry`).
*   **Flexible Deployment:** Supports Cloud-Hosted (Azure) and Self-Hosted options.
*   **Modern .NET Stack:** Built with .NET 9, Aspire, Cosmos DB, and `Microsoft.Extensions.AI`.
*   **Agentic Development Focus:** Documentation and code are tightly cross-linked to support AI-assisted development workflows.

## Getting Started

1.  **Prerequisites:**
    *   .NET 9 SDK
    *   Docker Desktop (or compatible OCI runtime)
    *   Azure Developer CLI (`azd`) (Optional, for Azure deployment)
    *   Access to an AI Provider (e.g., Google Gemini API Key)
2.  **Clone the repository.**
3.  **Configure Secrets:** Set up necessary API keys (e.g., AI provider) and connection details (if not using emulators) via .NET User Secrets for the `Nucleus.AppHost` project. Refer to component documentation for required keys.
4.  **Run with Aspire:**
    ```bash
    # Navigate to the AppHost directory
    cd Nucleus.AppHost 
    # Run the application
    dotnet run
    ```
    This will start the AppHost, related projects (API, Console), emulators (like Cosmos DB), and the .NET Aspire Dashboard.
5.  **Interact via Console:** Use the `Nucleus.Console` application (running via the AppHost) for initial interactions (MVP).
    ```bash
    # Example commands (run from a separate terminal)
    # nucleus query "Summarize the recent project updates"
    # nucleus ingest ./path/to/document.txt 
    ```

## Project Structure

*   **`/Nucleus.AppHost`**: .NET Aspire AppHost for local development orchestration.
*   **`/Nucleus.ApiService`**: Core backend API providing endpoints for clients.
*   **`/Nucleus.Console`**: Console application client (MVP interface).
*   **`/Nucleus.Processing`**: Contains ingestion, processing, and persona logic.
*   **`/Nucleus.ServiceDefaults`**: Shared configurations for Aspire services.
*   **`/Docs`**: Architecture, Requirements, Planning, and other documentation.
    *   [`/Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md`](./Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md): High-level system overview.
*   **`/AgentOps`**: Files supporting AI-assisted development (Methodology, Context, State, Plan).
*   **`/Nucleus.Tests`**: Unit and integration tests.

## Contributing

Please refer to the [AgentOps Methodology](./AgentOps/00_START_HERE_METHODOLOGY.md) and the [`.windsurfrules`](./.windsurfrules) for guidelines on contributing, especially when using AI assistance. Ensure code and documentation adhere to the project's cross-linking and quality standards.

## License

This project is licensed under the [MIT License](./LICENSE.txt).