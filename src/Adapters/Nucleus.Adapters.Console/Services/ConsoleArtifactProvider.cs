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

namespace Nucleus.Console.Services;

/// <summary>
/// Provides artifact content for local files referenced by the Console adapter.
/// See: Docs/Architecture/ClientAdapters/Console/ARCHITECTURE_ADAPTERS_CONSOLE_INTERFACES.md
/// </summary>
public class ConsoleArtifactProvider : IArtifactProvider
{
    private readonly ILogger<ConsoleArtifactProvider> _logger;

    public ConsoleArtifactProvider(ILogger<ConsoleArtifactProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string SupportedPlatformType => "ConsoleFile";

    /// <inheritdoc />
    public Task<(Stream? Stream, string? FileName, string? MimeType, string? Error)> GetArtifactStreamAsync(
        ArtifactReference reference,
        CancellationToken cancellationToken = default)
    {
        if (reference == null || reference.PlatformType != SupportedPlatformType || string.IsNullOrWhiteSpace(reference.ArtifactId))
        {
            _logger.LogWarning("Invalid artifact reference provided to ConsoleArtifactProvider. PlatformType: {Type}, ArtifactId: {Identifier}", reference?.PlatformType, reference?.ArtifactId);
            return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, null, null, "Invalid artifact reference."));
        }

        var filePath = reference.ArtifactId;
        _logger.LogDebug("Attempting to retrieve local file artifact: {FilePath}", filePath);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
            {
                _logger.LogError("Local file artifact not found: {FilePath}", filePath);
                return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, Path.GetFileName(filePath), null, "File not found."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            _logger.LogInformation("Successfully opened local file artifact for reading: {FilePath}", filePath);

            string? fileName = Path.GetFileName(filePath);
            string? mimeType = null; 

            return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((fileStream, fileName, mimeType, null));
        }
        catch (OperationCanceledException)
        {
             _logger.LogInformation("File retrieval cancelled for: {FilePath}", filePath);
             return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, Path.GetFileName(filePath), null, "Operation cancelled."));
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error accessing local file artifact: {FilePath}", filePath);
            return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, Path.GetFileName(filePath), null, $"IO error: {ex.Message}"));
        }
        catch (UnauthorizedAccessException ex)
        {
             _logger.LogError(ex, "Permission denied accessing local file artifact: {FilePath}", filePath);
             return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, Path.GetFileName(filePath), null, $"Permission denied: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving local file artifact: {FilePath}", filePath);
            return Task.FromResult<(Stream? Stream, string? FileName, string? MimeType, string? Error)>((null, Path.GetFileName(filePath), null, $"Unexpected error: {ex.Message}"));
        }
    }
}
