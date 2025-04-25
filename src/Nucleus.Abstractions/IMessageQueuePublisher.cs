// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Abstractions;

/// <summary>
/// Defines a contract for publishing messages to a queue or topic.
/// This abstraction supports decoupling components using asynchronous messaging patterns,
/// aligning with the principles outlined in the deployment architecture.
/// See: ../Docs/Architecture/Deployment/ARCHITECTURE_DEPLOYMENT_ABSTRACTIONS.md
/// </summary>
/// <typeparam name="T">The type of the message payload to be published.</typeparam>
public interface IMessageQueuePublisher<in T>
{
    /// <summary>
    /// Asynchronously publishes a message payload to the configured queue or topic.
    /// </summary>
    /// <param name="messagePayload">The message payload object.</param>
    /// <param name="queueOrTopicName">The specific queue or topic to publish to.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="messagePayload"/> or <paramref name="queueOrTopicName"/> is null or empty.</exception>
    /// <exception cref="System.Exception">Thrown if publishing fails for any reason (e.g., connection issues, serialization errors).</exception>
    Task PublishAsync(T messagePayload, string queueOrTopicName, CancellationToken cancellationToken = default);
}
