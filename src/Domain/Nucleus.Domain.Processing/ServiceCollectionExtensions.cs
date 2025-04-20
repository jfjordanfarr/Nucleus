using Microsoft.Extensions.DependencyInjection;
using Nucleus.Domain.Processing; // Correct namespace for implementations
using Nucleus.Abstractions; // For IOrchestrationService
using Nucleus.Abstractions.Orchestration; // For IPersonaResolver, IPersonaManager
using Nucleus.Processing.Services; // Corrected namespace for DatavizHtmlBuilder

namespace Nucleus.Domain.Processing; // Ensure namespace matches file location

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services defined in the Nucleus.Domain.Processing library to the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
    public static IServiceCollection AddProcessingServices(this IServiceCollection services)
    {
        // Orchestration Core
        services.AddScoped<IOrchestrationService, OrchestrationService>();

        // Persona Resolver (Using Default implementation for now)
        services.AddSingleton<IPersonaResolver, DefaultPersonaResolver>();

        // Persona Managers (Registering Default for now - real implementation would register multiple concrete managers)
        services.AddSingleton<IPersonaManager, DefaultPersonaManager>();

        // Register other services from Nucleus.Processing here in the future...

        // Register the DatavizHtmlBuilder
        // Transient is suitable as it appears stateless. Use Scoped if it needs request-scoped dependencies later.
        services.AddTransient<DatavizHtmlBuilder>();

        return services;
    }
}
