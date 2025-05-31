---
title: "Foundations: Model Context Protocol (MCP) and Microsoft 365 Agents SDK for Nucleus Developers"
description: "A foundational primer on Model Context Protocol (MCP) and the Microsoft 365 Agents SDK, crucial for understanding the Nucleus project's architecture. Incorporates May 2025 Microsoft Build insights."
version: 1.2
date: 2025-05-28
see_also:
  - title: "Nucleus System Architecture: Comprehensive Guide"
    link: "./01_NUCLEUS_SYSTEM_ARCHITECTURE_COMPREHENSIVE_GUIDE.md"
  - title: "System Executive Summary"
    link: "../CoreNucleus/00_SYSTEM_EXECUTIVE_SUMMARY.md"
  - title: "Project Mandate"
    link: "../../ProjectExecutionPlan/00_REQUIREMENTS_PROJECT_MANDATE.md"
---

# Foundations: Model Context Protocol (MCP) and Microsoft 365 Agents SDK for Nucleus Developers

**Purpose:** This guide serves as the foundational learning resource for developers contributing to Project Nucleus, focusing on the Model Context Protocol (MCP) and the Microsoft 365 Agents SDK. It aims to provide a comprehensive understanding of these core technologies, which are pivotal to Nucleus's architecture. **This document incorporates insights from the latest Microsoft Build 2025 announcements and official SDK documentation (May 2025).**

---

### **Part 1: Model Context Protocol (MCP) Deep Dive**

This part provides a thorough introduction to the Model Context Protocol, its architecture, benefits, and fundamental implementation details relevant to .NET developers.

#### **1.1 Introduction: The Rise of Model Context Protocol (MCP)**

By May 2025, the Model Context Protocol (MCP) has rapidly transitioned from a promising initiative to the de facto standard for enabling Large Language Models (LLMs) to interact with external tools and data sources. This ascent was significantly accelerated by pivotal announcements from major technology industry leaders around May 19th and 20th, 2025 (notably Microsoft Build and Google I/O), underscoring a collective move towards standardized agentic AI frameworks. For developers engaged in building sophisticated agentic AI applications, particularly within the .NET ecosystem, a comprehensive understanding of MCP is no longer optional but essential.

The swift industry coalescence around MCP is indicative of a maturing AI ecosystem. Previously, integrating AI models with a diverse array of tools often involved bespoke, one-off solutions, leading to a combinatorial explosion of effort—commonly referred to as the "M x N integration problem," where M models and N tools necessitated M\*N unique integrations. MCP's introduction and widespread adoption represent a paradigm shift towards interoperability, aiming to simplify this to an M+N problem. This standardization is not merely a technical convenience; it is a foundational enabler for building more complex, reliable, and scalable AI systems. Consequently, for an open-source initiative like Nucleus OmniRAG, aligning with an industry-backed standard like MCP ensures future compatibility, grants access to a rapidly expanding ecosystem of MCP-compliant tools and servers (evidenced by resources like the "Top 124 MCP Servers & Clients" guide and the upcoming public, community-managed MCP server registry), and attracts a broader community of developers already familiar with the protocol.

#### **1.2 Understanding Model Context Protocol (MCP)**

##### **1.2.1 What is MCP? Core Concepts and Origins**

Model Context Protocol (MCP) is an open standard, initiated by Anthropic in late 2024, designed to standardize the communication pathways between AI applications (referred to as "hosts") and external tools, data sources, and systems (exposed via "servers"). Its fundamental purpose is to provide a universal, consistent mechanism for AI models, particularly LLMs, to access external context and invoke external functionalities. This allows AI systems to move beyond their pre-trained knowledge and interact with real-time, dynamic information and capabilities.

Anthropic aptly describes MCP as being "like a USB-C port for AI applications". This analogy highlights its role as a universal connector: just as USB-C allows various devices to connect using a single standard, MCP aims to enable any compliant AI model to "plug into" any compliant tool or data source without requiring custom-built adapters for each pairing. This vision is central to simplifying the development of context-aware and action-capable AI.

##### **1.2.2 The "Why": Need for a Standardized Protocol**

The primary driver for MCP's development was the escalating complexity and inefficiency of integrating AI models with a growing number of external tools and data sources. Without a common standard, developers faced the "M x N integration problem". This approach is not only resource-intensive but also leads to fragmented, brittle systems that are difficult to scale and evolve.

Previous approaches, such as bespoke API integrations for each tool or early, less standardized forms of function calling, suffered from this lack of interoperability. While function calling, as popularized by models like OpenAI's GPT series, was a step forward, it often still required model-specific or tool-specific adaptation and did not offer a universal discovery or interaction mechanism across different tool providers or AI model vendors. MCP was conceived to address these limitations by providing a common language and a defined set of interaction patterns.

##### **1.2.3 Key Benefits of MCP**

The adoption of MCP offers several significant advantages for the development and deployment of agentic AI applications:

*   **Improved AI Performance and Relevance:** By enabling AI models to seamlessly access relevant, real-time, and specific contextual information from diverse sources, MCP allows them to generate responses that are more accurate, nuanced, and useful. For instance, an AI coding assistant with MCP access to a project's specific codebase and documentation can produce more functional code with fewer errors.
*   **Interoperability Across Systems:** As an open standard, MCP is not tied to any single AI vendor or data platform. This fosters a rich ecosystem where tools and models from different providers can interoperate seamlessly. An MCP-compliant data source can serve context to any MCP-enabled AI client, and vice-versa, future-proofing integrations and allowing developers to switch models or platforms without overhauling their entire context pipeline.
*   **Development Efficiency and Reusability:** MCP allows developers to build integrations against a single, standard protocol. This means integration logic can be developed once and reused across multiple projects and AI models. Furthermore, the growing availability of pre-built MCP servers for popular services (e.g., GitHub, Slack, databases) and the upcoming **public, community-managed MCP server registry** significantly reduces the need for custom integration code, accelerating development cycles.
*   **Modularity and Scalability:** The protocol encourages a modular architecture by decoupling AI models from the data sources and tools they use. This separation allows each component—the AI application, the MCP client, the MCP server, and the external tool—to be developed, scaled, and upgraded independently. Adding a new data source simply involves deploying a new MCP server for it, without altering the core AI application logic. This modularity is also key to building composable AI agents, where different capabilities (exposed via MCP servers) can be combined like building blocks to create sophisticated workflows.

The architecture of MCP inherently promotes a clear "separation of concerns." The AI model can focus on higher-level reasoning, while MCP servers encapsulate the specific details of interacting with particular tools or data sources. MCP's explicit distinction between "Resources" (application-controlled data sources) and "Tools" (model-controlled actions) is particularly salient for applications employing Retrieval Augmented Generation (RAG) patterns. "Resources" allow the application to proactively furnish the LLM with relevant contextual data, which is the cornerstone of RAG.

#### **1.3 Deep Dive: MCP Architecture and Technical Specifications**

Understanding the architecture and technical underpinnings of MCP is crucial for .NET developers.

##### **1.3.1 The MCP Client-Server Architecture**

MCP operates on a client-server model, which involves several distinct components working in concert:

*   **Hosts (AI Applications):** These are the applications that the end-user interacts with. Examples include AI assistants like Claude Desktop, Integrated Development Environments (IDEs) such as Visual Studio Code with AI extensions, or custom agentic systems like Nucleus. The Host is responsible for managing one or more MCP Clients and often handles user interface, overall workflow orchestration, and the integration of the LLM's reasoning capabilities. It also plays a crucial role in security, managing permissions and user consent for accessing tools and resources.
*   **MCP Clients:** Residing within the Host application, an MCP Client acts as an intermediary that manages a dedicated, one-to-one connection with a specific MCP Server. Its responsibilities include initiating the connection, discovering the capabilities (Tools, Resources, Prompts) offered by the server, formatting and forwarding requests from the Host (or the LLM via the Host) to the server, and receiving and processing responses or errors from the server.
*   **MCP Servers:** These are external programs or lightweight services that act as wrappers or bridges to specific external functionalities. They expose Tools, Resources, and Prompts to MCP Clients via a standardized API defined by the MCP specification. An MCP Server translates generic MCP requests into the specific commands or API calls required by the underlying external system (e.g., querying a database, calling a SaaS API, reading from a local file system). It also handles authentication with the external service, data transformation, and error handling.
*   **External Service/Data Source:** This is the actual backend system, application, API, database, or file system that provides the data or performs the actions requested through the MCP Server.

##### **1.3.2 Communication Flow**

The interaction between MCP Clients and Servers follows a well-defined lifecycle and communication flow:

1.  **Initialization & Handshake:** When a Host application starts, it may instantiate MCP Clients. The MCP Client initiates a connection with its corresponding MCP Server. During this phase, client and server exchange information about their supported protocol versions and capabilities.
2.  **Discovery:** Once connected, the MCP Client can request a list of capabilities (Tools, Resources, and Prompts) that the MCP Server offers. The server responds with a machine-readable description, often an "action schema" or manifest.
3.  **Invocation:** When the AI model decides to use a specific Tool or access a Resource, the MCP Client sends a standardized request (typically a JSON structure) to the MCP Server. The Server receives, validates, translates, and executes.
4.  **Result:** After the external service processes the request, the MCP Server formats the outcome into a standardized MCP response and sends it back to the MCP Client.
5.  **Chaining:** MCP supports multi-step tasks by allowing an AI to execute multiple tool calls sequentially or in parallel, potentially orchestrating interactions across several different MCP Servers. The AI Host maintains overall context.

##### **1.3.3 Core Primitives: Tools, Resources, Prompts**

MCP defines three fundamental types of capabilities, or "primitives," that a server can expose:

*   **Tools (Model-controlled):** These are functions or actions that the LLM can decide to invoke to perform specific operations or gather information (e.g., calling a weather API, sending an email, querying a database). This is analogous to "function calling."
*   **Resources (Application-controlled):** These represent data sources that an LLM can access to obtain contextual information. Accessing Resources is generally considered a read-only operation with no side effects (e.g., specific project files, user profile information).
*   **Prompts (User-controlled):** These are pre-defined templates or instructions that guide the LLM in how to use certain Tools or Resources in an optimal or specific way (e.g., "Summarize this document using the 'technical brief' style").

This tripartite structure offers a sophisticated framework for agent interaction that surpasses simple function calling.

##### **1.3.4 Communication Protocols & Transport Mechanisms**

MCP relies on established, open standards for its communication layer:

*   **JSON-RPC 2.0:** The foundational messaging protocol for MCP is JSON-RPC 2.0. This is a lightweight remote procedure call protocol that uses JSON for data encoding.
    *   **Request Structure:** Typically includes `jsonrpc` (version), `method` (name of method), `params` (parameters), and `id` (unique client identifier).
    *   **Response Structure:** Includes `jsonrpc`, the same `id` as the request, and either `result` (successful outcome) or `error` (error details).
*   **Transport Mechanisms:** MCP supports multiple transport mechanisms:
    *   **stdio (Standard Input/Output):** Primarily used when the MCP Client and Server are on the same machine (e.g., IDE communicating with a local tool server).
    *   **HTTP with SSE (Server-Sent Events):** Suited for client-remote MCP Server communication. The server uses SSE to push messages over a persistent HTTP connection. Client-to-server requests often use HTTP POST. While WebSockets have been mentioned, the core specification primarily emphasizes stdio and HTTP/SSE.

#### **1.4 MCP for the .NET Developer: Implementation Fundamentals**

With robust backing and dedicated tooling, .NET developers are well-equipped for MCP.

##### **1.4.1 The Official MCP C\# SDK: ModelContextProtocol NuGet Package**

The cornerstone for .NET developers is the official C\# SDK, distributed via the `ModelContextProtocol` NuGet package. Developed by Microsoft in collaboration with Anthropic, this open-source SDK is the standard toolset for building **both MCP clients (which an M365 Agent could utilize to consume external tools) and MCP servers (which would typically be separate backend Nucleus services exposing specific capabilities)** in C\#. It often builds upon or supersedes earlier community efforts, incorporating best practices and deep integration with the .NET ecosystem.

To install using the .NET CLI: `dotnet add package ModelContextProtocol --prerelease`. **(Note: Always check for the latest stable or recommended version).**

The SDK provides core functionalities for building clients, servers, and AI helper libraries that facilitate MCP integration with LLMs, often via abstractions from `Microsoft.Extensions.AI`.

##### **1.4.2 Setting up MCP Clients in .NET (Basics)**

Creating an MCP client typically involves `McpClientFactory.CreateAsync`. This factory requires transport options, such as `StdioClientTransportOptions` or an HTTP-based transport using SSE.

Once an `IMcpClient` instance is connected:
*   **Tool Discovery:** `await client.ListToolsAsync()` returns `McpClientTool` objects with metadata.
*   **Tool Invocation:** `await client.CallToolAsync("tool_name", argumentsDictionary, cancellationToken)` executes a tool.
*   **Integration with Microsoft.Extensions.AI:** `McpClientTool` instances often inherit from or can be adapted to `AIFunction`, a type recognized by `IChatClient` from `Microsoft.Extensions.AI`, allowing seamless integration with LLM function calling.

##### **1.4.3 Building MCP Servers in C\# (Basics)**

The C\# SDK simplifies MCP server creation, typically leveraging `Microsoft.Extensions.Hosting`.
*   **Server Setup:** Use `Host.CreateApplicationBuilder()` and register MCP services with `builder.Services.AddMcpServer()` (or a similar extension method provided by the SDK).
*   **Transport Configuration:** Specify transport, e.g., `.WithStdioServerTransport()` or SSE transport for remote servers.
*   **Defining Primitives using Attributes:**
    *   **Tools:** Classes with tool methods marked `[McpServerToolType]`. Methods decorated `[McpTool]` (or similar, e.g., `[ToolMethod]`) providing name and description. Parameters can have `[McpParameterDescription]` (or `[ToolParameter]`). Tool methods can use dependency injection. Server can auto-discover using `.WithToolsFromAssembly()` or similar.
    *   **Resources & Prompts:** Similar attribute-based mechanisms (`[McpResource]`, `[McpPrompt]`) are available.

##### **1.4.4 Comparison: MCP Tooling vs. Traditional OpenAI Function Calling in .NET**

While OpenAI's function calling was pioneering, MCP offers a more comprehensive and standardized solution.

| Aspect                      | Model Context Protocol (MCP)                                                                          | OpenAI Function Calling (Traditional)                                                              |
| :-------------------------- | :---------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------- |
| **Standardization**         | Open, industry-wide standard; promotes interoperability across models and tool providers.      | Primarily tied to OpenAI models; less standardized across the industry.                         |
| **Tool Discovery**          | Built-in dynamic discovery of server capabilities (tools, resources, prompts) via schemas.    | Functions must be pre-defined and passed to the model with each call.                           |
| **Multi-Tool Orchestration** | Designed to support complex workflows involving multiple tools and servers.                   | Orchestration of multi-step function chains often requires custom client-side logic.           |
| **Primitives**              | Richer model: Tools (actions), Resources (data), Prompts (guidance).                          | Primarily focused on "functions" (actions).                                                     |
| **Complexity**              | Higher initial setup for servers, but simplifies M x N integration problem.                   | Simpler for single-tool, single-model scenarios.                                           |
| **Ecosystem**               | Rapidly growing ecosystem of servers and clients from diverse vendors.                          | Primarily tools and libraries focused on OpenAI models.                                         |
| **.NET Support**            | Official Microsoft C\# SDK, strong integration with .NET ecosystem (Aspire, SK, `Microsoft.Extensions.AI`).              | Well-supported by community libraries for .NET.                                                 |

The official Microsoft C\# SDK for MCP, integrated with `Microsoft.Extensions.AI`, streamlines development by aligning MCP with familiar .NET AI patterns.

#### **1.5 Securing MCP-Powered Agentic Applications**

As AI agents become more autonomous, security is paramount.

##### **1.5.1 Microsoft's Security Framework for MCP in Windows**

Microsoft has outlined a comprehensive security framework for MCP's integration into Windows 11:
*   **Key Principles:** User control (MCP server access off by default, requires explicit consent), full auditability and transparency of agent actions, and the principle of least privilege (declarative capabilities and isolation for MCP servers).
*   **Proxy-Mediated Communication:** Interactions may be routed through a trusted Windows proxy for policy enforcement and auditing.
*   **Tool-Level Authorization:** Users may need to explicitly approve client-tool pairings, potentially with per-resource granularity.
*   **Central Server Registry with Security Criteria:** The Windows MCP Registry will only list servers meeting defined baseline security criteria.
*   **Server Security Requirements:** Mandatory code signing, static (non-changeable at runtime) tool definitions, security testing of exposed interfaces, mandatory package identity, and declaration of required privileges. This framework aims to address threats like credential leakage, tool poisoning, Cross-Prompt Injection Attacks (XPIA), and command injection.

##### **1.5.2 Server-Side Security Best Practices for .NET MCP Servers**

When developing custom MCP servers in .NET:
*   **Input Validation:** Rigorously validate all parameters received by tool methods.
*   **Proper Error Handling:** Implement robust error handling without leaking sensitive internal details.
*   **Secure Logging:** Log relevant activities, ensuring sensitive data is not logged in plain text.
*   **Authentication and Authorization for Sensitive Tools:** Implement additional authN/authZ layers beyond protocol-level security.
*   **Principle of Least Privilege for Server Processes:** The MCP server process should operate with minimum necessary permissions.
*   **Dependency Management:** Regularly scan and update server dependencies.
*   **Secure Configuration Management:** Store secrets like API keys securely (e.g., Azure Key Vault, .NET secret management tools).

##### **1.5.3 Authentication and Authorization: Leveraging Entra ID with MCP**

A significant development is the new Identity and Authorization Specification for MCP, allowing MCP-connected applications to use robust identity providers like Microsoft Entra ID.
*   **Azure API Management (APIM) as an OAuth 2.0 Gateway:** For remote MCP servers, Azure APIM can act as a secure OAuth 2.0 gateway, fronting the MCP server and integrating with Microsoft Entra ID for authentication and authorization. The flow involves APIM redirecting to Entra ID, user authentication, token issuance, and APIM validating tokens before forwarding requests.
*   **Azure MCP Server:** The official Azure MCP Server has built-in support for Entra ID authentication via the Azure Identity library.

The standardization of identity and authorization paves the way for "identity-aware" AI agents operating securely within enterprise ecosystems.

#### **1.6 The Future of MCP and Agentic AI**

The rapid adoption and strong industry backing of MCP suggest a trajectory towards increasingly sophisticated AI agents.

##### **1.6.1 Expected Evolution of the MCP Standard and Ecosystem**

*   **Growth in Servers and Clients:** The number and variety are projected to grow exponentially, facilitated by the **public, community-managed MCP server registry.**
*   **Protocol Enhancements (vNext):** Future versions may address more complex requirements like sophisticated multi-tool chaining, finer-grained security, standardized eventing, or more expressive schemas.
*   **Advanced Capabilities:** Possibilities include "hardware brokers" for IoT devices and "formal verification" of tool actions for reliability and trust.

##### **1.6.2 Preparing for a Future Dominated by Interoperable AI Agents**

For developers and organizations:
*   **Embracing Standards:** Actively learn and adopt open standards like MCP.
*   **Focus on Modularity and Security:** Design AI systems and tools with these as core principles.
*   **Exposing Capabilities via MCP:** Consider how internal data and services can be securely exposed via MCP servers.
*   **Developing Higher-Order Reasoning:** Invest in advanced AI reasoning, planning, and learning capabilities within agent Hosts.
The "public, community-managed MCP server registry" is poised to become vital, analogous to package managers but for AI capabilities.

### **Part 2: Microsoft 365 Agents SDK Deep Dive**

This part provides an in-depth analysis of the Microsoft 365 Agents SDK, its capabilities, and how it differs from the preceding Bot Framework.

#### **2.1 Understanding the Microsoft 365 Agents SDK**

The Microsoft 365 Agents SDK is an integrated development environment specifically engineered for creating sophisticated AI agents. It provides developers with the tools and libraries necessary to build agents that can be deployed across a multitude of channels, including Microsoft 365 Copilot, Microsoft Teams, and custom web applications, while managing the required communication.

##### **2.1.1 Purpose and Vision**

Microsoft's vision for the M365 Agents SDK is to provide a flexible and powerful platform for developers to build "custom engine agents". These agents offer full control over orchestration, AI models, and data integrations, enabling businesses to create advanced, tailored workflows. The SDK is designed to empower developers to create agents capable of performing complex tasks, reasoning over data, and collaborating with other agents and systems. A core principle is developer choice; it is intentionally "agnostic regarding the AI you choose", allowing integration of agents from any provider or technology stack.

##### **2.1.2 Core Features and Capabilities**

*   **Rapid Agent Containerization:** Allows for the quick construction of an "agent container" complete with state management, storage capabilities, and the ability to manage activities and events, deployable across channels like M365 Copilot or Teams.
*   **AI and Technology Stack Agnosticism:** Supports implementation using any AI services, models, or orchestration tools (e.g., Azure AI Foundry, OpenAI Agents, Semantic Kernel, LangChain).
*   **Client-Specific Customization:** Agents can be tailored to align with specific behaviors and capabilities of different client channels.
*   **Multi-Channel Deployment:** Agents built with the SDK can be surfaced across M365 Copilot, Teams, web applications, and third-party platforms.
*   **Integration with Orchestration Frameworks:** Seamlessly integrates with popular orchestration frameworks like Semantic Kernel and LangChain.
*   **Enterprise-Grade Development:** Positioned as a comprehensive framework for building enterprise-grade agents, facilitating integration of components from Azure AI Foundry SDK, Semantic Kernel, and other vendors.

##### **2.1.3 Architectural Overview: The SDK and Azure Bot Service**

The Microsoft 365 Agents SDK is an evolution and abstraction layer built upon Microsoft's existing bot infrastructure. The SDK **utilizes the Azure Bot Service** for channel management and provides **scaffolding to handle the required communication.** The Azure Bot Service defines a REST API and an activity protocol for interactions; the M365 Agents SDK builds upon this REST API, providing a higher-level abstraction that simplifies development by handling much of the underlying Bot Service REST API and activity protocol complexities. Core concepts from the Bot Framework, such as "activities" (representing interactions) and "turns" (units of work), persist but are framed within a richer agent context. Manual deployment to Azure and registration with Azure Bot Service remains a viable option.

##### **2.1.4 Development Environment and Tooling**

*   **Supported Languages:** C\# (using the .NET 8.0 SDK), JavaScript (using Node.js version 18 and above), and Python (versions 3.9 to 3.11, **with General Availability planned for June 2025**).
*   **Microsoft 365 Agents Toolkit:** An evolution of the Teams Toolkit, available for Visual Studio and VS Code. It provides project templates (e.g., "Echo Agent," "Weather Agent"), debugging capabilities (local testing in "Microsoft 365 Agents Playground," direct debugging in Teams or M365 Copilot), and streamlined deployment workflows to Azure. The toolkit and SDK achieved General Availability (GA) at Microsoft Build 2025.
*   **Getting Started:** Developers typically clone official Agents GitHub repositories containing SDK source libraries and samples.

#### **2.2 The Transition: From Bot Framework to the Microsoft 365 Agents SDK**

The M365 Agents SDK marks a paradigm shift from traditional Bot Framework to a more powerful, AI-driven, agent-centric approach.

##### **2.2.1 Fundamental Differences and the New Agent-Centric Paradigm**

The M365 Agents SDK is the "evolution of the Azure Bot Framework SDK". While Bot Framework focused on "conversational AI around topics, dialogs, and messages," the M365 Agents SDK is for "modern agent development", characterized by:
*   Generative AI (GAI) Driven Functionality.
*   Enterprise Knowledge Grounding.
*   Orchestration of Actions.
The term "agents" suggests more proactive, intelligent entities capable of complex reasoning and autonomous task execution compared to "bots". **The SDK is designed to be un-opinionated about the AI you use. You can implement agentic patterns without being locked into a tech stack.**

##### **2.2.2 Deprecated Bot Framework Components and Key Migration Insights for .NET**

**This section has been significantly updated based on the M365 Agents SDK Overview and Migration Guidance documentation.**

Several Bot Framework features are not being brought forward or are no longer directly relevant in the new agent paradigm. It is crucial for developers migrating from Bot Framework to understand these changes.

**Unsupported and Deprecated Bot Framework Functionalities (explicitly listed from SDK documentation):**
*   Adaptive Dialogs
*   AdaptiveExpressions
*   Composer Artifacts (Bot Framework Composer)
*   Previous Generation AI Tooling (LUIS, QnA Maker, and associated SDK components like `Microsoft.Bot.Builder.AI.Luis` and `Microsoft.Bot.Builder.AI.QnA`)
*   Language Understanding (`Microsoft.Bot.Builder.Parsers.LU`)
*   Language Generation (LG) tooling and `Microsoft.Bot.Builder.LanguageGeneration`
*   TemplateManager
*   ASP.Net WebAPI (the older `Microsoft.Bot.Builder.Integration.AspNet.WebApi` integration)
*   Application Insights (as previously integrated with `Microsoft.Bot.Builder.Integration.ApplicationInsights.Core`)
*   Streaming Connections (`Microsoft.Bot.Streaming`)
*   QueueStorage (as a transcript logger `Microsoft.Bot.Builder.Azure.Queues`)
*   Inspection (middleware for inspecting activity and state)
*   `BotFrameworkAdapter` (the primary adapter in older Bot Framework versions)
*   Deprecated Activities (e.g., `ActivityTypes.InvokeResponse`, `ActivityTypes.InstallationUpdate` with `action = "add-bot"`, `ActivityTypes.ContactRelationUpdate`, `ActivityTypes.Typing`)
*   Generators (Yeoman generators for Bot Framework)
*   CLI (`bf` command-line interface)

**Key .NET SDK Code Changes (Migration from Bot Framework SDK to M365 Agents SDK):**

*   **Token Authentication (Startup):**
    *   Authentication setup **must be configured in the host application** (e.g., ASP.NET Core web app).
    *   Manual `AppCredentials` creation (e.g., `new MicrosoftAppCredentials(appId, appPassword)`) **no longer exists**.
    *   This is replaced by the `IAccessTokenProvider` interface and implementations like `Microsoft.Agents.Authentication.Msal` (or equivalent from `Microsoft.Bot.Connector.Authentication` if still applicable for certain channel service interactions) for more robust and secure token handling.
    *   `ConfigurationBotFrameworkAuthentication` (from `Microsoft.Bot.Builder.Integration.Configuration`) is replaced by `RestChannelServiceClientFactory` (from `Microsoft.Agents.BotFramework`) or direct usage of `ChannelServiceClient` with appropriate authentication.
*   **Serialization:**
    *   The Bot Framework SDK primarily used **Newtonsoft.Json** for serialization.
    *   The M365 Agents SDK transitions to using **`System.Text.Json`** for improved performance and consistency with modern .NET.
*   **Adapter:**
    *   `BotFrameworkAdapter`: This core adapter from the Bot Framework SDK **is replaced and removed from the M365 Agents SDK**.
    *   While `CloudAdapter` was its successor in later versions of the Bot Framework SDK, the M365 Agents SDK typically uses an implementation of `IAgentHttpAdapter` (e.g., `Microsoft.Agents.Hosting.AspNetCore.AgentHttpAdapter`) when mapping the agent endpoint (e.g., `app.MapPost("/api/messages", ...)`). The `CloudAdapter` might still be relevant for direct interactions with the Bot Connector service if not using the full agent hosting model.
*   **Interface/Type Name Changes:**
    *   `Microsoft.Bot.Connector.IAttachments.GetAttachmentInfo` is renamed to `GetAttachmentInfoAsync`.
    *   `Microsoft.Bot.Builder.Integration.AspNet.Core.IBotFrameworkHttpAdapter` is conceptually replaced by `Microsoft.Agents.Hosting.AspNetCore.IAgentHttpAdapter` within the agent hosting model. The older `Microsoft.Bot.Builder.IBotHttpAdapter` might still exist for lower-level channel interactions.
    *   `Microsoft.Bot.Builder.BotAdapter` is renamed to `Microsoft.Bot.Builder.ChannelAdapter`.
    *   `Microsoft.Bot.Builder.CloudAdapterBase` is renamed to `Microsoft.Bot.Builder.ChannelServiceAdapterBase`.
*   **State Management:**
    *   Accessing connector client: `TurnState.Get<IConnectorClient>()` becomes `turnContext.Services.Get<IConnectorClient>()` (or `turnState.Services.Get<IConnectorClient>()`).
    *   Accessing user token client: `TurnState.Get<IUserTokenClient>()` becomes `turnContext.Services.Get<IUserTokenClient>()` (or `turnState.Services.Get<IUserTokenClient>()`).
    *   The general `TurnState` property bag concept from Bot Framework is evolved. While `ITurnState` is the type of the `turnState` parameter in activity handlers, direct manipulation might involve `turnState.StackState`.
    *   Adding to turn state: `turnState.Add<T>(key, value)` (Bot Framework) is conceptually replaced by `turnState.StackState.Set<T>(key, value)` or direct use of `ITurnContext.TurnState` for simpler scenarios.
*   **Application Classes:**
    *   `Microsoft.Bot.Builder.Application<TState>` (Bot Framework) is replaced by `Microsoft.Agents.AgentApplication`.
    *   `Microsoft.Bot.Builder.ApplicationOptions<TState>` (Bot Framework) is replaced by `Microsoft.Agents.AgentApplicationOptions`.
*   **Namespaces:** Significant changes require updating `using` statements:
    *   `using Microsoft.Bot.Builder;` generally becomes `using Microsoft.Agents.BotBuilder;` (for core agent types) or remains `using Microsoft.Bot.Builder;` (for foundational types like `ITurnContext`, `IStorage`).
    *   `using Microsoft.Bot.Schema;` is replaced by `using Microsoft.Agents.Core.Models;` (for agent-specific models) and potentially `using Microsoft.Bot.Schema;` (for activity types and basic schema elements if still directly used). `Microsoft.Agents.Core.Serialization;` might also be relevant.
    *   `using Microsoft.Bot.Builder.Integration.AspNet.Core;` is replaced by `using Microsoft.Agents.Hosting.AspNetCore;`

These changes reflect a move towards a more modern, ASP.NET Core-aligned, and extensible architecture in the M365 Agents SDK.

#### **2.3 Developing with the M365 Agents SDK for .NET: Fundamentals**

##### **2.3.1 Agent Development Lifecycle with M365 Agents Toolkit (.NET)**

*   **Project Scaffolding:** In Visual Studio, select "Microsoft 365 Agents" project type. Templates like "Weather Agent" (with Semantic Kernel, Azure AI Foundry/OpenAI integration) or "Empty Agent" are available.
*   **Core Agent Implementation (C\#):**
    *   **`Program.cs` Structure (based on SDK Samples):**
        ```csharp
        // using Microsoft.Agents.BotBuilder; // For AgentApplication, AgentApplicationOptions
        // using Microsoft.Agents.Hosting.AspNetCore; // For AddAgent, AddAgentApplicationOptions, IAgentHttpAdapter
        // using Microsoft.Bot.Builder; // For IStorage, MemoryStorage, ITurnContext
        // using Microsoft.Bot.Schema; // For ActivityTypes
        // using Microsoft.Extensions.DependencyInjection; // For AddSingleton, AddHttpClient
        // using Microsoft.Extensions.Logging; // For AddConsole

        // WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // builder.Services.AddHttpClient(); // Common practice for making HTTP calls
        // builder.Logging.AddConsole();    // Common practice for logging

        // // Add agent application options from configuration (e.g., appsettings.json)
        // builder.AddAgentApplicationOptions(); 

        // // Register the agent type
        // builder.AddAgent<MyAgent>(); 

        // // Register storage (MemoryStorage for development, persistent storage for production)
        // builder.Services.AddSingleton<IStorage, MemoryStorage>();

        // WebApplication app = builder.Build();

        // // Map the agent messaging endpoint
        // app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
        // {
        //     await adapter.ProcessAsync(request, response, agent, cancellationToken);
        // });

        // // Configure listening URL (optional, often from launchSettings.json or config)
        // // app.Urls.Add($"http://localhost:3978"); 

        // app.Run();
        ```
        *(Source: Samples - `empty-agent/dotnet/Program.cs`, `copilotstudio-skill/dotnet/Program.cs`)*
    *   **Agent Class (`MyAgent.cs`) Structure (based on SDK Samples):**
        ```csharp
        // using Microsoft.Agents.BotBuilder; // For AgentApplication, AgentApplicationOptions
        // using Microsoft.Bot.Builder; // For ITurnContext, ITurnState, MessageFactory
        // using Microsoft.Bot.Schema; // For ActivityTypes, Activity
        // using System.Threading; // For CancellationToken
        // using System.Threading.Tasks; // For Task

        // public class MyAgent : AgentApplication
        // {
        //     public MyAgent(AgentApplicationOptions options) : base(options)
        //     {
        //         // Define activity handlers
        //         OnActivity(ActivityTypes.Message, async (ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken) =>
        //         {
        //             // Echo back the received message
        //             await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
        //         });

        //         OnActivity(ActivityTypes.ConversationUpdate, async (ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken) =>
        //         {
        //             if (turnContext.Activity.MembersAdded != null)
        //             {
        //                 foreach (var member in turnContext.Activity.MembersAdded)
        //                 {
        //                     if (member.Id != turnContext.Activity.Recipient.Id)
        //                     {
        //                         await turnContext.SendActivityAsync(MessageFactory.Text($"Hello, {member.Name}! Welcome to the agent."), cancellationToken);
        //                     }
        //                 }
        //             }
        //         });
        //     }
        // }
        ```
        *(Source: Samples - `empty-agent/dotnet/MyAgent.cs`, SDK Documentation OCR p4, p12, p37)*
*   **Testing and Debugging:** The M365 Agents Toolkit provides the "Microsoft 365 Agents Playground" for local testing. Direct debugging in Microsoft Teams or M365 Copilot is also supported.
*   **Deployment:** Manual deployment to Azure involves Azure Bot Service app registration (App ID matching Azure Bot Service record) and publishing agent code (e.g., to Azure Web App) [18, referring to its section 2.1; 31]. The messaging endpoint in Azure Bot Service config must point to the deployed agent's API (e.g., `https://{yourwebsite}/api/messages`). For Teams/M365 Copilot deployment, a manifest .zip package (with `manifest.json`, icons) is side-loaded, referencing the Bot ID.

##### **2.3.2 Advanced State Management: ITurnState, StackState, and IStorage**

Managing conversational context is crucial. The M365 Agents SDK provides mechanisms for state and storage. **Agents created with the Agents SDK are stateless by default.** State is supported as an optional parameter when the agent is built and includes concepts like Private State, User State, and Conversation State. State requires a configured storage mechanism. The SDK supports Memory storage (for development), Azure Blob storage, Azure Cosmos DB, or custom storage implementations.
*   **IStorage:** For basic scenarios/testing, `MemoryStorage` can be registered: `builder.Services.AddSingleton<IStorage, MemoryStorage>();`. Production uses persistent stores (e.g., Azure Blob Storage, Cosmos DB) by implementing `IStorage`.
*   **AgentState:** The SDK includes an `AgentState` class, typically constructed with an `IStorage` implementation and a `stateName` string [23, referring to its section 5.2; 32]. It persists state properties and caches state within turns. **The `AgentApplicationOptions` can be configured with `Storage` and `ConversationState`, `UserState`, `TempState` properties.**
*   **ITurnState and StackState:** Migration guidance indicates a shift.
    *   The `turnState` parameter (type `ITurnState`) is passed to `OnActivity` handlers.
    *   Bot Framework's `TurnState` property bag is conceptually replaced by `StackState`. The `ITurnState` interface provides access to `ConversationState`, `UserState`, and `TempState` (which includes `Input`, `Output`, `History`).
    *   Accessing services (e.g., via `turnContext.Services.Get<T>()`).
    *   `StackState.Add()` is replaced by `StackState.Set()` for managing values within turn-specific state. **More commonly, state is accessed via `turnState.ConversationState.Get<T>()` or `turnState.UserState.Set<T>()`.**

#### **2.4 Security and Governance for M365 Agents**

As AI agents become more autonomous, security and governance are paramount.

##### **2.4.1 Identity and Access Management with Microsoft Entra Agent ID**

The public preview of **Microsoft Entra Agent ID** is a fundamental shift.
*   **Centralized Agent Identity:** Agents created with Azure AI Foundry and Microsoft Copilot Studio are automatically assigned unique, first-class identities in Microsoft Entra ID. Support for M365 Agents SDK-built agents (via M365 Copilot) and third-party solutions is planned. **The `teams-manifest.json` and `m365copilot-manifest.json` files in SDK samples often use a placeholder like `${{AAD_APP_CLIENT_ID}}` for `bots.botId` (in Teams manifest) or `webApplicationInfo.id` (in M365 Copilot manifest). This `AAD_APP_CLIENT_ID` refers to the Application (client) ID of the Azure AD App Registration for the bot itself, which aligns with the concept of the Entra Agent ID.**
*   **Visibility and Control:** Administrators can view and manage these agent identities (new "Agent ID (Preview)" application type) in the Microsoft Entra admin center.
*   **Least Privilege and Just-In-Time (JIT) Access:** Entra Agent ID promotes a least-privileged access model, with agents requesting JIT, scoped tokens for only the resources they need.
*   **Simplified Enterprise Onboarding:** Treats agents as standard Entra ID identities, streamlining security reviews and leveraging existing tools for discovery, approval, auditing, and lifecycle management.

##### **2.4.2 Data Security and Compliance using Microsoft Purview**

Microsoft Purview's data security and compliance capabilities are extended to the AI agent landscape.
*   **Extended Controls:** Purview controls can be applied to custom AI apps via a new Purview SDK, and natively for agents in Azure AI Foundry and Copilot Studio.
*   **Governance for AI Agents:** Enforce data classification, sensitivity labeling, data loss prevention (DLP), and other compliance policies on data AI agents access, process, and generate.

##### **2.4.3 Implementing Zero Trust for the Agentic Workforce**

Microsoft extends its **Zero Trust security model** ("never trust, always verify," "assume breach," "use least privileged access") to the "agentic workforce".
*   **Identity as the New Perimeter:** Entra Agent ID plays a pivotal role.
*   **Holistic Security Integration:** Involves embedding identity (Entra), security (Microsoft Defender), and governance (Purview) into agent-building platforms and operational lifecycles.

### **Part 3: The Unified Ecosystem - Key Announcements and Synergies**

This part consolidates key industry announcements, primarily from Microsoft Build 2025, and discusses the synergistic relationship between MCP, the M365 Agents SDK, and other Microsoft AI technologies.

#### **3.1 Pivotal Announcements (Microsoft Build 2025 & Context)**

May 2025, particularly around Microsoft Build (May 19-21), marked a watershed moment for MCP and the M365 agent ecosystem. **Ensure all General Availability (GA) dates and preview statuses mentioned reflect the latest information from primary sources like the OCR'd SDK documentation (e.g., Python SDK for M365 Agents is planned for GA in June 2025 - Source: OCR p9).**

##### **3.1.1 MCP Becomes Foundational (Microsoft Build 2025)**

*   **Windows 11 Native MCP Support:** MCP embraced as a foundational layer for secure agentic computing in Windows 11, providing a standardized framework for AI agents to connect with native Windows capabilities. Features include an MCP Registry and security principles like user control, auditability, and least privilege.
*   **Broad MCP Adoption Across Microsoft's AI Stack:** Extensive support declared across GitHub, Copilot Studio (MCP connections for enterprise knowledge systems reached General Availability), Dynamics 365, Azure, Azure AI Foundry, Semantic Kernel, and Foundry Agents. An Azure MCP Server enables interaction with Azure resources.
*   **New MCP Identity & Authorization Specification:** In collaboration with Anthropic and the MCP Steering Committee, Microsoft introduced an updated specification allowing MCP-connected applications to securely connect to servers using Microsoft Entra ID or other trusted logins.
*   **Public, Community-Managed MCP Server Registry:** Microsoft announced contributions towards a service for discovering and managing MCP server entries.
*   **Microsoft & GitHub Join MCP Steering Committee:** Solidifying commitment to shape MCP's future.

##### **3.1.2 Google I/O 2025 MCP Support**

Google also signaled its support for MCP at its I/O 2025 conference, announcing integration into the Gemini API and SDK to facilitate the use of open-source tools with Google's AI models.

##### **3.1.3 Agent Store & New Microsoft-Built Agents (Microsoft Build 2025)**

*   **General Availability (GA) of the Agent Store** within Microsoft 365 Copilot Chat. This centralized marketplace allows users to find, pin, and utilize agents from Microsoft, partners, and their own company.
*   New prebuilt agents (GA in June 2025): **Employee Self-Service Agent** and **Skills Agent**.

##### **3.1.4 Copilot Studio Advancements (Microsoft Build 2025)**

Microsoft Copilot Studio received several enhancements:
*   **Multi-Agent Orchestration (Private Preview):** Enables agents built with various Microsoft tools (M365 agent builder, Azure AI Agents Service, Microsoft Fabric) to collaborate on complex tasks.
*   **Computer Use in Copilot Studio Agents:** Agents can interact with desktop applications and websites.
*   **Bring Your Own Model (BYOM) and Model Fine-Tuning:** Copilot Studio supports BYOM and **Microsoft 365 Copilot Tuning** (a low-code capability for training models with company data). Integration with Azure AI Foundry models also provides access to over 1,900 models.
*   **New Publishing Channels:** Agents can be published to **SharePoint** and (starting July 2025) to **WhatsApp**.
*   **Additional Maker Controls for Knowledge (Public Preview):** New controls for shaping agent responses and reasoning, including uploading multiple related files into a file collection as a single knowledge source.

##### **3.1.5 Microsoft Entra Agent ID Deep Dive (Microsoft Build 2025)**

As covered in Section 2.4.1, the public preview of Microsoft Entra Agent ID was announced, providing unique, manageable identities for agents in Entra ID.

##### **3.1.6 M365 Agents Toolkit & SDK: General Availability (Microsoft Build 2025)**

The **Microsoft 365 Agents Toolkit and Software Development Kit (SDK) are now Generally Available**. These tools are designed for professional developers, offering full control, easier building/testing/evolving of agents, and deployment to Azure with smart defaults.

#### **3.2 Synergies: MCP, M365 Agents SDK, and the Broader AI Ecosystem**

##### **3.2.1 Connecting M365 Agents SDK with Azure AI Foundry and Semantic Kernel**

The M365 Agents SDK is engineered for seamless integration:
*   **Azure AI Foundry:** Developers can integrate components from the Azure AI Foundry SDK into their M365 Agents [24, referring to its section 3.1]. Azure AI Foundry provides specialized agent services, multi-agent orchestration, and enterprise-grade agent identity via Entra Agent ID [23, referring to its section 1.1].
*   **Semantic Kernel SDK:** Can be effectively used within M365 Agents for orchestration, enabling agents to chain calls to AI models, plugins, and memory sources. The **`weather-agent/dotnet` sample is a key example of integrating Semantic Kernel, demonstrating `AddKernel()` and `AddAzureOpenAIChatCompletion()` (or similar for other LLMs) in `Program.cs` for DI, and then using the `Kernel` instance within the agent class (e.g., `MyAgent.cs`).**
The M365 Agents SDK's AI agnosticism also supports other frameworks like LangChain or custom logic [19, 20, referring to its section 3.1]. **Developers can choose to implement AI services at the same time the core agent container gets built (e.g., in `Program.cs` via DI), or implement them elsewhere, for example, in the agent class (e.g., `MyAgent`) that gets built and passed to the agent at runtime. This latter approach allows for flexible use of Semantic Kernel or even multiple orchestrators, built separately from the main agent container logic.**

##### **3.2.2 Mastering Model Context Protocol (MCP) for .NET within M365 Agents**

Microsoft's significant investment signals MCP as a standard for agent interoperability.
*   **Consuming MCP Tools with .NET M365 Agents:** Agents built with the M365 Agents SDK can act as MCP clients using the official C\# SDK for MCP to consume tools, resources, and prompts from external MCP servers [40, referring to its section 2.2].
*   **Exposing M365 Agent Capabilities via MCP with .NET:** An M365 Agent can also expose its functionalities as an MCP server using the C\# MCP SDK [40, referring to its section 2.2]. Azure API Management can transform existing REST APIs into remote MCP servers [40, referring to its section 3.2].
MCP typically uses JSON-RPC 2.0 for messaging, with Server-Sent Events (SSE) as a common transport [23, referring to its section 3.2].

##### **3.2.3 The Pro-Code/Low-Code Bridge: M365 Agents SDK and Copilot Studio**

Microsoft fosters a complementary development environment for pro-code and low-code agent building:
*   **Distinct Tools:** Copilot Studio for fusion teams/citizen devs (visual, managed SaaS); M365 Agents SDK for pro-devs needing deep customization [20, referring to its section 1.2].
*   **Interoperability Pathways:**
    *   Agents built with M365 Agents SDK can be surfaced as "skills" in Copilot Studio.
    *   Multi-agent orchestration enables collaboration between agents built with different tools (M365 Agents SDK, Azure AI Agents Service, Fabric, Copilot Studio).
*   **Custom Engine Agents:** The M365 Agents SDK is a primary tool for "custom engine agents," offering developers complete authority over orchestration, AI models, and data integrations. These can be deployed within M365 Copilot [20, referring to its sections 3.1; 23, referring to its section 4.1].

### **Conclusion**

This foundational guide has introduced the Model Context Protocol (MCP) and the Microsoft 365 Agents SDK, outlining their core principles, architectures, and key implementation aspects for .NET developers within the Nucleus project. MCP provides the standard for tool interaction, while the M365 Agents SDK offers the framework for building modern, AI-powered agents integrated into the Microsoft ecosystem. Understanding these technologies is crucial for leveraging the full potential of Project Nucleus's refactored architecture. The "Nucleus Project: Advanced Architecture, Implementation, and Operations Guide" will build upon these foundations to detail project-specific patterns and advanced strategies.

---

### **Works Cited (for Document 1)**

\ AI agents unleashed in Windows with Model Context Protocol ..., accessed May 21, 2025, [https://siliconangle.com/2025/05/19/microsoft-unleashing-ai-agents-windows-model-context-protocol/](https://siliconangle.com/2025/05/19/microsoft-unleashing-ai-agents-windows-model-context-protocol/)
\ Model Context Protocol (MCP) an overview - Philschmid, accessed May 21, 2025, [https://www.philschmid.de/mcp-introduction](https://www.philschmid.de/mcp-introduction)
\ The Model Context Protocol MCP Architecture 2025 - CustomGPT, accessed May 21, 2025, [https://customgpt.ai/the-model-context-protocol-mcp-architecture/](https://customgpt.ai/the-model-context-protocol-mcp-architecture/)
\ A Complete Guide to the Model Context Protocol (MCP) in 2025 - Keywords AI, accessed May 21, 2025, [https://www.keywordsai.co/blog/introduction-to-mcp](https://www.keywordsai.co/blog/introduction-to-mcp)
\ Function Calling vs. MCP vs. A2A: Developer's Guide to AI Agent Protocols - Zilliz blog, accessed May 21, 2025, [https://zilliz.com/blog/function-calling-vs-mcp-vs-a2a-developers-guide-to-ai-agent-protocols](https://zilliz.com/blog/function-calling-vs-mcp-vs-a2a-developers-guide-to-ai-agent-protocols)
\ A beginners Guide on Model Context Protocol (MCP) - OpenCV, accessed May 21, 2025, [https://opencv.org/blog/model-context-protocol/](https://opencv.org/blog/model-context-protocol/)
\ Building Standardized AI Tools with the Model Context Protocol (MCP) - INNOQ, accessed May 21, 2025, [https://www.innoq.com/en/articles/2025/03/model-context-protocol/](https://www.innoq.com/en/articles/2025/03/model-context-protocol/) (Note: This source discusses JSON-RPC and SSE in MCP context)
\ Model Context Protocol (MCP): Why it matters! | AWS re:Post, accessed May 21, 2025, [https://repost.aws/articles/ARK3Jah0ZyS8GkPTsOJSnZkA/model-context-protocol-mcp-why-it-matters](https://repost.aws/articles/ARK3Jah0ZyS8GkPTsOJSnZkA/model-context-protocol-mcp-why-it-matters)
\ Build a Model Context Protocol (MCP) server in C\# - .NET Blog, accessed May 21, 2025, [https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
\ Microsoft Collaborates with Anthropic to Launch C\# SDK for MCP Integration - InfoQ, accessed May 21, 2025, [https://www.infoq.com/news/2025/04/microsoft-csharp-sdk-mcp/](https://www.infoq.com/news/2025/04/microsoft-csharp-sdk-mcp/)
\ modelcontextprotocol/csharp-sdk: The official C\# SDK for ... - GitHub, accessed May 21, 2025, [https://github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
\ Model Context Protocol (MCP) vs Function Calling: A Deep Dive into AI Integration Architectures - MarkTechPost, accessed May 21, 2025, [https://www.marktechpost.com/2025/04/18/model-context-protocol-mcp-vs-function-calling-a-deep-dive-into-ai-integration-architectures/](https://www.marktechpost.com/2025/04/18/model-context-protocol-mcp-vs-function-calling-a-deep-dive-into-ai-integration-architectures/)
\ What you need to know about the Model Context Protocol (MCP) - Merge.dev, accessed May 21, 2025, [https://www.merge.dev/blog/model-context-protocol](https://www.merge.dev/blog/model-context-protocol)
\ Securing the Model Context Protocol: Building a safer agentic future on Windows, accessed May 21, 2025, [https://blogs.windows.com/windowsexperience/2025/05/19/securing-the-model-context-protocol-building-a-safer-agentic-future-on-windows/](https://blogs.windows.com/windowsexperience/2025/05/19/securing-the-model-context-protocol-building-a-safer-agentic-future-on-windows/)
\ Building Claude-Ready Entra ID-Protected MCP Servers with Azure ..., accessed May 21, 2025, [https://devblogs.microsoft.com/blog/claude-ready-secure-mcp-apim](https://devblogs.microsoft.com/blog/claude-ready-secure-mcp-apim)
\ What is the Azure MCP Server (Preview)? - Learn Microsoft, accessed May 21, 2025, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/)
\ Microsot Build 2025| The main Highlights - Plain Concepts, accessed May 21, 2025, [https://www.plainconcepts.com/microsot-build-2025-recap/](https://www.plainconcepts.com/microsot-build-2025-recap/) (Note: Original "MCP Guide" used this as Ref 2 for MCP registry and MS joining steering committee)
\ Microsoft 365 Agents SDK documentation, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/)
\ What is the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview)
\ Custom engine agents for Microsoft 365 overview, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-custom-engine-agent](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-custom-engine-agent)
\ Multi-agent orchestration and more: Copilot Studio announcements - Microsoft, accessed May 23, 2025, [https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/](https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/)
\ microsoft/Agents-for-net: This repository is for active ... - GitHub, accessed May 23, 2025, [https://github.com/microsoft/Agents-for-net](https://github.com/microsoft/Agents-for-net)
\ How agents work in the Microsoft 365 Agents SDK (preview), accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk)
\ Create and Deploy a Custom Engine Agent with Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk)
\ Create a new .NET agent in Visual Studio using the Microsoft 365 Agents Toolkit, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-new-toolkit-project-vs](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-new-toolkit-project-vs)
\ Migration guidance from Azure Bot Framework SDK to the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance)
\ Introducing the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://devblogs.microsoft.com/microsoft365dev/introducing-the-microsoft-365-agents-sdk/](https://devblogs.microsoft.com/microsoft365dev/introducing-the-microsoft-365-agents-sdk/)
\ What's is Microsoft 365 Agents SDK and the Evolution of the Bot Framework #bot #agent #m365 - YouTube, accessed May 23, 2025, [https://www.youtube.com/watch?v=LdjiSEb4CPA](https://www.youtube.com/watch?v=LdjiSEb4CPA)
\ Quickstart: Create an agent with the Agents SDK - Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent) (Used as ref 17 in M365 Agents SDK Guide for IAgentHttpAdapter example)
\ Building agents with Agents SDK | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/building-agents](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/building-agents) (Used as ref 2 in M365 Agents SDK Guide for OnActivity handler example)
\ Deploy your agent to Azure and register with Azure Bot Service manually | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually)
\ AgentState(IStorage, String) Constructor (Microsoft.Agents.Builder.State), accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.state.agentstate.-ctor?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.state.agentstate.-ctor?view=m365-agents-sdk)
\ Announcing Microsoft Entra Agent ID: Secure and manage your AI agents, accessed May 23, 2025, [https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392](https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392)
\ Microsoft extends Zero Trust to secure the agentic workforce, accessed May 23, 2025, [https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/](https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/)
\ Multi-agent orchestration and more: Copilot Studio announcements ..., accessed May 21, 2025 (MCP Guide Ref 23), [https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/](https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/)
\ modelcontextprotocol/registry: A community driven registry ... - GitHub, accessed May 21, 2025 (MCP Guide Ref 26), [https://github.com/modelcontextprotocol/registry](https://github.com/modelcontextprotocol/registry)
\ Google I/O 2025: Key Takeaways of AI-Powered Tech Event Everyone Must Know About!, accessed May 21, 2025 (MCP Guide Ref 3), [https://theusaleaders.com/news/google-i-o-2025/](https://theusaleaders.com/news/google-i-o-2025/)
\ Build 2025: Agents in Microsoft 365 announcements, accessed May 23, 2025 (M365 Agents SDK Guide Ref 7), [https://techcommunity.microsoft.com/blog/microsoft365copilotblog/build-2025-agents-in-microsoft-365-announcements/4414281](https://techcommunity.microsoft.com/blog/microsoft365copilotblog/build-2025-agents-in-microsoft-365-announcements/4414281)
\ Introducing Microsoft 365 Copilot Tuning, multi-agent orchestration, and more from Microsoft Build 2025 | Microsoft 365 Blog, accessed May 23, 2025 (M365 Agents SDK Guide Ref 14), [https://www.microsoft.com/en-us/microsoft-365/blog/2025/05/19/introducing-microsoft-365-copilot-tuning-multi-agent-orchestration-and-more-from-microsoft-build-2025/](https://www.microsoft.com/en-us/microsoft-365/blog/2025/05/19/introducing-microsoft-365-copilot-tuning-multi-agent-orchestration-and-more-from-microsoft-build-2025/)
\ Connect Once, Integrate Anywhere with MCPs - Microsoft Developer Blogs, accessed May 23, 2025 (M365 Agents SDK Guide Ref 26), [https://devblogs.microsoft.com/blog/connect-once-integrate-anywhere-with-mcps](https://devblogs.microsoft.com/blog/connect-once-integrate-anywhere-with-mcps)