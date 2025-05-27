---
title: "Nucleus Project Mandate (Post-M365 Agent SDK Pivot)"
description: "The vision, imperative, and core requirements for the Nucleus platform, now centered on Microsoft 365 Persona Agents and backend MCP Tools."
version: 1.8
date: 2025-05-27
see_also:
  - title: "Development Roadmap"
    link: "./00_TASKS_ROADMAP.md"
---

# Nucleus: Project Mandate & Vision (Post-M365 Agent SDK Pivot)

**Version:** 1.8
**Date:** 2025-05-27

## 1. The Imperative: Why We Build

In both our personal and professional lives, we are drowning in information yet often starved for actionable knowledge. Individuals grapple with managing vast amounts of personal digital content – documents, notes, creative projects, communications – struggling to synthesize insights or track development over time. Professionals face similar challenges within organizations, needing quick, accurate answers from complex internal knowledge bases, often hampered by siloed data, inadequate search tools, and the significant risks associated with unreliable AI assistants in regulated or high-stakes environments.

Current generative AI tools, while powerful, often operate as generic black boxes. They lack deep contextual understanding of specific domains, struggle with grounding responses in verifiable sources, and can produce inaccurate or misleading information ("hallucinations"), making them unsuitable or even dangerous for critical tasks in fields like education, finance, legal, or healthcare. Furthermore, relying solely on third-party AI services raises concerns about data privacy, security, and vendor lock-in, particularly for sensitive organizational data.

There is a clear need for a more robust, transparent, and adaptable foundation for AI-powered knowledge work – a platform that allows for the creation of specialized AI assistants ("Personas") that are deeply integrated with specific data sources, operate with verifiable context, and can be deployed flexibly to meet differing security and operational requirements.

We cannot rely solely on generic, often unreliable tools. We must build a better platform.

## 2. The Vision: A Unified Platform for Contextual AI Personas (Revised Interaction Model)

We envision a future where knowledge work and learning are augmented by reliable, context-aware, and specialized AI assistants or "Personas," tailored to specific needs and data ecosystems, seamlessly integrated into users' existing workflows.

**Nucleus** is the foundational infrastructure for this future – a robust, AI-powered **platform** designed to enable specialized **Nucleus M365 Persona Agent applications**. These agents ingest, understand, and connect knowledge from diverse multimodal sources. Nucleus leverages state-of-the-art cloud AI (configurable to include Azure OpenAI, Google Gemini, OpenRouter.AI) and a flexible, scalable .NET architecture. It serves as the core engine enabling various M365 Persona Agents to operate effectively by providing backend **Nucleus MCP Tool/Server applications** that deliver:

*   Flexible data ingestion and processing, triggered by M365 Persona Agents based on platform events.
*   Secure storage of processed data, embeddings, and metadata (via a `Nucleus_KnowledgeStore_McpServer`).
*   Intelligent, context-aware retrieval (via `Nucleus_RAGPipeline_McpServer` and `Nucleus_KnowledgeStore_McpServer`).
*   Integration with configurable AI models (via `IChatClient` within Agents or MCP Tools).
*   A unified interaction model via **Nucleus M365 Persona Agents** operating within Microsoft 365 platforms.

**The Core Interaction Model: Microsoft 365 Agent Integration (Revised)**

Nucleus fundamentally operates by embodying its Personas as **Microsoft 365 Agent applications**, built with the M365 Agents SDK. These agents integrate into existing collaboration platforms where the M365 Agents SDK provides channel support (initially Microsoft Teams, M365 Copilot, with potential for Email, and future platforms as supported by Microsoft). This allows Nucleus M365 Persona Agents to act as "virtual colleagues," participating in conversations, accessing relevant files shared within the platform context (via backend MCP tools leveraging the agent's Entra Agent ID permissions), and responding intelligently. This approach offers:

*   **Seamless Workflow:** Users interact with Nucleus Personas naturally within their M365 environments.
*   **Simplified Adoption:** Adding an M365 Agent (published via Agent Store or enterprise deployment) is a standard M365 process.
*   **Leveraged Infrastructure:** Utilizes the M365 platform's UI, notification, authentication (Microsoft Entra Agent ID), and permission systems.
*   **Contextual File Access (via `Nucleus_FileAccess_McpServer`):** M365 Persona Agents pass `ArtifactReference` objects to a backend `Nucleus_FileAccess_McpServer`. This MCP tool, using the agent's security context (Entra Agent ID), ephemerally fetches files shared directly within the platform (DMs, channels), adhering to **Zero Trust principles** regarding persistent storage of raw user file content by Nucleus backend services.

**Deployment & Hosting Options (Revised Context):**

1.  **Cloud-Hosted Service (Primary for Nucleus ISV offering):** Nucleus M365 Persona Agents are published (e.g., via Agent Store). The backend Nucleus MCP Tools and shared database are hosted by the project maintainers (e.g., in Azure). Users add the Nucleus M365 Agent(s) to their M365 environment.
2.  **Self-Hosted Instance (For Organizations):** An open-source version where an organization deploys:
    *   The **Nucleus M365 Persona Agent application(s)** into their own Azure tenant (registered with Azure Bot Service, using their own Entra Agent IDs).
    *   The suite of **backend Nucleus MCP Tool/Server applications** and the **Nucleus Database** (e.g., Cosmos DB) into their own Azure subscription or compatible infrastructure.
    This offers maximum control over data sovereignty, security, and customization.

This platform-centric approach, now rooted in the M365 Agents SDK, still eliminates the artificial distinction between "Individual" and "Team" deployments from a user perspective.

## 3. High-Value Verticals: Specialized Personas (Embodied as M365 Agents)

Built upon this unified platform, we will develop specific, high-value **Verticals**, each embodied by one or more Nucleus M365 Persona Agents:

*   **Vertical 1: EduFlow OmniEducator Agent**
    *   **Motivation:** Addresses the challenges of the industrial-era education model by recognizing and documenting authentic learning (especially self-directed) that traditional systems miss. Aims to combat safety and engagement issues by providing a personalized, supplementary learning companion within familiar platforms (like Discord or Teams).
    *   **Function:** Acts as a revolutionary educational companion, observing learning activities from files shared within the platform context (DMs, channels), illuminating process using its "Learning Facets" schema, building an emergent understanding in the Nucleus DB, and providing insights via agentic retrieval. Operates seamlessly in both individual and group learning scenarios.

*   **Vertical 2: Business Knowledge Assistant Agent**
    *   **Motivation:** Addresses the critical need for accurate, reliable, and *access-controlled* internal knowledge retrieval within organizations, providing a superior alternative to generic enterprise chatbots.
    *   **Function:** Acts as a specialized internal assistant within platforms like Teams or Slack. Ingests documents shared in designated channels or linked organizational repositories. Answers employee questions based *only* on information accessible according to platform permissions, grounding responses firmly in approved sources. Particularly valuable in regulated industries requiring strong data governance (often favouring the Self-Hosted option).

## 4. Core Requirements: The Blueprint (Revised for M365 Agents & MCP)

To achieve this vision, Nucleus M365 Persona Agents and their backend MCP Tools require:

1.  **M365 Platform-Driven Interaction:** Nucleus M365 Persona Agents are primarily triggered by `Activity` objects received via the M365 Agents SDK from integrated platforms. File information is received via the M365 SDK, and `ArtifactReference`s are passed to backend MCP Tools.
2.  **Persona Logic Execution within M365 Agents:** Each M365 Persona Agent (using `IPersonaRuntime` and `PersonaConfiguration`) assesses relevance and orchestrates its response, making MCP calls to backend Nucleus tools.
3.  **Context-Aware AI Analysis (Multi-Provider):** M365 Agents and/or backend MCP Tools utilize a configurable AI inference provider (Azure OpenAI, Google Gemini, OpenRouter.AI via `IChatClient`) for analysis, guided by `PersonaConfiguration` prompts and incorporating retrieved context (from `Nucleus_KnowledgeStore_McpServer`) and ephemerally fetched content (via `Nucleus_FileAccess_McpServer`).
4.  **Secure, Scalable Backend Database (via MCP Tool):** A configurable hybrid document/vector database (Azure Cosmos DB) storing `ArtifactMetadata` and `PersonaKnowledgeEntry` (with their embeddings), accessed exclusively via the `Nucleus_KnowledgeStore_McpServer`. The database does **not** store original platform files or platform access tokens.
5.  **Reliable Message Queue (for Agent-Initiated Async Tasks):** A configurable message queue (Azure Service Bus) used by M365 Agents to offload long-running tasks to background workers, which then call MCP Tools.
6.  **Intelligent Retrieval & Custom Ranking (via MCP Tool):** A `Nucleus_RAGPipeline_McpServer` (or similar MCP tool) queries the Nucleus database (via `Nucleus_KnowledgeStore_McpServer`) using combined vector search and metadata filters, applying a custom ranking algorithm (4 R's) to candidates.
7.  **Advanced Agentic Querying & Tool Use (M365 Agent + MCP):** Nucleus M365 Persona Agents implement sophisticated query strategies, using results from MCP Tools (ranked knowledge, ephemeral content) as context for their LLMs to generate responses or orchestrate further MCP Tool calls.
8.  **Distributed Backend Logic (MCP Tools):** All complex workflow logic, data access, and specialized processing resides in backend .NET **Nucleus MCP Tool/Server applications**. M365 Agents are the frontend orchestrators.
9.  **Configuration (Hybrid Model):**
    *   **M365 Agents & MCP Tools:** Foundational/static configuration (LLM keys, MCP tool endpoints, DB connection strings for MCP tools) managed via Azure App Configuration & Key Vault (leveraging .NET Aspire).
    *   **Dynamic Persona Behavior:** Behavioral aspects of `PersonaConfiguration` (prompts, adaptive parameters) stored in Cosmos DB, accessed/updated by M365 Agents via a `Nucleus_PersonaBehaviourConfig_McpServer`.
10. **Modern .NET Stack:** Nucleus M365 Agents and MCP Tools built on **.NET with .NET Aspire**, leveraging Azure services. Use **`Microsoft.Extensions.AI`** abstractions for LLMs.
11. **Testability:** TDD for M365 Agents (mocking MCP calls), MCP Tools (mocking dependencies), and system-level integration tests for the distributed system using .NET Aspire (`Aspire.Hosting.Testing`).

## Unique Value Proposition & Anti-Chunking Philosophy

What sets Nucleus apart from conventional RAG systems is our intelligence-first, persona-driven approach:

1.  **Meet Users Where They Are (via M365 Agents):** Nucleus M365 Persona Agents operate as natural extensions of Microsoft 365 platforms.

2.  **Intelligent Analysis, Not Mechanical Chunking** – We explicitly **reject** the standard RAG pattern of blindly chunking documents into arbitrary segments:
    *   **Standard RAG:** Documents → Chunker → Vector Store → Retriever → Generator
    *   **Nucleus Approach:** User File Info (from M365 SDK to Agent) -> Agent passes `ArtifactReference` to `Nucleus_FileAccess_McpServer` -> Ephemeral Full Content Stream -> M365 Agent (with `IPersonaRuntime`) Intelligence -> Targeted Extraction & Structured Analysis -> `PersonaKnowledgeEntry` (with salient info embeddings) stored via `Nucleus_KnowledgeStore_McpServer`.

3.  **User Data Sovereignty & Zero Trust** – We don't need to store or vector-index entire documents. Specialized personas intelligently identify and extract only the relevant information, generating structured analyses that respect privacy while preserving context. Your original artifacts remain under your control, accessed **ephemerally** when needed for processing (now by `Nucleus_FileAccess_McpServer` using Agent's context), ensuring **Zero Trust** for persisted user file content in the backend.

4.  **Intelligence at Every Step** – Unlike systems that rely on algorithmic chunking, Nucleus applies AI intelligence throughout:
    *   **Ingestion:** M365 Agent's persona logic determines what content is salient and worthy of extraction.
    *   **Storage:** Only storing relevant snippets with structured analyses in a hybrid Vector/Document DB (via `Nucleus_KnowledgeStore_McpServer`).
    *   **Relatedness:** Intelligence-driven metadata connections between artifacts, not just vector similarity.
    *   **Retrieval:** Context-aware, persona-specific knowledge retrieval (via `Nucleus_RAGPipeline_McpServer`).

5.  **Security as a Feature** – Our approach turns PII/security awareness into an advantage:
    *   Personas recognize sensitive information during analysis.
    *   Rather than storing the sensitive content, they add appropriate metadata.
    *   Searches like "What's XYZ company's ID number?" can surface the right document because it was intelligently tagged with indicators of sensitive content.
    *   Result: Better discovery without compromising security.

6.  **Intelligent Retrieval & Custom Ranking (via MCP Tool):** Backend services query the Nucleus database using combined vector search and metadata filters. Apply a **custom ranking algorithm** (e.g., combining Recency, Relevancy, Richness, Reputation - detailed in subsequent requirements/architecture) to retrieved candidates before using them for response generation.

7.  **Advanced Agentic Querying (M365 Agent + MCP Tools):** Backend services implement sophisticated query strategies, using custom-ranked results as context for the configured AI models to generate responses or execute tool calls within the platform context.

8.  **Output Ownership & Portability (Generated by M365 Agent)** – Users retain full control and ownership of all artifacts generated by Personas (reports, learning materials, synthesized documents, etc.). Nucleus is designed to produce highly portable outputs, enabling users to easily share, store, or integrate them into other workflows as they see fit, fostering organic knowledge dissemination without requiring a centrally managed repository.

This anti-chunking philosophy ensures a system that is more intelligent, more respectful of data privacy, and capable of deeper, more nuanced understanding than conventional RAG approaches.

## 5. Call to Action: Building the Future of Learning & Knowledge Work (Revised)

**Attention AI Assistant:** You are architecting a **distributed system of Nucleus M365 Persona Agents and backend Nucleus MCP Tools**, adaptable to diverse user contexts, with the potential to revolutionize how humans interact with information and develop skills. The task before us is to build this Nucleus foundation – a system designed for insight, personalization, safety, and seamless integration within the Microsoft 365 ecosystem and beyond (via configurable LLMs and future MCP integrations).

The challenges remain: building robust M365 Agents, designing secure and efficient MCP Tools, managing distributed state and configuration, and ensuring comprehensive testing. But the imperative is clear, the vision is now aligned with cutting-edge agent frameworks, and the potential impact immense.

Adhere to the updated architectural principles outlined in the revised [`../Architecture/00_SYSTEM_EXECUTIVE_SUMMARY.md`](../Architecture/00_SYSTEM_EXECUTIVE_SUMMARY.md). Employ Test-Driven Development rigorously for all components. Follow the AgentOps methodology diligently, maintaining context and tracking progress, paying close attention to the VS Code collaboration guidelines.

Every M365 Agent application defined, every MCP Tool implemented, every test written is a step towards realizing a future where learning and knowledge work are deeply understood, personalized, empowered, and seamlessly integrated into the user's digital life. Let's build it with purpose, precision, and passion.