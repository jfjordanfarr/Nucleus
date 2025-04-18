---
title: AgentOps - Session State Template
description: Template for tracking the current state of a development session.
version: 1.0
date: 2025-04-18
---

# Nucleus OmniRAG: Current Session State (TEMPLATE)

**Attention AI Assistant & Developer:** This is a **TEMPLATE** for the **MICROSTATE** document. **COPY** this file and rename it (e.g., `02_SESSION_STATE_YYYYMMDD_HHMM.md`) for the **CURRENT SESSION**. Fill in the details below.

Focus your collaborative efforts on the "Immediate Next Step". Update the **current session's copy** frequently. Accuracy is key for training data and continuity.

---

## 🔄 Session Info

*   **Date:** `{{YYYY-MM-DD}}`
*   **Time:** `{{HH:MM UTC+/-Offset}}` *(Approximate time of state update)*
*   **Developer:** `{{Developer Name/Handle}}`

---

## 🎯 Active Task (from Kanban - `03_PROJECT_PLAN_KANBAN.md`)

*   **ID/Name:** `{{TASK-ID: Task Name}}`
*   **Goal:** `{{Briefly describe the goal of the current task}}`

---

## 🔬 Current Focus / Micro-Goal

*   `{{Describe the specific, immediate objective within the Active Task}}`

---

## ⏪ Last Action(s) Taken

*   `{{List the last significant actions completed, including tool calls, edits, or decisions made}}`
*   `{{...}}`

---

## ❗ Current Error / Blocker (if any)

*   `{{Describe any errors, unexpected results, or obstacles preventing progress}}`
*   `{{If none, state "None"}}`

---

## ▶️ Immediate Next Step

1.  `{{Specify the very next action to be taken}}`
2.  `{{(Optional) Subsequent action if clear}}`

---

## ❓ Open Questions / Verification Needed

*   `{{List any questions requiring clarification or decisions needed}}`
*   `{{What needs to be verified before proceeding?}}`

---

## </> Relevant Code Snippet(s) / Files

*   **File:** `{{Path to relevant file}}`
*   **Code Item:** `{{Namespace.Class.Method (if applicable)}}`
*   `{{...}}`
