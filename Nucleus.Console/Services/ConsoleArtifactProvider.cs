// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

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
    public string SupportedArtifactType => "localfile";

    /// <inheritdoc />
    public Task<Stream?> GetArtifactContentAsync(ArtifactReference reference)
    {
        if (reference == null || reference.Type != SupportedArtifactType || string.IsNullOrWhiteSpace(reference.Identifier))
        {
            _logger.LogWarning("Invalid artifact reference provided to ConsoleArtifactProvider. Type: {Type}, Identifier: {Identifier}", reference?.Type, reference?.Identifier);
            return Task.FromResult<Stream?>(null);
        }

        var filePath = reference.Identifier;
        _logger.LogDebug("Attempting to retrieve local file artifact: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("Local file artifact not found: {FilePath}", filePath);
                return Task.FromResult<Stream?>(null);
            }

            // Open the file for reading. The caller is responsible for disposing the stream.
            Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogInformation("Successfully opened local file artifact for reading: {FilePath}", filePath);
            return Task.FromResult<Stream?>(fileStream);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error accessing local file artifact: {FilePath}", filePath);
            return Task.FromResult<Stream?>(null);
        }
        catch (UnauthorizedAccessException ex)
        {
             _logger.LogError(ex, "Permission denied accessing local file artifact: {FilePath}", filePath);
            return Task.FromResult<Stream?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving local file artifact: {FilePath}", filePath);
            return Task.FromResult<Stream?>(null);
        }
    }
}
