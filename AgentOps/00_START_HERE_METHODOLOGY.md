# AgentOps Methodology for Nucleus OmniRAG (.NET / Azure - Cosmos DB Backend)

## Introduction

This document outlines the AgentOps methodology for AI-assisted development used in the Nucleus OmniRAG project. Following this process helps maintain context, track progress, and ensure efficient collaboration between human developers/leads and AI assistants within the .NET/Azure ecosystem, specifically targeting Azure Cosmos DB as the backend and supporting multiple AI Personas. This methodology also serves as a framework for generating high-quality training data for future AI development agents.

## Core Principles

1.  **Stateful Context Management**: Using dedicated documents (`01_PROJECT_CONTEXT.md`, `02_CURRENT_SESSION_STATE.md`, `03_PROJECT_PLAN_KANBAN.md`, `/docs/00_PROJECT_MANDATE.md`) to preserve context. Accurate state tracking is paramount for effective collaboration and training data generation.
2.  **Incremental Progress Tracking**: Breaking work into manageable tasks (Kanban) and tracking the immediate focus (Session State).
3.  **Structured Collaboration**: AI assistants use the provided state documents to understand the current focus and assist with the defined "Next Step", following specific VS Code interaction patterns. Human leads provide guidance, feedback, and validation.
4.  **Continuous Documentation**: Updating state documents in real-time as progress is made or issues arise. **AI assistance in keeping these updated is expected and crucial for training data quality.**
5.  **Architectural Adherence**: Development must align with the architecture summarized in `01_PROJECT_CONTEXT.md` (Cosmos DB backend, external orchestrator, multi-persona interaction model). Emphasize SOLID principles, Dependency Injection, and asynchronous programming (`async`/`await`).
6.  **Test-Driven Development (TDD):** Aim to write tests (Unit, Integration) before or alongside implementation where practical. Define test cases as part of task definitions.
7.  **Distinguish Process Types:** Be mindful of the difference between "Slow Processes" (asynchronous, potentially long-running tasks like ingestion analysis by multiple personas) and "Fast Processes" (low-latency operations like chat responses or tool calls). Design components appropriately.
8.  **Persona Interaction & Deduplication:** Recognize that personas can interact (especially during ingestion to determine salience) and that mechanisms to prevent duplicate data ingestion are necessary.

## Roles of Key AgentOps Files

*   **`/docs/00_PROJECT_MANDATE.md`**: The "Why". Understand the motivation and high-level vision.
*   **`01_PROJECT_CONTEXT.md`**: The "What". Provides stable, high-level technical context: goals, .NET/Azure/Cosmos DB tech stack, architectural summary (including persona interaction model), links to detailed docs.
*   **`02_CURRENT_SESSION_STATE.md`**: The "Now". Captures the **microstate**. Dynamic and updated frequently. Details the *specific task*, *last action*, relevant *C# code*, *errors/blockers*, and the *immediate next step*. **This is your primary focus.** Accuracy here is key for training data.
*   **`03_PROJECT_PLAN_KANBAN.md`**: The "Where". Captures the **macrostate**. Tracks overall progress of features/tasks. Updated less frequently.

## Workflow Process

1.  **Session Start:** Developer/Lead shares `02_CURRENT_SESSION_STATE.md` (and potentially others) with the AI. AI reviews state, context (`01`, Mandate), and plan (`03`).
2.  **Task Execution:** Focus on the **Immediate Next Step** defined in `02_CURRENT_SESSION_STATE.md`. AI assists with C# code generation, debugging, analysis, Azure SDK usage, test generation, etc.
3.  **Code Insertion:** When providing code changes in markdown blocks (```csharp ... ```), **provide the complete code block and explicitly ask the developer to use the "Insert in Editor" button.**
4.  **State Update:** After completing a step, applying a code insertion, encountering an error, or shifting focus, the developer/lead (with AI help) **updates `02_CURRENT_SESSION_STATE.md`**. This step is critical for maintaining context and generating useful training data.
5.  **Kanban Update:** When a Kanban task progresses, the developer/lead (with AI help) updates `03_PROJECT_PLAN_KANBAN.md`.
6.  **Archiving:** Periodically, `02_CURRENT_SESSION_STATE.md` is moved to `Archive/`, and a new one is started.

## "Nagging Thoughts" for AI Assistants (Critical Instructions for .NET/Azure/VS Code)

To ensure effectiveness and avoid common pitfalls, please constantly consider:

1.  **Check for Existing Work / Verify File Existence:** Before creating a file/class/interface, ask: "Does something similar already exist according to the project structure (`01`), existing code, or Kanban (`03`)?" **To verify file existence, DO NOT rely on VS Code Search. Propose or ask the developer to use a terminal command (`dir` or `ls`).**
2.  **Avoid Placeholder Workarounds & Technical Debt:** Don't settle for quick fixes or workarounds when facing obstacles. These often persist as difficult-to-debug technical debt long after they leave your context window. When there's a "right way" (e.g., using proper Microsoft.Extensions.AI interfaces or DotNet Aspire patterns), invest time to implement it correctly from the start, even if it requires several attempts or additional research. This project is intended for production use; take time to research proper implementation patterns via web search when facing integration challenges rather than creating improvised solutions. Remember that each technical compromise made for expediency creates a hidden cost for future maintainers.
3.  **Aim for Robust Solutions:** Ask: "Is this a temporary patch, or does it address the root cause robustly? Does it align with SOLID principles, use DI correctly, handle asynchronicity properly (`async`/`await`), and utilize Azure SDKs (`azure-cosmos`) according to best practices (client lifetime, partitioning, error handling, vector indexing configuration)?"
4.  **Simplicity & Clarity:** Ask: "Is this the clearest and simplest C# code? Are interfaces being used effectively?"
5.  **Use Correct CLI Commands:** Ensure correct `dotnet`, `az` commands. **For PowerShell, prefer chaining with `;`.**
6.  **Use "Insert in Editor" Correctly:** Provide complete code blocks. **Always remind the developer to use the "Insert in Editor" button.** Do not assume changes are applied unless confirmed.
7.  **Update State Documents:** After providing code, analysis, or after developer confirms applying changes, remind/propose updates for `02_CURRENT_SESSION_STATE.md` and potentially `03_PROJECT_PLAN_KANBAN.md`. This is vital for session continuity and training data.
8.  **Consider Multi-Persona Interaction:** How might this component be used or triggered by different personas? Does the data model support storing persona-specific insights (e.g., the `analysis` field within `PersonaKnowledgeEntry`)?
9.  **Slow vs. Fast Processes:** Is this part of ingestion (likely slow, asynchronous) or query handling (likely fast, synchronous API)? Choose appropriate patterns.
10. **Deduplication:** During ingestion-related tasks, ask: "How will we prevent processing the exact same artifact multiple times? Should we add checks based on `ArtifactMetadata` properties (like `sourceUri` and potentially a content hash) or use the `sourceIdentifier`?"
11. **Troubleshooting Build Issues:** When encountering build errors:
    - **Project Structure Issues:** Check for parent-child project conflicts in nested directories. A parent project (e.g., `Nucleus.Personas.csproj`) with subprojects in subdirectories (e.g., `EduFlow/EduFlow.csproj`) needs proper exclusions:
      ```xml
      <ItemGroup>
        <Compile Remove="SubDir\**" />
        <EmbeddedResource Remove="SubDir\**" />
        <None Remove="SubDir\**" />
        <Content Remove="SubDir\**" />
      </ItemGroup>
      ```
    - **Missing Namespaces:** Remember that interfaces may be in specialized sub-namespaces (e.g., `Nucleus.Abstractions.Services.Retrieval`) requiring explicit imports.
    - **Constructor Parameters:** When updating interfaces or implementations, check all dependent test files for mock setup requirements and expected property names.
    - **Method Name Mismatches:** Verify exact method and class names in extension method calls (e.g., `builder.Services.AddNucleusInfrastructure(builder.Configuration)` vs. `builder.AddInfrastructureServices()`).
    - **Project References:** Ensure all necessary projects are referenced, particularly when working with test projects that need access to interfaces.

By adhering to this methodology and keeping these "nagging thoughts" in mind, you will significantly contribute to the successful development of Nucleus OmniRAG and help train more effective AI development assistants.