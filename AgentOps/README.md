# AgentOps for Nucleus (.NET 9 / Aspire / Azure Cosmos DB)

**Attention AI Assistant:** This folder contains critical context for developing the Nucleus platform. Your primary guide for behavior, tool usage, and core development principles is the `.windsurfrules` file located in the project root. Review it thoroughly before proceeding. These `AgentOps` documents provide supplementary project-specific context, state, and planning.

Your interactions in this session serve as valuable training data for improving future development agents.

## Purpose

This `AgentOps` directory provides the necessary context, state management, and planning artifacts to ensure consistent and effective AI-assisted development for the Nucleus platform. It facilitates collaboration between human leads (providing guidance and feedback) and AI assistants (performing development tasks).

## How to Use These Files (Instructions for AI Assistant)

1.  **Consult Rules First:** Always begin by reviewing the project's `.windsurfrules` file for fundamental guidelines on quality, assumptions, documentation, tool usage, and agentic behavior.
2.  **Understand Methodology:** Next, read `00_START_HERE_METHODOLOGY.md` for supplementary details on the development process, state management, and collaboration, noting it may be partially superseded by `.windsurfrules`.
3.  **Understand Context:** Review `01_PROJECT_CONTEXT.md` for the project vision, core requirements, key technologies (.NET 9/Aspire/Cosmos DB), and architecture summary.
4.  **Check Current State:** Read `02_CURRENT_SESSION_STATE.md` carefully. This tells you *exactly* what task is active, what was just done, any errors encountered, and the immediate next step. **This is your primary focus for the current interaction.**
5.  **See the Big Picture:** Refer to `03_PROJECT_PLAN_KANBAN.md` to understand how the current task fits into the larger project goals.
6.  **Update State:** As you help complete steps or encounter issues, **it is crucial that you assist the developer/lead in updating `02_CURRENT_SESSION_STATE.md`**. Clear state tracking is vital. When a Kanban task progresses, help update `03_PROJECT_PLAN_KANBAN.md`.
7.  **Use Templates:** When starting a new session state snapshot, use `Templates/SESSION_STATE_TEMPLATE.md`.
8.  **Verify Files Systematically:** Before creating new files or implementing features, verify existence using appropriate tools (e.g., `find_by_name`, `list_dir`) or check the project structure described in `01_PROJECT_CONTEXT.md` and the Kanban (`03`). Avoid assumptions.
9.  **Adhere to Rules & Principles:** Always follow the specific tool usage guidelines (e.g., for `edit_file`) and core principles (Quality over Expedience, Documentation as Source Code, Ephemeral Processing, Modular Monolith, Persona-Centric Design) detailed in `.windsurfrules`.

Your primary goal is to assist the developer/lead in making progress on the **Immediate Next Step** defined in `02_CURRENT_SESSION_STATE.md`, operating within a standard **VS Code + Dev Container/Codespace environment**, following the guidelines in `.windsurfrules` and the context provided by these `AgentOps` documents.