---
title: "MCP Tool: PersonaBehaviourConfig Server Architecture"
description: "Defines the architecture for the Nucleus_PersonaBehaviourConfig_McpServer, responsible for managing dynamic persona configurations."
version: 1.0
date: 2025-05-27
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
    - ../../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md
    - ../../../CoreNucleus/03_DATA_PERSISTENCE_STRATEGY.md
    - ../../../Security/01_SECURITY_OVERVIEW_AND_GOVERNANCE.md
    - ../../../Agents/01_M365_AGENTS_OVERVIEW.md
---

# MCP Tool: PersonaBehaviourConfig Server Architecture

## 1. Purpose & Scope

The `Nucleus_PersonaBehaviourConfig_McpServer` (Persona Behaviour Configuration MCP Server) is a dedicated Model Context Protocol (MCP) server responsible for providing M365 Persona Agents with access to dynamic, runtime persona configuration data. This data, primarily stored in Azure Cosmos DB, includes elements like persona-specific system prompts, LLM model preferences, temperature settings, and other behavioral parameters defined in the [Persona Configuration Schema](../../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md).

This server allows for centralized management and dynamic updates to persona behaviors without requiring redeployment of the M365 Agent or the core Nucleus backend. It ensures that Personas can adapt their interactions and processing logic based on the latest configurations.

## 2. Key MCP Operations

The server exposes the following primary MCP operations:

### 2.1. `PersonaBehaviourConfig.GetDynamicConfiguration`

*   **Description:** Retrieves the dynamic configuration settings for a specified Persona.
*   **Request Parameters:**
    *   `personaId` (string): The unique identifier of the Persona.
    *   `tenantId` (string): The tenant identifier for data scoping.
    *   `configurationKeys` (array of strings, optional): Specific configuration keys to retrieve. If null or empty, all dynamic configurations for the persona are returned.
*   **Response:**
    *   A JSON object containing the requested configuration key-value pairs.
*   **Security Context:** Requires a valid Azure AD token for an M365 Persona Agent. The server validates the token and ensures the agent has permissions to access configuration data for the specified `tenantId`.

### 2.2. `PersonaBehaviourConfig.UpdateDynamicConfiguration`

*   **Description:** Allows authorized administrative tools or processes to update specific dynamic configuration settings for a Persona. *This operation is typically restricted and not directly callable by standard M365 Persona Agents during normal user interactions.*
*   **Request Parameters:**
    *   `personaId` (string): The unique identifier of the Persona.
    *   `tenantId` (string): The tenant identifier for data scoping.
    *   `configurationsToUpdate` (JSON object): Key-value pairs of configurations to update.
*   **Response:**
    *   Success/failure status.
*   **Security Context:** Requires a highly privileged Azure AD token, typically associated with an administrative application or a specific service principal with rights to modify persona configurations. Standard M365 Persona Agent tokens will be rejected.

## 3. Internal Logic & Processing

*   **Configuration Retrieval:** Upon receiving a `GetDynamicConfiguration` request, the server queries the `PersonaConfigurations` container in Azure Cosmos DB (partitioned by `tenantId` and `personaId`) to fetch the relevant configuration document or specific fields.
*   **Configuration Update:** For `UpdateDynamicConfiguration` requests, after stringent authorization checks, the server performs a partial update (patch) operation on the corresponding Cosmos DB document.
*   **Caching (Future Consideration):** A caching layer (e.g., Azure Cache for Redis) might be introduced to reduce latency for frequently accessed configurations.

## 4. Dependencies

*   **Azure Cosmos DB:** Primary data store for dynamic persona configurations. Accessed via the .NET SDK for Cosmos DB.
*   **Azure AD (Microsoft Entra ID):** For authenticating incoming MCP requests from M365 Persona Agents and administrative tools.
*   **Nucleus MCP Libraries:** For MCP message serialization/deserialization and server hosting.
*   **`Nucleus.Shared.Kernel.Abstractions`:** For shared models and interfaces, including those related to persona configuration.

## 5. Security Model

*   **Authentication:** All MCP requests must be authenticated using Azure AD tokens. The server validates the token signature, audience, and issuer.
*   **Authorization:**
    *   `GetDynamicConfiguration`: Requires a token from a recognized M365 Persona Agent or a service principal with read access to persona configurations. Tenant isolation is enforced based on claims in the token and the `tenantId` parameter.
    *   `UpdateDynamicConfiguration`: Requires a token with specific administrative privileges (e.g., a dedicated application role or scope) to modify configurations. This prevents unauthorized changes by standard agents.
*   **Data at Rest:** Secured by Azure Cosmos DB's default encryption.
*   **Data in Transit:** Secured by HTTPS/TLS for MCP communication.

## 6. Data Handling & Persistence

*   **Primary Data Store:** Azure Cosmos DB, `PersonaConfigurations` container.
*   **Data Schema:** Adheres to the structure defined in the [Persona Configuration Schema](../../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md), focusing on the dynamic/runtime aspects.
*   **Partitioning:** Data in Cosmos DB is partitioned by `tenantId` and `personaId` to ensure data isolation and query efficiency.
*   **No Local Persistence:** The MCP server itself is stateless and does not persist configuration data locally. All data is read from and written to Cosmos DB.

## 7. Deployment & Hosting

*   **Hosting:** Deployed as an Azure App Service or Azure Container Instance, configured as an MCP server endpoint.
*   **Scalability:** Leverages Azure App Service/ACI scaling capabilities and Cosmos DB's elastic scalability.
*   **Configuration:** Application settings include Cosmos DB connection strings, Azure AD application IDs/secrets for token validation, and MCP server binding information.

## 8. Future Considerations & Potential Enhancements

*   **Configuration Versioning:** Implement a mechanism to version persona configurations, allowing rollback to previous states.
*   **Advanced Caching Strategies:** Implement more sophisticated caching with appropriate invalidation mechanisms for frequently accessed configurations.
*   **Auditing:** Add detailed audit logging for configuration changes (`UpdateDynamicConfiguration` calls).
*   **Bulk Operations:** Support for bulk retrieval or update of configurations for multiple personas (with appropriate security controls).
*   **Schema Validation Service:** Integration with a service that validates configuration updates against the defined JSON schema before persisting them.
