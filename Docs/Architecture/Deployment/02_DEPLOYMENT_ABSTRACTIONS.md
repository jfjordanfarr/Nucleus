---
title: "Nucleus Deployment Abstractions"
description: "Core deployment units and abstractions for the Nucleus platform, focusing on M365 Agents, MCP Tools, and .NET Aspire orchestration."
version: 3.0 # Updated version
date: 2025-05-27 # Updated date
---

## 1. Introduction

This document defines the core deployment abstractions for the Nucleus platform, reflecting the shift to a distributed architecture based on Microsoft 365 Persona Agents, backend Model Context Protocol (MCP) Tools/Servers, and orchestration via .NET Aspire. Understanding these abstractions is crucial for deploying, managing, and scaling Nucleus components effectively across different hosting environments.

This document links to:
*   [Deployment Overview](./01_DEPLOYMENT_OVERVIEW.md) (Parent)
*   [Azure Hosting](./Hosting/ARCHITECTURE_HOSTING_AZURE.md)
*   [Cloudflare Hosting](./Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md)
*   [Self-Host (Home Network)](./Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md)

## 2. Core Deployment Units Orchestrated by .NET Aspire

The Nucleus platform, orchestrated by a .NET Aspire `AppHost` project, is composed of several distinct, independently deployable units:

### 2.1. Microsoft 365 Persona Agent Application Runtimes

*   **Description:** Represents the runtime environment for a specific Persona embodied as a Microsoft 365 Agent application. This is the primary user-facing component, handling interactions within the M365 ecosystem.
*   **Requirement:** Must be hosted in Azure to leverage the M365 Agents SDK and integrate with services like Azure Bot Service.
*   **Key Needs:** Secure communication with backend MCP Tools, access to M365 Graph APIs, LLM service integration, state management.
*   **Examples:** Azure App Service, Azure Container Apps hosting the .NET M365 Agent application.
*   **Aspire Role:** Defined as a project/service in the AppHost, configured for Azure deployment.

### 2.2. Nucleus MCP Tool/Server Application Runtimes

*   **Description:** Backend services implementing specific capabilities (e.g., `Nucleus_KnowledgeStore_McpServer`, `Nucleus_ContentProcessor_McpServer`, `Nucleus_RAGPipeline_McpServer`) exposed via the Model Context Protocol (MCP).
*   **Requirement:** Network accessibility from M365 Persona Agent Applications and other MCP Tools.
*   **Key Needs:** Scalability, independent deployability, defined API contracts (MCP), access to shared infrastructure (database, queues).
*   **Examples:** Containerized ASP.NET Core applications running in Azure Container Apps, Azure Kubernetes Service, or self-hosted Docker environments.
*   **Aspire Role:** Defined as distinct services/projects in the AppHost, with service discovery and configuration managed by Aspire. Aspire can build and push container images.

### 2.3. Shared Infrastructure Components (as .NET Aspire Resources)

These are common services, defined as resources in the .NET Aspire AppHost, that support both M365 Persona Agents (indirectly, via MCP Tools) and Nucleus MCP Tools directly:

*   **Asynchronous Messaging Queue (e.g., `IBackgroundTaskQueue` abstraction implementation):
    *   **Description:** Decouples tasks, manages asynchronous workflows (e.g., ingestion processing, long-running agent actions), and enhances resilience. The `IBackgroundTaskQueue` interface provides an abstraction, with concrete implementations for different queue providers.
    *   **Requirement:** Reliable message delivery, persistence (for critical tasks).
    *   **Key Needs:** Queue and topic support, dead-lettering, monitoring.
    *   **Examples:** Azure Service Bus (cloud), RabbitMQ (local/self-hosted, containerized).
    *   **Aspire Role:** Defined as a resource (e.g., `AddAzureServiceBus`, `AddRabbitMQClient`) in the AppHost, with connection strings injected into dependent services.

*   **Document & Vector Database (e.g., accessed via `Nucleus_KnowledgeStore_McpServer`):
    *   **Description:** The central data store for `ArtifactMetadata`, `PersonaKnowledgeEntry` records, vector embeddings, and configurations. Access is typically abstracted via a dedicated MCP Tool like `Nucleus_KnowledgeStore_McpServer`.
    *   **Requirement:** Scalable storage for structured and unstructured data, efficient vector search capabilities.
    *   **Key Needs:** Data partitioning, indexing, backup/restore, security.
    *   **Examples:** Azure Cosmos DB for NoSQL (with integrated vector search) for cloud; emulators (e.g., Cosmos DB emulator) or local containerized databases (e.g., PostgreSQL with pgvector, Weaviate) for development.
    *   **Aspire Role:** Defined as a resource (e.g., `AddAzureCosmosDB`) in the AppHost, with connection details provided to the relevant MCP Tool(s).

*   **Background Worker Service Runtime (e.g., `QueuedInteractionProcessorService` logic):
    *   **Description:** This refers to the logical hosting of background processing tasks, often triggered by messages from a queue. This logic might reside within an M365 Persona Agent's backend components or within a dedicated MCP Tool designed for background processing.
    *   **Requirement:** Ability to run continuously or on-demand, processing queued items.
    *   **Key Needs:** Scalability based on queue depth, integration with logging/monitoring, reliable execution.
    *   **Examples:** Hosted as part of an ASP.NET Core application (M365 Agent or MCP Tool) using `IHostedService`, or as a dedicated Azure Function/WebJob triggered by a queue.
    *   **Aspire Role:** If part of an Aspire-managed project, its lifecycle and configuration are managed by Aspire. If an external service like Azure Functions, Aspire can manage its configuration and references to it.

## 3. Deployment Scenarios & Interdependencies with .NET Aspire

The key characteristic of the architecture is the **distributed nature orchestrated by .NET Aspire**, where M365 Persona Agents (Azure-bound) make calls to potentially diversely-hosted Nucleus MCP Tools, all managed within a unified Aspire application model.

*   **M365 Persona Agent Applications always require an Azure hosting environment.** This is due to their reliance on the M365 Agents SDK and deep integration with Azure services (e.g., Azure Bot Service). .NET Aspire facilitates packaging and deploying these agents to Azure (e.g., via `azd`).
*   **Nucleus MCP Tools offer hosting flexibility, managed by Aspire.** They can be co-located with M365 Agents in Azure (e.g., in Azure Container Apps) or hosted externally (e.g., self-hosted Docker containers on-premises), as long as they are network-accessible and discoverable through Aspire's service discovery mechanisms.
*   **Shared Infrastructure (Database, Message Queue) is ideally Azure-hosted** for cloud deployments, provisioned or referenced by Aspire as resources. For local development, Aspire supports emulators (e.g., Azurite for Azure Storage Queues, Cosmos DB Emulator) and local containers (e.g., RabbitMQ, PostgreSQL).

## 4. Key Abstractions for Deployment Configuration with .NET Aspire

*   **MCP Tool Endpoint Discovery:** .NET Aspire's built-in service discovery mechanisms (e.g., DNS-based for local development, integration with Azure service discovery for deployed apps) are used by M365 Persona Agents to locate MCP Tool endpoints. This abstracts the actual location of the MCP Tools.
*   **Service Connection Strings/API Keys:** Managed by .NET Aspire's configuration system, which seamlessly integrates with environment variables, user secrets (for local development), Azure App Configuration, and Azure Key Vault for production deployments.
*   **Persona Configuration:** Defines which MCP Tools a Persona Agent can use, LLM preferences, etc. This configuration data is typically stored in the Nucleus Database and accessed via a dedicated configuration MCP Tool, whose endpoint is discoverable via Aspire.
*   **.NET Aspire Application Host (`Nucleus.Aspire.AppHost`):** This project is the heart of the deployment strategy. It programmatically defines all services (M365 Agents, MCP Tools), their relationships, configurations (connection strings, environment variables), and how they are launched for local development or packaged for deployment to Azure or other environments.
*   **Docker Images (for MCP Tools & self-hosting):** MCP Tools are designed to be containerized. .NET Aspire can be configured to build these Docker images as part of the development and deployment process and orchestrate their deployment to container registries and hosting platforms.

## 5. Implications of the Distributed Model with .NET Aspire

*   **Network Security:** Secure communication (HTTPS by default with Aspire) is crucial. For Azure-to-Azure traffic between M365 Agents and MCP Tools, Virtual Network integration or Azure Private Link can be utilized. .NET Aspire helps manage related configurations and certificates, especially for local development with self-signed certs.
*   **Service Discovery:** Handled transparently by .NET Aspire, abstracting away the complexities of endpoint resolution across different environments (local, Azure, self-hosted).
*   **Resiliency & Fault Tolerance:** .NET Aspire itself doesn't provide resiliency primitives but simplifies their integration. Libraries like Polly can be used within M365 Agents or MCP Tools to implement retries, circuit breakers, etc. The independent scalability of MCP tools, managed by the hosting platform but defined in the Aspire AppHost, contributes to overall system resilience.
*   **Monitoring & Logging (OpenTelemetry):** .NET Aspire provides a unified dashboard for local development, aggregating logs, metrics, and traces from all services. It natively integrates with OpenTelemetry, enabling seamless export of telemetry data to systems like Azure Monitor (Application Insights), Prometheus, Grafana, etc., in production environments.

This revised set of abstractions, centered around .NET Aspire, provides a clearer framework for understanding how Nucleus components are developed, deployed, and interact in the new M365 Agent-centric architecture.
