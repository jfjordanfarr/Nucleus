// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Adapters.Teams
{
    /// <summary>
    /// Provides authenticated access to Microsoft Graph API for Teams/SharePoint operations.
    /// Handles authentication using application credentials.
    /// See: Docs/Architecture/ClientAdapters/Teams/ARCHITECTURE_ADAPTERS_TEAMS_INTERFACES.md
    /// </summary>
    public class GraphClientService
    {
        private readonly ILogger<GraphClientService> _logger;
        private readonly TeamsAdapterConfiguration _config;
        private GraphServiceClient? _graphClient;

        public GraphClientService(IOptions<TeamsAdapterConfiguration> configOptions, ILogger<GraphClientService> logger)
        {
            ArgumentNullException.ThrowIfNull(configOptions);
            _logger = logger;
            _config = configOptions.Value;

            // Validate essential configuration
            if (string.IsNullOrWhiteSpace(_config.MicrosoftAppId) ||
                string.IsNullOrWhiteSpace(_config.MicrosoftAppPassword) ||
                string.IsNullOrWhiteSpace(_config.TargetTeamId))
            {
                _logger.LogError("Required TeamsAdapterConfiguration missing (MicrosoftAppId, MicrosoftAppPassword, TargetTeamId).");
                throw new InvalidOperationException("Required Teams adapter configuration is missing. Check appsettings.");
            }
        }

        // Renamed for clarity - initializes and returns the client
        // Made synchronous as no await calls were present (CS1998)
        private GraphServiceClient EnsureGraphClientInitialized()
        {
            if (_graphClient != null) return _graphClient;

            _logger.LogInformation("Initializing GraphServiceClient...");

            try
            {
                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(_config.MicrosoftAppId)
                    .WithClientSecret(_config.MicrosoftAppPassword)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{_config.TenantId ?? "organizations"}"))
                    .Build();

                string[] scopes = ["https://graph.microsoft.com/.default"];

                // Use BaseBearerTokenAuthenticationProvider which handles token acquisition internally
                // Requires Microsoft.Graph.Core package >= 3.0.0
                var tokenAuthProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(confidentialClient, scopes, _logger));

                _graphClient = new GraphServiceClient(tokenAuthProvider);

                _logger.LogInformation("GraphServiceClient initialized successfully.");
                return _graphClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing GraphServiceClient: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Lists files within the primary channel's files folder for the configured Team ID.
        /// </summary>
        /// <returns>A list of DriveItems representing the files, or null if an error occurs.</returns>
        public async Task<List<DriveItem>?> ListTeamFilesAsync()
        {
            try
            {
                var client = EnsureGraphClientInitialized(); // Remove await, use updated method name
                _logger.LogInformation("Attempting to list files for Team ID: {TeamId}. Path: '{Path}'", _config.TargetTeamId, _config.TargetSharePointLibraryPath);

                // 1. Get the Drive associated with the Team (Group)
                var teamDrive = await client.Groups[_config.TargetTeamId].Drive.GetAsync();
                if (teamDrive?.Id == null)
                {
                    _logger.LogWarning("Could not find Drive for Team ID: {TeamId}", _config.TargetTeamId);
                    return null; // Or empty list
                }
                _logger.LogInformation("Found Drive ID: {DriveId} for Team ID: {TeamId}", teamDrive.Id, _config.TargetTeamId);

                DriveItem? targetFolderItem = null;

                // 2. Determine the target folder DriveItem
                if (!string.IsNullOrEmpty(_config.TargetSharePointLibraryPath))
                {
                    // Path is specified, try to get it relative to the drive root
                    _logger.LogInformation("TargetSharePointLibraryPath specified: '{Path}'. Attempting to get DriveItem relative to root.", _config.TargetSharePointLibraryPath);
                    try
                    {
                        // Ensure path doesn't start with '/', as ItemWithPath expects path relative to root.
                        var relativePath = _config.TargetSharePointLibraryPath.TrimStart('/');
                        targetFolderItem = await client.Drives[teamDrive.Id].Root.ItemWithPath(relativePath).GetAsync();
                        if (targetFolderItem == null || targetFolderItem.Folder == null)
                        {
                            _logger.LogWarning("Path '{Path}' found but is not a folder for Team ID: {TeamId}. Falling back to Primary Channel FilesFolder.", _config.TargetSharePointLibraryPath, _config.TargetTeamId);
                            targetFolderItem = null; // Reset to trigger fallback
                        }
                        else
                        {
                            _logger.LogInformation("Found DriveItem ID: {FolderId} for specified path: '{Path}'", targetFolderItem.Id, _config.TargetSharePointLibraryPath);
                        }
                    }
                    catch (ODataError odataError) when (odataError.ResponseStatusCode == 404) // Catch specifically 'Not Found'
                    {
                        _logger.LogWarning(odataError, "Specified path '{Path}' not found for Team ID: {TeamId}. Falling back to Primary Channel FilesFolder.", _config.TargetSharePointLibraryPath, _config.TargetTeamId);
                        targetFolderItem = null; // Explicitly set to null to trigger fallback logic
                    }
                    // Let other ODataErrors or exceptions propagate up
                }

                // Fallback or default: Use the Primary Channel's Files Folder
                if (targetFolderItem == null)
                {
                    _logger.LogInformation("Using Primary Channel FilesFolder as target.");
                    targetFolderItem = await client.Teams[_config.TargetTeamId].PrimaryChannel.FilesFolder.GetAsync();
                }

                // Check if we successfully determined a target folder
                if (targetFolderItem?.Id == null)
                {
                    _logger.LogWarning("Could not determine a valid target folder (checked path and primary channel) for Team ID: {TeamId}", _config.TargetTeamId);
                    return null; // Or empty list
                }
                _logger.LogInformation("Using target folder DriveItem ID: {FolderId}", targetFolderItem.Id);

                // 3. List the children of the determined target folder DriveItem
                var driveItemsResponse = await client.Drives[teamDrive.Id].Items[targetFolderItem.Id].Children.GetAsync();

                if (driveItemsResponse?.Value != null)
                {
                    _logger.LogInformation("Found {Count} items in the target folder ID {FolderId} for Team ID: {TeamId}", driveItemsResponse.Value.Count, targetFolderItem.Id, _config.TargetTeamId);
                    // Filter for actual files (items that are not folders)
                    var files = driveItemsResponse.Value.Where(item => item.Folder == null).ToList();
                    _logger.LogInformation("Found {Count} files in the target folder ID {FolderId} for Team ID: {TeamId}", files.Count, targetFolderItem.Id, _config.TargetTeamId);
                    return files;
                }
                else
                {
                    _logger.LogWarning("No items found or response was null for target folder ID {FolderId}, Team ID: {TeamId}", targetFolderItem.Id, _config.TargetTeamId);
                    return new List<DriveItem>(); // Return empty list instead of null
                }
            }
            // Catch ODataError exceptions specifically if possible, otherwise ServiceException
            catch (ODataError odataError) when (odataError.Error != null)
            {
                _logger.LogError(odataError, "Microsoft Graph OData error listing files for Team ID {TeamId}: {ErrorCode} - {ErrorMessage}",
                                _config.TargetTeamId, odataError.Error.Code, odataError.Error.Message);
                return null;
            }
            catch (ServiceException graphEx)
            {
                // Log Graph API specific errors using status code and message
                _logger.LogError(graphEx, "Microsoft Graph ServiceException listing files for Team ID {TeamId}: {StatusCode} - {ErrorMessage}",
                                _config.TargetTeamId, graphEx.ResponseStatusCode, graphEx.Message); // Use graphEx.Message
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error listing files for Team ID {TeamId}: {ErrorMessage}", _config.TargetTeamId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Downloads the content stream and metadata for a specific DriveItem.
        /// </summary>
        /// <param name="driveId">The ID of the Drive containing the item.</param>
        /// <param name="driveItemId">The ID of the DriveItem (file) to download.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A Task representing the asynchronous operation, containing a tuple:
        /// - <c>FileStream</c>: A readable Stream with the item's content. The caller is responsible for disposing this stream.
        /// - <c>FileName</c>: The actual filename from Graph.
        /// - <c>ContentType</c>: The MIME content type from Graph.
        /// - <c>Error</c>: An error message if retrieval failed, otherwise null.
        /// If retrieval fails, the FileStream will be null.
        /// </returns>
        public async Task<(Stream? FileStream, string? FileName, string? ContentType, string? Error)> DownloadDriveItemContentStreamAsync(
            string driveId,
            string driveItemId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(driveId) || string.IsNullOrWhiteSpace(driveItemId))
            {
                _logger.LogError("DownloadDriveItemContentStreamAsync called with null or empty driveId or driveItemId.");
                return (null, null, null, "Drive ID and Drive Item ID cannot be empty.");
            }

            try
            {
                var client = EnsureGraphClientInitialized(); // Remove await, use updated method name
                _logger.LogInformation("Attempting to download content for DriveItem ID: {DriveItemId} in Drive ID: {DriveId}", driveItemId, driveId);

                // 1. Get DriveItem metadata first to retrieve filename and content type
                DriveItem? driveItem = null;
                try
                {
                    driveItem = await client.Drives[driveId].Items[driveItemId].GetAsync(requestConfiguration =>
                    {
                        // Request specific properties needed
                        requestConfiguration.QueryParameters.Select = ["id", "name", "file"];
                    }, cancellationToken);

                    if (driveItem?.File == null)
                    {
                        _logger.LogWarning("DriveItem ID {DriveItemId} in Drive ID {DriveId} was found but is not a file (it might be a folder or other item type).", driveItemId, driveId);
                        return (null, driveItem?.Name, null, "The specified item is not a file.");
                    }
                    _logger.LogInformation("Retrieved metadata for file: {FileName} ({ContentType})", driveItem.Name, driveItem.File.MimeType);
                }
                catch (ODataError odataError) when (odataError.ResponseStatusCode == 404)
                {
                    _logger.LogError(odataError, "DriveItem ID {DriveItemId} not found in Drive ID {DriveId}.", driveItemId, driveId);
                    return (null, null, null, "File not found.");
                }
                catch (Exception metaEx)
                {
                    _logger.LogError(metaEx, "Error retrieving metadata for DriveItem ID {DriveItemId} in Drive ID {DriveId}: {ErrorMessage}", driveItemId, driveId, metaEx.Message);
                    // Decide if we should attempt to get content anyway? Probably not.
                    return (null, null, null, $"Error retrieving file metadata: {metaEx.Message}");
                }

                // If metadata retrieval failed, driveItem would be null or we'd have returned.
                if (driveItem == null) // Should not happen if previous checks passed, but safety first.
                {
                     _logger.LogError("Unexpected null DriveItem after metadata retrieval attempt for DriveItem ID: {DriveItemId}", driveItemId);
                     return (null, null, null, "Internal error retrieving file metadata.");
                }

                // 2. Get the content stream
                Stream? contentStream = null;
                try
                {
                    contentStream = await client.Drives[driveId].Items[driveItemId].Content.GetAsync(cancellationToken: cancellationToken);
                    if (contentStream == null)
                    {
                        // This is unexpected if the item is a file and metadata was retrieved.
                        _logger.LogError("Graph API returned a null content stream for DriveItem ID {DriveItemId} despite it being identified as a file.", driveItemId);
                        return (null, driveItem.Name, driveItem.File?.MimeType, "Failed to retrieve file content stream from Graph API.");
                    }

                    _logger.LogInformation("Successfully retrieved content stream for DriveItem ID: {DriveItemId}", driveItemId);
                    return (contentStream, driveItem.Name, driveItem.File?.MimeType, null); // Success
                }
                catch (Exception contentEx)
                {
                    _logger.LogError(contentEx, "Error retrieving content stream for DriveItem ID {DriveItemId} in Drive ID {DriveId}: {ErrorMessage}", driveItemId, driveId, contentEx.Message);
                    // Clean up stream if partially created? GetAsync likely handles this.
                    if (contentStream != null)
                    {
                         await contentStream.DisposeAsync(); // Dispose if exception occurred after creation
                    }
                    return (null, driveItem.Name, driveItem.File?.MimeType, $"Error downloading file content: {contentEx.Message}");
                }
            }
            catch (ODataError odataError) when (odataError.Error != null)
            {
                _logger.LogError(odataError, "Microsoft Graph OData error downloading file for DriveItem ID {DriveItemId} in Drive ID {DriveId}: {ErrorCode} - {ErrorMessage}",
                                 driveItemId, driveId, odataError.Error.Code, odataError.Error.Message);
                return (null, null, null, $"Graph API Error: {odataError.Error.Code} - {odataError.Error.Message}");
            }
            catch (ServiceException graphEx)
            {
                _logger.LogError(graphEx, "Microsoft Graph ServiceException downloading file for DriveItem ID {DriveItemId} in Drive ID {DriveId}: {StatusCode} - {ErrorMessage}",
                                 driveItemId, driveId, graphEx.ResponseStatusCode, graphEx.Message);
                return (null, null, null, $"Graph Service Error: {graphEx.ResponseStatusCode} - {graphEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error downloading file for DriveItem ID {DriveItemId} in Drive ID {DriveId}: {ErrorMessage}", driveItemId, driveId, ex.Message);
                return (null, null, null, $"General Error: {ex.Message}");
            }
        }

        // Helper class for BaseBearerTokenAuthenticationProvider
        private class TokenProvider : IAccessTokenProvider
        {
            private readonly IConfidentialClientApplication _confidentialClient;
            private readonly string[] _scopes;
            private readonly ILogger _logger;

            public TokenProvider(IConfidentialClientApplication confidentialClient, string[] scopes, ILogger logger)
            {
                _confidentialClient = confidentialClient;
                _scopes = scopes;
                _logger = logger;
            }

            public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            {
                var result = await _confidentialClient.AcquireTokenForClient(_scopes).ExecuteAsync(cancellationToken);
                _logger.LogInformation("Acquired Graph API token."); // Simple log, avoid logging the token itself
                return result.AccessToken;
            }

            public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator(new[] { "graph.microsoft.com" }); // Restrict to Graph host
        }
    }
}
