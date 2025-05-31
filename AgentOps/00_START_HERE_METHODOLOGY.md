---
title: AgentOps - Nucleus Development Methodology
description: Supplementary AgentOps methodology for AI-assisted development within the Nucleus project.
version: 1.4
date: 2025-05-28
---

# AgentOps Methodology for Nucleus (.NET 9 / Aspire / Azure Cosmos DB Backend)

## Introduction

This document outlines supplementary AgentOps methodology for AI-assisted development used in the Nucleus project. **The primary source of specific rules and guidelines for the AI development assistant (currently GitHub Copilot) is the `.github/copilot-instructions.md` file in the project root.** This document provides additional project-specific context and workflow details.

Following this process helps maintain context, track progress, and ensure efficient collaboration between human developers/leads and AI assistants within the .NET/Azure ecosystem, specifically targeting Azure Cosmos DB as the backend. This methodology also serves as a framework for generating high-quality training data for future AI development agents.

## Core Principles (Supplementary to `.github/copilot-instructions.md`)

**(Review the foundational principles like Quality over Expedience, No Assumptions, Documentation Rigor, Tool Usage, Persona-Centric Design, and Ephemeral Processing defined in [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) first.)**

1.  **Stateful Context Management**: Using dedicated documents (`01_PROJECT_CONTEXT.md`, `02_CURRENT_SESSION_STATE.md`, `../Docs/ProjectExecutionPlan/00_REQUIREMENTS_PROJECT_MANDATE.md`) to preserve context for all collaborators (Human, Long Context Gemini, GitHub Copilot). Accurate state tracking is paramount for effective collaboration and training data generation.
2.  **Incremental Progress Tracking**: Breaking work into manageable tasks and tracking the immediate focus (Session State).
3.  **Structured Collaboration**: AI assistants use the provided state documents to understand the current focus and assist with the defined "Next Step", following specific interaction patterns. Human leads provide guidance, feedback, and validation.
4.  **Continuous Documentation**: Updating state documents in real-time as progress is made or issues arise. **AI assistance in keeping these updated is expected and crucial for training data quality.**
5.  **Architectural Adherence**: Development must align with the established architectural patterns and component responsibilities defined in the project's architecture documents (see [`../Docs/Architecture/`](../Docs/Architecture/)). Emphasize SOLID principles, Dependency Injection, and asynchronous programming (`async`/`await`).
6.  **Test-Driven Development (TDD):** Aim to write tests (Unit, Integration) before or alongside implementation where practical. Define test cases as part of task definitions.
7.  **Distinguish Process Types:** Be mindful of the difference between "Slow Processes" (asynchronous, potentially long-running tasks like ingestion analysis) and "Fast Processes" (low-latency operations like chat responses or tool calls). Design components appropriately.
8.  **Deduplication:** Recognize that mechanisms to prevent duplicate processing or storage of the same source artifact (based on `ArtifactMetadata` or similar) are necessary, especially during ingestion. (See [Storage Architecture](../Docs/Architecture/CoreNucleus/03_DATA_PERSISTENCE_STRATEGY.md)).
9.  **TRACS Methodology for Document Reviews**: From time to time, especially during intensive documentation passes, a systematic review process called TRACS may be employed. If the `02_CURRENT_SESSION_STATE.md` indicates `Agent Mode: TRACS_PASS_MODE`, this is the active methodology for the documents under review. TRACS stands for:
    *   **T**ransform: Significant content modifications to align with new architectures, requirements, or insights.
    *   **R**ealign: Moving files, renaming them, or adjusting their frontmatter links (like `parent` or `see_also`) to improve structural integrity and navigation.
    *   **A**rchive: Setting aside outdated documents, clearly marking them as superseded to preserve historical context while keeping current documentation clean.
    *   **C**onsolidate: Merging information from multiple sources into a more coherent whole, or ensuring key concepts from smaller, archived documents are adequately covered in primary documents.
    *   **S**olidify: Confirming that a document is largely correct and primarily needs minor touch-ups, such as verifying link accuracy, ensuring terminological consistency, correcting grammar/typos, or updating version/date information.

## Roles of Key AgentOps Files

**(Refer to `AgentOps/README.md` for the recommended reading order)**

*   **`.github/copilot-instructions.md` (Project Root):** The primary authority for GitHub Copilot's behavior, core principles, and tool usage.
*   **`[/Docs/Requirements/00_PROJECT_MANDATE.md](../Docs/Requirements/00_PROJECT_MANDATE.md)`**: The "Why". Understand the motivation and high-level vision.
*   **`00_START_HERE_METHODOLOGY.md` (This file):** Supplementary workflow details.
*   **`01_PROJECT_CONTEXT.md`**: The "What". Provides stable, high-level technical context: goals, **Microsoft 365 Agents SDK, MCP,** .NET 9/Aspire, Azure Cosmos DB tech stack, architectural summary, links to detailed docs.
*   **`02_CURRENT_SESSION_STATE.md`**: The "Now". Captures the **microstate**. Dynamic and updated frequently. Details the *specific task*, *last action*, relevant *code*, *errors/blockers*, and the *immediate next step*. **This is your primary focus.**
*   **`03_AGENT_TO_AGENT_CONVERSATION.md`**: **[ARCHIVED/DEPRECATED]** Previously used for structured communication with other AI models (e.g., external Gemini calls). This workflow is now generally handled by the Human Lead or Long Context Gemini directly providing context or instructions.

## Workflow Process

1.  **Session Start:** Developer/Lead shares `02_CURRENT_SESSION_STATE.md` (and potentially others) with the AI. AI reviews state, context (`01`, Mandate, `.github/copilot-instructions.md`), and plan.
2.  **Task Execution:** Focus on the **Immediate Next Step** defined in `02_CURRENT_SESSION_STATE.md`. GitHub Copilot assists with file modifications, C# code generation, debugging, analysis, Azure SDK usage, test generation, etc., **following the guidelines in `.github/copilot-instructions.md`**.
3.  **Code Modification:** Use the `edit_file` tool according to the specific guidelines in `.github/copilot-instructions.md` (e.g., using placeholders `{{ ... }}`, precision, atomicity).
4.  **Check Current State:** Read `02_CURRENT_SESSION_STATE.md` carefully. This tells you *exactly* what task is active, what was just done, any errors encountered, and the immediate next step. **This is your primary focus for the current interaction.**
5.  **See the Big Picture (If Applicable):** If a broad understanding of project progression is needed, you might be directed to task tracking or planning documents. Project progression and high-level planning are primarily tracked in the `Docs/ProjectExecutionPlan/` documents (e.g., `00_TASKS_ROADMAP.md`).
6.  **State Update:** As you help complete steps or encounter issues, **it is crucial that you (GitHub Copilot) accurately update `02_CURRENT_SESSION_STATE.md` as the first action ('Step Zero') of your turn.** Clear state tracking is vital.
7.  **Documentation:** Apply documentation rigor as specified in `.github/copilot-instructions.md` (metadata, cross-linking).

## Nagging Thoughts / Best Practices (To Consider Alongside `.github/copilot-instructions.md`)

This section offers reminders and best practices to consider alongside the primary guidelines in `.github/copilot-instructions.md`.

1.  **Correctness & Alignment:** Ask: "Does this change align with `01_PROJECT_CONTEXT.md`, relevant architecture docs (`/Docs/Architecture`), and the core principles in `.github/copilot-instructions.md`?"
2.  **Avoid Technical Debt:** Prioritize the "right way" (e.g., using proper Microsoft extensions, Aspire patterns) over quick fixes. Adhere to the "Quality over Expedience" rule. Research proper implementation patterns when needed.
3.  **Robust Solutions:** Ask: "Is this robust? Does it align with SOLID, use DI correctly, handle `async`/`await` properly, and use Azure SDKs according to best practices?"
4.  **Simplicity & Clarity:** Ask: "Is this the clearest and simplest C# code? Are interfaces used effectively?"
5.  **Use Tools Correctly:** Ensure correct tool usage (`run_command` with `Cwd`, `edit_file` with placeholders, appropriate search tools) as defined in `.github/copilot-instructions.md`.
6.  **Update State Documents:** (Reinforced) Remind/propose updates for `02_CURRENT_SESSION_STATE.md`.
7.  **Consider Multi-Persona Interaction:** How might this component be used by different personas? Does the data model support persona-specific insights?
8.  **Slow vs. Fast Processes:** Is this part of ingestion (slow, async) or query handling (fast, sync API)?
9.  **Deduplication:** During ingestion, ask: "How will we prevent processing the same artifact multiple times?"

## Session Management and State (`02_CURRENT_SESSION_STATE.md`)

This file is the **single source of truth for the current session's activity**. It includes:
*   The overall objective for the session.
*   A checklist of files being reviewed/worked on.
*   The specific file currently under review.
*   A log of actions taken, decisions made, and errors encountered (newest first).
*   The **Immediate Next Step** to be taken.

Before doing *anything else*, an AI assistant must read this file to understand the current context. After *any* action (file edit, search, analysis, tool use), the assistant must help update this file accurately. This includes noting if a planned step was successful, failed, or needs to be re-scoped.

### Initializing a New Session

By adhering to this methodology, the `.github/copilot-instructions.md`, and keeping these thoughts in mind, you will significantly contribute to the successful development of Nucleus and help train more effective AI development assistants.