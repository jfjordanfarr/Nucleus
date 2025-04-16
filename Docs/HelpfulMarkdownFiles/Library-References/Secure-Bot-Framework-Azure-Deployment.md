# **Securely Integrating a.NET Aspire Bot Framework Application with Microsoft Teams in a Restricted Azure Network Environment**

## **1\. Executive Summary**

Integrating applications with collaboration platforms like Microsoft Teams offers significant productivity benefits, but organizations with stringent network security requirements face challenges. Enabling communication for a Microsoft Bot Framework bot, hosted within a secured Azure environment, requires careful architectural planning, particularly when dealing with network restrictions such as mandatory Private Endpoints for inbound traffic and centralized Azure Firewall control for all outbound traffic via a hub Virtual Network (VNet). This report details architectural approaches for hosting a.NET Aspire Bot Framework application on Azure App Service, Azure Logic Apps (Standard), or Azure Container Apps while adhering to such strict network policies.

The analysis reveals that the standard Bot Framework communication model relies on public endpoints, necessitating specific network isolation strategies to meet security mandates. Azure Private Endpoints effectively secure inbound traffic to the hosting services (App Service, Logic Apps Standard, Container Apps), preventing direct public exposure. For outbound traffic control, VNet Integration combined with User-Defined Routes (UDRs) directing traffic to an Azure Firewall in a hub VNet provides the necessary mechanism for inspection and policy enforcement.

A key finding is that Azure App Service, when used with the Direct Line App Service Extension (DL-ASE), offers the most comprehensive network isolation for the bot's messaging endpoint itself, bypassing the need for the public Bot Connector service to reach the bot directly. Azure Logic Apps Standard presents complexities, particularly concerning the outbound network path of managed connectors, which may not inherently follow VNet Integration routes intended for the central firewall. Azure Container Apps, while offering containerization benefits, requires the use of Workload Profiles to enable advanced networking features like Private Endpoints and UDR support. Furthermore, Microsoft Graph API, essential for many bot interactions with Teams data, currently lacks Private Endpoint support, making controlled public egress through the firewall an unavoidable necessity..NET Aspire primarily assists in streamlining local development orchestration and application configuration, rather than directly configuring the underlying Azure network infrastructure.

Based on these findings, suitable architectural patterns are proposed for each hosting option. App Service with DL-ASE is recommended for scenarios demanding the highest level of messaging endpoint isolation. Container Apps (using Workload Profiles) offers flexibility for container-native applications needing robust network control. Logic Apps Standard is viable primarily when relying on built-in connectors or when the distinct egress path of managed connectors can be appropriately managed within the organization's security framework.

## **2\. Bot Framework and Teams Network Communication Flow**

### **Overview**

Understanding the standard network communication flow between a Microsoft Teams user and a Bot Framework bot is crucial for designing secure architectures in restricted environments. The interaction typically involves several components and services acting in sequence.1

1. **User Interaction:** A user interacts with the bot through the Microsoft Teams client (web, desktop, or mobile). The Teams client itself requires connectivity to standard Microsoft Office 365 endpoints, as defined by Microsoft's endpoint management guidelines. No additional bot-specific network configuration is needed on the *client-side* just to use a bot within Teams.1  
2. **Teams Service:** Messages and interaction events from the Teams client are sent to the Teams service, hosted within the Microsoft 365 cloud.1  
3. **Bot Connector Service:** The Teams service forwards the message to the Azure Bot Connector service. This service acts as a central proxy or gateway, translating messages between the native format of the channel (Teams) and the standardized Bot Framework Activity protocol that the bot understands. It handles the complexities of connecting to various channels.2  
4. **Bot Messaging Endpoint:** The Bot Connector service then relays the Activity object to the bot's registered HTTPS messaging endpoint. This endpoint is typically hosted on a web service like Azure App Service, Azure Functions, Logic Apps, or Container Apps.1 The bot processes the activity and prepares a response.  
5. **Response Path:** The bot sends the response Activity back to the Bot Connector service, using a reply URL provided in the initial incoming request (e.g., https://smba.trafficmanager.net/{region}).1 The Connector service translates the Activity back into the Teams format and delivers it via the Teams service to the user's client.

### **Protocols and Endpoints**

The communication between these components relies on specific protocols and endpoints:

* **Protocol:** The primary communication protocol throughout the flow, especially between the Bot Connector and the bot's messaging endpoint, is HTTPS over TLS/SSL.1 For bots using the Direct Line App Service Extension (discussed later), WebSocket communication is also involved.7  
* **Bot Messaging Endpoint:** The bot must expose an HTTPS endpoint that the Bot Connector service can reach.1 Traditionally, this endpoint needed to be publicly accessible on the internet.  
* **Bot Connector Service Endpoints:** The bot needs to send replies and make API calls back to the Bot Connector service. These calls target regional endpoints managed by Microsoft, often fronted by Traffic Manager (e.g., smba.trafficmanager.net).1  
* **Authentication Endpoints:** Bots require outbound connectivity to authenticate themselves to the Bot Connector service and potentially for user authentication flows. Key endpoints include login.botframework.com and login.microsoftonline.com (or equivalent Azure AD endpoints).5 Communication involves exchanging JSON Web Tokens (JWT) within the Authorization: Bearer header of HTTP requests.4 The bot must validate tokens received from the Connector, and the Connector validates tokens sent by the bot.

### **Network Isolation Challenge**

The fundamental challenge in secure environments arises from the standard requirement that the bot's messaging endpoint must be publicly accessible via HTTPS.1 Organizations mandating that applications reside within a private network, accessible only via Private Endpoints, cannot directly use this standard model. The public Bot Connector service, residing on the internet, cannot initiate connections to a private IP address within a customer's VNet.

This inherent reliance on a public endpoint is the core conflict. Simply placing the hosting service (like App Service) behind a Private Endpoint secures inbound access *from within the VNet or peered networks*, but it simultaneously prevents the public Bot Connector service from delivering messages from Teams. This necessitates architectural patterns that either bypass the public connector for the messaging endpoint (like DL-ASE) or implement alternative, secure ways for Teams messages to reach the bot within the private network.

## **3\. Securing Inbound Traffic with Private Endpoints**

Azure Private Endpoints are a foundational technology for securing inbound access to Azure PaaS services within a private network context.

### **Azure Private Endpoint Fundamentals**

A Private Endpoint is essentially a network interface (NIC) deployed within a subnet of your Azure Virtual Network (VNet). This NIC is assigned a private IP address from the VNet's address space and establishes a secure, private connection to a specific Azure PaaS resource (or a sub-resource within it) using Azure Private Link technology.9

Key characteristics include:

* **Private Connectivity:** It allows resources within the VNet, peered VNets, or connected on-premises networks (via VPN or ExpressRoute) to access the Azure service using the private IP address, without exposing the service to the public internet.9 Traffic flows over the Microsoft backbone network.12  
* **Inbound Traffic Only:** Private Endpoints manage *inbound* connections to the target Azure service. Outbound traffic originating *from* the service utilizes different mechanisms, such as VNet Integration.11  
* **Service Integration:** By enabling a Private Endpoint, the Azure service is effectively brought *into* the VNet's private address space.9

### **Private Endpoint Support for Hosting Options**

The three hosting options considered support Private Endpoints, albeit with some variations:

* **Azure App Service:** Natively supports Private Endpoints for web apps. The connection targets the Microsoft.Web/sites resource type with the sites subresource.11 Configuration can be done per deployment slot, allowing for testing in isolated environments. It is strongly recommended to disable public network access on the App Service once a Private Endpoint is configured to ensure true isolation.11 An App Service or slot can support up to 100 Private Endpoints.11  
* **Azure Logic Apps (Standard):** Supports Private Endpoints for securing inbound traffic directed at the Logic App's runtime endpoint (used by triggers like 'Request' or 'HTTP \+ Webhook').14 Configuration is managed through the Networking blade of the Logic App resource.15 Additionally, Standard Logic Apps rely on an Azure Storage account for state and artifacts; Private Endpoints are also crucial for securing access to this backend storage account if it's locked down.16  
* **Azure Container Apps:** Supports Private Endpoints, currently in preview, specifically for environments using the **Workload Profiles** type.17 This feature requires disabling public network access at the Container Apps Environment level.17 The Private Endpoint connects to the managedEnvironment subresource of the Container Apps Environment.18 A key limitation is that it currently only supports inbound **HTTP** traffic; general TCP traffic is not supported via the Private Endpoint.17

### **DNS Configuration**

Correct DNS configuration is critical for Private Endpoints to function. When a Private Endpoint is created, Azure performs several DNS actions 19:

1. **Public DNS Update:** The public DNS CNAME record for the Azure service (e.g., \<app-name\>.azurewebsites.net) is typically updated to point to a new alias within a privatelink subdomain (e.g., \<app-name\>.privatelink.azurewebsites.net).11  
2. **Private DNS Zone Creation:** Azure automatically creates (or uses an existing) Azure Private DNS Zone corresponding to the privatelink subdomain (e.g., privatelink.azurewebsites.net for App Service 11, privatelink.logic.azure.com likely for Logic Apps, privatelink.{regionName}.azurecontainerapps.io for ACA 18).  
3. **A Record Creation:** An 'A' record is created within this Private DNS Zone, mapping the privatelink hostname to the private IP address assigned to the Private Endpoint NIC within the VNet.11  
4. **VNet Linking:** The Private DNS Zone is linked to the VNet hosting the Private Endpoint (and potentially other VNets that need to resolve the private IP).

This setup ensures that DNS queries originating from within the linked VNet for the service's original public hostname ultimately resolve to the private IP address of the Private Endpoint, keeping traffic within the private network. Queries from outside the VNet resolve to the public endpoint (which may be disabled).20

### **Implications for Bot Communication**

While Private Endpoints effectively secure the *hosting platform* (App Service, Logic App, Container App) by giving it a private IP address for inbound connections, they do not, in isolation, resolve the core issue for standard Bot Framework communication originating from Teams. The public Bot Connector service still cannot initiate a connection to this private IP address.2

However, Private Endpoints become a crucial enabler when combined with specific architectures:

* **App Service with DL-ASE:** The Direct Line App Service Extension runs *within* the App Service instance.7 A Private Endpoint configured for the App Service allows clients *within the VNet* (or connected networks) to securely access the DL-ASE endpoint via the private IP.21 This combination achieves genuine network isolation for the bot's messaging endpoint from the public Bot Connector, as communication now happens directly between the client (or an internal gateway) and the DL-ASE running inside the secured App Service.  
* **Logic Apps / Container Apps (without DL-ASE):** For these platforms, a Private Endpoint secures the host's trigger endpoint. However, if standard Teams channel integration (which relies on the public Bot Connector initiating the call) is required, the Private Endpoint alone is insufficient. The PE primarily serves to restrict access *to* the Logic App/Container App from authorized internal networks or gateways. If Teams channel integration is mandatory without DL-ASE, alternative approaches like placing an Application Gateway in front of the PE (with strict WAF rules) might be needed to securely expose the endpoint, or accepting that the standard Teams channel connector cannot directly call the PE-secured endpoint. These options might be better suited for bots accessed via internal applications or custom clients that *can* reach the Private Endpoint directly.

## **4\. Securing Outbound Traffic from Azure Hosting Services**

Controlling outbound network traffic is as critical as securing inbound access in high-security environments. Azure VNet Integration, combined with Network Security Groups (NSGs) and User-Defined Routes (UDRs), provides the mechanisms to manage and direct outbound flows from PaaS services.

### **Azure VNet Integration Fundamentals**

VNet Integration allows supported Azure PaaS services to inject their backend infrastructure (worker roles or equivalent) into a designated subnet within a customer's VNet.10 This enables the service to make outbound calls *as if* it were running directly within that VNet.

Key aspects include:

* **Outbound Focus:** VNet Integration is primarily for enabling and controlling *outbound* calls *from* the Azure service *into* or *through* the VNet.23  
* **Delegated Subnet:** It requires a dedicated subnet within the VNet, delegated to the specific Azure service (e.g., Microsoft.Web/serverFarms for App Service/Logic Apps Standard).24 This subnet cannot be used for other purposes and must have sufficient IP address space (e.g., /26 recommended for App Service scalability).24 For App Service, this subnet must be different from the one used for Private Endpoints.11  
* **Source IP Address:** When the integrated service makes calls to resources within the VNet (or peered VNets), the source IP address of the traffic is a private IP from the integration subnet.23 The specific IP assigned to an instance is often available via an environment variable like WEBSITE\_PRIVATE\_IP.23  
* **Route All Traffic:** A crucial setting (often controlled via an App Setting like WEBSITE\_VNET\_ROUTE\_ALL=1 or a portal toggle) allows *all* outbound traffic, including internet-bound traffic, to be routed into the VNet.23 This is essential for forcing traffic through a central firewall via UDRs. If disabled, only traffic destined for private IP ranges (RFC1918) or service endpoints configured on the subnet goes through the VNet; internet traffic bypasses it.23

### **VNet Integration Capabilities and Configuration**

* **Azure App Service:** Offers mature VNet Integration capabilities available in Basic (for testing), Standard, Premium, and Elastic Premium tiers.23 Configuration is done via the Networking blade in the Azure portal or via CLI/PowerShell.24 Supports the 'Route All' traffic option.25  
* **Azure Logic Apps (Standard):** Leverages the underlying App Service platform's VNet Integration capabilities.15 Configuration is similar to App Service, including the need for settings like WEBSITE\_VNET\_ROUTE\_ALL=1 and WEBSITE\_CONTENTOVERVNET=1 (especially if the backend storage uses a Private Endpoint).16 A critical distinction exists between connector types:  
  * **Built-in Connectors:** These run within the Logic App's hosting process. Their outbound traffic utilizes the VNet Integration path and respects NSG/UDR rules applied to the integration subnet.27 If 'Route All' is enabled, their traffic can be forced through a central firewall.  
  * **Managed Connectors:** These run in a separate, shared Microsoft-managed environment.27 Their outbound traffic does *not* originate from the integration subnet's private IPs but rather from shared public IP ranges associated with managed connectors in that Azure region.27 This traffic inherently bypasses the VNet Integration path and any UDRs configured on the integration subnet. Controlling this traffic via a central firewall requires either complex NSG rules on the integration subnet allowing outbound to these specific public IPs (potentially undermining the firewall's purpose) or configuring the firewall itself to explicitly allow traffic *from* the firewall *to* the target services accessed by the managed connectors, relying on the firewall's source IP.  
* **Azure Container Apps:** Requires the **Workload Profiles** environment type to utilize VNet Integration that supports advanced networking features like UDRs and NAT Gateway integration.18 The environment must be created with (or integrated into) a custom VNet, providing a dedicated subnet.18 This setup allows for routing all outbound traffic via UDRs to a central firewall.30

### **Controlling Outbound Flow with NSGs and UDRs**

Once VNet Integration is established (and 'Route All' is enabled for firewall scenarios):

* **Network Security Groups (NSGs):** An NSG applied to the VNet integration subnet acts as a firewall for outbound traffic originating from the integrated service.23 Outbound rules can filter traffic based on destination IP, port, protocol, or Service Tags. Inbound NSG rules do not apply to the service itself via VNet integration.23  
* **User-Defined Routes (UDRs):** A Route Table associated with the integration subnet allows overriding Azure's default routing.18 By creating a default route (0.0.0.0/0) with the next hop type set to 'Virtual appliance' and the next hop address pointing to the private IP of a central Azure Firewall, all outbound traffic from the integrated service can be forced through the firewall for inspection and policy enforcement.25

### **Network Isolation using Direct Line App Service Extension (DL-ASE)**

DL-ASE introduces specific outbound considerations when used with VNet Integration in App Service:

* **DL-ASE Traffic:** Since DL-ASE runs within the App Service process 7, its own outbound calls (e.g., to retrieve bot configuration from Azure Bot Service, validate tokens via the token endpoint) also leverage the App Service's VNet Integration.22  
* **Required Outbound Rules:** The NSG on the integration subnet must explicitly allow outbound traffic on HTTPS (port 443\) to the AzureBotService service tag (for core Bot Service communication) and the AzureActiveDirectory service tag (for token validation/OAuth flows).33 These rules ensure DL-ASE can perform its necessary backend operations even when general internet outbound traffic is denied or routed through a firewall.  
* **Platform Specificity:** DL-ASE is exclusive to Azure App Service (Windows plans) 7 and Azure Bot Service.34 It cannot be used with Azure Logic Apps 34 or Azure Container Apps.33

### **Implications of Logic Apps Managed Connectors for Egress Control**

The requirement for strict, centralized egress control via Azure Firewall highlights a significant architectural consideration for Azure Logic Apps Standard. While App Service and Container Apps (with Workload Profiles) can straightforwardly route all outbound traffic through the firewall using VNet Integration, 'Route All', and UDRs 25, Logic Apps Standard behaves differently depending on the connector type.27

Traffic from built-in connectors follows the expected path through the VNet integration subnet and adheres to the UDR directing it to the firewall. However, traffic from managed connectors originates from shared public IPs outside the VNet integration path.27 This traffic will not follow the UDR on the integration subnet by default. To meet the requirement that *all* traffic transits the firewall, specific configurations are needed:

1. **Avoid Managed Connectors:** Use only built-in connectors whose traffic naturally flows through VNet Integration. This may limit functionality if required services only have managed connectors.  
2. **Firewall Rules for Managed Connector IPs:** Allow outbound traffic from the integration subnet NSG specifically to the public IP ranges of the Azure managed connectors for the region. This traffic would then potentially hit the firewall from the public side if the firewall also manages internet traffic, or it might bypass the firewall entirely depending on the overall network routing. This approach complicates the firewall ruleset and potentially weakens the intended isolation.  
3. **Firewall Rules for Target Services:** Configure the central Azure Firewall to explicitly allow outbound traffic *from the firewall's IP* to the public endpoints of the services targeted by the managed connectors (e.g., Office 365, Salesforce). This assumes the managed connector traffic somehow routes back to the firewall, or accepts that the firewall only controls traffic *after* it leaves the managed connector service.

This dichotomy makes Logic Apps Standard potentially complex or unsuitable for scenarios demanding absolute, uniform firewall control over *all* outbound connections, especially if reliance on managed connectors (like those for Teams or Graph API) is high.

## **5\. Hub-Spoke Topology and Azure Firewall Integration**

The user's environment utilizes a hub-spoke network topology with a central Azure Firewall in the hub VNet managing traffic, including egress to the internet and potentially cross-spoke or on-premises communication. Integrating the bot hosting service (in a spoke VNet) with this model requires specific routing and firewall configurations.

### **Routing via Hub Firewall**

To ensure outbound traffic from the bot's hosting environment (App Service, Logic App Standard, or Container App integrated with a spoke VNet) is routed through the central Azure Firewall in the hub VNet, the following steps are necessary:

1. **VNet Peering:** Establish VNet peering between the spoke VNet hosting the application and the hub VNet containing the Azure Firewall. Ensure peering settings allow traffic forwarding and potentially gateway transit if applicable.  
2. **VNet Integration:** Configure VNet Integration for the chosen hosting service (App Service, Logic App Standard, Container App) into a dedicated subnet within the spoke VNet, as detailed previously. Enable the 'Route All' traffic option (or equivalent) for the integration.23  
3. **User-Defined Route (UDR):** Create a Route Table and associate it with the VNet integration subnet in the spoke VNet.25  
4. **Default Route:** Within the Route Table, define a UDR for the address prefix 0.0.0.0/0 (representing all internet-bound or unknown traffic). Set the nextHopType to Virtual appliance and the nextHopAddress to the **private IP address** of the Azure Firewall instance located in the hub VNet.25

With this configuration, any outbound traffic initiated by the VNet-integrated service that isn't destined for another address within the spoke VNet (or explicitly routed elsewhere) will be directed to the Azure Firewall in the hub for inspection and forwarding based on its rules. The user's example of controlling Azure Service Bus access via AMQP ports on the hub firewall confirms this routing pattern is the standard practice in their organization \[User Query\].

### **Required Firewall Rules**

The Azure Firewall in the hub VNet must be configured with rules to permit the necessary outbound communication for the bot to function. These rules fall into two main categories: Application Rules (Layer 7, FQDN-based) and Network Rules (Layer 3/4, IP/Port/Protocol/Service Tag-based).

**Application Rules (HTTPS Traffic):**

* **Bot Framework Authentication:** Allow HTTPS (port 443\) traffic to login.botframework.com and login.microsoftonline.com.5 The AzureActiveDirectory FQDN Tag can potentially cover login.microsoftonline.com and related domains.33  
* **Bot Framework Channels/Connector:** Allow HTTPS (port 443\) traffic to \*.botframework.com.5 More specific FQDNs like teams.botframework.com or directline.botframework.com can be used if preferred. The AzureBotService FQDN Tag should be evaluated for coverage 37, noting that FQDN tags require the protocol to be set to HTTPS.37  
* **Microsoft Graph API:** Allow HTTPS (port 443\) traffic to graph.microsoft.com.1 No specific Microsoft-managed FQDN Tag appears to cover this endpoint directly.37  
* **DL-ASE Specific (If Applicable):** If using App Service with DL-ASE, its outbound calls to Bot Framework services for metadata and token operations need similar allowances, likely covered by rules for \*.botframework.com and login.microsoftonline.com, or potentially the AzureBotService and AzureActiveDirectory FQDN tags.33  
* **Other Dependencies:** Allow HTTPS traffic to any other required external services (e.g., Azure Cognitive Services endpoints like \*.api.cognitive.microsoft.com 5, third-party APIs).

**Network Rules (IP/Port/Service Tag Traffic):**

* **Non-HTTPS Protocols:** If the bot or its dependencies require outbound communication over protocols other than HTTPS (like the AMQP example for Service Bus), Network Rules based on destination IP/port/protocol are necessary.  
* **Service Tags:** Leverage Service Tags for Azure services where FQDN rules are insufficient or not applicable. Relevant tags might include:  
  * AzureCloud.\<REGION\>: For underlying platform communication.31  
  * Storage.\<REGION\>: If accessing Azure Storage directly via IP/port (less common than FQDN) or if required by platform dependencies.31  
  * AzureMonitor: If sending telemetry directly to Azure Monitor endpoints.31  
  * AzureBotService: Represents IP ranges for the Bot Service, usable in Network Rules.38  
  * AzureActiveDirectory: Represents IP ranges for Azure AD services.38  
* **Platform Requirements:** Underlying platforms (like the AKS clusters potentially powering Container Apps 39 or App Service infrastructure) might require specific outbound rules, often covered by Service Tags like AzureCloud or specific ports (e.g., UDP 1194, TCP 9000 mentioned for ACA's underlying AKS needs 31).

### **Leveraging FQDN Tags and Service Tags**

Azure Firewall offers FQDN Tags and Service Tags to simplify rule management:

* **FQDN Tags:** Represent groups of FQDNs for well-known Microsoft services (e.g., WindowsUpdate, Office365, AzureKubernetesService).37 Using a tag like AzureActiveDirectory in an Application Rule (HTTPS only) allows traffic to all associated Azure AD domains without needing to list them individually.33 Microsoft manages the FQDNs within the tag.37  
* **Service Tags:** Represent groups of IP address prefixes for Azure services (e.g., Storage, Sql, AzureBotService, AzureActiveDirectory).10 They are used in the source or destination fields of Network Rules to allow/deny traffic based on IP ranges, simplifying rules compared to managing explicit IP lists.38 Microsoft manages the IP prefixes within the tag.38 The AzureBotService tag is confirmed usable with Azure Firewall.38

The choice between using specific FQDNs/IPs versus Tags involves a trade-off. Tags offer easier management as Microsoft handles updates to the underlying addresses.37 However, explicit rules provide more granular control, which might be preferred or required by organizational security policies, as suggested by the user's Service Bus/AMQP port example \[User Query\]. Given the lack of a specific FQDN Tag for graph.microsoft.com 37, an explicit Application Rule for this FQDN is necessary. A practical approach often involves using Tags where available and appropriate (e.g., AzureActiveDirectory) and supplementing with explicit FQDN rules (graph.microsoft.com, \*.botframework.com) and Network Rules (using Service Tags or specific IPs/ports) as needed for other dependencies or stricter control.

## **6\. Comparative Analysis of Hosting Options for Secure Bot Deployment**

Choosing the optimal Azure service to host the Bot Framework application requires comparing Azure App Service, Azure Logic Apps (Standard), and Azure Container Apps based on their networking capabilities, particularly concerning Private Endpoint support for inbound traffic and controlled, firewalled VNet Integration for outbound traffic.

### **Feature Comparison**

The following table summarizes the key networking features relevant to this secure deployment scenario across the three hosting options:

| Feature | Azure App Service | Azure Logic Apps (Standard) | Azure Container Apps |
| :---- | :---- | :---- | :---- |
| **Inbound: Private Endpoint** | Yes (sites subresource) 11 | Yes (Logic App endpoint & backend Storage) 15 | Yes (managedEnvironment subresource, requires Workload Profile, HTTP only) 17 |
| **Outbound: VNet Integration** | Yes (Dedicated subnet, Standard+ tiers) 23 | Yes (Dedicated subnet) 15 | Yes (Dedicated subnet, requires Workload Profile) 18 |
| **Outbound: Route All Traffic** | Yes (App Setting/Portal Toggle) 25 | Yes (App Setting WEBSITE\_VNET\_ROUTE\_ALL) 16 | Yes (Implied via UDR support) 30 |
| **Outbound: UDR Support** | Yes (On integration subnet) 25 | Yes (On integration subnet) | Yes (Requires Workload Profile) 18 |
| **Outbound: NAT Gateway** | Yes (On integration subnet) | Yes (On integration subnet) | Yes (Requires Workload Profile) 18 |
| **Outbound: NSG Support** | Yes (On integration subnet) 23 | Yes (On integration subnet) | Yes (On integration subnet, requires Workload Profile) 18 |
| **Bot Isolation (DL-ASE)** | Yes (Windows only) 7 | No 34 | No 33 |
| **Networking Complexity** | Moderate (High if using DL-ASE) | High (due to Built-in vs. Managed connector VNet paths 27) | Moderate to High (Requires Workload Profiles for advanced networking) 18 |
| **Key Consideration** | Best isolation with DL-ASE. Mature networking. | Managed connector egress challenges. Workflow focus. | Container flexibility. Workload Profile cost/complexity. |

### **Detailed Comparison**

* **Azure App Service:** As a mature PaaS offering, App Service provides well-understood and robust networking features, including Private Endpoints and VNet Integration.11 Its unique advantage for bot scenarios is the support for the Direct Line App Service Extension (DL-ASE) on Windows plans.7 Combining DL-ASE with Private Endpoints and VNet Integration allows for the highest degree of network isolation, enabling the bot's messaging endpoint itself to reside entirely within the private network, inaccessible to the public Bot Connector service.21 While standard VNet integration is relatively straightforward, implementing the full DL-ASE network isolation pattern adds significant configuration complexity.21  
* **Azure Logic Apps (Standard):** Built on the App Service platform, Logic Apps Standard inherits strong networking capabilities like Private Endpoints (for both the trigger endpoint and backend storage) and VNet Integration.15 It excels at orchestrating complex workflows involving multiple services. However, the critical differentiator lies in how its connectors handle outbound traffic. Built-in connectors utilize VNet Integration correctly, allowing their traffic to be routed via UDRs to a central firewall.27 Managed connectors, conversely, operate outside this path, making uniform egress control through a central firewall challenging without careful planning or limitations on connector usage.27 Checking for the availability and suitability of *built-in* connectors for essential services like Teams 43 and Microsoft Graph 14 is crucial; relying on managed versions introduces the egress complexity.  
* **Azure Container Apps:** Provides a modern, serverless container platform built on Kubernetes principles.36 It supports Private Endpoints (preview) and VNet Integration, but requires the use of the **Workload Profiles** environment type to enable advanced networking features like UDR support (for firewall routing) and NAT Gateway integration.18 Consumption-only environments have more limited networking capabilities.18 While offering flexibility for containerized applications and microservices, achieving the required level of network control necessitates the potentially higher cost and complexity associated with Workload Profiles. Container Apps does not support DL-ASE.33

### **Selecting the Appropriate Option**

The optimal choice among these three services depends heavily on the specific priorities and constraints of the project and organization:

* **Prioritizing Bot Endpoint Isolation:** If the primary security driver is to completely isolate the bot's messaging endpoint from any potential interaction with the public Bot Connector service, Azure App Service combined with the Direct Line App Service Extension is the only architecture discussed that achieves this.7 This comes at the cost of increased setup complexity for DL-ASE networking.21  
* **Prioritizing Uniform Egress Control:** If the mandate is that *all* outbound traffic must strictly and uniformly transit the central Azure Firewall, then Azure App Service or Azure Container Apps (using Workload Profiles) are the more suitable choices.25 Azure Logic Apps Standard becomes less ideal in this scenario if managed connectors are required, due to their distinct network egress path.27  
* **Prioritizing Platform Features:** If the core requirement is workflow orchestration, Logic Apps Standard remains a strong candidate, provided the networking implications of managed connectors are understood and managed. If containerization, microservices, and Kubernetes-like features (without managing full AKS) are desired, Container Apps (with Workload Profiles) is the appropriate choice.18 App Service provides a general-purpose, well-established platform for web applications and APIs.

The decision requires balancing the need for specific isolation levels (DL-ASE), the consistency of network control mechanisms (especially for outbound traffic), platform capabilities, and tolerance for complexity and potential cost (Workload Profiles).

## **7\. Secure Microsoft Graph API Access from VNet-Integrated Bot**

Microsoft Graph API is often essential for Teams bots to interact with organizational data, such as user profiles, team memberships, channel messages, and files stored in SharePoint or OneDrive. Accessing Graph API securely from a bot hosted within a VNet-integrated service requires proper authentication and careful network path consideration.

### **Authentication**

The recommended and most secure method for authenticating the bot's calls to Microsoft Graph API is using **Managed Identities**.1

* **Mechanism:** Azure App Service, Logic Apps Standard, and Azure Container Apps all support system-assigned or user-assigned managed identities. The hosting platform manages the credentials (secrets or certificates) associated with the identity, rotating them automatically.46 The bot application code uses the managed identity to acquire OAuth tokens for Graph API without needing to handle or store any secrets directly.  
* **Permissions:** Appropriate Graph API permissions must be granted to the managed identity's corresponding service principal in Microsoft Entra ID. Permissions should follow the principle of least privilege. Examples include:  
  * User.Read.All: To read basic user profile information.  
  * ChannelMessage.Read.All: To read messages in channels the bot is part of.  
  * Files.ReadWrite.All: To read and write files accessible by the bot (e.g., in Teams channels).  
  * Specific permissions depend entirely on the bot's required functionality.  
* **Alternative:** If managed identities cannot be used, an alternative is to register the application in Microsoft Entra ID and use a service principal with client secrets or certificates. However, this requires robust secret management practices, ideally storing the secret or certificate thumbprint in Azure Key Vault and retrieving it securely at runtime.

### **Network Path**

When a bot running on a VNet-integrated service calls the Microsoft Graph API:

1. **Origin:** The HTTPS request originates from the application code within the hosting service (App Service, Logic App action, Container App).  
2. **VNet Integration:** The traffic egresses through the service's VNet Integration configuration into the designated spoke VNet subnet.15  
3. **UDR Routing:** The User-Defined Route (UDR) associated with the integration subnet intercepts the traffic destined for the public Graph API endpoint (graph.microsoft.com) and directs it to the private IP address of the Azure Firewall in the hub VNet.25  
4. **Firewall Inspection:** The Azure Firewall receives the traffic, inspects it against its configured policies.  
5. **Firewall Forwarding:** If an Application Rule permits HTTPS (port 443\) traffic to the FQDN graph.microsoft.com, the firewall forwards the request out to the public internet endpoint of the Microsoft Graph API.1  
6. **Response:** The response from Graph API follows the reverse path back through the firewall to the bot application.

### **Private Endpoint Availability for Graph API**

A critical factor influencing the network path is the availability of Private Endpoints for the target service. Based on current Azure Private Link documentation and service availability lists, **Microsoft Graph API does not support Private Endpoints**.9

This absence means that direct, private communication from the VNet to the Graph API is impossible. All interactions must target the public graph.microsoft.com endpoint.

### **Implications for Secure Architecture**

The lack of Private Endpoint support for Graph API reinforces the necessity of the VNet Integration, UDR, and Azure Firewall architecture for secure outbound communication. While Private Endpoints aim to keep traffic entirely within the private network boundary where possible, scenarios involving essential services like Graph API require a mechanism to securely *control* and *monitor* the required public egress, rather than eliminating it.

The Azure Firewall plays a vital role here:

* **Enforcement:** It ensures that outbound traffic to Graph API is explicitly allowed, logged, and potentially inspected.  
* **Restriction:** It prevents the bot from making unauthorized outbound calls to other internet destinations.

Therefore, configuring the firewall with a specific Application Rule allowing HTTPS traffic to graph.microsoft.com is not just recommended, but essential for enabling the bot to perform its required functions involving Teams or other Microsoft 365 data via Graph API.1 Authentication via Managed Identity ensures that even though the traffic transits a public endpoint, the access is securely authenticated and authorized.46

## **8..NET Aspire Impact on Secured Networking**

.NET Aspire is an opinionated stack designed to simplify the development of observable, production-ready, distributed.NET cloud-native applications.48 Its primary focus is on improving the *developer experience*, particularly during the local development (inner loop) phase, and providing standardized ways to integrate common services. Its impact on deploying applications into strictly controlled Azure network environments needs careful consideration.

### **Relevant.NET Aspire Features**

* **App Host Project:** Acts as an orchestrator during local development. It defines the different parts of the application (e.g.,.NET projects, containers like Redis, executables) and manages how they connect and run together.48  
* **Service Discovery & Configuration:** Simplifies how different components of the application discover and connect to each other locally. It automatically injects necessary connection strings, URLs, and environment variables into dependent projects, abstracting away the complexities of local network setup.48  
* **Azure Integration Components:** Provides NuGet packages (e.g., Aspire.Azure.Messaging.ServiceBus, Aspire.Azure.Storage.Blobs, Aspire.Npgsql.EntityFrameworkCore.PostgreSQL 52) that offer standardized patterns for connecting to specific Azure services.48 These components handle Dependency Injection (DI) registration, configuration binding, and often include built-in health checks and OpenTelemetry integration (logging, metrics, tracing).48 They can be configured to provision basic Azure resources during local development (using developer credentials) or connect to existing Azure resources or local emulators/containers.54  
* **Deployment Manifest & Bicep:** Aspire includes tooling (like the Azure Developer CLI integration) that can generate a deployment manifest or assist in producing Bicep templates to deploy the application components to Azure targets such as Azure Container Apps or Azure Kubernetes Service.49

### **Potential Simplification in Secured Environments**

While.NET Aspire doesn't directly configure the core Azure network infrastructure, it can offer benefits *within* such an environment:

* **Environment-Aware Configuration:** Aspire's configuration injection mechanism can simplify managing differences between local development and the secured Azure environment. The App Host can define resources differently based on the context (e.g., use a local Redis container locally, but connect to an Azure Cache for Redis instance via its connection string when deployed). For the bot, this could mean injecting the public graph.microsoft.com URL consistently, while potentially injecting the private endpoint FQDN for a different dependency (like a private Azure SQL Database or Azure Key Vault) when deployed into the VNet.48 This reduces the need for extensive \#if DEBUG directives or complex configuration loading logic within the bot application itself.  
* **Standardized Service Connections:** Using Aspire's Azure integration components ensures a consistent approach to connecting to dependent Azure services (like Azure Storage for Logic Apps state, or Azure Key Vault for secrets). This includes standardized setup for DI, health checks, and telemetry, which remain valuable even when the connection traverses a private network path (like VNet Integration or a Private Endpoint).53

### **Limitations**

* **No Network Infrastructure Configuration:**.NET Aspire's scope is the *application* and its immediate dependencies during development and deployment packaging. It does **not** provide abstractions or tooling for defining or configuring Azure network resources like VNets, Subnets, Network Security Groups, User-Defined Routes, Private Endpoints, or Azure Firewall rules.48 These foundational elements must be provisioned and managed independently using standard infrastructure-as-code tools (ARM, Bicep, Terraform), the Azure CLI, or the Azure portal.  
* **Inner-Loop Focus:** Aspire's orchestration and service discovery capabilities are primarily aimed at streamlining the *local development* experience.48 While its deployment features help package the application, it doesn't influence or manage the runtime network behavior or policy enforcement within Azure itself.  
* **Bot Framework Component:** As of the reviewed materials, there isn't a specific, dedicated .NET Aspire.BotFramework integration component listed.49 A Bot Framework application built with.NET would typically be treated as a standard ASP.NET Core project within the Aspire AppHost model. Configuration specific to the Bot SDK (like App ID/Password, or potentially DL-ASE settings) would likely be managed via standard environment variables or app settings, which Aspire can help inject.

### **Role of Aspire in the Secured Context**

In the context of deploying a bot into a highly secured Azure network,.NET Aspire's primary contribution is to streamline the **application's configuration and its integration with dependent services**, making the application code more adaptable to the underlying network architecture. It helps ensure the bot receives the correct connection strings and endpoint URLs (e.g., the public Graph API FQDN, a private Key Vault endpoint FQDN) based on the deployment environment, simplifying the application code. It also standardizes how connections to supported Azure services are configured regarding DI, health checks, and telemetry.

However, Aspire does **not** reduce the complexity of designing, provisioning, and configuring the necessary Azure networking infrastructure (VNets, subnets, Private Endpoints, VNet Integration, UDRs, NSGs, Azure Firewall rules). The responsibility for building and maintaining the secure network foundation remains separate from the application development facilitated by Aspire.

## **9\. Proposed Architectural Approaches**

Based on the analysis of communication flows, hosting options, and security features, the following architectural approaches can be considered. All approaches assume the organization's standard hub-spoke VNet topology with a central Azure Firewall managing egress traffic.

**Common Elements (Applicable to all architectures):**

* **Network Topology:** Hub VNet containing Azure Firewall; Spoke VNet hosting the bot application. VNet Peering established between Hub and Spoke.  
* **Egress Routing:** User-Defined Route (UDR) applied to the VNet Integration subnet in the Spoke VNet, with a default route (0.0.0.0/0) pointing to the Hub Firewall's private IP address.25  
* **Egress Filtering (Subnet):** Network Security Group (NSG) applied to the VNet Integration subnet, potentially restricting traffic but primarily ensuring traffic flows towards the Hub Firewall as dictated by the UDR.  
* **Egress Filtering (Firewall):** Azure Firewall configured with necessary Application Rules (HTTPS FQDNs/Tags: login.botframework.com, login.microsoftonline.com, \*.botframework.com, graph.microsoft.com, etc.) and potentially Network Rules (Service Tags/Ports) to allow required outbound communication from the bot.3  
* **Authentication:** System-assigned or user-assigned Managed Identity configured for the hosting service, granted necessary Microsoft Graph API permissions.46

### **Architecture 1: Azure App Service with Direct Line App Service Extension (DL-ASE)**

This architecture prioritizes maximum network isolation for the bot's messaging endpoint.

* **Hosting:** Bot application (.NET Aspire project treated as ASP.NET Core) deployed to an Azure App Service Plan (Windows SKU required for DL-ASE).7  
* **Inbound Security:**  
  * Direct Line channel added to the Azure Bot resource, and the App Service Extension tab configured.55  
  * DL-ASE enabled in the App Service configuration with necessary App Settings (DirectLineExtensionKey, DIRECTLINE\_EXTENSION\_VERSION).55 Bot code updated to use named pipes (UseNamedPipes in.NET).56  
  * Private Endpoint created for the **App Service** (Microsoft.Web/sites, sites subresource), providing a private IP within a dedicated subnet in the Spoke VNet.11  
  * Private Endpoint created for the **Azure Bot resource** targeting the Bot subresource. This allows DL-ASE (running in the App Service) to securely communicate with the Bot Service backend for metadata/operations via the VNet.21 The App Service requires an App Setting DirectLineExtensionABSEndpoint pointing to this Bot PE's private FQDN (e.g., https://\<your\_azure\_bot\>.privatelink.directline.botframework.com/v3/extension).21  
  * (Optional but recommended) Private Endpoint created for the **Azure Bot resource** targeting the Token subresource, allowing secure token endpoint access from within the VNet.22  
  * Public network access disabled on the App Service.11  
* **Outbound Security:**  
  * App Service configured with VNet Integration into a dedicated subnet (different from PE subnets) in the Spoke VNet.24  
  * 'Route All' traffic enabled for VNet Integration.25  
  * Outbound traffic routed via UDR to Hub Firewall.  
  * NSG on the integration subnet must allow outbound HTTPS (443) to AzureBotService and AzureActiveDirectory Service Tags to permit DL-ASE backend communication.33 Other required outbound traffic is allowed via the Firewall.  
* **Teams Connectivity:** Communication from Teams now targets the DL-ASE endpoint hosted within the App Service (e.g., https://\<app\_service\_name\>.azurewebsites.net/.bot/).57 Since the App Service only has a private endpoint, access from the Teams client requires a path that can resolve and reach this private endpoint. This typically implies users accessing Teams from within the corporate network (on-premises or VNet-connected workstations) where DNS resolves to the private IP, or potentially routing through a secure gateway (like Application Gateway) configured to target the App Service's Private Endpoint. The standard public Bot Connector is bypassed for message delivery to the bot.  
* **Conceptual Flow:** Teams Client \-\> (Internal Network / Secure Gateway) \-\> App Service Private Endpoint \-\> App Service (Bot \+ DL-ASE). Outbound: App Service (via VNet Int Subnet) \-\> UDR \-\> Hub Firewall \-\> Required Internet Endpoints (Graph API, Bot Service Backend).

### **Architecture 2: Azure Logic Apps (Standard)**

This architecture leverages Logic Apps for workflow-based bot logic but faces challenges with uniform egress control if managed connectors are used.

* **Hosting:** Bot logic implemented within a Standard Logic App resource.14  
* **Inbound Security:**  
  * Workflow triggered by a built-in trigger like 'Request' or 'HTTP \+ Webhook'.15  
  * Private Endpoint created for the **Logic App resource** itself, providing a private IP in the Spoke VNet.15  
  * Private Endpoint created for the **backend Azure Storage account** used by the Logic App Standard instance.16  
  * Public network access potentially disabled for the Logic App and Storage Account.  
* **Outbound Security:**  
  * Logic App configured with VNet Integration into a dedicated subnet in the Spoke VNet.15  
  * Required App Settings (WEBSITE\_VNET\_ROUTE\_ALL=1, WEBSITE\_CONTENTOVERVNET=1, potentially DNS settings if custom DNS is used) configured.16  
  * Outbound traffic from **Built-in Connectors** utilizes VNet Integration and is routed via UDR to the Hub Firewall.27  
  * Outbound traffic from **Managed Connectors** (e.g., potentially Teams, Graph API if used as managed connectors) originates from shared public IPs and bypasses the VNet Integration UDR by default.27 Firewall rules must explicitly allow traffic from the firewall to the target services, or complex NSG rules are needed on the integration subnet.  
* **Teams Connectivity:** Similar challenge to DL-ASE avoidance. The public Bot Connector cannot call the Logic App's Private Endpoint. Teams integration would require either:  
  * A custom client/adapter within Teams that *can* call the Logic App's private endpoint (via internal network or secure gateway).  
  * Exposing the Logic App trigger endpoint securely via Application Gateway (targeting the PE) and configuring the Bot Channel registration to point to the App Gateway public endpoint (requires careful security review).  
  * Accepting that standard Teams channel integration via Bot Connector is not feasible with this PE-only inbound approach.  
* **Conceptual Flow:** (Internal Client / Secure Gateway) \-\> Logic App Private Endpoint \-\> Logic App Workflow. Outbound (Built-in): Logic App (via VNet Int Subnet) \-\> UDR \-\> Hub Firewall \-\> Target Service. Outbound (Managed): Logic App \-\> Managed Connector Service (Public IPs) \-\> Target Service (Potentially bypassing Hub Firewall egress path).

### **Architecture 3: Azure Container Apps**

This architecture provides a container-native platform, requiring Workload Profiles for the necessary network controls.

* **Hosting:** Bot application packaged as a container and deployed to an Azure Container Apps Environment configured with **Workload Profiles**.18  
* **Inbound Security:**  
  * Ingress enabled for the specific container app within the environment.18  
  * Private Endpoint created for the **Container Apps Environment** (managedEnvironment subresource), providing a private IP for the environment's internal load balancer within the Spoke VNet.17  
  * Public network access disabled at the **Environment** level.17  
* **Outbound Security:**  
  * Container Apps Environment integrated with the Spoke VNet using a dedicated subnet during creation.18  
  * UDR applied to the integration subnet, routing 0.0.0.0/0 traffic to the Hub Firewall.30  
  * NSG applied to the integration subnet allows necessary outbound flows (towards firewall, potentially platform requirements).31  
  * All outbound traffic from containers within the environment is forced through the Hub Firewall.  
* **Teams Connectivity:** Faces the same challenge as Logic Apps Standard regarding the public Bot Connector being unable to reach the Private Endpoint of the ACA environment. Requires an internal access path or secure exposure via a gateway (e.g., Application Gateway targeting the PE) for Teams integration.  
* **Conceptual Flow:** (Internal Client / Secure Gateway) \-\> ACA Environment Private Endpoint \-\> ACA Internal Load Balancer \-\> Container App Instance. Outbound: Container App (via VNet Int Subnet) \-\> UDR \-\> Hub Firewall \-\> Required Internet Endpoints (Graph API, Bot Services).

## **10\. Recommendations and Conclusion**

This report has analyzed the network communication flows and security mechanisms pertinent to deploying a.NET Aspire Bot Framework application within a highly restricted Azure environment featuring Private Endpoints and mandatory hub firewall egress control. Key findings highlight the inherent conflict between the standard Bot Framework's reliance on public endpoints and the requirement for private network isolation, the critical role of VNet Integration and Azure Firewall for controlled outbound traffic, the unique isolation capabilities offered by the Direct Line App Service Extension (DL-ASE), the networking complexities introduced by managed connectors in Logic Apps Standard, the necessity of Workload Profiles for advanced networking in Container Apps, the unavoidable requirement for controlled public egress to access Microsoft Graph API due to its lack of Private Endpoint support, and the role of.NET Aspire in application configuration rather than network infrastructure setup.

Based on these findings, the following recommendations are provided, considering different organizational priorities:

1. **For Maximum Bot Endpoint Isolation:** If the primary security objective is to completely isolate the bot's messaging endpoint from the public internet and the standard Bot Connector service, the **Azure App Service with Direct Line App Service Extension (DL-ASE)** architecture is the most suitable approach.7 This requires using a Windows App Service plan and involves significant configuration complexity for the Private Endpoints (App Service, Bot resource 'Bot' and 'Token' subresources) and VNet Integration NSG rules.21 Access from Teams clients will likely require internal network connectivity or a securely configured gateway targeting the App Service Private Endpoint.  
2. **For Uniform Egress Control & Containerization:** If the priority is leveraging container technology and ensuring *all* outbound traffic uniformly transits the central Azure Firewall, **Azure Container Apps (using Workload Profiles)** is a strong recommendation.18 This provides robust network control via VNet Integration, UDRs, and NSGs, similar to App Service, but in a container-native platform. It requires accepting the cost and complexity of Workload Profiles and understanding that the bot's inbound endpoint (secured by the environment's PE) cannot be directly called by the public Bot Connector for standard Teams channel integration.  
3. **For Uniform Egress Control & PaaS Simplicity:** If containerization is not a requirement, but uniform egress control is essential, **Azure App Service (without DL-ASE)** offers a mature, well-understood platform with reliable VNet Integration, UDR, and NSG support.23 Similar to ACA and Logic Apps, the Private Endpoint secures inbound access but prevents direct calls from the public Bot Connector, requiring alternative integration patterns for Teams communication if PE-only access is enforced.  
4. **For Workflow-Centric Bots (with Caveats):** **Azure Logic Apps Standard** is viable if the bot's logic is primarily workflow-based *and* either a) only built-in connectors are used, or b) the organization can accept or manage the distinct, non-VNet-integrated egress path of managed connectors.27 If strict, uniform firewall control for *all* outbound traffic (including that from potential Teams or Graph managed connectors) is non-negotiable, Logic Apps Standard presents significant challenges in this specific network architecture. Like ACA and App Service without DL-ASE, PE secures inbound but complicates standard Teams channel integration.

**Final Conclusion:**

Successfully deploying a Teams bot in a highly secured Azure environment demands a deliberate choice of hosting platform based on specific security priorities and operational preferences. For the strictest isolation of the bot endpoint itself, App Service with DL-ASE is the technically appropriate, albeit complex, solution. For consistent network control in containerized or standard PaaS environments, Container Apps (with Workload Profiles) and App Service (without DL-ASE) offer reliable options, acknowledging the implications for standard Teams channel connectivity when using only Private Endpoints for inbound access. Logic Apps Standard requires careful consideration of connector types to ensure compliance with strict egress policies. Regardless of the chosen platform, secure access to Microsoft Graph API necessitates controlled public egress managed via Azure Firewall, as Private Link support is currently unavailable..NET Aspire serves as a valuable tool for streamlining the bot application's configuration and dependency management within these complex network architectures but does not configure the network infrastructure itself. Careful planning, considering the trade-offs between isolation depth, egress control consistency, platform features, and complexity, is essential for a successful and secure deployment.

#### **Works cited**

1. Communication flow for Microsoft Teams bot applications, accessed April 16, 2025, [https://cdn.graph.office.net/prod/pdf/teams-to-bot-communication-final.pdf](https://cdn.graph.office.net/prod/pdf/teams-to-bot-communication-final.pdf)  
2. Bot Framework Availability Frequently Asked Questions | Azure Docs, accessed April 16, 2025, [https://docs.azure.cn/en-us/bot-service/bot-service-resources-faq-availability?view=azure-bot-service-4.0](https://docs.azure.cn/en-us/bot-service/bot-service-resources-faq-availability?view=azure-bot-service-4.0)  
3. Communication flow for Microsoft Teams bot applications, accessed April 16, 2025, [https://cdn.graph.office.net/prod/pdf/teams-to-bot-communication-final.pdf?v={2/string}](https://cdn.graph.office.net/prod/pdf/teams-to-bot-communication-final.pdf?v=%7B2/string%7D)  
4. Bot Framework authentication types | Azure Docs, accessed April 16, 2025, [https://docs.azure.cn/en-us/bot-service/bot-builder-concept-authentication-types?view=azure-bot-service-4.0](https://docs.azure.cn/en-us/bot-service/bot-builder-concept-authentication-types?view=azure-bot-service-4.0)  
5. Bot Framework Security and Privacy Frequently Asked Questions ..., accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-service-resources-faq-security?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-resources-faq-security?view=azure-bot-service-4.0)  
6. Authenticate requests with the Bot Connector API \- Bot Service | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/rest-api/bot-framework-rest-connector-authentication?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/rest-api/bot-framework-rest-connector-authentication?view=azure-bot-service-4.0)  
7. Direct Line App Service extension \- Bot Service \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension?view=azure-bot-service-4.0)  
8. Azure Bot Services: Deploy an Endpoint (Microsoft Teams) \- Cognigy.AI Help Center, accessed April 16, 2025, [https://support.cognigy.com/hc/en-us/articles/360016183720-Azure-Bot-Services-Deploy-an-Endpoint-Microsoft-Teams](https://support.cognigy.com/hc/en-us/articles/360016183720-Azure-Bot-Services-Deploy-an-Endpoint-Microsoft-Teams)  
9. What is a private endpoint? \- Azure Private Link | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview](https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview)  
10. Virtual network integration of Azure services for network isolation | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/virtual-network/vnet-integration-for-azure-services](https://learn.microsoft.com/en-us/azure/virtual-network/vnet-integration-for-azure-services)  
11. Use private endpoints for Azure App Service apps \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/app-service/overview-private-endpoint](https://learn.microsoft.com/en-us/azure/app-service/overview-private-endpoint)  
12. What is Azure Private Link? | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/private-link/private-link-overview](https://learn.microsoft.com/en-us/azure/private-link/private-link-overview)  
13. Quickstart: Create a private endpoint by using the Azure portal \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/private-link/create-private-endpoint-portal](https://learn.microsoft.com/en-us/azure/private-link/create-private-endpoint-portal)  
14. Overview \- Azure Logic Apps | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-overview](https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-overview)  
15. Secure traffic between Standard logic apps and Azure virtual networks using private endpoints \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/logic-apps/secure-single-tenant-workflow-virtual-network-private-endpoint](https://learn.microsoft.com/en-us/azure/logic-apps/secure-single-tenant-workflow-virtual-network-private-endpoint)  
16. Deploy Standard logic apps to private storage accounts \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/logic-apps/deploy-single-tenant-logic-apps-private-storage-account](https://learn.microsoft.com/en-us/azure/logic-apps/deploy-single-tenant-logic-apps-private-storage-account)  
17. Use a private endpoint with an Azure Container Apps environment (preview), accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/container-apps/how-to-use-private-endpoint](https://learn.microsoft.com/en-us/azure/container-apps/how-to-use-private-endpoint)  
18. Networking in Azure Container Apps environment | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/container-apps/networking](https://learn.microsoft.com/en-us/azure/container-apps/networking)  
19. Azure Private Endpoint private DNS zone values \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns](https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns)  
20. Using private endpoints for Azure App Configuration | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/azure-app-configuration/concept-private-endpoint](https://learn.microsoft.com/en-us/azure/azure-app-configuration/concept-private-endpoint)  
21. Configure network isolation \- Bot Service | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-how-to?view=azure-bot-service-4.0)  
22. About network isolation in Azure AI Bot Service, accessed April 16, 2025, [https://docs.azure.cn/en-us/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0](https://docs.azure.cn/en-us/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0)  
23. Integrate your app with an Azure virtual network \- Azure App Service ..., accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/app-service/overview-vnet-integration](https://learn.microsoft.com/en-us/azure/app-service/overview-vnet-integration)  
24. Enable virtual network integration in Azure App Service \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/app-service/configure-vnet-integration-enable](https://learn.microsoft.com/en-us/azure/app-service/configure-vnet-integration-enable)  
25. App Service outbound traffic control with Azure Firewall \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/app-service/network-secure-outbound-traffic-azure-firewall](https://learn.microsoft.com/en-us/azure/app-service/network-secure-outbound-traffic-azure-firewall)  
26. VNet Integration in Azure Standard Logic App Slots Error \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/answers/questions/2258309/vnet-integration-in-azure-standard-logic-app-slots](https://learn.microsoft.com/en-us/answers/questions/2258309/vnet-integration-in-azure-standard-logic-app-slots)  
27. Built-in operations versus Azure connectors in Standard \- Azure ..., accessed April 16, 2025, [https://docs.azure.cn/en-us/connectors/compare-built-in-azure-connectors](https://docs.azure.cn/en-us/connectors/compare-built-in-azure-connectors)  
28. Built-in operations versus Azure connectors in Standard \- Azure Logic Apps | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/connectors/compare-built-in-azure-connectors](https://learn.microsoft.com/en-us/azure/connectors/compare-built-in-azure-connectors)  
29. Do I need to use Azure NAT Gateway for outbound internet access for Azure Container Apps or Azure Database for PostgreSQL? \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/answers/questions/1689828/do-i-need-to-use-azure-nat-gateway-for-outbound-in](https://learn.microsoft.com/en-us/answers/questions/1689828/do-i-need-to-use-azure-nat-gateway-for-outbound-in)  
30. Control outbound traffic in Azure Container Apps with user defined routes \- Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/container-apps/user-defined-routes](https://learn.microsoft.com/en-us/azure/container-apps/user-defined-routes)  
31. Securing a custom VNET in Azure Container Apps | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/container-apps/firewall-integration](https://learn.microsoft.com/en-us/azure/container-apps/firewall-integration)  
32. Networking features \- Azure App Service | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/app-service/networking-features](https://learn.microsoft.com/en-us/azure/app-service/networking-features)  
33. Use Direct Line App Service extension within a virtual network \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-vnet?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-vnet?view=azure-bot-service-4.0)  
34. azure-docs/articles/private-link/availability.md at main \- GitHub, accessed April 16, 2025, [https://github.com/MicrosoftDocs/azure-docs/blob/main/articles/private-link/availability.md?toc=/azure/reliability/toc.json\&bc=/azure/reliability/breadcrumb/toc.json](https://github.com/MicrosoftDocs/azure-docs/blob/main/articles/private-link/availability.md?toc=/azure/reliability/toc.json&bc=/azure/reliability/breadcrumb/toc.json)  
35. Edit the host.json in my Azure Standard Logic App \- Stack Overflow, accessed April 16, 2025, [https://stackoverflow.com/questions/79464782/edit-the-host-json-in-my-azure-standard-logic-app](https://stackoverflow.com/questions/79464782/edit-the-host-json-in-my-azure-standard-logic-app)  
36. Any reason to deploy as Azure Container App (Linux) vs. Azure App Service \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/AZURE/comments/198yirn/any\_reason\_to\_deploy\_as\_azure\_container\_app\_linux/](https://www.reddit.com/r/AZURE/comments/198yirn/any_reason_to_deploy_as_azure_container_app_linux/)  
37. FQDN tags overview for Azure Firewall | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/firewall/fqdn-tags](https://learn.microsoft.com/en-us/azure/firewall/fqdn-tags)  
38. Azure service tags overview | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/virtual-network/service-tags-overview](https://learn.microsoft.com/en-us/azure/virtual-network/service-tags-overview)  
39. Azure/azure-functions-on-container-apps: Docs , samples and issues for Azure Functions on Azure Container Apps \- GitHub, accessed April 16, 2025, [https://github.com/Azure/azure-functions-on-container-apps](https://github.com/Azure/azure-functions-on-container-apps)  
40. Set just one outboundAdress in container app \- Microsoft Q\&A, accessed April 16, 2025, [https://learn.microsoft.com/en-us/answers/questions/2125471/set-just-one-outboundadress-in-container-app](https://learn.microsoft.com/en-us/answers/questions/2125471/set-just-one-outboundadress-in-container-app)  
41. About network isolation in Azure AI Bot Service \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/dl-network-isolation-concept?view=azure-bot-service-4.0)  
42. About network isolation in Azure AI Bot Service \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/architecture/example-scenario/teams/securing-bot-teams-channel](https://learn.microsoft.com/en-us/azure/architecture/example-scenario/teams/securing-bot-teams-channel)  
43. What are connectors \- Azure Logic Apps | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/connectors/introduction](https://learn.microsoft.com/en-us/azure/connectors/introduction)  
44. Built-in connectors in Azure Logic Apps \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/connectors/built-in](https://learn.microsoft.com/en-us/azure/connectors/built-in)  
45. How to use Managed or Enterprise connector on Azure Logic App Standard plan?, accessed April 16, 2025, [https://stackoverflow.com/questions/70297531/how-to-use-managed-or-enterprise-connector-on-azure-logic-app-standard-plan](https://stackoverflow.com/questions/70297531/how-to-use-managed-or-enterprise-connector-on-azure-logic-app-standard-plan)  
46. Azure security baseline for Azure Bot Service | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline](https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/azure-bot-service-security-baseline)  
47. Tutorial: Connect to an Azure SQL server using an Azure Private Endpoint \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/private-link/tutorial-private-endpoint-sql-portal](https://learn.microsoft.com/en-us/azure/private-link/tutorial-private-endpoint-sql-portal)  
48. NET Aspire overview \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)  
49. .NET Aspire documentation | Microsoft Learn, accessed April 16, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/](https://learn.microsoft.com/en-us/dotnet/aspire/)  
50. Why Everyone Will be Using .NET Aspire /w David Fowler : r/dotnet \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/dotnet/comments/1isllmz/why\_everyone\_will\_be\_using\_net\_aspire\_w\_david/](https://www.reddit.com/r/dotnet/comments/1isllmz/why_everyone_will_be_using_net_aspire_w_david/)  
51. NET Aspire inner loop networking overview \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview)  
52. .NET Aspire different services for production and local development? : r/dotnet \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/dotnet/comments/1jawpwt/net\_aspire\_different\_services\_for\_production\_and/](https://www.reddit.com/r/dotnet/comments/1jawpwt/net_aspire_different_services_for_production_and/)  
53. NET Aspire integrations overview \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/integrations-overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/integrations-overview)  
54. NET Aspire Azure integrations overview \- Learn Microsoft, accessed April 16, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/azure/integrations-overview](https://learn.microsoft.com/en-us/dotnet/aspire/azure/integrations-overview)  
55. Configure Node.js bots for Direct Line App Service extension in the Bot Framework SDK, accessed April 16, 2025, [https://docs.azure.cn/en-us/bot-service/bot-service-channel-directline-extension-node-bot?view=azure-bot-service-4.0](https://docs.azure.cn/en-us/bot-service/bot-service-channel-directline-extension-node-bot?view=azure-bot-service-4.0)  
56. Configure .NET bots for the Direct Line App Service extension in the Bot Framework SDK, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-net-bot?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-net-bot?view=azure-bot-service-4.0)  
57. Use Web Chat with the Direct Line App Service extension in Bot Framework SDK, accessed April 16, 2025, [https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-webchat-client?view=azure-bot-service-4.0](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-channel-directline-extension-webchat-client?view=azure-bot-service-4.0)