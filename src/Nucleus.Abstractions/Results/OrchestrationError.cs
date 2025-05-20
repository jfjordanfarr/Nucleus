// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Abstractions.Results;

/// <summary>
/// Defines the possible errors that can occur during the orchestration process.
/// </summary>
/// <seealso cref="Result{TSuccess,TError}"/>
/// <seealso cref="Orchestration.IOrchestrationService"/>
public enum OrchestrationError
{
    /// <summary>
    /// An unknown or unspecified error occurred.
    /// </summary>
    UnknownError = 0,

    /// <summary>
    /// The request was invalid or malformed.
    /// </summary>
    InvalidRequest = 1,

    /// <summary>
    /// The specified persona could not be resolved or found.
    /// </summary>
    PersonaResolutionFailed = 2,

    /// <summary>
    /// The interaction did not meet the criteria for activation.
    /// </summary>
    ActivationCheckFailed = 3,

    /// <summary>
    /// An error occurred during artifact processing (e.g., fetching, content extraction).
    /// </summary>
    ArtifactProcessingFailed = 4,

    /// <summary>
    /// An error occurred during the runtime execution of persona logic.
    /// </summary>
    RuntimeExecutionFailed = 5,

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    OperationCancelled = 6,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 7,

    /// <summary>
    /// An internal service error occurred.
    /// </summary>
    InternalServiceError = 8,

    /// <summary>
    /// The system is currently too busy to handle the request.
    /// </summary>
    ServiceUnavailable = 9,

    /// <summary>
    /// The user is not authorized to perform this action.
    /// </summary>
    Unauthorized = 10
}
