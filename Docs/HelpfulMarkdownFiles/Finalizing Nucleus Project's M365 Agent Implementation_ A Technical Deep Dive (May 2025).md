# **Finalizing Nucleus Project's M365 Agent Implementation: A Technical Deep Dive (May 2025\)**

This report provides definitive, actionable answers to critical implementation questions for Project Nucleus's adoption of the Microsoft 365 Agents SDK and Model Context Protocol (MCP). It builds upon the assumed baseline understanding that each Nucleus Persona will be a distinct M365 Agent application, leveraging backend MCP Tool/Server applications for core capabilities, and employing specific Azure services for data persistence, asynchronous tasks, and AI model integration.

## **I. CRITICAL: Proactive Messaging from Background Services (.NET M365 Agents SDK)**

Enabling M365 Agents to send proactive messages initiated by decoupled background services (e.g., Azure Functions, IHostedService workers processing Azure Service Bus messages) is crucial for Project Nucleus. This requires careful Dependency Injection (DI) setup for the M365 Agents SDK components and robust authentication to the Bot Connector Service using Managed Identities.

**A. Dependency Injection (DI) Setup for Proactive Messaging**

The ability of a background service to send proactive messages hinges on correctly instantiating and configuring the necessary M365 Agents SDK adapter components. This typically involves an adapter like the CloudAdapter (or a more M365-specific IAgentHttpAdapter if available and recommended by May 2025\) and an IAccessTokenProvider for authentication.

**1\. DI Registration in Azure Functions (Isolated Worker) or IHostedService**

  For an Azure Function running in an isolated worker process or a generic.NET \`IHostedService\`, the DI configuration in \`Program.cs\` (or a \`Startup.cs\` equivalent) is paramount. The core objective is to register the M365 Agent adapter and its dependencies, particularly configuring it to use \`DefaultAzureCredential\` for acquiring tokens via an \`IAccessTokenProvider\`. This approach leverages Managed Identity, enhancing security by eliminating the need to manage explicit application secrets for Bot Connector authentication within the background service.

  The following C\# conceptual code illustrates the DI setup:

  \`\`\`csharp  
  // Program.cs for an Azure Function (Isolated Worker) or IHostedService  
  using Microsoft.Extensions.DependencyInjection;  
  using Microsoft.Extensions.Hosting;  
  using Microsoft.Extensions.Logging;  
  using Azure.Identity; // For DefaultAzureCredential  
  using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;  
  using Microsoft.Bot.Builder.Integration.AspNet.Core; // May contain CloudAdapter or similar  
  using Microsoft.Bot.Connector.Authentication; // For IAccessTokenProvider, AuthenticationConstants  
  // Potentially M365 Agents SDK specific namespaces for IAgentHttpAdapter or similar

  public class Program  
  {  
      public static async Task Main(string args)  
      {  
          var host \= new HostBuilder()  
             .ConfigureFunctionsWorkerDefaults() // For Azure Functions  
              // For IHostedService, use.ConfigureServices directly  
             .ConfigureServices((hostContext, services) \=\>  
              {  
                  // 1\. Register DefaultAzureCredential as the IAccessTokenProvider  
                  // This allows the adapter to use Managed Identity for Bot Connector auth  
                  services.AddSingleton\<IAccessTokenProvider\>(sp \=\>  
                  {  
                      // The DefaultAzureCredential will attempt multiple auth flows,  
                      // including Managed Identity when deployed to Azure.  
                      var credential \= new DefaultAzureCredential();  
                      return new ManagedIdentityTokenProvider(credential, AuthenticationConstants.ToBotFrameworkEmulatorTokenApi);  
                      // Note: The scope might need to be adjusted for direct Bot Connector API calls,  
                      // e.g., "https://api.botframework.com/.default"  
                      // The M365 Agents SDK might provide a specific constant or configuration for this.  
                  });

                  // 2\. Register the Bot/Agent Adapter (e.g., CloudAdapter or IAgentHttpAdapter)  
                  // The specific adapter and its registration method may vary with the M365 Agents SDK version.  
                  // Assuming CloudAdapter or a similar modern equivalent:  
                  services.AddSingleton\<BotFrameworkHttpAdapter, CloudAdapterWithErrorHandler\>();  
                  // Or, if a specific M365 Agent adapter is available:  
                  // services.AddSingleton\<IAgentHttpAdapter, YourM365AgentHttpAdapterImplementation\>();

                  // Register other dependencies the adapter might need, e.g., HttpClientFactory  
                  services.AddHttpClient();

                  // Register ILogger, IConfiguration, etc. as needed by your services  
                  services.AddLogging();

                  // Register your background service (e.g., Service Bus queue processor)  
                  // services.AddHostedService\<MyProactiveMessagingService\>(); // For IHostedService  
                  // For Azure Functions, the function trigger itself is the entry point.  
              })  
             .Build();

          await host.RunAsync();  
      }  
  }

  // Example IAccessTokenProvider implementation using DefaultAzureCredential  
  public class ManagedIdentityTokenProvider : IAccessTokenProvider  
  {  
      private readonly DefaultAzureCredential \_credential;  
      private readonly string \_resource;

      public ManagedIdentityTokenProvider(DefaultAzureCredential credential, string resource)  
      {  
          \_credential \= credential?? throw new ArgumentNullException(nameof(credential));  
          \_resource \= resource?? throw new ArgumentNullException(nameof(resource)); // e.g., "https://api.botframework.com/.default"  
      }

      public async Task\<string\> GetAccessTokenAsync(CancellationToken cancellationToken \= default)  
      {  
          var token \= await \_credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new { \_resource }), cancellationToken);  
          return token.Token;  
      }

      public Task\<string\> GetAccessTokenAsync(string scope, CancellationToken cancellationToken \= default)  
      {  
          // This overload might be used by newer SDK components.  
          // Ensure the scope provided here is compatible with what DefaultAzureCredential expects or adapt as needed.  
          // Typically, scope would be like "https://api.botframework.com/.default"  
          return GetAccessTokenAsync(cancellationToken);  
      }  
  }

  // Example error handler for CloudAdapter (adapt as needed)  
  public class CloudAdapterWithErrorHandler : CloudAdapter  
  {  
      public CloudAdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger\<CloudAdapterWithErrorHandler\> logger)  
          : base(auth, logger)  
      {  
          OnTurnError \= async (turnContext, exception) \=\>  
          {  
              logger.LogError(exception, $" unhandled error : {exception.Message}");  
              // Send a trace activity, send a message to the user, etc.  
              await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https.www.botframework.com/schemas/error", "TurnError");  
          };  
      }  
  }  
  \`\`\`  
  This setup ensures that when the adapter needs to communicate with the Bot Connector Service for proactive messaging, it will request a token via the registered \`IAccessTokenProvider\`, which in turn uses the Managed Identity of the hosting Azure service.\[1, 2\] The \`AddBotFrameworkAdapterIntegration\` method \[3\] or similar patterns from the M365 Agents SDK would be used if they provide higher-level abstractions for this registration.

**2\. Proactive Message Sending with ConversationReference**

  Once the DI is configured, the background service can use a stored \`ConversationReference\` to initiate a message. The core Bot Framework SDK method for this is \`ContinueConversationAsync\`, available on the adapter.

  The following conceptual C\# code, intended to be part of an Azure Function triggered by a Service Bus message or a method within an \`IHostedService\`, demonstrates this:

  \`\`\`csharp  
  // Inside an Azure Function or IHostedService method  
  using Microsoft.Bot.Builder;  
  using Microsoft.Bot.Schema;  
  using Microsoft.Extensions.Logging;  
  // Using M365 Agents SDK specific adapter if different from BotFrameworkHttpAdapter  
  // e.g., using Microsoft.Bot.Builder.Integration.AspNet.Core.BotFrameworkHttpAdapter;

  // Assume these are injected via DI:  
  // private readonly BotFrameworkHttpAdapter \_adapter; // Or IAgentHttpAdapter  
  // private readonly IAccessTokenProvider \_tokenProvider; // For auth if not handled by adapter directly  
  // private readonly string \_botAppId; // The M365 Agent's App ID (Entra Agent ID)  
  // private readonly ILogger \_logger;

  public async Task SendProactiveMessageAsync(ConversationReference storedConversationReference, string messageToSend)  
  {  
      if (storedConversationReference \== null)  
      {  
          // \_logger.LogError("ConversationReference is null. Cannot send proactive message.");  
          return;  
      }

      // The \_botAppId is the Microsoft Entra Agent ID of the specific Nucleus Persona Agent  
      // for whom this proactive message is being sent. This needs to be configurable and  
      // available to the background service, possibly from the Service Bus message payload or configuration.  
      string targetBotAppId \= storedConversationReference.Bot.Id; // Or retrieved from configuration if more reliable

      // The BotCallback delegate defines what happens when the conversation is continued.  
      // It receives an ITurnContext, allowing the bot to send activities.  
      async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)  
      {  
          await turnContext.SendActivityAsync(MessageFactory.Text(messageToSend), cancellationToken);  
          // \_logger.LogInformation("Proactive message sent successfully.");  
      }

      try  
      {  
          // Ensure the service URL is trusted if not handled automatically by the adapter/SDK version.  
          // For older ConnectorClient usage, this was explicit:  
          // MicrosoftAppCredentials.TrustServiceUrl(storedConversationReference.ServiceUrl); \[4\]  
          // Modern adapters like CloudAdapter often handle this internally based on configuration.  
          // If using IAccessTokenProvider, the adapter should be configured to use it.

          // Continue the conversation using the adapter.  
          // The adapter uses the IAccessTokenProvider (configured with DefaultAzureCredential)  
          // to authenticate its calls to the Bot Connector Service.  
          await \_adapter.ContinueConversationAsync(  
              botAppId: targetBotAppId, // Identifies the M365 Agent to act as  
              reference: storedConversationReference,  
              callback: BotCallback,  
              cancellationToken: CancellationToken.None); // Provide a proper CancellationToken  
      }  
      catch (Exception ex)  
      {  
          // \_logger.LogError(ex, "Error sending proactive message: {ErrorMessage}", ex.Message);  
          // Handle exceptions, retry logic, etc.  
      }  
  }  
  \`\`\`  
  The \`ConversationReference\` (obtained and stored by the M365 Agent during a previous user interaction) contains all necessary details (like \`serviceUrl\`, \`channelId\`, \`conversation.id\`) for the Bot Connector Service to route the message back to the correct user in the correct channel.\[5, 6\]

**3\. Authentication to Bot Connector Service via Managed Identity (DefaultAzureCredential)**

  When the background service (Azure Function or \`IHostedService\`) initiates a proactive message, its authentication to the Bot Connector Service is handled by the configured M365 Agent SDK adapter components, leveraging the service's Managed Identity.

  \*   \*\*Token Acquisition:\*\* The \`IAccessTokenProvider\` implementation (e.g., \`ManagedIdentityTokenProvider\` shown above) uses \`DefaultAzureCredential\` to acquire an OAuth 2.0 access token from Microsoft Entra ID. The token is requested for the Bot Connector Service resource (typically with a scope like \`https://api.botframework.com/.default\`). \`DefaultAzureCredential\` automatically attempts various authentication mechanisms, and when deployed to an Azure service with Managed Identity enabled (e.g., Azure Functions, App Service, Container Apps), it will use the Managed Identity to get the token without needing explicit credentials stored in the service.\[1, 2\]  
  \*   \*\*Adapter Usage:\*\* The adapter (e.g., \`CloudAdapter\` or \`IAgentHttpAdapter\`), when \`ContinueConversationAsync\` is called, internally makes HTTPS requests to the Bot Connector Service. It uses the \`IAccessTokenProvider\` to fetch the access token and includes it in the \`Authorization\` header of these requests.  
  \*   \*\*Permissions for Managed Identity:\*\* The Managed Identity of the Azure compute resource hosting the background service must be granted appropriate permissions to communicate with the Bot Connector Service. This typically means ensuring the Managed Identity's service principal has permissions like \`Microsoft.BotService/botServices/write\` or other relevant Bot Framework application permissions that allow it to send messages. This is distinct from the M365 Agent's own Entra Agent ID and its permissions within the customer tenant. The background service's Managed Identity authenticates \*itself\* to the Bot Connector, and then acts \*in the context of\* the specified \`botAppId\` (the M365 Agent's ID) when sending the message.  
  \*   \*\*\`botAppId\` in \`ContinueConversationAsync\`:\*\* It is important to understand the role of the \`botAppId\` parameter passed to \`ContinueConversationAsync\`. While the background service authenticates to the Bot Connector using its own Managed Identity, the \`botAppId\` specifies \*which M365 Agent Persona\* the proactive message should appear to originate from. The Bot Connector Service uses this \`botAppId\` (which is the M365 Agent's Entra Agent ID) to correctly process and attribute the message. The background service's Managed Identity must be authorized to perform this action, potentially requiring specific permissions that allow it to send messages for that \`botAppId\` or related conversations.  
  \*   \*\*Security Benefit:\*\* This approach significantly enhances security by removing the need to store and manage Bot App ID/Passwords (like \`MicrosoftAppCredentials\` \[4\]) directly within the background service's configuration for authenticating to the Bot Connector.

The combination of DI-configured adapters, IAccessTokenProvider with DefaultAzureCredential, and the ContinueConversationAsync method provides a robust and secure mechanism for Project Nucleus's background services to send proactive messages via its M365 Agents.

## **II. CRITICAL: Network Isolation for M365 Agents (Azure Bot Service "Last Mile")**

Securing the "last mile" of message delivery from public channels like Microsoft Teams to a VNet-isolated M365 Agent messaging endpoint is a critical architectural consideration. This section outlines the definitive Microsoft-recommended approach as of May 2025\.

**A. Definitive Network Architecture for VNet-Isolated Messaging Endpoints**

**1\. Microsoft-Recommended Secure Network Architecture (May 2025\)**

  For an M365 Agent hosted on VNet-isolated Azure compute (e.g., Azure Container Apps or App Service with only a Private Endpoint for all inbound HTTP/S traffic), direct communication from public channels like Microsoft Teams requires a carefully architected public ingress point. Microsoft Teams, being a public SaaS application, cannot directly connect into a private customer VNet without an intermediary.

  The definitive, Microsoft-recommended, and most secure network architecture as of May 2025 involves using a reverse proxy service like Azure Application Gateway (with Web Application Firewall \- WAF) or Azure API Management (APIM).\[7, 8\]

  \*   \*\*Traffic Flow:\*\*  
      1\.  A user sends a message via Microsoft Teams (or M365 Copilot).  
      2\.  The message is routed to the Azure Bot Service channel connector.  
      3\.  Azure Bot Service forwards the message to the publicly registered messaging endpoint of the M365 Agent.  
      4\.  This public endpoint is the public IP address of either Azure Application Gateway or Azure API Management.  
      5\.  Application Gateway/APIM receives the request, performs security checks (WAF, policy enforcement), terminates SSL, and then forwards the request over a private connection (e.g., via Private Link to the Private Endpoint) to the M365 Agent's compute resource (e.g., Azure Container App, App Service) within the VNet.  
      6\.  The M365 Agent's compute resource only accepts inbound traffic via its Private Endpoint.

  \*   \*\*Architectural Diagram (Conceptual):\*\*

      \`\`\`  
+---+     +---+     +---+     +---+     +---+

| Microsoft Teams | --> | Azure Bot Service | --> | Public Internet | --> | Azure Application Gateway| --> | Private Endpoint on | --> | M365 Agent in VNet |  
| (Public Cloud) | | (Channel Connector) | | (HTTPS to App GW/APIM Pub IP) | | (WAF enabled) / Azure APIM | | Customer VNet | | (e.g., ACA/App Svc) |  
+---+ +---+ +---+ +---+ +---+
(Public IP Address)  
\`\`\`

  \*   \*\*Security Components:\*\*  
      \*   \*\*Azure Application Gateway with WAF:\*\* Provides Layer 7 load balancing, SSL termination, and protection against common web exploits (e.g., OWASP Top 10).\[9, 10\]  
      \*   \*\*Azure API Management:\*\* Can be used for more advanced API gateway functionalities like request/response transformation, authentication/authorization policies (e.g., validating JWTs, client certificate authentication), rate limiting, and quota enforcement.\[7\]  
      \*   \*\*Private Endpoints:\*\* Ensure that the M365 Agent's compute resource is not directly exposed to the public internet; it only receives traffic from the Application Gateway/APIM via a private IP address within the VNet.  
      \*   \*\*Network Security Groups (NSGs):\*\* Applied to the subnets hosting Application Gateway/APIM and the M365 Agent's Private Endpoint to restrict traffic flow (e.g., allowing inbound traffic to App Gateway/APIM only from the \`BotFramework\` service tag, and allowing traffic from App Gateway/APIM subnet to the agent's private endpoint).  
      \*   \*\*Mutual TLS (mTLS) or other authentication:\*\* Can be configured between Application Gateway/APIM and the backend M365 Agent service for an additional layer of security.

  This architecture is consistently recommended because "Teams does not support bots that are hosted entirely inside private networks without public ingress".\[7\] The Azure Bot resource's messaging endpoint must be configured with the public URL of the Application Gateway or APIM.

**2\. Azure Bot Service Private Link/Private Endpoint Capabilities for ALL Channel Traffic**

  As of May 2025, Azure Bot Service itself \*\*does not offer\*\* robust Private Link capabilities that enable it to directly and privately connect to an agent's VNet-isolated endpoint for \*all incoming channel traffic\* from public channels like Microsoft Teams or M365 Copilot, thereby eliminating the need for any public IP exposure on the agent's compute or its fronting proxy.

  The Private Endpoints associated with Azure Bot Service, such as those for the \`Bot\` and \`Token\` sub-resources (e.g., \`yourbot.privatelink.directline.botframework.com\`), serve different purposes \[11, 12\]:  
  \*   \*\*Outbound Communication from Bot:\*\* They allow the VNet-isolated bot to securely make \*outbound\* calls to required Bot Framework services (e.g., for retrieving configuration, validating tokens from the token endpoint) without this traffic traversing the public internet. The \`Token\` sub-resource private endpoint facilitates access to the token endpoint, and the \`Bot\` sub-resource private endpoint is used by the Direct Line App Service Extension to access the Bot Service for meta-operations.\[12\]  
  \*   \*\*Direct Line App Service Extension (DL-ASE) Clients:\*\* They enable Direct Line clients \*that are also within the same VNet (or peered VNets)\* to connect to the bot privately via the DL-ASE.

  These private endpoints do not provide a mechanism for Azure Bot Service to route messages from public channels like Teams \*into\* a customer's VNet to a completely private endpoint without the customer exposing some form of public ingress. If public network access is disabled on the bot's App Service (where the agent runs), Teams channels become unconfigured, indicating their reliance on a publicly reachable messaging endpoint registered with Azure Bot Service.\[11\] The Azure Bot Service security baseline also indicates that the service itself is not deployed into the customer's virtual network.\[13\]

  A recent Microsoft Q\&A response from May 2025 reiterates: "Microsoft Teams is a cloud-based service that relies on public Bot Framework endpoints to communicate with bots. When public access is disabled on the Azure Bot service, the Teams channel gets automatically unconfigured, making direct integration via private endpoint unsupported out-of-the-box".\[8\]

**3\. Role of Controlled Public Ingress (Application Gateway/APIM)**

  A controlled public ingress point, such as Azure Application Gateway with WAF or Azure API Management, remains the standard best practice for connecting Azure Bot Service channel traffic to a VNet-isolated agent.\[7, 8\]

  A fully private path from the Azure Bot Service for \*inbound public channel messages\* is not (yet) the norm primarily due to:  
  \*   \*\*Azure Bot Service Architecture:\*\* It acts as a global, multi-tenant relay service. Public channels (like Teams) are designed to send messages to publicly resolvable endpoints registered with the Bot Service.  
  \*   \*\*Public Channel Nature:\*\* Channels like Microsoft Teams are themselves public SaaS applications and are not architected to peer into numerous individual customer VNets directly.  
  \*   \*\*Scalability and Complexity:\*\* Enabling Azure Bot Service to establish private connections into potentially thousands of customer VNets for inbound message delivery from public channels would introduce significant operational and networking complexity for the Azure platform.  
  \*   \*\*Security Responsibility Model:\*\* The current model places the onus on the customer to secure the "last mile" from the public internet to their private VNet resource, using established and feature-rich Azure services like Application Gateway/WAF and APIM. This allows customers to tailor the security to their specific needs.

  As stated in a May 2025 Microsoft Q\&A response, "Teams (a public SaaS app) needs publicly accessible endpoints to communicate with your bot".\[7\]

**4\. Status and Applicability of the Direct Line App Service Extension (DL-ASE)**

  The Direct Line App Service Extension (DL-ASE) is a feature designed to enable network isolation for the Direct Line channel specifically. It co-locates a Direct Line endpoint with the bot's App Service within the VNet.\[11, 12, 13, 14, 15\]

  \*   \*\*Primary Use Case for DL-ASE:\*\* Its main purpose is to allow Direct Line clients (e.g., a web chat control on an internal company portal, or applications running \*within the same VNet or peered VNets\*) to communicate with a bot whose messaging endpoint is also within that VNet, thereby keeping the traffic entirely private to the VNet.\[12\]  
  \*   \*\*Applicability to General M365 Agent Channel Traffic (Teams, M365 Copilot):\*\* DL-ASE \*does not\* inherently provide a private communication path for public channels like Microsoft Teams or M365 Copilot to reach a VNet-isolated bot. These public channels interface with the Azure Bot Service through its standard channel connectors, which, as discussed, require a publicly accessible messaging endpoint for the bot. Disabling public network access on the App Service hosting the bot (a common step when using DL-ASE for full isolation) leads to the unconfiguration of Teams channels.\[11\]  
  \*   \*\*Microsoft Guidance (Service Tags vs. DL-ASE):\*\* Microsoft guidance from September 2023 advises employing the Azure Service Tag method for network isolation and limiting the use of DL-ASE to "highly specific scenarios," recommending consultation with support teams before production deployment.\[16\] This suggests DL-ASE is not the default or broadly recommended solution for all network isolation needs, especially when public channels are involved.  
  \*   \*\*Role of Service Tags:\*\* Service Tags (e.g., \`BotFramework\`, \`AzureActiveDirectory\`) are used to define network security rules for traffic to and from Azure services. For instance, an NSG rule on the Application Gateway can be configured to allow inbound traffic only from the \`BotFramework\` service tag (representing Azure Bot Service IP ranges). Service Tags help secure the public frontend and the bot's outbound connections but do not create a private path for inbound message delivery from public channels themselves.

  In summary, for Project Nucleus M365 Agents that need to interact with public channels like Microsoft Teams or M365 Copilot while being hosted in a VNet-isolated manner, DL-ASE is not the solution for that public channel connectivity. The recommended architecture remains a public ingress point (Application Gateway/APIM) fronting a private endpoint to the agent's compute. DL-ASE would be relevant if Nucleus had specific scenarios requiring Direct Line clients \*within their VNet\* to communicate privately with the agents.

**Table: Comparison of Network Isolation Architectures for M365 Agent Messaging Endpoints (May 2025\)**

| Architecture | Key Characteristics | Security Implications | Complexity | Use Case Suitability for Project Nucleus (Teams/M365 Copilot) | Current Microsoft Recommendation (May 2025\) & GA Status |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **VNet-Integrated Agent \+ App Gateway/WAF** | Public IP on App Gateway. Traffic: Teams \-\> Bot Service \-\> App GW (Public IP) \-\> Agent Private Endpoint. | WAF protection. SSL offload. Agent not directly public. | Moderate setup for App GW, WAF, Private Endpoint, NSGs. | **Suitable & Recommended.** Allows secure access from public channels. | **Recommended.** All components GA. 7 |
| **VNet-Integrated Agent \+ APIM** | Public IP on APIM. Traffic: Teams \-\> Bot Service \-\> APIM (Public IP) \-\> Agent Private Endpoint. | Advanced policies (authN/authZ, rate limits). SSL offload. Agent not directly public. | Moderate to High setup for APIM, policies, Private Endpoint, NSGs. | **Suitable & Recommended.** Offers more control than App GW alone. | **Recommended.** All components GA. 7 |
| **DL-ASE for Internal Direct Line Clients** | No public ingress for Direct Line if clients are VNet-internal. Agent's App Service uses DL-ASE. 'Bot' & 'Token' PEs for outbound. | Traffic for Direct Line stays within VNet. | Moderate setup for DL-ASE, Private Endpoints. | **Not for Teams/Copilot ingress.** Only for private Direct Line clients. | Limited to specific scenarios. Service Tags preferred for general isolation. 16 DL-ASE is GA. |
| **Hypothetical: Full Private Link for Bot Service Inbound Channel Traffic** | Azure Bot Service directly connects to Agent's Private Endpoint for Teams/Copilot traffic. No customer public IP. | Ideal security, no public exposure. | High (if it existed, platform complexity). | Highly desirable if available. | **Not currently available (May 2025).** 8 |

This table underscores that for Project Nucleus's requirement to integrate M365 Agents with public channels like Teams and M365 Copilot while maintaining VNet isolation for the agent compute, the architecture involving Azure Application Gateway with WAF or Azure API Management as a public ingress is the definitive and supported approach.

## **III. Key Supporting Implementation Details**

Beyond proactive messaging and network isolation, several other implementation details are crucial for Project Nucleus.

**A. Complex PersonaConfiguration Loading (Aspire, App Config, Key Vault \- Concrete Pattern)**

M365 Agents will require potentially complex configurations (PersonaConfiguration) that include both non-sensitive settings and sensitive data like API keys for backend MCP tools. A robust and secure pattern for loading this configuration involves.NET Aspire, Azure App Configuration, and Azure Key Vault.

1. **Define POCO:** Create a C\# Plain Old C\# Object (POCO) that mirrors the structure of the PersonaConfiguration, including any nested objects.  
   C\#  
   public class PersonaConfigurationPoco  
   {  
       public string PersonaName { get; set; }  
       public string WelcomeMessage { get; set; }  
       public KnowledgeStoreMcpSettings KnowledgeStore { get; set; }  
       //... other persona-specific settings  
   }

   public class KnowledgeStoreMcpSettings  
   {  
       public string Endpoint { get; set; }  
       public string ApiKey { get; set; } // This will be a Key Vault reference  
   }

2. **Azure App Configuration Setup:**  
   * Store non-sensitive configuration values directly in Azure App Configuration as key-value pairs, potentially using JSON content for structured data. For example, a key like PersonaConfig could hold the JSON structure for PersonaConfigurationPoco.  
   * For sensitive values (e.g., KnowledgeStore.ApiKey), store the actual secret in Azure Key Vault.  
   * In Azure App Configuration, for the sensitive field, use a Key Vault reference. The JSON value in App Configuration for the ApiKey field would look like: {"@Microsoft.KeyVault(SecretUri=https://yourkeyvault.vault.azure.net/secrets/NucleusKnowledgeStoreApiKey)"}.  
3. **.NET Aspire and Application Configuration:**  
   * In the.NET Aspire AppHost project (Program.cs), define resources for Azure App Configuration and Azure Key Vault. Aspire can facilitate connecting to these services, often by injecting environment variables or configuration settings into the M365 Agent service.  
     C\#  
     // In Aspire AppHost Program.cs (conceptual)  
     var builder \= DistributedApplication.CreateBuilder(args);

     var appConfig \= builder.AddAzureAppConfiguration("appConfig");  
     var keyVault \= builder.AddAzureKeyVault("kv"); // Assumes Key Vault is used for KV references

     builder.AddProject\<Projects.MyNucleusM365AgentService\>("nucleusagent")  
           .WithReference(appConfig)  
           .WithReference(keyVault); // Making KV available for reference resolution

   * In the M365 Agent application's Program.cs (or an extension method called by Aspire):  
     C\#  
     // In M365 Agent's Program.cs  
     builder.Configuration.AddAzureAppConfiguration(options \=\>  
     {  
         // Endpoint can be injected by Aspire or read from IConfiguration  
         var appConfigEndpoint \= builder.Configuration;  
         options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())  
                // Enable Key Vault reference resolution  
               .ConfigureKeyVault(kv \=\>  
                {  
                    kv.SetCredential(new DefaultAzureCredential());  
                    // Optionally, specify specific Key Vaults if not all are to be used  
                    // kv.Register(new Uri("https://yourkeyvault.vault.azure.net/"));  
                })  
                // Optional: Select specific keys or labels  
               .Select("PersonaConfig:\*", "SharedSettings"); // Example key filter  
     });

     // Bind the configuration section to the POCO using IOptions  
     builder.Services.Configure\<PersonaConfigurationPoco\>(  
         builder.Configuration.GetSection("PersonaConfig") // Matches the key in App Configuration  
     );  
     // Or if the entire JSON is under one key:  
     // builder.Services.Configure\<PersonaConfigurationPoco\>(builder.Configuration.GetSection("TheRootKeyForPersonaConfigJson"));  
     The DefaultAzureCredential will allow the M365 Agent (running with a Managed Identity in Azure) to authenticate to both Azure App Configuration and Azure Key Vault.17  
4. **Inject and Use:** Inject IOptions\<PersonaConfigurationPoco\> into services within the M365 Agent that require access to the configuration.18  
   C\#  
   public class MyAgentService  
   {  
       private readonly PersonaConfigurationPoco \_personaConfig;

       public MyAgentService(IOptions\<PersonaConfigurationPoco\> personaConfigOptions)  
       {  
           \_personaConfig \= personaConfigOptions.Value;  
           // string apiKey \= \_personaConfig.KnowledgeStore.ApiKey; // ApiKey is resolved from Key Vault  
       }  
   }

This pattern provides a layered approach: non-sensitive configuration is managed in App Configuration, sensitive data is secured in Key Vault, and.NET Aspire simplifies the integration and local development experience. The application code remains clean, working with strongly-typed IOptions objects.

**B. ISV Entra ID Application Registration Strategy (Multiple Distinct Nucleus Persona M365 Agents)**

For Project Nucleus, an ISV offering multiple distinct M365 Persona Agents (e.g., EduFlowM365Agent, HelpdeskM365Agent), the Microsoft Entra ID application registration strategy is pivotal for manageability, security, and customer experience. The project assumes a "one distinct Nucleus Persona \= one deployed Microsoft 365 Agent application" model, each with its own Microsoft Entra Agent ID.

* **Option A:** Each distinct M365 Persona Agent application has its *own, separate* multi-tenant Entra application registration in the Nucleus (ISV) home tenant.  
* **Option B:** Nucleus registers *one single* multi-tenant "Nucleus Platform" Entra application. All distinct M365 Persona Agent applications are linked to this single ISV application identity, likely differentiated by App Roles.

**Recommendation: Option B (Single Multi-Tenant App Registration with Granular App Roles)**

For ISVs with a suite of related applications or modules, **Option B is generally the more scalable, manageable, and Microsoft-recommended best practice**.19 This approach streamlines customer consent (customers consent once to the "Nucleus Platform" application) and simplifies management for the ISV.

* How Distinct Entra Agent IDs are Handled:  
  The "Entra Agent ID" referenced in the project context likely refers to the unique identifier assigned by the M365 Agent platform or Bot Framework to each deployed agent instance within a customer's environment. This M365-specific Agent ID is distinct from the Microsoft Entra Application (client) ID or the Service Principal Object ID.  
  When a customer consents to the single multi-tenant "Nucleus Platform" application, a single service principal representing this ISV application is created in the customer's Microsoft Entra ID tenant. All deployed Nucleus Persona Agent instances (e.g., an instance of EduFlowM365Agent and an instance of HelpdeskM365Agent) would operate under the identity of this single service principal. The M365 platform itself differentiates message routing and context for individual agent instances using their unique M365 Agent IDs.  
* Granular Permission Management in Customer Tenant:  
  Permissions are managed using App Roles defined within the single multi-tenant "Nucleus Platform" application registration in the ISV's tenant.21  
  1. **Define App Roles:** The ISV defines granular application permissions (app roles) in the manifest of the "Nucleus Platform" app. Examples:  
     * Nucleus.EduFlow.Use (Allows usage of the EduFlow Persona)  
     * Nucleus.Helpdesk.Use (Allows usage of the Helpdesk Persona)  
     * Nucleus.KnowledgeStore.Search (Allows searching the knowledge store, potentially used by multiple Personas)  
     * Nucleus.FileProcessing.ReadWrite (Allows file processing capabilities)  
  2. **Customer Admin Consent:** When a customer administrator installs or consents to a specific Nucleus Persona Agent (e.g., EduFlowM365Agent), the consent process should ideally request only the app roles required by that specific Persona type. The customer admin grants these permissions to the "Nucleus Platform" service principal in their tenant.  
  3. **Runtime Enforcement:** When a Nucleus M365 Agent operates, the tokens it receives (or acquires for backend calls) will contain the granted app roles. The agent's logic, and the backend Nucleus MCP Tools it calls, must check for the presence of these roles to authorize specific functionalities. For example, the EduFlowM365Agent would only perform its core functions if the Nucleus.EduFlow.Use role (and any other necessary data access roles like Nucleus.KnowledgeStore.Search) has been granted.

This model allows customers to control which capabilities of the Nucleus platform are active in their tenant, even if all agents operate under a single service principal identity. If a customer wants to enable the EduFlowM365Agent but not the HelpdeskM365Agent, they would consent to the app roles associated with EduFlow only.

* **Advantages of Option B:**  
  * **Simplified ISV Management:** Managing one app registration is easier than many.  
  * **Streamlined Customer Consent:** Customers consent once to the platform, though they approve specific sets of permissions (app roles).  
  * **Consistent Branding:** A single application identity for the Nucleus platform.

If truly distinct Entra service principals *per deployed agent instance* were an absolute requirement for customer-side IAM (e.g., assigning different Conditional Access policies to an EduFlowM365Agent instance versus a HelpdeskM365Agent instance), then Option A might be forced. However, this significantly increases ISV overhead and is generally not the preferred pattern for ISV suites. The M365 Agent framework is more likely to handle instance differentiation at its own platform level, with the underlying Entra permissions being managed via app roles on the single ISV application.

**Table: Comparison of Entra ID Application Registration Strategies for Nucleus ISV Agents (May 2025\)**

| Feature | Option A: Per-Agent App Reg | Option B: Single Platform App Reg with App Roles |
| :---- | :---- | :---- |
| **Entra Agent ID Representation** | Each Persona Agent type has its own Entra App Reg. Each deployed instance in customer tenant results in a distinct SP for that *type*. | Single Entra App Reg for "Nucleus Platform." One SP in customer tenant for the platform. M365 Agent platform differentiates instances by M365 Agent ID. |
| **Permission Granularity (Customer Admin)** | Customer admin grants permissions to each Persona Agent type's SP separately. | Customer admin grants specific App Roles (e.g., EduFlow.Use, Helpdesk.Use) to the single "Nucleus Platform" SP. |
| **Scalability (ISV)** | Low. Managing many app registrations is complex. | High. Single app registration to manage. |
| **Scalability (Customer)** | Moderate. Multiple SPs to manage if many Personas used. | High. Single SP, permissions managed via roles. |
| **ISV Management Overhead** | High (certs/secrets, manifests, updates per app). | Low (single point of management). |
| **Customer Onboarding Experience** | Potentially multiple consent prompts if deploying different Persona types over time. | Single primary consent to platform, with scope defined by requested App Roles for the specific Persona being deployed. |
| **Microsoft Best Practice for ISV Suites (May 2025\)** | Less aligned for cohesive suites. | **Generally Recommended.** 19 |
| **Alignment with "One Persona \= One Deployed M365 Agent App"** | Maps directly if "Entra Agent ID" means distinct Entra SP per Persona *type*. | Aligns if "Entra Agent ID" is the M365 platform's agent instance ID, with Entra permissions managed by app roles on the single ISV app. |

**C. LLM Tool Calling of Backend Nucleus MCP Tools (M365 Agent.NET SDK Flow)**

When a Nucleus M365 Agent's primary LLM (e.g., Gemini via IChatClient) decides to invoke a backend Nucleus capability exposed as an MCP Tool (e.g., Nucleus\_KnowledgeStore\_McpServer.SearchKnowledge), Semantic Kernel plays a central role in orchestrating this interaction within the M365 Agent.

The recommended sequence and components are as follows:

1. **Tool Presentation to LLM (via Semantic Kernel):**  
   * **Loading MCP Tools as Plugins:** The M365 Agent, upon initialization or as needed, uses Semantic Kernel to connect to the backend Nucleus MCP Tool/Server applications. The ModelContextProtocol C\# SDK provides McpClientFactory to establish a connection (Stdio or SSE/HTTPS) to an MCP server and list its available tools (await mcpClient.ListToolsAsync()).22  
   * **Converting MCP Tools to KernelFunctions:** Each discovered MCP tool can be converted into a Semantic Kernel KernelFunction. An extension method like tool.AsKernelFunction() is often used for this.22 These functions are then added to a KernelPlugin within the M365 Agent's Kernel instance (e.g., kernel.Plugins.AddFromFunctions("NucleusKnowledgeStore", mcpTools.Select(t \=\> t.AsKernelFunction()))).  
   * **Function Calling Enablement:** When the M365 Agent (through Semantic Kernel's planner or by directly invoking a prompt function) interacts with the LLM (via IChatClient), the descriptions and schemas of these KernelFunctions (derived from the MCP tools) are provided to the LLM. This enables the LLM's native function calling capabilities.23 The descriptions must be clear and LLM-friendly for effective tool use. The OpenAIPromptExecutionSettings might require FunctionChoiceBehavior.Auto and RetainArgumentTypes \= true for seamless MCP tool invocation.22  
2. **LLM Tool Invocation Request:**  
   * If the LLM determines that one of the Nucleus MCP Tools is needed to fulfill the user's request, its response will include a structured "function call" or "tool invocation" request, specifying the target tool (function) name and the arguments it has inferred.  
3. **Translation to MCP Call by M365 Agent/Semantic Kernel:**  
   * **Kernel Receives Request:** Semantic Kernel, within the M365 Agent, intercepts this tool invocation request from the LLM.  
   * **MCP Plugin Invocation:** The KernelFunction corresponding to the LLM's request (which wraps the MCP tool) is executed. The Semantic Kernel MCP plugin (or the custom logic using the ModelContextProtocol SDK) is responsible for:  
     * Identifying the correct backend Nucleus MCP Tool/Server (e.g., Nucleus\_KnowledgeStore\_McpServer).  
     * Constructing the actual MCP request payload using the tool name and arguments provided by the LLM.  
   * **Secure tenantId Propagation:** This is a critical step for multi-tenancy and data security.  
     * The M365 Agent has access to the tenantId from the incoming activity (e.g., Activity.Conversation.TenantId).  
     * This tenantId *must* be securely propagated to the backend Nucleus MCP Tool.  
     * **Recommended Method:** The M365 Agent (specifically, its MCP client component when calling the backend MCP server) should acquire an authentication token destined for the target Nucleus MCP Tool/Server. This token should include the tenantId as a verifiable claim.  
       * This could involve the M365 Agent's Managed Identity (if the MCP server is an Entra ID protected API and the agent has permissions to call it) requesting a token and potentially augmenting it or using an On-Behalf-Of flow if the agent itself has an identity in the customer tenant.  
       * Alternatively, if the MCP server uses a custom authentication scheme, the M365 Agent would need to securely construct and pass the tenantId within that scheme.  
     * The backend Nucleus MCP Tool/Server must validate the incoming token (if applicable) and extract the tenantId claim to scope its operations (e.g., Cosmos DB queries partitioned by tenantId). Relying solely on an unauthenticated tenantId parameter in the MCP call is not secure.  
4. **Return MCP Tool Response to LLM:**  
   * **MCP Server Responds:** The Nucleus MCP Tool/Server processes the request (scoped by the validated tenantId) and returns its response via the MCP.  
   * **Kernel Forwards to LLM:** The M365 Agent's Semantic Kernel (via its MCP plugin) receives this response. It then formats this tool execution result and sends it back to the LLM.  
   * **LLM Generates Final Answer:** The LLM uses the tool's output to formulate its final response to the end-user and sends it back through the M365 Agent.

This flow leverages Semantic Kernel's strengths in plugin management and LLM orchestration, combined with the Model Context Protocol for standardized tool communication, ensuring that M365 Agents can effectively and securely utilize backend Nucleus capabilities. The secure propagation of tenantId is paramount for maintaining data isolation in the multi-tenant backend.

## **IV. Consolidated Recommendations and Next Steps**

Based on the detailed analysis of Project Nucleus's requirements and the current Microsoft technology landscape (as of May 2025), the following consolidated recommendations are provided:

1\. Proactive Messaging from Background Services:  
\* Authentication: Implement IAccessTokenProvider using DefaultAzureCredential in background services (Azure Functions, IHostedService) to leverage Managed Identity for authenticating to the Bot Connector Service. This avoids storing explicit App ID/Passwords.  
\* SDK Usage: Utilize the CloudAdapter (or the latest M365 Agents SDK equivalent like IAgentHttpAdapter) and its ContinueConversationAsync method for sending proactive messages. Ensure correct DI registration of the adapter and its dependencies.  
\* botAppId: The botAppId passed to ContinueConversationAsync should be the Entra Agent ID of the target M365 Persona Agent. The background service's Managed Identity must have permissions to send messages via the Bot Connector, potentially scoped to act for these agent IDs.  
2\. Network Isolation for M365 Agents:  
\* Public Ingress for Public Channels (Teams/Copilot): For M365 Agents hosted in a VNet and needing to connect to public channels like Microsoft Teams or M365 Copilot, a public ingress point is mandatory. The recommended approach is Azure Application Gateway (with WAF) or Azure API Management, which then privately connects to the agent's compute resource (e.g., ACA, App Service) via its Private Endpoint.  
\* Azure Bot Service Private Endpoints (Bot/Token): These are primarily for the VNet-isolated bot's outbound communication to Bot Framework services and for internal VNet clients using the Direct Line App Service Extension (DL-ASE). They do not enable public channels to directly and privately deliver messages to a VNet-isolated bot without a public ingress.  
\* DL-ASE: Use DL-ASE only for specific scenarios where Direct Line clients within the VNet need to communicate privately with the bot. It is not a solution for Teams/Copilot ingress. Follow Microsoft's guidance on preferring Service Tags for general network isolation over DL-ASE where applicable.  
\* Service Tags: Utilize the BotFramework service tag to restrict inbound traffic to the Application Gateway/APIM and other relevant service tags for securing the bot's outbound connections.  
3\. Key Supporting Implementation Details:  
\* Configuration Loading: Employ the pattern of Azure App Configuration (for non-sensitive settings and structure) with Azure Key Vault references (for sensitive data like API keys). Use.NET Aspire to manage and inject these configurations, binding them to strongly-typed IOptions\<PersonaConfigurationPoco\> in the M365 Agent application. Authenticate to App Configuration and Key Vault using Managed Identity.  
\* ISV Entra ID App Registration: Adopt Option B: a single multi-tenant "Nucleus Platform" Entra application registration in the ISV's tenant. Define granular App Roles within this application to represent the capabilities of different Persona types (e.g., Nucleus.EduFlow.Use, Nucleus.Knowledge.Access). Customer administrators will consent to these app roles for the single service principal created in their tenant. The M365 Agent platform's unique Agent ID will differentiate deployed instances, while Entra permissions are managed via these app roles.  
\* LLM Tool Calling of MCP Tools: Leverage Semantic Kernel within the M365 Agent.  
\* Connect to backend Nucleus MCP Tool/Servers using the ModelContextProtocol C\# SDK.  
\* Convert discovered MCP tools into KernelFunctions and add them as plugins to the Semantic Kernel instance.  
\* Enable LLM function calling by providing these tool descriptions to the LLM.  
\* Crucially, ensure secure tenantId propagation from the M365 Agent (derived from Activity.Conversation.TenantId) to the backend MCP Tool. This should ideally be achieved by including the tenantId as a validated claim in an authentication token passed from the M365 Agent (as MCP client) to the backend MCP Tool/Server.  
**Next Steps for Project Nucleus:**

1. **Prototype Proactive Messaging:** Develop a small proof-of-concept Azure Function or Worker Service demonstrating DI setup for CloudAdapter (or IAgentHttpAdapter), IAccessTokenProvider with DefaultAzureCredential, and sending a proactive message using a stored ConversationReference. Verify the Managed Identity permissions required on the Bot Connector Service.  
2. **Validate Network Architecture:** Design and (optionally) prototype the VNet-isolated agent hosting with Application Gateway/WAF. Confirm NSG rules, Private Endpoint connectivity, and successful message flow from Teams to the isolated agent.  
3. **Confirm M365 Agent ID vs. Entra ID Details:** Clarify with Microsoft documentation or support the exact nature of the "Microsoft Entra Agent ID" for an M365 Agent instance when using a single ISV multi-tenant app registration. Specifically, how this M365 Agent ID is established and if it has any direct representation as a unique service principal object in the customer tenant beyond the main SP for the ISV app.  
4. **Design tenantId Propagation for MCP Calls:** Detail the precise token issuance and validation mechanism for securely passing the tenantId from the M365 Agent to backend MCP tools. This includes defining the token type, claims, and the authentication/authorization setup on the MCP servers.  
5. **Evaluate Semantic Kernel MCP Integration:** Begin integrating Semantic Kernel with a sample Nucleus MCP tool server to validate the tool discovery, KernelFunction conversion, and LLM function calling flow, paying close attention to argument mapping and response handling.

By addressing these critical implementation details with the recommended approaches, Project Nucleus can build a secure, scalable, and robust M365 Agent platform.

#### **Works cited**

1. PnP Core SDK with "system-assigned managed identity" authentication \#457 \- GitHub, accessed May 24, 2025, [https://github.com/pnp/pnpcore/issues/457](https://github.com/pnp/pnpcore/issues/457)  
2. Azure Active Directory Develop | PDF | .NET Framework | Autenticación \- Scribd, accessed May 24, 2025, [https://es.scribd.com/document/695740912/azure-active-directory-develop](https://es.scribd.com/document/695740912/azure-active-directory-develop)  
3. ServiceCollectionExtensions.AddBotFrameworkAdapterIntegration Method (Microsoft.Bot.Builder.Integration.AspNet.Core), accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.integration.aspnet.core.servicecollectionextensions.addbotframeworkadapterintegration?view=botbuilder-dotnet-stable](https://learn.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.integration.aspnet.core.servicecollectionextensions.addbotframeworkadapterintegration?view=botbuilder-dotnet-stable)  
4. Microsoft-Teams-Samples/samples/bot-proactive-messaging/csharp ..., accessed May 24, 2025, [https://github.com/OfficeDev/Microsoft-Teams-Samples/blob/main/samples/bot-proactive-messaging/csharp/proactive-cmd/Program.cs](https://github.com/OfficeDev/Microsoft-Teams-Samples/blob/main/samples/bot-proactive-messaging/csharp/proactive-cmd/Program.cs)  
5. BotFrameworkHttpAdapter Class (Microsoft.Bot.Builder.Integration.AspNet.WebApi), accessed May 24, 2025, [https://learn.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.integration.aspnet.webapi.botframeworkhttpadapter?view=botbuilder-dotnet-stable](https://learn.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.integration.aspnet.webapi.botframeworkhttpadapter?view=botbuilder-dotnet-stable)  
6. Proactive Messaging Documentation · Issue \#787 · microsoft/botbuilder-dotnet \- GitHub, accessed May 24, 2025, [https://github.com/microsoft/botbuilder-dotnet/issues/787](https://github.com/microsoft/botbuilder-dotnet/issues/787)  
7. How to create Azure Bot service in a Private network and integrate with MS Teams application \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo](https://learn.microsoft.com/en-us/answers/questions/2153606/how-to-create-azure-bot-service-in-a-private-netwo)  
8. Is it possible to integrate Azure Bot with Teams when the public access is disabled for Azure Bot? \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2263616/is-it-possible-to-integrate-azure-bot-with-teams-w](https://learn.microsoft.com/en-us/answers/questions/2263616/is-it-possible-to-integrate-azure-bot-with-teams-w)  
9. Baseline OpenAI End-to-End Chat Reference Architecture \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/architecture/baseline-openai-e2e-chat)  
10. Azure Networking architecture documentation \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/networking/fundamentals/architecture-guides](https://learn.microsoft.com/en-us/azure/networking/fundamentals/architecture-guides)  
11. Configure network isolation \- Bot Service | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0)  
12. About network isolation in Azure AI Bot Service \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0)  
13. Azure security baseline for Azure Bot Service | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline](https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline)  
14. Delivering increased productivity for bot development and deployment \- Microsoft Azure, accessed May 24, 2025, [https://azure.microsoft.com/en-us/blog/delivering-increased-productivity-for-bot-development-and-deployment/](https://azure.microsoft.com/en-us/blog/delivering-increased-productivity-for-bot-development-and-deployment/)  
15. Conversational AI updates for July 2019 | Microsoft Azure Blog, accessed May 24, 2025, [https://azure.microsoft.com/en-us/blog/conversational-ai-updates-for-july-2019/](https://azure.microsoft.com/en-us/blog/conversational-ai-updates-for-july-2019/)  
16. Recommended way to network isolate with an Azure Bot Service \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/answers/questions/2243086/recommended-way-to-network-isolate-with-an-azure-b](https://learn.microsoft.com/en-us/answers/questions/2243086/recommended-way-to-network-isolate-with-an-azure-b)  
17. azure-dev/cli/azd/CHANGELOG.md at main \- GitHub, accessed May 24, 2025, [https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md](https://github.com/Azure/azure-dev/blob/main/cli/azd/CHANGELOG.md)  
18. Options pattern in ASP.NET Core | Microsoft Learn, accessed May 24, 2025, [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0)  
19. Authenticate applications and users with Microsoft Entra ID, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/architecture/authenticate-applications-and-users](https://learn.microsoft.com/en-us/entra/architecture/authenticate-applications-and-users)  
20. Microsoft Entra ID Guide for independent software developers, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/architecture/guide-for-independent-software-developers](https://learn.microsoft.com/en-us/entra/architecture/guide-for-independent-software-developers)  
21. Overview of permissions and consent in the Microsoft identity platform, accessed May 24, 2025, [https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview](https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview)  
22. Using Model Context Protocol in agents \- Pro-code agents with Semantic Kernel, accessed May 24, 2025, [https://www.developerscantina.com/p/mcp-semantic-kernel/](https://www.developerscantina.com/p/mcp-semantic-kernel/)  
23. Plugins in Semantic Kernel \- Learn Microsoft, accessed May 24, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/)