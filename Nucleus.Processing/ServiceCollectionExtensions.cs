using Microsoft.Extensions.DependencyInjection;
using Nucleus.Processing.Services; // Add this using directive

namespace Nucleus.Processing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services defined in the Nucleus.Processing library to the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
    public static IServiceCollection AddProcessingServices(this IServiceCollection services)
    {
        // Register other services from Nucleus.Processing here in the future...

        // Register the DatavizHtmlBuilder
        // Transient is suitable as it appears stateless. Use Scoped if it needs request-scoped dependencies later.
        services.AddTransient<DatavizHtmlBuilder>();

        return services;
    }
}
