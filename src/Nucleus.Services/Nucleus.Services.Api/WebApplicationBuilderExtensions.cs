// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization;
using Microsoft.Extensions.AI; 
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Personas.Core;
using Nucleus.Domain.Personas.Core.Interfaces;
using Nucleus.Domain.Personas.Core.Strategies;
using Nucleus.Domain.Processing;
using Nucleus.Infrastructure.Data.Persistence;
using Nucleus.Infrastructure.Data.Persistence.Repositories;
using Nucleus.Infrastructure.Providers;
using Nucleus.Services.Api.Configuration;
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Infrastructure;
using Nucleus.Services.Api.Infrastructure.Messaging;
using Nucleus.Services.Shared;
using Nucleus.Services.Shared.Extraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nucleus.Services.Api;

/// <summary>
/// Adds Nucleus specific services to the dependency injection container.
/// </summary>
/// <remarks>
/// This includes core domain services, repositories, AI client registration,
/// messaging components, artifact providers, and persona managers.
/// See: <seealso cref="../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
/// </remarks>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Nucleus specific services to the dependency injection container.
    /// </summary>
    public static WebApplicationBuilder AddNucleusServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;
        var configuration = builder.Configuration;
        // Get a logger
        using var loggerFactory = LoggerFactory.Create(lb => lb.AddConfiguration(configuration.GetSection("Logging")).AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Registering Nucleus services...");

        // === Configuration ===
        // Note: Configuration sources like AddJsonFile, AddEnvironmentVariables are typically added directly to builder.Configuration in Program.cs
        // We assume they are already configured before this method is called.
        services.Configure<List<PersonaConfiguration>>(configuration.GetSection(NucleusConstants.ConfigurationKeys.PersonasSection));
        services.Configure<GoogleAiOptions>(configuration.GetSection(GoogleAiOptions.SectionName));
        // Removed TeamsAdapterConfiguration binding as it's adapter-specific

        // --- Caching --- ADDED
        services.AddMemoryCache();
        logger.LogInformation("Registered IMemoryCache.");

        // --- AI Services ---
        logger.LogInformation("Configuring AI Services...");
        var googleAiSection = configuration.GetSection("AI:GoogleAI");
        if (googleAiSection.Exists())
        {
            var apiKey = googleAiSection["ApiKey"];
            var modelId = googleAiSection["ModelId"];
            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR_API_KEY_HERE" && !string.IsNullOrWhiteSpace(modelId))
            {
                logger.LogInformation("Registering GeminiChatClient (Mscc.GenerativeAI.Microsoft) as IChatClient (Model: {ModelId})", modelId);
                // Instantiate GeminiChatClient directly and register as singleton for IChatClient
                services.AddSingleton<IChatClient>(provider => 
                    new GeminiChatClient(apiKey!, modelId) // Use non-nullable assertion due to check above
                );
            }
            else
            {
                logger.LogWarning("Google AI configuration found, but ApiKey or ModelId is missing or placeholder. Skipping IChatClient registration.");
                // Consider registering a NullChatClient or throwing an error if an AI client is strictly required
                // services.AddSingleton<IChatClient, NullChatClient>(); // Example fallback
            }
        }
        else
        {
             logger.LogWarning("AI:GoogleAI configuration section not found. Skipping IChatClient registration.");
             // Consider registering a NullChatClient or throwing an error if an AI client is strictly required
             // services.AddSingleton<IChatClient, NullChatClient>(); // Example fallback
        }

        // --- Persistence (Cosmos DB / In-Memory) ---
        string? cosmosConnectionString = configuration.GetConnectionString("cosmosdb"); // Get Aspire connection string
        string? cosmosDatabaseName = configuration["CosmosDb:DatabaseName"]; // Keep using existing config for names
        string cosmosContainerName = configuration.GetValue<string>("CosmosDb:ArtifactMetadataContainerName", CosmosDbArtifactMetadataRepository.ArtifactMetadataContainerName); // Default from constant

        // --- DIAGNOSTIC LOGGING --- (Added for Aspire test debugging)
        logger.LogInformation("Cosmos Config Check: ConnectionString='{ConnStr}', DatabaseName='{DbName}'", 
            string.IsNullOrWhiteSpace(cosmosConnectionString) ? "<NULL_OR_EMPTY>" : "<PRESENT>", // Don't log sensitive connection string
            cosmosDatabaseName ?? "<NULL>");
        // --- END DIAGNOSTIC LOGGING ---

        if (!string.IsNullOrWhiteSpace(cosmosConnectionString) && !string.IsNullOrWhiteSpace(cosmosDatabaseName))
        {
            logger.LogInformation("Cosmos DB connection string and database name found. Configuring Cosmos DB Client and Repositories.");
            try
            {
                // <seealso cref="../../../../Docs/Architecture/04_ARCHITECTURE_DATABASE.md"/>
                // Use custom serializer options matching Aspire defaults
                CosmosClientOptions clientOptions = new()
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };

                // Register CosmosClient as Singleton using a factory to ensure proper disposal
                services.AddSingleton<CosmosClient>(sp => 
                {
                    // This factory function will be called by the DI container
                    // The container will manage the disposal of the returned CosmosClient
                    return new CosmosClient(cosmosConnectionString, clientOptions);
                });
                
                // Register the database instance using the registered client
                services.AddSingleton<Database>(sp => 
                {
                    var client = sp.GetRequiredService<CosmosClient>();
                    return client.GetDatabase(cosmosDatabaseName);
                });

                // Register the container instance using the registered database
                services.AddSingleton<Container>(sp => 
                {
                    var database = sp.GetRequiredService<Database>();
                    var containerName = configuration.GetValue<string>("CosmosDb:ArtifactMetadataContainerName", CosmosDbArtifactMetadataRepository.ArtifactMetadataContainerName);
                    if (string.IsNullOrWhiteSpace(containerName))
                    {
                        throw new InvalidOperationException("Cosmos DB container name ('CosmosDb:ArtifactMetadataContainerName') is missing in configuration.");
                    }
                    // Consider adding logic here to create the container if it doesn't exist
                    // return database.CreateContainerIfNotExistsAsync(containerName, "/id").GetAwaiter().GetResult().Container;
                    return database.GetContainer(containerName);
                });

                // Register the production repository implementation (depends on Container)
                services.AddSingleton<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
                logger.LogInformation("Registered CosmosClient, Database, Container, and CosmosDbArtifactMetadataRepository using database '{DatabaseName}'.", cosmosDatabaseName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Cosmos DB from connection string. Artifact metadata repository will be unavailable.");
                // DO NOT register InMemoryArtifactMetadataRepository here. Fail explicitly.
            }
        }
        else
        {
            logger.LogWarning("Cosmos DB connection string ('cosmosdb') or DatabaseName ('CosmosDb:DatabaseName') not found in configuration. Cosmos DB dependent repositories will be unavailable.");
            // Removed: services.AddSingleton<IArtifactMetadataRepository, InMemoryArtifactMetadataRepository>();
            // Removed: services.AddSingleton<IPersonaDefinitionRepository, InMemoryPersonaDefinitionRepository>();
            // DO NOT register InMemory repositories here. Fail explicitly if required config is missing.
        }

        // --- Aspire Service Discovery --- (Configured in Program.cs)

        // --- Background Task Queue (In-Memory) ---
        logger.LogInformation("Registering Background Task Processor Service.");
        services.AddSingleton<IBackgroundTaskQueue, ServiceBusBackgroundTaskQueue>();
        // The QueuedInteractionProcessorService is added via AddNucleusDomainProcessing()
        // services.AddHostedService<QueuedInteractionProcessorService>(); // REMOVED - Added by AddNucleusDomainProcessing

        // --- Authentication (Example: Microsoft Entra ID for downstream APIs / Graph) ---
        // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
        // logger.LogInformation("Configured Microsoft Identity Web API Authentication.");

        // --- Authorization ---
        // services.AddAuthorization();

        // --- Nucleus Domain Services (Delegated to AddProcessingServices in Program.cs) ---
        // builder.Services.AddProcessingServices(); // DO NOT CALL HERE - Called in Program.cs

        // --- Core AI and Artifact Services ---
        logger.LogInformation("Registering Core AI and Artifact Services...");

        // --- Content Extractors ---
        // Register multiple implementations. Consumers can inject IEnumerable<IContentExtractor>
        // to find the appropriate one based on SupportedMimeTypes.
        // See: [ARCHITECTURE_PROCESSING_INTERFACES.md](../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_INTERFACES.md)
        services.AddSingleton<IContentExtractor, PlainTextContentExtractor>();
        services.AddSingleton<IContentExtractor, HtmlContentExtractor>();
        logger.LogInformation("Registered Content Extractors.");

        // --- Orchestration ---
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddScoped<ActivationChecker>();
        logger.LogInformation("Registered Orchestration Service.");

        // --- Agentic Strategy Handlers ---
        // See: [Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_RUNTIME.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_RUNTIME.md:0:0-0:0)
        // Agentic strategy details are covered within: [Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md:0:0-0:0)
        services.AddScoped<IAgenticStrategyHandler, EchoAgenticStrategyHandler>(); // Default/Example Handler
        logger.LogInformation("Registered Agentic Strategy Handlers.");

        // --- Background Services ---
        services.AddHostedService<QueuedInteractionProcessorService>();

        // --- Artifact Providers ---
        logger.LogInformation("Registering Artifact Providers...");
        // Register all implementations of IArtifactProvider
        services.AddScoped<IArtifactProvider, NullArtifactProvider>();
        services.AddScoped<IArtifactProvider, ConsoleArtifactProvider>();
        // The OrchestrationService depends on IArtifactProvider[], which DI will resolve
        // by collecting all registered IArtifactProvider services.
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        logger.LogInformation("Registered IOrchestrationService with its dependencies (including IArtifactProvider implementations).");

        // --- Data Persistence (Repositories) ---
        logger.LogInformation("Registering Data Persistence Services (Repositories)...");
        services.AddScoped<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
        services.AddScoped<IPersonaKnowledgeRepository, CosmosDbPersonaKnowledgeRepository>();
        services.AddScoped<IPersonaConfigurationProvider, ConfigurationPersonaConfigurationProvider>(); 
        logger.LogInformation("Registered Repositories (IArtifactMetadataRepository, IPersonaKnowledgeRepository, IPersonaConfigurationProvider). Using Configuration provider for Personas.");

        // --- AI Services --- DEPRECATED BLOCK, Needs Review/Removal ---
        // ...

        // --- API Controllers --- 
        services.AddControllers();
        logger.LogInformation("Registered API Controllers.");

        // --- Swagger / OpenAPI --- 
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        logger.LogInformation("Registered Swagger/OpenAPI services.");

        // --- Bot Framework Integration Removed ---
        // Removed: services.AddHttpClient().AddTransient<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
        // Removed: services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
        // Removed: services.AddTransient<IBot, TeamsAdapterBot>(); // Register your bot

        // Default Platform Notifier - Reverted to Null as TeamsNotifier is adapter-specific
        services.AddSingleton<IPlatformNotifier, NullPlatformNotifier>();

        services.AddKeyedSingleton<IPlatformNotifier, NullPlatformNotifier>(PlatformType.Api);

        // --- Processing Services ---
        logger.LogInformation("Registering Processing Services...");
        services.AddScoped<IOrchestrationService, OrchestrationService>(); // Already exists
        logger.LogInformation("Registered Processing Services (IOrchestrationService, IPersonaRuntime). Configuration provider must be added separately.");

        // --- Persona Runtime and Strategies ---
        services.AddScoped<IPersonaRuntime, PersonaRuntime>();
        // TODO: Dynamically discover and register strategy handlers from assemblies?
        // For now, register known ones:
        services.AddScoped<IAgenticStrategyHandler, EchoAgenticStrategyHandler>();
        // services.AddScoped<IPersonaStrategyFactory, PersonaStrategyFactory>(); // Obsolete - PersonaRuntime uses injected IEnumerable<IAgenticStrategyHandler>

        // --- Messaging (Service Bus / In-Memory) ---
        // Check if Service Bus is configured for messaging
        string? serviceBusConnectionString = configuration.GetConnectionString("servicebus"); // Ensure this is the only declaration in this scope
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            logger.LogInformation("Service Bus connection string found. Registering Azure Service Bus client and related services.");
            
            // *** ADDED: Register ServiceBusClient using Aspire extension ***
            // This correctly handles connection strings for both cloud and emulators.
            builder.AddAzureServiceBusClient("servicebus"); // Matches resource name in AppHost

            // Register publishers and consumers that DEPEND ON ServiceBusClient
            services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(AzureServiceBusPublisher<>));
            services.AddSingleton<IBackgroundTaskQueue, ServiceBusBackgroundTaskQueue>();
            // TODO: Review if ServiceBusQueueConsumerService is still needed or if BackgroundTaskQueue handles consumption.
            // services.AddHostedService<ServiceBusQueueConsumerService>(); // Potentially replace/remove
            
            // Add smoke test if SB is configured
            services.AddHostedService<ServiceBusSmokeTestService>(); 
        }
        else
        {
            logger.LogWarning("Service Bus connection string 'servicebus' not found. Registering Null publishers/queues.");
            services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(NullMessageQueuePublisher<>));
            services.AddSingleton<IBackgroundTaskQueue, NullBackgroundTaskQueue>(); // Now resolves correctly
            // No consumer or smoke test needed if Service Bus is not configured
        }

        logger.LogInformation("Nucleus services registration complete.");
        return builder;
    }

    /// <summary>
    /// Maps Nucleus specific endpoints.
    /// </summary>
    public static WebApplication MapNucleusEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.Logger.LogInformation("Swagger UI enabled for Development environment.");
        }

        app.UseHttpsRedirection();

        // Enable Authentication and Authorization if configured
        // app.UseAuthentication();
        // app.UseAuthorization();

        // Map standard API controllers
        app.MapControllers();
        app.Logger.LogInformation("Mapped API controllers.");

        // --- Bot Framework Endpoint Removed ---
        // Removed: app.MapBotFramework("/api/messages");

        // Map Aspire default endpoints (e.g., health checks)
        app.MapDefaultEndpoints();
        app.Logger.LogInformation("Mapped Aspire default endpoints.");

        // --- API Endpoints ---
        var apiGroup = app.MapGroup("/api/v1");

        // Interaction Endpoint (Primary endpoint for adapters)
        // REMOVED: Minimal API mapping - Handled by InteractionController
        /*
        apiGroup.MapPost("/interactions", async (AdapterRequest request, IOrchestrationService orchestrationService, ILogger<Program> logger) =>
        {
            // TODO: Add Authentication/Authorization
            logger.LogInformation("Received interaction request: {RequestType}", request.InteractionType);
            // TODO: Implement proper handling based on InteractionType
            var result = await orchestrationService.ProcessInteractionAsync(request);
            return Results.Ok(result);
        })
        .WithName("ProcessInteraction")
        .WithSummary("Processes an interaction request from an adapter.")
        .WithOpenApi();
        */

        // TODO: Add other API endpoints as needed (e.g., status checks, configuration)

        app.Logger.LogInformation("Nucleus endpoint mapping complete.");
        return app;
    }
}
