using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Repositories;
using Nucleus.Infrastructure.Data.Persistence.Repositories;
using Nucleus.Infrastructure.Data.Persistence.Configuration;
using Nucleus.Infrastructure.Providers.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Nucleus.Infrastructure.Data.Persistence;

/// <summary>
/// Extension methods for setting up persistence-related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds persistence services (e.g., Repositories) to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Get a logger
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Nucleus.Infrastructure.Data.Persistence.ServiceCollectionExtensions");

        // Log the connection string Aspire *should* be providing
        var aspireConnectionString = configuration.GetConnectionString("cosmosdb");
        logger.LogInformation("Attempting to retrieve 'cosmosdb' connection string directly from IConfiguration (Aspire conventional key): '{ConnectionString}'", string.IsNullOrWhiteSpace(aspireConnectionString) ? "<NULL_OR_EMPTY>" : "<PROVIDED_BUT_MASKED_FOR_LOG>"); // Mask potentially sensitive info

        // Configure CosmosDbSettings
        services.Configure<CosmosDbSettings>(configuration.GetSection("CosmosDb"));

        // Register CosmosClient as a singleton
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var cosmosDbSettings = serviceProvider.GetRequiredService<IOptions<CosmosDbSettings>>().Value;
            var settingsLoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>(); // Logger for CosmosClient context
            var settingsLogger = settingsLoggerFactory.CreateLogger("CosmosClientSetup");

            settingsLogger.LogInformation("CosmosDbSettings.ConnectionString from IOptions: '{ConnectionString}'", string.IsNullOrWhiteSpace(cosmosDbSettings.ConnectionString) ? "<NULL_OR_EMPTY>" : "<PROVIDED_BUT_MASKED_FOR_LOG>");

            if (string.IsNullOrWhiteSpace(cosmosDbSettings.ConnectionString))
            {
                settingsLogger.LogError("CosmosDB ConnectionString from CosmosDbSettings is not configured.");
                throw new InvalidOperationException("CosmosDB ConnectionString is not configured.");
            }
            // OPTIONAL: Configure CosmosClientOptions if needed (e.g., serializer, consistency level)
            // var clientOptions = new CosmosClientOptions
            // {
            //     SerializerOptions = new CosmosSerializationOptions
            //     {
            //         PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            //     }
            // };
            try
            {
                return new CosmosClient(cosmosDbSettings.ConnectionString /*, clientOptions */);
            }
            catch (ArgumentException ex)
            {
                settingsLogger.LogError(ex, "ArgumentException while creating CosmosClient. ConnectionString used: '{ConnectionString}'", string.IsNullOrWhiteSpace(cosmosDbSettings.ConnectionString) ? "<NULL_OR_EMPTY>" : "<PROVIDED_BUT_MASKED_FOR_LOG>");
                throw; // Re-throw the original exception to preserve stack trace and type
            }
        });

        // Register repositories
        services.AddScoped<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
        services.AddScoped<IPersonaKnowledgeRepository, CosmosDbPersonaKnowledgeRepository>();

        // Register configuration providers conditionally
        var testPersonaConfigsJson = Environment.GetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON");
        if (!string.IsNullOrWhiteSpace(testPersonaConfigsJson))
        {
            services.AddScoped<IPersonaConfigurationProvider>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<InMemoryPersonaConfigurationProvider>(); 
                List<PersonaConfiguration>? initialConfigs = null;
                try
                {
                    initialConfigs = JsonSerializer.Deserialize<List<PersonaConfiguration>>(testPersonaConfigsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    logger.LogInformation("Successfully deserialized NUCLEUS_TEST_PERSONA_CONFIGS_JSON. Count: {Count}", initialConfigs?.Count ?? 0);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to deserialize NUCLEUS_TEST_PERSONA_CONFIGS_JSON. Falling back to CosmosDbPersonaConfigurationProvider.");
                    // Fallback registration within the factory if deserialization fails
                    return new CosmosDbPersonaConfigurationProvider(
                        serviceProvider.GetRequiredService<CosmosClient>(),
                        serviceProvider.GetRequiredService<IOptions<CosmosDbSettings>>(),
                        serviceProvider.GetRequiredService<ILogger<CosmosDbPersonaConfigurationProvider>>()); 
                }
                return new InMemoryPersonaConfigurationProvider(logger, initialConfigs);
            });
            services.AddScoped<InMemoryPersonaConfigurationProvider>(serviceProvider => 
                (InMemoryPersonaConfigurationProvider)serviceProvider.GetRequiredService<IPersonaConfigurationProvider>());
        }
        else
        {
            services.AddScoped<IPersonaConfigurationProvider, CosmosDbPersonaConfigurationProvider>();
        }

        return services;
    }
}
