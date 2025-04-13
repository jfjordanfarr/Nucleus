# Reference Implementation: Azure .NET IT Helpdesk

This document outlines a reference architecture for implementing the Nucleus OmniRAG system specifically for an IT Helpdesk scenario, deployed within a Microsoft Azure environment using .NET technologies.

## 1. High-Level Component Architecture

**Purpose:** Shows the major software components and their interactions within the Azure ecosystem for the Helpdesk use case.

```mermaid
graph LR
    subgraph User Interaction (Teams)
        User([fa:fa-user IT Staff])
    end

    subgraph Teams Integration Layer
        Teams[Microsoft Teams]
        BotFramework[fa:fa-robot .NET Bot Framework]
        TeamsAdapter[Nucleus Teams Adapter]
    end

    subgraph Nucleus Backend Services (Azure @ WWR)
        Queue[fa:fa-envelope Azure Service Bus]
        Processor[fa:fa-cogs .NET Processing Service <br/>(Functions/Worker)<br/>Handles Ephemeral Ingestion]
        Persona[fa:fa-brain IT Helpdesk Persona Module <br/>(Analyzes Ephemeral Data)]
        AIService[fa:fa-microchip Configurable AI Model <br/>(e.g., Google Gemini)]
        KnowledgeDB[fa:fa-database Azure Cosmos DB <br/>(ArtifactMetadata & PersonaKnowledgeEntries)]
        Config[fa:fa-cog System Configuration]
    end

    User -- 1. Sends Query / Shares Info --> Teams
    Teams -- 2. Routes Event --> BotFramework
    BotFramework -- 3. Invokes --> TeamsAdapter
    TeamsAdapter -- 4. Sends Query to API / Enqueues Job --> Queue
    Queue -- 5. Triggers --> Processor
    Processor -- 6. Reads/Writes --> KnowledgeDB %% Reads existing ArtifactMetadata, Writes updated status %%
    Processor -- 7. Fetches Fresh Source (via Adapter if needed), Processes Ephemerally --> Persona
    Persona -- 8. Analyzes Ephemeral Data, Uses --> AIService
    Persona -- 9. Returns Derived Knowledge/Snippets --> Processor
    Processor -- 10. Generates Embeddings (on Snippets) --> AIService
    Processor -- 11. Writes Results to PersonaKnowledgeEntries --> KnowledgeDB
    Processor -- 12. Notifies Adapter via Queue/API --> TeamsAdapter
    TeamsAdapter -- 13. Handles Direct Queries / Receives Async Results --> BotFramework
    BotFramework -- 14. Delivers Response --> Teams
    Config -- Manages Settings --> Processor
    Config -- Manages Settings --> AIService
    Config -- Manages Settings --> KnowledgeDB
```

**Explanation:** This architecture uses Azure services. User interaction occurs via Teams, handled by a .NET Bot Framework bot and a Nucleus Teams Adapter. Queries and processing jobs are often queued via Azure Service Bus for asynchronous handling by a .NET Processing Service (potentially Azure Functions or a Worker Service). This service orchestrates ephemeral ingestion, interacts with the IT Helpdesk Persona module (which uses Azure OpenAI or similar for analysis), generates embeddings for derived snippets, and reads/writes metadata (`ArtifactMetadata`) and derived knowledge (`PersonaKnowledgeEntry`) to Azure Cosmos DB. Configuration is centrally managed.

## 2. Key Architectural Characteristics

**Purpose:** Highlights the non-functional benefits and design principles of this specific implementation approach.

```mermaid
mindmap
  root((Proposed Nucleus Architecture <br/> for Enterprise IT))
    ::icon(fa fa-sitemap)
    (+) Integrated IT Workflow
      ::icon(fab fa-microsoft)
      Operates within Microsoft Teams
      Uses Familiar Bot Interaction Model
      Leverages Teams Permissions (RSC)
    (+) Structured Knowledge Extraction
      ::icon(fa fa-filter)
      IT Persona analyzes content contextually (Ephemeral)
      Identifies solutions, steps, links (Not just text chunks)
      Stores derived knowledge/snippets in Cosmos DB
    (+) Targeted Retrieval
      ::icon(fa fa-search-location)
      Combines semantic search (vectors) with structured data filters on Cosmos DB
      Aims for higher relevance of retrieved solutions/KBs
      Reduces noise compared to pure text search
    (+) Secure Deployment
      ::icon(fa fa-shield-alt)
      Self-Hosted within the Organization's Azure Tenant
      Source Data remains in the organization's systems; Nucleus stores metadata/derived knowledge in the organization's Azure
      Utilizes Azure security services (Key Vault, etc.)
    (+) Modular & Maintainable Design
      ::icon(fa fa-puzzle-piece)
      Built on .NET & Azure Services
      Separation of concerns (Adapters, Personas, Storage)
      Foundation allows future extensions/customization
```

**Explanation:** This mind map summarizes the advantages of this Azure/.NET-based implementation for a specific IT Helpdesk scenario. Key benefits include seamless integration into the existing Teams workflow, the ability to extract structured solutions (not just text), more precise retrieval using combined vector and metadata filtering, a secure deployment model within the company's Azure tenant, and a modular design based on standard .NET and Azure practices facilitating future maintenance and expansion.
