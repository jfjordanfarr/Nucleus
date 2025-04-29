// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Personas.Core;
using Nucleus.Domain.Processing;
using Nucleus.Infrastructure.Data.Persistence.Repositories; // Required for CosmosDbArtifactMetadataRepository
using Nucleus.Services.Api.Infrastructure; // Corrected namespace
using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory; // ADDED for IMemoryCache
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Nucleus.Abstractions;
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Infrastructure.Messaging; // ADDED
using Nucleus.Infrastructure.Adapters.Teams;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization;
using Microsoft.Extensions.AI; // ADDED for IChatClient
using Nucleus.Infrastructure.Persistence.Configuration; // ADDED for InMemoryPersonaConfigurationProvider

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
        services.Configure<TeamsAdapterConfiguration>(configuration.GetSection("TeamsAdapter")); // For Teams Bot

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

        if (!string.IsNullOrWhiteSpace(cosmosConnectionString) && !string.IsNullOrWhiteSpace(cosmosDatabaseName))
        {
            logger.LogInformation("Cosmos DB connection string found. Configuring Cosmos DB Client and Container.");
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

                // Register CosmosClient as Singleton
                services.AddSingleton(sp => new CosmosClient(cosmosConnectionString, clientOptions));

                // Register Container as Singleton, ensuring Database/Container exist.
                // Note: Blocking on async calls during startup (GetAwaiter().GetResult()) is generally discouraged,
                // but necessary here for synchronous registration. Consider async factories for complex scenarios.
                services.AddSingleton(sp =>
                {
                    var client = sp.GetRequiredService<CosmosClient>();
                    var databaseResponse = client.CreateDatabaseIfNotExistsAsync(cosmosDatabaseName).GetAwaiter().GetResult();
                    // Use /partitionKey based on repository usage of ArtifactMetadata.PartitionKey property
                    var containerResponse = databaseResponse.Database.CreateContainerIfNotExistsAsync(cosmosContainerName, "/partitionKey").GetAwaiter().GetResult();
                    return containerResponse.Container;
                });

                // Register the Cosmos DB implementation
                services.AddSingleton<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
                logger.LogInformation("Registered CosmosDbArtifactMetadataRepository using connection string from Aspire/Configuration.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Cosmos DB from connection string. Falling back to in-memory repository.");
                services.AddSingleton<IArtifactMetadataRepository, InMemoryArtifactMetadataRepository>();
            }
        }
        else
        {
            logger.LogWarning("Cosmos DB connection string ('cosmosdb') or DatabaseName ('CosmosDb:DatabaseName') not found in configuration. Using InMemoryArtifactMetadataRepository.");
            services.AddSingleton<IArtifactMetadataRepository, InMemoryArtifactMetadataRepository>();
        }

        // --- Aspire Service Discovery --- (Configured in Program.cs)

        // --- Messaging (Service Bus / Null) ---
        var serviceBusConnectionString = configuration.GetConnectionString("servicebus");
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            logger.LogInformation("Azure Service Bus connection string found. Configuring Azure Service Bus.");
            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddServiceBusClientWithNamespace(serviceBusConnectionString)
                    .WithCredential(new DefaultAzureCredential());
            });
            // Register the typed publisher
            services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(AzureServiceBusPublisher<>));
            // Register the hosted service for consuming messages
            services.AddHostedService<ServiceBusQueueConsumerService>();

            // Optionally add a smoke test
            if (builder.Environment.IsDevelopment()) // Example: Only run in Dev
            {
                 services.AddHostedService<ServiceBusSmokeTestService>();
            }
        }
        else
        {
            logger.LogWarning("Azure Service Bus connection string not found. Configuring Null Message Queue Publisher.");
            services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(NullMessageQueuePublisher<>));
        }

        // --- Background Task Queue (In-Memory) ---
        logger.LogInformation("Registering In-Memory Background Task Queue and Processor Service.");
        services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
        services.AddHostedService<QueuedInteractionProcessorService>();

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

        // --- Artifact Providers ---
        logger.LogInformation("Registering Artifact Providers...");
        // Register NullArtifactProvider for scenarios where no provider should act (e.g., if no other provider matches a reference type)
        services.AddSingleton<IArtifactProvider, NullArtifactProvider>();
        logger.LogInformation("Registered Artifact Providers (NullArtifactProvider). Ensure specific providers (e.g., for Graph) are registered elsewhere if needed.");

        // --- API Controllers --- 
        services.AddControllers();
        logger.LogInformation("Registered API Controllers.");

        // --- Swagger / OpenAPI --- 
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        logger.LogInformation("Registered Swagger/OpenAPI services.");

        // --- Bot Framework --- 
        logger.LogInformation("Registering Bot Framework services...");
        services.AddHttpClient().AddTransient<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
        services.AddTransient<IBot, TeamsAdapterBot>(); // Register your bot
        logger.LogInformation("Registered Bot Framework services.");

        services.AddSingleton<IOrchestrationService, OrchestrationService>();
        services.AddSingleton<IPersonaResolver, DefaultPersonaResolver>();
        services.AddSingleton<IActivationChecker, ActivationChecker>(); // ADDED
        services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
        services.AddHostedService<QueuedInteractionProcessorService>(); // Background worker

        // Register NullPlatformNotifier as the keyed IPlatformNotifier for PlatformType.Api
        services.AddKeyedSingleton<IPlatformNotifier, NullPlatformNotifier>(PlatformType.Api);

        // --- Register Persona Implementations ---
        // TODO: This should ideally be keyed or dynamically loaded based on config.
        // Register BootstrapperPersona as a keyed service implementing IPersona<EmptyAnalysisData>
        services.AddKeyedSingleton<IPersona<EmptyAnalysisData>, BootstrapperPersona>(NucleusConstants.PersonaKeys.Default);
        logger.LogInformation("Registered keyed BootstrapperPersona.");

        // --- Persona Management (Keyed by PersonaId) ---
        services.AddKeyedScoped<IPersonaManager, DefaultPersonaManager>(NucleusConstants.PersonaKeys.Default, (Func<IServiceProvider, object, DefaultPersonaManager>)((sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<DefaultPersonaManager>>();
            var personaConfigs = sp.GetRequiredService<IOptions<List<PersonaConfiguration>>>();
            var metadataRepo = sp.GetRequiredService<IArtifactMetadataRepository>();
            var persona = sp.GetRequiredService<IPersona<EmptyAnalysisData>>(); // Corrected: Use GetRequiredService
            var artifactProviders = sp.GetRequiredService<IEnumerable<IArtifactProvider>>(); // Resolve artifact providers

            // Pass the resolved generic persona AS the non-generic IPersona, plus the providers
            return new DefaultPersonaManager(logger, sp, personaConfigs, metadataRepo, persona, artifactProviders);
        }));
        logger.LogInformation("Successfully registered FACTORY for IPersonaManager with key: {Key} (Scoped)", NucleusConstants.PersonaKeys.Default);

        // --- Processing Services ---
        logger.LogInformation("Registering Processing Services...");
        services.AddSingleton<IPersonaConfigurationProvider, InMemoryPersonaConfigurationProvider>(); // ADDED
        services.AddScoped<IOrchestrationService, OrchestrationService>(); // Already exists
        services.AddScoped<IPersonaRuntime, PersonaRuntime>(); // Already exists
        logger.LogInformation("Registered Processing Services (IOrchestrationService, IPersonaRuntime, IPersonaConfigurationProvider).");

        // --- Messaging (Service Bus / In-Memory) ---
        // Check if Service Bus is configured for messaging
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

        // Map the Bot Framework messaging endpoint
        app.MapPost("/api/messages", async (HttpRequest req, HttpResponse res, IBotFrameworkHttpAdapter adapter, IBot bot, ILogger<Program> logger) =>
        {
            logger.LogInformation("Received POST request on /api/messages endpoint.");
            await adapter.ProcessAsync(req, res, bot);
        });
        app.Logger.LogInformation("Mapped Bot Framework endpoint at /api/messages.");

        // Map Aspire default endpoints (e.g., health checks)
        app.MapDefaultEndpoints();
        app.Logger.LogInformation("Mapped Aspire default endpoints.");

        app.Logger.LogInformation("Nucleus endpoint mapping complete.");
        return app;
    }
}
