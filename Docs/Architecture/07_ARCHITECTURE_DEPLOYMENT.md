---
title: Nucleus Deployment Architecture Overview
description: Provides an overview of deployment strategies and links to detailed architectures for Azure, Cloudflare, and Self-Hosting.
version: 2.7
date: 2025-05-06
---

# Nucleus: Deployment Architecture Overview


This document provides a high-level overview of the deployment architecture for the Nucleus OmniRAG system, complementing the [System Architecture Overview](./00_ARCHITECTURE_OVERVIEW.md). It establishes the core principles and links to detailed strategies for specific deployment targets.

## 1. Core Philosophy

The deployment architecture prioritizes:

*   **Flexibility:** Enabling deployment across different environments (cloud providers, self-hosted) based on needs.
*   **Leveraging Managed Services (where applicable):** Minimizing infrastructure management overhead when using cloud providers.
*   **Containerization:** Packaging application components into containers for consistent deployment and scaling.
*   **Scalability:** Designing components to scale independently based on load.
*   **Configuration Management:** Securely managing configuration and secrets across environments.

## 2. Abstract Deployment Requirements

Regardless of the specific target environment, Nucleus fundamentally requires a set of core infrastructure capabilities. These abstract requirements are defined in detail in:

*   **[Deployment Abstractions](./Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md):** Defines the conceptual building blocks (Compute Runtime, Messaging Queue, Document/Vector Database) needed, independent of specific providers. The primary code implementation of the Compute Runtime is the [Nucleus API Service](../../src/Nucleus.Services/Nucleus.Services.Api/Program.cs).

Understanding these abstractions is key to mapping the system onto different infrastructure platforms.
The **Compute Runtime** primarily serves to host the central **`Nucleus.Services.Api`**, which acts as the single point of interaction for all clients and adapters according to the [API-First](./00_ARCHITECTURE_OVERVIEW.md#1-core-principles) principle.

## 3. Specific Deployment Strategies

Based on the abstract requirements, detailed deployment strategies have been outlined for common target environments. These documents specify the chosen services, configuration considerations, networking, security, and IaC approaches for each:

*   **[Azure Deployment Strategy](./Deployment/Hosting/ARCHITECTURE_HOSTING_AZURE.md):** Details the recommended architecture using Azure services like Container Apps, Cosmos DB, and Service Bus.
*   **[Cloudflare Deployment Strategy](./Deployment/Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md):** Explores a potential architecture leveraging Cloudflare Workers, Queues, Vectorize, and D1, focusing on cost-efficiency and edge performance.
*   **[Self-Hosted Home Network Strategy](./Deployment/Hosting/ARCHITECTURE_HOSTING_SELFHOST_HOMENETWORK.md):** Outlines deploying Nucleus locally using Docker containers for components like RabbitMQ/NATS and PostgreSQL/pgvector.

Refer to these specific documents for implementation details relevant to your chosen deployment target.

## 4. CI/CD Strategy (Open Source)

Given the open-source nature of Nucleus, the Continuous Integration and Continuous Delivery (CI/CD) strategy focuses on validating code quality and producing consumable artifacts (like Docker images) rather than deploying to a specific instance. The approach is detailed in:

*   **[CI/CD Strategy for Open Source](./Deployment/ARCHITECTURE_DEPLOYMENT_CICD_OSS.md):** Outlines the use of GitHub Actions for building, testing, packaging, and releasing versioned artifacts securely.

## 5. Next Steps (Cross-Cutting Concerns)

Regardless of the chosen deployment strategy, several key steps and considerations remain crucial for a successful implementation:

1.  **Infrastructure as Code (IaC):** Develop templates (e.g., Bicep, Terraform, `docker-compose.yml`) to provision the chosen infrastructure repeatably, fulfilling a key aspect of [Phase 4 Maturity Requirements](../Requirements/04_REQUIREMENTS_PHASE4_MATURITY.md#33-enterprise-readiness--admin-features) (REQ-P4-ADM-005).
2.  **CI/CD Pipeline:** Create automated pipelines (e.g., Azure DevOps Pipelines, GitHub Actions) to build application artifacts (container images, Worker bundles), push them to a registry (if applicable), and deploy updates to the target environment.
3.  **Monitoring Setup:** Configure appropriate monitoring and logging solutions (e.g., Application Insights, Cloudflare Analytics, Prometheus/Grafana) for observability.
4.  **Cost Analysis:** Estimate and monitor costs based on expected usage patterns for the chosen services and tiers.
5.  **Backup and Recovery:** Define and test backup/recovery strategies for databases, as required by [Phase 4 Maturity Requirements](../Requirements/04_REQUIREMENTS_PHASE4_MATURITY.md#33-enterprise-readiness--admin-features) (REQ-P4-ADM-006).

---
