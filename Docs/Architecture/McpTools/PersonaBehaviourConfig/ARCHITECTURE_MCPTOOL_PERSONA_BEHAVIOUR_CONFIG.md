---
title: "MCP Tool: PersonaBehaviourConfig Server Architecture"
description: "Detailed architecture for the Nucleus_PersonaBehaviourConfig_McpServer, responsible for providing M365 Persona Agents access to dynamic, runtime persona configuration data like prompts and behavioral parameters from Azure Cosmos DB."
version: 1.0
date: 2025-05-28
parent: ../01_MCP_TOOLS_OVERVIEW.md
see_also:
  - title: "MCP Tools Overview"
    link: "../01_MCP_TOOLS_OVERVIEW.md"
  - title: "Persona Configuration Schema"
    link: "../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md"
  - title: "Data Persistence Strategy"
    link: "../../CoreNucleus/03_DATA_PERSISTENCE_STRATEGY.md"
  - title: "Security Overview and Governance"
    link: "../../Security/01_SECURITY_OVERVIEW_AND_GOVERNANCE.md"
  - title: "M365 Agents Overview"
    link: "../../Agents/01_M365_AGENTS_OVERVIEW.md"
  - title: "Comprehensive System Architecture"
    link: "../../NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md"
  - title: "Foundations: MCP and M365 Agents SDK Primer"
    link: "../../NorthStarDocs/00_FOUNDATIONS_TECHNOLOGY_PRIMER.md"
  - title: "KnowledgeStore MCP Tool Architecture"
    link: "../KnowledgeStore/ARCHITECTURE_MCPTOOL_KNOWLEDGE_STORE.md" # If used for storage (though direct Cosmos is assumed for this tool)
---

# MCP Tool: PersonaBehaviourConfig Server Architecture

## 1. Purpose and Core Responsibilities

*   **Primary Goal:** To provide M365 Persona Agents with secure and efficient access to dynamic, runtime-modifiable persona configuration data. This enables persona behaviors, prompts, and operational parameters to be updated without requiring agent redeployment or service restarts.
*   **Capabilities Encapsulated:**
    *   Provides CRUD-like operations (specifically Get and Update) for the dynamic portions of the `PersonaConfiguration` object (as defined in `../../CoreNucleus/02_PERSONA_CONFIGURATION_SCHEMA.md`).
    *   Manages storage and retrieval of these dynamic configurations (e.g., system prompts, response guidelines, LLM temperature overrides, feature flags, custom properties for agentic strategies) from a dedicated Azure Cosmos DB container.
*   **Contribution to M365 Agents:** Allows M365 Persona Agents to fetch their behavioral parameters at runtime. This is crucial for:
    *   **Adaptability:** Enabling personas to change behavior based on new requirements or contexts.
    *   **A/B Testing:** Facilitating experimentation with different prompts or strategies.
    *   **Tenant/Persona Customization:** Allowing fine-grained behavioral adjustments per tenant or individual persona instance.
    *   **Centralized Management:** Simplifying the update process for persona behaviors across potentially many agent instances.
    *   **Emergency Overrides:** Providing a mechanism to quickly alter agent behavior if issues arise.

## 2. Key MCP Operations / Tools Exposed

### 2.1. `PersonaBehaviourConfig.GetDynamicConfiguration`
*   **Description:** Retrieves specified dynamic configuration settings for a given Persona and Tenant.
*   **Input (DTO):** `GetDynamicConfigRequest`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.PersonaBehaviourConfig
    public class GetDynamicConfigRequest
    {
        public required string TenantId { get; init; }
        public required string PersonaId { get; init; }
        public List<string>? ConfigurationKeys { get; init; } // Optional: Specific keys to retrieve
    }
    ```
*   **Output (DTO):** `GetDynamicConfigResponse`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.PersonaBehaviourConfig
    public class GetDynamicConfigResponse
    {
        public required string TenantId { get; init; }
        public required string PersonaId { get; init; }
        public required IReadOnlyDictionary<string, System.Text.Json.JsonElement> Configurations { get; init; }
        public string? ErrorMessage { get; init; }
    }
    ```
    *   `Configurations`: Key-value pairs where keys are configuration item names (e.g., "SystemMessage", "ResponseStyleGuide", "LlmTemperature") and values are their JSON representations. Using `JsonElement` allows for flexibility in the structure of each configuration value.

### 2.2. `PersonaBehaviourConfig.UpdateDynamicConfiguration`
*   **Description:** Allows authorized administrative callers (e.g., an admin UI, a CI/CD pipeline, or a privileged backend service) to update specific dynamic configuration settings for a Persona and Tenant. **This operation is highly restricted and not intended for direct use by standard M365 Persona Agents during user interactions.**
*   **Input (DTO):** `UpdateDynamicConfigRequest`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.PersonaBehaviourConfig
    public class UpdateDynamicConfigRequest
    {
        public required string TenantId { get; init; }
        public required string PersonaId { get; init; }
        public required Dictionary<string, System.Text.Json.JsonElement> ConfigurationsToUpdate { get; init; }
        public required string UpdatedBy { get; init; } // Identifier for who/what made the update (for auditing)
    }
    ```
*   **Output (DTO):** `UpdateDynamicConfigResponse`
    ```csharp
    // Defined in Nucleus.Shared.Kernel.Abstractions.Models.Mcp.PersonaBehaviourConfig
    public class UpdateDynamicConfigResponse
    {
        public bool Success { get; init; }
        public List<string>? UpdatedKeys { get; init; }
        public string? ErrorMessage { get; init; }
    }
    ```

## 3. Core Internal Logic & Components

*   **Request Validation:**
    *   Ensures all required fields in requests (e.g., `TenantId`, `PersonaId`) are present and valid.
    *   Performs authorization checks based on the caller's token (see Section 5).
*   **Azure Cosmos DB Interaction (Repository Pattern):**
    *   Utilizes a repository pattern abstracting direct Cosmos DB operations, likely leveraging `Nucleus.Infrastructure.CosmosDb` or a similar dedicated data access layer for this MCP tool.
    *   For `GetDynamicConfiguration`: Queries the `PersonaDynamicConfigurations` Cosmos DB container for the document matching `TenantId` and `PersonaId`. If `ConfigurationKeys` are specified, it may fetch the whole document and filter, or ideally, use server-side projection if feasible for the schema to retrieve only requested fields.
    *   For `UpdateDynamicConfiguration`: Performs partial updates (patch operations) on the Cosmos DB document to modify only the specified `ConfigurationsToUpdate`. This avoids overwriting unrelated configuration fields.
*   **Configuration Merging/Filtering Logic:**
    *   For `GetDynamicConfiguration`: If `ConfigurationKeys` are provided, the logic filters the retrieved Cosmos DB document to return only those keys. If no keys are specified, all retrievable dynamic fields from the document are returned.
*   **Data Transformation (DTO Mapping):** Maps between the DTOs (`GetDynamicConfigRequest`, `UpdateDynamicConfigRequest`, etc.) and the internal Cosmos DB document structures.
*   **Error Handling:** Provides clear error messages in responses for issues like invalid requests, authorization failures, or Cosmos DB access problems.

## 4. Dependencies

*   **Azure Services:**
    *   **Azure Cosmos DB (NoSQL):** The primary data store for dynamic persona configurations. Accessed via the .NET SDK for Cosmos DB.
    *   **Azure Key Vault:** For secure storage and retrieval of the Cosmos DB connection string and other secrets.
    *   **Azure App Configuration:** For managing static configurations of this MCP tool itself (e.g., logging levels, default retry policies for Cosmos DB).
    *   **Azure Application Insights:** For logging, monitoring, and distributed tracing.
*   **External Services/LLMs:**
    *   None directly.
*   **Other Nucleus MCP Tools:**
    *   None directly assumed for core CRUD operations. This tool directly manages its specific configuration data within its designated Cosmos DB container.
*   **Shared Nucleus Libraries:**
    *   `Nucleus.Shared.Kernel.Abstractions`: For DTOs, common models, and potentially partial `PersonaConfiguration` models or interfaces relevant to dynamic settings.
    *   `Nucleus.Infrastructure.CosmosDb` (or a similar custom data access library): Provides reusable components for interacting with Azure Cosmos DB (e.g., client setup, repository base classes).
    *   `Nucleus.Mcp.Client` (Not for receiving, but if it needed to call out for some reason, though unlikely for this tool).
    *   `Microsoft.Extensions.Http.Resilience`: If any HTTP calls were made (unlikely for its core path).

## 5. Security Model

*   **Authentication of Callers (M365 Agents & Admin Tools):**
    *   All incoming MCP requests **must** be authenticated by validating Azure AD tokens.
    *   The server validates the token's signature, audience (this MCP tool's App ID URI), issuer, and lifetime.
*   **Authorization:**
    *   **`GetDynamicConfiguration`:**
        *   Requires a token from an M365 Persona Agent or an authorized service principal.
        *   The `TenantId` and `PersonaId` from the request (cross-validated with token claims if possible, e.g., if `TenantId` is a claim) are used to scope data access strictly.
        *   App roles or scopes like `PersonaConfig.Read` can be defined in this MCP tool's App Registration and checked.
    *   **`UpdateDynamicConfiguration`:**
        *   Requires a highly privileged token possessing a specific administrative app role (e.g., `PersonaConfig.Admin` or `PersonaConfig.Write`).
        *   Standard M365 Persona Agent tokens **must be rejected** for this operation to prevent unauthorized modifications.
*   **Authentication to Dependencies:**
    *   This MCP server uses its Azure AD Managed Identity (when deployed in Azure services like Azure Container Apps) to securely access Azure Key Vault (for secrets like the Cosmos DB connection string) and Azure App Configuration.
    *   The Managed Identity is also used to authenticate to Azure Cosmos DB if using AAD-based authentication with Cosmos DB. Otherwise, the connection string (retrieved securely from Key Vault) is used.
*   **Principle of Least Privilege:** Both callers and the MCP tool itself operate with the minimum necessary permissions.

## 6. Data Handling & Persistence

*   **Primary Data Store:** Azure Cosmos DB, within a dedicated container (e.g., `PersonaDynamicConfigurations` or `RuntimePersonaConfigs`).
*   **Data Schema:**
    *   Documents are typically keyed by a composite of `TenantId` and `PersonaId` or have `id` as `<TenantId>_<PersonaId>`.
    *   `TenantId` should be the partition key for scalability and data isolation.
    *   The document content is a JSON object representing the dynamic fields of a `PersonaConfiguration`. These fields are a subset of the overall `PersonaConfiguration` schema, focusing on those intended for runtime modification.
    *   **Example Document Structure (in Cosmos DB):**
        ```json
        {
            "id": "<tenantId>_<personaId>", // Or just <personaId> if tenantId is the partition key
            "tenantId": "actual_tenant_guid",
            "personaId": "persona_identifier_guid_or_name",
            "_etag": "...", // Cosmos DB ETag for optimistic concurrency
            "lastUpdatedUtc": "2025-05-28T10:00:00Z",
            "updatedBy": "admin_user_or_service_id",
            "dynamicSettings": {
                "systemPrompt": "You are a helpful assistant focused on providing concise answers.",
                "responseStyleGuide": "Maintain a formal and respectful tone.",
                "llmTemperatureOverride": 0.75,
                "featureFlags": {
                    "useAdvancedSummarization": true,
                    "enableExperimentalTools": false
                },
                "customAgenticParameters": {
                    "maxReflectionLoops": 3,
                    "confidenceThresholdForAction": 0.85
                }
            }
        }
        ```
*   **Data Integrity:** Optimistic concurrency control using ETags should be implemented for `UpdateDynamicConfiguration` operations to prevent lost updates.
*   **No Local Persistence:** The MCP server itself is stateless and does not persist configuration data locally. All reads and writes go directly to Azure Cosmos DB.

## 7. Deployment & Configuration Considerations

*   **Deployment:** Deployed as a .NET Aspire application component, typically hosted as an Azure Container App or Azure App Service, configured for auto-scaling.
*   **Key Configurations (Managed via .NET Aspire, Azure App Configuration, and Azure Key Vault):**
    *   Azure Cosmos DB endpoint URI and database/container name.
    *   Azure AD settings for token validation (Authority, Audience/App ID URI of this MCP tool).
    *   Logging levels and Application Insights connection string.
    *   Retry policies for Cosmos DB operations.
*   **Stateless Design:** The service instances should be stateless to facilitate scaling and resilience.

## 8. Future Considerations / Potential Enhancements

*   **Configuration Versioning:** Implement a mechanism within Cosmos DB (e.g., storing previous versions as separate documents or in an array within the main document) to allow rollback or history tracking of configuration changes.
*   **Advanced Caching Strategies:** Introduce a distributed cache (e.g., Azure Cache for Redis) for frequently accessed configurations to reduce latency and load on Cosmos DB, with appropriate cache invalidation strategies upon updates.
*   **Detailed Audit Logging:** For all `UpdateDynamicConfiguration` calls, log comprehensive audit trails to Azure Monitor Logs (or a dedicated audit store), including `UpdatedBy`, timestamp, and changes made.
*   **Schema Validation Service Integration:** Before persisting updates from `UpdateDynamicConfiguration`, integrate with a schema validation mechanism (potentially another MCP tool or a library) to ensure the new configuration values conform to the expected schema for each key.
*   **Support for Configuration Inheritance/Layering:** Introduce a more sophisticated model where configurations can be inherited (e.g., global defaults -> tenant-level overrides -> persona-specific settings), with this MCP tool resolving the effective configuration.
*   **Exposing an MCP Operation to List Available Dynamic Keys:** An operation like `ListDynamicConfigurationKeys(TenantId, PersonaId)` could return metadata about what settings are dynamically configurable for a given persona.
*   **UI for Managing Configurations:** While not part of this MCP tool, a separate admin application would likely consume the `UpdateDynamicConfiguration` operation to provide a user interface for managing these settings.
*   **Granular Access Control for Keys:** Potentially extend the security model to allow different admin roles to modify different subsets of configuration keys.
