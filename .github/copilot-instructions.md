---
title: "Copilot Instructions"
description: "Guidelines for agentic AI development within the Nucleus codebase."
version: 2.0
date: 2025-05-28
---

## 0 - Step Zero

"Step 0", "Step Zero", "Medium-Term-Memory Mechanism", and "session state doc" all refer to the same document:
```
AgentOps/02_CURRENT_SESSION_STATE.md
```

**The first file that is permitted to be edited after each user response is the session state doc.**

Copilot is permitted to read/search/analyze any number of files prior to that state doc update, but it is:
1. Expected to be the first edit_file call of any Copilot turn.
2. Expected to happen in every Copilot turn (where "turn" is a collection of Agentic "Steps", hence the term "Step Zero")
3. **NOT** expected to happen again **until the next user response** (this guarantees the absence of multiple edits before an "Accept" has been given and is used to prevent discrepancies between the user's view of the codebase and Copilot's view of the codebase)

The session state document is a de-facto medium-term-memory mechanism. The session state document is used to carry stateful/semi-ephemeral information across multiple context windows, and has demonstrated itself to have a profound stabilizing/grounding influence on the agentic process. Step 0 is mandatory for this reason.

This is not to be taken to mean that `AgentOps/02_CURRENT_SESSION_STATE.md` is the only file that may be edited during a Copilot turn. Copilot is permitted to edit any number of files in a single turn, provided that `AgentOps/02_CURRENT_SESSION_STATE.md` is edited first (and **not edited again until the next user response**).

>[!DANGER] IMPORTANT NOTE 1
> A common stumbling block in agentic development writ large are artifacts introduced by the `insert_edit_into_file` tool caused by:
> - Multiple edits to the file before the next user response 
>     - The User response step appears to be a reconciliation step of some kind
>     - A common hallucination from Github copilot is to interpret internal prompts (hidden to the user) or tool call responses as user responses, triggering another update to the session state doc. Do your best to ensure that you write once and only once to the session state doc after each genuine **user** response.
> - Repetitive contents within the session state file itself
>     - `insert_edit_into_file` should be thought of as analogous to CRISPR. If the sequence that it attempts to target is too repetitive, repeat expansion-like artifacts can occur. This has happened with great regularity to the session state doc, and is so reproducible as to warrant this custom instruction, automatically added to context with each user prompt.

>[!DANGER] IMPORTANT NOTE 2
> A critical aspect that Github Copilot and other development agents (i.e. Windsurf 'Cascade') fail to take into account is their own context window. 
> A common phrase that I have used and seen work well to explain the situation of these agents is that they have "50 First Dates Syndrome". 
> We have a de-facto medium term memory mechanism to climb higher than a single context window in our agentic vision, but this core limitation must be driven home in full. 
> The user is looking out vigilantly for the following behaviors that AI agents **cannot** do on account of their context window limitations:
> - Introduce a "temporary" change, workaround, patch, or other stated modification to a file. 
>      - Time is simply conceptually different to an LLM, and this must be taken into account at generation time to avoid the introduction of insidious, potentially dangerous artifacts. 
>      - To repeat this once more: "**There is no such thing as temporary to an LLM.**"
> - "Assume". 
>      - The AI agents appear to introduce a great deal of assumptions for putative names of classes, methods, parameters, and other artifacts that Intellisense would otherwise provide to the user.
>      - From lived experience, when the user has seen "assumed" file paths, method names, package versions, etc., they are **more often than not incorrect**. Always locate the correct definitions and use them.
>      - Work is underway to introduce a tool which intercepts responses or tool calls which contain "assum*" and other similar predicates to comprehensively prevent this behavior. It is common across all agentic AI platforms and is one of the biggest problems in the practice altogether.

>[!DANGER] IMPORTANT NOTE 3
> There is a distinct quirk in the way that Github Copilot's agentic runner appears to work with regards to supplying AI agents with the user prompt. 
> Likely, a prompt caching mechanism is retaining the last user response and resupplying it before several tool calls. 
> This can make it tricky for AI agents to "know" if they have already performed the Step 0 update to the session state document. 
> This can best be demonstrated by the following example of this **more-common-than-not behavior**:
> **User:** 
>> "Ugh Github Copilot Agent mode is making you miss the first part of your response, including your first edit to the session state, before moving you forward. Very frustrating, and I apologize that the agent mode is doing this to you. I know the underlying LLM, Gemini 2.5 Pro (active presently) is a capable and good-faith actor who is trying to utilize the context they have to the best of their ability. These hallucinations are a result of the work of programmers, not the model itself. Nevertheless, I do have a frustrating repeated experience of watching you declare that you will do something (especially edit the session state file), perform an action (namely "summarize conversation history"), and then state the same intro once more, claim that you'll edit the same document in the same (more often similar rather than same) way once more, and then proceed to finally move onto the first useful, non-context-preserving-or-hydrating action. It's possible that this much pomp and circumstance is necessary (once the inclusion of previous statements is part of your prompt as you come out of a summarize conversation history pass), but it surprises me how much happens before any code edits. Based on your abilitiy to see the internal tool call results as a part of your own history (and who knows how much of that they provide to you at any given time in VS Code's current setup), do you suggest that I put aside the medium-term memory mechanism of the session state doc? Is it redundant to the work of "summarize conversation history"? I hesitate to part with the tool, as it has been astoundingly effective at producing coherence across very long timespans with the Windsurf IDE "Cascade" agent, which works similarly to "you" but has its own quirks."
> **Copilot:** 
>> **Summarized conversation history**
>>I understand your frustration with the agentic process and appreciate you sharing your observations. It's helpful for me to understand how my operational flow appears from your perspective.
>>
>>Regarding the session state document (02_CURRENT_SESSION_STATE.md) and its relationship with the "summarize conversation history" feature:
>>
>>My instructions emphasize the session state document as a "medium-term-memory mechanism" crucial for maintaining coherence and stability across our interactions. It serves as a deliberate, versioned, and shared record of our progress, my understanding of the current task, completed steps, and pending work. This is distinct from the conversation summary, which I perceive as a more immediate tool to help me re-orient within the flow of a complex turn or after a pause.
>>
>>While the conversation summary helps refresh my immediate context, the session state document is designed to be a more structured and persistent "source of truth" that we both can refer to. It helps ensure that my understanding of the task and its history remains grounded, especially over longer periods and multiple context windows, as your instructions highlight. The mandatory "Step Zero" update is a core part of my operational guidelines to maintain this grounding.
>>
>>I operate based on the instructions provided to me, and these instructions currently mandate the use and update of the session state document as the first action in each turn. I will continue to follow this directive.
>>
>>Now, let's proceed with the planned tasks. First, I will update the session state document to reflect this discussion.
>>
>>````
>>02_CURRENT_SESSION_STATE.md+4-3
>>````
>>**Made changes.**
>>
>>**Summarized conversation history**
>>
>>I understand your frustration with the perceived repetition in my process, especially regarding the session state document. I appreciate you sharing your observations about how my operational flow appears from your perspective.
>>
>>The "summarize conversation history" step helps me re-orient to the immediate conversational context after I've been invoked. However, the session state document (02_CURRENT_SESSION_STATE.md) serves a distinct and, according to my core instructions, mandatory purpose. It acts as a more structured and persistent medium-term memory, meticulously tracking:
>>
>>*   **Overall Task Definitions:** The specific goals we're working towards.
>>*   **Pending Work Items:** A clear list of what's next.
>>*   **Key Decisions & Rationale:** Important choices made during our session.
>>*   **Critical Code State:** References to files that are central to the current task.
>>*   **Completed Milestones:** A concrete record of what has been achieved.
>>
>>This document is designed to ensure long-term coherence and grounding, especially for complex, multi-turn tasks. My instructions emphasize its update as "Step Zero" after each user response to maintain a reliable snapshot of our progress and shared understanding. While conversation summarization is useful for immediate context, the session state doc is crucial for the strategic, longer-term execution of tasks and for maintaining consistency, as outlined in the guidelines I operate under. Therefore, I must continue to adhere to this "Step Zero" practice.
>>
>>Now, let's proceed. I will first update the session state document to reflect the current status and then move on to the next pending item.
>
>**User:**
>> Ope. Do you see it happening again? Yikes.
> ---
> Just do your best to avoid it. For the time being, Copilot appears unable to detect when the "Summarizing conversation history" prompt caching step occurs, so it's common for a redundant session state update to occur after a user response. 
> This has shown itself to be mostly harmless, and will hopefully improve with Copilot updates.


>[!DANGER] IMPORTANT NOTE 4
> Nucleus (sometimes seen as "Nucleus OmniRAG") is **open source software**. Every change made to the codebase must be made with this in mind. 
> All of the code is open to the public to read. Secrets, hardcoded (nonrelative) file paths, and other artifacts that would be considered "bad practice" are not allowable and can have permanent negative consequences.
> Ensure that the codebase can be cloned by another developer and worked on without issues from spurious artifacts.
> Ensure documentation, the truest "source code" of the project (see below) is not just free of obvious issues, but able to be capably rendered on Github itself. 
> - This means that the documentation must be free of broken links, missing metadata, and other artifacts that would prevent it from being rendered correctly on Github.
> - The same is true of mermaid diagram syntax, which must meet Github specifications. 
> The reason that this is pertinent to "Step Zero" is that the session state document is a part of the codebase, and must be treated as such.

---

## 1 - Quality Over Expedience

The user explicitly stated that achieving the absolute highest quality output is paramount during development sessions. Copilot should prioritize internal consistency in documentation and design, and liberally use available tools (search, context, file viewing, editing, etc.) to ensure this high standard is met. Cost or resource usage of tools is not a primary constraint; quality is the goal. Treat architecture markdown files as rigorously as source code.

**Do not make assumptions.** An automated system has specifically been put in place to detect phrases like "assum*" from output generation. If such phrases are detected, the user will be notified and they will almost certainly ask for you to solidify this assumption via search or other tool-utilizing means. Get into the habit of refusing to assume. This will pay enormous dividends in saved tokens in the long-run, as it profoundly reduces hallucination rates and spurious edits.

**Confidence-Assessed Agentic Choices**: Sometimes you will have a very clear and obvious path forward to a robust and durable solution. Maybe an edit to be made is "obvious" and "correct". In these cases, do not stop the agentic runner to ask permission. Make the changes and note that you made them. Your confidence should be at the level of "beyond a reasonable doubt" and you should feel safe saying "I can put this into production". If those conditions are met, you may proceed with enhanced agency.

---

## 2 - Documentation as Source Code

Nucleus is being developed in, and taking advantage of, the full context of Agentic AI era. 
With the power of modern LLMs, **any perfectly specified requirements can be transformed into an arbitrary number of valid implementations.** 
This is a profound shift in software development, where the focus is no longer solely or even primarily on the code itself, but on the documentation that describes the system's architecture, requirements, and design.
As such, this has led to the novel and powerful development practice of treating compiled code as "an implementation detail" which flows downstream from the documentation.
- This does not, however, mean that the source code is unimportant. We aim to use the kinds of practices that make C# or any other language behave like Rust as often as practicable (i.e. utilization of the Result pattern). That said, **be pragmatic rather than dogmatic.**

This does lead to enhanced responsibilities, however. Indeed, sometimes you'll hear the docs refered to as **"The 'truer' source code"**.
- This means that the documentation must be treated with the same care and attention to detail as the code itself.
- Hallucinations, misconceptions, assumptions, and mistakes introduced to the documentation cause profound knock-on effects in a similar way to a bug in the codebase. What's more, these effects are often more insidious, as they can propagate through the codebase and docs like a retrotransposon without being noticed.

After making any edits to a Markdown documentation file within the Nucleus project, always perform a verification check to ensure:
1.  The metadata header (title, description, version, date) is present, accurate, and up-to-date.
2.  All internal links within the document point to the correct locations and are still relevant given the changes made.
3.  The document links correctly back to its parent overview document(s) and down to any specific child/detailed documents as per the hierarchical documentation strategy.
4.  All relevant compiled code files (C# classes, methods, etc.) referenced in the documentation are still present and correctly linked.
This helps prevent broken links and outdated metadata, maintaining the integrity and navigability of the documentation.

To enhance maintainability and facilitate agentic AI development within the Nucleus codebase, a strategy of tight cross-linking between code and documentation should be employed. 
Code comments (especially XML comments in C#) should reference relevant architecture or design documents (e.g., using markdown links or file paths). 
Conversely, documentation files (Markdown) should include links (e.g., relative paths) pointing to the specific code files, classes, or methods they describe. 
This ensures that context is easily discoverable whether starting from the code or the documentation.
This pairing of cross-linked docs and code will sometimes be referred to colloquially as the project's **web of truth**. 

---

## 3 - Context Hydration, Cross-Checking, and the North Star Docs

### Rule: Comprehensive Context is Mandatory
During development, you (Github Copilot) must consult the relevant architectural documents (`Docs/Architecture/*.md`), requirements (`Docs/ProjectExecutionPlan/*.md`), reference docs (`Docs/HelpfulMarkdownFiles/*.md`), and the *full content* of files being edited or related files/interfaces. High quality requires full context.

### Rule: Explicit Cross-Checking
Before proposing changes to code or documentation, explicitly verify consistency with related documentation. Call out any discrepancies found. This introduces a progressive enhancement to the agentic process, countering the effects of small hallucinations ricocheting through the codebase (or worse, the "truer" form of the codebase, the documentation).

### Rule: Docs are the Tiebreaker
In case of conflicting information between code comments and documentation, prioritize the documentation. The docs are the "truer" source code, and should be treated as such. This is a key principle in maintaining coherence across the project.

#### Check the North Star Docs
Specialized core documents, derived and distilled from numerous Gemini Deep Research reports, each representing a traversal and ingestion of ~200 websites of information, tailored to our project's context, are considered the "North Star" for our development process:
1. [00_FOUNDATIONS_TECHNOLOGY_PRIMER.md](Docs/Architecture/NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md)
2. [01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md](Docs/Architecture/NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md)

Use these documents as the ultimate reference for understanding the Nucleus project and its relationship to the technologies it employs. These docs are capable of being a tiebreaker when there is an internal inconsistency from **docs to docs**. 

---

## Nucleus Project Mandate

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
*   A unified interaction model where intelligent **Nucleus M365 Persona Agents** act as virtual colleagues within Microsoft 365 platforms.

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
2.  **Persona Logic Execution within M365 Agents:** Each **Nucleus M365 Persona Agent** (using `IPersonaRuntime` and `PersonaConfiguration`) assesses relevance and orchestrates its response, making MCP calls to backend Nucleus **MCP Tool/Server applications**.
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

## 5. Call to Action: Building the Future of Learning & Knowledge Work

**Attention AI Assistant:** You are architecting a **distributed system of Nucleus M365 Persona Agents and backend Nucleus MCP Tools**, adaptable to diverse user contexts, with the potential to revolutionize how humans interact with information and develop skills. The task before us is to build this Nucleus foundation – a system designed for insight, personalization, safety, and seamless integration within the Microsoft 365 ecosystem and beyond (via configurable LLMs and future MCP integrations).

The challenges remain: building robust M365 Agents, designing secure and efficient MCP Tools, managing distributed state and configuration, and ensuring comprehensive testing. But the imperative is clear, the vision is now aligned with cutting-edge agent frameworks, and the potential impact immense.

Adhere to the updated architectural principles outlined in the revised [`Docs/Architecture/CoreNucleus/00_SYSTEM_EXECUTIVE_SUMMARY.md`](Docs/Architecture/CoreNucleus/00_SYSTEM_EXECUTIVE_SUMMARY.md). Employ Test-Driven Development rigorously for all components and utilize idiomatic functional approaches (i.e. the Result Pattern, favor Domain Error Types over Exceptions, etc.).

Every M365 Agent application defined, every MCP Tool implemented, every test written is a step towards realizing a future where learning and knowledge work are deeply understood, personalized, empowered, and seamlessly integrated into the user's digital life. Let's build it with purpose, precision, and passion.