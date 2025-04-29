using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
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
using Nucleus.Infrastructure.Data.Persistence.Repositories; // For CosmosDbArtifactMetadataRepository etc.
using Nucleus.Services.Api.Diagnostics;
using Nucleus.Services.Api.Infrastructure; // For LocalFileArtifactProvider
using Nucleus.Services.Api.Infrastructure.Messaging; // For ServiceBusQueueConsumerService and NullMessageQueuePublisher
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Infrastructure.Adapters.Teams;
using Nucleus.Domain.Personas.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization;
using Nucleus.Services.Api; // ADDED

namespace Nucleus.Services.Api
{
    /// <summary>
    /// Main entry point for the Nucleus API service.
    /// Configures and runs the ASP.NET Core web application.
    /// Utilizes .NET Aspire for service discovery and resilience.
    /// See API Endpoint mapping in ApiEndpointsExtensions.cs.
    /// <seealso cref="../../../../../Docs/Architecture/10_ARCHITECTURE_API.md"/>
    /// <seealso cref="../../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
    /// <seealso cref="../../../../../Docs/Architecture/07_ARCHITECTURE_DEPLOYMENT.md"/>
    /// </summary>
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

            // Conditionally disable strict scope validation in Development/Test environments
            // to potentially work around validation issues with keyed/factory registrations.
            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = !builder.Environment.IsDevelopment();
                options.ValidateOnBuild = !builder.Environment.IsDevelopment(); // Also disable build-time validation
                _logger.LogInformation("Strict DI Scope/Build Validation Enabled: {ValidationEnabled}", options.ValidateScopes);
            });

            // --- Nucleus Domain Services --- 
            // REMOVED: builder.Services.AddProcessingServices(); // Handled by AddNucleusServices

            var app = builder.Build();

            // Middleware and endpoint mapping are now handled in MapNucleusEndpoints
            app.MapNucleusEndpoints();

            // --- Initialize Cosmos DB Container on startup --- 
            // REMOVED: Initialization is now handled within AddNucleusServices when registering the Container singleton.

            await app.RunAsync();
        }
    }
}
