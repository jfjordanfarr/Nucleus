---
title: Architecture - Hosting Strategy: Azure
description: Details the specific Azure services and configuration for deploying the Nucleus system.
version: 1.5
date: 2025-04-27
parent: ../07_ARCHITECTURE_DEPLOYMENT.md
---

# Nucleus: Azure Deployment Strategy

## 1. Introduction

This document specifies the recommended architecture for deploying the Nucleus system using Microsoft Azure services. It builds upon the [Deployment Abstractions](../ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md) and refines the concepts initially outlined in the [Overall Deployment Architecture](../07_ARCHITECTURE_DEPLOYMENT.md).

**Goal:** Leverage Azure's PaaS offerings for scalability, manageability, and integration while using Google Gemini for AI services.

## 2. Core Azure Service Mapping

Based on the [Deployment Abstractions](../ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md), the following Azure services are chosen:

1.  **Containerized Compute Runtime:**
    *   **Service:** **Azure Container Apps (ACA)**
    *   **Rationale:** Serverless container orchestration providing auto-scaling (including scale-to-zero), simplified networking (managed environment), built-in Dapr integration (optional but useful), and HTTPS ingress.
    *   **Components Hosted:**
        *   `Nucleus.Api` (Primary web API)
        *   Background processing workers (consuming from queues)
        *   Future platform adapters (e.g., Teams Bot backend)
    *   **Alternative:** Azure Kubernetes Service (AKS) - Provides more control but significantly increases management overhead.

2.  **Asynchronous Messaging Queue:**
    *   **Service:** **Azure Service Bus (Standard Tier or higher)**
    *   **Entity Type:** **Queue**
    *   **Rationale:** Reliable enterprise messaging for decoupling long-running or asynchronous tasks. Features like dead-lettering and competing consumers are beneficial.
    *   **Implementation:** [`Nucleus.Services.Api/.../AzureServiceBusPublisher.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/Infrastructure/Messaging/AzureServiceBusPublisher.cs)
    *   **Usage (Asynchronous Tasks):** Primarily used for scenarios like bulk ingestion or background processing initiated by the API. For example, an adapter might make a direct API call to *initiate* a large ingestion, and the API service might then place detailed work items onto a Service Bus queue for its background workers (running in ACA) to process asynchronously.
    *   **Note on Direct API Calls:** For standard, interactive client requests (e.g., processing a chat message), Adapters typically make direct, synchronous or asynchronous **HTTPS calls** to the `Nucleus.Services.Api` endpoints (e.g., `POST /api/interactions`) hosted in ACA. Service Bus is generally reserved for decoupling background work, not replacing the primary request/response API interaction pattern.
    *   **Alternative:** Azure Storage Queues - Simpler and cheaper, but fewer features.

3.  **Document & Vector Database:**
    *   **Service:** **Azure Cosmos DB (NoSQL API)**
    *   **Rationale:** Globally distributed, multi-model database with flexible schema, ideal for storing `ArtifactMetadata` and `PersonaKnowledgeEntry`. Offers integrated vector search capabilities (currently in preview but expected GA), eliminating the need for a separate vector database.
    *   **Implementation:** [`Nucleus.Infrastructure.Persistence/.../CosmosDbArtifactMetadataRepository.cs`](../../../../src/Nucleus.Infrastructure/Data/Nucleus.Infrastructure.Persistence/Repositories/CosmosDbArtifactMetadataRepository.cs), DI Setup: [`Nucleus.Services.Api/WebApplicationBuilderExtensions.cs`](../../../../src/Nucleus.Services/Nucleus.Services.Api/WebApplicationBuilderExtensions.cs)
    *   **Usage:** Storing structured/semi-structured metadata and derived knowledge including vector embeddings.
    *   **Alternative:** Azure PostgreSQL Flexible Server with `pgvector` - Viable, but requires managing relational schema and scaling more explicitly.

## 3. External Dependencies

*   **AI Services (LLM & Embeddings):**
    *   **Provider:** **Google Gemini API**
    *   **Integration:** Direct API calls from ACA compute instances to the Google Cloud APIs.
    *   **Consideration:** Network latency between Azure and Google Cloud. Ensure secure handling of Google API keys/credentials (e.g., via Azure Key Vault).

## 5. Networking & Security

*   **ACA Environment:** Deploy within an ACA Environment, potentially linked to a Virtual Network for enhanced security and control if needed (e.g., using Private Endpoints for Service Bus, Cosmos DB).
*   **Ingress:** Managed HTTPS ingress provided by ACA.
*   **Secrets Management:** Utilize **Azure Key Vault** for storing connection strings, API keys (including Google Gemini keys), and other secrets. Access Key Vault using Managed Identities from ACA.
    *   **Implementation Status:** *Not Yet Implemented*. Code integration using `Azure.Security.KeyVault` and `Azure.Identity` is pending.
*   **Authentication:** Implement robust authentication, likely using Azure AD (Entra ID) for user and potentially service principal authentication ([Architecture - Security](../06_ARCHITECTURE_SECURITY.md)).
*   **Bot Framework Security:** For specific security considerations when deploying Bot Framework applications (like the Teams Adapter) within this Azure environment, refer to: [Secure Bot Framework Azure Deployment](../../../HelpfulMarkdownFiles/Secure-Bot-Framework-Azure-Deployment.md).
*   **Monitoring:** Azure Monitor for logs, metrics, and application insights.

## 6. Cost Considerations

*   **Cost:** Primarily driven by ACA compute hours, Cosmos DB RU/s and storage, Service Bus operations. Utilize scaling features (especially scale-to-zero for ACA) to manage costs.
*   **Management:** PaaS services reduce operational overhead compared to IaaS or self-hosting.

## 7. Monitoring and Observability

*   **Service:** **Azure Monitor (Application Insights)**
    *   **Rationale:** Integrated monitoring solution for collecting logs, metrics, and traces from ACA, Cosmos DB, Service Bus, etc.
    *   **Usage:** Application performance monitoring (APM), log analytics, alerting.
    *   **Implementation Status:** *Not Yet Implemented*. Code integration using `Microsoft.ApplicationInsights` SDK is pending.

## 8. Infrastructure as Code (IaC)

*   **Tool:** **Bicep** or **Terraform**
    *   **Rationale:** Define and deploy Azure resources consistently and repeatably.
    *   **Recommendation:** Use Bicep for Azure-native focus, Terraform for potential multi-cloud scenarios (though this document focuses on Azure).
    *   **Implementation Status:** *Not Yet Implemented*. No `.bicep` or `.tf` files currently exist in the repository.

## 9. CI/CD

*   **Platform:** **Azure DevOps Pipelines** or **GitHub Actions**
    *   **Workflow:** Build container images, push to Azure Container Registry (ACR), deploy to Azure Container Apps.
    *   **Implementation Status:** *Not Yet Implemented*. No `azure-pipelines.yml` or GitHub Actions workflows currently exist in the repository.

## 10. Summary

This Azure-based deployment provides a robust, scalable, and manageable PaaS solution leveraging integrated services well-suited for the Nucleus architecture's core requirements (Compute, Messaging, Database).

---
