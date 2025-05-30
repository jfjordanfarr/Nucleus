using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// using Nucleus.Abstractions.Providers; // Example: Add specific using directives as needed
// using Nucleus.Infrastructure.Providers.Artifacts; // Example

namespace Nucleus.Infrastructure.Providers;

/// <summary>
/// Extension methods for setting up infrastructure-related services (Providers, Adapters, etc.) in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services (e.g., Providers, Adapters, LLM clients) to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInfrastructureProviderServices(this IServiceCollection services, IConfiguration configuration) // Renamed method
    {
        // Note: LocalFileArtifactProvider is now registered with LocalAdapter
        // Example: Add LLM Client registration (Gemini, OpenAI, etc.)
        // Example: Add other *general* Client Adapters registration (Console, Teams, etc.) if not self-registering
        
        return services;
    }
}
