# **Project Nucleus: Advanced Implementation Details for Microsoft 365 Agents SDK and MCP Pivot (May 2025\)**

## **1\. Introduction**

Project Nucleus aims to leverage the Microsoft 365 Agents SDK to create sophisticated, persona-driven AI agents integrated within the Microsoft 365 ecosystem. This report details advanced implementation strategies, building upon prior research establishing the 'one persona per agent' model, the feasibility of basic non-Azure Large Language Model (LLM) integration, and the use of Azure Key Vault for secrets management. The focus here is on resolving specific, advanced technical challenges critical for the successful deployment and operation of these agents. Key areas of investigation include enabling proactive messaging capabilities, managing complex configuration landscapes, integrating diverse non-Azure LLMs with robust tool calling and streaming, defining secure and scalable multi-tenant ISV patterns, effectively utilizing the Model Context Protocol (MCP) for backend capabilities, deploying with.NET Aspire to Azure, and establishing definitive network isolation architectures. This document provides concrete examples, definitive patterns, and strategic recommendations to guide Project Nucleus through these advanced implementation phases.

## **2\. Proactive Messaging with Microsoft 365 Agents SDK**

Proactive messaging, where an agent initiates conversation without direct user input, is a critical capability for many advanced agent scenarios, such as notifications, alerts, or follow-ups. The Microsoft 365 Agents SDK, an evolution of the Azure Bot Framework SDK 1, provides mechanisms to achieve this, primarily through the ChannelAdapter and its ContinueConversationAsync method.

### **2.1. Understanding ChannelAdapter.ContinueConversationAsync**

The ChannelAdapter (formerly BotAdapter in Bot Framework SDK 1) is a key component for facilitating communication between the agent and various channels. The ContinueConversationAsync method is specifically designed to send proactive messages.2 This method allows an agent to resume a conversation using a previously saved ConversationReference. Most channels require a user to initiate a conversation with an agent before the agent can send proactive messages.3

The ContinueConversationAsync method has several overloads, typically requiring:

* A ClaimsIdentity or AppId to authenticate the bot.  
* A ConversationReference object, which contains details of the conversation to resume (e.g., user ID, conversation ID, service URL). This reference must be stored from a previous interaction with the user.  
* An AgentCallbackHandler (a delegate) which is the method that will be invoked in the context of the resumed conversation, allowing the agent to send one or more activities.  
* A CancellationToken.2

For example, a simplified conceptual signature might look like:  
Task ContinueConversationAsync(string botAppId, ConversationReference conversationReference, AgentCallbackHandler callback, CancellationToken cancellationToken)

### **2.2. Proactive Messaging from Background Services (e.g., Azure Functions)**

Sending proactive messages often originates from a background service, such as an Azure Function, triggered by an external event (e.g., a database update, a message on a queue).

Dependency Injection (DI) Setup:  
To use ChannelAdapter.ContinueConversationAsync from an Azure Function or similar background service, the necessary components must be resolved via Dependency Injection.

1. **ChannelAdapter (IChannelAdapter or IBotHttpAdapter):** The specific adapter implementation (e.g., ChannelAdapter derived from ChannelServiceAdapterBase 1) needs to be registered and resolved. In an ASP.NET Core hosted agent, this is typically handled by builder.AddAgent(...) and related DI extensions.4 For a background service, a similar DI setup is required.  
   * The IAgentHttpAdapter (formerly IBotFrameworkHttpAdapter 1) is responsible for processing HTTP requests but ContinueConversationAsync is a method on the adapter itself, allowing it to initiate outbound communication.  
2. **Authentication Components:**  
   * **IAccessTokenProvider:** The M365 Agents SDK has moved away from manual AppCredentials creation towards IAccessTokenProvider and Microsoft.Agents.Authentication.Msal for acquiring tokens for outgoing requests.1 This provider needs to be configured and registered in the DI container.  
   * **RestChannelServiceClientFactory:** This factory, which replaces ConfigurationBotFrameworkAuthentication 1, is likely involved in creating clients for communicating with the Bot Connector service. It would also need to be available via DI.  
   * **Configuration (IConfiguration):** Bot App ID, App Password (Client Secret), and other necessary settings must be available through IConfiguration, ideally sourced securely (e.g., Azure App Configuration and Key Vault).

**Conceptual Azure Function Setup for Proactive Messaging:**

C\#

// In Azure Function Startup.cs or Program.cs (for isolated worker)  
public override void Configure(IFunctionsHostBuilder builder)  
{  
    // 1\. Add IConfiguration  
    var configuration \= new ConfigurationBuilder()  
       .SetBasePath(Environment.CurrentDirectory)  
       .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)  
       .AddEnvironmentVariables()  
       .Build();  
    builder.Services.AddSingleton\<IConfiguration\>(configuration);

    // 2\. Register M365 Agents SDK services  
    // This is conceptual and depends on actual SDK DI extensions for background services.  
    // May require registering IBotHttpAdapter/ChannelAdapter, IAccessTokenProvider,  
    // RestChannelServiceClientFactory, and AgentApplicationOptions.  
    // Example: builder.Services.AddSingleton\<IAccessTokenProvider\>(sp \=\> new MsalAccessTokenProvider(configuration));  
    // Example: builder.Services.AddSingleton\<IBotHttpAdapter\>(sp \=\>  
    // {  
    //     var options \= sp.GetRequiredService\<IOptions\<AgentApplicationOptions\>\>().Value; // Assuming options are registered  
    //     var tokenProvider \= sp.GetRequiredService\<IAccessTokenProvider\>();  
    //     var clientFactory \= sp.GetRequiredService\<RestChannelServiceClientFactory\>(); // May need specific config  
    //     // Simplified: Actual adapter construction might be more complex or via a specific SDK method  
    //     return new YourSpecificChannelAdapterImplementation(configuration, tokenProvider, clientFactory, options.AppId);  
    // });  
    // Actual SDK registration for background services needs to be confirmed from SDK samples/docs.  
    // The standard ASP.NET Core hosting setup \[4\] uses builder.AddAgentApplicationOptions() and builder.AddAgent().  
    // An equivalent for non-hosted services like Azure Functions is needed.  
}

// In the Azure Function class  
public class ProactiveMessageFunction  
{  
    private readonly IBotHttpAdapter \_adapter; // Or IChannelAdapter  
    private readonly string \_botAppId;  
    private readonly IStorage \_conversationReferenceStore; // Example: To store/retrieve ConversationReference

    public ProactiveMessageFunction(IBotHttpAdapter adapter, IConfiguration configuration, IStorage conversationReferenceStore)  
    {  
        \_adapter \= adapter;  
        \_botAppId \= configuration\["MicrosoftAppId"\]; // Ensure this is configured  
        \_conversationReferenceStore \= conversationReferenceStore;  
    }

     
    public async Task Run( string eventData, ILogger log)  
    {  
        log.LogInformation($"C\# Queue trigger function processed: {eventData}");

        // 1\. Deserialize eventData to get target user/conversation details  
        // 2\. Retrieve the stored ConversationReference for the target  
        //    ConversationReference conversationReference \= await \_conversationReferenceStore.GetAsync(targetUserId);  
        //    if (conversationReference \== null) { log.LogError("ConversationReference not found."); return; }

        // Conceptual ConversationReference  
        var conversationReference \= new ConversationReference  
        {  
            Conversation \= new ConversationAccount(id: "some-conversation-id"),  
            User \= new ChannelAccount(id: "user-id-to-notify"),  
            Bot \= new ChannelAccount(id: \_botAppId, name: "MyAgent"),  
            ServiceUrl \= "https://smba.trafficmanager.net/amer/", // Example, get from stored reference  
            ChannelId \= "msteams" // Example  
        };

        // 3\. Define the callback that sends the message  
        async Task AgentCallback(ITurnContext turnContext, CancellationToken cancellationToken)  
        {  
            await turnContext.SendActivityAsync(MessageFactory.Text("This is a proactive message\!"), cancellationToken);  
        }

        // 4\. Call ContinueConversationAsync  
        // The first parameter (AppId or ClaimsIdentity) is crucial for authentication.  
        // If using AppId/Password, the adapter needs to be configured with these credentials.  
        // If using Managed Identity, IAccessTokenProvider handles token acquisition.  
        // The exact method signature variant to use depends on SDK specifics and auth setup.  
        // One overload takes botAppId directly \[2, 3\]  
        try  
        {  
            // Assuming \_adapter is correctly configured with credentials/auth provider  
            await ((ChannelAdapter)\_adapter).ContinueConversationAsync(  
                \_botAppId,  
                conversationReference,  
                AgentCallback,  
                CancellationToken.None); // Pass appropriate token  
            log.LogInformation("Proactive message sent successfully.");  
        }  
        catch (Exception ex)  
        {  
            log.LogError($"Error sending proactive message: {ex.Message}");  
            // Handle exceptions, e.g., authentication failures, invalid conversation reference  
        }  
    }  
}

Authentication in a Non-Request Context:  
When an agent responds to an incoming user message, authentication is typically handled as part of the request pipeline. In a background service, the ChannelAdapter must still authenticate to the Bot Connector Service to send a message.

* The IAccessTokenProvider (e.g., using MSAL with app credentials or Managed Identity) is responsible for obtaining the necessary OAuth token.  
* The ChannelAdapter (or the underlying RestChannelServiceClientFactory) uses this token when making calls to the ServiceUrl specified in the ConversationReference.  
* It is critical that the Azure Function (or other background service) has its configuration (App ID, secret, or Managed Identity permissions) correctly set up for the IAccessTokenProvider to function.

The M365 Agents SDK facilitates communication between clients and agents, supporting various channels like Microsoft Teams.5 The SDK allows building agents hosted on Azure Bot Service, defining a REST API and activity protocol.5 Interactions involve exchanging activities within turns, which are fundamental units of work.5 Storing ConversationReference is key for enabling proactive messages.8 While GitHub samples for proactive messaging in the new M365 Agents SDK context are evolving 11, patterns from Bot Framework SDK often provide foundational concepts.

A significant consideration is the evolution from the Azure Bot Framework SDK. Token authentication, previously involving manual AppCredentials creation, is now handled via IAccessTokenProvider and Microsoft.Agents.Authentication.Msal. ConfigurationBotFrameworkAuthentication has been replaced with RestChannelServiceClientFactory.1 These changes simplify authentication but require careful DI setup in decoupled scenarios like Azure Functions. The ChannelAdapter itself, replacing BotAdapter, is central to this.1

The DI setup within an Azure Function needs to replicate the necessary services that are typically available in an ASP.NET Core hosted environment. This includes IConfiguration for settings, an IAccessTokenProvider for Bot Connector authentication, and the ChannelAdapter itself. The AgentApplicationOptions would also need to be configured and made available. If ChannelAdapter instances are created directly, their dependencies (like ICredentialProvider or BotFrameworkAuthentication) must be satisfied. The AddAgentApplicationOptions() and builder.AddAgent() methods 4 in ASP.NET Core hosting handle much of this registration. An equivalent streamlined setup for Azure Functions would be beneficial, potentially through dedicated SDK extension methods or clear guidance on manual registration. Without direct SDK samples for Azure Functions, one might need to inspect the source code of AddAgent or related hosting extensions to understand the minimal required services and their configurations.

## **3\. Complex Configuration Loading for M365 Agents**

M365 Agents, especially those with multiple personas and integrations, require a robust and flexible configuration system. This involves managing structured configuration data, handling sensitive values securely, and potentially leveraging cloud-native configuration services. ASP.NET Core's configuration system, combined with Azure App Configuration and Azure Key Vault, provides a powerful solution, further streamlined by.NET Aspire for Azure deployments.

### **3.1. Binding Nested JSON to IOptions\<T\> with Azure App Configuration and Key Vault**

A common pattern for managing complex configurations in.NET is to use strongly-typed C\# classes (e.g., PersonaConfiguration, LlmSettings) that mirror the structure of the JSON configuration. ASP.NET Core's IOptions\<T\> pattern is then used to bind configuration sections to these classes, making them available via Dependency Injection.12

* **Hierarchical Configuration:** Configuration keys in appsettings.json or Azure App Configuration are typically represented with colons to denote hierarchy (e.g., ProjectNucleus:PersonaConfiguration:EduFlowAgent:LlmConfig:EndpointUrl).13  
* **Azure App Configuration:** This service centralizes application configuration.13 It can load settings from various sources, including JSON content.  
* **Azure Key Vault Integration:** Sensitive values like API keys or connection strings should be stored in Azure Key Vault.13 Azure App Configuration can reference secrets stored in Key Vault.15 The application reads a Key Vault reference from App Configuration, and the App Configuration provider (with appropriate permissions, typically via Managed Identity) resolves the actual secret value from Key Vault.  
* **Key Vault Secret Naming for Hierarchy:** When Key Vault secrets represent nested configuration values, the \-- (double dash) convention must be used in the Key Vault secret name (e.g., ProjectNucleus--PersonaConfiguration--EduFlowAgent--LlmConfig--ApiKey). The Azure Key Vault configuration provider automatically translates these double dashes into colons, fitting into the ASP.NET Core hierarchical configuration system.16

**Conceptual PersonaConfiguration Structure and Resolution:**

Consider a scenario where PersonaConfiguration includes details for multiple personas, each potentially with its own LLM settings, and API keys are stored in Key Vault.

* **In Azure App Configuration (example key-value):**  
  * Key: ProjectNucleus:PersonaConfiguration:EduFlowAgent  
  * Value (JSON):  
    JSON  
    {  
      "DisplayName": "EduFlow Agent for Education",  
      "DefaultPrompt": "You are an assistant for educators...",  
      "LlmConfigName": "EduLlm",  
      "EnabledFeatures":,  
      "WelcomeMessage": "Hello\! How can I help with your educational tasks today?"  
    }

  * Key: ProjectNucleus:LlmEndpoints:EduLlm  
  * Value (JSON):  
    JSON  
    {  
      "ApiUrl": "https://api.example-edu-llm.com/v1/chat",  
      "ApiKeySecretName": "EduLlmApiKey", // Name of the secret in Key Vault  
      "ModelName": "edu-model-v2"  
    }

  * Key: ProjectNucleus:LlmEndpoints:EduLlm:ApiKey  
  * Value (Key Vault Reference): {"uri":"https://mykeyvault.vault.azure.net/secrets/EduLlmApiKey"} (This is how App Config stores a KV reference)  
* **In Azure Key Vault:**  
  * Secret Name: EduLlmApiKey  
  * Secret Value: actual\_api\_key\_for\_edu\_llm  
* **C\# Model Classes:**  
  C\#  
  public class ProjectNucleusSettings  
  {  
      public Dictionary\<string, PersonaConfig\> PersonaConfiguration { get; set; }  
      public Dictionary\<string, LlmEndpointConfig\> LlmEndpoints { get; set; }  
  }

  public class PersonaConfig  
  {  
      public string DisplayName { get; set; }  
      public string DefaultPrompt { get; set; }  
      public string LlmConfigName { get; set; }  
      public List\<string\> EnabledFeatures { get; set; }  
      public string WelcomeMessage { get; set; } // New property  
  }

  public class LlmEndpointConfig  
  {  
      public string ApiUrl { get; set; }  
      public string ApiKey { get; set; } // This will be populated from Key Vault  
      public string ModelName { get; set; }  
      public string ApiKeySecretName {get; set;} // Could be used to dynamically form KV ref if not using AppConfig's direct KV ref feature  
  }

**Configuration Provider Setup in Program.cs:**

C\#

public static IHostBuilder CreateHostBuilder(string args) \=\>  
    Host.CreateDefaultBuilder(args)  
       .ConfigureAppConfiguration((hostingContext, config) \=\>  
        {  
            var settings \= config.Build();  
            var appConfigConnectionString \= settings.GetConnectionString("AppConfig"); // Connection string to App Configuration  
            if (\!string.IsNullOrEmpty(appConfigConnectionString))  
            {  
                config.AddAzureAppConfiguration(options \=\>  
                {  
                    options.Connect(appConfigConnectionString)  
                          .ConfigureKeyVault(kv \=\> // Configure Key Vault resolution  
                           {  
                               // Use DefaultAzureCredential for Managed Identity access to Key Vault  
                               kv.SetCredential(new DefaultAzureCredential());  
                           })  
                           // Load configuration values prefixed with "ProjectNucleus"  
                          .Select("ProjectNucleus:\*", LabelFilter.Null)  
                           // Configure refresh for dynamic updates  
                          .ConfigureRefresh(refresh \=\>  
                           {  
                               refresh.Register("ProjectNucleus:Sentinel", refreshAll: true)  
                                     .SetCacheExpiration(TimeSpan.FromMinutes(5));  
                           });  
                    // Expose IConfigurationRefresher for dynamic refresh  
                    hostingContext.Properties \= options.GetRefresher();  
                });  
            }  
            // Key Vault provider can also be added directly if not using App Config's KV reference feature  
            // var keyVaultEndpoint \= settings\["KeyVaultEndpoint"\];  
            // if (\!string.IsNullOrEmpty(keyVaultEndpoint))  
            // {  
            //     config.AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential());  
            // }  
        })  
       .ConfigureServices((hostContext, services) \=\>  
        {  
            // Bind the entire "ProjectNucleus" section to ProjectNucleusSettings  
            services.Configure\<ProjectNucleusSettings\>(hostContext.Configuration.GetSection("ProjectNucleus"));

            // Make IConfigurationRefresher available for injection if dynamic refresh is needed  
            if (hostContext.Properties.TryGetValue("AzureAppConfigurationRefresher", out var refresher))  
            {  
                services.AddSingleton(refresher as IConfigurationRefresher);  
            }

            // Register other services for the M365 Agent  
            // services.AddHttpClient();  
            // services.AddSingleton\<IStorage, MemoryStorage\>(); // Example from \[4\]  
            // builder.AddAgentApplicationOptions(); // From M365 Agents SDK  
            // builder.AddAgent(...); // From M365 Agents SDK  
        });

In this setup, AddAzureAppConfiguration connects to Azure App Configuration. ConfigureKeyVault with SetCredential(new DefaultAzureCredential()) enables the App Configuration provider to resolve Key Vault references using the application's Managed Identity.15 When services.Configure\<ProjectNucleusSettings\>(...) is called, the configuration system binds the values. If a value in App Configuration is a Key Vault reference (like ProjectNucleus:LlmEndpoints:EduLlm:ApiKey), the provider automatically fetches the secret from Key Vault and injects it into the LlmEndpointConfig.ApiKey property.

### **3.2. Leveraging.NET Aspire for Configuration in Azure Deployments**

.NET Aspire simplifies the setup and management of configuration for distributed applications, both locally and when deployed to Azure.20

* **Hosting Integration:** In the.NET Aspire AppHost project, developers can add Azure App Configuration and Azure Key Vault as resources:  
  C\#  
  // In AppHost/Program.cs  
  var builder \= DistributedApplication.CreateBuilder(args);

  var appConfig \= builder.AddAzureAppConfiguration("appconfigstore"); // \[20\]  
  var keyVault \= builder.AddAzureKeyVault("keyvault"); // \[21\]

  builder.AddProject\<Projects.MyM365AgentService\>("m365agentservice")  
        .WithReference(appConfig)  
        .WithReference(keyVault);

* **Automatic Configuration Injection:** Aspire's WithReference method facilitates the injection of necessary connection information (like endpoints or connection strings) into the client project (MyM365AgentService) as environment variables. The client project's Program.cs would then use these environment variables to connect to App Configuration and Key Vault, typically using DefaultAzureCredential which seamlessly works with Managed Identities in Azure.  
* **Local Development vs. Azure Deployment:**  
  * **Local:** Aspire can use local user secrets for connection strings to development instances of App Configuration/Key Vault, or even emulators if available (though App Config doesn't have a standard emulator, Key Vault access can be local if secrets are seeded). The azd tool can provision these Azure resources for local development if they don't exist.20  
  * **Azure:** When deployed to Azure (e.g., Azure Container Apps), Aspire and azd ensure that the application containers are configured with the correct environment variables to connect to the provisioned Azure App Configuration and Key Vault instances, using Managed Identities for authentication.22 Aspire generates Bicep templates for provisioning these resources.20  
* **IOptions\<T\> with Aspire:** The IOptions\<T\> pattern within the M365 Agent service project remains the standard way to consume strongly-typed configuration..NET Aspire's role is to ensure that the IConfiguration instance, from which IOptions\<T\> is populated, is correctly built from Azure App Configuration and Key Vault, whether running locally or in a deployed Azure environment.

### **3.3. State Management Configuration**

The M365 Agents SDK supports building agent containers with state and storage capabilities.7 Configuration for the chosen storage provider (e.g., an Azure Cosmos DB connection string) would also be managed through this hierarchical configuration system. Sensitive parts, such as the Cosmos DB primary key, should be stored in Azure Key Vault and referenced via Azure App Configuration..NET Aspire can also manage references to Azure Cosmos DB resources, injecting connection strings securely.24

### **3.4. Considerations for Complex Configuration Management**

Managing complex configurations like PersonaConfiguration introduces challenges beyond simple key-value pairs, particularly in evolving systems.

One critical aspect is **configuration schema versioning and evolution**. As Project Nucleus develops, with its 'one persona per agent' model, the structure of PersonaConfiguration and other related configuration objects will inevitably change. New features will demand new configuration properties, existing ones might be refactored, or their data types might change. If these configurations are loaded via IOptions\<T\>, any breaking changes in the JSON structure stored in Azure App Configuration (e.g., removing or renaming a property that the C\# class expects, or altering its type) can lead to deserialization errors at application startup or runtime, or result in unexpected behavior if default values are not handled gracefully. While adding new, optional properties is generally safe, more substantial changes require a deliberate strategy. This might involve versioning configuration sections within App Configuration, employing tolerant JSON deserialization settings in the.NET application, or implementing custom IConfigureOptions\<T\> classes. These custom configurators could handle transformations, provide sensible defaults for missing keys, or manage migrations from older configuration structures, ensuring backward compatibility or at least a controlled failure mode. Azure App Configuration offers features like snapshots and labeling, which could be explored as part of a robust versioning strategy for configuration data, allowing an agent to potentially load a specific "version" of its configuration if needed.

Another important consideration is **dynamic configuration refresh and its impact on agent behavior**. Azure App Configuration supports the dynamic refresh of configuration values without requiring an application restart, and Key Vault references can also be refreshed.15 For an M365 Agent, this capability is highly valuable. Certain configurations, such as feature flags for a specific persona, prompt templates, or even an LLM API key (in case of a compromise and rotation), might need to be updated on-the-fly. The IOptionsMonitor\<T\> interface in ASP.NET Core is designed for this, allowing application components to react to such configuration changes.12 If an agent's operational logic is heavily reliant on PersonaConfiguration, any modifications to this configuration should ideally reflect in the agent's behavior without a full redeployment cycle. However, not all components within an agent may be designed for dynamic reconfiguration. For instance, an LLM client might be initialized once with specific settings like an API key. Therefore, a careful design is needed to identify which parts of the agent's configuration are suitable for dynamic refresh and how to implement this safely. This involves using IOptionsMonitor\<T\> and ensuring that relevant agent components can re-initialize or adapt their behavior based on the updated configuration. For highly sensitive changes, such as API keys, the refresh mechanism must be robust and secure, ensuring that the new key is correctly applied and old instances are appropriately handled. The interaction of.NET Aspire's configuration management with IOptionsMonitor\<T\> and the dynamic refresh capabilities of Azure App Configuration and Key Vault in a deployed Azure environment is an area that warrants specific attention to ensure seamless updates.

The following table outlines a conceptual strategy for loading a complex PersonaConfiguration object:

**Table 1: PersonaConfiguration Loading Strategy**

| Configuration Aspect | Storage Location (App Config Key / KV Secret Name) | .NET IOptions\<T\> Path (Example) | Resolution Mechanism |
| :---- | :---- | :---- | :---- |
| Persona Display Name (EduFlowAgent) | ProjectNucleus:PersonaConfiguration:EduFlowAgent:DisplayName (in App Config) | ProjectNucleusSettings.PersonaConfiguration\["EduFlowAgent"\].DisplayName | Direct binding from App Configuration. |
| Persona Default Prompt (EduFlowAgent) | ProjectNucleus:PersonaConfiguration:EduFlowAgent:DefaultPrompt (in App Config) | ProjectNucleusSettings.PersonaConfiguration\["EduFlowAgent"\].DefaultPrompt | Direct binding from App Configuration. |
| LLM API Key (for EduLlm) | KV Secret: EduLlmApiKey (Referenced by App Config key ProjectNucleus:LlmEndpoints:EduLlm:ApiKey) | ProjectNucleusSettings.LlmEndpoints\["EduLlm"\].ApiKey | App Configuration resolves Key Vault reference using Managed Identity. Value bound to IOptions\<T\>. |
| LLM API URL (for EduLlm) | ProjectNucleus:LlmEndpoints:EduLlm:ApiUrl (in App Config) | ProjectNucleusSettings.LlmEndpoints\["EduLlm"\].ApiUrl | Direct binding from App Configuration. |
| Feature Flag (e.g., DocumentAnalysis) | ProjectNucleus:PersonaConfiguration:EduFlowAgent:EnabledFeatures (Array in App Config) | ProjectNucleusSettings.PersonaConfiguration\["EduFlowAgent"\].EnabledFeatures | Direct binding from App Configuration. Feature flags can also be managed by Azure App Configuration's dedicated feature management capabilities. |
| Storage Connection String (e.g., Cosmos) | KV Secret: AgentStateCosmosDbConnectionString (Referenced by App Config key ProjectNucleus:StateStore:ConnectionString) | hostContext.Configuration.GetConnectionString("AgentStateStore") | App Configuration resolves Key Vault reference. IStorage implementation would use this. |

This structured approach, combining Azure App Configuration for general settings and Key Vault for sensitive data, bound to strongly-typed IOptions\<T\> objects, provides a secure, manageable, and scalable configuration solution for Project Nucleus..NET Aspire further enhances this by simplifying the resource setup and connection management in both local and Azure environments.

## **4\. Advanced Integration of Non-Azure Large Language Models**

Project Nucleus's requirement to potentially integrate non-Azure LLMs, such as Google Gemini or models accessed via OpenRouter.AI, necessitates a robust strategy that leverages the M365 Agents SDK's inherent flexibility while addressing specific compatibility challenges, particularly around tool calling and streaming.

### **4.1. Bridging M365 Agents SDK with External LLMs**

The Microsoft 365 Agents SDK is intentionally designed to be LLM-agnostic, providing developers the freedom to choose their preferred AI services and models.1 This foundational principle is key to integrating external LLMs.

* **Semantic Kernel as an Orchestration Layer:** Semantic Kernel emerges as a powerful orchestrator within an M365 Agent.1 It supports a variety of LLMs and can abstract many ofr the differences between them, simplifying the integration of models like Gemini or those available through OpenRouter.AI.30 The Semantic Kernel Agent Framework further allows for the creation of modular AI components that can collaborate, aligning well with the multi-persona vision of Project Nucleus.30 Its abstractions for chat completion and function calling are particularly relevant.31  
* **Microsoft.Extensions.AI.IChatClient Abstraction:** The Microsoft.Extensions.AI.IChatClient interface offers a higher-level abstraction for chat interactions with LLMs.32 If a provider (e.g., a community or official wrapper for Gemini or OpenRouter.AI) implements this interface, it can significantly streamline the integration process into an M365 Agent. The library also includes a FunctionInvokingChatClient, which aids in the automatic invocation of tools defined by the developer.32

### **4.2. Tool Calling and Streaming: Addressing Compatibility and Nuances**

Tool calling (also known as function calling) and response streaming are advanced LLM features crucial for building interactive and capable agents. However, their implementation varies across LLM providers.

* **Schema Compatibility and Normalization:**  
  * A primary challenge lies in the differing JSON schemas and conventions LLM providers use for defining tools/functions and for formatting tool call requests and responses. For example, Google Gemini has its own schema and requirements 33, while OpenAI has its established format, which OpenRouter.AI generally aims to be compatible with.35 Despite standardization efforts by aggregators like OpenRouter.AI, provider-specific behaviors and limitations can persist.39  
  * Abstraction layers like Semantic Kernel and IChatClient attempt to normalize these differences. Semantic Kernel, for instance, can handle variations like function name delimiters required by Gemini.31 Similarly, Microsoft.Extensions.AI.IChatClient aims to provide a consistent interface for tool definition and invocation.32  
  * Despite these abstractions, developers may still need to be cognizant of the underlying LLM's native capabilities and limitations. For example, Gemini's OpenAPI specification has known limitations that might affect how tools are defined or how responses are structured.34 The actions.json file pattern, seen in the Teams AI library for defining tool schemas that the SDK uses to augment prompts 40, could be an analogous approach for M365 Agents to declare tools in a way that the chosen orchestrator or LLM can understand.  
* **Ensuring Robust Streaming Support:**  
  * Real-time, streamed responses are essential for a good user experience. LLM providers vary in their support for streaming, especially when tool calls are part of the interaction sequence.  
  * OpenRouter.AI, for example, supports streaming for many, but not all, of its aggregated models.41 It also employs specific mechanisms like sending keep-alive comments during Server-Sent Events (SSE) streams to prevent timeouts, which the client-side handling must accommodate.41  
  * Both Semantic Kernel and any IChatClient implementation must be capable of correctly processing these streams, including any provider-specific artifacts, and propagating the streamed content effectively to the M365 Agent and ultimately to the end-user interface.

### **4.3. Specific Challenges and Considerations for Google Gemini and OpenRouter.AI**

* **Google Gemini:**  
  * **Tool Calling:** Gemini possesses its own function calling API and schema.33 While Google also offers an OpenAI-compatible endpoint for Gemini to ease migration 38, subtle discrepancies can arise. For instance, Gemini might return a "model" role in its response messages, whereas SDKs built for OpenAI might strictly expect an "assistant" role, potentially causing deserialization or validation errors if not handled.42 Furthermore, the extent of Gemini's tool schema support when accessed via an OpenAI-compatible layer might be more constrained than its native API.34  
  * **Streaming:** The Gemini API, including the Gemini Live API, supports low-latency bidirectional streaming for interactive use cases.43  
  * **Rate Limiting:** Developers must be mindful of Gemini API's rate limits. The free tier of Gemini 1.5 Pro, for example, has a low request-per-minute (RPM) limit, and exceeding this can result in HTTP 429 (Too Many Requests) errors.44 Proper error handling and potentially a quota management strategy are necessary.  
  * **.NET SDKs:** Google provides its own Google.Ai.Generativelanguage.NET SDK for interacting with Gemini models.46 Project Nucleus could use this SDK directly or seek/develop an IChatClient wrapper around it for more standardized integration.  
* **OpenRouter.AI:**  
  * **Unified API Access:** OpenRouter.AI acts as an aggregator, providing a single API endpoint (often OpenAI-compatible) to access a wide array of models from different providers.35  
  * **Tool Calling Standardization:** It generally standardizes tool calling to align with the OpenAI specification.36 However, issues can still surface depending on the specific underlying model being routed to. For example, some non-OpenAI models accessed via OpenRouter might have problems with tool definitions that have no arguments 39, or may not correctly handle scenarios where tool call history is present but no tools are offered in the current LLM request.37  
  * **Streaming:** Streaming is supported for a significant number of models, but not universally across all providers available through OpenRouter. It's crucial to verify streaming support for any specific model chosen for a persona.41  
  * **Error Handling:** OpenRouter.AI provides a set of standardized error codes, which can help in building more resilient integrations.47

### **4.4. Model Context Protocol (MCP) and LLM Tool Calling**

If Project Nucleus's backend capabilities are exposed as MCP tools, the M365 Agent's LLM will interact with them using its native tool/function calling mechanism. MCP itself is a protocol that standardizes how applications and LLMs discover and use tools and context.48 The LLM doesn't "see" MCP directly; rather, the M365 Agent SDK, or an integrated orchestrator like Semantic Kernel, translates the LLM's standard tool call request (e.g., a JSON object specifying tool name and arguments) into an MCP-compliant request directed at the Nucleus MCP server.53 Semantic Kernel, for instance, can consume MCP tools by converting their definitions into KernelFunction objects, making them seamlessly available to the LLM's planning and execution engine.54

### **4.5. Deeper Considerations for Non-Azure LLM Integration**

The integration of non-Azure LLMs, while offering flexibility, introduces complexities that require careful architectural consideration. A primary aspect is the **"abstraction versus specificity" trade-off for tool calling**. Abstraction layers like Semantic Kernel 31 and Microsoft.Extensions.AI.IChatClient 32 aim to provide a unified interface, simplifying development by shielding applications from the idiosyncrasies of each LLM provider. However, this normalization can sometimes mean adhering to a "lowest common denominator" of features. If an M365 Agent needs to exploit a unique or highly advanced tool-calling feature specific to a particular non-Azure LLM (e.g., Gemini's propertyOrdering for custom output schemas 34 or a specialized invocation mode like Gemini's ANY mode for function calling 33), the abstraction layer might either not support it directly or require workarounds, potentially diminishing the benefit of the abstraction for that specific use case. Debugging can also become more intricate, as issues could stem from the LLM provider, the abstraction layer, or the agent's own logic. Therefore, Project Nucleus must weigh the benefits of simplified development against the potential need for direct access to provider-specific features. A strategy might involve starting with abstractions but allowing for targeted, direct integrations or extensions to the abstraction layer when a compelling provider-specific capability is required. A thorough understanding of the chosen non-Azure LLM's native tool-calling capabilities remains indispensable, even when using abstractions.

Furthermore, the **effectiveness of tool use by any LLM is profoundly influenced by prompt engineering, specifically how tools are described and presented to the model**.40 While the M365 Agents SDK itself is LLM-agnostic 1, different LLMs (e.g., a Gemini model versus a Claude model via OpenRouter, or an OpenAI model) can interpret the same tool description and parameter definitions differently due to variations in their training data, architectural nuances, and internal reasoning processes. The system prompt and any contextual instructions provided to the M365 Agent's LLM must be meticulously crafted to guide tool selection and argument formulation accurately. If Project Nucleus intends to maintain the flexibility to switch the underlying LLM for a given persona (e.g., migrating a persona from Azure OpenAI to Google Gemini), the prompts, tool definitions, and even the expected interaction patterns for tool use might require re-evaluation and LLM-specific adjustments to preserve optimal performance. The verbosity and structure of tool schemas also play a role, impacting token consumption, cost, and potentially the LLM's comprehension of the tool's purpose and usage.31 Consequently, Project Nucleus cannot assume a "one-size-fits-all" approach to tool descriptions and prompting strategies will yield consistent results across diverse LLMs. A framework for rigorously testing tool-use efficacy with different LLMs and potentially maintaining LLM-specific prompt augmentations or tool description variants will be necessary if high performance and reliability in tool use are critical. This introduces a layer of complexity to the "LLM-agnostic" ideal when advanced, reliable tool invocation is a core requirement.

**Table 2: LLM Tool Calling Feature Comparison**

| Feature | Azure OpenAI (Latest GPT-4 series) | Google Gemini (Latest via Native API) | Google Gemini (via OpenRouter.AI \- OpenAI compatible) | Representative OpenRouter.AI Model (e.g., Claude 3.5 Sonnet) |
| :---- | :---- | :---- | :---- | :---- |
| **Tool/Function Schema Syntax** | OpenAI JSON format | Google-specific JSON schema 33 | OpenAI JSON format (subject to OpenRouter translation & Gemini limitations 34) | OpenAI JSON format (subject to OpenRouter translation & model's native support) |
| **Streaming Support for Tool Calls** | Yes | Yes (Native API, Gemini Live API 43) | Dependent on OpenRouter & Gemini API support; generally yes 41 | Dependent on OpenRouter & underlying model support; often yes 41 |
| **Parallel Function Calling** | Yes | Yes (Native API) | Dependent on Gemini API via OpenRouter | Dependent on underlying model via OpenRouter |
| **Microsoft.Extensions.AI.IChatClient Compatibility** | Native (via Azure SDKs) | Wrapper/Custom Implementation Needed | Wrapper/Custom Implementation Needed (potential for ChatMessageRole mismatches 42) | Wrapper/Custom Implementation Needed (potential for model-specific issues 39) |
| **Semantic Kernel Compatibility** | Native Connector | Connector available/in development (may need specific handling for delimiters, etc. 31) | Via OpenAI-compatible endpoint; potential nuances | Via OpenAI-compatible endpoint; potential nuances |
| **Key Limitations/Considerations** | Mature, well-documented | Schema differences from OpenAI, potential propertyOrdering needs 34, rate limits 44 | Potential schema mapping limitations, role name issues 42 | Model-specific behavior for argument-less functions or history handling 37 |

This comparative analysis underscores the need for careful evaluation and potentially adaptive integration strategies when incorporating diverse non-Azure LLMs into Project Nucleus, especially for advanced features like tool calling and streaming.

## **5\. Multi-Tenant ISV Patterns: Identity, Data, and Entra Agent ID**

For Project Nucleus, operating as an Independent Software Vendor (ISV) solution, establishing robust multi-tenant identity and data management patterns is paramount. This involves a secure Microsoft Entra ID application registration strategy, diligent backend API security, optimized data storage with Azure Cosmos DB, and an understanding of the emerging Entra Agent ID concept.

### **5.1. Microsoft Entra ID App Registration for Multi-Persona M365 Agents**

When an ISV offers multiple distinct M365 Persona Agents (e.g., EduFlowM365Agent, HelpdeskM365Agent), the choice of app registration strategy in Microsoft Entra ID is critical.

* Recommended Pattern: Single Multi-Tenant App Registration with App Roles:  
  For most SaaS applications, the generally advised approach is to use a single multi-tenant application registration in the ISV's Microsoft Entra tenant.55 Distinct functionalities or access levels for different Persona Agents can then be managed by defining app roles within this single registration.59 For example, app roles like EduFlow.ReadWrite, Helpdesk.ReadOnlyTickets, or CrossPersona.Analytics could be defined. When defining these app roles, the "Allowed member types" should be set to "Applications" if these roles are intended to be granted to the service principal of the ISV's application in the customer's tenant, representing application permissions.59  
  This approach offers centralized management for the ISV, a simpler initial consent experience for customers (as they consent to a single application), and consistent branding.  
* Alternative (Generally Less Suitable for this ISV Model): Separate App Registrations per Persona:  
  Creating separate multi-tenant app registrations for each individual Persona Agent is technically feasible but typically increases management complexity for both the ISV and their customers. This path might be considered only if the personas have fundamentally different branding requirements, operate under entirely separate trust boundaries, or have such divergent compliance and permission needs that a single application identity becomes impractical or raises significant consent concerns for customers. While 61 suggests separate app registrations for different environments (dev/test/prod) or for distinct frontend/backend components, this differs from segmenting distinct product functionalities offered to the same customer under a unified SaaS offering. Similarly62 mentions the possibility of assigning separate service principals per customer tenant for enhanced data separation, which is a different architectural consideration than per-persona app registrations within the ISV's model.  
* Customer Tenant Admin Consent and Management:  
  When a customer administrator consents to the ISV's multi-tenant application, a service principal representing that application is instantiated in the customer's tenant.57 The customer admin can then review the permissions requested by the ISV application. For application permissions (app roles), the customer admin must explicitly grant tenant-wide admin consent.64 Through the Microsoft Entra admin center (typically under "Enterprise applications" \-\> \-\> "Permissions"), customer admins can view the granted permissions and, importantly, assign users or groups to the ISV application if it's configured to require user assignment. They also manage which of the defined app roles (application permissions) are active for the ISV's service principal within their tenant, thereby controlling which specific functionalities or personas the ISV application is authorized to perform.59

### **5.2. Secure Backend API Design for Multi-Tenancy**

Backend APIs supporting the M365 Agents must rigorously enforce tenant isolation.

* Validating tid (Tenant ID) Claim:  
  All backend APIs that are called by the M365 Agent (or the agent itself, if it acts as a client to other services) must validate the tid (tenant ID) claim present in the incoming JWT access token. This validation is crucial to ensure that the request originates from an authenticated and authorized tenant that is permitted to access the API.66 The Microsoft.Identity.Web library for ASP.NET Core greatly simplifies token validation, including issuer validation, which for multi-tenant applications implicitly involves the tenant ID (as the issuer claim is typically https://login.microsoftonline.com/{tenantid}/v2.0).66 The API should maintain a list of subscribed or allowed tenant IDs and explicitly verify the tid claim from the token against this list before processing any request.  
* Enforcing Data Tenancy in Azure Cosmos DB Queries:  
  The validated tid from the access token is not just for authentication/authorization at the API entry point; it must be propagated and used as a mandatory filter in all Azure Cosmos DB queries to ensure strict data isolation.73  
  * For the shared ArtifactMetadataContainer (partitioned by tenantId), every query must include a predicate like WHERE c.tenantId \= @validatedTenantId.  
  * For per-Persona-type KnowledgeContainers (partitioned by userId but logically scoped by tenantId), queries must incorporate both tenant and user context, for example, WHERE c.tenantId \= @validatedTenantId AND c.userId \= @userIdFromTokenOrContext.

### **5.3. Optimizing Multi-Tenant Data with Azure Cosmos DB (Build 2025 Announcements)**

Recent announcements from Microsoft Build 2025 regarding Azure Cosmos DB introduce several features that can significantly benefit multi-tenant applications like Project Nucleus.75

* Global Secondary Indexes (GSIs) (Preview):  
  GSIs allow defining alternate partition keys and indexing policies on new containers that are automatically synchronized from the primary container.75  
  * **Benefit for ArtifactMetadataContainer:** If Nucleus needs to query artifact metadata based on attributes other than tenantId (e.g., artifactStatus, creationDate across multiple tenants for an administrative dashboard, or filtering by specific metadata fields within a single tenant without relying solely on the tenantId partition key), GSIs can enable efficient queries on these secondary attributes. This avoids costly cross-partition scans, dramatically improving performance for analytical or cross-cutting query patterns.  
  * **Benefit for KnowledgeContainers:** For KnowledgeContainers (partitioned by userId and scoped by tenantId), if the MCP Tool frequently needs to retrieve knowledge items based on criteria other than userId (e.g., knowledgeCategory, lastModifiedDate, relevanceScore) within a specific tenant or across users in that tenant, GSIs on these attributes (scoped appropriately) would optimize these lookups. This allows the MCP Tool to fetch relevant knowledge much faster.  
* Filtered Vector Search with DiskANN (Preview/GA):  
  DiskANN provides high-performance vector similarity search. The "filtered vector search" capability is particularly crucial for multi-tenant scenarios.75  
  * **Benefit for KnowledgeContainers:** If Nucleus employs vector embeddings for semantic search within KnowledgeContainers (e.g., to find knowledge items semantically similar to a user's query via the MCP Tool), DiskANN offers efficient search. Critically, filtered vector search allows these semantic searches to be performed *within the strict scope of a specific tenantId* (and potentially userId or persona type). This ensures that search results are always relevant to the correct tenant and persona, enhancing both accuracy and data security for the MCP Tool.  
* Full-Text Search Enhancements (GA/Preview):  
  Features like BM25 scoring for relevance, hybrid search (combining keyword and vector results using Reciprocal Rank Fusion \- RRF), fuzzy search for typo tolerance, and multi-language support significantly enhance traditional search capabilities.75  
  * **Benefit for KnowledgeContainers:** These improvements make keyword-based search within knowledge content more accurate (BM25), robust against user input errors (fuzzy search), capable of blending the strengths of keyword and semantic matching (hybrid search), and globally applicable (multi-language support). This directly improves the quality of information retrieval for the MCP Tool.  
  * **Benefit for ArtifactMetadataContainer:** Full-text search could also be applied to textual metadata fields within ArtifactMetadataContainer (e.g., artifactDescription, tags) to enable more flexible and powerful discovery of artifacts.  
* Fleet Management with RU Pooling:  
  This feature allows for more cost-effective and flexible management of Request Units (RUs) across multiple tenants or containers.75  
  * **Benefit for Nucleus:** As an ISV, Nucleus can pool RUs across all its tenants. This smooths out variable loads from different tenants, leading to better overall RU utilization and potentially significant cost savings compared to provisioning RUs statically per tenant. While RUs are pooled, the feature aims to provide mechanisms for performance isolation if needed. Fleet analytics offer insights into per-tenant consumption, aiding in capacity planning and cost allocation.  
* Per Partition Automatic Failover (PPAF) (Preview):  
  PPAF enhances availability by ensuring that if an issue affects a specific partition (e.g., data for a particular tenantId), only that partition fails over, rather than the entire Cosmos DB account.75  
  * **Benefit for Nucleus:** This significantly reduces the blast radius of potential failures, minimizing impact on other tenants and improving overall service uptime and resilience. The rapid recovery time (P99 under 2 minutes) is crucial for maintaining a seamless experience.  
* Foundry Connection & BYO Thread Storage:  
  If M365 Agents store conversational state or thread-specific data in Cosmos DB, the direct integration with Azure AI Foundry and the Azure AI Agent Service's "Bring-Your-Own Thread Storage" feature simplifies this aspect of the architecture, making Cosmos DB a natural fit for agent memory and state.75

### **5.4. Entra Agent ID**

Microsoft Entra Agent ID is an emerging capability that assigns unique, manageable identities in Microsoft Entra ID to AI agents, initially those created in Azure AI Foundry and Copilot Studio, with future support planned for M365 Copilot agents and third-party tools.79

* **Relevance to Project Nucleus:** If Nucleus agents are developed or managed using platforms that support Entra Agent ID, they will automatically receive these identities. For custom engine agents built purely with the M365 Agents SDK, the direct applicability and mechanism for obtaining an Entra Agent ID need to be monitored as the feature evolves.  
* **Multi-Persona Differentiation:** The query touches upon differentiating agent identities under a single multi-tenant app registration. An Entra Agent ID provides an identity for the *agent instance or definition*. If multiple personas are part of a single logical M365 Agent application (deployed under one app registration), they might share the same Entra Agent ID if that ID is tied to the hosting construct or the overarching agent application. In such a scenario, functional differentiation would still heavily rely on internal logic within the agent, potentially governed by app roles assigned to the agent's service principal. If each persona were deployed as a completely distinct agent instance (even if sharing some codebase), they might then acquire distinct Entra Agent IDs. This interaction between the ISV's app registration model (single vs. multiple) and the assignment of Entra Agent IDs will be important to clarify.

### **5.5. Advanced Considerations for Multi-Tenant ISV Architecture**

The choice between a single multi-tenant app registration with app roles and separate app registrations for distinct M365 Agent Personas presents a **granularity dilemma, especially as personas evolve**. While a single app registration is often recommended for ISV SaaS applications due to simpler management and customer onboarding 55, M365 Agent Personas like EduFlowM365Agent and HelpdeskM365Agent might develop highly specialized functionalities over time. Initially, their permission needs might overlap (e.g., basic user profile access). However, as they mature, EduFlowM365Agent could require deep integration with education-specific Microsoft Graph APIs (e.g., Teams for Education, SharePoint for course materials, Education Roster APIs), while HelpdeskM365Agent might need permissions for IT ticketing system integrations or device management APIs. If these permission sets diverge significantly, the manifest of a single app registration could become excessively broad, requesting a wide array of permissions. This could lead to customer administrators expressing concern during the consent process, even if app roles are intended to gate actual access to functionalities. Customers might be hesitant to grant extensive permissions to a single application, particularly if they only plan to utilize a subset of its personas. Project Nucleus should therefore define clear criteria or a threshold for when a new Persona Agent's permission requirements become so distinct that a separate app registration, despite the increased management overhead, becomes a more prudent choice to align with the principle of least privilege at the application identity level.

The advent of **Entra Agent ID also has potential implications for how customer tenant administrators manage permissions for ISV agents**.79 Currently, customer admins interact with the ISV's application via its service principal in their tenant.64 Entra Agent ID aims to make these AI agent identities more visible and directly manageable, potentially appearing as a new "Agent ID" application type in the Microsoft Entra admin center.79 If an ISV, like Project Nucleus, offers multiple agent personas, and these are deployed as distinct agent instances each acquiring an Entra Agent ID, customers will see and manage multiple agent identities from that ISV. Conversely, if personas are bundled within one logical agent application that receives a single Entra Agent ID, the differentiation remains internal to that agent's runtime, governed by app roles. A key aspect to monitor is how the "least-privileged approach requesting just-in-time, scoped tokens" promised for Entra Agent ID 79 will interact with the app roles defined by the ISV in their primary application registration. Will app roles continue to be the primary mechanism for ISVs to define functional boundaries that customer admins then consent to for the Entra Agent ID? Or will Entra Agent ID itself offer more granular, per-agent-instance permission scoping capabilities that either supersede or complement the traditional app role model? This is a critical question for ISV architecture as Entra Agent ID matures and extends support to M365 Copilot agents and third-party tools.

**Table 3: Entra ID Registration Strategy for Multi-Persona Agents**

| Feature | Single Multi-Tenant App Registration (with App Roles) | Separate Multi-Tenant App Registrations (per Persona) |
| :---- | :---- | :---- |
| **ISV Management** | Centralized management of one app registration. App roles define functional boundaries.59 | Decentralized; requires managing multiple app registrations, secrets, and lifecycles. |
| **Customer Onboarding** | Simpler initial consent; customer consents to one application. | Customer needs to consent to multiple applications if using multiple personas. |
| **Customer Admin Experience** | Manages one service principal. Assigns app roles to this SP to enable/disable specific persona functionalities.59 | Manages multiple service principals. Consent and permission management per persona app. |
| **Permission Granularity** | Achieved via app roles within the single app. API must check roles claim.59 | Achieved at the app registration level; each app requests only permissions needed for its specific persona. |
| **Principle of Least Privilege (App Reg Level)** | May require requesting a broader set of permissions in the manifest if personas are diverse. | Each app registration requests only the minimal permissions required for its specific persona, adhering more strictly. |
| **Branding & Trust** | Consistent branding under one app identity. | Allows for distinct branding per persona if needed, but could also fragment ISV identity. |
| **Complexity** | Lower initial complexity for ISV and customer. Potential for complex app role management as personas grow. | Higher initial and ongoing management complexity for both ISV and customer. |
| **Recommendation for Project Nucleus** | Start with a single multi-tenant app registration and leverage app roles for persona differentiation. Re-evaluate if persona permission sets become extremely divergent or if customer feedback indicates concerns with broad permission requests. Monitor Entra Agent ID evolution for its impact on this model. | Consider only if personas are truly distinct products with minimal overlap in permissions and require separate lifecycle management or trust boundaries. |

**Table 4: Azure Cosmos DB Feature Application to Nucleus Data Stores**

| Cosmos DB Feature (Build 2025\) | Application to ArtifactMetadataContainer (Partitioned by tenantId) | Application to KnowledgeContainers (Partitioned by userId, scoped by tenantId) | Multi-Tenancy Benefit for Nucleus |
| :---- | :---- | :---- | :---- |
| **Global Secondary Indexes (GSIs) (Preview)** 75 | Enable efficient queries on non-partition key attributes (e.g., status, creationDate, artifactType) across tenants or within a tenant. Reduces need for cross-partition scans for analytical/admin views. | Optimize queries by attributes other than userId (e.g., category, tags, lastUpdated) for MCP Tool access within a tenant. Improves knowledge retrieval speed. | Enhanced query flexibility and performance without data duplication or complex client-side logic. Supports diverse access patterns common in multi-tenant apps. |
| **Filtered Vector Search (DiskANN) (Preview/GA)** 75 | Less directly applicable unless metadata itself is vectorized for semantic search. | Crucial for semantic search on knowledge items. Allows high-performance vector searches filtered by tenantId (and userId), ensuring results are isolated and relevant to the specific tenant/user context for the MCP Tool. | Provides secure and highly relevant AI-powered search within tenant boundaries, critical for personalized agent experiences. |
| **Full-Text Search Enhancements (BM25, Hybrid, Fuzzy, Multi-lingual) (GA/Preview)** 75 | Improve keyword search on metadata fields (e.g., description, tags) for better artifact discovery. | Significantly enhance keyword-based search for MCP Tool: more relevant results (BM25), typo tolerance (fuzzy), combined semantic/keyword (hybrid), global reach (multi-lingual). | Richer, more accurate, and user-friendly search experiences for all tenants, accommodating diverse data and user inputs. |
| **Fleet Management (RU Pooling)** 75 | Enables cost-effective RU sharing across all tenants using this container. Smooths load variations and optimizes overall RU consumption. Provides fleet analytics for tenant usage. | Allows RUs to be pooled across all persona knowledge containers or tenant-specific instances, optimizing costs. | Significant cost savings for the ISV by improving RU utilization across a variable multi-tenant workload. Simplifies capacity management. |
| **Per Partition Automatic Failover (PPAF) (Preview)** 75 | If a specific tenantId partition has issues, only that partition fails over, minimizing impact on other tenants. Improves overall service availability. | If a specific userId partition (within a tenant's scope) fails, impact is localized. Enhances resilience for individual user experiences. | Increased application uptime and resilience, reducing the blast radius of failures, which is critical for maintaining tenant trust and SLAs. |

## **6\. Leveraging Model Context Protocol (MCP) with M365 Agents**

The Model Context Protocol (MCP) offers a standardized way for AI agents to interact with external tools and data sources.48 For Project Nucleus, this means backend capabilities, such as accessing artifact metadata or querying knowledge bases, can be exposed as MCP tools for consumption by M365 Agents.

### **6.1. Defining and Exposing Backend Nucleus Capabilities as MCP Tools**

To make backend Nucleus functionalities available to an M365 Agent's LLM via MCP, these capabilities must be defined as MCP tools. This involves specifying a schema that the LLM can understand and use to determine when and how to invoke the tool.

* Core Elements of an MCP Tool Definition:  
  An MCP tool definition, to be effectively utilized by an LLM for function/tool calling, generally requires the following components 40:  
  1. **name**: A unique, machine-readable identifier for the tool (e.g., Nucleus.GetArtifactMetadataById, Nucleus.SearchKnowledgeByPersona). This name is used by the LLM when it decides to call the tool.  
  2. **description**: A clear, concise, and unambiguous natural language description of the tool's purpose, capabilities, and appropriate use cases. This is a critical element, as the LLM relies heavily on this description to understand when the tool is relevant to the user's query or the ongoing task.  
  3. **inputSchema (or parameters)**: A structured definition, typically in JSON Schema format, outlining the input parameters the tool accepts. For each parameter, this schema should specify its name, type (e.g., string, integer, boolean, object, array), a description explaining its purpose, and whether it is required or optional. Default values can also be part of this schema.  
* **Conceptual Example of a Nucleus MCP Tool Definition (SearchKnowledge):**  
  JSON  
  {  
    "name": "Nucleus.SearchKnowledge",  
    "description": "Searches the Nucleus knowledge base for a specific persona type and tenant to find relevant information based on a natural language query. Use this tool to answer user questions that require information retrieval from stored documents or FAQs. This tool is ideal for finding supporting documents, articles, or specific data points related to a user's request within their authorized tenant context.",  
    "inputSchema": {  
      "type": "object",  
      "properties": {  
        "personaType": {  
          "type": "string",  
          "description": "The specific M365 Persona Agent making the request (e.g., 'EduFlowM365Agent', 'HelpdeskM365Agent'). This helps scope the search to relevant knowledge areas."  
        },  
        "query": {  
          "type": "string",  
          "description": "The user's natural language query or the specific information to search for in the knowledge base."  
        },  
        "tenantId": {  
          "type": "string",  
          "description": "The unique identifier of the tenant for whom the search is being performed. This ensures data isolation."  
        },  
        "maxResults": {  
          "type": "integer",  
          "description": "Optional. The maximum number of search results to return. Defaults to 5 if not specified.",  
          "default": 5  
        },  
        "filters": {  
          "type": "object",  
          "description": "Optional. Additional filters to apply to the search, such as date ranges or categories. Structure depends on available knowledge metadata.",  
          "properties": {  
              "category": {"type": "string", "description": "Filter by a specific knowledge category."},  
              "createdAfter": {"type": "string", "format": "date-time", "description": "Filter for items created after this date."}  
          }  
        }  
      },  
      "required":  
    }  
  }

* Exposing Nucleus Capabilities via an MCP Server:  
  The backend services of Project Nucleus that implement these functionalities (e.g., querying ArtifactMetadataContainer or KnowledgeContainers in Cosmos DB) will need to be fronted by an MCP server. This server is responsible for advertising the defined MCP tools and handling incoming tool invocation requests.  
  * Semantic Kernel provides capabilities to build MCP servers, allowing existing Kernel Functions to be exposed as MCP tools.54  
  * Alternatively, Azure Functions can be used to create MCP servers, leveraging attributes like and to define tools and their parameters, with metadata exposed via builder.EnableMcpToolMetaData().83

### **6.2. LLM-driven Invocation of MCP Tools by M365 Agent**

Once Nucleus backend capabilities are exposed as MCP tools, the M365 Agent's integrated LLM can discover and invoke them.

* Tool Discovery and Selection by LLM:  
  The M365 Agent's runtime (or an orchestrator like Semantic Kernel) provides the LLM with the list of available MCP tools, including their names, descriptions, and input schemas. Based on the current user prompt and the ongoing conversation context, the LLM analyzes the user's intent and matches it against the descriptions of the available tools to determine if any tool is suitable for fulfilling the request.53 A well-crafted tool description is paramount for accurate selection.  
* Argument Formulation:  
  If the LLM selects an MCP tool, it then uses the tool's inputSchema to formulate the necessary arguments. It attempts to extract or infer the values for these arguments from the user's query, conversation history, or other available context.  
* Integration with LLM's Native Function/Tool Calling Capabilities:  
  MCP is designed to work with an LLM's inherent function or tool calling capabilities. It acts as a standardized protocol layer.52 The M365 Agent's LLM will generate a request to call a function (the MCP tool) with specific arguments. The M365 Agents SDK, or an integrated orchestrator like Semantic Kernel, is then responsible for:  
  1. Recognizing the LLM's intent to call a tool.  
  2. Translating this intent into an MCP-compliant request.  
  3. Sending this request to the appropriate Nucleus MCP server.  
  4. Receiving the result from the MCP server.  
  5. Formatting this result and feeding it back to the LLM so it can generate a final response to the user. Semantic Kernel offers a streamlined way to achieve this by allowing MCP tools to be converted into KernelFunction objects. These functions are then added to the kernel's plugins and become available for the LLM to invoke through its standard function calling flow.54  
* Prompt Engineering Strategies for MCP Tool Use:  
  Effective use of MCP tools by the LLM requires careful prompt engineering:  
  * **System Prompts:** The system prompt for the M365 Agent should include instructions guiding the LLM on how and when to use the available Nucleus MCP tools, how to formulate queries for them, and how to interpret their results.  
  * **Tool Descriptions:** As emphasized, the descriptions within the MCP tool definitions themselves must be extremely clear, action-oriented, and unambiguous to the LLM.  
  * **Few-Shot Examples:** If tool usage is complex or nuanced, providing few-shot examples of user queries and corresponding correct tool invocations within the prompt can significantly improve the LLM's performance.  
  * **Iterative Refinement:** The process of prompting for tool use will likely require iteration and testing to optimize the LLM's ability to select and use tools correctly.  
* .NET Implementation Patterns:  
  If Semantic Kernel is integrated within the M365 Agent, its functionalities for connecting to MCP servers, listing available tools, and adding them to the kernel's planner (or directly to the kernel as plugins) should be utilized.54 The core agent logic, likely within an OnActivity handler or a similar turn processing method, would involve invoking the LLM (potentially through Semantic Kernel's chat completion services or an IChatClient implementation). If the LLM decides to use an MCP tool, Semantic Kernel would handle the invocation and return the tool's output to the LLM, which then formulates the final user-facing response.

The M365 Agents SDK, being model and orchestrator-agnostic 29, does not natively dictate MCP usage but provides the flexibility to integrate components (like Semantic Kernel or custom logic) that can communicate via MCP. The broader Microsoft ecosystem's adoption of MCP, for example in Copilot Studio which can consume MCP tools via connectors 48, signals its strategic importance.

### **6.3. Advanced Considerations for MCP Integration**

The integration of MCP tools introduces nuanced challenges that go beyond basic tool definition. A key factor is that the **semantic richness of MCP tool definitions is paramount for LLM autonomy**. While MCP standardizes the *protocol* for tool interaction 49, the actual effectiveness of an LLM in discovering, selecting, and correctly using these tools hinges critically on the quality and clarity of the tool's name, description, and inputSchema.40 The LLM does not "understand" the underlying code of the tool; its interaction is solely based on the provided metadata. A poorly worded or ambiguous description, or unclear parameter definitions, can lead to the LLM failing to use a tool when appropriate, selecting an incorrect tool, or formulating invalid arguments. For complex backend Nucleus capabilities, translating their intricate logic and data requirements into concise, LLM-comprehensible descriptions is a significant "prompt engineering" task, but applied at the tool definition or manifest level. This is distinct from prompting the LLM to generate a user-facing response; it's about prompting the LLM to effectively use its available tools. Project Nucleus must therefore invest considerable effort in crafting high-quality, semantically rich MCP tool definitions. This includes using action-oriented names, providing detailed yet concise descriptions that clearly outline use cases and limitations, and defining parameters with explicit types and clear, unambiguous descriptions.

Another important consideration is the **potential for complex orchestration and chaining of MCP tools**. A single user request to an M365 Agent might necessitate a sequence of actions involving multiple MCP tools (which could be a mix of Nucleus-specific tools and other third-party MCP tools) to be fulfilled. For example, a user query might first require fetching artifact metadata using one MCP tool, then using that metadata to query a knowledge base via a second MCP tool, and finally, invoking a third tool to summarize the findings or take an action. The M365 Agent's primary LLM, or an orchestrator like Semantic Kernel employing a planner (as described in 40 for the Teams AI library context), would be responsible for decomposing the user's request and orchestrating this sequence of tool calls. This implies that state (i.e., results from one tool call) must be managed and passed as input to subsequent tool calls within the context of a single user turn. Furthermore, error handling for such multi-step MCP tool chains becomes significantly more complex. The agent architecture must account for scenarios where one tool in the chain might fail, and implement robust error handling, potential retry logic, or graceful degradation strategies. The prompt engineering for the LLM also needs to extend beyond guiding single tool use to potentially instructing it on how to plan and execute sequences of tool invocations, including how to handle intermediate results and errors.

## **7..NET Aspire for Azure Deployment: Advanced Considerations**

.NET Aspire provides a streamlined experience for developing and deploying cloud-native applications, including those composed of multiple microservices like the M365 Agent and its associated backend MCP tool services. When deploying to Azure Container Apps (ACA), Aspire, in conjunction with the Azure Developer CLI (azd), handles many of the complexities of configuration, service discovery, and inter-service communication.

### **7.1. Service Discovery and Inter-Service Communication in Azure Container Apps (ACA)**

* Transition from Local AppHost Orchestration:  
  Locally, the.NET Aspire AppHost project orchestrates service discovery, often by directly injecting environment variables containing service URLs or connection strings into dependent projects.23 This local orchestration is not replicated in the deployed Azure environment. Instead, Aspire generates a deployment manifest (manifest.json) which azd uses as a blueprint to provision and configure resources in Azure.86  
* Azure Container Apps Environment DNS:  
  When multiple services (e.g., an M365 Agent frontend container and several backend MCP tool containers) are deployed to the same Azure Container Apps environment, ACA provides built-in service discovery. Services within the same environment can typically discover and communicate with each other using DNS names automatically assigned by ACA. These DNS names are often of the format \<app-name\>.\<environment-name\>.\<region\>.azurecontainerapps.io or shorter internal DNS names resolvable within the ACA environment.23  
* Environment Variable Injection by Aspire/azd:  
  During deployment with azd, connection strings and service URLs (for both Azure PaaS dependencies like Cosmos DB or Redis, and for other microservices within the ACA environment) are typically injected as environment variables into the respective containers.22 The Aspire-generated manifest guides azd in determining which environment variables to set for each service, based on the WithReference() calls and other configurations in the AppHost project.

### **7.2. Secure Inter-Service Authentication in ACA**

Secure communication between services in an ACA environment is critical.

* **Managed Identities:** The recommended approach for Azure service-to-service authentication is to use Managed Identities.25 Each container app can be assigned a system-assigned or user-assigned managed identity.  
* **.NET Aspire 9.2+ Enhancements for Managed Identities:**.NET Aspire version 9.2 and later enhance this by defaulting to assigning each Azure Container App its own dedicated managed identity. This promotes the principle of least privilege by ensuring each app only has the identity and permissions it specifically requires.24 Aspire also introduced APIs for modeling least-privilege Azure role assignments in C\# within the AppHost project, which azd then translates into the Bicep deployment templates.  
* **Token-Based Authentication:** Services authenticate to one another by acquiring an OAuth 2.0 access token for the target service's resource ID (or App ID URI) using their own managed identity. This token is then presented as a Bearer token in the HTTP request to the target service. Libraries like Azure.Identity (specifically DefaultAzureCredential which can use ManagedIdentityCredential in Azure) simplify the token acquisition process. The target service validates the incoming token to ensure it's from an expected caller and has the necessary permissions (e.g., via scp or roles claims).

### **7.3. Configuration Injection, Health Checks, and Telemetry in Deployed ACA**

* Configuration and Secrets Management:  
  .NET Aspire's integration with Azure App Configuration and Azure Key Vault (12) extends seamlessly to deployed Azure environments. Connection strings or service endpoints for App Configuration and Key Vault are injected as environment variables into the application containers by Aspire/azd. The applications then use their Managed Identities to authenticate to these services and retrieve configuration and secrets. Secrets required for inter-MCP-tool communication or other service dependencies should be stored in Key Vault and referenced either through App Configuration or directly by services if Aspire models Key Vault as a direct dependency. Aspire 9.2+ further allows configuring services to store their specific secrets (e.g., Redis keys, Cosmos DB keys if not using RBAC) in a shared, centralized Key Vault, simplifying secret management.24  
* Health Checks:  
  Standard ASP.NET Core health checks, typically configured in.NET Aspire projects via the AddServiceDefaults() extension method 23, expose HTTP endpoints (e.g., /healthz, /livez). When deployed to Azure Container Apps, ACA can be configured to probe these health check endpoints. ACA uses these probes to monitor the health of the containers, manage their lifecycle (e.g., restart unhealthy containers), and influence traffic routing decisions.  
* Telemetry (Logging, Metrics, and Tracing):  
  The OpenTelemetry support configured by AddServiceDefaults() in Aspire projects 23 continues to function when applications are deployed to ACA. This includes structured logging, runtime metrics, and distributed tracing. This telemetry data can be configured to be exported to Azure Monitor Application Insights, providing comprehensive observability into the distributed system running in ACA. The.NET Aspire Dashboard itself might be deployable to ACA for monitoring, or azd monitor can be used to launch it locally, connecting to the telemetry backend.88 Azure Container Apps also offers its own observability features, which can complement Aspire's telemetry.

### **7.4. Deploying Distributed Systems with azd**

The Azure Developer CLI (azd) is the primary tool for deploying.NET Aspire applications to Azure.

* The azd up command typically handles both provisioning of Azure resources (as defined by Bicep templates generated from the Aspire manifest) and the initial deployment of the application containers.86  
* For subsequent updates, azd deploy can be used to redeploy code changes without re-provisioning infrastructure.86  
* azd automates the process of building container images for the.NET projects, pushing these images to a provisioned Azure Container Registry (ACR), and then updating the Azure Container Apps to use the new images.86  
* Dependencies like Azure Cosmos DB, Azure Cache for Redis, App Configuration, and Key Vault are defined as resources in the Aspire AppHost and are provisioned by azd as part of the generated Bicep infrastructure..NET Aspire 9.2+ introduced improved managed identity defaults for Azure SQL and Azure PostgreSQL 24, and similar secure-by-default patterns are expected for other Azure service integrations. Automatic database creation is also supported for several database types.27

### **7.5. Advanced Deployment Considerations with.NET Aspire**

The deployment of a distributed system using.NET Aspire involves a crucial translation layer: **the manifest generated by the AppHost project serves as the "source of truth" that bridges local development orchestration with cloud deployment configurations**.86 Locally, the AppHost directly manages and injects configurations. For cloud deployment, this direct control is superseded by the declarative manifest. azd interprets this manifest to generate Bicep for Azure resources and to configure environment variables that enable service discovery and connections within Azure Container Apps. The accuracy and completeness of the AppHost project's C\# definitions are therefore critical, as they dictate the content of the manifest and, consequently, the behavior of the deployed application. Any assumptions made during local development that are not explicitly captured in the AppHost definitions (and thus the manifest) may lead to misconfigurations or runtime issues in the Azure environment. Developers should understand this translation process and may need to inspect the generated manifest and Bicep files to debug deployment issues.

Another significant consideration is the **complexity of managing Managed Identities and Azure role assignments in multi-service Aspire applications**. While.NET Aspire 9.2+ defaults to per-app managed identities and facilitates least-privilege role assignments 24, the practical implementation for a complex system like Project Nucleus (with multiple M365 Agent components, backend MCP tools as separate ACAs, and dependencies like Cosmos DB, Key Vault, and App Configuration) can become intricate. Defining precisely "which roles each app needs for specific Azure resources" 24 demands a thorough analysis of each service's access requirements. For service-to-service communication (e.g., an agent frontend ACA calling an MCP tool backend ACA), both services require appropriate configurations for token acquisition (using their managed identity) and token validation. Each Azure dependency (like Cosmos DB) needs specific role assignments (e.g., "Cosmos DB Data Contributor") granted to the managed identity of every service that needs to access it. Although azd and Aspire automate much of the Bicep generation for these roles, understanding and verifying these assignments against Azure's RBAC model is essential. Misconfigured role assignments are a common source of runtime "access denied" errors that can be challenging to diagnose in a distributed environment. Therefore, meticulous planning and documentation of the required Azure role assignments for each service's managed identity are crucial, and tools like the Azure portal (IAM blades) or Azure CLI will be necessary for inspection and troubleshooting in the deployed environment.

## **8\. Definitive Network Isolation Architectures**

Ensuring robust network isolation for M365 Agents, particularly when hosted within a Virtual Network (VNet) but requiring connectivity to public channels like Microsoft Teams and potentially external LLMs, is critical for security and compliance. This section outlines definitive architectures and configurations.

### **8.1. Securing VNet-Hosted M365 Agents with Public Channel Connectivity**

The core challenge is to allow the Azure Bot Service (which connects to public channels like Teams) to communicate with the M365 Agent's messaging endpoint, while the agent's compute instances (e.g., running on Azure Container Apps or App Service within a VNet) have no direct public inbound IP addresses.

* **Pattern 1: Azure Application Gateway with Web Application Firewall (WAF)** 93  
  * **Topology:** Microsoft Teams \-\> Azure Bot Service \-\> Public IP of Application Gateway \-\> Application Gateway (deployed in the VNet) \-\> Private IP of the M365 Agent (e.g., Azure Container App with internal-only ingress).  
  * **Application Gateway Configuration:**  
    * Listeners configured for HTTPS traffic on its public IP.  
    * Backend pools pointing to the internal IP address or internal FQDN of the M365 Agent's service (e.g., the internal load balancer of an ACA environment or the private IP of an App Service with VNet integration).  
    * Health probes to monitor the health of the backend agent instances.  
    * URL path-based routing rules to direct traffic for the messaging endpoint (e.g., /api/messages) to the appropriate backend pool.  
  * **WAF Policies:** Integrated with Application Gateway, WAF should be configured with a managed rule set (e.g., OWASP Core Rule Set) to protect against common web vulnerabilities like SQL injection and cross-site scripting. Custom WAF rules can be added for application-specific protection or to block traffic from untrusted sources.  
  * **Agent Hosting:** The M365 Agent itself, running on ACA or App Service, is configured with VNet integration and internal-only ingress, meaning it does not have a direct public IP address and only accepts traffic from within the VNet (specifically, from the Application Gateway's subnet).  
* **Pattern 2: Azure API Management (APIM) with Private Link or VNet Integration** 93  
  * **Topology:** Microsoft Teams \-\> Azure Bot Service \-\> Public IP of APIM \-\> APIM instance \-\> Private Endpoint to M365 Agent (or direct VNet routing if APIM is VNet-injected).  
  * **APIM Configuration:**  
    * Expose only the required messaging endpoint (e.g., /api/messages).  
    * Implement policies for authentication and authorization (e.g., validating the JWT token from the Bot Connector service).  
    * Apply rate limiting and quota policies to protect the backend agent.  
    * APIM can be deployed in "External" mode but integrated into a VNet (VNet injection), allowing it to reach private endpoints within that VNet. Alternatively, APIM can be in "Internal" mode (fully private) and fronted by an Application Gateway if public access is still needed for the Bot Service.  
  * **Private Link:** If the M365 Agent is hosted on a PaaS service that supports Private Link (e.g., App Service, ACA), APIM (especially if in a different VNet or needing to traverse VNet boundaries securely) can connect to the agent's private endpoint.  
  * **Agent Hosting:** Similar to the App Gateway pattern, the agent's compute instance is VNet-integrated and not directly exposed to the public internet.  
* Azure Bot Service Configuration:  
  In both patterns, the messaging endpoint registered with the Azure Bot Service resource in the Azure portal must be the public FQDN of the Application Gateway or the API Management instance.

### **8.2. Azure Firewall: Essential FQDNs and Service Tag Configurations**

For comprehensive network isolation, all egress (outbound) traffic from the VNet hosting the M365 Agent and its dependencies (such as Azure Functions used for proactive messaging or backend MCP tools) should be routed through an Azure Firewall instance. This allows for centralized logging, inspection, and policy enforcement for outbound connections.

* Required Azure Dependencies (Service Tags):  
  Azure Firewall supports Service Tags, which represent groups of IP address prefixes for Microsoft services, simplifying rule creation.102  
  * AzureBotService: For communication from the agent to Bot Framework services (e.g., for proactive messaging initiated by the agent).  
  * AzureActiveDirectory: For Microsoft Entra ID authentication (e.g., Managed Identity token acquisition, user authentication if applicable).  
  * AzureKeyVault, AzureStorage, AzureAppConfiguration, AzureCosmosDB, AzureMonitor: For accessing these essential Azure PaaS services. Ideally, these services are accessed via Private Endpoints, which keeps traffic off the public internet. However, firewall rules might still be needed for control plane operations or if Private Endpoints are not used for all interactions.  
  * AzureCognitiveServices (if applicable, for Azure OpenAI or other Azure AI services).  
  * AzureMachineLearning (if Azure AI Foundry or Machine Learning workspaces are used for LLM orchestration or hosting).  
* External LLM Endpoints (FQDNs):  
  If the M365 Agent uses non-Azure LLMs, their API endpoints must be explicitly allowed.  
  * **Google Gemini API:**  
    * generativelanguage.googleapis.com (for the Gemini API, including the OpenAI-compatible endpoint).38  
    * gemini.google.com (often cited for general access/UI, may share backend infrastructure).103  
    * For environments using VPC Service Controls with Gemini, traffic to googleapis.com might be routed to restricted.googleapis.com (IP range 199.36.153.4/30).104 This is more relevant for GCP-hosted components calling Gemini securely within GCP. For an Azure-hosted agent, the public Gemini API FQDNs are primary.  
    * Protocol: TCP, Port: 443\.  
  * **OpenRouter.AI API:**  
    * openrouter.ai (specifically API calls target https://openrouter.ai/api/v1/...).35  
    * Protocol: TCP, Port: 443\.  
* Network Security Group (NSG) Policies:  
  NSGs should be applied to the subnets hosting the M365 Agent (e.g., ACA subnet), the Application Gateway/APIM subnet, and any Azure Functions subnets to enforce micro-segmentation within the VNet.95  
  * **Inbound Rules:** Default deny all inbound traffic from the internet to the agent's subnet. Allow inbound traffic only from the Application Gateway's or APIM's subnet on the specific port the agent listens on (e.g., HTTPS/443).  
  * **Outbound Rules:** Default deny all outbound traffic from the agent's subnet. Create a User-Defined Route (UDR) to force all outbound traffic (0.0.0.0/0) through the Azure Firewall. Azure Firewall will then apply its rules.

### **8.3. Azure Bot Service Private Endpoint Support**

The Azure Bot Service security baseline indicates that Azure Private Link is supported, with the caveat that "the only way to do this in Azure Bot Service is to use the Direct Line App Service extension".95 This suggests that comprehensive Private Endpoint support for *all* communication paths of the Azure Bot Service itself (e.g., connectivity from various public channels like Microsoft Teams directly to a private instance of the Bot Service) might be limited or tied to specific configurations like the Direct Line App Service Extension running in an App Service Environment (ASE).

For an M365 Agent hosted in a VNet, the primary focus of network isolation is on:

1. Protecting the agent's own compute instance (ACA, App Service) from direct public inbound access by placing it behind an Application Gateway or APIM.  
2. Controlling the agent's outbound traffic to its dependencies (Azure PaaS, external LLMs) via Azure Firewall.  
3. Using Private Endpoints for the agent to access its Azure PaaS dependencies (Cosmos DB, Key Vault, etc.) securely from within the VNet.93

The Azure Bot Service itself remains a globally available Azure service that orchestrates communication between the user's channel (e.g., Teams) and the registered (publicly accessible) messaging endpoint of the bot/agent.

### **8.4. Deeper Considerations for Network Isolation Architectures**

Achieving robust network isolation, especially when integrating external services like non-Azure LLMs, presents ongoing challenges. One such challenge is the **complexity of egress control for external LLMs that may use dynamic IP addresses**. While FQDNs are provided for services like Google Gemini and OpenRouter.AI (103), these FQDNs can resolve to a wide or frequently changing range of IP addresses, particularly if they leverage Content Delivery Networks (CDNs) or dynamic IP allocation. Relying solely on FQDN filtering in Azure Firewall network rules (which operate at Layer 4 and resolve FQDNs at rule processing time) can be problematic if these IPs change often. Azure Firewall Application Rules, which can inspect FQDNs in HTTP/S traffic (often via SNI for encrypted traffic, or with TLS inspection enabled), offer more robust FQDN-based filtering. However, if an external LLM provider does not publish stable IP ranges or a dedicated Azure Service Tag, maintaining accurate firewall rules can become an operational burden. This could lead to either overly permissive rules (e.g., allowing all outbound HTTPS to any destination, which defeats the purpose of egress filtering) or intermittent connectivity failures if new IP addresses used by the LLM provider are not promptly added to the allowlist. Project Nucleus should prioritize Azure Firewall Application Rules for FQDNs where feasible, regularly review and update FQDN allowlists based on provider documentation, and consider the potential need for more advanced egress filtering solutions if precise control over dynamic external endpoints is paramount.

Another critical aspect to understand is the concept of the **"weakest link" in the private endpoint strategy for bot communication**. While the M365 Agent itself can be highly isolated within a VNet, and its connections to Azure PaaS dependencies like Cosmos DB or Key Vault can be secured using Private Endpoints 99, the Azure Bot Service, which facilitates communication with channels like Microsoft Teams, is an Azure-managed multi-tenant PaaS offering. The communication path from a Teams client to the Azure Bot Service, and subsequently from the Azure Bot Service to the publicly exposed messaging endpoint of the agent (fronted by Application Gateway or APIM), inherently traverses public network segments, albeit secured by TLS and further protected by WAF/APIM policies.93 Private Endpoints primarily secure connections *from within* a VNet *to* Azure PaaS services or allow services *hosted within* a VNet to be accessed privately by other VNet resources. The Azure Bot Service itself is not typically deployed *into the customer's VNet* in the same manner that an App Service or Azure Container App can be. While the Direct Line App Service Extension offers a degree of private connectivity for bots hosted in an App Service Environment 95, its universal applicability and equivalence to full Private Endpoint integration for all M365 Agent scenarios across various channels like Teams needs careful evaluation. Therefore, it's crucial for Project Nucleus to understand that "end-to-end private communication" in the context of a Teams-connected M365 Agent primarily refers to securing the agent's hosting environment and its backend dependencies. The path involving the Azure Bot Service channel connector will rely on robust security at the public ingress point (App Gateway/APIM) and secure protocols.

**Table 5: Azure Firewall Egress Allowlist for VNet-Hosted M365 Agent (Conceptual)**

| Service/Dependency | Type | Protocol/Port | Azure Firewall Rule Type | Purpose/Notes | Source Snippet(s) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Azure Bot Service Communication | Service Tag: AzureBotService | TCP/443 | Network/Application | Agent to Bot Framework (e.g., proactive messages, state) | 102 |
| Microsoft Entra ID | Service Tag: AzureActiveDirectory | TCP/443 | Network/Application | Authentication (Managed Identity, App Reg) | 102 |
| Azure Key Vault | Service Tag: AzureKeyVault (or FQDN if PE used: \*.vault.azure.net) | TCP/443 | Network/Application | Secrets management (ideally via Private Endpoint) | 102 |
| Azure App Configuration | Service Tag: AzureAppConfiguration (or FQDN if PE used: \*.azconfig.io) | TCP/443 | Network/Application | Centralized configuration (ideally via Private Endpoint) | 102 |
| Azure Cosmos DB | Service Tag: AzureCosmosDB (or FQDN if PE used: \*.documents.azure.com) | TCP/443 (and 10250-10255 if gateway mode) | Network/Application | Data storage (ideally via Private Endpoint) | 102 |
| Azure Monitor | Service Tag: AzureMonitor (or specific FQDNs for Log Analytics/App Insights) | TCP/443 | Network/Application | Telemetry and logging (ideally via Private Endpoint) | 102 |
| Google Gemini API | FQDN: generativelanguage.googleapis.com | TCP/443 | Application | Access to Google Gemini LLM | 38 |
| OpenRouter.AI API | FQDN: openrouter.ai | TCP/443 | Application | Access to various LLMs via OpenRouter | 105 |
| Azure Control Plane (General) | FQDNs: management.azure.com, login.microsoftonline.com | TCP/443 | Application | Azure resource management, token acquisition | 97 |
| CRL/OCSP Endpoints | Various FQDNs (provider-specific) | TCP/80, TCP/443 | Application | Certificate revocation checks for TLS | General Best Practice |
| NTP Servers | UDP/123 (if custom NTP needed) | Network | Time synchronization | General Best Practice |  |

*Note: This table is conceptual. Specific FQDNs for PaaS services might vary if Private Endpoints are used, as traffic would then target the private IP within the VNet. However, control plane operations or fallback scenarios might still require public FQDN access. Always prefer Service Tags where available and applicable. For external services like LLMs, FQDNs are necessary.*

## **9\. Responsible AI (RAI) and M365 Agent Store Submission**

Developing M365 Agents, especially "custom engine agents" that utilize non-Azure LLMs, carries significant responsibilities regarding Responsible AI (RAI), security, and compliance. These factors are critical for gaining enterprise trust and for successful submission to the Microsoft 365 Agent Store or Commercial Marketplace.

### **9.1. RAI Validation for Custom Engine Agents Using Non-Azure LLMs**

Microsoft has established RAI validation checks for *declarative agents* that customize Microsoft 365 Copilot. These checks, which run during manifest validation and user prompt processing, include components like an RAI LLM prompt, a jailbreak classifier, and an offensiveness classifier.109

For **custom engine agents**, particularly those leveraging non-Azure LLMs, the onus of ensuring ethical and safe behavior shifts more directly to the developer. The guidance states that developers "Must ensure your own compliance, RAI practices, and security measures".110 This implies a greater degree of self-governance and the need to implement robust RAI frameworks independently. Microsoft itself advocates a layered approach to mitigating potential harms from LLMs, encompassing the model itself, a safety system (e.g., content filters), metaprompt engineering and grounding techniques, and careful user experience design.111 Developers of custom engine agents will be expected to demonstrate that they have addressed these layers.

While 109 primarily details the validation for declarative agents, it is highly probable that the core principles and categories of RAI concerns (e.g., preventing harmful content, bias, misinformation, privacy violations) will be applied, perhaps even more stringently, to custom engine agents submitted to any Microsoft marketplace. The goal of automated agent validation for store policies and performance optimization is on Microsoft's roadmap 112, which will likely encompass custom engine agents in the future.

### **9.2. Developer Responsibilities for Security, Compliance, and RAI**

* **Data Handling, Privacy, and Security:**  
  * Strict adherence to data privacy regulations (e.g., GDPR, CCPA) is non-negotiable, especially when agents process sensitive enterprise data accessed via Microsoft Graph or other sources.113 Microsoft Purview offers tools to help govern data and ensure compliance within the Microsoft ecosystem.80  
  * When integrating non-Azure LLMs, developers are responsible for the security of that integration. This includes secure API key management (e.g., using Azure Key Vault and not hardcoding keys), ensuring secure (TLS-encrypted) communication channels to the external LLM, and validating inputs and outputs to and from the LLM to prevent injection attacks or data leakage.  
* **Responsible AI Practices:**  
  * Developers must proactively implement mechanisms to detect, mitigate, and manage AI-related risks such as the generation of biased, harmful, or offensive content, the propagation of misinformation, and ensuring fairness in agent responses and actions.111  
  * This involves meticulous prompt engineering to guide the LLM's behavior, the implementation of content filters (either custom or provided by the LLM service), and potentially incorporating human-in-the-loop processes for reviewing or approving agent actions in sensitive scenarios.116  
  * Transparency about the agent's capabilities, limitations, and data usage is also a key RAI principle.

### **9.3. Implications for Microsoft 365 Agent Store / Commercial Marketplace Submission**

Agents intended for broader distribution via the Microsoft 365 Agent Store or the Microsoft Commercial Marketplace will undergo a certification process.

* **Stricter Scrutiny for Non-Azure LLMs:** Agents utilizing non-Azure LLMs are likely to face detailed scrutiny regarding their implemented RAI measures, data security protocols, and overall compliance posture. ISVs will need to clearly articulate how they ensure the safety and trustworthiness of the external LLM components.  
* **Adherence to Marketplace Policies:** All offers submitted to the Microsoft Commercial Marketplace must comply with the prevailing certification policies.117 These policies cover a range of areas including technical validation (e.g., Azure-compatible infrastructure, API performance, security protocols), security compliance (potentially requiring certifications like ISO 27001 if handling sensitive data), clear privacy policies, and terms of use.118  
* **AI-Powered Application Guidelines:** Existing guidelines for AI-powered apps in the Teams Store (which may serve as a precursor or parallel to M365 Agent Store guidelines) emphasize the need for a clear description of AI functionality, user-facing mechanisms for reporting inappropriate or harmful content, and ensuring a high quality of agent responses.109 These types of requirements are expected to be central to M365 Agent validation.  
* **Disclosure of External Service Dependencies:** The use of non-Azure LLMs constitutes a significant external service dependency. Marketplace policies typically require clear disclosure of such dependencies and evidence of robust error handling and security measures related to them.

### **9.4. Advanced Considerations for RAI and Store Submission**

The use of non-Azure LLMs by M365 Agents introduces a **"trust boundary" challenge**. Microsoft has invested heavily in its own RAI framework and security measures for Azure OpenAI services. When an M365 Agent incorporates a non-Azure LLM, the core reasoning engine operates outside Microsoft's direct RAI and security oversight for that specific LLM's processing. Microsoft's RAI validation for declarative agents 109 likely leverages internal tools and models optimized for and integrated with Azure OpenAI. Validating the RAI posture of an agent whose intelligence is powered by an external model (e.g., Gemini, or a model accessed via OpenRouter.AI) is inherently more complex for Microsoft. The validation process would likely need to rely more heavily on the ISV's attestations, documented processes, evidence of testing, and potentially the external LLM provider's own RAI commitments and certifications. This creates a "delegated trust" model, where Project Nucleus, as the ISV, becomes responsible for vouching for the RAI safety and compliance of the non-Azure LLM components integrated into their M365 Agents. Consequently, Project Nucleus must be prepared to provide comprehensive documentation and tangible evidence of their due diligence regarding any non-Azure LLM. This includes detailing the selection criteria for the LLM, the methods used to monitor its outputs for safety and accuracy, the implementation of content filtering mechanisms, and any contractual agreements with the LLM provider concerning RAI, data handling, and security. This level of preparedness could be a decisive factor in achieving marketplace approval and gaining enterprise customer trust.

Furthermore, there's a consideration regarding the potential for **"RAI arbitrage" and the imperative for consistent standards across the M365 ecosystem**. If the RAI validation process for custom engine agents using non-Azure LLMs is perceived as significantly different or less stringent than that for agents built exclusively with Azure OpenAI, it could lead to inconsistencies in the safety, reliability, and trustworthiness of agents available to M365 users. Microsoft has a compelling interest in ensuring that all agents interacting with Microsoft 365 data and users meet a consistently high RAI bar, irrespective of the underlying LLM technology. If developers perceive it as "easier" to pass RAI checks with a non-Azure LLM simply because the burden of proof falls more heavily on them 110, this could introduce risks. It is more plausible that Microsoft will expect developers of custom engine agents to meet equivalent RAI *outcomes* to those achieved with Azure OpenAI, even if the specific methods to achieve and demonstrate this alignment differ. Therefore, Project Nucleus should proactively design its agents to align with the spirit and principles of Microsoft's RAI framework (e.g., robust content filtering, mechanisms for detecting and mitigating jailbreaks or harmful outputs, promoting fairness and transparency) even when employing non-Azure LLMs. A proactive stance, demonstrating a commitment to these principles with concrete evidence, will be far more effective than merely stating that the external LLM is the ISV's sole responsibility.

## **10\. Conclusion and Recommendations**

This report has detailed advanced implementation strategies for Project Nucleus, focusing on proactive messaging, complex configuration management, integration of non-Azure LLMs, multi-tenant ISV patterns, MCP tool invocation,.NET Aspire for Azure deployment, and definitive network isolation architectures. The M365 Agents SDK provides a flexible foundation, but successful execution of advanced scenarios requires careful attention to emerging patterns and specific technical nuances.

**Key Architectural Patterns and Definitive Answers:**

* **Proactive Messaging:** Achievable via ChannelAdapter.ContinueConversationAsync, requiring careful DI setup of the adapter, IAccessTokenProvider, and RestChannelServiceClientFactory in background services like Azure Functions. Authentication in non-request contexts relies on the configured IAccessTokenProvider.  
* **Complex Configuration:** IOptions\<T\> combined with Azure App Configuration (for general settings and Key Vault references) and Azure Key Vault (for secrets) provides a robust solution. The \-- convention for hierarchical Key Vault secret names is essential..NET Aspire simplifies resource provisioning and connection management for these services in Azure.  
* **Non-Azure LLM Integration:** The M365 Agents SDK's agnosticism, coupled with orchestrators like Semantic Kernel or abstractions like IChatClient, facilitates integration. However, tool calling and streaming require careful attention to schema compatibility (OpenAI vs. Gemini vs. OpenRouter standards) and provider-specific limitations.  
* **Multi-Tenant ISV Design:** A single multi-tenant Microsoft Entra app registration with app roles for persona/functionality differentiation is generally recommended. Backend APIs must validate the tid claim for tenant isolation, and this tid must drive data tenancy in Cosmos DB. New Cosmos DB features (GSIs, filtered vector search, RU pooling, PPAF) offer significant optimization opportunities for multi-tenant SaaS. Entra Agent ID is an emerging factor to monitor for agent identity management.  
* **MCP Tool Invocation:** Backend Nucleus capabilities exposed as MCP tools require clear definitions (name, description, input schema) for LLM consumption. The M365 Agent's LLM uses its native tool calling, translated by the SDK or orchestrator (like Semantic Kernel) into MCP requests.  
* **.NET Aspire for Azure Deployment:** azd leverages the Aspire manifest to deploy to Azure Container Apps. Service discovery in ACA relies on built-in DNS, with Aspire injecting configuration via environment variables. Managed Identities are key for secure service-to-service authentication. Health checks and OpenTelemetry integrate with ACA's observability.  
* **Network Isolation:** VNet-hosted agents require either Azure Application Gateway with WAF or Azure API Management to securely expose their messaging endpoint to public Bot Service channels. Azure Firewall should control all VNet egress, with specific FQDN/Service Tag allowlists for Azure services and external LLMs. Direct public IP access to agent compute must be prevented.

**Key Recommendations for Project Nucleus:**

1. **Proactive Messaging DI:** Actively seek or contribute to reference implementations for DI setup of ChannelAdapter and its authentication dependencies (IAccessTokenProvider, RestChannelServiceClientFactory) within Azure Functions or similar serverless background services for the M365 Agents SDK.  
2. **Configuration Schema Management:** Implement a versioning strategy for complex configuration objects like PersonaConfiguration and design for dynamic refresh using IOptionsMonitor\<T\> for applicable settings, ensuring agent components can adapt safely.  
3. **Non-Azure LLM Tooling Rigor:** For each non-Azure LLM considered, conduct thorough testing of tool calling and streaming capabilities through the chosen abstraction layer (Semantic Kernel, IChatClient). Develop LLM-specific tool descriptions or prompt augmentations if necessary to ensure reliable tool use.  
4. **Entra ID App Role Design:** Adopt the single multi-tenant app registration model. Meticulously design app roles to correspond to distinct Persona Agent functionalities, ensuring they align with the principle of least privilege when customer admins grant consent.  
5. **Exploit New Cosmos DB Features:** Strategically implement Global Secondary Indexes, filtered vector search, and investigate RU pooling for the ArtifactMetadataContainer and KnowledgeContainers to optimize performance, search relevance, and cost in the multi-tenant environment.  
6. **MCP Tool Definition Quality:** Invest significant effort in crafting clear, unambiguous, and semantically rich MCP tool definitions (name, description, input schema) for all backend Nucleus capabilities to maximize LLM accuracy in tool selection and invocation.  
7. **.NET Aspire Manifest Validation:** Establish a practice of reviewing the Aspire-generated deployment manifest (manifest.json) and the derived Bicep templates before azd up to ensure they accurately reflect the intended Azure resource configuration and inter-service dependencies.  
8. **Network Egress Control for LLMs:** For external LLM FQDNs, prioritize Azure Firewall Application Rules. Continuously monitor and update rules based on provider guidance due to potentially dynamic IPs.  
9. **RAI Documentation for Non-Azure LLMs:** Proactively develop comprehensive documentation detailing the RAI diligence, safety measures, content filtering, and compliance adherence for any non-Azure LLM used. This will be crucial for Microsoft 365 Agent Store submission and enterprise adoption.  
10. **Entra Agent ID Monitoring:** Closely monitor the evolution of Microsoft Entra Agent ID and its integration with M365 Copilot agents and third-party tools, assessing its impact on the ISV agent identity and permission model.

**Future Considerations and Emerging Trends:**

Project Nucleus should remain agile and monitor advancements in several key areas:

* **Evolution of Entra Agent ID:** As Entra Agent ID matures and extends to M365 Copilot and third-party tools, its role in defining and managing ISV agent identities and permissions will become clearer. This could influence app registration strategies and how customer tenants govern agent access.  
* **Standardization of Model Context Protocol (MCP):** Wider adoption and standardization of MCP, along with richer SDK support and community-driven tool servers, will simplify the exposure and consumption of backend capabilities for AI agents.  
* **LLM Capabilities and Abstractions:** The pace of innovation in LLMs continues to be rapid. New models with enhanced reasoning, tool use, and modality support will emerge. Abstraction layers like Semantic Kernel and Microsoft.Extensions.AI will need to evolve to harness these new capabilities while maintaining interoperability.  
* **Serverless and Edge AI for Agents:** As agent functionalities become more distributed, patterns for deploying agent components or MCP tools to serverless platforms (beyond Azure Functions) or even edge devices might become relevant for specific use cases.

By addressing the advanced implementation details outlined in this report and staying attuned to these evolving trends, Project Nucleus can build a robust, secure, and highly capable M365 Agent platform.

#### **Works cited**

1. Migration guidance from Azure Bot Framework SDK to the Microsoft 365 Agents SDK, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance)  
2. ChannelAdapter.ContinueConversationAsync Method (Microsoft ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.channeladapter.continueconversationasync?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.channeladapter.continueconversationasync?view=m365-agents-sdk)  
3. IChannelAdapter.ContinueConversationAsync Method (Microsoft.Agents.Builder), accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.ichanneladapter.continueconversationasync?view=m365-agents-sdk](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.builder.ichanneladapter.continueconversationasync?view=m365-agents-sdk)  
4. Quickstart: Create an agent with the Agents SDK \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/create-test-basic-agent)  
5. Building agents with Agents SDK | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/how-agent-works-sdk)  
6. Microsoft 365 Agents SDK, accessed May 24, 2025, [https://microsoft.github.io/Agents/](https://microsoft.github.io/Agents/)  
7. What is the Microsoft 365 Agents SDK | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/agents-sdk-overview)  
8. Make bot send a message to a certain user in response to another user action · Issue \#1957 · microsoft/botframework-sdk \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/botframework-sdk/issues/1957](https://github.com/microsoft/botframework-sdk/issues/1957)  
9. BotFrameworkAdapter class \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/javascript/api/botbuilder/botframeworkadapter?view=botbuilder-ts-latest](https://learn.microsoft.com/en-us/javascript/api/botbuilder/botframeworkadapter?view=botbuilder-ts-latest)  
10. BotAdapter class | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/javascript/api/botbuilder-core/botadapter?view=botbuilder-ts-latest](https://learn.microsoft.com/en-us/javascript/api/botbuilder-core/botadapter?view=botbuilder-ts-latest)  
11. microsoft/Agents-for-net: This repository is for active ... \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/Agents-for-net](https://github.com/microsoft/Agents-for-net)  
12. Options pattern in ASP.NET Core | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0)  
13. Configuration in ASP.NET Core | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0)  
14. Beginner's Guide \- How to Configure Application Settings in .NET Core \- MoldStud, accessed May 24, 2025, [https://moldstud.com/articles/p-beginners-guide-how-to-configure-application-settings-in-net-core](https://moldstud.com/articles/p-beginners-guide-how-to-configure-application-settings-in-net-core)  
15. .NET Configuration Provider \- Azure App Configuration | Microsoft ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/reference-dotnet-provider](https://learn.microsoft.com/en-us/azure/azure-app-configuration/reference-dotnet-provider)  
16. Azure Key Vault configuration provider in ASP.NET Core | Microsoft ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0)  
17. Tutorial: Use Key Vault references in an ASP.NET Core app, accessed May 24, 2025, [https://docs.azure.cn/en-us/azure-app-configuration/use-key-vault-references-dotnet-core](https://docs.azure.cn/en-us/azure-app-configuration/use-key-vault-references-dotnet-core)  
18. Tutorial: Use Key Vault references in an ASP.NET Core app \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-key-vault-references-dotnet-core](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-key-vault-references-dotnet-core)  
19. App Settings config section from Azure Key Vault \- Stack Overflow, accessed May 24, 2025, [https://stackoverflow.com/questions/66477321/app-settings-config-section-from-azure-key-vault](https://stackoverflow.com/questions/66477321/app-settings-config-section-from-azure-key-vault)  
20. Azure App Configuration integration \- .NET Aspire | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-configuration-integration](https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-configuration-integration)  
21. NET Aspire Azure Key Vault integration \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/security/azure-security-key-vault-integration](https://learn.microsoft.com/en-us/dotnet/aspire/security/azure-security-key-vault-integration)  
22. .NET Aspire Integrations (SQL Server Integration in Aspire Applications) \- C\# Corner, accessed May 24, 2025, [https://www.c-sharpcorner.com/article/net-aspire-integrations-sql-server-integration-in-aspire-applications/](https://www.c-sharpcorner.com/article/net-aspire-integrations-sql-server-integration-in-aspire-applications/)  
23. NET Aspire overview \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)  
24. docs-aspire/docs/whats-new/dotnet-aspire-9.2.md at main \- GitHub, accessed May 24, 2025, [https://github.com/dotnet/docs-aspire/blob/main/docs/whats-new/dotnet-aspire-9.2.md](https://github.com/dotnet/docs-aspire/blob/main/docs/whats-new/dotnet-aspire-9.2.md)  
25. Authenticate Azure-hosted .NET apps to Azure resources using a system-assigned managed identity \- .NET | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/system-assigned-managed-identity](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/system-assigned-managed-identity)  
26. Choose the right agent solution to support your use case | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/choose-agent-solution)  
27. What's new in .NET Aspire 9.2 \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.2](https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.2)  
28. Tutorial: Use dynamic configuration in a .NET background service \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-dotnet-background-service](https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-dotnet-background-service)  
29. Create and Deploy a Custom Engine Agent with Microsoft 365 Agents SDK, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/create-deploy-agents-sdk)  
30. Semantic Kernel Agent Framework | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/)  
31. Function calling with chat completion | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/)  
32. Microsoft.Extensions.AI libraries \- .NET | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)  
33. Function Calling with the Gemini API | Google AI for Developers, accessed May 24, 2025, [https://ai.google.dev/gemini-api/docs/function-calling](https://ai.google.dev/gemini-api/docs/function-calling)  
34. Gemini AI Structured Output with references via OpenAI SDK \- Stack Overflow, accessed May 24, 2025, [https://stackoverflow.com/questions/79588648/gemini-ai-structured-output-with-references-via-openai-sdk](https://stackoverflow.com/questions/79588648/gemini-ai-structured-output-with-references-via-openai-sdk)  
35. OpenRouter FAQ | Developer Documentation, accessed May 24, 2025, [https://openrouter.ai/docs/faq](https://openrouter.ai/docs/faq)  
36. Tool & Function Calling | Use Tools with OpenRouter, accessed May 24, 2025, [https://openrouter.ai/docs/features/tool-calling](https://openrouter.ai/docs/features/tool-calling)  
37. Gemini models via Openrouter not supported · Issue \#5621 · microsoft/autogen \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/autogen/issues/5621](https://github.com/microsoft/autogen/issues/5621)  
38. OpenAI compatibility | Gemini API | Google AI for Developers, accessed May 24, 2025, [https://ai.google.dev/gemini-api/docs/openai](https://ai.google.dev/gemini-api/docs/openai)  
39. Openrouter: Error inside tool result when using functions without arguments · Issue \#5666 · microsoft/autogen \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/autogen/issues/5666](https://github.com/microsoft/autogen/issues/5666)  
40. Build an AI agent bot in Teams \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/build-an-ai-agent-in-teams)  
41. API Streaming | Real-time Model Responses in OpenRouter, accessed May 24, 2025, [https://openrouter.ai/docs/api-reference/streaming](https://openrouter.ai/docs/api-reference/streaming)  
42. OpenAI library doesn't work with Gemini's OpenAI compat endpoint · Issue \#289 \- GitHub, accessed May 24, 2025, [https://github.com/openai/openai-dotnet/issues/289](https://github.com/openai/openai-dotnet/issues/289)  
43. Compare Gemini Live API vs. OpenAI Realtime API in 2025 \- Slashdot, accessed May 24, 2025, [https://slashdot.org/software/comparison/Gemini-Live-API-vs-OpenAI-Realtime-API/](https://slashdot.org/software/comparison/Gemini-Live-API-vs-OpenAI-Realtime-API/)  
44. Why does my code send 429 Quota Exceeds on Google Gemini API with generative UI?, accessed May 24, 2025, [https://stackoverflow.com/questions/78535824/why-does-my-code-send-429-quota-exceeds-on-google-gemini-api-with-generative-ui](https://stackoverflow.com/questions/78535824/why-does-my-code-send-429-quota-exceeds-on-google-gemini-api-with-generative-ui)  
45. google cloud platform \- Why am I getting a 429 \- resource exhausted despite being under my limit on the Gemini AI studio API despite using Pay as you Go? \- Stack Overflow, accessed May 24, 2025, [https://stackoverflow.com/questions/78588575/why-am-i-getting-a-429-resource-exhausted-despite-being-under-my-limit-on-the](https://stackoverflow.com/questions/78588575/why-am-i-getting-a-429-resource-exhausted-despite-being-under-my-limit-on-the)  
46. gunpal5/Google\_GenerativeAI: Most complete C\# .Net SDK for Google Generative AI and Vertex AI (Google Gemini), featuring function calling, easiest JSON Mode, multi-modal live streaming, chat sessions, and more\! \- GitHub, accessed May 24, 2025, [https://github.com/gunpal5/Google\_GenerativeAI](https://github.com/gunpal5/Google_GenerativeAI)  
47. API Error Handling | OpenRouter Error Documentation, accessed May 24, 2025, [https://openrouter.ai/docs/api-reference/errors](https://openrouter.ai/docs/api-reference/errors)  
48. Extend your agent with Model Context Protocol (preview) \- Microsoft Copilot Studio, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agent-extend-action-mcp)  
49. Handle remote tool calling with Model Context Protocol \- IBM Developer, accessed May 24, 2025, [https://developer.ibm.com/tutorials/awb-handle-remote-tool-calling-model-context-protocol](https://developer.ibm.com/tutorials/awb-handle-remote-tool-calling-model-context-protocol)  
50. Introducing Model Context Protocol (MCP) in Copilot Studio: Simplified Integration with AI Apps and Agents \- Microsoft, accessed May 24, 2025, [https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/introducing-model-context-protocol-mcp-in-copilot-studio-simplified-integration-with-ai-apps-and-agents/](https://www.microsoft.com/en-us/microsoft-copilot/blog/copilot-studio/introducing-model-context-protocol-mcp-in-copilot-studio-simplified-integration-with-ai-apps-and-agents/)  
51. Unlocking AI Potential: Exploring the Model Context Protocol with AI Toolkit, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/educatordeveloperblog/unlocking-ai-potential-exploring-the-model-context-protocol-with-ai-toolkit/4411198](https://techcommunity.microsoft.com/blog/educatordeveloperblog/unlocking-ai-potential-exploring-the-model-context-protocol-with-ai-toolkit/4411198)  
52. MCP \- The Secret Sauce of AI Agents and Automation \- The Prompt Engineering Institute, accessed May 24, 2025, [https://promptengineering.org/mcp-the-secret-sauce-of-ai-agents-and-automation/](https://promptengineering.org/mcp-the-secret-sauce-of-ai-agents-and-automation/)  
53. How Does an LLM "See" MCP as a Client? \- Reddit, accessed May 24, 2025, [https://www.reddit.com/r/mcp/comments/1jl8j1n/how\_does\_an\_llm\_see\_mcp\_as\_a\_client/](https://www.reddit.com/r/mcp/comments/1jl8j1n/how_does_an_llm_see_mcp_as_a_client/)  
54. Integrating Model Context Protocol Tools with Semantic Kernel: A ..., accessed May 24, 2025, [https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/](https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/)  
55. Establish applications in the Microsoft Entra ID ecosystem \- GitHub, accessed May 24, 2025, [https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/architecture/establish-applications.md](https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/architecture/establish-applications.md)  
56. Building secure multi-tenant applications with Microsoft Entra ID: A guide for ISVs, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/marketplace-blog/building-secure-multi-tenant-applications-with-microsoft-entra-id-a-guide-for-is/4387862](https://techcommunity.microsoft.com/blog/marketplace-blog/building-secure-multi-tenant-applications-with-microsoft-entra-id-a-guide-for-is/4387862)  
57. Identity and account types for single- and multitenant apps \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/security/zero-trust/develop/identity-supported-account-types](https://learn.microsoft.com/en-us/security/zero-trust/develop/identity-supported-account-types)  
58. Application model \- Microsoft identity platform | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/application-model](https://learn.microsoft.com/en-us/entra/identity-platform/application-model)  
59. Add app roles and get them from a token \- Microsoft identity platform ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps)  
60. Single and multitenant apps in Microsoft Entra ID \- Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/single-and-multi-tenant-apps](https://learn.microsoft.com/en-us/entra/identity-platform/single-and-multi-tenant-apps)  
61. How many app registration should I create for an app? \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/1168458/how-many-app-registration-should-i-create-for-an-a](https://learn.microsoft.com/en-us/answers/questions/1168458/how-many-app-registration-should-i-create-for-an-a)  
62. Service principal profiles for multitenancy apps in Power BI Embedded \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/power-bi/developer/embedded/embed-multi-tenancy](https://learn.microsoft.com/en-us/power-bi/developer/embedded/embed-multi-tenancy)  
63. Securing service principals in Microsoft Entra ID \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/architecture/service-accounts-principal](https://learn.microsoft.com/en-us/entra/architecture/service-accounts-principal)  
64. Grant tenant-wide admin consent to an application \- Microsoft Entra ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/grant-admin-consent](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/grant-admin-consent)  
65. Review permissions granted to enterprise applications \- Microsoft Entra ID, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/manage-application-permissions](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/manage-application-permissions)  
66. Tutorial: Build and secure an ASP.NET Core web API with the Microsoft identity platform, accessed May 24, 2025, [https://docs.azure.cn/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app](https://docs.azure.cn/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app)  
67. How to secure an ASP.NET Core Web API with the Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/samples/azure-samples/ms-identity-ciam-dotnet-tutorial/ms-identity-ciam-dotnet-tutorial-1-call-own-api-aspnet-core-mvc/](https://learn.microsoft.com/en-us/samples/azure-samples/ms-identity-ciam-dotnet-tutorial/ms-identity-ciam-dotnet-tutorial-1-call-own-api-aspnet-core-mvc/)  
68. Issue : OAuth Token Generation in Azure AD with Client Credentials Flow for not permitted scopes \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2264781/issue-oauth-token-generation-in-azure-ad-with-clie](https://learn.microsoft.com/en-us/answers/questions/2264781/issue-oauth-token-generation-in-azure-ad-with-clie)  
69. Tutorial: Build and secure an ASP.NET Core web API with the Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app](https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-api-dotnet-core-build-app)  
70. Azure AD: Validate access\_token \- Microsoft Q\&A, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/793793/azure-ad-validate-access-token](https://learn.microsoft.com/en-us/answers/questions/793793/azure-ad-validate-access-token)  
71. Secure applications and APIs by validating claims \- Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/claims-validation](https://learn.microsoft.com/en-us/entra/identity-platform/claims-validation)  
72. Convert single-tenant app to multitenant on Microsoft Entra ID ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/howto-convert-app-to-be-multi-tenant](https://learn.microsoft.com/en-us/entra/identity-platform/howto-convert-app-to-be-multi-tenant)  
73. Need help setting up isolation models for secure multi tennancy service \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2240069/need-help-setting-up-isolation-models-for-secure-m](https://learn.microsoft.com/en-us/answers/questions/2240069/need-help-setting-up-isolation-models-for-secure-m)  
74. How to isolate each tenant's data in a multi-tenant SAAS application in Azure?, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/1920935/how-to-isolate-each-tenants-data-in-a-multi-tenant](https://learn.microsoft.com/en-us/answers/questions/1920935/how-to-isolate-each-tenants-data-in-a-multi-tenant)  
75. Announced at Build 2025: Foundry connection for Azure Cosmos ..., accessed May 24, 2025, [https://devblogs.microsoft.com/cosmosdb/announced-at-build-2025-foundry-connection-for-azure-cosmos-db-global-secondary-index-full-text-search-and-more/](https://devblogs.microsoft.com/cosmosdb/announced-at-build-2025-foundry-connection-for-azure-cosmos-db-global-secondary-index-full-text-search-and-more/)  
76. New Generally Available and Preview Search Capabilities in Azure Cosmos DB for NoSQL, accessed May 24, 2025, [https://devblogs.microsoft.com/cosmosdb/new-generally-available-and-preview-search-capabilities-in-azure-cosmos-db-for-nosql/](https://devblogs.microsoft.com/cosmosdb/new-generally-available-and-preview-search-capabilities-in-azure-cosmos-db-for-nosql/)  
77. Powering the next AI frontier with Microsoft Fabric and the Azure data portfolio, accessed May 24, 2025, [https://azure.microsoft.com/en-us/blog/powering-the-next-ai-frontier-with-microsoft-fabric-and-the-azure-data-portfolio/](https://azure.microsoft.com/en-us/blog/powering-the-next-ai-frontier-with-microsoft-fabric-and-the-azure-data-portfolio/)  
78. Azure Cosmos DB Conf 2025 Recap: AI, Apps & Scale : r/CosmosDB \- Reddit, accessed May 24, 2025, [https://www.reddit.com/r/CosmosDB/comments/1k5arjs/azure\_cosmos\_db\_conf\_2025\_recap\_ai\_apps\_scale/](https://www.reddit.com/r/CosmosDB/comments/1k5arjs/azure_cosmos_db_conf_2025_recap_ai_apps_scale/)  
79. Announcing Microsoft Entra Agent ID: Secure and manage your AI agents, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392](https://techcommunity.microsoft.com/blog/microsoft-entra-blog/announcing-microsoft-entra-agent-id-secure-and-manage-your-ai-agents/3827392)  
80. Microsoft extends Zero Trust to secure the agentic workforce, accessed May 24, 2025, [https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/](https://www.microsoft.com/en-us/security/blog/2025/05/19/microsoft-extends-zero-trust-to-secure-the-agentic-workforce/)  
81. Find multi-tenant applications using weak authentication methods \- Our Cloud Network, accessed May 24, 2025, [https://ourcloudnetwork.com/find-multi-tenant-applications-using-weak-authentication-methods/](https://ourcloudnetwork.com/find-multi-tenant-applications-using-weak-authentication-methods/)  
82. How to use Azure AI Foundry Agent Service with function calling ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/function-calling](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/function-calling)  
83. Build MCP Remote Servers with Azure Functions \- .NET Blog, accessed May 24, 2025, [https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/](https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions/)  
84. Build MCP Remote Servers with Azure Functions \- .NET Blog, accessed May 24, 2025, [https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions](https://devblogs.microsoft.com/dotnet/build-mcp-remote-servers-with-azure-functions)  
85. NET Aspire inner loop networking overview \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview)  
86. docs-aspire/docs/deployment/azure/aca-deployment-azd-in ... \- GitHub, accessed May 24, 2025, [https://github.com/dotnet/docs-aspire/blob/main/docs/deployment/azure/aca-deployment-azd-in-depth.md](https://github.com/dotnet/docs-aspire/blob/main/docs/deployment/azure/aca-deployment-azd-in-depth.md)  
87. How to deploy .NET Aspire apps to Azure Container Apps \- Microsoft Developer Blogs, accessed May 24, 2025, [https://devblogs.microsoft.com/dotnet/how-to-deploy-dotnet-aspire-apps-to-azure-container-apps/](https://devblogs.microsoft.com/dotnet/how-to-deploy-dotnet-aspire-apps-to-azure-container-apps/)  
88. Deploy .NET Aspire projects to Azure Container Apps \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment)  
89. Architecture Best Practices for Azure Container Apps \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-container-apps](https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-container-apps)  
90. Authenticate Azure-hosted .NET apps to Azure resources using a user-assigned managed identity \- .NET | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/user-assigned-managed-identity](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/user-assigned-managed-identity)  
91. Deploy multiple containers to Azure \- Visual Studio (Windows) | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/visualstudio/containers/deploy-multicontainer-azure-container-apps?view=vs-2022](https://learn.microsoft.com/en-us/visualstudio/containers/deploy-multicontainer-azure-container-apps?view=vs-2022)  
92. What's new in .NET Aspire 9.3 \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.3](https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.3)  
93. How to create Azure Bot service in a Private network and integrate with MS Teams application \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo](https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo)  
94. Introduction to Azure security | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/security/fundamentals/overview](https://learn.microsoft.com/en-us/azure/security/fundamentals/overview)  
95. Azure security baseline for Azure Bot Service | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline](https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline)  
96. Azure Network Security Best Practices To Protect Your Cloud Infrastructure | Build5Nines, accessed May 24, 2025, [https://build5nines.com/azure-network-security-best-practices-to-protect-your-cloud-infrastructure/](https://build5nines.com/azure-network-security-best-practices-to-protect-your-cloud-infrastructure/)  
97. Baseline OpenAI End-to-End Chat Reference Architecture \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat)  
98. Architecture Best Practices for Azure Application Gateway v2 \- Microsoft Azure Well-Architected Framework, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-application-gateway](https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-application-gateway)  
99. Use private endpoints in the classic Microsoft Purview governance portal, accessed May 24, 2025, [https://learn.microsoft.com/en-us/purview/data-gov-classic-private-link](https://learn.microsoft.com/en-us/purview/data-gov-classic-private-link)  
100. Architecture Best Practices for Azure API Management \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-api-management](https://learn.microsoft.com/en-us/azure/well-architected/service-guides/azure-api-management)  
101. Deploy Azure API Management instance to internal VNet | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/api-management/api-management-using-with-internal-vnet](https://learn.microsoft.com/en-us/azure/api-management/api-management-using-with-internal-vnet)  
102. Overview of Azure Firewall service tags | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/firewall/service-tags](https://learn.microsoft.com/en-us/azure/firewall/service-tags)  
103. Block access to LLM applications using keywords and FQDN ..., accessed May 24, 2025, [https://docs.fortinet.com/document/fortigate/7.6.2/administration-guide/116184/block-access-to-llm-applications-using-keywords-and-fqdn](https://docs.fortinet.com/document/fortigate/7.6.2/administration-guide/116184/block-access-to-llm-applications-using-keywords-and-fqdn)  
104. Configure VPC Service Controls for Gemini | Gemini Code Assist ..., accessed May 24, 2025, [https://developers.google.com/gemini-code-assist/docs/configure-vpc-service-controls](https://developers.google.com/gemini-code-assist/docs/configure-vpc-service-controls)  
105. Provisioning API Keys | Programmatic Control of OpenRouter API Keys, accessed May 24, 2025, [https://openrouter.ai/docs/features/provisioning-api-keys](https://openrouter.ai/docs/features/provisioning-api-keys)  
106. Private Link \- Microsoft Azure, accessed May 24, 2025, [https://azure.microsoft.com/en-us/products/private-link](https://azure.microsoft.com/en-us/products/private-link)  
107. GitHub \- Azure-Samples/function-app-with-private-http-endpoint, accessed May 24, 2025, [https://github.com/Azure-Samples/function-app-with-private-http-endpoint](https://github.com/Azure-Samples/function-app-with-private-http-endpoint)  
108. Use private endpoints to integrate Azure Functions with a virtual network | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-vnet](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-vnet)  
109. Responsible AI Validation Checks for Declarative Agents | Microsoft ..., accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/rai-validation](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/rai-validation)  
110. Agents for Microsoft 365 Copilot, accessed May 24, 2025, [https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/agents-overview)  
111. Deploy large language models responsibly with Azure AI \- Microsoft Tech Community, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/machinelearningblog/deploy-large-language-models-responsibly-with-azure-ai/3876792](https://techcommunity.microsoft.com/blog/machinelearningblog/deploy-large-language-models-responsibly-with-azure-ai/3876792)  
112. Microsoft Build 2025 Book of News, accessed May 24, 2025, [https://news.microsoft.com/build-2025-book-of-news/](https://news.microsoft.com/build-2025-book-of-news/)  
113. Data Protection Impact Assessment (DPIA) – Microsoft CoPilot 365 \- Information Commissioner's Office, accessed May 24, 2025, [https://ico.org.uk/media2/ob4ncmpz/ic-359252-x5s0-copilot-dpia.pdf](https://ico.org.uk/media2/ob4ncmpz/ic-359252-x5s0-copilot-dpia.pdf)  
114. Learn about data governance with Microsoft Purview, accessed May 24, 2025, [https://learn.microsoft.com/en-us/purview/data-governance-overview](https://learn.microsoft.com/en-us/purview/data-governance-overview)  
115. Microsoft Purview Data Governance | Microsoft Security, accessed May 24, 2025, [https://www.microsoft.com/en-us/security/business/risk-management/microsoft-purview-data-governance](https://www.microsoft.com/en-us/security/business/risk-management/microsoft-purview-data-governance)  
116. AI Agents: Mastering Agentic RAG \- Part 5 | Microsoft Community Hub, accessed May 24, 2025, [https://techcommunity.microsoft.com/blog/educatordeveloperblog/ai-agents-mastering-agentic-rag---part-5/4396171](https://techcommunity.microsoft.com/blog/educatordeveloperblog/ai-agents-mastering-agentic-rag---part-5/4396171)  
117. Commercial marketplace policies and terms \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/partner-center/marketplace-offers/policies-terms](https://learn.microsoft.com/en-us/partner-center/marketplace-offers/policies-terms)  
118. Common Azure Marketplace Publishing Challenges Solved \- WeTransact, accessed May 24, 2025, [https://www.wetransact.io/blog/common-azure-marketplace-publishing-challenges-solved](https://www.wetransact.io/blog/common-azure-marketplace-publishing-challenges-solved)