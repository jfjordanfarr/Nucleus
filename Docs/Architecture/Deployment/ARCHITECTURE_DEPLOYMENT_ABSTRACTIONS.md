---
title: Architecture - Deployment Abstractions
description: Defines the core abstract components required for deploying the Nucleus system, independent of specific cloud providers.
version: 1.2
date: 2025-04-27
parent: ../07_ARCHITECTURE_DEPLOYMENT.md
---

# Nucleus: Deployment Abstractions

## 1. Introduction

This document outlines the fundamental, abstract components required to deploy and operate the Nucleus system, situated within the overall [Deployment Architecture](../07_ARCHITECTURE_DEPLOYMENT.md). The goal is to define the necessary capabilities in a provider-agnostic way, allowing for flexibility in choosing deployment targets based on factors like cost, performance, existing infrastructure, or specific features.

While specific implementations will leverage provider-specific services (detailed in separate documents like [Azure Deployment](./Hosting/ARCHITECTURE_HOSTING_AZURE.md) and [Cloudflare Deployment](./Hosting/ARCHITECTURE_HOSTING_CLOUDFLARE.md)), understanding these core abstractions is crucial for maintaining architectural consistency and enabling potential portability.

## 2. Core Deployment Units

The Nucleus system fundamentally requires the following types of infrastructure components:

1.  **Containerized Compute Runtime:**
    *   **Requirement:** A platform capable of running the core .NET applications packaged as OCI-compliant containers (e.g., `Nucleus.Api`, background processors, future adapters).
    *   **Key Needs:** Scalability (manual or automatic), reliable execution, integration with networking and other services.
    *   **Examples:** Azure Container Apps, Azure Kubernetes Service, AWS ECS/EKS, Google Cloud Run/GKE, Cloudflare Workers (via Workers for Platforms or similar container support), Self-hosted Kubernetes/Docker.

2.  **Asynchronous Messaging (Queue/Topic):**
    *   **Purpose:** To decouple components (e.g., adapters from the core API service), enable asynchronous processing, and reliably deliver requests or events between services.
    *   **Abstraction:** A message queuing or topic-based system.
        *   **Queues:** Point-to-point delivery, where one consumer processes a message.
        *   **Topics (Pub/Sub):** Broadcast delivery, where multiple subscribers can receive copies of a message.
        *   **Nucleus Abstraction:** The [`IMessageQueuePublisher<T>`](cci:2://file:///d:/Projects/Nucleus/Nucleus.Abstractions/IMessageQueuePublisher.cs:18:0-30:1) interface defines the contract for publishing messages. This allows different implementations based on the chosen provider.
    *   **Key Capabilities:**
        *   Guaranteed delivery (at-least-once).
        *   Decoupling of publishers and consumers/subscribers.
        *   Support for competing consumers (for scaling queue processing).
    *   **Examples:**
        *   Azure Service Bus Queues & Topics (The *current* API Service implementation uses a Queue via [`AzureServiceBusPublisher<T>`](cci:2://file:///d:/Projects/Nucleus/Nucleus.ApiService/Infrastructure/Messaging/AzureServiceBusPublisher.cs:23:0-99:1))
        *   Google Cloud Pub/Sub
        *   RabbitMQ (using Direct or Topic exchanges)
        *   NATS / NATS JetStream
        *   AWS SQS / SNS
        *   Cloudflare Queues
    *   **Selection Criteria:** Reliability, scalability, ordering requirements (if any), cost, integration with the chosen compute runtime, latency requirements.

3.  **Document & Vector Database:**
    *   **Requirement:** A database solution capable of storing both structured/semi-structured metadata (`ArtifactMetadata`) and the derived knowledge (`PersonaKnowledgeEntry`), including vector embeddings for semantic search.
    *   **Key Needs:** Scalability, efficient querying of structured data, efficient vector similarity search (ANN), flexible schema (for `PersonaKnowledgeEntry`), persistence, backup/recovery.
    *   **Examples:** Azure Cosmos DB (NoSQL API + Integrated Vector Search), PostgreSQL with `pgvector`, Elasticsearch/OpenSearch, specialized vector databases (Pinecone, Weaviate, Qdrant, Milvus), Cloudflare Vectorize (for vectors) potentially paired with D1 or R2 (for structured/document data).

## 3. External Dependencies

Beyond the core infrastructure, Nucleus relies on:

1.  **AI Services (LLM & Embeddings):**
    *   **Requirement:** Access to Large Language Models for generation/analysis and embedding models for vector creation.
    *   **Current Choice:** Google Gemini API.
    *   **Alternatives:** Azure OpenAI, OpenAI API, Anthropic Claude, local models (via Ollama etc. - requires significant compute).

## 4. Cross-Cutting Concerns

Any deployment must also address:

*   **Networking & Security:** Secure communication between components, ingress control, potential VNet integration or private endpoints, firewalling.
*   **Authentication & Authorization:** Securely authenticating users and services.
*   **Monitoring & Logging:** Collecting logs, metrics, and traces for observability.
*   **Infrastructure as Code (IaC):** Provisioning and managing infrastructure consistently (e.g., Bicep, Terraform, Pulumi).
*   **CI/CD:** Automating the build, test, and deployment process.

## 5. Provider Flexibility Considerations

Defining these abstractions allows for strategic choices:

*   **Azure:** Offers a mature, integrated ecosystem with services like ACA, Cosmos DB, and Service Bus providing a well-rounded PaaS solution.
*   **Cloudflare:** Presents a compelling alternative, particularly for cost-sensitive scenarios (e.g., education, prototypes) due to its generous free tiers and built-in security features (WAF, DDoS protection). Services like Workers, Queues, and Vectorize map well to the core needs, though the document database aspect requires careful consideration (e.g., pairing Vectorize with D1 or R2).

Detailed provider-specific architectures should reference these abstractions.

---
