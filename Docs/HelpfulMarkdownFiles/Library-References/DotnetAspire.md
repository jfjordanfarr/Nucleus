---
title: What's new in .NET Aspire 9.2
description: Learn what's new in the official general availability release of .NET Aspire 9.2.
ms.date: 04/10/2025
---

# What's new in .NET Aspire 9.2

📢 .NET Aspire 9.2 is the next minor version release of .NET Aspire; it supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://discord.com/invite/h87kDAHQgJ) to chat with team members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of .NET Aspire align with .NET major versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product life cycle details.

## ⬆️ Upgrade to .NET Aspire 9.2

> [!IMPORTANT]
> If you are using `azd` to deploy Azure PostgreSQL or Azure SQL Server, you now have to configure Azure Managed Identities. For more information, see [🛡️ Improved Managed Identity defaults](#improved-managed-identity-defaults).

Moving between minor releases of .NET Aspire is simple:

1. In your app host project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.2.0`:

    ```diff
    <Project Sdk="Microsoft.NET.Sdk">

        <Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0" />
        
        <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net9.0</TargetFramework>
    -       <IsAspireHost>true</IsAspireHost>
            <!-- Omitted for brevity -->
        </PropertyGroup>
        
        <ItemGroup>
            <PackageReference Include="Aspire.Hosting.AppHost" Version="9.2.0" />
        </ItemGroup>
    
        <!-- Omitted for brevity -->
    </Project>
    ```

    > [!IMPORTANT]
    > The `IsAspireHost` property is no longer required in the project file. For more information, see [🚧 Project file changes](#project-file-changes).

    For more information, see [.NET Aspire SDK](xref:dotnet/aspire/sdk).

1. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command in VS Code.
1. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new update
    ```

    > [!IMPORTANT]
    > The `dotnet new update` command updates all of your templates to the latest version.

If your app host project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9.0, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).

## 🖥️ App host enhancements

The [app host](../fundamentals/app-host-overview.md) is the core of .NET Aspire, providing the local hosting environment for your distributed applications. In .NET Aspire 9.2, we've made several improvements to the app host:

### 🚧 Project file changes

<span id="project-file-changes"></span>

The .NET Aspire app host project file no longer requires the `IsAspireHost` property. This property was moved to the `Aspire.AppHost.Sdk` SDK, therefore, you can remove it from your project file. For more information, see [dotnet/aspire issue #8144](https://github.com/dotnet/aspire/pull/8144).

### 🔗 Define custom resource URLs

Resources can now define custom URLs. This makes it easier to build custom experiences for your resources. For example, you can define a custom URL for a database resource that points to the database management console. This makes it easier to access the management console directly from the dashboard, you can even give it a friendly name.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithDataVolume()
                       .WithPgAdmin(resource =>
                       {
                           resource.WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");
                       })
                       .AddDatabase("catalogdb");
```

The preceding code sets the display text for the `PG Admin` URL to `PG Admin`. This makes it easier to access the management console directly from the dashboard.

For more information, see [Define custom resource URLs](../fundamentals/custom-resource-urls.md).

## 🔧 Dashboard user experience improvements

.NET Aspire 9.2 adds new features to the [dashboard](../fundamentals/dashboard/overview.md), making it a more powerful developer tool than ever. The following features were added to the dashboard in .NET Aspire 9.2:

### 🧩 Resource graph

The resource graph is a new way to visualize the resources in your apps. It displays a graph of resources, linked by relationships. Click the 'Graph' tab on the Resources page to view the resource graph. See it in action on [James's BlueSky](https://bsky.app/profile/james.newtonking.com/post/3lj7odu4re22p).

For more information, see [.NET Aspire dashboard: Resources page](../fundamentals/dashboard/explore.md#resources-page).

### 🎨 Resource icons

We've added resource icons to the resources page. The icon color matches the resource's telemetry in structured logs and traces.

:::image type="content" source="media/dashboard-resource-icons.png" lightbox="media/dashboard-resource-icons.png" alt-text="Screenshot of dashboard resource's page showing the new resource icons.":::

### ⏯️ Pause and resume telemetry

New buttons were added to the **Console logs**, **Structured logs**, **Traces** and **Metrics** pages to pause collecting telemetry. Click the pause button again to resume collecting telemetry.

This feature allows you to pause telemetry in the dashboard while continuing to interact with your app.

:::image type="content" source="media/dashboard-pause-telemetry.png" lightbox="media/dashboard-pause-telemetry.png" alt-text="Screenshot of the dashboard showing the pause button.":::

### ❤️‍🩹 Metrics health warning

The dashboard now warns you when a metric exceeds the configured cardinality limit. Once exceeded, the metric no longer provides accurate information.

:::image type="content" source="media/dashboard-cardinality-limit.png" lightbox="media/dashboard-cardinality-limit.png" alt-text="Screenshot of a metric with the cardinality limit warning.":::

### 🕰️ UTC Console logs option

Console logs now supports UTC timestamps. The setting is accessible via the console logs options button.

:::image type="content" source="media/dashboard-console-logs-utc.png" lightbox="media/dashboard-console-logs-utc.png" alt-text="Screenshot of console logs page showing the UTC timestamps option.":::

### 🔎 Trace details search text box

We've added a search text box to trace details. Now you can quickly filter large traces to find the exact span you need. See it in action on [BluSky](https://bsky.app/profile/james.newtonking.com/post/3llunn7fc4s2p).

### 🌐 HTTP-based resource command functionality

[Custom resource commands](../fundamentals/custom-resource-commands.md) now support HTTP-based functionality with the addition of the `WithHttpCommand` API, enabling you to define endpoints for tasks like database migrations or resets. These commands can be run directly from the .NET Aspire dashboard.

Adds WithHttpCommand(), which lets you define a resource command that sends an HTTP request to your app during development. Useful for triggering endpoints like seed or reset from the dashboard.

```csharp
if (builder.Environment.IsDevelopment())
{
    var resetDbKey = Guid.NewGuid().ToString();

    catalogDbApp.WithEnvironment("DatabaseResetKey", resetDbKey)
                .WithHttpCommand("/reset-db", "Reset Database",
                    commandOptions: new()
                    {
                        Description = "Reset the catalog database to its initial state. This will delete and recreate the database.",
                        ConfirmationMessage = "Are you sure you want to reset the catalog database?",
                        IconName = "DatabaseLightning",
                        PrepareRequest = requestContext =>
                        {
                            requestContext.Request.Headers.Add("Authorization", $"Key {resetDbKey}");
                            return Task.CompletedTask;
                        }
                    });
}
```

For more information, see [Custom HTTP commands in .NET Aspire](../fundamentals/http-commands.md).

### 🗂️ Connection string resource type

We've introduced a new `ConnectionStringResource` type that makes it easier to build dynamic connection strings without defining a separate resource type. This makes it easier to work with and build dynamic parameterized connection strings.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("apiKey");
var cs = builder.AddConnectionString("openai", 
    ReferenceExpression.Create($"Endpoint=https://api.openai.com/v1;AccessKey={apiKey};"));

var api = builder.AddProject<Projects.Api>("api")
                .WithReference(cs);
```

### 📥 Container resources can now specify an image pull policy

Container resources can now specify an `ImagePullPolicy` to control when the image is pulled. This is useful for resources that are updated frequently or that have a large image size. The following policies are supported:

- `Default`: Default behavior (which is the same as `Missing` in 9.2).
- `Always`: Always pull the image.
- `Missing`: Ensures the image is always pulled when the container starts.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithImageTag("latest")
                   .WithImagePullPolicy(ImagePullPolicy.Always)
                   .WithRedisInsight();
```

The `ImagePullPolicy` is set to `Always`, which means the image will always be pulled when the resource is created. This is useful for resources that are updated frequently.

### 📂 New container files API

In .NET Aspire 9.2, we've added a new `WithContainerFiles` API, a way to create files and folders inside a container at runtime by defining them in code. Under the hood, it uses `docker cp` / `podman cp` to copy the files in. Supports setting contents, permissions, and ownership—no bind mounts or temp files needed.

## 🤝 Integrations updates

Integrations are a key part of .NET Aspire, allowing you to easily add and configure services in your app. In .NET Aspire 9.2, we've made several updates to integrations:

### 🔐 Redis/Valkey/Garnet: Password support enabled by default

The Redis, Valkey, and Garnet containers enable password authentication by default. This is part of our goal to be secure by default—protecting development environments with sensible defaults while still making them easy to configure. Passwords can be set explicitly or generated automatically if not provided.

### 💾 Automatic database creation support

There's [plenty of feedback and confusion](https://github.com/dotnet/aspire/issues/7101) around the `AddDatabase` API. The name implies that it adds a database, but it didn't actually create the database. In .NET Aspire 9.2, the `AddDatabase` API now creates a database for the following hosting integrations:

| Hosting integration | API reference |
|--|--|
| [📦 Aspire.Hosting.SqlServer](https://www.nuget.org/packages/Aspire.Hosting.SqlServer) | <xref:Aspire.Hosting.SqlServerBuilderExtensions.AddDatabase*> |
| [📦 Aspire.Hosting.PostgreSql](https://www.nuget.org/packages/Aspire.Hosting.PostgreSql) | <xref:Aspire.Hosting.PostgresBuilderExtensions.AddDatabase*> |

The Azure SQL and Azure PostgreSQL hosting integrations also expose `AddDatabase` APIs which work with their respective `RunAsContainer` methods. For more information, see [Understand Azure integration APIs](../azure/integrations-overview.md#understand-azure-integration-apis).

By default, .NET Aspire will create an empty database if it doesn't exist. You can also optionally provide a custom script to run during creation for advanced setup or seeding.

Example using Postgres:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("pg1");

postgres.AddDatabase("todoapp")
    .WithCreationScript($$"""
        CREATE DATABASE {{databaseName}}
            ENCODING = 'UTF8';
        """);
```

For more information and examples of using the `AddDatabase` API, see:

- [Add PostgreSQL resource with database scripts](../database/postgresql-integration.md#add-postgresql-resource-with-database-scripts)
- [Add SQL Server resource with database scripts](../database/sql-server-integration.md#add-sql-server-resource-with-database-scripts)

The following hosting integrations don't currently support database creation:

- [📦 Aspire.Hosting.MongoDb](https://www.nuget.org/packages/Aspire.Hosting.MongoDb)
- [📦 Aspire.Hosting.MySql](https://www.nuget.org/packages/Aspire.Hosting.MySql)
- [📦 Aspire.Hosting.Oracle](https://www.nuget.org/packages/Aspire.Hosting.Oracle)

## ☁️ Azure integration updates

In .NET Aspire 9.2, we've made significant updates to Azure integrations, including:

### ⚙️ Configure Azure Container Apps environments

.NET Aspire 9.2 introduces `AddAzureContainerAppEnvironment`, allowing you to define an Azure Container App environment directly in your app model. This adds an `AzureContainerAppsEnvironmentResource` that lets you configure the environment and its supporting infrastructure (like container registries and volume file shares) using C# and the <xref:Azure.Provisioning> APIs—without relying on `azd` for infrastructure generation.

> [!IMPORTANT]
> This uses a different resource naming scheme than `azd`. If you're upgrading an existing deployment, this may create duplicate resources. To avoid this, you can opt into `azd`'s naming convention:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("my-env")
       .WithAzdResourceNaming();
```

For more information, see [Configure Azure Container Apps environments](../azure/configure-aca-environments.md).

### 🆕 New Client integrations: Azure PostgreSQL (Npgsql & EF Core)

.NET Aspire 9.2 adds client integrations for working with **Azure Database for PostgreSQL**, supporting both local development and secure cloud deployment.

These integrations automatically use **Managed Identity (Entra ID)** in the cloud and during local development by default. They also support username/password, if configured in your AppHost. No application code changes are required to switch between authentication models.

- [📦 Aspire.Azure.Npgsql](https://www.nuget.org/packages/Aspire.Azure.Npgsql)
- [📦 Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL)

**In AppHost:**

```csharp
var postgres = builder.AddAzurePostgresFlexibleServer("pg")
                      .AddDatabase("postgresdb");

builder.AddProject<Projects.MyService>()
       .WithReference(postgres);
```

**In MyService:**

```csharp
builder.AddAzureNpgsqlDbContext<MyDbContext>("postgresdb");
```

### 🖇️ Resource Deep Linking for Cosmos DB, Event Hubs, Service Bus, and OpenAI

CosmosDB databases and containers, EventHub hubs, ServiceBus queues/topics, and Azure OpenAI deployments now support **resource deep linking**. This allows connection information to target specific child resources—like a particular **Cosmos DB container**, **Event Hubs**, or **OpenAI deployment**—rather than just the top-level account or namespace.

Hosting integrations preserve the full resource hierarchy in connection strings, and client integrations can resolve and inject clients scoped to those specific resources.

**AppHost:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
                    .RunAsPreviewEmulator(e => e.WithDataExplorer());

var db = cosmos.AddCosmosDatabase("appdb");
db.AddContainer("todos", partitionKey: "/userId");
db.AddContainer("users", partitionKey: "/id");

builder.AddProject<Projects.TodoApi>("api")
       .WithReference(db);
```

**In the API project:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddAzureCosmosDatabase("appdb")
       .AddKeyedContainer("todos")
       .AddKeyedContainer("users");

app.MapPost("/todos", async ([FromKeyedServices("todos")] Container container, TodoItem todo) =>
{
    todo.Id = Guid.NewGuid().ToString();
    await container.CreateItemAsync(todo, new PartitionKey(todo.UserId));
    return Results.Created($"/todos/{todo.Id}", todo);
});
```

This makes it easy and convenient to use the SDKs to interact with specific resources directly—without extra wiring or manual configuration. It's especially useful in apps that deal with multiple containers or Azure services.

### 🛡️ Improved Managed Identity defaults

<span id="improved-managed-identity-defaults"></span>

Starting in **.NET Aspire 9.2**, each Azure Container App now gets its **own dedicated managed identity** by default. This is a significant change from previous versions, where all apps shared a single, highly privileged identity.

This change strengthens Aspire's *secure by default* posture:

- Each app only gets access to the Azure resources it needs.
- It enforces the principle of least privilege.
- It provides better isolation between apps in multi-service environments.

By assigning identities individually, Aspire can now scope role assignments more precisely—improving security, auditability, and alignment with Azure best practices.

This is a **behavioral breaking change** and may impact apps using:

- **Azure SQL Server** - Azure SQL only supports one Azure AD admin. With multiple identities, only the *last deployed app* will be granted admin access by default. Other apps will need explicit users and role assignments.

- **Azure PostgreSQL** - The app that creates the database becomes the owner. Other apps (like those running migrations or performing data operations) will need explicit `GRANT` permissions to access the database correctly.

See the [breaking changes](../compatibility/9.2/index.md) page for more details.

This new identity model is an important step toward more secure and maintainable applications in Aspire. While it introduces some setup considerations, especially for database integrations, it lays the groundwork for better default security across the board.

### 🔑 Least-privilege role assignment functionality

.NET Aspire now supports APIs for modeling **least-privilege role assignments** when deploying to. This enables more secure defaults by allowing you to define exactly which roles each app needs for specific Azure resources.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Persistent));

var blobs = storage.AddBlobs("blobs");

builder.AddProject<Projects.AzureContainerApps_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(blobs)
       .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor);
```

In this example, the API project is granted **Storage Blob Data Contributor** only for the referenced storage account. This avoids over-provisioning permissions and helps enforce the principle of least privilege.

Each container app automatically gets its own **managed identity**, and Aspire now generates the necessary role assignment infrastructure for both default and per-reference roles. When targeting existing Azure resources, role assignments are scoped correctly using separate Bicep resources.

### 1️⃣ First-class Azure Key Vault Secret support

Aspire now supports `IAzureKeyVaultSecretReference`, a new primitive for modeling secrets directly in the app model. This replaces `BicepSecretOutputReference` and avoids creating a separate Key Vault per resource.

You can now:

- Add a shared Key Vault in C#
- Configure services that support keys (e.g., Redis, Cosmos DB) to store their secrets there
- Reference those secrets in your app as environment variables or via the Key Vault config provider

Use KeyVault directly in your api:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var vault = builder.AddAzureKeyVault("kv");

var redis = builder.AddAzureRedis("redis")
                   .WithAccessKeyAuthentication(vault);

builder.AddProject<Projects.Api>("api")
       .WithReference(redis);
```

Let the compute environment handle the secret management for you:

```csharp
var redis = builder.AddAzureRedis("redis")
                   .WithAccessKeyAuthentication();

builder.AddProject<Projects.Api>("api")
       .WithReference(redis);
```

**Previous behavior:**

  `azd` created and managed secrets using a key vault per resource, with no visibility in the app model. Secrets were handled implicitly and couldn't be customized in C#.

**New behavior in 9.2:**

  Calling `WithKeyAccessAuthentication` or `WithPasswordAuthentication` now creates an actual `AzureKeyVaultResource` (or accepts a reference to one), and stores connection strings there. Secret names follow the pattern `connectionstrings--{resourcename}` to prevent naming conflicts with other vault entries.

### 🔒 Improved default permissions for Azure Key Vault references

When referencing a Key Vault, Aspire previously granted the broad **Key Vault Administrator** role by default. In 9.2, this has been changed to **Key Vault Secrets User**, which provides read-only access to secrets—suitable for most application scenarios.

This update continues the security-focused improvements in this release.

## 🚀 Deployment improvements

We're excited to announce several new deployment features in .NET Aspire 9.2, including:

### 📦 Publishers (Preview)

Publishers are a new extensibility point in .NET Aspire that allow you to define how your distributed application gets transformed into deployable assets. Rather than relying on an [intermediate manifest format](../deployment/manifest-format.md), publishers can now plug directly into the application model to generate Docker Compose files, Kubernetes manifests, Azure resources, or whatever else your environment needs.

When .NET Aspire launched, it introduced a deployment manifest format—a serialized snapshot of the application model. While useful it burdened deployment tools with interpreting the manifest and resource authors with ensuring accurate serialization. This approach also complicated schema evolution and target-specific behaviors.

Publishers simplify this process by working directly with the full application model in-process, enabling richer, more flexible, and maintainable publishing experiences.

The following NuGet packages expose preview publishers:

- [📦 Aspire.Hosting.Azure](https://www.nuget.org/packages/Aspire.Hosting.Azure)
- [📦 Aspire.Hosting.Docker (Preview)](https://www.nuget.org/packages/Aspire.Hosting.Docker)
- [📦 Aspire.Hosting.Kubernetes (Preview)](https://www.nuget.org/packages/Aspire.Hosting.Kubernetes)

> [!IMPORTANT]
> The Docker and Kubernetes publishers were contributed by community contributor, [Dave Sekula](https://github.com/Prom3theu5)—a great example of the community stepping up to extend the model. 💜 Thank you, Dave!

To use a publisher, add the corresponding NuGet package to your app host project file and then call the `Add[Name]Publisher()` method in your app host builder.

```csharp
builder.AddDockerComposePublisher();
```

> [!TIP]
> Publisher registration methods follow the `Add[Name]Publisher()` convention.

You can also build your own publisher by implementing the publishing APIs and calling your custom registration method. Some publishers are still in preview, and the APIs are subject to change. The goal is to provide a more flexible and extensible way to publish distributed applications, making it easier to adapt to different deployment environments and scenarios.

### 🆕 Aspire CLI (Preview)

.NET Aspire 9.2 introduces the new **`aspire` CLI**, a tool for creating, running, and publishing Aspire applications from the command line. It provides a rich, interactive experience tailored for Aspire users.

The CLI is available as a .NET tool and can be installed with:

```bash
dotnet tool install --global aspire.cli --prerelease
```

#### Example usage:

```bash
aspire new
aspire run
aspire add redis
aspire publish --publisher docker-compose
```

#### Available commands:

- `new <template>` – Create a new Aspire sample project  
- `run` – Run an Aspire app host in development mode  
- `add <integration>` – Add an integration to your project  
- `publish` – Generate deployment artifacts from your app host

🧪 The CLI is **preview**. We're exploring how to make it a first-class experience for .NET Aspire users—your feedback is welcome!

## 🧪 Testing template updates

The xUnit testing project template now supports a version selector, allowing the user to select either:

- `v2`: The previous xUnit testing experience.
- `v3`: The new xUnit testing experience and template.
- `v3 with Microsoft Test Platform`: The next xUnit testing experience, template and uses the [Microsoft Testing Platform](/dotnet/core/testing/microsoft-testing-platform-intro).

By default, to the `v3` experience. For more information, see:

- [What's new in xUnit v.3](https://xunit.net/docs/getting-started/v3/whats-new)
- [Microsoft Testing Platform support in xUnit.net v3](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)

> [!NOTE]
> Both `v3` versions are only supported with .NET Aspire 9.2 or later.

## 💔 Breaking changes

With every release, we strive to make .NET Aspire better. However, some changes may break existing functionality. The following breaking changes are introduced in .NET Aspire 9.2:

- [Breaking changes in .NET Aspire 9.2](../compatibility/9.2/index.md)

---
title: .NET Aspire overview
description: Learn about .NET Aspire, an application stack designed to improve the experience of building distributed applications.
ms.date: 11/12/2024
---

# .NET Aspire overview

:::row:::
:::column:::

:::image type="icon" border="false" source="../../assets/dotnet-aspire-logo-128.svg":::

:::column-end:::
:::column span="3":::

.NET Aspire is a set of tools, templates, and packages for building observable, production ready apps.​​ .NET Aspire is delivered through a collection of NuGet packages that bootstrap or improve specific challenges with modern app development. Today's apps generally consume a large number of services, such as databases, messaging, and caching, many of which are supported via [.NET Aspire Integrations](../fundamentals/integrations-overview.md). For information on support, see the [.NET Aspire Support Policy](https://dotnet.microsoft.com/platform/support/policy/aspire).

:::column-end:::
:::row-end:::

## Why .NET Aspire?

.NET Aspire improves the experience of building apps that have a variety of projects and resources. With dev-time productivity enhancements that emulate deployed scenarios, you can quickly develop interconnected apps. Designed for flexibility, .NET Aspire allows you to replace or extend parts with your preferred tools and workflows. Key features include:

- [**Dev-Time Orchestration**](#dev-time-orchestration): .NET Aspire provides features for running and connecting multi-project applications, container resources, and other dependencies for [local development environments](../fundamentals/networking-overview.md).
- [**Integrations**](#net-aspire-integrations): .NET Aspire integrations are NuGet packages for commonly used services, such as Redis or Postgres, with standardized interfaces ensuring they connect consistently and seamlessly with your app.
- [**Tooling**](#project-templates-and-tooling): .NET Aspire comes with project templates and tooling experiences for Visual Studio, Visual Studio Code, and the [.NET CLI](/dotnet/core/tools/) to help you create and interact with .NET Aspire projects.

## Dev-time orchestration

In .NET Aspire, "orchestration" primarily focuses on enhancing the _local development_ experience by simplifying the management of your app's configuration and interconnections. It's important to note that .NET Aspire's orchestration isn't intended to replace the robust systems used in production environments, such as [Kubernetes](../deployment/overview.md#deploy-to-kubernetes). Instead, it's a set of abstractions that streamline the setup of service discovery, environment variables, and container configurations, eliminating the need to deal with low-level implementation details. With .NET Aspire, your code has a consistent bootstrapping experience on any dev machine without the need for complex manual steps, making it easier to manage during the development phase.

.NET Aspire orchestration assists with the following concerns:

- **App composition**: Specify the .NET projects, containers, executables, and cloud resources that make up the application.
- **Service discovery and connection string management**: The app host injects the right connection strings, network configurations, and service discovery information to simplify the developer experience.

For example, using .NET Aspire, the following code creates a local Redis container resource, waits for it to become available, and then configures the appropriate connection string in the `"frontend"` project with a few helper method calls:

```csharp
// Create a distributed application builder given the command line arguments.
var builder = DistributedApplication.CreateBuilder(args);

// Add a Redis server to the application.
var cache = builder.AddRedis("cache");

// Add the frontend project to the application and configure it to use the 
// Redis server, defined as a referenced dependency.
builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(cache)
       .WaitFor(cache);
```

For more information, see [.NET Aspire orchestration overview](../fundamentals/app-host-overview.md).

> [!IMPORTANT]
> The call to <xref:Aspire.Hosting.RedisBuilderExtensions.AddRedis*> creates a new Redis container in your local dev environment. If you'd rather use an existing Redis instance, you can use the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString*> method to reference an existing connection string. For more information, see [Reference existing resources](../fundamentals/app-host-overview.md#reference-existing-resources).

## .NET Aspire integrations

[.NET Aspire integrations](../fundamentals/integrations-overview.md) are NuGet packages designed to simplify connections to popular services and platforms, such as Redis or PostgreSQL. .NET Aspire integrations handle cloud resource setup and interaction for you through standardized patterns, such as adding health checks and telemetry. Integrations are two-fold - ["hosting" integrations](../fundamentals/integrations-overview.md#hosting-integrations) represents the service you're connecting to, and ["client" integrations](../fundamentals/integrations-overview.md#client-integrations) represents the client or consumer of that service. In other words, for many hosting packages there's a corresponding client package that handles the service connection within your code.

Each integration is designed to work with the .NET Aspire app host, and their configurations are injected automatically by [referencing named resources](../fundamentals/app-host-overview.md#reference-resources). In other words, if _Example.ServiceFoo_ references _Example.ServiceBar_, _Example.ServiceFoo_ inherits the integration's required configurations to allow them to communicate with each other automatically.

For example, consider the following code using the .NET Aspire Service Bus integration:

```csharp
builder.AddAzureServiceBusClient("servicebus");
```

The <xref:Microsoft.Extensions.Hosting.AspireServiceBusExtensions.AddAzureServiceBusClient%2A> method handles the following concerns:

- Registers a <xref:Azure.Messaging.ServiceBus.ServiceBusClient> as a singleton in the DI container for connecting to Azure Service Bus.
- Applies <xref:Azure.Messaging.ServiceBus.ServiceBusClient> configurations either inline through code or through configuration.
- Enables corresponding health checks, logging, and telemetry specific to the Azure Service Bus usage.

A full list of available integrations is detailed on the [.NET Aspire integrations](../fundamentals/integrations-overview.md) overview page.

## Project templates and tooling

.NET Aspire provides a set of project templates and tooling experiences for Visual Studio, Visual Studio Code, and the [.NET CLI](/dotnet/core/tools/). These templates are designed to help you create and interact with .NET Aspire projects, or add .NET Aspire into your existing codebase. The templates include a set of opinionated defaults to help you get started quickly - for example, it has boilerplate code for turning on health checks and logging in .NET apps. These defaults are fully customizable, so you can edit and adapt them to suit your needs.

.NET Aspire templates also include boilerplate extension methods that handle common service configurations for you:

```csharp
builder.AddServiceDefaults();
```

For more information on what `AddServiceDefaults` does, see [.NET Aspire service defaults](../fundamentals/service-defaults.md).

When added to your _:::no-loc text="Program.cs":::_ file, the preceding code handles the following concerns:

- **OpenTelemetry**: Sets up formatted logging, runtime metrics, built-in meters, and tracing for ASP.NET Core, gRPC, and HTTP. For more information, see [.NET Aspire telemetry](../fundamentals/telemetry.md).
- **Default health checks**: Adds default health check endpoints that tools can query to monitor your app. For more information, see [.NET app health checks in C#](/dotnet/core/diagnostics/diagnostic-health-checks).
- **Service discovery**: Enables [service discovery](../service-discovery/overview.md) for the app and configures <xref:System.Net.Http.HttpClient> accordingly.

## Next steps

> [!div class="nextstepaction"]
> [Quickstart: Build your first .NET Aspire project](build-your-first-aspire-app.md)

----------
----------
----------
----------

---
title: What's new in .NET Aspire 9.1
description: Learn what's new in the official general availability release of .NET Aspire 9.1.
ms.date: 02/25/2025
---

# What's new in .NET Aspire 9.1

📢 .NET Aspire 9.1 is the next minor version release of .NET Aspire; it supports _both_:

- .NET 8.0 Long Term Support (LTS) _or_
- .NET 9.0 Standard Term Support (STS).

> [!NOTE]
> You're able to use .NET Aspire 9.1 with either .NET 8 or .NET 9!

As always, we focused on highly requested features and pain points from the community. Our theme for 9.1 was "polish, polish, polish"—so you see quality of life fixes throughout the whole platform. Some highlights from this release are resource relationships in the dashboard, support for working in GitHub Codespaces, and publishing resources as a Dockerfile.

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://discord.com/invite/h87kDAHQgJ) to chat with team members.

Whether you're new to .NET Aspire or have been with us since the preview, it's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of .NET Aspire align with .NET major versions, minor versions are released more frequently. For more details on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product life cycle details.

## ⬆️ Upgrade to .NET Aspire 9.1

Moving between minor releases of .NET Aspire is simple:

1. In your app host project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.1.0`:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">

        <Sdk Name="Aspire.AppHost.Sdk" Version="9.1.0" />
        
        <!-- Omitted for brevity -->
    
    </Project>
    ```

    For more information, see [.NET Aspire SDK](xref:dotnet/aspire/sdk).

1. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command in VS Code.
1. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new update
    ```

    > [!NOTE]
    > The `dotnet new update` command updates all of your templates to the latest version.

If your app host project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9.0, you can follow [the documentation from last release](../get-started/upgrade-to-aspire-9.md).

## 🌱 Improved onboarding experience

The onboarding experience for .NET Aspire is improved with 9.1. The team worked on creating a GitHub Codespaces template that installs all the necessary dependencies for .NET Aspire, making it easier to get started, including the templates and the ASP.NET Core developer certificate. Additionally, there's support for Dev Containers. For more information, see:

- [.NET Aspire and GitHub Codespaces](../get-started/github-codespaces.md)
- [.NET Aspire and Visual Studio Code Dev Containers](../get-started/dev-containers.md)

## 🔧 Dashboard UX and customization

With every release of .NET Aspire, the [dashboard](../fundamentals/dashboard/overview.md) gets more powerful and customizable, this release is no exception. The following features were added to the dashboard in .NET Aspire 9.1:

### 🧩 Resource relationships

The dashboard now supports "parent" and "child" resource relationships. For instance, when you create a Postgres instance with multiple databases, these databases are nested under the same instance on the **Resource** page.

:::image type="content" source="media/dashboard-parentchild.png" lightbox="media/dashboard-parentchild.png" alt-text="A screenshot of the .NET Aspire dashboard showing the Postgres resource with a database nested underneath it.":::

For more information, see [Explore the .NET Aspire dashboard](../fundamentals/dashboard/explore.md).

### 🔤 Localization overrides

The dashboard defaults to the language set in your browser. This release introduces the ability to override this setting and change the dashboard language independently from the browser language. Consider the following screen capture that demonstrates the addition of the language dropdown in the dashboard:

:::image type="content" source="media/dashboard-language.png" lightbox="media/dashboard-language.png" alt-text="A screenshot of the .NET Aspire dashboard showing the new flyout menu to change language.":::

### 🗑️ Clear logs and telemetry from the dashboard

New buttons were added to the **Console logs**, **Structured logs**, **Traces** and **Metrics** pages to clear data. There's also a "Remove all" button in the settings popup to remove everything with one action.

Now you use this feature to reset the dashboard to a blank slate, test your app, view only the relevant logs and telemetry, and repeat.

:::image type="content" source="media/dashboard-remove-telemetry.png" lightbox="media/dashboard-remove-telemetry.png" alt-text="A screenshot of the .NET Aspire dashboard showing the remove button on the structured logs page.":::

We 💜 love the developer community and thrive on its feedback, collaboration, and contributions. This feature is a community contribution from [@Daluur](https://github.com/Daluur). Join us in celebrating their contribution by using the feature!

> [!TIP]
> If you're interested in contributing to .NET Aspire, look for issues labeled with [good first issue](https://github.com/dotnet/aspire/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22) and follow the [contributor guide](https://github.com/dotnet/aspire/blob/main/docs/contributing.md).

### 🔢 New filtering

You can now filter what you see in the **Resource** page by **Resource type**, **State**, and **Health state**. Consider the following screen capture, which demonstrates the addition of the filter options in the dashboard:

:::image type="content" source="media/dashboard-filter.png" lightbox="media/dashboard-filter.png" alt-text="A screenshot of the .NET Aspire dashboard showing the new filter options.":::

### 📝 More resource details

When you select a resource in the dashboard, the details pane now displays new data points, including **References**, **Back references**, and **Volumes** with their mount types. This enhancement provides a clearer and more comprehensive view of your resources, improving the overall user experience by making relevant details more accessible.

:::image type="content" source="media/dashboard-resourcedetails.png" lightbox="media/dashboard-resourcedetails.png" alt-text="A screenshot of the .NET Aspire dashboard with references and back references showing.":::

For more information, see [.NET Aspire dashboard: Resources page](../fundamentals/dashboard/explore.md#resources-page).

### 🛡️ CORS support for custom local domains

You can now set the `DOTNET_DASHBOARD_CORS_ALLOWED_ORIGINS` environment variable to allow the dashboard to receive telemetry from other browser apps, such as if you have resources running on custom localhost domains.

For more information, see [.NET Aspire app host: Dashboard configuration](../app-host/configuration.md#dashboard).

### 🪵 Flexibility with console logs

The console log page has two new options. You're now able to download your logs so you can view them in your own diagnostics tools. Plus, you can turn timestamps on or off to reduce visual clutter when needed.

:::image type="content" source="media/consolelogs-download.png" lightbox="media/consolelogs-download.png" alt-text="A screenshot of the console logs page with the download button, turn off timestamps button, and logs that don't show timestamps.":::

For more information, see [.NET Aspire dashboard: Console logs page](../fundamentals/dashboard/explore.md#console-logs-page).

### 🎨 Various UX improvements

Several new features in .NET Aspire 9.1 enhance and streamline the following popular tasks:

- ▶️ Resource commands, such as **Start** and **Stop** buttons, are now available on the **Console logs** page.
- 🔍 Single selection to open in the _text visualizer_.
- 🔗 URLs within logs are now automatically clickable, with commas removed from endpoints.

Additionally, the 🖱️ scroll position resets when switching between different resources—this helps to visually reset the current resource view.  

For more details on the latest dashboard enhancements, check out [James Newton-King on :::image type="icon" source="../media/bluesky-icon.svg" border="false"::: Bluesky](https://bsky.app/profile/james.newtonking.com), where he's been sharing new features daily.

## ⚙️ Local development enhancements

In .NET Aspire 9.1, several improvements to streamline your local development experience were an emphasis. These enhancements are designed to provide greater flexibility, better integration with Docker, and more efficient resource management. Here are some of the key updates:

### ▶️ Start resources on demand

You can now tell resources not to start with the rest of your app by using <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExplicitStart*> on the resource in your app host. Then, you can start it whenever you're ready from inside the dashboard.

For more information, see [Configure explicit resource start](../fundamentals/app-host-overview.md#configure-explicit-resource-start).

### 🐳 Better Docker integration

The `PublishAsDockerfile()` feature was introduced for all projects and executable resources. This enhancement allows for complete customization of the Docker container and Dockerfile used during the publish process.

While this API was available in previous versions, it couldn't be used with <xref:Aspire.Hosting.ApplicationModel.ProjectResource> or <xref:Aspire.Hosting.ApplicationModel.ExecutableResource> types.

### 🧹 Cleaning up Docker networks

In 9.1, we addressed a persistent issue where Docker networks created by .NET Aspire would remain active even after the application was stopped. This bug, tracked in [.NET Aspire GitHub issue #6504](https://github.com/dotnet/aspire/issues/6504), is resolved. Now, Docker networks are properly cleaned up, ensuring a more efficient and tidy development environment.

### ✅ Socket address issues fixed

Several users reported issues ([#6693](https://github.com/dotnet/aspire/issues/6693), [#6704](https://github.com/dotnet/aspire/issues/6704), [#7095](https://github.com/dotnet/aspire/issues/7095)) with restarting the .NET Aspire app host, including reconciliation errors and "address already in use" messages.

This release introduces a more robust approach to managing socket addresses, ensuring only one instance of each address is used at a time. Additionally, improvements were made to ensure proper project restarts and resource releases, preventing hanging issues. These changes enhance the stability and reliability of the app host, especially during development and testing.

## 🔌 Integration updates

.NET Aspire continues to excel through its [integrations](../fundamentals/integrations-overview.md) with various platforms. This release includes numerous updates to existing integrations and details about ownership migrations, enhancing the overall functionality and user experience.

### ☁️ Azure updates

This release also focused on improving various [Azure integrations](../azure/integrations-overview.md):

#### 🆕 New emulators

We're excited to bring new emulators for making local development easier. The following integrations got new emulators in this release:

- [Azure Service Bus](../messaging/azure-service-bus-integration.md#add-azure-service-bus-emulator-resource)
- [Azure Cosmos DB Linux-based (preview)](../database/azure-cosmos-db-integration.md#use-linux-based-emulator-preview)
- [Azure SignalR](/azure/azure-signalr/signalr-howto-emulator)

```csharp
var serviceBus = builder.AddAzureServiceBus("servicebus")
                        .RunAsEmulator();

#pragma warning disable ASPIRECOSMOSDB001
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
                      .RunAsPreviewEmulator();

var signalr = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless)
                     .RunAsEmulator();
```

These new emulators work side-by-side with the existing emulators for:

- [Azure Storage](../storage/azure-storage-integrations.md)
- [Azure Event Hubs](../messaging/azure-event-hubs-integration.md#add-azure-event-hubs-emulator-resource)
- [Azure Cosmos DB](../database/azure-cosmos-db-integration.md#add-azure-cosmos-db-emulator-resource)

#### 🌌 Cosmos DB

Along with support for the new emulator, Cosmos DB added the following features.

##### 🔒 Support for Entra ID authentication by default

Previously, the Cosmos DB integration used access keys and a Key Vault secret to connect to the service. .NET Aspire 9.1 added support for using more secure authentication using managed identities by default. If you need to keep using access key authentication, you can get back to the previous behavior by calling <xref:Aspire.Hosting.AzureCosmosExtensions.WithAccessKeyAuthentication*>.

##### 💽 Support for modeling Database and Containers in the app host

You can define a Cosmos DB database and containers in the app host and these resources are available when you run the application in both the emulator and in Azure. This allows you to define these resources up front and no longer need to create them from the application, which might not have permission to create them.

For example API usage to add database and containers, see the following related articles:

- [.NET Aspire Azure Cosmos DB integration](../database/azure-cosmos-db-integration.md#add-azure-cosmos-db-database-and-container-resources)
- [.NET Aspire Cosmos DB Entity Framework Core integration](../database/azure-cosmos-db-entity-framework-integration.md#add-azure-cosmos-db-database-and-container-resources)

##### ⚡ Support for Cosmos DB-based triggers in Azure Functions

The <xref:Aspire.Hosting.AzureCosmosDBResource> was modified to support consumption in Azure Functions applications that uses the Cosmos DB trigger. A Cosmos DB resource can be initialized and added as a reference to an Azure Functions resource with the following code:

```csharp
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
                      .RunAsEmulator();
var database = cosmosDb.AddCosmosDatabase("mydatabase");
database.AddContainer("mycontainer", "/id");

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithReference(cosmosDb)
    .WaitFor(cosmosDb);
```

The resource can be used in the Azure Functions trigger as follows:

```csharp
public class MyCosmosDbTrigger(ILogger<MyCosmosDbTrigger> logger)
{
    [Function(nameof(MyCosmosDbTrigger))]
    public void Run([CosmosDBTrigger(
        databaseName: "mydatabase",
        containerName: "mycontainer",
        CreateLeaseContainerIfNotExists = true,
        Connection = "cosmosdb")] IReadOnlyList<Document> input)
    {
        logger.LogInformation(
            "C# cosmosdb trigger function processed: {Count} messages",
            input.Count);
    }
}
```

For more information using Azure Functions with .NET Aspire, see [.NET Aspire Azure Functions integration (Preview)](../serverless/functions.md).

#### 🚚 Service Bus and Event Hubs

Similar to Cosmos DB, the Service Bus and Event Hubs integrations now allow you to define Azure Service Bus queues, topics, subscriptions, and Azure Event Hubs instances and consumer groups directly in your app host code. This enhancement simplifies your application logic by enabling the creation and management of these resources outside the application itself.

For more information, see the following updated articles:

- [.NET Aspire Azure Service Bus integration](../messaging/azure-service-bus-integration.md)
- [.NET Aspire Azure Event Hubs integration](../messaging/azure-event-hubs-integration.md)

#### ♻️ Working with existing resources

There's consistent feedback about making it easier to connect to existing Azure resources in .NET Aspire. With 9.1, you can now easily connect to an existing Azure resource either directly by `string` name, or with [app model parameters](../fundamentals/external-parameters.md) which can be changed at deployment time. For example to connect to an Azure Service Bus account, we can use the following code:

```csharp
var existingServiceBusName = builder.AddParameter("serviceBusName");
var existingServiceBusResourceGroup = builder.AddParameter("serviceBusResourceGroup");

var serviceBus = builder.AddAzureServiceBus("messaging")
                        .AsExisting(existingServiceBusName, existingServiceBusResourceGroup);
```

The preceding code reads the name and resource group from the parameters, and connects to the existing resource when the application is run or deployed. For more information, see [use existing Azure resources](../azure/integrations-overview.md#use-existing-azure-resources).

#### 🌍 Azure Container Apps

Experimental support for configuring custom domains in Azure Container Apps (ACA) was added. For example:

```csharp
#pragma warning disable ASPIREACADOMAINS001

var customDomain = builder.AddParameter("customDomain");
var certificateName = builder.AddParameter("certificateName");

builder.AddProject<Projects.AzureContainerApps_ApiService>("api")
       .WithExternalHttpEndpoints()
       .PublishAsAzureContainerApp((infra, app) =>
       {
           app.ConfigureCustomDomain(customDomain, certificateName);
       });
```

For more information, see [.NET Aspire diagnostics overview](../diagnostics/overview.md).

### ➕ Even more integration updates

- OpenAI now supports the [📦 Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI) NuGet package.
- RabbitMQ updated to version 7, and MongoDB to version 3. These updates introduced breaking changes, leading to the release of new packages with version-specific suffixes. The original packages continue to use the previous versions, while the new packages are as follows:
  - [📦 Aspire.RabbitMQ.Client.v7](https://www.nuget.org/packages/Aspire.RabbitMQ.Client.v7) NuGet package. For more information, see the [.NET Aspire RabbitMQ client integration](../messaging/rabbitmq-integration.md#client-integration) documentation.
  - [📦 Aspire.MongoDB.Driver.v3](https://www.nuget.org/packages/Aspire.MongoDB.Driver.v3) NuGet package. For more information, see the [.NET Aspire MongoDB client integration](../database/mongodb-integration.md#client-integration) documentation.
- Dapr migrated to the [CommunityToolkit](https://github.com/CommunityToolkit/Aspire/tree/main/src/CommunityToolkit.Aspire.Hosting.Dapr) to facilitate faster innovation.
- Numerous other integrations received updates, fixes, and new features. For detailed information, refer to our [GitHub release notes](https://github.com/dotnet/aspire/releases).

The [📦 Aspire.Hosting.AWS](https://www.nuget.org/packages/Aspire.Hosting.AWS) NuGet package and source code migrated under [Amazon Web Services (AWS)) ownership](https://github.com/aws/integrations-on-dotnet-aspire-for-aws). This migration happened as part of .NET Aspire 9.0, we're just restating that change here.

## 🧪 Testing in .NET Aspire

.NET Aspire 9.1 simplifies writing cross-functional integration tests with a robust approach. The app host allows you to create, evaluate, and manage containerized environments seamlessly within a test run. This functionality supports popular testing frameworks like xUnit, NUnit, and MSTest, enhancing your testing capabilities and efficiency.

Now, you're able to disable port randomization or enable the [dashboard](../fundamentals/dashboard/overview.md). For more information, see [.NET Aspire testing overview](../testing/overview.md). Additionally, you can now [Pass arguments to your app host](../testing/manage-app-host.md#pass-arguments-to-your-app-host).

Some of these enhancements were introduced as a result of stability issues that were reported, such as [.NET Aspire GitHub issue #6678](https://github.com/dotnet/aspire/issues/6678)—where some resources failed to start do to "address in use" errors.

## 🚀 Deployment

Significant improvements to the Azure Container Apps (ACA) deployment process are included in .NET Aspire 9.1, enhancing both the `azd` CLI and app host options. One of the most requested features—support for deploying `npm` applications to ACA—is now implemented. This new capability allows `npm` apps to be deployed to ACA just like other resources, streamlining the deployment process and providing greater flexibility for developers.

We recognize there's more work to be done in the area of deployment. Future releases will continue to address these opportunities for improvement. For more information on deploying .NET Aspire to ACA, see [Deploy a .NET Aspire project to Azure Container Apps](../deployment/azure/aca-deployment.md).

## ⚠️ Breaking changes

.NET Aspire is moving quickly, and with that comes breaking changes. Breaking are categorized as either:

- **Binary incompatible**: The assembly version has changed, and you need to recompile your code.
- **Source incompatible**: The source code has changed, and you need to change your code.
- **Behavioral change**: The code behaves differently, and you need to change your code.

Typically APIs are decorated with the <xref:System.ObsoleteAttribute> giving you a warning when you compile, and an opportunity to adjust your code. For an overview of breaking changes in .NET Aspire 9.1, see [Breaking changes in .NET Aspire 9.1](../compatibility/9.1/index.md).

## 🎯 Upgrade today

Follow the directions outlined in the [Upgrade to .NET Aspire 9.1](#-upgrade-to-net-aspire-91) section to make the switch to 9.1 and take advantage of all these new features today! As always, we're listening for your feedback on [GitHub](https://github.com/dotnet/aspire/issues)-and looking out for what you want to see in 9.2 ☺️.

For a complete list of issues addressed in this release, see [.NET Aspire GitHub repository—9.1 milestone](https://github.com/dotnet/aspire/issues?q=is%3Aissue%20state%3Aclosed%20milestone%3A9.1%20).

--------------
--------------
--------------
--------------

---
title: .NET Aspire tooling
description: Learn about essential tooling concepts for .NET Aspire.
ms.date: 03/11/2025
zone_pivot_groups: dev-environment
uid: dotnet/aspire/setup-tooling
---

# .NET Aspire setup and tooling

.NET Aspire includes tooling to help you create and configure cloud-native apps. The tooling includes useful starter project templates and other features to streamline getting started with .NET Aspire for Visual Studio, Visual Studio Code, and CLI workflows. In the sections ahead, you learn how to work with .NET Aspire tooling and explore the following tasks:

> [!div class="checklist"]
>
> - Install .NET Aspire and its dependencies
> - Create starter project templates using Visual Studio, Visual Studio Code, or the .NET CLI
> - Install .NET Aspire integrations
> - Work with the .NET Aspire dashboard

## Install .NET Aspire prerequisites

To work with .NET Aspire, you need the following installed locally:

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0).
- An OCI compliant container runtime, such as:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman](https://podman.io/). For more information, see [Container runtime](#container-runtime).
- An Integrated Developer Environment (IDE) or code editor, such as:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) version 17.9 or higher (Optional)
  - [Visual Studio Code](https://code.visualstudio.com/) (Optional)
    - [C# Dev Kit: Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (Optional)
  - [JetBrains Rider with .NET Aspire plugin](https://blog.jetbrains.com/dotnet/2024/02/19/jetbrains-rider-and-the-net-aspire-plugin/) (Optional)

> [!TIP]
> Alternatively, you can develop .NET Aspire solutions using [GitHub Codespaces](../get-started/github-codespaces.md) or [Dev Containers](../get-started/dev-containers.md).

:::zone pivot="visual-studio"

Visual Studio 2022 17.9 or higher includes the latest [.NET Aspire SDK](dotnet-aspire-sdk.md) by default when you install the Web & Cloud workload. If you have an earlier version of Visual Studio 2022, you can either upgrade to Visual Studio 2022 17.9 or you can install the .NET Aspire SDK using the following steps:

To install the .NET Aspire workload in Visual Studio 2022, use the Visual Studio installer.

1. Open the Visual Studio Installer.
1. Select **Modify** next to Visual Studio 2022.
1. Select the **ASP.NET and web development** workload.
1. On the **Installation details** panel, select **.NET Aspire SDK**.
1. Select **Modify** to install the .NET Aspire integration.

   :::image type="content" loc-scope="visual-studio" source="media/setup-tooling/web-workload-with-aspire.png" lightbox="media/setup-tooling/web-workload-with-aspire.png" alt-text="A screenshot showing how to install the .NET Aspire workload with the Visual Studio installer.":::

:::zone-end
:::zone pivot="vscode,dotnet-cli"

<!-- Visual Studio Code and .NET CLI instructions

  Intentionally left blank, as you don't need to do anything extra.

-->

:::zone-end

## .NET Aspire templates

.NET Aspire provides a set of solution and project templates. These templates are available in your favorite .NET developer integrated environment. You can use these templates to create full .NET Aspire solutions, or add individual projects to existing .NET Aspire solutions.

### Install the .NET Aspire templates

:::zone pivot="visual-studio"

To install the .NET Aspire templates in Visual Studio, you need to manually install them unless you're using Visual Studio 17.12 or later. For Visual Studio 17.9 to 17.11, follow these steps:

1. Open Visual Studio.
1. Go to **Tools** > **NuGet Package Manager** > **Package Manager Console**.
1. Run the following command to install the templates:

  ```dotnetcli
  dotnet new install Aspire.ProjectTemplates
  ```

For Visual Studio 17.12 or later, the .NET Aspire templates are installed automatically.

:::zone-end
:::zone pivot="vscode,dotnet-cli"

To install these templates, use the [dotnet new install](/dotnet/core/tools/dotnet-new-install) command, passing in the `Aspire.ProjectTemplates` NuGet identifier.

```dotnetcli
dotnet new install Aspire.ProjectTemplates
```

To install a specific version, append the version number to the package name:

```dotnetcli
dotnet new install Aspire.ProjectTemplates::9.0.0
```

> [!TIP]
> If you already have the .NET Aspire workload installed, you need to pass the `--force` flag to overwrite the existing templates. Feel free to uninstall the .NET Aspire workload.

:::zone-end

### List the .NET Aspire templates

:::zone pivot="visual-studio"

The .NET Aspire templates are installed automatically when you install Visual Studio 17.9 or later. To see what .NET Aspire templates are available, select **File** > **New** > **Project** in Visual Studio, and search for "Aspire" in the search bar (<kbd>Alt</kbd>+<kbd>S</kbd>). You'll see a list of available .NET Aspire project templates:

:::image type="content" source="media/vs-create-dotnet-aspire-proj.png" alt-text="Visual Studio: Create new project and search for 'Aspire'." lightbox="media/vs-create-dotnet-aspire-proj.png":::

:::zone-end
:::zone pivot="vscode"

To view the available templates in Visual Studio Code with the C# DevKit installed, select the **Create .NET Project** button when no folder is opened in the **Explorer** view:

:::image type="content" source="media/vscode-create-dotnet-proj.png" alt-text="Visual Studio Code: Create .NET Project button." lightbox="media/vscode-create-dotnet-proj.png":::

Then, search for "Aspire" in the search bar to see the available .NET Aspire project templates:

:::image type="content" source="media/vscode-create-dotnet-aspire-proj.png" alt-text="Visual Studio Code: Create new project and search for 'Aspire'." lightbox="media/vscode-create-dotnet-aspire-proj.png":::

:::zone-end
:::zone pivot="dotnet-cli"

To verify that the .NET Aspire templates are installed, use the [dotnet new list](/dotnet/core/tools/dotnet-new-list) command, passing in the `aspire` template name:

```dotnetcli
dotnet new list aspire
```

Your console output should look like the following:

[!INCLUDE [dotnet-new-list-aspire-output](includes/dotnet-new-list-aspire-output.md)]

:::zone-end

For more information, see [.NET Aspire templates](aspire-sdk-templates.md).

## Container runtime

.NET Aspire projects are designed to run in containers. You can use either Docker Desktop or Podman as your container runtime. [Docker Desktop](https://www.docker.com/products/docker-desktop/) is the most common container runtime. [Podman](https://podman.io/docs/installation) is an open-source daemonless alternative to Docker, that can build and run Open Container Initiative (OCI) containers. If your host environment has both Docker and Podman installed, .NET Aspire defaults to using Docker. You can instruct .NET Aspire to use Podman instead, by setting the `DOTNET_ASPIRE_CONTAINER_RUNTIME` environment variable to `podman`:

## [Linux](#tab/linux)

```bash
export DOTNET_ASPIRE_CONTAINER_RUNTIME=podman
```

For more information, see [Install Podman on Linux](https://podman.io/docs/installation#installing-on-linux).

## [Windows](#tab/windows)

```powershell
[System.Environment]::SetEnvironmentVariable("DOTNET_ASPIRE_CONTAINER_RUNTIME", "podman", "User")
```

For more information, see [Install Podman on Windows](https://podman.io/docs/installation#installing-on-mac--windows).

---

## .NET Aspire dashboard

.NET Aspire templates that expose the [app host](app-host-overview.md) project also include a useful developer [dashboard](dashboard/overview.md) that's used to monitor and inspect various aspects of your app, such as logs, traces, and environment configurations. This dashboard is designed to improve the local development experience and provides an overview of the overall state and structure of your app.

The .NET Aspire dashboard is only visible while the app is running and starts automatically when you start the _*.AppHost_ project. Visual Studio and Visual Studio Code launch both your app and the .NET Aspire dashboard for you automatically in your browser. If you start the app using the .NET CLI, copy and paste the dashboard URL from the output into your browser, or hold <kbd>Ctrl</kbd> and select the link (if your terminal supports hyperlinks).

:::image type="content" source="dashboard/media/explore/dotnet-run-login-url.png" lightbox="dashboard/media/explore/dotnet-run-login-url.png" alt-text="A screenshot showing how to launch the dashboard using the CLI.":::

The left navigation provides links to the different parts of the dashboard, each of which you explore in the following sections.

:::image type="content" source="../get-started/media/aspire-dashboard.png" lightbox="../get-started/media/aspire-dashboard.png" alt-text="A screenshot of the .NET Aspire dashboard Projects page.":::

The .NET Aspire dashboard is also available in a standalone mode. For more information, see [Standalone .NET Aspire dashboard](dashboard/standalone.md).

:::zone pivot="visual-studio"

## Visual Studio tooling

Visual Studio provides additional features for working with .NET Aspire integrations and the App Host orchestrator project. Not all of these features are currently available in Visual Studio Code or through the CLI.

### Add an integration package

You add .NET Aspire integrations to your app like any other NuGet package using Visual Studio. However, Visual Studio also provides UI options to add .NET Aspire integrations directly.

1. In Visual Studio, right select on the project you want to add an .NET Aspire integration to and select **Add** > **.NET Aspire package...**.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-add-aspire-package.png" lightbox="../media/visual-studio-add-aspire-package.png" alt-text="The Visual Studio context menu displaying the Add .NET Aspire Component option.":::

1. The package manager opens with search results preconfigured (populating filter criteria) for .NET Aspire integrations, allowing you to easily browse and select the desired integration.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-add-aspire-comp-nuget.png" lightbox="../media/visual-studio-add-aspire-comp-nuget.png" alt-text="The Visual Studio context menu displaying the Add .NET Aspire integration options.":::

For more information on .NET Aspire integrations, see [.NET Aspire integrations overview](integrations-overview.md).

### Add hosting packages

.NET Aspire hosting packages are used to configure various resources and dependencies an app may depend on or consume. Hosting packages are differentiated from other integration packages in that they're added to the _*.AppHost_ project. To add a hosting package to your app, follow these steps:

1. In Visual Studio, right select on the _*.AppHost_ project and select **Add** > **.NET Aspire package...**.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-add-aspire-hosting-package.png" lightbox="../media/visual-studio-add-aspire-hosting-package.png" alt-text="The Visual Studio context menu displaying the Add .NET Aspire Hosting Resource option.":::

1. The package manager opens with search results preconfigured (populating filter criteria) for .NET Aspire hosting packages, allowing you to easily browse and select the desired package.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-add-aspire-hosting-nuget.png" lightbox="../media/visual-studio-add-aspire-hosting-nuget.png" alt-text="The Visual Studio context menu displaying the Add .NET Aspire resource options.":::

### Add orchestration projects

You can add .NET Aspire orchestration projects to an existing app using the following steps:

1. In Visual Studio, right select on an existing project and select **Add** > **.NET Aspire Orchestrator Support..**.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-add-aspire-orchestrator.png" lightbox="../media/visual-studio-add-aspire-orchestrator.png" alt-text="The Visual Studio context menu displaying the Add .NET Aspire Orchestrator Support option.":::

1. A dialog window opens with a summary of the _*.AppHost_ and _*.ServiceDefaults_ projects that are added to your solution.

    :::image type="content" loc-scope="visual-studio" source="../media/add-orchestrator-app.png" alt-text="A screenshot showing the Visual Studio add .NET Aspire orchestration summary.":::

1. Select **OK** and the following changes are applied:

    - The _*.AppHost_ and _*.ServiceDefaults_ orchestration projects are added to your solution.
    - A call to `builder.AddServiceDefaults` will be added to the _:::no-loc text="Program.cs":::_ file of your original project.
    - A reference to your original project will be added to the _:::no-loc text="Program.cs":::_ file of the _*.AppHost_ project.

For more information on .NET Aspire orchestration, see [.NET Aspire orchestration overview](app-host-overview.md).

### Enlist in orchestration

Visual Studio provides the option to **Enlist in Aspire orchestration** during the new project workflow. Select this option to have Visual Studio create _*.AppHost_ and _*.ServiceDefaults_ projects alongside your selected project template.

:::image type="content" loc-scope="visual-studio" source="../media/aspire-enlist-orchestration.png" lightbox="../media/aspire-enlist-orchestration.png" alt-text="A screenshot showing how to enlist in .NET Aspire orchestration.":::

### Create test project

When you're using Visual Studio, and you select the **.NET Aspire Start Application** template, you have the option to include a test project. This test project is an xUnit project that includes a sample test that you can use as a starting point for your tests.

:::image type="content" source="media/setup-tooling/create-test-projects-template.png" lightbox="media/setup-tooling/create-test-projects-template.png" alt-text="A screenshot of Visual Studio displaying the option to create a test project.":::

For more information, see [Write your first .NET Aspire test](../testing/write-your-first-test.md).

:::zone-end
:::zone pivot="vscode"

## Visual Studio Code tooling

You can use Visual Studio Code, with the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit), to create and develop .NET Aspire projects. To create a new .NET Aspire project in Visual Studio Code, select the **Create .NET Project** button in the **Explorer** view, then select one of the .NET Aspire templates:

:::image type="content" source="media/vscode-create-dotnet-aspire-proj.png" lightbox="media/vscode-create-dotnet-aspire-proj.png" alt-text="A screenshot showing how to create a new .NET Aspire project in Visual Studio Code.":::

Once you create a new .NET Aspire project, you run and debug the app, stepping through breakpoints, and inspecting variables using the Visual Studio Code debugger:

:::image type="content" source="media/setup-tooling/vscode-debugging.png" lightbox="media/setup-tooling/vscode-debugging.png" alt-text="A screenshot showing how to debug a .NET Aspire project in Visual Studio Code.":::

:::zone-end

## See also

- [Unable to install .NET Aspire workload](../troubleshooting/unable-to-install-workload.md)
- [Use Dev Proxy with .NET Aspire project](/microsoft-cloud/dev/dev-proxy/how-to/use-dev-proxy-with-dotnet-aspire)

-----------------
-----------------
-----------------
-----------------


---
title: .NET Aspire templates
description: Learn about the .NET Aspire templates and how to use them to create new apps.
ms.date: 03/11/2025
zone_pivot_groups: dev-environment
uid: dotnet/aspire/templates
---

# .NET Aspire templates

There are a number of .NET Aspire project templates available to you. You can use these templates to create full .NET Aspire solutions, or add individual projects to existing .NET Aspire solutions.

The .NET Aspire templates are available in the [📦 Aspire.ProjectTemplates](https://www.nuget.org/packages/Aspire.ProjectTemplates) NuGet package.

## Available templates

The .NET Aspire templates allow you to create new apps pre-configured with the .NET Aspire solutions structure and default settings. These projects also provide a unified debugging experience across the different resources of your app.

.NET Aspire templates are available in two categories: solution templates and project templates. Solution templates create a new .NET Aspire solution with multiple projects, while project templates create individual projects that can be added to an existing .NET Aspire solution.

### Solution templates

The following .NET Aspire solution templates are available, assume the solution is named _AspireSample_:

<a name="empty-app"></a>

- **.NET Aspire Empty App**: A minimal .NET Aspire project that includes the following:

  - [**AspireSample.AppHost**](#app-host): An orchestrator project designed to connect and configure the different projects and services of your app.
  - [**AspireSample.ServiceDefaults**](#service-defaults): A .NET Aspire shared project to manage configurations that are reused across the projects in your solution related to [resilience](/dotnet/core/resilience/http-resilience), [service discovery](../service-discovery/overview.md), and [telemetry](telemetry.md).

<a name="starter-app"></a>

- **.NET Aspire Starter App**: In addition to the [**.AppHost**](#app-host) and [**.ServiceDefaults**](#service-defaults) projects, the .NET Aspire Starter App also includes the following:

  - **AspireSample.ApiService**: An [ASP.NET Core Minimal API](/aspnet/core/fundamentals/minimal-apis) project is used to provide data to the frontend. This project depends on the shared [**AspireSample.ServiceDefaults**](#service-defaults) project.
  - **AspireSample.Web**: An [ASP.NET Core Blazor App](/aspnet/core/blazor) project with default .NET Aspire service configurations, this project depends on the [**AspireSample.ServiceDefaults**](#service-defaults) project.
  - **AspireSample.Test**: Either an [MSTest](#mstest-project), [NUnit](#nunit-project), or [xUnit](#xunit-project) test project with project references to the [**AspireSample.AppHost**](#app-host) and an example _WebTests.cs_ file demonstrating an integration test.

### Project templates

The following .NET Aspire project templates are available:

<a name="app-host"></a>

- **.NET Aspire App Host**: A standalone **.AppHost** project that can be used to orchestrate and manage the different projects and services of your app.

<a name="mstest-project"></a>
<a name="nunit-project"></a>
<a name="xunit-project"></a>

- **.NET Aspire Test projects**: These project templates are used to create test projects for your .NET Aspire app, and they're intended to represent functional and integration tests. The test projects include the following templates:

  - **MSTest**: A project that contains MSTest integration of a .NET Aspire AppHost project.
  - **NUnit**: A project that contains NUnit integration of a .NET Aspire AppHost project.
  - **xUnit**: A project that contains xUnit.net integration of a .NET Aspire AppHost project.
  
  For more information on the test templates, see [Testing in .NET Aspire](testing.md).

<a name="service-defaults"></a>

- **.NET Aspire Service Defaults**: A standalone **.ServiceDefaults** project that can be used to manage configurations that are reused across the projects in your solution related to [resilience](/dotnet/core/resilience/http-resilience), [service discovery](../service-discovery/overview.md), and [telemetry](./telemetry.md).

  > [!IMPORTANT]
  > The service defaults project template takes a `FrameworkReference` dependency on `Microsoft.AspNetCore.App`. This may not be ideal for some project types. For more information, see [.NET Aspire service defaults](service-defaults.md).

## Create solutions and projects using templates

To create a .NET Aspire solution or project, use Visual Studio, Visual Studio Code, or the .NET CLI, and base it on the available templates. Explore additional .NET Aspire templates in the [.NET Aspire samples](https://github.com/dotnet/aspire-samples) repository.

:::zone pivot="visual-studio"

To create a .NET Aspire project using Visual Studio, search for *Aspire* in the Visual Studio new project window and select your desired template.

:::image type="content" source="media/vs-create-dotnet-aspire-proj.png" lightbox="media/vs-create-dotnet-aspire-proj.png" alt-text="Visual Studio: .NET Aspire templates.":::

Follow the prompts to configure your project or solution from the template, and then select **Create**.

:::zone-end
:::zone pivot="vscode"

To create a .NET Aspire project using Visual Studio Code, search for *Aspire* in the Visual Studio Code new project window and select your desired template.

:::image type="content" source="media/vscode-create-dotnet-aspire-proj.png" lightbox="media/vscode-create-dotnet-aspire-proj.png" alt-text="Visual Studio Code: .NET Aspire templates.":::

Select the desired location, enter a name, and select **Create**.

:::zone-end
:::zone pivot="dotnet-cli"

To create a .NET Aspire solution or project using the .NET CLI, use the [dotnet new](/dotnet/core/tools/dotnet-new) command and specify which template you would like to create. Consider the following examples:

To create a basic [.NET Aspire app host](app-host-overview.md) project targeting the latest .NET version:

```dotnetcli
dotnet new aspire-apphost
```

To create a .NET Aspire starter app, which is a full solution with a sample UI and backing API included:

```dotnetcli
dotnet new aspire-starter
```

> [!TIP]
> .NET Aspire templates default to using the latest .NET version, even when using an earlier version of the .NET CLI. To manually specify the .NET version, use the `--framework <tfm>` option, e.g. to create a basic [.NET Aspire app host](app-host-overview.md) project targeting .NET 8:
>
> ```dotnetcli
> dotnet new aspire-apphost --framework net8.0
> ```

:::zone-end

## See also

- [.NET Aspire SDK](dotnet-aspire-sdk.md)
- [.NET Aspire setup and tooling](setup-tooling.md)
- [Testing in .NET Aspire](testing.md)

-----------------
-----------------
-----------------
-----------------

---
title: .NET Aspire and GitHub Codespaces
description: Learn how to use .NET Aspire with GitHub Codespaces.
ms.date: 02/24/2025
---

# .NET Aspire and GitHub Codespaces

[GitHub Codespaces](https://github.com/features/codespaces) offers a cloud-hosted development environment based on Visual Studio Code. It can be accessed directly from a web browser or through Visual Studio Code locally, where Visual Studio Code acts as a client connecting to a cloud-hosted backend. With .NET Aspire 9.1, comes logic to better support GitHub Codespaces including:

- Automatically configure port forwarding with the correct protocol.
- Automatically translate URLs in the .NET Aspire dashboard.

Before .NET Aspire 9.1 it was still possible to use .NET Aspire within a GitHub Codespace, however more manual configuration was required.

## GitHub Codespaces vs. Dev Containers

GitHub Codespaces builds upon Visual Studio Code and the [Dev Containers specification](https://containers.dev/implementors/spec/). In addition to supporting GitHub Codespaces, .NET Aspire 9.1 enhances support for using Visual Studio Code and locally hosted Dev Containers. While the experiences are similar, there are some differences. For more information, see [.NET Aspire and Visual Studio Code Dev Containers](dev-containers.md).

## Quick start using template repository

To configure GitHub Codespaces for .NET Aspire, use the _.devcontainer/devcontainer.json_ file in your repository. The simplest way to get started is by creating a new repository from our [template repository](https://github.com/dotnet/aspire-devcontainer). Consider the following steps:

1. [Create a new repository](https://github.com/new?template_name=aspire-devcontainer&template_owner=dotnet) using our template.

    :::image source="media/new-repository-from-template.png" lightbox="media/new-repository-from-template.png" alt-text="Create new repository.":::

    Once you provide the details and select **Create repository**, the repository is created and shown in GitHub.

1. From the new repository, select on the Code button and select the Codespaces tab and then select **Create codespace on main**.

    :::image source="media/create-codespace-from-repository.png" lightbox="media/create-codespace-from-repository.png" alt-text="Create codespace":::

    After you select **Create codespace on main**, you navigate to a web-based version of Visual Studio Code. Before you use the Codespace, the containerized development environment needs to be prepared. This process happens automatically on the server and you can review progress by selecting the **Building codespace** link on the notification in the bottom right of the browser window.

    :::image source="media/building-codespace-image.png" lightbox="media/building-codespace-image.png" alt-text="Building codespace":::

    When the container image has finished being built the **Terminal** prompt appears which signals that the environment is ready to be interacted with.

    :::image source="media/codespace-terminal.png" lightbox="media/codespace-terminal.png" alt-text="Codespace terminal prompt":::

    At this point, the .NET Aspire templates have been installed and the ASP.NET Core developer certificate has been added and accepted.

1. Create a new .NET Aspire project using the starter template.

    ```dotnetcli
    dotnet new aspire-starter --name HelloAspire
    ```

    This results in many files and folders being created in the repository, which are visible in the **Explorer** panel on the left side of the window.

    :::image source="media/codespaces-explorer-panel.png" lightbox="media/codespaces-explorer-panel.png" alt-text="Codespaces Explorer panel":::

1. Launch the app host via the _HelloAspire.AppHost/Program.cs_ file, by selecting the **Run project** button near the top-right corner of the **Tab bar**.

    :::image source="media/codespace-launch-apphost.png" lightbox="media/codespace-launch-apphost.png" alt-text="Launch app host in Codespace":::

    After a few moments the **Debug Console** panel is displayed, and it includes a link to the .NET Aspire dashboard exposed on a GitHub Codespaces endpoint with the authentication token.

    :::image source="media/codespaces-debug-console.png" lightbox="media/codespaces-debug-console.png" alt-text="Codespaces debug console":::

1. Open the .NET Aspire dashboard by selecting the dashboard URL in the **Debug Console**. This opens the .NET Aspire dashboard in a separate tab within your browser.

    You notice on the dashboard that all HTTP/HTTPS endpoints defined on resources have had their typical `localhost` address translated to a unique fully qualified subdomain on the `app.github.dev` domain.

    :::image source="media/codespaces-translated-urls.png" lightbox="media/codespaces-translated-urls.png" alt-text="Codespaces translated URLs":::

    Traffic to each of these endpoints is automatically forwarded to the underlying process or container running within the Codespace. This includes development time tools such as PgAdmin and Redis Insight.

    > [!NOTE]
    > In addition to the authentication token embedded within the URL of the dashboard link of the **Debug Console**, endpoints also require authentication via your GitHub identity to avoid port forwarded endpoints being accessible to everyone. For more information on port forwarding in GitHub Codespaces, see [Forwarding ports in your codespace](https://docs.github.com/codespaces/developing-in-a-codespace/forwarding-ports-in-your-codespace?tool=webui).

1. Commit changes to the GitHub repository.

    GitHub Codespaces doesn't automatically commit your changes to the branch you're working on in GitHub. You have to use the **Source Control** panel to stage and commit the changes and push them back to the repository.

    Working in a GitHub Codespace is similar to working with Visual Studio Code on your own machine. You can checkout different branches and push changes just like you normally would. In addition, you can easily spin up multiple Codespaces simultaneously if you want to quickly work on another branch without disrupting your existing debug session. For more information, see [Developing in a codespace](https://docs.github.com/codespaces/developing-in-a-codespace/developing-in-a-codespace?tool=webui).

1. Clean up your Codespace.

    GitHub Codespaces are temporary development environments and while you might use one for an extended period of time, they should be considered a disposable resource that you recreate as needed (with all of the customization/setup contained within the _devcontainer.json_ and associated configuration files).

    To delete your GitHub Codespace, visit the GitHub Codespaces page. This shows you a list of all of your Codespaces. From here you can perform management operations on each Codespace, including deleting them.

    GitHub charges for the use of Codespaces. For more information, see [Managing the cost of GitHub Codespaces in your organization](https://docs.github.com/codespaces/managing-codespaces-for-your-organization/choosing-who-owns-and-pays-for-codespaces-in-your-organization).

    > [!NOTE]
    > .NET Aspire supports the use of Dev Containers in Visual Studio Code independent of GitHub Codespaces. For more information on how to use Dev Containers locally, see [.NET Aspire and Dev Containers in Visual Studio Code](dev-containers.md).

## Manually configuring _devcontainer.json_

The preceding walkthrough demonstrates the streamlined process of creating a GitHub Codespace using the .NET Aspire Devcontainer template. If you already have an existing repository and wish to utilize Devcontainer functionality with .NET Aspire, add a _devcontainer.json_ file to the _.devcontainer_ folder within your repository:

```Directory
└───📂 .devcontainer
     └─── devcontainer.json
```

The [template repository](https://github.com/dotnet/aspire-devcontainer) contains a copy of the _devcontainer.json_ file that you can use as a starting point, which should be sufficient for .NET Aspire. The following JSON represents the latest version of the _.devcontainer/devcontainer.json_ file from the template:

:::code language="json" source="~/aspire-devcontainer/.devcontainer/devcontainer.json":::

## Speed up Codespace creation

Creating a GitHub Codespace can take some time as it prepares the underlying container image. To expedite this process, you can utilize _prebuilds_ to significantly reduce the creation time to approximately 30-60 seconds (exact timing might vary). For more information on GitHub Codespaces prebuilds, see [GitHub Codespaces prebuilds](https://docs.github.com/codespaces/prebuilding-your-codespaces/about-github-codespaces-prebuilds).

-----------------
-----------------
-----------------
-----------------


---
title: Dev Containers in Visual Studio Code
description: Learn how to use .NET Aspire with Dev Containers in Visual Studio Code.
ms.date: 02/25/2025
---

# .NET Aspire and Visual Studio Code Dev Containers

The [Dev Containers Visual Studio Code extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) provides a way for development teams to develop within a containerized environment where all dependencies are preconfigured. With .NET Aspire 9.1, there's added logic to better support working with .NET Aspire within a Dev Container environment by automatically configuring port forwarding.

Before .NET Aspire 9.1, it possible to use .NET Aspire within a Dev Container, however more manual configuration was required.

## Dev Containers vs. GitHub Codespaces

Using Dev Containers in Visual Studio Code is similar to using GitHub Codespaces. With the release of .NET Aspire 9.1, support for both Dev Containers in Visual Studio Code and GitHub Codespaces was enhanced. Although the experiences are similar, there are some differences. For more information on using .NET Aspire with GitHub Codespaces, see [.NET Aspire and GitHub Codespaces](github-codespaces.md).

## Quick start using template repository

To configure Dev Containers in Visual Studio Code, use the _.devcontainer/devcontainer.json file in your repository. The simplest way to get started is by creating a new repository from our [template repository](https://github.com/dotnet/aspire-devcontainer). Consider the following steps:

1. [Create a new repository](https://github.com/new?template_name=aspire-devcontainer&template_owner=dotnet) using our template.

    :::image source="media/new-repository-from-template.png" lightbox="media/new-repository-from-template.png" alt-text="Create new repository.":::

    Once you provide the details and select **Create repository**, the repository is created and shown in GitHub.

1. Clone the repository to your local developer workstation using the following command:

    ```dotnetcli
    git clone https://github.com/<org>/<username>/<repository>
    ```

1. Open the repository in Visual Studio Code. After a few moments Visual Studio Code detects the _.devcontainer/devcontainer.json_ file and prompt to open the repository inside a container. Select whichever option is most appropriate for your workflow.

    :::image source="media/reopen-in-container.png" lightbox="media/reopen-in-container.png" alt-text="Prompt to open repository inside a container.":::

    After a few moments, the list of files become visible and the local build of the dev container will be completed.

    :::image source="media/devcontainer-build-completed.png" lightbox="media/devcontainer-build-completed.png" alt-text="Dev Container build completed.":::

1. Open a new terminal window in Visual Studio Code (<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>\`</kbd>) and create a new .NET Aspire project using the `dotnet` command-line.

    ```dotnetcli
    dotnet new aspire-starter -n HelloAspire
    ```

    After a few moments, the project will be created and initial dependencies restored.

1. Open the _ProjectName.AppHost/Program.cs_ file in the editor and select the run button on the top right corner of the editor window.

    :::image source="media/vscode-run-button.png" lightbox="media/vscode-run-button.png" alt-text="Run button in editor.":::

    Visual Studio Code builds and starts the .NET Aspire app host and automatically opens the .NET Aspire Dashboard. Because the endpoints hosted in the container are using a self-signed certificate the first time, you access an endpoint for a specific Dev Container you're presented with a certificate error.

    :::image source="media/browser-certificate-error.png" lightbox="media/browser-certificate-error.png" alt-text="Browser certificate error.":::

    The certificate error is expected. Once you've confirmed that the URL being requested corresponds to the dashboard in the Dev Container you can ignore this warning.

    :::image source="media/aspire-dashboard-in-devcontainer.png" lightbox="media/aspire-dashboard-in-devcontainer.png" alt-text=".NET Aspire dashboard running in Dev Container.":::

    .NET Aspire automatically configures forwarded ports so that when you select on the endpoints in the .NET Aspire dashboard they're tunneled to processes and nested containers within the Dev Container.

1. Commit changes to the GitHub repository

    After successfully creating the .NET Aspire project and verifying that it launches and you can access the dashboard, it's a good idea to commit the changes to the repository.

## Manually configuring _devcontainer.json_

The preceding walkthrough demonstrates the streamlined process of creating a Dev Container using the .NET Aspire Dev Container template. If you already have an existing repository and wish to utilize Dev Container functionality with .NET Aspire, add a _devcontainer.json_ file to the _.devcontainer_ folder within your repository:

```Directory
└───📂 .devcontainer
     └─── devcontainer.json
```

The [template repository](https://github.com/dotnet/aspire-devcontainer) contains a copy of the _devcontainer.json_ file that you can use as a starting point, which should be sufficient for .NET Aspire. The following JSON represents the latest version of the _.devcontainer/devcontainer.json_ file from the template:

:::code language="json" source="~/aspire-devcontainer/.devcontainer/devcontainer.json":::

-----------
-----------
-----------
-----------


---
title: .NET Aspire orchestration overview
description: Learn the fundamental concepts of .NET Aspire orchestration and explore the various APIs for adding resources and expressing dependencies.
ms.date: 03/14/2025
ms.topic: overview
uid: dotnet/aspire/app-host
---

# .NET Aspire orchestration overview

.NET Aspire provides APIs for expressing resources and dependencies within your distributed application. In addition to these APIs, [there's tooling](setup-tooling.md#install-net-aspire-prerequisites) that enables several compelling scenarios. The orchestrator is intended for _local development_ purposes and isn't supported in production environments.

<span id="terminology"></span>

Before continuing, consider some common terminology used in .NET Aspire:

- **App model**: A collection of resources that make up your distributed application (<xref:Aspire.Hosting.DistributedApplication>), defined within the <xref:Aspire.Hosting.ApplicationModel> namespace. For a more formal definition, see [Define the app model](#define-the-app-model).
- **App host/Orchestrator project**: The .NET project that orchestrates the _app model_, named with the _*.AppHost_ suffix (by convention).
- **Resource**: A [resource](#built-in-resource-types) is a dependent part of an application, such as a .NET project, container, executable, database, cache, or cloud service. It represents any part of the application that can be managed or referenced.
- **Integration**: An integration is a NuGet package for either the _app host_ that models a _resource_ or a package that configures a client for use in a consuming app. For more information, see [.NET Aspire integrations overview](integrations-overview.md).
- **Reference**: A reference defines a connection between resources, expressed as a dependency using the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> API. For more information, see [Reference resources](#reference-resources) or [Reference existing resources](#reference-existing-resources).

> [!NOTE]
> .NET Aspire's orchestration is designed to enhance your _local development_ experience by simplifying the management of your cloud-native app's configuration and interconnections. While it's an invaluable tool for development, it's not intended to replace production environment systems like [Kubernetes](../deployment/overview.md#deploy-to-kubernetes), which are specifically designed to excel in that context.

## Define the app model

.NET Aspire empowers you to seamlessly build, provision, deploy, configure, test, run, and observe your distributed applications. All of these capabilities are achieved through the utilization of an _app model_ that outlines the resources in your .NET Aspire solution and their relationships. These resources encompass projects, executables, containers, and external services and cloud resources that your app depends on. Within every .NET Aspire solution, there's a designated [App host project](#app-host-project), where the app model is precisely defined using methods available on the <xref:Aspire.Hosting.IDistributedApplicationBuilder>. This builder is obtained by invoking <xref:Aspire.Hosting.DistributedApplication.CreateBuilder%2A?displayProperty=nameWithType>.

```csharp
// Create a new app model builder
var builder = DistributedApplication.CreateBuilder(args);

// TODO:
//   Add resources to the app model
//   Express dependencies between resources

builder.Build().Run();
```

## App host project

The app host project handles running all of the projects that are part of the .NET Aspire project. In other words, it's responsible for orchestrating all apps within the app model. The project itself is a .NET executable project that references the [📦 Aspire.Hosting.AppHost](https://www.nuget.org/packages/Aspire.Hosting.AppHost) NuGet package, sets the `IsAspireHost` property to `true`, and references the [.NET Aspire SDK](dotnet-aspire-sdk.md):

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <Sdk Name="Aspire.AppHost.Sdk" Version="9.1.0" />
    
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net9.0</TargetFramework>
        <IsAspireHost>true</IsAspireHost>
        <!-- Omitted for brevity -->
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Aspire.Hosting.AppHost" Version="9.1.0" />
    </ItemGroup>

    <!-- Omitted for brevity -->

</Project>
```

The following code describes an app host `Program` with two project references and a Redis cache:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiservice = builder.AddProject<Projects.AspireApp_ApiService>("apiservice");

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
       .WithExternalHttpEndpoints()
       .WithReference(cache)
       .WaitFor(cache)
       .WithReference(apiService)
       .WaitFor(apiService);

builder.Build().Run();
```

The preceding code:

- Creates a new app model builder using the <xref:Aspire.Hosting.DistributedApplication.CreateBuilder%2A> method.
- Adds a Redis `cache` resource named "cache" using the <xref:Aspire.Hosting.RedisBuilderExtensions.AddRedis*> method.
- Adds a project resource named "apiservice" using the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> method.
- Adds a project resource named "webfrontend" using the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> method.
  - Specifies that the project has external HTTP endpoints using the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExternalHttpEndpoints%2A> method.
  - Adds a reference to the `cache` resource and waits for it to be ready using the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> and <xref:Aspire.Hosting.ResourceBuilderExtensions.WaitFor*> methods.
  - Adds a reference to the `apiservice` resource and waits for it to be ready using the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> and <xref:Aspire.Hosting.ResourceBuilderExtensions.WaitFor*> methods.
- Builds and runs the app model using the <xref:Aspire.Hosting.DistributedApplicationBuilder.Build%2A> and <xref:Aspire.Hosting.DistributedApplication.Run%2A> methods.

The example code uses the [.NET Aspire Redis hosting integration](../caching/stackexchange-redis-integration.md#hosting-integration).

To help visualize the relationship between the app host project and the resources it describes, consider the following diagram:

:::image type="content" source="../media/app-host-resource-diagram.png" lightbox="../media/app-host-resource-diagram.png" alt-text="The relationship between the projects in the .NET Aspire Starter Application template.":::

Each resource must be uniquely named. This diagram shows each resource and the relationships between them. The container resource is named "cache" and the project resources are named "apiservice" and "webfrontend". The web frontend project references the cache and API service projects. When you're expressing references in this way, the web frontend project is saying that it depends on these two resources, the "cache" and "apiservice" respectively.

## Built-in resource types

.NET Aspire projects are made up of a set of resources. The primary base resource types in the [📦 Aspire.Hosting.AppHost](https://www.nuget.org/packages/Aspire.Hosting.AppHost) NuGet package are described in the following table:

| Method | Resource type | Description |
|--|--|--|
| <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> | <xref:Aspire.Hosting.ApplicationModel.ProjectResource> | A .NET project, for example, an ASP.NET Core web app. |
| <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.AddContainer%2A> | <xref:Aspire.Hosting.ApplicationModel.ContainerResource> | A container image, such as a Docker image. |
| <xref:Aspire.Hosting.ExecutableResourceBuilderExtensions.AddExecutable%2A> | <xref:Aspire.Hosting.ApplicationModel.ExecutableResource> | An executable file, such as a [Node.js app](../get-started/build-aspire-apps-with-nodejs.md). |
| <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddParameter%2A> | <xref:Aspire.Hosting.ApplicationModel.ParameterResource> | A parameter resource that can be used to [express external parameters](external-parameters.md). |

Project resources represent .NET projects that are part of the app model. When you add a project reference to the app host project, the .NET Aspire SDK generates a type in the `Projects` namespace for each referenced project. For more information, see [.NET Aspire SDK: Project references](dotnet-aspire-sdk.md#project-references).

To add a project to the app model, use the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Adds the project "apiservice" of type "Projects.AspireApp_ApiService".
var apiservice = builder.AddProject<Projects.AspireApp_ApiService>("apiservice");
```

Projects can be replicated and scaled out by adding multiple instances of the same project to the app model. To configure replicas, use the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.WithReplicas*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Adds the project "apiservice" of type "Projects.AspireApp_ApiService".
var apiservice = builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
                        .WithReplicas(3);
```

The preceding code adds three replicas of the "apiservice" project resource to the app model. For more information, see [.NET Aspire dashboard: Resource replicas](dashboard/explore.md#resource-replicas).

## Configure explicit resource start

Project, executable and container resources are automatically started with your distributed application by default. A resource can be configured to wait for an explicit startup instruction with the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExplicitStart*> method. A resource configured with <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExplicitStart*> is initialized with <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.NotStarted?displayProperty=nameWithType>.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.AspireApp_DbMigration>("dbmigration")
       .WithReference(postgresdb)
       .WithExplicitStart();
```

In the preceeding code the "dbmigration" resource is configured to not automatically start with the distributed application.

Resources with explicit start can be started from the .NET Aspire dashboard by clicking the "Start" command. For more information, see [.NET Aspire dashboard: Stop or Start a resource](dashboard/explore.md#stop-or-start-a-resource).

## Reference resources

A reference represents a dependency between resources. For example, you can probably imagine a scenario where you a web frontend depends on a Redis cache. Consider the following example app host `Program` C# code:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
       .WithReference(cache);
```

The "webfrontend" project resource uses <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> to add a dependency on the "cache" container resource. These dependencies can represent connection strings or [service discovery](../service-discovery/overview.md) information. In the preceding example, an environment variable is _injected_ into the "webfrontend" resource with the name `ConnectionStrings__cache`. This environment variable contains a connection string that the `webfrontend` uses to connect to Redis via the [.NET Aspire Redis integration](../caching/stackexchange-redis-caching-overview.md), for example, `ConnectionStrings__cache="localhost:62354"`.

### Waiting for resources

In some cases, you might want to wait for a resource to be ready before starting another resource. For example, you might want to wait for a database to be ready before starting an API that depends on it. To express this dependency, use the <xref:Aspire.Hosting.ResourceBuilderExtensions.WaitFor*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
       .WithReference(postgresdb)
       .WaitFor(postgresdb);
```

In the preceding code, the "apiservice" project resource waits for the "postgresdb" database resource to enter the <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.Running?displayProperty=nameWithType>. The example code shows the [.NET Aspire PostgreSQL integration](../database/postgresql-integration.md), but the same pattern can be applied to other resources.

Other cases might warrant waiting for a resource to run to completion, either <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.Exited?displayProperty=nameWithType> or <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.Finished?displayProperty=nameWithType> before the dependent resource starts. To wait for a resource to run to completion, use the <xref:Aspire.Hosting.ResourceBuilderExtensions.WaitForCompletion*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

var migration = builder.AddProject<Projects.AspireApp_Migration>("migration")
                       .WithReference(postgresdb)
                       .WaitFor(postgresdb);

builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
       .WithReference(postgresdb)
       .WaitForCompletion(migration);
```

In the preceding code, the "apiservice" project resource waits for the "migration" project resource to run to completion before starting. The "migration" project resource waits for the "postgresdb" database resource to enter the <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.Running?displayProperty=nameWithType>. This can be useful in scenarios where you want to run a database migration before starting the API service, for example.

#### Forcing resource start in the dashboard

Waiting for a resource can be bypassed using the "Start" command in the dashboard. Clicking "Start" on a waiting resource in the dashboard instructs it to start immediately without waiting for the resource to be healthy or completed. This can be useful when you want to test a resource immediately and don't want to wait for the app to be in the right state.

### APIs for adding and expressing resources

.NET Aspire [hosting integrations](integrations-overview.md#hosting-integrations) and [client integrations](integrations-overview.md#client-integrations) are both delivered as NuGet packages, but they serve different purposes. While _client integrations_ provide client library configuration for consuming apps outside the scope of the app host, _hosting integrations_ provide APIs for expressing resources and dependencies within the app host. For more information, see [.NET Aspire integrations overview: Integration responsibilities](integrations-overview.md#integration-responsibilities).

### Express container resources

To express a <xref:Aspire.Hosting.ApplicationModel.ContainerResource> you add it to an <xref:Aspire.Hosting.IDistributedApplicationBuilder> instance by calling the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.AddContainer%2A> method:

#### [Docker](#tab/docker)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithBindMount("ollama", "/root/.ollama")
    .WithBindMount("./ollamaconfig", "/usr/config")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama")
    .WithEntrypoint("/usr/config/entrypoint.sh")
    .WithContainerRuntimeArgs("--gpus=all");
```

For more information, see [GPU support in Docker Desktop](https://docs.docker.com/desktop/gpu/).

#### [Podman](#tab/podman)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithBindMount("ollama", "/root/.ollama")
    .WithBindMount("./ollamaconfig", "/usr/config")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama")
    .WithEntrypoint("/usr/config/entrypoint.sh")
    .WithContainerRuntimeArgs("--device", "nvidia.com/gpu=all");
```

For more information, see [GPU support in Podman](https://github.com/containers/podman/issues/19005).

---

The preceding code adds a container resource named "ollama" with the image `ollama/ollama`. The container resource is configured with multiple bind mounts, a named HTTP endpoint, an entrypoint that resolves to Unix shell script, and container run arguments with the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithContainerRuntimeArgs%2A> method.

#### Customize container resources

All <xref:Aspire.Hosting.ApplicationModel.ContainerResource> subclasses can be customized to meet your specific requirements. This can be useful when using a [hosting integration](integrations-overview.md#hosting-integrations) that models a container resource, but requires modifications. When you have an `IResourceBuilder<ContainerResource>` you can chain calls to any of the available APIs to modify the container resource. .NET Aspire container resources typically point to pinned tags, but you might want to use the `latest` tag instead.

To help exemplify this, imagine a scenario where you're using the [.NET Aspire Redis integration](../caching/stackexchange-redis-integration.md). If the Redis integration relies on the `7.4` tag and you want to use the `latest` tag instead, you can chain a call to the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithImageTag*> API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
                   .WithImageTag("latest");

// Instead of using the "7.4" tag, the "cache" 
// container resource now uses the "latest" tag.
```

For more information and additional APIs available, see <xref:Aspire.Hosting.ContainerResourceBuilderExtensions#methods>.

#### Container resource lifecycle

When the app host is run, the <xref:Aspire.Hosting.ApplicationModel.ContainerResource> is used to determine what container image to create and start. Under the hood, .NET Aspire runs the container using the defined container image by delegating calls to the appropriate OCI-compliant container runtime, either Docker or Podman. The following commands are used:

#### [Docker](#tab/docker)

First, the container is created using the `docker container create` command. Then, the container is started using the `docker container start` command.

- [docker container create](https://docs.docker.com/reference/cli/docker/container/create/): Creates a new container from the specified image, without starting it.
- [docker container start](https://docs.docker.com/reference/cli/docker/container/start/): Start one or more stopped containers.

These commands are used instead of `docker run` to manage attached container networks, volumes, and ports. Calling these commands in this order allows any IP (network configuration) to already be present at initial startup.

#### [Podman](#tab/podman)

First, the container is created using the `podman container create` command. Then, the container is started using the `podman container start` command.

- [podman container create](https://docs.podman.io/en/latest/markdown/podman-create.1.html): Creates a writable container layer over the specified image and prepares it for running.
- [podman container start](https://docs.podman.io/en/latest/markdown/podman-start.1.html): Start one or more stopped containers.

These commands are used instead of `podman run` to manage attached container networks, volumes, and ports. Calling these commands in this order allows any IP (network configuration) to already be present at initial startup.

---

Beyond the base resource types, <xref:Aspire.Hosting.ApplicationModel.ProjectResource>, <xref:Aspire.Hosting.ApplicationModel.ContainerResource>, and <xref:Aspire.Hosting.ApplicationModel.ExecutableResource>, .NET Aspire provides extension methods to add common resources to your app model. For more information, see [Hosting integrations](integrations-overview.md#hosting-integrations).

#### Container resource lifetime

By default, container resources use the _session_ container lifetime. This means that every time the app host process is started, the container is created and started. When the app host stops, the container is stopped and removed. Container resources can opt-in to a _persistent_ lifetime to avoid unnecessary restarts and use persisted container state. To achieve this, chain a call the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithLifetime*?displayProperty=nameWithType> API and pass <xref:Aspire.Hosting.ApplicationModel.ContainerLifetime.Persistent?displayProperty=nameWithType>:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithLifetime(ContainerLifetime.Persistent);
```

The preceding code adds a container resource named "ollama" with the image "ollama/ollama" and a persistent lifetime.

### Connection string and endpoint references

It's common to express dependencies between project resources. Consider the following example code:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiservice = builder.AddProject<Projects.AspireApp_ApiService>("apiservice");

builder.AddProject<Projects.AspireApp_Web>("webfrontend")
       .WithReference(cache)
       .WithReference(apiservice);
```

Project-to-project references are handled differently than resources that have well-defined connection strings. Instead of connection string being injected into the "webfrontend" resource, environment variables to support service discovery are injected.

| Method | Environment variable |
|--|--|
| `WithReference(cache)` | `ConnectionStrings__cache="localhost:62354"` |
| `WithReference(apiservice)` | `services__apiservice__http__0="http://localhost:5455"` <br /> `services__apiservice__https__0="https://localhost:7356"` |

Adding a reference to the "apiservice" project results in service discovery environment variables being added to the frontend. This is because typically, project-to-project communication occurs over HTTP/gRPC. For more information, see [.NET Aspire service discovery](../service-discovery/overview.md).

To get specific endpoints from a <xref:Aspire.Hosting.ApplicationModel.ContainerResource> or an <xref:Aspire.Hosting.ApplicationModel.ExecutableResource>, use one of the following endpoint APIs:

- <xref:Aspire.Hosting.ResourceBuilderExtensions.WithEndpoint*>
- <xref:Aspire.Hosting.ResourceBuilderExtensions.WithHttpEndpoint*>
- <xref:Aspire.Hosting.ResourceBuilderExtensions.WithHttpsEndpoint*>

Then call the <xref:Aspire.Hosting.ResourceBuilderExtensions.GetEndpoint*> API to get the endpoint which can be used to reference the endpoint in the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var customContainer = builder.AddContainer("myapp", "mycustomcontainer")
                             .WithHttpEndpoint(port: 9043, name: "endpoint");

var endpoint = customContainer.GetEndpoint("endpoint");

var apiservice = builder.AddProject<Projects.AspireApp_ApiService>("apiservice")
                        .WithReference(endpoint);
```

| Method                    | Environment variable                                  |
|---------------------------|-------------------------------------------------------|
| `WithReference(endpoint)` | `services__myapp__endpoint__0=https://localhost:9043` |

The `port` parameter is the port that the container is listening on. For more information on container ports, see [Container ports](networking-overview.md#container-ports). For more information on service discovery, see [.NET Aspire service discovery](../service-discovery/overview.md).

### Service endpoint environment variable format

In the preceding section, the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> method is used to express dependencies between resources. When service endpoints result in environment variables being injected into the dependent resource, the format might not be obvious. This section provides details on this format.

When one resource depends on another resource, the app host injects environment variables into the dependent resource. These environment variables configure the dependent resource to connect to the resource it depends on. The format of the environment variables is specific to .NET Aspire and expresses service endpoints in a way that is compatible with [Service Discovery](../service-discovery/overview.md).

Service endpoint environment variable names are prefixed with `services__` (double underscore), then the service name, the endpoint name, and finally the index. The index supports multiple endpoints for a single service, starting with `0` for the first endpoint and incrementing for each endpoint.

Consider the following environment variable examples:

```Environment
services__apiservice__http__0
```

The preceding environment variable expresses the first HTTP endpoint for the `apiservice` service. The value of the environment variable is the URL of the service endpoint. A named endpoint might be expressed as follows:

```Environment
services__apiservice__myendpoint__0
```

In the preceding example, the `apiservice` service has a named endpoint called `myendpoint`. The value of the environment variable is the URL of the service endpoint.

## Reference existing resources

Some situations warrant that you reference an existing resource, perhaps one that is deployed to a cloud provider. For example, you might want to reference an Azure database. In this case, you'd rely on the [Execution context](#execution-context) to dynamically determine whether the app host is running in "run" mode or "publish" mode. If you're running locally and want to rely on a cloud resource, you can use the `IsRunMode` property to conditionally add the reference. You might choose to instead create the resource in publish mode. Some [hosting integrations](integrations-overview.md#hosting-integrations) support providing a connection string directly, which can be used to reference an existing resource.

Likewise, there might be use cases where you want to integrate .NET Aspire into an existing solution. One common approach is to add the .NET Aspire app host project to an existing solution. Within your app host, you express dependencies by adding project references to the app host and [building out the app model](#define-the-app-model). For example, one project might depend on another. These dependencies are expressed using the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> method. For more information, see [Add .NET Aspire to an existing .NET app](../get-started/add-aspire-existing-app.md).

## App host life cycles

The .NET Aspire app host exposes several life cycles that you can hook into by implementing the <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook> interface. The following lifecycle methods are available:

| Order | Method | Description |
|--|--|--|
| **1** | <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook.BeforeStartAsync%2A> | Executes before the distributed application starts. |
| **2** | <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook.AfterEndpointsAllocatedAsync%2A> | Executes after the orchestrator allocates endpoints for resources in the application model. |
| **3** | <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook.AfterResourcesCreatedAsync%2A> | Executes after the resource was created by the orchestrator. |

While the app host provides life cycle hooks, you might want to register custom events. For more information, see [Eventing in .NET Aspire](../app-host/eventing.md).

### Register a life cycle hook

To register a life cycle hook, implement the <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook> interface and register the hook with the app host using the <xref:Aspire.Hosting.Lifecycle.LifecycleHookServiceCollectionExtensions.AddLifecycleHook*> API:

:::code source="snippets/lifecycles/AspireApp/AspireApp.AppHost/Program.cs":::

The preceding code:

- Implements the <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook> interface as a `LifecycleLogger`.
- Registers the life cycle hook with the app host using the <xref:Aspire.Hosting.Lifecycle.LifecycleHookServiceCollectionExtensions.AddLifecycleHook*> API.
- Logs a message for all the events.

When this app host is run, the life cycle hook is executed for each event. The following output is generated:

```Output
info: LifecycleLogger[0]
      BeforeStartAsync
info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 9.0.0
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Application host directory is: ..\AspireApp\AspireApp.AppHost
info: LifecycleLogger[0]
      AfterEndpointsAllocatedAsync
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: https://localhost:17043
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at https://localhost:17043/login?t=d80f598bc8a64c7ee97328a1cbd55d72
info: LifecycleLogger[0]
      AfterResourcesCreatedAsync
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application started. Press Ctrl+C to shut down.
```

The preferred way to hook into the app host life cycle is to use the eventing API. For more information, see [Eventing in .NET Aspire](../app-host/eventing.md).

## Execution context

The <xref:Aspire.Hosting.IDistributedApplicationBuilder> exposes an execution context (<xref:Aspire.Hosting.DistributedApplicationExecutionContext>), which provides information about the current execution of the app host. This context can be used to evaluate whether or not the app host is executing as "run" mode, or as part of a publish operation. Consider the following properties:

- <xref:Aspire.Hosting.DistributedApplicationExecutionContext.IsRunMode%2A>: Returns `true` if the current operation is running.
- <xref:Aspire.Hosting.DistributedApplicationExecutionContext.IsPublishMode%2A>: Returns `true` if the current operation is publishing.

This information can be useful when you want to conditionally execute code based on the current operation. Consider the following example that demonstrates using the `IsRunMode` property. In this case, an extension method is used to generate a stable node name for RabbitMQ for local development runs.

```csharp
private static IResourceBuilder<RabbitMQServerResource> RunWithStableNodeName(
    this IResourceBuilder<RabbitMQServerResource> builder)
{
    if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
    {
        builder.WithEnvironment(context =>
        {
            // Set a stable node name so queue storage is consistent between sessions
            var nodeName = $"{builder.Resource.Name}@localhost";
            context.EnvironmentVariables["RABBITMQ_NODENAME"] = nodeName;
        });
    }

    return builder;
}
```

The execution context is often used to conditionally add resources or connection strings that point to existing resources. Consider the following example that demonstrates conditionally adding Redis or a connection string based on the execution context:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.ExecutionContext.IsRunMode
    ? builder.AddRedis("redis")
    : builder.AddConnectionString("redis");

builder.AddProject<Projects.WebApplication>("api")
       .WithReference(redis);

builder.Build().Run();
```

In the preceding code:

- If the app host is running in "run" mode, a Redis container resource is added.
- If the app host is running in "publish" mode, a connection string is added.

This logic can easily be inverted to connect to an existing Redis resource when you're running locally, and create a new Redis resource when you're publishing.

> [!IMPORTANT]
> .NET Aspire provides common APIs to control the modality of resource builders, allowing resources to behave differently based on the execution mode. The fluent APIs are prefixed with `RunAs*` and `PublishAs*`. The `RunAs*` APIs influence the local development (or run mode) behavior, whereas the `PublishAs*` APIs influence the publishing of the resource. For more information on how the Azure resources use these APIs, see [Use existing Azure resources](../azure/integrations-overview.md#use-existing-azure-resources).

## Resource relationships  

Resource relationships link resources together. Relationships are informational and don't impact an app's runtime behavior. Instead, they're used when displaying details about resources in the dashboard. For example, relationships are visible in the [dashboard's resource details](./dashboard/explore.md#resource-details), and `Parent` relationships control resource nesting on the resources page.

Relationships are automatically created by some app model APIs. For example:

- <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> adds a relationship to the target resource with the type `Reference`.
- <xref:Aspire.Hosting.ResourceBuilderExtensions.WaitFor*> adds a relationship to the target resource with the type `WaitFor`.
- Adding a database to a DB container creates a relationship from the database to the container with the type `Parent`.

Relationships can also be explicitly added to the app model using <xref:Aspire.Hosting.ResourceBuilderExtensions.WithRelationship*> and <xref:Aspire.Hosting.ResourceBuilderExtensions.WithParentRelationship*>.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithDataVolume()
                       .AddDatabase("catalogdb");

builder.AddProject<Projects.AspireApp_CatalogDbMigration>("migration")
       .WithReference(catalogDb)
       .WithParentRelationship(catalogDb);

builder.Build().Run();
```

The preceding example uses <xref:Aspire.Hosting.ResourceBuilderExtensions.WithParentRelationship*> to configure `catalogdb` database as the `migration` project's parent. The `Parent` relationship is special because it controls resource nesting on the resource page. In this example, `migration` is nested under `catalogdb`.

> [!NOTE]
> There's validation for parent relationships to prevent a resource from having multiple parents or creating a circular reference. These configurations can't be rendered in the UI, and the app model will throw an error.

## See also

- [.NET Aspire integrations overview](integrations-overview.md)
- [.NET Aspire SDK](dotnet-aspire-sdk.md)
- [Eventing in .NET Aspire](../app-host/eventing.md)
- [Service discovery in .NET Aspire](../service-discovery/overview.md)
- [.NET Aspire service defaults](service-defaults.md)
- [Expressing external parameters](external-parameters.md)
- [.NET Aspire inner-loop networking overview](networking-overview.md)

--------------------
--------------------
--------------------
--------------------


---
title: Orchestrate Python apps in .NET Aspire
description: Learn how to integrate Python apps into a .NET Aspire app host project.
ms.date: 11/11/2024
---

# Orchestrate Python apps in .NET Aspire

In this article, you learn how to use Python apps in a .NET Aspire app host. The sample app in this article demonstrates launching a Python application. The Python extension for .NET Aspire requires the use of virtual environments.

[!INCLUDE [aspire-prereqs](../includes/aspire-prereqs.md)]

Additionally, you need to install [Python](https://www.python.org/downloads) on your machine. The sample app in this article was built with Python version 3.12.4 and pip version 24.1.2. To verify your Python and pip versions, run the following commands:

```python
python --version
```

```python
pip --version
```

To download Python (including `pip`), see the [Python download page](https://www.python.org/downloads).

## Create a .NET Aspire project using the template

To get started launching a Python project in .NET Aspire first use the starter template to create a .NET Aspire application host:

```dotnetcli
dotnet new aspire -o PythonSample
```

In the same terminal session, change directories into the newly created project:

```dotnetcli
cd PythonSample
```

Once the template has been created launch the app host with the following command to ensure that the app host and the [.NET Aspire dashboard](../fundamentals/dashboard/overview.md) launches successfully:

```dotnetcli
dotnet run --project PythonSample.AppHost/PythonSample.AppHost.csproj
```

Once the app host starts it should be possible to click on the dashboard link in the console output. At this point the dashboard will not show any resources. Stop the app host by pressing <kbd>Ctrl</kbd> + <kbd>C</kbd> in the terminal.

## Prepare a Python app

From your previous terminal session where you created the .NET Aspire solution, create a new directory to contain the Python source code.

```Console
mkdir hello-python
```

Change directories into the newly created _hello-python_ directory:

```Console
cd hello-python
```

### Initialize the Python virtual environment

To work with Python apps, they need to be within a virtual environment. To create a virtual environment, run the following command:

```python
python -m venv .venv
```

For more information on virtual environments, see the [Python: Install packages in a virtual environment using pip and venv](https://packaging.python.org/en/latest/guides/installing-using-pip-and-virtual-environments/).

To activate the virtual environment, enabling installation and usage of packages, run the following command:

### [Unix/macOS](#tab/bash)

```bash
source .venv/bin/activate
```

### [Windows](#tab/powershell)

```powershell
.venv\Scripts\Activate.ps1
```

---

Ensure that pip within the virtual environment is up-to-date by running the following command:

```python
python -m pip install --upgrade pip
```

## Install Python packages

Install the Flask package by creating a _requirements.txt_ file in the _hello-python_ directory and adding the following line:

```python
Flask==3.0.3
```

Then, install the Flask package by running the following command:

```python
python -m pip install -r requirements.txt
```

After Flask is installed, create a new file named _main.py_ in the _hello-python_ directory and add the following code:

```python
import os
import flask

app = flask.Flask(__name__)

@app.route('/', methods=['GET'])
def hello_world():
    return 'Hello, World!'

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8111))
    app.run(host='0.0.0.0', port=port)
```

The preceding code creates a simple Flask app that listens on port 8111 and returns the message `"Hello, World!"` when the root endpoint is accessed.

## Update the app host project

Install the Python hosting package by running the following command:

```dotnetcli
dotnet add ../PythonSample.AppHost/PythonSample.AppHost.csproj package Aspire.Hosting.Python --version 9.0.0
```

After the package is installed, the project XML should have a new package reference similar to the following:

:::code language="xml" source="snippets/PythonSample/PythonSample.AppHost/PythonSample.AppHost.csproj":::

Update the app host _Program.cs_ file to include the Python project, by calling the `AddPythonApp` API and specifying the project name, project path, and the entry point file:

:::code source="snippets/PythonSample/PythonSample.AppHost/Program.cs":::

> [!IMPORTANT]
> The `AddPythonApp` API is experimental and may change in future releases. For more information, see [ASPIREHOSTINGPYTHON001](../diagnostics/overview.md#aspirehostingpython001).

## Run the app

Now that you've added the Python hosting package, updated the app host _Program.cs_ file, and created a Python project, you can run the app host:

```dotnetcli
dotnet run --project ../PythonSample.AppHost/PythonSample.AppHost.csproj
```

Launch the dashboard by clicking the link in the console output. The dashboard should display the Python project as a resource.

:::image source="media/python-dashboard.png" lightbox="media/python-dashboard.png" alt-text=".NET Aspire dashboard: Python sample app.":::

Select the **Endpoints** link to open the `hello-python` endpoint in a new browser tab. The browser should display the message "Hello, World!":

:::image source="media/python-hello-world.png" lightbox="media/python-hello-world.png" alt-text=".NET Aspire dashboard: Python sample app endpoint.":::

Stop the app host by pressing <kbd>Ctrl</kbd> + <kbd>C</kbd> in the terminal.

## Add telemetry support.

To add a bit of observability, add telemetry to help monitor the dependant Python app. In the Python project, add the following OpenTelemetry package as a dependency in the _requirements.txt_ file:

:::code language="python" source="snippets/PythonSample/hello-python/requirements.txt" highlight="2-5":::

The preceding requirement update, adds the OpenTelemetry package and the OTLP exporter. Next, re-install the Python app requirements into the virtual environment by running the following command:

```python
python -m pip install -r requirements.txt
```

The preceding command installs the OpenTelemetry package and the OTLP exporter, in the virtual environment. Update the Python app to include the OpenTelemetry code, by replacing the existing _main.py_ code with the following:

:::code language="python" source="snippets/PythonSample/hello-python/main.py":::

Update the app host project's _launchSettings.json_ file to include the `ASPIRE_ALLOW_UNSECURED_TRANSPORT` environment variable:

:::code language="json" source="snippets/PythonSample/PythonSample.AppHost/Properties/launchSettings.json":::

The `ASPIRE_ALLOW_UNSECURED_TRANSPORT` variable is required because when running locally the OpenTelemetry client in Python rejects the local development certificate. Launch the _app host_ again:

```dotnetcli
dotnet run --project ../PythonSample.AppHost/PythonSample.AppHost.csproj
```

Once the app host has launched navigate to the dashboard and note that in addition to console log output, structured logging is also being routed through to the dashboard.

:::image source="media/python-telemetry-in-dashboard.png" lightbox="media/python-telemetry-in-dashboard.png" alt-text=".NET Aspire dashboard: Structured logging from Python process.":::

## Summary

While there are several considerations that are beyond the scope of this article, you learned how to build .NET Aspire solution that integrates with Python. You also learned how to use the `AddPythonApp` API to host Python apps.

## See also

- [GitHub: .NET Aspire Samples—Python hosting integration](https://github.com/dotnet/aspire-samples/tree/main/samples/AspireWithPython)

--------------------
--------------------
--------------------
--------------------


---
title: .NET Aspire app host configuration
description: Learn about the .NET Aspire app host configuration options.
ms.date: 11/21/2024
ms.topic: reference
---

# App host configuration

The app host project configures and starts your distributed application (<xref:Aspire.Hosting.DistributedApplication>). When a `DistributedApplication` runs it reads configuration from the app host. Configuration is loaded from environment variables that are set on the app host and <xref:Aspire.Hosting.DistributedApplicationOptions>.

Configuration includes:

- Settings for hosting the resource service, such as the address and authentication options.
- Settings used to start the [.NET Aspire dashboard](../fundamentals/dashboard/overview.md), such the dashboard's frontend and OpenTelemetry Protocol (OTLP) addresses.
- Internal settings that .NET Aspire uses to run the app host. These are set internally but can be accessed by integrations that extend .NET Aspire.

App host configuration is provided by the app host launch profile. The app host has a launch settings file call _launchSettings.json_ which has a list of launch profiles. Each launch profile is a collection of related options which defines how you would like `dotnet` to start your application.

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17134;http://localhost:15170",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21030",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22057"
      }
    }
  }
}
```

The preceding launch settings file:

- Has one launch profile named `https`.
- Configures an .NET Aspire app host project:
  - The `applicationUrl` property configures the dashboard launch address (`ASPNETCORE_URLS`).
  - Environment variables such as `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` and `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` are set on the app host.

For more information, see [.NET Aspire and launch profiles](../fundamentals/launch-profiles.md).

> [!NOTE]
> Configuration described on this page is for .NET Aspire app host project. To configure the standalone dashboard, see [dashboard configuration](../fundamentals/dashboard/configuration.md).

## Common configuration

| Option | Default value | Description |
|--|--|--|
| `ASPIRE_ALLOW_UNSECURED_TRANSPORT` | `false` | Allows communication with the app host without https. `ASPNETCORE_URLS` (dashboard address) and `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` (app host resource service address) must be secured with HTTPS unless true. |
| `DOTNET_ASPIRE_CONTAINER_RUNTIME` | `docker` | Allows the user of alternative container runtimes for resources backed by containers. Possible values are `docker` (default) or `podman`. See [Setup and tooling overview for more details](../fundamentals/setup-tooling.md).  |

## Resource service

A resource service is hosted by the app host. The resource service is used by the dashboard to fetch information about resources which are being orchestrated by .NET Aspire.

| Option | Default value | Description |
|--|--|--|
| `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` | `null` | Configures the address of the resource service hosted by the app host. Automatically generated with _launchSettings.json_ to have a random port on localhost. For example, `https://localhost:17037`. |
| `DOTNET_DASHBOARD_RESOURCESERVICE_APIKEY` | Automatically generated 128-bit entropy token. | The API key used to authenticate requests made to the app host's resource service. The API key is required if the app host is in run mode, the dashboard isn't disabled, and the dashboard isn't configured to allow anonymous access with `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`. |

## Dashboard

By default, the dashboard is automatically started by the app host. The dashboard supports [its own set of configuration](../fundamentals/dashboard/configuration.md), and some settings can be configured from the app host.

| Option | Default value | Description |
|--|--|--|
| `ASPNETCORE_URLS` | `null` | Dashboard address. Must be `https` unless `ASPIRE_ALLOW_UNSECURED_TRANSPORT` or `DistributedApplicationOptions.AllowUnsecuredTransport` is true. Automatically generated with _launchSettings.json_ to have a random port on localhost. The value in launch settings is set on the `applicationUrls` property. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Configures the environment the dashboard runs as. For more information, see [Use multiple environments in ASP.NET Core](/aspnet/core/fundamentals/environments). |
| `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` | `http://localhost:18889` if no gRPC endpoint is configured. | Configures the dashboard OTLP gRPC address. Used by the dashboard to receive telemetry over OTLP. Set on resources as the `OTEL_EXPORTER_OTLP_ENDPOINT` env var. The `OTEL_EXPORTER_OTLP_PROTOCOL` env var is `grpc`.  Automatically generated with _launchSettings.json_ to have a random port on localhost. |
| `DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` | `null` | Configures the dashboard OTLP HTTP address. Used by the dashboard to receive telemetry over OTLP. If only `DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` is configured then it is set on resources as the `OTEL_EXPORTER_OTLP_ENDPOINT` env var. The `OTEL_EXPORTER_OTLP_PROTOCOL` env var is `http/protobuf`. |
| `DOTNET_DASHBOARD_CORS_ALLOWED_ORIGINS` | `null` | Overrides the CORS allowed origins configured in the dashboard. This setting replaces the default behavior of calculating allowed origins based on resource endpoints. |
| `DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN` | Automatically generated 128-bit entropy token. | Configures the frontend browser token. This is the value that must be entered to access the dashboard when the auth mode is BrowserToken. If no browser token is specified then a new token is generated each time the app host is launched. |

## Internal

Internal settings are used by the app host and integrations. Internal settings aren't designed to be configured directly.

| Option | Default value | Description |
|--|--|--|
| `AppHost:Directory` | The content root if there's no project. | Directory of the project where the app host is located. Accessible from the <xref:Aspire.Hosting.IDistributedApplicationBuilder.AppHostDirectory?displayProperty=nameWithType>. |
| `AppHost:Path` | The directory combined with the application name. | The path to the app host. It combines the directory with the application name. |
| `AppHost:Sha256` | It is created from the app host name when the app host is in publish mode. Otherwise it is created from the app host path. | Hex encoded hash for the current application. The hash is based on the location of the app on the current machine so it is stable between launches of the app host. |
| `AppHost:OtlpApiKey` | Automatically generated 128-bit entropy token. | The API key used to authenticate requests sent to the dashboard OTLP service. The value is present if needed: the app host is in run mode, the dashboard isn't disabled, and the dashboard isn't configured to allow anonymous access with `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`. |
| `AppHost:BrowserToken` | Automatically generated 128-bit entropy token. | The browser token used to authenticate browsing to the dashboard when it is launched by the app host. The browser token can be set by `DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN`. The value is present if needed: the app host is in run mode, the dashboard isn't disabled, and the dashboard isn't configured to allow anonymous access with `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`. |
| `AppHost:ResourceService:AuthMode` | `ApiKey`. If `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` is true then the value is `Unsecured`. | The authentication mode used to access the resource service. The value is present if needed: the app host is in run mode and the dashboard isn't disabled. |
| `AppHost:ResourceService:ApiKey` | Automatically generated 128-bit entropy token. | The API key used to authenticate requests made to the app host's resource service. The API key can be set by `DOTNET_DASHBOARD_RESOURCESERVICE_APIKEY`. The value is present if needed: the app host is in run mode, the dashboard isn't disabled, and the dashboard isn't configured to allow anonymous access with `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`. |

----------------
----------------
----------------
----------------

---
title: Add Dockerfiles to your .NET app model
description: Learn how to add Dockerfiles to your .NET app model.
ms.date: 07/23/2024
---

# Add Dockerfiles to your .NET app model

With .NET Aspire it's possible to specify a _Dockerfile_ to build when the [app host](../fundamentals/app-host-overview.md) is started using either the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.AddDockerfile%2A> or <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithDockerfile%2A> extension methods.

## Add a Dockerfile to the app model

In the following example the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.AddDockerfile%2A> extension method is used to specify a container by referencing the context path for the container build.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var container = builder.AddDockerfile(
    "mycontainer", "relative/context/path");
```

Unless the context path argument is a rooted path the context path is interpreted as being relative to the app host projects directory (where the AppHost `*.csproj` folder is located).

By default the name of the _Dockerfile_ which is used is `Dockerfile` and is expected to be within the context path directory. It's possible to explicitly specify the _Dockerfile_ name either as an absolute path or a relative path to the context path.

This is useful if you wish to modify the specific _Dockerfile_ being used when running locally or when the app host is deploying.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var container = builder.ExecutionContext.IsRunMode
    ? builder.AddDockerfile(
          "mycontainer", "relative/context/path", "Dockerfile.debug")
    : builder.AddDockerfile(
          "mycontainer", "relative/context/path", "Dockerfile.release");
```

## Customize existing container resources

When using <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.AddDockerfile%2A> the return value is an `IResourceBuilder<ContainerResource>`. .NET Aspire includes many custom resource types that are derived from <xref:Aspire.Hosting.ApplicationModel.ContainerResource>.

Using the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithDockerfile%2A> extension method it's possible to continue using these strongly typed resource types and customize the underlying container that is used.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var pgsql = builder.AddPostgres("pgsql")
                   .WithDockerfile("path/to/context")
                   .WithPgAdmin();
```

## Pass build arguments

The <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithBuildArg%2A> method can be used to pass arguments into the container image build.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var container = builder.AddDockerfile("mygoapp", "relative/context/path")
                       .WithBuildArg("GO_VERSION", "1.22");
```

The value parameter on the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithBuildArg%2A> method can be a literal value (`boolean`, `string`, `int`) or it can be a resource builder for a [parameter resource](../fundamentals/external-parameters.md). The following code replaces the `GO_VERSION` with a parameter value that can be specified at deployment time.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var goVersion = builder.AddParameter("goversion");

var container = builder.AddDockerfile("mygoapp", "relative/context/path")
                       .WithBuildArg("GO_VERSION", goVersion);
```

Build arguments correspond to the [`ARG` command](https://docs.docker.com/build/guide/build-args/) in _Dockerfiles_. Expanding the preceding example, this is a multi-stage _Dockerfile_ which specifies specific container image version to use as a parameter.

```dockerfile
# Stage 1: Build the Go program
ARG GO_VERSION=1.22
FROM golang:${GO_VERSION} AS builder
WORKDIR /build
COPY . .
RUN go build mygoapp.go

# Stage 2: Run the Go program
FROM mcr.microsoft.com/cbl-mariner/base/core:2.0
WORKDIR /app
COPY --from=builder /build/mygoapp .
CMD ["./mygoapp"]
```

> [!NOTE]
> Instead of hardcoding values into the container image, it's recommended to use environment variables for values that frequently change. This avoids the need to rebuild the container image whenever a change is required.

## Pass build secrets

In addition to build arguments it's possible to specify build secrets using <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithBuildSecret%2A> which are made selectively available to individual commands in the _Dockerfile_ using the `--mount=type=secret` syntax on `RUN` commands.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var accessToken = builder.AddParameter("accesstoken", secret: true);

var container = builder.AddDockerfile("myapp", "relative/context/path")
                       .WithBuildSecret("ACCESS_TOKEN", accessToken);
```

For example, consider the `RUN` command in a _Dockerfile_ which exposes the specified secret to the specific command:

```dockerfile
# The helloworld command can read the secret from /run/secrets/ACCESS_TOKEN
RUN --mount=type=secret,id=ACCESS_TOKEN helloworld
```

> [!CAUTION]
> Caution should be exercised when passing secrets in build environments. This is often done when using a token to retrieve dependencies from private repositories or feeds before a build. It is important to ensure that the injected secrets are not copied into the final or intermediate images.

----------------
----------------
----------------
----------------

---
title: Custom resource commands in .NET Aspire
description: Learn how to create custom resource commands in .NET Aspire.
ms.date: 11/07/2024
ms.topic: how-to
---

# Custom resource commands in .NET Aspire

Each resource in the .NET Aspire [app model](app-host-overview.md#define-the-app-model) is represented as an <xref:Aspire.Hosting.ApplicationModel.IResource> and when added to the [distributed application builder](xref:Aspire.Hosting.IDistributedApplicationBuilder), it's the generic-type parameter of the <xref:Aspire.Hosting.ApplicationModel.IResourceBuilder`1> interface. You use the _resource builder_ API to chain calls, configuring the underlying resource, and in some situations, you might want to add custom commands to the resource. Some common scenario for creating a custom command might be running database migrations or seeding/resetting a database. In this article, you learn how to add a custom command to a Redis resource that clears the cache.

> [!IMPORTANT]
> These [.NET Aspire dashboard](dashboard/overview.md) commands are only available when running the dashboard locally. They're not available when running the dashboard in Azure Container Apps.

## Add custom commands to a resource

Start by creating a new .NET Aspire Starter App from the [available templates](aspire-sdk-templates.md). To create the solution from this template, follow the [Quickstart: Build your first .NET Aspire solution](../get-started/build-your-first-aspire-app.md). After creating this solution, add a new class named _RedisResourceBuilderExtensions.cs_ to the [app host project](app-host-overview.md#app-host-project). Replace the contents of the file with the following code:

:::code source="snippets/custom-commands/AspireApp/AspireApp.AppHost/RedisResourceBuilderExtensions.cs":::

The preceding code:

- Shares the <xref:Aspire.Hosting> namespace so that it's visible to the app host project.
- Is a `static class` so that it can contain extension methods.
- It defines a single extension method named `WithClearCommand`, extending the `IResourceBuilder<RedisResource>` interface.
- The `WithClearCommand` method registers a command named `clear-cache` that clears the cache of the Redis resource.
- The `WithClearCommand` method returns the `IResourceBuilder<RedisResource>` instance to allow chaining.

The `WithCommand` API adds the appropriate annotations to the resource, which are consumed in the [.NET Aspire dashboard](dashboard/overview.md). The dashboard uses these annotations to render the command in the UI. Before getting too far into those details, let's ensure that you first understand the parameters of the `WithCommand` method:

- `name`: The name of the command to invoke.
- `displayName`: The name of the command to display in the dashboard.
- `executeCommand`: The `Func<ExecuteCommandContext, Task<ExecuteCommandResult>>` to run when the command is invoked, which is where the command logic is implemented.
- `updateState`: The `Func<UpdateCommandStateContext, ResourceCommandState>` callback is invoked to determine the "enabled" state of the command, which is used to enable or disable the command in the dashboard.
- `iconName`: The name of the icon to display in the dashboard. The icon is optional, but when you do provide it, it should be a valid [Fluent UI Blazor icon name](https://www.fluentui-blazor.net/Icon#explorer).
- `iconVariant`: The variant of the icon to display in the dashboard, valid options are `Regular` (default) or `Filled`.

## Execute command logic

The `executeCommand` delegate is where the command logic is implemented. This parameter is defined as a `Func<ExecuteCommandContext, Task<ExecuteCommandResult>>`. The `ExecuteCommandContext` provides the following properties:

- `ExecuteCommandContext.ServiceProvider`: The `IServiceProvider` instance that's used to resolve services.
- `ExecuteCommandContext.ResourceName`: The name of the resource instance that the command is being executed on.
- `ExecuteCommandContext.CancellationToken`: The <xref:System.Threading.CancellationToken> that's used to cancel the command execution.

In the preceding example, the `executeCommand` delegate is implemented as an `async` method that clears the cache of the Redis resource. It delegates out to a private class-scoped function named `OnRunClearCacheCommandAsync` to perform the actual cache clearing. Consider the following code:

```csharp
private static async Task<ExecuteCommandResult> OnRunClearCacheCommandAsync(
    IResourceBuilder<RedisResource> builder,
    ExecuteCommandContext context)
{
    var connectionString = await builder.Resource.GetConnectionStringAsync() ??
        throw new InvalidOperationException(
            $"Unable to get the '{context.ResourceName}' connection string.");

    await using var connection = ConnectionMultiplexer.Connect(connectionString);

    var database = connection.GetDatabase();

    await database.ExecuteAsync("FLUSHALL");

    return CommandResults.Success();
}
```

The preceding code:

- Retrieves the connection string from the Redis resource.
- Connects to the Redis instance.
- Gets the database instance.
- Executes the `FLUSHALL` command to clear the cache.
- Returns a `CommandResults.Success()` instance to indicate that the command was successful.

## Update command state logic

The `updateState` delegate is where the command state is determined. This parameter is defined as a `Func<UpdateCommandStateContext, ResourceCommandState>`. The `UpdateCommandStateContext` provides the following properties:

- `UpdateCommandStateContext.ServiceProvider`: The `IServiceProvider` instance that's used to resolve services.
- `UpdateCommandStateContext.ResourceSnapshot`: The snapshot of the resource instance that the command is being executed on.

The immutable snapshot is an instance of `CustomResourceSnapshot`, which exposes all sorts of valuable details about the resource instance. Consider the following code:

```csharp
private static ResourceCommandState OnUpdateResourceState(
    UpdateCommandStateContext context)
{
    var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation(
            "Updating resource state: {ResourceSnapshot}",
            context.ResourceSnapshot);
    }

    return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
        ? ResourceCommandState.Enabled
        : ResourceCommandState.Disabled;
}
```

The preceding code:

- Retrieves the logger instance from the service provider.
- Logs the resource snapshot details.
- Returns `ResourceCommandState.Enabled` if the resource is healthy; otherwise, it returns `ResourceCommandState.Disabled`.

## Test the custom command

To test the custom command, update your app host project's _Program.cs_ file to include the following code:

:::code source="snippets/custom-commands/AspireApp/AspireApp.AppHost/Program.cs" highlight="4":::

The preceding code calls the `WithClearCommand` extension method to add the custom command to the Redis resource. Run the app and navigate to the .NET Aspire dashboard. You should see the custom command listed under the Redis resource. On the **Resources** page of the dashboard, select the ellipsis button under the **Actions** column:

:::image source="media/custom-clear-cache-command.png" lightbox="media/custom-clear-cache-command.png" alt-text=".NET Aspire dashboard: Redis cache resource with custom command displayed.":::

The preceding image shows the **Clear cache** command that was added to the Redis resource. The icon displays as a rabbit crosses out to indicate that the speed of the dependant resource is being cleared.

Select the **Clear cache** command to clear the cache of the Redis resource. The command should execute successfully, and the cache should be cleared:

:::image source="media/custom-clear-cache-command-succeeded.png" lightbox="media/custom-clear-cache-command-succeeded.png" alt-text=".NET Aspire dashboard: Redis cache resource with custom command executed.":::

## See also

- [.NET Aspire orchestration overview](app-host-overview.md)
- [.NET Aspire dashboard: Resource submenu actions](dashboard/explore.md#resource-submenu-actions)

----------------
----------------
----------------
----------------


---
title: .NET Aspire inner loop networking overview
description: Learn how .NET Aspire handles networking and endpoints, and how you can use them in your app code.
ms.date: 10/29/2024
ms.topic: overview
---

# .NET Aspire inner-loop networking overview

One of the advantages of developing with .NET Aspire is that it enables you to develop, test, and debug cloud-native apps locally. Inner-loop networking is a key aspect of .NET Aspire that allows your apps to communicate with each other in your development environment. In this article, you learn how .NET Aspire handles various networking scenarios with proxies, endpoints, endpoint configurations, and launch profiles.

## Networking in the inner loop

The inner loop is the process of developing and testing your app locally before deploying it to a target environment. .NET Aspire provides several tools and features to simplify and enhance the networking experience in the inner loop, such as:

- **Launch profiles**: Launch profiles are configuration files that specify how to run your app locally. You can use launch profiles (such as the _launchSettings.json_ file) to define the endpoints, environment variables, and launch settings for your app.
- **Kestrel configuration**: Kestrel configuration allows you to specify the endpoints that the Kestrel web server listens on. You can configure Kestrel endpoints in your app settings, and .NET Aspire automatically uses these settings to create endpoints.
- **Endpoints/Endpoint configurations**: Endpoints are the connections between your app and the services it depends on, such as databases, message queues, or APIs. Endpoints provide information such as the service name, host port, scheme, and environment variable. You can add endpoints to your app either implicitly (via launch profiles) or explicitly by calling <xref:Aspire.Hosting.ResourceBuilderExtensions.WithEndpoint%2A>.
- **Proxies**: .NET Aspire automatically launches a proxy for each service binding you add to your app, and assigns a port for the proxy to listen on. The proxy then forwards the requests to the port that your app listens on, which might be different from the proxy port. This way, you can avoid port conflicts and access your app and services using consistent and predictable URLs.

## How endpoints work

A service binding in .NET Aspire involves two integrations: a **service** representing an external resource your app requires (for example, a database, message queue, or API), and a **binding** that establishes a connection between your app and the service and provides necessary information.

.NET Aspire supports two service binding types: **implicit**, automatically created based on specified launch profiles defining app behavior in different environments, and **explicit**, manually created using <xref:Aspire.Hosting.ResourceBuilderExtensions.WithEndpoint%2A>.

Upon creating a binding, whether implicit or explicit, .NET Aspire launches a lightweight reverse proxy on a specified port, handling routing and load balancing for requests from your app to the service. The proxy is a .NET Aspire implementation detail, requiring no configuration or management concern.

To help visualize how endpoints work, consider the .NET Aspire starter templates inner-loop networking diagram:

:::image type="content" source="media/networking/networking-proxies-1x.png" lightbox="media/networking/networking-proxies.png" alt-text=".NET Aspire Starter Application template inner loop networking diagram.":::

## Launch profiles

When you call <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A>, the app host looks for _Properties/launchSettings.json_ to determine the default set of endpoints. The app host selects a specific launch profile using the following rules:

1. An explicit `launchProfileName` argument passed when calling `AddProject`.
1. The `DOTNET_LAUNCH_PROFILE` environment variable. For more information, see [.NET environment variables](/dotnet/core/tools/dotnet-environment-variables).
1. The first launch profile defined in _launchSettings.json_.

Consider the following _launchSettings.json_ file:

:::code language="json" source="snippets/networking/Networking.Frontend/Networking.Frontend/Properties/launchSettings.json":::

For the remainder of this article, imagine that you've created an <xref:Aspire.Hosting.IDistributedApplicationBuilder> assigned to a variable named `builder` with the <xref:Aspire.Hosting.DistributedApplication.CreateBuilder> API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
```

To specify the **http** and **https** launch profiles, configure the `applicationUrl` values for both in the _launchSettings.json_ file. These URLs are used to create endpoints for this project. This is the equivalent of:

:::code source="snippets/networking/Networking.AppHost/Program.WithLaunchProfile.cs" id="verbose":::

> [!IMPORTANT]
> If there's no _launchSettings.json_ (or launch profile), there are no bindings by default.

For more information, see [.NET Aspire and launch profiles](launch-profiles.md).

## Kestrel configured endpoints

.NET Aspire supports Kestrel endpoint configuration. For example, consider an _appsettings.json_ file for a project that defines a Kestrel endpoint with the HTTPS scheme and port 5271:

:::code language="json" source="snippets/networking/Networking.Frontend/Networking.Frontend/appsettings.Development.json" highlight="8-14":::

The preceding configuration specifies an `Https` endpoint. The `Url` property is set to `https://*:5271`, which means the endpoint listens on all interfaces on port 5271. For more information, see [Configure endpoints for the ASP.NET Core Kestrel web server](/aspnet/core/fundamentals/servers/kestrel/endpoints).

With the Kestrel endpoint configured, the project should remove any configured `applicationUrl` from the _launchSettings.json_ file.

> [!NOTE]
> If the `applicationUrl` is present in the _launchSettings.json_ file and the Kestrel endpoint is configured, the app host will throw an exception.

When you add a project resource, there's an overload that lets you specify that the Kestrel endpoint should be used instead of the _launchSettings.json_ file:

:::code source="snippets/networking/Networking.AppHost/Program.KestrelConfiguration.cs" id="kestrel":::

For more information, see <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A>.

## Ports and proxies

When defining a service binding, the host port is *always* given to the proxy that sits in front of the service. This allows single or multiple replicas of a service to behave similarly. Additionally, all resource dependencies that use the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> API rely of the proxy endpoint from the environment variable.

Consider the following method chain that calls <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A>, <xref:Aspire.Hosting.ResourceBuilderExtensions.WithHttpEndpoint%2A>, and then <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.WithReplicas%2A>:

:::code source="snippets/networking/Networking.AppHost/Program.WithReplicas.cs" id="withreplicas":::

The preceding code results in the following networking diagram:

:::image type="content" source="media/networking/proxy-with-replicas-1x.png" lightbox="media/networking/proxy-with-replicas.png" alt-text=".NET Aspire frontend app networking diagram with specific host port and two replicas.":::

The preceding diagram depicts the following:

- A web browser as an entry point to the app.
- A host port of 5066.
- The frontend proxy sitting between the web browser and the frontend service replicas, listening on port 5066.
- The `frontend_0` frontend service replica listening on the randomly assigned port 65001.
- The `frontend_1` frontend service replica listening on the randomly assigned port 65002.

Without the call to `WithReplicas`, there's only one frontend service. The proxy still listens on port 5066, but the frontend service listens on a random port:

:::code source="snippets/networking/Networking.AppHost/Program.HostPortAndRandomPort.cs" id="hostport":::

There are two ports defined:

- A host port of 5066.
- A random proxy port that the underlying service will be bound to.

:::image type="content" source="media/networking/proxy-host-port-and-random-port-1x.png" lightbox="media/networking/proxy-host-port-and-random-port.png" alt-text=".NET Aspire frontend app networking diagram with specific host port and random port.":::

The preceding diagram depicts the following:

- A web browser as an entry point to the app.
- A host port of 5066.
- The frontend proxy sitting between the web browser and the frontend service, listening on port 5066.
- The frontend service listening on random port of 65001.

The underlying service is fed this port via `ASPNETCORE_URLS` for project resources. Other resources access to this port by specifying an environment variable on the service binding:

:::code source="snippets/networking/Networking.AppHost/Program.EnvVarPort.cs" id="envvarport":::

The previous code makes the random port available in the `PORT` environment variable. The app uses this port to listen to incoming connections from the proxy. Consider the following diagram:

:::image type="content" source="media/networking/proxy-with-env-var-port-1x.png" lightbox="media/networking/proxy-with-env-var-port.png" alt-text=".NET Aspire frontend app networking diagram with specific host port and environment variable port.":::

The preceding diagram depicts the following:

- A web browser as an entry point to the app.
- A host port of 5067.
- The frontend proxy sitting between the web browser and the frontend service, listening on port 5067.
- The frontend service listening on an environment 65001.

> [!TIP]
> To avoid an endpoint being proxied, set the `IsProxied` property to `false` when calling the `WithEndpoint` extension method. For more information, see [Endpoint extensions: additional considerations](#additional-considerations).

## Omit the host port

When you omit the host port, .NET Aspire generates a random port for both host and service port. This is useful when you want to avoid port conflicts and don't care about the host or service port. Consider the following code:

:::code source="snippets/networking/Networking.AppHost/Program.OmitHostPort.cs" id="omithostport":::

In this scenario, both the host and service ports are random, as shown in the following diagram:

:::image type="content" source="media/networking/proxy-with-random-ports-1x.png" lightbox="media/networking/proxy-with-random-ports.png" alt-text=".NET Aspire frontend app networking diagram with random host port and proxy port.":::

The preceding diagram depicts the following:

- A web browser as an entry point to the app.
- A random host port of 65000.
- The frontend proxy sitting between the web browser and the frontend service, listening on port 65000.
- The frontend service listening on a random port of 65001.

## Container ports

When you add a container resource, .NET Aspire automatically assigns a random port to the container. To specify a container port, configure the container resource with the desired port:

:::code source="snippets/networking/Networking.AppHost/Program.ContainerPort.cs" id="containerport":::

The preceding code:

- Creates a container resource named `frontend`, from the `mcr.microsoft.com/dotnet/samples:aspnetapp` image.
- Exposes an `http` endpoint by binding the host to port 8000 and mapping it to the container's port 8080.

Consider the following diagram:

:::image type="content" source="media/networking/proxy-with-docker-port-mapping-1x.png" alt-text=".NET Aspire frontend app networking diagram with a docker host.":::

## Endpoint extension methods

Any resource that implements the <xref:Aspire.Hosting.ApplicationModel.IResourceWithEndpoints> interface can use the `WithEndpoint` extension methods. There are several overloads of this extension, allowing you to specify the scheme, container port, host port, environment variable name, and whether the endpoint is proxied.

There's also an overload that allows you to specify a delegate to configure the endpoint. This is useful when you need to configure the endpoint based on the environment or other factors. Consider the following code:

:::code source="snippets/networking/Networking.AppHost/Program.WithEndpoint.cs" id="withendpoint":::

The preceding code provides a callback delegate to configure the endpoint. The endpoint is named `admin` and configured to use the `http` scheme and transport, as well as the 17003 host port. The consumer references this endpoint by name, consider the following `AddHttpClient` call:

```csharp
builder.Services.AddHttpClient<WeatherApiClient>(
    client => client.BaseAddress = new Uri("http://_admin.apiservice"));
```

The `Uri` is constructed using the `admin` endpoint name prefixed with the `_` sentinel. This is a convention to indicate that the `admin` segment is the endpoint name belonging to the `apiservice` service. For more information, see [.NET Aspire service discovery](../service-discovery/overview.md).

### Additional considerations

When calling the `WithEndpoint` extension method, the `callback` overload exposes the raw <xref:Aspire.Hosting.ApplicationModel.EndpointAnnotation>, which allows the consumer to customize many aspects of the endpoint.

The `AllocatedEndpoint` property allows you to get or set the endpoint for a service. The `IsExternal` and `IsProxied` properties determine how the endpoint is managed and exposed: `IsExternal` decides if it should be publicly accessible, while `IsProxied` ensures DCP manages it, allowing for internal port differences and replication.

> [!TIP]
> If you're hosting an external executable that runs its own proxy and encounters port binding issues due to DCP already binding the port, try setting the `IsProxied` property to `false`. This prevents DCP from managing the proxy, allowing your executable to bind the port successfully.

The `Name` property identifies the service, whereas the `Port` and `TargetPort` properties specify the desired and listening ports, respectively.

For network communication, the `Protocol` property supports **TCP** and **UDP**, with potential for more in the future, and the `Transport` property indicates the transport protocol (**HTTP**, **HTTP2**, **HTTP3**). Lastly, if the service is URI-addressable, the `UriScheme` property provides the URI scheme for constructing the service URI.

For more information, see the available properties of the [EndpointAnnotation properties](/dotnet/api/aspire.hosting.applicationmodel.endpointannotation#properties).

## Endpoint filtering

All .NET Aspire project resource endpoints follow a set of default heuristics. Some endpoints are included in `ASPNETCORE_URLS` at runtime, some are published as `HTTP/HTTPS_PORTS`, and some configurations are resolved from Kestrel configuration. Regardless of the default behavior, you can filter the endpoints that are included in environment variables by using the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.WithEndpointsInEnvironment%2A> extension method:

:::code source="snippets/networking/Networking.AppHost/Program.EndpointFilter.cs" id="filter":::

The preceding code adds a default HTTPS endpoint, as well as an `admin` endpoint on port 19227. However, the `admin` endpoint is excluded from the environment variables. This is useful when you want to expose an endpoint for internal use only.

-------------
-------------
-------------
-------------

---
title: Eventing in .NET Aspire
description: Learn how to use the .NET eventing features with .NET Aspire.
ms.date: 11/13/2024
---

# Eventing in .NET Aspire

In .NET Aspire, eventing allows you to publish and subscribe to events during various [app host life cycles](xref:dotnet/aspire/app-host#app-host-life-cycles). Eventing is more flexible than life cycle events. Both let you run arbitrary code during event callbacks, but eventing offers finer control of event timing, publishing, and provides supports for custom events.

The eventing mechanisms in .NET Aspire are part of the [📦 Aspire.Hosting](https://www.nuget.org/packages/Aspire.Hosting) NuGet package. This package provides a set of interfaces and classes in the <xref:Aspire.Hosting.Eventing> namespace that you use to publish and subscribe to events in your .NET Aspire app host project. Eventing is scoped to the app host itself and the resources within.

In this article, you learn how to use the eventing features in .NET Aspire.

## App host eventing

The following events are available in the app host and occur in the following order:

1. <xref:Aspire.Hosting.ApplicationModel.BeforeStartEvent>: This event is raised before the app host starts.
1. <xref:Aspire.Hosting.ApplicationModel.AfterEndpointsAllocatedEvent>: This event is raised after the app host allocated endpoints.
1. <xref:Aspire.Hosting.ApplicationModel.AfterResourcesCreatedEvent>: This event is raised after the app host created resources.

All of the preceding events are analogous to the [app host life cycles](xref:dotnet/aspire/app-host#app-host-life-cycles). That is, an implementation of the <xref:Aspire.Hosting.Lifecycle.IDistributedApplicationLifecycleHook> could handle these events just the same. With the eventing API, however, you can run arbitrary code when these events are raised and event define custom events—any event that implements the <xref:Aspire.Hosting.Eventing.IDistributedApplicationEvent> interface.

### Subscribe to app host events

To subscribe to the built-in app host events, use the eventing API. After you have a distributed application builder instance, walk up to the <xref:Aspire.Hosting.IDistributedApplicationBuilder.Eventing?displayProperty=nameWithType> property and call the <xref:Aspire.Hosting.Eventing.IDistributedApplicationEventing.Subscribe``1(System.Func{``0,System.Threading.CancellationToken,System.Threading.Tasks.Task})> API. Consider the following sample app host _Program.cs_ file:

:::code source="snippets/AspireApp/AspireApp.AppHost/Program.cs" highlight="17-25,27-35,37-45":::

The preceding code is based on the starter template with the addition of the calls to the `Subscribe` API. The `Subscribe<T>` API returns a <xref:Aspire.Hosting.Eventing.DistributedApplicationEventSubscription> instance that you can use to unsubscribe from the event. It's common to discard the returned subscriptions, as you don't usually need to unsubscribe from events as the entire app is torn down when the app host is shut down.

When the app host is run, by the time the .NET Aspire dashboard is displayed, you should see the following log output in the console:

:::code language="Plaintext" source="snippets/AspireApp/AspireApp.AppHost/Console.txt" highlight="2,10,16":::

The log output confirms that event handlers are executed in the order of the app host life cycle events. The subscription order doesn't affect execution order. The `BeforeStartEvent` is triggered first, followed by `AfterEndpointsAllocatedEvent`, and finally `AfterResourcesCreatedEvent`.

## Resource eventing

In addition to the app host events, you can also subscribe to resource events. Resource events are raised specific to an individual resource. Resource events are defined as implementations of the <xref:Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent> interface. The following resource events are available in the listed order:

1. <xref:Aspire.Hosting.ApplicationModel.ConnectionStringAvailableEvent>: Raised when a connection string becomes available for a resource.
1. <xref:Aspire.Hosting.ApplicationModel.BeforeResourceStartedEvent>: Raised before the orchestrator starts a new resource.
1. <xref:Aspire.Hosting.ApplicationModel.ResourceReadyEvent>: Raised when a resource initially transitions to a ready state.

### Subscribe to resource events

To subscribe to resource events, use the eventing API. After you have a distributed application builder instance, walk up to the <xref:Aspire.Hosting.IDistributedApplicationBuilder.Eventing?displayProperty=nameWithType> property and call the <xref:Aspire.Hosting.Eventing.IDistributedApplicationEventing.Subscribe``1(Aspire.Hosting.ApplicationModel.IResource,System.Func{``0,System.Threading.CancellationToken,System.Threading.Tasks.Task})> API. Consider the following sample app host _Program.cs_ file:

:::code source="snippets/AspireApp/AspireApp.ResourceAppHost/Program.cs" highlight="8-17,19-28,30-39":::

The preceding code subscribes to the `ResourceReadyEvent`, `ConnectionStringAvailableEvent`, and `BeforeResourceStartedEvent` events on the `cache` resource. When <xref:Aspire.Hosting.RedisBuilderExtensions.AddRedis*> is called, it returns an <xref:Aspire.Hosting.ApplicationModel.IResourceBuilder`1> where `T` is a <xref:Aspire.Hosting.ApplicationModel.RedisResource>. The resource builder exposes the resource as the <xref:Aspire.Hosting.ApplicationModel.IResourceBuilder`1.Resource?displayProperty=nameWithType> property. The resource in question is then passed to the `Subscribe` API to subscribe to the events on the resource.

When the app host is run, by the time the .NET Aspire dashboard is displayed, you should see the following log output in the console:

:::code language="Plaintext" source="snippets/AspireApp/AspireApp.ResourceAppHost/Console.txt" highlight="8,10,12":::

> [!NOTE]
> Some events are blocking. For example, when the `BeforeResourceStartEvent` is published, the startup of the resource will be blocked until all subscriptions for that event on a given resource have completed executing. Whether an event is blocking or not depends on how it is published (see the following section).

## Publish events

When subscribing to any of the built-in events, you don't need to publish the event yourself as the app host orchestrator manages to publish built-in events on your behalf. However, you can publish custom events with the eventing API. To publish an event, you have to first define an event as an implementation of either the <xref:Aspire.Hosting.Eventing.IDistributedApplicationEvent> or <xref:Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent> interface. You need to determine which interface to implement based on whether the event is a global app host event or a resource-specific event.

Then, you can subscribe and publish the event by calling the either of the following APIs:

- <xref:Aspire.Hosting.Eventing.IDistributedApplicationEventing.PublishAsync``1(``0,System.Threading.CancellationToken)>: Publishes an event to all subscribes of the specific event type.
- <xref:Aspire.Hosting.Eventing.IDistributedApplicationEventing.PublishAsync``1(``0,Aspire.Hosting.Eventing.EventDispatchBehavior,System.Threading.CancellationToken)>: Publishes an event to all subscribes of the specific event type with a specified dispatch behavior.

### Provide an `EventDispatchBehavior`

When events are dispatched, you can control how the events are dispatched to subscribers. The event dispatch behavior is specified with the `EventDispatchBehavior` enum. The following behaviors are available:

- <xref:Aspire.Hosting.Eventing.EventDispatchBehavior.BlockingSequential?displayProperty=nameWithType>: Fires events sequentially and blocks until they're all processed.
- <xref:Aspire.Hosting.Eventing.EventDispatchBehavior.BlockingConcurrent?displayProperty=nameWithType>: Fires events concurrently and blocks until they are all processed.
- <xref:Aspire.Hosting.Eventing.EventDispatchBehavior.NonBlockingSequential?displayProperty=nameWithType>: Fires events sequentially but doesn't block.
- <xref:Aspire.Hosting.Eventing.EventDispatchBehavior.NonBlockingConcurrent?displayProperty=nameWithType>: Fires events concurrently but doesn't block.

The default behavior is `EventDispatchBehavior.BlockingSequential`. To override this behavior, when calling a publishing API such as <xref:Aspire.Hosting.Eventing.IDistributedApplicationEventing.PublishAsync*>, provide the desired behavior as an argument.

---------------------
---------------------
---------------------
---------------------

---
title: Persist data with .NET Aspire using volume mounts
description: Learn about .NET Aspire volume configurations.
ms.date: 04/26/2024
ms.topic: how-to
---

# Persist .NET Aspire project data using volumes

In this article, you learn how to configure .NET Aspire projects to persist data across app launches using volumes. A continuous set of data during local development is useful in many scenarios. Various .NET Aspire resource container types are able to leverage volume storage, such as PostgreSQL, Redis and Azure Storage.

## When to use volumes

By default, every time you start and stop a .NET Aspire project, the app also creates and destroys the app resource containers. This setup creates problems when you want to persist data in a database or storage services between app launches for testing or debugging. For example, you may want to handle the following scenarios:

- Work with a continuous set of data in a database during an extended development session.
- Test or debug a changing set of files in an Azure Blob Storage emulator.
- Maintain cached data or messages in a Redis instance across app launches.

These goals can all be accomplished using volumes. With volumes, you decide which services retain data between launches of your .NET Aspire project.

## Understand volumes

Volumes are the recommended way to persist data generated by containers and supported on both Windows and Linux. Volumes can store data from multiple containers at a time, offer high performance and are easy to back up or migrate. With .NET Aspire, you configure a volume for each resource container using the <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithBindMount%2A?displayProperty=nameWithType> method, which accepts three parameters:

- **Source**: The source path of the volume, which is the physical location on the host.
- **Target**: The target path in the container of the data you want to persist.

For the remainder of this article, imagine that your exploring a `Program` class in a .NET Aspire [app host project](app-host-overview.md) that's already defined the distributed app builder bits:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// TODO:
//   Consider various code snippets for configuring 
//   volumes here and persistent passwords.

builder.Build().Run();
```

The first code snippet to consider uses the `WithBindMount` API to configure a volume for a SQL Server resource. The following code demonstrates how to configure a volume for a SQL Server resource in a .NET Aspire app host project:

:::code language="csharp" source="snippets/volumes/VolumeMounts.AppHost/Program.WithBindMount.cs" id="mount":::

In this example:

- `VolumeMount.AppHost-sql-data` sets where the volume will be stored on the host.
- `/var/opt/mssql` sets the path to the database files in the container.

All .NET Aspire container resources can utilize volume mounts, and some provide convenient APIs for adding named volumes derived from resources. Using the `WithDataVolume` as an example, the following code is functionally equivalent to the previous example but more succinct:

:::code language="csharp" source="snippets/volumes/VolumeMounts.AppHost/Program.Implicit.cs" id="implicit":::

With the app host project being named `VolumeMount.AppHost`, the `WithDataVolume` method automatically creates a named volume as `VolumeMount.AppHost-sql-data` and is mounted to the `/var/opt/mssql` path in the SQL Server container. The naming convention is as follows:

- `{appHostProjectName}-{resourceName}-data`: The volume name is derived from the app host project name and the resource name.

## Create a persistent password

Named volumes require a consistent password between app launches. .NET Aspire conveniently provides random password generation functionality. Consider the previous example once more, where a password is generated automatically:

:::code language="csharp" source="snippets/volumes/VolumeMounts.AppHost/Program.Implicit.cs" id="implicit":::

Since the `password` parameter isn't provided when calling `AddSqlServer`, .NET Aspire automatically generates a password for the SQL Server resource.

> [!IMPORTANT]
> This isn't a persistent password! Instead, it changes every time the app host runs.

To create a _persistent_ password, you must override the generated password. To do this, run the following command in your app host project directory to set a local password in your .NET user secrets:

```dotnetcli
dotnet user-secrets set Parameters:sql-password <password>
```

The naming convention for these secrets is important to understand. The password is stored in configuration with the `Parameters:sql-password` key. The naming convention follows this pattern:

- `Parameters:{resourceName}-password`: In the case of the SQL Server resource (which was named `"sql"`), the password is stored in the configuration with the key `Parameters:sql-password`.

The same pattern applies to the other server-based resource types, such as those shown in the following table:

| Resource type | Hosting package | Example resource name | Override key |
|--|--|--|
| MySQL | [📦 Aspire.Hosting.MySql](https://www.nuget.org/packages/Aspire.Hosting.MySql) | `mysql` | `Parameters:mysql-password` |
| Oracle | [📦 Aspire.Hosting.Oracle](https://www.nuget.org/packages/Aspire.Hosting.Oracle) | `oracle` | `Parameters:oracle-password` |
| PostgreSQL | [📦 Aspire.Hosting.PostgreSQL](https://www.nuget.org/packages/Aspire.Hosting.PostgreSQL) | `postgresql` | `Parameters:postgresql-password` |
| RabbitMQ | [📦 Aspire.Hosting.RabbitMq](https://www.nuget.org/packages/Aspire.Hosting.RabbitMq) | `rabbitmq` | `Parameters:rabbitmq-password` |
| SQL Server | [📦 Aspire.Hosting.SqlServer](https://www.nuget.org/packages/Aspire.Hosting.SqlServer) | `sql` | `Parameters:sql-password` |

By overriding the generated password, you can ensure that the password remains consistent between app launches, thus creating a persistent password. An alternative approach is to use the `AddParameter` method to create a parameter that can be used as a password. The following code demonstrates how to create a persistent password for a SQL Server resource:

:::code language="csharp" source="snippets/volumes/VolumeMounts.AppHost/Program.ExplicitStable.cs" id="explicit":::

The preceding code snippet demonstrates how to create a persistent password for a SQL Server resource. The `AddParameter` method is used to create a parameter named `sql-password` that's considered a secret. The `AddSqlServer` method is then called with the `password` parameter to set the password for the SQL Server resource. For more information, see [External parameters](external-parameters.md).

## Next steps

You can apply the volume concepts in the preceding code to a variety of services, including seeding a database with data that will persist across app launches. Try combining these techniques with the resource implementations demonstrated in the following tutorials:

- [Tutorial: Connect an ASP.NET Core app to .NET Aspire storage integrations](../storage/azure-storage-integrations.md)
- [Tutorial: Connect an ASP.NET Core app to SQL Server using .NET Aspire and Entity Framework Core](../database/sql-server-integrations.md)
- [.NET Aspire orchestration overview](../fundamentals/app-host-overview.md)

---------------------
---------------------
---------------------
---------------------

---
title: External parameters
description: Learn how to express parameters such as secrets, connection strings, and other configuration values that might vary between environments.
ms.topic: how-to
ms.date: 12/06/2024
---

# External parameters

Environments provide context for the application to run in. Parameters express the ability to ask for an external value when running the app. Parameters can be used to provide values to the app when running locally, or to prompt for values when deploying. They can be used to model a wide range of scenarios including secrets, connection strings, and other configuration values that might vary between environments.

## Parameter values

Parameter values are read from the `Parameters` section of the app host's configuration and are used to provide values to the app while running locally. When you publish the app, if the value isn't configured you're prompted to provide it.

Consider the following example app host _:::no-loc text="Program.cs":::_ file:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter named "value"
var value = builder.AddParameter("value");

builder.AddProject<Projects.ApiService>("api")
       .WithEnvironment("EXAMPLE_VALUE", value);
```

The preceding code adds a parameter named `value` to the app host. The parameter is then passed to the `Projects.ApiService` project as an environment variable named `EXAMPLE_VALUE`.

### Configure parameter values

Adding parameters to the builder is only one aspect of the configuration. You must also provide the value for the parameter. The value can be provided in the app host configuration file, set as a user secret, or configured in any [other standard configuration](/dotnet/core/extensions/configuration). When parameter values aren't found, they're prompted for when publishing the app.

Consider the following app host configuration file _:::no-loc text="appsettings.json":::_:

```json
{
    "Parameters": {
        "value": "local-value"
    }
}
```

The preceding JSON configures a parameter in the `Parameters` section of the app host configuration. In other words, that app host is able to find the parameter as its configured. For example, you could walk up to the <xref:Aspire.Hosting.IDistributedApplicationBuilder.Configuration?displayProperty=nameWithType> and access the value using the `Parameters:value` key:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var key = $"Parameters:value";
var value = builder.Configuration[key]; // value = "local-value"
```

> [!IMPORTANT]
> However, you don't need to access this configuration value yourself in the app host. Instead, the <xref:Aspire.Hosting.ApplicationModel.ParameterResource> is used to pass the parameter value to dependent resources. Most often as an environment variable.

### Parameter representation in the manifest

.NET Aspire uses a [deployment manifest](../deployment/manifest-format.md) to represent the app's resources and their relationships. Parameters are represented in the manifest as a new primitive called `parameter.v0`:

```json
{
  "resources": {
    "value": {
      "type": "parameter.v0",
      "value": "{value.inputs.value}",
      "inputs": {
        "value": {
          "type": "string"
        }
      }
    }
  }
}
```

## Secret values

Parameters can be used to model secrets. When a parameter is marked as a secret, it serves as a hint to the manifest that the value should be treated as a secret. When you publish the app, the value is prompted for and stored in a secure location. When you run the app locally, the value is read from the `Parameters` section of the app host configuration.

Consider the following example app host _:::no-loc text="Program.cs":::_ file:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a secret parameter named "secret"
var secret = builder.AddParameter("secret", secret: true);

builder.AddProject<Projects.ApiService>("api")
       .WithEnvironment("SECRET", secret);

builder.Build().Run();
```

Now consider the following app host configuration file _:::no-loc text="appsettings.json":::_:

```json
{
    "Parameters": {
        "secret": "local-secret"
    }
}
```

The manifest representation is as follows:

```json
{
  "resources": {
    "value": {
      "type": "parameter.v0",
      "value": "{value.inputs.value}",
      "inputs": {
        "value": {
          "type": "string",
          "secret": true
        }
      }
    }
  }
}
```

## Connection string values

Parameters can be used to model connection strings. When you publish the app, the value is prompted for and stored in a secure location. When you run the app locally, the value is read from the `ConnectionStrings` section of the app host configuration.

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

Consider the following example app host _:::no-loc text="Program.cs":::_ file:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddConnectionString("redis");

builder.AddProject<Projects.WebApplication>("api")
       .WithReference(redis);

builder.Build().Run();
```

Now consider the following app host configuration file _:::no-loc text="appsettings.json":::_:

```json
{
    "ConnectionStrings": {
        "redis": "local-connection-string"
    }
}
```

For more information pertaining to connection strings and their representation in the deployment manifest, see [Connection string and binding references](../deployment/manifest-format.md#connection-string-and-binding-references).

## Parameter example

To express a parameter, consider the following example code:

:::code source="snippets/params/Parameters.AppHost/Program.cs":::

The following steps are performed:

- Adds a SQL Server resource named `sql` and publishes it as a connection string.
- Adds a database named `db`.
- Adds a parameter named `insertionRows`.
- Adds a project named `api` and associates it with the `Projects.Parameters_ApiService` project resource type-parameter.
- Passes the `insertionRows` parameter to the `api` project.
- References the `db` database.

The value for the `insertionRows` parameter is read from the `Parameters` section of the app host configuration file _:::no-loc text="appsettings.json":::_:

:::code language="json" source="snippets/params/Parameters.AppHost/appsettings.json":::

The `Parameters_ApiService` project consumes the `insertionRows` parameter. Consider the _:::no-loc text="Program.cs":::_ example file:

:::code source="snippets/params/Parameters.ApiService/Program.cs":::

## See also

- [.NET Aspire manifest format for deployment tool builders](../deployment/manifest-format.md)
- [Tutorial: Connect an ASP.NET Core app to SQL Server using .NET Aspire and Entity Framework Core](../database/sql-server-integrations.md)

---------------------
---------------------
---------------------
---------------------

---
title: .NET Aspire testing overview
description: Learn how .NET Aspire helps you to test your applications.
ms.date: 03/11/2025
---

# .NET Aspire testing overview

.NET Aspire supports automated testing of your application through the [📦 Aspire.Hosting.Testing](https://www.nuget.org/packages/Aspire.Hosting.Testing) NuGet package. This package provides the <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder> class, which is used to create a test host for your application. The testing builder launches your app host project in a background thread and manages its lifecycle, allowing you to control and manipulate the application and its resources through <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder> or <xref:Aspire.Hosting.DistributedApplication> instances.

By default, the testing builder disables the dashboard and randomizes the ports of proxied resources to enable multiple instances of your application to run concurrently. Once your test completes, disposing of the application or testing builder cleans up your app resources.

To get started writing your first integration test with .NET Aspire, see the [Write your first .NET Aspire test](./write-your-first-test.md) article.

## Testing .NET Aspire solutions

.NET Aspire's testing capabilities are designed specifically for closed-box integration testing of your entire distributed application. Unlike unit tests or open-box integration tests, which typically run individual components in isolation, .NET Aspire tests launch your complete solution (the app host and all its resources) as separate processes, closely simulating real-world scenarios.

Consider the following diagram that shows how the .NET Aspire testing project starts the app host, which then starts the application and its resources:

:::image type="content" source="media/testing-diagram-thumb.png" alt-text=".NET Aspire testing diagram" lightbox="media/testing-diagram.png":::

1. The **test project** starts the app host.
1. The **app host** process starts.
1. The **app host** runs the `Database`, `API`, and `Frontend` applications.
1. The **test project** sends an HTTP request to the `Frontend` application.

The diagram illustrates that the **test project** starts the app host, which then orchestrates the all dependent app resources—regardless of their type. The test project is able to send an HTTP request to the `Frontend` app, which depends on an `API` app, and the `API` app depends on a `Database`. A successful request confirms that the `Frontend` app can communicate with the `API` app, and that the `API` app can successfully get data from the `Database`. For more information on seeing this approach in action, see the [Write your first .NET Aspire test](write-your-first-test.md) article.

> [!IMPORTANT]
> .NET Aspire testing doesn't enable scenarios for mocking, substituting, or replacing services in dependency injection—as the tests run in a separate process.

Use .NET Aspire testing when you want to:

- Verify end-to-end functionality of your distributed application.
- Ensure interactions between multiple services and resources (such as databases) behave correctly in realistic conditions.
- Confirm data persistence and integration with real external dependencies, like a PostgreSQL database.

If your goal is to test a single project in isolation, run components in-memory, or mock external dependencies, consider using <xref:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory%601> instead.

> [!NOTE]
> .NET Aspire tests run your application as separate processes, meaning you don't have direct access to internal services or components from your test code. You can influence application behavior through environment variables or configuration settings, but internal state and services remain encapsulated within their respective processes.

## Disable port randomization

By default, .NET Aspire uses random ports to allow multiple instances of your application to run concurrently without interference. It uses [.NET Aspire's service discovery](../service-discovery/overview.md) to ensure applications can locate each other's endpoints. To disable port randomization, pass `"DcpPublisher:RandomizePorts=false"` when constructing your testing builder, as shown in the following snippet:

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.MyAppHost>(
        [
            "DcpPublisher:RandomizePorts=false"
        ]);
```

## Enable the dashboard

The testing builder disables the [.NET Aspire dashboard](../fundamentals/dashboard/overview.md) by default. To enable it, you can set the `DisableDashboard` property to `false`, when creating your testing builder as shown in the following snippet:

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.MyAppHost>(
        args: [],
        configureBuilder: (appOptions, hostSettings) =>
        {
            appOptions.DisableDashboard = false;
        });
```

## See also

- [Write your first .NET Aspire test](./write-your-first-test.md)
- [Managing the app host in .NET Aspire tests](./manage-app-host.md)
- [Access resources in .NET Aspire tests](./accessing-resources.md)

------------------
------------------
------------------
------------------

---
title: Write your first .NET Aspire test
description: Learn how to test your .NET Aspire solutions using the xUnit, NUnit, and MSTest testing frameworks.
ms.date: 2/24/2025
zone_pivot_groups: unit-testing-framework
---

# Write your first .NET Aspire test

In this article, you learn how to create a test project, write tests, and run them for your .NET Aspire solutions. The tests in this article aren't unit tests, but rather functional or integration tests. .NET Aspire includes several variations of [testing project templates](../fundamentals/setup-tooling.md#net-aspire-templates) that you can use to test your .NET Aspire resource dependencies—and their communications. The testing project templates are available for MSTest, NUnit, and xUnit testing frameworks and include a sample test that you can use as a starting point for your tests.

The .NET Aspire test project templates rely on the [📦 Aspire.Hosting.Testing](https://www.nuget.org/packages/Aspire.Hosting.Testing) NuGet package. This package exposes the <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder> class, which is used to create a test host for your distributed application. The distributed application testing builder launches your app host project with instrumentation hooks so that you can access and manipulate the host at various stages of its lifecyle. In particular, <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder> provides you access to <xref:Aspire.Hosting.IDistributedApplicationBuilder> and <xref:Aspire.Hosting.DistributedApplication> class to create and start the [app host](../fundamentals/app-host-overview.md).

## Create a test project

The easiest way to create a .NET Aspire test project is to use the testing project template. If you're starting a new .NET Aspire project and want to include test projects, the [Visual Studio tooling supports that option](../fundamentals/setup-tooling.md#create-test-project). If you're adding a test project to an existing .NET Aspire project, you can use the `dotnet new` command to create a test project:

:::zone pivot="xunit"

```dotnetcli
dotnet new aspire-xunit
```

:::zone-end
:::zone pivot="mstest"

```dotnetcli
dotnet new aspire-mstest
```

:::zone-end
:::zone pivot="nunit"

```dotnetcli
dotnet new aspire-nunit
```

:::zone-end

For more information, see the .NET CLI [dotnet new](/dotnet/core/tools/dotnet-new) command documentation.

## Explore the test project

The following example test project was created as part of the **.NET Aspire Starter Application** template. If you're unfamiliar with it, see [Quickstart: Build your first .NET Aspire project](../get-started/build-your-first-aspire-app.md). The .NET Aspire test project takes a project reference dependency on the target app host. Consider the template project:

:::zone pivot="xunit"

:::code language="xml" source="snippets/testing/xunit/AspireApp.Tests/AspireApp.Tests.csproj":::

:::zone-end
:::zone pivot="mstest"

:::code language="xml" source="snippets/testing/mstest/AspireApp.Tests/AspireApp.Tests.csproj":::

:::zone-end
:::zone pivot="nunit"

:::code language="xml" source="snippets/testing/nunit/AspireApp.Tests/AspireApp.Tests.csproj":::

:::zone-end

The preceding project file is fairly standard. There's a `PackageReference` to the [📦 Aspire.Hosting.Testing](https://www.nuget.org/packages/Aspire.Hosting.Testing) NuGet package, which includes the required types to write tests for .NET Aspire projects.

The template test project includes a `IntegrationTest1` class with a single test. The test verifies the following scenario:

- The app host is successfully created and started.
- The `webfrontend` resource is available and running.
- An HTTP request can be made to the `webfrontend` resource and returns a successful response (HTTP 200 OK).

Consider the following test class:

:::zone pivot="xunit"

:::code language="csharp" source="snippets/testing/xunit/AspireApp.Tests/IntegrationTest1.cs":::

:::zone-end
:::zone pivot="mstest"

:::code language="csharp" source="snippets/testing/mstest/AspireApp.Tests/IntegrationTest1.cs":::

:::zone-end
:::zone pivot="nunit"

:::code language="csharp" source="snippets/testing/nunit/AspireApp.Tests/IntegrationTest1.cs":::

:::zone-end

The preceding code:

- Relies on the <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder.CreateAsync*?displayProperty=nameWithType> API to asynchronously create the app host.
  - The `appHost` is an instance of `IDistributedApplicationTestingBuilder` that represents the app host.
  - The `appHost` instance has its service collection configured with the standard HTTP resilience handler. For more information, see [Build resilient HTTP apps: Key development patterns](/dotnet/core/resilience/http-resilience).
- The `appHost` has its <xref:Aspire.Hosting.Testing.IDistributedApplicationTestingBuilder.BuildAsync(System.Threading.CancellationToken)?displayProperty=nameWithType> method invoked, which returns the `DistributedApplication` instance as the `app`.
  - The `app` has its service provider get the <xref:Aspire.Hosting.ApplicationModel.ResourceNotificationService> instance.
  - The `app` is started asynchronously.
- An <xref:System.Net.Http.HttpClient> is created for the `webfrontend` resource by calling `app.CreateHttpClient`.
- The `resourceNotificationService` is used to wait for the `webfrontend` resource to be available and running.
- A simple HTTP GET request is made to the root of the `webfrontend` resource.
- The test asserts that the response status code is `OK`.

## Test resource environment variables

To further test resources and their expressed dependencies in your .NET Aspire solution, you can assert that environment variables are injected correctly. The following example demonstrates how to test that the `webfrontend` resource has an HTTPS environment variable that resolves to the `apiservice` resource:

:::zone pivot="xunit"

:::code language="csharp" source="snippets/testing/xunit/AspireApp.Tests/EnvVarTests.cs":::

:::zone-end
:::zone pivot="mstest"

:::code language="csharp" source="snippets/testing/mstest/AspireApp.Tests/EnvVarTests.cs":::

:::zone-end
:::zone pivot="nunit"

:::code language="csharp" source="snippets/testing/nunit/AspireApp.Tests/EnvVarTests.cs":::

:::zone-end

The preceding code:

- Relies on the <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder.CreateAsync*?displayProperty=nameWithType> API to asynchronously create the app host.
- The `builder` instance is used to retrieve an <xref:Aspire.Hosting.ApplicationModel.IResourceWithEnvironment> instance named "webfrontend" from the <xref:Aspire.Hosting.Testing.IDistributedApplicationTestingBuilder.Resources%2A?displayProperty=nameWithType>.
- The `webfrontend` resource is used to call <xref:Aspire.Hosting.ApplicationModel.ResourceExtensions.GetEnvironmentVariableValuesAsync%2A> to retrieve its configured environment variables.
- The <xref:Aspire.Hosting.DistributedApplicationOperation.Publish?displayProperty=nameWithType> argument is passed when calling `GetEnvironmentVariableValuesAsync` to specify environment variables that are published to the resource as binding expressions.
- With the returned environment variables, the test asserts that the `webfrontend` resource has an HTTPS environment variable that resolves to the `apiservice` resource.

## Summary

The .NET Aspire testing project template makes it easier to create test projects for .NET Aspire solutions. The template project includes a sample test that you can use as a starting point for your tests. The `DistributedApplicationTestingBuilder` follows a familiar pattern to the <xref:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1> in ASP.NET Core. It allows you to create a test host for your distributed application and run tests against it.

Finally, when using the `DistributedApplicationTestingBuilder` all resource logs are redirected to the `DistributedApplication` by default. The redirection of resource logs enables scenarios where you want to assert that a resource is logging correctly.

## See also

- [Unit testing C# in .NET using dotnet test and xUnit](/dotnet/core/testing/unit-testing-with-dotnet-test)
- [MSTest overview](/dotnet/core/testing/unit-testing-mstest-intro)
- [Unit testing C# with NUnit and .NET Core](/dotnet/core/testing/unit-testing-with-nunit)

------------------
------------------
------------------
------------------


---
title: Manage the app host in .NET Aspire tests
description: Learn how to manage the app host in .NET Aspire tests.
ms.date: 02/24/2025
zone_pivot_groups: unit-testing-framework
---

# Manage the app host in .NET Aspire tests

When writing functional or integration tests with .NET Aspire, managing the [app host](../fundamentals/app-host-overview.md) instance efficiently is crucial. The app host represents the full application environment and can be costly to create and tear down. This article explains how to manage the app host instance in your .NET Aspire tests.

For writing tests with .NET Aspire, you use the [📦 `Aspire.Hosting.Testing`](https://www.nuget.org/packages/Aspire.Hosting.Testing) NuGet package which contains some helper classes to manage the app host instance in your tests.

## Use the `DistributedApplicationTestingBuilder` class

In the [tutorial on writing your first test](./write-your-first-test.md), you were introduced to the <xref:Aspire.Hosting.Testing.DistributedApplicationTestingBuilder> class which can be used to create the app host instance:

```csharp
var appHost = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.AspireApp_AppHost>();
```

The `DistributedApplicationTestingBuilder.CreateAsync<T>` method takes the app host project type as a generic parameter to create the app host instance. While this method is executed at the start of each test, it's more efficient to create the app host instance once and share it across tests as the test suite grows.

:::zone pivot="xunit"

With xUnit, you implement the [IAsyncLifetime](https://github.com/xunit/xunit/blob/master/src/xunit.core/IAsyncLifetime.cs) interface on the test class to support asynchronous initialization and disposal of the app host instance. The `InitializeAsync` method is used to create the app host instance before the tests are run and the `DisposeAsync` method disposes the app host once the tests are completed.

```csharp
public class WebTests : IAsyncLifetime
{
    private DistributedApplication _app;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AspireApp_AppHost>();

        _app = await appHost.BuildAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // test code here
    }
}
```

:::zone-end
:::zone pivot="mstest"

With MSTest, you use the <xref:Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute> and <xref:Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute> on static methods of the test class to provide the initialization and cleanup of the app host instance. The `ClassInitialize` method is used to create the app host instance before the tests are run and the `ClassCleanup` method disposes the app host instance once the tests are completed.

```csharp
[TestClass]
public class WebTests
{
    private static DistributedApplication _app;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
       var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AspireApp_AppHost>();

        _app = await appHost.BuildAsync();
    }
    
    [ClassCleanup]
    public static async Task ClassCleanup() => await _app.DisposeAsync();

    [TestMethod]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // test code here
    }
}
```

:::zone-end
:::zone pivot="nunit"

With NUnit, you use the [OneTimeSetUp](https://docs.nunit.org/articles/nunit/writing-tests/attributes/onetimesetup.html) and [OneTimeTearDown](https://docs.nunit.org/articles/nunit/writing-tests/attributes/onetimeteardown.html) attributes on methods of the test class to provide the setup and teardown of the app host instance. The `OneTimeSetUp` method is used to create the app host instance before the tests are run and the `OneTimeTearDown` method disposes the app host instance once the tests are completed.

```csharp
public class WebTests
{
    private DistributedApplication _app;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
       var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AspireApp_AppHost>();

        _app = await appHost.BuildAsync();
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _app.DisposeAsync();

    [Test]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // test code here
    }
}
```

:::zone-end

By capturing the app host in a field when the test run is started, you can access it in each test without the need to recreate it, decreasing the time it takes to run the tests. Then, when the test run completes, the app host is disposed, which cleans up any resources that were created during the test run, such as containers.

## Pass arguments to your app host

You can access the arguments from your app host with the `args` parameter. Arguments are also passed to [.NET's configuration system](/dotnet/core/extensions/configuration), so you can override many configuration settings this way. In the following example, you override the [environment](/aspnet/core/fundamentals/environments) by specifying it as a command line option:

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.MyAppHost>(
    [
        "--environment=Testing"
    ]);
```

Other arguments can be passed to your app host `Program` and made available in your app host. In the next example, you pass an argument to the app host and use it to control whether you add data volumes to a Postgres instance.

In the app host `Program`, you use configuration to support enabling or disabling volumes:

```csharp
var postgres = builder.AddPostgres("postgres1");
if (builder.Configuration.GetValue("UseVolumes", true))
{
    postgres.WithDataVolume();
}
```

In test code, you pass `"UseVolumes=false"` in the `args` to the app host:

```csharp
public async Task DisableVolumesFromTest()
{
    // Disable volumes in the test builder via arguments:
    using var builder = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.TestingAppHost1_AppHost>(
        [
            "UseVolumes=false"
        ]);

    // The container will have no volume annotation since we disabled volumes by passing UseVolumes=false
    var postgres = builder.Resources.Single(r => r.Name == "postgres1");

    Assert.Empty(postgres.Annotations.OfType<ContainerMountAnnotation>());
}
```

## Use the `DistributedApplicationFactory` class

While the `DistributedApplicationTestingBuilder` class is useful for many scenarios, there might be situations where you want more control over starting the app host, such as executing code before the builder is created or after the app host is built. In these cases, you implement your own version of the <xref:Aspire.Hosting.Testing.DistributedApplicationFactory> class. This is what the `DistributedApplicationTestingBuilder` uses internally.

```csharp
public class TestingAspireAppHost()
    : DistributedApplicationFactory(typeof(Projects.AspireApp_AppHost))
{
    // override methods here
}
```

The constructor requires the type of the app host project reference as a parameter. Optionally, you can provide arguments to the underlying host application builder. These arguments control how the app host starts and provide values to the args variable used by the _Program.cs_ file to start the app host instance.

### Lifecycle methods

The `DistributionApplicationFactory` class provides several lifecycle methods that can be overridden to provide custom behavior throughout the preparation and creation of the app host. The available methods are `OnBuilderCreating`, `OnBuilderCreated`, `OnBuilding`, and `OnBuilt`.

For example, we can use the `OnBuilderCreating` method to set configuration, such as the subscription and resource group information for Azure, before the app host is created and any dependent Azure resources are provisioned, resulting in our tests using the correct Azure environment.

```csharp
public class TestingAspireAppHost() : DistributedApplicationFactory(typeof(Projects.AspireApp_AppHost))
{
    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Configuration ??= new();
        hostOptions.Configuration["environment"] = "Development";
        hostOptions.Configuration["AZURE_SUBSCRIPTION_ID"] = "00000000-0000-0000-0000-000000000000";
        hostOptions.Configuration["AZURE_RESOURCE_GROUP"] = "my-resource-group";
    }
}
```

Because of the order of precedence in the .NET configuration system, the environment variables will be used over anything in the _appsettings.json_ or _secrets.json_ file.

Another scenario you might want to use in the lifecycle is to configure the services used by the app host. In the following example, consider a scenario where you override the `OnBuilderCreated` API to add resilience to the `HttpClient`:

```csharp
protected override void OnBuilderCreated(
    DistributedApplicationBuilder applicationBuilder)
{
    applicationBuilder.Services.ConfigureHttpClientDefaults(clientBuilder =>
    {
        clientBuilder.AddStandardResilienceHandler();
    });
}
```

## See also

- [Write your first .NET Aspire test](./write-your-first-test.md)

-----------------
-----------------
-----------------
-----------------

---
title: Access resources in .NET Aspire tests
description: Learn how to access the resources from the .NET Aspire app host in your tests.
ms.date: 02/24/2025
zone_pivot_groups: unit-testing-framework
---

# Access resources in .NET Aspire tests

In this article, you learn how to access the resources from the .NET Aspire app host in your tests. The app host represents the full application environment and contains all the resources that are available to the application. When writing functional or integration tests with .NET Aspire, you might need to access these resources to verify the behavior of your application.

## Access HTTP resources

To access an HTTP resource, use the <xref:System.Net.Http.HttpClient> to request and receive responses. The <xref:Aspire.Hosting.DistributedApplication> and the <xref:Aspire.Hosting.Testing.DistributedApplicationFactory> both provide a <xref:Aspire.Hosting.Testing.DistributedApplicationFactory.CreateHttpClient*> method that's used to create an `HttpClient` instance for a specific resource, based on the resource name from the app host. This method also takes an optional `endpointName` parameter, so if the resource has multiple endpoints, you can specify which one to use.

## Access other resources

In a test, you might want to access other resources by the connection information they provide, for example, querying a database to verify the state of the data. For this, you use the <xref:Microsoft.Extensions.Configuration.ConfigurationExtensions.GetConnectionString*?displayProperty=nameWithType> method to retrieve the connection string for a resource, and then provide that to a client library within the test to interact with the resource.

## Ensure resources are available

Starting with .NET Aspire 9, there's support for waiting on dependent resources to be available (via the [health check](../fundamentals/health-checks.md) mechanism). This is useful in tests that ensure a resource is available before attempting to access it. The <xref:Aspire.Hosting.ApplicationModel.ResourceNotificationService> class provides a <xref:Aspire.Hosting.ApplicationModel.ResourceNotificationService.WaitForResourceAsync*?displayProperty=nameWithType> method that's used to wait for a named resource to be available. This method takes the resource name and the desired state of the resource as parameters and returns a <xref:System.Threading.Tasks.Task> that yields back when the resource is available. You can access the <xref:Aspire.Hosting.ApplicationModel.ResourceNotificationService> via <xref:Aspire.Hosting.DistributedApplication.ResourceNotifications?displayProperty=nameWithType>, as in the following example.

> [!NOTE]
> It's recommended to provide a time-out when waiting for resources, to prevent the test from hanging indefinitely in situations where a resource never becomes available.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await app.ResourceNotifications.WaitForResourceAsync(
    "webfrontend",  
    KnownResourceStates.Running,
    cts.Token); 
```

A resource enters the <xref:Aspire.Hosting.ApplicationModel.KnownResourceStates.Running?displayProperty=nameWithType> state as soon as it starts executing, but this doesn't mean that it's ready to serve requests. If you want to wait for the resource to be ready to serve requests, and your resource has health checks, you can wait for the resource to become healthy by using the <xref:Aspire.Hosting.ApplicationModel.ResourceNotificationService.WaitForResourceHealthyAsync*?displayProperty=nameWithType> method.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await app.ResourceNotifications.WaitForResourceHealthyAsync(
    "webfrontend",
    cts.Token);
```

This resource-notification pattern ensures that the resources are available before running the tests, avoiding potential issues with the tests failing due to the resources not being ready.

## See also

- [Write your first .NET Aspire test](./write-your-first-test.md)  
- [Managing the app host in .NET Aspire tests](./manage-app-host.md)

-----------------
-----------------
-----------------
-----------------

---
title: .NET Aspire service discovery
description: Understand essential service discovery concepts in .NET Aspire.
ms.date: 04/10/2024
ms.topic: quickstart
---

# .NET Aspire service discovery

In this article, you learn how service discovery works within a .NET Aspire project. .NET Aspire includes functionality for configuring service discovery at development and testing time. Service discovery functionality works by providing configuration in the format expected by the _configuration-based endpoint resolver_ from the .NET Aspire AppHost project to the individual service projects added to the application model. For more information, see [Service discovery in .NET](/dotnet/core/extensions/service-discovery).

## Implicit service discovery by reference

Configuration for service discovery is only added for services that are referenced by a given project. For example, consider the following AppHost program:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.CatalogService>("catalog");
var basket = builder.AddProject<Projects.BasketService>("basket");

var frontend = builder.AddProject<Projects.MyFrontend>("frontend")
                      .WithReference(basket)
                      .WithReference(catalog);
```

In the preceding example, the _frontend_ project references the _catalog_ project and the _basket_ project. The two <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> calls instruct the .NET Aspire project to pass service discovery information for the referenced projects (_catalog_, and _basket_) into the _frontend_ project.

## Named endpoints

Some services expose multiple, named endpoints. Named endpoints can be resolved by specifying the endpoint name in the host portion of the HTTP request URI, following the format `scheme://_endpointName.serviceName`. For example, if a service named "basket" exposes an endpoint named "dashboard", then the URI `https+http://_dashboard.basket` can be used to specify this endpoint, for example:

```csharp
builder.Services.AddHttpClient<BasketServiceClient>(
    static client => client.BaseAddress = new("https+http://basket"));

builder.Services.AddHttpClient<BasketServiceDashboardClient>(
    static client => client.BaseAddress = new("https+http://_dashboard.basket"));
```

In the preceding example, two <xref:System.Net.Http.HttpClient> classes are added, one for the core basket service and one for the basket service's dashboard.

### Named endpoints using configuration

With the configuration-based endpoint resolver, named endpoints can be specified in configuration by prefixing the endpoint value with `_endpointName.`, where `endpointName` is the endpoint name. For example, consider this _:::no-loc text="appsettings.json":::_ configuration which defined a default endpoint (with no name) and an endpoint named "dashboard":

```json
{
  "Services": {
    "basket":
      "https": "https://10.2.3.4:8080", /* the https endpoint, requested via https://basket */
      "dashboard": "https://10.2.3.4:9999" /* the "dashboard" endpoint, requested via https://_dashboard.basket */
    }
  }
}
```

In the preceding JSON:

- The default endpoint, when resolving `https://basket` is `10.2.3.4:8080`.
- The "dashboard" endpoint, resolved via `https://_dashboard.basket` is `10.2.3.4:9999`.

### Named endpoints in .NET Aspire

```csharp
var basket = builder.AddProject<Projects.BasketService>("basket")
    .WithHttpsEndpoint(hostPort: 9999, name: "dashboard");
```

### Named endpoints in Kubernetes using DNS SRV

When deploying to [Kubernetes](../deployment/overview.md#deploy-to-kubernetes), the DNS SRV service endpoint resolver can be used to resolve named endpoints. For example, the following resource definition will result in a DNS SRV record being created for an endpoint named "default" and an endpoint named "dashboard", both on the service named "basket".

```yml
apiVersion: v1
kind: Service
metadata:
  name: basket
spec:
  selector:
    name: basket-service
  clusterIP: None
  ports:
  - name: default
    port: 8080
  - name: dashboard
    port: 9999
```

To configure a service to resolve the "dashboard" endpoint on the "basket" service, add the DNS SRV service endpoint resolver to the host builder as follows:

```csharp
builder.Services.AddServiceDiscoveryCore();
builder.Services.AddDnsSrvServiceEndpointProvider();
```

For more information, see <xref:Microsoft.Extensions.DependencyInjection.ServiceDiscoveryServiceCollectionExtensions.AddServiceDiscoveryCore%2A> and <xref:Microsoft.Extensions.Hosting.ServiceDiscoveryDnsServiceCollectionExtensions.AddDnsSrvServiceEndpointProvider%2A>.

The special port name "default" is used to specify the default endpoint, resolved using the URI `https://basket`.

As in the previous example, add service discovery to an `HttpClient` for the basket service:

```csharp
builder.Services.AddHttpClient<BasketServiceClient>(
    static client => client.BaseAddress = new("https://basket"));
```

Similarly, the "dashboard" endpoint can be targeted as follows:

```csharp
builder.Services.AddHttpClient<BasketServiceDashboardClient>(
    static client => client.BaseAddress = new("https://_dashboard.basket"));
```

<!--
# TODO: Configuring polling interval and pending status refresh interval via `ServiceEndPointResolverOptions`

# TODO: Configuring DNS SRV

# TODO: DNS resolver (non-SRV)

# TODO: Configuring DNS
-->

## See also

- [Service discovery in .NET](/dotnet/core/extensions/service-discovery)
- [Make HTTP requests with the HttpClient class](/dotnet/fundamentals/networking/http/httpclient)
- [IHttpClientFactory with .NET](/dotnet/core/extensions/httpclient-factory)

-----------------------
-----------------------
-----------------------
-----------------------

---
title: .NET Aspire service defaults
description: Learn about the .NET Aspire service defaults project.
ms.date: 11/04/2024
ms.topic: reference
uid: dotnet/aspire/service-defaults
---

# .NET Aspire service defaults

In this article, you learn about the .NET Aspire service defaults project, a set of extension methods that:

- Connect [telemetry](telemetry.md), [health checks](health-checks.md), [service discovery](../service-discovery/overview.md) to your app.
- Are customizable and extensible.

Cloud-native applications often require extensive configurations to ensure they work across different environments reliably and securely. .NET Aspire provides many helper methods and tools to streamline the management of configurations for OpenTelemetry, health checks, environment variables, and more.

## Explore the service defaults project

When you either [**Enlist in .NET Aspire orchestration**](setup-tooling.md#enlist-in-orchestration) or [create a new .NET Aspire project](../get-started/build-your-first-aspire-app.md), the _YourAppName.ServiceDefaults.csproj_ project is added to your solution. For example, when building an API, you call the `AddServiceDefaults` method in the _:::no-loc text="Program.cs":::_ file of your apps:

```csharp
builder.AddServiceDefaults();
```

The `AddServiceDefaults` method handles the following tasks:

- Configures OpenTelemetry metrics and tracing.
- Adds default health check endpoints.
- Adds service discovery functionality.
- Configures <xref:System.Net.Http.HttpClient> to work with service discovery.

For more information, see [Provided extension methods](#provided-extension-methods) for details on the `AddServiceDefaults` method.

> [!IMPORTANT]
> The .NET Aspire service defaults project is specifically designed for sharing the _Extensions.cs_ file and its functionality. Don't include other shared functionality or models in this project. Use a conventional shared class library project for those purposes.

## Project characteristics

The _YourAppName.ServiceDefaults_ project is a .NET 9.0 library that contains the following XML:

:::code language="xml" source="snippets/template/YourAppName/YourAppName.ServiceDefaults.csproj" highlight="11":::

The service defaults project template imposes a `FrameworkReference` dependency on `Microsoft.AspNetCore.App`.

> [!TIP]
> If you don't want to take a dependency on `Microsoft.AspNetCore.App`, you can create a custom service defaults project. For more information, see [Custom service defaults](#custom-service-defaults).

The `IsAspireSharedProject` property is set to `true`, which indicates that this project is a shared project. The .NET Aspire tooling uses this project as a reference for other projects added to a .NET Aspire solution. When you enlist the new project for orchestration, it automatically references the _YourAppName.ServiceDefaults_ project and updates the _:::no-loc text="Program.cs":::_ file to call the `AddServiceDefaults` method.

## Provided extension methods

The _YourAppName.ServiceDefaults_ project exposes a single _Extensions.cs_ file that contains several opinionated extension methods:

- `AddServiceDefaults`: Adds service defaults functionality.
- `ConfigureOpenTelemetry`: Configures OpenTelemetry metrics and tracing.
- `AddDefaultHealthChecks`: Adds default health check endpoints.
- `MapDefaultEndpoints`: Maps the health checks endpoint to `/health` and the liveness endpoint to `/alive`.

### Add service defaults functionality

The `AddServiceDefaults` method defines default configurations with the following opinionated functionality:

:::code source="snippets/template/YourAppName/Extensions.cs" id="addservicedefaults":::

The preceding code:

- Configures OpenTelemetry metrics and tracing, by calling the `ConfigureOpenTelemetry` method.
- Adds default health check endpoints, by calling the `AddDefaultHealthChecks` method.
- Adds [service discovery](../service-discovery/overview.md) functionality, by calling the `AddServiceDiscovery` method.
- Configures <xref:System.Net.Http.HttpClient> defaults, by calling the `ConfigureHttpClientDefaults` method—which is based on [Build resilient HTTP apps: Key development patterns](/dotnet/core/resilience/http-resilience):
  - Adds the standard HTTP resilience handler, by calling the `AddStandardResilienceHandler` method.
  - Specifies that the <xref:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder> should use service discovery, by calling the `UseServiceDiscovery` method.
- Returns the `IHostApplicationBuilder` instance to allow for method chaining.

### OpenTelemetry configuration

Telemetry is a critical part of any cloud-native application. .NET Aspire provides a set of opinionated defaults for OpenTelemetry, which are configured with the `ConfigureOpenTelemetry` method:

:::code source="snippets/template/YourAppName/Extensions.cs" id="configureotel":::

The `ConfigureOpenTelemetry` method:

- Adds [.NET Aspire telemetry](telemetry.md) logging to include formatted messages and scopes.
- Adds OpenTelemetry metrics and tracing that include:
  - Runtime instrumentation metrics.
  - ASP.NET Core instrumentation metrics.
  - HttpClient instrumentation metrics.
  - In a development environment, the `AlwaysOnSampler` is used to view all traces.
  - Tracing details for ASP.NET Core, gRPC and HTTP instrumentation.
- Adds OpenTelemetry exporters, by calling `AddOpenTelemetryExporters`.

The `AddOpenTelemetryExporters` method is defined privately as follows:

:::code source="snippets/template/YourAppName/Extensions.cs" id="addotelexporters":::

The `AddOpenTelemetryExporters` method adds OpenTelemetry exporters based on the following conditions:

- If the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is set, the OpenTelemetry exporter is added.
- Optionally consumers of .NET Aspire service defaults can uncomment some code to enable the Prometheus exporter, or the Azure Monitor exporter.

For more information, see [.NET Aspire telemetry](telemetry.md).

### Health checks configuration

Health checks are used by various tools and systems to assess the readiness of your app. .NET Aspire provides a set of opinionated defaults for health checks, which are configured with the `AddDefaultHealthChecks` method:

:::code source="snippets/template/YourAppName/Extensions.cs" id="addhealthchecks":::

The `AddDefaultHealthChecks` method adds a default liveness check to ensure the app is responsive. The call to <xref:Microsoft.Extensions.DependencyInjection.HealthCheckServiceCollectionExtensions.AddHealthChecks%2A> registers the <xref:Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>. For more information, see [.NET Aspire health checks](health-checks.md).

#### Web app health checks configuration

To expose health checks in a web app, .NET Aspire automatically determines the type of project being referenced within the solution, and adds the appropriate call to `MapDefaultEndpoints`:

:::code source="snippets/template/YourAppName/Extensions.cs" id="mapdefaultendpoints":::

The `MapDefaultEndpoints` method:

- Allows consumers to optionally uncomment some code to enable the Prometheus endpoint.
- Maps the health checks endpoint to `/health`.
- Maps the liveness endpoint to `/alive` route where the health check tag contains `live`.

For more information, see [.NET Aspire health checks](health-checks.md).

## Custom service defaults

If the default service configuration provided by the project template is not sufficient for your needs, you have the option to create your own service defaults project. This is especially useful when your consuming project, such as a Worker project or WinForms project, cannot or does not want to have a `FrameworkReference` dependency on `Microsoft.AspNetCore.App`.

To do this, create a new .NET 9.0 class library project and add the necessary dependencies to the project file, consider the following example:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
  </ItemGroup>
</Project>
```

Then create an extensions class that contains the necessary methods to configure the app defaults:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class AppDefaultsExtensions
{
    public static IHostApplicationBuilder AddAppDefaults(
        this IHostApplicationBuilder builder)
    {
        builder.ConfigureAppOpenTelemetry();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureAppOpenTelemetry(
        this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(static metrics =>
            {
                metrics.AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddGrpcClientInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(
        this IHostApplicationBuilder builder)
    {
        var useOtlpExporter =
            !string.IsNullOrWhiteSpace(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(
                logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(
                metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(
                tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }
}
```

This is only an example, and you can customize the `AppDefaultsExtensions` class to meet your specific needs.

## Next steps

This code is derived from the .NET Aspire Starter Application template and is intended as a starting point. You're free to modify this code however you deem necessary to meet your needs. It's important to know that service defaults project and its functionality are automatically applied to all project resources in a .NET Aspire solution.

- [Service discovery in .NET Aspire](../service-discovery/overview.md)
- [.NET Aspire SDK](dotnet-aspire-sdk.md)
- [.NET Aspire templates](aspire-sdk-templates.md)
- [Health checks in .NET Aspire](health-checks.md)
- [.NET Aspire telemetry](telemetry.md)
- [Build resilient HTTP apps: Key development patterns](/dotnet/core/resilience/http-resilience)

----------------------
----------------------
----------------------
----------------------

---
title: .NET Aspire and launch profiles
description: Learn how .NET Aspire integrates with .NET launch profiles.
ms.date: 04/23/2024
---

# .NET Aspire and launch profiles

.NET Aspire makes use of _launch profiles_ defined in both the app host and service projects to simplify the process of configuring multiple aspects of the debugging and publishing experience for .NET Aspire-based distributed applications.

## Launch profile basics

When creating a new .NET application from a template developers will often see a `Properties` directory which contains a file named _launchSettings.json_. The launch settings file contains a list of _launch profiles_. Each launch profile is a collection of related options which defines how you would like `dotnet` to start your application.

The code below is an example of launch profiles in a _launchSettings.json_ file for an **ASP.NET Core** application.

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5130",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7106;http://localhost:5130",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

The _launchSettings.json_ file above defines two _launch profiles_, `http` and `https`. Each has its own set of environment variables, launch URLs and other options. When launching a .NET Core application developers can choose which launch profile to use.

```dotnetcli
dotnet run --launch-profile https
```

If no launch profile is specified, then the first launch profile is selected by default. It is possible to launch a .NET Core application without a launch profile using the `--no-launch-profile` option. Some fields from the _launchSettings.json_ file are translated to environment variables. For example, the `applicationUrl` field is converted to the `ASPNETCORE_URLS` environment variable which controls which address and port ASP.NET Core binds to.

In Visual Studio it's possible to select the launch profile when launching the application making it easy to switch between configuration scenarios when manually debugging issues:

:::image type="content" loc-scope="visual-studio" source="./media/launch-profiles/vs-launch-profile-toolbar.png" lightbox="./media/launch-profiles/vs-launch-profile-toolbar.png" alt-text="Screenshot of the standard toolbar in Visual Studio with the launch profile selector highlighted.":::

When a .NET application is launched with a launch profile a special environment variable called `DOTNET_LAUNCH_PROFILE` is populated with the name of the launch profile that was used when launching the process.

## Launch profiles for .NET Aspire app host

In .NET Aspire, the AppHost is just a .NET application. As a result it has a `launchSettings.json` file just like any other application. Here is an example of the `launchSettings.json` file generated when creating a new .NET Aspire project from the starter template (`dotnet new aspire-starter`).

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:17134;http://localhost:15170",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21030",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22057"
      }
    },
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:15170",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19240",
        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "http://localhost:20154"
      }
    }
  }
}
```

The .NET Aspire templates have a very similar set of _launch profiles_ to a regular ASP.NET Core application. When the .NET Aspire app project launches, it starts a <xref:Aspire.Hosting.DistributedApplication> and hosts a web-server which is used by the .NET Aspire Dashboard to fetch information about resources which are being orchestrated by .NET Aspire.

For information about app host configuration options, see [.NET Aspire app host configuration](../app-host/configuration.md).

## Relationship between app host launch profiles and service projects

In .NET Aspire the app host is responsible for coordinating the launch of multiple service projects. When you run the app host either via the command line or from Visual Studio (or other development environment) a launch profile is selected for the app host. In turn, the app host will attempt to find a matching launch profile in the service projects it is launching and use those options to control the environment and default networking configuration for the service project.

When the app host launches a service project it doesn't simply launch the service project using the `--launch-profile` option. Therefore, there will be no `DOTNET_LAUNCH_PROFILE` environment variable set for service projects. This is because .NET Aspire modifies the `ASPNETCORE_URLS` environment variable (derived from the `applicationUrl` field in the launch profile) to use a different port. By default, .NET Aspire inserts a reverse proxy in front of the ASP.NET Core application to allow for multiple instances of the application using the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.WithReplicas%2A> method.

Other settings such as options from the `environmentVariables` field are passed through to the application without modification.

## Control launch profile selection

Ideally, it's possible to align the launch profile names between the app host and the service projects to make it easy to switch between configuration options on all projects coordinated by the app host at once. However, it may be desirable to control launch profile that a specific project uses. The <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> extension method provides a mechanism to do this.

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.InventoryService>(
    "inventoryservice",
    launchProfileName: "mylaunchprofile");
```

The preceding code shows that the `inventoryservice` resource (a .NET project) is launched using the options from the `mylaunchprofile` launch profile. The launch profile precedence logic is as follows:

1. Use the launch profile specified by `launchProfileName` argument if specified.
2. Use the launch profile with the same name as the AppHost (determined by reading the `DOTNET_LAUNCH_PROFILE` environment variable).
3. Use the default (first) launch profile in _launchSettings.json_.
4. Don't use a launch profile.

To force a service project to launch without a launch profile the `launchProfileName` argument on the <xref:Aspire.Hosting.ProjectResourceBuilderExtensions.AddProject%2A> method can be set to null.

## Launch profiles and endpoints

When adding an ASP.NET Core project to the app host, .NET Aspire will parse the _launchSettings.json_ file selecting the appropriate launch profile and automatically generate endpoints in the application model based on the URL(s) present in the `applicationUrl` field. To modify the endpoints that are automatically injected the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithEndpoint%2A> extension method.

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.InventoryService>("inventoryservice")
       .WithEndpoint("https", endpoint => endpoint.IsProxied = false);
```

The preceding code shows how to disable the reverse proxy that .NET Aspire deploys in front for the .NET Core application and instead allows the .NET Core application to respond directly on requests over HTTP(S). For more information on networking options within .NET Aspire see [.NET Aspire inner loop networking overview](./networking-overview.md).

## See also

- [Kestrel configured endpoints](networking-overview.md#kestrel-configured-endpoints)

---------------------
---------------------
---------------------
---------------------

---
title: .NET Aspire health checks
description: Explore .NET Aspire health checks
ms.date: 09/24/2024
ms.topic: quickstart
uid: dotnet/aspire/health-checks
---

# Health checks in .NET Aspire

Health checks provide availability and state information about an app. Health checks are often exposed as HTTP endpoints, but can also be used internally by the app to write logs or perform other tasks based on the current health. Health checks are typically used in combination with an external monitoring service or container orchestrator to check the status of an app. The data reported by health checks can be used for various scenarios:

- Influence decisions made by container orchestrators, load balancers, API gateways, and other management services. For instance, if the health check for a containerized app fails, it might be skipped by a load balancer routing traffic.
- Verify that underlying dependencies are available, such as a database or cache, and return an appropriate status message.
- Trigger alerts or notifications when an app isn't responding as expected.

## .NET Aspire health check endpoints

.NET Aspire exposes two default health check HTTP endpoints in **Development** environments when the `AddServiceDefaults` and `MapDefaultEndpoints` methods are called from the _:::no-loc text="Program.cs":::_ file:

- The `/health` endpoint indicates if the app is running normally where it's ready to receive requests. All health checks must pass for app to be considered ready to accept traffic after starting.

    ```http
    GET /health
    ```

    The `/health` endpoint returns an HTTP status code 200 and a `text/plain` value of <xref:Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy> when the app is _healthy_.

- The `/alive` indicates if an app is running or has crashed and must be restarted. Only health checks tagged with the _live_ tag must pass for app to be considered alive.

    ```http
    GET /alive
    ```

    The `/alive` endpoint returns an HTTP status code 200 and a `text/plain` value of <xref:Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy> when the app is _alive_.

The `AddServiceDefaults` and `MapDefaultEndpoints` methods also apply various configurations to your app beyond just health checks, such as [OpenTelemetry](telemetry.md) and [service discovery](../service-discovery/overview.md) configurations.

### Non-development environments

In non-development environments, the `/health` and `/alive` endpoints are disabled by default. If you need to enable them, its recommended to protect these endpoints with various routing features, such as host filtering and/or authorization. For more information, see [Health checks in ASP.NET Core](/aspnet/core/host-and-deploy/health-checks#use-health-checks-routing).

Additionally, it may be advantageous to configure request timeouts and output caching for these endpoints to prevent abuse or denial-of-service attacks. To do so, consider the following modified `AddDefaultHealthChecks` method:

:::code language="csharp" source="snippets/healthz/Healthz.ServiceDefaults/Extensions.cs" id="healthchecks":::

The preceding code:

- Adds a timeout of 5 seconds to the health check requests with a policy named `HealthChecks`.
- Adds a 10-second cache to the health check responses with a policy named `HealthChecks`.

Now consider the updated `MapDefaultEndpoints` method:

:::code language="csharp" source="snippets/healthz/Healthz.ServiceDefaults/Extensions.cs" id="mapendpoints":::

The preceding code:

- Groups the health check endpoints under the `/` path.
- Caches the output and specifies a request time with the corresponding `HealthChecks` policy.

In addition to the updated `AddDefaultHealthChecks` and `MapDefaultEndpoints` methods, you must also add the corresponding services for both request timeouts and output caching.

In the appropriate consuming app's entry point (usually the _:::no-loc text="Program.cs":::_ file), add the following code:

```csharp
// Wherever your services are being registered.
// Before the call to Build().
builder.Services.AddRequestTimeouts();
builder.Services.AddOutputCache();

var app = builder.Build();

// Wherever your app has been built, before the call to Run().
app.UseRequestTimeouts();
app.UseOutputCache();

app.Run();
```

For more information, see [Request timeouts middleware in ASP.NET Core](/aspnet/core/performance/timeouts) and [Output caching middleware in ASP.NET Core](/aspnet/core/performance/caching/output).

## Integration health checks

.NET Aspire integrations can also register additional health checks for your app. These health checks contribute to the returned status of the `/health` and `/alive` endpoints. For example, the .NET Aspire PostgreSQL integration automatically adds a health check to verify the following conditions:

- A database connection could be established
- A database query could be executed successfully

If either of these operations fail, the corresponding health check also fails.

### Configure health checks

You can disable health checks for a given integration using one of the available configuration options. .NET Aspire integrations support [Microsoft.Extensions.Configurations](/dotnet/api/microsoft.extensions.configuration) to apply settings through config files such as _:::no-loc text="appsettings.json":::_:

```json
{
  "Aspire": {
    "Npgsql": {
      "DisableHealthChecks": true,
    }
  }
}
```

You can also use an inline delegate to configure health checks:

```csharp
builder.AddNpgsqlDbContext<MyDbContext>(
    "postgresdb",
    static settings => settings.DisableHealthChecks  = true);
```

## See also

- [.NET app health checks in C#](/dotnet/core/diagnostics/diagnostic-health-checks)
- [Health checks in ASP.NET Core](/aspnet/core/host-and-deploy/health-checks)

-------------------------
-------------------------
-------------------------
-------------------------

---
title: .NET Aspire telemetry
description: Learn about essential telemetry concepts for .NET Aspire.
ms.date: 12/08/2023
---

# .NET Aspire telemetry

One of the primary objectives of .NET Aspire is to ensure that apps are straightforward to debug and diagnose. .NET Aspire integrations automatically set up Logging, Tracing, and Metrics configurations, which are sometimes known as the pillars of observability, using the [.NET OpenTelemetry SDK](https://github.com/open-telemetry/opentelemetry-dotnet).

- **[Logging](/dotnet/core/diagnostics/logging-tracing)**: Log events describe what's happening as an app runs. A baseline set is enabled for .NET Aspire integrations by default and more extensive logging can be enabled on-demand to diagnose particular problems.

- **[Tracing](/dotnet/core/diagnostics/distributed-tracing)**: Traces correlate log events that are part of the same logical activity (e.g. the handling of a single request), even if they're spread across multiple machines or processes.

- **[Metrics](/dotnet/core/diagnostics/metrics)**: Metrics expose the performance and health characteristics of an app as simple numerical values. As a result, they have low performance overhead and many services configure them as always-on telemetry. This also makes them suitable for triggering alerts when potential problems are detected.

Together, these types of telemetry allow you to gain insights into your application's behavior and performance using various monitoring and analysis tools. Depending on the backing service, some integrations may only support some of these features.

## .NET Aspire OpenTelemetry integration

The [.NET OpenTelemetry SDK](https://github.com/open-telemetry/opentelemetry-dotnet) includes features for gathering data from several .NET APIs, including <xref:Microsoft.Extensions.Logging.ILogger>, <xref:System.Activities.Activity>, <xref:System.Diagnostics.Metrics.Meter>, and <xref:System.Diagnostics.Metrics.Instrument%601>. These APIs correspond to telemetry features like logging, tracing, and metrics. .NET Aspire projects define OpenTelemetry SDK configurations in the _ServiceDefaults_ project. For more information, see [.NET Aspire service defaults](service-defaults.md).

By default, the `ConfigureOpenTelemetry` method enables logging, tracing, and metrics for the app. It also adds exporters for these data points so they can be collected by other monitoring tools.

## Export OpenTelemetry data for monitoring

The .NET OpenTelemetry SDK facilitates the export of this telemetry data to a data store or reporting tool. The telemetry export mechanism relies on the [OpenTelemetry protocol (OTLP)](https://opentelemetry.io/docs/specs/otel/protocol), which serves as a standardized approach for transmitting telemetry data through REST or gRPC. The `ConfigureOpenTelemetry` method also registers exporters to provide your telemetry data to other monitoring tools, such as Prometheus or Azure Monitor. For more information, see [OpenTelemetry configuration](service-defaults.md#opentelemetry-configuration).

## OpenTelemetry environment variables

OpenTelemetry has a [list of known environment variables](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/) that configure the most important behavior for collecting and exporting telemetry. OpenTelemetry SDKs, including the .NET SDK, support reading these variables.

.NET Aspire projects launch with environment variables that configure the name and ID of the app in exported telemetry and set the address endpoint of the OTLP server to export data. For example:

- `OTEL_SERVICE_NAME` = myfrontend
- `OTEL_RESOURCE_ATTRIBUTES` = service.instance.id=1a5f9c1e-e5ba-451b-95ee-ced1ee89c168
- `OTEL_EXPORTER_OTLP_ENDPOINT` = `http://localhost:4318`

The environment variables are automatically set in local development.

## .NET Aspire local development

When you create a .NET Aspire project, the .NET Aspire dashboard provides a UI for viewing app telemetry by default. Telemetry data is sent to the dashboard using OTLP, and the dashboard implements an OTLP server to receive telemetry data and store it in memory. The .NET Aspire debugging workflow is as follows:

- Developer starts the .NET Aspire project with debugging, presses <kbd>F5</kbd>.
- .NET Aspire dashboard and developer control plane (DCP) start.
- App configuration is run in the _AppHost_ project.
  - OpenTelemetry environment variables are automatically added to .NET projects during app configuration.
  - DCP provides the name (`OTEL_SERVICE_NAME`) and ID (`OTEL_RESOURCE_ATTRIBUTES`) of the app in exported telemetry.
  - The OTLP endpoint is an HTTP/2 port started by the dashboard. This endpoint is set in the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable on each project. That tells projects to export telemetry back to the dashboard.
  - Small export intervals (`OTEL_BSP_SCHEDULE_DELAY`, `OTEL_BLRP_SCHEDULE_DELAY`, `OTEL_METRIC_EXPORT_INTERVAL`) so data is quickly available in the dashboard. Small values are used in local development to prioritize dashboard responsiveness over efficiency.
- The DCP starts configured projects, containers, and executables.
- Once started, apps send telemetry to the dashboard.
- Dashboard displays near real-time telemetry of all .NET Aspire projects.

All of these steps happen internally, so in most cases the developer simply needs to run the app to see this process in action.

## .NET Aspire deployment

.NET Aspire deployment environments should configure OpenTelemetry environment variables that make sense for their environment. For example, `OTEL_EXPORTER_OTLP_ENDPOINT` should be configured to the environment's local OTLP collector or monitoring service.

.NET Aspire telemetry works best in environments that support OTLP. OTLP exporting is disabled if `OTEL_EXPORTER_OTLP_ENDPOINT` isn't configured.

For more information, see [.NET Aspire deployments](../deployment/overview.md).

-------------------------
-------------------------
-------------------------
-------------------------

---
title: .NET Aspire integrations overview
description: Explore the fundamental concepts of .NET Aspire integrations and learn how to integrate them into your apps.
ms.date: 02/06/2025
ms.topic: conceptual
uid: dotnet/aspire/integrations
---

# .NET Aspire integrations overview

.NET Aspire integrations are a curated suite of NuGet packages selected to facilitate the integration of cloud-native applications with prominent services and platforms, such as Redis and PostgreSQL. Each integration furnishes essential cloud-native functionalities through either automatic provisioning or standardized configuration patterns.

> [!TIP]
> Always strive to use the latest version of .NET Aspire integrations to take advantage of the latest features, improvements, and security updates.

## Integration responsibilities

Most .NET Aspire integrations are made up of two separate libraries, each with a different responsibility. One type represents resources within the [_app host_](app-host-overview.md) project—known as [hosting integrations](#hosting-integrations). The other type of integration represents client libraries that connect to the resources modeled by hosting integrations, and they're known as [client integrations](#client-integrations).

### Hosting integrations

Hosting integrations configure applications by provisioning resources (like containers or cloud resources) or pointing to existing instances (such as a local SQL server). These packages model various services, platforms, or capabilities, including caches, databases, logging, storage, and messaging systems.

Hosting integrations extend the <xref:Aspire.Hosting.IDistributedApplicationBuilder> interface, enabling the _app host_ project to express resources within its [_app model_](app-host-overview.md#terminology). The official [hosting integration NuGet packages](https://www.nuget.org/packages?q=owner%3A+aspire+tags%3A+aspire+hosting+integration&includeComputedFrameworks=true&prerel=true&sortby=relevance) are tagged with `aspire`, `integration`, and `hosting`. In addition to the official hosting integrations, the [community has created hosting integrations](../community-toolkit/overview.md) for various services and platforms as part of the Community Toolkit.

For information on creating a custom _hosting integration_, see [Create custom .NET Aspire hosting integration](../extensibility/custom-hosting-integration.md).

### Client integrations

Client integrations wire up client libraries to [dependency injection (DI)](/dotnet/core/extensions/dependency-injection), define configuration schema, and add [health checks](health-checks.md), [resiliency](/dotnet/core/resilience), and [telemetry](telemetry.md) where applicable. .NET Aspire client integration libraries are prefixed with `Aspire.` and then include the full package name that they integrate with, such as `Aspire.StackExchange.Redis`.

These packages configure existing client libraries to connect to hosting integrations. They extend the <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> interface allowing client-consuming projects, such as your web app or API, to use the connected resource. The official [client integration NuGet packages](https://www.nuget.org/packages?q=owner%3A+aspire+tags%3A+aspire+client+integration&includeComputedFrameworks=true&prerel=true&sortby=relevance) are tagged with `aspire`, `integration`, and `client`. In addition to the official client integrations, the [community has created client integrations](../community-toolkit/overview.md) for various services and platforms as part of the Community Toolkit.

For more information on creating a custom client integration, see [Create custom .NET Aspire client integrations](../extensibility/custom-client-integration.md).

### Relationship between hosting and client integrations

Hosting and client integrations are best when used together, but are **not** coupled and can be used separately. Some hosting integrations don't have a corresponding client integration. Configuration is what makes the hosting integration work with the client integration.

Consider the following diagram that depicts the relationship between hosting and client integrations:

:::image type="content" source="media/integrations-thumb.png" lightbox="media/integrations.png" alt-text="A diagram ":::

The app host project is where hosting integrations are used. Configuration, specifically environment variables, is injected into projects, executables, and containers, allowing client integrations to connect to the hosting integrations.

## Integration features

When you add a client integration to a project within your .NET Aspire solution, [service defaults](service-defaults.md) are automatically applied to that project; meaning the Service Defaults project is referenced and the `AddServiceDefaults` extension method is called. These defaults are designed to work well in most scenarios and can be customized as needed. The following service defaults are applied:

- **Observability and telemetry**: Automatically sets up logging, tracing, and metrics configurations:

  - **[Logging](/dotnet/core/diagnostics/logging-tracing)**: A technique where code is instrumented to produce logs of interesting events that occurred while the program was running.
  - **[Tracing](/dotnet/core/diagnostics/distributed-tracing)**: A specialized form of logging that helps you localize failures and performance issues within applications distributed across multiple machines or processes.
  - **[Metrics](/dotnet/core/diagnostics/metrics)**: Numerical measurements recorded over time to monitor application performance and health. Metrics are often used to generate alerts when potential problems are detected.

- **[Health checks](health-checks.md)**: Exposes HTTP endpoints to provide basic availability and state information about an app. Health checks are used to influence decisions made by container orchestrators, load balancers, API gateways, and other management services.
- **[Resiliency](/dotnet/core/resilience/http-resilience)**: The ability of your system to react to failure and still remain functional. Resiliency extends beyond preventing failures to include recovering and reconstructing your cloud-native environment back to a healthy state.

## Versioning considerations

Hosting and client integrations are updated each release to target the latest stable versions of dependent resources. When container images are updated with new image versions, the hosting integrations update to these new versions. Similarly, when a new NuGet version is available for a dependent client library, the corresponding client integration updates to the new version. This ensures the latest features and security updates are available to applications. The .NET Aspire update type (major, minor, patch) doesn't necessarily indicate the type of update in dependent resources. For example, a new major version of a dependent resource may be updated in a .NET Aspire patch release, if necessary.

When major breaking changes happen in dependent resources, integrations may temporarily split into version-dependent packages to ease updating across the breaking change. For more information, see the [first example of such a breaking change](https://github.com/dotnet/aspire/issues/3956).

## Official integrations

.NET Aspire provides many integrations to help you build cloud-native applications. These integrations are designed to work seamlessly with the .NET Aspire app host and client libraries. The following sections detail cloud-agnostic, Azure-specific, Amazon Web Services (AWS), and Community Toolkit integrations.

### Cloud-agnostic integrations

The following section details cloud-agnostic .NET Aspire integrations with links to their respective docs and NuGet packages, and provides a brief description of each integration.

<!-- markdownlint-disable MD033 MD045 -->
| Integration docs and NuGet packages | Description |
|--|--|
| - **Learn more**: [📄 Apache Kafka](../messaging/kafka-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Kafka](https://www.nuget.org/packages/Aspire.Hosting.Kafka)<br>- **Client**: [📦 Aspire.Confluent.Kafka](https://www.nuget.org/packages/Aspire.Confluent.Kafka) | A library for producing and consuming messages from an [Apache Kafka](https://kafka.apache.org/) broker. |
| - **Learn more**: [📄 Dapr](../frameworks/dapr.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Dapr](https://www.nuget.org/packages/Aspire.Hosting.Dapr)<br>- **Client**: N/A | A library for modeling [Dapr](https://dapr.io/) as a .NET Aspire resource. |
| - **Learn more**: [📄 Elasticsearch](../search/elasticsearch-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Elasticsearch](https://www.nuget.org/packages/Aspire.Hosting.Elasticsearch)<br>- **Client**: [📦 Aspire.Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Aspire.Elastic.Clients.Elasticsearch) | A library for accessing [Elasticsearch](https://www.elastic.co/guide/en/elasticsearch/client/index.html) databases. |
| - **Learn more**: [📄 Keycloak](../authentication/keycloak-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Keycloak](https://www.nuget.org/packages/Aspire.Hosting.Keycloak)<br>- **Client**: [📦 Aspire.Keycloak.Authentication](https://www.nuget.org/packages/Aspire.Keycloak.Authentication) | A library for accessing [Keycloak](https://www.keycloak.org/docs/latest/server_admin/index.html) authentication. |
| - **Learn more**: [📄 Milvus](../database/milvus-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Milvus](https://www.nuget.org/packages/Aspire.Hosting.Milvus)<br>- **Client**: [📦 Aspire.Milvus.Client](https://www.nuget.org/packages/Aspire.Milvus.Client) | A library for accessing [Milvus](https://milvus.io/) databases. |
| - **Learn more**: [📄 MongoDB Driver](../database/mongodb-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.MongoDB](https://www.nuget.org/packages/Aspire.Hosting.MongoDB)<br>- **Client**: [📦 Aspire.MongoDB.Driver](https://www.nuget.org/packages/Aspire.MongoDB.Driver) | A library for accessing [MongoDB](https://www.mongodb.com/docs) databases. |
| - **Learn more**: [📄 MySqlConnector](../database/mysql-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.MySql](https://www.nuget.org/packages/Aspire.Hosting.MySql)<br>- **Client**: [📦 Aspire.MySqlConnector](https://www.nuget.org/packages/Aspire.MySqlConnector) | A library for accessing [MySqlConnector](https://mysqlconnector.net/) databases. |
| - **Learn more**: [📄 NATS](../messaging/nats-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Nats](https://www.nuget.org/packages/Aspire.Hosting.Nats)<br>- **Client**: [📦 Aspire.NATS.Net](https://www.nuget.org/packages/Aspire.NATS.Net) | A library for accessing [NATS](https://nats.io/) messaging. |
| - **Learn more**: [📄 Oracle - EF Core](../database/oracle-entity-framework-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Oracle](https://www.nuget.org/packages/Aspire.Hosting.Oracle)<br>- **Client**: [📦 Aspire.Oracle.EntityFrameworkCore](https://www.nuget.org/packages/Aspire.Oracle.EntityFrameworkCore) | A library for accessing Oracle databases with [Entity Framework Core](/ef/core). |
| - **Learn more**: [📄 Orleans](../frameworks/Orleans.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Orleans](https://www.nuget.org/packages/Aspire.Hosting.Orleans)<br>- **Client**: N/A | A library for modeling [Orleans](/dotnet/Orleans) as a .NET Aspire resource. |
| - **Learn more**: [📄 Pomelo MySQL - EF Core](../database/mysql-entity-framework-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.MySql](https://www.nuget.org/packages/Aspire.Hosting.MySql)<br>- **Client**: [📦 Aspire.Pomelo.EntityFrameworkCore.MySql](https://www.nuget.org/packages/Aspire.Pomelo.EntityFrameworkCore.MySql) | A library for accessing MySql databases with [Entity Framework Core](/ef/core). |
| - **Learn more**: [📄 PostgreSQL - EF Core](../database/postgresql-entity-framework-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.PostgreSQL](https://www.nuget.org/packages/Aspire.Hosting.PostgreSQL)<br>- **Client**: [📦 Aspire.Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Aspire.Npgsql.EntityFrameworkCore.PostgreSQL) | A library for accessing PostgreSQL databases using [Entity Framework Core](https://www.npgsql.org/efcore/index.html). |
| - **Learn more**: [📄 PostgreSQL](../database/postgresql-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.PostgreSQL](https://www.nuget.org/packages/Aspire.Hosting.PostgreSQL)<br>- **Client**: [📦 Aspire.Npgsql](https://www.nuget.org/packages/Aspire.Npgsql) | A library for accessing [PostgreSQL](https://www.npgsql.org/doc/index.html) databases. |
| - **Learn more**: [📄 Qdrant](../database/qdrant-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Qdrant](https://www.nuget.org/packages/Aspire.Hosting.Qdrant)<br>- **Client**: [📦 Aspire.Qdrant.Client](https://www.nuget.org/packages/Aspire.Qdrant.Client) | A library for accessing [Qdrant](https://qdrant.tech/) databases. |
|  - **Learn more**: [📄 RabbitMQ](../messaging/rabbitmq-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.RabbitMQ](https://www.nuget.org/packages/Aspire.Hosting.RabbitMQ)<br>- **Client**: [📦 Aspire.RabbitMQ.Client](https://www.nuget.org/packages/Aspire.RabbitMQ.Client) | A library for accessing [RabbitMQ](https://www.rabbitmq.com/dotnet.html). |
| - **Learn more**: [📄 Redis Distributed Caching](../caching/stackexchange-redis-distributed-caching-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Redis](https://www.nuget.org/packages/Aspire.Hosting.Redis), [📦 Aspire.Hosting.Garnet](https://www.nuget.org/packages/Aspire.Hosting.Garnet), or [📦 Aspire.Hosting.Valkey](https://www.nuget.org/packages/Aspire.Hosting.Valkey)<br>- **Client**: [📦 Aspire.StackExchange.Redis.DistributedCaching](https://www.nuget.org/packages/Aspire.StackExchange.Redis.DistributedCaching) | A library for accessing [Redis](https://stackexchange.github.io/StackExchange.Redis/) caches for [distributed caching](/aspnet/core/performance/caching/distributed). |
| - **Learn more**: [📄 Redis Output Caching](../caching/stackexchange-redis-output-caching-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Redis](https://www.nuget.org/packages/Aspire.Hosting.Redis), [📦 Aspire.Hosting.Garnet](https://www.nuget.org/packages/Aspire.Hosting.Garnet), or [📦 Aspire.Hosting.Valkey](https://www.nuget.org/packages/Aspire.Hosting.Valkey)<br>- **Client**: [📦 Aspire.StackExchange.Redis.OutputCaching](https://www.nuget.org/packages/Aspire.StackExchange.Redis.OutputCaching) | A library for accessing [Redis](https://stackexchange.github.io/StackExchange.Redis/) caches for [output caching](/aspnet/core/performance/caching/output). |
| - **Learn more**: [📄 Redis](../caching/stackexchange-redis-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Redis](https://www.nuget.org/packages/Aspire.Hosting.Redis), [📦 Aspire.Hosting.Garnet](https://www.nuget.org/packages/Aspire.Hosting.Garnet), or [📦 Aspire.Hosting.Valkey](https://www.nuget.org/packages/Aspire.Hosting.Valkey)<br>- **Client**: [📦 Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis) | A library for accessing [Redis](https://stackexchange.github.io/StackExchange.Redis/) caches. |
| - **Learn more**: [📄 Seq](../logging/seq-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Seq](https://www.nuget.org/packages/Aspire.Hosting.Seq)<br>- **Client**: [📦 Aspire.Seq](https://www.nuget.org/packages/Aspire.Seq) | A library for logging to [Seq](https://datalust.co/seq). |
| - **Learn more**: [📄 SQL Server - EF Core](../database/sql-server-entity-framework-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.SqlServer](https://www.nuget.org/packages/Aspire.Hosting.SqlServer)<br>- **Client**: [📦 Aspire.Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Aspire.Microsoft.EntityFrameworkCore.SqlServer) | A library for accessing [SQL Server databases using EF Core](/ef/core/providers/sql-server/). |
| - **Learn more**: [📄 SQL Server](../database/sql-server-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.SqlServer](https://www.nuget.org/packages/Aspire.Hosting.SqlServer)<br>- **Client**: [📦 Aspire.Microsoft.Data.SqlClient](https://www.nuget.org/packages/Aspire.Microsoft.Data.SqlClient) | A library for accessing [SQL Server](/sql/sql-server/) databases. |
<!-- markdownlint-enable MD033 MD045 -->

For more information on working with .NET Aspire integrations in Visual Studio, see [Visual Studio tooling](setup-tooling.md#visual-studio-tooling).

### Azure integrations

Azure integrations configure applications to use Azure resources. These hosting integrations are available in the `Aspire.Hosting.Azure.*` NuGet packages, while their client integrations are available in the `Aspire.*` NuGet packages:

<!-- markdownlint-disable MD033 MD045 -->
| Integration | Docs and NuGet packages | Description |
|--|--|--|
| <img src="media/icons/AzureAppConfig_256x.png" alt="Azure App Configuration logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure App Configuration](https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Azure.AppConfiguration/README.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.AppConfiguration](https://www.nuget.org/packages/Aspire.Hosting.Azure.AppConfiguration)<br>- **Client**: N/A | A library for interacting with [Azure App Configuration](/azure/azure-app-configuration/). |
| <img src="media/icons/AzureAppInsights_256x.png" alt="Azure Application Insights logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Application Insights](https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Azure.ApplicationInsights/README.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.ApplicationInsights](https://www.nuget.org/packages/Aspire.Hosting.Azure.ApplicationInsights)<br>- **Client**: N/A | A library for interacting with [Azure Application Insights](/azure/azure-monitor/app/app-insights-overview). |
| <img src="media/icons/AzureCacheRedis_256x.png" alt="Azure Cache for Redis logo" role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Cache for Redis](../caching/azure-cache-for-redis-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Redis](https://www.nuget.org/packages/Aspire.Hosting.Azure.Redis)<br>- **Client**: [📦 Aspire.StackExchange.Redis](https://www.nuget.org/packages/Aspire.StackExchange.Redis) or [📦 Aspire.StackExchange.Redis.DistributedCaching](https://www.nuget.org/packages/Aspire.StackExchange.Redis.DistributedCaching) or [📦 Aspire.StackExchange.Redis.OutputCaching](https://www.nuget.org/packages/Aspire.StackExchange.Redis.OutputCaching) | A library for accessing [Azure Cache for Redis](/azure/azure-cache-for-redis/). |
| <img src="media/icons/AzureCosmosDB_256x.png" alt="Azure Cosmos DB EF logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Cosmos DB - EF Core](../database/azure-cosmos-db-entity-framework-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.CosmosDB](https://www.nuget.org/packages/Aspire.Hosting.Azure.CosmosDB)<br>- **Client**: [📦 Aspire.Microsoft.EntityFrameworkCore.Cosmos](https://www.nuget.org/packages/Aspire.Microsoft.EntityFrameworkCore.Cosmos) | A library for accessing Azure Cosmos DB databases with [Entity Framework Core](/ef/core/providers/cosmos/). |
| <img src="media/icons/AzureCosmosDB_256x.png" alt="Azure Cosmos DB logo." role="presentation" width="78" data-linktype="relative-path">| - **Learn more**: [📄 Azure Cosmos DB](../database/azure-cosmos-db-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.CosmosDB](https://www.nuget.org/packages/Aspire.Hosting.Azure.CosmosDB)<br>- **Client**: [📦 Aspire.Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Aspire.Microsoft.Azure.Cosmos) | A library for accessing [Azure Cosmos DB](/azure/cosmos-db/introduction) databases. |
| <img src="media/icons/AzureEventHubs_256x.png" alt="Azure Event Hubs logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Event Hubs](../messaging/azure-event-hubs-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.EventHubs](https://www.nuget.org/packages/Aspire.Hosting.Azure.EventHubs)<br>- **Client**: [📦 Aspire.Azure.Messaging.EventHubs](https://www.nuget.org/packages/Aspire.Azure.Messaging.EventHubs) | A library for accessing [Azure Event Hubs](/azure/event-hubs/event-hubs-about). |
| <img src="media/icons/AzureFunctionApps_256x.png" alt="Azure Functions logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Functions](../serverless/functions.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Functions](https://www.nuget.org/packages/Aspire.Hosting.Azure.Functions)<br>- **Client**: N/A | A library for integrating with [Azure Functions](/azure/azure-functions/). |
| <img src="media/icons/AzureKeyVault_256x.png" alt="Azure Key Vault logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Key Vault](../security/azure-security-key-vault-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.KeyVault](https://www.nuget.org/packages/Aspire.Hosting.Azure.KeyVault)<br>- **Client**: [📦 Aspire.Azure.Security.KeyVault](https://www.nuget.org/packages/Aspire.Azure.Security.KeyVault) | A library for accessing [Azure Key Vault](/azure/key-vault/general/overview). |
| <img src="media/icons/AzureLogAnalytics_256x.png" alt="Azure Operational Insights logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Operational Insights](https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Azure.OperationalInsights/README.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.OperationalInsights](https://www.nuget.org/packages/Aspire.Hosting.Azure.OperationalInsights)<br>- **Client**: N/A | A library for interacting with [Azure Operational Insights](/azure/azure-monitor/logs/log-analytics-workspace-overview). |
| <img src="media/icons/AzureOpenAI_256x.png" alt="Azure OpenAI logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure AI OpenAI](../azureai/azureai-openai-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.CognitiveServices](https://www.nuget.org/packages/Aspire.Hosting.Azure.CognitiveServices)<br>- **Client**: [📦 Aspire.Azure.AI.OpenAI](https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI) | A library for accessing [Azure AI OpenAI](/azure/ai-services/openai/overview) or OpenAI functionality. |
| <img src="media/icons/AzurePostgreSQL_256x.png" alt="Azure PostgreSQL logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure PostgreSQL](https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Azure.PostgreSQL/README.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.PostgreSQL](https://www.nuget.org/packages/Aspire.Hosting.Azure.PostgreSQL)<br>- **Client**: N/A | A library for interacting with [Azure Database for PostgreSQL](/azure/postgresql/). |
| <img src="media/icons/AzureSearch_256x.png" alt="Azure AI Search logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure AI Search](../azureai/azureai-search-document-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Search](https://www.nuget.org/packages/Aspire.Hosting.Azure.Search)<br>- **Client**: [📦 Aspire.Azure.Search.Documents](https://www.nuget.org/packages/Aspire.Azure.Search.Documents) | A library for accessing [Azure AI Search](/azure/search/search-what-is-azure-search) functionality. |
| <img src="media/icons/AzureServiceBus_256x.png" alt="Azure Service Bus logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Service Bus](../messaging/azure-service-bus-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.ServiceBus](https://www.nuget.org/packages/Aspire.Hosting.Azure.ServiceBus)<br>- **Client**: [📦 Aspire.Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Aspire.Azure.Messaging.ServiceBus) | A library for accessing [Azure Service Bus](/azure/service-bus-messaging/service-bus-messaging-overview). |
| <img src="media/icons/AzureSignalR_256x.png" alt="Azure SignalR Service logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure SignalR Service](../real-time/azure-signalr-scenario.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.SignalR](https://www.nuget.org/packages/Aspire.Hosting.Azure.SignalR)<br>- **Client**: [Microsoft.Azure.SignalR](https://www.nuget.org/packages/Microsoft.Azure.SignalR) | A library for accessing [Azure SignalR Service](/azure/azure-signalr/signalr-overview). |
| <img src="media/icons/AzureBlobPageStorage_256x.png" alt="Azure Blob Storage logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Blob Storage](../storage/azure-storage-blobs-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage)<br>- **Client**: [📦 Aspire.Azure.Storage.Blobs](https://www.nuget.org/packages/Aspire.Azure.Storage.Blobs) | A library for accessing [Azure Blob Storage](/azure/storage/blobs/storage-blobs-introduction). |
| <img src="media/icons/AzureStorageQueue_256x.png" alt="Azure Storage Queues logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Storage Queues](../storage/azure-storage-queues-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage)<br>- **Client**: [📦 Aspire.Azure.Storage.Queues](https://www.nuget.org/packages/Aspire.Azure.Storage.Queues) | A library for accessing [Azure Storage Queues](/azure/storage/queues/storage-queues-introduction). |
| <img src="media/icons/AzureTable_256x.png" alt="Azure Table Storage logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Table Storage](../storage/azure-storage-tables-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage)<br>- **Client**: [📦 Aspire.Azure.Data.Tables](https://www.nuget.org/packages/Aspire.Azure.Data.Tables) | A library for accessing the [Azure Table](/azure/storage/tables/table-storage-overview) service. |
| <img src="media/icons/AzureWebPubSub_256x.png" alt="Azure Web PubSub logo." role="presentation" width="78" data-linktype="relative-path"> | - **Learn more**: [📄 Azure Web PubSub](../messaging/azure-web-pubsub-integration.md) <br/> - **Hosting**: [📦 Aspire.Hosting.Azure.WebPubSub](https://www.nuget.org/packages/Aspire.Hosting.Azure.WebPubSub)<br>- **Client**: [📦 Aspire.Azure.Messaging.WebPubSub](https://www.nuget.org/packages/Aspire.Azure.Messaging.WebPubSub) | A library for accessing the [Azure Web PubSub](/azure/azure-web-pubsub/) service. |
<!-- markdownlint-enable MD033 MD045 -->

### Amazon Web Services (AWS) hosting integrations

<!-- markdownlint-disable MD033 MD045 -->
| Integration docs and NuGet packages | Description |
|--|--|
| - **Learn more**: [📄 AWS Hosting](https://github.com/aws/integrations-on-dotnet-aspire-for-aws/blob/main/src/Aspire.Hosting.AWS/README.md) <br/> - **Hosting**: [📦 Aspire.Hosting.AWS](https://www.nuget.org/packages/Aspire.Hosting.AWS)<br>- **Client**: N/A | A library for modeling [AWS resources](https://aws.amazon.com/cdk/). |
<!-- markdownlint-enable MD033 MD045 -->

For more information, see [GitHub: Aspire.Hosting.AWS library](https://github.com/aws/integrations-on-dotnet-aspire-for-aws/tree/main/src/Aspire.Hosting.AWS).

### Community Toolkit integrations

> [!NOTE]
> The Community Toolkit integrations are community-driven and maintained by the .NET Aspire community. These integrations are not officially supported by the .NET Aspire team.

<!-- markdownlint-disable MD033 MD045 -->
| Integration docs and NuGet packages | Description |
|--|--|
| - **Learn More**: [📄 Azure Static Web Apps emulator](../community-toolkit/hosting-azure-static-web-apps.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Azure.StaticWebApps](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Azure.StaticWebApps) <br /> - **Client**: N/A | A hosting integration for the [Azure Static Web Apps emulator](/azure/static-web-apps/static-web-apps-cli-overview) (Note: this does not support deployment of a project to Azure Static Web Apps). |
| - **Learn More**: [📄 Bun hosting](../community-toolkit/hosting-bun.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Bun](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Bun) <br /> - **Client**: N/A | A hosting integration for Bun apps. |
| - **Learn More**: [📄 Deno hosting](../community-toolkit/hosting-deno.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Deno](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Deno) <br /> - **Client**: N/A | A hosting integration for Deno apps. |
| - **Learn More**: [📄 Go hosting](../community-toolkit/hosting-golang.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Golang](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Golang) <br /> - **Client**: N/A | A hosting integration for Go apps. |
| - **Learn More**: [📄 Java/Spring hosting](../community-toolkit/hosting-java.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Java](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Java) <br /> - **Client**: N/A | A integration for running Java code in .NET Aspire either using the local JDK or using a container. |
| - **Learn More**: [📄 Node.js hosting extensions](../community-toolkit/hosting-nodejs-extensions.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.NodeJs.Extensions](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.NodeJS.Extensions) <br /> - **Client**: N/A  | An integration that contains some additional extensions for running Node.js applications |
| - **Learn More**: [📄 Ollama](../community-toolkit/ollama.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Ollama](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Ollama) <br /> - **Client**: [📦 Aspire.CommunitToolkit.OllamaSharp](https://nuget.org/packages/CommunityToolkit.Aspire.OllamaSharp) | An Aspire component leveraging the [Ollama](https://ollama.com) container with support for downloading a model on startup. |
| - **Learn More**: [📄 Meilisearch hosting](../community-toolkit/hosting-meilisearch.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Meilisearch](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Meilisearch) <br /> - **Client**: [📦 Aspire.CommunitToolkit.Meilisearch](https://nuget.org/packages/CommunityToolkit.Aspire.Meilisearch) | An Aspire component leveraging the [Meilisearch](https://meilisearch.com) container. |
| - **Learn More**: [📄 Rust hosting](../community-toolkit/hosting-rust.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.Rust](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.Rust) <br /> - **Client**: N/A | A hosting integration for Rust apps. |
| - **Learn More**: [📄 SQL Database projects hosting](../community-toolkit/hosting-sql-database-projects.md) <br /> - **Hosting**: [📦 CommunityToolkit.Aspire.Hosting.SqlDatabaseProjects](https://nuget.org/packages/CommunityToolkit.Aspire.Hosting.SqlDatabaseProjects) <br /> - **Client**: N/A | An Aspire hosting integration for SQL Database Projects. |
<!-- markdownlint-enable MD033 MD045 -->

For more information, see [.NET Aspire Community Toolkit](../community-toolkit/overview.md).

-----------------------
-----------------------
-----------------------
-----------------------

---
title: Azure integrations overview
description: Overview of the Azure integrations available in the .NET Aspire.
ms.date: 03/07/2025
uid: dotnet/aspire/integrations/azure-overview
---

# .NET Aspire Azure integrations overview

[Azure](/azure) is the most popular cloud platform for building and deploying [.NET applications](/dotnet/azure). The [Azure SDK for .NET](/dotnet/azure/sdk/azure-sdk-for-dotnet) allows for easy management and use of Azure services. .NET Aspire provides a set of integrations with Azure services, where you're free to add new resources or connect to existing ones. This article details some common aspects of all Azure integrations in .NET Aspire and aims to help you understand how to use them.

## Add Azure resources

All .NET Aspire Azure hosting integrations expose Azure resources and by convention are added using `AddAzure*` APIs. When you add these resources to your .NET Aspire app host, they represent an Azure service. The `AddAzure*` API returns an <xref:Aspire.Hosting.ApplicationModel.IResourceBuilder`1> where `T` is the type of Azure resource. These `IResourceBuilder<T>` (builder) interfaces provide a fluent API that allows you to configure the underlying Azure resource within the [app model](xref:dotnet/aspire/app-host#terminology). There are APIs for adding new Azure resources, marking resources as existing, and configuring how the resources behave in various execution contexts.

### Typical developer experience

When your .NET Aspire app host contains Azure resources, and you run it locally (typical developer <kbd>F5</kbd> or `dotnet run` experience), the [Azure resources are provisioned](local-provisioning.md) in your Azure subscription. This allows you as the developer to debug against them locally in the context of your app host.

.NET Aspire aims to minimize costs by defaulting to _Basic_ or _Standard_ [Stock Keeping Unit (SKU)](/partner-center/developer/product-resources#sku) for its Azure integrations. While these sensible defaults are provided, you can [customize the Azure resources](#azureprovisioning-customization) to suit your needs. Additionally, some integrations support [emulators](#local-emulators) or [containers](#local-containers), which are useful for local development, testing, and debugging. By default, when you run your app locally, the Azure resources use the actual Azure service. However, you can configure them to use local emulators or containers, avoiding costs associated with the actual Azure service during local development.

### Local emulators

Some Azure services can be emulated to run locally. Currently, .NET Aspire supports the following Azure emulators:

| Hosting integration | Description |
|--|--|
| Azure Cosmos DB | Call <xref:Aspire.Hosting.AzureCosmosExtensions.RunAsEmulator*?displayProperty=nameWithType> on the `IResourceBuilder<AzureCosmosDBResource>` to configure the Cosmos DB resource to be [emulated with the NoSQL API](/azure/cosmos-db/how-to-develop-emulator). |
| Azure Event Hubs | Call <xref:Aspire.Hosting.AzureEventHubsExtensions.RunAsEmulator*?displayProperty=nameWithType> on the `IResourceBuilder<AzureEventHubsResource>` to configure the Event Hubs resource to be [emulated](/azure/event-hubs/overview-emulator). |
| Azure Service Bus | Call <xref:Aspire.Hosting.AzureServiceBusExtensions.RunAsEmulator*?displayProperty=nameWithType> on the `IResourceBuilder<AzureServiceBusResource>` to configure the Service Bus resource to be [emulated with Service Bus emulator](/azure/service-bus-messaging/overview-emulator). |
| Azure SignalR Service | Call <xref:Aspire.Hosting.AzureSignalRExtensions.RunAsEmulator*?displayProperty=nameWithType> on the `IResourceBuilder<AzureSignalRResource>` to configure the SignalR resource to be [emulated with Azure SignalR emulator](/azure/azure-signalr/signalr-howto-emulator). |
| Azure Storage | Call <xref:Aspire.Hosting.AzureStorageExtensions.RunAsEmulator*?displayProperty=nameWithType> on the `IResourceBuilder<AzureStorageResource>` to configure the Storage resource to be [emulated with Azurite](/azure/storage/common/storage-use-azurite). |

To have your Azure resources use the local emulators, chain a call the `RunAsEmulator` method on the Azure resource builder. This method configures the Azure resource to use the local emulator instead of the actual Azure service.

> [!IMPORTANT]
> Calling any of the available `RunAsEmulator` APIs on an Azure resource builder doesn't effect the [publishing manifest](../deployment/manifest-format.md). When you publish your app, [generated Bicep file](#infrastructure-as-code) reflects the actual Azure service, not the local emulator.

### Local containers

Some Azure resources can be substituted locally using open-source or on-premises containers. To substitute an Azure resource locally in a container, chain a call to the `RunAsContainer` method on the Azure resource builder. This method configures the Azure resource to use a containerized version of the service for local development and testing, rather than the actual Azure service.

Currently, .NET Aspire supports the following Azure services as containers:

| Hosting integration | Details |
|--|--|
| Azure Cache for Redis | Call <xref:Aspire.Hosting.AzureRedisExtensions.RunAsContainer*?displayProperty=nameWithType> on the `IResourceBuilder<AzureRedisCacheResource>` to configure it to run locally in a container, based on the `docker.io/library/redis` image. |
| Azure PostgreSQL Flexible Server | Call <xref:Aspire.Hosting.AzurePostgresExtensions.RunAsContainer*?displayProperty=nameWithType> on the `IResourceBuilder<AzurePostgresFlexibleServerResource>` to configure it to run locally in a container, based on the `docker.io/library/postgres` image. |
| Azure SQL Server | Call <xref:Aspire.Hosting.AzureSqlExtensions.RunAsContainer*?displayProperty=nameWithType> on the `IResourceBuilder<AzureSqlServerResource>` to configure it to run locally in a container, based on the `mcr.microsoft.com/mssql/server` image. |

> [!NOTE]
> Like emulators, calling `RunAsContainer` on an Azure resource builder doesn't effect the [publishing manifest](../deployment/manifest-format.md). When you publish your app, the [generated Bicep file](#infrastructure-as-code) reflects the actual Azure service, not the local container.

### Understand Azure integration APIs

.NET Aspire's strength lies in its ability to provide an amazing developer inner-loop. The Azure integrations are no different. They provide a set of common APIs and patterns that are shared across all Azure resources. These APIs and patterns are designed to make it easy to work with Azure resources in a consistent manner.

In the preceding containers section, you saw how to run Azure services locally in containers. If you're familiar with .NET Aspire, you might wonder how calling `AddAzureRedis("redis").RunAsContainer()` to get a local `docker.io/library/redis` container differs from `AddRedis("redis")`—as they both result in the same local container.

The answer is that there's no difference when running locally. However, when they're published you get different resources:

| API | Run mode | Publish mode |
|--|--|--|
| [AddAzureRedis("redis").RunAsContainer()](xref:Aspire.Hosting.AzureRedisExtensions.RunAsContainer*) | Local Redis container | Azure Cache for Redis |
| [AddRedis("redis")](xref:Aspire.Hosting.RedisBuilderExtensions.AddRedis*) | Local Redis container | Azure Container App with Redis image |

The same is true for SQL and PostgreSQL services:

| API | Run mode | Publish mode |
|--|--|--|
| [AddAzurePostgresFlexibleServer("postgres").RunAsContainer()](xref:Aspire.Hosting.AzurePostgresExtensions.RunAsContainer*) | Local PostgreSQL container | Azure PostgreSQL Flexible Server |
| [AddPostgres("postgres")](xref:Aspire.Hosting.PostgresBuilderExtensions.AddPostgres*) | Local PostgreSQL container | Azure Container App with PostgreSQL image |
| [AddAzureSqlServer("sql").RunAsContainer()](xref:Aspire.Hosting.AzureSqlExtensions.RunAsContainer*) | Local SQL Server container | Azure SQL Server |
| [AddSqlServer("sql")](xref:Aspire.Hosting.SqlServerBuilderExtensions.AddSqlServer*) | Local SQL Server container | Azure Container App with SQL Server image |

For more information on the difference between run and publish modes, see [.NET Aspire app host: Execution context](xref:dotnet/aspire/app-host#execution-context).

### APIs for expressing Azure resources in different modes

The distributed application builder, part of the [app host](../fundamentals/app-host-overview.md), uses the builder pattern to `AddAzure*` resources to the [_app model_](../fundamentals/app-host-overview.md#terminology). Developers can configure these resources and define their behavior in different execution contexts. Azure hosting integrations provide APIs to specify how these resources should be "published" and "run."

When the app host executes, the [_execution context_](../fundamentals/app-host-overview.md#execution-context) is used to determine whether the app host is in <xref:Aspire.Hosting.DistributedApplicationOperation.Run> or <xref:Aspire.Hosting.DistributedApplicationOperation.Publish> mode. The naming conventions for these APIs indicate the intended action for the resource.

The following table summarizes the naming conventions used to express Azure resources:

| Operation | API | Description |
|--|--|--|
| Publish | <xref:Aspire.Hosting.AzureResourceExtensions.PublishAsConnectionString``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0})> | Changes the resource to be published as a connection string reference in the manifest. |
| Publish | <xref:Aspire.Hosting.ExistingAzureResourceExtensions.PublishAsExisting*> | Uses an existing Azure resource when the application is deployed instead of creating a new one. |
| Run | <xref:Aspire.Hosting.AzureSqlExtensions.RunAsContainer(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureSqlServerResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.SqlServerServerResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzureRedisExtensions.RunAsContainer(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureRedisCacheResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.RedisResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzurePostgresExtensions.RunAsContainer(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzurePostgresFlexibleServerResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.PostgresServerResource}})> | Configures an equivalent container to run locally. For more information, see [Local containers](#local-containers). |
| Run | <xref:Aspire.Hosting.AzureCosmosExtensions.RunAsEmulator(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.AzureCosmosDBResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureCosmosDBEmulatorResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzureSignalRExtensions.RunAsEmulator(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.AzureSignalRResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureSignalREmulatorResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzureStorageExtensions.RunAsEmulator(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureStorageResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureStorageEmulatorResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzureEventHubsExtensions.RunAsEmulator(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureEventHubsResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureEventHubsEmulatorResource}})?displayProperty=nameWithType> <br/> <xref:Aspire.Hosting.AzureServiceBusExtensions.RunAsEmulator(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusResource},System.Action{Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusEmulatorResource}})?displayProperty=nameWithType> | Configures the Azure resource to be emulated. For more information, see [Local emulators](#local-emulators). |
| Run | <xref:Aspire.Hosting.ExistingAzureResourceExtensions.RunAsExisting*> | Uses an existing resource when the application is running instead of creating a new one. |
| Publish and Run | <xref:Aspire.Hosting.ExistingAzureResourceExtensions.AsExisting``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource})> | Uses an existing resource regardless of the operation. |

> [!NOTE]
> Not all APIs are available on all Azure resources. For example, some Azure resources can be containerized or emulated, while others can't.

For more information on execution modes, see [Execution context](../fundamentals/app-host-overview.md#execution-context).

#### General run mode API use cases

Use <xref:Aspire.Hosting.ExistingAzureResourceExtensions.RunAsExisting*> when you need to dynamically interact with an existing resource during runtime without needing to deploy or update it. Use <xref:Aspire.Hosting.ExistingAzureResourceExtensions.PublishAsExisting*> when declaring existing resources as part of a deployment configuration, ensuring the correct scopes and permissions are applied. Finally, use <xref:Aspire.Hosting.ExistingAzureResourceExtensions.AsExisting``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource})> when declaring existing resources in both configurations, with a requirement to parameterize the references.

You can query whether a resource is marked as an existing resource, by calling the <xref:Aspire.Hosting.ExistingAzureResourceExtensions.IsExisting(Aspire.Hosting.ApplicationModel.IResource)> extension method on the <xref:Aspire.Hosting.ApplicationModel.IResource>. For more information, see [Use existing Azure resources](#use-existing-azure-resources).

## Use existing Azure resources

.NET Aspire provides support for referencing existing Azure resources. You mark an existing resource through the `PublishAsExisting`, `RunAsExisting`, and `AsExisting` APIs. These APIs allow developers to reference already-deployed Azure resources, configure them, and generate appropriate deployment manifests using Bicep templates.

Existing resources referenced with these APIs can be enhanced with role assignments and other customizations that are available with .NET Aspire's [infrastructure as code capabilities](#infrastructure-as-code). These APIs are limited to Azure resources that can be deployed with Bicep templates.

### Configure existing Azure resources for run mode

The <xref:Aspire.Hosting.ExistingAzureResourceExtensions.RunAsExisting*> method is used when a distributed application is executing in "run" mode. In this mode, it assumes that the referenced Azure resource already exists and integrates with it during execution without provisioning the resource. To mark an Azure resource as existing, call the `RunAsExisting` method on the resource builder. Consider the following example:

```csharp
var builder = DistributedApplication.CreateBuilder();

var existingServiceBusName = builder.AddParameter("existingServiceBusName");
var existingServiceBusResourceGroup = builder.AddParameter("existingServiceBusResourceGroup");

var serviceBus = builder.AddAzureServiceBus("messaging")
                        .RunAsExisting(existingServiceBusName, existingServiceBusResourceGroup);

serviceBus.AddServiceBusQueue("queue");
```

The preceding code:

- Creates a new `builder` instance.
- Adds a parameter named `existingServiceBusName` to the builder.
- Adds an Azure Service Bus resource named `messaging` to the builder.
- Calls the `RunAsExisting` method on the `serviceBus` resource builder, passing the `existingServiceBusName` parameter—alternatively, you can use the `string` parameter overload.
- Adds a queue named `queue` to the `serviceBus` resource.

By default, the Service Bus parameter reference is assumed to be in the same Azure resource group. However, if it's in a different resource group, you can pass the resource group explicitly as a parameter to correctly specify the appropriate resource grouping.

### Configure existing Azure resources for publish mode

The <xref:Aspire.Hosting.ExistingAzureResourceExtensions.PublishAsExisting*> method is used in "publish" mode when the intent is to declare and reference an already-existing Azure resource during publish mode. This API facilitates the creation of manifests and templates that include resource definitions that map to existing resources in Bicep.

To mark an Azure resource as existing in for the "publish" mode, call the `PublishAsExisting` method on the resource builder. Consider the following example:

```csharp
var builder = DistributedApplication.CreateBuilder();

var existingServiceBusName = builder.AddParameter("existingServiceBusName");
var existingServiceBusResourceGroup = builder.AddParameter("existingServiceBusResourceGroup");

var serviceBus = builder.AddAzureServiceBus("messaging")
                        .PublishAsExisting(existingServiceBusName, existingServiceBusResourceGroup);

serviceBus.AddServiceBusQueue("queue");
```

The preceding code:

- Creates a new `builder` instance.
- Adds a parameter named `existingServiceBusName` to the builder.
- Adds an Azure Service Bus resource named `messaging` to the builder.
- Calls the `PublishAsExisting` method on the `serviceBus` resource builder, passing the `existingServiceBusName` parameter—alternatively, you can use the `string` parameter overload.
- Adds a queue named `queue` to the `serviceBus` resource.

After the app host is executed in publish mode, the generated manifest file will include the `existingResourceName` parameter, which can be used to reference the existing Azure resource. Consider the following generated partial snippet of the manifest file:

```json
"messaging": {
  "type": "azure.bicep.v0",
  "connectionString": "{messaging.outputs.serviceBusEndpoint}",
  "path": "messaging.module.bicep",
  "params": {
    "existingServiceBusName": "{existingServiceBusName.value}",
    "principalType": "",
    "principalId": ""
  }
},
"queue": {
  "type": "value.v0",
  "connectionString": "{messaging.outputs.serviceBusEndpoint}"
}
```

For more information on the manifest file, see [.NET Aspire manifest format for deployment tool builders](../deployment/manifest-format.md).

Additionally, the generated Bicep template includes the `existingResourceName` parameter, which can be used to reference the existing Azure resource. Consider the following generated Bicep template:

```bicep
@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param existingServiceBusName string

param principalType string

param principalId string

resource messaging 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: existingServiceBusName
}

resource messaging_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(messaging.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: principalType
  }
  scope: messaging
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'queue'
  parent: messaging
}

output serviceBusEndpoint string = messaging.properties.serviceBusEndpoint
```

For more information on the generated Bicep templates, see [Infrastructure as code](#infrastructure-as-code) and [consider other publishing APIs](#publish-as-azure-container-app).

> [!WARNING]
> When interacting with existing resources that require authentication, ensure the authentication strategy that you're configuring in the .NET Aspire application model aligns with the authentication strategy allowed by the existing resource. For example, it's not possible to use managed identity against an existing Azure PostgreSQL resource that isn't configured to allow managed identity. Similarly, if an existing Azure Redis resource disabled access keys, it's not possible to use access key authentication.

### Configure existing Azure resources in all modes

The <xref:Aspire.Hosting.ExistingAzureResourceExtensions.AsExisting``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource},Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.ParameterResource})> method is used when the distributed application is running in "run" or "publish" mode. Because the `AsExisting` method operates in both scenarios, it only supports a parameterized reference to the resource name or resource group name. This approach helps prevent the use of the same resource in both testing and production environments.

To mark an Azure resource as existing, call the `AsExisting` method on the resource builder. Consider the following example:

```csharp
var builder = DistributedApplication.CreateBuilder();

var existingServiceBusName = builder.AddParameter("existingServiceBusName");
var existingServiceBusResourceGroup = builder.AddParameter("existingServiceBusResourceGroup");

var serviceBus = builder.AddAzureServiceBus("messaging")
                        .AsExisting(existingServiceBusName, existingServiceBusResourceGroup);

serviceBus.AddServiceBusQueue("queue");
```

The preceding code:

- Creates a new `builder` instance.
- Adds a parameter named `existingServiceBusName` to the builder.
- Adds an Azure Service Bus resource named `messaging` to the builder.
- Calls the `AsExisting` method on the `serviceBus` resource builder, passing the `existingServiceBusName` parameter.
- Adds a queue named `queue` to the `serviceBus` resource.

## Add existing Azure resources with connection strings

.NET Aspire provides the ability to [connect to existing resources](../fundamentals/app-host-overview.md#reference-existing-resources), including Azure resources. Expressing connection strings is useful when you have existing Azure resources that you want to use in your .NET Aspire app. The <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString*> API is used with the app host's [execution context](../fundamentals/app-host-overview.md#execution-context) to conditionally add a connection string to the app model.

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

Consider the following example, where in _publish_ mode you add an Azure Storage resource while in _run_ mode you add a connection string to an existing Azure Storage:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureStorage("storage")
    : builder.AddConnectionString("storage");

builder.AddProject<Projects.Api>("api")
       .WithReference(storage);

// After adding all resources, run the app...
```

The preceding code:

- Creates a new `builder` instance.
- Adds an Azure Storage resource named `storage` in "publish" mode.
- Adds a connection string to an existing Azure Storage named `storage` in "run" mode.
- Adds a project named `api` to the builder.
- The `api` project references the `storage` resource regardless of the mode.

The consuming API project uses the connection string information with no knowledge of how the app host configured it. In "publish" mode, the code adds a new Azure Storage resource—which would be reflected in the [deployment manifest](../deployment/manifest-format.md) accordingly. When in "run" mode the connection string corresponds to a configuration value visible to the app host. It's assumed that all role assignments for the target resource are configured. This means, you'd likely configure an environment variable or a user secret to store the connection string. The configuration is resolved from the `ConnectionStrings__storage` (or `ConnectionStrings:storage`) configuration key. These configuration values can be viewed when the app runs. For more information, see [Resource details](../fundamentals/dashboard/explore.md#resource-details).

Unlike existing resources modeled with [the first-class `AsExisting` API](#use-existing-azure-resources), existing resource modeled as connection strings can't be enhanced with additional role assignments or infrastructure customizations.

## Publish as Azure Container App

.NET Aspire allows you to publish primitive resources as [Azure Container Apps](/azure/container-apps/overview), a serverless platform that reduces infrastructure management. Supported resource types include:

- <xref:Aspire.Hosting.ApplicationModel.ContainerResource>: Represents a specified container.
- <xref:Aspire.Hosting.ApplicationModel.ExecutableResource>: Represents a specified executable process.
- <xref:Aspire.Hosting.ApplicationModel.ProjectResource>: Represents a specified .NET project.

To publish these resources, use the following APIs:

- <xref:Aspire.Hosting.AzureContainerAppContainerExtensions.PublishAsAzureContainerApp``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure,Azure.Provisioning.AppContainers.ContainerApp})?displayProperty=nameWithType>
- <xref:Aspire.Hosting.AzureContainerAppExecutableExtensions.PublishAsAzureContainerApp``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure,Azure.Provisioning.AppContainers.ContainerApp})?displayProperty=nameWithType>
- <xref:Aspire.Hosting.AzureContainerAppProjectExtensions.PublishAsAzureContainerApp``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure,Azure.Provisioning.AppContainers.ContainerApp})?displayProperty=nameWithType>

These APIs configure the resource to be published as an Azure Container App and implicitly call <xref:Aspire.Hosting.AzureContainerAppExtensions.AddAzureContainerAppsInfrastructure(Aspire.Hosting.IDistributedApplicationBuilder)> to add the necessary infrastructure and Bicep files to your app host. As an example, consider the following code:

```csharp
var builder = DistributedApplication.CreateBuilder();

var env = builder.AddParameter("env");

var api = builder.AddProject<Projects.AspireApi>("api")
                 .PublishAsAzureContainerApp<Projects.AspireApi>((infra, app) =>
                 {
                     app.Template.Containers[0].Value!.Env.Add(new ContainerAppEnvironmentVariable
                     {
                         Name = "Hello",
                         Value = env.AsProvisioningParameter(infra)
                     });
                 });
```

The preceding code:

- Creates a new `builder` instance.
- Adds a parameter named `env` to the builder.
- Adds a project named `api` to the builder.
- Calls the `PublishAsAzureContainerApp` method on the `api` resource builder, passing a lambda expression that configures the Azure Container App infrastructure—where `infra` is the <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> and `app` is the <xref:Azure.Provisioning.AppContainers.ContainerApp>.
  - Adds an environment variable named `Hello` to the container app, using the `env` parameter.
  - The `AsProvisioningParameter` method is used to treat `env` as either a new <xref:Azure.Provisioning.ProvisioningParameter> in infrastructure, or reuses an existing bicep parameter if one with the same name already exists.

For more information, see <xref:Azure.Provisioning.AppContainers.ContainerApp> and <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.AsProvisioningParameter*>.

## Infrastructure as code

The Azure SDK for .NET provides the [📦 Azure.Provisioning](https://www.nuget.org/packages/Azure.Provisioning) NuGet package and a suite of service-specific [Azure provisioning packages](https://www.nuget.org/packages?q=owner%3A+azure-sdk+description%3A+declarative+resource+provisioning&sortby=relevance). These Azure provisioning libraries make it easy to declaratively specify Azure infrastructure natively in .NET. Their APIs enable you to write object-oriented infrastructure in C#, resulting in Bicep. [Bicep is a domain-specific language (DSL)](/azure/azure-resource-manager/bicep/overview) for deploying Azure resources declaratively.

<!-- TODO: Add link from here to the Azure docs when they're written. -->

While it's possible to provision Azure resources manually, .NET Aspire simplifies the process by providing a set of APIs to express Azure resources. These APIs are available as extension methods in .NET Aspire Azure hosting libraries, extending the <xref:Aspire.Hosting.IDistributedApplicationBuilder> interface. When you add Azure resources to your app host, they add the appropriate provisioning functionality implicitly. In other words, you don't need to call any provisioning APIs directly.

Since .NET Aspire models Azure resources within Azure hosting integrations, the Azure SDK is used to provision these resources. Bicep files are generated that define the Azure resources you need. The generated Bicep files are output alongside the manifest file when you publish your app.

There are several ways to influence the generated Bicep files:

- [Azure.Provisioning customization](#azureprovisioning-customization):
  - [Configure infrastructure](#configure-infrastructure): Customize Azure resource infrastructure.
  - [Add Azure infrastructure](#add-azure-infrastructure): Manually add Azure infrastructure to your app host.
- [Use custom Bicep templates](#use-custom-bicep-templates):
  - [Reference Bicep files](#reference-bicep-files): Add a reference to a Bicep file on disk.
  - [Reference Bicep inline](#reference-bicep-inline): Add an inline Bicep template.

### Local provisioning and `Azure.Provisioning`

To avoid conflating terms and to help disambiguate "provisioning," it's important to understand the distinction between _local provisioning_ and _Azure provisioning_:

- **_Local provisioning:_**

  By default, when you call any of the Azure hosting integration APIs to add Azure resources, the <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning(Aspire.Hosting.IDistributedApplicationBuilder)> API is called implicitly. This API registers services in the dependency injection (DI) container that are used to provision Azure resources when the app host starts. This concept is known as _local provisioning_.  For more information, see [Local Azure provisioning](local-provisioning.md).

- **_`Azure.Provisioning`:_**

  `Azure.Provisioning` refers to the NuGet package, and is a set of libraries that lets you use C# to generate Bicep. The Azure hosting integrations in .NET Aspire use these libraries under the covers to generate Bicep files that define the Azure resources you need. For more information, see [`Azure.Provisioning` customization](#azureprovisioning-customization).

### `Azure.Provisioning` customization

All .NET Aspire Azure hosting integrations expose various Azure resources, and they're all subclasses of the <xref:Aspire.Hosting.Azure.AzureProvisioningResource> type—which itself inherits the <xref:Aspire.Hosting.Azure.AzureBicepResource>. This enables extensions that are generically type-constrained to this type, allowing for a fluent API to customize the infrastructure to your liking. While .NET Aspire provides defaults, you're free to influence the generated Bicep using these APIs.

#### Configure infrastructure

Regardless of the Azure resource you're working with, to configure its underlying infrastructure, you chain a call to the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure*> extension method. This method allows you to customize the infrastructure of the Azure resource by passing a `configure` delegate of type `Action<AzureResourceInfrastructure>`. The <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> type is a subclass of the <xref:Azure.Provisioning.Infrastructure?displayProperty=fullName>. This type exposes a massive API surface area for configuring the underlying infrastructure of the Azure resource.

Consider the following example:

:::code language="csharp" source="../snippets/azure/AppHost/Program.ConfigureInfrastructure.cs" id="infra":::

The preceding code:

- Adds a parameter named `storage-sku`.
- Adds Azure Storage with the <xref:Aspire.Hosting.AzureStorageExtensions.AddAzureStorage*> API named `storage`.
- Chains a call to `ConfigureInfrastructure` to customize the Azure Storage infrastructure:
  - Gets the provisionable resources.
  - Filters to a single <xref:Azure.Provisioning.Storage.StorageAccount>.
  - Assigns the `storage-sku` parameter to the <xref:Azure.Provisioning.Storage.StorageAccount.Sku?displayProperty=nameWithType> property:
    - A new instance of the <xref:Azure.Provisioning.Storage.StorageSku> has its `Name` property assigned from the result of the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.AsProvisioningParameter*> API.

This exemplifies flowing an [external parameter](../fundamentals/external-parameters.md) into the Azure Storage infrastructure, resulting in the generated Bicep file reflecting the desired configuration.

#### Add Azure infrastructure

Not all Azure services are exposed as .NET Aspire integrations. While they might be at a later time, you can still provision services that are available in `Azure.Provisioning.*` libraries. Imagine a scenario where you have worker service that's responsible for managing an Azure Container Registry. Now imagine that an app host project takes a dependency on the [📦 Azure.Provisioning.ContainerRegistry](https://www.nuget.org/packages/Azure.Provisioning.ContainerRegistry) NuGet package.

You can use the `AddAzureInfrastructure` API to add the Azure Container Registry infrastructure to your app host:

:::code language="csharp" source="../snippets/azure/AppHost/Program.AddAzureInfra.cs" id="add":::

The preceding code:

- Calls <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.AddAzureInfrastructure*> with a name of `acr`.
- Provides a `configureInfrastructure` delegate to customize the Azure Container Registry infrastructure:
  - Instantiates a <xref:Azure.Provisioning.ContainerRegistry.ContainerRegistryService> with the name `acr` and a standard SKU.
  - Adds the Azure Container Registry service to the `infra` variable.
  - Instantiates a <xref:Azure.Provisioning.ProvisioningOutput> with the name `registryName`, a type of `string`, and a value that corresponds to the name of the Azure Container Registry.
  - Adds the output to the `infra` variable.
- Adds a project named `worker` to the builder.
- Chains a call to <xref:Aspire.Hosting.ResourceBuilderExtensions.WithEnvironment*> to set the `ACR_REGISTRY_NAME` environment variable in the project to the value of the `registryName` output.

The functionality demonstrates how to add Azure infrastructure to your app host project, even if the Azure service isn't directly exposed as a .NET Aspire integration. It further shows how to flow the output of the Azure Container Registry into the environment of a dependent project.

Consider the resulting Bicep file:

:::code language="bicep" source="../snippets/azure/AppHost/acr.module.bicep":::

The Bicep file reflects the desired configuration of the Azure Container Registry, as defined by the `AddAzureInfrastructure` API.

### Use custom Bicep templates

When you're targeting Azure as your desired cloud provider, you can use Bicep to define your infrastructure as code. It aims to drastically simplify the authoring experience with a cleaner syntax and better support for modularity and code reuse.

While .NET Aspire provides a set of prebuilt Bicep templates, there might be times when you either want to customize the templates or create your own. This section explains the concepts and corresponding APIs that you can use to customize the Bicep templates.

> [!IMPORTANT]
> This section isn't intended to teach you Bicep, but rather to provide guidance on how to create custom Bicep templates for use with .NET Aspire.

As part of the [Azure deployment story for .NET Aspire](../deployment/overview.md), the Azure Developer CLI (`azd`) provides an understanding of your .NET Aspire project and the ability to deploy it to Azure. The `azd` CLI uses the Bicep templates to deploy the application to Azure.

#### Install `Aspire.Hosting.Azure` package

When you want to reference Bicep files, it's possible that you're not using any of the Azure hosting integrations. In this case, you can still reference Bicep files by installing the `Aspire.Hosting.Azure` package. This package provides the necessary APIs to reference Bicep files and customize the Azure resources.

> [!TIP]
> If you're using any of the Azure hosting integrations, you don't need to install the `Aspire.Hosting.Azure` package, as it's a transitive dependency.

To use any of this functionality, the [📦 Aspire.Hosting.Azure](https://www.nuget.org/packages/Aspire.Hosting.Azure) NuGet package must be installed:

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

#### What to expect from the examples

All the examples in this section assume that you're using the <xref:Aspire.Hosting.Azure> namespace. Additionally, the examples assume you have an <xref:Aspire.Hosting.IDistributedApplicationBuilder> instance:

```csharp
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// Examples go here...

builder.Build().Run();
```

By default, when you call any of the Bicep-related APIs, a call is also made to <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning%2A> that adds support for generating Azure resources dynamically during application startup. For more information, see [Local provisioning and `Azure.Provisioning`](#local-provisioning-and-azureprovisioning).

#### Reference Bicep files

Imagine that you have a Bicep template in a file named `storage.bicep` that provisions an Azure Storage Account:

:::code language="bicep" source="snippets/AppHost.Bicep/storage.bicep":::

To add a reference to the Bicep file on disk, call the <xref:Aspire.Hosting.AzureBicepResourceExtensions.AddBicepTemplate%2A> method. Consider the following example:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.ReferenceBicep.cs" id="addfile":::

The preceding code adds a reference to a Bicep file located at `../infra/storage.bicep`. The file paths should be relative to the _app host_ project. This reference results in an <xref:Aspire.Hosting.Azure.AzureBicepResource> being added to the application's resources collection with the `"storage"` name, and the API returns an `IResourceBuilder<AzureBicepResource>` instance that can be used to further customize the resource.

#### Reference Bicep inline

While having a Bicep file on disk is the most common scenario, you can also add Bicep templates inline. Inline templates can be useful when you want to define a template in code or when you want to generate the template dynamically. To add an inline Bicep template, call the <xref:Aspire.Hosting.AzureBicepResourceExtensions.AddBicepTemplateString%2A> method with the Bicep template as a `string`. Consider the following example:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.InlineBicep.cs" id="addinline":::

In this example, the Bicep template is defined as an inline `string` and added to the application's resources collection with the name `"ai"`. This example provisions an Azure AI resource.

#### Pass parameters to Bicep templates

[Bicep supports accepting parameters](/azure/azure-resource-manager/bicep/parameters), which can be used to customize the behavior of the template. To pass parameters to a Bicep template from .NET Aspire, chain calls to the <xref:Aspire.Hosting.AzureBicepResourceExtensions.WithParameter%2A> method as shown in the following example:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.PassParameter.cs" id="addparameter":::

The preceding code:

- Adds a parameter named `"region"` to the `builder` instance.
- Adds a reference to a Bicep file located at `../infra/storage.bicep`.
- Passes the `"region"` parameter to the Bicep template, which is resolved using the standard parameter resolution.
- Passes the `"storageName"` parameter to the Bicep template with a _hardcoded_ value.
- Passes the `"tags"` parameter to the Bicep template with an array of strings.

For more information, see [External parameters](../fundamentals/external-parameters.md).

##### Well-known parameters

.NET Aspire provides a set of well-known parameters that can be passed to Bicep templates. These parameters are used to provide information about the application and the environment to the Bicep templates. The following well-known parameters are available:

| Field | Description | Value |
|--|--|--|
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.KeyVaultName?displayProperty=nameWithType> | The name of the key vault resource used to store secret outputs. | `"keyVaultName"` |
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.Location?displayProperty=nameWithType> | The location of the resource. This is required for all resources. | `"location"` |
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId?displayProperty=nameWithType> | The resource ID of the log analytics workspace. | `"logAnalyticsWorkspaceId"` |
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.PrincipalId?displayProperty=nameWithType> | The principal ID of the current user or managed identity. | `"principalId"` |
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.PrincipalName?displayProperty=nameWithType> | The principal name of the current user or managed identity. | `"principalName"` |
| <xref:Aspire.Hosting.Azure.AzureBicepResource.KnownParameters.PrincipalType?displayProperty=nameWithType> | The principal type of the current user or managed identity. Either `User` or `ServicePrincipal`. | `"principalType"` |

To use a well-known parameter, pass the parameter name to the <xref:Aspire.Hosting.AzureBicepResourceExtensions.WithParameter%2A> method, such as `WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)`. You don't pass values for well-known parameters, as .NET Aspire resolves them on your behalf.

Consider an example where you want to set up an Azure Event Grid webhook. You might define the Bicep template as follows:

 :::code language="bicep" source="snippets/AppHost.Bicep/event-grid-webhook.bicep" highlight="3-4,27-35":::

This Bicep template defines several parameters, including the `topicName`, `webHookEndpoint`, `principalId`, `principalType`, and the optional `location`. To pass these parameters to the Bicep template, you can use the following code snippet:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.PassParameter.cs" id="addwellknownparams":::

- The `webHookApi` project is added as a reference to the `builder`.
- The `topicName` parameter is passed a hardcoded name value.
- The `webHookEndpoint` parameter is passed as an expression that resolves to the URL from the `api` project references' "https" endpoint with the `/hook` route.
- The `principalId` and `principalType` parameters are passed as well-known parameters.

The well-known parameters are convention-based and shouldn't be accompanied with a corresponding value when passed using the `WithParameter` API. Well-known parameters simplify some common functionality, such as _role assignments_, when added to the Bicep templates, as shown in the preceding example. Role assignments are required for the Event Grid webhook to send events to the specified endpoint. For more information, see [Event Grid Data Sender role assignment](/azure/role-based-access-control/built-in-roles/integration#eventgrid-data-sender).

### Get outputs from Bicep references

In addition to passing parameters to Bicep templates, you can also get outputs from the Bicep templates. Consider the following Bicep template, as it defines an `output` named `endpoint`:

:::code language="bicep" source="snippets/AppHost.Bicep/storage-out.bicep":::

The Bicep defines an output named `endpoint`. To get the output from the Bicep template, call the <xref:Aspire.Hosting.AzureBicepResourceExtensions.GetOutput%2A> method on an `IResourceBuilder<AzureBicepResource>` instance as demonstrated in following C# code snippet:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.GetOutputReference.cs" id="getoutput":::

In this example, the output from the Bicep template is retrieved and stored in an `endpoint` variable. Typically, you would pass this output as an environment variable to another resource that relies on it. For instance, if you had an ASP.NET Core Minimal API project that depended on this endpoint, you could pass the output as an environment variable to the project using the following code snippet:

```csharp
var storage = builder.AddBicepTemplate(
                name: "storage",
                bicepFile: "../infra/storage.bicep"
            );

var endpoint = storage.GetOutput("endpoint");

var apiService = builder.AddProject<Projects.AspireSample_ApiService>(
        name: "apiservice"
    )
    .WithEnvironment("STORAGE_ENDPOINT", endpoint);
```

For more information, see [Bicep outputs](/azure/azure-resource-manager/bicep/outputs).

### Get secret outputs from Bicep references

It's important to [avoid outputs for secrets](/azure/azure-resource-manager/bicep/scenarios-secrets#avoid-outputs-for-secrets) when working with Bicep. If an output is considered a _secret_, meaning it shouldn't be exposed in logs or other places, you can treat it as such. This can be achieved by storing the secret in Azure Key Vault and referencing it in the Bicep template. .NET Aspire's Azure integration provides a pattern for securely storing outputs from the Bicep template by allows resources to use the `keyVaultName` parameter to store secrets in Azure Key Vault.

Consider the following Bicep template as an example the helps to demonstrate this concept of securing secret outputs:

:::code language="bicep" source="snippets/AppHost.Bicep/cosmosdb.bicep" highlight="2,41":::

The preceding Bicep template expects a `keyVaultName` parameter, among several other parameters. It then defines an Azure Cosmos DB resource and stashes a secret into Azure Key Vault, named `connectionString` which represents the fully qualified connection string to the Cosmos DB instance. To access this secret connection string value, you can use the following code snippet:

:::code language="csharp" source="snippets/AppHost.Bicep/Program.cs" id="secrets":::

In the preceding code snippet, the `cosmos` Bicep template is added as a reference to the `builder`. The `connectionString` secret output is retrieved from the Bicep template and stored in a variable. The secret output is then passed as an environment variable (`ConnectionStrings__cosmos`) to the `api` project. This environment variable is used to connect to the Cosmos DB instance.

When this resource is deployed, the underlying deployment mechanism will automatically [Reference secrets from Azure Key Vault](/azure/container-apps/manage-secrets?tabs=azure-portal#reference-secret-from-key-vault). To guarantee secret isolation, .NET Aspire creates a Key Vault per source.

> [!NOTE]
> In _local provisioning_ mode, the secret is extracted from Key Vault and set it in an environment variable. For more information, see [Local Azure provisioning](local-provisioning.md).

## Publishing

When you publish your app, the Azure provisioning generated Bicep is used by the Azure Developer CLI to create the Azure resources in your Azure subscription. .NET Aspire outputs a [publishing manifest](../deployment/manifest-format.md), that's also a vital part of the publishing process. The Azure Developer CLI is a command-line tool that provides a set of commands to manage Azure resources.

For more information on publishing and deployment, see [Deploy a .NET Aspire project to Azure Container Apps using the Azure Developer CLI (in-depth guide)](../deployment/azure/aca-deployment-azd-in-depth.md).

-------------------------------
-------------------------------
-------------------------------
-------------------------------

---
title: Local Azure provisioning
description: Learn how to use Azure resources in your local development environment.
ms.date: 12/13/2024
uid: dotnet/aspire/local-azure-provisioning
---

# Local Azure provisioning

.NET Aspire simplifies local cloud-native app development with its compelling app host model. This model allows you to run your app locally with the same configuration and services as in Azure. In this article you learn how to provision Azure resources from your local development environment through the [.NET Aspire app host](xref:dotnet/aspire/app-host).

> [!NOTE]
> To be clear, resources are provisioned in Azure, but the provisioning process is initiated from your local development environment. To optimize your local development experience, consider using emulator or containers when available. For more information, see [Typical developer experience](integrations-overview.md#typical-developer-experience).

## Requirements

This article assumes that you have an Azure account and subscription. If you don't have an Azure account, you can create a free one at [Azure Free Account](https://azure.microsoft.com/free/). For provisioning functionality to work correctly, you'll need to be authenticated with Azure. Ensure that you have the [Azure Developer CLI](/cli/azure/install-azure-cli) installed. Additionally, you'll need to provide some configuration values so that the provisioning logic can create resources on your behalf.

## App host provisioning APIs

The app host provides a set of APIs to express Azure resources. These APIs are available as extension methods in .NET Aspire Azure hosting libraries, extending the <xref:Aspire.Hosting.IDistributedApplicationBuilder> interface. When you add Azure resources to your app host, they'll add the appropriate provisioning functionality implicitly. In other words, you don't need to call any provisioning APIs directly.

When the app host starts, the following provisioning logic is executed:

1. The `Azure` configuration section is validated.
1. When invalid the dashboard and app host output provides hints as to what's missing. For more information, see [Missing configuration value hints](#missing-configuration-value-hints).
1. When valid Azure resources are conditionally provisioned:
    1. If an Azure deployment for a given resource doesn't exist, it's created and configured as a deployment.
    1. The configuration of said deployment is stamped with a checksum as a means to support only provisioning resources as necessary.

### Use existing Azure resources

The app host automatically manages provisioning of Azure resources. The first time the app host runs, it provisions the resources specified in the app host. Subsequent runs don't provision the resources again unless the app host configuration changes.

If you've already provisioned Azure resources outside of the app host and want to use them, you can provide the connection string with the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString*> API as shown in the following Azure Key Vault example:

```csharp
// Service registration
var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");

// Service consumption
builder.AddProject<Projects.ExampleProject>()
       .WithReference(secrets)
```

The preceding code snippet shows how to add an Azure Key Vault to the app host. The <xref:Aspire.Hosting.AzureKeyVaultResourceExtensions.AddAzureKeyVault*> API is used to add the Azure Key Vault to the app host. The `AddConnectionString` API is used to provide the connection string to the app host.

Alternatively, for some Azure resources, you can opt-in to running them as an emulator with the `RunAsEmulator` API. This API is available for [Azure Cosmos DB](../database/azure-cosmos-db-integration.md) and [Azure Storage](../storage/azure-storage-integrations.md) integrations. For example, to run Azure Cosmos DB as an emulator, you can use the following code snippet:

```csharp
var cosmos = builder.AddAzureCosmosDB("cosmos")
                    .RunAsEmulator();
```

The <xref:Aspire.Hosting.AzureCosmosExtensions.RunAsEmulator*> API configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API.

### .NET Aspire Azure hosting integrations

If you're using Azure resources in your app host, you're using one or more of the [.NET Aspire Azure hosting integrations](integrations-overview.md). These hosting libraries provide extension methods to the <xref:Aspire.Hosting.IDistributedApplicationBuilder> interface to add Azure resources to your app host.

## Configuration

When utilizing Azure resources in your local development environment, you need to provide the necessary configuration values. Configuration values are specified under the `Azure` section:

- `SubscriptionId`: The Azure subscription ID.
- `AllowResourceGroupCreation`: A boolean value that indicates whether to create a new resource group.
- `ResourceGroup`: The name of the resource group to use.
- `Location`: The Azure region to use.

Consider the following example _:::no-loc text="appsettings.json":::_ configuration:

```json
{
  "Azure": {
    "SubscriptionId": "<Your subscription id>",
    "AllowResourceGroupCreation": true,
    "ResourceGroup": "<Valid resource group name>",
    "Location": "<Valid Azure location>"
  }
}
```

> [!IMPORTANT]
> It's recommended to store these values as app secrets. For more information, see [Manage app secrets](/aspnet/core/security/app-secrets).

After you've configured the necessary values, you can start provisioning Azure resources in your local development environment.

### Azure provisioning credential store

The .NET Aspire app host uses a credential store for Azure resource authentication and authorization. Depending on your subscription, the correct credential store may be needed for multi-tenant provisioning scenarios.

With the [📦 Aspire.Hosting.Azure](https://nuget.org/packages/Aspire.Hosting.Azure) NuGet package installed, and if your app host depends on Azure resources, the default Azure credential store relies on the <xref:Azure.Identity.DefaultAzureCredential>. To change this behavior, you can set the credential store value in the _:::no-loc text="appsettings.json":::_ file, as shown in the following example:

```json
{
  "Azure": {
    "CredentialSource": "AzureCli"
  }
}
```

As with all [configuration-based settings](/dotnet/core/extensions/configuration), you can configure these with alternative providers, such as [user secrets](/aspnet/core/security/app-secrets) or [environment variables](/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider). The `Azure:CredentialSource` value can be set to one of the following values:

- `AzureCli`: Delegates to the <xref:Azure.Identity.AzureCliCredential>.
- `AzurePowerShell`: Delegates to the <xref:Azure.Identity.AzurePowerShellCredential>.
- `VisualStudio`: Delegates to the <xref:Azure.Identity.VisualStudioCredential>.
- `VisualStudioCode`: Delegates to the <xref:Azure.Identity.VisualStudioCodeCredential>.
- `AzureDeveloperCli`: Delegates to the <xref:Azure.Identity.AzureDeveloperCliCredential>.
- `InteractiveBrowser`: Delegates to the <xref:Azure.Identity.InteractiveBrowserCredential>.

> [!TIP]
> For more information about the Azure SDK authentication and authorization, see [Credential chains in the Azure Identity library for .NET](/dotnet/azure/sdk/authentication/credential-chains?tabs=dac#defaultazurecredential-overview).

### Tooling support

In Visual Studio, you can use Connected Services to configure the default Azure provisioning settings. Select the app host project, right-click on the **Connected Services** node, and select **Azure Resource Provisioning Settings**:

:::image type="content" loc-scope="visual-studio" source="media/azure-resource-provisioning-settings.png" lightbox="media/azure-resource-provisioning-settings.png" alt-text="Visual Studio 2022: .NET Aspire App Host project, Connected Services context menu.":::

This will open a dialog where you can configure the Azure provisioning settings, as shown in the following screenshot:

:::image type="content" loc-scope="visual-studio" source="media/azure-provisioning-settings-dialog.png" lightbox="media/azure-provisioning-settings-dialog.png" alt-text="Visual Studio 2022: Azure Resource Provisioning Settings dialog.":::

### Missing configuration value hints

When the `Azure` configuration section is missing, has missing values, or is invalid, the [.NET Aspire dashboard](../fundamentals/dashboard/overview.md) provides useful hints. For example, consider an app host that's missing the `SubscriptionId` configuration value that's attempting to use an Azure Key Vault resource. The **Resources** page indicates the **State** as **Missing subscription configuration**:

:::image type="content" source="media/resources-kv-missing-subscription.png" alt-text=".NET Aspire dashboard: Missing subscription configuration.":::

Additionally, the **Console logs** display this information as well, consider the following screenshot:

:::image type="content" source="media/console-logs-kv-missing-subscription.png" lightbox="media/console-logs-kv-missing-subscription.png" alt-text=".NET Aspire dashboard: Console logs, missing subscription configuration.":::

## Known limitations

After provisioning Azure resources in this way, you must manually clean up the resources in the Azure portal as .NET Aspire doesn't provide a built-in mechanism to delete Azure resources. The easiest way to achieve this is by deleting the configured resource group. This can be done in the [Azure portal](/azure/azure-resource-manager/management/delete-resource-group?tabs=azure-portal#delete-resource-group) or by using the Azure CLI:

```azurecli
az group delete --name <ResourceGroupName>
```

Replace `<ResourceGroupName>` with the name of the resource group you want to delete. For more information, see [az group delete](/cli/azure/group#az-group-delete).

--------------------------
--------------------------
--------------------------
--------------------------

---
title: .NET Aspire Azure Cosmos DB integration
description: Learn how to install and configure the .NET Aspire Azure Cosmos DB integration to connect to existing Cosmos DB instances or create new instances from .NET with the Azure Cosmos DB emulator.
ms.date: 02/26/2025
uid: dotnet/aspire/azure-cosmos-db-integration
---

# .NET Aspire Azure Cosmos DB integration

[!INCLUDE [includes-hosting-and-client](../includes/includes-hosting-and-client.md)]

[Azure Cosmos DB](https://azure.microsoft.com/services/cosmos-db/) is a fully managed NoSQL database service for modern app development. The .NET Aspire Azure Cosmos DB integration enables you to connect to existing Cosmos DB instances or create new instances from .NET with the Azure Cosmos DB emulator.

## Hosting integration

[!INCLUDE [cosmos-app-host](includes/cosmos-app-host.md)]

### Hosting integration health checks

The Azure Cosmos DB hosting integration automatically adds a health check for the Cosmos DB resource. The health check verifies that the Cosmos DB is running and that a connection can be established to it.

The hosting integration relies on the [📦 AspNetCore.HealthChecks.CosmosDb](https://www.nuget.org/packages/AspNetCore.HealthChecks.CosmosDb) NuGet package.

## Client integration

To get started with the .NET Aspire Azure Cosmos DB client integration, install the [📦 Aspire.Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Aspire.Microsoft.Azure.Cosmos) NuGet package in the client-consuming project, that is, the project for the application that uses the Cosmos DB client. The Cosmos DB client integration registers a <xref:Microsoft.Azure.Cosmos.CosmosClient> instance that you can use to interact with Cosmos DB.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Microsoft.Azure.Cosmos
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Microsoft.Azure.Cosmos"
                  Version="*" />
```

---

### Add Cosmos DB client

In the :::no-loc text="Program.cs"::: file of your client-consuming project, call the <xref:Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.AddAzureCosmosClient*> extension method on any <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> to register a <xref:Azure.Cosmos.CosmosClient> for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureCosmosClient(connectionName: "cosmos-db");
```

> [!TIP]
> The `connectionName` parameter must match the name used when adding the Cosmos DB resource in the app host project. In other words, when you call `AddAzureCosmosDB` and provide a name of `cosmos-db` that same name should be used when calling `AddAzureCosmosClient`. For more information, see [Add Azure Cosmos DB resource](#add-azure-cosmos-db-resource).

You can then retrieve the <xref:Azure.Cosmos.CosmosClient> instance using dependency injection. For example, to retrieve the connection from an example service:

```csharp
public class ExampleService(CosmosClient client)
{
    // Use client...
}
```

For more information on dependency injection, see [.NET dependency injection](/dotnet/core/extensions/dependency-injection).

### Add keyed Cosmos DB client

There might be situations where you want to register multiple `CosmosClient` instances with different connection names. To register keyed Cosmos DB clients, call the <xref:Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.AddKeyedAzureCosmosClient*> method:

```csharp
builder.AddKeyedAzureCosmosClient(name: "mainDb");
builder.AddKeyedAzureCosmosClient(name: "loggingDb");
```

> [!IMPORTANT]
> When using keyed services, it's expected that your Cosmos DB resource configured two named databases, one for the `mainDb` and one for the `loggingDb`.

Then you can retrieve the `CosmosClient` instances using dependency injection. For example, to retrieve the connection from an example service:

```csharp
public class ExampleService(
    [FromKeyedServices("mainDb")] CosmosClient mainDbClient,
    [FromKeyedServices("loggingDb")] CosmosClient loggingDbClient)
{
    // Use clients...
}
```

For more information on keyed services, see [.NET dependency injection: Keyed services](/dotnet/core/extensions/dependency-injection#keyed-services).

### Configuration

The .NET Aspire Azure Cosmos DB integration provides multiple options to configure the connection based on the requirements and conventions of your project.

#### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling the <xref:Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.AddAzureCosmosClient*> method:

```csharp
builder.AddAzureCosmosClient("cosmos-db");
```

Then the connection string is retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "cosmos-db": "AccountEndpoint=https://{account_name}.documents.azure.com:443/;AccountKey={account_key};"
  }
}
```

For more information on how to format this connection string, see the ConnectionString documentation.

#### Use configuration providers

The .NET Aspire Azure Cosmos DB integration supports <xref:Microsoft.Extensions.Configuration>. It loads the <xref:Aspire.Microsoft.Azure.Cosmos.MicrosoftAzureCosmosSettings> from configuration by using the `Aspire:Microsoft:Azure:Cosmos` key. The following snippet is an example of a :::no-loc text="appsettings.json"::: file that configures some of the options:

```json
{
  "Aspire": {
    "Microsoft": {
      "Azure": {
        "Cosmos": {
          "DisableTracing": false,
        }
      }
    }
  }
}
```

For the complete Cosmos DB client integration JSON schema, see [Aspire.Microsoft.Azure.Cosmos/ConfigurationSchema.json](https://github.com/dotnet/aspire/blob/v9.1.0/src/Components/Aspire.Microsoft.Azure.Cosmos/ConfigurationSchema.json).

#### Use inline delegates

Also you can pass the `Action<MicrosoftAzureCosmosSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
builder.AddAzureCosmosClient(
    "cosmos-db",
    static settings => settings.DisableTracing = true);
```

You can also set up the <xref:Microsoft.Azure.Cosmos.CosmosClientOptions?displayProperty=fullName> using the optional `Action<CosmosClientOptions> configureClientOptions` parameter of the `AddAzureCosmosClient` method. For example to set the <xref:Microsoft.Azure.Cosmos.CosmosClientOptions.ApplicationName?displayProperty=nameWithType> user-agent header suffix for all requests issues by this client:

```csharp
builder.AddAzureCosmosClient(
    "cosmosConnectionName",
    configureClientOptions:
        clientOptions => clientOptions.ApplicationName = "myapp");
```

### Client integration health checks

The .NET Aspire Cosmos DB client integration currently doesn't implement health checks, though this may change in future releases.

[!INCLUDE [integration-observability-and-telemetry](../includes/integration-observability-and-telemetry.md)]

#### Logging

The .NET Aspire Azure Cosmos DB integration uses the following log categories:

- `Azure-Cosmos-Operation-Request-Diagnostics`

In addition to getting Azure Cosmos DB request diagnostics for failed requests, you can configure latency thresholds to determine which successful Azure Cosmos DB request diagnostics will be logged. The default values are 100 ms for point operations and 500 ms for non point operations.

```csharp
builder.AddAzureCosmosClient(
    "cosmosConnectionName",
    configureClientOptions:
        clientOptions => {
            clientOptions.CosmosClientTelemetryOptions = new()
            {
                CosmosThresholdOptions = new()
                {
                    PointOperationLatencyThreshold = TimeSpan.FromMilliseconds(50),
                    NonPointOperationLatencyThreshold = TimeSpan.FromMilliseconds(300)
                }
            };
        });
```

#### Tracing

The .NET Aspire Azure Cosmos DB integration will emit the following tracing activities using OpenTelemetry:

- `Azure.Cosmos.Operation`

Azure Cosmos DB tracing is currently in preview, so you must set the experimental switch to ensure traces are emitted.

```csharp
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
```

For more information, see [Azure Cosmos DB SDK observability: Trace attributes](/azure/cosmos-db/nosql/sdk-observability?tabs=dotnet#trace-attributes).

#### Metrics

The .NET Aspire Azure Cosmos DB integration currently doesn't support metrics by default due to limitations with the Azure SDK.

## See also

- [Azure Cosmos DB](https://azure.microsoft.com/services/cosmos-db)
- [.NET Aspire Cosmos DB Entity Framework Core integration](azure-cosmos-db-entity-framework-integration.md)
- [.NET Aspire integrations overview](../fundamentals/integrations-overview.md)
- [.NET Aspire Azure integrations overview](../azure/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)

----------------------------
----------------------------
----------------------------
----------------------------

---
title: .NET Aspire Azure Event Hubs integration
description: This article describes the .NET Aspire Azure Event Hubs integration features and capabilities.
ms.date: 03/10/2025
---

# .NET Aspire Azure Event Hubs integration

[!INCLUDE [includes-hosting-and-client](../includes/includes-hosting-and-client.md)]

[Azure Event Hubs](/azure/event-hubs/event-hubs-about) is a native data-streaming service in the cloud that can stream millions of events per second, with low latency, from any source to any destination. The .NET Aspire Azure Event Hubs integration enables you to connect to Azure Event Hubs instances from your .NET applications.

## Hosting integration

The .NET Aspire [Azure Event Hubs](https://azure.microsoft.com/products/event-hubs) hosting integration models the various Event Hub resources as the following types:

- <xref:Aspire.Hosting.Azure.AzureEventHubsResource>: Represents a top-level Azure Event Hubs resource, used for representing collections of hubs and the connection information to the underlying Azure resource.
- <xref:Aspire.Hosting.Azure.AzureEventHubResource>: Represents a single Event Hub resource.
- <xref:Aspire.Hosting.Azure.AzureEventHubsEmulatorResource>: Represents an Azure Event Hubs emulator as a container resource.
- <xref:Aspire.Hosting.Azure.AzureEventHubConsumerGroupResource>: Represents a consumer group within an Event Hub resource.

To access these types and APIs for expressing them within your [app host](xref:dotnet/aspire/app-host) project, install the [📦 Aspire.Hosting.Azure.EventHubs](https://www.nuget.org/packages/Aspire.Hosting.Azure.EventHubs) NuGet package:

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure.EventHubs
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure.EventHubs"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

### Add an Azure Event Hubs resource

To add an <xref:Aspire.Hosting.Azure.AzureEventHubsResource> to your app host project, call the <xref:Aspire.Hosting.AzureEventHubsExtensions.AddAzureEventHubs*> method providing a name, and then call <xref:Aspire.Hosting.AzureEventHubsExtensions.AddHub*>:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs");
eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

When you add an Azure Event Hubs resource to the app host, it exposes other useful APIs to add Event Hub resources, consumer groups, express explicit provisioning configuration, and enables the use of the Azure Event Hubs emulator. The preceding code adds an Azure Event Hubs resource named `event-hubs` and an Event Hub named `messages` to the app host project. The <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> method passes the connection information to the `ExampleService` project.

> [!IMPORTANT]
> When you call <xref:Aspire.Hosting.AzureEventHubsExtensions.AddAzureEventHubs*>, it implicitly calls <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning(Aspire.Hosting.IDistributedApplicationBuilder)>—which adds support for generating Azure resources dynamically during app startup. The app must configure the appropriate subscription and location. For more information, see [Local provisioning: Configuration](../azure/local-provisioning.md#configuration)

#### Generated provisioning Bicep

If you're new to [Bicep](/azure/azure-resource-manager/bicep/overview), it's a domain-specific language for defining Azure resources. With .NET Aspire, you don't need to write Bicep by-hand, instead the provisioning APIs generate Bicep for you. When you publish your app, the generated Bicep is output alongside the manifest file. When you add an Azure Event Hubs resource, the following Bicep is generated:

:::code language="bicep" source="../snippets/azure/AppHost/event-hubs.module.bicep":::

The preceding Bicep is a module that provisions an Azure Event Hubs resource with the following defaults:

- `location`: The location of the resource group.
- `sku`: The SKU of the Event Hubs resource, defaults to `Standard`.
- `principalId`: The principal ID of the Event Hubs resource.
- `principalType`: The principal type of the Event Hubs resource.
- `event_hubs`: The Event Hubs namespace resource.
- `event_hubs_AzureEventHubsDataOwner`: The Event Hubs resource owner, based on the build-in `Azure Event Hubs Data Owner` role. For more information, see [Azure Event Hubs Data Owner](/azure/role-based-access-control/built-in-roles/analytics#azure-event-hubs-data-owner).
- `messages`: The Event Hub resource.
- `eventHubsEndpoint`: The endpoint of the Event Hubs resource.

The generated Bicep is a starting point and is influenced by changes to the provisioning infrastructure in C#. Customizations to the Bicep file directly will be overwritten, so make changes through the C# provisioning APIs to ensure they are reflected in the generated files.

#### Customize provisioning infrastructure

All .NET Aspire Azure resources are subclasses of the <xref:Aspire.Hosting.Azure.AzureProvisioningResource> type. This type enables the customization of the generated Bicep by providing a fluent API to configure the Azure resources—using the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure})> API. For example, you can configure the `kind`, `consistencyPolicy`, `locations`, and more. The following example demonstrates how to customize the Azure Cosmos DB resource:

:::code language="csharp" source="../snippets/azure/AppHost/Program.ConfigureEventHubsInfra.cs" id="configure":::

The preceding code:

- Chains a call to the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure*> API:
  - The `infra` parameter is an instance of the <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> type.
  - The provisionable resources are retrieved by calling the <xref:Azure.Provisioning.Infrastructure.GetProvisionableResources> method.
  - The single <xref:Azure.Provisioning.EventHubs.EventHubsNamespace> resource is retrieved.
  - The <xref:Azure.Provisioning.EventHubs.EventHubsNamespace.Sku?displayProperty=nameWithType> property is assigned to a new instance of <xref:Azure.Provisioning.EventHubs.EventHubsSku> with a `Premium` name and tier, and a `Capacity` of `7`.
  - The <xref:Azure.Provisioning.EventHubs.EventHubsNamespace.PublicNetworkAccess> property is assigned to `SecuredByPerimeter`.
  - A tag is added to the Event Hubs resource with a key of `ExampleKey` and a value of `Example value`.

There are many more configuration options available to customize the Event Hubs resource resource. For more information, see <xref:Azure.Provisioning.PostgreSql>. For more information, see [`Azure.Provisioning` customization](../azure/integrations-overview.md#azureprovisioning-customization).

### Connect to an existing Azure Event Hubs namespace

You might have an existing Azure Event Hubs namespace that you want to connect to. Instead of representing a new Azure Event Hubs resource, you can add a connection string to the app host. To add a connection to an existing Azure Event Hubs namespace, call the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddConnectionString("event-hubs");

builder.AddProject<Projects.WebApplication>("web")
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

The connection string is configured in the app host's configuration, typically under [User Secrets](/aspnet/core/security/app-secrets), under the `ConnectionStrings` section. The app host injects this connection string as an environment variable into all dependent resources, for example:

```json
{
  "ConnectionStrings": {
    "event-hubs": "{your_namespace}.servicebus.windows.net"
  }
}
```

The dependent resource can access the injected connection string by calling the <xref:Microsoft.Extensions.Configuration.ConfigurationExtensions.GetConnectionString*> method, and passing the connection name as the parameter, in this case `"event-hubs"`. The `GetConnectionString` API is shorthand for `IConfiguration.GetSection("ConnectionStrings")[name]`.

### Add Event Hub consumer group

To add a consumer group, chain a call on an `IResourceBuilder<AzureEventHubsResource>` to the <xref:Aspire.Hosting.AzureEventHubsExtensions.AddConsumerGroup*> API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs");
var messages = eventHubs.AddHub("messages");
messages.AddConsumerGroup("messagesConsumer");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

When you call `AddConsumerGroup`, it configures your `messages` Event Hub resource to have a consumer group named `messagesConsumer`. The consumer group is created in the Azure Event Hubs namespace that's represented by the `AzureEventHubsResource` that you added earlier. For more information, see [Azure Event Hubs: Consumer groups](/azure/event-hubs/event-hubs-features#consumer-groups).

### Add Azure Event Hubs emulator resource

The .NET Aspire Azure Event Hubs hosting integration supports running the Event Hubs resource as an emulator locally, based on the `mcr.microsoft.com/azure-messaging/eventhubs-emulator/latest` container image. This is beneficial for situations where you want to run the Event Hubs resource locally for development and testing purposes, avoiding the need to provision an Azure resource or connect to an existing Azure Event Hubs server.

To run the Event Hubs resource as an emulator, call the <xref:Aspire.Hosting.AzureEventHubsExtensions.RunAsEmulator*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator();

eventHubs.AddHub("messages");

var exampleProject = builder.AddProject<Projects.ExampleProject>()
                            .WithReference(eventHubs);

// After adding all resources, run the app...
```

The preceding code configures an Azure Event Hubs resource to run locally in a container. For more information, see [Azure Event Hubs Emulator](/azure/event-hubs/overview-emulator).

#### Configure Event Hubs emulator container

There are various configurations available for container resources, for example, you can configure the container's ports, data bind mounts, data volumes, or providing a wholistic JSON configuration which overrides everything.

##### Configure Event Hubs emulator container host port

By default, the Event Hubs emulator container when configured by .NET Aspire, exposes the following endpoints:

| Endpoint | Image | Container port | Host port |
|--|--|--|--|
| `emulator` | `mcr.microsoft.com/azure-messaging/eventhubs-emulator/latest` | 5672 | dynamic |

The port that it's listening on is dynamic by default. When the container starts, the port is mapped to a random port on the host machine. To configure the endpoint port, chain calls on the container resource builder provided by the `RunAsEmulator` method and then the <xref:Aspire.Hosting.AzureEventHubsExtensions.WithHostPort(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureEventHubsEmulatorResource},System.Nullable{System.Int32})> as shown in the following example:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator(emulator =>
                       {
                           emulator.WithHostPort(7777);
                       });

eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

The preceding code configures the Azure Event emulator container's existing `emulator` endpoint to listen on port `7777`. The Azure Event emulator container's port is mapped to the host port as shown in the following table:

| Endpoint name | Port mapping (`container:host`) |
|---------------|---------------------------------|
| `emulator`    | `5672:7777`                     |

##### Add Event Hubs emulator with data volume

To add a data volume to the Event Hubs emulator resource, call the <xref:Aspire.Hosting.AzureEventHubsExtensions.WithDataVolume*> method on the Event Hubs emulator resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator(emulator =>
                       {
                           emulator.WithDataVolume();
                       });

eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

The data volume is used to persist the Event Hubs emulator data outside the lifecycle of its container. The data volume is mounted at the `/data` path in the container. A name is generated at random unless you provide a set the `name` parameter. For more information on data volumes and details on why they're preferred over [bind mounts](#add-event-hubs-emulator-with-data-bind-mount), see [Docker docs: Volumes](https://docs.docker.com/engine/storage/volumes).

##### Add Event Hubs emulator with data bind mount

The add a bind mount to the Event Hubs emulator container, chain a call to the <xref:Aspire.Hosting.AzureEventHubsExtensions.WithDataBindMount*> API, as shown in the following example:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator(emulator =>
                       {
                           emulator.WithDataBindMount("/path/to/data");
                       });

eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

[!INCLUDE [data-bind-mount-vs-volumes](../includes/data-bind-mount-vs-volumes.md)]

Data bind mounts rely on the host machine's filesystem to persist the Azure Event Hubs emulator resource data across container restarts. The data bind mount is mounted at the `/path/to/data` path on the host machine in the container. For more information on data bind mounts, see [Docker docs: Bind mounts](https://docs.docker.com/engine/storage/bind-mounts).

##### Configure Event Hubs emulator container JSON configuration

The Event Hubs emulator container runs with a default [_config.json_](https://github.com/Azure/azure-event-hubs-emulator-installer/blob/main/EventHub-Emulator/Config/Config.json) file. You can override this file entirely, or update the JSON configuration with a <xref:System.Text.Json.Nodes.JsonNode> representation of the configuration.

To provide a custom JSON configuration file, call the <xref:Aspire.Hosting.AzureEventHubsExtensions.WithConfigurationFile(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureEventHubsEmulatorResource},System.String)> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator(emulator =>
                       {
                           emulator.WithConfigurationFile("./messaging/custom-config.json");
                       });

eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

The preceding code configures the Event Hubs emulator container to use a custom JSON configuration file located at `./messaging/custom-config.json`. This will be mounted at the `/Eventhubs_Emulator/ConfigFiles/Config.json` path on the container, as a read-only file. To instead override specific properties in the default configuration, call the <xref:Aspire.Hosting.AzureEventHubsExtensions.WithConfiguration(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureEventHubsEmulatorResource},System.Action{System.Text.Json.Nodes.JsonNode})> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var eventHubs = builder.AddAzureEventHubs("event-hubs")
                       .RunAsEmulator(emulator =>
                       {
                           emulator.WithConfiguration(
                               (JsonNode configuration) =>
                               {
                                   var userConfig = configuration["UserConfig"];
                                   var ns = userConfig["NamespaceConfig"][0];
                                   var firstEntity = ns["Entities"][0];
                                   
                                   firstEntity["PartitionCount"] = 5;
                               });
                       });

eventHubs.AddHub("messages");

builder.AddProject<Projects.ExampleService>()
       .WithReference(eventHubs);

// After adding all resources, run the app...
```

The preceding code retrieves the `UserConfig` node from the default configuration. It then updates the first entity's `PartitionCount` to a `5`.

### Hosting integration health checks

The Azure Event Hubs hosting integration automatically adds a health check for the Event Hubs resource. The health check verifies that the Event Hubs is running and that a connection can be established to it.

The hosting integration relies on the [📦 AspNetCore.HealthChecks.Azure.Messaging.EventHubs](https://www.nuget.org/packages/AspNetCore.HealthChecks.Azure.Messaging.EventHubs) NuGet package.

## Client integration

To get started with the .NET Aspire Azure Event Hubs client integration, install the [📦 Aspire.Azure.Messaging.EventHubs](https://www.nuget.org/packages/Aspire.Azure.Messaging.EventHubs) NuGet package in the client-consuming project, that is, the project for the application that uses the Event Hubs client.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Azure.Messaging.EventHubs
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Azure.Messaging.EventHubs"
                  Version="*" />
```

---

### Supported Event Hubs client types

The following Event Hub clients are supported by the library, along with their corresponding options and settings classes:

| Azure client type | Azure options class | .NET Aspire settings class |
|--|--|--|
| <xref:Azure.Messaging.EventHubs.Producer.EventHubProducerClient> | <xref:Azure.Messaging.EventHubs.Producer.EventHubProducerClientOptions> | <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProducerSettings> |
| <xref:Azure.Messaging.EventHubs.Producer.EventHubBufferedProducerClient> | <xref:Azure.Messaging.EventHubs.Producer.EventHubBufferedProducerClientOptions> | <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsBufferedProducerSettings> |
| <xref:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClient> | <xref:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClientOptions> | <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsConsumerSettings> |
| <xref:Azure.Messaging.EventHubs.EventProcessorClient> | <xref:Azure.Messaging.EventHubs.EventProcessorClientOptions> | <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsProcessorSettings> |
| <xref:Microsoft.Azure.EventHubs.PartitionReceiver> | <xref:Azure.Messaging.EventHubs.Primitives.PartitionReceiverOptions> | <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsPartitionReceiverSettings> |

The client types are from the Azure SDK for .NET, as are the corresponding options classes. The settings classes are provided by the .NET Aspire. The settings classes are used to configure the client instances.

### Add an Event Hubs producer client

In the _:::no-loc text="Program.cs":::_ file of your client-consuming project, call the <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzureEventHubProducerClient*> extension method on any <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> to register an <xref:Azure.Messaging.EventHubs.Producer.EventHubProducerClient> for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureEventHubProducerClient(connectionName: "event-hubs");
```

> [!TIP]
> The `connectionName` parameter must match the name used when adding the Event Hubs resource in the app host project. For more information, see [Add an Azure Event Hubs resource](#add-an-azure-event-hubs-resource).

After adding the `EventHubProducerClient`, you can retrieve the client instance using dependency injection. For example, to retrieve your data source object from an example service define it as a constructor parameter and ensure the `ExampleService` class is registered with the dependency injection container:

```csharp
public class ExampleService(EventHubProducerClient client)
{
    // Use client...
}
```

For more information, see:

- [Azure.Messaging.EventHubs documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/README.md) for examples on using the `EventHubProducerClient`.
- [Dependency injection in .NET](/dotnet/core/extensions/dependency-injection) for details on dependency injection.

#### Additional APIs to consider

The client integration provides additional APIs to configure client instances. When you need to register an Event Hubs client, consider the following APIs:

| Azure client type | Registration API |
|--|--|
| <xref:Azure.Messaging.EventHubs.Producer.EventHubProducerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzureEventHubProducerClient*> |
| <xref:Azure.Messaging.EventHubs.Producer.EventHubBufferedProducerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzureEventHubBufferedProducerClient*> |
| <xref:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzureEventHubConsumerClient*> |
| <xref:Azure.Messaging.EventHubs.EventProcessorClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzureEventProcessorClient*> |
| <xref:Microsoft.Azure.EventHubs.PartitionReceiver> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddAzurePartitionReceiverClient*> |

All of the aforementioned APIs include optional parameters to configure the client instances.

### Add keyed Event Hubs producer client

There might be situations where you want to register multiple `EventHubProducerClient` instances with different connection names. To register keyed Event Hubs clients, call the <xref:Microsoft.Extensions.Hosting.AspireServiceBusExtensions.AddKeyedAzureServiceBusClient*> method:

```csharp
builder.AddKeyedAzureEventHubProducerClient(name: "messages");
builder.AddKeyedAzureEventHubProducerClient(name: "commands");
```

> [!IMPORTANT]
> When using keyed services, it's expected that your Event Hubs resource configured two named hubs, one for the `messages` and one for the `commands`.

Then you can retrieve the client instances using dependency injection. For example, to retrieve the clients from a service:

```csharp
public class ExampleService(
    [KeyedService("messages")] EventHubProducerClient messagesClient,
    [KeyedService("commands")] EventHubProducerClient commandsClient)
{
    // Use clients...
}
```

For more information, see [Keyed services in .NET](/dotnet/core/extensions/dependency-injection#keyed-services).

#### Additional keyed APIs to consider

The client integration provides additional APIs to configure keyed client instances. When you need to register a keyed Event Hubs client, consider the following APIs:

| Azure client type | Registration API |
|--|--|
| <xref:Azure.Messaging.EventHubs.Producer.EventHubProducerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddKeyedAzureEventHubProducerClient*> |
| <xref:Azure.Messaging.EventHubs.Producer.EventHubBufferedProducerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddKeyedAzureEventHubBufferedProducerClient*> |
| <xref:Azure.Messaging.EventHubs.Consumer.EventHubConsumerClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddKeyedAzureEventHubConsumerClient*> |
| <xref:Azure.Messaging.EventHubs.EventProcessorClient> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddKeyedAzureEventProcessorClient*> |
| <xref:Microsoft.Azure.EventHubs.PartitionReceiver> | <xref:Microsoft.Extensions.Hosting.AspireEventHubsExtensions.AddKeyedAzurePartitionReceiverClient*> |

All of the aforementioned APIs include optional parameters to configure the client instances.

### Configuration

The .NET Aspire Azure Event Hubs library provides multiple options to configure the Azure Event Hubs connection based on the requirements and conventions of your project. Either a `FullyQualifiedNamespace` or a `ConnectionString` is a required to be supplied.

#### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, provide the name of the connection string when calling `builder.AddAzureEventHubProducerClient()` and other supported Event Hubs clients. In this example, the connection string does not include the `EntityPath` property, so the `EventHubName` property must be set in the settings callback:

```csharp
builder.AddAzureEventHubProducerClient(
    "event-hubs",
    static settings =>
    {
        settings.EventHubName = "MyHub";
    });
```

And then the connection information will be retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

##### Fully Qualified Namespace (FQN)

The recommended approach is to use a fully qualified namespace, which works with the <xref:Aspire.Azure.Messaging.EventHubs.AzureMessagingEventHubsSettings.Credential?displayProperty=nameWithType> property to establish a connection. If no credential is configured, the <xref:Azure.Identity.DefaultAzureCredential> is used.

```json
{
  "ConnectionStrings": {
    "event-hubs": "{your_namespace}.servicebus.windows.net"
  }
}
```

#### Connection string

Alternatively, use a connection string:

```json
{
  "ConnectionStrings": {
    "event-hubs": "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=accesskeyname;SharedAccessKey=accesskey;EntityPath=MyHub"
  }
}
```

### Use configuration providers

The .NET Aspire Azure Event Hubs library supports <xref:Microsoft.Extensions.Configuration?displayProperty=fullName>. It loads the `AzureMessagingEventHubsSettings` and the associated Options, e.g. `EventProcessorClientOptions`, from configuration by using the `Aspire:Azure:Messaging:EventHubs:` key prefix, followed by the name of the specific client in use. For example, consider the _:::no-loc text="appsettings.json":::_ that configures some of the options for an `EventProcessorClient`:

```json
{
  "Aspire": {
    "Azure": {
      "Messaging": {
        "EventHubs": {
          "EventProcessorClient": {
            "EventHubName": "MyHub",
            "ClientOptions": {
              "Identifier": "PROCESSOR_ID"
            }
          }
        }
      }
    }
  }
}
```

For the complete Azure Event Hubs client integration JSON schema, see [Aspire.Azure.Messaging.EventHubs/ConfigurationSchema.json](https://github.com/dotnet/aspire/blob/v9.1.0/src/Components/Aspire.Azure.Messaging.EventHubs/ConfigurationSchema.json).

You can also setup the Options type using the optional `Action<IAzureClientBuilder<EventProcessorClient, EventProcessorClientOptions>> configureClientBuilder` parameter of the `AddAzureEventProcessorClient` method. For example, to set the processor's client ID for this client:

```csharp
builder.AddAzureEventProcessorClient(
    "event-hubs",
    configureClientBuilder: clientBuilder => clientBuilder.ConfigureOptions(
        options => options.Identifier = "PROCESSOR_ID"));
```

[!INCLUDE [integration-observability-and-telemetry](../includes/integration-observability-and-telemetry.md)]

### Logging

The .NET Aspire Azure Event Hubs integration uses the following log categories:

- `Azure.Core`
- `Azure.Identity`

### Tracing

The .NET Aspire Azure Event Hubs integration will emit the following tracing activities using OpenTelemetry:

- `Azure.Messaging.EventHubs.*`

### Metrics

The .NET Aspire Azure Event Hubs integration currently doesn't support metrics by default due to limitations with the Azure SDK for .NET. If that changes in the future, this section will be updated to reflect those changes.

## See also

- [Azure Event Hubs](/azure/event-hubs/)
- [.NET Aspire Azure integrations overview](../azure/integrations-overview.md)
- [.NET Aspire integrations](../fundamentals/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)

------------------
------------------
------------------
------------------

---
title: .NET Aspire Azure Service Bus integration
description: Learn how to install and configure the .NET Aspire Azure Service Bus integration to connect to Azure Service Bus instances from .NET applications.
ms.date: 02/25/2025
---

# .NET Aspire Azure Service Bus integration

[!INCLUDE [includes-hosting-and-client](../includes/includes-hosting-and-client.md)]

[Azure Service Bus](https://azure.microsoft.com/services/service-bus/) is a fully managed enterprise message broker with message queues and publish-subscribe topics. The .NET Aspire Azure Service Bus integration enables you to connect to Azure Service Bus instances from .NET applications.

## Hosting integration

The .NET Aspire [Azure Service Bus](https://azure.microsoft.com/services/service-bus/) hosting integration models the various Service Bus resources as the following types:

- <xref:Aspire.Hosting.Azure.AzureServiceBusResource>: Represents an Azure Service Bus resource.
- <xref:Aspire.Hosting.Azure.AzureServiceBusEmulatorResource>: Represents an Azure Service Bus emulator resource.

To access these types and APIs for expressing them, add the [📦 Aspire.Hosting.Azure.ServiceBus](https://www.nuget.org/packages/Aspire.Hosting.Azure.ServiceBus) NuGet package in the [app host](xref:dotnet/aspire/app-host) project.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure.ServiceBus
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure.ServiceBus"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

### Add Azure Service Bus resource

In your app host project, call <xref:Aspire.Hosting.AzureServiceBusExtensions.AddAzureServiceBus*> to add and return an Azure Service Bus resource builder.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging");

// After adding all resources, run the app...
```

When you add an <xref:Aspire.Hosting.Azure.AzureServiceBusResource> to the app host, it exposes other useful APIs to add queues and topics. In other words, you must add an `AzureServiceBusResource` before adding any of the other Service Bus resources.

> [!IMPORTANT]
> When you call <xref:Aspire.Hosting.AzureServiceBusExtensions.AddAzureServiceBus*>, it implicitly calls <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning*>—which adds support for generating Azure resources dynamically during app startup. The app must configure the appropriate subscription and location. For more information, see [Configuration](../azure/local-provisioning.md#configuration).

#### Generated provisioning Bicep

If you're new to Bicep, it's a domain-specific language for defining Azure resources. With .NET Aspire, you don't need to write Bicep by-hand, instead the provisioning APIs generate Bicep for you. When you publish your app, the generated Bicep is output alongside the manifest file. When you add an Azure Service Bus resource, the following Bicep is generated:

:::code language="bicep" source="../snippets/azure/AppHost/service-bus.module.bicep":::

The preceding Bicep is a module that provisions an Azure Service Bus namespace with the following defaults:

- `sku`: The SKU of the Service Bus namespace. The default is Standard.
- `location`: The location for the Service Bus namespace. The default is the resource group's location.

In addition to the Service Bus namespace, it also provisions an Azure role-based access control (Azure RBAC) built-in role of Azure Service Bus Data Owner. The role is assigned to the Service Bus namespace's resource group. For more information, see [Azure Service Bus Data Owner](/azure/role-based-access-control/built-in-roles/integration#azure-service-bus-data-owner).

#### Customize provisioning infrastructure

All .NET Aspire Azure resources are subclasses of the <xref:Aspire.Hosting.Azure.AzureProvisioningResource> type. This type enables the customization of the generated Bicep by providing a fluent API to configure the Azure resources—using the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure})> API. For example, you can configure the sku, location, and more. The following example demonstrates how to customize the Azure Service Bus resource:

:::code language="csharp" source="../snippets/azure/AppHost/Program.ConfigureServiceBusInfra.cs" id="configure":::

The preceding code:

- Chains a call to the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure*> API:
  - The infra parameter is an instance of the <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> type.
  - The provisionable resources are retrieved by calling the <xref:Azure.Provisioning.Infrastructure.GetProvisionableResources> method.
  - The single <xref:Azure.Provisioning.ServiceBus.ServiceBusNamespace> is retrieved.
  - The <xref:Azure.Provisioning.ServiceBus.ServiceBusNamespace.Sku?displayProperty=nameWithType> created with a <xref:Azure.Provisioning.ServiceBus.ServiceBusSkuTier.Premium?displayProperty=nameWithType>
  - A tag is added to the Service Bus namespace with a key of `ExampleKey` and a value of `Example value`.

There are many more configuration options available to customize the Azure Service Bus resource. For more information, see <xref:Azure.Provisioning.ServiceBus>. For more information, see [Azure.Provisioning customization](../azure/integrations-overview.md#azureprovisioning-customization).

### Connect to an existing Azure Service Bus namespace

You might have an existing Azure Service Bus namespace that you want to connect to. Instead of representing a new Azure Service Bus resource, you can add a connection string to the app host. To add a connection to an existing Azure Service Bus namespace, call the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString*> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddConnectionString("messaging");

builder.AddProject<Projects.WebApplication>("web")
       .WithReference(serviceBus);

// After adding all resources, run the app...
```

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

The connection string is configured in the app host's configuration, typically under [User Secrets](/aspnet/core/security/app-secrets), under the `ConnectionStrings` section. The app host injects this connection string as an environment variable into all dependent resources, for example:

```json
{
    "ConnectionStrings": {
        "messaging": "Endpoint=sb://{namespace}.servicebus.windows.net/;SharedAccessKeyName={key_name};SharedAccessKey={key_value};"
    }
}
```

The dependent resource can access the injected connection string by calling the <xref:Microsoft.Extensions.Configuration.ConfigurationExtensions.GetConnectionString*> method, and passing the connection name as the parameter, in this case `"messaging"`. The `GetConnectionString` API is shorthand for `IConfiguration.GetSection("ConnectionStrings")[name]`.

### Add Azure Service Bus queue

To add an Azure Service Bus queue, call the <xref:Aspire.Hosting.AzureServiceBusExtensions.AddServiceBusQueue*> method on the `IResourceBuilder<AzureServiceBusResource>`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging");
serviceBus.AddServiceBusQueue("queue");

// After adding all resources, run the app...
```

When you call <xref:Aspire.Hosting.AzureServiceBusExtensions.AddServiceBusQueue(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusResource},System.String,System.String)>, it configures your Service Bus resources to have a queue named `queue`. The queue is created in the Service Bus namespace that's represented by the `AzureServiceBusResource` that you added earlier. For more information, see [Queues, topics, and subscriptions in Azure Service Bus](/azure/service-bus-messaging/service-bus-queues-topics-subscriptions).

### Add Azure Service Bus topic and subscription

To add an Azure Service Bus topic, call the <xref:Aspire.Hosting.AzureServiceBusExtensions.AddServiceBusTopic*> method on the `IResourceBuilder<AzureServiceBusResource>`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging");
serviceBus.AddServiceBusTopic("topic");

// After adding all resources, run the app...
```

When you call <xref:Aspire.Hosting.AzureServiceBusExtensions.AddServiceBusTopic(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusResource},System.String,System.String)>, it configures your Service Bus resources to have a topic named `topic`. The topic is created in the Service Bus namespace that's represented by the `AzureServiceBusResource` that you added earlier.

To add a subscription for the topic, call the <xref:Aspire.Hosting.AzureServiceBusExtensions.AddServiceBusSubscription*> method on the `IResourceBuilder<AzureServiceBusTopicResource>` and configure it using the <xref:Aspire.Hosting.AzureServiceBusExtensions.WithProperties*> method:

```csharp
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging");
var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("sub1")
     .WithProperties(subscription =>
     {
         subscription.MaxDeliveryCount = 10;
         subscription.Rules.Add(
             new AzureServiceBusRule("app-prop-filter-1")
             {
                 CorrelationFilter = new()
                 {
                     ContentType = "application/text",
                     CorrelationId = "id1",
                     Subject = "subject1",
                     MessageId = "msgid1",
                     ReplyTo = "someQueue",
                     ReplyToSessionId = "sessionId",
                     SessionId = "session1",
                     SendTo = "xyz"
                 }
             });
     });

// After adding all resources, run the app...
```

The preceding code not only adds a topic and creates and configures a subscription named `sub1` for the topic. The subscription has a maximum delivery count of `10` and a rule named `app-prop-filter-1`. The rule is a correlation filter that filters messages based on the `ContentType`, `CorrelationId`, `Subject`, `MessageId`, `ReplyTo`, `ReplyToSessionId`, `SessionId`, and `SendTo` properties.

For more information, see [Queues, topics, and subscriptions in Azure Service Bus](/azure/service-bus-messaging/service-bus-queues-topics-subscriptions).

### Add Azure Service Bus emulator resource

To add an Azure Service Bus emulator resource, chain a call on an `<IResourceBuilder<AzureServiceBusResource>>` to the <xref:Aspire.Hosting.AzureServiceBusExtensions.RunAsEmulator*> API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging")
                        .RunAsEmulator();

// After adding all resources, run the app...
```

When you call `RunAsEmulator`, it configures your Service Bus resources to run locally using an emulator. The emulator in this case is the [Azure Service Bus Emulator](/azure/service-bus-messaging/overview-emulator). The Azure Service Bus Emulator provides a free local environment for testing your Azure Service Bus apps and it's a perfect companion to the .NET Aspire Azure hosting integration. The emulator isn't installed, instead, it's accessible to .NET Aspire as a container. When you add a container to the app host, as shown in the preceding example with the `mcr.microsoft.com/azure-messaging/servicebus-emulator` image (and the companion `mcr.microsoft.com/azure-sql-edge` image), it creates and starts the container when the app host starts. For more information, see [Container resource lifecycle](../fundamentals/app-host-overview.md#container-resource-lifecycle).

#### Configure Service Bus emulator container

There are various configurations available for container resources, for example, you can configure the container's ports or providing a wholistic JSON configuration which overrides everything.

##### Configure Service Bus emulator container host port

By default, the Service Bus emulator container when configured by .NET Aspire, exposes the following endpoints:

| Endpoint | Image | Container port | Host port |
|--|--|--|--|
| `emulator` | `mcr.microsoft.com/azure-messaging/servicebus-emulator` | 5672 | dynamic |
| `tcp` | `mcr.microsoft.com/azure-sql-edge` | 1433 | dynamic |

The port that it's listening on is dynamic by default. When the container starts, the port is mapped to a random port on the host machine. To configure the endpoint port, chain calls on the container resource builder provided by the `RunAsEmulator` method and then the <xref:Aspire.Hosting.AzureServiceBusExtensions.WithHostPort(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusEmulatorResource},System.Nullable{System.Int32})> as shown in the following example:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator(
                         emulator =>
                         {
                             emulator.WithHostPort(7777);
                         });

// After adding all resources, run the app...
```

The preceding code configures the Service Bus emulator container's existing `emulator` endpoint to listen on port `7777`. The Service Bus emulator container's port is mapped to the host port as shown in the following table:

| Endpoint name | Port mapping (`container:host`) |
|---------------|---------------------------------|
| `emulator`    | `5672:7777`                     |

##### Configure Service Bus emulator container JSON configuration

The Service Bus emulator automatically generates a configration similar to this [_config.json_](https://github.com/Azure/azure-service-bus-emulator-installer/blob/main/ServiceBus-Emulator/Config/Config.json) file from the configured resources. You can override this generated file entirely, or update the JSON configuration with a <xref:System.Text.Json.Nodes.JsonNode> representation of the configuration.

To provide a custom JSON configuration file, call the <xref:Aspire.Hosting.AzureServiceBusExtensions.WithConfigurationFile(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusEmulatorResource},System.String)> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator(
                         emulator =>
                         {
                             emulator.WithConfigurationFile(
                                 path: "./messaging/custom-config.json");
                         });
```

The preceding code configures the Service Bus emulator container to use a custom JSON configuration file located at `./messaging/custom-config.json`. To instead override specific properties in the default configuration, call the <xref:Aspire.Hosting.AzureServiceBusExtensions.WithConfiguration(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.Azure.AzureServiceBusEmulatorResource},System.Action{System.Text.Json.Nodes.JsonNode})> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging").RunAsEmulator(
                         emulator =>
                         {
                             emulator.WithConfiguration(
                                 (JsonNode configuration) =>
                                 {
                                     var userConfig = configuration["UserConfig"];
                                     var ns = userConfig["Namespaces"][0];
                                     var firstQueue = ns["Queues"][0];
                                     var properties = firstQueue["Properties"];
                                     
                                     properties["MaxDeliveryCount"] = 5;
                                     properties["RequiresDuplicateDetection"] = true;
                                     properties["DefaultMessageTimeToLive"] = "PT2H";
                                 });
                         });

// After adding all resources, run the app...
```

The preceding code retrieves the `UserConfig` node from the default configuration. It then updates the first queue's properties to set the `MaxDeliveryCount` to `5`, `RequiresDuplicateDetection` to `true`, and `DefaultMessageTimeToLive` to `2 hours`.

### Hosting integration health checks

The Azure Service Bus hosting integration automatically adds a health check for the Service Bus resource. The health check verifies that the Service Bus is running and that a connection can be established to it.

The hosting integration relies on the [📦 AspNetCore.HealthChecks.AzureServiceBus](https://www.nuget.org/packages/AspNetCore.HealthChecks.AzureServiceBus) NuGet package.

## Client integration

To get started with the .NET Aspire Azure Service Bus client integration, install the [📦 Aspire.Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Aspire.Azure.Messaging.ServiceBus) NuGet package in the client-consuming project, that is, the project for the application that uses the Service Bus client. The Service Bus client integration registers a <xref:Azure.Messaging.ServiceBus.ServiceBusClient> instance that you can use to interact with Service Bus.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Azure.Messaging.ServiceBus
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Azure.Messaging.ServiceBus"
                  Version="*" />
```

---

### Add Service Bus client

In the :::no-loc text="Program.cs"::: file of your client-consuming project, call the <xref:Microsoft.Extensions.Hosting.AspireServiceBusExtensions.AddAzureServiceBusClient*> extension method on any <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> to register a <xref:Azure.Messaging.ServiceBus.ServiceBusClient> for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureServiceBusClient(connectionName: "messaging");
```

> [!TIP]
> The `connectionName` parameter must match the name used when adding the Service Bus resource in the app host project. In other words, when you call `AddAzureServiceBus` and provide a name of `messaging` that same name should be used when calling `AddAzureServiceBusClient`. For more information, see [Add Azure Service Bus resource](#add-azure-service-bus-resource).

You can then retrieve the <xref:Azure.Messaging.ServiceBus.ServiceBusClient> instance using dependency injection. For example, to retrieve the connection from an example service:

```csharp
public class ExampleService(ServiceBusClient client)
{
    // Use client...
}
```

For more information on dependency injection, see [.NET dependency injection](/dotnet/core/extensions/dependency-injection).

### Add keyed Service Bus client

There might be situations where you want to register multiple `ServiceBusClient` instances with different connection names. To register keyed Service Bus clients, call the <xref:Microsoft.Extensions.Hosting.AspireServiceBusExtensions.AddKeyedAzureServiceBusClient*> method:

```csharp
builder.AddKeyedAzureServiceBusClient(name: "mainBus");
builder.AddKeyedAzureServiceBusClient(name: "loggingBus");
```

> [!IMPORTANT]
> When using keyed services, it's expected that your Service Bus resource configured two named buses, one for the `mainBus` and one for the `loggingBus`.

Then you can retrieve the `ServiceBusClient` instances using dependency injection. For example, to retrieve the connection from an example service:

```csharp
public class ExampleService(
    [FromKeyedServices("mainBus")] ServiceBusClient mainBusClient,
    [FromKeyedServices("loggingBus")] ServiceBusClient loggingBusClient)
{
    // Use clients...
}
```

For more information on keyed services, see [.NET dependency injection: Keyed services](/dotnet/core/extensions/dependency-injection#keyed-services).

### Configuration

The .NET Aspire Azure Service Bus integration provides multiple options to configure the connection based on the requirements and conventions of your project.

#### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling the <xref:Microsoft.Extensions.Hosting.AspireServiceBusExtensions.AddAzureServiceBusClient*> method:

```csharp
builder.AddAzureServiceBusClient("messaging");
```

Then the connection string is retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "messaging": "Endpoint=sb://{namespace}.servicebus.windows.net/;SharedAccessKeyName={keyName};SharedAccessKey={key};"
  }
}
```

For more information on how to format this connection string, see the ConnectionString documentation.

#### Use configuration providers

The .NET Aspire Azure Service Bus integration supports <xref:Microsoft.Extensions.Configuration>. It loads the <xref:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings> from configuration by using the `Aspire:Azure:Messaging:ServiceBus` key. The following snippet is an example of a :::no-loc text="appsettings.json"::: file that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Messaging": {
        "ServiceBus": {
          "ConnectionString": "Endpoint=sb://{namespace}.servicebus.windows.net/;SharedAccessKeyName={keyName};SharedAccessKey={key};",
          "DisableTracing": false
        }
      }
    }
  }
}
```

For the complete Service Bus client integration JSON schema, see [Aspire.Azure.Messaging.ServiceBus/ConfigurationSchema.json](https://github.com/dotnet/aspire/blob/v9.1.0/src/Components/Aspire.Azure.Messaging.ServiceBus/ConfigurationSchema.json).

#### Use inline delegates

Also you can pass the `Action<AzureMessagingServiceBusSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
builder.AddAzureServiceBusClient(
    "messaging",
    static settings => settings.DisableTracing = true);
```

You can also set up the <xref:Azure.Messaging.ServiceBus.ServiceBusClientOptions?displayProperty=fullName> using the optional `Action<ServiceBusClientOptions> configureClientOptions` parameter of the `AddAzureServiceBusClient` method. For example to set the <xref:Azure.Messaging.ServiceBus.ServiceBusClientOptions.Identifier?displayProperty=nameWithType> user-agent header suffix for all requests issues by this client:

```csharp
builder.AddAzureServiceBusClient(
    "messaging",
    configureClientOptions:
        clientOptions => clientOptions.Identifier = "myapp");
```

### Client integration health checks

By default, .NET Aspire integrations enable health checks for all services. For more information, see [.NET Aspire integrations overview](../fundamentals/integrations-overview.md).

The .NET Aspire Azure Service Bus integration:

- Adds the health check when <xref:Aspire.Azure.Messaging.ServiceBus.AzureMessagingServiceBusSettings.DisableTracing?displayProperty=nameWithType> is `false`, which attempts to connect to the Service Bus.
- Integrates with the `/health` HTTP endpoint, which specifies all registered health checks must pass for app to be considered ready to accept traffic.

[!INCLUDE [integration-observability-and-telemetry](../includes/integration-observability-and-telemetry.md)]

#### Logging

The .NET Aspire Azure Service Bus integration uses the following log categories:

- `Azure.Core`
- `Azure.Identity`
- `Azure-Messaging-ServiceBus`

In addition to getting Azure Service Bus request diagnostics for failed requests, you can configure latency thresholds to determine which successful Azure Service Bus request diagnostics will be logged. The default values are 100 ms for point operations and 500 ms for non point operations.

```csharp
builder.AddAzureServiceBusClient(
    "messaging",
    configureClientOptions:
        clientOptions => {
            clientOptions.ServiceBusClientTelemetryOptions = new()
            {
                ServiceBusThresholdOptions = new()
                {
                    PointOperationLatencyThreshold = TimeSpan.FromMilliseconds(50),
                    NonPointOperationLatencyThreshold = TimeSpan.FromMilliseconds(300)
                }
            };
        });
```

#### Tracing

The .NET Aspire Azure Service Bus integration will emit the following tracing activities using OpenTelemetry:

- `Message`
- `ServiceBusSender.Send`
- `ServiceBusSender.Schedule`
- `ServiceBusSender.Cancel`
- `ServiceBusReceiver.Receive`
- `ServiceBusReceiver.ReceiveDeferred`
- `ServiceBusReceiver.Peek`
- `ServiceBusReceiver.Abandon`
- `ServiceBusReceiver.Complete`
- `ServiceBusReceiver.DeadLetter`
- `ServiceBusReceiver.Defer`
- `ServiceBusReceiver.RenewMessageLock`
- `ServiceBusSessionReceiver.RenewSessionLock`
- `ServiceBusSessionReceiver.GetSessionState`
- `ServiceBusSessionReceiver.SetSessionState`
- `ServiceBusProcessor.ProcessMessage`
- `ServiceBusSessionProcessor.ProcessSessionMessage`
- `ServiceBusRuleManager.CreateRule`
- `ServiceBusRuleManager.DeleteRule`
- `ServiceBusRuleManager.GetRules`

Azure Service Bus tracing is currently in preview, so you must set the experimental switch to ensure traces are emitted.

```csharp
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
```

For more information, see [Azure Service Bus: Distributed tracing and correlation through Service Bus messaging](/azure/service-bus-messaging/service-bus-end-to-end-tracing).

#### Metrics

The .NET Aspire Azure Service Bus integration currently doesn't support metrics by default due to limitations with the Azure SDK.

## See also

- [Azure Service Bus](https://azure.microsoft.com/services/service-bus)
- [.NET Aspire integrations overview](../fundamentals/integrations-overview.md)
- [.NET Aspire Azure integrations overview](../azure/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)

-----------------
-----------------
-----------------
-----------------

---
title: .NET Aspire Azure OpenAI integration (Preview)
description: Learn how to use the .NET Aspire Azure OpenAI integration.
ms.date: 03/06/2025
---

# .NET Aspire Azure OpenAI integration (Preview)

[!INCLUDE [includes-hosting-and-client](../includes/includes-hosting-and-client.md)]

[Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service) provides access to OpenAI's powerful language and embedding models with the security and enterprise promise of Azure. The .NET Aspire Azure OpenAI integration enables you to connect to Azure OpenAI Service or OpenAI's API from your .NET applications.

## Hosting integration

The .NET Aspire [Azure OpenAI](/azure/ai-services/openai/) hosting integration models Azure OpenAI resources as <xref:Aspire.Hosting.ApplicationModel.AzureOpenAIResource>. To access these types and APIs for expressing them within your [app host](xref:dotnet/aspire/app-host) project, install the [📦 Aspire.Hosting.Azure.CognitiveServices](https://www.nuget.org/packages/Aspire.Hosting.Azure.CognitiveServices) NuGet package:

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure.CognitiveServices
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure.CognitiveServices"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

### Add an Azure OpenAI resource

To add an <xref:Aspire.Hosting.ApplicationModel.AzureOpenAIResource> to your app host project, call the <xref:Aspire.Hosting.AzureOpenAIExtensions.AddAzureOpenAI%2A> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddAzureOpenAI("openai");

builder.AddProject<Projects.ExampleProject>()
       .WithReference(openai);

// After adding all resources, run the app...
```

The preceding code adds an Azure OpenAI resource named `openai` to the app host project. The <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> method passes the connection information to the `ExampleProject` project.

> [!IMPORTANT]
> When you call <xref:Aspire.Hosting.AzureOpenAIExtensions.AddAzureOpenAI%2A>, it implicitly calls <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning(Aspire.Hosting.IDistributedApplicationBuilder)>—which adds support for generating Azure resources dynamically during app startup. The app must configure the appropriate subscription and location. For more information, see [Local provisioning: Configuration](../azure/local-provisioning.md#configuration).

### Add an Azure OpenAI deployment resource

To add an Azure OpenAI deployment resource, call the <xref:Aspire.Hosting.AzureOpenAIExtensions.AddDeployment(Aspire.Hosting.ApplicationModel.IResourceBuilder{Aspire.Hosting.ApplicationModel.AzureOpenAIResource},Aspire.Hosting.ApplicationModel.AzureOpenAIDeployment)> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddAzureOpenAI("openai");
openai.AddDeployment(
    new AzureOpenAIDeployment(
        name: "preview",
        modelName: "gpt-4.5-preview",
        modelVersion: "2025-02-27"));

builder.AddProject<Projects.ExampleProject>()
       .WithReference(openai)
       .WaitFor(openai);

// After adding all resources, run the app...
```

The preceding code:

- Adds an Azure OpenAI resource named `openai`.
- Adds an Azure OpenAI deployment resource named `preview` with a model name of `gpt-4.5-preview`. The model name must correspond to an [available model](/azure/ai-services/openai/concepts/models) in the Azure OpenAI service.

### Generated provisioning Bicep

If you're new to [Bicep](/azure/azure-resource-manager/bicep/overview), it's a domain-specific language for defining Azure resources. With .NET Aspire, you don't need to write Bicep by-hand, instead the provisioning APIs generate Bicep for you. When you publish your app, the generated Bicep provisions an Azure OpenAI resource with standard defaults.

:::code language="bicep" source="../snippets/azure/AppHost/openai.module.bicep":::

The preceding Bicep is a module that provisions an Azure Cognitive Services resource with the following defaults:

- `location`: The location of the resource group.
- `principalType`: The principal type of the Cognitive Services resource.
- `principalId`: The principal ID of the Cognitive Services resource.
- `openai`: The Cognitive Services account resource.
  - `kind`: The kind of the resource, set to `OpenAI`.
  - `properties`: The properties of the resource.
    - `customSubDomainName`: The custom subdomain name for the resource, based on the unique string of the resource group ID.
    - `publicNetworkAccess`: Set to `Enabled`.
    - `disableLocalAuth`: Set to `true`.
  - `sku`: The SKU of the resource, set to `S0`.
- `openai_CognitiveServicesOpenAIContributor`: The Cognitive Services resource owner, based on the build-in `Azure Cognitive Services OpenAI Contributor` role. For more information, see [Azure Cognitive Services OpenAI Contributor](/azure/role-based-access-control/built-in-roles/ai-machine-learning#cognitive-services-openai-contributor).
- `preview`: The deployment resource, based on the `preview` name.
  - `properties`: The properties of the deployment resource.
    - `format`: The format of the deployment resource, set to `OpenAI`.
    - `modelName`: The model name of the deployment resource, set to `gpt-4.5-preview`.
    - `modelVersion`: The model version of the deployment resource, set to `2025-02-27`.
- `connectionString`: The connection string, containing the endpoint of the Cognitive Services resource.

The generated Bicep is a starting point and is influenced by changes to the provisioning infrastructure in C#. Customizations to the Bicep file directly will be overwritten, so make changes through the C# provisioning APIs to ensure they are reflected in the generated files.

### Customize provisioning infrastructure

All .NET Aspire Azure resources are subclasses of the <xref:Aspire.Hosting.Azure.AzureProvisioningResource> type. This enables customization of the generated Bicep by providing a fluent API to configure the Azure resources—using the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure})> API:

:::code language="csharp" source="../snippets/azure/AppHost/Program.ConfigureOpenAIInfra.cs" id="configure":::

The preceding code:

- Chains a call to the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure*> API:
  - The `infra` parameter is an instance of the <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> type.
  - The provisionable resources are retrieved by calling the <xref:Azure.Provisioning.Infrastructure.GetProvisionableResources> method.
  - The single <xref:Azure.Provisioning.CognitiveServices.CognitiveServicesAccount> resource is retrieved.
  - The <xref:Azure.Provisioning.CognitiveServices.CognitiveServicesAccount.Sku?displayProperty=nameWithType> property is assigned to a new instance of <xref:Azure.Provisioning.CognitiveServices.CognitiveServicesSku> with an `E0` name and <xref:Azure.Provisioning.CognitiveServices.CognitiveServicesSkuTier.Enterprise?displayProperty=nameWithType> tier.
  - A tag is added to the Cognitive Services resource with a key of `ExampleKey` and a value of `Example value`.

### Connect to an existing Azure OpenAI service

You might have an existing Azure OpenAI service that you want to connect to. You can chain a call to annotate that your <xref:Aspire.Hosting.ApplicationModel.AzureOpenAIResource> is an existing resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var existingOpenAIName = builder.AddParameter("existingOpenAIName");
var existingOpenAIResourceGroup = builder.AddParameter("existingOpenAIResourceGroup");

var openai = builder.AddAzureOpenAI("openai")
                    .AsExisting(existingOpenAIName, existingOpenAIResourceGroup);

builder.AddProject<Projects.ExampleProject>()
       .WithReference(openai);

// After adding all resources, run the app...
```

For more information on treating Azure OpenAI resources as existing resources, see [Use existing Azure resources](../azure/integrations-overview.md#use-existing-azure-resources).

Alternatively, instead of representing an Azure OpenAI resource, you can add a connection string to the app host. Which is a weakly-typed approach that's based solely on a `string` value. To add a connection to an existing Azure OpenAI service, call the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString%2A> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
    : builder.AddConnectionString("openai");

builder.AddProject<Projects.ExampleProject>()
       .WithReference(openai);

// After adding all resources, run the app...
```

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

The connection string is configured in the app host's configuration, typically under User Secrets, under the `ConnectionStrings` section:

```json
{
  "ConnectionStrings": {
    "openai": "https://{account_name}.openai.azure.com/"
  }
}
```

For more information, see [Add existing Azure resources with connection strings](../azure/integrations-overview.md#add-existing-azure-resources-with-connection-strings).

## Client integration

To get started with the .NET Aspire Azure OpenAI client integration, install the [📦 Aspire.Azure.AI.OpenAI](https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI) NuGet package in the client-consuming project, that is, the project for the application that uses the Azure OpenAI client.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Azure.AI.OpenAI
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Azure.AI.OpenAI"
                  Version="*" />
```

---

### Add an Azure OpenAI client

In the _Program.cs_ file of your client-consuming project, use the <xref:Microsoft.Extensions.Hosting.AspireAzureOpenAIExtensions.AddAzureOpenAIClient(Microsoft.Extensions.Hosting.IHostApplicationBuilder,System.String,System.Action{Aspire.Azure.AI.OpenAI.AzureOpenAISettings},System.Action{Azure.Core.Extensions.IAzureClientBuilder{Azure.AI.OpenAI.AzureOpenAIClient,Azure.AI.OpenAI.AzureOpenAIClientOptions}})> method on any <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> to register an `OpenAIClient` for dependency injection (DI). The `AzureOpenAIClient` is a subclass of `OpenAIClient`, allowing you to request either type from DI. This ensures code not dependent on Azure-specific features remains generic. The `AddAzureOpenAIClient` method requires a connection name parameter.

```csharp
builder.AddAzureOpenAIClient(connectionName: "openai");
```

> [!TIP]
> The `connectionName` parameter must match the name used when adding the Azure OpenAI resource in the app host project. For more information, see [Add an Azure OpenAI resource](#add-an-azure-openai-resource).

After adding the `OpenAIClient`, you can retrieve the client instance using dependency injection:

```csharp
public class ExampleService(OpenAIClient client)
{
    // Use client...
}
```

For more information, see:

- [Azure.AI.OpenAI documentation](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/README.md) for examples on using the `OpenAIClient`.
- [Dependency injection in .NET](/dotnet/core/extensions/dependency-injection) for details on dependency injection.
- [Quickstart: Get started using GPT-35-Turbo and GPT-4 with Azure OpenAI Service](/azure/ai-services/openai/chatgpt-quickstart?pivots=programming-language-csharp).

### Add Azure OpenAI client with registered `IChatClient`

If you're interested in using the <xref:Microsoft.Extensions.AI.IChatClient> interface, with the OpenAI client, simply chain either of the following APIs to the `AddAzureOpenAIClient` method:

- <xref:Microsoft.Extensions.Hosting.AspireOpenAIClientBuilderChatClientExtensions.AddChatClient(Aspire.OpenAI.AspireOpenAIClientBuilder,System.String)>: Registers a singleton `IChatClient` in the services provided by the <xref:Aspire.OpenAI.AspireOpenAIClientBuilder>.
- <xref:Microsoft.Extensions.Hosting.AspireOpenAIClientBuilderChatClientExtensions.AddKeyedChatClient(Aspire.OpenAI.AspireOpenAIClientBuilder,System.String,System.String)>: Registers a keyed singleton `IChatClient` in the services provided by the <xref:Aspire.OpenAI.AspireOpenAIClientBuilder>.

For example, consider the following C# code that adds an `IChatClient` to the DI container:

```csharp
builder.AddAzureOpenAIClient(connectionName: "openai")
       .AddChatClient("deploymentName");
```

Similarly, you can add a keyed `IChatClient` with the following C# code:

```csharp
builder.AddAzureOpenAIClient(connectionName: "openai")
       .AddKeyedChatClient("serviceKey", "deploymentName");
```

For more information on the `IChatClient` and its corresponding library, see [Artificial intelligence in .NET (Preview)](/dotnet/core/extensions/artificial-intelligence).

### Configure Azure OpenAI client settings

The .NET Aspire Azure OpenAI library provides a set of settings to configure the Azure OpenAI client. The  `AddAzureOpenAIClient` method exposes an optional `configureSettings` parameter of type `Action<AzureOpenAISettings>?`. To configure settings inline, consider the following example:

```csharp
builder.AddAzureOpenAIClient(
    connectionName: "openai",
    configureSettings: settings =>
    {
        settings.DisableTracing = true;

        var uriString = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

        settings.Endpoint = new Uri(uriString);
    });
```

The preceding code sets the <xref:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.DisableTracing?displayProperty=nameWithType> property to `true`, and sets the <xref:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.Endpoint?displayProperty=nameWithType> property to the Azure OpenAI endpoint.

### Configure Azure OpenAI client builder options

To configure the <xref:Azure.AI.OpenAI.AzureOpenAIClientOptions> for the client, you can use the <xref:Microsoft.Extensions.Hosting.AspireAzureOpenAIExtensions.AddAzureOpenAIClient%2A> method. This method takes an optional `configureClientBuilder` parameter of type `Action<IAzureClientBuilder<OpenAIClient, AzureOpenAIClientOptions>>?`. Consider the following example:

```csharp
builder.AddAzureOpenAIClient(
    connectionName: "openai",
    configureClientBuilder: clientBuilder =>
    {
        clientBuilder.ConfigureOptions(options =>
        {
            options.UserAgentApplicationId = "CLIENT_ID";
        });
    });
```

The client builder is an instance of the <xref:Azure.Core.Extensions.IAzureClientBuilder`2> type, which provides a fluent API to configure the client options. The preceding code sets the <xref:Azure.AI.OpenAI.AzureOpenAIClientOptions.UserAgentApplicationId?displayProperty=nameWithType> property to `CLIENT_ID`. For more information, see <xref:Microsoft.Extensions.AI.ConfigureOptionsChatClientBuilderExtensions.ConfigureOptions(Microsoft.Extensions.AI.ChatClientBuilder,System.Action{Microsoft.Extensions.AI.ChatOptions})>.

### Add Azure OpenAI client from configuration

Additionally, the package provides the <xref:Microsoft.Extensions.Hosting.AspireConfigurableOpenAIExtensions.AddOpenAIClientFromConfiguration(Microsoft.Extensions.Hosting.IHostApplicationBuilder,System.String)> extension method to register an `OpenAIClient` or `AzureOpenAIClient` instance based on the provided connection string. This method follows these rules:

- If the `Endpoint` attribute is empty or missing, an `OpenAIClient` instance is registered using the provided key, for example, `Key={key};`.
- If the `IsAzure` attribute is `true`, an `AzureOpenAIClient` is registered; otherwise, an `OpenAIClient` is registered, for example, `Endpoint={azure_endpoint};Key={key};IsAzure=true` registers an `AzureOpenAIClient`, while `Endpoint=https://localhost:18889;Key={key}` registers an `OpenAIClient`.
- If the `Endpoint` attribute contains `".azure."`, an `AzureOpenAIClient` is registered; otherwise, an `OpenAIClient` is registered, for example, `Endpoint=https://{account}.azure.com;Key={key};`.

Consider the following example:

```csharp
builder.AddOpenAIClientFromConfiguration("openai");
```

> [!TIP]
> A valid connection string must contain at least an `Endpoint` or a `Key`.

Consider the following example connection strings and whether they register an `OpenAIClient` or `AzureOpenAIClient`:

| Example connection string | Registered client type |
|--|--|
| `Endpoint=https://{account_name}.openai.azure.com/;Key={account_key}` | `AzureOpenAIClient` |
| `Endpoint=https://{account_name}.openai.azure.com/;Key={account_key};IsAzure=false` | `OpenAIClient` |
| `Endpoint=https://{account_name}.openai.azure.com/;Key={account_key};IsAzure=true` | `AzureOpenAIClient` |
| `Endpoint=https://localhost:18889;Key={account_key}` | `OpenAIClient` |

### Add keyed Azure OpenAI clients

There might be situations where you want to register multiple `OpenAIClient` instances with different connection names. To register keyed Azure OpenAI clients, call the <xref:Microsoft.Extensions.Hosting.AspireAzureOpenAIExtensions.AddKeyedAzureOpenAIClient*> method:

```csharp
builder.AddKeyedAzureOpenAIClient(name: "chat");
builder.AddKeyedAzureOpenAIClient(name: "code");
```

> [!IMPORTANT]
> When using keyed services, ensure that your Azure OpenAI resource configures two named connections, one for `chat` and one for `code`.

Then you can retrieve the client instances using dependency injection. For example, to retrieve the clients from a service:

```csharp
public class ExampleService(
    [KeyedService("chat")] OpenAIClient chatClient,
    [KeyedService("code")] OpenAIClient codeClient)
{
    // Use clients...
}
```

For more information, see [Keyed services in .NET](/dotnet/core/extensions/dependency-injection#keyed-services).

### Add keyed Azure OpenAI clients from configuration

The same functionality and rules exist for keyed Azure OpenAI clients as for the nonkeyed clients. You can use the <xref:Microsoft.Extensions.Hosting.AspireConfigurableOpenAIExtensions.AddKeyedOpenAIClientFromConfiguration(Microsoft.Extensions.Hosting.IHostApplicationBuilder,System.String)> extension method to register an `OpenAIClient` or `AzureOpenAIClient` instance based on the provided connection string.

Consider the following example:

```csharp
builder.AddKeyedOpenAIClientFromConfiguration("openai");
```

This method follows the same rules as detailed in the [Add Azure OpenAI client from configuration](#add-azure-openai-client-from-configuration).

### Configuration

The .NET Aspire Azure OpenAI library provides multiple options to configure the Azure OpenAI connection based on the requirements and conventions of your project. Either a `Endpoint` or a `ConnectionString` is required to be supplied.

#### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddAzureOpenAIClient`:

```csharp
builder.AddAzureOpenAIClient("openai");
```

The connection string is retrieved from the `ConnectionStrings` configuration section, and there are two supported formats:

##### Account endpoint

The recommended approach is to use an **Endpoint**, which works with the `AzureOpenAISettings.Credential` property to establish a connection. If no credential is configured, the <xref:Azure.Identity.DefaultAzureCredential> is used.

```json
{
  "ConnectionStrings": {
    "openai": "https://{account_name}.openai.azure.com/"
  }
}
```

For more information, see [Use Azure OpenAI without keys](/azure/developer/ai/keyless-connections?tabs=csharp%2Cazure-cli).

##### Connection string

Alternatively, a custom connection string can be used:

```json
{
  "ConnectionStrings": {
    "openai": "Endpoint=https://{account_name}.openai.azure.com/;Key={account_key};"
  }
}
```

In order to connect to the non-Azure OpenAI service, drop the `Endpoint` property and only set the Key property to set the [API key](https://platform.openai.com/account/api-keys).

#### Use configuration providers

The .NET Aspire Azure OpenAI integration supports <xref:Microsoft.Extensions.Configuration>. It loads the `AzureOpenAISettings` from configuration by using the `Aspire:Azure:AI:OpenAI` key. Example _:::no-loc text="appsettings.json":::_ that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "AI": {
        "OpenAI": {
          "DisableTracing": false
        }
      }
    }
  }
}
```

For the complete Azure OpenAI client integration JSON schema, see [Aspire.Azure.AI.OpenAI/ConfigurationSchema.json](https://github.com/dotnet/aspire/blob/v9.1.0/src/Components/Aspire.Azure.AI.OpenAI/ConfigurationSchema.json).

#### Use inline delegates

You can pass the `Action<AzureOpenAISettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
builder.AddAzureOpenAIClient(
    "openai",
    static settings => settings.DisableTracing = true);
```

You can also set up the OpenAIClientOptions using the optional `Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>> configureClientBuilder` parameter of the `AddAzureOpenAIClient` method. For example, to set the client ID for this client:

```csharp
builder.AddAzureOpenAIClient(
    "openai",
    configureClientBuilder: builder => builder.ConfigureOptions(
        options => options.Diagnostics.ApplicationId = "CLIENT_ID"));
```

[!INCLUDE [integration-observability-and-telemetry](../includes/integration-observability-and-telemetry.md)]

### Logging

The .NET Aspire Azure OpenAI integration uses the following log categories:

- `Azure`
- `Azure.Core`
- `Azure.Identity`

### Tracing

The .NET Aspire Azure OpenAI integration emits tracing activities using OpenTelemetry for operations performed with the `OpenAIClient`.

> [!IMPORTANT]
> Tracing is currently experimental with this integration. To opt-in to it, set either the `OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY` environment variable to `true` or `1`, or call `AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true))` during app startup.

## See also

- [Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service/)
- [.NET Aspire integrations overview](../fundamentals/integrations-overview.md)
- [.NET Aspire Azure integrations overview](../azure/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)

-----------------------
-----------------------
-----------------------
-----------------------

---
title: .NET Aspire Azure AI Search integration
description: Learn how to integrate Azure AI Search with .NET Aspire.
ms.date: 03/07/2025
---

# .NET Aspire Azure AI Search integration

[!INCLUDE [includes-hosting-and-client](../includes/includes-hosting-and-client.md)]

The .NET Aspire Azure AI Search Documents integration enables you to connect to [Azure AI Search](/azure/search/search-what-is-azure-search) (formerly Azure Cognitive Search) services from your .NET applications. Azure AI Search is an enterprise-ready information retrieval system for your heterogeneous content that you ingest into a search index, and surface to users through queries and apps. It comes with a comprehensive set of advanced search technologies, built for high-performance applications at any scale.

## Hosting integration

The .NET Aspire Azure AI Search hosting integration models the Azure AI Search resource as the <xref:Aspire.Hosting.Azure.AzureSearchResource> type. To access this type and APIs for expressing them within your [app host](xref:dotnet/aspire/app-host) project, install the [📦 Aspire.Hosting.Azure.Search](https://www.nuget.org/packages/Aspire.Hosting.Azure.Search) NuGet package:

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Search
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure.Search"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

### Add an Azure AI Search resource

To add an <xref:Aspire.Hosting.Azure.AzureSearchResource> to your app host project, call the <xref:Aspire.Hosting.AzureSearchExtensions.AddAzureSearch*> method providing a name:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var search = builder.AddAzureSearch("search");

builder.AddProject<Projects.ExampleProject>()
       .WithReference(search);

// After adding all resources, run the app...
```

The preceding code adds an Azure AI Search resource named `search` to the app host project. The <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference*> method passes the connection information to the `ExampleProject` project.

> [!IMPORTANT]
> When you call <xref:Aspire.Hosting.AzureSearchExtensions.AddAzureSearch*>, it implicitly calls <xref:Aspire.Hosting.AzureProvisionerExtensions.AddAzureProvisioning(Aspire.Hosting.IDistributedApplicationBuilder)>—which adds support for generating Azure resources dynamically during app startup. The app must configure the appropriate subscription and location. For more information, see Local provisioning: Configuration

#### Generated provisioning Bicep

If you're new to [Bicep](/azure/azure-resource-manager/bicep/overview), it's a domain-specific language for defining Azure resources. With .NET Aspire, you don't need to write Bicep by hand; instead, the provisioning APIs generate Bicep for you. When you publish your app, the generated Bicep is output alongside the manifest file. When you add an Azure AI Search resource, Bicep is generated to provision the search service with appropriate defaults.

:::code language="bicep" source="../snippets/azure/AppHost/search.module.bicep":::

The preceding Bicep is a module that provisions an Azure AI Search service resource with the following defaults:

- `location`: The location parameter of the resource group, defaults to `resourceGroup().location`.
- `principalType`: The principal type parameter of the Azure AI Search resource.
- `principalId`: The principal ID  parameter of the Azure AI Search resource.
- `search`: The resource representing the Azure AI Search service.
  - `properties`: The properties of the Azure AI Search service:
    - `hostingMode`: Is set to `default`.
    - `disableLocalAuth`: Is set to `true`.
    - `partitionCount`: Is set to `1`.
    - `replicaCount`: Is set to `1`.
  - `sku`: Defaults to `basic`.
- `search_SearchIndexDataContributor`: The role assignment for the Azure AI Search index data contributor role. For more information, see [Search Index Data Contributor](/azure/role-based-access-control/built-in-roles/ai-machine-learning#search-index-data-contributor).
- `search_SearchServiceContributor`: The role assignment for the Azure AI Search service contributor role. For more information, see [Search Service Contributor](/azure/role-based-access-control/built-in-roles/ai-machine-learning#search-service-contributor).
- `connectionString`: The connection string for the Azure AI Search service, which is used to connect to the service. The connection string is generated using the `Endpoint` property of the Azure AI Search service.

The generated Bicep is a starting point and is influenced by changes to the provisioning infrastructure in C#. Customizations to the Bicep file directly will be overwritten, so make changes through the C# provisioning APIs to ensure they are reflected in the generated files.

#### Customize provisioning infrastructure

All .NET Aspire Azure resources are subclasses of the <xref:Aspire.Hosting.Azure.AzureProvisioningResource> type. This type enables the customization of the generated Bicep by providing a fluent API to configure the Azure resources—using the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure``1(Aspire.Hosting.ApplicationModel.IResourceBuilder{``0},System.Action{Aspire.Hosting.Azure.AzureResourceInfrastructure})> API. For example, you can configure the search service partitions, replicas, and more:

:::code language="csharp" source="../snippets/azure/AppHost/Program.ConfigureSearchInfra.cs" id="configure":::

The preceding code:

- Chains a call to the <xref:Aspire.Hosting.AzureProvisioningResourceExtensions.ConfigureInfrastructure*> API:
  - The `infra` parameter is an instance of the <xref:Aspire.Hosting.Azure.AzureResourceInfrastructure> type.
  - The provisionable resources are retrieved by calling the <xref:Azure.Provisioning.Infrastructure.GetProvisionableResources> method.
  - The single <xref:Azure.Provisioning.Search.SearchService> resource is retrieved.
    - The <xref:Azure.Provisioning.Search.SearchService.PartitionCount?displayProperty=nameWithType> is set to `6`.
    - The <xref:Azure.Provisioning.Search.SearchService.ReplicaCount?displayProperty=nameWithType> is set to `3`.
    - The <xref:Azure.Provisioning.Search.SearchService.SearchSkuName?displayProperty=nameWithType> is set to <xref:Azure.Provisioning.Search.SearchServiceSkuName.Standard3?displayProperty=nameWithType>.
    - A tag is added to the Cognitive Services resource with a key of `ExampleKey` and a value of `Example value`.

There are many more configuration options available to customize the Azure AI Search resource. For more information, see [`Azure.Provisioning` customization](../azure/integrations-overview.md#azureprovisioning-customization).

### Connect to an existing Azure AI Search service

You might have an existing Azure AI Search service that you want to connect to. You can chain a call to annotate that your <xref:Aspire.Hosting.Azure.AzureSearchResource> is an existing resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var existingSearchName = builder.AddParameter("existingSearchName");
var existingSearchResourceGroup = builder.AddParameter("existingSearchResourceGroup");

var search = builder.AddAzureSearch("search")
                    .AsExisting(existingSearchName, existingSearchResourceGroup);

builder.AddProject<Projects.ExampleProject>()
       .WithReference(search);

// After adding all resources, run the app...
```

For more information on treating Azure AI Search resources as existing resources, see [Use existing Azure resources](../azure/integrations-overview.md#use-existing-azure-resources).

Alternatively, instead of representing an Azure AI Search resource, you can add a connection string to the app host. Which is a weakly-typed approach that's based solely on a `string` value. To add a connection to an existing Azure AI Search service, call the <xref:Aspire.Hosting.ParameterResourceBuilderExtensions.AddConnectionString%2A> method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var search = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSearch("search")
    : builder.AddConnectionString("search");

builder.AddProject<Projects.ExampleProject>()
       .WithReference(search);

// After adding all resources, run the app...
```

[!INCLUDE [connection-strings-alert](../includes/connection-strings-alert.md)]

The connection string is configured in the app host's configuration, typically under User Secrets, under the `ConnectionStrings` section:

```json
{
  "ConnectionStrings": {
    "search": "https://{account_name}.search.azure.com/"
  }
}
```

For more information, see [Add existing Azure resources with connection strings](../azure/integrations-overview.md#add-existing-azure-resources-with-connection-strings).

### Hosting integration health checks

The Azure AI Search hosting integration doesn't currently implement any health checks. This limitation is subject to change in future releases. As always, feel free to [open an issue](https://github.com/dotnet/aspire/issues) if you have any suggestions or feedback.

## Client integration

To get started with the .NET Aspire Azure AI Search Documents client integration, install the [📦 Aspire.Azure.Search.Documents](https://www.nuget.org/packages/Aspire.Azure.Search.Documents) NuGet package in the client-consuming project, that is, the project for the application that uses the Azure AI Search Documents client.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Azure.Search.Documents
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Azure.Search.Documents"
                  Version="*" />
```

---

### Add an Azure AI Search index client

In the _:::no-loc text="Program.cs":::_ file of your client-consuming project, call the <xref:Microsoft.Extensions.Hosting.AspireAzureSearchExtensions.AddAzureSearchClient*> extension method on any <xref:Microsoft.Extensions.Hosting.IHostApplicationBuilder> to register a <xref:Microsoft.Azure.Search.SearchIndexClient> for use via the dependency injection container. The method takes a connection name parameter.

```csharp
builder.AddAzureSearchClient(connectionName: "search");
```

> [!TIP]
> The `connectionName` parameter must match the name used when adding the Azure AI Search resource in the app host project. For more information, see [Add an Azure AI Search resource](#add-an-azure-ai-search-resource).

After adding the `SearchIndexClient`, you can retrieve the client instance using dependency injection. For example, to retrieve the client instance from an example service:

```csharp
public class ExampleService(SearchIndexClient indexClient)
{
    // Use indexClient
}
```

You can also retrieve a `SearchClient` which can be used for querying, by calling the <xref:Azure.Search.Documents.Indexes.SearchIndexClient.GetSearchClient(System.String)> method:

```csharp
public class ExampleService(SearchIndexClient indexClient)
{
    public async Task<long> GetDocumentCountAsync(
        string indexName,
        CancellationToken cancellationToken)
    {
        var searchClient = indexClient.GetSearchClient(indexName);

        var documentCountResponse = await searchClient.GetDocumentCountAsync(
            cancellationToken);

        return documentCountResponse.Value;
    }
}
```

For more information, see:

- Azure AI Search client library for .NET [samples using the `SearchIndexClient`](/azure/search/samples-dotnet).
- [Dependency injection in .NET](/dotnet/core/extensions/dependency-injection) for details on dependency injection.

### Add keyed Azure AI Search index client

There might be situations where you want to register multiple `SearchIndexClient` instances with different connection names. To register keyed Azure AI Search clients, call the <xref:Microsoft.Extensions.Hosting.AspireAzureSearchExtensions.AddKeyedAzureSearchClient*> method:

```csharp
builder.AddKeyedAzureSearchClient(name: "images");
builder.AddKeyedAzureSearchClient(name: "documents");
```

> [!IMPORTANT]
> When using keyed services, it's expected that your Azure AI Search resource configured two named connections, one for the `images` and one for the `documents`.

Then you can retrieve the client instances using dependency injection. For example, to retrieve the clients from a service:

```csharp
public class ExampleService(
    [KeyedService("images")] SearchIndexClient imagesClient,
    [KeyedService("documents")] SearchIndexClient documentsClient)
{
    // Use clients...
}
```

For more information, see [Keyed services in .NET](/dotnet/core/extensions/dependency-injection#keyed-services).

### Configuration

The .NET Aspire Azure AI Search Documents library provides multiple options to configure the Azure AI Search connection based on the requirements and conventions of your project. Either an `Endpoint` or a `ConnectionString` is required to be supplied.

#### Use a connection string

A connection can be constructed from the **Keys and Endpoint** tab with the format `Endpoint={endpoint};Key={key};`. You can provide the name of the connection string when calling `builder.AddAzureSearchClient()`:

```csharp
builder.AddAzureSearchClient("searchConnectionName");
```

The connection string is retrieved from the `ConnectionStrings` configuration section. Two connection formats are supported:

##### Account endpoint

The recommended approach is to use an `Endpoint`, which works with the `AzureSearchSettings.Credential` property to establish a connection. If no credential is configured, the <xref:Azure.Identity.DefaultAzureCredential> is used.

```json
{
  "ConnectionStrings": {
    "search": "https://{search_service}.search.windows.net/"
  }
}
```

##### Connection string

Alternatively, a connection string with key can be used, however; it's not the recommended approach:

```json
{
  "ConnectionStrings": {
    "search": "Endpoint=https://{search_service}.search.windows.net/;Key={account_key};"
  }
}
```

### Use configuration providers

The .NET Aspire Azure AI Search library supports <xref:Microsoft.Extensions.Configuration?displayProperty=fullName>. It loads the `AzureSearchSettings` and `SearchClientOptions` from configuration by using the `Aspire:Azure:Search:Documents` key. Example _:::no-loc text="appsettings.json":::_ that configures some of the options:

```json
{
  "Aspire": {
    "Azure": {
      "Search": {
        "Documents": {
          "DisableTracing": false
        }
      }
    }
  }
}
```

For the complete Azure AI Search Documents client integration JSON schema, see [Aspire.Azure.Search.Documents/ConfigurationSchema.json](https://github.com/dotnet/aspire/blob/v9.1.0/src/Components/Aspire.Azure.Search.Documents/ConfigurationSchema.json).

### Use inline delegates

You can also pass the `Action<AzureSearchSettings> configureSettings` delegate to set up some or all the options inline, for example to disable tracing from code:

```csharp
builder.AddAzureSearchClient(
    "searchConnectionName",
    static settings => settings.DisableTracing = true);
```

You can also set up the <xref:Azure.Search.Documents.SearchClientOptions> using the optional `Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>> configureClientBuilder` parameter of the `AddAzureSearchClient` method. For example, to set the client ID for this client:

```csharp
builder.AddAzureSearchClient(
    "searchConnectionName",
    configureClientBuilder: builder => builder.ConfigureOptions(
        static options => options.Diagnostics.ApplicationId = "CLIENT_ID"));
```

[!INCLUDE [client-integration-health-checks](../includes/client-integration-health-checks.md)]

The .NET Aspire Azure AI Search Documents integration implements a single health check that calls the <xref:Azure.Search.Documents.Indexes.SearchIndexClient.GetServiceStatisticsAsync%2A> method on the `SearchIndexClient` to verify that the service is available.

[!INCLUDE [integration-observability-and-telemetry](../includes/integration-observability-and-telemetry.md)]

### Logging

The .NET Aspire Azure AI Search Documents integration uses the following log categories:

- `Azure`
- `Azure.Core`
- `Azure.Identity`

### Tracing

The .NET Aspire Azure AI Search Documents integration emits tracing activities using OpenTelemetry when interacting with the search service.

## See also

- [Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search)
- [.NET Aspire integrations overview](../fundamentals/integrations-overview.md)
- [.NET Aspire Azure integrations overview](../azure/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)

---------------------
---------------------
---------------------
---------------------

---
title: .NET Aspire Azure Functions integration (Preview)
description: Learn how to integrate Azure Functions with .NET Aspire.
ms.date: 11/13/2024
zone_pivot_groups: dev-environment
---

# .NET Aspire Azure Functions integration (Preview)

[!INCLUDE [includes-hosting](../includes/includes-hosting.md)]

> [!IMPORTANT]
> The .NET Aspire Azure Functions integration is currently in preview and is subject to change.

[Azure Functions](/azure/azure-functions/functions-overview) is a serverless solution that allows you to write less code, maintain less infrastructure, and save on costs. The .NET Aspire Azure Functions integration enables you to develop, debug, and orchestrate an Azure Functions .NET project as part of the app host.

It's expected that you've installed the required Azure tooling:

:::zone pivot="visual-studio"

- [Configure Visual Studio for Azure development with .NET](/dotnet/azure/configure-visual-studio)

:::zone-end
:::zone pivot="vscode"

- [Configure Visual Studio Code for Azure development with .NET](/dotnet/azure/configure-vs-code)

:::zone-end
:::zone pivot="dotnet-cli"

- [Install the Azure Functions Core Tools](/azure/azure-functions/functions-run-local?tabs=isolated-process&pivots=programming-language-csharp#install-the-azure-functions-core-tools)

:::zone-end

## Supported scenarios

The .NET Aspire Azure Functions integration has several key supported scenarios. This section outlines the scenarios and provides details related to the implementation of each approach.

### Supported triggers

The following table lists the supported triggers for Azure Functions in the .NET Aspire integration:

| Trigger | Attribute | Details |
|--|--|--|
| Azure Event Hubs trigger | `EventHubTrigger` | [📦 Aspire.Hosting.Azure.EventHubs](https://www.nuget.org/packages/Aspire.Hosting.Azure.EventHubs) |
| Azure Service Bus trigger | `ServiceBusTrigger` | [📦 Aspire.Hosting.Azure.ServiceBus](https://www.nuget.org/packages/Aspire.Hosting.Azure.ServiceBus) |
| Azure Storage Blobs trigger | `BlobTrigger` | [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage) |
| Azure Storage Queues trigger | `QueueTrigger` | [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage) |
| Azure CosmosDB trigger | `CosmosDbTrigger` | [📦 Aspire.Hosting.Azure.CosmosDB](https://www.nuget.org/packages/Aspire.Hosting.Azure.CosmosDB) |
| HTTP trigger | `HttpTrigger` | Supported without any additional resource dependencies. |
| Timer trigger | `TimerTrigger` | Supported without any additional resource dependencies—relies on implicit host storage. |

> [!IMPORTANT]
> Other Azure Functions triggers and bindings aren't currently supported in the .NET Aspire Azure Functions integration.

### Deployment

Currently, deployment is supported only to containers on Azure Container Apps (ACA) using the SDK container publish function in `Microsoft.Azure.Functions.Worker.Sdk`. This deployment methodology doesn't currently support KEDA-based autoscaling.

#### Configure external HTTP endpoints

To make HTTP triggers publicly accessible, call the <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExternalHttpEndpoints*> API on the <xref:Aspire.Hosting.Azure.AzureFunctionsProjectResource>. For more information, see [Add Azure Functions resource](#add-azure-functions-resource).

## Azure Function project constraints

The .NET Aspire Azure Functions integration has the following project constraints:

- You must target .NET 8.0 or later.
- You must use a .NET 9 SDK.
- It currently only supports .NET workers with the [isolated worker model](/azure/azure-functions/dotnet-isolated-process-guide).
- Requires the following NuGet packages:
  - [📦 Microsoft.Azure.Functions.Worker](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker): Use the `FunctionsApplicationBuilder`.
  - [📦 Microsoft.Azure.Functions.Worker.Sdk](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Sdk): Adds support for `dotnet run` and `azd publish`.
  - [📦 Microsoft.Azure.Functions.Http.AspNetCore](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore): Adds HTTP trigger-supporting APIs.

:::zone pivot="visual-studio"

If you encounter issues with the Azure Functions project, such as:

> There is no Functions runtime available that matches the version specified in the project

In Visual Studio, try checking for an update on the Azure Functions tooling. Open the **Options** dialog, navigate to **Projects and Solutions**, and then select **Azure Functions**. Select the **Check for updates** button to ensure you have the latest version of the Azure Functions tooling:

:::image type="content" source="media/visual-studio-auzre-functions-options.png" alt-text="Visual Studio: Options / Projects and Solutions / Azure Functions.":::

:::zone-end

## Hosting integration

The Azure Functions hosting integration models an Azure Functions resource as the <xref:Aspire.Hosting.Azure.AzureFunctionsProjectResource> (subtype of <xref:Aspire.Hosting.ApplicationModel.ProjectResource>) type. To access this type and APIs that allow you to add it to your [app host](xref:dotnet/aspire/app-host) project install the [📦 Aspire.Hosting.Azure.Functions](https://www.nuget.org/packages/Aspire.Hosting.Azure.Functions) NuGet package.

### [.NET CLI](#tab/dotnet-cli)

```dotnetcli
dotnet add package Aspire.Hosting.Azure.Functions --prerelease
```

### [PackageReference](#tab/package-reference)

```xml
<PackageReference Include="Aspire.Hosting.Azure.Functions"
                  Version="*" />
```

---

For more information, see [dotnet add package](/dotnet/core/tools/dotnet-add-package) or [Manage package dependencies in .NET applications](/dotnet/core/tools/dependencies).

### Add Azure Functions resource

In your app host project, call <xref:Aspire.Hosting.AzureFunctionsProjectResourceExtensions.AddAzureFunctionsProject*> on the `builder` instance to add an Azure Functions resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var functions = builder.AddAzureFunctionsProject<Projects.ExampleFunctions>("functions")
                       .WithExternalHttpEndpoints();

builder.AddProject<Projects.ExampleProject>()
       .WithReference(functions)
       .WaitFor(functions);

// After adding all resources, run the app...
```

When .NET Aspire adds an Azure Functions project resource the app host, as shown in the preceding example, the `functions` resource can be referenced by other project resources. The <xref:Aspire.Hosting.ResourceBuilderExtensions.WithReference%2A> method configures a connection in the `ExampleProject` named `"functions"`. If the Azure Resource was deployed and it exposed an HTTP trigger, its endpoint would be external due to the call to <xref:Aspire.Hosting.ResourceBuilderExtensions.WithExternalHttpEndpoints*>. For more information, see [Reference resources](../fundamentals/app-host-overview.md#reference-resources).

### Add Azure Functions resource with host storage

If you want to modify the default host storage account that the Azure Functions host uses, call the <xref:Aspire.Hosting.AzureFunctionsProjectResourceExtensions.WithHostStorage*> method on the Azure Functions project resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator();

var functions = builder.AddAzureFunctionsProject<Projects.ExampleFunctions>("functions")
                       .WithHostStorage(storage);

builder.AddProject<Projects.ExampleProject>()
       .WithReference(functions)
       .WaitFor(functions);

// After adding all resources, run the app...
```

The preceding code relies on the [📦 Aspire.Hosting.Azure.Storage](https://www.nuget.org/packages/Aspire.Hosting.Azure.Storage) NuGet package to add an Azure Storage resource that runs as an emulator. The `storage` resource is then passed to the `WithHostStorage` API, explicitly setting the host storage to the emulated resource.

> [!NOTE]
> If you're not using the implicit host storage, you must manually assign the `StorageAccountContributor` role to your resource for deployed instances. This role is automatically assigned for the implicitly generated host storage.

### Reference resources in Azure Functions

To reference other Azure resources in an Azure Functions project, chain a call to `WithReference` on the Azure Functions project resource and provide the resource to reference:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

builder.AddAzureFunctionsProject<Projects.ExampleFunctions>("functions")
       .WithHostStorage(storage)
       .WithReference(blobs);

builder.Build().Run();
```

The preceding code adds an Azure Storage resource to the app host and references it in the Azure Functions project. The `blobs` resource is added to the `storage` resource and then referenced by the `functions` resource. The connection information required to connect to the `blobs` resource is automatically injected into the Azure Functions project and enables the project to define a `BlobTrigger` that relies on `blobs` resource.

## See also

- [.NET Aspire integrations](../fundamentals/integrations-overview.md)
- [.NET Aspire GitHub repo](https://github.com/dotnet/aspire)
- [Azure Functions documentation](/azure/azure-functions/functions-overview)
- [.NET Aspire and Functions image gallery sample](/samples/dotnet/aspire-samples/aspire-azure-functions-with-blob-triggers)

---------------------
---------------------
---------------------
---------------------

---
title: Deploy .NET Aspire projects to Azure Container Apps
description: Learn how to use the Azure Developer CLI to deploy .NET Aspire projects to Azure.
ms.date: 06/14/2024
ms.custom: devx-track-extended-azdevcli
---

# Deploy a .NET Aspire project to Azure Container Apps

.NET Aspire projects are designed to run in containerized environments. Azure Container Apps is a fully managed environment that enables you to run microservices and containerized applications on a serverless platform. This article will walk you through creating a new .NET Aspire solution and deploying it to Microsoft Azure Container Apps using the Azure Developer CLI (`azd`). You'll learn how to complete the following tasks:

> [!div class="checklist"]
>
> - Provision an Azure resource group and Container Registry
> - Publish the .NET Aspire projects as container images in Azure Container Registry
> - Provision a Redis container in Azure
> - Deploy the apps to an Azure Container Apps environment
> - View application console logs to troubleshoot application issues

[!INCLUDE [aspire-prereqs](../../includes/aspire-prereqs.md)]

As an alternative to this tutorial and for a more in-depth guide, see [Deploy a .NET Aspire project to Azure Container Apps using `azd` (in-depth guide)](aca-deployment-azd-in-depth.md).

## Deploy .NET Aspire projects with `azd`

With .NET Aspire and Azure Container Apps (ACA), you have a great hosting scenario for building out your cloud-native apps with .NET. We built some great new features into the Azure Developer CLI (`azd`) specific for making .NET Aspire development and deployment to Azure a friction-free experience. You can still use the Azure CLI and/or Bicep options when you need a granular level of control over your deployments. But for new projects, you won't find an easier path to success for getting a new microservice topology deployed into the cloud.

## Create a .NET Aspire project

As a starting point, this article assumes that you've created a .NET Aspire project from the **.NET Aspire Starter Application** template. For more information, see [Quickstart: Build your first .NET Aspire project](../../get-started/build-your-first-aspire-app.md).

### Resource naming

[!INCLUDE [azure-container-app-naming](../../includes/azure-container-app-naming.md)]

## Install the Azure Developer CLI

The process for installing `azd` varies based on your operating system, but it is widely available via `winget`, `brew`, `apt`, or directly via `curl`. To install `azd`, see [Install Azure Developer CLI](/azure/developer/azure-developer-cli/install-azd).

[!INCLUDE [init-workflow](includes/init-workflow.md)]

[!INCLUDE [azd-up-workflow](includes/azd-up-workflow.md)]

[!INCLUDE [test-deployed-app](includes/test-deployed-app.md)]

[!INCLUDE [azd-dashboard](includes/azd-dashboard.md)]

[!INCLUDE [clean-up-resources](../../includes/clean-up-resources.md)]

-----------------------
-----------------------
-----------------------
-----------------------

---
title: Deploy .NET Aspire projects to Azure Container Apps using Visual Studio
description: Learn how to use Bicep, the Azure CLI, and Azure Developer CLI to deploy .NET Aspire projects to Azure using Visual Studio.
ms.date: 06/14/2024
---

# Deploy a .NET Aspire project to Azure Container Apps using Visual Studio

.NET Aspire projects are designed to run in containerized environments. Azure Container Apps is a fully managed environment that enables you to run microservices and containerized applications on a serverless platform. This article will walk you through creating a new .NET Aspire solution and deploying it to Microsoft Azure Container Apps using the Visual Studio. You'll learn how to complete the following tasks:

> [!div class="checklist"]
>
> - Provision an Azure resource group and Container Registry
> - Publish the .NET Aspire projects as container images in Azure Container Registry
> - Provision a Redis container in Azure
> - Deploy the apps to an Azure Container Apps environment
> - View application console logs to troubleshoot application issues

[!INCLUDE [aspire-prereqs](../../includes/aspire-prereqs.md)]

## Create a .NET Aspire project

As a starting point, this article assumes that you've created a .NET Aspire project from the **.NET Aspire Starter Application** template. For more information, see [Quickstart: Build your first .NET Aspire project](../../get-started/build-your-first-aspire-app.md).

### Resource naming

[!INCLUDE [azure-container-app-naming](../../includes/azure-container-app-naming.md)]

### Deploy the app

1. In the solution explorer, right-click on the **.AppHost** project and select **Publish** to open the **Publish** dialog.

1. Select **Azure Container Apps for .NET Aspire** as the publishing target.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-deploy.png" alt-text="A screenshot of the publishing dialog workflow.":::

1. On the **AzDev Environment** step, select your desired **Subscription** and **Location** values and then enter an **Environment name** such as *aspire-vs*. The environment name determines the naming of Azure Container Apps environment resources.

1. Select **Finish** to close the dialog workflow and view the deployment environment summary.

1. Select **Publish** to provision and deploy the resources on Azure. This process may take several minutes to complete. Visual Studio provides status updates on the deployment progress.

1. When the publish completes, Visual Studio displays the resource URLs at the bottom of the environment screen. Use these links to view the various deployed resources. Select the **webfrontend** URL to open a browser to the deployed app.

    :::image type="content" loc-scope="visual-studio" source="../media/visual-studio-deploy-complete.png" alt-text="A screenshot of the completed publishing process and deployed resources.":::

[!INCLUDE [test-deployed-app](includes/test-deployed-app.md)]

[!INCLUDE [azd-dashboard](includes/azd-dashboard.md)]

[!INCLUDE [clean-up-resources](../../includes/clean-up-resources-visual-studio.md)]

----------------------------
----------------------------
----------------------------
----------------------------

---
title: Use .NET Aspire with Application Insights
description: Learn how to send .NET Aspire telemetry to Application Insights.
ms.date: 04/12/2024
ms.topic: how-to
---

# Use Application Insights for .NET Aspire telemetry

Azure Application Insights, a feature of Azure Monitor, excels in Application Performance Management (APM) for live web applications. .NET Aspire projects are designed to use OpenTelemetry for application telemetry. OpenTelemetry supports an extension model to support sending data to different APMs. .NET Aspire uses OTLP by default for telemetry export, which is used by the dashboard during development. Azure Monitor doesn't (yet) support OTLP, so the applications need to be modified to use the Azure Monitor exporter, and configured with the connection string.

To use Application insights, you specify its configuration in the app host project *and* use the [Azure Monitor distro in the service defaults project](#use-the-azure-monitor-distro).

## Choosing how Application Insights is provisioned

.NET Aspire has the capability to provision cloud resources as part of cloud deployment, including Application Insights. In your .NET Aspire project, you can decide if you want .NET Aspire to provision an Application Insights resource when deploying to Azure. You can also select to use an existing Application Insights resource by providing its connection string. The connection information is managed by the resource configuration in the app host project.

### Provisioning Application insights during Azure deployment

With this option, an instance of Application Insights will be created for you when the application is deployed using the Azure Developer CLI (`azd`).

To use automatic provisioning, you specify a dependency in the app host project, and reference it in each project/resource that needs to send telemetry to Application Insights. The steps include:

- Add a Nuget package reference to [Aspire.Hosting.Azure.ApplicationInsights](https://nuget.org/packages/Aspire.Hosting.Azure.ApplicationInsights) in the app host project.

- Update the app host code to use the Application Insights resource, and reference it from each project:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Automatically provision an Application Insights resource
var insights = builder.AddAzureApplicationInsights("MyApplicationInsights");

// Reference the resource from each project 
var apiService = builder.AddProject<Projects.ApiService>("apiservice")
    .WithReference(insights);

builder.AddProject<Projects.Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(insights);

builder.Build().Run();
```

Follow the steps in [Deploy a .NET Aspire project to Azure Container Apps using the Azure Developer CLI (in-depth guide)](./aca-deployment-azd-in-depth.md) to deploy the application to Azure Container Apps. `azd` will create an Application Insights resource as part of the same resource group, and configure the connection string for each container.

### Manual provisioning of Application Insights resource

Application Insights uses a connection string to tell the OpenTelemetry exporter where to send the telemetry data. The connection string is specific to the instance of Application Insights you want to send the telemetry to. It can be found in the Overview page for the application insights instance.

:::image type="content" loc-scope="azure" source="../media/app-insights-connection-string.png" lightbox="../media/app-insights-connection-string.png" alt-text="Connection string placement in the Azure Application Insights portal UI.":::

If you wish to use an instance of Application Insights that you have provisioned manually, then you should use the `AddConnectionString` API in the app host project to tell the projects/containers where to send the telemetry data. The Azure Monitor distro expects the environment variable to be `APPLICATIONINSIGHTS_CONNECTION_STRING`, so that needs to be explicitly set when defining the connection string.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var insights = builder.AddConnectionString(
    "myInsightsResource",
    "APPLICATIONINSIGHTS_CONNECTION_STRING");

var apiService = builder.AddProject<Projects.ApiService>("apiservice")
    .WithReference(insights);

builder.AddProject<Projects.Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(insights);

builder.Build().Run();
```

#### Resource usage during development

When running the .NET Aspire project locally, the preceding code reads the connection string from configuration. As this is a secret, you should store the value in [app secrets](/aspnet/core/security/app-secrets). Right click on the app host project and choose **Manage Secrets** from the context menu to open the secrets file for the app host project. In the file add the key and your specific connection string, the example below is for illustration purposes.

```json
{
  "ConnectionStrings": {
    "myInsightsResource": "InstrumentationKey=12345678-abcd-1234-abcd-1234abcd5678;IngestionEndpoint=https://westus3-1.in.applicationinsights.azure.com"
  }
}
```

> [!NOTE]
> The `name` specified in the app host code needs to match a key inside the `ConnectionStrings` section in the settings file.

#### Resource usage during deployment

When [deploying an Aspire application with Azure Developer CLI (`azd`)](./aca-deployment-azd-in-depth.md), it will recognize the connection string resource and prompt for a value. This enables a different resource to be used for the deployment from the value used for local development.

### Mixed deployment

If you wish to use a different deployment mechanism per execution context, use the appropriate API conditionally. For example, the following code uses a pre-supplied connection at development time, and an automatically provisioned resource at deployment time.

``` csharp
var builder = DistributedApplication.CreateBuilder(args);

var insights = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureApplicationInsights("myInsightsResource")
    : builder.AddConnectionString("myInsightsResource", "APPLICATIONINSIGHTS_CONNECTION_STRING");

var apiService = builder.AddProject<Projects.ApiService>("apiservice")
    .WithReference(insights);

builder.AddProject<Projects.Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(insights);

builder.Build().Run();
```

> [!TIP]
> The preceding code requires you to supply the connection string information in app secrets for development time usage, and will be prompted for the connection string by `azd` at deployment time.

## Use the Azure Monitor distro

To make exporting to Azure Monitor simpler, this example uses the Azure Monitor Exporter Repo. This is a wrapper package around the Azure Monitor OpenTelemetry Exporter package that makes it easier to export to Azure Monitor with a set of common defaults.

Add the following package to the `ServiceDefaults` project, so that it will be included in each of the .NET Aspire services. For more information, see [.NET Aspire service defaults](../../fundamentals/service-defaults.md).

``` xml
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" 
                  Version="*" />
```

Add a using statement to the top of the project.

``` csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;
```

Uncomment the line in `AddOpenTelemetryExporters` to use the Azure Monitor exporter:

```csharp
private static IHostApplicationBuilder AddOpenTelemetryExporters(
    this IHostApplicationBuilder builder)
{
    // Omitted for brevity...

    // Uncomment the following lines to enable the Azure Monitor exporter 
    // (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
    if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    {
        builder.Services.AddOpenTelemetry().UseAzureMonitor();
    }
    return builder;
}
```

It's possible to further customize the Azure Monitor exporter, including customizing the resource name and changing the sampling. For more information, see [Customize the Azure Monitor exporter](/azure/azure-monitor/app/opentelemetry-configuration?tabs=aspnetcore). Using the parameterless version of `UseAzureMonitor()`, will pickup the connection string from the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable, we configured via the app host project.

-------------------
-------------------
-------------------
-------------------