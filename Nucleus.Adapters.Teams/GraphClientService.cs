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
using System.Threading.Tasks;

namespace Nucleus.Adapters.Teams
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
        private async Task<GraphServiceClient> EnsureGraphClientInitializedAsync()
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
                var client = await EnsureGraphClientInitializedAsync(); // Use updated init method name
                _logger.LogInformation("Attempting to list files in primary channel files folder for Team ID: {TeamId}", _config.TargetTeamId);

                // 1. Get the Drive associated with the Team (Group)
                var teamDrive = await client.Groups[_config.TargetTeamId].Drive.GetAsync();
                if (teamDrive?.Id == null)
                {
                    _logger.LogWarning("Could not find Drive for Team ID: {TeamId}", _config.TargetTeamId);
                    return null; // Or empty list
                }
                _logger.LogInformation("Found Drive ID: {DriveId} for Team ID: {TeamId}", teamDrive.Id, _config.TargetTeamId);

                // 2. Get the DriveItem representing the primary channel's files folder
                var filesFolder = await client.Teams[_config.TargetTeamId].PrimaryChannel.FilesFolder.GetAsync();
                if (filesFolder?.Id == null)
                {
                    _logger.LogWarning("Could not find primary channel FilesFolder for Team ID: {TeamId}", _config.TargetTeamId);
                    return null; // Or empty list
                }
                _logger.LogInformation("Found FilesFolder DriveItem ID: {FolderId}", filesFolder.Id);

                // 3. List the children of that folder DriveItem within the Team's Drive
                var driveItemsResponse = await client.Drives[teamDrive.Id].Items[filesFolder.Id].Children.GetAsync();

                if (driveItemsResponse?.Value != null)
                {
                    _logger.LogInformation("Found {Count} items in the folder ID {FolderId} for Team ID: {TeamId}", driveItemsResponse.Value.Count, filesFolder.Id, _config.TargetTeamId);
                    // Filter for actual files (items that are not folders)
                    var files = driveItemsResponse.Value.Where(item => item.Folder == null).ToList();
                    _logger.LogInformation("Found {Count} files in the folder ID {FolderId} for Team ID: {TeamId}", files.Count, filesFolder.Id, _config.TargetTeamId);
                    return files;
                }
                else
                {
                    _logger.LogWarning("No items found or response was null for folder ID {FolderId}, Team ID: {TeamId}", filesFolder.Id, _config.TargetTeamId);
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

        // TODO: Add method GetFileContentAsync(string fileId, string driveId) - Needs Drive ID too
        // TODO: Add method FindFileByNameAsync(string fileName)
    }
}
