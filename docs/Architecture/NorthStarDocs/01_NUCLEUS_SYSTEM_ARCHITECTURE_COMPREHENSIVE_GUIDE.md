---
title: "Nucleus Project: Advanced Architecture, Implementation, and Operations Guide"
description: "Advanced architectural and implementation details for Project Nucleus, focusing on M365 Agents SDK, MCP, multi-LLM integration, multi-tenancy, .NET Aspire, network isolation, and RAI."
version: 1.1
date: 2025-05-28
see_also:
    - "[00_FOUNDATIONS_TECHNOLOGY_PRIMER.md](00_FOUNDATIONS_TECHNOLOGY_PRIMER.md)"
    - "[00_SYSTEM_EXECUTIVE_SUMMARY.md](../../CoreNucleus/00_SYSTEM_EXECUTIVE_SUMMARY.md)"
    - "[00_REQUIREMENTS_PROJECT_MANDATE.md](../../../ProjectExecutionPlan/00_REQUIREMENTS_PROJECT_MANDATE.md)"
filepath_override: "Docs/Architecture/NorthStarDocs/01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md"
---

# Nucleus Project: Advanced Architecture, Implementation, and Operations Guide

**Purpose:** This guide provides advanced architectural and implementation details for Project Nucleus, building upon the foundational knowledge of MCP and the Microsoft 365 Agents SDK. It is intended for architects and senior developers involved in building, deploying, and managing the distributed Nucleus system.

---

### **Chapter 1: Core Nucleus Agent Architecture**

This chapter delves into the advanced architectural considerations for Nucleus M365 Persona Agents, including strategies for managing multiple personas, orchestrating complex interactions, and advanced state management.

#### **1.1 Implementing Multi-Persona Strategies within a Single Nucleus Agent vs. Multi-Agent Architectures**

The Microsoft 365 Agents SDK is engineered to enable the construction of an "agent container" complete with state management, storage, and comprehensive activity/event management features \. This foundational characteristic suggests that a singular agent application can indeed encapsulate multiple logical personas or distinct skills. The SDK itself is built upon the Azure Bot Service \. The Azure Bot Service, in turn, has a well-established history of supporting "skills" within the Bot Framework v4, where a skill is essentially a bot capable of performing a set of tasks for another bot, with a "root bot" acting as a consumer of one or more such skills \. This pattern holds relevance as the M365 Agents SDK explicitly allows for the implementation of skills \.

A significant design principle of the M365 Agents SDK is its unopinionated stance regarding the AI technologies employed, affording developers the latitude to implement diverse agentic patterns \. This inherent flexibility is conducive to building complex, multi-faceted agents. Furthermore, documentation illustrates scenarios where a "main agent," developed using the Agents SDK, can orchestrate and leverage other agents, including those potentially built with different tools like Copilot Studio \. **The M365 Agents SDK also supports implementing "Skills," which are essentially other agents. This is detailed in the "Choose the right agent solution" documentation (Source: OCR p8), allowing an M365 Agent to potentially call another M365 Agent acting as a skill. This could be relevant for complex persona interactions if not using MCP for all inter-agent communication.**

However, a critical consideration is Microsoft Entra Agent ID. Current understanding suggests an Entra Agent ID is assigned at the agent application level. If a single agent houses numerous personas with varied permission requirements, its singular Entra Agent ID might necessitate a superset of all permissions, which could contravene the principle of least privilege for operations specific to individual personas. Microsoft's overarching vision encompasses multi-agent systems where agents constructed with Copilot Studio, Azure Agent Service, or the M365 Agents SDK can collaborate effectively as a cohesive team, lending further support to a distributed architectural model \.

**Recommendation:** Project Nucleus should adopt a **hybrid multi-persona strategy**.
*   Develop a core Nucleus M365 Agent for common functionalities and well-defined, closely related personas.
*   For highly specialized, resource-intensive, or permission-sensitive personas, create separate, specialized M365 Agents.
*   This balances development agility with security granularity (each specialized agent can have a narrowly scoped Entra Agent ID) and manageable complexity. Internal routing within the core agent and inter-agent communication (possibly adapting patterns from Bot Framework skills or leveraging emerging multi-agent orchestration capabilities) would be key.

#### **1.2 Advanced Orchestration, Asynchronous Processing, and Proactive Messaging**

##### **1.2.1 Orchestration and Asynchronous Processing with M365 Agents SDK**

The M365 Agents SDK is engineered to support agents that not only engage in conversational turns but also "react to events, and those events can be a conversational or operate in the background action to trigger autonomously" \. This capability for autonomous background actions is particularly pertinent for Nucleus. The SDK employs a "turn-based conversational model," and a core feature is the management of multi-turn conversations and the associated storage of context \.

For Nucleus tasks characterized by true asynchronicity and extended execution times (e.g., complex report generation, in-depth data analysis), a robust architectural pattern involves:
1.  **Initial Agent Interaction:** The M365 Agent gathers necessary parameters and captures a `ConversationReference` from the initial interaction \.
2.  **Offload to Background Worker:** The actual long-running workload is then best delegated to a background service. This could be an Azure Function, an Azure WebJob, or an `IHostedService` operating within the agent's Azure App Service environment \. Attempting to manage very long operations solely within extended conversational turns is unlikely to be scalable or reliable, given that conversational turns are generally expected to be relatively short-lived, and HTTP requests to the bot's messaging endpoint are subject to timeouts \.
3.  **Proactive Notification:** Upon completion of the task, this background service would trigger a proactive notification back to the user or channel using the `ChannelAdapter.ContinueConversationAsync` method (or its M365 Agents SDK equivalent) and the stored `ConversationReference` \.

This decoupling of the long-running task execution into a dedicated background worker enhances resilience and scalability. Reliable triggering of these background tasks from the agent can be achieved using mechanisms like Azure Service Bus or other message queuing systems \. Agents can also be architected to subscribe to various system events (e.g., notifications from Microsoft Graph webhooks or Azure Event Grid) and initiate workflows independently, subsequently using proactive messaging to notify users or other systems \.

##### **1.2.2 Dependency Injection (DI) for Proactive Messaging in Background Services**

For a background service (e.g., Azure Function, `IHostedService`) to send proactive messages using the M365 Agents SDK, correct DI setup for the adapter components (e.g., `CloudAdapter` or a more M365-specific `IAgentHttpAdapter` implementation) and an `IAccessTokenProvider` for authentication is crucial \.

*   **CRITICAL REFINEMENT (Based on M365 Agents SDK Samples & Docs):**
    *   The standard way an agent's adapter authenticates to the Bot Connector service (for channel communications, including proactive replies) is via the **`Connections:ServiceConnection` settings in `appsettings.json`**. These settings typically include `AuthType: "ClientSecret"`, along with the `ClientId` (Azure Bot registration App ID) and `ClientSecret` (Azure Bot registration secret). This is evident in SDK samples like `copilotstudio-skill/dotnet/appsettings.json` and `empty-agent/dotnet/appsettings.json`. **The `Microsoft.Agents.Authentication.Msal` package is often used in this context.**
    *   The previously synthesized idea of a `ManagedIdentityTokenProvider` for the *adapter itself* to use Managed Identity for Bot Connector calls is an **advanced scenario not directly demonstrated in the basic SDK setup samples for the primary adapter-to-Bot Connector authentication**. The samples consistently show ClientId/Secret for this `ServiceConnection`.
    *   **Clarification for Background Services Sending Proactive Messages:**
        1.  **Using ClientId/Secret (Standard Sample Approach):** If the background service can securely access the primary agent's `appsettings.json` (e.g., if it's an `IHostedService` within the same application), it might be able to obtain a pre-configured adapter instance that uses the ClientId/Secret from `Connections:ServiceConnection`. This co-locates the secret dependency with the background service if it's part of the same deployment unit.
        2.  **Using Managed Identity (Custom/Advanced Setup):** For a background service to send proactive messages *and avoid secrets for Bot Connector authentication*, it would need a more custom setup. This involves dynamically instantiating and configuring an adapter (e.g., a `CloudAdapter` instance or similar compatible with `ContinueConversationAsync`). This adapter must be configured with an `IAccessTokenProvider` that is backed by `DefaultAzureCredential` (or a specific Managed Identity credential). This allows the adapter to acquire tokens for the Bot Connector service using the background service's Managed Identity. This is a more advanced pattern than the out-of-the-box SDK samples for the primary agent's adapter.
    *   **Emphasis:** The adapter used for proactive messaging needs *some* form of valid credentials to authenticate to the Bot Connector Service. The M365 Agents SDK samples primarily demonstrate configuration-based ClientId/Secret for the adapter-to-service authentication. Using Managed Identity for *this specific adapter-to-Bot Connector purpose* in a background service is an *advanced, custom pattern* that requires careful custom configuration of the adapter and its token provider, differing from the sample agent's typical configuration.

*   **Recommended DI Strategy (for the Managed Identity approach in a background service):**
    *   Register `DefaultAzureCredential` (or a specific `ManagedIdentityCredential`) as the `IAccessTokenProvider` in the background service's DI container.
    *   Register the appropriate Bot/Agent Adapter (e.g., `CloudAdapter` or a custom-configured M365-era adapter compatible with proactive messaging) and its dependencies like `IConfiguration` (for Bot ID) and `ILogger`.
*   **Message Sending Logic:** The background service uses the injected adapter's `ContinueConversationAsync` method. This method requires the `botAppId` of the M365 Agent Persona on whose behalf the message is being sent, and the stored `ConversationReference`. The adapter, using the configured `IAccessTokenProvider` (and thus Managed Identity), handles authentication to the Bot Connector Service \.

#### **1.3 Refactoring Nucleus Core Components for the M365 Agents SDK**

The M365 Agents SDK equips developers with tools to construct "custom engine agents," implying the ability to integrate Nucleus's existing, potentially sophisticated AI stack \. The SDK is model- and orchestrator-agnostic \. Agents developed with the SDK listen for and react to events originating from various channels (e.g., M365 Copilot, Teams) typically using an `OnActivity` handler \.

*   **Refactoring Strategy:** The primary approach involves modularizing Nucleus core logic into distinct services or libraries. These can then be invoked from within the M365 Agent's `OnActivity` handlers or similar event processing structures. The M365 Agent itself should predominantly function as the "adapter" to the M365 ecosystem, while delegating complex business logic and AI processing to these refactored Nucleus services \.
*   **State Management Adaptation:** If Nucleus core components currently employ proprietary state handling mechanisms, significant adaptation will be required to align with the M365 Agents SDK's state management paradigms, which are rooted in Azure Bot Service patterns and use `IStorage` (e.g., `MemoryStorage` for local development, or Azure Blob/Cosmos DB storage extensions for production) \. M365 Agents are inherently stateless at the application layer, depending on SDK-provided mechanisms for state persistence \.

#### **1.4 Advanced State Management Techniques in Nucleus Agents**

The M365 Agents SDK furnishes a storage layer and state management abstractions, building upon established Azure Bot Service principles \. Effective state management is paramount for enabling sophisticated multi-turn conversations \. The SDK introduces the `Microsoft.Agents.Storage.IStorage` abstraction, offering `MemoryStorage` for local development purposes and providing extensibility for more robust solutions like Azure Blob Storage and Cosmos DB in production \. State is typically scoped at different levels: conversation state, user state, and private conversation state. **Crucially, agents created with the M365 Agents SDK are stateless by default. Developers must specifically configure state (or memory), choose where to store or persist that state (e.g., by registering an `IStorage` implementation like `MemoryStorage` or a Cosmos DB-backed one), and ensure it is retrieved and saved appropriately across turns. The `Microsoft.Agents.Hosting.AspNetCore` package provides helper methods like `AddAgentApplicationOptions()` and `AddAgent()` for this setup (Source: OCR p11, `empty-agent/dotnet/Program.cs`). While .NET specific packages for Blob/Cosmos `IStorage` are anticipated, JavaScript packages like `@microsoft/agents-hosting-storage-blob` and `@microsoft/agents-hosting-storage-cosmos` exist (Source: OCR p55), indicating similar patterns.**
*   **Hierarchical/Modular State:** For Nucleus agents that may embody multiple internal personas or engage in complex dialogs, adopting a hierarchical or modular state management approach within the SDK's provided state scopes (such as `ConversationState` and `UserState`) will be indispensable. This could involve defining distinct state objects tailored to each persona or skill, alongside a main orchestrator state that tracks the currently active persona.
*   **Isolation:** When accommodating multiple logical personas within a single agent instance, Nucleus must implement safeguards to ensure that state information pertinent to one persona is not inadvertently accessed or modified by another. This is especially critical if these personas handle data with different sensitivity levels. Such isolation necessitates careful design of state property accessors and potentially the implementation of custom middleware within the agent's turn processing pipeline \.

### **Chapter 2: Holistic Configuration Management for M365 Agents**

Effective configuration management is crucial for M365 Agents, especially in multi-tenant ISV contexts. This involves managing static, deployment-time settings and dynamic, behavioral configurations.

#### **2.1 Foundations of M365 Agent Configuration**

##### **2.1.1 Distinguishing Static (Deployment-Time) vs. Dynamic (Behavioral) Configurations**

*   **Static Configurations:** Core operational settings, service endpoints, and fixed parameters established at deployment, generally read-only at runtime (e.g., default retry policies, fixed thresholds, non-frequent feature flags) \.
*   **Dynamic Behavioral Configurations:** LLM prompts, operational parameters (e.g., sensitivity thresholds, model selection criteria), or business rules designed to adapt based on feedback or runtime factors. Require secure read/write capabilities \.

##### **2.1.2 Core Azure Services for Static Configurations**

*   **Azure App Configuration:** Centralized management of application settings and feature flags. Best practices include using labels for environments, content types for structured data, and considering snapshots for versioning. Supports dynamic refresh of configurations without application restart \ .
*   **Azure Key Vault:** Secure storage for secrets (API keys, connection strings). Key Vault references within Azure App Configuration are recommended, where App Configuration stores a URI to the secret in Key Vault, enhancing security \ .
*   **.NET Integration Patterns:** Utilize the Options pattern (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) to bind configuration sections to strongly-typed C\# objects, promoting type safety and cleaner consumption via Dependency Injection \ . `IOptionsMonitor<T>` is particularly suited for agents needing to pick up static configuration changes without restarting.
*   **Agent-Specific Configuration Structure:** The `appsettings.json` structure observed in M365 Agents SDK samples (e.g., for `AgentApplication` options, `Connections`, `ConnectionsMap`, `AIServices`) should serve as the primary example for agent-specific static configuration. Azure App Configuration and Azure Key Vault would then be used to populate these expected configuration structures in a secure and manageable way for deployed environments.
    *   **Common `AgentApplication` settings** found in `appsettings.json` can include `StartTypingTimer`, `RemoveRecipientMention`, and `NormalizeMentions` (Source: `empty-agent/dotnet/appsettings.json`).
    *   The **`Connections` and `ConnectionsMap` sections** are vital for configuring how the agent connects to the Bot Service (Source: `empty-agent/dotnet/appsettings.json`, `copilotstudio-skill/dotnet/appsettings.json`).
    *   A **`UserAuthorization` section** (seen in the `auto-signin` sample) can configure OAuth connection names.

#### **2.2 Architecting Dynamic Behavioral Configuration Management**

Azure Cosmos DB is recommended for storing dynamic agent configurations like adaptive LLM prompts.

##### **2.2.1 Leveraging Azure Cosmos DB for Storing Agent-Updatable Configurations**

Cosmos DB's global distribution, low-latency access, scalability, and schema flexibility make it well-suited for dynamic agent configurations \ . **The M365 Agents SDK explicitly supports Azure Cosmos DB as a storage option for agent state (e.g., via the `@microsoft/agents-hosting-storage-cosmos` JavaScript package, with .NET equivalents expected or buildable based on the `IStorage` interface), which reinforces its suitability for also storing dynamic behavioral configurations that might influence agent state or processing.**

*   **Schema Design:** Include fields like `id`, `tenantId` (as partition key), `agentId` (optional), `configType`, `configName`, `version`, `isActive` (optional), `value` (string or complex JSON), `metadata` (optional), `lastUpdatedBy`, and `timestamp` (\_ts) \ . Consider a `schemaVersion` field for the document structure itself \.
*   **Tenant Isolation Models (ISV):**
    *   **Partition Key per Tenant:** Often most cost-effective for high tenant density. Each document includes a `tenantId` as the logical partition key in a shared container. Application logic must enforce tenant data separation \.
    *   **Database Account per Tenant:** Strongest physical isolation, higher cost and management.
    *   **Recommendation:** Start with partition-key-per-tenant; offer dedicated accounts for premium tenants.

##### **2.2.2 Secure Read/Write Patterns for Agents**

*   **Agent Identity:** M365 Agent (hosted on Azure) uses Managed Identity to authenticate to Azure resources (Cosmos DB, App Config, Key Vault) \.
*   **Interaction with Cosmos DB (MCP Tool Abstraction Recommended for Nucleus):**
    1.  A custom `Nucleus_PersonaBehaviourConfig_McpServer` (MCP Tool) encapsulates Cosmos DB logic.
    2.  The M365 Agent (as an MCP client using `MCPClientManager`) calls this tool using OAuth 2.1 for secure connection \.
    3.  Critically, `tenantId` from the M365 Agent context is securely propagated to the MCP tool (e.g., as a custom claim in the OAuth token or a validated parameter). The MCP tool uses this `tenantId` for all Cosmos DB operations.
    4.  The MCP tool uses its own Managed Identity for Cosmos DB access.

#### **2.3 Security and Access Control for Agent Self-Configuration (Multi-Tenant ISV Focus)**

A layered authorization model is essential:
1.  **Microsoft Entra ID App Roles (ISV Application):** Define custom app roles (e.g., `Agent.Config.Read.Self`, `Agent.Config.Write.Self`) on the ISV's single multi-tenant app registration. Customer admins grant these to the ISV app's service principal in their tenant \.
2.  **Azure RBAC (Agent/MCP Tool Managed Identity):** The Managed Identity of the agent's backend (or the `PersonaBehaviourConfigMCP` tool) needs Azure RBAC roles for App Config ("App Configuration Data Reader"), Key Vault ("Key Vault Secrets User"), and Cosmos DB ("Cosmos DB Built-in Data Contributor" or custom) \.
3.  **Azure Cosmos DB Data Plane RBAC + Application Logic (Tenant Scope):** Create custom Cosmos DB roles (e.g., `TenantConfigReader`). Assign agent's/MCP tool's Managed Identity these roles. **Crucially, the application logic (in agent or MCP tool) *must* use the `tenantId` (from M365 context) as the partition key in all Cosmos DB operations to enforce tenant scope.** Cosmos DB data plane RBAC alone does not typically restrict operations based on partition key values matching token claims dynamically \.
4.  **Mitigating Dynamic Prompt Modification Risks:** Implement input validation, strict schemas, human-in-the-loop approval for significant prompt changes, segregation of duties, contextual awareness in prompts, output filtering, and never embed secrets in prompts. Adhere to OWASP LLM Top 10 principles, particularly regarding prompt injection and sensitive information disclosure \.

#### **2.4 Operationalizing Dynamic Configuration Updates**

*   **Versioning in Cosmos DB:** Each config document includes `version` and `isActive` fields. Updates create new versions; activation involves toggling `isActive`. Use transactional batch operations for atomic updates of related items within the same logical partition \.
*   **Approval Workflows:** For significant changes, use Azure Logic Apps/Power Automate with Adaptive Cards in Microsoft Teams. Changes are saved as "PendingApproval" in Cosmos DB; workflow handles approval and activation \.
*   **Auditing and Monitoring:**
    *   **Azure Cosmos DB Change Feed:** Processed by Azure Functions to send audit logs (change details, tenant, agent, principal, timestamp) to Azure Monitor (Application Insights/Log Analytics) or Microsoft Sentinel \. Control plane logs for Cosmos DB itself are also important \.
    *   **Agent-Side Logging:** Log configuration reads/updates and errors to Application Insights.
    *   **Monitoring/Alerting:** Set up alerts in Azure Monitor based on audit logs.

#### **2.5 Holistic Configuration Strategy (Layering and Refresh)**

*   **Layering:** Local Defaults (`appsettings.json`) -> Environment Variables (Hosting) -> Azure App Configuration (+ Key Vault Refs for secrets) -> Azure Cosmos DB (Dynamic Behavioral, via `Nucleus_PersonaBehaviourConfig_McpServer`).
*   **Dynamic Refresh:**
    *   **App Config:** Use `IOptionsMonitor<T>` with the .NET provider for App Config (polling or sentinel key) \.
    *   **Cosmos DB:** Agent reads on-demand, with caching (e.g., `IMemoryCache` with expiry), or advanced event-driven refresh (e.g., Change Feed -> SignalR -> Agent).

Concrete pattern for loading complex `PersonaConfiguration` including nested JSON with Key Vault resolution using `IOptions<T>`:
1.  Define POCOs (`PersonaConfigurationPoco`, `LlmSettingsPoco`).
2.  Azure App Configuration stores non-sensitive values and Key Vault references for sensitive fields (e.g., `{"@Microsoft.KeyVault(SecretUri=...)}"`) \.
3.  .NET Aspire AppHost defines resources for App Config and Key Vault, passing connection info to the agent service \.
4.  Agent's `Program.cs` uses `AddAzureAppConfiguration` with `.ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()))` to enable Managed Identity access and Key Vault reference resolution \.
5.  `builder.Services.Configure<PersonaConfigurationPoco>(builder.Configuration.GetSection("PersonaConfigRootKey"))` binds the configuration.

#### **2.6 Complex Configuration Management Considerations**

*   **Schema Versioning and Evolution:** For `PersonaConfiguration` and other complex objects, plan for schema changes. Use versioning in App Config (snapshots, labels) or Cosmos DB (document versioning). Implement tolerant deserialization or custom `IConfigureOptions<T>` for migrations \.
*   **Dynamic Configuration Refresh Impact:** Identify which agent components can handle dynamic reconfiguration via `IOptionsMonitor<T>`. Ensure robust re-initialization or behavior adaptation for sensitive changes like API keys \.

### **Chapter 3: Integrating External AI Models and Backend Services**

This chapter details strategies for integrating diverse AI models and how M365 Agents will leverage backend Nucleus MCP Tools.

#### **3.1 Advanced Integration of Non-Azure Large Language Models**

##### **3.1.1 Bridging M365 Agents SDK with External LLMs (Gemini, OpenRouter.AI)**

The M365 Agents SDK's LLM agnosticism is key \.
*   **Semantic Kernel:** Can act as an orchestrator within the M365 Agent, abstracting LLM differences (Gemini, OpenRouter.AI) and supporting tool calling \ . **The `weather-agent/dotnet` SDK sample is a strong example of this pattern, where Semantic Kernel (`Kernel` instance and `ChatCompletionAgent`) is used directly within the `AgentApplication`-derived class (e.g., `MyAgent.cs`) to interact with an LLM and its plugins.**
*   **`Microsoft.Extensions.AI.IChatClient`:** Primary .NET abstraction. Custom wrappers for Gemini (using official Google SDK) or OpenRouter (HTTP calls) can implement `IChatClient` \ . The library also includes `FunctionInvokingChatClient` for tool invocation \ .
*   **SDK Documentation Emphasis:** "A key feature of the Agents SDK is that you can make your choice of AI services and orchestrator and integrate these into your agent using their SDKs. Choose from Azure AI Foundry, Semantic Kernel, LangChain and more." (Source: OCR p13 - Add AI services and orchestration to your agent).

##### **3.1.2 Tool Calling and Streaming: Compatibility and Nuances**

*   **Schema Compatibility:** LLM providers have differing JSON schemas for tools/functions (e.g., Google Gemini \ versus OpenAI/OpenRouter \). Abstraction layers like Semantic Kernel and `IChatClient` attempt normalization, but provider-specific limitations (e.g., Gemini OpenAPI spec issues \ , OpenRouter model-specific behaviors for argument-less functions or history handling \ ) can persist. The `actions.json` pattern from Teams AI library for declaring tools is a relevant concept \.
*   **Robust Streaming:** LLM support for streaming varies, especially with tool calls. OpenRouter.AI supports streaming for many models and uses keep-alive comments \. Client-side handling (Semantic Kernel or `IChatClient` implementation) must accommodate these.
*   **Abstraction vs. Specificity (Tool Calling):** Abstraction layers simplify but might not support unique/advanced tool features of a specific LLM (e.g., Gemini's `propertyOrdering` \ or `ANY` mode for function calling \). Evaluate trade-offs.
*   **Prompt Engineering for Tool Use:** Tool descriptions and system prompts must be clear and potentially adapted for different LLMs \. The verbosity and structure of tool schemas also impact token consumption and LLM comprehension \.

#### **3.2 Secure Credential Management for Third-Party LLM API Keys**

Microsoft Entra Agent ID (manifesting as a Managed Identity for the agent's Azure-hosted compute) is used to securely access Azure Key Vault. Key Vault stores the API keys for third-party LLMs (e.g., Google Gemini, OpenRouter.AI) \.
*   **Pattern:** The M365 Agent backend (e.g., hosted on Azure App Service) uses its Managed Identity to retrieve these LLM API keys from Azure Key Vault at runtime. API keys should never be hardcoded or stored directly in application configuration files \.

#### **3.3 Leveraging Model Context Protocol (MCP) for Backend Nucleus Capabilities**

##### **3.3.1 Defining and Exposing Backend Nucleus Capabilities as MCP Tools**

Backend Nucleus functionalities (e.g., data access via `ArtifactMetadataContainer` or `KnowledgeContainers` in Cosmos DB, file processing, RAG) are exposed as MCP tools \.
*   **Tool Definition:** An MCP tool definition requires:
    1.  `name`: A unique, machine-readable identifier.
    2.  `description`: Clear, concise natural language description of purpose, capabilities, and use cases (critical for LLM understanding).
    3.  `inputSchema` (or `parameters`): Structured definition (typically JSON Schema) of input parameters, their types, descriptions, and if required/optional \.
*   **Exposing via MCP Server:** Backend Nucleus services act as MCP servers.
    *   Semantic Kernel can expose existing `KernelFunction`s as MCP tools \.
    *   Azure Functions can define tools using attributes like `[McpServerToolType]` and `[McpTool]`, with metadata exposed via `builder.EnableMcpToolMetaData()` \.

##### **3.3.2 LLM-driven Invocation of MCP Tools by M365 Agent**

The M365 Agent's integrated LLM can discover and invoke these MCP tools \.
1.  **Tool Presentation to LLM:** The M365 Agent's runtime (or an orchestrator like Semantic Kernel) provides the LLM with available MCP tools (names, descriptions, schemas). Semantic Kernel can convert MCP tools into `KernelFunction` objects for its plugins \. The `OpenAIPromptExecutionSettings` might require `FunctionChoiceBehavior.Auto` \.
2.  **LLM Tool Invocation Request:** The LLM, based on user intent, generates a request to call a function (the MCP tool) with arguments.
3.  **Translation to MCP Call:** The M365 Agents SDK, or an integrated orchestrator like Semantic Kernel, recognizes the LLM's intent, translates it into an MCP-compliant request, and sends this to the Nucleus MCP server. This includes securely propagating `tenantId` (from M365 context) to the MCP tool, ideally via an authentication token with a `tenantId` claim \.
4.  **Return Response to LLM:** The MCP server processes the request (scoped by `tenantId`) and returns the result. The M365 Agent/Semantic Kernel formats this and feeds it back to the LLM for generating the final user response \.
*   **Semantic Richness of Tool Definitions:** Essential for LLM autonomy. High-quality names, descriptions, and parameter definitions are paramount \.
*   **Complex Orchestration/Chaining of MCP Tools:** The agent's LLM or orchestrator (e.g., Semantic Kernel planner) handles decomposing user requests, managing state between calls, and error handling for multi-step MCP tool chains \.

### **Chapter 4: Multi-Tenant ISV Design and Governance for Nucleus**

This chapter covers strategies for ISV publishing, identity management with Entra Agent ID in customer tenants, and ensuring data isolation for a multi-tenant Nucleus deployment.

#### **4.1 Distribution Models for Nucleus: ISV and Open-Source Publishing via Agent Store**

*   **Agent Store:** The primary distribution channel for ISVs like Nucleus, enabling discovery within Microsoft 365 Copilot Chat. It supports agents for both M365 Copilot licensed and unlicensed customers \. Published agents (whether low-code via Copilot Studio or pro-code via M365 Agents Toolkit) must adhere to validation guidelines covering functionality, reliability, security, and Responsible AI \.
*   **Open-Source Coexistence:** An open-source distribution model for the Nucleus agent *code* (e.g., via GitHub releases) can coexist with publishing a discoverable M365 Agent via the Agent Store. The Agent Store acts as the "shopfront" and integration point into the M365 Copilot ecosystem.

#### **4.2 Microsoft Entra ID App Registration Strategy & Entra Agent ID in Customer Tenants**

*   **Recommended Pattern (ISV): Single Multi-Tenant App Registration with App Roles.** For most SaaS applications, a single multi-tenant app registration in the ISV's home Entra ID tenant is advised \ . Distinct functionalities for different Persona Agents are managed by defining app roles (e.g., `EduFlow.ReadWrite`, `Helpdesk.Use`) within this registration \ . "Allowed member types" for these app roles would typically be "Applications" if other M365 Agents (Service Principals) or backend services need to call them, or "Users/Groups" if direct user consent/assignment is involved for certain permissions. Customer administrators grant consent to the ISV application's service principal in their tenant, which then allows the ISV application to act with the granted roles \ .
    *   **Clarification on Identities:**
        *   The **`${{AAD_APP_CLIENT_ID}}`** placeholder in agent manifest files (`teams-manifest.json`, `m365copilot-manifest.json`) refers to the **Azure AD App Registration ID of the Azure Bot resource** itself. This is the identity the Bot Service uses to communicate with the agent.
        *   In `appsettings.json`, the **`Connections:ServiceConnection:Settings:ClientId`** is typically this same Azure Bot's App Registration ID.
        *   A **single multi-tenant ISV app registration** for the broader Nucleus *platform* (e.g., for backend MCP tools requiring unified Microsoft Graph access, or for M365 Agents to authenticate to those MCP tools using OAuth) is a distinct identity. This platform app registration would have its own App ID and would be consented to by customers for platform-level capabilities. This separation is crucial: individual M365 Persona Agents have their Bot App IDs for channel communication, while the overarching Nucleus platform might use a different App ID for its backend services and cross-cutting concerns.
*   **Entra Agent ID in Customer Tenants:** When an M365 Agent (published by an ISV like Nucleus) is installed in a customer tenant, an Entra Agent ID (representing the agent's identity in that specific tenant) is provisioned. This allows the agent to operate securely within the customer's M365 environment, respecting their policies. The exact mechanisms for how this Entra Agent ID relates to the ISV's app registration and the permissions granted require careful attention to Microsoft's evolving guidance \ .

#### **4.3 Data Isolation and Security in Multi-Tenant Nucleus**

Ensuring data isolation and security in a multi-tenant environment is critical, especially when using shared resources like Azure Cosmos DB.

##### **4.3.1 Data Isolation Strategies**

*   **Logical Isolation with Shared Resources:** Use of shared Azure resources (e.g., Cosmos DB) with logical isolation enforced at the application level. Each tenant's data is segregated using techniques like partition keys (`tenantId`) and careful access control \.
*   **Dedicated Resources for High-Security Tenants:** Option to provision dedicated resources (e.g., separate Cosmos DB accounts) for tenants with stringent security requirements. This provides physical data separation at a higher cost \.
*   **Dynamic Data Masking and Encryption:** Implement dynamic data masking and encryption to protect sensitive information within shared databases. Only authorized users or services should have the ability to decrypt and view sensitive data \.

##### **4.3.2 Security Best Practices**

*   **Least Privilege Access:** Always adhere to the principle of least privilege when assigning permissions to agents, services, and users. Regularly review and audit permissions to ensure compliance \.
*   **Network Security:** Utilize Azure Virtual Networks, Network Security Groups, and Azure Firewall to control and restrict network access to Azure resources. Consider using private endpoints for critical services like Cosmos DB and Key Vault \.
*   **Monitoring and Auditing:** Implement comprehensive monitoring and auditing of all access and configuration changes. Use Azure Monitor, Azure Security Center, and Azure Sentinel to gain insights and detect potential security incidents \.
*   **Regular Security Assessments:** Conduct regular security assessments and penetration testing to identify and remediate vulnerabilities in the Nucleus deployment \.

### **Chapter 5: Deployment, Networking, and Operations**

This chapter focuses on deploying the distributed Nucleus system using .NET Aspire, establishing secure Azure network architectures, and managing operations.

#### **5.1 Leveraging .NET Aspire for Developing and Deploying Nucleus**

.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready, distributed .NET applications, simplifying local development and Azure deployment \.
*   **Local Development (AppHost):** The `Nucleus.AppHost` project orchestrates local instances of M365 Agent applications, backend MCP Tool/Server applications, and emulated services (e.g., Cosmos DB emulator as a container, Redis, or local RabbitMQ/NATS for queues if Service Bus emulator is tricky). It manages configurations and inter-service communication by injecting environment variables or connection strings \.
*   **Azure Deployment (`azd`):**
    *   .NET Aspire (in publish mode) generates a deployment manifest (`manifest.json`). The Azure Developer CLI (`azd`) uses this manifest and associated Bicep templates (either generated by Aspire or custom-authored) to provision and deploy the entire distributed Nucleus system to Azure \.
    *   This typically involves provisioning multiple Azure Container Apps (ACA) for M365 Agents and MCP Tools, along with Azure Cosmos DB, Azure Service Bus, Azure Key Vault, and Azure App Configuration.
    *   **Service Discovery in ACA:** ACA provides built-in DNS-based service discovery for services within the same ACA environment. Aspire/`azd` injects connection strings and service URLs as environment variables into containers based on `WithReference()` calls in the AppHost \.
    *   **Secure Inter-Service Authentication (ACA):** Use Managed Identities. .NET Aspire version 9.2 and later default to assigning each ACA its own dedicated managed identity and facilitate defining least-privilege Azure role assignments in C\# within AppHost, which `azd` translates to Bicep \. Services authenticate by acquiring/validating OAuth 2.0 tokens.
    *   **Configuration, Health Checks, Telemetry (ACA):** Integration with Azure App Configuration and Key Vault uses Managed Identities. The `AddServiceDefaults()` extension in Aspire projects configures standard ASP.NET Core health check endpoints (probed by ACA) and OpenTelemetry support (for logs, metrics, traces), which can export to Azure Monitor Application Insights \.
*   **Manifest as Source of Truth:** The accuracy of AppHost C\# definitions is critical as they dictate the deployment manifest and thus cloud behavior. Any local development assumptions not captured in AppHost definitions can lead to misconfigurations \.
*   **Managed Identities & RBAC Complexity:** Meticulously plan and verify Azure role assignments for each service's Managed Identity to all Azure resources it accesses. Misconfigured roles are a common source of "access denied" errors \.

#### **5.2 Definitive Network Isolation Architectures for VNet-Hosted M365 Agents**

This addresses securing VNet-hosted M365 Agents that require connectivity to public channels (like Microsoft Teams) and potentially external LLMs.
*   **Securing VNet-Hosted M365 Agents with Public Channel Connectivity (Application Gateway/APIM Recommended):**
    *   **Pattern:** Microsoft Teams → Azure Bot Service → Public IP of Azure Application Gateway (with WAF) or Azure API Management → Application Gateway/APIM (in VNet) → Private Endpoint on Customer VNet → M365 Agent compute (e.g., ACA/App Service with internal-only ingress) \. This architecture is recommended because channels like Teams cannot directly connect into a private customer VNet without an intermediary \.
    *   **Agent Endpoint Security (VERY IMPORTANT):** The agent's messaging endpoint (e.g., `/api/messages`) **must be secured**. The `full-authentication/dotnet` SDK sample provides a crucial example in its `AspNetExtensions.cs` file with the `AddAgentAspNetAuthentication()` method. This extension:
        *   Configures JWT Bearer authentication.
        *   Validates tokens from Azure Bot Service (e.g., issuer `https://api.botframework.com`).
        *   Can validate standard Entra ID tokens (for other scenarios like direct agent-to-agent calls or skills).
        *   Uses `OpenIdConnectConfigurationRetriever` and `ConfigurationManager` for fetching/caching OpenID metadata to validate signing keys.
        *   Allows configuration of `Audiences`, `TenantId`, `ValidIssuers`, and `IsGov` cloud settings.
        *   The `MapPost(...).RequireAuthorization()` pattern in the `full-authentication` sample's `Program.cs` demonstrates applying this security to the agent's endpoint. This is the standard approach for how an Application Gateway or APIM should forward requests to a secured agent endpoint.
    *   **Azure Bot Service Private Link/Private Endpoint Capabilities for ALL Channel Traffic:** As of May 2025, Azure Bot Service **does not offer** robust Private Link capabilities that enable it to directly and privately connect to an agent's VNet-isolated endpoint for *all incoming channel traffic* from public channels like Teams or M365 Copilot, thereby eliminating the need for any public IP exposure on the agent's compute or its fronting proxy. Private Endpoints for Bot Service sub-resources (`Bot`, `Token`) serve other purposes like outbound bot communication or internal VNet Direct Line clients \.
    *   **Direct Line App Service Extension (DL-ASE) Applicability:** DL-ASE is *not* for Teams/Copilot public channel ingress but for private Direct Line clients within the VNet. Microsoft guidance (from September 2023) advises using the Azure Service Tag method for network isolation and limiting DL-ASE use \.
*   **Azure Firewall for Egress Control:**
    *   All VNet outbound traffic (from M365 Agents, MCP Tools, Background Workers) should be routed through a central Azure Firewall instance using User-Defined Routes (UDRs).
    *   **Required Azure Dependencies (Service Tags):** Use Service Tags in Azure Firewall rules for `AzureBotService`, `AzureActiveDirectory`, `AzureKeyVault`, `AzureStorage`, `AzureAppConfiguration`, `AzureCosmosDB`, `AzureMonitor` \.
    *   **External LLM Endpoints (FQDNs):** Explicitly allow API endpoints for non-Azure LLMs:
        *   Google Gemini API: `generativelanguage.googleapis.com` \.
        *   OpenRouter.AI API: `openrouter.ai` (API calls usually to `https://openrouter.ai/api/v1/...`) \.
    *   Prioritize Azure Firewall Application Rules for FQDNs due to potentially dynamic IPs.
*   **"Weakest Link" in Private Endpoint Strategy:** Communication from Teams to Azure Bot Service, and Bot Service to the public ingress (App GW/APIM), inherently traverses public network segments (secured by TLS, WAF). True "end-to-end private" refers to securing the agent's hosting and backend dependencies \.

### **Chapter 6: Governance and Compliance for Nucleus Agents**

This chapter addresses Responsible AI (RAI) considerations and requirements for submitting Nucleus M365 Agents to marketplaces.

#### **6.1 Responsible AI (RAI) for Custom Engine Agents (including non-Azure LLMs)**

*   **Developer Responsibility:** For custom engine agents like Nucleus M365 Agents, particularly those using non-Azure LLMs, the developer bears significant responsibility for ensuring compliance, RAI practices, and security measures \. Microsoft advocates a layered approach to mitigating LLM harms (model, safety system, metaprompt/grounding, UX) \.
*   **Data Handling, Privacy, Security:** Strict adherence to data privacy regulations (e.g., GDPR) is essential, especially when agents process enterprise data \. Secure external LLM integration includes Key Vault for API keys, TLS, and input/output validation. Microsoft Purview can assist with data governance \.
*   **RAI Practices:** Proactively implement mechanisms to detect, mitigate, and manage AI-related risks (harmful content, bias, misinformation). This involves careful prompt engineering, content filters, potential human-in-the-loop processes, and transparency \.

#### **6.2 Implications for Microsoft 365 Agent Store / Commercial Marketplace Submission**

*   **Stricter Scrutiny for Non-Azure LLMs:** Agents using non-Azure LLMs will likely face detailed scrutiny regarding their implemented RAI measures, data security, and compliance.
*   **Adherence to Marketplace Policies:** All offers must comply with Microsoft Commercial Marketplace certification policies (technical validation, security compliance, privacy policies, terms of use) \.
*   **AI-Powered Application Guidelines:** Guidelines (e.g., from Teams Store, likely similar for M365 Agent Store) emphasize clear AI description, user reporting for harmful content, and high-quality agent responses \.
*   **"Trust Boundary" Challenge:** With non-Azure LLMs, the core reasoning engine operates outside Microsoft's direct RAI oversight. The ISV (Nucleus) becomes responsible for vouching for the RAI safety of these components, requiring comprehensive documentation and evidence of due diligence.
*   **Consistent Standards:** Microsoft will likely expect custom engine agents to meet RAI *outcomes* equivalent to those achieved with Azure OpenAI, regardless of the underlying LLM. Proactive alignment with Microsoft's RAI framework principles is advisable.

### **Overall Conclusion and Strategic Recommendations for Project Nucleus**

This advanced guide has detailed strategies for core agent architecture, holistic configuration management, multi-LLM integration, multi-tenancy, ISV design, backend MCP tool utilization, .NET Aspire for deployment, definitive network isolation, and RAI/governance.

**Key Strategic Recommendations (Consolidated):**

1.  **Proactive Messaging DI:** Finalize DI setup for `ChannelAdapter`/`IAccessTokenProvider` (with `DefaultAzureCredential`) in background services (Azure Functions, `IHostedService`) for secure, Managed Identity-based proactive messaging by M365 Agents.
2.  **Configuration Schema Management & Dynamic Refresh:** Implement versioning for dynamic `PersonaConfiguration` in Cosmos DB (accessed via `Nucleus_PersonaBehaviourConfig_McpServer`). Design M365 Agent components using `IOptionsMonitor<T>` for dynamic refresh of static configurations from Azure App Configuration.
3.  **Non-Azure LLM Tooling Rigor:** For each non-Azure LLM (Gemini, OpenRouter models), conduct thorough testing of tool calling and streaming capabilities through the chosen abstraction layer (Semantic Kernel, `IChatClient`). Develop LLM-specific tool descriptions or prompt augmentations if necessary for reliable tool use by M365 Agents.
4.  **Entra ID App Role Design (ISV):** Adopt the single multi-tenant "Nucleus Platform" Entra app registration model. Meticulously design app roles to correspond to distinct Nucleus M365 Persona Agent functionalities, ensuring they align with the principle of least privilege when customer administrators grant consent.
5.  **Exploit New Cosmos DB Features:** Strategically implement Global Secondary Indexes, filtered vector search (with `tenantId` scope), and investigate RU pooling for the `ArtifactMetadataContainer` and `KnowledgeContainers` to optimize performance, search relevance, and cost in the multi-tenant backend accessed by `Nucleus_KnowledgeStore_McpServer`.
6.  **MCP Tool Definition Quality:** Invest significant effort in crafting clear, unambiguous, and semantically rich MCP tool definitions (name, description, input schema) for all backend Nucleus capabilities to maximize LLM accuracy in tool selection and invocation by M365 Agents.
7.  **.NET Aspire Manifest Validation:** Establish a practice of reviewing the Aspire-generated deployment manifest (`manifest.json`) and the derived Bicep templates before `azd up` to ensure they accurately reflect the intended Azure resource configuration and inter-service dependencies for the distributed Nucleus system.
8.  **Network Egress Control for LLMs:** For external LLM FQDNs, prioritize Azure Firewall Application Rules. Continuously monitor and update rules based on provider guidance due to potentially dynamic IPs.
9.  **RAI Documentation for Non-Azure LLMs:** Proactively develop comprehensive documentation detailing the RAI diligence, safety measures, content filtering, and compliance adherence for any non-Azure LLM used by Nucleus M365 Persona Agents or backend MCP Tools. This will be crucial for Microsoft 365 Agent Store submission and enterprise adoption.
10. **Entra Agent ID Monitoring:** Closely monitor the evolution of Microsoft Entra Agent ID and its integration with M365 Copilot agents and third-party tools, assessing its impact on the ISV agent identity and permission model for Nucleus M365 Persona Agents.

By addressing these advanced implementation details with the recommended approaches, Project Nucleus can build a robust, secure, scalable, and highly capable M365 Agent platform. Continuous monitoring of Microsoft's evolving guidance in these rapidly advancing areas will also be crucial.

---

### **Works Cited**

1.  Choose the right agent solution to support your use case | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution)
2.  How agents work in the Microsoft 365 Agents SDK (preview) | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk)
3.  Skills overview - Bot Service | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/bot-service/skills-conceptual?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/skills-conceptual?view=azure-bot-service-4.0)
4.  Microsoft 365 Agents SDK | Microsoft Learn. Accessed May 24, 2025. [https://microsoft.github.io/Agents/](https://microsoft.github.io/Agents/)
5.  Architecting your multi agent solutions with Copilot Studio and M365 ... | YouTube. Accessed May 24, 2025. [https://www.youtube.com/watch?v=pG01UDoM3xE](https://www.youtube.com/watch?v=pG01UDoM3xE)
6.  Microsoft Build 2025 | Microsoft News. Accessed May 24, 2025. [https://news.microsoft.com/build-2025/](https://news.microsoft.com/build-2025/)
7.  Send proactive notifications to users - Azure documentation | Microsoft Learn (docs.azure.cn). Accessed May 24, 2025. [https://docs.azure.cn/en-us/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0](https://docs.azure.cn/en-us/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0)
8.  ChannelAdapter.ContinueConversationAsync Method (Microsoft.Agents.Builder) | Microsoft Learn (.NET API docs M365 Agents SDK). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.channeladapter.continueconversationasync?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.channeladapter.continueconversationasync?view=m365-agents-sdk)
9.  PnP Core SDK with "system-assigned managed identity" authentication #457 | GitHub (pnp/pnpcore). Accessed May 24, 2025. [https://github.com/pnp/pnpcore/issues/457](https://github.com/pnp/pnpcore/issues/457)
10. Azure Active Directory Develop | PDF | .NET Framework | Autenticación - Scribd. Accessed May 24, 2025. [https://es.scribd.com/document/695740912/azure-active-directory-develop](https://es.scribd.com/document/695740912/azure-active-directory-develop)
11. Event driven architecture to process the M365 resource activities | YouTube. Accessed May 24, 2025. [https://www.youtube.com/watch?v=HP2HYpFm6qI](https://www.youtube.com/watch?v=HP2HYpFm6qI)
12. Best practices for improving performance using Azure Service Bus | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements)
13. Create and Deploy a Custom Engine Agent with Microsoft 365 Agents SDK | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk)
14. Microsoft 365 Agents SDK JavaScript reference | Microsoft Learn (overview/agents-overview). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/javascript/api/overview/agents-overview?view=agents-sdk-js-latest](https://learn.microsoft.com/en-us/javascript/api/overview/agents-overview?view=agents-sdk-js-latest)
15. Quickstart: Create an agent with the Agents SDK | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent)
16. Agents for Microsoft 365 Copilot | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview)
17. Bring Your Agents into Microsoft 365 Copilot with the Agents SDK | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/bring-agents-to-copilot](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/bring-agents-to-copilot)
18. Set Up Your Development Environment to Extend Microsoft 365 Copilot | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/prerequisites](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/prerequisites)
19. Tutorial for using Azure App Configuration dynamic configuration in ... | Microsoft Learn (Azure Functions C#). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-azure-functions-csharp](https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-azure-functions-csharp)
20. Azure App Configuration best practices | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices](https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices)
21. azure-dev/cli/azd/CHANGELOG.md at main | GitHub (Azure/azure-dev). Accessed May 24, 2025. [https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md](https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md)
22. Options pattern in ASP.NET Core | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0)
23. Azure MCP Server tools | Microsoft Learn (Cosmos DB reference). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/)
24. What is the Microsoft 365 Agents SDK | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview)
25. Azure Cosmos DB design pattern: Document Versioning - Code Samples | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/document-versioning/](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/document-versioning/)
26. Azure Cosmos DB design pattern: Schema Versioning - Code Samples | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/schema-versioning/](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/schema-versioning/)
27. Multitenancy and Azure Cosmos DB - Azure Architecture Center | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/cosmos-db](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/cosmos-db)
28. Configure managed identities with Microsoft Entra ID for your Azure Cosmos DB account | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-managed-identity](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-managed-identity)
29. App Configuration tools for the Azure MCP Server | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/app-configuration](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/app-configuration)
30. Piecing together the Agent puzzle: MCP, authentication ... | Cloudflare Blog. Accessed May 24, 2025. [https://blog.cloudflare.com/building-ai-agents-with-mcp-authn-authz-and-durable-objects/](https://blog.cloudflare.com/building-ai-agents-with-mcp-authn-authz-and-durable-objects/)
31. Overview of permissions and consent in the Microsoft identity platform | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview](https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview)
32. Use data plane role-based access control - Azure Cosmos DB for ... (NoSQL) | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac)
33. Large Language Model (LLM) Security Risks and Best Practices | Legit Security. Accessed May 24, 2025. [https://www.legitsecurity.com/aspm-knowledge-base/llm-security-risks](https://www.legitsecurity.com/aspm-knowledge-base/llm-security-risks)
34. LLM01: Prompt Injection - OWASP Top 10 for LLM & Generative AI Security | OWASP Foundation. Accessed May 24, 2025. [https://genai.owasp.org/llmrisk2023-24/llm01-24-prompt-injection/](https://genai.owasp.org/llmrisk2023-24/llm01-24-prompt-injection/)
35. Work with stored procedures, triggers, and UDFs in Azure Cosmos DB | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/stored-procedures-triggers-udfs](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/stored-procedures-triggers-udfs)
36. Build API plugins as declarative agent actions using TypeSpec for Microsoft 365 Copilot | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/build-api-plugins-typespec](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/build-api-plugins-typespec)
37. How to audit an Azure Cosmos DB - Vunvulea Radu | Blogger.com. Accessed May 24, 2025. [http://vunvulearadu.blogspot.com/2018/02/how-to-audit-azure-cosmos-db.html](http://vunvulearadu.blogspot.com/2018/02/how-to-audit-azure-cosmos-db.html) (Change Feed context)
38. How to audit Azure Cosmos DB control plane operations | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/cosmos-db/audit-control-plane-logs](https://learn.microsoft.com/en-us/azure/cosmos-db/audit-control-plane-logs)
39. Semantic Kernel Agent Framework | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/)
40. Function calling with chat completion | Microsoft Learn (Semantic Kernel). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/)
41. Microsoft.Extensions.AI libraries - .NET | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
42. gunpal5/Google_GenerativeAI ... C# .Net SDK for Google Generative AI | GitHub. Accessed May 24, 2025. [https://github.com/gunpal5/Google_GenerativeAI](https://github.com/gunpal5/Google_GenerativeAI)
43. Function Calling with the Gemini API | Google AI for Developers. Accessed May 24, 2025. [https://ai.google.dev/gemini-api/docs/function-calling](https://ai.google.dev/gemini-api/docs/function-calling)
44. OpenRouter FAQ | Developer Documentation. Accessed May 24, 2025. [https://openrouter.ai/docs/faq](https://openrouter.ai/docs/faq)
45. Tool & Function Calling | Use Tools with OpenRouter. Accessed May 24, 2025. [https://openrouter.ai/docs/features/tool-calling](https://openrouter.ai/docs/features/tool-calling)
46. Gemini AI Structured Output with references via OpenAI SDK - Stack Overflow. Accessed May 24, 2025. [https://stackoverflow.com/questions/79588648/gemini-ai-structured-output-with-references-via-openai-sdk](https://stackoverflow.com/questions/79588648/gemini-ai-structured-output-with-references-via-openai-sdk)
47. Gemini models via Openrouter not supported · Issue #5621 · microsoft/autogen | GitHub. Accessed May 24, 2025. [https://github.com/microsoft/autogen/issues/5621](https://github.com/microsoft/autogen/issues/5621)
48. Openrouter: Error inside tool result when using functions without arguments · Issue #5666 · microsoft/autogen | GitHub. Accessed May 24, 2025. [https://github.com/microsoft/autogen/issues/5666](https://github.com/microsoft/autogen/issues/5666)
49. Build an AI agent bot in Teams | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams)
50. API Streaming | Real-time Model Responses in OpenRouter. Accessed May 24, 2025. [https://openrouter.ai/docs/api-reference/streaming](https://openrouter.ai/docs/api-reference/streaming)
51. Announcing Microsoft Entra Agent ID: Secure and manage your AI agents | Microsoft Tech Community (Entra Blog). Accessed May 24, 2025. [https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392](https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392)
52. Best practices for using Azure Key Vault | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)
53. Build MCP Remote Servers with Azure Functions | .NET Blog. Accessed May 24, 2025. [https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/](https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/)
54. Integrating Model Context Protocol Tools with Semantic Kernel ... | Microsoft Developer Blogs. Accessed May 24, 2025. [https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/](https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/)
55. Using Model Context Protocol in agents - Pro-code agents with Semantic Kernel | developerscantina.com. Accessed May 24, 2025. [https://www.developerscantina.com/p/mcp-semantic-kernel/](https://www.developerscantina.com/p/mcp-semantic-kernel/)
56. Plugins in Semantic Kernel | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/)
57. Introducing the Agent Store: Build, publish, and discover agents ... | Microsoft 365 Developer Blog. Accessed May 24, 2025. [https://devblogs.microsoft.com/microsoft365dev/introducing-the-agent-store-build-publish-and-discover-agents-in-microsoft-365-copilot/](https://devblogs.microsoft.com/microsoft365dev/introducing-the-agent-store-build-publish-and-discover-agents-in-microsoft-365-copilot/)
58. Guidelines to Validate Agents - Teams | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoftteams/platform/concepts/deploy-and-publish/appsource/prepare/review-copilot-validation-guidelines](https://learn.microsoft.com/en-us/microsoftteams/platform/concepts/deploy-and-publish/appsource/prepare/review-copilot-validation-guidelines)
59. Establish applications in the Microsoft Entra ID ecosystem | GitHub (MicrosoftDocs/entra-docs). Accessed May 24, 2025. [https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/architecture/establish-applications.md](https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/architecture/establish-applications.md)
60. Add app roles and get them from a token - Microsoft identity platform ... | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps)
61. Identity and account types for single- and multitenant apps - Microsoft identity platform | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/security/zero-trust/develop/identity-supported-account-types](https://learn.microsoft.com/en-us/security/zero-trust/develop/identity-supported-account-types)
62. Grant tenant-wide admin consent to an application - Microsoft Entra ... | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/grant-admin-consent](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/grant-admin-consent)
63. Convert single-tenant app to multitenant on Microsoft Entra ID - Azure documentation | Microsoft Learn (docs.azure.cn). Accessed May 24, 2025. [https://docs.azure.cn/en-us/entra/identity-platform/howto-convert-app-to-be-multi-tenant](https://docs.azure.cn/en-us/entra/identity-platform/howto-convert-app-to-be-multi-tenant)
64. Data Isolation Strategies in Multi-Tenancy Azure Architecture | NashTech Blog. Accessed May 24, 2025. [https://blog.nashtechglobal.com/data-isolation-strategies-in-multi-tenancy-azure-architecture/](https://blog.nashtechglobal.com/data-isolation-strategies-in-multi-tenancy-azure-architecture/)
65. Tutorial: Build and secure an ASP.NET Core web API with the Microsoft identity platform | Microsoft Learn (docs.azure.cn). Accessed May 24, 2025. [https://docs.azure.cn/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app](https://docs.azure.cn/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app)
66. Need help setting up isolation models for secure multi tennancy service | Microsoft Learn (Answers). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/answers/questions/2240069/need-help-setting-up-isolation-models-for-secure-m](https://learn.microsoft.com/en-us/answers/questions/2240069/need-help-setting-up-isolation-models-for-secure-m)
67. Announced at Build 2025: Foundry connection for Azure Cosmos ... | Microsoft Developer Blogs (CosmosDB). Accessed May 24, 2025. [https://devblogs.microsoft.com/cosmosdb/announced-at-build-2025-foundry-connection-for-azure-cosmos-db-global-secondary-index-full-text-search-and-more/](https://devblogs.microsoft.com/cosmosdb/announced-at-build-2025-foundry-connection-for-azure-cosmos-db-global-secondary-index-full-text-search-and-more/)
68. .NET Aspire overview | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
69. Tutorial: Add .NET Aspire to an existing .NET app | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app)
70. Local Azure provisioning - .NET Aspire | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/azure/local-provisioning](https://learn.microsoft.com/en-us/dotnet/aspire/azure/local-provisioning)
71. .NET Aspire Azure Cosmos DB integration | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-integration](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-integration)
72. aspire-samples/.../AspireShop.AppHost/Program.cs at main · dotnet/aspire-samples | GitHub. Accessed May 24, 2025. [https://github.com/dotnet/aspire-samples/blob/main/samples/AspireShop/AspireShop.AppHost/Program.cs](https://github.com/dotnet/aspire-samples/blob/main/samples/AspireShop/AspireShop.AppHost/Program.cs)
73. Unable to follow docs to implement Aspire specific CosmosDB package #2318 | GitHub (dotnet/aspire). Accessed May 24, 2025. [https://github.com/dotnet/aspire/discussions/2318](https://github.com/dotnet/aspire/discussions/2318)
74. .NET Aspire Azure integrations overview | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/azure/integrations-overview](https://learn.microsoft.com/en-us/dotnet/aspire/azure/integrations-overview)
75. .NET Aspire architecture overview | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/architecture/overview](https://learn.microsoft.com/en-us/dotnet/aspire/architecture/overview)
76. Deploying MCP Servers with Azure Container Apps | Stochastic Coder. Accessed May 24, 2025. [https://stochasticcoder.com/2025/04/29/deploying-mcp-servers-with-azure-container-apps/](https://stochasticcoder.com/2025/04/29/deploying-mcp-servers-with-azure-container-apps/)
77. Deploy .NET Aspire projects to Azure Container Apps | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment)
78. Configure Azure Container Apps environments - .NET Aspire | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/azure/configure-aca-environments](https://learn.microsoft.com/en-us/dotnet/aspire/azure/configure-aca-environments)
79. docs-aspire/docs/deployment/azure/aca-deployment-azd-in ... | GitHub (dotnet/docs-aspire). Accessed May 24, 2025. [https://github.com/dotnet/docs-aspire/blob/main/docs/deployment/azure/aca-deployment-azd-in-depth.md](https://github.com/dotnet/docs-aspire/blob/main/docs/deployment/azure/aca-deployment-azd-in-depth.md)
80. docs-aspire/docs/whats-new/dotnet-aspire-9.2.md at main | GitHub (dotnet/docs-aspire). Accessed May 24, 2025. [https://github.com/dotnet/docs-aspire/blob/main/docs/whats-new/dotnet-aspire-9.2.md](https://github.com/dotnet/docs-aspire/blob/main/docs/whats-new/dotnet-aspire-9.2.md)
81. Authenticate Azure-hosted .NET apps to Azure resources using a system-assigned managed identity - .NET | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/system-assigned-managed-identity](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/system-assigned-managed-identity)
82. Azure App Configuration integration - .NET Aspire | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-configuration-integration](https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-configuration-integration)
83. .NET Aspire Azure Key Vault integration | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/dotnet/aspire/security/azure-security-key-vault-integration](https://learn.microsoft.com/en-us/dotnet/aspire/security/azure-security-key-vault-integration)
84. .NET Aspire Integrations (SQL Server Integration in Aspire Applications) | C# Corner. Accessed May 24, 2025. [https://www.c-sharpcorner.com/article/net-aspire-integrations-sql-server-integration-in-aspire-applications/](https://www.c-sharpcorner.com/article/net-aspire-integrations-sql-server-integration-in-aspire-applications/)
85. How to create Azure Bot service in a Private network and integrate with MS Teams application | Microsoft Learn (Answers). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo](https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo)
86. Is it possible to integrate Azure Bot with Teams when the public access is disabled ... | Microsoft Learn (Answers). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/answers/questions/2263616/is-it-possible-to-integrate-azure-bot-with-teams-w](https://learn.microsoft.com/en-us/answers/questions/2263616/is-it-possible-to-integrate-azure-bot-with-teams-w)
87. Baseline OpenAI End-to-End Chat Reference Architecture | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat)
88. Azure Networking architecture documentation | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/networking/fundamentals/architecture-guides](https://learn.microsoft.com/en-us/azure/networking/fundamentals/architecture-guides)
89. Configure network isolation - Bot Service | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0)
90. About network isolation in Azure AI Bot Service | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0)
91. Azure security baseline for Azure Bot Service | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline](https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline)
92. Delivering increased productivity for bot development and deployment | Microsoft Azure Blog. Accessed May 24, 2025. [https://azure.microsoft.com/en-us/blog/delivering-increased-productivity-for-bot-development-and-deployment/](https://azure.microsoft.com/en-us/blog/delivering-increased-productivity-for-bot-development-and-deployment/)
93. Conversational AI updates for July 2019 | Microsoft Azure Blog. Accessed May 24, 2025. [https://azure.microsoft.com/en-us/blog/conversational-ai-updates-for-july-2019/](https://azure.microsoft.com/en-us/blog/conversational-ai-updates-for-july-2019/)
94. Recommended way to network isolate with an Azure Bot Service | Microsoft Learn (Answers). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/answers/questions/2243086/recommended-way-to-network-isolate-with-an-azure-b](https://learn.microsoft.com/en-us/answers/questions/2243086/recommended-way-to-network-isolate-with-an-azure-b)
95. OpenAI compatibility | Gemini API | Google AI for Developers. Accessed May 24, 2025. [https://ai.google.dev/gemini-api/docs/openai](https://ai.google.dev/gemini-api/docs/openai)
96. Overview of Azure Firewall service tags | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/azure/firewall/service-tags](https://learn.microsoft.com/en-us/azure/firewall/service-tags)
97. Block access to LLM applications using keywords and FQDN ... | Fortinet Docs. Accessed May 24, 2025. [https://docs.fortinet.com/document/fortigate/7.6.2/administration-guide/116184/block-access-to-llm-applications-using-keywords-and-fqdn](https://docs.fortinet.com/document/fortigate/7.6.2/administration-guide/116184/block-access-to-llm-applications-using-keywords-and-fqdn)
98. Provisioning API Keys | Programmatic Control of OpenRouter API Keys. Accessed May 24, 2025. [https://openrouter.ai/docs/features/provisioning-api-keys](https://openrouter.ai/docs/features/provisioning-api-keys)
99. Use private endpoints in the classic Microsoft Purview governance portal | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/purview/data-gov-classic-private-link](https://learn.microsoft.com/en-us/purview/data-gov-classic-private-link)
100. Responsible AI Validation Checks for Declarative Agents | Microsoft Learn (M365 Copilot Extensibility). Accessed May 24, 2025. [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/rai-validation](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/rai-validation)
101. Deploy large language models responsibly with Azure AI | Microsoft Tech Community (Machine Learning Blog). Accessed May 24, 2025. [https://techcommunity.microsoft.com/blog/machinelearningblog/deploy-large-language-models-responsibly-with-azure-ai/3876792](https://techcommunity.microsoft.com/blog/machinelearningblog/deploy-large-language-models-responsibly-with-azure-ai/3876792)
102. Microsoft extends Zero Trust to secure the agentic workforce | Microsoft Security Blog. Accessed May 24, 2025. [https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/](https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/)
103. Data Protection Impact Assessment (DPIA) – Microsoft CoPilot 365 | ICO UK. Accessed May 24, 2025. [https://ico.org.uk/media2/ob4ncmpz/ic-359252-x5s0-copilot-dpia.pdf](https://ico.org.uk/media2/ob4ncmpz/ic-359252-x5s0-copilot-dpia.pdf)
104. AI Agents: Mastering Agentic RAG - Part 5 | Microsoft Community Hub. Accessed May 24, 2025. [https://techcommunity.microsoft.com/blog/educatordeveloperblog/ai-agents-mastering-agentic-rag---part-5/4396171](https://techcommunity.microsoft.com/blog/educatordeveloperblog/ai-agents-mastering-agentic-rag---part-5/4396171)
105. Commercial marketplace policies and terms - Partner Center | Microsoft Learn. Accessed May 24, 2025. [https://learn.microsoft.com/en-us/partner-center/marketplace-offers/policies-terms](https://learn.microsoft.com/en-us/partner-center/marketplace-offers/policies-terms)
106. Common Azure Marketplace Publishing Challenges Solved | WeTransact. Accessed May 24, 2025. [https://www.wetransact.io/blog/common-azure-marketplace-publishing-challenges-solved](https://www.wetransact.io/blog/common-azure-marketplace-publishing-challenges-solved)