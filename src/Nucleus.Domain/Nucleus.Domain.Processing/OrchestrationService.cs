// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Domain.Personas.Core.Interfaces; // For IPersonaRuntime
using Nucleus.Services.Shared.Extraction; // Added for IContentExtractor
using System.Diagnostics;
using System.Threading;
using Nucleus.Services.Shared; // Added for MimeTypeHelper
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Core service responsible for orchestrating the processing of incoming interactions.
/// See: [ARCHITECTURE_PROCESSING_ORCHESTRATION.md](../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly ILogger<OrchestrationService> _logger;
    private readonly IPersonaResolver _personaResolver;
    private readonly IPersonaRuntime _personaRuntime;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;
    private readonly IEnumerable<IArtifactProvider> _artifactProviders;
    private readonly IEnumerable<IContentExtractor> _contentExtractors;
    private readonly IArtifactMetadataRepository _metadataRepository;
    private readonly ActivationChecker _activationChecker;
    private readonly IPersonaConfigurationProvider _personaConfigurationProvider;

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaResolver personaResolver,
        IPersonaRuntime personaRuntime,
        IServiceProvider serviceProvider,
        ActivitySource activitySource,
        IEnumerable<IArtifactProvider> artifactProviders,
        IEnumerable<IContentExtractor> contentExtractors,
        IArtifactMetadataRepository metadataRepository,
        ActivationChecker activationChecker,
        IPersonaConfigurationProvider personaConfigurationProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver));
        _personaRuntime = personaRuntime ?? throw new ArgumentNullException(nameof(personaRuntime));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));
        _contentExtractors = contentExtractors ?? throw new ArgumentNullException(nameof(contentExtractors));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        _activationChecker = activationChecker ?? throw new ArgumentNullException(nameof(activationChecker));
        _personaConfigurationProvider = personaConfigurationProvider ?? throw new ArgumentNullException(nameof(personaConfigurationProvider));
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); // CA1062 Fix: Validate request parameter

        // Use MessageId or ConversationId for logging correlation
        var interactionId = request.MessageId ?? request.ConversationId ?? Guid.NewGuid().ToString();
        using var activity = _activitySource.StartActivity("ProcessInteraction", ActivityKind.Internal, default(ActivityContext), tags: new[]
        {
            new KeyValuePair<string, object?>("nucleus.interaction.id", interactionId),
            new KeyValuePair<string, object?>("nucleus.platform.type", request.PlatformType),
            new KeyValuePair<string, object?>("nucleus.user.id", request.UserId),
            new KeyValuePair<string, object?>("nucleus.conversation.id", request.ConversationId)
        });

        _logger.LogInformation("Starting interaction processing for ID: {InteractionId}, Platform: {PlatformType}, User: {UserId}",
            interactionId, request.PlatformType, request.UserId);

        try
        {
            // 1. Parse PlatformType Enum
            if (!Enum.TryParse<PlatformType>(request.PlatformType, true, out var platformType))
            {
                _logger.LogWarning("Invalid PlatformType '{PlatformType}' received for interaction {InteractionId}.", request.PlatformType, interactionId);
                activity?.SetStatus(ActivityStatusCode.Error, $"Invalid PlatformType: {request.PlatformType}");
                // Return a failure result indicating the issue.
                return new OrchestrationResult(OrchestrationStatus.Failed,
                    ErrorMessage: $"Invalid PlatformType: {request.PlatformType}");
            }

            // 2. Resolve Persona ID
            using var resolvePersonaActivity = StartActivity("ResolvePersonaId");
            var personaId = await _personaResolver.ResolvePersonaIdAsync(
                platformType, // Pass parsed platformType enum
                request, 
                cancellationToken).ConfigureAwait(false);

            // Persona resolved successfully
            activity?.AddTag("nucleus.persona.id", personaId);
            _logger.LogInformation("Resolved Persona {PersonaId} for interaction {InteractionId}.", personaId, interactionId);

            // 3. Fetch and Extract Artifact Content
            using var extractContentActivity = StartActivity("FetchAndExtractArtifacts");
            var extractedArtifacts = await FetchAndExtractArtifactsAsync(request, cancellationToken).ConfigureAwait(false);
            extractContentActivity?.SetTag("artifactCount", extractedArtifacts.Count);
            extractContentActivity?.SetTag("extractedCount", extractedArtifacts.Count(a => a != null)); // Assuming ExtractedArtifact is not null on success

            // 4. Create Interaction Context
            using var createContextActivity = StartActivity("CreateInteractionContext");
            var interactionContext = new InteractionContext(
                request, 
                platformType, // Use parsed platformType enum
                personaId, 
                extractedArtifacts); // Add missing extractedArtifacts

            createContextActivity?.SetTag("personaId", personaId);
            createContextActivity?.SetTag("platformType", platformType.ToString());

            // 5. Get Persona Configuration
            using var getConfigActivity = StartActivity("GetPersonaConfiguration");
            var personaConfig = personaId != null 
                ? await _personaConfigurationProvider.GetConfigurationAsync(personaId, cancellationToken).ConfigureAwait(false) 
                : null;
            getConfigActivity?.SetTag("personaId", personaId);
            getConfigActivity?.SetTag("configFound", personaConfig != null);

            // 6. Execute Persona Runtime
            using var executeActivity = StartActivity("ExecutePersonaRuntime");
            executeActivity?.SetTag("personaId", personaId);

            // Add null check for personaConfig (addresses CS8604 warning)
            if (personaConfig == null)
            {
                _logger.LogWarning("Persona configuration not found for ID '{PersonaId}'. Cannot execute runtime.", personaId);
                // Consider returning a specific error response or throwing a specific exception
                // For now, returning a generic error response
                return new OrchestrationResult(OrchestrationStatus.Failed, 
                    ErrorMessage: "Error: Persona configuration not found."); // Return failed OrchestrationResult
            }

            var response = await _personaRuntime.ExecuteAsync(
                personaConfig, 
                interactionContext, 
                cancellationToken).ConfigureAwait(false);

            // 7. Return OrchestrationResult (Success)
            // If persona execution succeeded, the AdapterResponse is the payload.
            // The OrchestrationResult signals success and includes the response.
            _logger.LogInformation("Interaction {InteractionId} processed successfully by Persona {PersonaId}. Success={Success}",
                interactionId, personaId, response.Success);
            activity?.SetStatus(ActivityStatusCode.Ok, "Interaction processed.");

            // We return Success status with the AdapterResponse from the runtime.
            return new OrchestrationResult(
                OrchestrationStatus.Success,
                ResolvedPersonaId: personaId,
                AdapterResponse: response);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during interaction processing {InteractionId}", interactionId);
            activity?.SetStatus(ActivityStatusCode.Error, "Unhandled interaction error.").AddException(ex); // Use AddException
            return new OrchestrationResult(OrchestrationStatus.UnhandledError, ErrorMessage: $"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<ExtractedArtifact>> FetchAndExtractArtifactsAsync(AdapterRequest request, CancellationToken cancellationToken)
    {
        var results = new List<ExtractedArtifact>();

        if (request.ArtifactReferences != null) // Add null check for CS8602
        {
            foreach (var reference in request.ArtifactReferences)
            {
                using var fetchLoopActivity = StartActivity("FetchAndExtractLoop.Iteration");
                fetchLoopActivity?.SetTag("referenceId", reference.ReferenceId);
                fetchLoopActivity?.SetTag("referenceType", reference.ReferenceType);
                fetchLoopActivity?.SetTag("sourceUri", reference.SourceUri);

                try
                {
                    // Construct a stable source identifier
                    // Example: teams:channelId:messageId:attachmentId (adjust based on actual platform details)
                    // This logic might need refinement based on how PlatformContext is structured.
                    string sourceIdentifier = $"{request.PlatformType}:{reference.TenantId}:{reference.ReferenceId}"; // Use reference.TenantId
                    fetchLoopActivity?.SetTag("sourceIdentifier", sourceIdentifier);

                    // Check if metadata already exists
                    using var checkMetadataActivity = StartActivity("CheckExistingMetadata");
                    var existingMetadata = await _metadataRepository.GetBySourceIdentifierAsync(sourceIdentifier).ConfigureAwait(false); // Use GetBySourceIdentifierAsync
                    checkMetadataActivity?.SetTag("foundExisting", existingMetadata != null);

                    ExtractedArtifact? extractedArtifact = null;

                    // Only fetch/extract if metadata doesn't exist (or implement logic for re-extraction if needed)
                    if (existingMetadata == null)
                    {
                        // Find a provider that supports this reference
                        var provider = _artifactProviders.FirstOrDefault(p => 
                            p.SupportedReferenceTypes.Contains(reference.ReferenceType, StringComparer.OrdinalIgnoreCase)); // CORRECTED: Use SupportedReferenceTypes
                        
                        if (provider != null)
                        {
                            using var providerActivity = StartActivity($"FetchArtifactFromProvider_{provider.GetType().Name}");
                            // Fetch content using IArtifactProvider
                            using var fetchContentActivity = StartActivity("FetchArtifactContent");
                            var content = await provider.GetContentAsync(reference, cancellationToken).ConfigureAwait(false);
                            fetchContentActivity?.SetTag("contentFetched", content != null);

                            if (content != null)
                            {
                                // Extract text using IContentExtractor
                                using var extractContentActivity = StartActivity("ExtractArtifactContent");
                                try
                                {
                                    var extractor = _contentExtractors.FirstOrDefault(e => e.SupportsMimeType(content.ContentType!)); // Use ! for CS8604
                                    ContentExtractionResult? extractionResult = null;
                                    if (extractor != null)
                                    {
                                        extractionResult = await extractor.ExtractContentAsync(
                                            content.ContentStream!, // Use ! for CS8604
                                            content.ContentType!, // Use ! for CS8604
                                            reference.SourceUri).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No content extractor found for MimeType: {MimeType}, SourceUri: {SourceUri}", content.ContentType, reference.SourceUri);
                                        // Consider setting a specific status if no extractor found
                                    }

                                    extractContentActivity?.SetTag("extractionStatus", extractionResult?.Status.ToString());

                                    if (extractionResult?.Status == ExtractionStatus.Success && extractionResult?.ExtractedText != null) // Use ExtractionStatus
                                    {
                                        // Create ExtractedArtifact for successful extraction
                                        extractedArtifact = new ExtractedArtifact(
                                            SourceId: sourceIdentifier, // Use the generated sourceIdentifier
                                            ExtractedText: extractionResult.ExtractedText,
                                            ContentType: content.ContentType!,
                                            SourceUri: reference.SourceUri);
                                        results.Add(extractedArtifact);
                                        _logger.LogInformation("Successfully extracted content for artifact: {SourceIdentifier}", sourceIdentifier);

                                        // Create and save new metadata
                                        await CreateAndSaveMetadataAsync(sourceIdentifier, reference, content, request, cancellationToken).ConfigureAwait(false);
                                    }
                                    else
                                    { 
                                        _logger.LogWarning("Content extraction failed or returned no text for artifact: {SourceIdentifier}. Status: {Status}, Error: {Message}", 
                                            sourceIdentifier, extractionResult?.Status, extractionResult?.Message);
                                        extractContentActivity?.SetTag("error", extractionResult?.Message);
                                        // Optionally create metadata even on extraction failure, marking it as failed?
                                        // await CreateAndSaveMetadataAsync(sourceIdentifier, reference, content, request, cancellationToken, extractionResult.Status).ConfigureAwait(false); 
                                    }
                                }
                                finally
                                {
                                    // Ensure the stream is disposed if it's disposable
                                    if (content?.ContentStream is IDisposable disposableStream)
                                    {
                                        disposableStream.Dispose();
                                    }
                                    else
                                    {
                                        // If the stream is not IDisposable, consider if manual closing is needed
                                        // based on the IArtifactProvider implementation guarantees.
                                        // Often, streams returned might be managed elsewhere (e.g., HttpClient response streams).
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Failed to fetch artifact content for reference: {ReferenceId} (Provider not found or failed to get content)", reference.ReferenceId);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Metadata already exists for artifact: {SourceIdentifier}. Skipping fetch/extraction.", sourceIdentifier);
                        // Optionally, load extracted content from a cache or link if needed later
                        // For now, we just skip extraction if metadata exists.
                        // If we need the content later, InteractionContext might need enhancement
                        // or another service call. This aligns with not re-processing known artifacts.
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing artifact reference: {ReferenceId}", reference.ReferenceId);
                    // Add exception info to activity if it exists
                    fetchLoopActivity?.SetStatus(ActivityStatusCode.Error, "Exception processing artifact reference").AddException(ex);
                }
            }
        }

        return results.AsReadOnly();
    }

    private async Task CreateAndSaveMetadataAsync(
        string sourceIdentifier,
        ArtifactReference reference,
        ArtifactContent content,
        AdapterRequest request,
        CancellationToken cancellationToken,
        ExtractionStatus extractionStatus = ExtractionStatus.Success) // Corrected type name
    {
        using var activity = StartActivity("CreateAndSaveMetadata");
        activity?.SetTag("sourceIdentifier", sourceIdentifier);
        activity?.SetTag("extractionStatus", extractionStatus.ToString());

        try
        {
            // TODO: Populate Title, Author, Summary, Tags etc. potentially from extraction results or other sources.
            var newMetadata = new ArtifactMetadata
            {
                // --- Required Fields ---
                SourceIdentifier = sourceIdentifier,
                SourceSystemType = MapPlatformTypeToSourceSystemType(request.PlatformType), // Map from PlatformType
                SourceUri = reference.SourceUri,
                TenantId = reference.TenantId, // Assuming TenantId is on AdapterRequest or Reference
                UserId = request.UserId,       // Assuming UserId is on AdapterRequest

                // --- Optional Fields ---
                FileName = reference.FileName, // Use FileName from reference
                MimeType = content.ContentType, // Use ContentType from content
                SizeBytes = null, // Set to null for now, or determine size if possible
                CreatedAtSource = null, // TODO: Get from source system if available
                ModifiedAtSource = null, // TODO: Get from source system if available
                // PlatformContext = request.PlatformContext, // Assuming PlatformContext exists on AdapterRequest
                Title = content.Metadata?.TryGetValue("DisplayName", out var title) == true ? title : reference.FileName, // Use DisplayName if available, fallback to FileName
                // Author = ...,
                // Summary = ...,
                // Tags = ...,
                ModifiedByUserId = request.UserId // Set modifier initially
            };

            // Save the new metadata
            var savedMetadata = await _metadataRepository.SaveAsync(newMetadata).ConfigureAwait(false); // Use SaveAsync
            _logger.LogInformation("Created and saved metadata for artifact: {SourceIdentifier} with ID: {MetadataId}", sourceIdentifier, savedMetadata.Id);
            activity?.SetTag("metadataId", savedMetadata.Id);
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Failed to create or save metadata for artifact: {SourceIdentifier}", sourceIdentifier);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            // Re-throw or handle as appropriate for the application's error strategy
            throw;
        }
    }

    // Helper method to map PlatformType to SourceSystemType
    private static SourceSystemType MapPlatformTypeToSourceSystemType(string platformTypeString) // Add static for CA1822
    {
        // TODO: Implement proper mapping based on actual PlatformType values
        return platformTypeString switch
        {
            nameof(PlatformType.Teams) => SourceSystemType.MicrosoftTeams,
            nameof(PlatformType.Console) => SourceSystemType.FileSystem, // Example assumption
            nameof(PlatformType.Slack) => SourceSystemType.Slack,
            nameof(PlatformType.Email) => SourceSystemType.Email,
            _ => SourceSystemType.Unknown
        };
    }

    private System.Diagnostics.Activity? StartActivity(string name) => // Remove static for CS0120
        _activitySource.StartActivity(name);
}