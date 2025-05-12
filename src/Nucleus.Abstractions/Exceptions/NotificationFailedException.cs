// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Nucleus.Abstractions.Exceptions;

/// <summary>
/// Represents errors that occur during the notification process within Nucleus services.
/// </summary>
public class NotificationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationFailedException"/> class.
    /// </summary>
    public NotificationFailedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationFailedException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotificationFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationFailedException"/> class
    /// with a specified error message and a reference to the inner exception that is
    /// the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference if no inner exception is specified.</param>
    public NotificationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // Future: Consider adding properties like NotificationType, RecipientId, etc., if useful for context.
}