using Nucleus.Abstractions; 
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure.Messaging
{
    /// <summary>
    /// A dummy implementation of IMessageQueuePublisher that does nothing.
    /// Used when Azure Service Bus is not configured, allowing services 
    /// that depend on IMessageQueuePublisher to still be constructed.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
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
}
