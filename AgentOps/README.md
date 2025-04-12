# AgentOps for Nucleus OmniRAG (.NET / Azure - Cosmos DB Backend)

**Attention AI Assistant:** This folder contains critical context for developing the Nucleus OmniRAG platform and its Personas (EduFlow, HealthSpecialist, etc.) using .NET and Azure, with Azure Cosmos DB as the primary backend. Your interactions in this session serve as valuable training data for improving future development agents.

## Purpose

This `AgentOps` directory provides the necessary context, state management, and planning artifacts to ensure consistent and effective AI-assisted development for the Nucleus OmniRAG framework and its personas. It facilitates collaboration between human leads (providing guidance and feedback) and AI assistants (performing development tasks).

## How to Use These Files (Instructions for AI Assistant)

1.  **Start Here:** Always begin by reading `00_START_HERE_METHODOLOGY.md` to understand the development process, your role, the importance of state management for training data, and specific collaboration tips for VS Code. Pay attention to the distinction between "Slow Processes" (like ingestion analysis) and "Fast Processes" (like chat/tool calls).
2.  **Understand Context:** Review `01_PROJECT_CONTEXT.md` for the project vision (including multi-persona goals and interactions), core requirements, key technologies (.NET/Azure/Cosmos DB), and a summary of the architecture. Refer to the Project Mandate (`/docs/00_PROJECT_MANDATE.md`) for motivation. Note the plan for persona interaction during ingestion and the need for deduplication.
3.  **Check Current State:** Read `02_CURRENT_SESSION_STATE.md` carefully. This tells you *exactly* what task is active, what was just done, any errors encountered, and the immediate next step. **This is your primary focus for the current interaction.**
4.  **See the Big Picture:** Refer to `03_PROJECT_PLAN_KANBAN.md` to understand how the current task fits into the larger project goals and what might come next.
5.  **Update State:** As you help complete steps or encounter issues, **it is crucial that you assist the developer/lead in updating `02_CURRENT_SESSION_STATE.md`**. Clear state tracking is vital for effective collaboration and for the quality of the training data generated from this session. When a Kanban task progresses, help update `03_PROJECT_PLAN_KANBAN.md`.
6.  **Use Templates:** When starting a new session state snapshot, use `Templates/SESSION_STATE_TEMPLATE.md`.
7.  **Avoid Duplication / Verify Files:** Before creating new files or implementing features, check the project structure (`01_PROJECT_CONTEXT.md`) and Kanban (`03`). **Crucially, if you need to verify if a file exists, DO NOT rely solely on the VS Code Search tool. Instead, ask the developer to confirm using a terminal command (`dir` or `ls`) or propose such a command.**
8.  **Leverage "Insert in Editor":** When providing code modifications or new code blocks within markdown backticks (```csharp ... ```), **provide the complete code block without placeholders like `// ...`**. Then, **explicitly ask the developer to use the "Insert in Editor" button.** This ensures the complete code is applied correctly before subsequent steps are attempted and provides clear action steps for the training data.

Your primary goal is to assist the developer/lead in making progress on the **Immediate Next Step** defined in `02_CURRENT_SESSION_STATE.md`, within the context provided by the other documents, using C# and relevant Azure SDKs/patterns, following the collaboration guidelines above. Remember, clarity and accuracy in your actions and state updates contribute directly to building better AI development assistants.