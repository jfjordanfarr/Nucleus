// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Models.Configuration; 
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories; 
using Nucleus.Domain.Personas.Core.Interfaces;

namespace Nucleus.Domain.Personas.Core.Strategies;

/// <summary>
/// An agentic strategy handler that saves artifact metadata based on the interaction.
/// It specifically populates OriginalFileName from the QueryText.
/// </summary>
public class MetadataSavingStrategyHandler : IAgenticStrategyHandler
{
    public string StrategyKey => "MetadataSaver"; // Unique key for this strategy

    private readonly ILogger<MetadataSavingStrategyHandler> _logger;
    private readonly IArtifactMetadataRepository _metadataRepository;

    public MetadataSavingStrategyHandler(
        ILogger<MetadataSavingStrategyHandler> logger,
        IArtifactMetadataRepository metadataRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
    }

    public async Task<AdapterResponse> HandleAsync(
        PersonaConfiguration personaConfig, 
        AgenticStrategyParametersBase? strategyParameters,
        InteractionContext interactionContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personaConfig);

        if (interactionContext?.OriginalRequest == null)
        {
            _logger.LogWarning("{StrategyKey}: InteractionContext or OriginalRequest is null. Cannot save metadata.", StrategyKey);
            return new AdapterResponse(Success: false, ResponseMessage: "Internal error: Interaction context not available.");
        }

        _logger.LogInformation("{StrategyKey}: Executing for persona '{PersonaId}'. Saving metadata for QueryText: {QueryText}",
            StrategyKey, personaConfig.PersonaId, interactionContext.OriginalRequest.QueryText);

        try
        {
            // Helper to map PlatformType to SourceSystemType
            static SourceSystemType GetSourceSystemType(PlatformType platformType)
            {
                return platformType switch
                {
                    PlatformType.Teams => SourceSystemType.MicrosoftTeams,
                    PlatformType.Slack => SourceSystemType.Slack,
                    PlatformType.Email => SourceSystemType.Email,
                    // Consider mapping for Console, Api, TestHarness if they have analogous SourceSystemTypes
                    _ => SourceSystemType.Unknown, // Default for Unmapped or Unknown PlatformTypes
                };
            }

            var originalRequest = interactionContext.OriginalRequest;
            var tenantId = originalRequest.TenantId ?? "unknown_tenant";
            var senderId = originalRequest.UserId ?? "unknown_sender";
            // Use MessageId if available, otherwise generate a new GUID for uniqueness in SourceIdentifier/Uri context
            var messageContextId = originalRequest.MessageId ?? Guid.NewGuid().ToString();

            var metadata = new ArtifactMetadata
            {
                // 'Id' is auto-generated with a new GUID by ArtifactMetadata constructor if not set.
                SourceIdentifier = $"userinteraction:{tenantId}:{senderId}:{messageContextId}",
                SourceSystemType = GetSourceSystemType(interactionContext.PlatformType),
                SourceUri = new System.Uri($"urn:nucleus:userinteraction:{tenantId}:{senderId}:{messageContextId}"),
                TenantId = tenantId,
                UserId = senderId, // Assuming SenderId is the relevant UserId for metadata ownership
                FileName = originalRequest.QueryText, // Using QueryText as the 'file name' for this interaction artifact
                MimeType = "text/plain", // As QueryText is treated as plain text
                SizeBytes = originalRequest.QueryText?.Length ?? 0,
                PlatformContext = new Dictionary<string, string> 
                {
                    { "originalPlatformType", interactionContext.PlatformType.ToString() },
                    { "originalMessageId", messageContextId }
                }
                // Removed: ArtifactId (use Id), OriginalFileName (use FileName), SourcePlatform, 
                // SourceType (handled by SourceSystemType), ProcessedDate, Checksum, IsEphemeral, Status.
                // Properties like Title, Author, Summary, Tags can be populated if available/relevant.
            };

            // IArtifactMetadataRepository.SaveAsync does not take a CancellationToken
            ArtifactMetadata savedMetadata = await _metadataRepository.SaveAsync(metadata);

            _logger.LogInformation("{StrategyKey}: Successfully saved ArtifactMetadata with Id '{MetadataId}' for FileName: {FileName}",
                StrategyKey, savedMetadata.Id, savedMetadata.FileName);

            var responseMessage = $"Metadata saved for query: {originalRequest.QueryText}";
            return new AdapterResponse(Success: true, ResponseMessage: responseMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{StrategyKey}: Error saving ArtifactMetadata for QueryText: {QueryText}",
                StrategyKey, interactionContext.OriginalRequest.QueryText);
            return new AdapterResponse(
                Success: false, 
                ResponseMessage: "Failed to save artifact metadata due to an internal error.",
                ErrorMessage: ex.Message); 
        }
    }
}
