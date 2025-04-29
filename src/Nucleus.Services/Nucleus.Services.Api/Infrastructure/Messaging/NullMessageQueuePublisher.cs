using Nucleus.Abstractions; 
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging;

/// <summary>
/// A null implementation of <see cref="IMessageQueuePublisher{T}"/> that performs no operation.
/// Useful for testing or development environments where a real message queue is not available or needed.
/// Logs a warning when instantiated and debug messages when PublishAsync is called.
/// <seealso cref="../../../../../Docs/Architecture/Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md"/>
/// <seealso cref="Docs.Architecture.Processing.Orchestration.ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// </summary>
/// <typeparam name="T">The message type (ignored).</typeparam>
public class NullMessageQueuePublisher<T> : IMessageQueuePublisher<T> where T : class
{
    private readonly ILogger<NullMessageQueuePublisher<T>> _logger;

    public NullMessageQueuePublisher(ILogger<NullMessageQueuePublisher<T>> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using NullMessageQueuePublisher for type {MessageType}. Messages will not be sent.", typeof(T).Name);
    }

    public Task PublishAsync(T message, string queueOrTopicName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullMessageQueuePublisher: Ignoring PublishAsync call for message of type {MessageType} to queue/topic '{QueueOrTopic}'.", 
            typeof(T).Name, 
            queueOrTopicName);
        // Do nothing
        return Task.CompletedTask;
    }
}
