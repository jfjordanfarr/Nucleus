// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nucleus.Infrastructure.Adapters.Teams
{
    /// <summary>
    /// Configuration settings for the Teams Adapter.
    /// Typically populated from `appsettings.json`.
    /// </summary>
    /// <seealso cref="../../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_TEAMS.md"/>
    public class TeamsAdapterConfiguration
    {
        /// <summary>
        /// The Microsoft Application ID for the Azure Bot registration.
        /// </summary>
        public string? MicrosoftAppId { get; set; }

        /// <summary>
        /// The Microsoft Application Password (client secret) for the Azure Bot registration.
        /// </summary>
        public string? MicrosoftAppPassword { get; set; }

        /// <summary>
        /// Optional: The specific Tenant ID if the bot is single-tenant.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// The Microsoft Graph Group ID for the target Team (e.g., "IT Applications").
        /// </summary>
        public string? TargetTeamId { get; set; }

        /// <summary>
        /// The relative path within the Team's default SharePoint site's Documents library 
        /// where relevant files reside (e.g., "General" for the General channel's files).
        /// If null or empty, the root of the default Documents library is assumed.
        /// </summary>
        public string? TargetSharePointLibraryPath { get; set; }

        /// <summary>
        /// The base URL for the Nucleus backend API endpoint that the adapter should send ingestion requests to.
        /// Example: "https://your-nucleus-api.azurewebsites.net"
        /// </summary>
        public string? NucleusApiEndpoint { get; set; }
    }
}
