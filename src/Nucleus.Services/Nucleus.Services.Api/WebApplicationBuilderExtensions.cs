// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Personas.Core;
using Nucleus.Domain.Processing;
using Nucleus.Infrastructure.Data.Persistence.Repositories; // Required for CosmosDbArtifactMetadataRepository
using Nucleus.Services.Api.Infrastructure.Artifacts; // Required for LocalFileArtifactProvider
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

namespace Nucleus.Services.Api;

/// <summary>
/// Extension methods for configuring Nucleus services and endpoints.
/// </summary>
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
        services.Configure<List<PersonaConfiguration>>(configuration.GetSection("Personas"));
        services.Configure<GoogleAiOptions>(configuration.GetSection(GoogleAiOptions.SectionName));
        services.Configure<TeamsAdapterConfiguration>(configuration.GetSection("TeamsAdapter")); // For Teams Bot

        // --- Caching --- ADDED
        services.AddMemoryCache();
        logger.LogInformation("Registered IMemoryCache.");

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

        // --- Cosmos DB --- 
        var cosmosEndpointUrl = configuration["ConnectionStrings:cosmosdb_endpoint"]; // Correct key per appsettings
        var cosmosAccountKey = configuration["ConnectionStrings:cosmosdb_key"];     // Correct key per appsettings
        var cosmosDatabaseName = configuration["CosmosDb:DatabaseName"] ?? "NucleusDb";
        var cosmosContainerName = configuration["CosmosDb:ArtifactMetadataContainerName"] ?? "ArtifactMetadata";

        if (!string.IsNullOrWhiteSpace(cosmosEndpointUrl) && !string.IsNullOrWhiteSpace(cosmosAccountKey))
        {
            logger.LogInformation("Cosmos DB configuration found. Registering CosmosClient and Repository.");
            // Use Endpoint+Key authentication
            services.AddSingleton((provider) =>
            {
                var cosmosClientOptions = new CosmosClientOptions
                {
                    // ApplicationRegion = Regions.EastUS, // Optional: Set preferred region
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                // Use key-based auth
                return new CosmosClient(cosmosEndpointUrl, cosmosAccountKey, cosmosClientOptions);
            });

            // Register the repository, explicitly providing db and container names
            services.AddSingleton<IArtifactMetadataRepository>(sp =>
            {
                var cosmosClient = sp.GetRequiredService<CosmosClient>();
                var container = cosmosClient.GetContainer(cosmosDatabaseName, cosmosContainerName);
                return new CosmosDbArtifactMetadataRepository(container, sp.GetRequiredService<ILogger<CosmosDbArtifactMetadataRepository>>());
            });
        }
        else
        {
            logger.LogWarning("Cosmos DB configuration not found. ArtifactMetadataRepository will not be available.");
            // Consider registering a Null/InMemory repository here if needed for fallback
        }

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

        // --- Configure Google AI (Gemini via Mscc.GenerativeAI) ---
        var googleAiOptions = configuration.GetSection(GoogleAiOptions.SectionName).Get<GoogleAiOptions>();
        if (googleAiOptions != null && !string.IsNullOrWhiteSpace(googleAiOptions.ApiKey) && googleAiOptions.ApiKey != "YOUR_API_KEY_HERE")
        {
            logger.LogInformation("Registering Google AI (Gemini via Mscc) client.");
            // Register Google AI Chat Client directly as the extension method doesn't exist
            string modelId = googleAiOptions.ModelId ?? "gemini-1.5-flash"; // Use default if not specified
            services.AddSingleton<IChatClient>(provider => 
                new GeminiChatClient(googleAiOptions.ApiKey!, modelId) // Use non-nullable assertion for ApiKey due to check above
            );
            logger.LogInformation("Registered GeminiChatClient with model '{ModelId}'.", modelId);
            
            // Register our adapter/wrapper if needed (assuming one exists)
            // services.AddSingleton<IAiChatModel, GoogleAiChatAdapter>();
        }
        else
        {
            logger.LogWarning("Google AI (Gemini via Mscc) configuration missing or invalid ApiKey. Service will not be registered.");
        }

        // --- Artifact Providers ---
        logger.LogInformation("Registering Artifact Providers...");
        // Local File Provider (always register for now, reads from configuration)
        services.AddSingleton<IArtifactProvider, LocalFileArtifactProvider>();
        logger.LogInformation("Registered LocalFileArtifactProvider.");

        // TODO: Add other artifact providers (e.g., SharePoint/Graph) based on config

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
        // TODO: Find the correct IBot implementation class name in Nucleus.Adapters.Teams
        // services.AddTransient<IBot, TeamsMessagingBot>(); // Register your bot
        logger.LogInformation("Registered Bot Framework services.");

        services.AddSingleton<IOrchestrationService, OrchestrationService>();
        services.AddSingleton<IPersonaResolver, DefaultPersonaResolver>();
        services.AddSingleton<IActivationChecker, ActivationChecker>(); // ADDED
        services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
        services.AddHostedService<QueuedInteractionProcessorService>(); // Background worker

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
