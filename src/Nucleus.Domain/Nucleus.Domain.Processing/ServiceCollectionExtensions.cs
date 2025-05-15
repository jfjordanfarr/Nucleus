using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Processing;
using Nucleus.Domain.Processing.Services;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Extension methods for setting up domain processing services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core domain processing services to the specified <see cref="IServiceCollection" />.
    /// This includes the OrchestrationService, ActivationChecker, PersonaResolver, and related dependencies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddProcessingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core orchestration service
        // Use TryAddScoped to allow consumers to override the implementation if needed.
        services.TryAddScoped<IOrchestrationService, OrchestrationService>();

        // Register the default activation checker
        // Use TryAddScoped to allow consumers to override the implementation.
        // Note: ActivationChecker is currently bypassed in OrchestrationService (P0.3 refactor)
        services.TryAddScoped<IActivationChecker, ActivationChecker>();

        // Register the default persona resolver
        // Use TryAddScoped to allow consumers to override the implementation.
        // TODO: Replace DefaultPersonaResolver with a configurable implementation.
        /// <seealso cref="../../../../../Docs/Architecture/01_ARCHITECTURE_PROCESSING.md#ipersonaresolver-registration">Nucleus Processing Architecture - IPersonaResolver Registration</seealso>
        services.TryAddScoped<IPersonaResolver, DefaultPersonaResolver>();

        // Register the background task processor service.
        // The IBackgroundTaskQueue itself should be registered in the composition root
        // (e.g., API service startup or Test setup) with the appropriate implementation.
        services.AddHostedService<QueuedInteractionProcessorService>();

        // Register the Dataviz HTML builder service
        services.TryAddScoped<DatavizHtmlBuilder>();

        // Note: Repositories (like IArtifactMetadataRepository, IPersonaKnowledgeRepository)
        // and configuration providers (like IPersonaConfigurationProvider) are expected
        // to be registered separately in the Infrastructure or main application layer,
        // as their implementations depend on the chosen persistence/configuration mechanism.

        // TODO: Use 'configuration' parameter if needed in the future.

        return services;
    }
}
