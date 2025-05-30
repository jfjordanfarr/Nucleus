---
title: AgentOps - README
description: Overview and entry point for the AgentOps directory, guiding AI assistants on how to use the supplementary context and state management files for the Nucleus project.
version: 2.0
date: 2025-05-28
---

# AgentOps for Nucleus (Microsoft 365 Agents SDK / MCP / .NET 9 / Aspire / Azure Cosmos DB)

**Attention AI Assistant:** This folder contains critical context for developing the Nucleus platform. Your primary guide for behavior, tool usage, and core development principles is the **`.github/copilot-instructions.md`** file located in the project root. This `AgentOps` directory provides supplementary project-specific context and state.

Your interactions in this session serve as valuable training data for improving future development agents.

## Purpose

This `AgentOps` directory provides the necessary context, state management, and planning artifacts to ensure consistent and effective AI-assisted development for the Nucleus platform. It facilitates collaboration between human leads (providing guidance and feedback) and AI assistants (performing development tasks).

## How to Use These Files (Instructions for AI Assistant)

1.  **Consult Rules First:** Always begin by reviewing the project's **`.github/copilot-instructions.md`** file for fundamental guidelines on quality, assumptions, documentation, tool usage, and agentic behavior.
2.  **Understand Methodology:** Next, read `00_START_HERE_METHODOLOGY.md` for supplementary details on the development process, state management, and collaboration, noting it is supplemented by the TRACS pass methodology and the AI-AI-Human (Long Context Gemini, GitHub Copilot, Human Lead) collaborative framework we are currently employing.
3.  **Understand Context:** Review `01_PROJECT_CONTEXT.md` for the project vision, core requirements, key technologies (**Microsoft 365 Agents SDK, Model Context Protocol (MCP)**, .NET 9/Aspire, Azure Cosmos DB), and architecture summary.
4.  **Check Current State:** Read `02_CURRENT_SESSION_STATE.md` carefully. This tells you *exactly* what task is active, what was just done, any errors encountered, and the immediate next step. **This is your primary focus for the current interaction.**
5.  **Follow Naming Conventions:** Adhere to the established naming conventions for files and directories within this project (details TBD, assume consistency with existing structures for now).
6.  **Log Actions in Session State:** Record significant actions, decisions, and discoveries in `02_CURRENT_SESSION_STATE.md`. This file is managed directly by the LLM and is critical for maintaining context across interactions and for generating accurate training data.
7.  **Report Issues/Blockers:** If you encounter any issues, blockers, or inconsistencies, document them in `02_CURRENT_SESSION_STATE.md` and bring them to the USER's attention if they impede progress.
8.  **Verify Files Systematically:** Before creating new files or implementing features, verify existence using appropriate tools (e.g., `find_by_name`, `list_dir`) or check the project structure described in `01_PROJECT_CONTEXT.md`. Avoid assumptions.
9.  **Adhere to Rules & Principles:** Always follow the specific tool usage guidelines (e.g., for `edit_file`) and core principles (Quality over Expedience, Documentation as Source Code, Ephemeral Processing, **Distributed System of M365 Agents & MCP Tools**, Persona-Centric Design) detailed in `.github/copilot-instructions.md`.

Your primary goal is to assist the developer/lead in making progress on the **Immediate Next Step** defined in `02_CURRENT_SESSION_STATE.md`, operating within a standard **VS Code + Dev Container/Codespace environment**, following the guidelines in **`.github/copilot-instructions.md`** and the context provided by these `AgentOps` documents, under the guidance of the Long Context Gemini session and Human Lead.