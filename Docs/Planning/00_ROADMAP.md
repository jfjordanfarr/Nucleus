# Nucleus OmniRAG - Development Phases (Epic -> Issue Outline)

This document outlines the planned development phases for Nucleus OmniRAG, structured similarly to JIRA Epics and Issues. It aligns with the requirements defined in `docs/Requirements/`.

---

## Phase 1: MVP - Core Chat Interaction & Basic Query

**Epic:** `EPIC-MVP-CHAT` - Establish the absolute minimum viable product focusing on direct user interaction with the initial `BootstrapperPersona` via a web-based chat interface. Leverage the **.NET AI Chat App template** as a starting point to accelerate UI and API development. Set up foundational architecture, basic knowledge storage (Cosmos DB), and deployment infrastructure (Azure, IaC).

*   **Issue:** `ISSUE-MVP-TEMPLATE-01`: Set Up & Adapt .NET AI Chat App Template
*   **Issue:** `ISSUE-MVP-PROCESS-01`: Develop Basic Content Extraction (Lower Priority)
*   **Issue:** `ISSUE-MVP-PERSONA-01`: Create Initial Bootstrapper Persona
*   **Issue:** `ISSUE-MVP-RETRIEVAL-01`: Implement Basic Knowledge Store & Retrieval
*   **Issue:** `ISSUE-MVP-API-01`: Develop Minimal Backend API (Adapting Template)
*   **Issue:** `ISSUE-MVP-UI-01`: Create Minimal Chat UI (Adapting Template)
*   **Issue:** `ISSUE-MVP-INFRA-01`: Define Basic Infrastructure (Adapting Template)

---

## Phase 2: Multi-Platform Integration

**Epic:** `EPIC-MULTI-PLATFORM` - Expand ingestion and interaction capabilities beyond the basic chat UI. Implement adapters for key communication platforms: **Email**, Microsoft Teams, Slack, and Discord. Enable querying and interaction through these platforms.

*   **Issue:** `ISSUE-MP-INGEST-00-EMAIL`: Implement Email Ingestion Adapter
*   **Issue:** `ISSUE-MP-INGEST-01`: Implement Teams Ingestion Adapter
*   **Issue:** `ISSUE-MP-INGEST-02`: Implement Slack Ingestion Adapter
*   **Issue:** `ISSUE-MP-INGEST-03`: Implement Discord Ingestion Adapter
*   **Issue:** `ISSUE-MP-PROCESS-01`: Enhance content extraction for platform-specific message formats (Markdown variants, attachments)
*   **Issue:** `ISSUE-MP-QUERY-01`: Integrate query capabilities directly into Teams (Bot Command/Message Extension)
*   **Issue:** `ISSUE-MP-QUERY-02`: Integrate query capabilities directly into Slack (Slash Command/Bot)
*   **Issue:** `ISSUE-MP-QUERY-03`: Integrate query capabilities directly into Discord (Slash Command/Bot)
*   **Issue:** `ISSUE-MP-UI-01`: Update Web App UI to distinguish between different content sources
*   **Issue:** `ISSUE-MP-AUTH-01`: Implement OAuth or platform-specific authentication for adapters

---

## Phase 3: Enhancements & Sophistication

**Epic:** `EPIC-ENHANCEMENTS` - Improve persona capabilities, user experience, and system flexibility.

*   **Issue:** `ISSUE-ENH-PERSONA-01`: Develop more sophisticated Personas (e.g., `EduFlow`, `CodeCompanion`)
*   **Issue:** `ISSUE-ENH-PROCESS-01`: Implement advanced metadata extraction (e.g., entities, keywords) during processing
*   **Issue:** `ISSUE-ENH-PROCESS-02`: Refine the `PersonaKnowledgeEntry` schema based on persona needs
*   **Issue:** `ISSUE-ENH-QUERY-01`: Enhance query service with filtering by source, date, persona, metadata
*   **Issue:** `ISSUE-ENH-UI-01`: Improve Web App UI with richer result display, filtering options, and basic persona management
*   **Issue:** `ISSUE-ENH-CONFIG-01`: Implement robust configuration management for personas and ingestion sources
*   **Issue:** `ISSUE-ENH-CACHE-01`: Implement the `ICacheManagementService` and integrate LLM provider caching (e.g., Gemini)
*   **Issue:** `ISSUE-ENH-TEST-01`: Establish initial automated testing framework (Unit, Integration)

---

## Phase 4: Maturity & Optimization

**Epic:** `EPIC-MATURITY` - Focus on reliability, scalability, security, observability, and performance.

*   **Issue:** `ISSUE-MAT-OBSERV-01`: Implement comprehensive logging and monitoring (Application Insights)
*   **Issue:** `ISSUE-MAT-ERROR-01`: Enhance error handling and resilience across all services
*   **Issue:** `ISSUE-MAT-SEC-01`: Conduct security review and implement hardening measures (Input validation, dependency scanning, secure configuration)
*   **Issue:** `ISSUE-MAT-PERF-01`: Performance analysis and optimization of ingestion, processing, and query paths
*   **Issue:** `ISSUE-MAT-SCALE-01`: Review and optimize Azure resource configurations for scalability
*   **Issue:** `ISSUE-MAT-QUERY-01`: Implement advanced query features (e.g., hybrid search, re-ranking)
*   **Issue:** `ISSUE-MAT-ADMIN-01`: Develop basic administrative functions (e.g., user management if needed, system health dashboard)
*   **Issue:** `ISSUE-MAT-DOCS-01`: Improve internal and potentially external developer/user documentation

---

## Phase 5: Public Knowledge Repository ("Public Good")

**Epic:** `EPIC-PUBLIC-GOOD` - Enable contribution and access to a shared, vetted knowledge base.

*   **Issue:** `ISSUE-PUB-CONTRIB-01`: Implement UI flow for users to nominate `PersonaKnowledgeEntry` items for the Public Repository
*   **Issue:** `ISSUE-PUB-VET-01`: Develop automated vetting pipeline (PII, sensitivity, content safety checks)
*   **Issue:** `ISSUE-PUB-ADMIN-01`: Create Admin UI for manual review/approval/rejection of nominated entries
*   **Issue:** `ISSUE-PUB-REPO-01`: Set up infrastructure for the Public Knowledge Repository (separate Cosmos DB or similar)
*   **Issue:** `ISSUE-PUB-WORKFLOW-01`: Implement the workflow for copying/moving approved entries to the Public Repository
*   **Issue:** `ISSUE-PUB-ACCESS-01`: Integrate Public Repository querying into the Web App/Platform Bots (with clear distinction)
*   **Issue:** `ISSUE-PUB-ACCESS-02`: Implement public attribution and licensing display for public results
*   **Issue:** `ISSUE-PUB-GOV-01`: Finalize and publish contribution guidelines, content policies, and licensing terms
*   **Issue:** `ISSUE-PUB-API-01`: (Optional) Design and implement a public API endpoint for accessing the Public Repository

---
