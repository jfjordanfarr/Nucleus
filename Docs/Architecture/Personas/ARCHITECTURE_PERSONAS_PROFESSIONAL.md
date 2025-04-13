---
title: Persona -  Professional Colleague
description: Describes a pre-built persona for Nucleus OmniRAG that simulates a professional colleague, providing insights and assistance in a work context.
version: 1.0
date: 2025-04-13
---

# Persona: Professional Colleague

## Typical Request Flow (IT Helpdesk)

**Purpose:** Illustrates the sequence of interactions when an IT staff member queries the Helpdesk persona via Teams.

```mermaid
sequenceDiagram
    participant ITStaff as IT Staff Member
    participant TeamsClient as Teams Client
    participant BotFramework as .NET Bot Framework
    participant TeamsAdapter as Nucleus Teams Adapter
    participant BackendAPI as Nucleus API / Query Handler
    participant KnowledgeDB as Cosmos DB (PersonaKnowledgeEntries)
    participant AIService as AI Model

    ITStaff->>+TeamsClient: Asks query, e.g., "@HelpdeskBot VPN connection fails with code 123"
    TeamsClient->>+BotFramework: Sends Message Event
    BotFramework->>+TeamsAdapter: Event Received (Query, Context)

    %% --- Query Handling Path --- %%
    TeamsAdapter->>+BackendAPI: Route Query Request
    BackendAPI->>+AIService: Generate Embedding for Query Text
    AIService-->>-BackendAPI: Return Query Embedding
    BackendAPI->>+KnowledgeDB: Search PersonaKnowledgeEntries using Embedding & Filters
    KnowledgeDB-->>-BackendAPI: Return Relevant Entries (Ranked Structured Data, Snippets, Vectors)
    Note right of BackendAPI: Entries contain structured data (Problem, Solution Snippet, KB Link) + Vectors + Source Identifiers
    BackendAPI->>+AIService: Synthesize Response using Retrieved Entries/Snippets
    AIService-->>-BackendAPI: Generated Response Text
    BackendAPI-->>-TeamsAdapter: Return Synthesized Response

    %% --- Response Delivery --- %%
    TeamsAdapter->>+BotFramework: Post Response Message
    BotFramework->>+TeamsClient: Display Response in Teams
    TeamsClient-->>-ITStaff: Shows retrieved solution steps, KB links, related ticket refs

```

**Explanation:** This sequence shows a query originating from an IT staff member in Teams. The message flows through the Bot Framework to the Nucleus Teams Adapter. The adapter forwards the query to the backend API/handler. The backend generates a vector embedding for the query, searches the `PersonaKnowledgeEntry` data in Cosmos DB using the embedding and any relevant filters (like detected error codes), retrieves the most relevant derived knowledge (structured data, snippets, vectors), and uses the AI service to synthesize a user-friendly response based on those retrieved items. The response is then sent back through the layers to the user in Teams.