using Azure.Identity; // For DefaultAzureCredential if used by AddInfrastructureServices or its callees
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.ApiContracts; // Added for NucleusIngestionRequest for publisher types
using Nucleus.Infrastructure.Adapters.Local; 
using Nucleus.Infrastructure.Data.Persistence; // For AddPersistenceServices
using Nucleus.Domain.Processing;             // For AddProcessingServices
using Nucleus.Infrastructure.Providers;      // Corrected: Was Nucleus.Infrastructure
using Nucleus.Infrastructure.Messaging;      // For IBackgroundTaskQueue, IMessageQueuePublisher<>, AzureServiceBusPublisher, NullMessageQueuePublisher, ServiceBusBackgroundTaskQueue, InMemoryBackgroundTaskQueue
using System.Threading.Tasks;
using Nucleus.Services.Api; // For AddNucleusServices, MapNucleusEndpoints

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

            // --- Nucleus Domain & Infrastructure Service Registrations ---
            // These extension methods are expected to register their specific services,
            // including any hosted services like QueuedInteractionProcessorService.

            // Add Persistence services (CosmosDB Repositories, Persona Configuration Provider)
            builder.Services.AddPersistenceServices(builder.Configuration);
            _logger.LogInformation("Registered Persistence Services.");

            // Add Domain Processing services (Orchestration, Persona Resolver, QueuedInteractionProcessorService)
            builder.Services.AddProcessingServices(builder.Configuration);
            _logger.LogInformation("Registered Processing Services.");

            // Add Infrastructure services (Adapters, general Providers not covered by specific adapters)
            // This is where IMessageQueuePublisher and IBackgroundTaskQueue should be registered based on environment.
            builder.Services.AddInfrastructureProviderServices(builder.Configuration); // Removed builder.Environment.IsDevelopment()
            _logger.LogInformation("Registered Infrastructure Services (including environment-specific messaging).");

            // Add Local Adapter Services (registers LocalFileArtifactProvider, etc.)
            // This is called separately as it's a specific, pluggable adapter.
            builder.Services.AddLocalAdapterServices();
            _logger.LogInformation("Registered Local Adapter Services.");

            var app = builder.Build();

            // Middleware and endpoint mapping are now handled in MapNucleusEndpoints
            app.MapNucleusEndpoints();

            await app.RunAsync();
        }
    }
}
