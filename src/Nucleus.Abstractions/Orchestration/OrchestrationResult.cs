using Nucleus.Abstractions.Models;
using System;

namespace Nucleus.Abstractions.Orchestration;

/// <summary>
/// Enum representing the status of the initial orchestration phase.
/// </summary>
public enum OrchestrationStatus
{
    /// <summary>
    /// Orchestration completed successfully, and a direct response is available.
    /// </summary>
    Success,
    /// <summary>
    /// The interaction did not meet activation criteria and was ignored.
    /// </summary>
    Ignored,
    /// <summary>
    /// The interaction was successfully queued for background processing.
    /// </summary>
    Queued,
    /// <summary>
    /// An error occurred during the orchestration process.
    /// </summary>
    Failed,
    /// <summary>
    /// The required Persona could not be resolved or loaded.
    /// </summary>
    PersonaResolutionFailed,
    /// <summary>
    /// Activation check failed.
    /// </summary>
    ActivationCheckFailed,
    /// <summary>
    /// Artifact processing failed.
    /// </summary>
    ArtifactProcessingFailed,
    /// <summary>
    /// An error occurred during the persona runtime execution.
    /// </summary>
    RuntimeExecutionFailed,
    /// <summary>
    /// An unexpected or unhandled error occurred during orchestration.
    /// </summary>
    UnhandledError
}

/// <summary>
/// Represents the outcome of the initial orchestration phase for an interaction request.
/// This is returned by IOrchestrationService.ProcessInteractionAsync.
/// </summary>
/// <param name="Status">The overall status of the orchestration attempt.</param>
/// <param name="ResolvedPersonaId">The canonical Persona ID resolved during the process, if applicable.</param>
/// <param name="AdapterResponse">The response to send back to the adapter, if generated directly (e.g., for ignored/failed requests).</param>
/// <param name="ErrorMessage">Detailed error message if Status is Failed or other error states.</param>
/// <param name="Exception">The exception that occurred, if any.</param>
public record OrchestrationResult(
    OrchestrationStatus Status,
    string? ResolvedPersonaId = null,
    AdapterResponse? AdapterResponse = null, // Only populated for immediate responses (Ignore, Fail)
    string? ErrorMessage = null,
    Exception? Exception = null);
