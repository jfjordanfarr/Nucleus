// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Adapters.Teams;

/// <summary>
/// Implements <see cref="IPlatformAttachmentFetcher"/> for Microsoft Teams.
/// Uses the <see cref="GraphClientService"/> to download files referenced by their Graph DriveItem ID.
/// Assumes the necessary Graph permissions are granted and the <see cref="GraphClientService"/> is configured.
/// See: ../../Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_FETCHER.md (Assuming this doc exists or will be created)
/// </summary>
public class TeamsGraphFileFetcher : IPlatformAttachmentFetcher
{
    private readonly GraphClientService _graphClientService;
    private readonly ILogger<TeamsGraphFileFetcher> _logger;

    public string SupportedPlatformType => "Teams";

    public TeamsGraphFileFetcher(GraphClientService graphClientService, ILogger<TeamsGraphFileFetcher> logger)
    {
        _graphClientService = graphClientService ?? throw new ArgumentNullException(nameof(graphClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<(Stream? FileStream, string? FileName, string? ContentType, string? Error)> GetAttachmentStreamAsync(
        PlatformAttachmentReference reference,
        CancellationToken cancellationToken = default)
    {
        if (reference is null || string.IsNullOrWhiteSpace(reference.PlatformSpecificId))
        {
            _logger.LogError("Cannot fetch attachment: PlatformAttachmentReference or its PlatformSpecificId is null or empty.");
            return (null, null, null, "Invalid attachment reference provided.");
        }

        // Extract DriveId from context. Teams attachments require this.
        if (reference.PlatformContext == null || !reference.PlatformContext.TryGetValue("DriveId", out var driveId) || string.IsNullOrWhiteSpace(driveId))
        {
            _logger.LogError("Cannot fetch Teams attachment: 'DriveId' is missing from PlatformContext for PlatformSpecificId: {PlatformSpecificId}", reference.PlatformSpecificId);
            return (null, reference.FileName, reference.ContentType, "Missing required 'DriveId' in attachment context.");
        }

        // The PlatformSpecificId for Teams attachments *should* be the Graph DriveItem ID.
        string driveItemId = reference.PlatformSpecificId;
        string? expectedFileName = reference.FileName; // Use provided filename if available

        _logger.LogInformation("Attempting to fetch Teams attachment (Drive ID: {DriveId}, DriveItem ID: {DriveItemId}) using GraphClientService.", driveId, driveItemId);

        try
        {
            // Call the new method in GraphClientService, providing both DriveId and DriveItemId.
            var (contentStream, actualFileName, actualContentType, graphError) = await _graphClientService.DownloadDriveItemContentStreamAsync(driveId, driveItemId, cancellationToken);

            if (contentStream != null)
            {
                // Prefer Graph's filename/content type if available, otherwise use the reference's hint.
                string finalFileName = actualFileName ?? expectedFileName ?? "downloaded_file";
                string? finalContentType = actualContentType ?? reference.ContentType;
                _logger.LogInformation("Successfully fetched attachment {FileName} ({ContentType}) for Drive ID: {DriveId}, DriveItem ID: {DriveItemId}.", finalFileName, finalContentType, driveId, driveItemId);
                return (contentStream, finalFileName, finalContentType, null);
            }
            else
            {
                string errorMessage = graphError ?? "GraphClientService returned null stream without specific error.";
                _logger.LogError("Failed to fetch attachment for Drive ID: {DriveId}, DriveItem ID: {DriveItemId}. Error: {ErrorMessage}", driveId, driveItemId, errorMessage);
                return (null, null, null, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching attachment for Drive ID: {DriveId}, DriveItem ID: {DriveItemId}. Error: {ErrorMessage}", driveId, driveItemId, ex.Message);
            return (null, null, null, $"Failed to fetch attachment: {ex.Message}");
        }
    }
}
