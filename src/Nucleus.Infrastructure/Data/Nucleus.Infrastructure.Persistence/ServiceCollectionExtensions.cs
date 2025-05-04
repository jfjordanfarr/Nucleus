using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// using Nucleus.Abstractions.Repositories; // Example: Add specific using directives as needed
// using Nucleus.Infrastructure.Data.Persistence.Repositories; // Example

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
        // TODO: Register actual persistence services here
        // Example: services.AddScoped<IArtifactMetadataRepository, CosmosDbArtifactMetadataRepository>();
        // Example: services.AddScoped<IPersonaKnowledgeRepository, CosmosDbPersonaKnowledgeRepository>();
        // Example: services.AddScoped<IPersonaConfigurationProvider, ConfigurationPersonaConfigurationProvider>();

        // Read configuration if needed for specific registrations
        // var cosmosSettings = configuration.GetSection("CosmosDb").Get<CosmosDbSettings>();

        return services;
    }
}
