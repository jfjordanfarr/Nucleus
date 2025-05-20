// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Azure Core types (e.g. TokenCredential)
using Azure.Core; 
// Azure Identity types (e.g. DefaultAzureCredential)
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models; // For OpenApiInfo
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Adapters; // For IPlatformNotifier
using Nucleus.Abstractions.Extraction;
using Nucleus.Abstractions.Models;   // For PlatformType
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Personas.Core;
using Nucleus.Domain.Personas.Core.Interfaces;
using Nucleus.Domain.Personas.Core.Strategies;
using Nucleus.Domain.Processing;
using Nucleus.Infrastructure.Adapters.Local;
using Nucleus.Infrastructure.Data.Persistence.Repositories;
using Nucleus.Infrastructure.Messaging;
using Nucleus.Infrastructure.Providers.ContentExtraction;
using Nucleus.Services.Api.Configuration;
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Endpoints;
using Nucleus.Services.Api.Infrastructure; // For NullArtifactProvider
using Nucleus.Services.Api.Infrastructure.Messaging; // For NullPlatformNotifier
using System;
using System.Collections.Generic;
using System.Linq;

// Ensure Swashbuckle.AspNetCore package is referenced in .csproj for AddSwaggerGen
// Ensure Microsoft.AspNetCore.Authentication.JwtBearer package is referenced if JWT is used
// Ensure Microsoft.Identity.Web package is referenced if Azure AD auth is used

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
        var logger = loggerFactory.CreateLogger("Nucleus.Services.Api.WebApplicationBuilderExtensions"); 

        logger.LogInformation("Registering Nucleus services in AddNucleusServices...");

        // === Configuration Bindings ===
        services.Configure<List<PersonaConfiguration>>(configuration.GetSection(NucleusConstants.ConfigurationKeys.PersonasSection));
        services.Configure<GoogleAiOptions>(configuration.GetSection(GoogleAiOptions.SectionName));
        
        // --- Caching ---
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
                services.AddSingleton<IChatClient>(provider => new GeminiChatClient(apiKey!, modelId));
            }
            else
            {
                logger.LogWarning("Google AI configuration found, but ApiKey or ModelId is missing or placeholder. Skipping IChatClient registration.");
            }
        }
        else
        {
             logger.LogWarning("AI:GoogleAI configuration section not found. Skipping IChatClient registration.");
        }

        // --- Content Extraction Services ---
        services.AddKeyedSingleton<IContentExtractor, HtmlContentExtractor>(NucleusConstants.ExtractorKeys.Html);
        services.AddKeyedSingleton<IContentExtractor, PlainTextContentExtractor>(NucleusConstants.ExtractorKeys.PlainText);
        // Also register them without keys to be resolvable via IEnumerable<IContentExtractor>
        services.AddSingleton<IContentExtractor, HtmlContentExtractor>();
        services.AddSingleton<IContentExtractor, PlainTextContentExtractor>();
        logger.LogInformation("Registered Content Extraction Services (Html, PlainText) with and without keys.");

        // --- Artifact Providers ---
        logger.LogInformation("Registering Artifact Providers...");
        // NullArtifactProvider is in Nucleus.Services.Api.Infrastructure
        services.AddSingleton<IArtifactProvider, NullArtifactProvider>(); 
        // LocalFileArtifactProvider is registered by AddLocalAdapterServices() in Program.cs
        // but we still need to ensure it's part of the IEnumerable<IArtifactProvider> collection if resolved directly.
        // This ensures that if someone resolves IEnumerable<IArtifactProvider>, LocalFileArtifactProvider is included.
        // It relies on AddLocalAdapterServices() having already registered LocalFileArtifactProvider as a singleton.
        services.AddSingleton<IArtifactProvider>(sp => sp.GetRequiredService<LocalFileArtifactProvider>()); 
        logger.LogInformation("Registered NullArtifactProvider as default IArtifactProvider and ensured LocalFileArtifactProvider is in IArtifactProvider collection.");

        // === Persistence (Cosmos DB) ===
        logger.LogInformation("Configuring Persistence (Cosmos DB)...");
        string? cosmosConnectionString = configuration.GetConnectionString("cosmosdb"); 
        string? cosmosDatabaseName = configuration["CosmosDb:DatabaseName"];

        logger.LogInformation("Cosmos Config Check: ConnectionString='{ConnStrPresent}', DatabaseName='{DbName}'", 
            !string.IsNullOrWhiteSpace(cosmosConnectionString), 
            cosmosDatabaseName ?? "<NULL>");

        if (!string.IsNullOrWhiteSpace(cosmosConnectionString) && !string.IsNullOrWhiteSpace(cosmosDatabaseName))
        {
            logger.LogInformation("Cosmos DB connection string and database name found. Configuring Cosmos DB Client and Repositories.");
            try
            {
                CosmosClientOptions clientOptions = new()
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                services.AddSingleton<CosmosClient>(sp => new CosmosClient(cosmosConnectionString, clientOptions));
                
                services.AddSingleton<Database>(sp => sp.GetRequiredService<CosmosClient>().GetDatabase(cosmosDatabaseName));

                services.AddSingleton<Container>(sp => 
                {
                    var database = sp.GetRequiredService<Database>();
                    var containerName = configuration.GetValue<string>("CosmosDb:ArtifactMetadataContainerName", "ArtifactMetadata");
                    if (string.IsNullOrWhiteSpace(containerName))
                    {
                        throw new InvalidOperationException("Cosmos DB container name ('CosmosDb:ArtifactMetadataContainerName') is missing.");
                    }
                    return database.GetContainer(containerName);
                });

                services.AddSingleton<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
                services.AddSingleton<IPersonaKnowledgeRepository, CosmosDbPersonaKnowledgeRepository>();
                logger.LogInformation("Registered CosmosClient, Database, Container, and Repositories (ArtifactMetadata, PersonaKnowledge) using database '{DatabaseName}'.", cosmosDatabaseName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Cosmos DB. Repositories will be unavailable.");
            }
        }
        else
        {
            logger.LogWarning("Cosmos DB connection string or DatabaseName not found. Repositories will be unavailable.");
        }

        // === Messaging & Background Task Queue ===
        logger.LogInformation("Messaging services (IBackgroundTaskQueue, IMessageQueuePublisher<>) are registered in Program.cs.");
        if (!builder.Environment.IsDevelopment())
        {
            services.AddAzureClients(clientBuilder =>
            {
                var keyVaultUri = builder.Configuration.GetConnectionString("kv-nucleusbot");
                if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out _))
                {
                    clientBuilder.AddSecretClient(new Uri(keyVaultUri));
                    clientBuilder.UseCredential(new DefaultAzureCredential()); // Requires Azure.Identity
                    logger.LogInformation("Registered Azure Key Vault client.");
                }
                else
                {
                    logger.LogWarning("Azure Key Vault connection string 'kv-nucleusbot' not found/invalid. Key Vault integration disabled.");
                }
            });
        }
        
        logger.LogInformation("Registering Core AI and Orchestration Services...");

        // --- Orchestration ---
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddScoped<ActivationChecker>();
        logger.LogInformation("Registered Orchestration Service and ActivationChecker.");

        // --- Agentic Strategy Handlers ---
        services.AddScoped<IAgenticStrategyHandler, EchoAgenticStrategyHandler>(); 
        services.AddScoped<IAgenticStrategyHandler, MetadataSavingStrategyHandler>(); 

        logger.LogInformation("Registered Agentic Strategy Handlers.");

        // --- Persona Runtime --- 
        services.AddScoped<IPersonaRuntime, PersonaRuntime>();
        logger.LogInformation("Registered PersonaRuntime.");

        // --- API Controllers & Swagger ---
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nucleus API", Version = "v1" });
            }); 
        logger.LogInformation("Registered API Controllers and Swagger/OpenAPI services.");

        // NullPlatformNotifier is in Nucleus.Services.Api.Infrastructure.Messaging
        // IPlatformNotifier is in Nucleus.Abstractions.Adapters
        // PlatformType is in Nucleus.Abstractions.Models
        services.AddSingleton<IPlatformNotifier, NullPlatformNotifier>(); 
        services.AddKeyedSingleton<IPlatformNotifier, NullPlatformNotifier>(PlatformType.Api); 
        logger.LogInformation("Registered Platform Notifiers.");

        logger.LogInformation("AddNucleusServices registration complete.");
        return builder;
    }
}
