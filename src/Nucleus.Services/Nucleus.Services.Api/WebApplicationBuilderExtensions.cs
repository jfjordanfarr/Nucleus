// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Aspire.Azure.Messaging.ServiceBus; // Add explicit using for Aspire extensions
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
using Microsoft.Extensions.DependencyInjection.Extensions; // For TryAddSingleton
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Nucleus.Abstractions; // Re-adding this as it's needed
using Nucleus.Abstractions.Adapters; 
using Nucleus.Abstractions.Extraction; // Added for IContentExtractor
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
using Nucleus.Infrastructure.Providers; // Added for ConsoleArtifactProvider
using Nucleus.Infrastructure.Providers.ContentExtraction; // Added for concrete extractors
using Nucleus.Services.Api.Configuration;
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Endpoints; // ADDED
using Nucleus.Services.Api.Infrastructure; // Added for NullArtifactProvider
using Nucleus.Services.Api.Infrastructure.Messaging;
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
        var logger = loggerFactory.CreateLogger("Nucleus.Services.Api.WebApplicationBuilderExtensions"); // Use string category for static class context

        logger.LogInformation("Registering Nucleus services...");

        // === Configuration Bindings ===
        services.Configure<List<PersonaConfiguration>>(configuration.GetSection(NucleusConstants.ConfigurationKeys.PersonasSection));
        services.Configure<GoogleAiOptions>(configuration.GetSection(GoogleAiOptions.SectionName));
        // Removed TeamsAdapterConfiguration binding

        // --- Caching ---
        services.AddMemoryCache();
        logger.LogInformation("Registered IMemoryCache.");

        // --- AI Services ---
        // Registers clients for interacting with AI models (e.g., OpenAI, Google Gemini).
        // These are abstracted by IChatClient and other interfaces for use in the application.
        // See: ../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md
        // See: ../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md
        logger.LogInformation("Configuring AI Services...");
        var googleAiSection = configuration.GetSection("AI:GoogleAI");
        if (googleAiSection.Exists())
        {
            var apiKey = googleAiSection["ApiKey"];
            var modelId = googleAiSection["ModelId"];
            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR_API_KEY_HERE" && !string.IsNullOrWhiteSpace(modelId))
            {
                logger.LogInformation("Registering GeminiChatClient (Mscc.GenerativeAI.Microsoft) as IChatClient (Model: {ModelId})", modelId);
                services.AddSingleton<IChatClient>(provider => 
                    new GeminiChatClient(apiKey!, modelId) // Use non-nullable assertion
                );
            }
            else
            {
                logger.LogWarning("Google AI configuration found, but ApiKey or ModelId is missing or placeholder. Skipping IChatClient registration.");
                // Consider registering a NullChatClient or throwing an error if required
            }
        }
        else
        {
             logger.LogWarning("AI:GoogleAI configuration section not found. Skipping IChatClient registration.");
             // Consider registering a NullChatClient or throwing an error if required
        }

        // --- Content Extraction Services ---
        services.AddKeyedSingleton<IContentExtractor, HtmlContentExtractor>(NucleusConstants.ExtractorKeys.Html);
        services.AddKeyedSingleton<IContentExtractor, PlainTextContentExtractor>(NucleusConstants.ExtractorKeys.PlainText);
        logger.LogInformation("Registered Content Extraction Services (Html, PlainText).");

        // --- Artifact Providers ---
        logger.LogInformation("Registering Artifact Providers...");
        services.AddSingleton<IArtifactProvider, NullArtifactProvider>(); // Default IArtifactProvider
        services.AddSingleton<ConsoleArtifactProvider>(); // Register concrete type
        services.AddSingleton<IArtifactProvider>(sp => sp.GetRequiredService<ConsoleArtifactProvider>()); // Add to IEnumerable<IArtifactProvider>
        logger.LogInformation("Registered NullArtifactProvider as default IArtifactProvider and ConsoleArtifactProvider for collection.");

        // === Persistence (Cosmos DB) ===
        logger.LogInformation("Configuring Persistence...");
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
                // Use custom serializer options matching Aspire defaults
                CosmosClientOptions clientOptions = new()
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };

                // Register CosmosClient as Singleton
                services.AddSingleton<CosmosClient>(sp => new CosmosClient(cosmosConnectionString, clientOptions));
                
                // Register the database instance
                services.AddSingleton<Database>(sp => 
                {
                    var client = sp.GetRequiredService<CosmosClient>();
                    return client.GetDatabase(cosmosDatabaseName);
                });

                // Register the primary container instance
                services.AddSingleton<Container>(sp => 
                {
                    var database = sp.GetRequiredService<Database>();
                    var containerName = configuration.GetValue<string>("CosmosDb:ArtifactMetadataContainerName", "ArtifactMetadata");
                    if (string.IsNullOrWhiteSpace(containerName))
                    {
                        throw new InvalidOperationException("Cosmos DB container name ('CosmosDb:ArtifactMetadataContainerName') is missing.");
                    }
                    // Consider adding CreateContainerIfNotExistsAsync logic here if needed
                    return database.GetContainer(containerName);
                });

                // Register Production Repositories
                services.AddSingleton<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
                services.AddSingleton<IPersonaKnowledgeRepository, CosmosDbPersonaKnowledgeRepository>();
                logger.LogInformation("Registered CosmosClient, Database, Container, and Repositories (ArtifactMetadata, PersonaKnowledge) using database '{DatabaseName}'.", cosmosDatabaseName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Cosmos DB from connection string. Dependent repositories will be unavailable.");
                // Fail explicitly instead of falling back to in-memory
            }
        }
        else
        {
            logger.LogWarning("Cosmos DB connection string ('cosmosdb') or DatabaseName ('CosmosDb:DatabaseName') not found. Cosmos DB dependent repositories will be unavailable.");
            // Fail explicitly instead of falling back to in-memory
        }

        // === Messaging & Background Task Queue ===
        logger.LogInformation("Registering Service Bus client and Background Task Queue...");
        try
        {
            // NOTE: IBackgroundTaskQueue and IMessageQueuePublisher<> (and their specific ServiceBusClient) are now registered in Program.cs.
            // This section is now only for other general Azure clients if needed by services registered in AddNucleusServices (e.g., Key Vault).
            if (!builder.Environment.IsDevelopment())
            {
                // Example: If Key Vault is needed by other services configured here:
                services.AddAzureClients(clientBuilder =>
                {
                    var keyVaultUri = builder.Configuration.GetConnectionString("kv-nucleusbot");
                    if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out _))
                    {
                        clientBuilder.AddSecretClient(new Uri(keyVaultUri));
                        clientBuilder.UseCredential(new DefaultAzureCredential());
                        logger.LogInformation("Registered Azure Key Vault client.");
                    }
                    else
                    {
                        logger.LogWarning("Azure Key Vault connection string 'kv-nucleusbot' not found or invalid. Key Vault integration disabled for services in AddNucleusServices.");
                    }
                });
            }
            logger.LogInformation("Background Task Queue and Publisher registrations are handled in Program.cs.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during conditional Azure client registration in AddNucleusServices (e.g., Key Vault). Dependent features might not function correctly.");
            // Fallback or rethrow depending on requirements
        }

        // QueuedInteractionProcessorService is registered later as a Hosted Service

        // --- Core AI / Orchestration / Artifact Services ---
        logger.LogInformation("Registering Core AI and Artifact Services...");

        // --- Orchestration ---
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddScoped<ActivationChecker>();
        logger.LogInformation("Registered Orchestration Service and ActivationChecker.");

        // --- Agentic Strategy Handlers ---
        services.AddScoped<IAgenticStrategyHandler, EchoAgenticStrategyHandler>(); // Default/Example
        services.AddScoped<IAgenticStrategyHandler, MetadataSavingStrategyHandler>(); // For saving metadata in tests

        logger.LogInformation("Registered Agentic Strategy Handlers (EchoAgenticStrategyHandler, MetadataSavingStrategyHandler).");

        // --- Persona Runtime --- (Depends on strategies, config provider)
        services.AddScoped<IPersonaRuntime, PersonaRuntime>();
        logger.LogInformation("Registered PersonaRuntime.");

        // --- API Controllers & Swagger ---
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        logger.LogInformation("Registered API Controllers and Swagger/OpenAPI services.");

        // --- Platform Notifiers ---
        // Default (used if no specific platform is targeted)
        /// <remarks>
        /// For detailed architectural context on platform notification, see <see href="../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md#platform-notification-iplatformnotifier">01_ARCHITECTURE_PROCESSING.md</see>.
        /// </remarks>
        services.AddSingleton<IPlatformNotifier, NullPlatformNotifier>(); 
        // Keyed notifier for explicit API platform use
        services.AddKeyedSingleton<IPlatformNotifier, NullPlatformNotifier>(PlatformType.Api);
        logger.LogInformation("Registered Platform Notifiers (Default Null, Keyed API Null).");

        // --- Hosted Services ---
        services.AddHostedService<QueuedInteractionProcessorService>();
        logger.LogInformation("Registered QueuedInteractionProcessorService as Hosted Service.");

        logger.LogInformation("Nucleus service registration complete.");
        return builder;
    }
}
