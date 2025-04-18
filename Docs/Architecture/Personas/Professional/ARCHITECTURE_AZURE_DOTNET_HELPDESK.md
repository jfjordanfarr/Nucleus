---
title: Reference Implementation - Azure .NET IT Helpdesk Persona
description: Describes a reference architecture for the Nucleus Professional Persona in an IT Helpdesk scenario, deployed on Azure using .NET.
version: 1.0
date: 2025-04-13
---

# Reference Implementation: Azure .NET IT Helpdesk

This document outlines a reference architecture for implementing the Nucleus OmniRAG system specifically for an IT Helpdesk scenario, deployed within a Microsoft Azure environment using .NET technologies. It provides a specific implementation example for the general concepts described in the main [Professional Colleague Persona Overview](../ARCHITECTURE_PERSONAS_PROFESSIONAL.md).

## 1. High-Level Component Architecture

**Purpose:** Shows the major software components and their interactions within the Azure ecosystem for the Helpdesk use case.

```mermaid
graph LR
    subgraph "User Interaction (Teams)"
        User["fa:fa-user IT Staff"]
    end

    subgraph "Teams Integration Layer"
        TeamsClient["Teams Client"]
        DotNetBotFramework["fa:fa-robot .NET Bot Framework"]
        TeamsAdapter["Nucleus Teams Adapter"]
    end

    subgraph "Nucleus Backend Services (Azure @ WWR)"
        ServiceBus["(Azure Service Bus Topic)"]
        DotNetProcessingSvc["fa:fa-cogs .NET Processing Service <br/>(Azure Container App / Function / Worker)<br/>Handles Ephemeral Ingestion"]
        Orchestrator["fa:fa-cogs Orchestrator"]
        ItHelpdeskPersona["fa:fa-brain IT Helpdesk Persona Module <br/>(Analyzes Ephemeral Data)"]
        AzureOpenAI["fa:fa-microchip Azure OpenAI <br/>(e.g., Google Gemini)"]
        CosmosDB["fa:fa-database Azure Cosmos DB <br/>(ArtifactMetadata & PersonaKnowledgeEntries)"]
        AzureAppConfig["fa:fa-cog Azure App Configuration"]
    end

    User -- 1. Sends Message via Teams --> TeamsClient
    TeamsClient -- 2. Interacts with Bot --> DotNetBotFramework
    DotNetBotFramework -- 3. Uses Adapter --> TeamsAdapter
    TeamsAdapter -- 4. Sends Query to API / Publishes Job Trigger --> ServiceBus
    ServiceBus -- 5. Triggers Processing Service --> DotNetProcessingSvc
    DotNetProcessingSvc -- 6. Orchestrates --> Orchestrator
    Orchestrator -- Uses --> TeamsAdapter
    Orchestrator -- Uses --> ItHelpdeskPersona
    ItHelpdeskPersona -- 7. Calls AI --> AzureOpenAI
    Orchestrator -- 8. Reads/Writes Metadata --> CosmosDB
    Orchestrator -- 9. Reads/Writes Knowledge --> CosmosDB
    DotNetProcessingSvc -- 11. Sends Response via Adapter --> TeamsAdapter
    TeamsAdapter -- 12. Relays to Bot --> DotNetBotFramework
    DotNetBotFramework -- 13. Sends Reply --> TeamsClient
    TeamsClient -- 14. Displays to User --> User

    DotNetBotFramework -- Reads Config --> AzureAppConfig
    DotNetProcessingSvc -- Reads Config --> AzureAppConfig
```

**Explanation:** This architecture uses Azure services. User interaction occurs via Teams, handled by a .NET Bot Framework bot and a Nucleus Teams Adapter. Queries and processing job triggers are published to Azure Service Bus Topics for asynchronous handling by a .NET Processing Service (potentially Azure Functions or a Worker Service running in Azure Container Apps). This service orchestrates ephemeral ingestion, interacts with the IT Helpdesk Persona module (which uses Azure OpenAI or similar for analysis), generates embeddings for derived snippets, and reads/writes metadata (`ArtifactMetadata`) and derived knowledge (`PersonaKnowledgeEntry`) to Azure Cosmos DB. Configuration is centrally managed via Azure App Configuration.

## 2. Key Architectural Characteristics

**Purpose:** Highlights the non-functional benefits and design principles of this specific implementation approach.

```mermaid
mindmap
  root((Proposed Nucleus Architecture <br/> for Enterprise IT))
    ::icon(fa fa-sitemap)
    (+) "Integrated IT Workflow"
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
