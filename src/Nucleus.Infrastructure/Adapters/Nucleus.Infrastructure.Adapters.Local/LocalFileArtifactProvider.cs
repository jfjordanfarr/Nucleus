// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// Architecture: See [ARCHITECTURE_ADAPTERS_LOCAL.md](../../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md)
// Note: The above link will need to be adjusted based on the final file location if it moves from a "Providers" subfolder equivalent.
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic; // Added for IEnumerable

namespace Nucleus.Infrastructure.Adapters.Local // Updated namespace
{

    /// <summary>
    /// Provides artifact content for local files.
    /// This provider is typically used in scenarios where the application has direct access to the file system,
    /// such as during local development, testing, or for the LocalAdapter.
    /// See: Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md
    /// <seealso cref="../../../../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
    /// </summary>
    public class LocalFileArtifactProvider : IArtifactProvider
    {
        private readonly ILogger<LocalFileArtifactProvider> _logger;

        public LocalFileArtifactProvider(ILogger<LocalFileArtifactProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public const string FileReferenceType = "file";

        /// <inheritdoc />
        public IEnumerable<string> SupportedReferenceTypes => new[] { FileReferenceType };

        /// <inheritdoc />
        public Task<ArtifactContent?> GetContentAsync(
            ArtifactReference reference,
            CancellationToken cancellationToken = default)
        {
            if (reference == null ||
                !reference.ReferenceType.Equals(FileReferenceType, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(reference.ReferenceId))
            {
                _logger.LogWarning("Invalid or unsupported artifact reference provided to LocalFileArtifactProvider. ReferenceType: {Type}, ReferenceId: {Id}", reference?.ReferenceType, reference?.ReferenceId);
                return Task.FromResult<ArtifactContent?>(null);
            }

            var filePath = reference.ReferenceId;
            _logger.LogDebug("Attempting to retrieve local file artifact: {FilePath}", filePath);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(filePath))
                {
                    _logger.LogError("Local file artifact not found: {FilePath}", filePath);
                    return Task.FromResult<ArtifactContent?>(null);
                }

                var fileName = Path.GetFileName(filePath);
                var mimeType = reference.MimeType;
                
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _logger.LogInformation("Opened file stream for {FilePath}", filePath);

                var artifactContent = new ArtifactContent(
                    originalReference: reference,
                    contentStream: fileStream, 
                    contentType: mimeType
                );

                return Task.FromResult<ArtifactContent?>(artifactContent);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled while retrieving file {FilePath}", filePath);
                return Task.FromResult<ArtifactContent?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving local file artifact {FilePath}: {ErrorMessage}", filePath, ex.Message);
                return Task.FromResult<ArtifactContent?>(null);
            }
        }
    }
}
