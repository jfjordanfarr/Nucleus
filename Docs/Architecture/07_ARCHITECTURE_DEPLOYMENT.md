# Nucleus OmniRAG: Deployment & Provisioning Architecture

**Version:** 1.0
**Date:** 2025-04-13

This document outlines the target deployment architecture for the Nucleus OmniRAG system, focusing on Azure infrastructure and aiming for efficiency and scalability.

## 1. Core Philosophy: Managed Services & Containerization

The architecture prioritizes:

*   **Leveraging Azure PaaS/Managed Services:** Minimize infrastructure management overhead by using services like Azure Container Apps, Cosmos DB, Azure Key Vault, and Azure Functions.
*   **Containerization:** Package application components (API, Adapters, potentially processing workers) into containers for consistent deployment and scaling.
*   **Scalability:** Design components to scale independently based on load.
*   **Configuration Management:** Securely manage configuration and secrets.

## 2. Target Deployment Units (Azure)

The system is composed of the following primary deployable units within Azure:

1.  **Azure Container Apps (ACA) Environment:** Provides the core runtime for containerized application components.
    *   **Main Application Container App:** Hosts the primary Nucleus backend service. This container runs:
        *   The main **ASP.NET Core Web API** endpoint.
        *   **Platform Adapter** logic (e.g., Teams Bot endpoint, potentially future Web App backend).
        *   The core **Processing Pipeline Orchestration** logic (can be triggered via API calls or internal events).
        *   **Persona implementations** (loaded dynamically).
        *   **Content Synthesis Logic** (e.g., the Plaintext processor using an LLM).
    *   **(Optional) Background Processing Container App:** For self-hosted or high-volume scenarios, a separate ACA instance could handle long-running background ingestion tasks triggered by message queues, keeping the main API responsive.
2.  **Azure Cosmos DB:** The primary database for storing:
    *   `ArtifactMetadata` (potentially in a dedicated container).
    *   `PersonaKnowledgeEntry` documents (in persona-specific containers).
    *   Configuration or operational state if needed.
    *   Utilizes the **Serverless** capacity mode for cost-effectiveness in varying load scenarios, or Provisioned Throughput for predictable high load.
3.  **(Optional but Recommended) Azure Functions App:** Dedicated to heavy, potentially long-running *initial content extraction* tasks that benefit from a consumption plan or specialized scaling.
    *   **Function Triggers:** HTTP trigger (called by the main ACA pipeline), Queue trigger (for background processing scenarios).
    *   **Function Logic:** Hosts `IContentExtractor` implementations for complex types like PDF (using libraries like iTextSharp/PdfPig), DOCX, and potentially integrates with Azure AI Vision (OCR) or Azure AI Document Intelligence.
    *   **Output:** Returns extracted text or structured data back to the calling pipeline (e.g., via HTTP response or placing results in a queue/storage for the ACA to pick up).
4.  **Azure Key Vault:** Securely stores all secrets, API keys, and connection strings.
5.  **Azure AI Services:** External dependencies used by the application:
    *   **Azure OpenAI Service** or equivalent (e.g., Google Gemini via API): For LLM chat completions and embedding generation.
    *   **(Optional) Azure AI Vision / Document Intelligence:** Used by the Azure Functions App for advanced content extraction.
6.  **(Optional) Azure Service Bus / Azure Storage Queues:** For orchestrating background processing tasks, decoupling the main API from ingestion workers (more relevant for self-hosted or high-throughput scenarios).
7.  **(Optional) Azure Application Insights:** For monitoring, logging, and diagnostics.
8.  **(Optional) Azure API Management:** To act as a gateway, providing security, rate limiting, and a unified frontend for APIs if needed (more relevant for complex or publicly exposed scenarios).

## 3. Minimal Viable Deployment Configuration

For many scenarios, particularly initial cloud-hosted deployments or smaller self-hosted instances, a highly efficient minimal setup is possible:

*   **1 x Azure Container Apps Environment**
    *   **1 x Main Application Container App:** Running the combined API, Adapters, Pipeline Orchestration, Synthesis, and Personas.
*   **1 x Azure Cosmos DB Account:** Using Serverless mode, with containers for metadata and each active persona.
*   **1 x Azure Functions App (Consumption Plan):** Hosting extractors for PDF, DOCX, and potentially OCR/Document Intelligence integration.
*   **1 x Azure Key Vault:** For secrets.
*   **Azure AI Service Endpoints:** (OpenAI/Gemini).

**Diagram (Conceptual Mermaid):**

```mermaid
graph TD
    subgraph Azure Resource Group
        subgraph ACA Environment
            ACA[Main App Container App <br/> (API, Adapters, Pipeline, Synthesis, Personas)]
        end
        Cosmos[(Azure Cosmos DB <br/> Metadata, Knowledge)]
        Functions[Azure Functions App <br/> (PDF/DOCX Extraction, OCR)]
        KV[(Azure Key Vault)]
    end

    subgraph External Azure Services
        AISvc[(Azure AI Services <br/> OpenAI/Gemini, Vision)]
    end

    User -->|HTTPS| ACA
    ACA -->|API Call/Event| Functions
    Functions -->|Extracted Data| ACA
    ACA -->|CRUD| Cosmos
    ACA -->|Secrets| KV
    ACA -->|LLM/Embeddings| AISvc
    Functions -->|OCR/Layout| AISvc

```

## 4. Scalability

*   **ACA:** Can scale out instances based on HTTP traffic, CPU/Memory usage, or queue length (if using KEDA scalers).
*   **Functions:** Consumption plan scales automatically. Premium plan offers more control.
*   **Cosmos DB:** Serverless scales automatically. Provisioned throughput can be scaled manually or via autoscale.
*   **Azure AI Services:** Managed services with their own scaling characteristics.

## 5. Configuration & Secrets Management

*   Leverage Azure Key Vault for all secrets.
*   Use Managed Identity for the ACA and Functions App to access Key Vault and other Azure resources securely without connection strings in configuration.
*   Use `appsettings.json` for non-sensitive configuration, potentially overridden by environment variables set within the ACA/Functions configuration.
*   Consider Azure App Configuration for more complex or centrally managed configuration scenarios.

## 6. Deployment Models

*   **Cloud-Hosted (SaaS):** Typically uses the minimal configuration, potentially scaled up. Focus on ephemeral user sessions, managed identity.
*   **Self-Hosted:** May involve larger ACA instances, potentially dedicated background worker ACAs, Provisioned Throughput on Cosmos DB, integration with VNETs, and use of Azure Service Bus for robust background processing orchestration.

## 7. Next Steps

1.  **Infrastructure as Code (IaC):** Develop Bicep or Terraform templates to provision this infrastructure repeatably.
2.  **CI/CD Pipeline:** Create pipelines (e.g., Azure DevOps Pipelines, GitHub Actions) to build container images, push them to a registry (e.g., ACR), and deploy updates to ACA and Functions.
3.  **Monitoring Setup:** Configure Application Insights for logging and performance monitoring.
4.  **Cost Analysis:** Estimate costs based on expected usage patterns for different tiers/scales.
