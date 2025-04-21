using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nucleus.Domain.Processing; // Correct namespace for implementations
using Nucleus.Abstractions; // For IOrchestrationService
using Nucleus.Abstractions.Orchestration; // For IPersonaResolver, IPersonaManager
using Nucleus.Processing.Services; // Corrected namespace for DatavizHtmlBuilder
using Microsoft.Extensions.Logging; // Added for logging

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
        // Obtain a logger for registration confirmation. A dedicated static logger or resolving ILoggerFactory might be better long-term.
        using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug)); // Simple console logger for setup
        var logger = loggerFactory.CreateLogger("Nucleus.Domain.Processing.ServiceCollectionExtensions");

        // Orchestration Core
        services.AddScoped<IOrchestrationService, OrchestrationService>();

        // Persona Resolver (Using Default implementation for now)
        services.AddSingleton<IPersonaResolver, DefaultPersonaResolver>();

        // Persona Managers (Registering Default for now - real implementation would register multiple concrete managers)
        // Register the concrete implementation first, allowing DI to resolve its dependencies (like IClientAdapterResolver)
        // services.AddSingleton<DefaultPersonaManager>();
        // Now, ensure requests for the interface resolve to the registered singleton instance.
        // services.AddSingleton<IPersonaManager>(provider => provider.GetRequiredService<DefaultPersonaManager>());

        // Register Persona Managers using Keyed Services
        // The key should match the ManagedPersonaTypeId property of the implementation.
        // TODO: Consider a central registry or reflection-based approach for keys instead of hardcoding.
        services.AddKeyedSingleton<IPersonaManager, DefaultPersonaManager>(DefaultPersonaManager.ManagedPersonaTypeIdConstant);

        logger.LogInformation("Successfully registered IPersonaManager with key: {Key}", DefaultPersonaManager.ManagedPersonaTypeIdConstant);

        // Register other services from Nucleus.Processing here in the future...

        // Register the DatavizHtmlBuilder
        // Transient is suitable as it appears stateless. Use Scoped if it needs request-scoped dependencies later.
        services.AddTransient<DatavizHtmlBuilder>();

        return services;
    }
}
