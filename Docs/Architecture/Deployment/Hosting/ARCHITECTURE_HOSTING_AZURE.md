---
title: Architecture - Hosting Strategy: Azure
description: Details the specific Azure services and configuration for deploying the Nucleus OmniRAG system.
version: 1.2
date: 2025-04-22
---

# Nucleus OmniRAG: Azure Deployment Strategy

## 1. Introduction

This document specifies the recommended architecture for deploying the Nucleus OmniRAG system using Microsoft Azure services. It builds upon the [Deployment Abstractions](../ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md) and refines the concepts initially outlined in the [Overall Deployment Architecture](../07_ARCHITECTURE_DEPLOYMENT.md).

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
    *   **Rationale:** Reliable enterprise messaging with features like dead-lettering, competing consumers, and good integration with Azure Functions/Container Apps via triggers or KEDA scalers. Suitable for point-to-point delivery of ingestion requests.
    *   **Usage:** Decoupling Platform Adapters (like Teams) from the core API Service. Adapters publish [`NucleusIngestionRequest`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/Models/NucleusIngestionRequest.cs:10:0-49:1) messages to a dedicated queue (e.g., `nucleus-ingestion-requests`).
    *   **Consumer:** The [`ServiceBusQueueConsumerService`](cci:2://file:///d:/Projects/Nucleus/Nucleus.ApiService/Infrastructure/Messaging/ServiceBusQueueConsumerService.cs:24:0-174:1), hosted within the `Nucleus.ApiService` container app instance(s), listens to this queue and processes messages.
    *   **Alternative:** Azure Storage Queues - Simpler and cheaper, but fewer features (e.g., no built-in dead-lettering without extra logic). Azure Service Bus Topics - More suitable for pub/sub scenarios where multiple independent services need to react to the same event; potentially useful in the future but overkill for the current ingestion flow.

3.  **Document & Vector Database:**
    *   **Service:** **Azure Cosmos DB (NoSQL API)**
    *   **Rationale:** Globally distributed, multi-model database with flexible schema, ideal for storing `ArtifactMetadata` and `PersonaKnowledgeEntry`. Offers integrated vector search capabilities (currently in preview but expected GA), eliminating the need for a separate vector database.
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

## 8. Infrastructure as Code (IaC)

*   **Tool:** **Bicep** or **Terraform**
    *   **Rationale:** Define and deploy Azure resources consistently and repeatably.
    *   **Recommendation:** Use Bicep for Azure-native focus, Terraform for potential multi-cloud scenarios (though this document focuses on Azure).

## 9. CI/CD

*   **Platform:** **Azure DevOps Pipelines** or **GitHub Actions**
    *   **Workflow:** Build container images, push to Azure Container Registry (ACR), deploy to Azure Container Apps.

## 10. Summary

This Azure-based deployment provides a robust, scalable, and manageable PaaS solution leveraging integrated services well-suited for the Nucleus OmniRAG architecture's core requirements (Compute, Messaging, Database).

---
