// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Represents a message dequeued from a background task queue, including both the
/// deserialized payload and the context needed to manage the message lifecycle (e.g., complete, abandon).
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
/// <param name="Payload">The deserialized message payload.</param>
/// <param name="MessageContext">The underlying message object or context from the message broker, required for completion/abandonment.</param>
public record DequeuedMessage<T>(
    T Payload,
    object MessageContext
);
