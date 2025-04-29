---
title: Reference Implementation - Azure .NET IT Helpdesk Persona
description: Describes a reference architecture for the Nucleus Professional Persona configuration in an IT Helpdesk scenario, deployed on Azure using .NET and executed by the Persona Runtime.
version: 1.2
date: 2025-04-28
parent: ../ARCHITECTURE_PERSONAS_PROFESSIONAL.md
---

# Reference Implementation: Azure .NET IT Helpdesk

This document outlines a reference architecture for implementing the Nucleus system specifically for an IT Helpdesk scenario, deployed within a Microsoft Azure environment using .NET technologies. It provides a specific implementation example for the general concepts described in the main [Professional Colleague Persona Overview](../ARCHITECTURE_PERSONAS_PROFESSIONAL.md) and how its **configuration** would be executed by the central [Persona Runtime/Engine](../../../02_ARCHITECTURE_PERSONAS.md#112-persona-runtimeengine).

## 1. High-Level Component Architecture

**Purpose:** Shows the major software components and their interactions within the Azure ecosystem for the Helpdesk use case, utilizing the configuration-driven Persona Runtime.

```mermaid
graph LR
    User[IT Staff]
    TeamsClient[Teams Client]
    DotNetBotFramework[Bot Framework]
    TeamsAdapter[Teams Adapter]
    ServiceBus[[Service Bus]](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs)
    DotNetProcessingSvc[Processing Service]
    Orchestrator[Orchestrator]
    PersonaRuntime[Runtime (Helpdesk Config)]
    AIService[[AI Service]]
    CosmosDB[[Cosmos DB]](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs)
    AzureAppConfig[App Configuration]

    User -- Sends Message --> TeamsClient
    TeamsClient -- Interacts with Bot --> DotNetBotFramework
    DotNetBotFramework -- Uses Adapter --> TeamsAdapter
    TeamsAdapter -- Publishes Job Trigger --> ServiceBus
    ServiceBus -- Triggers Processing --> DotNetProcessingSvc
    DotNetProcessingSvc -- Orchestrates --> Orchestrator
    Orchestrator -- Invokes Runtime w/ Config --> PersonaRuntime
    PersonaRuntime -- Uses AI (via Config) --> AIService
    PersonaRuntime -- Stores Knowledge (via Repo) --> CosmosDB
    AzureAppConfig -- Configures --> Orchestrator
    AzureAppConfig -- Provides Persona Config --> PersonaRuntime
```

**Explanation:** This architecture uses Azure services. User interaction occurs via Teams, handled by a .NET Bot Framework bot and a Nucleus Teams Adapter. Queries and processing job triggers are published to Azure [Service Bus](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs) Topics for asynchronous handling by a .NET Processing Service (potentially Azure Functions or a Worker Service running in Azure Container Apps). This service orchestrates ephemeral ingestion and interacts with the **[Persona Runtime/Engine](../../../02_ARCHITECTURE_PERSONAS.md#112-persona-runtimeengine)**, loading the **IT Helpdesk Persona configuration** (defined according to [Persona Configuration](../../ARCHITECTURE_PERSONAS_CONFIGURATION.md)). The Runtime, guided by the configuration, uses an [AI Service via IChatClient] like Azure OpenAI for analysis, generates embeddings for derived snippets, and reads/writes metadata (`ArtifactMetadata`) and derived knowledge (`PersonaKnowledgeEntry`) to Azure [Cosmos DB](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs) via appropriate repositories. Configuration, including Persona configurations, is centrally managed via Azure App Configuration.

## 2. Key Architectural Characteristics

**Purpose:** Highlights the non-functional benefits and design principles of this specific implementation approach.

```mermaid
mindmap
  root((Proposed Nucleus Architecture for Enterprise IT))
    Integrated IT Workflow
      Operates within Microsoft Teams
      Uses Familiar Bot Interaction Model
      Leverages Teams Permissions
    Structured Knowledge Extraction
      Runtime (Helpdesk Config) analyzes content contextually
      Identifies solutions, steps, links
      Stores derived knowledge/snippets in Cosmos DB
    Targeted Retrieval
      Combines semantic search with structured data filters
      Aims for higher relevance of retrieved solutions
      Reduces noise compared to pure text search
    Secure Deployment
      Self-Hosted in Azure Tenant
      Source Data remains in organization
      Uses Azure security services (See: [Nucleus Security Principles](../../../06_ARCHITECTURE_SECURITY.md))
    Modular and Maintainable Design
      Built on .NET and Azure Services
      Separation of concerns (Runtime vs Config)
```

**Explanation:** This mind map summarizes the advantages of this Azure/.NET-based implementation for a specific IT Helpdesk scenario. Key benefits include seamless integration into the existing Teams workflow, the ability to extract structured solutions (not just text) via the **configured Persona Runtime**, more precise retrieval using combined vector and metadata filtering, a secure deployment model within the company's Azure tenant, and a modular design based on standard .NET and Azure practices facilitating future maintenance and expansion.
