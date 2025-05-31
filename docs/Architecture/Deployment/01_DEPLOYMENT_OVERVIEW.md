---
title: "Nucleus Deployment Overview"
description: "Overview of deployment strategies and models for the Nucleus platform, incorporating M365 Agents, MCP Tools, and .NET Aspire."
version: 2.1
date: 2025-05-29
parent: ../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md
see_also:
  - title: "Deployment Abstractions"
    link: "./02_DEPLOYMENT_ABSTRACTIONS.md"
  - title: "Azure Hosting Strategy"
    link: "./Hosting/ARCHITECTURE_HOSTING_AZURE.md"
  - title: "Cloudflare Hosting Strategy"
    link: "./Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md"
  - title: "Self-Hosting Strategy"
    link: "./Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md"
  - title: "CI/CD Strategy"
    link: "../DevelopmentLifecycle/03_CICD_STRATEGY.md"
  - title: "Comprehensive System Architecture"
    link: "../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md"
---

# Nucleus Deployment Overview

## 1. Core Principles

The Nucleus platform is designed for flexibility in deployment, catering to different operational needs and data governance requirements. Key principles guiding our deployment strategy include:

*   **Platform Integration First:** Personas primarily interact via existing collaboration platforms (Teams, Slack, Discord, Email).
*   **.NET Aspire Orchestration:** Leveraging .NET Aspire for simplified development, orchestration, and deployment of distributed applications, whether cloud-hosted or self-hosted. This includes the orchestration of Nucleus M365 Persona Agent Applications and backend Nucleus MCP Tool/Server Applications.
*   **M365 Agent Compatibility:** Ensuring deployment models robustly support the requirements of Microsoft 365 Agents, including their lifecycle, Azure-based hosting, and communication patterns with backend services.
*   **MCP Tool Integration:** Facilitating the seamless deployment, discovery, and accessibility of Model Context Protocol (MCP) Tools, which form the backbone of Persona capabilities and are managed as distinct services within the .NET Aspire AppHost.
*   **Scalability & Resilience:** Architecting for growth and fault tolerance using modern cloud-native patterns and Azure services (or their equivalents in self-hosted scenarios), managed and monitored through .NET Aspire where applicable.
*   **Security by Design:** Implementing security best practices across all deployment models, including secure communication between M365 Agents and MCP Tools.

## 2. Deployment Models

Nucleus supports two primary deployment models:

### 2.1. Cloud-Hosted SaaS (Primary Model)

*   **Description:** A fully managed service hosted by the Nucleus project maintainers (or a designated entity). Users subscribe to the service and integrate Nucleus Personas (M365 Agents) into their collaboration platforms by adding the official Nucleus bot/app. Backend MCP Tools and shared infrastructure are managed by the provider.
*   **Infrastructure:** Typically leverages Azure services managed by the Nucleus team, including:
    *   Azure App Service / Azure Container Apps (for hosting .NET Aspire applications, including M365 Agent backend logic and MCP Tools)
    *   Azure Cosmos DB (for data persistence)
    *   Azure Service Bus (for message queuing)
    *   Azure Key Vault (for secrets management)
    *   Azure Application Gateway / Front Door (for ingress and load balancing)
*   **Orchestration:** .NET Aspire dashboard and deployment tools are used by the service administrators.
*   **Data Sovereignty:** Processed data and metadata are stored within the managed Azure environment. Original user content accessed via platform APIs is handled ephemerally.
*   **Use Cases:** Individuals, teams, and organizations seeking a turnkey solution with minimal operational overhead for both M365 Agent and MCP Tool functionalities.

### 2.2. Self-Hosted Instance (Open Source)

*   **Description:** Organizations or individuals deploy and manage Nucleus within their own infrastructure. This involves deploying their own instances of Nucleus M365 Persona Agent applications (typically within their Azure tenant) and their own instances of backend Nucleus MCP Tools and databases (on-premises or private cloud).
*   **Infrastructure:** The organization is responsible for provisioning and managing the necessary infrastructure. M365 Agents will require an Azure environment. MCP Tools and databases could include:
    *   Kubernetes, Docker Swarm, or other container orchestration platforms.
    *   Self-managed databases (e.g., PostgreSQL with vector extensions, or other NoSQL/vector DBs).
    *   Self-managed message queues (e.g., RabbitMQ).
    *   Appropriate networking and security infrastructure.
*   **Orchestration:** .NET Aspire provides the tools for packaging and orchestrating the deployment of Nucleus components within the chosen environment. The organization's IT team manages the Aspire dashboard and underlying infrastructure.
*   **Data Sovereignty:** All data remains within the organization's control and infrastructure.
*   **Use Cases:** Organizations with strict data residency or compliance requirements for all components, those needing deep customization of MCP Tools, or those wishing to integrate Nucleus with proprietary internal systems while managing their M365 Agent deployments.

## 3. Key Deployable Components Orchestrated by .NET Aspire

Regardless of the deployment model, the core Nucleus system comprises several key deployable components, defined and orchestrated by the `.NET Aspire AppHost` project:

*   **Nucleus M365 Persona Agent Applications:**
    *   **Description:** These are the user-facing applications, embodying specific Personas (e.g., EduFlow OmniEducator). They run as Microsoft 365 Agents, interacting within the user's M365 environment (Teams, Outlook).
    *   **Deployment:** Hosted in Azure, leveraging services like Azure Bot Service and Azure App Service/Container Apps. Their deployment is managed as part of the .NET Aspire application.
*   **Backend Nucleus MCP Tool/Server Applications:**
    *   **Description:** These are distinct backend services that provide specific functionalities (e.g., knowledge storage, RAG pipelines, content processing) to the M365 Persona Agents via the Model Context Protocol (MCP).
    *   **Deployment:** Can be deployed as containerized applications (e.g., to Azure Container Apps, Kubernetes, or self-hosted Docker environments), managed and discovered as services within the .NET Aspire AppHost.
*   **Shared Infrastructure (Managed as .NET Aspire Resources):**
    *   **Description:** Common backend services required by both M365 Persona Agents (indirectly via MCP Tools) and the MCP Tools themselves.
    *   **Examples:**
        *   **Nucleus Database:** Azure Cosmos DB for NoSQL (with vector search) for cloud deployments; emulators or local containers for development.
        *   **Message Queue:** Azure Service Bus for cloud; RabbitMQ or other local queues for development.
        *   **Configuration & Secrets:** Azure App Configuration, Azure Key Vault integrated with .NET Aspire.

The `.NET Aspire AppHost` is central to defining these components, their configurations, inter-service communication, and facilitating their deployment and local development experience.

## 4. Configuration

*   **Cloud-Hosted:** Configuration is largely managed by the service provider. Users may configure Persona behaviors and platform integration settings.
*   **Self-Hosted:** Administrators have full access to configuration files and environment variables to set up database connections, AI service API keys, message queue connections, and other infrastructure-specific parameters.

## 5. CI/CD (Continuous Integration/Continuous Deployment)

Refer to [CI/CD Strategy](../DevelopmentLifecycle/03_CICD_STRATEGY.md) for details on how Nucleus is built, tested, and deployed.

## 6. Next Steps

*   [Deployment Abstractions](./02_DEPLOYMENT_ABSTRACTIONS.md)
*   [Hosting Details](./Hosting/)
