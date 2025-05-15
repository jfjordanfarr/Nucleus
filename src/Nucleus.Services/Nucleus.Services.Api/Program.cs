using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Microsoft;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Processing;
using Nucleus.Infrastructure.Data.Persistence;
using Nucleus.Infrastructure.Data.Persistence.Repositories; // For CosmosDbArtifactMetadataRepository etc.
using Nucleus.Infrastructure.Adapters.Local; // Added for LocalClientAdapter
using Nucleus.Infrastructure.Providers;
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Infrastructure; // For LocalFileArtifactProvider
using Nucleus.Services.Api.Infrastructure.Messaging; // For NullMessageQueuePublisher
using Nucleus.Infrastructure.Messaging; // For IBackgroundTaskQueue
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization;
using Nucleus.Services.Api; // ADDED

namespace Nucleus.Services.Api
{
    /// <summary>
    /// Main entry point and configuration for the Nucleus API service.
    /// This project serves as the primary gateway for all external interactions with the Nucleus platform.
    /// </summary>
    /// <seealso cref="../../../../Docs/Architecture/00_ARCHITECTURE_OVERVIEW.md"/>
    /// <seealso cref="../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
    /// <seealso cref="../../../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md"/>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // *** Obtain logger early for setup logging ***
            // Note: Standard practice is to configure logging via the builder
            // Keeping this early logger setup for now, but consider integrating with builder's logging.
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole());
            var _logger = loggerFactory.CreateLogger("Program");

            var builder = WebApplication.CreateBuilder(args);

            // Add service defaults & Aspire components.
            builder.AddServiceDefaults();

            // -------------------- Nucleus Core Services Registration ---------------------
            // Call the extension method to register Nucleus services
            builder.AddNucleusServices();

            // === Configuration ===
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();

            // --- Aspire Service Discovery ---
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });
            // --- End Service Discovery Block ---

            // Enable strict scope and build validation to catch DI lifetime issues.
            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true; 
                _logger.LogInformation("Strict DI Scope/Build Validation Enabled: {ValidationEnabled}", options.ValidateScopes);
            });

            // --- Nucleus Domain Services --- 
            // Add Persistence services (including Persona Configuration Provider)
            builder.Services.AddPersistenceServices(builder.Configuration);

            // Add Domain Processing services (including Orchestration, Persona Resolver)
            builder.Services.AddProcessingServices(builder.Configuration);

            // Add Infrastructure services (Adapters, Providers)
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Add services specific to the Development environment
            if (builder.Environment.IsDevelopment())
            {
                _logger.LogInformation("Development environment detected. Registering InMemoryBackgroundTaskQueue and NullMessageQueuePublisher.");
                builder.Services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
                builder.Services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(NullMessageQueuePublisher<>));
            }
            else
            {
                _logger.LogInformation("Non-Development environment detected. Registering Azure Service Bus for background tasks and publishing.");
                builder.Services.AddAzureClients(clientBuilder =>
                {
                    // Configure the Service Bus Client connection using the connection string from AppHost (expected)
                    // The connection string "sbBackgroundTasks" is the key Aspire uses by default when a resource is named "sbBackgroundTasks"
                    clientBuilder.AddServiceBusClient(builder.Configuration.GetConnectionString("sbBackgroundTasks"))
                                 .WithName(NucleusConstants.ServiceBusNames.BackgroundTasksClientName) // Ensures it can be resolved by this name if needed, e.g. by ServiceBusHealthIndicator
                                 .ConfigureOptions(options =>
                                 {
                                     // Configure client-level options if needed
                                     // options.TransportType = ServiceBusTransportType.AmqpWebSockets;
                                 });
                });

                builder.Services.AddSingleton<IBackgroundTaskQueue, ServiceBusBackgroundTaskQueue>();
                builder.Services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(AzureServiceBusPublisher<>));
            }

            // Add Local Adapter Services (this is generic, might be used in dev or specific deployments)
            // Ensure this doesn't take builder.Configuration if not needed by the method itself
            builder.Services.AddLocalAdapterServices();
            _logger.LogInformation("Registered Local Adapter Services.");

            var app = builder.Build();

            // Middleware and endpoint mapping are now handled in MapNucleusEndpoints
            app.MapNucleusEndpoints();

            // --- Initialize Cosmos DB Container on startup --- 
            // REMOVED: Initialization is now handled within AddNucleusServices when registering the Container singleton.

            await app.RunAsync();
        }
    }
}
