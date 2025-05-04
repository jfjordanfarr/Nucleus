// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Abstractions.Models.Configuration; // Corrected namespace for IPersonaConfigurationProvider
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

namespace Nucleus.Domain.Processing;

/// <summary>
/// Core service responsible for orchestrating the processing of incoming interactions.
/// See: [ARCHITECTURE_PROCESSING_ORCHESTRATION.md](../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md)
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly ILogger<OrchestrationService> _logger;
    private readonly IPersonaRuntime _personaRuntime; // Inject IPersonaRuntime directly
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;
    private readonly IEnumerable<IArtifactProvider> _artifactProviders;
    private readonly IEnumerable<IContentExtractor> _contentExtractors;
    private readonly IArtifactMetadataRepository _metadataRepository;
    private readonly IPersonaConfigurationProvider _personaConfigProvider;
    private readonly IPersonaKnowledgeRepository _personaKnowledgeRepository; // Inject repository for knowledge
    private readonly IPersonaResolver _personaResolver; // Added

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaRuntime personaRuntime, // Inject IPersonaRuntime directly
        IServiceProvider serviceProvider,
        ActivitySource activitySource,
        IEnumerable<IArtifactProvider> artifactProviders,
        IEnumerable<IContentExtractor> contentExtractors,
        IArtifactMetadataRepository metadataRepository,
        IPersonaConfigurationProvider personaConfigProvider,
        IPersonaKnowledgeRepository personaKnowledgeRepository, // Inject repository
        IPersonaResolver personaResolver // Added
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaRuntime = personaRuntime ?? throw new ArgumentNullException(nameof(personaRuntime)); // Assign injected runtime
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));
        _contentExtractors = contentExtractors ?? throw new ArgumentNullException(nameof(contentExtractors));
        _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
        _personaConfigProvider = personaConfigProvider ?? throw new ArgumentNullException(nameof(personaConfigProvider));
        _personaKnowledgeRepository = personaKnowledgeRepository ?? throw new ArgumentNullException(nameof(personaKnowledgeRepository)); // Assign injected repository
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver)); // Added
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); // CA1062 Fix: Validate request parameter

        // Use conversation ID or message ID as a fallback interaction ID
        var interactionId = request.MessageId ?? request.ConversationId ?? Guid.NewGuid().ToString();

        // Reintroduce the activity variable declaration
        using var activity = _activitySource.StartActivity("ProcessInteraction", ActivityKind.Internal, default(ActivityContext), tags: new Dictionary<string, object?>
        {
            { "interactionId", interactionId },
            { "PlatformType", request.PlatformType }, // Keep original platform string for initial tag
            { "UserId", request.UserId },
            { "ConversationId", request.ConversationId },
            { "MessageId", request.MessageId }
        });

        // CS1503/CS7036 FIX: Parse PlatformType enum early
        if (!Enum.TryParse<PlatformType>(request.PlatformType, ignoreCase: true, out var platformTypeEnum))
        {
            _logger.LogError("Invalid PlatformType '{PlatformType}' received in request {InteractionId}.", request.PlatformType, interactionId);
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid PlatformType");
            // CS0117 FIX: Use constructor, not Failure static method
            return new OrchestrationResult(Status: OrchestrationStatus.Failed, ErrorMessage: $"Invalid PlatformType: {request.PlatformType}");
        }
        activity?.SetTag("platformType", platformTypeEnum.ToString()); // Update tag with enum value

        try
        {
            // 1. Resolve Persona ID
            // CS1503 FIX: Provide platformTypeEnum
            string? personaId = await _personaResolver.ResolvePersonaIdAsync(platformTypeEnum, request, cancellationToken).ConfigureAwait(false);
            activity?.SetTag("resolvedPersonaId", personaId ?? "null");

            // Fetch Persona Configuration if ID was resolved
            PersonaConfiguration? personaConfig = null;
            if (!string.IsNullOrEmpty(personaId))
            {
                using var configActivity = StartActivity("FetchPersonaConfiguration");
                configActivity?.SetTag("personaId", personaId);
                // CS1061 FIX: Use GetConfigurationAsync instead of GetPersonaConfigurationAsync
                personaConfig = await _personaConfigProvider.GetConfigurationAsync(personaId, cancellationToken).ConfigureAwait(false);
                configActivity?.SetTag("foundConfiguration", personaConfig != null);
            }
            else
            {
                _logger.LogWarning("Persona ID resolution failed for request {InteractionId}.", interactionId);
                // Return specific status if resolution fails
                // CS0117 FIX: Use constructor, not Failure static method
                return new OrchestrationResult(
                    Status: OrchestrationStatus.PersonaResolutionFailed,
                    ErrorMessage: "Failed to resolve a Persona ID from the request.",
                    // CS1061 FIX: Use ConversationId instead of SessionId
                    ResolvedPersonaId: null); // Explicitly null as resolution failed
            }

            // Handle case where configuration is not found
            if (personaConfig == null)
            {
                _logger.LogError("Configuration not found for Persona ID: {PersonaId}", personaId);
                // CS0117 FIX: Use constructor, not Failure static method
                // CS0117 FIX: Use PersonaResolutionFailed status
                return new OrchestrationResult(
                    Status: OrchestrationStatus.PersonaResolutionFailed,
                    ResolvedPersonaId: personaId, // We resolved the ID, but couldn't find config
                    ErrorMessage: $"Persona configuration not found for ID '{personaId}'.",
                    // CS1061 FIX: Use ConversationId instead of SessionId
                    AdapterResponse: null); // No direct response needed here
            }

            // Fetch and extract artifacts
            using var artifactActivity = StartActivity("FetchAndExtractArtifacts");
            // CS1501 FIX: Pass only request and cancellationToken
            var extractedArtifacts = await FetchAndExtractArtifactsAsync(request, cancellationToken).ConfigureAwait(false);
            artifactActivity?.SetTag("artifactCount", extractedArtifacts.Count);

            // Prepare context for persona execution
            // CS7036 FIX: Provide platformTypeEnum and personaId to constructor
            var interactionContext = new InteractionContext(request, platformTypeEnum, personaId, extractedArtifacts);

            // Declare variables outside the try block and initialize
            AdapterResponse? personaResult = null; // Initialize to null
            PersonaExecutionStatus personaStatus = default; // Initialize to default

            try
            {
                // Execute the persona logic
                using var executionActivity = StartActivity("ExecutePersonaRuntime");
                executionActivity?.SetTag("personaId", personaConfig.PersonaId);

                // CS0136/CS0165 FIX: Remove explicit type declaration to assign to outer variables
                (personaResult, personaStatus) = await _personaRuntime.ExecuteAsync(
                    personaConfig, 
                    interactionContext, 
                    cancellationToken).ConfigureAwait(false);

                executionActivity?.SetTag("executionStatus", personaStatus.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Persona runtime execution failed for Persona ID: {PersonaId}", personaConfig.PersonaId);
                activity?.SetStatus(ActivityStatusCode.Error, "Persona runtime execution failed").AddException(ex);
                // Use correct OrchestrationResult constructor
                // Use personaConfig.PersonaId here as personaId might be null if config fetch failed earlier
                return new OrchestrationResult(Status: OrchestrationStatus.RuntimeExecutionFailed, ResolvedPersonaId: personaConfig.PersonaId, ErrorMessage: $"Persona runtime execution failed: {ex.Message}", Exception: ex);
            }

            // Map PersonaExecutionStatus to OrchestrationStatus
            var finalStatus = MapPersonaStatus(personaStatus);

            _logger.LogInformation("Persona execution completed for Persona ID: {PersonaId} with status: {Status}", personaConfig.PersonaId, finalStatus);

            // Construct and return the final result
            // CS1739 FIX: Remove InteractionId parameter (not part of constructor)
            // CS1061 FIX: Use ConversationId instead of SessionId (implicit via AdapterResponse?)
            // Note: The constructor takes ResolvedPersonaId, AdapterResponse, ErrorMessage, Exception.
            // We need to ensure personaResult (which is AdapterResponse) is passed correctly.
            return new OrchestrationResult(
                Status: finalStatus,
                ResolvedPersonaId: personaConfig.PersonaId, // Pass the resolved ID
                AdapterResponse: personaResult, // Pass the result from ExecuteAsync
                ErrorMessage: finalStatus != OrchestrationStatus.Success ? personaResult?.ErrorMessage : null
                // SessionId is implicitly part of the AdapterResponse if needed by the adapter
            );
        }
        catch (Exception ex) // Catch any unhandled exceptions during orchestration
        {
            _logger.LogError(ex, "Unhandled error during orchestration for interaction {InteractionId}", interactionId);
            activity?.SetStatus(ActivityStatusCode.Error, "Unhandled orchestration error").AddException(ex);
            // Return a general error result using correct constructor
             // Wrap error message in an AdapterResponse for consistency if needed, although ErrorMessage param exists
            return new OrchestrationResult(Status: OrchestrationStatus.UnhandledError, ErrorMessage: $"An unexpected error occurred: {ex.Message}", Exception: ex);
        }
    }

    // Helper method to map PersonaExecutionStatus to OrchestrationStatus
    private static OrchestrationStatus MapPersonaStatus(PersonaExecutionStatus personaStatus)
    {
        return personaStatus switch
        {
            PersonaExecutionStatus.Success => OrchestrationStatus.Success, // Assuming direct success maps
            PersonaExecutionStatus.Failed => OrchestrationStatus.RuntimeExecutionFailed, // Map specific failure
            PersonaExecutionStatus.Filtered => OrchestrationStatus.Ignored, // Map Filtered to Ignored
            PersonaExecutionStatus.NoActionTaken => OrchestrationStatus.Ignored, // Map NoActionTaken to Ignored
            _ => OrchestrationStatus.UnhandledError // Default to UnhandledError for unknown status
        };
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
        using var activity = _activitySource.StartActivity("CreateAndSaveMetadata");
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
                TenantId = reference.TenantId, // Ensure TenantId is propagated
                UserId = request.UserId,       // Assuming UserId is on AdapterRequest

                // --- Optional Fields ---
                FileName = reference.FileName, // Use FileName from reference
                MimeType = content.ContentType, // Use ContentType from content
                SizeBytes = null, // Set to null for now, or determine size if possible
                CreatedAtSource = null, // TODO: Get from source system if available
                ModifiedAtSource = null, // TODO: Get from source system if available
                // PlatformContext = request.PlatformContext, // Assuming PlatformContext exists on AdapterRequest
                Title = content.Metadata?.TryGetValue("DisplayName", out var title) == true ? title : reference.FileName, // Use DisplayName if available, fallback to FileName
                // Author = ..., // TODO: Populate Author if available
                // Summary = ..., // TODO: Populate Summary if available
                // Tags = ..., // TODO: Populate Tags if available
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