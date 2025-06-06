using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration; // For NucleusConstants if needed for config keys
using Nucleus.Abstractions.Orchestration;

namespace Nucleus.Infrastructure.Messaging;

/// <summary>
/// Extension methods for setting up messaging-related services (e.g., Background Task Queues, Message Publishers)
/// in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds messaging services, including the <see cref="IBackgroundTaskQueue"/> and potentially
    /// <see cref="Nucleus.Abstractions.IMessageQueuePublisher{T}"/> implementations, to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isDevelopment">A flag indicating if the environment is development. This can be used to select appropriate implementations (e.g., in-memory vs. cloud).</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Ensure logging services are registered  
        services.AddLogging(loggingBuilder => 
        {  
            loggingBuilder.AddConsole();  
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));  
        });  

        // Resolve the logger directly from the service collection  
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();  
        var logger = loggerFactory.CreateLogger<InMemoryBackgroundTaskQueue>();  // Or a more generic logger like CreateLogger("Nucleus.Infrastructure.Messaging")

        logger.LogInformation("Registering Messaging Services...");

        // Register IBackgroundTaskQueue
        // For now, we'll default to InMemoryBackgroundTaskQueue.
        // In the future, this could be conditional based on configuration or isDevelopment flag.
        // For example:
        // if (isDevelopment || configuration.GetValue<string>("QueueType") == "InMemory")
        // {
        //    logger.LogInformation("Registering InMemoryBackgroundTaskQueue as IBackgroundTaskQueue.");
        //    services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
        // }
        // else
        // {
        //    logger.LogInformation("Registering AzureServiceBusBackgroundTaskQueue as IBackgroundTaskQueue. (Placeholder - not implemented yet)");
        //    // services.AddSingleton<IBackgroundTaskQueue, AzureServiceBusBackgroundTaskQueue>(); // Example for Azure
        //    // Fallback to in-memory if cloud not configured or desired for now
        //    logger.LogWarning("AzureServiceBusBackgroundTaskQueue not implemented, falling back to InMemoryBackgroundTaskQueue.");
        //    services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
        // }
        
        logger.LogInformation("Registering InMemoryBackgroundTaskQueue as IBackgroundTaskQueue.");
        services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();


        // Register IMessageQueuePublisher<T> implementations here if needed
        // Example:
        // services.AddSingleton<IMessageQueuePublisher<NucleusIngestionRequest>, NullMessageQueuePublisher<NucleusIngestionRequest>>();
        // logger.LogInformation("Registered NullMessageQueuePublisher<NucleusIngestionRequest> as default IMessageQueuePublisher.");

        logger.LogInformation("Messaging Services registration complete.");
        return services;
    }
}
