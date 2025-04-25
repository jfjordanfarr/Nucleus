---
title: Architecture - Hosting Strategy: Self-Host (Home Network)
description: Details the strategy and considerations for deploying Nucleus OmniRAG on a local home network using Docker.
version: 1.3
date: 2025-04-23
---

# Nucleus OmniRAG: Self-Hosted Home Network Deployment Strategy

## 1. Introduction

This document outlines the architecture and considerations for deploying the Nucleus OmniRAG system within a home network environment, as part of the overall [Deployment Architecture](../07_ARCHITECTURE_DEPLOYMENT.md). It primarily targets individual users or developers who wish to run the system locally on their own hardware. This approach leverages Docker Desktop (or a similar containerization platform) for packaging and running the necessary components.

**Goal:** Provide a self-contained Nucleus instance accessible within the user's home network, minimizing reliance on cloud services for core infrastructure while still potentially using cloud AI providers.

## 2. Feasibility Assessment

Self-hosting Nucleus on a home network is generally **feasible**, especially for access *within* the local network. Key considerations include:

*   **Outbound Connectivity:** Required for the Nucleus API/Workers to reach external services like the Google Gemini API. Standard home internet connections usually permit this.
*   **Inbound Connectivity (Local):** Accessing the Nucleus API from other devices *on the same home network* is straightforward (using the host machine's local IP address and the mapped port). **Client adapters (e.g., `Nucleus.Console`, test scripts) running on the local network would connect to this exposed API endpoint.**
*   **Inbound Connectivity (External - Optional/Complex):** Allowing access from *outside* the home network introduces security risks and complexity. This is typically needed if:
    *   An *external client adapter* (e.g., a cloud-hosted Teams Bot) needs to send requests *to* the self-hosted `Nucleus.Api`.
    *   An adapter *itself* needs to be externally reachable (e.g., the Teams Bot messaging endpoint receiving webhooks from Microsoft), and this adapter then needs to communicate with the self-hosted `Nucleus.Api` (which might require the API also being externally accessible or reachable through other secure means like VPN/tunneling).
    *   This generally requires: Dynamic DNS (DDNS), Port Forwarding, and robust security measures (Firewall, HTTPS, Reverse Proxy).
*   **ISP Terms:** Some ISPs prohibit hosting servers on residential connections.
*   **Hardware Resources:** Requires a reasonably powerful host machine (CPU, ample RAM, sufficient disk space) to run Docker Desktop and multiple containers concurrently (API, database, queue, potentially workers).

## 3. Core Component Mapping (Docker Containers)

Based on the [Deployment Abstractions](../ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md), the system can be run using standard Docker containers:

1.  **Containerized Compute Runtime:**
    *   **Technology:** **Docker Desktop** (Windows, macOS) or Docker Engine (Linux).
    *   **Components:**
        *   Custom-built Docker images for `Nucleus.Api`, background workers, etc., based on the .NET SDK/runtime base images.
        *   Orchestration via `docker-compose.yml` for defining and linking services.

2.  **Asynchronous Messaging Queue:**
    *   **Technology:** **Official RabbitMQ Docker Image** or **NATS Docker Image**.
    *   **Rationale:** Widely used, reliable open-source message brokers available as ready-to-run containers.
    *   **Configuration:** Define as a service in `docker-compose.yml`, configure Nucleus apps to connect to the container's service name on the Docker network.

3.  **Document & Vector Database:**
    *   **Technology:** **Official PostgreSQL Docker Image** + **`pgvector` extension** (requires a compatible image or adding the extension).
    *   **Rationale:** Robust open-source relational database with a mature vector search extension. Feasible to run in a container.
    *   **Configuration:** Define as a service in `docker-compose.yml`, configure persistent volume mapping for data durability, set up user/database, install `pgvector` extension.
    *   **Alternative:** Running other vector databases like Qdrant or Weaviate as separate containers.

## 4. External Dependencies

*   **AI Services (LLM & Embeddings):**
    *   **Provider:** **Google Gemini API** (or others like Azure OpenAI, local models via Ollama container).
    *   **Integration:** Outbound API calls from Nucleus containers. Requires host machine to have internet access.
    *   **Credentials:** Pass API keys securely via Docker secrets or environment variables managed through `docker-compose.yml`.

## 5. Networking Configuration (`docker-compose.yml`)

*   **Docker Network:** Define a custom bridge network for all Nucleus services to communicate via service names.
*   **Port Mapping:** Expose the `Nucleus.Api` container's port to a port on the host machine (e.g., map host `5000` to container `8080`).
*   **Access:** Other devices on the home network access the API via `http://<host-machine-local-ip>:<mapped-host-port>`. **External clients would use the public IP/DDNS name and forwarded port.**

## 6. Security Considerations

*   **Local Access:** Generally safe within a trusted home network, but be mindful of who has access.
*   **External Access:** High risk. Requires careful configuration of firewalls, HTTPS, authentication, and understanding the implications of exposing your home network.
*   **Secrets:** Avoid hardcoding secrets. Use Docker secrets or environment variables (potentially loaded from a `.env` file excluded from source control).

## 7. Setup and Management

*   **`docker-compose.yml`:** The central piece for defining services, networks, volumes, and environment variables.
*   **Building Images:** Requires Dockerfiles for each custom .NET application (`Nucleus.Api`, workers).
*   **Running:** Use `docker-compose up -d` to start the services.
*   **Maintenance:** Requires managing Docker (updates, resource usage), container images, persistent data volumes, and backups.

## 8. Summary

Self-hosting Nucleus on a home network via Docker is viable and provides maximum data privacy and control for local use **by ensuring the core `Nucleus.Api` and data stores remain within the user's network**. It requires technical proficiency with Docker, managing different containerized services, and understanding basic home networking. While outbound connections (for AI APIs) are easy, enabling reliable and secure *inbound* access from the internet (for external clients or certain adapter types) poses significant challenges and security risks that users must carefully consider and mitigate.

---
