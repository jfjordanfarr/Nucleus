# **Best Practices for Managing Static and Dynamic Configurations for M365 Agents in.NET**

## **1\. Executive Summary**

The development of sophisticated Microsoft 365 (M365) Agents using the.NET SDK introduces complex configuration management requirements. These agents often rely on a combination of static, deployment-time settings and dynamic, behavioral configurations that can be updated by the agent itself, particularly in response to user feedback or evolving operational needs. This report outlines Microsoft's recommended best practices for managing these diverse configuration types, with a strong emphasis on security, adaptability, and operational control, especially within multi-tenant Independent Software Vendor (ISV) environments.

The recommended approach involves a layered configuration strategy. Static configurations, such as core operational settings and service endpoints, are best managed using Azure App Configuration, with sensitive values like API keys and connection strings securely stored in Azure Key Vault and accessed via Key Vault references. For dynamic behavioral configurations, including adaptable Large Language Model (LLM) prompts and operational parameters, Azure Cosmos DB is recommended as the backend store due to its schema flexibility, global distribution, and low-latency access.

Agents should leverage Managed Identities for secure, credential-less authentication to these Azure services. In multi-tenant ISV scenarios, robust tenant isolation is paramount. This is achieved through a combination of Microsoft Entra ID application registrations with clearly defined app roles, Azure RBAC for resource access, and meticulous application-level enforcement of tenant context when interacting with shared resources like Cosmos DB (typically using tenantId as a partition key).

Securely updating dynamic configurations involves careful consideration of the agent's write permissions, potentially abstracting database interactions through a custom Model Context Protocol (MCP) tool. Such tools, if used, must also adhere to strict security and tenant isolation principles, with the M365 Agent SDK facilitating secure connections via OAuth 2.1.

Operationalizing these dynamic configurations necessitates robust versioning and rollback mechanisms within Cosmos DB, approval workflows (potentially using Adaptive Cards in Microsoft Teams) for significant changes, and comprehensive auditing through services like Azure Cosmos DB Change Feed and Azure Monitor. This holistic approach ensures that M365 Agents are not only powerful and adaptable but also secure, manageable, and compliant within complex enterprise and ISV landscapes.

## **2\. Foundations of M365 Agent Configuration**

Effective configuration management is a cornerstone of building robust and maintainable M365 Agents. A fundamental aspect of this is distinguishing between static configurations, defined at deployment, and dynamic behavioral configurations, which may change during the agent's lifecycle. This distinction directly influences the choice of storage solutions, update mechanisms, and, critically, the security controls applied.

### **2.1. Distinguishing Static (Deployment-Time) vs. Dynamic (Behavioral) Configurations**

**Static Configurations:** These encompass core operational settings, service endpoints, and fixed parameters that are typically established when the agent is deployed. At runtime, the agent generally treats these as read-only. Examples include:

* Default retry policies for external service calls.  
* Fixed thresholds for certain internal logic.  
* Feature flags that are not intended for frequent runtime changes.  
* Endpoints for dependent services (though sensitive parts like API keys should be in Key Vault).

The primary concern for static configurations is secure and centralized management, with updates being relatively infrequent and often tied to new deployments or planned maintenance.

**Dynamic Behavioral Configurations:** This category includes elements like LLM prompts, operational parameters (e.g., sensitivity thresholds for content analysis, model selection criteria for specific tasks), or business rules that are designed to adapt. This adaptation might be driven by direct user feedback, A/B testing results, machine learning model drift, or other runtime factors. These configurations require secure read and, crucially, write capabilities, either by the agent itself (in an autonomous or semi-autonomous fashion) or by authorized administrative processes.

The differentiation is vital. Static configurations prioritize secure, auditable, and version-controlled management through deployment pipelines, with runtime access being read-only. Dynamic configurations, on the other hand, demand agility, robust versioning for rollback, fine-grained write access control, and detailed auditing of changes, as they directly influence the agent's observable behavior and effectiveness.

### **2.2. Core Azure Services for Static Configurations**

For M365 Agents built on.NET and hosted in Azure, a combination of Azure App Configuration and Azure Key Vault provides a powerful and secure foundation for managing static configurations.

* Azure App Configuration:  
  This service offers centralized management of application settings and feature flags.1 For M365 Agents, it can store non-sensitive operational parameters, references to secrets, and feature toggles.  
  * **Best Practices:**  
    * **Labels:** Utilize labels to differentiate configurations across environments (e.g., dev, staging, prod) or for different versions of the agent.  
    * **Content Types:** Employ content types for structured configuration data (e.g., JSON), allowing for more complex settings to be managed coherently.  
    * **Snapshots:** Consider using App Configuration snapshots for versioning foundational settings, providing a way to roll back to known good configurations.4  
  * **Dynamic Refresh:** Azure App Configuration supports dynamic refresh capabilities, enabling.NET applications (including agents hosted in Azure App Service or Azure Functions) to reload configuration values without requiring an application restart.2 This is particularly useful for settings that might change infrequently but need to be applied without service interruption. This is typically achieved by configuring the App Configuration provider in the.NET application to monitor for changes, either by polling at a set interval or by watching a specific "sentinel" key.  
* Azure Key Vault:  
  Azure Key Vault is the designated service for securely storing and managing secrets, connection strings, certificates, and API keys.5 For M365 Agents, any sensitive static configuration value, such as database connection strings or API keys for third-party services, must be stored in Key Vault.  
  * **Integration with App Configuration via Key Vault References:** A key best practice is to use Key Vault references within Azure App Configuration.5 Instead of storing a secret directly in App Configuration, a setting in App Configuration will store a URI pointing to the secret in Key Vault. When the M365 Agent (or its hosting environment) retrieves this setting from App Configuration, the App Configuration service (or the agent's identity, depending on configuration) resolves the reference by fetching the secret directly from Key Vault. This ensures that the secret itself is never exposed in App Configuration logs or to personnel who only have access to App Configuration. The agent's Managed Identity requires permissions to read from App Configuration and, if it's resolving the reference itself, to get secrets from Key Vault. Alternatively, App Configuration's own Managed Identity can be granted access to Key Vault. This layered approach enhances security by minimizing the direct exposure of secrets.  
* Integration Patterns for.NET M365 Agents:  
  M365 Agents, being.NET applications, can leverage standard.NET configuration patterns.  
  * **Options Pattern:** Utilize IOptions\<T\>, IOptionsSnapshot\<T\>, and IOptionsMonitor\<T\> to bind configuration sections to strongly-typed C\# objects.6 This promotes type safety and makes configuration consumption cleaner within the agent's services.  
    * IOptions\<T\>: Provides singleton configuration, loaded once at startup.  
    * IOptionsSnapshot\<T\>: Provides scoped configuration, recomputed per request (useful in web contexts, less so for background agents unless specific scoping is used).  
    * IOptionsMonitor\<T\>: Provides singleton configuration that can be dynamically reloaded when changes are detected in the underlying configuration source (like Azure App Configuration). This is often the most suitable for agents needing to pick up static configuration changes without restarting.  
  * **Dependency Injection:** Configuration objects, populated via the options pattern, should be injected into agent services and components that require them.  
  * The Microsoft 365 Agents SDK 11 is generally unopinionated about specific configuration providers, allowing developers to integrate with standard.NET mechanisms. The hosting environment (e.g., Azure Functions, Azure App Service, or a custom service) will determine how these configuration providers are bootstrapped into the.NET IConfiguration pipeline.

The combination of Azure App Configuration for general settings and feature flags, and Azure Key Vault for all sensitive data, accessed via Key Vault references, forms a secure and manageable pattern for the static configuration needs of M365 Agents. Dynamic refresh capabilities ensure that even these foundational settings can be updated with minimal disruption.

**Table 1: Configuration Store Comparison for M365 Agents**

| Store | Configuration Type | Primary Use Cases for M365 Agents | Update Mechanism | Key Security Considerations | Versioning/Rollback Support |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Azure App Configuration | Static Application Settings, Feature Flags, References to Secrets | Core agent settings, non-sensitive operational parameters, feature toggles, environment-specifics | Deployment pipeline, Azure portal/CLI, SDK, Dynamic Refresh | Managed Identity access, Key Vault references, RBAC for control plane, labels for segregation | Snapshots, Labeling |
| Azure Key Vault | Secrets, Connection Strings, API Keys, Certificates | API keys for dependent services, database connection strings, sensitive credentials | Deployment pipeline (for initial setup/rotation), Azure portal/CLI | Managed Identity access, Access Policies/RBAC, soft delete, purge protection, network restrictions | Automatic secret versioning |
| Azure Cosmos DB | Dynamic Behavioral Prompts, Dynamic Operational Parameters, User-specific settings | Adaptive LLM prompts, agent personality settings, user-feedback driven parameters, A/B test configs | Agent self-update, Admin tool, Backend process (e.g., via MCP Tool) | Managed Identity access, Data Plane RBAC, Partition key for tenant isolation, Encryption at rest/transit, Network security (Private Endpoints) | Custom implementation (e.g., document versioning, audit trail) |
| appsettings.json | Local Development Static Settings | Default values for local development, initial bootstrap settings (overridden in cloud) | Code deployment | Avoid storing production secrets; source control security | Source control (Git) |
| Environment Variables | Hosting Environment Specific Settings | Overrides for appsettings.json in Azure, connection strings (though Key Vault preferred) | Hosting platform configuration | Securely managed by Azure hosting environment; avoid exposing sensitive data if logs are not secured | Deployment/Platform specific |

## **3\. Architecting Dynamic Behavioral Configuration Management**

Dynamic behavioral configurations, such as LLM prompts that adapt to user feedback or operational parameters that tune agent performance, require a more agile and robust storage and management solution than static settings. Azure Cosmos DB emerges as a strong candidate for this purpose, offering the necessary flexibility, scalability, and performance characteristics.

### **3.1. Leveraging Azure Cosmos DB for Storing Agent-Updatable Configurations**

* Suitability of Cosmos DB:  
  Azure Cosmos DB is a globally distributed, multi-model database service that provides low-latency access, automatic scalability, and schema flexibility. These features make it well-suited for storing dynamic agent configurations that may vary in structure and need to be accessed and updated frequently from geographically diverse locations.10 For M365 Agents, this means prompts, parameters, and even small knowledge snippets specific to a tenant or user can be efficiently managed.  
* Schema Design for Configuration Items:  
  When storing configurations in Cosmos DB, a well-defined schema is important, even with its schema-agnostic nature. Consider the following fields for each configuration document:  
  * id: A unique identifier for the configuration item.  
  * tenantId: Crucial for multi-tenant scenarios, used as the partition key for data isolation and efficient querying.  
  * agentId (optional): If an ISV offers multiple distinct agent types or if a tenant can have multiple instances of the same agent type with different configurations.  
  * configType: A string indicating the type of configuration (e.g., "LLMPrompt", "OperationalParameter", "BusinessRule").  
  * configName: A human-readable name or key for the specific configuration (e.g., "CustomerServiceGreetingPrompt", "SentimentAnalysisThreshold").  
  * version: An integer or timestamp to track versions of the configuration content.28  
  * isActive (optional): A boolean flag to indicate if this version is the currently active one.  
  * value: The actual configuration content. This can be a simple string, a number, or a complex JSON object (e.g., for structured LLM prompts with placeholders, or a set of related parameters).  
  * metadata (optional): A JSON object for additional information like description, author, or usage notes.  
  * lastUpdatedBy: Identifier of the entity (agent's service principal, user ID via an admin tool) that last modified the item.  
  * timestamp (or \_ts): Automatically managed by Cosmos DB, indicating the last modification time.

In addition to versioning the *content* of a configuration item (e.g., different versions of a prompt), it may also be necessary to version the *schema* of the configuration document itself if its structure evolves significantly over time.29 This can be handled by a separate schemaVersion field within the document.

* Tenant Isolation Models in Cosmos DB:  
  For ISV scenarios, isolating tenant configurations is paramount. Azure Cosmos DB offers several models 22:  
  * **Partition Key per Tenant:** This is often the most cost-effective approach for high tenant density. Each document includes a tenantId property, which is used as the logical partition key within a shared container (or set of containers). All queries and write operations by the agent or management tools *must* include the tenantId to ensure they operate only on the correct tenant's data. This model places the onus of tenant data separation on the application logic.  
  * **Database Account per Tenant:** This model provides the strongest physical isolation, as each tenant has its own dedicated Cosmos DB account. It allows for per-tenant customization of features like customer-managed keys (CMK), throughput, and geo-replication. However, it incurs higher costs and greater management overhead (provisioning, connection string management).  
  * **Recommendation for ISVs:** A common strategy is to start with a partition-key-per-tenant model for general customers to optimize costs. For premium tenants or those with stringent compliance or isolation requirements, offering a dedicated database account model can be an option. The application architecture should be designed to potentially support both models or facilitate migration.

The selection of an isolation model profoundly impacts security design. If a partition-key-per-tenant model is used, the agent (or any MCP tool it uses) must rigorously enforce the tenant context in every database operation. A failure to correctly apply the tenantId as a partition key could lead to cross-tenant data access or modification, a severe security breach.

### **3.2. Secure Read/Write Patterns for Agents**

M365 Agents need a secure and reliable way to read their dynamic configurations and, in some cases, update them.

* Agent Identity and Authentication:  
  The M365 Agent, when hosted on Azure services like Azure App Service or Azure Functions, should use a Managed Identity (either system-assigned or user-assigned) to authenticate to other Azure resources, including Azure Cosmos DB, Azure App Configuration, and Azure Key Vault.30 Managed Identities eliminate the need to store credentials (like connection strings or keys) in the agent's configuration or code, enhancing security. The agent's Managed Identity is granted appropriate Azure RBAC roles on the target resources.  
* Interacting with Cosmos DB: Direct SDK Usage vs. MCP Tool Abstraction:  
  There are two primary patterns for an agent to interact with Cosmos DB for its configurations:  
  1. Direct SDK Usage:  
     The agent's.NET code directly utilizes the Azure Cosmos DB SDK for.NET.  
     * **Pros:** Full control over the interaction, potentially lower latency as there's no intermediary.  
     * **Cons:** Requires careful implementation of tenant context handling (ensuring tenantId is always used as the partition key in queries and writes), robust error handling, and retry logic directly within the agent code. The agent's Managed Identity would need direct data plane RBAC permissions on the Cosmos DB container.  
  2. MCP Tool Abstraction:  
     A Model Context Protocol (MCP) server can act as an intermediary between the M365 Agent and Azure Cosmos DB.3  
     * **M365 Agents SDK and MCP Clients:** The M365 Agents SDK provides capabilities, such as the MCPClientManager, for an agent to act as an MCP client, enabling it to connect to and invoke tools exposed by an MCP server.13  
     * **Custom MCP Server:** The ISV would develop a custom MCP server (e.g., as an Azure Function or App Service) that encapsulates the logic for reading and writing specific configurations (like LLM prompts or operational parameters) from/to Cosmos DB. This server would expose these operations as "tools" via the MCP.  
     * **Secure Connection to Custom MCP Tool:** The connection between the M365 Agent (MCP client) and the custom MCP server (tool provider) must be secured. The MCPClientManager within the Agents SDK supports OAuth 2.1 for this purpose.47 The custom MCP server would be registered as an OAuth 2.0 resource server in Microsoft Entra ID, and the M365 Agent's identity would obtain tokens to call it.  
     * **Tenant Context Propagation to MCP Tool:** This is a critical security consideration. The M365 Agent, operating within a specific user's M365 context, will be aware of the tenantId. This tenantId must be securely transmitted to the MCP tool with every request.  
       * **Mechanism:** This could be achieved by including the tenantId as a custom claim in the OAuth token that the M365 Agent acquires for calling the MCP tool. Alternatively, it could be passed as a validated parameter in the tool invocation. The MCP tool must then use this tenantId to scope its operations within Cosmos DB (e.g., as the partition key).  
       * The MCP tool itself would authenticate to Cosmos DB using its own Managed Identity, which would be granted the necessary data plane permissions.  
     * **Pros:** Abstracts database-specific logic from the agent, promotes reusability of the MCP tool if other services also need to manage these configurations, and can centralize complex query/update logic.  
     * **Cons:** Introduces an additional network hop and another service (the MCP server) to secure and manage. The secure propagation and enforcement of tenant context by the MCP tool are paramount.

The decision between direct SDK access and an MCP tool abstraction depends on factors such as the complexity of configuration management logic, the need for reusability of this logic across different agents or services, and the desired architectural layering. If an MCP tool is used, its design must prioritize security and correct tenant context handling to prevent it from becoming a point of vulnerability for cross-tenant data access.

## **4\. Security and Access Control for Agent Self-Configuration (Multi-Tenant ISV Focus)**

In a multi-tenant ISV scenario, where an M365 Agent serves multiple customer tenants, security and access control for configuration updates are paramount. The agent must only be able to read and write configurations pertinent to the specific tenant context in which it's operating.

### **4.1. Identity Strategy for the ISV Application**

* **Single Multi-Tenant App Registration:** The ISV should typically use a single multi-tenant Microsoft Entra ID application registration for their M365 Agent offering.49 This application registration represents the agent service as a whole. When a customer organization onboards the ISV's agent, an administrator in the customer tenant consents to the permissions requested by this multi-tenant application, creating a service principal for the ISV's application in the customer's tenant.  
* **Agent Backend Identity (Managed Identity):** The actual backend service hosting the M365 Agent's logic (e.g., an Azure App Service, Azure Function, or Virtual Machine) should use a Managed Identity (system-assigned or user-assigned) for accessing Azure resources like Azure App Configuration, Key Vault, and Cosmos DB.30 This Managed Identity is a distinct service principal in the ISV's Microsoft Entra ID tenant. Permissions to Azure resources are granted to this Managed Identity.

### **4.2. Authorization Model for Agent Updating Its Own Configuration**

A layered authorization model is essential:

1. **Microsoft Entra ID App Roles (Application-Level Permissions):**  
   * Define custom app roles on the ISV's multi-tenant application registration.51 These roles represent specific capabilities the agent might perform related to its configuration. For example:  
     * Agent.Config.Read.Self: Allows the agent to read its own behavioral configurations for the current tenant.  
     * Agent.Config.Write.Self: Allows the agent to update its own behavioral configurations for the current tenant (e.g., adapting an LLM prompt based on user feedback).  
     * More granular roles could be defined if different agent functionalities or modules require different configuration update permissions.  
   * During the admin consent process in a customer tenant, the customer's administrator grants these defined app roles to the service principal of the ISV's application within their tenant. This grant signifies that the customer authorizes the ISV's agent to perform these configuration-related actions within the scope of their tenant.  
2. **Azure RBAC (Resource-Level Permissions for Managed Identity):**  
   * The Managed Identity of the agent's backend service (hosted in the ISV's Azure subscription) needs permissions to access the Azure resources where configurations are stored. For instance:  
     * **Azure App Configuration:** "App Configuration Data Reader" role to read static configurations.  
     * **Azure Key Vault:** "Key Vault Secrets User" role to retrieve secrets referenced by App Configuration.  
     * **Azure Cosmos DB:** A role like "Cosmos DB Built-in Data Contributor" (or a more restrictive custom Azure RBAC role) on the Cosmos DB account or specific database/container. This grants the Managed Identity permission to interact with the data plane of Cosmos DB at the Azure resource level.  
3. Azure Cosmos DB Data Plane RBAC (Fine-Grained, Tenant-Scoped Data Access):  
   While Azure RBAC grants the agent's Managed Identity broad permissions to the Cosmos DB data plane (e.g., read/write any item in a container), true tenant isolation for configuration data stored within a shared container requires finer-grained control. This is where Cosmos DB's native data plane RBAC comes into play.33  
   * **Custom Cosmos DB Roles:** Create custom roles within Cosmos DB (e.g., TenantConfigReader, TenantConfigWriter). These roles define specific data actions allowed, such as Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read or Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/upsert.  
   * **Assigning Cosmos DB Roles to Managed Identity:** The agent's Managed Identity (via its Microsoft Entra ID Object ID) is assigned these custom Cosmos DB roles.  
   * **Enforcing Tenant Scope (The Critical Challenge):** A significant challenge is that Cosmos DB data plane RBAC, by itself, does not typically allow for dynamic conditions in role definitions that restrict operations based on the item's partition key value matching a claim in the incoming token (e.g., a tenantId claim).  
     * **Application-Level Enforcement:** Therefore, the primary mechanism for ensuring the agent only operates on its current tenant's configuration data is **strict application-level logic**. When the agent (or an MCP tool it calls) interacts with Cosmos DB:  
       1. It must securely obtain the tenantId from the authenticated M365 user/agent context.  
       2. This tenantId *must* be used as the partition key value in all Cosmos DB queries and write operations (create, replace, upsert, delete).  
     * The Cosmos DB data plane role grants the Managed Identity the *permission* to perform actions (e.g., write items). The application logic, by correctly specifying the partition key, enforces the *scope* of those actions to a specific tenant's data. If the application logic were flawed or compromised and supplied an incorrect tenantId as the partition key, the Managed Identity, having general write permissions to the container via its Cosmos DB role, could inadvertently access or modify another tenant's configuration.

This multi-layered approach—Entra ID app roles for application consent, Azure RBAC for resource access by the agent's identity, and Cosmos DB data plane roles combined with diligent application-level tenant context enforcement—is crucial for secure agent self-configuration in multi-tenant environments.

### **4.3. Ensuring Tenant Data Isolation During Configuration Updates**

The core principle for tenant data isolation during configuration updates within Cosmos DB is the consistent and correct use of the tenantId as the partition key.

* All database queries for fetching configurations must filter by the current tenantId.  
* All write operations (creating new configurations, updating existing ones) must explicitly set the tenantId partition key.  
* If an MCP tool is used as an intermediary to Cosmos DB, the M365 Agent must securely pass the tenantId to the tool, and the tool must be designed to honor this tenantId in its database operations. The MCP tool should validate any incoming tenantId against the authenticated context if possible, or at least ensure its operations are strictly scoped by the provided tenantId.

### **4.4. Mitigating Risks of Dynamic Prompt Modification**

Allowing agents to dynamically update their own LLM prompts, often based on user feedback or automated learning, introduces specific security risks, primarily related to prompt injection and its consequences.56

* Prompt Injection Vulnerability:  
  If an M365 Agent updates its prompts stored in Cosmos DB based on external input (e.g., user feedback, data from other systems), an attacker could attempt to craft this input in such a way that malicious instructions are embedded into the stored prompt. When the agent later uses this compromised prompt, it could lead to unintended actions, data exfiltration, or generation of harmful content. This is a form of indirect prompt injection.60  
  * **Mitigation Strategies:**  
    * **Input Validation and Sanitization:** Rigorously validate and sanitize any external data (especially user-provided feedback) that is used to modify prompts. Disallow or neutralize characters and sequences that could be interpreted as instructions by the LLM.  
    * **Strict Schema and Modification Limits:** Define a strict schema for how prompts can be structured and what parts are modifiable. Limit the scope of changes an automated process or user feedback can induce.  
    * **Human-in-the-Loop Approval:** For significant or sensitive prompt changes, especially those derived from potentially untrusted sources, implement an approval workflow (see Section 5.2).  
    * **Segregation of Duties:** The mechanism that updates prompts should operate with the least privilege necessary.  
    * **Contextual Awareness:** Design prompts to be robust against attempts to "forget" previous instructions if parts of the prompt are dynamically assembled.  
    * **Output Filtering:** Apply content safety filters to the LLM's output, even if the prompt is believed to be safe.  
* Excessive Agency and Unintended Behavior:  
  If an agent can too freely modify its operational parameters or the core logic embedded in its prompts, it could lead to unstable or undesirable behavior.  
  * **Mitigation Strategies:**  
    * **Guardrails and Thresholds:** Implement strict limits on the range and type of parameters an agent can self-modify.  
    * **Monitoring and Alerting:** Continuously monitor agent behavior and performance metrics. Alert on significant deviations or when parameters approach critical thresholds.  
    * **Regular Audits:** Periodically review agent-modified configurations.  
* Sensitive Information Disclosure (OWASP LLM06):  
  If prompts are constructed or modified in a way that inadvertently includes sensitive system details, API keys, or other confidential data, and these prompts are then processed by the LLM, there's a risk of this information leaking into the LLM's responses or logs.  
  * **Mitigation Strategies:**  
    * **Never Embed Secrets in Prompts:** Prompts should guide behavior, not store credentials or sensitive static data. Use Azure Key Vault for secrets.  
    * **Data Minimization:** Only provide the LLM with the necessary contextual information to perform its task.  
    * **System Prompt Security:** Protect the integrity of system prompts. While dynamic user-facing prompts are stored in Cosmos DB, core system prompts guiding the agent's fundamental behavior should be managed more like static configuration, with stricter change controls. System prompt leakage itself is a risk.56

Storing LLM prompts in a database and allowing agent-driven updates creates a powerful feedback loop for adaptation but also opens a new attack surface. The update mechanism itself must be secure, and the content of the prompts must be managed to prevent the introduction of vulnerabilities or the leakage of sensitive information. Adherence to OWASP LLM Top 10 guidelines, particularly LLM01 (Prompt Injection) 60 and LLM06 (Sensitive Information Disclosure), is crucial.

## **5\. Operationalizing Dynamic Configuration Updates**

Managing dynamic behavioral configurations effectively requires robust operational practices, including versioning for safe rollbacks, approval workflows for governance, and comprehensive auditing for traceability and compliance.

### **5.1. Versioning and Rollback Strategies for Behavioral Configurations in Cosmos DB**

When agent behavioral configurations like LLM prompts or operational parameters are stored in Cosmos DB and are subject to updates, implementing a versioning strategy is critical for stability and recoverability.28 Simply overwriting existing configurations is risky, as it can lead to unintended consequences without an easy way to revert.

* Schema for Versioning:  
  Each configuration document in Cosmos DB should include fields to support versioning:  
  * version: An integer that increments with each new version, or a timestamp representing the version creation time.  
  * isActive (or isCurrent): A boolean flag indicating if this specific document version is the one currently in use by the agent. Only one version of a given configuration (e.g., for a specific tenantId, agentId, configType, and configName) should be active at any time.  
  * configId (or similar): A stable identifier that groups all versions of the same conceptual configuration item. The id field in Cosmos DB would be unique per document (i.e., per version).  
* Update Process:  
  Instead of updating a configuration document in-place, the recommended pattern is:  
  1. Read the current active version of the configuration.  
  2. Create a new document representing the new version of the configuration. Increment the version number.  
  3. Initially, this new version might be marked as isActive: false or stored in a "staging" state if an approval workflow is involved.  
  4. Once the new version is approved or ready to be activated:  
     * Set isActive: false on the previously active version.  
     * Set isActive: true on the new version.  
     * This two-step update (deactivating old, activating new) should ideally be performed atomically if possible, or managed carefully to avoid a state where no version is active or multiple are.  
* Rollback Mechanism:  
  To roll back to a previous known-good configuration:  
  1. Identify the desired previous version document (e.g., by its version number and configId).  
  2. Set isActive: false on the current problematic active version.  
  3. Set isActive: true on the chosen previous version. The agent's logic for fetching configurations should always query for the document where isActive: true for the given configId (and other relevant scopes like tenantId).  
* Archival (Optional):  
  Over time, many versions of configurations may accumulate. A strategy for archiving or soft-deleting very old, inactive versions can be implemented to manage storage costs and query performance. This could involve moving them to a separate "archive" container or marking them with an isArchived: true flag.  
* Azure Cosmos DB Transactions:  
  If an update involves changes to multiple related configuration items that must be applied atomically (all succeed or all fail together), Azure Cosmos DB's transactional batch operations can be used.64 These operations are scoped to a single logical partition key. This means if multiple configuration items for the same tenant (sharing the same tenantId as the partition key) need to be updated consistently, they can be part of a single transactional batch.

Implementing a robust versioning strategy directly within the Cosmos DB data model and application logic is essential for managing the lifecycle of dynamic agent behaviors safely.

### **5.2. Implementing Approval Workflows for Configuration Changes**

For significant changes to an agent's behavioral configuration—such as major revisions to core LLM prompts, adjustments to critical operational thresholds, or enabling new sensitive capabilities—an approval workflow can provide an essential layer of governance and safety. This is particularly relevant in ISV scenarios where customer administrators may require oversight of changes affecting their tenant, or within enterprises where compliance and risk management demand such controls.

* Scenario for Approval:  
  Approvals are typically needed when:  
  * The configuration change has a high potential impact on agent behavior or performance.  
  * The change is initiated by an automated process (e.g., agent self-learning) and requires human validation.  
  * The change affects sensitive operations or data access.  
  * In a multi-tenant ISV context, the customer admin wishes to review and approve changes specific to their tenant's agent instance.  
* Architectural Pattern for Approval Workflow:  
  A common pattern involves integrating Azure Logic Apps or Power Automate with Microsoft Teams using Adaptive Cards 66:  
  1. **Change Proposal:** The agent (or an administrative tool) proposes a configuration change. This change is saved in Cosmos DB as a new version, marked with a status like "PendingApproval" or isActive: false.  
  2. **Trigger Workflow:** The creation of this "PendingApproval" configuration item in Cosmos DB can trigger an Azure Function (via Change Feed) or another eventing mechanism, which in turn starts a Power Automate flow or Logic App.  
  3. **Send Adaptive Card for Approval:** The workflow constructs an Adaptive Card detailing the proposed change (e.g., showing a diff of an LLM prompt, or the old vs. new parameter values). This card is sent to a designated approver or an approval group via Microsoft Teams (e.g., posted to a specific channel or sent as a direct chat to the customer admin or internal governance team). The card includes "Approve" and "Reject" actions.  
  4. **Process Approval Response:** The workflow waits for the approver's response from the Adaptive Card.  
  5. **Update Configuration Status:**  
     * **On Approval:** The workflow updates the corresponding configuration item in Cosmos DB. This typically involves setting the isActive flag of the new version to true and the isActive flag of the previously active version to false.  
     * **On Rejection:** The workflow might update the status to "Rejected" and notify the initiator. The rejected configuration remains inactive.  
  6. **Notification:** The initiator of the change (e.g., the user who triggered it via an admin tool, or a system notification) is informed of the approval outcome.  
* Role of M365 Admin Center for ISV Agents:  
  While the Microsoft 365 admin center allows tenant administrators to manage deployed ISV applications (including agents), such as enabling/disabling them or controlling user assignments 1, it does not typically provide in-built mechanisms for approving internal behavioral configurations of those agents. Therefore, ISVs needing to offer such approval capabilities to their customers would likely need to build a custom interface (e.g., a settings portal for their agent) where customer admins can manage these approvals. This interface would then interact with the backend approval workflow.  
* **Considerations:**  
  * **Approver Identification:** Clearly define who the approvers are for different types of configuration changes or for different tenants.  
  * **Granularity:** Determine the level of detail required in the approval request.  
  * **Escalation and Timeouts:** Implement logic for approval reminders, escalations if an approval is pending too long, or default actions (e.g., auto-reject after a certain period).

Approval workflows, especially when integrated seamlessly into tools like Microsoft Teams via Adaptive Cards, add a crucial control layer, enhancing safety, compliance, and trust in dynamically configurable M365 Agents.

### **5.3. Auditing and Monitoring**

Comprehensive auditing and monitoring are non-negotiable for dynamic agent configurations. It's essential to track what changed, when, by whom (or what process), and the impact of those changes.

* **Tracking Configuration Changes in Cosmos DB:**  
  * **Azure Cosmos DB Change Feed:** This is the primary mechanism for capturing all data modifications (creations, updates, deletes) within a Cosmos DB container.75 For configuration data, every change to a prompt or parameter document will generate an event in the Change Feed.  
    * **Processing Change Feed Events:** Azure Functions are commonly used to listen to the Change Feed. When a configuration document is changed, the Azure Function can:  
      1. Extract relevant details from the changed document (e.g., tenantId, agentId, configName, version, the new value, lastUpdatedBy, timestamp).  
      2. Enrich this information with context (e.g., if the change was part of an approval workflow).  
      3. Send structured audit logs to a centralized logging and monitoring system like Azure Monitor (specifically, Application Insights for application-level logs or Log Analytics for broader operational logs) or Microsoft Sentinel for security information and event management (SIEM).  
  * **Content of Audit Logs:** Audit logs should capture:  
    * The specific configuration item that changed (e.g., its unique ID or name).  
    * The tenantId and agentId to provide context.  
    * The new version number.  
    * The principal that made the change (e.g., the agent's service principal ID if it was an automated update, or a user ID if an admin tool was used).  
    * The timestamp of the change.  
    * Optionally, a snapshot of the old and new values, or a diff, for critical configurations (though this can increase log volume).  
  * **Cosmos DB Control Plane Logs:** While Change Feed tracks data-level changes, Azure Cosmos DB also provides control plane logs that audit operations on the Cosmos DB account itself, such as changes to throughput, replication settings, or firewall rules.81 These are also important for overall security posture but are distinct from auditing the configuration *content*. Disabling key-based metadata write access is a recommended security measure to ensure control plane changes go through Azure Resource Manager and are thus auditable via Azure Activity Log.81  
* **Logging Agent Actions and Configuration Usage:**  
  * **Agent-Side Logging:** The M365 Agent itself should log significant actions, such as:  
    * When it reads a specific version of a configuration.  
    * When it attempts to update a configuration (and the outcome).  
    * Any errors encountered during configuration access or processing. These logs, typically sent to Application Insights, provide an operational view from the agent's perspective.  
  * **Microsoft Purview for Copilot Studio (Analogous Insights):** For agents built or managed via Copilot Studio, Microsoft Purview provides auditing capabilities for agent content, settings changes, and user interactions.82 While the user query focuses on.NET SDK-based M365 Agents (which are often custom engine agents), the principles of comprehensive auditing provided by Purview highlight the types of activities that are important to capture. If custom.NET agents are integrated into the broader M365 Copilot ecosystem or managed through similar administrative interfaces, their activities might also become visible in such centralized audit logs.  
* **Monitoring and Alerting:**  
  * Set up alerts in Azure Monitor based on audit logs. For example, alert on:  
    * Frequent or unexpected configuration changes.  
    * Changes made outside of expected processes (e.g., direct database updates not going through an approval workflow).  
    * Failures in applying configuration changes.  
    * Attempts to access or modify configurations across tenant boundaries (if detectable through audit patterns).

A robust auditing strategy combining Cosmos DB Change Feed for data-level changes, Azure control plane logs for resource-level changes, and application-level logging from the agent provides a comprehensive audit trail. This is essential for troubleshooting, security investigations, and demonstrating compliance in multi-tenant environments.

## **6\. Holistic Configuration Strategy**

A successful M365 Agent relies on a well-thought-out, holistic configuration strategy that layers different configuration sources appropriately and ensures that settings can be refreshed dynamically as needed.

### **6.1. Layering Configuration Sources**

M365 Agents typically draw configuration from multiple sources, forming a hierarchy where more specific or dynamic settings can override more general or static ones:

1. **Local Development Defaults (appsettings.json):**  
   * During local development, appsettings.json (and environment-specific variants like appsettings.Development.json) provide baseline settings and connection strings for local emulators or development services. These should **not** contain production secrets.  
2. **Environment Variables (Hosting Environment):**  
   * When deployed to Azure (e.g., App Service, Azure Functions), environment variables are a common way to provide settings specific to that hosting environment. These can override values from appsettings.json. Azure Key Vault references can also be surfaced as environment variables in some hosting models.  
3. **Azure App Configuration (Centralized Static & Secrets Management):**  
   * This is the primary store for centralized static application settings and feature flags for all environments (dev, staging, prod), differentiated by labels.1  
   * **Key Vault References:** Crucially, sensitive values like API keys and connection strings should be stored in Azure Key Vault, with App Configuration holding only references to these secrets.5 This ensures secrets are managed securely and accessed by the agent's Managed Identity. This layer overrides environment variables and appsettings.json for the settings it manages.  
4. **Azure Cosmos DB (Dynamic Behavioral Configurations):**  
   * This is the store for agent-updatable configurations like LLM prompts, adaptive operational parameters, and tenant-specific or user-specific behavioral settings.15 These are the most dynamic and are read (and potentially written) by the agent at runtime based on its operational logic and tenant context.

This layered approach provides a clear separation of concerns: build-time defaults, environment-specific overrides, centrally managed static application configurations and secrets, and finally, highly dynamic, agent-specific behavioral data.

### **6.2. Ensuring Dynamic Refresh of Settings**

For an M365 Agent to be responsive to changes without requiring restarts or redeployments, dynamic refresh of configurations is essential:

* **Refreshing Settings from Azure App Configuration:**  
  * .NET applications, including M365 Agents, should use the Azure App Configuration provider library. This provider can be configured for dynamic refresh.2  
  * **IOptionsMonitor\<T\>:** When using the options pattern in.NET, injecting IOptionsMonitor\<MyConfigOptions\> allows services to access the latest configuration values. The App Configuration provider, when refresh is enabled, will periodically check for updates or watch a sentinel key. When an update is detected, IOptionsMonitor\<T\> will provide the new values on its next OnChange event or when CurrentValue is accessed.  
  * **Refresh Interval vs. Sentinel Key:**  
    * **Refresh Interval:** The provider polls App Configuration at a defined interval (e.g., every 30 seconds). This is simple but may lead to unnecessary polling if configurations change infrequently.  
    * **Sentinel Key:** The provider watches a specific "sentinel" key (or keys) in App Configuration. Only when this sentinel key's value changes does the provider refresh all other monitored configurations. This is more efficient if changes are infrequent, as it reduces polling load. The update to the sentinel key acts as a signal to reload.  
* **Refreshing Behavioral Configurations from Azure Cosmos DB:**  
  * Configurations stored in Cosmos DB (e.g., LLM prompts) are typically read by the agent based on its operational needs. There isn't an automatic "push" refresh mechanism from Cosmos DB to the agent in the same way App Configuration provides.  
  * **Strategies for Refresh:**  
    * **Read-on-Demand:** The agent fetches the relevant configuration (e.g., a specific tenant's prompt for a specific task) from Cosmos DB at the beginning of a relevant operation (e.g., when a new conversation starts, or a specific skill is invoked).  
    * **Caching with Expiry:** To reduce latency and load on Cosmos DB, the agent can cache frequently accessed configurations in memory (e.g., using IMemoryCache in.NET). The cache entries would have an expiration time, after which the agent re-fetches from Cosmos DB.  
    * **Event-Driven Refresh (Advanced):** For more immediate updates, an event-driven approach could be used. For example, an update to a configuration in Cosmos DB (detected via Change Feed by an Azure Function) could send a signal (e.g., via Azure SignalR Service or a message queue) to active agent instances, prompting them to invalidate their cache and re-fetch the specific configuration. This is more complex to implement but offers near real-time updates.

A well-defined hierarchy of configuration sources, combined with appropriate dynamic refresh mechanisms for both static and behavioral settings, ensures that M365 Agents can operate with stability while remaining adaptable to changing requirements and runtime conditions.

**Table 2: Access Control Patterns for Agent Configuration Updates in Multi-Tenant Scenarios**

| Control Layer | Authentication/Authorization Mechanism | Key Implementation Detail | Tenant Isolation Benefit/Consideration |
| :---- | :---- | :---- | :---- |
| **M365 Agent Identity (Backend Service)** | Managed Identity (System or User-Assigned) for Azure resources | Agent's hosting service (e.g., App Service, Function) uses MI to get Microsoft Entra ID token for target Azure resource (e.g., Cosmos DB). | Ensures the agent backend authenticates as a trusted Azure service principal. Permissions are granted to this specific identity. |
| **ISV Application Registration (Logical Agent Service)** | Microsoft Entra ID App Roles defined on the ISV's multi-tenant app | Customer admin consents to app roles (e.g., Agent.Config.Write.Self) for the ISV app's service principal in their tenant. | Defines what the ISV's application is *allowed to do* within a specific customer tenant. Enforces application-level permissions. |
| **Custom MCP Tool (if used as intermediary)** | OAuth 2.0 Bearer Token (Agent is client, MCP Tool is resource server) | M365 Agent (via MCPClientManager) obtains token for MCP tool. Tool validates token and extracts claims (potentially tenantId). | Secures the communication channel between agent and tool. Tool must validate caller's identity and authorization. |
| **Azure Resource Manager (Control Plane)** | Azure RBAC roles (e.g., "Cosmos DB Built-in Data Contributor") | Managed Identity of agent backend (or MCP tool) is assigned Azure RBAC roles on the Cosmos DB account/App Configuration store. | Grants the identity permission to interact with the Azure service at a resource level (e.g., read/write data to a container, read from App Config). |
| **Azure Cosmos DB (Data Plane)** | Cosmos DB Data Plane RBAC with Custom Roles (e.g., TenantConfigWriter) \+ Application Logic | Managed Identity (or MCP tool's MI) assigned custom Cosmos DB roles. Application code *must* use tenantId from M365 context as partition key for all operations. | Cosmos DB role grants *ability* to perform data operations (e.g., upsertItem). Application logic using the partition key enforces that these operations are *scoped* to the correct tenant's data. **Critical for preventing cross-tenant data access/modification.** |

## **7\. Conclusion and Key Recommendations**

Managing configurations for M365 Agents built with the.NET SDK, particularly in dynamic and multi-tenant ISV contexts, requires a deliberate and layered architectural approach. The distinction between static, deployment-time configurations and dynamic, agent-updatable behavioral configurations is fundamental, guiding the choice of Azure services and security patterns.

**Key Recommendations:**

1. **Embrace a Layered Configuration Strategy:**  
   * Use **Azure App Configuration** for centralized static application settings and feature flags, leveraging labels for environmental separation and snapshots for versioning.  
   * Store all secrets, connection strings, and sensitive static data in **Azure Key Vault**, accessed via Key Vault references from App Configuration to maximize security.  
   * Employ **Azure Cosmos DB** for dynamic, agent-updatable behavioral configurations (e.g., LLM prompts, adaptive parameters) due to its flexibility, scalability, and performance. Use tenantId as the partition key for robust tenant isolation in shared Cosmos DB resources.  
2. **Prioritize Security with Managed Identities and Granular RBAC:**  
   * Utilize **Managed Identities** for the M365 Agent's backend services to authenticate to all Azure resources (App Configuration, Key Vault, Cosmos DB), eliminating credential management in code.  
   * Implement **Microsoft Entra ID App Roles** on the ISV's multi-tenant application registration to define agent capabilities (e.g., Agent.Config.Write.Self), to which customer administrators consent.  
   * Combine Azure RBAC for resource-level access with **Azure Cosmos DB Data Plane RBAC** (custom roles) for fine-grained permissions on configuration data. Critically, ensure application logic (within the agent or an intermediary MCP tool) strictly enforces tenant context by using the authenticated tenantId as the partition key for all Cosmos DB operations.  
3. **Securely Manage Dynamic Updates via Agents or MCP Tools:**  
   * If agents update their own configurations, the process must be highly secure.  
   * If using a **Model Context Protocol (MCP) tool** as an intermediary to Cosmos DB:  
     * Secure the agent-to-MCP-tool communication using OAuth 2.1, facilitated by the M365 Agents SDK's MCPClientManager.  
     * Ensure the tenantId is securely propagated from the M365 Agent to the MCP tool and rigorously used by the tool to scope all database operations.  
     * The MCP tool itself should use a Managed Identity to authenticate to Cosmos DB.  
4. **Implement Robust Operational Practices for Dynamic Configurations:**  
   * **Versioning:** Design Cosmos DB schemas to support versioning of configuration items (e.g., version field, isActive flag) to enable safe updates and rollbacks.  
   * **Approval Workflows:** For significant behavioral changes, implement approval workflows using technologies like Power Automate and Adaptive Cards in Microsoft Teams, providing governance and oversight, especially for ISV-customer interactions.  
   * **Auditing:** Leverage **Azure Cosmos DB Change Feed** processed by Azure Functions to create a comprehensive audit trail of all configuration modifications. Send these audit logs, enriched with tenant context, to Azure Monitor (Application Insights/Log Analytics) or Microsoft Sentinel.  
5. **Address Risks of Dynamic LLM Prompt Management:**  
   * Recognize that storing LLM prompts in a database and allowing agent updates creates an attack surface.  
   * Implement strong input validation and sanitization for any feedback or data influencing prompt updates to mitigate prompt injection risks (OWASP LLM01).  
   * Avoid embedding sensitive information in prompts and follow security best practices for LLM interactions.  
6. **Ensure Dynamic Refresh and Consistency:**  
   * Utilize.NET's IOptionsMonitor\<T\> with the Azure App Configuration provider for dynamic refresh of static settings without agent restarts.  
   * Develop a clear strategy for how and when agents refresh dynamic behavioral configurations from Cosmos DB (e.g., on-demand, cached with expiry, or event-driven).

By adopting these best practices, developers can build M365 Agents that are not only intelligent and adaptable but also secure, manageable, and well-suited for the complexities of multi-tenant ISV environments. The focus must always be on a defense-in-depth security posture, rigorous tenant isolation, and comprehensive operational controls.

#### **Works cited**

1. Set Up Your Development Environment to Extend Microsoft 365 Copilot, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/prerequisites](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/prerequisites)  
2. Tutorial for using Azure App Configuration dynamic configuration in ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-azure-functions-csharp](https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-azure-functions-csharp)  
3. App Configuration tools for the Azure MCP Server \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/app-configuration](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/app-configuration)  
4. Azure App Configuration best practices | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices](https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices)  
5. azure-dev/cli/azd/CHANGELOG.md at main \- GitHub, accessed May 24, 2025, [https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md](https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md)  
6. Options pattern in ASP.NET Core | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0)  
7. awesome-azd/website/static/templates.json at main \- GitHub, accessed May 24, 2025, [https://github.com/Azure/awesome-azd/blob/main/website/static/templates.json](https://github.com/Azure/awesome-azd/blob/main/website/static/templates.json)  
8. azure-mcp/TROUBLESHOOTING.md at main \- GitHub, accessed May 24, 2025, [https://github.com/Azure/azure-mcp/blob/main/TROUBLESHOOTING.md](https://github.com/Azure/azure-mcp/blob/main/TROUBLESHOOTING.md)  
9. farzad528/mcp-server-azure-ai-agents: Model Context Protocol Servers for Azure AI Search, accessed May 24, 2025, [https://github.com/farzad528/mcp-server-azure-ai-agents](https://github.com/farzad528/mcp-server-azure-ai-agents)  
10. Azure MCP Server tools \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/)  
11. Create and Deploy a Custom Engine Agent with Microsoft 365 Agents SDK, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk)  
12. Microsoft 365 Agents SDK, accessed May 24, 2025, [https://microsoft.github.io/Agents/](https://microsoft.github.io/Agents/)  
13. What is the Microsoft 365 Agents SDK, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview)  
14. How agents work in the Microsoft 365 Agents SDK (preview), accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk)  
15. A Model Context Protocol (MCP) server that provides secure access to Azure Cosmos DB datasets. Enables Large Language Models (LLMs) to safely query and analyze data through a standardized interface. \- GitHub, accessed May 24, 2025, [https://github.com/AzureCosmosDB/azure-cosmos-mcp-server](https://github.com/AzureCosmosDB/azure-cosmos-mcp-server)  
16. Integration with Azure AI Agent Service \- Azure Cosmos DB for NoSQL | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/gen-ai/azure-agent-service](https://learn.microsoft.com/en-us/azure/cosmos-db/gen-ai/azure-agent-service)  
17. How to Store Agent Threads with Azure Cosmos DB and Azure AI Agent Service \- YouTube, accessed May 24, 2025, [https://www.youtube.com/watch?v=in6hJiXO-pw](https://www.youtube.com/watch?v=in6hJiXO-pw)  
18. Azure Cosmos DB Conf 2025 Recap: AI, Apps & Scale, accessed May 24, 2025, [https://devblogs.microsoft.com/cosmosdb/azure-cosmos-db-conf-2025-recap-ai-apps-scale/](https://devblogs.microsoft.com/cosmosdb/azure-cosmos-db-conf-2025-recap-ai-apps-scale/)  
19. Cosmos DB tools for the Azure MCP Server \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/cosmos-db](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/cosmos-db)  
20. Setting Up Azure MCP Server with VS Code, accessed May 24, 2025, [https://www.cloudproinc.com.au/index.php/2025/04/18/setting-up-azure-mcp-server-with-vs-code/](https://www.cloudproinc.com.au/index.php/2025/04/18/setting-up-azure-mcp-server-with-vs-code/)  
21. TX-RAMP Certified Cloud Products \- Texas Department of Information Resources, accessed May 24, 2025, [https://dir.texas.gov/sites/default/files/2025-05/TX-RAMP%20Certified%20Cloud%20Products%205.13.25\_0.xlsx](https://dir.texas.gov/sites/default/files/2025-05/TX-RAMP%20Certified%20Cloud%20Products%205.13.25_0.xlsx)  
22. Multitenancy and Azure Cosmos DB \- Azure Architecture Center | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/cosmos-db](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/cosmos-db)  
23. Multitenant app with dedicated databases for each tenant on Azure \- DEV Community, accessed May 24, 2025, [https://dev.to/bobur/multitenant-app-with-dedicated-databases-for-each-tenant-on-azure-7o0](https://dev.to/bobur/multitenant-app-with-dedicated-databases-for-each-tenant-on-azure-7o0)  
24. A sample for multi-agent orchestration in Python using Azure Cosmos DB with LangGraph \- GitHub, accessed May 24, 2025, [https://github.com/AzureCosmosDB/multi-agent-langgraph](https://github.com/AzureCosmosDB/multi-agent-langgraph)  
25. Repositories \- Azure Cosmos DB \- GitHub, accessed May 24, 2025, [https://github.com/orgs/AzureCosmosDB/repositories](https://github.com/orgs/AzureCosmosDB/repositories)  
26. Architecture best practices for Azure Cosmos DB for NoSQL \- GitHub, accessed May 24, 2025, [https://github.com/MicrosoftDocs/well-architected/blob/main/well-architected/service-guides/cosmos-db.md](https://github.com/MicrosoftDocs/well-architected/blob/main/well-architected/service-guides/cosmos-db.md)  
27. Best Practices for Building Multi-Tenant Applications on Azure Cosmos DB \- YouTube, accessed May 24, 2025, [https://www.youtube.com/watch?v=6iZSMazdBpY](https://www.youtube.com/watch?v=6iZSMazdBpY)  
28. Azure Cosmos DB design pattern: Document Versioning \- Code Samples | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/document-versioning/](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/document-versioning/)  
29. Azure Cosmos DB design pattern: Schema Versioning \- Code Samples | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/schema-versioning/](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/schema-versioning/)  
30. Configure managed identities with Microsoft Entra ID for your Azure Cosmos DB account, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-managed-identity](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-managed-identity)  
31. azure-databases-docs/articles/cosmos-db/how-to-setup-cross-tenant-customer-managed-keys.md at main \- GitHub, accessed May 24, 2025, [https://github.com/MicrosoftDocs/azure-databases-docs/blob/main/articles/cosmos-db/how-to-setup-cross-tenant-customer-managed-keys.md](https://github.com/MicrosoftDocs/azure-databases-docs/blob/main/articles/cosmos-db/how-to-setup-cross-tenant-customer-managed-keys.md)  
32. Connect to Azure Cosmos DB using a managed identity (Azure AI Search) \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/search/search-howto-managed-identities-cosmos-db](https://learn.microsoft.com/en-us/azure/search/search-howto-managed-identities-cosmos-db)  
33. Use data plane role-based access control \- Azure Cosmos DB for ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac)  
34. Dynamic Tool Discovery: Azure AI Agent Service \+ MCP Server Integration, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/azure-ai-services-blog/dynamic-tool-discovery-azure-ai-agent-service--mcp-server-integration/4412651](https://techcommunity.microsoft.com/blog/azure-ai-services-blog/dynamic-tool-discovery-azure-ai-agent-service--mcp-server-integration/4412651)  
35. Azure-Samples/remote-mcp-functions-dotnet: This is a quickstart template to easily build and deploy a custom remote MCP server to the cloud using Azure functions. You can clone/restore/run on your local machine with debugging, and \`azd up\` to have it in the cloud in a \- GitHub, accessed May 24, 2025, [https://github.com/Azure-Samples/remote-mcp-functions-dotnet](https://github.com/Azure-Samples/remote-mcp-functions-dotnet)  
36. microsoft/mcp-dotnet-samples: A comprehensive set of ... \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/mcp-dotnet-samples](https://github.com/microsoft/mcp-dotnet-samples)  
37. microsoft/mcp \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/mcp](https://github.com/microsoft/mcp)  
38. The Azure MCP Server, bringing the power of Azure to your agents. \- GitHub, accessed May 24, 2025, [https://github.com/Azure/azure-mcp](https://github.com/Azure/azure-mcp)  
39. azure-mcp/docs/azmcp-commands.md at main \- GitHub, accessed May 24, 2025, [https://github.com/Azure/azure-mcp/blob/main/docs/azmcp-commands.md](https://github.com/Azure/azure-mcp/blob/main/docs/azmcp-commands.md)  
40. Extending Copilot coding agent with the Model Context Protocol (MCP) \- GitHub Docs, accessed May 24, 2025, [https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/extending-copilot-coding-agent-with-mcp](https://docs.github.com/en/enterprise-cloud@latest/copilot/customizing-copilot/extending-copilot-coding-agent-with-mcp)  
41. Error trying to implement cost management exports for billing account scope · Issue \#184 · Azure/terraform-provider-azapi \- GitHub, accessed May 24, 2025, [https://github.com/Azure/terraform-provider-azapi/issues/184](https://github.com/Azure/terraform-provider-azapi/issues/184)  
42. Extend your agent with Model Context Protocol (preview) \- Microsoft Copilot Studio, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp)  
43. Securing the Model Context Protocol: Building a safer agentic future on Windows, accessed May 24, 2025, [https://blogs.windows.com/windowsexperience/2025/05/19/securing-the-model-context-protocol-building-a-safer-agentic-future-on-windows/](https://blogs.windows.com/windowsexperience/2025/05/19/securing-the-model-context-protocol-building-a-safer-agentic-future-on-windows/)  
44. Build a Model Context Protocol (MCP) server in C\# \- .NET Blog, accessed May 24, 2025, [https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)  
45. MCP server: A step-by-step guide to building from scratch \- Composio, accessed May 24, 2025, [https://composio.dev/blog/mcp-server-step-by-step-guide-to-building-from-scrtch/](https://composio.dev/blog/mcp-server-step-by-step-guide-to-building-from-scrtch/)  
46. Build MCP Remote Servers with Azure Functions \- .NET Blog, accessed May 24, 2025, [https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/](https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/)  
47. Piecing together the Agent puzzle: MCP, authentication ..., accessed May 24, 2025, [https://blog.cloudflare.com/building-ai-agents-with-mcp-authn-authz-and-durable-objects/](https://blog.cloudflare.com/building-ai-agents-with-mcp-authn-authz-and-durable-objects/)  
48. Microsoft 365 Agents SDK documentation | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/)  
49. Authenticate applications and users with Microsoft Entra ID, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/architecture/authenticate-applications-and-users](https://learn.microsoft.com/en-us/entra/architecture/authenticate-applications-and-users)  
50. Microsoft Entra ID Guide for independent software developers, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/architecture/guide-for-independent-software-developers](https://learn.microsoft.com/en-us/entra/architecture/guide-for-independent-software-developers)  
51. Overview of permissions and consent in the Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview](https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview)  
52. Roles, permissions, and workspace access for users \- Partner Center | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/partner-center/account-settings/permissions-overview](https://learn.microsoft.com/en-us/partner-center/account-settings/permissions-overview)  
53. Use control plane role-based access control \- Azure Cosmos DB for NoSQL, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-grant-control-plane-access](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-grant-control-plane-access)  
54. Use data plane role-based access control \- Azure Cosmos DB for NoSQL | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-grant-data-plane-access](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-grant-data-plane-access)  
55. Azure custom roles \- Azure RBAC | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/role-based-access-control/custom-roles](https://learn.microsoft.com/en-us/azure/role-based-access-control/custom-roles)  
56. Large Language Model (LLM) Security Risks and Best Practices, accessed May 24, 2025, [https://www.legitsecurity.com/aspm-knowledge-base/llm-security-risks](https://www.legitsecurity.com/aspm-knowledge-base/llm-security-risks)  
57. Security planning for LLM-based applications | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/ai/playbook/technology-guidance/generative-ai/mlops-in-openai/security/security-plan-llm-application](https://learn.microsoft.com/en-us/ai/playbook/technology-guidance/generative-ai/mlops-in-openai/security/security-plan-llm-application)  
58. LLM System Prompt Leakage: Prevention Strategies \- Cobalt, accessed May 24, 2025, [https://www.cobalt.io/blog/llm-system-prompt-leakage-prevention-strategies](https://www.cobalt.io/blog/llm-system-prompt-leakage-prevention-strategies)  
59. OWASP Top 10 LLM Applications 2025 | Indusface Blog, accessed May 24, 2025, [https://www.indusface.com/blog/owasp-top-10-llm/](https://www.indusface.com/blog/owasp-top-10-llm/)  
60. LLM01: Prompt Injection \- OWASP Top 10 for LLM & Generative AI Security, accessed May 24, 2025, [https://genai.owasp.org/llmrisk2023-24/llm01-24-prompt-injection/](https://genai.owasp.org/llmrisk2023-24/llm01-24-prompt-injection/)  
61. OWASP Top 10 LLM, Updated 2025: Examples & Mitigation Strategies \- Oligo Security, accessed May 24, 2025, [https://www.oligo.security/academy/owasp-top-10-llm-updated-2025-examples-and-mitigation-strategies](https://www.oligo.security/academy/owasp-top-10-llm-updated-2025-examples-and-mitigation-strategies)  
62. Agent system design patterns \- Azure Databricks | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/databricks/generative-ai/guide/agent-system-design-patterns](https://learn.microsoft.com/en-us/azure/databricks/generative-ai/guide/agent-system-design-patterns)  
63. AI Agents Behavior Versioning and Evaluation in Practice \- DEV Community, accessed May 24, 2025, [https://dev.to/bobur/ai-agents-behavior-versioning-and-evaluation-in-practice-5b6g](https://dev.to/bobur/ai-agents-behavior-versioning-and-evaluation-in-practice-5b6g)  
64. Work with stored procedures, triggers, and UDFs in Azure Cosmos DB | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/stored-procedures-triggers-udfs](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/stored-procedures-triggers-udfs)  
65. How to control CosmosDB acid transactions (commit and rollback) using JAVA SDK?, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/1290319/how-to-control-cosmosdb-acid-transactions-(commit](https://learn.microsoft.com/en-us/answers/questions/1290319/how-to-control-cosmosdb-acid-transactions-\(commit)  
66. Build API plugins as declarative agent actions using TypeSpec for Microsoft 365 Copilot, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/build-api-plugins-typespec](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/build-api-plugins-typespec)  
67. provision-assist-m365/Approval-flow.md at main \- GitHub, accessed May 24, 2025, [https://github.com/pnp/provision-assist-m365/blob/main/Approval-flow.md](https://github.com/pnp/provision-assist-m365/blob/main/Approval-flow.md)  
68. Ask with Adaptive Cards \- Microsoft Copilot Studio, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-ask-with-adaptive-card](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-ask-with-adaptive-card)  
69. How to implement approval in Teams using Adaptive Cards \- Power Platform Community, accessed May 24, 2025, [https://community.powerplatform.com/blogs/post/?postid=1998fbe8-30e2-4da4-a7eb-214a6998e9c0](https://community.powerplatform.com/blogs/post/?postid=1998fbe8-30e2-4da4-a7eb-214a6998e9c0)  
70. Build your own Windows 365 request approval flow | Peter Klapwijk \- In The Cloud 24-7, accessed May 24, 2025, [https://inthecloud247.com/build-your-own-windows-365-request-approval-flow/](https://inthecloud247.com/build-your-own-windows-365-request-approval-flow/)  
71. Create flows that post adaptive cards to Microsoft Teams \- Power Automate, accessed May 24, 2025, [https://learn.microsoft.com/en-us/power-automate/create-adaptive-cards](https://learn.microsoft.com/en-us/power-automate/create-adaptive-cards)  
72. Agents for Microsoft 365 Copilot, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview)  
73. Custom Approval with Adaptive Card \- Karl-Johan Spiik, Microsoft MVP, accessed May 24, 2025, [https://www.karlex.fi/custom-approval-with-adaptive-card/](https://www.karlex.fi/custom-approval-with-adaptive-card/)  
74. Manage agents for Microsoft 365 Copilot in Integrated Apps ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/admin/manage/manage-copilot-agents-integrated-apps?view=o365-worldwide](https://learn.microsoft.com/en-us/microsoft-365/admin/manage/manage-copilot-agents-integrated-apps?view=o365-worldwide)  
75. How to audit an Azure Cosmos DB \- Vunvulea Radu \- Blogger.com, accessed May 24, 2025, [http://vunvulearadu.blogspot.com/2018/02/how-to-audit-azure-cosmos-db.html](http://vunvulearadu.blogspot.com/2018/02/how-to-audit-azure-cosmos-db.html)  
76. Powering Live Data with Cosmos DB Change Feed \- Applied Information Sciences, accessed May 24, 2025, [https://www.ais.com/powering-live-data-with-cosmos-db-change-feed/](https://www.ais.com/powering-live-data-with-cosmos-db-change-feed/)  
77. Managing Record Changes with Change Feed in Azure Cosmos DB \- NashTech Blog, accessed May 24, 2025, [https://blog.nashtechglobal.com/managing-record-changes-with-change-feed-in-azure-cosmos-db/](https://blog.nashtechglobal.com/managing-record-changes-with-change-feed-in-azure-cosmos-db/)  
78. Getting insights from changes to items in Azure Cosmos DB just got easier\!, accessed May 24, 2025, [https://devblogs.microsoft.com/cosmosdb/getting-insights-from-changes-to-items-in-azure-cosmos-db-just-got-easier/](https://devblogs.microsoft.com/cosmosdb/getting-insights-from-changes-to-items-in-azure-cosmos-db-just-got-easier/)  
79. Change Feed \- Azure Cosmos DB Blog, accessed May 24, 2025, [https://devblogs.microsoft.com/cosmosdb/category/change-feed/feed/](https://devblogs.microsoft.com/cosmosdb/category/change-feed/feed/)  
80. Change feed support in Azure Blob Storage \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-change-feed](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-change-feed)  
81. How to audit Azure Cosmos DB control plane operations \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/audit-control-plane-logs](https://learn.microsoft.com/en-us/azure/cosmos-db/audit-control-plane-logs)  
82. View audit logs for admins, makers, and users of Copilot Studio \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/admin-logging-copilot-studio](https://learn.microsoft.com/en-us/microsoft-copilot-studio/admin-logging-copilot-studio)  
83. Phase 5 of Governance and Security best practices: Monitoring and optimization \- Microsoft Copilot Studio, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/guidance/sec-gov-phase5](https://learn.microsoft.com/en-us/microsoft-copilot-studio/guidance/sec-gov-phase5)