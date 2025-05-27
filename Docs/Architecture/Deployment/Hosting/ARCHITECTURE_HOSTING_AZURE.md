---
title: "Azure Hosting Strategy for Nucleus"
description: "Comprehensive guide to deploying the Nucleus platform (M365 Persona Agents, MCP Tools, and Shared Infrastructure) on Microsoft Azure, leveraging .NET Aspire for orchestration and deployment."
version: 3.0 # Major Revision
date: 2025-05-27 # Current Date
parent: ../01_DEPLOYMENT_OVERVIEW.md
---

## 1. Introduction

This document outlines the recommended strategy for hosting the entire Nucleus platform on Microsoft Azure. Given the pivot to Microsoft 365 Persona Agents and their inherent ties to the Azure ecosystem (including Microsoft Entra ID for Agent identities and Azure Bot Service for communication), **Azure is the primary and most cohesive hosting environment for all Nucleus components**: M365 Persona Agents, backend Model Context Protocol (MCP) Tool/Server Applications, and Shared Infrastructure.

This strategy emphasizes the **central role of .NET Aspire** (specifically the `Nucleus.Aspire.AppHost` project) for:
*   Orchestrating the distributed application components during local development.
*   Generating deployment manifests and simplifying the deployment process to Azure, primarily using the **Azure Developer CLI (`azd`)**.

This document assumes familiarity with the concepts outlined in:
*   [Deployment Overview](../01_DEPLOYMENT_OVERVIEW.md)
*   [Deployment Abstractions](../02_DEPLOYMENT_ABSTRACTIONS.md)

## 2. Core Philosophy: Azure-Native & Aspire-Driven

The deployment philosophy for Nucleus on Azure is:
*   **Azure-Native:** Leverage Azure PaaS services where possible to maximize integration, scalability, and manageability, especially for components with strong Azure dependencies like M365 Agents.
*   **Aspire-Driven:** Utilize .NET Aspire for defining the application model, orchestrating services locally, and streamlining the deployment to Azure via `azd` and Bicep.

Co-locating Nucleus MCP Tools and Shared Infrastructure (Database, Message Queue, Configuration) in the same Azure environment as the M365 Agent compute simplifies networking, security, and management, while maximizing performance.

## 3. Azure Service Mapping (Orchestrated by .NET Aspire)

The `.NET Aspire AppHost` project defines how each Nucleus deployment unit maps to Azure services. `azd` then uses this definition, along with Bicep templates, to provision and deploy these services.

### 3.1. Nucleus M365 Persona Agent Application Runtimes

*   **Description:** The runtime environment for a specific Persona embodied as an M365 Agent.
*   **Azure Services:**
    *   **Azure Bot Service:** Essential for registering the M365 Agent, managing communication channels (Teams, Outlook), and integrating with the Microsoft Bot Framework.
    *   **Azure Container Apps (ACA) or Azure App Service:** Host the .NET application logic for the M365 Agent. ACA is preferred for its serverless container capabilities and ease of integration with Aspire.
*   **Microsoft Entra ID:** Each M365 Agent requires an Application Registration in Microsoft Entra ID, which provides its unique identity (Entra Agent ID).
*   **Aspire Role:** The M365 Agent application is a project within the Aspire AppHost. Aspire manages its configuration and service discovery references for backend MCP Tools.
*   **`azd` Role:** Deploys the agent logic to ACA/App Service and configures the Azure Bot Service.

### 3.2. Backend Nucleus MCP Tool/Server Application Runtimes

*   **Description:** Backend services implementing specific capabilities (e.g., `Nucleus_KnowledgeStore_McpServer`, `Nucleus_ContentProcessor_McpServer`).
*   **Azure Services:**
    *   **Azure Container Apps (ACA):** Preferred for containerized MCP Tools.
    *   **Azure App Service:** Alternative for web-based MCP Tools.
    *   **Azure Functions:** Suitable for event-driven or smaller, stateless MCP Tools.
*   **Aspire Role:** MCP Tools are defined as distinct projects or container resources in the AppHost. Aspire handles local orchestration, service discovery, and configuration injection.
*   **`azd` Role:** Builds container images (pushing to Azure Container Registry - ACR), deploys them to ACA/App Service/Functions, and configures networking and scaling.

### 3.3. Shared Infrastructure Components (as .NET Aspire Resources)

*   **Asynchronous Messaging Queue (`IBackgroundTaskQueue` implementation):**
    *   **Azure Service:** **Azure Service Bus** (Topics and Subscriptions for pub/sub, Queues for point-to-point).
    *   **Aspire Role:** `AddAzureServiceBus` in AppHost, connection strings managed.
*   **Document & Vector Database (accessed via `Nucleus_KnowledgeStore_McpServer`):**
    *   **Azure Service:** **Azure Cosmos DB for NoSQL** (with integrated vector search capabilities).
    *   **Aspire Role:** `AddAzureCosmosDB` in AppHost, connection details managed.
*   **Background Worker Service Runtime (e.g., `QueuedInteractionProcessorService` logic):**
    *   **Azure Service:** Typically hosted within an **Azure Container App** instance (if part of an `IHostedService` in an MCP Tool) or as an **Azure Function** triggered by Azure Service Bus.
    *   **Aspire Role:** Managed as part of the hosting MCP Tool project or referenced if an Azure Function.
*   **Configuration & Secrets Management:**
    *   **Azure Services:**
        *   **Azure App Configuration:** For application settings and feature flags.
        *   **Azure Key Vault:** For secrets, certificates, and connection strings.
    *   **Aspire Role:** Seamless integration for injecting configurations and secrets into all services during local development and for deployed applications.
*   **Container Registry:**
    *   **Azure Service:** **Azure Container Registry (ACR)** for storing Docker images of MCP Tools and potentially M365 Agent backends.
    *   **`azd` Role:** `azd` and Aspire can be configured to build and push images to ACR.

## 4. External AI Service Integration

*   **Azure OpenAI Service:** The preferred and most integrated LLM provider for enterprise scenarios within Azure.
    *   **Access:** Deployed within the Azure subscription. API keys and endpoints are stored in Azure Key Vault.
*   **Other LLMs (Google Gemini, OpenRouter, etc.):**
    *   **Access:** API keys stored securely in Azure Key Vault.
    *   **Networking:** Egress to these external services from VNet-integrated Azure resources should be routed through **Azure Firewall** for security and policy enforcement.
*   **Aspire Role:** Manages connection information (endpoints, API key references from Key Vault) for all LLM services, injecting them into the relevant M365 Agent or MCP Tool configurations.

## 5. Networking & Security (Key Principles for Azure Deployment)

A secure networking posture is critical.
*   **Virtual Network (VNet) Integration:** All applicable Azure services (ACA, App Service, Functions, Cosmos DB, Service Bus, Key Vault, App Configuration) should be VNet-integrated or have VNet endpoints.
*   **Public Ingress for M365 Agents:**
    *   The Azure Bot Service endpoint for the M365 Agent is inherently public.
    *   The underlying compute (ACA/App Service) hosting the agent logic should be secured, potentially fronted by **Azure Application Gateway** (with Web Application Firewall - WAF) or **Azure API Management (APIM)** if direct HTTPs invocation beyond Bot Framework is needed. These ingress points should then route to VNet-integrated services.
*   **Egress Control:**
    *   **Azure Firewall:** All outbound traffic from VNet-integrated resources (including calls to external LLMs or other public APIs) **must** be routed through Azure Firewall. This allows for centralized logging, threat intelligence, and policy enforcement. User-Defined Routes (UDRs) will be necessary.
*   **Private Endpoints:**
    *   Utilize Private Endpoints for all supported Azure PaaS services (Azure Cosmos DB, Azure Service Bus, Azure Key Vault, Azure App Configuration, Azure Container Registry) to ensure they are not exposed to the public internet and are only accessible from within the VNet or peered VNets.
*   **Authentication & Authorization:**
    *   **M365 Agent Identity:** Uses its **Microsoft Entra ID Application Registration (Entra Agent ID)**.
    *   **Agent-to-MCP-Tool Communication:** Secure using **Azure AD tokens (OAuth 2.0 client credentials flow or similar)**. The M365 Agent (client) acquires a token for the MCP Tool (resource/API).
    *   **MCP-Tool-to-Azure-Resource Communication:** Use **Managed Identities** for all Azure resources. MCP Tools deployed to ACA, App Service, or Functions will use their Managed Identity to authenticate with Azure Service Bus, Cosmos DB, Key Vault, etc.
*   **Secrets Management:** All secrets (API keys, connection strings not handled by MI) must be stored in Azure Key Vault.

## 6. .NET Aspire & Deployment (`azd`)

This combination is key to the Nucleus deployment strategy on Azure.
*   **`Nucleus.Aspire.AppHost` (`Program.cs`):**
    *   Defines all M365 Agent projects, MCP Tool projects (or container images).
    *   Adds references to Azure resources (e.g., `builder.AddAzureServiceBus("serviceBus")`, `builder.AddAzureCosmosDB("cosmos")`).
    *   Configures service discovery, environment variables, and connection strings (often referencing Key Vault for production values).
*   **Bicep Templates (`*.bicep`):**
    *   Define all Azure infrastructure (ACR, ACA Environment, App Service Plan, Cosmos DB account, Service Bus namespace, Key Vault, App Configuration store, Azure Firewall, VNets, Private Endpoints, etc.).
    *   Organized into modules for reusability and clarity.
*   **`azure.yaml` (for `azd`):**
    *   Maps services defined in the Aspire AppHost to Azure services defined in Bicep.
    *   Specifies the Bicep files for provisioning.
    *   Defines hooks for `azd` to interact with the Aspire AppHost (e.g., how to get deployment manifests or build container images).
*   **Deployment Workflow & Commands:**
    1.  `azd auth login`: Authenticate with Azure.
    2.  `azd provision`: (Optional, can be part of `up`) Provisions or updates all Azure infrastructure defined in Bicep. This step creates the resources, sets up Managed Identities, and configures network rules.
    3.  `azd deploy`: Deploys the application code.
        *   For containerized services (MCP Tools, M365 Agent backend on ACA): `azd` (often via Aspire tooling) builds Docker images, pushes them to Azure Container Registry (ACR), and deploys to Azure Container Apps.
        *   Configures application settings in ACA/App Service, sourcing values from Key Vault via App Configuration as defined in Aspire.
        *   Sets up permissions for Managed Identities.
    4.  `azd up`: A composite command that typically runs `azd provision` and then `azd deploy`.
*   **Containerization:** MCP Tools and M365 Agent backends are containerized. `azd` and Aspire facilitate building these images and deploying them to Azure Container Registry (ACR) and then to Azure Container Apps.

## 7. Scalability and Resilience in Azure

*   **Compute:** ACA, App Service, and Functions offer various auto-scaling capabilities based on metrics (CPU, memory, queue depth for Service Bus triggers).
*   **Data Stores & Messaging:** Azure Cosmos DB and Azure Service Bus are designed for high availability and elastic scalability. Choose appropriate SKUs and configure partitioning/throughput.
*   **Resiliency Patterns:** Implement standard .NET resiliency patterns (e.g., using Polly for retries, circuit breakers) within M365 Agent and MCP Tool code. .NET Aspire health checks integrate with Azure monitoring.
*   **Regional Redundancy:** For higher tiers of resilience, consider multi-region deployments, though this significantly increases complexity and cost.

## 8. Cost Management Considerations in Azure

*   **Right-Sizing:** Choose appropriate SKUs and instance sizes for ACA, App Service, Cosmos DB, Service Bus, etc.
*   **Serverless Options:** Leverage serverless aspects of ACA and Azure Functions to pay for consumption.
*   **Azure Cost Management + Billing:** Regularly monitor costs, set budgets, and use Azure Advisor recommendations.
*   **Reserved Instances/Savings Plans:** Consider for predictable workloads to reduce costs.
*   **Development/Test Environments:** Use lower-cost SKUs or emulators (facilitated by Aspire for local dev) where possible.

## 9. Observability

*   **Azure Monitor:** The central observability solution.
    *   **Application Insights:** Collects logs, traces (OpenTelemetry from Aspire), and metrics from all deployed applications.
    *   **Log Analytics:** Query logs for troubleshooting and analysis.
    *   **Dashboards & Alerts:** Create dashboards for monitoring key metrics and set up alerts for critical issues.
*   **.NET Aspire Dashboard:** Provides excellent local observability, and OpenTelemetry integration ensures telemetry flows to Azure Monitor in deployed environments.

## 10. Conclusion

Deploying the Nucleus platform to Microsoft Azure, leveraging .NET Aspire for orchestration and `azd` for streamlined provisioning and deployment, offers a powerful, scalable, and secure solution. This approach aligns with modern cloud-native best practices and fully supports the distributed architecture of M365 Persona Agents and backend MCP Tools. Careful planning of Azure service selection, networking, security, and cost management is essential for a successful deployment.
