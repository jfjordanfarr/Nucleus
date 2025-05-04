// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// Architecture: See [ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md](../../../Docs/Architecture/ClientAdapters/Console/ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md)
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Nucleus.Infrastructure.Providers;

/// <summary>
/// Provides artifact content for local files referenced by the Console adapter.
/// See: Docs/Architecture/ClientAdapters/Console/ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md
/// <seealso cref="../../../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </summary>
public class ConsoleArtifactProvider : IArtifactProvider
{
    private readonly ILogger<ConsoleArtifactProvider> _logger;

    public ConsoleArtifactProvider(ILogger<ConsoleArtifactProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    // Define the specific reference type this provider handles
    public const string FileReferenceType = "file";

    /// <inheritdoc />
    public IEnumerable<string> SupportedReferenceTypes => new[] { FileReferenceType };

    /// <inheritdoc />
    public Task<ArtifactContent?> GetContentAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default)
    {
        // Validate the reference type and ID
        if (reference == null ||
            !reference.ReferenceType.Equals(FileReferenceType, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(reference.ReferenceId))
        {
            _logger.LogWarning("Invalid or unsupported artifact reference provided to ConsoleArtifactProvider. ReferenceType: {Type}, ReferenceId: {Id}", reference?.ReferenceType, reference?.ReferenceId);
            return Task.FromResult<ArtifactContent?>(null); // Return null Task for invalid/unsupported refs
        }

        // Assume ReferenceId is the full file path for this provider
        var filePath = reference.ReferenceId;
        _logger.LogDebug("Attempting to retrieve local file artifact: {FilePath}", filePath);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
            {
                _logger.LogError("Local file artifact not found: {FilePath}", filePath);
                return Task.FromResult<ArtifactContent?>(null); // Return null Task if file doesn't exist
            }

            // Determine FileName and MimeType from the path/reference
            var fileName = Path.GetFileName(filePath);
            var mimeType = reference.MimeType; // Use provided MimeType if available
            // TODO: Consider using a library to infer MimeType from extension if null

            // Open the file stream
            // Note: FileStream is IAsyncDisposable, but ArtifactContent takes Stream (IDisposable).
            // The consumer of ArtifactContent (e.g., Persona) is responsible for disposing the stream.
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogInformation("Opened file stream for {FilePath}", filePath);

            // Create ArtifactContent object
            var artifactContent = new ArtifactContent(
                originalReference: reference,
                contentStream: fileStream, // Transfer ownership of the stream
                contentType: mimeType     // Use potentially null MimeType from reference
            );

            // Return the Task containing the result
            return Task.FromResult<ArtifactContent?>(artifactContent);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation cancelled while retrieving file {FilePath}", filePath);
            return Task.FromResult<ArtifactContent?>(null); // Return null Task on cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving local file artifact {FilePath}: {ErrorMessage}", filePath, ex.Message);
            return Task.FromResult<ArtifactContent?>(null); // Return null Task on error
        }
    }
}
