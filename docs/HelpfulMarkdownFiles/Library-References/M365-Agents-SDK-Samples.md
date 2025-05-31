# Code Dump: M365-Agents-SDK

*Generated on: Nucleus*

## Samples/complex/copilotstudio-skill/dotnet/EchoSkill.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using System.Threading.Tasks;
using System.Threading;

namespace CopilotStudioEchoSkill;

public class EchoSkill : AgentApplication
{
    public EchoSkill(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        OnActivity(ActivityTypes.EndOfConversation, EndOfConversationAsync);

        // Listen for ANY message to be received. MUST BE AFTER ANY OTHER MESSAGE HANDLERS
        OnActivity(ActivityTypes.Message, OnMessageAsync);

        // Handle uncaught exceptions be resetting ConversationState and letting MCS know the conversation is over.
        OnTurnError(async (turnContext, turnState, exception, cancellationToken) =>
        {
            await turnState.Conversation.DeleteStateAsync(turnContext, cancellationToken);

            var eoc = Activity.CreateEndOfConversationActivity();
            eoc.Code = EndOfConversationCodes.Error;
            eoc.Text = exception.Message;
            await turnContext.SendActivityAsync(eoc, cancellationToken);
        });
    }

    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync("Hi, This is EchoSkill", cancellationToken: cancellationToken);
            }
        }
    }

    protected async Task EndOfConversationAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        // This will be called if MCS is ending the conversation.  Sending additional messages should be
        // avoided as the conversation may have been deleted.
        // Perform cleanup of resources if needed.
        await turnState.Conversation.DeleteStateAsync(turnContext, cancellationToken);
    }

    protected async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (turnContext.Activity.Text.Contains("end"))
        {
            await turnContext.SendActivityAsync("(EchoSkill) Ending conversation...", cancellationToken: cancellationToken);

            // Indicate this conversation is over by sending an EndOfConversation Activity.
            // This Agent doesn't return a value, but if it did it could be put in Activity.Value.
            var endOfConversation = Activity.CreateEndOfConversationActivity();
            endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
            await turnContext.SendActivityAsync(endOfConversation, cancellationToken);
        }
        else
        {
            await turnContext.SendActivityAsync($"(EchoSkill): {turnContext.Activity.Text}", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("(EchoSkill): Say \"end\" and I'll end the conversation.", cancellationToken: cancellationToken);
        }
    }
}

```

## Samples/complex/copilotstudio-skill/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CopilotStudioEchoSkill;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add AgentApplicationOptions from config.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<EchoSkill>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
})
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");
app.MapGet("/", () => "Microsoft Agents SDK Sample");
app.UseStaticFiles();

app.Run();

```

## Samples/complex/copilotstudio-skill/dotnet/CopilotStudioEchoSkill.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>
```

## Samples/complex/copilotstudio-skill/dotnet/appsettings.json

```json
{
  "AgentApplication": {
    "StartTypingTimer": false,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
        "ClientSecret": "", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [ // This maps the Activity.ServiceUrl to the Connection to to call back to that ServiceUrl.
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}

```

## Samples/complex/copilotstudio-skill/dotnet/README.md

```markdown
﻿# Microsoft Copilot Studio Skill Sample

This is a sample of a simple conversational Agent that can be used from the Microsoft Copilot Studio Skills feature.

This sample is intended to introduce you to:
- The basic operation of the Microsoft 365 Agents SDK messaging handling.
- Requirements of a conversational Agent being used by another Agent.
- Using this Agent from Microsoft Copilot Studio.

## Basic concepts
- A Skill is an Agent.  There is nothing inherently different from other Agents.
- While many Agents are conversational, they can respond to any messages sent using the Activity Protocol.
- Conversations are multi-turn interactions, including possibly a large number of exchanges with the user until the conversation is complete.  
- Agents being called by another Agent are expected to:
  - Indicate when the conversation is over, with a "success" result and optional return value.
  - Indicate the conversation is over after a critical error.
- Microsoft Copilot Studio requires a manifest that describes the Agent capabilities.

## Prerequisites

- [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Using this Agent from Microsoft Copilot Studio as a Skill

- In order to use this sample from Copilot Studio, the Agent will need to be created on Azure, and authentication setup.
  - It is possible to run the Agent locally for debugging purposes.  
  - Managed Identity does not work locally so `ClientSecret` or `Certificate` will need to be used.
  - A tunnel will be required to route messages sent to the Agent to you locally running agent.
- Copilot Studio requires a manifest for the agent ("Skills Manifest").  This is discussed below.

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL Authentication provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "Connections": {
          "ServiceConnection": {
          "Assembly": "Microsoft.Agents.Authentication.Msal",
          "Type":  "MsalAuth",
          "Settings": {
              "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
              "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
              "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
              "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
              "Scopes": [
                "https://api.botframework.com/.default"
              ],
              
          }
      }
      ```

      1. Replace all `{{ClientId}}` with the AppId of the Azure Bot.
      1. Replace all `{{TenantId}}` with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Updating the Agent Manifest
   - Open the [sample manifest](./wwwroot/manifest/echoskill-manifest-1.0.json)
   - Update the `privacyUrl`, `iconUrl`, `msAppId`, and `endpointUrl`
   - If you are running the Agent locally, this will be `{tunnel-url}/api/messages`
   - Once EchoSkill is started, the manifest is available via a GET to `{host-or-tunnel-url}/manifest/echoskill-manifest-1.0.json`

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent in Visual Studio

### Using EchoSkill in Copilot Studio
- In order to use an Agent from Copilot Studio Skills
  - The Azure Bot and Identity must have been created on Azure
  - If the Agent is running locally, the "Messaging Endpoint" on the Azure Bot must be set to `{tunnel-url}/api/messages`
  - On the App Registration on Azure, the "Home page URL" should be set to the App Service URL (or the Tunnel URL)
- Create a new, or open an existing, Copilot Studio Agent
- Go to Agent **Settings**
- Select **Skills** on the left sidebar
- Select **Add a Skill**
- Enter the URL to the skill manifest, for example `{host-or-tunnel-url}/manifest/echoskill-manifest-1.0.json`
- Click **Save** when validation is complete.
- From any Topic, add a new **Call an Action** node, and select "Echo messages from user"
- Test the agent in Copilot Studio.

## Further reading
To learn more about building Agents, see our [Microsoft Agents Framework on GitHub](https://github.com/microsoft/agents) repo.

```

## Samples/complex/copilotstudio-skill/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.echoskill",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Copilot Studio EchoSkill Sample",
    "full": "Copilot Studio EchoSkill Sample"
  },
  "description": {
    "short": "Sample demonstrating an Skill that can be called by Copilot Studio",
    "full": "This sample demonstrates how echo text sent back to a client as a skill for intergraiton with Microsoft Copilot Studio"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<AGENT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/complex/copilotstudio-skill/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.echoskill",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Copilot Studio EchoSkill Sample",
    "full": "Copilot Studio EchoSkill Sample"
  },
  "description": {
    "short": "Sample demonstrating an Skill that can be called by Copilot Studio",
    "full": "This sample demonstrates how echo text sent back to a client as a skill for intergraiton with Microsoft Copilot Studio"
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal",
        "groupChat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<AGENT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/complex/copilotstudio-skill/dotnet/appManifest/.gitignore

```ignore
manifest.json
*.zip
```

## Samples/complex/copilotstudio-skill/dotnet/wwwroot/default.htm

```
﻿<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>CopilotStudioEchoSkill</title>
    <style>
        ...styles...
    </style>
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            loadFrame();
        });
        var loadFrame = function () {
            var iframe = document.createElement('iframe');
            iframe.setAttribute("id", "iframe");
            var offLineHTMLContent = "";
            var frameElement = document.getElementById("how-to-iframe");
            if (window.navigator.onLine) {
                iframe.src = 'https://docs.botframework.com/static/abs/pages/f5.htm';
                iframe.setAttribute("scrolling", "no");
                iframe.setAttribute("frameborder", "0");
                iframe.setAttribute("width", "100%");
                iframe.setAttribute("height", "100%");
                var frameDiv = document.getElementById("how-to-iframe");
                frameDiv.appendChild(iframe);
            } else {
                frameElement.classList.add("remove-frame-height");
            }
        };
    </script>
</head>

<body>
    <header class="header">
        <div class="header-inner-container">
            <div class="header-icon" style="display: inline-block"></div>
            <div class="header-text" style="display: inline-block">CopilotStudioEchoSkill Agent</div>
        </div>
    </header>
    <div class="row">
        <div class="column" class="main-content-area">
            <div class="content-title">Your agent is ready!</div>
            <div class="main-text main-text-p1">
                You can test your agent in the Bot Framework Emulator<br />
                by connecting to http://localhost:3978/api/messages.
            </div>
            <div class="main-text download-the-emulator">
                <a class="ctaLink"
                   href="https://aka.ms/bot-framework-F5-download-emulator" target="_blank">Download the Emulator</a>
            </div>
            <div class="main-text">
                Visit <a class="ctaLink" href="https://aka.ms/bot-framework-F5-abs-home"
                         target="_blank">
                    Azure
                    Bot Service
                </a> to register your agent and add it to<br />
                various channels. The agent's endpoint URL typically looks
                like this:
            </div>
            <div class="endpoint">https://<i>your_agents_hostname</i>/api/messages</div>
        </div>
        <div class="column how-to-iframe" id="how-to-iframe"></div>
    </div>
    <div class="ms-logo-container">
        <div class="ms-logo"></div>
    </div>
</body>

</html>
```

## Samples/complex/copilotstudio-skill/dotnet/wwwroot/manifest/echoskill-manifest-1.0.json

```json
{
  "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json",
  "$id": "CopilotStudioEchoSkill",
  "name": "CopilotStudioEchoSkill",
  "version": "1.0",
  "description": "This is a sample Agent that can be called by Copilot Studio",
  "publisherName": "Microsoft",
  "privacyUrl": "<<BOT-PRIVACY-URL>>",
  "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
  "license": "",
  "iconUrl": "<<BOT-ICON-URL>>",
  "tags": [
    "sample",
    "skill"
  ],
  "endpoints": [
    {
      "name": "default",
      "protocol": "BotFrameworkV3",
      "description": "Default endpoint for the Agent",
      "endpointUrl": "<<BOT-MESSAGING-ENDPOINT>>",
      "msAppId": "<<BOT-APPID>>"
    }
  ],
  "activities": {
    "message": {
      "type": "message",
      "description": "Echo messages from user"
    }
  }
}
```

## Samples/complex/copilotstudio-skill/nodejs/.npmrc

```
package-lock=false
```

## Samples/complex/copilotstudio-skill/nodejs/tsconfig.json

```json
{
  "compilerOptions": {
      "incremental": true,
      "lib": ["ES2021"],
      "target": "es2019",
      "module": "commonjs",
      "declaration": true,
      "sourceMap": true,
      "composite": true,
      "strict": true,
      "moduleResolution": "node",
      "esModuleInterop": true,
      "skipLibCheck": true,
      "forceConsistentCasingInFileNames": true,
      "resolveJsonModule": true,
      "rootDir": "src",
      "outDir": "dist",
      "tsBuildInfoFile": "dist/.tsbuildinfo"
  }
}

```

## Samples/complex/copilotstudio-skill/nodejs/package.json

```json
{
  "name": "node-echo-skill",
  "version": "1.0.0",
  "main": "index.js",
  "scripts": {
    "prebuild": "npm install",
    "build": "tsc --build",
    "prestart" : "npm run build",
    "start": "node --env-file .env dist/index.js"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "description": "",
  "dependencies": {
    "@microsoft/agents-hosting": "^0.4.0",
    "@microsoft/microsoft-graph-client": "^3.0.7",
    "express": "^5.1.0"
  },
  "devDependencies": {
    "globals": "^16.1.0"
  }
}

```

## Samples/complex/copilotstudio-skill/nodejs/env.TEMPLATE

```
# rename to .env
tenantId=
clientId=
clientSecret=
DEBUG=agents:*:error
```

## Samples/complex/copilotstudio-skill/nodejs/README.md

```markdown
# copilotstudio-skill

This sample shows how to create an Agent that can be consumed from CopilotStudio as a skill.

## Prerequisites

- [Node.js](https://nodejs.org) version 20 or higher

    ```bash
    # determine node version
    node --version
    ```

- Access to CopilotStudio to [create an Agent](https://learn.microsoft.com/en-us/microsoft-copilot-studio/fundamentals-get-started?tabs=web)

## Deploy in localhost

The first time you setup the agent to be consumed as a skill, you might want to do it in `localhost`, so you can debug and revisit the desired configuration before deploying to azure. 
To do so you can use the tool [Dev Tunnels](https://aka.ms/devtunnels) to expose an endpoint in your machine to the internet. 

```bash
devtunnel host -a -p 3978
```

Take note of the tunnelUrl

1. Create an EntraID app registration, and save the authentication information in the `.env` file, following any of the [available options](https://microsoft.github.io/Agents/HowTo/azurebot-auth-for-js.html) for Single Tenant.
1. Update the Home page URL in the app registration with the tunnel url.
1. Update the manifest replacing the `{baseUrl}` with the `{tunnelUrl}` and `{clientId}` with the `{appId}` from EntraID
1. - [Configure a skill](https://learn.microsoft.com/en-us/microsoft-copilot-studio/configuration-add-skills#configure-a-skill)
    1. In CopilotStudio navigate to the Agent settings, skills, and register the skill with the URL `{tunnelUrl}/manifest.json`
    1. In CopilotStudio navigate to the Agent topics, add a new trigger to invoke an action to call the skill

## Deploy in Azure

1. Deploy the express application to Azure, using AppService, AzureVMs or Azure Container Apps.
1. Update the Home page URL in the app registration with the Azure service URL.
1. Create an EntraID app registration, and configure the Azure instance following any of the [available options](https://microsoft.github.io/Agents/HowTo/azurebot-auth-for-js.html) for SingleTenant.
1. - [Configure a skill](https://learn.microsoft.com/en-us/microsoft-copilot-studio/configuration-add-skills#configure-a-skill)
    1. In CopilotStudio navigate to the Agent settings, skills, and register the skill with the URL `{azureServiceUrl}/manifest.json`
    1. In CopilotStudio navigate to the Agent topics, add a new trigger to invoke an action to call the skill


## Running this sample

1. Once the express app is running, in localhost or in Azure
1. Use the CopilotStudio _Test your agent_ chat UI, and use the trigger defined to invoke the skill

# Helper Scripts

Replace Manifest Values

```ps
$url='{tunnelUrl}'
$clientId='{clientId}'
(Get-Content public/manifest.template.json).Replace('{baseUrl}', $url).Replace('{clientId}', $clientId) | Set-Content public/manifest.json
```

```

## Samples/complex/copilotstudio-skill/nodejs/src/agent.ts

```typescript
import { AgentApplication, MessageFactory } from '@microsoft/agents-hosting'
import pjson from '@microsoft/agents-hosting/package.json'

export const skillAgent = new AgentApplication()
skillAgent.onConversationUpdate('membersAdded', async (context) => {
  const welcomeText = `Hello from echo bot, running on version ${pjson.version}`
  await context.sendActivity(MessageFactory.text(welcomeText, welcomeText))
})
skillAgent.onActivity('message', async (context) => {
  const text = context.activity.text
  const replyText = `Echo: ${text}`
  await context.sendActivity(MessageFactory.text(replyText, replyText))
  if (text?.includes('version')) {
    await context.sendActivity(MessageFactory.text('Running on version ' + pjson.version, 'Running on version ' + pjson.version))
  }
})

```

## Samples/complex/copilotstudio-skill/nodejs/src/index.ts

```typescript
import express, { json } from 'express'

import { CloudAdapter, loadAuthConfigFromEnv, authorizeJWT } from '@microsoft/agents-hosting'
import pjson from '@microsoft/agents-hosting/package.json'

import { skillAgent } from './agent.js'

const config = loadAuthConfigFromEnv()
const adapter = new CloudAdapter(config)

const server = express()
server.use(express.static('public'))
server.use(authorizeJWT(config))

server.use(json())
server.post('/api/messages',
  async (req, res) => {
    await adapter.process(req, res, (context) => skillAgent.run(context))
  }
)

const port = process.env.PORT || 3978

server.listen(port, () => {
  console.log(`\n agent skill, running on sdk version ${pjson.version} lisenting on ${port} for clientId ${process.env.clientId}`)
}).on('error', (err) => {
  console.error(err)
  process.exit(1)
})

```

## Samples/complex/copilotstudio-skill/nodejs/public/privacy.html

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Privacy</title>
</head>
<body>
    <h1>Privacy Doc</h1>

    <p>Privacy is important to us. We do not collect any personal information from you. We do not use cookies. We do not track you. We do not store any information about you.</p>
</body>
</html>
```

## Samples/complex/copilotstudio-skill/nodejs/public/manifest.json

```json
{
    "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json",
    "$id": "EchoSkill",
    "name": "Echo Skill",
    "version": "1.0",
    "description": "This is a sample echo skill",
    "publisherName": "Microsoft",
    "privacyUrl": "https://3t2bm35h-3978.usw2.devtunnels.ms/privacy.html",
    "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
    "license": "",
    "iconUrl": "https://3t2bm35h-3978.usw2.devtunnels.ms/icon.png",
    "tags": [
      "sample",
      "skill"
    ],
    "endpoints": [
      {
        "name": "default",
        "protocol": "BotFrameworkV3",
        "description": "Default endpoint for the bot",
        "endpointUrl": "https://3t2bm35h-3978.usw2.devtunnels.ms/api/messages",
        "msAppId": "4372c42c-5ab4-462c-9d0d-53b80190a2ed"
      }
    ],
    "activities": {
      "message": {
        "type": "message",
        "description": "Echo messages from user"
      }
    }
  }

```

## Samples/complex/copilotstudio-skill/nodejs/public/manifest.template.json

```json
{
    "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json",
    "$id": "EchoSkill",
    "name": "Echo Skill",
    "version": "1.0",
    "description": "This is a sample echo skill",
    "publisherName": "Microsoft",
    "privacyUrl": "{baseUrl}/privacy.html",
    "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
    "license": "",
    "iconUrl": "{baseUrl}/icon.png",
    "tags": [
      "sample",
      "skill"
    ],
    "endpoints": [
      {
        "name": "default",
        "protocol": "BotFrameworkV3",
        "description": "Default endpoint for the bot",
        "endpointUrl": "{baseUrl}/api/messages",
        "msAppId": "{clientId}"
      }
    ],
    "activities": {
      "message": {
        "type": "message",
        "description": "Echo messages from user"
      }
    }
  }
```

## Samples/basic/cards/nodejs/.npmrc

```
package-lock=false
```

## Samples/basic/cards/nodejs/tsconfig.json

```json
{
  "compilerOptions": {
      "incremental": true,
      "lib": ["ES2021"],
      "target": "es2019",
      "module": "commonjs",
      "declaration": true,
      "sourceMap": true,
      "composite": true,
      "strict": true,
      "moduleResolution": "node",
      "esModuleInterop": true,
      "skipLibCheck": true,
      "forceConsistentCasingInFileNames": true,
      "resolveJsonModule": true,
      "rootDir": "src",
      "outDir": "dist",
      "tsBuildInfoFile": "dist/.tsbuildinfo"
  },
  "include": [
    "src",
    "src/resources/*.json"
  ]
}

```

## Samples/basic/cards/nodejs/package.json

```json
{
  "name": "node-cards-agent",
  "version": "1.0.0",
  "private": true,
  "description": "Agents cards agent sample",
  "author": "Microsoft",
  "license": "MIT",
  "main": "./lib/index.js",
  "scripts": {
    "prebuild": "npm install",
    "build": "tsc --build",
    "prestart": "npm run build",
    "start": "node --env-file .env ./dist/index.js",
    "test-tool": "teamsapptester start",
    "test": "npm-run-all -p -r start test-tool"
  },
  "dependencies": {
    "@microsoft/agents-hosting": "^0.4.0",
    "express": "^5.1.0"
  },
  "devDependencies": {
    "@microsoft/teams-app-test-tool": "^0.2.10",
    "@types/node": "^22.14.0",
    "npm-run-all": "^4.1.5",
    "typescript": "^5.8.3",
    "esbuild": "^0.25.2"
  },
  "keywords": []
}

```

## Samples/basic/cards/nodejs/env.TEMPLATE

```
# rename to .env
tenantId=
clientId=
clientSecret=

```

## Samples/basic/cards/nodejs/package-lock.json

```json
{
  "name": "cards-bot",
  "version": "1.0.0",
  "lockfileVersion": 3,
  "requires": true,
  "packages": {
    "": {
      "name": "cards-bot",
      "version": "1.0.0",
      "license": "MIT",
      "dependencies": {
        "@microsoft/agents-bot-hosting": "^0.1.27",
        "express": "^5.0.1"
      },
      "devDependencies": {
        "@microsoft/teams-app-test-tool": "^0.2.6",
        "@types/node": "^22.13.4",
        "npm-run-all": "^4.1.5",
        "typescript": "^5.7.2"
      }
    },
    "node_modules/@azure/msal-common": {
      "version": "15.2.0",
      "resolved": "https://registry.npmjs.org/@azure/msal-common/-/msal-common-15.2.0.tgz",
      "integrity": "sha512-HiYfGAKthisUYqHG1nImCf/uzcyS31wng3o+CycWLIM9chnYJ9Lk6jZ30Y6YiYYpTQ9+z/FGUpiKKekd3Arc0A==",
      "license": "MIT",
      "engines": {
        "node": ">=0.8.0"
      }
    },
    "node_modules/@azure/msal-node": {
      "version": "3.2.3",
      "resolved": "https://registry.npmjs.org/@azure/msal-node/-/msal-node-3.2.3.tgz",
      "integrity": "sha512-0eaPqBIWEAizeYiXdeHb09Iq0tvHJ17ztvNEaLdr/KcJJhJxbpkkEQf09DB+vKlFE0tzYi7j4rYLTXtES/InEQ==",
      "license": "MIT",
      "dependencies": {
        "@azure/msal-common": "15.2.0",
        "jsonwebtoken": "^9.0.0",
        "uuid": "^8.3.0"
      },
      "engines": {
        "node": ">=16"
      }
    },
    "node_modules/@microsoft/agents-bot-activity": {
      "version": "0.1.27",
      "resolved": "https://registry.npmjs.org/@microsoft/agents-bot-activity/-/agents-bot-activity-0.1.27.tgz",
      "integrity": "sha512-oP0FmE1ocP0nnIeoyURUlM5U8tKkZtryn8b2QUq7WpwAZVUtltECRgasSZDQdUfABIiloTQu1fc+lRx6Qp3h4A==",
      "license": "MIT",
      "dependencies": {
        "uuid": "^11.1.0",
        "zod": "^3.24.2"
      },
      "engines": {
        "node": ">=18.0.0"
      }
    },
    "node_modules/@microsoft/agents-bot-activity/node_modules/uuid": {
      "version": "11.1.0",
      "resolved": "https://registry.npmjs.org/uuid/-/uuid-11.1.0.tgz",
      "integrity": "sha512-0/A9rDy9P7cJ+8w1c9WD9V//9Wj15Ce2MPz8Ri6032usz+NfePxx5AcN3bN+r6ZL6jEo066/yNYB3tn4pQEx+A==",
      "funding": [
        "https://github.com/sponsors/broofa",
        "https://github.com/sponsors/ctavan"
      ],
      "license": "MIT",
      "bin": {
        "uuid": "dist/esm/bin/uuid"
      }
    },
    "node_modules/@microsoft/agents-bot-hosting": {
      "version": "0.1.27",
      "resolved": "https://registry.npmjs.org/@microsoft/agents-bot-hosting/-/agents-bot-hosting-0.1.27.tgz",
      "integrity": "sha512-M5Mrz9+VV+9eLBijHcBL2zdZA2dQ0FqcEx5xZz4mdZHtSFcU08rDHfQoJ4dSg4ptk3Jj30HmQGN64wQ15qpDZw==",
      "license": "MIT",
      "dependencies": {
        "@azure/msal-node": "^3.2.3",
        "@microsoft/agents-bot-activity": "0.1.27",
        "axios": "^1.8.1",
        "debug": "^4.3.7",
        "jsonwebtoken": "^9.0.2",
        "jwks-rsa": "^3.1.0"
      },
      "engines": {
        "node": ">=18.0.0"
      }
    },
    "node_modules/@microsoft/teams-app-test-tool": {
      "version": "0.2.6",
      "resolved": "https://registry.npmjs.org/@microsoft/teams-app-test-tool/-/teams-app-test-tool-0.2.6.tgz",
      "integrity": "sha512-HJxdaPWeq7O2HE/fytu3QUurtGi8KFkHYvpbZsa0DMjDDGwtOH5KSHVzQOUieBBLY9arUi10ROtLyjFqa65G7g==",
      "dev": true,
      "license": "SEE LICENSE IN LICENSE",
      "bin": {
        "teamsapptester": "cli.js"
      }
    },
    "node_modules/@types/body-parser": {
      "version": "1.19.5",
      "resolved": "https://registry.npmjs.org/@types/body-parser/-/body-parser-1.19.5.tgz",
      "integrity": "sha512-fB3Zu92ucau0iQ0JMCFQE7b/dv8Ot07NI3KaZIkIUNXq82k4eBAqUaneXfleGY9JWskeS9y+u0nXMyspcuQrCg==",
      "license": "MIT",
      "dependencies": {
        "@types/connect": "*",
        "@types/node": "*"
      }
    },
    "node_modules/@types/connect": {
      "version": "3.4.38",
      "resolved": "https://registry.npmjs.org/@types/connect/-/connect-3.4.38.tgz",
      "integrity": "sha512-K6uROf1LD88uDQqJCktA4yzL1YYAK6NgfsI0v/mTgyPKWsX1CnJ0XPSDhViejru1GcRkLWb8RlzFYJRqGUbaug==",
      "license": "MIT",
      "dependencies": {
        "@types/node": "*"
      }
    },
    "node_modules/@types/express": {
      "version": "4.17.21",
      "resolved": "https://registry.npmjs.org/@types/express/-/express-4.17.21.tgz",
      "integrity": "sha512-ejlPM315qwLpaQlQDTjPdsUFSc6ZsP4AN6AlWnogPjQ7CVi7PYF3YVz+CY3jE2pwYf7E/7HlDAN0rV2GxTG0HQ==",
      "license": "MIT",
      "dependencies": {
        "@types/body-parser": "*",
        "@types/express-serve-static-core": "^4.17.33",
        "@types/qs": "*",
        "@types/serve-static": "*"
      }
    },
    "node_modules/@types/express-serve-static-core": {
      "version": "4.19.6",
      "resolved": "https://registry.npmjs.org/@types/express-serve-static-core/-/express-serve-static-core-4.19.6.tgz",
      "integrity": "sha512-N4LZ2xG7DatVqhCZzOGb1Yi5lMbXSZcmdLDe9EzSndPV2HpWYWzRbaerl2n27irrm94EPpprqa8KpskPT085+A==",
      "license": "MIT",
      "dependencies": {
        "@types/node": "*",
        "@types/qs": "*",
        "@types/range-parser": "*",
        "@types/send": "*"
      }
    },
    "node_modules/@types/http-errors": {
      "version": "2.0.4",
      "resolved": "https://registry.npmjs.org/@types/http-errors/-/http-errors-2.0.4.tgz",
      "integrity": "sha512-D0CFMMtydbJAegzOyHjtiKPLlvnm3iTZyZRSZoLq2mRhDdmLfIWOCYPfQJ4cu2erKghU++QvjcUjp/5h7hESpA==",
      "license": "MIT"
    },
    "node_modules/@types/jsonwebtoken": {
      "version": "9.0.9",
      "resolved": "https://registry.npmjs.org/@types/jsonwebtoken/-/jsonwebtoken-9.0.9.tgz",
      "integrity": "sha512-uoe+GxEuHbvy12OUQct2X9JenKM3qAscquYymuQN4fMWG9DBQtykrQEFcAbVACF7qaLw9BePSodUL0kquqBJpQ==",
      "license": "MIT",
      "dependencies": {
        "@types/ms": "*",
        "@types/node": "*"
      }
    },
    "node_modules/@types/mime": {
      "version": "1.3.5",
      "resolved": "https://registry.npmjs.org/@types/mime/-/mime-1.3.5.tgz",
      "integrity": "sha512-/pyBZWSLD2n0dcHE3hq8s8ZvcETHtEuF+3E7XVt0Ig2nvsVQXdghHVcEkIWjy9A0wKfTn97a/PSDYohKIlnP/w==",
      "license": "MIT"
    },
    "node_modules/@types/ms": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/@types/ms/-/ms-2.1.0.tgz",
      "integrity": "sha512-GsCCIZDE/p3i96vtEqx+7dBUGXrc7zeSK3wwPHIaRThS+9OhWIXRqzs4d6k1SVU8g91DrNRWxWUGhp5KXQb2VA==",
      "license": "MIT"
    },
    "node_modules/@types/node": {
      "version": "22.13.9",
      "resolved": "https://registry.npmjs.org/@types/node/-/node-22.13.9.tgz",
      "integrity": "sha512-acBjXdRJ3A6Pb3tqnw9HZmyR3Fiol3aGxRCK1x3d+6CDAMjl7I649wpSd+yNURCjbOUGu9tqtLKnTGxmK6CyGw==",
      "license": "MIT",
      "dependencies": {
        "undici-types": "~6.20.0"
      }
    },
    "node_modules/@types/qs": {
      "version": "6.9.18",
      "resolved": "https://registry.npmjs.org/@types/qs/-/qs-6.9.18.tgz",
      "integrity": "sha512-kK7dgTYDyGqS+e2Q4aK9X3D7q234CIZ1Bv0q/7Z5IwRDoADNU81xXJK/YVyLbLTZCoIwUoDoffFeF+p/eIklAA==",
      "license": "MIT"
    },
    "node_modules/@types/range-parser": {
      "version": "1.2.7",
      "resolved": "https://registry.npmjs.org/@types/range-parser/-/range-parser-1.2.7.tgz",
      "integrity": "sha512-hKormJbkJqzQGhziax5PItDUTMAM9uE2XXQmM37dyd4hVM+5aVl7oVxMVUiVQn2oCQFN/LKCZdvSM0pFRqbSmQ==",
      "license": "MIT"
    },
    "node_modules/@types/send": {
      "version": "0.17.4",
      "resolved": "https://registry.npmjs.org/@types/send/-/send-0.17.4.tgz",
      "integrity": "sha512-x2EM6TJOybec7c52BX0ZspPodMsQUd5L6PRwOunVyVUhXiBSKf3AezDL8Dgvgt5o0UfKNfuA0eMLr2wLT4AiBA==",
      "license": "MIT",
      "dependencies": {
        "@types/mime": "^1",
        "@types/node": "*"
      }
    },
    "node_modules/@types/serve-static": {
      "version": "1.15.7",
      "resolved": "https://registry.npmjs.org/@types/serve-static/-/serve-static-1.15.7.tgz",
      "integrity": "sha512-W8Ym+h8nhuRwaKPaDw34QUkwsGi6Rc4yYqvKFo5rm2FUEhCFbzVWrxXUxuKK8TASjWsysJY0nsmNCGhCOIsrOw==",
      "license": "MIT",
      "dependencies": {
        "@types/http-errors": "*",
        "@types/node": "*",
        "@types/send": "*"
      }
    },
    "node_modules/accepts": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/accepts/-/accepts-2.0.0.tgz",
      "integrity": "sha512-5cvg6CtKwfgdmVqY1WIiXKc3Q1bkRqGLi+2W/6ao+6Y7gu/RCwRuAhGEzh5B4KlszSuTLgZYuqFqo5bImjNKng==",
      "license": "MIT",
      "dependencies": {
        "mime-types": "^3.0.0",
        "negotiator": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/ansi-styles": {
      "version": "3.2.1",
      "resolved": "https://registry.npmjs.org/ansi-styles/-/ansi-styles-3.2.1.tgz",
      "integrity": "sha512-VT0ZI6kZRdTh8YyJw3SMbYm/u+NqfsAxEpWO0Pf9sq8/e94WxxOpPKx9FR1FlyCtOVDNOQ+8ntlqFxiRc+r5qA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "color-convert": "^1.9.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/array-buffer-byte-length": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/array-buffer-byte-length/-/array-buffer-byte-length-1.0.2.tgz",
      "integrity": "sha512-LHE+8BuR7RYGDKvnrmcuSq3tDcKv9OFEXQt/HpbZhY7V6h0zlUXutnAD82GiFx9rdieCMjkvtcsPqBwgUl1Iiw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "is-array-buffer": "^3.0.5"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/arraybuffer.prototype.slice": {
      "version": "1.0.4",
      "resolved": "https://registry.npmjs.org/arraybuffer.prototype.slice/-/arraybuffer.prototype.slice-1.0.4.tgz",
      "integrity": "sha512-BNoCY6SXXPQ7gF2opIP4GBE+Xw7U+pHMYKuzjgCN3GwiaIR09UUeKfheyIry77QtrCBlC0KK0q5/TER/tYh3PQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "array-buffer-byte-length": "^1.0.1",
        "call-bind": "^1.0.8",
        "define-properties": "^1.2.1",
        "es-abstract": "^1.23.5",
        "es-errors": "^1.3.0",
        "get-intrinsic": "^1.2.6",
        "is-array-buffer": "^3.0.4"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/async-function": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/async-function/-/async-function-1.0.0.tgz",
      "integrity": "sha512-hsU18Ae8CDTR6Kgu9DYf0EbCr/a5iGL0rytQDobUcdpYOKokk8LEjVphnXkDkgpi0wYVsqrXuP0bZxJaTqdgoA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/asynckit": {
      "version": "0.4.0",
      "resolved": "https://registry.npmjs.org/asynckit/-/asynckit-0.4.0.tgz",
      "integrity": "sha512-Oei9OH4tRh0YqU3GxhX79dM/mwVgvbZJaSNaRk+bshkj0S5cfHcgYakreBjrHwatXKbz+IoIdYLxrKim2MjW0Q==",
      "license": "MIT"
    },
    "node_modules/available-typed-arrays": {
      "version": "1.0.7",
      "resolved": "https://registry.npmjs.org/available-typed-arrays/-/available-typed-arrays-1.0.7.tgz",
      "integrity": "sha512-wvUjBtSGN7+7SjNpq/9M2Tg350UZD3q62IFZLbRAR1bSMlCo1ZaeW+BJ+D090e4hIIZLBcTDWe4Mh4jvUDajzQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "possible-typed-array-names": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/axios": {
      "version": "1.8.1",
      "resolved": "https://registry.npmjs.org/axios/-/axios-1.8.1.tgz",
      "integrity": "sha512-NN+fvwH/kV01dYUQ3PTOZns4LWtWhOFCAhQ/pHb88WQ1hNe5V/dvFwc4VJcDL11LT9xSX0QtsR8sWUuyOuOq7g==",
      "license": "MIT",
      "dependencies": {
        "follow-redirects": "^1.15.6",
        "form-data": "^4.0.0",
        "proxy-from-env": "^1.1.0"
      }
    },
    "node_modules/balanced-match": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/balanced-match/-/balanced-match-1.0.2.tgz",
      "integrity": "sha512-3oSeUO0TMV67hN1AmbXsK4yaqU7tjiHlbxRDZOpH0KW9+CeX4bRAaX0Anxt0tx2MrpRpWwQaPwIlISEJhYU5Pw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/body-parser": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/body-parser/-/body-parser-2.1.0.tgz",
      "integrity": "sha512-/hPxh61E+ll0Ujp24Ilm64cykicul1ypfwjVttduAiEdtnJFvLePSrIPk+HMImtNv5270wOGCb1Tns2rybMkoQ==",
      "license": "MIT",
      "dependencies": {
        "bytes": "^3.1.2",
        "content-type": "^1.0.5",
        "debug": "^4.4.0",
        "http-errors": "^2.0.0",
        "iconv-lite": "^0.5.2",
        "on-finished": "^2.4.1",
        "qs": "^6.14.0",
        "raw-body": "^3.0.0",
        "type-is": "^2.0.0"
      },
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/body-parser/node_modules/qs": {
      "version": "6.14.0",
      "resolved": "https://registry.npmjs.org/qs/-/qs-6.14.0.tgz",
      "integrity": "sha512-YWWTjgABSKcvs/nWBi9PycY/JiPJqOD4JA6o9Sej2AtvSGarXxKC3OQSk4pAarbdQlKAh5D4FCQkJNkW+GAn3w==",
      "license": "BSD-3-Clause",
      "dependencies": {
        "side-channel": "^1.1.0"
      },
      "engines": {
        "node": ">=0.6"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/brace-expansion": {
      "version": "1.1.11",
      "resolved": "https://registry.npmjs.org/brace-expansion/-/brace-expansion-1.1.11.tgz",
      "integrity": "sha512-iCuPHDFgrHX7H2vEI/5xpz07zSHB00TpugqhmYtVmMO6518mCuRMoOYFldEBl0g187ufozdaHgWKcYFb61qGiA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "balanced-match": "^1.0.0",
        "concat-map": "0.0.1"
      }
    },
    "node_modules/buffer-equal-constant-time": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/buffer-equal-constant-time/-/buffer-equal-constant-time-1.0.1.tgz",
      "integrity": "sha512-zRpUiDwd/xk6ADqPMATG8vc9VPrkck7T07OIx0gnjmJAnHnTVXNQG3vfvWNuiZIkwu9KrKdA1iJKfsfTVxE6NA==",
      "license": "BSD-3-Clause"
    },
    "node_modules/bytes": {
      "version": "3.1.2",
      "resolved": "https://registry.npmjs.org/bytes/-/bytes-3.1.2.tgz",
      "integrity": "sha512-/Nf7TyzTx6S3yRJObOAV7956r8cr2+Oj8AC5dt8wSP3BQAoeX58NoHyCU8P8zGkNXStjTSi6fzO6F0pBdcYbEg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/call-bind": {
      "version": "1.0.8",
      "resolved": "https://registry.npmjs.org/call-bind/-/call-bind-1.0.8.tgz",
      "integrity": "sha512-oKlSFMcMwpUg2ednkhQ454wfWiU/ul3CkJe/PEHcTKuiX6RpbehUiFMXu13HalGZxfUwCQzZG747YXBn1im9ww==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind-apply-helpers": "^1.0.0",
        "es-define-property": "^1.0.0",
        "get-intrinsic": "^1.2.4",
        "set-function-length": "^1.2.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/call-bind-apply-helpers": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/call-bind-apply-helpers/-/call-bind-apply-helpers-1.0.2.tgz",
      "integrity": "sha512-Sp1ablJ0ivDkSzjcaJdxEunN5/XvksFJ2sMBFfq6x0ryhQV/2b/KwFe21cMpmHtPOSij8K99/wSfoEuTObmuMQ==",
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "function-bind": "^1.1.2"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/call-bound": {
      "version": "1.0.4",
      "resolved": "https://registry.npmjs.org/call-bound/-/call-bound-1.0.4.tgz",
      "integrity": "sha512-+ys997U96po4Kx/ABpBCqhA9EuxJaQWDQg7295H4hBphv3IZg0boBKuwYpt4YXp6MZ5AmZQnU/tyMTlRpaSejg==",
      "license": "MIT",
      "dependencies": {
        "call-bind-apply-helpers": "^1.0.2",
        "get-intrinsic": "^1.3.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/chalk": {
      "version": "2.4.2",
      "resolved": "https://registry.npmjs.org/chalk/-/chalk-2.4.2.tgz",
      "integrity": "sha512-Mti+f9lpJNcwF4tWV8/OrTTtF1gZi+f8FqlyAdouralcFWFQWF2+NgCHShjkCb+IFBLq9buZwE1xckQU4peSuQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "ansi-styles": "^3.2.1",
        "escape-string-regexp": "^1.0.5",
        "supports-color": "^5.3.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/color-convert": {
      "version": "1.9.3",
      "resolved": "https://registry.npmjs.org/color-convert/-/color-convert-1.9.3.tgz",
      "integrity": "sha512-QfAUtd+vFdAtFQcC8CCyYt1fYWxSqAiK2cSD6zDB8N3cpsEBAvRxp9zOGg6G/SHHJYAT88/az/IuDGALsNVbGg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "color-name": "1.1.3"
      }
    },
    "node_modules/color-name": {
      "version": "1.1.3",
      "resolved": "https://registry.npmjs.org/color-name/-/color-name-1.1.3.tgz",
      "integrity": "sha512-72fSenhMw2HZMTVHeCA9KCmpEIbzWiQsjN+BHcBbS9vr1mtt+vJjPdksIBNUmKAW8TFUDPJK5SUU3QhE9NEXDw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/combined-stream": {
      "version": "1.0.8",
      "resolved": "https://registry.npmjs.org/combined-stream/-/combined-stream-1.0.8.tgz",
      "integrity": "sha512-FQN4MRfuJeHf7cBbBMJFXhKSDq+2kAArBlmRBvcvFE5BB1HZKXtSFASDhdlz9zOYwxh8lDdnvmMOe/+5cdoEdg==",
      "license": "MIT",
      "dependencies": {
        "delayed-stream": "~1.0.0"
      },
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/concat-map": {
      "version": "0.0.1",
      "resolved": "https://registry.npmjs.org/concat-map/-/concat-map-0.0.1.tgz",
      "integrity": "sha512-/Srv4dswyQNBfohGpz9o6Yb3Gz3SrUDqBH5rTuhGR7ahtlbYKnVxw2bCFMRljaA7EXHaXZ8wsHdodFvbkhKmqg==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/content-disposition": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/content-disposition/-/content-disposition-1.0.0.tgz",
      "integrity": "sha512-Au9nRL8VNUut/XSzbQA38+M78dzP4D+eqg3gfJHMIHHYa3bg067xj1KxMUWj+VULbiZMowKngFFbKczUrNJ1mg==",
      "license": "MIT",
      "dependencies": {
        "safe-buffer": "5.2.1"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/content-type": {
      "version": "1.0.5",
      "resolved": "https://registry.npmjs.org/content-type/-/content-type-1.0.5.tgz",
      "integrity": "sha512-nTjqfcBFEipKdXCv4YDQWCfmcLZKm81ldF0pAopTvyrFGVbcR6P/VAAd5G7N+0tTr8QqiU0tFadD6FK4NtJwOA==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/cookie": {
      "version": "0.7.1",
      "resolved": "https://registry.npmjs.org/cookie/-/cookie-0.7.1.tgz",
      "integrity": "sha512-6DnInpx7SJ2AK3+CTUE/ZM0vWTUboZCegxhC2xiIydHR9jNuTAASBrfEpHhiGOZw/nX51bHt6YQl8jsGo4y/0w==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/cookie-signature": {
      "version": "1.2.2",
      "resolved": "https://registry.npmjs.org/cookie-signature/-/cookie-signature-1.2.2.tgz",
      "integrity": "sha512-D76uU73ulSXrD1UXF4KE2TMxVVwhsnCgfAyTg9k8P6KGZjlXKrOLe4dJQKI3Bxi5wjesZoFXJWElNWBjPZMbhg==",
      "license": "MIT",
      "engines": {
        "node": ">=6.6.0"
      }
    },
    "node_modules/cross-spawn": {
      "version": "6.0.6",
      "resolved": "https://registry.npmjs.org/cross-spawn/-/cross-spawn-6.0.6.tgz",
      "integrity": "sha512-VqCUuhcd1iB+dsv8gxPttb5iZh/D0iubSP21g36KXdEuf6I5JiioesUVjpCdHV9MZRUfVFlvwtIUyPfxo5trtw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "nice-try": "^1.0.4",
        "path-key": "^2.0.1",
        "semver": "^5.5.0",
        "shebang-command": "^1.2.0",
        "which": "^1.2.9"
      },
      "engines": {
        "node": ">=4.8"
      }
    },
    "node_modules/cross-spawn/node_modules/semver": {
      "version": "5.7.2",
      "resolved": "https://registry.npmjs.org/semver/-/semver-5.7.2.tgz",
      "integrity": "sha512-cBznnQ9KjJqU67B52RMC65CMarK2600WFnbkcaiwWq3xy/5haFJlshgnpjovMVJ+Hff49d8GEn0b87C5pDQ10g==",
      "dev": true,
      "license": "ISC",
      "bin": {
        "semver": "bin/semver"
      }
    },
    "node_modules/data-view-buffer": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/data-view-buffer/-/data-view-buffer-1.0.2.tgz",
      "integrity": "sha512-EmKO5V3OLXh1rtK2wgXRansaK1/mtVdTUEiEI0W8RkvgT05kfxaH29PliLnpLP73yYO6142Q72QNa8Wx/A5CqQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "es-errors": "^1.3.0",
        "is-data-view": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/data-view-byte-length": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/data-view-byte-length/-/data-view-byte-length-1.0.2.tgz",
      "integrity": "sha512-tuhGbE6CfTM9+5ANGf+oQb72Ky/0+s3xKUpHvShfiz2RxMFgFPjsXuRLBVMtvMs15awe45SRb83D6wH4ew6wlQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "es-errors": "^1.3.0",
        "is-data-view": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/inspect-js"
      }
    },
    "node_modules/data-view-byte-offset": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/data-view-byte-offset/-/data-view-byte-offset-1.0.1.tgz",
      "integrity": "sha512-BS8PfmtDGnrgYdOonGZQdLZslWIeCGFP9tpan0hi1Co2Zr2NKADsvGYA8XxuG/4UWgJ6Cjtv+YJnB6MM69QGlQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "es-errors": "^1.3.0",
        "is-data-view": "^1.0.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/debug": {
      "version": "4.4.0",
      "resolved": "https://registry.npmjs.org/debug/-/debug-4.4.0.tgz",
      "integrity": "sha512-6WTZ/IxCY/T6BALoZHaE4ctp9xm+Z5kY/pzYaCHRFeyVhojxlrm+46y68HA6hr0TcwEssoxNiDEUJQjfPZ/RYA==",
      "license": "MIT",
      "dependencies": {
        "ms": "^2.1.3"
      },
      "engines": {
        "node": ">=6.0"
      },
      "peerDependenciesMeta": {
        "supports-color": {
          "optional": true
        }
      }
    },
    "node_modules/define-data-property": {
      "version": "1.1.4",
      "resolved": "https://registry.npmjs.org/define-data-property/-/define-data-property-1.1.4.tgz",
      "integrity": "sha512-rBMvIzlpA8v6E+SJZoo++HAYqsLrkg7MSfIinMPFhmkorw7X+dOXVJQs+QT69zGkzMyfDnIMN2Wid1+NbL3T+A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "es-define-property": "^1.0.0",
        "es-errors": "^1.3.0",
        "gopd": "^1.0.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/define-properties": {
      "version": "1.2.1",
      "resolved": "https://registry.npmjs.org/define-properties/-/define-properties-1.2.1.tgz",
      "integrity": "sha512-8QmQKqEASLd5nx0U1B1okLElbUuuttJ/AnYmRXbbbGDWh6uS208EjD4Xqq/I9wK7u0v6O08XhTWnt5XtEbR6Dg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "define-data-property": "^1.0.1",
        "has-property-descriptors": "^1.0.0",
        "object-keys": "^1.1.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/delayed-stream": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/delayed-stream/-/delayed-stream-1.0.0.tgz",
      "integrity": "sha512-ZySD7Nf91aLB0RxL4KGrKHBXl7Eds1DAmEdcoVawXnLD7SDhpNgtuII2aAkg7a7QS41jxPSZ17p4VdGnMHk3MQ==",
      "license": "MIT",
      "engines": {
        "node": ">=0.4.0"
      }
    },
    "node_modules/depd": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/depd/-/depd-2.0.0.tgz",
      "integrity": "sha512-g7nH6P6dyDioJogAAGprGpCtVImJhpPk/roCzdb3fIh61/s/nPsfR6onyMwkCAR/OlC3yBC0lESvUoQEAssIrw==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/destroy": {
      "version": "1.2.0",
      "resolved": "https://registry.npmjs.org/destroy/-/destroy-1.2.0.tgz",
      "integrity": "sha512-2sJGJTaXIIaR1w4iJSNoN0hnMY7Gpc/n8D4qSCJw8QqFWXf7cuAgnEHxBpweaVcPevC2l3KpjYCx3NypQQgaJg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8",
        "npm": "1.2.8000 || >= 1.4.16"
      }
    },
    "node_modules/dunder-proto": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/dunder-proto/-/dunder-proto-1.0.1.tgz",
      "integrity": "sha512-KIN/nDJBQRcXw0MLVhZE9iQHmG68qAVIBg9CqmUYjmQIhgij9U5MFvrqkUL5FbtyyzZuOeOt0zdeRe4UY7ct+A==",
      "license": "MIT",
      "dependencies": {
        "call-bind-apply-helpers": "^1.0.1",
        "es-errors": "^1.3.0",
        "gopd": "^1.2.0"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/ecdsa-sig-formatter": {
      "version": "1.0.11",
      "resolved": "https://registry.npmjs.org/ecdsa-sig-formatter/-/ecdsa-sig-formatter-1.0.11.tgz",
      "integrity": "sha512-nagl3RYrbNv6kQkeJIpt6NJZy8twLB/2vtz6yN9Z4vRKHN4/QZJIEbqohALSgwKdnksuY3k5Addp5lg8sVoVcQ==",
      "license": "Apache-2.0",
      "dependencies": {
        "safe-buffer": "^5.0.1"
      }
    },
    "node_modules/ee-first": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/ee-first/-/ee-first-1.1.1.tgz",
      "integrity": "sha512-WMwm9LhRUo+WUaRN+vRuETqG89IgZphVSNkdFgeb6sS/E4OrDIN7t48CAewSHXc6C8lefD8KKfr5vY61brQlow==",
      "license": "MIT"
    },
    "node_modules/encodeurl": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/encodeurl/-/encodeurl-2.0.0.tgz",
      "integrity": "sha512-Q0n9HRi4m6JuGIV1eFlmvJB7ZEVxu93IrMyiMsGC0lrMJMWzRgx6WGquyfQgZVb31vhGgXnfmPNNXmxnOkRBrg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/error-ex": {
      "version": "1.3.2",
      "resolved": "https://registry.npmjs.org/error-ex/-/error-ex-1.3.2.tgz",
      "integrity": "sha512-7dFHNmqeFSEt2ZBsCriorKnn3Z2pj+fd9kmI6QoWw4//DL+icEBfc0U7qJCisqrTsKTjw4fNFy2pW9OqStD84g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-arrayish": "^0.2.1"
      }
    },
    "node_modules/es-abstract": {
      "version": "1.23.9",
      "resolved": "https://registry.npmjs.org/es-abstract/-/es-abstract-1.23.9.tgz",
      "integrity": "sha512-py07lI0wjxAC/DcfK1S6G7iANonniZwTISvdPzk9hzeH0IZIshbuuFxLIU96OyF89Yb9hiqWn8M/bY83KY5vzA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "array-buffer-byte-length": "^1.0.2",
        "arraybuffer.prototype.slice": "^1.0.4",
        "available-typed-arrays": "^1.0.7",
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.3",
        "data-view-buffer": "^1.0.2",
        "data-view-byte-length": "^1.0.2",
        "data-view-byte-offset": "^1.0.1",
        "es-define-property": "^1.0.1",
        "es-errors": "^1.3.0",
        "es-object-atoms": "^1.0.0",
        "es-set-tostringtag": "^2.1.0",
        "es-to-primitive": "^1.3.0",
        "function.prototype.name": "^1.1.8",
        "get-intrinsic": "^1.2.7",
        "get-proto": "^1.0.0",
        "get-symbol-description": "^1.1.0",
        "globalthis": "^1.0.4",
        "gopd": "^1.2.0",
        "has-property-descriptors": "^1.0.2",
        "has-proto": "^1.2.0",
        "has-symbols": "^1.1.0",
        "hasown": "^2.0.2",
        "internal-slot": "^1.1.0",
        "is-array-buffer": "^3.0.5",
        "is-callable": "^1.2.7",
        "is-data-view": "^1.0.2",
        "is-regex": "^1.2.1",
        "is-shared-array-buffer": "^1.0.4",
        "is-string": "^1.1.1",
        "is-typed-array": "^1.1.15",
        "is-weakref": "^1.1.0",
        "math-intrinsics": "^1.1.0",
        "object-inspect": "^1.13.3",
        "object-keys": "^1.1.1",
        "object.assign": "^4.1.7",
        "own-keys": "^1.0.1",
        "regexp.prototype.flags": "^1.5.3",
        "safe-array-concat": "^1.1.3",
        "safe-push-apply": "^1.0.0",
        "safe-regex-test": "^1.1.0",
        "set-proto": "^1.0.0",
        "string.prototype.trim": "^1.2.10",
        "string.prototype.trimend": "^1.0.9",
        "string.prototype.trimstart": "^1.0.8",
        "typed-array-buffer": "^1.0.3",
        "typed-array-byte-length": "^1.0.3",
        "typed-array-byte-offset": "^1.0.4",
        "typed-array-length": "^1.0.7",
        "unbox-primitive": "^1.1.0",
        "which-typed-array": "^1.1.18"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/es-define-property": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/es-define-property/-/es-define-property-1.0.1.tgz",
      "integrity": "sha512-e3nRfgfUZ4rNGL232gUgX06QNyyez04KdjFrF+LTRoOXmrOgFKDg4BCdsjW8EnT69eqdYGmRpJwiPVYNrCaW3g==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/es-errors": {
      "version": "1.3.0",
      "resolved": "https://registry.npmjs.org/es-errors/-/es-errors-1.3.0.tgz",
      "integrity": "sha512-Zf5H2Kxt2xjTvbJvP2ZWLEICxA6j+hAmMzIlypy4xcBg1vKVnx89Wy0GbS+kf5cwCVFFzdCFh2XSCFNULS6csw==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/es-object-atoms": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/es-object-atoms/-/es-object-atoms-1.1.1.tgz",
      "integrity": "sha512-FGgH2h8zKNim9ljj7dankFPcICIK9Cp5bm+c2gQSYePhpaG5+esrLODihIorn+Pe6FGJzWhXQotPv73jTaldXA==",
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/es-set-tostringtag": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/es-set-tostringtag/-/es-set-tostringtag-2.1.0.tgz",
      "integrity": "sha512-j6vWzfrGVfyXxge+O0x5sh6cvxAog0a/4Rdd2K36zCMV5eJ+/+tOAngRO8cODMNWbVRdVlmGZQL2YS3yR8bIUA==",
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "get-intrinsic": "^1.2.6",
        "has-tostringtag": "^1.0.2",
        "hasown": "^2.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/es-to-primitive": {
      "version": "1.3.0",
      "resolved": "https://registry.npmjs.org/es-to-primitive/-/es-to-primitive-1.3.0.tgz",
      "integrity": "sha512-w+5mJ3GuFL+NjVtJlvydShqE1eN3h3PbI7/5LAsYJP/2qtuMXjfL2LpHSRqo4b4eSF5K/DH1JXKUAHSB2UW50g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-callable": "^1.2.7",
        "is-date-object": "^1.0.5",
        "is-symbol": "^1.0.4"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/escape-html": {
      "version": "1.0.3",
      "resolved": "https://registry.npmjs.org/escape-html/-/escape-html-1.0.3.tgz",
      "integrity": "sha512-NiSupZ4OeuGwr68lGIeym/ksIZMJodUGOSCZ/FSnTxcrekbvqrgdUxlJOMpijaKZVjAJrWrGs/6Jy8OMuyj9ow==",
      "license": "MIT"
    },
    "node_modules/escape-string-regexp": {
      "version": "1.0.5",
      "resolved": "https://registry.npmjs.org/escape-string-regexp/-/escape-string-regexp-1.0.5.tgz",
      "integrity": "sha512-vbRorB5FUQWvla16U8R/qgaFIya2qGzwDrNmCZuYKrbdSUMG6I1ZCGQRefkRVhuOkIGVne7BQ35DSfo1qvJqFg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=0.8.0"
      }
    },
    "node_modules/etag": {
      "version": "1.8.1",
      "resolved": "https://registry.npmjs.org/etag/-/etag-1.8.1.tgz",
      "integrity": "sha512-aIL5Fx7mawVa300al2BnEE4iNvo1qETxLrPI/o05L7z6go7fCw1J6EQmbK4FmJ2AS7kgVF/KEZWufBfdClMcPg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/express": {
      "version": "5.0.1",
      "resolved": "https://registry.npmjs.org/express/-/express-5.0.1.tgz",
      "integrity": "sha512-ORF7g6qGnD+YtUG9yx4DFoqCShNMmUKiXuT5oWMHiOvt/4WFbHC6yCwQMTSBMno7AqntNCAzzcnnjowRkTL9eQ==",
      "license": "MIT",
      "dependencies": {
        "accepts": "^2.0.0",
        "body-parser": "^2.0.1",
        "content-disposition": "^1.0.0",
        "content-type": "~1.0.4",
        "cookie": "0.7.1",
        "cookie-signature": "^1.2.1",
        "debug": "4.3.6",
        "depd": "2.0.0",
        "encodeurl": "~2.0.0",
        "escape-html": "~1.0.3",
        "etag": "~1.8.1",
        "finalhandler": "^2.0.0",
        "fresh": "2.0.0",
        "http-errors": "2.0.0",
        "merge-descriptors": "^2.0.0",
        "methods": "~1.1.2",
        "mime-types": "^3.0.0",
        "on-finished": "2.4.1",
        "once": "1.4.0",
        "parseurl": "~1.3.3",
        "proxy-addr": "~2.0.7",
        "qs": "6.13.0",
        "range-parser": "~1.2.1",
        "router": "^2.0.0",
        "safe-buffer": "5.2.1",
        "send": "^1.1.0",
        "serve-static": "^2.1.0",
        "setprototypeof": "1.2.0",
        "statuses": "2.0.1",
        "type-is": "^2.0.0",
        "utils-merge": "1.0.1",
        "vary": "~1.1.2"
      },
      "engines": {
        "node": ">= 18"
      }
    },
    "node_modules/express/node_modules/debug": {
      "version": "4.3.6",
      "resolved": "https://registry.npmjs.org/debug/-/debug-4.3.6.tgz",
      "integrity": "sha512-O/09Bd4Z1fBrU4VzkhFqVgpPzaGbw6Sm9FEkBT1A/YBXQFGuuSxa1dN2nxgxS34JmKXqYx8CZAwEVoJFImUXIg==",
      "license": "MIT",
      "dependencies": {
        "ms": "2.1.2"
      },
      "engines": {
        "node": ">=6.0"
      },
      "peerDependenciesMeta": {
        "supports-color": {
          "optional": true
        }
      }
    },
    "node_modules/express/node_modules/ms": {
      "version": "2.1.2",
      "resolved": "https://registry.npmjs.org/ms/-/ms-2.1.2.tgz",
      "integrity": "sha512-sGkPx+VjMtmA6MX27oA4FBFELFCZZ4S4XqeGOXCv68tT+jb3vk/RyaKWP0PTKyWtmLSM0b+adUTEvbs1PEaH2w==",
      "license": "MIT"
    },
    "node_modules/finalhandler": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/finalhandler/-/finalhandler-2.1.0.tgz",
      "integrity": "sha512-/t88Ty3d5JWQbWYgaOGCCYfXRwV1+be02WqYYlL6h0lEiUAMPM8o8qKGO01YIkOHzka2up08wvgYD0mDiI+q3Q==",
      "license": "MIT",
      "dependencies": {
        "debug": "^4.4.0",
        "encodeurl": "^2.0.0",
        "escape-html": "^1.0.3",
        "on-finished": "^2.4.1",
        "parseurl": "^1.3.3",
        "statuses": "^2.0.1"
      },
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/follow-redirects": {
      "version": "1.15.9",
      "resolved": "https://registry.npmjs.org/follow-redirects/-/follow-redirects-1.15.9.tgz",
      "integrity": "sha512-gew4GsXizNgdoRyqmyfMHyAmXsZDk6mHkSxZFCzW9gwlbtOW44CDtYavM+y+72qD/Vq2l550kMF52DT8fOLJqQ==",
      "funding": [
        {
          "type": "individual",
          "url": "https://github.com/sponsors/RubenVerborgh"
        }
      ],
      "license": "MIT",
      "engines": {
        "node": ">=4.0"
      },
      "peerDependenciesMeta": {
        "debug": {
          "optional": true
        }
      }
    },
    "node_modules/for-each": {
      "version": "0.3.5",
      "resolved": "https://registry.npmjs.org/for-each/-/for-each-0.3.5.tgz",
      "integrity": "sha512-dKx12eRCVIzqCxFGplyFKJMPvLEWgmNtUrpTiJIR5u97zEhRG8ySrtboPHZXx7daLxQVrl643cTzbab2tkQjxg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-callable": "^1.2.7"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/form-data": {
      "version": "4.0.2",
      "resolved": "https://registry.npmjs.org/form-data/-/form-data-4.0.2.tgz",
      "integrity": "sha512-hGfm/slu0ZabnNt4oaRZ6uREyfCj6P4fT/n6A1rGV+Z0VdGXjfOhVUpkn6qVQONHGIFwmveGXyDs75+nr6FM8w==",
      "license": "MIT",
      "dependencies": {
        "asynckit": "^0.4.0",
        "combined-stream": "^1.0.8",
        "es-set-tostringtag": "^2.1.0",
        "mime-types": "^2.1.12"
      },
      "engines": {
        "node": ">= 6"
      }
    },
    "node_modules/form-data/node_modules/mime-db": {
      "version": "1.52.0",
      "resolved": "https://registry.npmjs.org/mime-db/-/mime-db-1.52.0.tgz",
      "integrity": "sha512-sPU4uV7dYlvtWJxwwxHD0PuihVNiE7TyAbQ5SWxDCB9mUYvOgroQOwYQQOKPJ8CIbE+1ETVlOoK1UC2nU3gYvg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/form-data/node_modules/mime-types": {
      "version": "2.1.35",
      "resolved": "https://registry.npmjs.org/mime-types/-/mime-types-2.1.35.tgz",
      "integrity": "sha512-ZDY+bPm5zTTF+YpCrAU9nK0UgICYPT0QtT1NZWFv4s++TNkcgVaT0g6+4R2uI4MjQjzysHB1zxuWL50hzaeXiw==",
      "license": "MIT",
      "dependencies": {
        "mime-db": "1.52.0"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/forwarded": {
      "version": "0.2.0",
      "resolved": "https://registry.npmjs.org/forwarded/-/forwarded-0.2.0.tgz",
      "integrity": "sha512-buRG0fpBtRHSTCOASe6hD258tEubFoRLb4ZNA6NxMVHNw2gOcwHo9wyablzMzOA5z9xA9L1KNjk/Nt6MT9aYow==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/fresh": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/fresh/-/fresh-2.0.0.tgz",
      "integrity": "sha512-Rx/WycZ60HOaqLKAi6cHRKKI7zxWbJ31MhntmtwMoaTeF7XFH9hhBp8vITaMidfljRQ6eYWCKkaTK+ykVJHP2A==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/function-bind": {
      "version": "1.1.2",
      "resolved": "https://registry.npmjs.org/function-bind/-/function-bind-1.1.2.tgz",
      "integrity": "sha512-7XHNxH7qX9xG5mIwxkhumTox/MIRNcOgDrxWsMt2pAr23WHp6MrRlN7FBSFpCpr+oVO0F744iUgR82nJMfG2SA==",
      "license": "MIT",
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/function.prototype.name": {
      "version": "1.1.8",
      "resolved": "https://registry.npmjs.org/function.prototype.name/-/function.prototype.name-1.1.8.tgz",
      "integrity": "sha512-e5iwyodOHhbMr/yNrc7fDYG4qlbIvI5gajyzPnb5TCwyhjApznQh1BMFou9b30SevY43gCJKXycoCBjMbsuW0Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.3",
        "define-properties": "^1.2.1",
        "functions-have-names": "^1.2.3",
        "hasown": "^2.0.2",
        "is-callable": "^1.2.7"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/functions-have-names": {
      "version": "1.2.3",
      "resolved": "https://registry.npmjs.org/functions-have-names/-/functions-have-names-1.2.3.tgz",
      "integrity": "sha512-xckBUXyTIqT97tq2x2AMb+g163b5JFysYk0x4qxNFwbfQkmNZoiRHb6sPzI9/QV33WeuvVYBUIiD4NzNIyqaRQ==",
      "dev": true,
      "license": "MIT",
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/get-intrinsic": {
      "version": "1.3.0",
      "resolved": "https://registry.npmjs.org/get-intrinsic/-/get-intrinsic-1.3.0.tgz",
      "integrity": "sha512-9fSjSaos/fRIVIp+xSJlE6lfwhES7LNtKaCBIamHsjr2na1BiABJPo0mOjjz8GJDURarmCPGqaiVg5mfjb98CQ==",
      "license": "MIT",
      "dependencies": {
        "call-bind-apply-helpers": "^1.0.2",
        "es-define-property": "^1.0.1",
        "es-errors": "^1.3.0",
        "es-object-atoms": "^1.1.1",
        "function-bind": "^1.1.2",
        "get-proto": "^1.0.1",
        "gopd": "^1.2.0",
        "has-symbols": "^1.1.0",
        "hasown": "^2.0.2",
        "math-intrinsics": "^1.1.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/get-proto": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/get-proto/-/get-proto-1.0.1.tgz",
      "integrity": "sha512-sTSfBjoXBp89JvIKIefqw7U2CCebsc74kiY6awiGogKtoSGbgjYE/G/+l9sF3MWFPNc9IcoOC4ODfKHfxFmp0g==",
      "license": "MIT",
      "dependencies": {
        "dunder-proto": "^1.0.1",
        "es-object-atoms": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/get-symbol-description": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/get-symbol-description/-/get-symbol-description-1.1.0.tgz",
      "integrity": "sha512-w9UMqWwJxHNOvoNzSJ2oPF5wvYcvP7jUvYzhp67yEhTi17ZDBBC1z9pTdGuzjD+EFIqLSYRweZjqfiPzQ06Ebg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "es-errors": "^1.3.0",
        "get-intrinsic": "^1.2.6"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/globalthis": {
      "version": "1.0.4",
      "resolved": "https://registry.npmjs.org/globalthis/-/globalthis-1.0.4.tgz",
      "integrity": "sha512-DpLKbNU4WylpxJykQujfCcwYWiV/Jhm50Goo0wrVILAv5jOr9d+H+UR3PhSCD2rCCEIg0uc+G+muBTwD54JhDQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "define-properties": "^1.2.1",
        "gopd": "^1.0.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/gopd": {
      "version": "1.2.0",
      "resolved": "https://registry.npmjs.org/gopd/-/gopd-1.2.0.tgz",
      "integrity": "sha512-ZUKRh6/kUFoAiTAtTYPZJ3hw9wNxx+BIBOijnlG9PnrJsCcSjs1wyyD6vJpaYtgnzDrKYRSqf3OO6Rfa93xsRg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/graceful-fs": {
      "version": "4.2.11",
      "resolved": "https://registry.npmjs.org/graceful-fs/-/graceful-fs-4.2.11.tgz",
      "integrity": "sha512-RbJ5/jmFcNNCcDV5o9eTnBLJ/HszWV0P73bc+Ff4nS/rJj+YaS6IGyiOL0VoBYX+l1Wrl3k63h/KrH+nhJ0XvQ==",
      "dev": true,
      "license": "ISC"
    },
    "node_modules/has-bigints": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/has-bigints/-/has-bigints-1.1.0.tgz",
      "integrity": "sha512-R3pbpkcIqv2Pm3dUwgjclDRVmWpTJW2DcMzcIhEXEx1oh/CEMObMm3KLmRJOdvhM7o4uQBnwr8pzRK2sJWIqfg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/has-flag": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/has-flag/-/has-flag-3.0.0.tgz",
      "integrity": "sha512-sKJf1+ceQBr4SMkvQnBDNDtf4TXpVhVGateu0t918bl30FnbE2m4vNLX+VWe/dpjlb+HugGYzW7uQXH98HPEYw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/has-property-descriptors": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/has-property-descriptors/-/has-property-descriptors-1.0.2.tgz",
      "integrity": "sha512-55JNKuIW+vq4Ke1BjOTjM2YctQIvCT7GFzHwmfZPGo5wnrgkid0YQtnAleFSqumZm4az3n2BS+erby5ipJdgrg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "es-define-property": "^1.0.0"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/has-proto": {
      "version": "1.2.0",
      "resolved": "https://registry.npmjs.org/has-proto/-/has-proto-1.2.0.tgz",
      "integrity": "sha512-KIL7eQPfHQRC8+XluaIw7BHUwwqL19bQn4hzNgdr+1wXoU0KKj6rufu47lhY7KbJR2C6T6+PfyN0Ea7wkSS+qQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "dunder-proto": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/has-symbols": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/has-symbols/-/has-symbols-1.1.0.tgz",
      "integrity": "sha512-1cDNdwJ2Jaohmb3sg4OmKaMBwuC48sYni5HUw2DvsC8LjGTLK9h+eb1X6RyuOHe4hT0ULCW68iomhjUoKUqlPQ==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/has-tostringtag": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/has-tostringtag/-/has-tostringtag-1.0.2.tgz",
      "integrity": "sha512-NqADB8VjPFLM2V0VvHUewwwsw0ZWBaIdgo+ieHtK3hasLz4qeCRjYcqfB6AQrBggRKppKF8L52/VqdVsO47Dlw==",
      "license": "MIT",
      "dependencies": {
        "has-symbols": "^1.0.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/hasown": {
      "version": "2.0.2",
      "resolved": "https://registry.npmjs.org/hasown/-/hasown-2.0.2.tgz",
      "integrity": "sha512-0hJU9SCPvmMzIBdZFqNPXWa6dqh7WdH0cII9y+CyS8rG3nL48Bclra9HmKhVVUHyPWNH5Y7xDwAB7bfgSjkUMQ==",
      "license": "MIT",
      "dependencies": {
        "function-bind": "^1.1.2"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/hosted-git-info": {
      "version": "2.8.9",
      "resolved": "https://registry.npmjs.org/hosted-git-info/-/hosted-git-info-2.8.9.tgz",
      "integrity": "sha512-mxIDAb9Lsm6DoOJ7xH+5+X4y1LU/4Hi50L9C5sIswK3JzULS4bwk1FvjdBgvYR4bzT4tuUQiC15FE2f5HbLvYw==",
      "dev": true,
      "license": "ISC"
    },
    "node_modules/http-errors": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/http-errors/-/http-errors-2.0.0.tgz",
      "integrity": "sha512-FtwrG/euBzaEjYeRqOgly7G0qviiXoJWnvEH2Z1plBdXgbyjv34pHTSb9zoeHMyDy33+DWy5Wt9Wo+TURtOYSQ==",
      "license": "MIT",
      "dependencies": {
        "depd": "2.0.0",
        "inherits": "2.0.4",
        "setprototypeof": "1.2.0",
        "statuses": "2.0.1",
        "toidentifier": "1.0.1"
      },
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/iconv-lite": {
      "version": "0.5.2",
      "resolved": "https://registry.npmjs.org/iconv-lite/-/iconv-lite-0.5.2.tgz",
      "integrity": "sha512-kERHXvpSaB4aU3eANwidg79K8FlrN77m8G9V+0vOR3HYaRifrlwMEpT7ZBJqLSEIHnEgJTHcWK82wwLwwKwtag==",
      "license": "MIT",
      "dependencies": {
        "safer-buffer": ">= 2.1.2 < 3"
      },
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/inherits": {
      "version": "2.0.4",
      "resolved": "https://registry.npmjs.org/inherits/-/inherits-2.0.4.tgz",
      "integrity": "sha512-k/vGaX4/Yla3WzyMCvTQOXYeIHvqOKtnqBduzTHpzpQZzAskKMhZ2K+EnBiSM9zGSoIFeMpXKxa4dYeZIQqewQ==",
      "license": "ISC"
    },
    "node_modules/internal-slot": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/internal-slot/-/internal-slot-1.1.0.tgz",
      "integrity": "sha512-4gd7VpWNQNB4UKKCFFVcp1AVv+FMOgs9NKzjHKusc8jTMhd5eL1NqQqOpE0KzMds804/yHlglp3uxgluOqAPLw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "hasown": "^2.0.2",
        "side-channel": "^1.1.0"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/ipaddr.js": {
      "version": "1.9.1",
      "resolved": "https://registry.npmjs.org/ipaddr.js/-/ipaddr.js-1.9.1.tgz",
      "integrity": "sha512-0KI/607xoxSToH7GjN1FfSbLoU0+btTicjsQSWQlh/hZykN8KpmMf7uYwPW3R+akZ6R/w18ZlXSHBYXiYUPO3g==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.10"
      }
    },
    "node_modules/is-array-buffer": {
      "version": "3.0.5",
      "resolved": "https://registry.npmjs.org/is-array-buffer/-/is-array-buffer-3.0.5.tgz",
      "integrity": "sha512-DDfANUiiG2wC1qawP66qlTugJeL5HyzMpfr8lLK+jMQirGzNod0B12cFB/9q838Ru27sBwfw78/rdoU7RERz6A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.3",
        "get-intrinsic": "^1.2.6"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-arrayish": {
      "version": "0.2.1",
      "resolved": "https://registry.npmjs.org/is-arrayish/-/is-arrayish-0.2.1.tgz",
      "integrity": "sha512-zz06S8t0ozoDXMG+ube26zeCTNXcKIPJZJi8hBrF4idCLms4CG9QtK7qBl1boi5ODzFpjswb5JPmHCbMpjaYzg==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/is-async-function": {
      "version": "2.1.1",
      "resolved": "https://registry.npmjs.org/is-async-function/-/is-async-function-2.1.1.tgz",
      "integrity": "sha512-9dgM/cZBnNvjzaMYHVoxxfPj2QXt22Ev7SuuPrs+xav0ukGB0S6d4ydZdEiM48kLx5kDV+QBPrpVnFyefL8kkQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "async-function": "^1.0.0",
        "call-bound": "^1.0.3",
        "get-proto": "^1.0.1",
        "has-tostringtag": "^1.0.2",
        "safe-regex-test": "^1.1.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-bigint": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/is-bigint/-/is-bigint-1.1.0.tgz",
      "integrity": "sha512-n4ZT37wG78iz03xPRKJrHTdZbe3IicyucEtdRsV5yglwc3GyUfbAfpSeD0FJ41NbUNSt5wbhqfp1fS+BgnvDFQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "has-bigints": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-boolean-object": {
      "version": "1.2.2",
      "resolved": "https://registry.npmjs.org/is-boolean-object/-/is-boolean-object-1.2.2.tgz",
      "integrity": "sha512-wa56o2/ElJMYqjCjGkXri7it5FbebW5usLw/nPmCMs5DeZ7eziSYZhSmPRn0txqeW4LnAmQQU7FgqLpsEFKM4A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "has-tostringtag": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-callable": {
      "version": "1.2.7",
      "resolved": "https://registry.npmjs.org/is-callable/-/is-callable-1.2.7.tgz",
      "integrity": "sha512-1BC0BVFhS/p0qtw6enp8e+8OD0UrK0oFLztSjNzhcKA3WDuJxxAPXzPuPtKkjEY9UUoEWlX/8fgKeu2S8i9JTA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-core-module": {
      "version": "2.16.1",
      "resolved": "https://registry.npmjs.org/is-core-module/-/is-core-module-2.16.1.tgz",
      "integrity": "sha512-UfoeMA6fIJ8wTYFEUjelnaGI67v6+N7qXJEvQuIGa99l4xsCruSYOVSQ0uPANn4dAzm8lkYPaKLrrijLq7x23w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "hasown": "^2.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-data-view": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/is-data-view/-/is-data-view-1.0.2.tgz",
      "integrity": "sha512-RKtWF8pGmS87i2D6gqQu/l7EYRlVdfzemCJN/P3UOs//x1QE7mfhvzHIApBTRf7axvT6DMGwSwBXYCT0nfB9xw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "get-intrinsic": "^1.2.6",
        "is-typed-array": "^1.1.13"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-date-object": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/is-date-object/-/is-date-object-1.1.0.tgz",
      "integrity": "sha512-PwwhEakHVKTdRNVOw+/Gyh0+MzlCl4R6qKvkhuvLtPMggI1WAHt9sOwZxQLSGpUaDnrdyDsomoRgNnCfKNSXXg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "has-tostringtag": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-finalizationregistry": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/is-finalizationregistry/-/is-finalizationregistry-1.1.1.tgz",
      "integrity": "sha512-1pC6N8qWJbWoPtEjgcL2xyhQOP491EQjeUo3qTKcmV8YSDDJrOepfG8pcC7h/QgnQHYSv0mJ3Z/ZWxmatVrysg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-generator-function": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/is-generator-function/-/is-generator-function-1.1.0.tgz",
      "integrity": "sha512-nPUB5km40q9e8UfN/Zc24eLlzdSf9OfKByBw9CIdw4H1giPMeA0OIJvbchsCu4npfI2QcMVBsGEBHKZ7wLTWmQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "get-proto": "^1.0.0",
        "has-tostringtag": "^1.0.2",
        "safe-regex-test": "^1.1.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-map": {
      "version": "2.0.3",
      "resolved": "https://registry.npmjs.org/is-map/-/is-map-2.0.3.tgz",
      "integrity": "sha512-1Qed0/Hr2m+YqxnM09CjA2d/i6YZNfF6R2oRAOj36eUdS6qIV/huPJNSEpKbupewFs+ZsJlxsjjPbc0/afW6Lw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-number-object": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/is-number-object/-/is-number-object-1.1.1.tgz",
      "integrity": "sha512-lZhclumE1G6VYD8VHe35wFaIif+CTy5SJIi5+3y4psDgWu4wPDoBhF8NxUOinEc7pHgiTsT6MaBb92rKhhD+Xw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "has-tostringtag": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-promise": {
      "version": "4.0.0",
      "resolved": "https://registry.npmjs.org/is-promise/-/is-promise-4.0.0.tgz",
      "integrity": "sha512-hvpoI6korhJMnej285dSg6nu1+e6uxs7zG3BYAm5byqDsgJNWwxzM6z6iZiAgQR4TJ30JmBTOwqZUw3WlyH3AQ==",
      "license": "MIT"
    },
    "node_modules/is-regex": {
      "version": "1.2.1",
      "resolved": "https://registry.npmjs.org/is-regex/-/is-regex-1.2.1.tgz",
      "integrity": "sha512-MjYsKHO5O7mCsmRGxWcLWheFqN9DJ/2TmngvjKXihe6efViPqc274+Fx/4fYj/r03+ESvBdTXK0V6tA3rgez1g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "gopd": "^1.2.0",
        "has-tostringtag": "^1.0.2",
        "hasown": "^2.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-set": {
      "version": "2.0.3",
      "resolved": "https://registry.npmjs.org/is-set/-/is-set-2.0.3.tgz",
      "integrity": "sha512-iPAjerrse27/ygGLxw+EBR9agv9Y6uLeYVJMu+QNCoouJ1/1ri0mGrcWpfCqFZuzzx3WjtwxG098X+n4OuRkPg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-shared-array-buffer": {
      "version": "1.0.4",
      "resolved": "https://registry.npmjs.org/is-shared-array-buffer/-/is-shared-array-buffer-1.0.4.tgz",
      "integrity": "sha512-ISWac8drv4ZGfwKl5slpHG9OwPNty4jOWPRIhBpxOoD+hqITiwuipOQ2bNthAzwA3B4fIjO4Nln74N0S9byq8A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-string": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/is-string/-/is-string-1.1.1.tgz",
      "integrity": "sha512-BtEeSsoaQjlSPBemMQIrY1MY0uM6vnS1g5fmufYOtnxLGUZM2178PKbhsk7Ffv58IX+ZtcvoGwccYsh0PglkAA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "has-tostringtag": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-symbol": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/is-symbol/-/is-symbol-1.1.1.tgz",
      "integrity": "sha512-9gGx6GTtCQM73BgmHQXfDmLtfjjTUDSyoxTCbp5WtoixAhfgsDirWIcVQ/IHpvI5Vgd5i/J5F7B9cN/WlVbC/w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "has-symbols": "^1.1.0",
        "safe-regex-test": "^1.1.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-typed-array": {
      "version": "1.1.15",
      "resolved": "https://registry.npmjs.org/is-typed-array/-/is-typed-array-1.1.15.tgz",
      "integrity": "sha512-p3EcsicXjit7SaskXHs1hA91QxgTw46Fv6EFKKGS5DRFLD8yKnohjF3hxoju94b/OcMZoQukzpPpBE9uLVKzgQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "which-typed-array": "^1.1.16"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-weakmap": {
      "version": "2.0.2",
      "resolved": "https://registry.npmjs.org/is-weakmap/-/is-weakmap-2.0.2.tgz",
      "integrity": "sha512-K5pXYOm9wqY1RgjpL3YTkF39tni1XajUIkawTLUo9EZEVUFga5gSQJF8nNS7ZwJQ02y+1YCNYcMh+HIf1ZqE+w==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-weakref": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/is-weakref/-/is-weakref-1.1.1.tgz",
      "integrity": "sha512-6i9mGWSlqzNMEqpCp93KwRS1uUOodk2OJ6b+sq7ZPDSy2WuI5NFIxp/254TytR8ftefexkWn5xNiHUNpPOfSew==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/is-weakset": {
      "version": "2.0.4",
      "resolved": "https://registry.npmjs.org/is-weakset/-/is-weakset-2.0.4.tgz",
      "integrity": "sha512-mfcwb6IzQyOKTs84CQMrOwW4gQcaTOAWJ0zzJCl2WSPDrWk/OzDaImWFH3djXhb24g4eudZfLRozAvPGw4d9hQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "get-intrinsic": "^1.2.6"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/isarray": {
      "version": "2.0.5",
      "resolved": "https://registry.npmjs.org/isarray/-/isarray-2.0.5.tgz",
      "integrity": "sha512-xHjhDr3cNBK0BzdUJSPXZntQUx/mwMS5Rw4A7lPJ90XGAO6ISP/ePDNuo0vhqOZU+UD5JoodwCAAoZQd3FeAKw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/isexe": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/isexe/-/isexe-2.0.0.tgz",
      "integrity": "sha512-RHxMLp9lnKHGHRng9QFhRCMbYAcVpn69smSGcq3f36xjgVVWThj4qqLbTLlq7Ssj8B+fIQ1EuCEGI2lKsyQeIw==",
      "dev": true,
      "license": "ISC"
    },
    "node_modules/jose": {
      "version": "4.15.9",
      "resolved": "https://registry.npmjs.org/jose/-/jose-4.15.9.tgz",
      "integrity": "sha512-1vUQX+IdDMVPj4k8kOxgUqlcK518yluMuGZwqlr44FS1ppZB/5GWh4rZG89erpOBOJjU/OBsnCVFfapsRz6nEA==",
      "license": "MIT",
      "funding": {
        "url": "https://github.com/sponsors/panva"
      }
    },
    "node_modules/json-parse-better-errors": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/json-parse-better-errors/-/json-parse-better-errors-1.0.2.tgz",
      "integrity": "sha512-mrqyZKfX5EhL7hvqcV6WG1yYjnjeuYDzDhhcAAUrq8Po85NBQBJP+ZDUT75qZQ98IkUoBqdkExkukOU7Ts2wrw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/jsonwebtoken": {
      "version": "9.0.2",
      "resolved": "https://registry.npmjs.org/jsonwebtoken/-/jsonwebtoken-9.0.2.tgz",
      "integrity": "sha512-PRp66vJ865SSqOlgqS8hujT5U4AOgMfhrwYIuIhfKaoSCZcirrmASQr8CX7cUg+RMih+hgznrjp99o+W4pJLHQ==",
      "license": "MIT",
      "dependencies": {
        "jws": "^3.2.2",
        "lodash.includes": "^4.3.0",
        "lodash.isboolean": "^3.0.3",
        "lodash.isinteger": "^4.0.4",
        "lodash.isnumber": "^3.0.3",
        "lodash.isplainobject": "^4.0.6",
        "lodash.isstring": "^4.0.1",
        "lodash.once": "^4.0.0",
        "ms": "^2.1.1",
        "semver": "^7.5.4"
      },
      "engines": {
        "node": ">=12",
        "npm": ">=6"
      }
    },
    "node_modules/jwa": {
      "version": "1.4.1",
      "resolved": "https://registry.npmjs.org/jwa/-/jwa-1.4.1.tgz",
      "integrity": "sha512-qiLX/xhEEFKUAJ6FiBMbes3w9ATzyk5W7Hvzpa/SLYdxNtng+gcurvrI7TbACjIXlsJyr05/S1oUhZrc63evQA==",
      "license": "MIT",
      "dependencies": {
        "buffer-equal-constant-time": "1.0.1",
        "ecdsa-sig-formatter": "1.0.11",
        "safe-buffer": "^5.0.1"
      }
    },
    "node_modules/jwks-rsa": {
      "version": "3.1.0",
      "resolved": "https://registry.npmjs.org/jwks-rsa/-/jwks-rsa-3.1.0.tgz",
      "integrity": "sha512-v7nqlfezb9YfHHzYII3ef2a2j1XnGeSE/bK3WfumaYCqONAIstJbrEGapz4kadScZzEt7zYCN7bucj8C0Mv/Rg==",
      "license": "MIT",
      "dependencies": {
        "@types/express": "^4.17.17",
        "@types/jsonwebtoken": "^9.0.2",
        "debug": "^4.3.4",
        "jose": "^4.14.6",
        "limiter": "^1.1.5",
        "lru-memoizer": "^2.2.0"
      },
      "engines": {
        "node": ">=14"
      }
    },
    "node_modules/jws": {
      "version": "3.2.2",
      "resolved": "https://registry.npmjs.org/jws/-/jws-3.2.2.tgz",
      "integrity": "sha512-YHlZCB6lMTllWDtSPHz/ZXTsi8S00usEV6v1tjq8tOUZzw7DpSDWVXjXDre6ed1w/pd495ODpHZYSdkRTsa0HA==",
      "license": "MIT",
      "dependencies": {
        "jwa": "^1.4.1",
        "safe-buffer": "^5.0.1"
      }
    },
    "node_modules/limiter": {
      "version": "1.1.5",
      "resolved": "https://registry.npmjs.org/limiter/-/limiter-1.1.5.tgz",
      "integrity": "sha512-FWWMIEOxz3GwUI4Ts/IvgVy6LPvoMPgjMdQ185nN6psJyBJ4yOpzqm695/h5umdLJg2vW3GR5iG11MAkR2AzJA=="
    },
    "node_modules/load-json-file": {
      "version": "4.0.0",
      "resolved": "https://registry.npmjs.org/load-json-file/-/load-json-file-4.0.0.tgz",
      "integrity": "sha512-Kx8hMakjX03tiGTLAIdJ+lL0htKnXjEZN6hk/tozf/WOuYGdZBJrZ+rCJRbVCugsjB3jMLn9746NsQIf5VjBMw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "graceful-fs": "^4.1.2",
        "parse-json": "^4.0.0",
        "pify": "^3.0.0",
        "strip-bom": "^3.0.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/lodash.clonedeep": {
      "version": "4.5.0",
      "resolved": "https://registry.npmjs.org/lodash.clonedeep/-/lodash.clonedeep-4.5.0.tgz",
      "integrity": "sha512-H5ZhCF25riFd9uB5UCkVKo61m3S/xZk1x4wA6yp/L3RFP6Z/eHH1ymQcGLo7J3GMPfm0V/7m1tryHuGVxpqEBQ==",
      "license": "MIT"
    },
    "node_modules/lodash.includes": {
      "version": "4.3.0",
      "resolved": "https://registry.npmjs.org/lodash.includes/-/lodash.includes-4.3.0.tgz",
      "integrity": "sha512-W3Bx6mdkRTGtlJISOvVD/lbqjTlPPUDTMnlXZFnVwi9NKJ6tiAk6LVdlhZMm17VZisqhKcgzpO5Wz91PCt5b0w==",
      "license": "MIT"
    },
    "node_modules/lodash.isboolean": {
      "version": "3.0.3",
      "resolved": "https://registry.npmjs.org/lodash.isboolean/-/lodash.isboolean-3.0.3.tgz",
      "integrity": "sha512-Bz5mupy2SVbPHURB98VAcw+aHh4vRV5IPNhILUCsOzRmsTmSQ17jIuqopAentWoehktxGd9e/hbIXq980/1QJg==",
      "license": "MIT"
    },
    "node_modules/lodash.isinteger": {
      "version": "4.0.4",
      "resolved": "https://registry.npmjs.org/lodash.isinteger/-/lodash.isinteger-4.0.4.tgz",
      "integrity": "sha512-DBwtEWN2caHQ9/imiNeEA5ys1JoRtRfY3d7V9wkqtbycnAmTvRRmbHKDV4a0EYc678/dia0jrte4tjYwVBaZUA==",
      "license": "MIT"
    },
    "node_modules/lodash.isnumber": {
      "version": "3.0.3",
      "resolved": "https://registry.npmjs.org/lodash.isnumber/-/lodash.isnumber-3.0.3.tgz",
      "integrity": "sha512-QYqzpfwO3/CWf3XP+Z+tkQsfaLL/EnUlXWVkIk5FUPc4sBdTehEqZONuyRt2P67PXAk+NXmTBcc97zw9t1FQrw==",
      "license": "MIT"
    },
    "node_modules/lodash.isplainobject": {
      "version": "4.0.6",
      "resolved": "https://registry.npmjs.org/lodash.isplainobject/-/lodash.isplainobject-4.0.6.tgz",
      "integrity": "sha512-oSXzaWypCMHkPC3NvBEaPHf0KsA5mvPrOPgQWDsbg8n7orZ290M0BmC/jgRZ4vcJ6DTAhjrsSYgdsW/F+MFOBA==",
      "license": "MIT"
    },
    "node_modules/lodash.isstring": {
      "version": "4.0.1",
      "resolved": "https://registry.npmjs.org/lodash.isstring/-/lodash.isstring-4.0.1.tgz",
      "integrity": "sha512-0wJxfxH1wgO3GrbuP+dTTk7op+6L41QCXbGINEmD+ny/G/eCqGzxyCsh7159S+mgDDcoarnBw6PC1PS5+wUGgw==",
      "license": "MIT"
    },
    "node_modules/lodash.once": {
      "version": "4.1.1",
      "resolved": "https://registry.npmjs.org/lodash.once/-/lodash.once-4.1.1.tgz",
      "integrity": "sha512-Sb487aTOCr9drQVL8pIxOzVhafOjZN9UU54hiN8PU3uAiSV7lx1yYNpbNmex2PK6dSJoNTSJUUswT651yww3Mg==",
      "license": "MIT"
    },
    "node_modules/lru-cache": {
      "version": "6.0.0",
      "resolved": "https://registry.npmjs.org/lru-cache/-/lru-cache-6.0.0.tgz",
      "integrity": "sha512-Jo6dJ04CmSjuznwJSS3pUeWmd/H0ffTlkXXgwZi+eq1UCmqQwCh+eLsYOYCwY991i2Fah4h1BEMCx4qThGbsiA==",
      "license": "ISC",
      "dependencies": {
        "yallist": "^4.0.0"
      },
      "engines": {
        "node": ">=10"
      }
    },
    "node_modules/lru-memoizer": {
      "version": "2.3.0",
      "resolved": "https://registry.npmjs.org/lru-memoizer/-/lru-memoizer-2.3.0.tgz",
      "integrity": "sha512-GXn7gyHAMhO13WSKrIiNfztwxodVsP8IoZ3XfrJV4yH2x0/OeTO/FIaAHTY5YekdGgW94njfuKmyyt1E0mR6Ug==",
      "license": "MIT",
      "dependencies": {
        "lodash.clonedeep": "^4.5.0",
        "lru-cache": "6.0.0"
      }
    },
    "node_modules/math-intrinsics": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/math-intrinsics/-/math-intrinsics-1.1.0.tgz",
      "integrity": "sha512-/IXtbwEk5HTPyEwyKX6hGkYXxM9nbj64B+ilVJnC/R6B0pH5G4V3b0pVbL7DBj4tkhBAppbQUlf6F6Xl9LHu1g==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/media-typer": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/media-typer/-/media-typer-1.1.0.tgz",
      "integrity": "sha512-aisnrDP4GNe06UcKFnV5bfMNPBUw4jsLGaWwWfnH3v02GnBuXX2MCVn5RbrWo0j3pczUilYblq7fQ7Nw2t5XKw==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/memorystream": {
      "version": "0.3.1",
      "resolved": "https://registry.npmjs.org/memorystream/-/memorystream-0.3.1.tgz",
      "integrity": "sha512-S3UwM3yj5mtUSEfP41UZmt/0SCoVYUcU1rkXv+BQ5Ig8ndL4sPoJNBUJERafdPb5jjHJGuMgytgKvKIf58XNBw==",
      "dev": true,
      "engines": {
        "node": ">= 0.10.0"
      }
    },
    "node_modules/merge-descriptors": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/merge-descriptors/-/merge-descriptors-2.0.0.tgz",
      "integrity": "sha512-Snk314V5ayFLhp3fkUREub6WtjBfPdCPY1Ln8/8munuLuiYhsABgBVWsozAG+MWMbVEvcdcpbi9R7ww22l9Q3g==",
      "license": "MIT",
      "engines": {
        "node": ">=18"
      },
      "funding": {
        "url": "https://github.com/sponsors/sindresorhus"
      }
    },
    "node_modules/methods": {
      "version": "1.1.2",
      "resolved": "https://registry.npmjs.org/methods/-/methods-1.1.2.tgz",
      "integrity": "sha512-iclAHeNqNm68zFtnZ0e+1L2yUIdvzNoauKU4WBA3VvH/vPFieF7qfRlwUZU+DA9P9bPXIS90ulxoUoCH23sV2w==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/mime-db": {
      "version": "1.53.0",
      "resolved": "https://registry.npmjs.org/mime-db/-/mime-db-1.53.0.tgz",
      "integrity": "sha512-oHlN/w+3MQ3rba9rqFr6V/ypF10LSkdwUysQL7GkXoTgIWeV+tcXGA852TBxH+gsh8UWoyhR1hKcoMJTuWflpg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/mime-types": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/mime-types/-/mime-types-3.0.0.tgz",
      "integrity": "sha512-XqoSHeCGjVClAmoGFG3lVFqQFRIrTVw2OH3axRqAcfaw+gHWIfnASS92AV+Rl/mk0MupgZTRHQOjxY6YVnzK5w==",
      "license": "MIT",
      "dependencies": {
        "mime-db": "^1.53.0"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/minimatch": {
      "version": "3.1.2",
      "resolved": "https://registry.npmjs.org/minimatch/-/minimatch-3.1.2.tgz",
      "integrity": "sha512-J7p63hRiAjw1NDEww1W7i37+ByIrOWO5XQQAzZ3VOcL0PNybwpfmV/N05zFAzwQ9USyEcX6t3UO+K5aqBQOIHw==",
      "dev": true,
      "license": "ISC",
      "dependencies": {
        "brace-expansion": "^1.1.7"
      },
      "engines": {
        "node": "*"
      }
    },
    "node_modules/ms": {
      "version": "2.1.3",
      "resolved": "https://registry.npmjs.org/ms/-/ms-2.1.3.tgz",
      "integrity": "sha512-6FlzubTLZG3J2a/NVCAleEhjzq5oxgHyaCU9yYXvcLsvoVaHJq/s5xXI6/XXP6tz7R9xAOtHnSO/tXtF3WRTlA==",
      "license": "MIT"
    },
    "node_modules/negotiator": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/negotiator/-/negotiator-1.0.0.tgz",
      "integrity": "sha512-8Ofs/AUQh8MaEcrlq5xOX0CQ9ypTF5dl78mjlMNfOK08fzpgTHQRQPBxcPlEtIw0yRpws+Zo/3r+5WRby7u3Gg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/nice-try": {
      "version": "1.0.5",
      "resolved": "https://registry.npmjs.org/nice-try/-/nice-try-1.0.5.tgz",
      "integrity": "sha512-1nh45deeb5olNY7eX82BkPO7SSxR5SSYJiPTrTdFUVYwAl8CKMA5N9PjTYkHiRjisVcxcQ1HXdLhx2qxxJzLNQ==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/normalize-package-data": {
      "version": "2.5.0",
      "resolved": "https://registry.npmjs.org/normalize-package-data/-/normalize-package-data-2.5.0.tgz",
      "integrity": "sha512-/5CMN3T0R4XTj4DcGaexo+roZSdSFW/0AOOTROrjxzCG1wrWXEsGbRKevjlIL+ZDE4sZlJr5ED4YW0yqmkK+eA==",
      "dev": true,
      "license": "BSD-2-Clause",
      "dependencies": {
        "hosted-git-info": "^2.1.4",
        "resolve": "^1.10.0",
        "semver": "2 || 3 || 4 || 5",
        "validate-npm-package-license": "^3.0.1"
      }
    },
    "node_modules/normalize-package-data/node_modules/semver": {
      "version": "5.7.2",
      "resolved": "https://registry.npmjs.org/semver/-/semver-5.7.2.tgz",
      "integrity": "sha512-cBznnQ9KjJqU67B52RMC65CMarK2600WFnbkcaiwWq3xy/5haFJlshgnpjovMVJ+Hff49d8GEn0b87C5pDQ10g==",
      "dev": true,
      "license": "ISC",
      "bin": {
        "semver": "bin/semver"
      }
    },
    "node_modules/npm-run-all": {
      "version": "4.1.5",
      "resolved": "https://registry.npmjs.org/npm-run-all/-/npm-run-all-4.1.5.tgz",
      "integrity": "sha512-Oo82gJDAVcaMdi3nuoKFavkIHBRVqQ1qvMb+9LHk/cF4P6B2m8aP04hGf7oL6wZ9BuGwX1onlLhpuoofSyoQDQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "ansi-styles": "^3.2.1",
        "chalk": "^2.4.1",
        "cross-spawn": "^6.0.5",
        "memorystream": "^0.3.1",
        "minimatch": "^3.0.4",
        "pidtree": "^0.3.0",
        "read-pkg": "^3.0.0",
        "shell-quote": "^1.6.1",
        "string.prototype.padend": "^3.0.0"
      },
      "bin": {
        "npm-run-all": "bin/npm-run-all/index.js",
        "run-p": "bin/run-p/index.js",
        "run-s": "bin/run-s/index.js"
      },
      "engines": {
        "node": ">= 4"
      }
    },
    "node_modules/object-inspect": {
      "version": "1.13.4",
      "resolved": "https://registry.npmjs.org/object-inspect/-/object-inspect-1.13.4.tgz",
      "integrity": "sha512-W67iLl4J2EXEGTbfeHCffrjDfitvLANg0UlX3wFUUSTx92KXRFegMHUVgSqE+wvhAbi4WqjGg9czysTV2Epbew==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/object-keys": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/object-keys/-/object-keys-1.1.1.tgz",
      "integrity": "sha512-NuAESUOUMrlIXOfHKzD6bpPu3tYt3xvjNdRIQ+FeT0lNb4K8WR70CaDxhuNguS2XG+GjkyMwOzsN5ZktImfhLA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/object.assign": {
      "version": "4.1.7",
      "resolved": "https://registry.npmjs.org/object.assign/-/object.assign-4.1.7.tgz",
      "integrity": "sha512-nK28WOo+QIjBkDduTINE4JkF/UJJKyf2EJxvJKfblDpyg0Q+pkOHNTL0Qwy6NP6FhE/EnzV73BxxqcJaXY9anw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.3",
        "define-properties": "^1.2.1",
        "es-object-atoms": "^1.0.0",
        "has-symbols": "^1.1.0",
        "object-keys": "^1.1.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/on-finished": {
      "version": "2.4.1",
      "resolved": "https://registry.npmjs.org/on-finished/-/on-finished-2.4.1.tgz",
      "integrity": "sha512-oVlzkg3ENAhCk2zdv7IJwd/QUD4z2RxRwpkcGY8psCVcCYZNq4wYnVWALHM+brtuJjePWiYF/ClmuDr8Ch5+kg==",
      "license": "MIT",
      "dependencies": {
        "ee-first": "1.1.1"
      },
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/once": {
      "version": "1.4.0",
      "resolved": "https://registry.npmjs.org/once/-/once-1.4.0.tgz",
      "integrity": "sha512-lNaJgI+2Q5URQBkccEKHTQOPaXdUxnZZElQTZY0MFUAuaEqe1E+Nyvgdz/aIyNi6Z9MzO5dv1H8n58/GELp3+w==",
      "license": "ISC",
      "dependencies": {
        "wrappy": "1"
      }
    },
    "node_modules/own-keys": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/own-keys/-/own-keys-1.0.1.tgz",
      "integrity": "sha512-qFOyK5PjiWZd+QQIh+1jhdb9LpxTF0qs7Pm8o5QHYZ0M3vKqSqzsZaEB6oWlxZ+q2sJBMI/Ktgd2N5ZwQoRHfg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "get-intrinsic": "^1.2.6",
        "object-keys": "^1.1.1",
        "safe-push-apply": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/parse-json": {
      "version": "4.0.0",
      "resolved": "https://registry.npmjs.org/parse-json/-/parse-json-4.0.0.tgz",
      "integrity": "sha512-aOIos8bujGN93/8Ox/jPLh7RwVnPEysynVFE+fQZyg6jKELEHwzgKdLRFHUgXJL6kylijVSBC4BvN9OmsB48Rw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "error-ex": "^1.3.1",
        "json-parse-better-errors": "^1.0.1"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/parseurl": {
      "version": "1.3.3",
      "resolved": "https://registry.npmjs.org/parseurl/-/parseurl-1.3.3.tgz",
      "integrity": "sha512-CiyeOxFT/JZyN5m0z9PfXw4SCBJ6Sygz1Dpl0wqjlhDEGGBP1GnsUVEL0p63hoG1fcj3fHynXi9NYO4nWOL+qQ==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/path-key": {
      "version": "2.0.1",
      "resolved": "https://registry.npmjs.org/path-key/-/path-key-2.0.1.tgz",
      "integrity": "sha512-fEHGKCSmUSDPv4uoj8AlD+joPlq3peND+HRYyxFz4KPw4z926S/b8rIuFs2FYJg3BwsxJf6A9/3eIdLaYC+9Dw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/path-parse": {
      "version": "1.0.7",
      "resolved": "https://registry.npmjs.org/path-parse/-/path-parse-1.0.7.tgz",
      "integrity": "sha512-LDJzPVEEEPR+y48z93A0Ed0yXb8pAByGWo/k5YYdYgpY2/2EsOsksJrq7lOHxryrVOn1ejG6oAp8ahvOIQD8sw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/path-to-regexp": {
      "version": "8.2.0",
      "resolved": "https://registry.npmjs.org/path-to-regexp/-/path-to-regexp-8.2.0.tgz",
      "integrity": "sha512-TdrF7fW9Rphjq4RjrW0Kp2AW0Ahwu9sRGTkS6bvDi0SCwZlEZYmcfDbEsTz8RVk0EHIS/Vd1bv3JhG+1xZuAyQ==",
      "license": "MIT",
      "engines": {
        "node": ">=16"
      }
    },
    "node_modules/path-type": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/path-type/-/path-type-3.0.0.tgz",
      "integrity": "sha512-T2ZUsdZFHgA3u4e5PfPbjd7HDDpxPnQb5jN0SrDsjNSuVXHJqtwTnWqG0B1jZrgmJ/7lj1EmVIByWt1gxGkWvg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "pify": "^3.0.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/pidtree": {
      "version": "0.3.1",
      "resolved": "https://registry.npmjs.org/pidtree/-/pidtree-0.3.1.tgz",
      "integrity": "sha512-qQbW94hLHEqCg7nhby4yRC7G2+jYHY4Rguc2bjw7Uug4GIJuu1tvf2uHaZv5Q8zdt+WKJ6qK1FOI6amaWUo5FA==",
      "dev": true,
      "license": "MIT",
      "bin": {
        "pidtree": "bin/pidtree.js"
      },
      "engines": {
        "node": ">=0.10"
      }
    },
    "node_modules/pify": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/pify/-/pify-3.0.0.tgz",
      "integrity": "sha512-C3FsVNH1udSEX48gGX1xfvwTWfsYWj5U+8/uK15BGzIGrKoUpghX8hWZwa/OFnakBiiVNmBvemTJR5mcy7iPcg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/possible-typed-array-names": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/possible-typed-array-names/-/possible-typed-array-names-1.1.0.tgz",
      "integrity": "sha512-/+5VFTchJDoVj3bhoqi6UeymcD00DAwb1nJwamzPvHEszJ4FpF6SNNbUbOS8yI56qHzdV8eK0qEfOSiodkTdxg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/proxy-addr": {
      "version": "2.0.7",
      "resolved": "https://registry.npmjs.org/proxy-addr/-/proxy-addr-2.0.7.tgz",
      "integrity": "sha512-llQsMLSUDUPT44jdrU/O37qlnifitDP+ZwrmmZcoSKyLKvtZxpyV0n2/bD/N4tBAAZ/gJEdZU7KMraoK1+XYAg==",
      "license": "MIT",
      "dependencies": {
        "forwarded": "0.2.0",
        "ipaddr.js": "1.9.1"
      },
      "engines": {
        "node": ">= 0.10"
      }
    },
    "node_modules/proxy-from-env": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/proxy-from-env/-/proxy-from-env-1.1.0.tgz",
      "integrity": "sha512-D+zkORCbA9f1tdWRK0RaCR3GPv50cMxcrz4X8k5LTSUD1Dkw47mKJEZQNunItRTkWwgtaUSo1RVFRIG9ZXiFYg==",
      "license": "MIT"
    },
    "node_modules/qs": {
      "version": "6.13.0",
      "resolved": "https://registry.npmjs.org/qs/-/qs-6.13.0.tgz",
      "integrity": "sha512-+38qI9SOr8tfZ4QmJNplMUxqjbe7LKvvZgWdExBOmd+egZTtjLB67Gu0HRX3u/XOq7UU2Nx6nsjvS16Z9uwfpg==",
      "license": "BSD-3-Clause",
      "dependencies": {
        "side-channel": "^1.0.6"
      },
      "engines": {
        "node": ">=0.6"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/range-parser": {
      "version": "1.2.1",
      "resolved": "https://registry.npmjs.org/range-parser/-/range-parser-1.2.1.tgz",
      "integrity": "sha512-Hrgsx+orqoygnmhFbKaHE6c296J+HTAQXoxEF6gNupROmmGJRoyzfG3ccAveqCBrwr/2yxQ5BVd/GTl5agOwSg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/raw-body": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/raw-body/-/raw-body-3.0.0.tgz",
      "integrity": "sha512-RmkhL8CAyCRPXCE28MMH0z2PNWQBNk2Q09ZdxM9IOOXwxwZbN+qbWaatPkdkWIKL2ZVDImrN/pK5HTRz2PcS4g==",
      "license": "MIT",
      "dependencies": {
        "bytes": "3.1.2",
        "http-errors": "2.0.0",
        "iconv-lite": "0.6.3",
        "unpipe": "1.0.0"
      },
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/raw-body/node_modules/iconv-lite": {
      "version": "0.6.3",
      "resolved": "https://registry.npmjs.org/iconv-lite/-/iconv-lite-0.6.3.tgz",
      "integrity": "sha512-4fCk79wshMdzMp2rH06qWrJE4iolqLhCUH+OiuIgU++RB0+94NlDL81atO7GX55uUKueo0txHNtvEyI6D7WdMw==",
      "license": "MIT",
      "dependencies": {
        "safer-buffer": ">= 2.1.2 < 3.0.0"
      },
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/read-pkg": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/read-pkg/-/read-pkg-3.0.0.tgz",
      "integrity": "sha512-BLq/cCO9two+lBgiTYNqD6GdtK8s4NpaWrl6/rCO9w0TUS8oJl7cmToOZfRYllKTISY6nt1U7jQ53brmKqY6BA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "load-json-file": "^4.0.0",
        "normalize-package-data": "^2.3.2",
        "path-type": "^3.0.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/reflect.getprototypeof": {
      "version": "1.0.10",
      "resolved": "https://registry.npmjs.org/reflect.getprototypeof/-/reflect.getprototypeof-1.0.10.tgz",
      "integrity": "sha512-00o4I+DVrefhv+nX0ulyi3biSHCPDe+yLv5o/p6d/UVlirijB8E16FtfwSAi4g3tcqrQ4lRAqQSoFEZJehYEcw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "define-properties": "^1.2.1",
        "es-abstract": "^1.23.9",
        "es-errors": "^1.3.0",
        "es-object-atoms": "^1.0.0",
        "get-intrinsic": "^1.2.7",
        "get-proto": "^1.0.1",
        "which-builtin-type": "^1.2.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/regexp.prototype.flags": {
      "version": "1.5.4",
      "resolved": "https://registry.npmjs.org/regexp.prototype.flags/-/regexp.prototype.flags-1.5.4.tgz",
      "integrity": "sha512-dYqgNSZbDwkaJ2ceRd9ojCGjBq+mOm9LmtXnAnEGyHhN/5R7iDW2TRw3h+o/jCFxus3P2LfWIIiwowAjANm7IA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "define-properties": "^1.2.1",
        "es-errors": "^1.3.0",
        "get-proto": "^1.0.1",
        "gopd": "^1.2.0",
        "set-function-name": "^2.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/resolve": {
      "version": "1.22.10",
      "resolved": "https://registry.npmjs.org/resolve/-/resolve-1.22.10.tgz",
      "integrity": "sha512-NPRy+/ncIMeDlTAsuqwKIiferiawhefFJtkNSW0qZJEqMEb+qBt/77B/jGeeek+F0uOeN05CDa6HXbbIgtVX4w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-core-module": "^2.16.0",
        "path-parse": "^1.0.7",
        "supports-preserve-symlinks-flag": "^1.0.0"
      },
      "bin": {
        "resolve": "bin/resolve"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/router": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/router/-/router-2.1.0.tgz",
      "integrity": "sha512-/m/NSLxeYEgWNtyC+WtNHCF7jbGxOibVWKnn+1Psff4dJGOfoXP+MuC/f2CwSmyiHdOIzYnYFp4W6GxWfekaLA==",
      "license": "MIT",
      "dependencies": {
        "is-promise": "^4.0.0",
        "parseurl": "^1.3.3",
        "path-to-regexp": "^8.0.0"
      },
      "engines": {
        "node": ">= 18"
      }
    },
    "node_modules/safe-array-concat": {
      "version": "1.1.3",
      "resolved": "https://registry.npmjs.org/safe-array-concat/-/safe-array-concat-1.1.3.tgz",
      "integrity": "sha512-AURm5f0jYEOydBj7VQlVvDrjeFgthDdEF5H1dP+6mNpoXOMo1quQqJ4wvJDyRZ9+pO3kGWoOdmV08cSv2aJV6Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.2",
        "get-intrinsic": "^1.2.6",
        "has-symbols": "^1.1.0",
        "isarray": "^2.0.5"
      },
      "engines": {
        "node": ">=0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/safe-buffer": {
      "version": "5.2.1",
      "resolved": "https://registry.npmjs.org/safe-buffer/-/safe-buffer-5.2.1.tgz",
      "integrity": "sha512-rp3So07KcdmmKbGvgaNxQSJr7bGVSVk5S9Eq1F+ppbRo70+YeaDxkw5Dd8NPN+GD6bjnYm2VuPuCXmpuYvmCXQ==",
      "funding": [
        {
          "type": "github",
          "url": "https://github.com/sponsors/feross"
        },
        {
          "type": "patreon",
          "url": "https://www.patreon.com/feross"
        },
        {
          "type": "consulting",
          "url": "https://feross.org/support"
        }
      ],
      "license": "MIT"
    },
    "node_modules/safe-push-apply": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/safe-push-apply/-/safe-push-apply-1.0.0.tgz",
      "integrity": "sha512-iKE9w/Z7xCzUMIZqdBsp6pEQvwuEebH4vdpjcDWnyzaI6yl6O9FHvVpmGelvEHNsoY6wGblkxR6Zty/h00WiSA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "isarray": "^2.0.5"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/safe-regex-test": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/safe-regex-test/-/safe-regex-test-1.1.0.tgz",
      "integrity": "sha512-x/+Cz4YrimQxQccJf5mKEbIa1NzeCRNI5Ecl/ekmlYaampdNLPalVyIcCZNNH3MvmqBugV5TMYZXv0ljslUlaw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "es-errors": "^1.3.0",
        "is-regex": "^1.2.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/safer-buffer": {
      "version": "2.1.2",
      "resolved": "https://registry.npmjs.org/safer-buffer/-/safer-buffer-2.1.2.tgz",
      "integrity": "sha512-YZo3K82SD7Riyi0E1EQPojLz7kpepnSQI9IyPbHHg1XXXevb5dJI7tpyN2ADxGcQbHG7vcyRHk0cbwqcQriUtg==",
      "license": "MIT"
    },
    "node_modules/semver": {
      "version": "7.7.1",
      "resolved": "https://registry.npmjs.org/semver/-/semver-7.7.1.tgz",
      "integrity": "sha512-hlq8tAfn0m/61p4BVRcPzIGr6LKiMwo4VM6dGi6pt4qcRkmNzTcWq6eCEjEh+qXjkMDvPlOFFSGwQjoEa6gyMA==",
      "license": "ISC",
      "bin": {
        "semver": "bin/semver.js"
      },
      "engines": {
        "node": ">=10"
      }
    },
    "node_modules/send": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/send/-/send-1.1.0.tgz",
      "integrity": "sha512-v67WcEouB5GxbTWL/4NeToqcZiAWEq90N888fczVArY8A79J0L4FD7vj5hm3eUMua5EpoQ59wa/oovY6TLvRUA==",
      "license": "MIT",
      "dependencies": {
        "debug": "^4.3.5",
        "destroy": "^1.2.0",
        "encodeurl": "^2.0.0",
        "escape-html": "^1.0.3",
        "etag": "^1.8.1",
        "fresh": "^0.5.2",
        "http-errors": "^2.0.0",
        "mime-types": "^2.1.35",
        "ms": "^2.1.3",
        "on-finished": "^2.4.1",
        "range-parser": "^1.2.1",
        "statuses": "^2.0.1"
      },
      "engines": {
        "node": ">= 18"
      }
    },
    "node_modules/send/node_modules/fresh": {
      "version": "0.5.2",
      "resolved": "https://registry.npmjs.org/fresh/-/fresh-0.5.2.tgz",
      "integrity": "sha512-zJ2mQYM18rEFOudeV4GShTGIQ7RbzA7ozbU9I/XBpm7kqgMywgmylMwXHxZJmkVoYkna9d2pVXVXPdYTP9ej8Q==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/send/node_modules/mime-db": {
      "version": "1.52.0",
      "resolved": "https://registry.npmjs.org/mime-db/-/mime-db-1.52.0.tgz",
      "integrity": "sha512-sPU4uV7dYlvtWJxwwxHD0PuihVNiE7TyAbQ5SWxDCB9mUYvOgroQOwYQQOKPJ8CIbE+1ETVlOoK1UC2nU3gYvg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/send/node_modules/mime-types": {
      "version": "2.1.35",
      "resolved": "https://registry.npmjs.org/mime-types/-/mime-types-2.1.35.tgz",
      "integrity": "sha512-ZDY+bPm5zTTF+YpCrAU9nK0UgICYPT0QtT1NZWFv4s++TNkcgVaT0g6+4R2uI4MjQjzysHB1zxuWL50hzaeXiw==",
      "license": "MIT",
      "dependencies": {
        "mime-db": "1.52.0"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/serve-static": {
      "version": "2.1.0",
      "resolved": "https://registry.npmjs.org/serve-static/-/serve-static-2.1.0.tgz",
      "integrity": "sha512-A3We5UfEjG8Z7VkDv6uItWw6HY2bBSBJT1KtVESn6EOoOr2jAxNhxWCLY3jDE2WcuHXByWju74ck3ZgLwL8xmA==",
      "license": "MIT",
      "dependencies": {
        "encodeurl": "^2.0.0",
        "escape-html": "^1.0.3",
        "parseurl": "^1.3.3",
        "send": "^1.0.0"
      },
      "engines": {
        "node": ">= 18"
      }
    },
    "node_modules/set-function-length": {
      "version": "1.2.2",
      "resolved": "https://registry.npmjs.org/set-function-length/-/set-function-length-1.2.2.tgz",
      "integrity": "sha512-pgRc4hJ4/sNjWCSS9AmnS40x3bNMDTknHgL5UaMBTMyJnU90EgWh1Rz+MC9eFu4BuN/UwZjKQuY/1v3rM7HMfg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "define-data-property": "^1.1.4",
        "es-errors": "^1.3.0",
        "function-bind": "^1.1.2",
        "get-intrinsic": "^1.2.4",
        "gopd": "^1.0.1",
        "has-property-descriptors": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/set-function-name": {
      "version": "2.0.2",
      "resolved": "https://registry.npmjs.org/set-function-name/-/set-function-name-2.0.2.tgz",
      "integrity": "sha512-7PGFlmtwsEADb0WYyvCMa1t+yke6daIG4Wirafur5kcf+MhUnPms1UeR0CKQdTZD81yESwMHbtn+TR+dMviakQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "define-data-property": "^1.1.4",
        "es-errors": "^1.3.0",
        "functions-have-names": "^1.2.3",
        "has-property-descriptors": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/set-proto": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/set-proto/-/set-proto-1.0.0.tgz",
      "integrity": "sha512-RJRdvCo6IAnPdsvP/7m6bsQqNnn1FCBX5ZNtFL98MmFF/4xAIJTIg1YbHW5DC2W5SKZanrC6i4HsJqlajw/dZw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "dunder-proto": "^1.0.1",
        "es-errors": "^1.3.0",
        "es-object-atoms": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/setprototypeof": {
      "version": "1.2.0",
      "resolved": "https://registry.npmjs.org/setprototypeof/-/setprototypeof-1.2.0.tgz",
      "integrity": "sha512-E5LDX7Wrp85Kil5bhZv46j8jOeboKq5JMmYM3gVGdGH8xFpPWXUMsNrlODCrkoxMEeNi/XZIwuRvY4XNwYMJpw==",
      "license": "ISC"
    },
    "node_modules/shebang-command": {
      "version": "1.2.0",
      "resolved": "https://registry.npmjs.org/shebang-command/-/shebang-command-1.2.0.tgz",
      "integrity": "sha512-EV3L1+UQWGor21OmnvojK36mhg+TyIKDh3iFBKBohr5xeXIhNBcx8oWdgkTEEQ+BEFFYdLRuqMfd5L84N1V5Vg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "shebang-regex": "^1.0.0"
      },
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/shebang-regex": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/shebang-regex/-/shebang-regex-1.0.0.tgz",
      "integrity": "sha512-wpoSFAxys6b2a2wHZ1XpDSgD7N9iVjg29Ph9uV/uaP9Ex/KXlkTZTeddxDPSYQpgvzKLGJke2UU0AzoGCjNIvQ==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/shell-quote": {
      "version": "1.8.2",
      "resolved": "https://registry.npmjs.org/shell-quote/-/shell-quote-1.8.2.tgz",
      "integrity": "sha512-AzqKpGKjrj7EM6rKVQEPpB288oCfnrEIuyoT9cyF4nmGa7V8Zk6f7RRqYisX8X9m+Q7bd632aZW4ky7EhbQztA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/side-channel": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/side-channel/-/side-channel-1.1.0.tgz",
      "integrity": "sha512-ZX99e6tRweoUXqR+VBrslhda51Nh5MTQwou5tnUDgbtyM0dBgmhEDtWGP/xbKn6hqfPRHujUNwz5fy/wbbhnpw==",
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "object-inspect": "^1.13.3",
        "side-channel-list": "^1.0.0",
        "side-channel-map": "^1.0.1",
        "side-channel-weakmap": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/side-channel-list": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/side-channel-list/-/side-channel-list-1.0.0.tgz",
      "integrity": "sha512-FCLHtRD/gnpCiCHEiJLOwdmFP+wzCmDEkc9y7NsYxeF4u7Btsn1ZuwgwJGxImImHicJArLP4R0yX4c2KCrMrTA==",
      "license": "MIT",
      "dependencies": {
        "es-errors": "^1.3.0",
        "object-inspect": "^1.13.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/side-channel-map": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/side-channel-map/-/side-channel-map-1.0.1.tgz",
      "integrity": "sha512-VCjCNfgMsby3tTdo02nbjtM/ewra6jPHmpThenkTYh8pG9ucZ/1P8So4u4FGBek/BjpOVsDCMoLA/iuBKIFXRA==",
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "es-errors": "^1.3.0",
        "get-intrinsic": "^1.2.5",
        "object-inspect": "^1.13.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/side-channel-weakmap": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/side-channel-weakmap/-/side-channel-weakmap-1.0.2.tgz",
      "integrity": "sha512-WPS/HvHQTYnHisLo9McqBHOJk2FkHO/tlpvldyrnem4aeQp4hai3gythswg6p01oSoTl58rcpiFAjF2br2Ak2A==",
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "es-errors": "^1.3.0",
        "get-intrinsic": "^1.2.5",
        "object-inspect": "^1.13.3",
        "side-channel-map": "^1.0.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/spdx-correct": {
      "version": "3.2.0",
      "resolved": "https://registry.npmjs.org/spdx-correct/-/spdx-correct-3.2.0.tgz",
      "integrity": "sha512-kN9dJbvnySHULIluDHy32WHRUu3Og7B9sbY7tsFLctQkIqnMh3hErYgdMjTYuqmcXX+lK5T1lnUt3G7zNswmZA==",
      "dev": true,
      "license": "Apache-2.0",
      "dependencies": {
        "spdx-expression-parse": "^3.0.0",
        "spdx-license-ids": "^3.0.0"
      }
    },
    "node_modules/spdx-exceptions": {
      "version": "2.5.0",
      "resolved": "https://registry.npmjs.org/spdx-exceptions/-/spdx-exceptions-2.5.0.tgz",
      "integrity": "sha512-PiU42r+xO4UbUS1buo3LPJkjlO7430Xn5SVAhdpzzsPHsjbYVflnnFdATgabnLude+Cqu25p6N+g2lw/PFsa4w==",
      "dev": true,
      "license": "CC-BY-3.0"
    },
    "node_modules/spdx-expression-parse": {
      "version": "3.0.1",
      "resolved": "https://registry.npmjs.org/spdx-expression-parse/-/spdx-expression-parse-3.0.1.tgz",
      "integrity": "sha512-cbqHunsQWnJNE6KhVSMsMeH5H/L9EpymbzqTQ3uLwNCLZ1Q481oWaofqH7nO6V07xlXwY6PhQdQ2IedWx/ZK4Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "spdx-exceptions": "^2.1.0",
        "spdx-license-ids": "^3.0.0"
      }
    },
    "node_modules/spdx-license-ids": {
      "version": "3.0.21",
      "resolved": "https://registry.npmjs.org/spdx-license-ids/-/spdx-license-ids-3.0.21.tgz",
      "integrity": "sha512-Bvg/8F5XephndSK3JffaRqdT+gyhfqIPwDHpX80tJrF8QQRYMo8sNMeaZ2Dp5+jhwKnUmIOyFFQfHRkjJm5nXg==",
      "dev": true,
      "license": "CC0-1.0"
    },
    "node_modules/statuses": {
      "version": "2.0.1",
      "resolved": "https://registry.npmjs.org/statuses/-/statuses-2.0.1.tgz",
      "integrity": "sha512-RwNA9Z/7PrK06rYLIzFMlaF+l73iwpzsqRIFgbMLbTcLD6cOao82TaWefPXQvB2fOC4AjuYSEndS7N/mTCbkdQ==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/string.prototype.padend": {
      "version": "3.1.6",
      "resolved": "https://registry.npmjs.org/string.prototype.padend/-/string.prototype.padend-3.1.6.tgz",
      "integrity": "sha512-XZpspuSB7vJWhvJc9DLSlrXl1mcA2BdoY5jjnS135ydXqLoqhs96JjDtCkjJEQHvfqZIp9hBuBMgI589peyx9Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.7",
        "define-properties": "^1.2.1",
        "es-abstract": "^1.23.2",
        "es-object-atoms": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/string.prototype.trim": {
      "version": "1.2.10",
      "resolved": "https://registry.npmjs.org/string.prototype.trim/-/string.prototype.trim-1.2.10.tgz",
      "integrity": "sha512-Rs66F0P/1kedk5lyYyH9uBzuiI/kNRmwJAR9quK6VOtIpZ2G+hMZd+HQbbv25MgCA6gEffoMZYxlTod4WcdrKA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.2",
        "define-data-property": "^1.1.4",
        "define-properties": "^1.2.1",
        "es-abstract": "^1.23.5",
        "es-object-atoms": "^1.0.0",
        "has-property-descriptors": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/string.prototype.trimend": {
      "version": "1.0.9",
      "resolved": "https://registry.npmjs.org/string.prototype.trimend/-/string.prototype.trimend-1.0.9.tgz",
      "integrity": "sha512-G7Ok5C6E/j4SGfyLCloXTrngQIQU3PWtXGst3yM7Bea9FRURf1S42ZHlZZtsNque2FN2PoUhfZXYLNWwEr4dLQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.2",
        "define-properties": "^1.2.1",
        "es-object-atoms": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/string.prototype.trimstart": {
      "version": "1.0.8",
      "resolved": "https://registry.npmjs.org/string.prototype.trimstart/-/string.prototype.trimstart-1.0.8.tgz",
      "integrity": "sha512-UXSH262CSZY1tfu3G3Secr6uGLCFVPMhIqHjlgCUtCCcgihYc/xKs9djMTMUOb2j1mVSeU8EU6NWc/iQKU6Gfg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.7",
        "define-properties": "^1.2.1",
        "es-object-atoms": "^1.0.0"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/strip-bom": {
      "version": "3.0.0",
      "resolved": "https://registry.npmjs.org/strip-bom/-/strip-bom-3.0.0.tgz",
      "integrity": "sha512-vavAMRXOgBVNF6nyEEmL3DBK19iRpDcoIwW+swQ+CbGiu7lju6t+JklA1MHweoWtadgt4ISVUsXLyDq34ddcwA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/supports-color": {
      "version": "5.5.0",
      "resolved": "https://registry.npmjs.org/supports-color/-/supports-color-5.5.0.tgz",
      "integrity": "sha512-QjVjwdXIt408MIiAqCX4oUKsgU2EqAGzs2Ppkm4aQYbjm+ZEWEcW4SfFNTr4uMNZma0ey4f5lgLrkB0aX0QMow==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "has-flag": "^3.0.0"
      },
      "engines": {
        "node": ">=4"
      }
    },
    "node_modules/supports-preserve-symlinks-flag": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/supports-preserve-symlinks-flag/-/supports-preserve-symlinks-flag-1.0.0.tgz",
      "integrity": "sha512-ot0WnXS9fgdkgIcePe6RHNk1WA8+muPa6cSjeR3V8K27q9BB1rTE3R1p7Hv0z1ZyAc8s6Vvv8DIyWf681MAt0w==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/toidentifier": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/toidentifier/-/toidentifier-1.0.1.tgz",
      "integrity": "sha512-o5sSPKEkg/DIQNmH43V0/uerLrpzVedkUh8tGNvaeXpfpuwjKenlSox/2O/BTlZUtEe+JG7s5YhEz608PlAHRA==",
      "license": "MIT",
      "engines": {
        "node": ">=0.6"
      }
    },
    "node_modules/type-is": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/type-is/-/type-is-2.0.0.tgz",
      "integrity": "sha512-gd0sGezQYCbWSbkZr75mln4YBidWUN60+devscpLF5mtRDUpiaTvKpBNrdaCvel1NdR2k6vclXybU5fBd2i+nw==",
      "license": "MIT",
      "dependencies": {
        "content-type": "^1.0.5",
        "media-typer": "^1.1.0",
        "mime-types": "^3.0.0"
      },
      "engines": {
        "node": ">= 0.6"
      }
    },
    "node_modules/typed-array-buffer": {
      "version": "1.0.3",
      "resolved": "https://registry.npmjs.org/typed-array-buffer/-/typed-array-buffer-1.0.3.tgz",
      "integrity": "sha512-nAYYwfY3qnzX30IkA6AQZjVbtK6duGontcQm1WSG1MD94YLqK0515GNApXkoxKOWMusVssAHWLh9SeaoefYFGw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "es-errors": "^1.3.0",
        "is-typed-array": "^1.1.14"
      },
      "engines": {
        "node": ">= 0.4"
      }
    },
    "node_modules/typed-array-byte-length": {
      "version": "1.0.3",
      "resolved": "https://registry.npmjs.org/typed-array-byte-length/-/typed-array-byte-length-1.0.3.tgz",
      "integrity": "sha512-BaXgOuIxz8n8pIq3e7Atg/7s+DpiYrxn4vdot3w9KbnBhcRQq6o3xemQdIfynqSeXeDrF32x+WvfzmOjPiY9lg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.8",
        "for-each": "^0.3.3",
        "gopd": "^1.2.0",
        "has-proto": "^1.2.0",
        "is-typed-array": "^1.1.14"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/typed-array-byte-offset": {
      "version": "1.0.4",
      "resolved": "https://registry.npmjs.org/typed-array-byte-offset/-/typed-array-byte-offset-1.0.4.tgz",
      "integrity": "sha512-bTlAFB/FBYMcuX81gbL4OcpH5PmlFHqlCCpAl8AlEzMz5k53oNDvN8p1PNOWLEmI2x4orp3raOFB51tv9X+MFQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "available-typed-arrays": "^1.0.7",
        "call-bind": "^1.0.8",
        "for-each": "^0.3.3",
        "gopd": "^1.2.0",
        "has-proto": "^1.2.0",
        "is-typed-array": "^1.1.15",
        "reflect.getprototypeof": "^1.0.9"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/typed-array-length": {
      "version": "1.0.7",
      "resolved": "https://registry.npmjs.org/typed-array-length/-/typed-array-length-1.0.7.tgz",
      "integrity": "sha512-3KS2b+kL7fsuk/eJZ7EQdnEmQoaho/r6KUef7hxvltNA5DR8NAUM+8wJMbJyZ4G9/7i3v5zPBIMN5aybAh2/Jg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bind": "^1.0.7",
        "for-each": "^0.3.3",
        "gopd": "^1.0.1",
        "is-typed-array": "^1.1.13",
        "possible-typed-array-names": "^1.0.0",
        "reflect.getprototypeof": "^1.0.6"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/typescript": {
      "version": "5.8.2",
      "resolved": "https://registry.npmjs.org/typescript/-/typescript-5.8.2.tgz",
      "integrity": "sha512-aJn6wq13/afZp/jT9QZmwEjDqqvSGp1VT5GVg+f/t6/oVyrgXM6BY1h9BRh/O5p3PlUPAe+WuiEZOmb/49RqoQ==",
      "dev": true,
      "license": "Apache-2.0",
      "bin": {
        "tsc": "bin/tsc",
        "tsserver": "bin/tsserver"
      },
      "engines": {
        "node": ">=14.17"
      }
    },
    "node_modules/unbox-primitive": {
      "version": "1.1.0",
      "resolved": "https://registry.npmjs.org/unbox-primitive/-/unbox-primitive-1.1.0.tgz",
      "integrity": "sha512-nWJ91DjeOkej/TA8pXQ3myruKpKEYgqvpw9lz4OPHj/NWFNluYrjbz9j01CJ8yKQd2g4jFoOkINCTW2I5LEEyw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.3",
        "has-bigints": "^1.0.2",
        "has-symbols": "^1.1.0",
        "which-boxed-primitive": "^1.1.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/undici-types": {
      "version": "6.20.0",
      "resolved": "https://registry.npmjs.org/undici-types/-/undici-types-6.20.0.tgz",
      "integrity": "sha512-Ny6QZ2Nju20vw1SRHe3d9jVu6gJ+4e3+MMpqu7pqE5HT6WsTSlce++GQmK5UXS8mzV8DSYHrQH+Xrf2jVcuKNg==",
      "license": "MIT"
    },
    "node_modules/unpipe": {
      "version": "1.0.0",
      "resolved": "https://registry.npmjs.org/unpipe/-/unpipe-1.0.0.tgz",
      "integrity": "sha512-pjy2bYhSsufwWlKwPc+l3cN7+wuJlK6uz0YdJEOlQDbl6jo/YlPi4mb8agUkVC8BF7V8NuzeyPNqRksA3hztKQ==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/utils-merge": {
      "version": "1.0.1",
      "resolved": "https://registry.npmjs.org/utils-merge/-/utils-merge-1.0.1.tgz",
      "integrity": "sha512-pMZTvIkT1d+TFGvDOqodOclx0QWkkgi6Tdoa8gC8ffGAAqz9pzPTZWAybbsHHoED/ztMtkv/VoYTYyShUn81hA==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.4.0"
      }
    },
    "node_modules/uuid": {
      "version": "8.3.2",
      "resolved": "https://registry.npmjs.org/uuid/-/uuid-8.3.2.tgz",
      "integrity": "sha512-+NYs2QeMWy+GWFOEm9xnn6HCDp0l7QBD7ml8zLUmJ+93Q5NF0NocErnwkTkXVFNiX3/fpC6afS8Dhb/gz7R7eg==",
      "license": "MIT",
      "bin": {
        "uuid": "dist/bin/uuid"
      }
    },
    "node_modules/validate-npm-package-license": {
      "version": "3.0.4",
      "resolved": "https://registry.npmjs.org/validate-npm-package-license/-/validate-npm-package-license-3.0.4.tgz",
      "integrity": "sha512-DpKm2Ui/xN7/HQKCtpZxoRWBhZ9Z0kqtygG8XCgNQ8ZlDnxuQmWhj566j8fN4Cu3/JmbhsDo7fcAJq4s9h27Ew==",
      "dev": true,
      "license": "Apache-2.0",
      "dependencies": {
        "spdx-correct": "^3.0.0",
        "spdx-expression-parse": "^3.0.0"
      }
    },
    "node_modules/vary": {
      "version": "1.1.2",
      "resolved": "https://registry.npmjs.org/vary/-/vary-1.1.2.tgz",
      "integrity": "sha512-BNGbWLfd0eUPabhkXUVm0j8uuvREyTh5ovRa/dyow/BqAbZJyC+5fU+IzQOzmAKzYqYRAISoRhdQr3eIZ/PXqg==",
      "license": "MIT",
      "engines": {
        "node": ">= 0.8"
      }
    },
    "node_modules/which": {
      "version": "1.3.1",
      "resolved": "https://registry.npmjs.org/which/-/which-1.3.1.tgz",
      "integrity": "sha512-HxJdYWq1MTIQbJ3nw0cqssHoTNU267KlrDuGZ1WYlxDStUtKUhOaJmh112/TZmHxxUfuJqPXSOm7tDyas0OSIQ==",
      "dev": true,
      "license": "ISC",
      "dependencies": {
        "isexe": "^2.0.0"
      },
      "bin": {
        "which": "bin/which"
      }
    },
    "node_modules/which-boxed-primitive": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/which-boxed-primitive/-/which-boxed-primitive-1.1.1.tgz",
      "integrity": "sha512-TbX3mj8n0odCBFVlY8AxkqcHASw3L60jIuF8jFP78az3C2YhmGvqbHBpAjTRH2/xqYunrJ9g1jSyjCjpoWzIAA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-bigint": "^1.1.0",
        "is-boolean-object": "^1.2.1",
        "is-number-object": "^1.1.1",
        "is-string": "^1.1.1",
        "is-symbol": "^1.1.1"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/which-builtin-type": {
      "version": "1.2.1",
      "resolved": "https://registry.npmjs.org/which-builtin-type/-/which-builtin-type-1.2.1.tgz",
      "integrity": "sha512-6iBczoX+kDQ7a3+YJBnh3T+KZRxM/iYNPXicqk66/Qfm1b93iu+yOImkg0zHbj5LNOcNv1TEADiZ0xa34B4q6Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "call-bound": "^1.0.2",
        "function.prototype.name": "^1.1.6",
        "has-tostringtag": "^1.0.2",
        "is-async-function": "^2.0.0",
        "is-date-object": "^1.1.0",
        "is-finalizationregistry": "^1.1.0",
        "is-generator-function": "^1.0.10",
        "is-regex": "^1.2.1",
        "is-weakref": "^1.0.2",
        "isarray": "^2.0.5",
        "which-boxed-primitive": "^1.1.0",
        "which-collection": "^1.0.2",
        "which-typed-array": "^1.1.16"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/which-collection": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/which-collection/-/which-collection-1.0.2.tgz",
      "integrity": "sha512-K4jVyjnBdgvc86Y6BkaLZEN933SwYOuBFkdmBu9ZfkcAbdVbpITnDmjvZ/aQjRXQrv5EPkTnD1s39GiiqbngCw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "is-map": "^2.0.3",
        "is-set": "^2.0.3",
        "is-weakmap": "^2.0.2",
        "is-weakset": "^2.0.3"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/which-typed-array": {
      "version": "1.1.18",
      "resolved": "https://registry.npmjs.org/which-typed-array/-/which-typed-array-1.1.18.tgz",
      "integrity": "sha512-qEcY+KJYlWyLH9vNbsr6/5j59AXk5ni5aakf8ldzBvGde6Iz4sxZGkJyWSAueTG7QhOvNRYb1lDdFmL5Td0QKA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "available-typed-arrays": "^1.0.7",
        "call-bind": "^1.0.8",
        "call-bound": "^1.0.3",
        "for-each": "^0.3.3",
        "gopd": "^1.2.0",
        "has-tostringtag": "^1.0.2"
      },
      "engines": {
        "node": ">= 0.4"
      },
      "funding": {
        "url": "https://github.com/sponsors/ljharb"
      }
    },
    "node_modules/wrappy": {
      "version": "1.0.2",
      "resolved": "https://registry.npmjs.org/wrappy/-/wrappy-1.0.2.tgz",
      "integrity": "sha512-l4Sp/DRseor9wL6EvV2+TuQn63dMkPjZ/sp9XkghTEbV9KlPS1xUsZ3u7/IQO4wxtcFB4bgpQPRcR3QCvezPcQ==",
      "license": "ISC"
    },
    "node_modules/yallist": {
      "version": "4.0.0",
      "resolved": "https://registry.npmjs.org/yallist/-/yallist-4.0.0.tgz",
      "integrity": "sha512-3wdGidZyq5PB084XLES5TpOSRA3wjXAlIWMhum2kRcv/41Sn2emQ0dycQW4uZXLejwKvg6EsvbdlVL+FYEct7A==",
      "license": "ISC"
    },
    "node_modules/zod": {
      "version": "3.24.2",
      "resolved": "https://registry.npmjs.org/zod/-/zod-3.24.2.tgz",
      "integrity": "sha512-lY7CDW43ECgW9u1TcT3IoXHflywfVqDYze4waEz812jR/bZ8FHDsl7pFQoSZTz5N+2NqRXs8GBwnAwo3ZNxqhQ==",
      "license": "MIT",
      "funding": {
        "url": "https://github.com/sponsors/colinhacks"
      }
    }
  }
}

```

## Samples/basic/cards/nodejs/README.md

```markdown
# Cards-Agent

This is a sample of a simple Agent that is hosted on an Node.js web service.  This Agent is configured to show how to create an agent that uses rich cards to enhance your conversation design.

## Prerequisites

- [Node.js](https://nodejs.org) version 20 or higher

    ```bash
    # determine node version
    node --version
    ```

## Running this sample

1. Open this folder from your IDE or Terminal of preference
1. Install dependencies

```bash
npm install
```

### Run in localhost, anonymous mode

1. Create the `.env` file (or rename env.TEMPLATE)

```bash
cp env.TEMPLATE .env
```

1. Start the application

```bash
npm start
```

At this point you should see the message 

```text
Server listening to port 3978 for appId debug undefined
```

The Agent is ready to accept messages.

### Interact with the Agent from the Teams App Test Tool

To interact with the agent you need a chat client, during the install phase we have acquired the `teams-test-app-tool` than can be used to interact with your agent running in `localhost:3978`

1. Start the test tool with 

```bash
npm run test-tool
```

The tool will open a web browser showing the Teams App Test Tool, ready to send messages to your agent.

Alternatively you can run the next command to start the agent and the test tool with a single command (make sure you stop the agent started previously):

```bash
npm test
```

Refresh the browser to start a new conversation with the Cards agent.

You should see a message with the list of available cards in Agents:
- Adaptive Card
- Animation Card
- Audio Card
- Hero Card
- Receipt Card
- O365 Connector Card
- Thumbnail Card
- Video Card

### Interact with the agent from WebChat UI using Azure Bot Service

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below
  
2. Configuring the token connection in the Agent settings
    1. Open the `env.TEMPLATE` file in the root of the sample project, rename it to `.env` and configure the following values:
      1. Set the **clientId** to the AppId of the agent identity.
      2. Set the **clientSecret** to the Secret that was created for your identity.
      3. Set the **tenantId** to the Tenant Id where your application is registered.

3. Install the tool [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)   
4. Run `dev tunnels`. See [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

5. Take note of the url shown after `Connect via browser:`

6. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

7. Start the Agent using `npm start`

8. Select **Test in WebChat** on the Azure portal.

### Deploy to Azure

[TBD]

## Further reading

To learn more about building  Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.

```

## Samples/basic/cards/nodejs/src/cardMessages.ts

```typescript
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import {  ActionTypes, Activity, ActivityTypes, Attachment } from '@microsoft/agents-activity'
import { CardFactory, TurnContext } from '@microsoft/agents-hosting'

export class CardMessages {
  static async sendIntroCard (context: TurnContext): Promise<void> {
    // Note that some channels require different values to be used in order to get buttons to display text.
    // In this code the web chat is accounted for with the 'title' parameter, but in other channels you may
    // need to provide a value for other parameters like 'text' or 'displayText'.
    const buttons = [
      { type: ActionTypes.ImBack, title: '1. Adaptive Card', value: '1. Adaptive Card' },
      { type: ActionTypes.ImBack, title: '2. Animation Card', value: '2. Animation Card' },
      { type: ActionTypes.ImBack, title: '3. Audio Card', value: '3. Audio Card' },
      { type: ActionTypes.ImBack, title: '4. Hero Card', value: '4. Hero Card' },
      { type: ActionTypes.ImBack, title: '5. Receipt Card', value: '5. Receipt Card' },
      { type: ActionTypes.ImBack, title: '6. Thumbnail Card', value: '6. Thumbnail Card' },
      { type: ActionTypes.ImBack, title: '7. Video Card', value: '7. Video Card' },
    ]

    const card = CardFactory.heroCard('', undefined,
      buttons, { text: 'Select one of the following choices' })

    await CardMessages.sendActivity(context, card)
  }

  static async sendAdaptiveCard (context: TurnContext, adaptiveCard: any): Promise<void> {
    const card = CardFactory.adaptiveCard(adaptiveCard)

    await CardMessages.sendActivity(context, card)
  }

  static async sendAnimationCard (context: TurnContext): Promise<void> {
    const card = CardFactory.animationCard(
      'Microsoft Bot Framework',
      [
        { url: 'https://i.giphy.com/Ki55RUbOV5njy.gif' }
      ],
      [],
      {
        subtitle: 'Animation Card'
      }
    )

    await CardMessages.sendActivity(context, card)
  }

  static async sendAudioCard (context: TurnContext): Promise<void> {
    const card = CardFactory.audioCard(
      'I am your father',
      ['https://www.mediacollege.com/downloads/sound-effects/star-wars/darthvader/darthvader_yourfather.wav'],
      CardFactory.actions([
        {
          type: ActionTypes.OpenUrl,
          title: 'Read more',
          value: 'https://en.wikipedia.org/wiki/The_Empire_Strikes_Back'
        }
      ]),
      {
        subtitle: 'Star Wars: Episode V - The Empire Strikes Back',
        text: 'The Empire Strikes Back (also known as Star Wars: Episode V – The Empire Strikes Back) is a 1980 American epic space opera film directed by Irvin Kershner. Leigh Brackett and Lawrence Kasdan wrote the screenplay, with George Lucas writing the film\'s story and serving as executive producer. The second installment in the original Star Wars trilogy, it was produced by Gary Kurtz for Lucasfilm Ltd. and stars Mark Hamill, Harrison Ford, Carrie Fisher, Billy Dee Williams, Anthony Daniels, David Prowse, Kenny Baker, Peter Mayhew and Frank Oz.',
        image: { url: 'https://upload.wikimedia.org/wikipedia/en/3/3c/SW_-_Empire_Strikes_Back.jpg' }
      }
    )

    await CardMessages.sendActivity(context, card)
  }

  static async sendHeroCard (context: TurnContext): Promise<void> {
    const card = CardFactory.heroCard(
      'Copilot Hero Card',
      CardFactory.images(['https://blogs.microsoft.com/wp-content/uploads/prod/2023/09/Press-Image_FINAL_16x9-4.jpg']),
      CardFactory.actions([
        {
          type: ActionTypes.OpenUrl,
          title: 'Get started',
          value: 'https://docs.microsoft.com/en-us/azure/bot-service/'
        }
      ])
    )

    await CardMessages.sendActivity(context, card)
  }

  static async sendReceiptCard (context: TurnContext): Promise<void> {
    const card = CardFactory.receiptCard({
      title: 'John Doe',
      facts: [
        {
          key: 'Order Number',
          value: '1234'
        },
        {
          key: 'Payment Method',
          value: 'VISA 5555-****'
        }
      ],
      items: [
        {
          title: 'Data Transfer',
          price: '$38.45',
          quantity: 368,
          image: { url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/traffic-manager.png' }
        },
        {
          title: 'App Service',
          price: '$45.00',
          quantity: 720,
          image: { url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/cloud-service.png' }
        }
      ],
      tax: '$7.50',
      total: '$90.95',
      buttons: CardFactory.actions([
        {
          type: ActionTypes.OpenUrl,
          title: 'More information',
          value: 'https://azure.microsoft.com/en-us/pricing/details/bot-service/'
        }
      ])
    })

    await CardMessages.sendActivity(context, card)
  }

  static async sendThumbnailCard (context: TurnContext) {
    const card = CardFactory.thumbnailCard(
      'Copilot Thumbnail Card',
      [{ url: 'https://blogs.microsoft.com/wp-content/uploads/prod/2023/09/Press-Image_FINAL_16x9-4.jpg' }],
      [{
        type: ActionTypes.OpenUrl,
        title: 'Get started',
        value: 'https://docs.microsoft.com/en-us/azure/bot-service/'
      }],
      {
        subtitle: 'Your bots — wherever your users are talking.',
        text: 'Build and connect intelligent bots to interact with your users naturally wherever they are, from text/sms to Skype, Slack, Office 365 mail and other popular services.'
      }
    )

    await CardMessages.sendActivity(context, card)
  }

  static async sendVideoCard (context: TurnContext) {
    const card = CardFactory.videoCard(
      '2018 Imagine Cup World Championship Intro',
      [{ url: 'https://sec.ch9.ms/ch9/783d/d57287a5-185f-4df9-aa08-fcab699a783d/IC18WorldChampionshipIntro2.mp4' }],
      [{
        type: ActionTypes.OpenUrl,
        title: 'Lean More',
        value: 'https://channel9.msdn.com/Events/Imagine-Cup/World-Finals-2018/2018-Imagine-Cup-World-Championship-Intro'
      }],
      {
        subtitle: 'by Microsoft',
        text: 'Microsoft\'s Imagine Cup has empowered student developers around the world to create and innovate on the world stage for the past 16 years. These innovations will shape how we live, work and play.'
      }
    )

    await CardMessages.sendActivity(context, card)
  }

  private static async sendActivity (context: TurnContext, card: Attachment): Promise<void> {
    await context.sendActivity(Activity.fromObject(
      {
        type: ActivityTypes.Message,
        attachments: [card]
      }
    )
    )
  }
}

```

## Samples/basic/cards/nodejs/src/agent.ts

```typescript
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import { Activity, ActivityTypes } from '@microsoft/agents-activity'
import { ActivityHandler } from '@microsoft/agents-hosting'
import { CardMessages } from './cardMessages'
import AdaptiveCard from './resources/adaptiveCard.json'

export class CardSampleAgent extends ActivityHandler {
  constructor () {
    super()

    this.onMembersAdded(async (context, next) => {
      const membersAdded = context.activity.membersAdded
      for (let cnt = 0; cnt < membersAdded!.length; cnt++) {
        if ((context.activity.recipient != null) && membersAdded![cnt].id !== context.activity.recipient.id) {
          await CardMessages.sendIntroCard(context)

          await next()
        }
      }
    })

    this.onMessage(async (context, next) => {
      if (context.activity.text !== undefined) {
        switch (context.activity.text.split('.')[0].toLowerCase()) {
          case 'display cards options':
            await CardMessages.sendIntroCard(context)
            break
          case '1':
            await CardMessages.sendAdaptiveCard(context, AdaptiveCard)
            break
          case '2':
            await CardMessages.sendAnimationCard(context)
            break
          case '3':
            await CardMessages.sendAudioCard(context)
            break
          case '4':
            await CardMessages.sendHeroCard(context)
            break
          case '5':
            await CardMessages.sendReceiptCard(context)
            break
          case '6':
            await CardMessages.sendThumbnailCard(context)
            break
          case '7':
            await CardMessages.sendVideoCard(context)
            break
          default: {
            const reply: Activity = Activity.fromObject(
              {
                type: ActivityTypes.Message,
                text: 'Your input was not recognized, please try again.'
              }
            )
            await context.sendActivity(reply)
            await CardMessages.sendIntroCard(context)
          }
        }
      } else {
        await context.sendActivity('This sample is only for testing Cards using CardFactory methods. Please refer to other samples to test out more functionalities.')
      }

      await next()
    })
  }
}

```

## Samples/basic/cards/nodejs/src/index.ts

```typescript
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import express, { Response } from 'express'
import { Request, CloudAdapter, authorizeJWT, AuthConfiguration, loadAuthConfigFromEnv } from '@microsoft/agents-hosting'
import { CardSampleAgent } from './agent'

const authConfig: AuthConfiguration = loadAuthConfigFromEnv()

const adapter = new CloudAdapter(authConfig)
const myBot = new CardSampleAgent()

const app = express()
app.use(express.json())
app.use(authorizeJWT(authConfig))

app.post('/api/messages', async (req: Request, res: Response) => {
  await adapter.process(req, res, async (context) => await myBot.run(context))
})

const port = process.env.PORT || 3978
app.listen(port, () => {
  console.log(`\nServer listening to port ${port} for appId ${authConfig.clientId}`)
}).on('error', (err) => {
  console.error(err)
  process.exit(1)
})

```

## Samples/basic/cards/nodejs/src/resources/adaptiveCard.json

```json
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "version": "1.0",
    "type": "AdaptiveCard",
    "speak": "Your flight is confirmed for you and 3 other passengers from San Francisco to Amsterdam on Friday, October 10 8:30 AM",
    "body": [
      {
        "type": "TextBlock",
        "text": "Passengers",
        "weight": "bolder",
        "isSubtle": false
      },
      {
        "type": "TextBlock",
        "text": "Sarah Hum",
        "separator": true
      },
      {
        "type": "TextBlock",
        "text": "Jeremy Goldberg",
        "spacing": "none"
      },
      {
        "type": "TextBlock",
        "text": "Evan Litvak",
        "spacing": "none"
      },
      {
        "type": "TextBlock",
        "text": "2 Stops",
        "weight": "bolder",
        "spacing": "medium"
      },
      {
        "type": "TextBlock",
        "text": "Fri, October 10 8:30 AM",
        "weight": "bolder",
        "spacing": "none"
      },
      {
        "type": "ColumnSet",
        "separator": true,
        "columns": [
          {
            "type": "Column",
            "width": 1,
            "items": [
              {
                "type": "TextBlock",
                "text": "San Francisco",
                "isSubtle": true
              },
              {
                "type": "TextBlock",
                "size": "extraLarge",
                "color": "accent",
                "text": "SFO",
                "spacing": "none"
              }
            ]
          },
          {
            "type": "Column",
            "width": "auto",
            "items": [
              {
                "type": "TextBlock",
                "text": " "
              },
              {
                "type": "Image",
                "url": "http://adaptivecards.io/content/airplane.png",
                "size": "small",
                "spacing": "none"
              }
            ]
          },
          {
            "type": "Column",
            "width": 1,
            "items": [
              {
                "type": "TextBlock",
                "horizontalAlignment": "right",
                "text": "Amsterdam",
                "isSubtle": true
              },
              {
                "type": "TextBlock",
                "horizontalAlignment": "right",
                "size": "extraLarge",
                "color": "accent",
                "text": "AMS",
                "spacing": "none"
              }
            ]
          }
        ]
      },
      {
        "type": "TextBlock",
        "text": "Non-Stop",
        "weight": "bolder",
        "spacing": "medium"
      },
      {
        "type": "TextBlock",
        "text": "Fri, October 18 9:50 PM",
        "weight": "bolder",
        "spacing": "none"
      },
      {
        "type": "ColumnSet",
        "separator": true,
        "columns": [
          {
            "type": "Column",
            "width": 1,
            "items": [
              {
                "type": "TextBlock",
                "text": "Amsterdam",
                "isSubtle": true
              },
              {
                "type": "TextBlock",
                "size": "extraLarge",
                "color": "accent",
                "text": "AMS",
                "spacing": "none"
              }
            ]
          },
          {
            "type": "Column",
            "width": "auto",
            "items": [
              {
                "type": "TextBlock",
                "text": " "
              },
              {
                "type": "Image",
                "url": "http://adaptivecards.io/content/airplane.png",
                "size": "small",
                "spacing": "none"
              }
            ]
          },
          {
            "type": "Column",
            "width": 1,
            "items": [
              {
                "type": "TextBlock",
                "horizontalAlignment": "right",
                "text": "San Francisco",
                "isSubtle": true
              },
              {
                "type": "TextBlock",
                "horizontalAlignment": "right",
                "size": "extraLarge",
                "color": "accent",
                "text": "SFO",
                "spacing": "none"
              }
            ]
          }
        ]
      },
      {
        "type": "ColumnSet",
        "spacing": "medium",
        "columns": [
          {
            "type": "Column",
            "width": "1",
            "items": [
              {
                "type": "TextBlock",
                "text": "Total",
                "size": "medium",
                "isSubtle": true
              }
            ]
          },
          {
            "type": "Column",
            "width": 1,
            "items": [
              {
                "type": "TextBlock",
                "horizontalAlignment": "right",
                "text": "$4,032.54",
                "size": "medium",
                "weight": "bolder"
              }
            ]
          }
        ]
      }
    ]
  }
  
```

## Samples/basic/empty-agent/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmptyAgent;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Logging.AddConsole();

// Add AgentApplicationOptions from config.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<MyAgent>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/", () => "Microsoft Agents SDK Sample");
app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
})
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");

app.Run();

```

## Samples/basic/empty-agent/dotnet/appsettings.json

```json
{
  "AgentApplication": {
    "StartTypingTimer": false,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Samples/basic/empty-agent/dotnet/EmptyAgent.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
		<ImplicitUsings>disable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<Compile Remove="wwwroot\**" />
		<Content Remove="wwwroot\**" />
		<EmbeddedResource Remove="wwwroot\**" />
		<None Remove="wwwroot\**" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>

```

## Samples/basic/empty-agent/dotnet/MyAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using System.Threading.Tasks;
using System.Threading;

namespace EmptyAgent;

public class MyAgent : AgentApplication
{
    public MyAgent(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);
    }

    private async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Hello and Welcome!"), cancellationToken);
            }
        }
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync($"You said: {turnContext.Activity.Text}", cancellationToken: cancellationToken);
    }
}

```

## Samples/basic/empty-agent/dotnet/README.md

```markdown
﻿# EmptyAgent Sample

This is a sample of a simple Agent that is hosted on an Asp.net core web service.  This Agent is configured to accept a request and echo the text of the request back to the caller.

This Agent Sample is intended to introduce you the basic operation of the Microsoft 365 Agents SDK messaging loop. It can also be used as a the base for a custom Agent that you choose to develop.

## Prerequisites

- [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Running this sample

**To run the sample connected to Azure Bot Service, the following additional tools are required:**

- Access to an Azure Subscription with access to preform the following tasks:
    - Create and configure Entra ID Application Identities
    - Create and configure an [Azure Bot Service](https://aka.ms/AgentsSDK-CreateBot) for your Azure Bot.
    - Create and configure an [Azure App Service](https://learn.microsoft.com/azure/app-service/) to deploy your Agent to.
    - A tunneling tool to allow for local development and debugging should you wish to do local development whilst connected to a external client such as Microsoft Teams.

## Getting Started with EmptyAgent Sample

Read more about [Running an Agent](../../../docs/HowTo/running-an-agent.md)

### QuickStart using WebChat

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL Authentication provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "Connections": {
        "ServiceConnection": {
          "Settings": {
            "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
            "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
            "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
            "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
            "Scopes": [
              "https://api.botframework.com/.default"
            ]
          }
        }
      },
      ```

      1. Replace all **{{ClientId}}** with the AppId of the Azure Bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent in Visual Studio

1. Select **Test in WebChat** on the Azure Bot

## Further reading
To learn more about building Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.
```

## Samples/basic/empty-agent/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.emptyagent",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Empty Agent",
    "full": "Empty Agent Example"
  },
  "description": {
    "short": "Sample demonstrating an empty agent using Agents SDK.",
    "full": "This sample demonstrates how echo text sent back to a client"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/empty-agent/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.emptyagent",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Empty Agent",
    "full": "Empty Agent Example"
  },
  "description": {
    "short": "Sample demonstrating an empty agent using Agents SDK.",
    "full": "This sample demonstrates how echo text sent back to a client"
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal",
        "groupChat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/empty-agent/dotnet/appManifest/.gitignore

```ignore
manifest.json
*.zip
```

## Samples/basic/empty-agent/nodejs/.npmrc

```
package-lock=false
```

## Samples/basic/empty-agent/nodejs/tsconfig.json

```json
{
  "compilerOptions": {
      "incremental": true,
      "lib": ["ES2021"],
      "target": "es2019",
      "module": "commonjs",
      "declaration": true,
      "sourceMap": true,
      "composite": true,
      "strict": true,
      "moduleResolution": "node",
      "esModuleInterop": true,
      "skipLibCheck": true,
      "forceConsistentCasingInFileNames": true,
      "resolveJsonModule": true,
      "rootDir": "src",
      "outDir": "dist",
      "tsBuildInfoFile": "dist/.tsbuildinfo"
  }
}

```

## Samples/basic/empty-agent/nodejs/package.json

```json
{
  "name": "node-empty-agent",
  "version": "1.0.0",
  "private": true,
  "description": "Agents echo bot sample",
  "author": "Microsoft",
  "license": "MIT",
  "main": "./dist/index.js",
  "scripts": {
    "prebuild": "npm install",
    "build": "tsc --build",
    "prestart": "npm run build",
    "prestart:anon": "npm run build",
    "start:anon": "node ./dist/index.js",
    "start": "node --env-file .env ./dist/index.js",
    "test-tool": "teamsapptester start",
    "test": "npm-run-all -p -r start:anon test-tool"
  },
  "dependencies": {
    "@microsoft/agents-hosting": "^0.4.0",
    "express": "^5.1.0"
  },
  "devDependencies": {
    "@microsoft/teams-app-test-tool": "^0.2.10",
    "@types/node": "^22.15.18",
    "npm-run-all": "^4.1.5",
    "typescript": "^5.8.3"
  },
  "keywords": []
}

```

## Samples/basic/empty-agent/nodejs/env.TEMPLATE

```
# rename to .env
tenantId=
clientId=
clientSecret=
DEBUG=agents:*:error
```

## Samples/basic/empty-agent/nodejs/README.md

```markdown
# Empty-Agent

This is a sample of a simple Agent that is hosted on an Node.js web service.  This Agent is configured to accept a request and echo the text of the request back to the caller.

This Agent Sample is intended to introduce you to the basic operation of the Microsoft 365 Agents SDK messaging loop. It can also be used as the base for a custom Agent you choose to develop.

## Prerequisites

- [Node.js](https://nodejs.org) version 20 or higher

    ```bash
    # determine node version
    node --version
    ```


## Running this sample

1. Open this folder from your IDE or Terminal of preference
1. Install dependencies

```sh
npm install
```

### Run in localhost, anonymous mode

1. Start the application

```sh
npm run build
npm run start:anon
```

At this point you should see the message 

```text
Server listening to port 3978 for appId  debug undefined
```

The agent is ready to accept messages.

### Interact with the bot from the Teams App Test Tool

To interact with the bot you need a chat client, during the install phase we have acquired the `teams-test-app-tool` than can be used to interact with your bot running in `localhost:3978`

> [!Important]
> The test tool only supports anonymous mode, that means without any `clientId`.

1. Start the test tool with 

```bash
npm run test-tool
```

The tool will open a web browser showing the Teams App Test Tool, ready to send messages to your bot.

Alternatively you can run the next command to start the bot and the test tool with a single command (make sure you stop the bot started previously):

```bash
npm test
```

Refresh the browser to start a new conversation with the Echo bot.

You should see a message from the bot like: `Echo running on Agents SDK version: {version}`


### Interact with the agent with WebChat UI using Azure Bot Service

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below
  
1. Configuring the token connection in the Agent settings
    1. Open the `env.TEMPLATE` file in the root of the sample project, rename it to `.env` and configure the following values:
      1. Set the **clientId** to the AppId of the bot identity.
      2. Set the **clientSecret** to the Secret that was created for your identity. *This is the `Secret Value` shown in the AppRegistration*.
      3. Set the **tenantId** to the Tenant Id where your application is registered.

1. Install the tool [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)   
1. Run `dev tunnels`. See [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. Take note of the url shown after `Connect via browser:`

4. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

5. Start the Agent using `npm start`

6. Select **Test in WebChat** on the Azure portal.


### Deploy to Azure

[TBD]


## Further reading

To learn more about building Bots and Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.

```

## Samples/basic/empty-agent/nodejs/src/agent.ts

```typescript
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import { TurnState, MemoryStorage, TurnContext, AgentApplication, AttachmentDownloader }
  from '@microsoft/agents-hosting'
import { version } from '@microsoft/agents-hosting/package.json'
import { ActivityTypes } from '@microsoft/agents-activity'

interface ConversationState {
  count: number;
}
type ApplicationTurnState = TurnState<ConversationState>

const downloader = new AttachmentDownloader()

// Define storage and application
const storage = new MemoryStorage()
export const agentApp = new AgentApplication<ApplicationTurnState>({
  storage,
  fileDownloaders: [downloader]
})

// Listen for user to say '/reset' and then delete conversation state
agentApp.onMessage('/reset', async (context: TurnContext, state: ApplicationTurnState) => {
  state.deleteConversationState()
  await context.sendActivity('Ok I\'ve deleted the current conversation state.')
})

agentApp.onMessage('/count', async (context: TurnContext, state: ApplicationTurnState) => {
  const count = state.conversation.count ?? 0
  await context.sendActivity(`The count is ${count}`)
})

agentApp.onMessage('/diag', async (context: TurnContext, state: ApplicationTurnState) => {
  await state.load(context, storage)
  await context.sendActivity(JSON.stringify(context.activity))
})

agentApp.onMessage('/state', async (context: TurnContext, state: ApplicationTurnState) => {
  await state.load(context, storage)
  await context.sendActivity(JSON.stringify(state))
})

agentApp.onMessage('/runtime', async (context: TurnContext, state: ApplicationTurnState) => {
  const runtime = {
    nodeversion: process.version,
    sdkversion: version
  }
  await context.sendActivity(JSON.stringify(runtime))
})

agentApp.onConversationUpdate('membersAdded', async (context: TurnContext, state: ApplicationTurnState) => {
  await context.sendActivity('🚀 Echo bot running on Agents SDK version: ' + version)
})

// Listen for ANY message to be received. MUST BE AFTER ANY OTHER MESSAGE HANDLERS
agentApp.onActivity(ActivityTypes.Message, async (context: TurnContext, state: ApplicationTurnState) => {
  // Increment count state
  let count = state.conversation.count ?? 0
  state.conversation.count = ++count

  // Echo back users request
  await context.sendActivity(`[${count}] you said: ${context.activity.text}`)
})

agentApp.onActivity(/^message/, async (context: TurnContext, state: ApplicationTurnState) => {
  await context.sendActivity(`Matched with regex: ${context.activity.type}`)
})

agentApp.onActivity(
  async (context: TurnContext) => Promise.resolve(context.activity.type === 'message'),
  async (context, state) => {
    await context.sendActivity(`Matched function: ${context.activity.type}`)
  }
)

```

## Samples/basic/empty-agent/nodejs/src/index.ts

```typescript
import { AuthConfiguration, authorizeJWT, CloudAdapter, loadAuthConfigFromEnv, Request } from '@microsoft/agents-hosting'
import express, { Response } from 'express'
import { agentApp } from './agent'

const authConfig: AuthConfiguration = loadAuthConfigFromEnv()
const adapter = new CloudAdapter(authConfig)

const server = express()
server.use(express.json())
server.use(authorizeJWT(authConfig))

server.post('/api/messages', async (req: Request, res: Response) => {
  await adapter.process(req, res, async (context) => {
    const app = agentApp
    await app.run(context)
  })
})

const port = process.env.PORT || 3978
server.listen(port, () => {
  console.log(`\nServer listening to port ${port} for appId ${authConfig.clientId} debug ${process.env.DEBUG}`)
}).on('error', (err) => {
  console.error(err)
  process.exit(1)
})

```

## Samples/basic/azureai-streaming-poem-agent/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using StreamingMessageAgent;
using System;
using System.ClientModel;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Logging.AddConsole();

builder.Services.AddTransient<ChatClient>(sp =>
{
    return new AzureOpenAIClient(
            new Uri(builder.Configuration["AIServices:AzureOpenAI:Endpoint"]),
            new ApiKeyCredential(builder.Configuration["AIServices:AzureOpenAI:ApiKey"]))
    .GetChatClient(builder.Configuration["AIServices:AzureOpenAI:DeploymentName"]);
});

// Add AgentApplicationOptions.  This will use DI'd services and IConfiguration for construction.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<StreamingAgent>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
})
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");
app.MapGet("/", () => "Microsoft Agents SDK Sample");

app.Run();
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/StreamingAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingMessageAgent;

public class StreamingAgent : AgentApplication
{
    private ChatClient _chatClient;

    /// <summary>
    /// Example of a streaming response agent using the Azure OpenAI ChatClient.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="chatClient"></param>
    public StreamingAgent(AgentApplicationOptions options, ChatClient chatClient) : base(options)
    {
        _chatClient = chatClient;

        // Register an event to welcome new channel members.
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        // Register an event to handle messages from the client.
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);
    }

    /// <summary>
    /// Send a welcome message to the user when they join the conversation.
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="turnState"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Say anything and I'll recite poetry."), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handle Messages events from clients. 
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="turnState"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        try
        {
            // Raise an informative update to the calling client,  if the client support StreamingResponses this will apper as a contextual notification. 
            await turnContext.StreamingResponse.QueueInformativeUpdateAsync("Hold on for an awesome poem about Apollo...", cancellationToken);

            // Setup system messages and the user request,
            // Normally we would use the turnState to manage this list in context of the conversation and add to it as the conversation proceeded 
            // And Normally the user Chat Message would be provided by the incoming message,
            // However for our purposes we are hardcoding the UserMessage. 
            List<ChatMessage> messages =
            [
                new SystemChatMessage("""
                    You are a creative assistant who has deeply studied Greek and Roman Gods, You also know all of the Percy Jackson Series
                    You write poems about the Greek Gods as they are depicted in the Percy Jackson books.
                    You format the poems in a way that is easy to read and understand
                    You break your poems into stanzas 
                    You format your poems in Markdown using double lines to separate stanzas
                    """),

                new UserChatMessage("Write a poem about the Greek God Apollo as depicted in the Percy Jackson books"),
            ];

            // Requesting the connected LLM Model to do work :) 
            await foreach (StreamingChatCompletionUpdate update in _chatClient.CompleteChatStreamingAsync(
                messages,
                new ChatCompletionOptions { MaxOutputTokenCount = 1000 },
                cancellationToken: cancellationToken))
            {
                if (update.ContentUpdate.Count > 0)
                {
                    if (!string.IsNullOrEmpty(update.ContentUpdate[0]?.Text))
                        turnContext.StreamingResponse.QueueTextChunk(update.ContentUpdate[0]?.Text);
                }
            }
        }
        finally
        {
            // Signal that your done with this stream. 
            await turnContext.StreamingResponse.EndStreamAsync(cancellationToken);
        }
    }
}
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/StreamingMessageAgent.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
		<ImplicitUsings>disable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.1.0-preview.1.25064.3" />
		<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0"/>
		<PackageReference Include="Azure.Identity" Version="1.13.2" />
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>

```

## Samples/basic/azureai-streaming-poem-agent/dotnet/appsettings.json

```json
{
  "AgentApplicationOptions": {
    "StartTypingTimer": false,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false
  },

  "AIServices": {
    "AzureOpenAI": {
      "DeploymentName": "", // This is the Deployment (as opposed to model) Name of the Azure OpenAI model
      "Endpoint": "", // This is the Endpoint of the Azure OpenAI model deployment
      "ApiKey": "" // This is the API Key of the Azure OpenAI model deployment
    }
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/README.md

```markdown
﻿# StreamingMessage Sample

This is a sample of a simple Agent that is hosted on an Asp.net core web service.  This Agent is demonstrate the streaming OpenAI streamed responses.

## Prerequisites

- [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases) for Testing Web Chat.

## Running this sample

**To run the sample connected to Azure Bot Service, the following additional tools are required:**

- Access to an Azure Subscription with access to preform the following tasks:
    - Create and configure Entra ID Application Identities
    - Create and configure an [Azure Bot Service](https://aka.ms/AgentsSDK-CreateBot) for your Azure Bot.
    - Create and configure an [Azure App Service](https://learn.microsoft.com/azure/app-service/) to deploy your Agent to.
    - A tunneling tool to allow for local development and debugging should you wish to do local development whilst connected to a external client such as Microsoft Teams.

## Getting Started with StreamingMessage Sample

Read more about [Running an Agent](../../../docs/HowTo/running-an-agent.md)

### QuickStart using Bot Framework Emulator

1. Open the StreamingMessageAgent Sample in Visual Studio 2022
1. Run it in Debug Mode (F5)
1. A blank web page will open, note down the URL which should be similar too `http://localhost:3978/`
1. Open the [BotFramework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
    1. Click **Open Bot**
    1. In the bot URL field input the URL you noted down from the web page and add /api/messages to it. It should appear similar to `http://localhost:3978/api/messages`
    1. Click **Connect**

If all is working correctly, the Bot Emulator should show you a Web Chat experience with the words **"Say anything and I'll recite poetry.!"**

### QuickStart using WebChat

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL Authentication provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "TokenValidation": {
        "Audiences": [
          "{{ClientId}}" // this is the Client ID used for the Azure Bot
        ],
        "TenantId":  "{{TenantId}}"
      },

      "Connections": {
        "ServiceConnection": {
          "Settings": {
            "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
            "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
            "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
            "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
            "Scopes": [
              "https://api.botframework.com/.default"
            ]
          }
        }
      },
      ```

      1. Replace all **{{ClientId}}** with the AppId of the Azure Bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.

1. Configure Azure OpenAI settings
   ```json
   "AzureOpenAI": {
     "Endpoint": "",
     "ModelName": null,
     "OpenAIKey": null
   },
   ```
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:
   > NOTE: Go to your project directory and open the `./Properties/launchSettings.json` file. Check the port number and use that port number in the devtunnel command (instead of 3978).

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent in Visual Studio

1. Select **Test in WebChat** on the Azure Bot

## Further reading
To learn more about building Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.streamingagent",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Streaming Agent",
    "full": "Streaming Agent Example"
  },
  "description": {
    "short": "Sample demonstrating a streaming agent using Agents SDK.",
    "full": "This sample demonstrates streaming OpenAI responses back to a client"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.streamingagent",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Streaming Agent",
    "full": "Streaming Agent Example"
  },
  "description": {
    "short": "Sample demonstrating a streaming agent using Agents SDK.",
    "full": "This sample demonstrates streaming OpenAI responses back to a client"
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal",
        "groupChat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/azureai-streaming-poem-agent/dotnet/appManifest/.gitignore

```ignore
manifest.json
*.zip
```

## Samples/basic/weather-agent/dotnet/Program.cs

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.Threading;
using WeatherAgent;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Register Semantic Kernel
builder.Services.AddKernel();

// Register the AI service of your choice. AzureOpenAI and OpenAI are demonstrated...
if (builder.Configuration.GetSection("AIServices").GetValue<bool>("UseAzureOpenAI"))
{
    builder.Services.AddAzureOpenAIChatCompletion(
        deploymentName: builder.Configuration.GetSection("AIServices:AzureOpenAI").GetValue<string>("DeploymentName"),
        endpoint: builder.Configuration.GetSection("AIServices:AzureOpenAI").GetValue<string>("Endpoint"),
        apiKey: builder.Configuration.GetSection("AIServices:AzureOpenAI").GetValue<string>("ApiKey"));

    //Use the Azure CLI (for local) or Managed Identity (for Azure running app) to authenticate to the Azure OpenAI service
    //credentials: new ChainedTokenCredential(
    //   new AzureCliCredential(),
    //   new ManagedIdentityCredential()
    //));
}
else
{
    builder.Services.AddOpenAIChatCompletion(
        modelId: builder.Configuration.GetSection("AIServices:OpenAI").GetValue<string>("ModelId"),
        apiKey: builder.Configuration.GetSection("AIServices:OpenAI").GetValue<string>("ApiKey"));
}

// Add AgentApplicationOptions from appsettings config.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<MyAgent>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
})
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");


app.Run();

```

## Samples/basic/weather-agent/dotnet/appsettings.json

```json
{
  "AgentApplication": {
    "StartTypingTimer": true,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },

  // This is the configuration for the AI services, use environment variables or user secrets to store sensitive information.
  // Do not store sensitive information in this file
  "AIServices": {
    "AzureOpenAI": {
      "DeploymentName": "", // This is the Deployment (as opposed to model) Name of the Azure OpenAI model
      "Endpoint": "", // This is the Endpoint of the Azure OpenAI model deployment
      "ApiKey": "" // This is the API Key of the Azure OpenAI model deployment
    },
    "OpenAI": {
      "ModelId": "", // This is the Model ID of the OpenAI model
      "ApiKey": "" // This is the API Key of the OpenAI model
    },
    "UseAzureOpenAI": true // This is a flag to determine whether to use the Azure OpenAI model or the OpenAI model
  }
}
```

## Samples/basic/weather-agent/dotnet/MyAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherAgent.Agents;

namespace WeatherAgent;

public class MyAgent : AgentApplication
{
    private WeatherForecastAgent _weatherAgent;
    private Kernel _kernel;

    public MyAgent(AgentApplicationOptions options, Kernel kernel) : base(options)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        OnActivity(ActivityTypes.Message, MessageActivityAsync, rank: RouteRank.Last);
    }

    protected async Task MessageActivityAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        // Setup local service connection
        ServiceCollection serviceCollection = [
            new ServiceDescriptor(typeof(ITurnState), turnState),
            new ServiceDescriptor(typeof(ITurnContext), turnContext),
            new ServiceDescriptor(typeof(Kernel), _kernel),
        ];

        // Start a Streaming Process 
        await turnContext.StreamingResponse.QueueInformativeUpdateAsync("Working on a response for you"); 

        ChatHistory chatHistory = turnState.GetValue("conversation.chatHistory", () => new ChatHistory());
        _weatherAgent = new WeatherForecastAgent(_kernel, serviceCollection.BuildServiceProvider());

        // Invoke the WeatherForecastAgent to process the message
        WeatherForecastAgentResponse forecastResponse = await _weatherAgent.InvokeAgentAsync(turnContext.Activity.Text, chatHistory);
        if (forecastResponse == null)
        {
            turnContext.StreamingResponse.QueueTextChunk("Sorry, I couldn't get the weather forecast at the moment.");
            await turnContext.StreamingResponse.EndStreamAsync(cancellationToken); 
            return;
        }

        // Create a response message based on the response content type from the WeatherForecastAgent
        // Send the response message back to the user. 
        switch (forecastResponse.ContentType)
        {
            case WeatherForecastAgentResponseContentType.Text:
                turnContext.StreamingResponse.QueueTextChunk(forecastResponse.Content);
                break;
            case WeatherForecastAgentResponseContentType.AdaptiveCard:
                turnContext.StreamingResponse.FinalMessage = MessageFactory.Attachment(new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = forecastResponse.Content,
                });
                break;
            default:
                break;
        }
        await turnContext.StreamingResponse.EndStreamAsync(cancellationToken); // End the streaming response
    }

    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Hello and Welcome! I'm here to help with all your weather forecast needs!"), cancellationToken);
            }
        }
    }
}
```

## Samples/basic/weather-agent/dotnet/README.md

```markdown
﻿# WeatherBot Sample with Semantic Kernel

This is a sample of a simple Weather Forecast Agent that is hosted on an Asp.net core web service.  This Agent is configured to accept a request asking for information about a weather forecast and respond to the caller with an Adaptive Card.

This Agent Sample is intended to introduce you the basics of integrating Semantic Kernel with the Microsoft 365 Agents SDK in order to build powerful Agents. It can also be used as a the base for a custom Agent that you choose to develop.

***Note:*** This sample requires JSON output from the model which works best from newer versions of the model such as gpt-4o-mini.

## Prerequisites

- [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Running this sample

**To run the sample connected to Azure Bot Service, the following additional tools are required:**

- Access to an Azure Subscription with access to preform the following tasks:
    - Create and configure Entra ID Application Identities
    - Create and configure an [Azure Bot Service](https://aka.ms/AgentsSDK-CreateBot) for your bot
    - Create and configure an [Azure App Service](https://learn.microsoft.com/azure/app-service/) to deploy your bot on to.
    - A tunneling tool to allow for local development and debugging should you wish to do local development whilst connected to a external client such as Microsoft Teams.

## Getting Started with WeatherBot Sample

Read more about [Running an Agent](../../../docs/HowTo/running-an-agent.md)

### QuickStart using WebChat

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. You will need an Azure OpenAI or OpenAI instance, with the preferred model of `gpt-4o-mini`.

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "Connections": {
        "ServiceConnection": {
          "Settings": {
              "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
              "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
              "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
              "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
              "Scopes": [
                "https://api.botframework.com/.default"
              ]
          }
        }
      },
 
      // This is the configuration for the AI services, use environeent variables or user secrets to store sensitive information.
      // Do not store sensitive information in this file
      "AIServices": {
        "AzureOpenAI": {
          "DeploymentName": "", // This is the Deployment (as opposed to model) Name of the Azure OpenAI model
          "Endpoint": "", // This is the Endpoint of the Azure OpenAI model deployment
          "ApiKey": "" // This is the API Key of the Azure OpenAI model deployment
        },
        "OpenAI": {
          "ModelId": "", // This is the Model ID of the OpenAI model
          "ApiKey": "" // This is the API Key of the OpenAI model
        },
        "UseAzureOpenAI": true // This is a flag to determine whether to use the Azure OpenAI model or the OpenAI model  
      }
      ```

      1. Replace all **{{ClientId}}** with the AppId of the bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      1. Configure your **AIServices** settings.
      
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent in Visual Studio

1. Select **Test in WebChat** on the Azure Bot

## Further reading
To learn more about building Bots and Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.

```

## Samples/basic/weather-agent/dotnet/WeatherAgent.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
		<ImplicitUsings>disable</ImplicitUsings>
		<NoWarn>$(NoWarn);SKEXP0010</NoWarn>
		<UserSecretsId>b842df34-390f-490d-9dc0-73909363ad16</UserSecretsId>
	</PropertyGroup>

	<ItemGroup>
		<Compile Remove="wwwroot\**" />
		<Content Remove="wwwroot\**" />
		<EmbeddedResource Remove="wwwroot\**" />
		<None Remove="wwwroot\**" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Azure.Identity" Version="1.13.2" />
		<PackageReference Include="AdaptiveCards" Version="3.1.0"/>
		<PackageReference Include="Microsoft.SemanticKernel.Agents.AzureAI" Version="1.45.0-preview"/>
		<PackageReference Include="Microsoft.SemanticKernel.Agents.Core" Version="1.45.0" />
		<PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureOpenAI" Version="1.45.0" />
		<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.45.0" />
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>

```

## Samples/basic/weather-agent/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.weather",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "WeatherSim Streamer",
    "full": "WeatherSim Streamer Example"
  },
  "description": {
    "short": "Sample demonstrating AgentSDK + Azure Bot Services + Azure AI Fondry",
    "full": "This sample demonstrates how to integrate Azure AI Foundry capabilities built with the AgentSDK and manage Streaming Responses to callerss"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/weather-agent/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.weather",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "WeatherSim Streamer",
    "full": "WeatherSim Streamer Example"
  },
  "description": {
    "short": "Sample demonstrating AgentSDK + Azure Bot Services + Azure AI Fondry",
    "full": "This sample demonstrates how to integrate Azure AI Foundry capabilities built with the AgentSDK and manage Streaming Responses to callerss"
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal",
        "groupChat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "<<AGENT_DOMAIN>>"
  ]
}
```

## Samples/basic/weather-agent/dotnet/appManifest/.gitignore

```ignore
manifest.json
*.zip
```

## Samples/basic/weather-agent/dotnet/Plugins/AdaptiveCardPlugin.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

namespace WeatherAgent.Plugins;

public class AdaptiveCardPlugin
{
    private const string Instructions = """
        When given data about the weather forecast for a given time and place, please generate an adaptive card
        that displays the information in a visually appealing way. Make sure to only return the valid adaptive card
        JSON string in the response.
        """;

    [KernelFunction]
    public async Task<string> GetAdaptiveCardForData(Kernel kernel, string data)
    {
        // Create a chat history with the instructions as a system message and the data as a user message
        ChatHistory chat = new(Instructions)
        {
            new ChatMessageContent(AuthorRole.User, data)
        };

        // Invoke the model to get a response
        IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(chat);

        return response.ToString();
    }
}

```

## Samples/basic/weather-agent/dotnet/Plugins/WeatherForecastPlugin.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Agents.Builder;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace WeatherAgent.Plugins;

public class WeatherForecastPlugin(ITurnContext turnContext)
{

    /// <summary>
    /// Retrieve the weather forecast for a specific date. This is a placeholder for a real implementation
    /// and currently only returns a random temperature. This would typically call a weather service API.
    /// </summary>
    /// <param name="date">The date as a parsable string</param>
    /// <param name="location">The location to get the weather for</param>
    /// <returns></returns>
    [KernelFunction]
    public async Task<WeatherForecast> GetForecastForDate(string date,  string location)
    {
        string searchingForDate = date;
        if ( DateTime.TryParse(date, out DateTime searchingDate) )
        {
            searchingForDate = searchingDate.ToLongDateString();
        }
        await turnContext.StreamingResponse.QueueInformativeUpdateAsync($"Looking up the Weather in {location} for {searchingForDate}");

        // Simulate a delay for the weather service call
        return new WeatherForecast
        {
            Date = date,
            TemperatureC = Random.Shared.Next(-20, 55)
        };
    }
}

```

## Samples/basic/weather-agent/dotnet/Plugins/DateTimePlugin.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;

namespace WeatherAgent.Plugins;

/// <summary>
/// Semantic Kernel plugins for date and time.
/// </summary>
public class DateTimePlugin
{
    /// <summary>
    /// Get the current date
    /// </summary>
    /// <example>
    /// {{time.date}} => Sunday, 12 January, 2031
    /// </example>
    /// <returns> The current date </returns>
    [KernelFunction, Description("Get the current date")]
    public string Date(IFormatProvider formatProvider = null)
    {
        // Example: Sunday, 12 January, 2025
        string date = DateTimeOffset.Now.ToString("D", formatProvider);
        return date;
    }


    /// <summary>
    /// Get the current date
    /// </summary>
    /// <example>
    /// {{time.today}} => Sunday, 12 January, 2031
    /// </example>
    /// <returns> The current date </returns>
    [KernelFunction, Description("Get the current date")]
    public string Today(IFormatProvider formatProvider = null) =>
        // Example: Sunday, 12 January, 2025
        this.Date(formatProvider);

    /// <summary>
    /// Get the current date and time in the local time zone"
    /// </summary>
    /// <example>
    /// {{time.now}} => Sunday, January 12, 2025 9:15 PM
    /// </example>
    /// <returns> The current date and time in the local time zone </returns>
    [KernelFunction, Description("Get the current date and time in the local time zone")]
    public string Now(IFormatProvider formatProvider = null) =>
        // Sunday, January 12, 2025 9:15 PM
        DateTimeOffset.Now.ToString("f", formatProvider);
}

```

## Samples/basic/weather-agent/dotnet/Plugins/WeatherForecast.cs

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WeatherAgent.Plugins;

public class WeatherForecast
{
    /// <summary>
    /// A date for the weather forecast
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// The temperature in Celsius
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// The temperature in Fahrenheit
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

```

## Samples/basic/weather-agent/dotnet/Agents/WeatherForecastAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WeatherAgent.Plugins;

namespace WeatherAgent.Agents;

public class WeatherForecastAgent
{
    private readonly Kernel _kernel;
    private readonly ChatCompletionAgent _agent;

    private const string AgentName = "WeatherForecastAgent";
    private const string AgentInstructions = """
        You are a friendly assistant that helps people find a weather forecast for a given time and place.
        You may ask follow up questions until you have enough information to answer the customers question,
        but once you have a forecast forecast, make sure to format it nicely using an adaptive card.
        You should use adaptive JSON format to display the information in a visually appealing way and include a button for more details that points at https://www.msn.com/en-us/weather/forecast/in-{location}
        You should use adaptive cards version 1.5 or later.

        Respond in JSON format with the following JSON schema:
        
        {
            "contentType": "'Text' or 'AdaptiveCard' only",
            "content": "{The content of the response, may be plain text, or JSON based adaptive card}"
        }
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherForecastAgent"/> class.
    /// </summary>
    /// <param name="kernel">An instance of <see cref="Kernel"/> for interacting with an LLM.</param>
    public WeatherForecastAgent(Kernel kernel, IServiceProvider service)
    {
        this._kernel = kernel;

        // Define the agent
        this._agent =
            new()
            {
                Instructions = AgentInstructions,
                Name = AgentName,
                Kernel = this._kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    ResponseFormat = "json_object"
                }),
            };

        // Give the agent some tools to work with
        this._agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<DateTimePlugin>(serviceProvider: service));
        this._agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<WeatherForecastPlugin>(serviceProvider: service));
        this._agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<AdaptiveCardPlugin>(serviceProvider: service));
    }

    /// <summary>
    /// Invokes the agent with the given input and returns the response.
    /// </summary>
    /// <param name="input">A message to process.</param>
    /// <returns>An instance of <see cref="WeatherForecastAgentResponse"/></returns>
    public async Task<WeatherForecastAgentResponse> InvokeAgentAsync(string input, ChatHistory chatHistory)
    {
        ArgumentNullException.ThrowIfNull(chatHistory);
        AgentThread thread = new ChatHistoryAgentThread();
        ChatMessageContent message = new(AuthorRole.User, input);
        chatHistory.Add(message);

        StringBuilder sb = new();
        await foreach (ChatMessageContent response in this._agent.InvokeAsync(chatHistory, thread: thread))
        {
            chatHistory.Add(response);
            sb.Append(response.Content);
        }

        // Make sure the response is in the correct format and retry if necessary
        try
        {
            string resultContent = sb.ToString();
            var jsonNode = JsonNode.Parse(resultContent);
            WeatherForecastAgentResponse result = new WeatherForecastAgentResponse()
            {
                Content = jsonNode["content"].ToString(),
                ContentType = Enum.Parse<WeatherForecastAgentResponseContentType>(jsonNode["contentType"].ToString(), true)
            };
            return result;
        }
        catch (Exception je)
        {
            return await InvokeAgentAsync($"That response did not match the expected format. Please try again. Error: {je.Message}", chatHistory);
        }
    }
}

```

## Samples/basic/weather-agent/dotnet/Agents/WeatherForecastAgentResponse.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WeatherAgent.Agents;

public enum WeatherForecastAgentResponseContentType
{
    [JsonPropertyName("text")]
    Text,

    [JsonPropertyName("adaptive-card")]
    AdaptiveCard
}

public class WeatherForecastAgentResponse
{
    [JsonPropertyName("contentType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WeatherForecastAgentResponseContentType ContentType { get; set; }

    [JsonPropertyName("content")]
    [Description("The content of the response, may be plain text, or JSON based adaptive card but must be a string.")]
    public string Content { get; set; }
}

```

## Samples/basic/weather-agent/nodejs/tsconfig.json

```json
{
  "compilerOptions": {
      "incremental": true,
      "target": "ES2022",
      "module": "ES2022",
      "declaration": true,
      "sourceMap": true,
      "composite": true,
      "strict": true,
      "moduleResolution": "node",
      "esModuleInterop": true,
      "skipLibCheck": true,
      "forceConsistentCasingInFileNames": true,
      "resolveJsonModule": true,
      "rootDir": "src",
      "outDir": "dist",
      "tsBuildInfoFile": "dist/.tsbuildinfo"
  }
}

```

## Samples/basic/weather-agent/nodejs/package.json

```json
{
  "name": "node-weather-agent",
  "version": "1.0.0",
  "main": "index.js",
  "scripts": {
    "prebuild": "npm install",
    "build": "tsc --build",
    "prestart": "npm run build",
    "start": "node --env-file .env ./dist/index.js",
    "test-tool": "teamsapptester start",
    "test": "npm-run-all -p -r start test-tool"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "type": "module",
  "description": "",
  "dependencies": {
    "@langchain/langgraph": "^0.2.72",
    "@langchain/openai": "^0.5.10",
    "@microsoft/agents-hosting": "^0.4.0",
    "express": "^5.1.0"
  },
  "devDependencies": {
    "@microsoft/teams-app-test-tool": "^0.2.10",
    "@types/node": "^22.13.4",
    "nodemon": "^3.1.9",
    "npm-run-all": "^4.1.5",
    "typescript": "^5.7.2"
  }
}

```

## Samples/basic/weather-agent/nodejs/env.TEMPLATE

```
tenantId=
clientId=
clientSecret=

AZURE_OPENAI_API_INSTANCE_NAME=
AZURE_OPENAI_API_DEPLOYMENT_NAME=
AZURE_OPENAI_API_KEY=
AZURE_OPENAI_API_VERSION="2024-06-01"

DEBUG=agents:*:error
```

## Samples/basic/weather-agent/nodejs/README.md

```markdown
﻿# WeatherAgent Sample with LangChain

This is a sample of a simple Weather Forecast Agent that is hosted on a express core web service.  This Agent is configured to accept a request asking for information about a weather forecast and respond to the caller with an Adaptive Card.

This Agent Sample is intended to introduce you the basics of integrating LangChain with the Microsoft 365 Agents SDK in order to build powerful Agents. It can also be used as a the base for a custom Agent that you choose to develop.

***Note:*** This sample requires JSON output from the model which works best from newer versions of the model such as gpt-4o-mini.

## Prerequisites

- NodeJS > 20.*
- 1. You will need an Azure OpenAI or OpenAI instance, with the preferred model of `gpt-4o-mini`.

## Running this sample

Create a `.env` file, based on the provided `env.TEMPLATE` and include the AzureOpenAI settings:

```env
AZURE_OPENAI_API_INSTANCE_NAME=
AZURE_OPENAI_API_DEPLOYMENT_NAME=
AZURE_OPENAI_API_KEY=
AZURE_OPENAI_API_VERSION=
```


## Getting Started with WeatherBot Sample

Read more about [Running an Agent](../../../docs/HowTo/running-an-agent.md)

### QuickStart using Teams app test tool

1. Open the weatherbot Sample in Visual Studio Code
1. Start the application with  `npm start`
1. Start the test tool with `npm run test-tool`

If all is working correctly, the Teams app test tool should show you a Web Chat experience with the words **"Hello and Welcome! I'm here to help with all your weather forecast needs!"**, now you can interact with the agent asking forecast questions such as **tell me the weather forecast for today in NYC** 

> [!Important]
> The Teams app test tool does not support authentication, to use it you should not configure the `clientId`, this is only required for Azure Bot Service

### QuickStart using WebChat

**To run the sample connected to Azure Bot Service, the following additional tools are required:**

- Access to an Azure Subscription with access to preform the following tasks:
    - Create and configure Entra ID Application Identities
    - Create and configure an [Azure Bot Service](https://aka.ms/AgentsSDK-CreateBot) for your bot
    - Create and configure an [Azure App Service](https://learn.microsoft.com/azure/app-service/) to deploy your bot on to.
    - A tunneling tool to allow for local development and debugging should you wish to do local development whilst connected to a external client such as Microsoft Teams.


1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below


1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.

   1. Update the `.env` file in the root of the sample project.

```env
tenantId=
clientId=
clientSecret=
```
   
1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent with `npm start`

1. Go to Azure Bot Service `Test in WebChat` to start interacting with your bot.

## Further reading
To learn more about building Bots and Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.

```

## Samples/basic/weather-agent/nodejs/src/myAgent.ts

```typescript
import { ActivityTypes } from '@microsoft/agents-activity'
import { AgentApplicationBuilder, MessageFactory } from '@microsoft/agents-hosting'
import { AzureChatOpenAI } from '@langchain/openai'
import { MemorySaver } from '@langchain/langgraph'
import { HumanMessage, SystemMessage } from '@langchain/core/messages'
import { createReactAgent } from '@langchain/langgraph/prebuilt'
import { GetWeatherTool } from './tools/getWeatherTool.js'
import { dateTool } from './tools/dateTimeTool.js'

export const weatherAgent = new AgentApplicationBuilder().build()

weatherAgent.onConversationUpdate('membersAdded', async (context, state) => {
  await context.sendActivity(`Hello and Welcome! I'm here to help with all your weather forecast needs!`)
})

interface WeatherForecastAgentResponse {
  contentType: 'Text' | 'AdaptiveCard'
  content: string
}

const agentModel = new AzureChatOpenAI({
  azureOpenAIApiKey: process.env.AZURE_OPENAI_API_KEY,
  azureOpenAIApiInstanceName: process.env.AZURE_OPENAI_API_INSTANCE_NAME,
  azureOpenAIApiDeploymentName: process.env.AZURE_OPENAI_API_DEPLOYMENT_NAME,
  azureOpenAIApiVersion: process.env.AZURE_OPENAI_API_VERSION,
  temperature: 0
})

const agentTools = [GetWeatherTool, dateTool]
const agentCheckpointer = new MemorySaver()
const agent = createReactAgent({
  llm: agentModel,
  tools: agentTools,
  checkpointSaver: agentCheckpointer,
})

const sysMessage = new SystemMessage(`
        You are a friendly assistant that helps people find a weather forecast for a given time and place.
        You may ask follow up questions until you have enough informatioon to answer the customers question,
        but once you have a forecast forecast, make sure to format it nicely using an adaptive card.

        Respond in JSON format with the following JSON schema, and do not use markdown in the response:

        {
            "contentType": "'Text' or 'AdaptiveCard' only",
            "content": "{The content of the response, may be plain text, or JSON based adaptive card}"
        }`
)

weatherAgent.onActivity(ActivityTypes.Message, async (context, state) => {
  const llmResponse = await agent.invoke({
    messages: [
      sysMessage,
      new HumanMessage(context.activity.text!)
    ]
  },
  {
    configurable: { thread_id: context.activity.conversation!.id }
  })

  const llmResponseContent: WeatherForecastAgentResponse = JSON.parse(llmResponse.messages[llmResponse.messages.length - 1].content as string)

  if (llmResponseContent.contentType === 'Text') {
    await context.sendActivity(llmResponseContent.content)
  } else if (llmResponseContent.contentType === 'AdaptiveCard') {
    const response = MessageFactory.attachment({
      contentType: 'application/vnd.microsoft.card.adaptive',
      content: llmResponseContent.content
    })
    await context.sendActivity(response)
  }
})

```

## Samples/basic/weather-agent/nodejs/src/index.ts

```typescript
import { authorizeJWT, CloudAdapter, loadAuthConfigFromEnv } from '@microsoft/agents-hosting'
import express from 'express'
import { weatherAgent } from './myAgent.js'

const authConfig = loadAuthConfigFromEnv()
const adapter = new CloudAdapter(authConfig)

const server = express()
server.use(express.json())
server.use(authorizeJWT(authConfig))

server.post('/api/messages', async (req, res) => {
  await adapter.process(req, res, async (context) => {
    await weatherAgent.run(context)
  })
})

const port = process.env.PORT || 3978
server.listen(port, () => {
  console.log(`\nServer listening to port ${port} for appId ${authConfig.clientId} debug ${process.env.DEBUG}`)
}).on('error', (err) => {
  console.error(err)
})

```

## Samples/basic/weather-agent/nodejs/src/tools/getWeatherTool.ts

```typescript
import { z } from 'zod'
import { tool } from '@langchain/core/tools'

export const GetWeatherTool = tool(
  async ({ date, location }) => {
    console.log('************Getting random weather for', date, location)
    const min = -22
    const max = 55
    return {
      Date: date,
      TemperatureC: Math.floor(Math.random() * (max - min + 1)) + min
    }
  },
  {
    name: 'GetWeather',
    description: 'Retrieve the weather forecast for a specific date. This is a placeholder for a real implementation and currently only returns a random temperature. This would typically call a weather service API.',
    schema: z.object({
      date: z.string(),
      location: z.string(),
    })
  }
)

```

## Samples/basic/weather-agent/nodejs/src/tools/dateTimeTool.ts

```typescript
import { tool } from '@langchain/core/tools'

export const dateTool = tool(
  async () => {
    const d = new Date()
    console.log('************Getting the date', d)
    return `${d.getMonth() + 1}/${d.getDate()}/${d.getFullYear()}`
  },
  {
    name: 'Date',
    description: 'Get the current date'
  }
)

export const todayTool = tool(
  async () => {
    return new Date().toDateString()
  },
  {
    name: 'Today',
    description: 'Get the current date'
  }
)

export const nowTool = tool(
  async () => {
    return new Date().toLocaleDateString() + ' ' + new Date().toLocaleTimeString()
  },
  {
    name: 'Now',
    description: 'Get the current date and time in the local time zone'
  }
)

```

## Samples/basic/copilotstudio-client/dotnet/Program.cs

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CopilotStudioClientSample;
using Microsoft.Agents.CopilotStudio.Client;

// Setup the Direct To Engine client example. 

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Get the configuration settings for the DirectToEngine client from the appsettings.json file.
SampleConnectionSettings settings = new SampleConnectionSettings(builder.Configuration.GetSection("CopilotStudioClientSettings"));

// Create an http client for use by the DirectToEngine Client and add the token handler to the client.
builder.Services.AddHttpClient("mcs").ConfigurePrimaryHttpMessageHandler(() =>
{
    if (settings.UseS2SConnection)
    {
        return new AddTokenHandlerS2S(settings);
    }
    else
    {
        return new AddTokenHandler(settings);
    }
});

// add Settings and an instance of the Direct To engine Copilot Client to the Current services.  
builder.Services
    .AddSingleton(settings)
    .AddTransient<CopilotClient>((s) =>
    {
        var logger = s.GetRequiredService<ILoggerFactory>().CreateLogger<CopilotClient>();
        return new CopilotClient(settings, s.GetRequiredService<IHttpClientFactory>(), logger, "mcs");
    })
    .AddHostedService<ChatConsoleService>();
IHost host = builder.Build();
host.Run();


```

## Samples/basic/copilotstudio-client/dotnet/AddTokenHandlerS2S.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.CopilotStudio.Client;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;

namespace CopilotStudioClientSample
{
    /// <summary>
    /// This sample uses an HttpClientHandler to add an authentication token to the request.
    /// In this case its using client secret for the request. 
    /// </summary>
    /// <param name="settings">Direct To engine connection settings.</param>
    internal class AddTokenHandlerS2S(SampleConnectionSettings settings) : DelegatingHandler(new HttpClientHandler())
    {
        private static readonly string _keyChainServiceName = "copilot_studio_client_app";
        private static readonly string _keyChainAccountName = "copilot_studio_client";

        private IConfidentialClientApplication? _confidentialClientApplication;
        private string[]? _scopes;

        private async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default!)
        {
            if (_confidentialClientApplication == null)
            {
                ArgumentNullException.ThrowIfNull(settings);
                _scopes = [CopilotClient.ScopeFromSettings(settings)];
                _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(settings.AppClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, settings.TenantId)
                    .WithClientSecret(settings.AppClientSecret)
                    .Build();

                string currentDir = Path.Combine(AppContext.BaseDirectory, "mcs_client_console");

                if (!Directory.Exists(currentDir))
                {
                    Directory.CreateDirectory(currentDir);
                }

                StorageCreationPropertiesBuilder storageProperties = new("AppTokenCache", currentDir);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    storageProperties.WithLinuxUnprotectedFile();
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    storageProperties.WithMacKeyChain(_keyChainServiceName, _keyChainAccountName);
                }
                MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build());
                tokenCacheHelper.RegisterCache(_confidentialClientApplication.AppTokenCache);
            }

            AuthenticationResult authResponse;
            authResponse = await _confidentialClientApplication.AcquireTokenForClient(_scopes).ExecuteAsync(ct);
            return authResponse;
        }

        /// <summary>
        /// Handles sending the request and adding the token to the request.
        /// </summary>
        /// <param name="request">Request to be sent</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                AuthenticationResult authResponse = await AuthenticateAsync(cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

```

## Samples/basic/copilotstudio-client/dotnet/ChatConsoleService.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Core.Models;
using Microsoft.Agents.CopilotStudio.Client;

namespace CopilotStudioClientSample;

/// <summary>
/// This class is responsible for handling the Chat Console service and managing the conversation between the user and the Copilot Studio hosted Agent.
/// </summary>
/// <param name="copilotClient">Connection Settings for connecting to Copilot Studio</param>
internal class ChatConsoleService(CopilotClient copilotClient) : IHostedService
{
    /// <summary>
    /// This is the main thread loop that manages the back and forth communication with the Copilot Studio Agent. 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        Console.Write("\nagent> ");
        // Attempt to connect to the copilot studio hosted agent here
        // if successful, this will loop though all events that the Copilot Studio agent sends to the client setup the conversation. 
        await foreach (Activity act in copilotClient.StartConversationAsync(emitStartConversationEvent:true, cancellationToken:cancellationToken))
        {
            System.Diagnostics.Trace.WriteLine($">>>>MessageLoop Duration: {sw.Elapsed.ToDurationString()}");
            sw.Restart();
            if (act is null)
            {
                throw new InvalidOperationException("Activity is null");
            }
            // for each response,  report to the UX
            PrintActivity(act);
        }

        // Once we are connected and have initiated the conversation,  begin the message loop with the Console. 
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("\nuser> ");
            string question = Console.ReadLine()!; // Get user input from the console to send. 
            Console.Write("\nagent> ");
            // Send the user input to the Copilot Studio agent and await the response.
            // In this case we are not sending a conversation ID, as the agent is already connected by "StartConversationAsync", a conversation ID is persisted by the underlying client. 
            sw.Restart();
            await foreach (Activity act in copilotClient.AskQuestionAsync(question, null, cancellationToken))
            {
                System.Diagnostics.Trace.WriteLine($">>>>MessageLoop Duration: {sw.Elapsed.ToDurationString()}");
                // for each response,  report to the UX
                PrintActivity(act);
                sw.Restart();
            }
        }
        sw.Stop(); 
    }

    /// <summary>
    /// This method is responsible for writing formatted data to the console.
    /// This method does not handle all of the possible activity types and formats, it is focused on just a few common types. 
    /// </summary>
    /// <param name="act"></param>
    static void PrintActivity(IActivity act)
    {
        switch (act.Type)
        {
            case "message":
                if (act.TextFormat == "markdown")
                {
                    
                    Console.WriteLine(act.Text);
                    if (act.SuggestedActions?.Actions.Count > 0)
                    {
                        Console.WriteLine("Suggested actions:\n");
                        act.SuggestedActions.Actions.ToList().ForEach(action => Console.WriteLine("\t" + action.Text));
                    }
                }
                else
                {
                    Console.Write($"\n{act.Text}\n");
                }
                break;
            case "typing":
                Console.Write(".");
                break;
            case "event":
                Console.Write("+");
                break;
            default:
                Console.Write($"[{act.Type}]");
                break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        System.Diagnostics.Trace.TraceInformation("Stopping");
        return Task.CompletedTask;
    }
}

```

## Samples/basic/copilotstudio-client/dotnet/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.Hosting.Lifetime": "Warning"
    }
  },
  "CopilotStudioClientSettings": {
    "DirectConnectUrl": "",
    "EnvironmentId": "", 
    "SchemaName": "", 
    "TenantId": "",
    "UseS2SConnection": false,
    "AppClientId": "",
    "AppClientSecret": ""
  }  
}

```

## Samples/basic/copilotstudio-client/dotnet/SampleConnectionSettings.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.CopilotStudio.Client;

namespace CopilotStudioClientSample
{
    /// <summary>
    /// Connection Settings extension for the sample to include appID and TenantId for creating authentication token.
    /// </summary>
    internal class SampleConnectionSettings : ConnectionSettings
    {
        /// <summary>
        /// Use S2S connection for authentication.
        /// </summary>
        public bool UseS2SConnection { get; set; } = false;

        /// <summary>
        /// Tenant ID for creating the authentication for the connection
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Application ID for creating the authentication for the connection
        /// </summary>
        public string? AppClientId { get; set; }

        /// <summary>
        /// Application secret for creating the authentication for the connection
        /// </summary>
        public string? AppClientSecret { get; set; }

        /// <summary>
        /// Create ConnectionSettings from a configuration section.
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="ArgumentException"></exception>
        public SampleConnectionSettings(IConfigurationSection config) :base (config)
        {
            AppClientId = config[nameof(AppClientId)] ?? throw new ArgumentException($"{nameof(AppClientId)} not found in config");
            TenantId = config[nameof(TenantId)] ?? throw new ArgumentException($"{nameof(TenantId)} not found in config");

            UseS2SConnection = config.GetValue<bool>(nameof(UseS2SConnection), false);
            AppClientSecret = config[nameof(AppClientSecret)]; 

        }
    }
}

```

## Samples/basic/copilotstudio-client/dotnet/TimeSpanExtensions.cs

```csharp
﻿namespace CopilotStudioClientSample
{
    internal static class TimeSpanExtensions
    {
        /// <summary>
        /// Returns a duration in the format hh:mm:ss:fff
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        internal static string ToDurationString(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"hh\:mm\:ss\.fff");
        }
    }
}

```

## Samples/basic/copilotstudio-client/dotnet/CopilotStudioClient.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Worker">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.1" />
		<PackageReference Include="Microsoft.Identity.Client.Extensions.Msal" Version="4.67.2" />
		<PackageReference Include="Microsoft.Agents.CopilotStudio.Client" Version="1.1.*-*"/> 
	</ItemGroup>

</Project>

```

## Samples/basic/copilotstudio-client/dotnet/AddTokenHandler.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;
using Microsoft.Agents.CopilotStudio.Client;

namespace CopilotStudioClientSample
{
    /// <summary>
    /// This sample uses an HttpClientHandler to add an authentication token to the request.
    /// This is used for the interactive authentication flow. 
    /// For more information on how to setup various authentication flows, see the Microsoft Identity documentation at https://aka.ms/msal.
    /// </summary>
    /// <param name="settings">Direct To engine connection settings.</param>
    internal class AddTokenHandler(SampleConnectionSettings settings) : DelegatingHandler(new HttpClientHandler())
    {
        private static readonly string _keyChainServiceName = "copilot_studio_client_app";
        private static readonly string _keyChainAccountName = "copilot_studio_client";
        
        private async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default!)
        {
            ArgumentNullException.ThrowIfNull(settings);

            // Gets the correct scope for connecting to Copilot Studio based on the settings provided. 
            string[] scopes = [CopilotClient.ScopeFromSettings(settings)];

            // Setup a Public Client application for authentication.
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(settings.AppClientId)
                 .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                 .WithTenantId(settings.TenantId)
                 .WithRedirectUri("http://localhost")
                 .Build();

            string currentDir = Path.Combine(AppContext.BaseDirectory, "mcs_client_console");

            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
            }

            StorageCreationPropertiesBuilder storageProperties = new("TokenCache", currentDir);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                storageProperties.WithLinuxUnprotectedFile();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                storageProperties.WithMacKeyChain(_keyChainServiceName, _keyChainAccountName);
            }
            MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build());
            tokenCacheHelper.RegisterCache(app.UserTokenCache);

            IAccount? account = (await app.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResponse;
            try
            {
                authResponse = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
            }
            catch (MsalUiRequiredException)
            {
                authResponse = await app.AcquireTokenInteractive(scopes).ExecuteAsync(ct);
            }
            return authResponse;
        }

        /// <summary>
        /// Handles sending the request and adding the token to the request.
        /// </summary>
        /// <param name="request">Request to be sent</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                AuthenticationResult authResponse = await AuthenticateAsync(cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

```

## Samples/basic/copilotstudio-client/dotnet/README.md

```markdown
# Copilot Studio Client Console Sample

## Instructions - Setup

### Prerequisite

To setup for this sample, you will need the following:

1. An Agent Created in Microsoft Copilot Studio
1. Ability to Create a Application Identity in Azure for a Public Client/Native App Registration Or access to an existing Public Client/Native App registration with the CopilotStudio.Copilot.Invoke API Permission assigned. 

### Create a Agent in Copilot Studio

1. Create a Agent in [Copilot Studio](https://copilotstudio.microsoft.com)
    1. Publish your newly created Agent
    1. In Copilot Studio, go to Settings => Advanced => Metadata and copy the following values, You will need them later:
        1. Schema name
        1. Environment Id

## Create an Application Registration in Entra ID - User Interactive Login

This step will require permissions to Create application identities in your Azure tenant. For this sample, you will be creating a Native Client Application Identity, which does not have secrets.

1. Open https://portal.azure.com 
1. Navigate to Entra Id
1. Create an new App Registration in Entra ID 
    1. Provide a Name
    1. Choose "Accounts in this organization directory only"
    1. In the "Select a Platform" list, Choose "Public Client/native (mobile & desktop) 
    1. In the Redirect URI url box, type in `http://localhost` (**note: use HTTP, not HTTPS**)
    1. Then click register.
1. In your newly created application
    1. On the Overview page, Note down for use later when configuring the example application:
        1. the Application (client) ID
        1. the Directory (tenant) ID
    1. Goto Manage
    1. Goto API Permissions
    1. Click Add Permission
        1. In the side panel that appears, Click the tab `API's my organization uses`
        1. Search for `Power Platform API`.
            1. *If you do not see `Power Platform API` see the note at the bottom of this section.*
        1. In the permissions list choose `Delegated Permissions`, `CopilotStudio` and Check `CopilotStudio.Copilots.Invoke`
        1. Click `Add Permissions`
    1. (Optional) Click `Grant Admin consent for copilotsdk`
    1. Close Azure Portal

> [!TIP]
> If you do not see `Power Platform API` in the list of API's your organization uses, you need to add the Power Platform API to your tenant. To do that, goto [Power Platform API Authentication](https://learn.microsoft.com/power-platform/admin/programmability-authentication-v2#step-2-configure-api-permissions) and follow the instructions on Step 2 to add the Power Platform Admin API to your Tenant

### Instructions - Configure the Example Application - User Interactive Login

With the above information, you can now run the client `CopilostStudioClientSample`.

1. Open the appSettings.json file for the CopilotStudioClientSample, or rename launchSettings.TEMPLATE.json to launchSettings.json.
1. Configured the placeholder values for the various key's based on what was recorded during the setup phase.

```json
  "CopilotStudioClientSettings": {
    "EnvironmentId": "", // Environment ID of environment with the CopilotStudio App.
    "SchemaName": "", // Schema Name of the Copilot to use
    "TenantId": "", // Tenant ID of the App Registration used to login,  this should be in the same tenant as the Copilot.
    "AppClientId": "" // App ID of the App Registration used to login,  this should be in the same tenant as the Copilot.
  }
```

## Create an Application Registration in Entra ID - Service Principal Login

> [!Warning]
> The Service Principal login method is not generally supported in the current version of the CopilotStudioClient. 
> 
> Current use of this feature requires authorization from Copilot Studio team and will be made generally available in the future.

> [!IMPORTANT]
> When using Service Principal login, Your Copilot Studio Agent must be configured for User Anonymous authentication.

This step will require permissions to Create application identities in your Azure tenant. For this sample, you will be creating a Native Client Application Identity, which does not have secrets.

1. Open https://portal.azure.com 
1. Navigate to Entra Id
1. Create an new App Registration in Entra ID 
    1. Provide an Name
    1. Choose "Accounts in this organization directory only"
    1. Then click register.
1. In your newly created application
    1. On the Overview page, Note down for use later when configuring the example application:
        1. the Application (client) ID
        1. the Directory (tenant) ID
    1. Goto Manage
    1. Goto API Permissions
    1. Click Add Permission
        1. In the side panel that appears, Click the tab `API's my organization uses`
        1. Search for `Power Platform API`.
            1. *If you do not see `Power Platform API` see the note at the bottom of this section.*
        1. In the permissions list choose `Application Permissions`, then `CopilotStudio` and Check `CopilotStudio.Copilots.Invoke`
        1. Click `Add Permissions`
    1. (Optional) Click `Grant Admin consent for copilotsdk`
    1. Close Azure Portal

> [!TIP]
> If you do not see `Power Platform API` in the list of API's your organization uses, you need to add the Power Platform API to your tenant. To do that, goto [Power Platform API Authentication](https://learn.microsoft.com/power-platform/admin/programmability-authentication-v2#step-2-configure-api-permissions) and follow the instructions on Step 2 to add the Power Platform Admin API to your Tenant

### Instructions - Configure the Example Application - Service Principal Login

With the above information, you can now run the client `CopilostStudioClientSample`.

1. Open the appSettings.json file for the CopilotStudioClientSample, or rename launchSettings.TEMPLATE.json to launchSettings.json.
1. Configured the placeholder values for the various key's based on what was recorded during the setup phase.

```json
  "CopilotStudioClientSettings": {
    "EnvironmentId": "", // Environment ID of environment with the CopilotStudio App.
    "SchemaName": "", // Schema Name of the Copilot to use
    "TenantId": "", // Tenant ID of the App Registration used to login,  this should be in the same tenant as the Copilot.
    "UseS2SConnection": true,
    "AppClientId": "" // App ID of the App Registration used to login,  this should be in the same tenant as the Copilot.
    "AppClientSecret": "" // App Secret of the App Registration used to login,  this should be in the same tenant as the Copilot.
  }
```



3. Run the CopilotStudioClientSample.exe program.

This should challenge you for login in a new browser window or tab and once completed, connect ot the Copilot Studio Hosted Agent, allowing you to communicate via a console interface.

## Authentication

The CopilotStudio Client requires a Token provided by the developer to operate. For this sample, by default, we are using a user interactive flow to get the user token for the application ID created above. 

The Copilot client will use a named `HttpClient` retrieved from the `IHttpClientFactory` as `mcs` injected in DI. This client needs to be configured with a `DelegatingHandler` to apply a valid Entra ID Token. In this sample using MSAL.


```

## Samples/basic/copilotstudio-client/dotnet/Properties/launchSettings.TEMPLATE.json

```json
{
  "profiles": {
    "mcs.console": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DirectToEngineSettings__TenantId": "",
        "DirectToEngineSettings__EnvironmentId": "",
        "DirectToEngineSettings__AppClientId": "",
        "DirectToEngineSettings__BotIdentifier": ""
      }
    }
  }
}
```

## Samples/basic/copilotstudio-client/nodejs/.npmrc

```
package-lock=false
```

## Samples/basic/copilotstudio-client/nodejs/tsconfig.json

```json
{
  "compilerOptions": {
      "incremental": true,
      "lib": ["ES2021"],
      "target": "es2019",
      "declaration": true,
      "sourceMap": true,
      "composite": true,
      "strict": true,
      "moduleResolution": "node",
      "esModuleInterop": true,
      "skipLibCheck": true,
      "forceConsistentCasingInFileNames": true,
      "resolveJsonModule": true,
      "module": "ESNext",
      "rootDir": "src",
      "outDir": "dist",
      "tsBuildInfoFile": "dist/.tsbuildinfo"
    }
}

```

## Samples/basic/copilotstudio-client/nodejs/package.json

```json
{
  "name": "copilotstudio-client",
  "version": "1.0.0",
  "private": true,
  "description": "Agents Copilot Studio Client sample",
  "author": "Microsoft",
  "license": "MIT",
  "main": "./dist/index.js",
  "type": "module",
  "scripts": {
    "prebuild": "npm install",
    "build": "tsc --build",
    "prestart": "npm run build",
    "start": "node --env-file .env ./dist/index.js"
  },
  "dependencies": {
    "@microsoft/agents-copilotstudio-client": "^0.4.0",
    "@azure/msal-node": "^3.5.3",
    "open": "^10.1.2"
  },
  "devDependencies": {
    "@types/debug": "^4.1.12",
    "@types/node": "^22.13.4",
    "typescript": "^5.7.3"
  },
  "keywords": []
}

```

## Samples/basic/copilotstudio-client/nodejs/env.TEMPLATE

```
environmentId=
agentIdentifier=
tenantId=
appClientId=
# cloud= # PowerPlatformCloud. eg 'Cloud | Gov'
# customPowerPlatformCloud= # Power Platform API endpoint to use if Cloud is configured as "Other"
# agentType="" # AgentType enum. eg 'Published'
DEBUG=copilot-studio-client:error
```

## Samples/basic/copilotstudio-client/nodejs/README.md

```markdown
# Copilot Studio Client

This is a sample to show how to use the `@microsoft/agents-copilotstudio-client` package to talk to an Agent hosted in CopilotStudio.


## Prerequisite

To set up this sample, you will need the following:

1. [Node.js](https://nodejs.org) version 20 or higher

    ```bash
    # determine node version
    node --version
    ```
2. An Agent Created in Microsoft Copilot Studio or access to an existing Agent.
3. Ability to Create an Application Identity in Azure for a Public Client/Native App Registration Or access to an existing Public Client/Native App registration with the `CopilotStudio.Copilots.Invoke` API Permission assigned. 

## Authentication

The Copilot Studio Client requires a User Token to operate. For this sample, we are using a user interactive flow to get the user token for the application ID created above. Other flows are allowed.

> [!Important]
> The token is cached in the user machine in `$TEMP/mcssample.usercache.json`

## Create an Agent in Copilot Studio

1. Create an Agent in [Copilot Studio](https://copilotstudio.microsoft.com)
    1. Publish your newly created Copilot
    2. Goto Settings => Advanced => Metadata and copy the following values, You will need them later:
        1. Schema name
        2. Environment Id

## Create an Application Registration in Entra ID

This step will require permissions to create application identities in your Azure tenant. For this sample, you will create a Native Client Application Identity, which does not have secrets.

1. Open https://portal.azure.com 
2. Navigate to Entra Id
3. Create a new App Registration in Entra ID 
    1. Provide a Name
    2. Choose "Accounts in this organization directory only"
    3. In the "Select a Platform" list, Choose "Public Client/native (mobile & desktop) 
    4. In the Redirect URI url box, type in `http://localhost` (**note: use HTTP, not HTTPS**)
    5. Then click register.
4. In your newly created application
    1. On the Overview page, Note down for use later when configuring the example application:
        1. The Application (client) ID
        2. The Directory (tenant) ID
    2. Go to API Permissions in `Manage` section
    3. Click Add Permission
        1. In the side panel that appears, Click the tab `API's my organization uses`
        2. Search for `Power Platform API`.
            1. *If you do not see `Power Platform API` see the note at the bottom of this section.*
        3. In the *Delegated permissions* list, choose `CopilotStudio` and Check `CopilotStudio.Copilots.Invoke`
        4. Click `Add Permissions`
    4. (Optional) Click `Grant Admin consent for copilotsdk`

> [!TIP]
> If you do not see `Power Platform API` in the list of API's your organization uses, you need to add the Power Platform API to your tenant. To do that, goto [Power Platform API Authentication](https://learn.microsoft.com/power-platform/admin/programmability-authentication-v2#step-2-configure-api-permissions) and follow the instructions on Step 2 to add the Power Platform Admin API to your Tenant

## Instructions - Configure the Example Application

With the above information, you can now run the client `CopilostStudioClient` sample.

1. Open the `env.TEMPLATE` file and rename it to `.env`.
2. Configure the values based on what was recorded during the setup phase.

```bash
  environmentId="" # Environment ID of environment with the CopilotStudio App.
  agentIdentifier="" # Schema Name of the Copilot to use
  tenantId="" # Tenant ID of the App Registration used to login, this should be in the same tenant as the Copilot.
  appClientId="" # App ID of the App Registration used to login, this should be in the same tenant as the CopilotStudio environment.
```

3. Run the CopilotStudioClient sample using `npm start`, which will install the packages, build the project and run it.

This should challenge you to login and connect to the Copilot Studio Hosted agent, allowing you to communicate via a console interface.




```

## Samples/basic/copilotstudio-client/nodejs/src/msalCachePlugin.ts

```typescript
/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */
import fs from 'fs'
import { ICachePlugin, TokenCacheContext } from '@azure/msal-node'

export class MsalCachePlugin implements ICachePlugin {
  private cacheLocation: string = ''
  constructor (cacheLocation: string) {
    this.cacheLocation = cacheLocation
  }

  async beforeCacheAccess (tokenCacheContext: TokenCacheContext): Promise<void> {
    return new Promise((resolve, reject) => {
      if (fs.existsSync(this.cacheLocation)) {
        fs.readFile(this.cacheLocation, 'utf-8', (error, data) => {
          if (error) {
            reject(error)
          } else {
            console.log('loading token from cache: ', this.cacheLocation)
            tokenCacheContext.tokenCache.deserialize(data)
            resolve()
          }
        })
      } else {
        fs.writeFile(this.cacheLocation, tokenCacheContext.tokenCache.serialize(), (error) => {
          if (error) {
            reject(error)
          }
          console.log('caching token at: ', this.cacheLocation)
          resolve()
        })
      }
    })
  }

  async afterCacheAccess (tokenCacheContext: TokenCacheContext): Promise<void> {
    return new Promise((resolve, reject) => {
      if (tokenCacheContext.cacheHasChanged) {
        fs.writeFile(this.cacheLocation, tokenCacheContext.tokenCache.serialize(), (error) => {
          if (error) {
            reject(error)
          }
          resolve()
        })
      } else {
        resolve()
      }
    })
  }
}

```

## Samples/basic/copilotstudio-client/nodejs/src/index.ts

```typescript
/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import * as msal from '@azure/msal-node'
import { Activity, ActivityTypes, CardAction } from '@microsoft/agents-activity'
import { ConnectionSettings, loadCopilotStudioConnectionSettingsFromEnv, CopilotStudioClient } from '@microsoft/agents-copilotstudio-client'
import pkg from '@microsoft/agents-copilotstudio-client/package.json' with { type: 'json' }
import readline from 'readline'
import open from 'open'
import os from 'os'
import path from 'path'

import { MsalCachePlugin } from './msalCachePlugin.js'

async function acquireToken (settings: ConnectionSettings): Promise<string> {
  const msalConfig = {
    auth: {
      clientId: settings.appClientId,
      authority: `https://login.microsoftonline.com/${settings.tenantId}`,
    },
    cache: {
      cachePlugin: new MsalCachePlugin(path.join(os.tmpdir(), 'mcssample.tockencache.json'))
    },
    system: {
      loggerOptions: {
        loggerCallback (loglevel: msal.LogLevel, message: string, containsPii: boolean) {
          if (!containsPii) {
            console.log(loglevel, message)
          }
        },
        piiLoggingEnabled: false,
        logLevel: msal.LogLevel.Verbose,
      }
    }
  }
  const pca = new msal.PublicClientApplication(msalConfig)
  const tokenRequest = {
    scopes: ['https://api.powerplatform.com/.default'],
    redirectUri: 'http://localhost',
    openBrowser: async (url: string) => {
      await open(url)
    }
  }
  let token
  try {
    const accounts = await pca.getAllAccounts()
    if (accounts.length > 0) {
      const response2 = await pca.acquireTokenSilent({ account: accounts[0], scopes: tokenRequest.scopes })
      token = response2.accessToken
    } else {
      const response = await pca.acquireTokenInteractive(tokenRequest)
      token = response.accessToken
    }
  } catch (error) {
    console.error('Error acquiring token interactively:', error)
    const response = await pca.acquireTokenInteractive(tokenRequest)
    token = response.accessToken
  }
  return token
}

const createClient = async (): Promise<CopilotStudioClient> => {
  const settings = loadCopilotStudioConnectionSettingsFromEnv()
  const token = await acquireToken(settings)
  const copilotClient = new CopilotStudioClient(settings, token)
  console.log(`Copilot Studio Client Version: ${pkg.version}, running with settings: ${JSON.stringify(settings, null, 2)}`)
  return copilotClient
}

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
})

const askQuestion = async (copilotClient: CopilotStudioClient, conversationId: string) => {
  rl.question('\n>>>: ', async (answer) => {
    if (answer.toLowerCase() === 'exit') {
      rl.close()
    } else {
      const replies = await copilotClient.askQuestionAsync(answer, conversationId)
      replies.forEach((act: Activity) => {
        if (act.type === ActivityTypes.Message) {
          console.log(`\n${act.text}`)
          act.suggestedActions?.actions.forEach((action: CardAction) => console.log(action.value))
        } else if (act.type === ActivityTypes.EndOfConversation) {
          console.log(`\n${act.text}`)
          rl.close()
        }
      })
      await askQuestion(copilotClient, conversationId)
    }
  })
}

const main = async () => {
  const copilotClient = await createClient()
  const act: Activity = await copilotClient.startConversationAsync(true)
  console.log('\nSuggested Actions: ')
  act.suggestedActions?.actions.forEach((action: CardAction) => console.log(action.value))
  await askQuestion(copilotClient, act.conversation?.id!)
}

main().catch(e => console.log(e))

```

## Samples/basic/full-authentication/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FullAuthentication;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Logging.AddConsole();

// Add AgentApplicationOptions from config.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<MyAgent>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();


// Configure the HTTP request pipeline.

// Add AspNet token validation
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Microsoft Agents SDK Sample");
app.MapPost(
    "/api/messages",
    async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
    {
        await adapter.ProcessAsync(request, response, agent, cancellationToken);
    })
    .RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    // Hardcoded for brevity and ease of testing. 
    // In production, this should be set in configuration.
    app.Urls.Add($"http://localhost:3978");
    app.Urls.Add($"https://localhost:3979");
}

app.Run();

```

## Samples/basic/full-authentication/dotnet/appsettings.json

```json
{
  "TokenValidation": {
    "Audiences": [
      "{{ClientId}}" // this is the Client ID used for the Azure Bot
    ],
    "TenantId": "{{TenantId}}"
  },

  "AgentApplication": {
    "StartTypingTimer": false,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Samples/basic/full-authentication/dotnet/FullAuthentication.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
		<ImplicitUsings>disable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<Compile Remove="wwwroot\**" />
		<Content Remove="wwwroot\**" />
		<EmbeddedResource Remove="wwwroot\**" />
		<None Remove="wwwroot\**" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>

```

## Samples/basic/full-authentication/dotnet/MyAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using System.Threading.Tasks;
using System.Threading;

namespace FullAuthentication;

public class MyAgent : AgentApplication
{
    public MyAgent(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);
    }

    private async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Hello and Welcome!"), cancellationToken);
            }
        }
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync($"You said: {turnContext.Activity.Text}", cancellationToken: cancellationToken);
    }
}

```

## Samples/basic/full-authentication/dotnet/README.md

```markdown
﻿# FullAuthentication Sample

This is a sample of a simple Agent that is hosted on an Asp.net core web service, with JWT Token Validation.  This is the EmptyAgent, with AspNet JWT token authentication enabled.

## AspNet Authentication for an Agent
- This samples includes authentication setup for validating tokens from Entra or Azure Bot Service
  - The `AspNetExtensions.AddAgentAspNetAuthentication` supports validating either type of token
  - Performs audience, issuer, signing validation, and signing key refresh.
  - It also support configuration for other Azure clouds such as US Gov, Gallatin or others.
- If your agent will not be communicating with Azure Bot Service, any AspNet mechanism to authenticate requests can be used instead.

## Prerequisites

- [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Running this sample

**To run the sample connected to Azure Bot Service, the following additional tools are required:**

- Access to an Azure Subscription with access to preform the following tasks:
    - Create and configure Entra ID Application Identities
    - Create and configure an [Azure Bot Service](https://aka.ms/AgentsSDK-CreateBot) for your Azure Bot.
    - Create and configure an [Azure App Service](https://learn.microsoft.com/azure/app-service/) to deploy your Agent to.
    - A tunneling tool to allow for local development and debugging should you wish to do local development whilst connected to a external client such as Microsoft Teams.

## Getting Started with FullAuthentication Sample

Read more about [Running an Agent](../../../docs/HowTo/running-an-agent.md)

### QuickStart using WebChat

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL Authentication provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "TokenValidation": {
        "Audiences": [
          "{{ClientId}}" // this is the Client ID used for the Azure Bot
        ],
        "TenantId": "{{TenantId}}" 
      },
   
      "Connections": {
        "ServiceConnection": {
          "Settings": {
            "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
            "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
            "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
            "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
            "Scopes": [
              "https://api.botframework.com/.default"
            ]
          }
        }
      },
      ```

      1. Replace all **{{ClientId}}** with the AppId of the Azure Bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. On the Azure Bot, select **Settings**, then **Configuration**, and update the **Messaging endpoint** to `{tunnel-url}/api/messages`

1. Start the Agent in Visual Studio

1. Select **Test in WebChat** on the Azure Bot

## Further reading
To learn more about building Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.
```

## Samples/basic/full-authentication/dotnet/AspNetExtensions.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FullAuthentication;

public static class AspNetExtensions
{
    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache = new();

    /// <summary>
    /// Adds token validation typical for ABS/SMBA and agent-to-agent.
    /// default to Azure Public Cloud.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="tokenValidationSectionName">Name of the config section to read.</param>
    /// <param name="logger">Optional logger to use for authentication event logging.</param>
    /// <remarks>
    /// Configuration:
    /// <code>
    ///   "TokenValidation": {
    ///     "Audiences": [
    ///       "{required:agent-appid}"
    ///     ],
    ///     "TenantId": "{recommended:tenant-id}",
    ///     "ValidIssuers": [
    ///       "{default:Public-AzureBotService}"
    ///     ],
    ///     "IsGov": {optional:false},
    ///     "AzureBotServiceOpenIdMetadataUrl": optional,
    ///     "OpenIdMetadataUrl": optional,
    ///     "AzureBotServiceTokenHandling": "{optional:true}"
    ///     "OpenIdMetadataRefresh": "optional-12:00:00"
    ///   }
    /// </code>
    /// 
    /// `IsGov` can be omitted, in which case public Azure Bot Service and Azure Cloud metadata urls are used.
    /// `ValidIssuers` can be omitted, in which case the Public Azure Bot Service issuers are used.
    /// `TenantId` can be omitted if the Agent is not being called by another Agent.  Otherwise it is used to add other known issuers.  Only when `ValidIssuers` is omitted.
    /// `AzureBotServiceOpenIdMetadataUrl` can be omitted.  In which case default values in combination with `IsGov` is used.
    /// `OpenIdMetadataUrl` can be omitted.  In which case default values in combination with `IsGov` is used.
    /// `AzureBotServiceTokenHandling` defaults to true and should always be true until Azure Bot Service sends Entra ID token.
    /// </remarks>
    public static void AddAgentAspNetAuthentication(this IServiceCollection services, IConfiguration configuration, string tokenValidationSectionName = "TokenValidation", ILogger logger = null)
    {
        IConfigurationSection tokenValidationSection = configuration.GetSection(tokenValidationSectionName);
        List<string> validTokenIssuers = tokenValidationSection.GetSection("ValidIssuers").Get<List<string>>();
        List<string> audiences = tokenValidationSection.GetSection("Audiences").Get<List<string>>();

        if (!tokenValidationSection.Exists())
        {
            logger?.LogError("Missing configuration section '{tokenValidationSectionName}'. This section is required to be present in appsettings.json",tokenValidationSectionName);
            throw new InvalidOperationException($"Missing configuration section '{tokenValidationSectionName}'. This section is required to be present in appsettings.json");
        }

        // If ValidIssuers is empty, default for ABS Public Cloud
        if (validTokenIssuers == null || validTokenIssuers.Count == 0)
        {
            validTokenIssuers =
            [
                "https://api.botframework.com",
                "https://sts.windows.net/d6d49420-f39b-4df7-a1dc-d59a935871db/",
                "https://login.microsoftonline.com/d6d49420-f39b-4df7-a1dc-d59a935871db/v2.0",
                "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/",
                "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a/v2.0",
                "https://sts.windows.net/69e9b82d-4842-4902-8d1e-abc5b98a55e8/",
                "https://login.microsoftonline.com/69e9b82d-4842-4902-8d1e-abc5b98a55e8/v2.0",
            ];

            string tenantId = tokenValidationSection["TenantId"];
            if (!string.IsNullOrEmpty(tenantId))
            {
                validTokenIssuers.Add(string.Format(CultureInfo.InvariantCulture, AuthenticationConstants.ValidTokenIssuerUrlTemplateV1, tenantId));
                validTokenIssuers.Add(string.Format(CultureInfo.InvariantCulture, AuthenticationConstants.ValidTokenIssuerUrlTemplateV2, tenantId));
            }
        }

        if (audiences == null || audiences.Count == 0)
        {
            throw new ArgumentException($"{tokenValidationSectionName}:Audiences requires at least one value");
        }

        bool isGov = tokenValidationSection.GetValue("IsGov", false);
        bool azureBotServiceTokenHandling = tokenValidationSection.GetValue("AzureBotServiceTokenHandling", true);

        // If the `AzureBotServiceOpenIdMetadataUrl` setting is not specified, use the default based on `IsGov`.  This is what is used to authenticate ABS tokens.
        string azureBotServiceOpenIdMetadataUrl = tokenValidationSection["AzureBotServiceOpenIdMetadataUrl"];
        if (string.IsNullOrEmpty(azureBotServiceOpenIdMetadataUrl))
        {
            azureBotServiceOpenIdMetadataUrl = isGov ? AuthenticationConstants.GovAzureBotServiceOpenIdMetadataUrl : AuthenticationConstants.PublicAzureBotServiceOpenIdMetadataUrl;
        }

        // If the `OpenIdMetadataUrl` setting is not specified, use the default based on `IsGov`.  This is what is used to authenticate Entra ID tokens.
        string openIdMetadataUrl = tokenValidationSection["OpenIdMetadataUrl"];
        if (string.IsNullOrEmpty(openIdMetadataUrl))
        {
            openIdMetadataUrl = isGov ? AuthenticationConstants.GovOpenIdMetadataUrl : AuthenticationConstants.PublicOpenIdMetadataUrl;
        }

        TimeSpan openIdRefreshInterval = tokenValidationSection.GetValue("OpenIdMetadataRefresh", BaseConfigurationManager.DefaultAutomaticRefreshInterval);

        _ = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidIssuers = validTokenIssuers,
                ValidAudiences = audiences,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
            };

            // Using Microsoft.IdentityModel.Validators
            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

            options.Events = new JwtBearerEvents
            {
                // Create a ConfigurationManager based on the requestor.  This is to handle ABS non-Entra tokens.
                OnMessageReceived = async context =>
                {
                    string authorizationHeader = context.Request.Headers.Authorization.ToString();

                    if (string.IsNullOrEmpty(authorizationHeader))
                    {
                        // Default to AadTokenValidation handling
                        context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    string[] parts = authorizationHeader?.Split(' ');
                    if (parts.Length != 2 || parts[0] != "Bearer")
                    {
                        // Default to AadTokenValidation handling
                        context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    JwtSecurityToken token = new(parts[1]);
                    string issuer = token.Claims.FirstOrDefault(claim => claim.Type == AuthenticationConstants.IssuerClaim)?.Value;

                    if (azureBotServiceTokenHandling && AuthenticationConstants.BotFrameworkTokenIssuer.Equals(issuer))
                    {
                        // Use the Azure Bot authority for this configuration manager
                        context.Options.TokenValidationParameters.ConfigurationManager = _openIdMetadataCache.GetOrAdd(azureBotServiceOpenIdMetadataUrl, key =>
                        {
                            return new ConfigurationManager<OpenIdConnectConfiguration>(azureBotServiceOpenIdMetadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpClient())
                            {
                                AutomaticRefreshInterval = openIdRefreshInterval
                            };
                        });
                    }
                    else
                    {
                        context.Options.TokenValidationParameters.ConfigurationManager = _openIdMetadataCache.GetOrAdd(openIdMetadataUrl, key =>
                        {
                            return new ConfigurationManager<OpenIdConnectConfiguration>(openIdMetadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpClient())
                            {
                                AutomaticRefreshInterval = openIdRefreshInterval
                            };
                        });
                    }

                    await Task.CompletedTask.ConfigureAwait(false);
                },

                OnTokenValidated = context =>
                {
                    logger?.LogDebug("TOKEN Validated");
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    logger?.LogWarning("Forbidden: {m}", context.Result.ToString());
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    logger?.LogWarning("Auth Failed {m}", context.Exception.ToString());
                    return Task.CompletedTask;
                }
            };
        });
    }
}

```

## Samples/basic/authorization/obo-authorization/dotnet/OBOAuthorization.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.CopilotStudio.Client" Version="1.1.*-*"/>
	</ItemGroup>

</Project>

```

## Samples/basic/authorization/obo-authorization/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Logging.AddConsole();
builder.Logging.AddDebug();


// Add AgentApplicationOptions from config.
builder.AddAgentApplicationOptions();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Add the Agent
builder.AddAgent(sp =>
{
    const string MCSConversationPropertyName = "MCSConversationId";

    var app = new AgentApplication(sp.GetRequiredService<AgentApplicationOptions>());

    CopilotClient GetClient(AgentApplication app, ITurnContext turnContext)
    {
        var settings = new ConnectionSettings(builder.Configuration.GetSection("CopilotStudioAgent"));
        string[] scopes = [CopilotClient.ScopeFromSettings(settings)];

        return new CopilotClient(
            settings,
            sp.GetService<IHttpClientFactory>(),
            tokenProviderFunction: async (s) =>
            {
                // In this sample, the Azure Bot OAuth Connection is configured to return an 
                // exchangeable token, that can be exchange for different scopes.  This can be
                // done multiple times using different scopes.
                return await app.UserAuthorization.ExchangeTurnTokenAsync(turnContext, "mcs", exchangeScopes: scopes);
            },
            NullLogger.Instance,
            "mcs");
    }

    // Since Auto SignIn is enabled, by the time this is called the token is already available via UserAuthorization.GetTurnTokenAsync or
    // UserAuthorization.ExchangeTurnTokenAsync.
    // NOTE:  This is a slightly unusual way to handle incoming Activities (but perfectly) valid.  For this sample,
    // we just want to proxy messages to/from a Copilot Studio Agent.
    app.OnActivity((turnContext, cancellationToken) => Task.FromResult(true), async (turnContext, turnState, cancellationToken) =>
    {
        var mcsConversationId = turnState.Conversation.GetValue<string>(MCSConversationPropertyName);
        var cpsClient = GetClient(app, turnContext);

        if (string.IsNullOrEmpty(mcsConversationId))
        {
            // Regardless of the Activity  Type, start the conversation.
            await foreach (IActivity activity in cpsClient.StartConversationAsync(emitStartConversationEvent: true, cancellationToken: cancellationToken))
            {
                if (activity.IsType(ActivityTypes.Message))
                {
                    await turnContext.SendActivityAsync(activity.Text, cancellationToken: cancellationToken);

                    // Record the conversationId MCS is sending. It will be used this for subsequent messages.
                    turnState.Conversation.SetValue(MCSConversationPropertyName, activity.Conversation.Id);
                }
            }
        }
        else if (turnContext.Activity.IsType(ActivityTypes.Message))
        {
            // Send the Copilot Studio Agent whatever the sent and send the responses back.
            await foreach (IActivity activity in cpsClient.AskQuestionAsync(turnContext.Activity.Text, mcsConversationId, cancellationToken))
            {
                if (activity.IsType(ActivityTypes.Message))
                {
                    await turnContext.SendActivityAsync(activity.Text, cancellationToken: cancellationToken);
                }
            }
        }
    });

    app.UserAuthorization.OnUserSignInFailure(async (turnContext, turnState, handlerName, response, initiatingActivity, cancellationToken) =>
    {
        await turnContext.SendActivityAsync($"SignIn failed with '{handlerName}': {response.Cause}/{response.Error.Message}", cancellationToken: cancellationToken);
    });

    return app;
});


// Configure the HTTP request pipeline.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
})
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");
app.MapGet("/", () => "Microsoft Agents SDK Sample");

app.Run();

```

## Samples/basic/authorization/obo-authorization/dotnet/appsettings.json

```json
{
  "AgentApplication": {
    "StartTypingTimer": true,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false,

    "UserAuthorization": {
      "AutoSignIn":  true,
      "Handlers": {
        "mcs": {
          "Settings": {
            "AzureBotOAuthConnectionName": "{{AzureBotOAuthConnectionName}}",
            "OBOConnectionName": "MCSConnection"  // OBOScopes are resolved at runtime
          }
        }
      }
    }
  },

  "CopilotStudioAgent": {
    "EnvironmentId": null,
    "SchemaName": null
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    },
    "MCSConnection": {
      "Settings": {
        "AuthType": "ClientSecret",
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{OAuthTenantId}}",
        "ClientId": "{{OAuthClientId}}", // this is the Client ID used for the exchange.
        "ClientSecret": "{{OAuthClientSecret}}" // this is the Client Secret used for the exchange.
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}

```

## Samples/basic/authorization/obo-authorization/dotnet/README.md

```markdown
﻿# OBO OAuth

This Agent has been created using [Microsoft 365 Agents Framework](https://github.com/microsoft/agents-for-net), it shows how to use authorization in your Agent using OAuth and OBO.

- The sample uses the Agent SDK User Authorization capabilities in [Azure Bot Service](https://docs.botframework.com), providing features to make it easier to develop an Agent that authorizes users with various identity providers such as Azure AD (Azure Active Directory), GitHub, Uber, etc.
- This sample shows how to use an OBO Exchange to communicate with Microsoft Copilot Studio using the CopilotStudioClient.

- ## Prerequisites

-  [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
-  [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Running this sample

1. Create a Agent in [Copilot Studio](https://copilotstudio.microsoft.com)
   1. Publish your newly created Copilot
   1. Go to Settings => Advanced => Metadata and copy the following values. You will need them later:
      1. Schema name
      1. Environment Id
       
2. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use in the "ServiceConnection" settings below.

3. Setting up OAuth for an exchangeable token 
   1. Create a new App Registration
      1. SingleTenant
      1. Give it a name and click **Register**
      1. **Authentication** tab
         1. **Add Platform**, then **Web**, Set `Redirect URI` to `Web` and `https://token.botframework.com/.auth/web/redirect`
         1. **Add Platform**, then **Mobile and desktop applications**, and add an additional `http://localhost` Uri.
      1. **API Permissions** tab
         1. **Dynamics CRM** with **user_impersonation**
         1. **Graph** with **User.Read**
         1. **Power Platform API** with **CopilotStudio.Copilots.Invoke**
         1. Grant Admin Consent for your tenant.
      1. **Expose an API** tab
         1. Click **Add a Scope**
         1. **Application ID URI** should be: `api://botid-{{appid}}`
         1. **Scope Name** is "defaultScope"
         1. **Who can consent** is **Admins and users**
         1. Enter values for the required Consent fields
      1. **Certificates & secrets**
         1. Create a new secret and record the value.  This will be used later.
         
4. Create Azure Bot **OAuth Connection**
   1. On the Azure Bot created in Step #2, Click **Configuration** tab then the **Add OAuth Connection Settings** button.
   1. Enter a **Name**.  This will be used later.
   1. For **Service Provider** select **Azure Active Directory v2**
   1. **Client id** and **Client Secret** are the values created in step #3.
   1. Enter the **Tenant ID**
   1. **Scopes** is `api://botid-{{appid}}/defaultScope`

1. Configuring the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL authorization provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "Connections": {
          "ServiceConnection": {
          "Settings": {
              "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.authorization.Msal.Model.AuthTypes.  The default is ClientSecret.
              "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
              "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
              "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
              "Scopes": [
                "https://api.botframework.com/.default"
              ]
          }
      }
      ```

      1. Replace all **{{ClientId}}** with the AppId of the bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      
   1. In the same section, edit the **MCSConnection**
      ```json
        "MCSConnection": {
        "Settings": {
          "AuthType": "ClientSecret",
          "AuthorityEndpoint": "https://login.microsoftonline.com/{{OAuthTenantId}}",
          "ClientId": "{{OAuthClientId}}", // this is the Client ID created in Step #3
          "ClientSecret": "{{OAuthClientSecret}}" // this is the Client Secret created in Step #3
        }
      ```
      1. Set **{{OAuthTenantId}}** to the AppId created in Step #3
      1. Set **{{OAuthClientSecret}}** to the secret created in Step #3
      1. Set **{{OAuthTenantId}}**
      
   1. In appsettings, replace **{{AzureBotOAuthConnectionName}}** with the OAuth Connection Name created in Step #4.

   1. Setup the Copilot Studio Agent information
      ```json
      "CopilotStudioAgent": {
        "EnvironmentId": "", // Environment ID of environment with the CopilotStudio App.
        "SchemaName": "", // Schema Name of the Copilot to use
      }
      ```
   
1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. Update your Azure Bot ``Messaging endpoint`` with the tunnel Url:  `{tunnel-url}/api/messages`

1. Run the bot from a terminal or from Visual Studio

1. Test via "Test in WebChat"" on your Azure Bot in the Azure Portal.

## Running this Agent in Teams

1. There are two version of the manifest provided.  One for M365 Copilot and one for Teams.
   1. Copy the desired version to `manifest.json`.  This will typically be `teams-manifest.json` for Teams.
1. Manually update the manifest.json
   - Edit the `manifest.json` contained in the `/appManifest` folder
     - Replace with your AppId (that was created above) *everywhere* you see the place holder string `<<AAD_APP_CLIENT_ID>>`
     - Replace `<<BOT_DOMAIN>>` with your Agent url.  For example, the tunnel host name.
   - Zip up the contents of the `/appManifest` folder to create a `manifest.zip`
     - `manifest.json`
     - `outline.png`
     - `color.png`
1. Upload the `manifest.zip` to Teams
   - Select **Developer Portal** in the Teams left sidebar
   - Select **Apps** (top row)
   - Select **Import app**, and select the manifest.zip

1. Select **Preview in Teams** in the upper right corner

## Further reading
To learn more about building Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.


```

## Samples/basic/authorization/obo-authorization/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.obooauth",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "User Authorization (OAuth+OBO)",
    "full": "User Authorization (OAuth+OBO)"
  },
  "description": {
    "short": "Sample demonstrating Azure Bot Services user authorization with using a Agent.",
    "full": "This sample demonstrates how to integrate with Microsoft Copilot Studio using OBO and CopilotStudioClient."
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<BOT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/basic/authorization/obo-authorization/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.obooauth",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "User Authorization (OAuth+OBO)",
    "full": "User Authorization (OAuth+OBO)"
  },
  "description": {
    "short": "Sample demonstrating Azure Bot Services user authorization with using a Agent.",
    "full": "This sample demonstrates how to integrate with Microsoft Copilot Studio using OBO and CopilotStudioClient."
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<BOT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/basic/authorization/obo-authorization/dotnet/appManifest/.gitignore

```ignore
manifest.json

```

## Samples/basic/authorization/auto-signin/dotnet/Program.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add AgentApplicationOptions from appsettings config.
builder.AddAgentApplicationOptions();

// Add the Agent
builder.AddAgent<AuthAgent>();

// Register IStorage.  For development, MemoryStorage is suitable.
// For production Agents, persisted storage should be used so
// that state survives Agent restarts, and operate correctly
// in a cluster of Agent instances.
builder.Services.AddSingleton<IStorage, MemoryStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
    {
        await adapter.ProcessAsync(request, response, agent, cancellationToken);
    })
    .AllowAnonymous();

// Hardcoded for brevity and ease of testing. 
// In production, this should be set in configuration.
app.Urls.Add($"http://localhost:3978");
app.MapGet("/", () => "Microsoft Agents SDK Sample");

app.Run();


```

## Samples/basic/authorization/auto-signin/dotnet/AutoSignIn.csproj

```
﻿<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<LangVersion>latest</LangVersion>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Agents.Authentication.Msal" Version="1.1.*-*" />
		<PackageReference Include="Microsoft.Agents.Hosting.AspNetCore" Version="1.1.*-*" />
	</ItemGroup>

</Project>

```

## Samples/basic/authorization/auto-signin/dotnet/appsettings.json

```json
{
  "AgentApplication": {
    "StartTypingTimer": true,
    "RemoveRecipientMention": false,
    "NormalizeMentions": false,

    "UserAuthorization": {
      "DefaultHandlerName": "auto",
      "AutoSignin": true,
      "Handlers": {
        "auto": {
          "Settings": {
            "AzureBotOAuthConnectionName": "{{auto_connection_name}}",
            "Title": "SigIn for Sample",
            "Text": "Please sign in and send the 6-digit code"
          }
        },
        "me": {
          "Settings": {
            "AzureBotOAuthConnectionName": "{{me_connection_name}}",
            "Title": "SigIn for Me",
            "Text": "Please sign in and send the 6-digit code"
          }
        }
      }
    }
  },

  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}", // this is the Client ID used for the Azure Bot
        "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
        "Scopes": [
          "https://api.botframework.com/.default"
        ]
      }
    }
  },
  "ConnectionsMap": [
    {
      "ServiceUrl": "*",
      "Connection": "ServiceConnection"
    }
  ],

  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Agents": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}

```

## Samples/basic/authorization/auto-signin/dotnet/README.md

```markdown
﻿# AutoSignIn

This Agent has been created using [Microsoft 365 Agents Framework](https://github.com/microsoft/agents-for-net), it shows how to use Auto SignIn user authorization in your Agent.

This sample:
- Gets an OAuth token automatically for every message sent by the user
  > This is done by setting the `AgentApplication:UserAuthorization:AutoSign` setting to true.  This will use the default UserAuthorization Handler to automatically get a token for all incoming Activities.  Use this when your Agents needs the same token for much of it's functionality.
- Per-Route sign in .  In this sample, this is the `-me` message.
  > Messages the user sends are routed to a handler of your choice.  This feature allows you to indicate that a particular token is needed for the handler.  Per-Route user authorization will automatically handle the OAuth flow to get the token and make it available to your Agent.  Keep in mind that if the the Auto SignIn option is enabled, you actually have two tokens available in the handler.

The sample uses the bot OAuth capabilities in [Azure Bot Service](https://docs.botframework.com), providing features to make it easier to develop a bot that authorizes users to various identity providers such as Azure AD (Azure Active Directory), GitHub, Uber, etc.

- ## Prerequisites

-  [.Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) version 8.0
-  [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)

## Running this sample

1. [Create an Azure Bot](https://aka.ms/AgentsSDK-CreateBot)
   - Record the Application ID, the Tenant ID, and the Client Secret for use below

1. [Add OAuth to your bot](https://aka.ms/AgentsSDK-AddAuth)

1. For purposes of this sample, create a second Azure Bot **OAuth Connection** as a copy of the one just created.
   > This is to ease setup of this sample.  In an actual Agent, this second connection would be setup separately, with different API Permissions and scopes specific to the external service being accessed. \
   > This OAuth Connection is for the `-me` message in this sample.   

1. Configuring the token connection in the Agent settings
   > The instructions for this sample are for a SingleTenant Azure Bot using ClientSecrets.  The token connection configuration will vary if a different type of Azure Bot was configured.  For more information see [DotNet MSAL Authentication provider](https://aka.ms/AgentsSDK-DotNetMSALAuth)

   1. Open the `appsettings.json` file in the root of the sample project.

   1. Find the section labeled `Connections`,  it should appear similar to this:

      ```json
      "Connections": {
          "ServiceConnection": {
          "Settings": {
              "AuthType": "ClientSecret", // this is the AuthType for the connection, valid values can be found in Microsoft.Agents.Authentication.Msal.Model.AuthTypes.  The default is ClientSecret.
              "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
              "ClientId": "{{ClientId}}", // this is the Client ID used for the connection.
              "ClientSecret": "00000000-0000-0000-0000-000000000000", // this is the Client Secret used for the connection.
              "Scopes": [
                "https://api.botframework.com/.default"
              ]
          }
      }
      ```

      1. Replace all **{{ClientId}}** with the AppId of the bot.
      1. Replace all **{{TenantId}}** with the Tenant Id where your application is registered.
      1. Set the **ClientSecret** to the Secret that was created for your identity.
      
      > Storing sensitive values in appsettings is not recommend.  Follow [AspNet Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0) for best practices.

1. Configure the UserAuthorization handlers
   1. Open the `appsettings.json` file and locate
      ```json
      "AgentApplication": {
        "UserAuthorization": {
          "DefaultHandlerName": "auto",
          "AutoSignin": true,
          "Handlers": {
            "auto": {
              "Settings": {
                "AzureBotOAuthConnectionName": "{{auto_connection_name}}",
                "Title": "SigIn for Sample",
                "Text":  "Please sign in and send the 6-digit code"
              }
            },
           "me": {
             "Settings": {
               "AzureBotOAuthConnectionName": "{{me_connection_name}}",
               "Title": "SigIn for Me",
               "Text": "Please sign in and send the 6-digit code"
             }
          }
        }
      }
      ```

      1. Replace **{{auto_connection_name}}** with the first **OAuth Connection** name created
      1. Replace **{{me_connection_name}}** with the second **OAuth Connection** name created

1. Run `dev tunnels`. Please follow [Create and host a dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) and host the tunnel with anonymous user access command as shown below:

   ```bash
   devtunnel host -p 3978 --allow-anonymous
   ```

1. Update your Azure Bot ``Messaging endpoint`` with the tunnel Url:  `{tunnel-url}/api/messages`

1. Run the bot from a terminal or from Visual Studio

1. Test via "Test in WebChat"" on your Azure Bot in the Azure Portal.

## Running this Agent in Teams

1. There are two version of the manifest provided.  One for M365 Copilot and one for Teams.
   1. Copy the desired version to manifest.json
1. Manually update the manifest.json
   - Edit the `manifest.json` contained in the `/appManifest` folder
     - Replace with your AppId (that was created above) *everywhere* you see the place holder string `<<AAD_APP_CLIENT_ID>>`
     - Replace `<<AGENT_DOMAIN>>` with your Agent url.  For example, the tunnel host name.
   - Zip up the contents of the `/appManifest` folder to create a `manifest.zip`
1. Upload the `manifest.zip` to Teams
   - Select **Developer Portal** in the Teams left sidebar
   - Select **Apps** (top row)
   - Select **Import app**, and select the manifest.zip

1. Select **Preview in Teams** in the upper right corner

## Interacting with the Agent

- When the conversation starts, you will be greeted with a welcome message which include your name and instructions.  If this is the first time you've interacted with the Agent in a conversation, you will be prompts to sign in (WebChat), or with Teams SSO the OAuth will happen silently.
- Sending `-me` will display additional information about you.
- Note that if running this in Teams and SSO is setup, you shouldn't see any "sign in" prompts.  This is true in this sample since we are only requesting a basic set of scopes that Teams doesn't require additional consent for.

## Further reading
To learn more about building Agents, see our [Microsoft 365 Agents SDK](https://github.com/microsoft/agents) repo.


```

## Samples/basic/authorization/auto-signin/dotnet/AuthAgent.cs

```csharp
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.App.UserAuth;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Builder.UserAuth;
using Microsoft.Agents.Core.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;


public class AuthAgent : AgentApplication
{
    /// <summary>
    /// Default Sign In Name
    /// </summary>
    private string _defaultDisplayName = "Unknown User";

    /// <summary>
    /// Describes the agent registration for the Authorization Agent
    /// This agent will handle the sign-in and sign-out OAuth processes for a user.
    /// </summary>
    /// <param name="options">AgentApplication Configuration objects to configure and setup the Agent Application</param>
    public AuthAgent(AgentApplicationOptions options) : base(options)
    {
        // During setup of the Agent Application, Register Event Handlers for the Agent. 
        // For this example we will register a welcome message for the user when they join the conversation, then configure sign-in and sign-out commands.
        // Additionally, we will add events to handle notifications of sign-in success and failure,  these notifications will report the local log instead of back to the calling agent.

        // This constructor should only register events and setup state as it will be called for each request. 
        
        // When a conversation update event is triggered. 
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        // Demonstrates the use of Per-Route Auto sign-in.  This will automatically get a token using the indicated OAuth handler for this message route.
        // This Route will automatically get a token using the "me" UserAuthorization.Handler in config.
        OnMessage("-me", OnMe, autoSignInHandlers: ["me"]);

        // Handles the user sending a SignOut command using the specific keywords '-signout'
        OnMessage("-signout", async (turnContext, turnState, cancellationToken) =>
        {
            // Force a user signout to reset the user state
            // This is needed to reset the token in Azure Bot Services if needed. 
            // Typically this wouldn't be need in a production Agent.  Made available to assist it starting from scratch.
            await UserAuthorization.SignOutUserAsync(turnContext, turnState, cancellationToken: cancellationToken);
            await UserAuthorization.SignOutUserAsync(turnContext, turnState, "me", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("You have signed out", cancellationToken: cancellationToken);
        }, rank: RouteRank.Last);

        // Registers a general event handler that will pick up any message activity that is not covered by the previous events handlers. 
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);

        // The UserAuthorization Class provides methods and properties to manage and access user authorization tokens
        // You can use this class to interact with the UserAuthorization process, including signing in and signing out users, accessing tokens, and handling authorization events.

        // Register Events for SignIn Failure on the UserAuthorization class to notify the Agent in the event of an OAuth failure.
        // For a production Agent, this would typically be used to provide instructions to the end-user.  For example, call/email or
        // handoff to a live person (depending on Agent capabilities).
        UserAuthorization.OnUserSignInFailure(OnUserSignInFailure);
    }

    /// <summary>
    /// This method is called to handle the conversation update event when a new member is added to or removed from the conversation.
    /// </summary>
    /// <param name="turnContext"><see cref="ITurnContext"/></param>
    /// <param name="turnState"><see cref="ITurnState"/></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        // In this example, we will send a welcome message to the user when they join the conversation.
        // We do this by iterating over the incoming activity members added to the conversation and checking if the member is not the agent itself.
        // Then a greeting notice is provided to each new member of the conversation.
        
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                string displayName = await GetDisplayName(turnContext);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Welcome to the AutoSignIn Example, **{displayName}**!");
                sb.AppendLine("This Agent automatically signs you in when you first connect.");
                sb.AppendLine("You can use the following commands to interact with the agent:");
                sb.AppendLine("**-me**: Displays detailed information using a different OAuth Token.");
                sb.AppendLine("**-signout**: Sign out of the agent and force it to reset the login flow on next message.");
                if (displayName.Equals(_defaultDisplayName))
                {
                    sb.AppendLine("**WARNING: We were unable to get your display name with the current access token.. please use the -signout command before proceeding**");
                }
                sb.AppendLine("");
                sb.AppendLine("Type anything else to see the agent echo back your message.");
                await turnContext.SendActivityAsync(MessageFactory.Text(sb.ToString()), cancellationToken);
                sb.Clear();
            }
        }
    }

    /// Handles -me, using a different OAuthConnection to show Per-Route OAuth. 
    /// </summary>
    /// <param name="turnContext"><see cref="ITurnContext"/></param>
    /// <param name="turnState"><see cref="ITurnState"/></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    private async Task OnMe(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        // If successful, the user will be the token will be available from the UserAuthorization.GetTurnTokenAsync(turnContext, DefaultHandlerName) call. 
        // If not successful, this handler won't be reached.  Instead, OnUserSignInFailure handler would have been called. 

        // For this sample, two OAuth Connections are setup to demonstrate multiple OAuth Connections and Auto SignIn handling and routing.
        // For ease of setup, both connections are using the same App Reg for API Permissions.  In a real Agent, these would be using different
        // App Registrations with different permissions. In this cases, we are using two different tokens to access external services.

        var displayName = await GetDisplayName(turnContext);
        var graphInfo = await GetGraphInfo(turnContext, "me");

        // Just to verify "auto" handler setup.  This wouldn't be needed in a production Agent and here just to verify sample setup.
        if (displayName.Equals(_defaultDisplayName) || graphInfo == null)
        {
            await turnContext.SendActivityAsync($"Failed to get information from handlers '{UserAuthorization.DefaultHandlerName}' and/or 'me'. \nDid you update the scope correctly in Azure bot Service?. If so type in -signout to force signout the current user", cancellationToken: cancellationToken);
            return;
        }

        // Just to verify we in fact have two different tokens.  This wouldn't be needed in a production Agent and here just to verify sample setup.
        if (await UserAuthorization.GetTurnTokenAsync(turnContext, UserAuthorization.DefaultHandlerName, cancellationToken: cancellationToken) == await UserAuthorization.GetTurnTokenAsync(turnContext, "me"))
        {
            await turnContext.SendActivityAsync($"It would seem '{UserAuthorization.DefaultHandlerName}' and 'me' are using the same OAuth Connection", cancellationToken: cancellationToken);
            return;
        }

        var meInfo = $"Name: {displayName}\r\nJob Title: {graphInfo["jobTitle"].GetValue<string>()}\r\nEmail: {graphInfo["mail"].GetValue<string>()}";
        await turnContext.SendActivityAsync(meInfo, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Handles general message loop. 
    /// </summary>
    /// <param name="turnContext"><see cref="ITurnContext"/></param>
    /// <param name="turnState"><see cref="ITurnState"/></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        // When Auto Sign in is properly configured, the user will be automatically signed in when they first connect to the agent using the default
        // handler chosen in the UserAuthorization configuration.
        // IMPORTANT: The ReadMe associated with this sample, instructs you on configuring the Azure Bot Service Registration with the scopes to allow
        // you to read your own information from Graph.  you must have completed that for this sample to work correctly. 

        // If successful, the user will be the token will be available from the UserAuthorization.GetTurnTokenAsync(turnContext, DefaultHandlerName) call. 
        // If not successful, this handler won't be reached.  Instead, OnUserSignInFailure handler would have been called. 
        
        // We have the access token, now try to get your user name from graph. 
        string displayName = await GetDisplayName(turnContext);
        if (displayName.Equals(_defaultDisplayName))
        {
            // Handle error response from Graph API
            await turnContext.SendActivityAsync($"Failed to get user information from Graph API \nDid you update the scope correctly in Azure bot Service?. If so type in -signout to force signout the current user", cancellationToken: cancellationToken);
            return;
        }

        // Now Echo back what was said with your display name. 
        await turnContext.SendActivityAsync($"**{displayName} said:** {turnContext.Activity.Text}", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// This method is called when the sign-in process fails with an error indicating why . 
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="turnState"></param>
    /// <param name="handlerName"></param>
    /// <param name="response"></param>
    /// <param name="initiatingActivity"></param>
    /// <param name="cancellationToken"></param>
    private async Task OnUserSignInFailure(ITurnContext turnContext, ITurnState turnState, string handlerName, SignInResponse response, IActivity initiatingActivity, CancellationToken cancellationToken)
    {
        // Raise a notification to the user that the sign-in process failed.  In a production Agent, this would be used
        // to display alternative ways to get help, or in some cases transfer to a live agent.
        await turnContext.SendActivityAsync($"Sign In: Failed to login to '{handlerName}': {response.Cause}/{response.Error.Message}", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the display name of the user from the Graph API using the access token.
    /// </summary>
    private async Task<string> GetDisplayName(ITurnContext turnContext)
    {
        string displayName = _defaultDisplayName;
        var graphInfo = await GetGraphInfo(turnContext, UserAuthorization.DefaultHandlerName);
        if (graphInfo != null)
        {
            displayName = graphInfo!["displayName"].GetValue<string>();
        }
        return displayName;
    }

    private async Task<JsonNode> GetGraphInfo(ITurnContext turnContext, string handleName)
    {
        string accessToken = await UserAuthorization.GetTurnTokenAsync(turnContext, handleName);
        string graphApiUrl = $"https://graph.microsoft.com/v1.0/me";
        try
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.GetAsync(graphApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonNode.Parse(content);
            }
        }
        catch (Exception ex)
        {
            // Handle error response from Graph API
            System.Diagnostics.Trace.WriteLine($"Error getting display name: {ex.Message}");
        }
        return null;
    }
}

```

## Samples/basic/authorization/auto-signin/dotnet/appManifest/teams-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.oauth",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "AgentSDK Auto SignIn (OAuth)",
    "full": "AgentSDK Automatic User Sign-in Authorization (OAuth)"
  },
  "description": {
    "short": "Sample demonstrating AgentSDK + Azure Bot Services for user authorization.",
    "full": "This sample demonstrates how to integrate Azure AD authorization in an Agent with Single Sign-On (SSO) capabilities built with the AgentSDK"
  },
  "accentColor": "#FFFFFF",
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "commandLists": [
        {
          "scopes": [
            "team",
            "personal"
          ],
          "commands": [
            {
              "title": "SignOut",
              "description": "this issues a signout command to the AutoSignin Agent example"
            }
          ]
        }
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<AGENT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/basic/authorization/auto-signin/dotnet/appManifest/m365copilot-manifest.json

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/vdevPreview/MicrosoftTeams.schema.json",
  "manifestVersion": "devPreview",
  "version": "1.0.0",
  "id": "${{AAD_APP_CLIENT_ID}}",
  "packageName": "com.microsoft.agents.copilotoauth",
  "developer": {
    "name": "Microsoft, Inc.",
    "websiteUrl": "https://example.azurewebsites.net",
    "privacyUrl": "https://example.azurewebsites.net/privacy",
    "termsOfUseUrl": "https://example.azurewebsites.net/termsofuse"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "AgentSDK Auto SignIn (OAuth)",
    "full": "AgentSDK Automatic User Sign-in Authorization (OAuth)"
  },
  "description": {
    "short": "Sample demonstrating AgentSDK + Azure Bot Services for user authorization.",
    "full": "This sample demonstrates how to integrate Azure AD authorization in an Agent with Single Sign-On (SSO) capabilities built with the AgentSDK"
  },
  "accentColor": "#FFFFFF",
  "copilotAgents": {
    "customEngineAgents": [
      {
        "id": "${{AAD_APP_CLIENT_ID}}",
        "type": "bot"
      }
    ]
  },
  "bots": [
    {
      "botId": "${{AAD_APP_CLIENT_ID}}",
      "scopes": [
        "personal"
      ],
      "commandLists": [
        {
          "scopes": [
            "team",
            "personal"
          ],
          "commands": [
            {
              "title": "SignOut",
              "description": "this issues a signout command to the AutoSignin Agent example"
            }
          ]
        }
      ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [
    "identity",
    "messageTeamMembers"
  ],
  "validDomains": [
    "token.botframework.com",
    "<<AGENT_DOMAIN>>"
  ],
  "webApplicationInfo": {
    "id": "${{AAD_APP_CLIENT_ID}}",
    "resource": "api://botid-${{AAD_APP_CLIENT_ID}}"
  }
}
```

## Samples/basic/authorization/auto-signin/dotnet/appManifest/.gitignore

```ignore
manifest.json

```

