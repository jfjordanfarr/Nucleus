using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Services.Api.Infrastructure;

/// <summary>
/// A null implementation of IArtifactProvider used in contexts (like the core API service)
/// where direct artifact retrieval is not expected or supported. It fulfills DI requirements
/// but logs a warning if its methods are invoked.
/// <seealso cref="../../../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
/// </summary>
public class NullArtifactProvider : IArtifactProvider
{
    private readonly ILogger<NullArtifactProvider> _logger;

    public NullArtifactProvider(ILogger<NullArtifactProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the platform type supported by this provider.
    /// </summary>
    /// <value>"Null"</value>
    public string SupportedPlatformType => "Null";

    /// <inheritdoc />
    public IEnumerable<string> SupportedReferenceTypes => Enumerable.Empty<string>();

    /// <inheritdoc />
    public Task<ArtifactContent?> GetContentAsync(ArtifactReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _logger.LogWarning("NullArtifactProvider was asked to handle reference type '{ReferenceType}' for SourceUri '{SourceUri}'. Returning null as it supports no types.",
            reference.ReferenceType, reference.SourceUri);
        // Return a completed task with a null result.
        return Task.FromResult<ArtifactContent?>(null);
    }

    /// <summary>
    /// Attempts to retrieve an artifact stream. Logs a warning as this provider should not be actively used.
    /// </summary>
    /// <param name="reference">The artifact reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple indicating failure (null stream, null filename, null mimetype, error message).</returns>
    public Task<(Stream? Stream, string? FileName, string? MimeType, string? Error)> GetArtifactStreamAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("NullArtifactProvider.GetArtifactStreamAsync invoked for platform '{ReferenceType}' artifact '{ReferenceId}'. This indicates an unexpected attempt to retrieve artifacts directly within the API service.", 
            reference?.ReferenceType ?? "unknown", 
            reference?.ReferenceId ?? "unknown");

        // Return nulls and an error message
        return Task.FromResult<(Stream?, string?, string?, string?)>(
            (null, null, null, "Artifact retrieval is not supported in this service context.")
        );
    }
}
