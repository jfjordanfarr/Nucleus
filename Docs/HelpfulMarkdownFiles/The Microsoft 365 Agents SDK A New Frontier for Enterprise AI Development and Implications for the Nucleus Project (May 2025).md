# **The Microsoft 365 Agents SDK: A New Frontier for Enterprise AI Development and Implications for the Nucleus Project (May 2025\)**

## **1\. Introduction: The Dawn of Enterprise-Grade AI Agents**

The landscape of enterprise application development is undergoing a significant transformation, driven by the rapid advancements in artificial intelligence (AI) and the emergence of sophisticated AI agents. These agents, capable of understanding context, reasoning, and taking action, promise to revolutionize workflows and user interactions. Microsoft, at the forefront of this evolution, has introduced the **Microsoft 365 Agents SDK**, a pivotal framework designed to empower developers to build, deploy, and manage these next-generation AI agents within the Microsoft ecosystem. This strategic move, underscored by key announcements at Microsoft Build 2025, signals a definitive shift from traditional bot development paradigms towards a more powerful, flexible, and integrated agent-centric model.

This report provides an in-depth analysis of the Microsoft 365 Agents SDK as of May 2025\. It examines the SDK's core capabilities, its architectural relationship with the Azure Bot Service, and its distinct advantages over the preceding Bot Framework. Furthermore, this report deconstructs the pivotal announcements from Microsoft Build 2025, including the general availability of the Agent Store, advancements in Copilot Studio, the introduction of Microsoft Entra Agent ID for secure agent identity, and the broad adoption of the Model Context Protocol (MCP) for enhanced interoperability.

The primary objective of this research is to inform crucial updates to three existing Nucleus open-source project reports: the "Nucleus Teams Adapter," "Secure Bot/Agent Deployment," and the "MCP Guide for.NET Developers." The focus will be on transitioning these guides from a Bot Framework-centric approach to one that fully embraces the Microsoft 365 Agents SDK, ensuring that Nucleus developers are equipped with the latest knowledge and best practices for building state-of-the-art AI agents within the Microsoft 365 ecosystem. This analysis will provide the technical details and strategic considerations necessary for Nucleus to adapt its architecture and guidance effectively.

## **2\. Understanding the Microsoft 365 Agents SDK**

The Microsoft 365 Agents SDK is an integrated development environment specifically engineered for creating sophisticated AI agents.1 It provides developers with the tools and libraries necessary to build agents that can be deployed across a multitude of channels, including Microsoft 365 Copilot, Microsoft Teams, custom web applications, and more, all while providing the essential scaffolding to manage the required communication.2

### **2.1. Purpose and Vision**

Microsoft's vision for the M365 Agents SDK is to provide a flexible and powerful platform for developers to build "custom engine agents".3 These agents offer full control over orchestration, AI models, and data integrations, enabling businesses to create advanced, tailored workflows that align with their unique requirements.3 The SDK is designed to empower developers to create agents that are not only conversational but also capable of performing complex tasks, reasoning over data, and collaborating with other agents and systems.5 This marks a significant step towards a future where human-agent collaboration powers a new era of productivity.7

A core principle underpinning the SDK is developer choice. It is intentionally "agnostic regarding the AI you choose" 2, allowing developers to integrate agents from any provider or technology stack into their enterprise systems. This flexibility ensures that organizations can leverage their existing AI investments or select the best-of-breed services, models, or orchestration frameworks to meet specific needs.2

### **2.2. Core Features and Capabilities**

The M365 Agents SDK offers several key features that distinguish it as a modern framework for agent development:

* **Rapid Agent Containerization:** The SDK allows for the quick construction of an "agent container" complete with state management, storage capabilities, and the ability to manage activities and events. This container can then be deployed across various channels like Microsoft 365 Copilot or Microsoft Teams.2 This containerized approach simplifies deployment and promotes consistency across different platforms.  
* **AI and Technology Stack Agnosticism:** Developers are not locked into a specific technology stack. The SDK supports the implementation of agentic patterns using any AI services, models, or orchestration tools preferred by the developer, including Azure AI Foundry, OpenAI Agents, Semantic Kernel, or LangChain.2  
* **Client-Specific Customization:** Agents can be tailored to align with the specific behaviors and capabilities of different client channels, such as Microsoft Teams or Microsoft 365 Copilot.2 This allows for optimized user experiences on each platform.  
* **Multi-Channel Deployment:** Agents built with the SDK can be surfaced across a wide array of channels, including Microsoft 365 Copilot, Microsoft Teams, web applications, and even third-party platforms like Facebook Messenger, Slack, or Twilio.2  
* **Integration with Orchestration Frameworks:** The SDK seamlessly integrates with popular orchestration frameworks like Semantic Kernel and LangChain, enabling developers to define complex workflows and reasoning chains for their agents.2  
* **Enterprise-Grade Development:** It is positioned as a comprehensive framework for building enterprise-grade agents, facilitating the integration of components from Azure AI Foundry SDK, Semantic Kernel, and AI components from other vendors.6

### **2.3. Architectural Overview: The SDK and Azure Bot Service**

The Microsoft 365 Agents SDK is not a complete departure from Microsoft's existing bot infrastructure; rather, it represents an evolution and an abstraction layer built upon it. The SDK works in conjunction with the Azure Bot Service to host agents and make them available across various channels.11 The Azure Bot Service defines a REST API and an activity protocol for interactions between agents, channels, and users. The M365 Agents SDK builds upon this REST API, providing a higher-level abstraction that allows developers to focus on conversational logic and agentic patterns rather than the intricacies of the underlying service communication.11

This architectural choice means that developers using the M365 Agents SDK are still leveraging the scalable and robust infrastructure of the Azure Bot Service. However, they interact with it through a more modern, powerful, and developer-friendly model. Core concepts from the Bot Framework, such as "activities" (representing interactions like messages or notifications) and "turns" (units of work consisting of incoming and outgoing activities), persist within the M365 Agents SDK but are framed within a richer agent context that supports more complex reasoning and state management.2 The SDK essentially provides the "agent container" and the "communication scaffolding," simplifying the development of sophisticated, multi-turn conversational experiences.2 Manual deployment to Azure and registration with Azure Bot Service remains a viable option for agents built with the SDK.1

The SDK facilitates communication between the client application (e.g., Teams, Copilot) and the agent logic. It handles the translation of channel-specific messages into a standardized activity format and manages the pipeline of operations from the client, through any middleware, to the agent itself.11

### **2.4. Development Environment and Tooling**

Microsoft provides a comprehensive development environment for the M365 Agents SDK, catering primarily to pro-code developers.

* **Supported Languages:** The SDK supports C\# (using the.NET 8.0 SDK), JavaScript (using Node.js version 18 and above), and Python (versions 3.9 to 3.11).2 This multi-language support offers flexibility for development teams with diverse skill sets.  
* **Microsoft 365 Agents Toolkit:** A key component of the development experience is the Microsoft 365 Agents Toolkit, an evolution of the Teams Toolkit.3 This toolkit, available as an extension for Visual Studio and Visual Studio Code, streamlines agent development by providing:  
  * **Project Templates:** Pre-built templates to scaffold new agent projects, such as the "Echo Agent" (a minimal baseline) and the "Weather Agent" (which integrates Azure AI Foundry or OpenAI services with orchestrators like Semantic Kernel or LangChain).4  
  * **Debugging Capabilities:** Tools for easy debugging, including local testing in the "Microsoft 365 Agents Playground" and direct debugging in Microsoft Teams or Microsoft 365 Copilot.10  
  * **Deployment Workflows:** Streamlined workflows for deploying agents to Azure, often with smart defaults.5 The Microsoft 365 Agents Toolkit and the SDK itself achieved General Availability (GA) as announced at Microsoft Build 2025\.5  
* **Getting Started:** Developers typically begin by cloning the official Agents GitHub repositories, which contain SDK source libraries and extensive samples for.NET, JavaScript, and Python.1 These samples serve as practical starting points and demonstrate various SDK features.  
* **Prerequisites (C\# Focus):** For C\# development, prerequisites include the.NET 8.0 SDK, the Bot Framework Emulator (useful for local testing, though Agents Playground is becoming more central), and a working knowledge of ASP.NET Core and asynchronous programming in C\#.2

This robust tooling and multi-language support underscores Microsoft's commitment to providing a rich and flexible development experience, allowing developers to choose their preferred tools and AI services. This "un-opinionated" nature 8 is a significant advantage for complex projects like Nucleus, which may have existing investments or specific preferences in AI models or orchestration tools. The SDK furnishes the essential "container" and "communication scaffolding" 2 without imposing rigid constraints on the internal agent logic.

## **3\. The Transition: From Bot Framework to the Microsoft 365 Agents SDK**

The introduction of the Microsoft 365 Agents SDK marks a significant evolution in Microsoft's strategy for building conversational AI experiences. It is not merely an update but represents a paradigm shift from the traditional Bot Framework model to a more powerful, AI-driven, and agent-centric approach.

### **3.1. Fundamental Differences and the New Agent-Centric Paradigm**

The Microsoft 365 Agents SDK is explicitly described as the "evolution of the Azure Bot Framework SDK".8 While Bot Framework primarily focused on "conversational AI around topics, dialogs, and messages" 8, the M365 Agents SDK is engineered for "modern agent development".8 This modern approach is characterized by:

* **Generative AI (GAI) Driven Functionality:** Moving beyond pre-programmed dialog flows to leverage the power of large language models (LLMs) for more natural and dynamic interactions.9  
* **Enterprise Knowledge Grounding:** Enabling agents to access and reason over vast amounts of enterprise data, providing contextually relevant and accurate responses.9  
* **Orchestration of Actions:** Equipping agents not just to answer questions but also to perform tasks and automate workflows across various systems.9

The very terminology shift from "bots" to "agents" is indicative of this change. "Bots" often implied reactive, rule-based systems, whereas "agents" suggest more proactive, intelligent entities capable of complex reasoning, learning, and autonomous task execution.15 This transition requires developers to adopt a new mindset, focusing on building agents that can understand intent more deeply, manage complex conversational states, and integrate seamlessly with a broader ecosystem of AI services and enterprise applications.

### **3.2. Deprecated Bot Framework Components and Key Migration Insights for.NET**

Migrating from the Bot Framework SDK to the Microsoft 365 Agents SDK involves more than just updating library references; it requires understanding which components are deprecated and how core functionalities are now handled. Several Bot Framework features are not being brought forward or are no longer directly relevant in the new agent paradigm [8 (1.2)]:

* **Adaptive Dialogs:** The Adaptive Dialog system, a cornerstone of complex conversation flow management in Bot Framework, is no longer directly relevant, and there are no plans to move this system into the Agents SDK.8  
* **Bot Framework Composer Artifacts:** Artifacts generated by the Bot Framework Composer (which often relied on Adaptive Dialogs and LG templates) are no longer required.8  
* **Legacy AI Tooling:** Services like LUIS (Language Understanding) and QnA Maker, along with their associated SDK components (e.g., Microsoft.Bot.Builder.Parsers.LU), are superseded by modern LLMs and retrieval augmented generation (RAG) patterns. The online services for LUIS and QnA Maker are already disabled.8  
* **Language Generation (LG) Tooling:** LG templates and related parsers are no longer needed, as general-purpose LLMs now handle response generation.8  
* **BotFrameworkAdapter:** This core adapter from Bot Framework has been replaced by CloudAdapter in later versions of Bot Framework and is now further abstracted or removed from the M365 Agents SDK.8 The new SDK introduces interfaces like IAgentHttpAdapter for handling HTTP interactions.  
* **Older Activity Types:** Certain older activity types, such as those related to payments, are deprecated.8  
* **Tooling:** Legacy code generators (like Yeoman for Bot Framework) and the Bot Framework CLI (bf command) are deprecated and not being brought forward.8

For.NET developers, the migration involves specific code changes 8:

* **Authentication:** The creation of AppCredentials-based objects for authentication is now handled through IConnections. Token authentication needs to be configured in the host application (e.g., ASP.NET Core web app). IAccessTokenProvider and the Microsoft.Agents.Authentication.Msal package are key for acquiring tokens. The Bot Framework's ConfigurationBotFrameworkAuthentication is replaced by RestChannelServiceClientFactory.  
* **Serialization:** The Bot Framework SDK predominantly used Newtonsoft.Json for serialization. The M365 Agents SDK transitions to using System.Text.Json.  
* **Adapter:** As mentioned, BotFrameworkAdapter is removed. While CloudAdapter was its successor in Bot Framework, the M365 Agents SDK introduces IAgentHttpAdapter (specifically, Microsoft.Agents.Hosting.AspNet.Core.IAgentHttpAdapter is seen in.NET examples for handling /api/messages endpoints 17). Documentation also notes that IBotFrameworkHttpAdapter was renamed to IBotHttpAdapter and CloudAdapterBase to ChannelServiceAdapterBase during the evolution, indicating a refinement of adapter interfaces.8  
* **State Management:** The way state is accessed and managed has changed. TurnState.Get\<T\>() (Bot Framework) is replaced by Services.Get\<T\>(). The TurnState object itself is conceptually replaced by StackState (or ITurnState becomes the interface for the turnState parameter passed to activity handlers). Furthermore, StackState.Add() is now StackState.Set().  
* **Application Classes:** Application\<TState\> from Bot Framework is now AgentApplication, and ApplicationOptions\<TState\> is AgentApplicationOptions.  
* **Namespaces:** Significant namespace changes occur:  
  * using Microsoft.Bot.Builder; becomes using Microsoft.Agents.BotBuilder;  
  * using Microsoft.Bot.Schema; changes to using Microsoft.Agents.Core.Models; and using Microsoft.Agents.Core.Serialization;  
  * using Microsoft.Bot.Builder.Integration.AspNet.Core; is replaced by using Microsoft.Agents.Hosting.AspNetCore;

The following table summarizes key differences and migration focus areas for Nucleus.NET developers:

**Table 1: M365 Agents SDK vs. Bot Framework SDK \- Comparative Analysis and Migration Focus for Nucleus (.NET)**

| Feature/Component | Bot Framework SDK Approach (.NET) | M365 Agents SDK Approach (.NET) | Key Changes & Migration Notes for Nucleus (.NET) |
| :---- | :---- | :---- | :---- |
| **Core Paradigm** | Dialog-driven, conversational AI (topics, dialogs, messages) 8 | Agent-centric, GAI-driven, orchestration, enterprise knowledge grounding 8 | Conceptual shift required; focus on building intelligent agents, not just reactive bots. |
| **Primary Adapter** | BotFrameworkAdapter | IAgentHttpAdapter (e.g., Microsoft.Agents.Hosting.AspNet.Core.IAgentHttpAdapter).17 IBotHttpAdapter also mentioned as an evolution.8 | BotFrameworkAdapter removed.8 Nucleus Teams Adapter needs to adopt new adapter interfaces for request processing. |
| **State Management** | TurnState property bag, UserState, ConversationState, BotStateSet. TurnState.Get\<T\>(), TurnState.Add() | ITurnState (parameter in OnActivity), StackState (replaces TurnState), AgentState class with IStorage.8 Services.Get\<T\>(), StackState.Set().8 | Significant refactoring for state handling. Adopt ITurnState/StackState for turn-specific data and AgentState with IStorage for persistent state. |
| **Dialog Management** | Adaptive Dialogs, Component Dialogs, Waterfall Dialogs | No direct equivalent of Adaptive Dialogs. Focus on custom orchestration with Semantic Kernel, LangChain, or other AI services.3 | Deprecation of Adaptive Dialogs means Nucleus agents relying on them need to be redesigned using orchestration patterns. |
| **Authentication** | AppCredentials, MicrosoftAppCredentials, ConfigurationBotFrameworkAuthentication | IConnections, IAccessTokenProvider, Microsoft.Agents.Authentication.Msal, RestChannelServiceClientFactory.8 Host-level setup. | Update authentication mechanisms to use new interfaces and Msal-based flows. |
| **Core Application Objects** | IBot interface, ActivityHandler, Application\<TState\>, ApplicationOptions\<TState\> | IAgent interface, AgentApplication, AgentApplicationOptions.2 OnActivity for event handling. | Refactor bot classes to AgentApplication and update event handling logic. |
| **AI Integration** | LUIS, QnA Maker integrations, custom AI | AI Agnostic. Integrates with Azure AI Foundry, Semantic Kernel, LangChain, OpenAI, any custom/third-party AI service.2 | Leverage flexibility for Nucleus's preferred AI models. Migrate from LUIS/QnA Maker. |
| **Serialization** | Newtonsoft.Json | System.Text.Json 8 | Update any custom serialization/deserialization logic. |
| **Tooling** | Bot Framework Emulator, bf CLI, Yeoman generators | M365 Agents Toolkit (VS, VS Code), Agents Playground.8 | Adopt new toolkit and testing tools. bf CLI and Yeoman generators are deprecated. |
| **Key Namespaces** | Microsoft.Bot.Builder, Microsoft.Bot.Schema | Microsoft.Agents.BotBuilder, Microsoft.Agents.Core.Models, Microsoft.Agents.Hosting.AspNetCore 8 | Update using statements and resolve type conflicts. |

This evolution of the adapter concept, moving from BotFrameworkAdapter to more specialized interfaces like IAgentHttpAdapter (or CloudAdapter in JavaScript contexts 20), is central to how the M365 Agents SDK handles multi-channel communication and abstracts the underlying Bot Service interactions. For Nucleus, this means the Teams Adapter will need to be re-architected around these new adapter patterns, which are designed to be more aligned with the agent model and offer potentially richer features for channel integration and even inter-agent communication.

### **3.3. Implications for Nucleus Components Currently Reliant on Bot Framework**

The transition to the M365 Agents SDK has direct and significant implications for existing Nucleus project components:

* **Nucleus Teams Adapter:** This component will require the most substantial refactoring. It must be updated to use the M365 Agents SDK's.NET libraries, implement the new adapter patterns (e.g., IAgentHttpAdapter), adopt the new authentication mechanisms, and correctly handle activities and state management according to the SDK's conventions.  
* **Secure Bot/Agent Deployment Guide:** This guide needs a complete overhaul. The focus must shift to securing M365 Agents, prominently featuring Microsoft Entra Agent ID as the primary identity and access management solution. Azure deployment strategies specific to M365 Agents, leveraging the Azure Bot Service where appropriate, will also need to be detailed.  
* **MCP Guide for.NET Developers:** While the Model Context Protocol itself is a separate standard, the agents that consume or expose MCP services will now be built using the M365 Agents SDK. This will affect how MCP interactions are coded within the agent, including the use of the new C\# SDK for MCP.

This migration is more than a simple library swap; it's a conceptual shift. Developers must embrace the new "agent-centric" model, which emphasizes GAI, sophisticated orchestration, and broader enterprise integration, moving away from the more constrained dialog-flow model of traditional bots.8 The deprecation of components like Adaptive Dialogs and Composer artifacts 8 clearly signals this move towards more dynamic, AI-driven interactions. The SDK's AI agnosticism 2 and integration with powerful tools like Semantic Kernel 3 mean that developers are now expected to build more advanced reasoning and planning capabilities directly into their agents.

## **4\. Microsoft Build 2025 Deconstructed: Pivotal Announcements for Agent Developers**

Microsoft Build 2025 delivered a suite of announcements that collectively reinforce Microsoft's strategy to build a comprehensive ecosystem for enterprise AI agents. These developments span agent discovery, low-code and pro-code development tools, security, and interoperability, all of which have direct implications for developers working with the Microsoft 365 Agents SDK.

### **4.1. The Agent Store & New Microsoft-Built Agents**

A significant announcement was the **General Availability (GA) of the Agent Store**.7 This centralized marketplace within Microsoft 365 Copilot Chat allows users to easily find, pin, and utilize a range of agents. The Agent Store features prebuilt agents developed by Microsoft (like the Researcher agent), agents from partners, and custom agents developed by the user's own company.7 This initiative aims to simplify agent discovery and access, making on-demand expertise readily available.

Alongside the Agent Store, Microsoft announced the upcoming GA in **June 2025 for two new prebuilt agents**:

* **Employee Self-Service Agent:** Connected to company knowledge sources and systems, this agent handles employee queries on HR and IT topics, processes requests, and provides self-service capabilities, aiming to reduce support tickets and optimize operations.7  
* **Skills Agent:** Powered by a new People Skills data layer, this agent helps employees find experts within the organization, understand colleagues' skillsets, and manage their own skill profiles. For leaders, it offers insights into talent strengths and gaps for strategic workforce planning.7

### **4.2. Copilot Studio Advancements**

Microsoft Copilot Studio, the low-code platform for building agents, received several powerful enhancements:

* **Multi-Agent Orchestration:** Now in private preview with a public preview coming soon, this feature enables agents built with various Microsoft tools—including the M365 agent builder (referring to agents built with M365 Agents SDK), Microsoft Azure AI Agents Service, and Microsoft Fabric—to collaborate on complex, business-critical tasks that span multiple systems and workflows.5 This signifies a major step towards intelligent agent teamwork.  
* **Computer Use in Copilot Studio Agents:** Agents can now interact with desktop applications and websites in a human-like manner, such as clicking buttons, navigating menus, and typing in fields. The system is designed to adapt automatically as user interfaces change.5  
* **Bring Your Own Model (BYOM) and Model Fine-Tuning:** Copilot Studio now supports bringing your own models and offers model fine-tuning capabilities.5 Specifically, **Microsoft 365 Copilot Tuning**, a new low-code capability, allows organizations to train models using their own company data, workflows, and processes without requiring data science expertise.14 Integration with Azure AI Foundry models also provides access to over 1,900 models.14  
* **New Publishing Channels:** Copilot Studio agents can now be published to **SharePoint** and, starting in early July 2025, to **WhatsApp**, expanding their reach to users where they already work and communicate.5  
* **Additional Maker Controls for Knowledge:** Now in public preview, new controls in Generative AI agent settings give makers more ways to shape agent responses and reasoning. This includes the ability to upload multiple related files into a file collection and use that collection as a single knowledge source for an agent.5

### **4.3. Securing Agents: Microsoft Entra Agent ID Deep Dive**

A cornerstone of Microsoft's agent strategy is robust security, addressed by the public preview of **Microsoft Entra Agent ID**.21

* **Core Concept:** Every agent built with Azure AI Foundry or Microsoft Copilot Studio is automatically assigned a unique, first-class identity within Microsoft Entra ID. This allows agents to be managed with the same identity and access management tools and principles as human users.21 Support for agents created with Microsoft Security Copilot, Microsoft 365 Copilot (which would include those built via M365 Agents SDK and surfaced in M365 Copilot), and third-party tools is planned for the near future.21  
* **Visibility and Management:** Identity practitioners can view and securely manage these agent identities (a new "Agent ID (Preview)" application type) directly within the Microsoft Entra admin center.21  
* **Benefits for Developers and Identity Practitioners:**  
  * **Least-Privileged Access:** Agent identities will be controlled by a least-privileged approach, requesting just-in-time (JIT), scoped tokens for only the resources the agent needs (e.g., a specific file or Teams channel).21  
  * **Instant Enterprise Onboarding:** Simplifies security reviews and eliminates the need for custom OAuth flows, as agents become complete identities in Entra.21  
  * **Scalability:** Agents can be registered once and have an identity in other Microsoft Entra tenants, each with its own policies, while maintaining a single codebase.21  
* **Integration with Workforce Systems:** Microsoft is partnering with ServiceNow and Workday to integrate Entra Agent ID with their platforms, enabling automated provisioning of identities for these "digital employees".16

### **4.4. Interoperability Unleashed: Model Context Protocol (MCP)**

Microsoft Build 2025 highlighted a significant push towards AI agent interoperability through broad support for the **Model Context Protocol (MCP)** specification.5

* **Purpose of MCP:** MCP is an open standard designed to connect AI agents to a diverse range of external systems where data and tools reside, such as content repositories, business applications, and development environments. It allows agents to use rich contextual information to act more efficiently and effectively.23 The goal is a "connect once, integrate anywhere" model.26  
* **Broad Ecosystem Support:** Microsoft is championing MCP across its platforms, including Copilot Studio, Azure AI Foundry, Semantic Kernel, Windows 11, and GitHub.5 Microsoft and GitHub have also joined the MCP Steering Committee.23  
* **MCP Servers:**  
  * **Dataverse MCP Server:** Transforms structured information from Dataverse into queryable knowledge for agents built in Copilot Studio.23  
  * **Dynamics 365 MCP Servers:** Enable agents to interoperate smoothly with Dynamics 365 ERP and CRM applications, synchronizing actions and knowledge in real-time.23  
  * **Azure API Management for MCP:** Azure API Management can be used to transform existing REST APIs into remote MCP servers using Server-Sent Events (SSE) and Streamable HTTP.26  
* **Consuming MCP in Copilot Studio:** MCP servers expose tools, resources (file-like data), and prompts. These can be consumed as actions by agents in Copilot Studio through the creation of custom connectors based on an OpenAPI specification YAML file describing the MCP server's API.24 Currently, Copilot Studio supports the Server-Sent Events (SSE) transport for MCP communication.24  
* **C\# SDK for MCP:** Microsoft has collaborated with Anthropic on an official C\# SDK for building MCP servers and clients, simplifying MCP integration within the.NET ecosystem.26

### **4.5. M365 Agents Toolkit & SDK: General Availability and What It Means**

The **Microsoft 365 Agents Toolkit and Software Development Kit (SDK) are now Generally Available (GA)**.5

* **Empowering Pro-Developers:** These tools are designed for professional developers, providing full control over agent ingredients.3 They make it easier to build, test, and evolve agents over time.  
* **Key Benefits:** Developers can swap models or orchestrators without starting from scratch, use SDK templates to jumpstart projects, and deploy to Azure with smart defaults.5 The SDK helps developers test and debug agents in Copilot, Teams, on the web, and in the Agent Playground.14

These announcements collectively paint a picture of a rapidly maturing ecosystem. Microsoft is not just providing tools for building individual agents but is architecting a foundational platform—akin to an "operating system" for enterprise agents. The Agent Store acts as a discovery mechanism, Copilot Studio and the M365 Agents SDK provide the development environments for low-code and pro-code "applications" (agents), multi-agent orchestration enables inter-agent collaboration, Entra Agent ID furnishes the critical identity and security model, and MCP offers the "APIs" and protocols for agents to access data and tools. For projects like Nucleus, understanding how its agents fit into this emerging "OS" is paramount.

Furthermore, the strong emphasis on Entra Agent ID and MCP underscores that robust security and open interoperability are not afterthoughts but foundational pillars for the widespread adoption of AI agents in the enterprise. This makes Nucleus's "Secure Bot/Agent Deployment" and "MCP Guide" reports central to aligning with Microsoft's core strategy.

**Table 2: Key Microsoft Build 2025 Announcements for Agent Development**

| Announcement | Core Details | Status (May 2025\) | Direct Relevance to M365 Agents SDK & Nucleus |
| :---- | :---- | :---- | :---- |
| **Agent Store** | Centralized marketplace in Copilot Chat for discovering and using Microsoft, partner, and custom agents.7 | Generally Available (GA) | Nucleus-built agents, if packaged appropriately, could potentially be discoverable here. Informs deployment strategy. |
| **New Prebuilt Agents (Employee Self-Service, Skills)** | Agents for HR/IT self-service and talent/skills discovery.7 | General Availability in June 2025 | Illustrates Microsoft's direction with first-party agents; Nucleus agents might complement or integrate with these. |
| **Copilot Studio: Multi-Agent Orchestration** | Agents from M365 agent builder, Azure AI Agents Service, Fabric can collaborate.5 | Private Preview (Public Preview Soon) | Nucleus agents built with M365 Agents SDK can participate. Impacts architectural considerations for complex tasks. |
| **Copilot Studio: Computer Use in Agents** | Agents can interact with desktop apps/websites like a human.5 | (Status not specified, likely Preview) | If Nucleus agents are surfaced via Copilot Studio, this capability could be leveraged. |
| **Copilot Studio: BYOM & Copilot Tuning** | Bring your own models; low-code model tuning with company data.5 | Public Preview (Copilot Tuning) | Offers flexibility if Nucleus components are integrated with Copilot Studio solutions. |
| **Copilot Studio: New Publishing Channels** | SharePoint and WhatsApp (early July 2025).5 | SharePoint (status not specified), WhatsApp (Early July 2025\) | Expands deployment options if Nucleus agents are exposed through Copilot Studio. |
| **Microsoft Entra Agent ID** | Unique, manageable identities for agents in Entra ID (initially for Azure AI Foundry, Copilot Studio).21 | Public Preview | Critical for secure deployment of Nucleus agents. "Secure Agent Deployment" guide must incorporate this. |
| **Model Context Protocol (MCP) Enhancements** | Broad support across Microsoft ecosystem; C\# SDK for MCP servers/clients.26 | MCP support GA in various tools; C\# SDK available. | Essential for interoperability. "MCP Guide for.NET Developers" for Nucleus needs updating for the C\# SDK and M365 Agents. |
| **M365 Agents Toolkit & SDK GA** | Tools for pro-developers to build, test, evolve, and deploy agents.5 | Generally Available (GA) | This is the core SDK for Nucleus's future agent development. All Nucleus reports must shift focus to this SDK. |

## **5\. Technical Deep Dive: Leveraging M365 Agents SDK for the Nucleus Project (.NET Focus)**

With the Microsoft 365 Agents SDK now generally available,.NET developers within the Nucleus project have a powerful new framework for building enterprise-grade AI agents. This section delves into the practical aspects of using the SDK, focusing on the development lifecycle, state management, proactive messaging, handling long-running operations, and specific considerations for Microsoft Teams integration.

### **5.1. Agent Development Lifecycle with M365 Agents Toolkit (.NET)**

The M365 Agents Toolkit for Visual Studio significantly streamlines the development process for.NET agents.10

* **Project Scaffolding:** Creating a new.NET agent project begins in Visual Studio by selecting the "Microsoft 365 Agents" project type. The toolkit offers templates like the "Weather Agent," which comes pre-configured with Semantic Kernel for orchestration and can integrate with Azure AI Foundry or Azure OpenAI models, or the "Empty Agent" for a minimal starting point.4  
* **Core Agent Implementation (C\#):** The basic structure of a C\# agent involves using WebApplication.CreateBuilder() and then adding agent-specific services and the agent itself. Key components include builder.AddAgent(...), which registers the AgentApplication. The AgentApplication is instantiated with AgentApplicationOptions and is where activity handlers are defined, such as agent.OnActivity(ActivityTypes.Message, async (turnContext, turnState, cancellationToken) \=\> {... }); to respond to incoming messages.2  
* **Testing and Debugging:** The toolkit provides the **Microsoft 365 Agents Playground** for local testing. Developers can select this as a debug target in Visual Studio, and it will open in a browser, allowing direct interaction with the locally running agent.10 Additionally, the toolkit supports setting the debug target directly to Microsoft Teams or Microsoft 365 Copilot, enabling in-situ debugging.10  
* **Deployment:** While the toolkit aims to simplify deployment, manual deployment to Azure is also documented. This typically involves creating an Azure Bot Service app registration (ensuring the App ID matches the Azure Bot Service record) and publishing the agent code to an Azure Web App or similar hosting service \[1 (2.1)\]. The messaging endpoint in the Azure Bot Service configuration must then be updated to point to the deployed agent's API endpoint (e.g., https://{yourwebsite}/api/messages).13 For deployment to Microsoft Teams or Microsoft 365 Copilot, a manifest .zip package (containing a manifest.json file and icons) needs to be prepared and side-loaded into the respective admin centers.13 The manifest references the Bot ID (Client ID) of the registered agent.13

### **5.2. Advanced State Management: ITurnState, StackState, and IStorage**

Managing conversational context across multiple turns is crucial for sophisticated agents.11 The M365 Agents SDK provides mechanisms for state and storage.2

* **IStorage:** For basic scenarios or testing, MemoryStorage can be registered as a singleton service: builder.Services.AddSingleton\<IStorage, MemoryStorage\>();.17 For production, more persistent storage solutions (e.g., Azure Blob Storage, Cosmos DB) would be used by implementing the IStorage interface.  
* **AgentState:** The SDK includes an AgentState class. Its constructor typically takes an IStorage implementation and a stateName string. This object uses the provided storage to persist state property values and caches state within the context of each turn, facilitating state management across interactions \[11 (5.2)\].  
* **ITurnState and StackState:** Migration guidance from Bot Framework to M365 Agents SDK indicates a significant change in how turn-specific state is handled.8  
  * The turnState parameter, now typed as ITurnState (or a concrete class implementing it), is passed directly into OnActivity handlers (e.g., async (turnContext, turnState, cancellationToken) \=\> {... }).2  
  * The Bot Framework's TurnState property (often a dynamic bag) is conceptually replaced by StackState.  
  * Accessing services previously done via turnState.Get\<T\>() is now typically turnContext.Services.Get\<T\>() or a similar pattern on ITurnState if it holds scoped services.  
  * For managing values within this turn-specific state, StackState.Add() from Bot Framework is replaced by StackState.Set() in the M365 Agents SDK context.8 This evolution towards a more explicit ITurnState and StackState suggests a more structured approach to managing the ephemeral data associated with a single turn of conversation, potentially offering better control over state scopes. Nucleus components, particularly the Teams Adapter and any stateful agents, must adapt to this new pattern.

### **5.3. Implementing Proactive Messaging and Handling Long-Running Operations**

Agents often need to send messages not directly in response to user input (proactive messages) or manage tasks that take time to complete.

* **Proactive Messaging:**  
  * **Core Requirement:** To send a proactive message, the agent needs a ConversationReference from a previous interaction with the user or channel. This reference contains all the necessary information to target the message correctly.30  
  * **Obtaining ConversationReference:** This can be obtained from an incoming activity within an OnActivity handler using turnContext.Activity.GetConversationReference().33 The IActivity interface itself defines GetConversationReference() 33 and ApplyConversationReference(ConversationReference, bool) 34 methods. This reference should then be stored securely (e.g., using IStorage) for later use.32  
  * **Sending the Proactive Message:** The general pattern involves using an adapter component to "continue" a conversation. The Bot Framework SDK used BotFrameworkHttpAdapter.ContinueConversationAsync(appId, conversationReference, botCallback, cancellationToken).30 For the M365 Agents SDK, the equivalent method would be sought on the relevant adapter interface (e.g., IBotHttpAdapter or IAgentHttpAdapter). The IAgentHttpAdapter is injected and used in the ASP.NET Core endpoint mapping for reactive messages (app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) \=\> { await adapter.ProcessAsync(request, response, agent, cancellationToken); });).17 To send proactively, this adapter instance (or one obtained via dependency injection) would be used along with the stored ConversationReference to create a new turn context outside of a direct user request, within which turnContext.SendActivityAsync(...) can be called. The Teams AI Library showcases an app.Send(conversationId, message) pattern 31, where app is an instance of the Application (or AgentApplication). If the M365 Agents SDK's AgentApplication offers a similar high-level send method that internally handles ConversationReference and adapter logic, it would simplify proactive messaging. Otherwise, direct use of the adapter's "continue conversation" capability is the standard approach.  
* **Long-Running Operations:** These are typically handled using standard.NET asynchronous programming patterns (async/await and Task\<T\>).36 An agent can initiate a long-running task in response to an activity. Before starting the task, it should store the ConversationReference. Once the task completes (potentially in a background service or a separate thread), the agent can use the stored ConversationReference and the proactive messaging mechanism described above to notify the user of the outcome.32 An external trigger, like a dedicated "notify" HTTP endpoint, can also be used to initiate the proactive turn.32

Effective proactive messaging relies on robust ConversationReference management and the adapter's ability to re-hydrate a turn context. This is crucial for Nucleus features requiring notifications, follow-ups, or updates after background processing.

### **5.4. Developing for Microsoft Teams: File Attachments and Channel-Specific Features**

The M365 Agents SDK utilizes the Bot Framework Activity schema, which allows for channel-specific data and rich attachments.11

* **Receiving File Attachments from Teams:** When a user uploads a file in a Teams chat with an agent, the incoming Activity object will have its Attachments property populated.  
  * Each Attachment object in the list will typically have:  
    * contentType: For files from Teams, this might be application/vnd.microsoft.teams.file.download.info.38  
    * contentUrl: This often points to the file's location in SharePoint or OneDrive.38  
    * name: The name of the file.  
    * content: A nested object that can contain further details like:  
      * downloadUrl: A direct URL to fetch the file content.  
      * uniqueId: The unique file ID, which is typically the OneDrive/SharePoint DriveItem ID.38  
      * fileType: The file extension (e.g., "pdf", "docx").38  
  * The Activity.ChannelData property might also contain Teams-specific information related to the file or interaction, which can be parsed using turnContext.Activity.GetChannelData\<T\>().34 The @microsoft/agents-hosting-teams JavaScript package defines types like FileUploadInfo 39, suggesting similar structures or parsing needs in.NET.  
* **Agent Uploading Files to Teams:** This is a multi-step process 38:  
  1. The agent sends a message to the user containing a FileConsentCard attachment, requesting permission to upload the file. This card includes details like the file description and size.  
  2. If the user accepts, the agent receives an Invoke activity (name: "fileConsent/invoke", value.action: "accept") containing an uploadInfo object with a contentUrl (target location) and an uploadUrl (a temporary URL for POSTing the file).  
  3. The agent then performs an HTTP POST request with the file content directly to the uploadUrl.  
  4. Optionally, the agent can then send a confirmation message with a FileInfoCard or remove the original consent card.  
* **Nucleus Implication:** The Nucleus Teams Adapter must be updated to correctly parse incoming file attachments from Teams, extracting relevant identifiers like uniqueId and downloadUrl from the Activity.Attachments structure. It should also be capable of initiating file uploads to Teams by following the consent card flow if required by Nucleus use cases.

## **6\. Fortifying Your Agents: Security and Governance with Microsoft's Ecosystem**

As AI agents become more autonomous and integrated into enterprise workflows, their security and governance are paramount. Microsoft is addressing this by building a comprehensive security framework around its agent ecosystem, primarily through Microsoft Entra Agent ID and Microsoft Purview, all underpinned by Zero Trust principles.

### **6.1. Identity and Access Management with Microsoft Entra Agent ID**

The public preview of **Microsoft Entra Agent ID** represents a fundamental shift in how AI agents are secured.21

* **Centralized Agent Identity:** Agents created using Azure AI Foundry and Microsoft Copilot Studio are automatically assigned unique, first-class identities within Microsoft Entra ID.21 This means each agent becomes a manageable principal in the enterprise's identity system, analogous to human users or service principals. Support for agents built with the M365 Agents SDK (when deployed and registered appropriately, likely via Azure Bot Service and then in Entra) and surfaced in M365 Copilot, as well as third-party solutions, is expected to follow.21  
* **Visibility and Control:** Administrators gain visibility into these agent identities through a new "Agent ID (Preview)" application type in the Microsoft Entra admin center, allowing them to inventory and manage agents centrally.21  
* **Least Privilege and Just-In-Time (JIT) Access:** Entra Agent ID promotes a least-privileged access model. Agents will request JIT, scoped tokens for precisely the resources they need to access, such as a specific file in SharePoint or a particular Teams channel, rather than broad, standing permissions.21  
* **Simplified Enterprise Onboarding:** By treating agents as standard Entra ID identities, organizations can leverage existing tools and processes for discovery, approval, auditing, and lifecycle management, streamlining security reviews and eliminating the need for custom OAuth flows for each agent.21  
* **Implications for Nucleus:** The "Secure Bot/Agent Deployment" guide for Nucleus must be significantly updated. Microsoft Entra Agent ID should be positioned as the primary mechanism for agent identity, authentication, and authorization. The guide should detail how M365 Agents, particularly those developed with the.NET SDK, can be registered and managed with Entra Agent ID.

The introduction of Entra Agent ID moves agent security beyond merely protecting the agent's runtime and communication channels. It establishes an identity-centric governance model where the agent itself is a recognized and managed entity within the enterprise security fabric.21 This allows for the application of standard identity governance practices directly to the agent, addressing the critical question of "who is the agent and what is it permitted to do?" at an enterprise identity level.

### **6.2. Data Security and Compliance using Microsoft Purview**

Microsoft Purview's data security and compliance capabilities are being extended to the AI agent landscape.16

* **Extended Controls:** Purview controls can now be applied to:  
  * Any custom-built AI application via a new Microsoft Purview software development kit (SDK).  
  * Natively for AI agents built within Azure AI Foundry and Copilot Studio.16  
* **Governance for AI Agents:** This integration allows organizations to enforce data classification, sensitivity labeling, data loss prevention (DLP), and other compliance policies on the data that AI agents access, process, and generate.  
* **Implications for Nucleus:** If Nucleus agents are designed to handle sensitive corporate data, understanding and potentially integrating with Microsoft Purview's capabilities will be essential for building secure and compliant solutions. The new Purview SDK might be relevant for custom logging or data interaction patterns within Nucleus agents.

### **6.3. Implementing Zero Trust for the Agentic Workforce**

Microsoft is explicitly extending its **Zero Trust security model** to what it terms the "agentic workforce".16 This model operates on the principles of "never trust, always verify," "assume breach," and "use least privileged access."

* **Identity as the New Perimeter:** Microsoft Entra, and specifically Entra Agent ID, plays a pivotal role in this strategy by ensuring that every AI agent has a strong, verifiable, and manageable identity.16  
* **Holistic Security Integration:** The approach involves embedding identity (Microsoft Entra), security (Microsoft Defender for threat detection and response), and governance (Microsoft Purview) capabilities directly into Microsoft's agent-building platforms and the operational lifecycle of agents.16  
* **Implications for Nucleus:** The architectural design and deployment strategies for Nucleus agents must align with Zero Trust principles. This includes ensuring that agents authenticate and authorize for every operation, access only the data and resources necessary for their tasks (least privilege via scoped tokens from Entra Agent ID), and that their communications and actions are auditable.

Microsoft is evidently creating a unified governance plane for AI. By integrating Entra ID for agent identity, Purview for data security, and Defender for threat protection across its agent platforms (Azure AI Foundry, Copilot Studio, and by extension, M365 Agents SDK-built agents registered within this framework), a consistent security and compliance posture can be applied. This is crucial for Nucleus, as it should aim to align its agent deployment and operational practices with these integrated governance tools to ensure enterprise-readiness.

**Table 3: Security and Governance Mechanisms for M365 Agents**

| Mechanism | Description & Capabilities | Application within M365 Agents SDK Context for Nucleus |
| :---- | :---- | :---- |
| **Microsoft Entra Agent ID** | Assigns unique, first-class identities in Entra ID to agents. Enables centralized management, visibility, least-privileged access (JIT, scoped tokens), lifecycle management, and auditing using standard Entra tools.21 | Nucleus agents (built with M365 Agents SDK, deployed via Azure Bot Service) should be registered as Entra Agent IDs. The "Secure Agent Deployment" guide must detail this process and best practices for.NET agents. |
| **Microsoft Purview Integration** | Extends data security and compliance (classification, protection, DLP) to AI agents. Native for Azure AI Foundry/Copilot Studio; SDK available for custom apps.16 | If Nucleus agents handle sensitive data, leverage Purview SDK for custom data interactions or ensure compliance with policies applied through native integrations if agents are part of a broader Azure AI/Copilot Studio solution. |
| **Microsoft Defender Integration** | Provides threat detection and response capabilities for the agent ecosystem (part of the holistic security approach).16 | Ensure Nucleus agent hosting environments and interactions are monitored by Defender, aligning with enterprise security operations. |
| **Zero Trust Principles** | Security model based on "never trust, always verify," assume breach, and least privileged access.16 | Nucleus agent architecture, authentication/authorization logic, and data access patterns must inherently follow Zero Trust principles. Entra Agent ID is a key enabler. |

## **7\. Synergies and Integrations: The M365 Agents SDK in the Broader AI Landscape**

The Microsoft 365 Agents SDK does not exist in isolation. It is designed to be a versatile component within Microsoft's broader AI ecosystem, enabling developers to connect with various AI services, orchestration tools, and data protocols. This interoperability is key to building truly powerful and integrated enterprise agents.

### **7.1. Connecting with Azure AI Foundry and Semantic Kernel**

The M365 Agents SDK is engineered for seamless integration with other powerful Microsoft AI technologies:

* **Azure AI Foundry:** Developers can integrate components from the Azure AI Foundry SDK into their M365 Agents \[4 (3.1)\]. Azure AI Foundry itself provides a comprehensive suite of capabilities, including specialized agent services, tools for multi-agent orchestration, and the foundational enterprise-grade identity for agents through its integration with Microsoft Entra Agent ID \[11 (1.1)\]. This allows M365 Agents to leverage advanced AI models, fine-tuning capabilities, and robust infrastructure provided by Azure AI.  
* **Semantic Kernel SDK:** Semantic Kernel can be effectively utilized within the middleware or agent logic components of an M365 Agents SDK-built agent.2 Its primary role is to facilitate orchestration, enabling agents to chain together calls to various AI models, plugins (custom functions or API connectors), and memory sources to perform complex reasoning and planning. The "Weather Agent" template provided with the M365 Agents Toolkit is a practical example of Semantic Kernel integration.4  
* **AI Agnosticism and Extensibility:** A defining characteristic of the M365 Agents SDK is its AI agnosticism.2 While it integrates smoothly with Microsoft offerings like Azure AI Foundry and Semantic Kernel, it also fully supports the use of other orchestration frameworks like LangChain, or even entirely custom-built orchestration logic and AI models \[3 (3.1)\]. This flexibility ensures that developers can choose the best tools for their specific requirements, rather than being confined to a single vendor's stack.

The M365 Agents SDK, therefore, acts as a powerful "glue." It provides the foundational structure for an agent (the "container" and communication scaffolding) while allowing developers to plug in diverse AI components—be it sophisticated models from Azure AI Foundry, orchestration logic from Semantic Kernel or LangChain, or bespoke AI services. This allows for the creation of highly customized and capable agents that can draw upon a heterogeneous set of AI capabilities. For Nucleus, this means it can serve as the central point for integrating its custom.NET logic with preferred AI models and potentially interact with or extend functionalities provided by other Microsoft AI platforms.

### **7.2. Mastering Model Context Protocol (MCP) for.NET: Consumption and Exposure**

Microsoft's significant investment in the Model Context Protocol (MCP) signals its intent to establish MCP as a standard for AI agent interoperability.26 MCP aims to provide a universal way for AI agents to discover and interact with external tools, data sources, and even other agents, fostering an open and collaborative ecosystem.26

* **Consuming MCP Tools with.NET Agents:** Agents built with the M365 Agents SDK can function as MCP clients. Microsoft, in collaboration with Anthropic, has released an official C\# SDK for building both MCP servers and clients \[26 (2.2)\]. This SDK will be instrumental for.NET M365 Agents to consume tools, resources (file-like data), and prompts exposed by external MCP servers. While Copilot Studio provides a low-code way to consume MCP tools via custom connectors 24, pro-code.NET agents will leverage this C\# SDK for direct integration.  
* **Exposing Agent Capabilities via MCP with.NET:** Conversely, an M365 Agent built with the.NET SDK can expose its own functionalities as an MCP server. This would make its unique tools, data access capabilities, or specialized logic available to other MCP-compliant clients, such as agents built in Copilot Studio or even third-party agents. The aforementioned C\# SDK for MCP will be used to implement such MCP server functionality.26 Additionally, Azure API Management offers a pathway to transform existing REST APIs into remote MCP servers, which can then be accessed by agents \[26 (3.2)\].  
* **MCP Architecture:** MCP follows a client-host-server architecture, typically using JSON-RPC 2.0 for messaging. Server-Sent Events (SSE) is a commonly supported transport mechanism for communication between MCP clients and remote servers \[11 (3.2)\].  
* **Nucleus Implication:** The "MCP Guide for.NET Developers" within the Nucleus project will require substantial updates. It must now focus on guiding developers on how to build robust MCP clients and potentially MCP servers using the new official C\# MCP SDK, all within the context of an M365 Agent. Security aspects like OAuth 2.1 for MCP interactions and leveraging Azure API Center for MCP server discovery and registration should also be covered.26

The widespread adoption of MCP across Microsoft platforms (Copilot Studio, Azure AI Foundry, Semantic Kernel, Windows, GitHub 23) means that for Nucleus agents to be effective and future-proof, embracing MCP for both consumption and exposure of capabilities will be critical.

### **7.3. The Pro-Code/Low-Code Bridge: M365 Agents SDK and Copilot Studio**

Microsoft is fostering a development environment where pro-code and low-code/no-code approaches to agent building are not mutually exclusive but complementary.

* **Distinct Tools for Different Audiences:** Copilot Studio caters to fusion teams and citizen developers, offering a visual, managed SaaS platform for rapid agent development without extensive coding.3 The M365 Agents SDK, on the other hand, is tailored for professional developers who require deep customization and control over the agent's architecture and logic \[3 (1.2)\].  
* **Interoperability Pathways:**  
  * **Skills in Copilot Studio:** Agents built with the M365 Agents SDK can be packaged and surfaced as "skills" within Copilot Studio.9 This allows low-code agents built in Copilot Studio to delegate complex tasks or access specialized functionalities provided by pro-code agents.  
  * **Multi-Agent Orchestration:** The multi-agent orchestration capabilities announced at Build 2025 further blur the lines, enabling agents built with different tools (M365 Agents SDK, Azure AI Agents Service, Microsoft Fabric, Copilot Studio) to collaborate towards shared goals.5  
* **Custom Engine Agents:** The M365 Agents SDK is a primary tool for constructing "custom engine agents." These agents provide developers with complete authority over orchestration logic, the choice of AI models, and data integrations. Such custom engine agents can then be deployed and surfaced within Microsoft 365 Copilot, extending its capabilities with bespoke functionalities \[3 (3.1), 11 (4.1)\].

This pro-code/low-code synergy allows organizations to leverage the strengths of different development approaches. Citizen developers can rapidly create agents for common tasks using Copilot Studio, while professional developers can use the M365 Agents SDK to build highly specialized, complex agents that can then be integrated into the broader agent ecosystem.

**Table 4: Agent Development Options within Microsoft Ecosystem (May 2025\)**

| Development Platform | Target Developer | Primary Use Cases | Orchestration Capabilities | AI Model Flexibility | Key Tooling |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **Microsoft 365 Agents SDK** | Professional Developers 3 | Building full-stack, multi-channel, custom engine agents for M365 Copilot, Teams, web, etc. Requiring deep customization and control.3 | Custom orchestration using Semantic Kernel, LangChain, Azure AI Foundry, or custom logic.3 | High (AI Agnostic) \- Supports any AI models or services (Azure OpenAI, third-party, custom).2 | M365 Agents Toolkit (VS, VS Code), Agents Playground,.NET/JS/Python SDKs.5 |
| **Teams AI Library** | Professional Developers (focused on Teams) 3 | Building collaborative AI agents specifically for Microsoft Teams channels and meetings; real-time user interaction within Teams.3 | Includes a built-in action planner orchestrator.3 | Supports GPT-based language models from Azure and OpenAI.3 | M365 Agents Toolkit (can be used for Teams AI Library projects).40 |
| **Copilot Studio** | Fusion Teams, Citizen Developers, Pro Developers (for extensions) 3 | Rapid development of conversational agents, extending M365 Copilot, automating workflows with Power Platform integration.3 | Visual workflow designer, can call "skills" (including those from M365 Agents SDK), multi-agent orchestration.5 | BYOM, Copilot Tuning (fine-tuning with company data), Azure AI Foundry model integration.5 | Web-based visual builder, Power Platform connectors.3 |
| **Azure AI Foundry SDK** | Professional Developers 12 | Building comprehensive AI applications and agents within Azure, fine-tuning models, evaluations, leveraging unified Azure AI SDK capabilities.12 | Multi-agent API, Assistants API, can integrate with Semantic Kernel for advanced orchestration.12 | High \- Access to a wide range of models in Azure AI, fine-tuning capabilities.22 | Azure AI SDK, Azure Portal, Azure AI Studio.22 |

This table clarifies the distinct roles and strengths of Microsoft's various agent development platforms, providing context for how the M365 Agents SDK fits into the overall strategy and aiding Nucleus in positioning its.NET components effectively.

## **8\. Strategic Roadmap for Nucleus: Adapting to the M365 Agents SDK**

The advent of the Microsoft 365 Agents SDK and the strategic direction revealed at Build 2025 necessitate a proactive and comprehensive adaptation strategy for the Nucleus open-source project. To maintain relevance and provide cutting-edge guidance for.NET developers, Nucleus must evolve its core components and documentation.

### **8.1. Modernizing the Nucleus Teams Adapter**

The Nucleus Teams Adapter, currently based on Bot Framework, requires a significant rewrite to align with the M365 Agents SDK for.NET.

* **Core Task:** Transition the adapter's foundation to the M365 Agents SDK.  
* **Key Architectural Changes:**  
  * Adopt the Microsoft.Agents.Hosting.AspNet.Core.IAgentHttpAdapter (or the confirmed.NET equivalent) for processing incoming HTTP requests from Teams and the AgentApplication class as the central point for agent logic.2  
  * Update activity handling to correctly parse Teams-specific payloads, especially for complex interactions like file attachments. This involves examining the Activity.Attachments array for file metadata 38 and potentially using turnContext.Activity.GetChannelData\<T\>() for other Teams-specific context.34 The adapter must also be able to handle file uploads initiated by the agent, following the consent card workflow.38  
  * Implement state management using the new ITurnState/StackState pattern for turn-specific data and leverage IStorage (with appropriate persistent providers for production) via the AgentState class for conversation and user state.8  
  * Refactor all authentication and authorization logic to align with M365 Agents SDK practices, likely involving IConnections and IAccessTokenProvider for secure communication with the Bot Service and other Microsoft services.8  
* **Goal:** Deliver a Nucleus Teams Adapter that is a showcase for best practices in building M365 Agents for Microsoft Teams, offering robust, feature-rich, and maintainable integration.

### **8.2. Reframing Secure Bot/Agent Deployment Practices**

The existing "Secure Bot Deployment" guide must be fundamentally updated to reflect the new security paradigms for AI agents, with a new title like "Secure Agent Deployment with Microsoft 365."

* **Core Task:** Center the guide on securing agents built with the M365 Agents SDK, with Microsoft Entra Agent ID as the cornerstone.  
* **Key Content Updates:**  
  * Provide detailed instructions on registering M365 Agents (particularly.NET agents deployed via Azure Bot Service) with Microsoft Entra to obtain an Agent ID.21  
  * Explain how to implement least-privilege access for agents using JIT scoped tokens requested via their Entra Agent ID.21  
  * Offer best practices for deploying M365 Agents to Azure, covering hosting (e.g., Azure App Service, Azure Functions) and continued use of Azure Bot Service for channel connectivity.11  
  * Integrate considerations for Microsoft Purview if agents handle sensitive data, discussing how Purview's data classification and protection policies apply.16  
  * Emphasize the application of Zero Trust principles (assume breach, verify explicitly, least privilege) throughout the agent design, development, and deployment lifecycle.16  
* **Goal:** Equip Nucleus users with comprehensive, actionable guidance for deploying M365 Agents that meet stringent enterprise security and compliance requirements, fully leveraging Microsoft's latest security infrastructure.

### **8.3. Advancing the MCP Guide for.NET Developers**

The "MCP Guide for.NET Developers" needs to be updated to reflect the availability of the official C\# SDK for MCP and its use within agents built using the M365 Agents SDK.

* **Core Task:** Focus the guide on practical implementation of MCP clients and servers using the C\# MCP SDK within the M365 Agents SDK framework.  
* **Key Content Enhancements:**  
  * Provide detailed code examples and patterns for how.NET M365 Agents can act as MCP clients, consuming tools, resources, and prompts from external MCP servers using the official C\# MCP client SDK \[26 (2.2)\].  
  * Explain and demonstrate how.NET M365 Agents can expose their own capabilities (e.g., custom logic, access to proprietary data) as MCP servers using the C\# MCP server SDK, making them available to other MCP-compliant clients.26  
  * Discuss the use of Azure API Management to transform existing.NET-based REST APIs into remote MCP servers, which can then be readily consumed by M365 Agents \[26 (3.2)\].  
  * Cover essential MCP security considerations, such as implementing OAuth 2.1 for authorization, and the role of Azure API Center in discovering and registering MCP servers within an enterprise \[26 (3.2)\].  
* **Goal:** Enable Nucleus.NET developers to build highly interoperable M365 Agents that can seamlessly participate in the burgeoning MCP ecosystem, both by consuming external capabilities and by securely exposing their own.

### **8.4. Nucleus Architectural Evolution in an Agent-First World**

Beyond updating specific reports, Nucleus should consider broader architectural evolution to fully capitalize on the agent-first strategy from Microsoft.

* **Embrace Multi-Agent Scenarios:** Design Nucleus agent components with an eye towards participation in multi-agent orchestrations.5 This could involve Nucleus agents collaborating with agents built in Copilot Studio or other specialized AI services to accomplish more complex enterprise tasks.  
* **Maximize AI Agnosticism:** Continue to champion the M365 Agents SDK's flexibility, providing patterns and guidance for integrating Nucleus agents with a diverse range of AI models (including open-source or third-party models) and custom orchestration logic, beyond just Microsoft's offerings.  
* **Strategic Channel Deployment:** Develop clear strategies and guidance for deploying Nucleus agents across the key channels supported by the M365 Agents SDK, with a primary focus on Microsoft Teams and Microsoft 365 Copilot, but also considering web and other potential channels.  
* **Foster Ecosystem Citizenship:** Architect Nucleus agents to be "good citizens" within the Microsoft agent ecosystem. This means paying close attention to discoverability (e.g., how Nucleus agents might be listed or found, potentially aligning with Agent Store concepts if applicable for custom enterprise agents), adhering to robust security practices centered on Entra Agent ID, and prioritizing interoperability through MCP.

By rapidly aligning its guidance and architecture with the M365 Agents SDK, Entra Agent ID, and MCP, the Nucleus open-source project has a significant opportunity. It can position itself as a vital resource and foundational framework for.NET developers aiming to build sophisticated, secure, and interoperable enterprise agents on Microsoft's new platform. As Microsoft heavily invests in this ecosystem, there will be a growing demand from the.NET community for best practices, reusable patterns, and clear guidance—a need that Nucleus is well-placed to fill.

The updates to the three Nucleus reports are not isolated endeavors but are deeply interconnected, mirroring the holistic nature of Microsoft's agent strategy. A secure Nucleus Teams Adapter (Report 1\) built with the M365 Agents SDK will depend on the principles of the Secure Agent Deployment guide (Report 2), leveraging Entra Agent ID. This same agent might then need to interact with data or tools via MCP, as detailed in the updated MCP Guide (Report 3). Therefore, a coordinated update process is essential to ensure consistency and reflect how these facets—channel integration, security, and data interoperability—converge in real-world M365 Agent development.

## **9\. Conclusion: Navigating the Future with Microsoft 365 Agents**

The introduction and rapid maturation of the Microsoft 365 Agents SDK, strongly reinforced by the announcements at Microsoft Build 2025, signify a pivotal moment in enterprise AI development. Microsoft is clearly moving beyond traditional bot frameworks towards a comprehensive ecosystem designed for building, deploying, securing, and orchestrating intelligent AI agents. This shift presents both immense opportunities and a learning curve for developers and organizations.

For the Nucleus open-source project, this transition is particularly salient. The M365 Agents SDK is now the definitive path for pro-code.NET agent development within the Microsoft ecosystem. Key takeaways for Nucleus include:

* **Embrace the Evolution:** The M365 Agents SDK is the successor to Bot Framework for building modern, AI-powered agents. Nucleus must fully align its guidance and components with this new SDK.  
* **Prioritize Security and Identity:** Microsoft Entra Agent ID is foundational to secure agent operation. Nucleus's security guidance must pivot to this identity-centric model.  
* **Champion Interoperability:** The Model Context Protocol (MCP) is key to enabling agents to connect with diverse data and tools. Nucleus should provide robust.NET guidance for MCP implementation.  
* **Leverage the Ecosystem:** Nucleus agents should be designed to integrate with and leverage the broader Microsoft AI landscape, including Azure AI Foundry, Semantic Kernel, and Copilot Studio.

The path forward for Nucleus involves a concerted effort to update its existing reports—the Teams Adapter, Secure Agent Deployment guide, and MCP Guide for.NET Developers—to reflect these new realities. This is not just a technical refresh but a strategic realignment to ensure Nucleus remains a valuable resource for.NET developers building next-generation enterprise solutions.

The AI agent landscape, especially within Microsoft's sphere, is characterized by rapid innovation.21 Features are moving from preview to GA, and new capabilities are continuously being introduced. This dynamic environment necessitates a commitment to continuous learning, experimentation, and adaptation for the Nucleus project. Static, one-time updates to guidance will quickly become obsolete. Instead, Nucleus must foster a culture of ongoing research and be prepared to iterate on its recommendations and architecture frequently to keep pace with Microsoft's evolving roadmap.

By embracing this new agent-centric paradigm and committing to ongoing adaptation, Nucleus can not only navigate the future but also play a significant role in shaping it for.NET developers building on Microsoft's powerful new platform for enterprise AI. The opportunity is to create more intelligent, integrated, and transformative solutions than ever before.

#### **Works cited**

1. Microsoft 365 Agents SDK documentation, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/)  
2. What is the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview)  
3. Custom engine agents for Microsoft 365 overview, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-custom-engine-agent](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-custom-engine-agent)  
4. Create and Deploy a Custom Engine Agent with Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk)  
5. Multi-agent orchestration and more: Copilot Studio announcements \- Microsoft, accessed May 23, 2025, [https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/](https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/multi-agent-orchestration-maker-controls-and-more-microsoft-copilot-studio-announcements-at-microsoft-build-2025/)  
6. microsoft/Agents-for-net: This repository is for active ... \- GitHub, accessed May 23, 2025, [https://github.com/microsoft/Agents-for-net](https://github.com/microsoft/Agents-for-net)  
7. Build 2025: Agents in Microsoft 365 announcements, accessed May 23, 2025, [https://techcommunity.microsoft.com/blog/microsoft365copilotblog/build-2025-agents-in-microsoft-365-announcements/4414281](https://techcommunity.microsoft.com/blog/microsoft365copilotblog/build-2025-agents-in-microsoft-365-announcements/4414281)  
8. Migration guidance from Azure Bot Framework SDK to the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance)  
9. Introducing the Microsoft 365 Agents SDK, accessed May 23, 2025, [https://devblogs.microsoft.com/microsoft365dev/introducing-the-microsoft-365-agents-sdk/](https://devblogs.microsoft.com/microsoft365dev/introducing-the-microsoft-365-agents-sdk/)  
10. Create a new .NET agent in Visual Studio using the Microsoft 365 Agents Toolkit, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-new-toolkit-project-vs](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-new-toolkit-project-vs)  
11. How agents work in the Microsoft 365 Agents SDK (preview), accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk)  
12. Choose the right agent solution to support your use case | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution)  
13. Deploy your agent to Azure and register with Azure Bot Service manually | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually)  
14. Introducing Microsoft 365 Copilot Tuning, multi-agent orchestration, and more from Microsoft Build 2025 | Microsoft 365 Blog, accessed May 23, 2025, [https://www.microsoft.com/en-us/microsoft-365/blog/2025/05/19/introducing-microsoft-365-copilot-tuning-multi-agent-orchestration-and-more-from-microsoft-build-2025/](https://www.microsoft.com/en-us/microsoft-365/blog/2025/05/19/introducing-microsoft-365-copilot-tuning-multi-agent-orchestration-and-more-from-microsoft-build-2025/)  
15. What's is Microsoft 365 Agents SDK and the Evolution of the Bot Framework \#bot \#agent \#m365 \- YouTube, accessed May 23, 2025, [https://www.youtube.com/watch?v=LdjiSEb4CPA](https://www.youtube.com/watch?v=LdjiSEb4CPA)  
16. Microsoft extends Zero Trust to secure the agentic workforce, accessed May 23, 2025, [https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/](https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/)  
17. Quickstart: Create an agent with the Agents SDK \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent)  
18. Building agents with Agents SDK | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/building-agents](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/building-agents)  
19. AgentState(IStorage, String) Constructor (Microsoft.Agents.Builder.State), accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.state.agentstate.-ctor?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.state.agentstate.-ctor?view=m365-agents-sdk)  
20. CloudAdapter class | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/javascript/api/@microsoft/agents-hosting/cloudadapter?view=agents-sdk-js-latest](https://learn.microsoft.com/en-us/javascript/api/@microsoft/agents-hosting/cloudadapter?view=agents-sdk-js-latest)  
21. Announcing Microsoft Entra Agent ID: Secure and manage your AI agents, accessed May 23, 2025, [https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392](https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392)  
22. Azure AI Foundry: Your AI App and agent factory | Microsoft Azure Blog, accessed May 23, 2025, [https://azure.microsoft.com/en-us/blog/azure-ai-foundry-your-ai-app-and-agent-factory/](https://azure.microsoft.com/en-us/blog/azure-ai-foundry-your-ai-app-and-agent-factory/)  
23. Microsoft Makes Major Push Into AI Agent Interoperability with New MCP Rollouts, accessed May 23, 2025, [https://cloudwars.com/ai/microsoft-makes-major-push-into-ai-agent-interoperability-with-new-mcp-rollouts/](https://cloudwars.com/ai/microsoft-makes-major-push-into-ai-agent-interoperability-with-new-mcp-rollouts/)  
24. Extend your agent with Model Context Protocol (preview) \- Microsoft Copilot Studio, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp)  
25. Register and discover remote MCP servers in your API inventory \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/azure/api-center/register-discover-mcp-server](https://learn.microsoft.com/en-us/azure/api-center/register-discover-mcp-server)  
26. Connect Once, Integrate Anywhere with MCPs \- Microsoft Developer Blogs, accessed May 23, 2025, [https://devblogs.microsoft.com/blog/connect-once-integrate-anywhere-with-mcps](https://devblogs.microsoft.com/blog/connect-once-integrate-anywhere-with-mcps)  
27. Building agents for Microsoft 365 Copilot | BRK165 \- YouTube, accessed May 23, 2025, [https://www.youtube.com/watch?v=NVyTAE-6xCY](https://www.youtube.com/watch?v=NVyTAE-6xCY)  
28. Debug bot using Agents Playground \- Teams | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/debug-your-agents-playground](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/debug-your-agents-playground)  
29. Using activities | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/using-activities](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/using-activities)  
30. Send proactive messages \- Teams | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages)  
31. Proactive Messaging (C\#) \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/teams-ai-library/csharp/essentials/sending-messages/proactive-messaging](https://learn.microsoft.com/en-us/microsoftteams/platform/teams-ai-library/csharp/essentials/sending-messages/proactive-messaging)  
32. Send proactive notifications to users \- Bot Service \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-howto-proactive-message?view=azure-bot-service-4.0)  
33. Activity.GetConversationReference Method (Microsoft.Agents.Core.Models), accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.activity.getconversationreference?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.activity.getconversationreference?view=m365-agents-sdk)  
34. Activity Class (Microsoft.Agents.Core.Models), accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.activity?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.activity?view=m365-agents-sdk)  
35. IActivity.ApplyConversationReference(ConversationReference, Boolean) Method (Microsoft.Agents.Core.Models), accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.iactivity.applyconversationreference?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.core.models.iactivity.applyconversationreference?view=m365-agents-sdk)  
36. Asynchronous programming scenarios \- C\# \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)  
37. Long-running operations in the Azure SDK for Java \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/azure/developer/java/sdk/lro](https://learn.microsoft.com/en-us/azure/developer/java/sdk/lro)  
38. Send and receive files from a bot \- Teams | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/resources/bot-v3/bots-files](https://learn.microsoft.com/en-us/microsoftteams/platform/resources/bot-v3/bots-files)  
39. @microsoft/agents-hosting-teams package | Microsoft Learn, accessed May 23, 2025, [https://learn.microsoft.com/en-us/javascript/api/@microsoft/agents-hosting-teams/?view=agents-sdk-js-latest](https://learn.microsoft.com/en-us/javascript/api/@microsoft/agents-hosting-teams/?view=agents-sdk-js-latest)  
40. Build an AI agent bot in Teams \- Learn Microsoft, accessed May 23, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams)