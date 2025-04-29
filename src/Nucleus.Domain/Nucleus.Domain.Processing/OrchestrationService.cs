// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; 
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models; 
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Repositories;
using Nucleus.Abstractions.Extraction;
using Nucleus.Domain.Personas.Core.Interfaces; // Added for IPersonaRuntime
using Nucleus.Abstractions.Configuration; // Added for IPersonaConfigurationProvider

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Implementation of the core processing orchestration service.
/// This service acts as the central router, coordinating with <see cref="IPersonaResolver"/>
/// and multiple <see cref="IPersonaManager"/> instances to handle incoming requests.
/// It first checks if a message is salient to an existing session via managers, and if not,
/// asks managers to evaluate if they should initiate a new session.
/// See: Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_ORCHESTRATION.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md"/>
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md"/>
/// <seealso cref="../../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_OVERVIEW.md"/>
/// <seealso cref="../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// <seealso cref="../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly ILogger<OrchestrationService> _logger;
    private readonly IPersonaResolver _personaResolver;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActivationChecker _activationChecker; 
    private readonly IBackgroundTaskQueue _backgroundTaskQueue; 
    private readonly IArtifactMetadataRepository _artifactMetadataRepository; 
    private readonly IEnumerable<IArtifactProvider> _artifactProviders; 
    private readonly IEnumerable<IContentExtractor> _contentExtractors;
    private readonly IPersonaRuntime _personaRuntime; // Added IPersonaRuntime
    private readonly IPersonaConfigurationProvider _personaConfigurationProvider; // Added provider

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaResolver personaResolver,
        IServiceProvider serviceProvider, 
        IActivationChecker activationChecker,
        IBackgroundTaskQueue backgroundTaskQueue,
        IArtifactMetadataRepository artifactMetadataRepository,
        IEnumerable<IArtifactProvider> artifactProviders,
        IEnumerable<IContentExtractor> contentExtractors,
        IPersonaRuntime personaRuntime,
        IPersonaConfigurationProvider personaConfigurationProvider) // Added provider
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _activationChecker = activationChecker ?? throw new ArgumentNullException(nameof(activationChecker));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _artifactMetadataRepository = artifactMetadataRepository ?? throw new ArgumentNullException(nameof(artifactMetadataRepository));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));
        _contentExtractors = contentExtractors ?? throw new ArgumentNullException(nameof(contentExtractors));
        _personaRuntime = personaRuntime ?? throw new ArgumentNullException(nameof(personaRuntime)); // Added IPersonaRuntime
        _personaConfigurationProvider = personaConfigurationProvider ?? throw new ArgumentNullException(nameof(personaConfigurationProvider)); // Added provider
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("[Orchestration] Processing interaction. Platform: {PlatformType}, ConvId: {ConversationId}, MsgId: {MessageId}",
            request.PlatformType, request.ConversationId, request.MessageId);

        // --- Effective Artifacts --- (References from request ONLY)
        // Start with artifact references explicitly provided in the request.
        List<ArtifactReference> effectiveArtifactReferences = request.ArtifactReferences?.ToList() ?? new List<ArtifactReference>();

        try
        {
            // *******************************************
            // 1. Activation Check
            // *******************************************
            bool isActivated = await _activationChecker.ShouldActivateAsync(request, cancellationToken);
            _logger.LogInformation("Activation Check Result for ConversationId {ConversationId}: {IsActivated}", request.ConversationId, isActivated);

            if (!isActivated)
            {
                _logger.LogInformation("Interaction not activated. Skipping processing. ConversationId: {ConversationId}", request.ConversationId);
                return new OrchestrationResult(OrchestrationStatus.NotActivated, null); // Use NotActivated status
            }

            // *******************************************
            // 2. Resolve Persona ID (only if activated)
            // *******************************************
            // Use the same platform type parsing logic as in HandleQueuedInteractionAsync
            (PlatformType parsedPlatformType, AdapterResponse? platformErrorResponse) = ParsePlatformType(request.PlatformType);
            if (platformErrorResponse != null)
            {
                _logger.LogError("Invalid PlatformType string '{PlatformTypeString}' in AdapterRequest. ConversationId: {ConversationId}", request.PlatformType, request.ConversationId);
                return new OrchestrationResult(OrchestrationStatus.Error, platformErrorResponse); // Return error status
            }

            string? resolvedPersonaId = await _personaResolver.ResolvePersonaIdAsync(parsedPlatformType, request, cancellationToken);
            _logger.LogInformation("Persona Resolution Result for ConversationId {ConversationId}: {ResolvedPersonaId}", request.ConversationId, resolvedPersonaId ?? "<null>");

            if (string.IsNullOrWhiteSpace(resolvedPersonaId))
            {
                // If persona resolution fails, we might still proceed if activation didn't strictly require it,
                // or return an error/specific status.
                _logger.LogWarning("Failed to resolve Persona ID for ConversationId: {ConversationId}. Activation was positive, but routing might fail.", request.ConversationId);
                // For now, let's return an error. Consider a 'RequiresPersonaResolution' flag in activation rules?
                return new OrchestrationResult(OrchestrationStatus.Error, new AdapterResponse(Success: false, ErrorMessage: "Persona could not be resolved.", ResponseMessage: ""));
            }

            // *******************************************
            // 3. Determine Processing Mode (Sync/Async) - Based on Persona Configuration or Request Hint
            // *******************************************
            // TODO: Add logic to determine sync/async preference. Defaulting to Sync for now.
            bool preferAsync = false;

            // *******************************************
            // 4. Execute or Queue
            // *******************************************
            if (!preferAsync) // Synchronous Processing Path
            {
                _logger.LogInformation("[Orchestration] Processing interaction synchronously. ConversationId: {ConversationId}, ResolvedPersonaId: {ResolvedPersonaId}", request.ConversationId, resolvedPersonaId);
                AdapterResponse response = await ProcessInteractionCoreAsync(
                    resolvedPersonaId,
                    new InteractionContext(
                        new AdapterRequest(
                            PlatformType: request.PlatformType,
                            ConversationId: request.ConversationId,
                            UserId: request.UserId,
                            QueryText: request.QueryText,
                            MessageId: request.MessageId,
                            ReplyToMessageId: request.ReplyToMessageId,
                            ArtifactReferences: effectiveArtifactReferences,
                            Metadata: request.Metadata
                        ),
                        parsedPlatformType,
                        resolvedPersonaId,
                        new List<ContentExtractionResult>() // Pass empty list for now
                    ),
                    cancellationToken
                );
                return new OrchestrationResult(OrchestrationStatus.ProcessedSync, response); // Use ProcessedSync status
            }
            else // Asynchronous Processing Path
            {
                _logger.LogInformation("[Orchestration] Queuing interaction for asynchronous processing. ConversationId: {ConversationId}, ResolvedPersonaId: {ResolvedPersonaId}", request.ConversationId, resolvedPersonaId);

                // Map AdapterRequest to NucleusIngestionRequest for the queue
                var ingestionRequest = new NucleusIngestionRequest(
                    PlatformType: request.PlatformType, // Store original string
                    OriginatingConversationId: request.ConversationId,
                    OriginatingUserId: request.UserId,
                    QueryText: request.QueryText,
                    OriginatingMessageId: request.MessageId,
                    OriginatingReplyToMessageId: request.ReplyToMessageId,
                    ResolvedPersonaId: resolvedPersonaId, // Pass the resolved ID
                    TimestampUtc: DateTimeOffset.UtcNow, // Set timestamp
                    ArtifactReferences: effectiveArtifactReferences, // Pass the combined/processed list
                    Metadata: request.Metadata, // Pass metadata
                    CorrelationId: request.MessageId ?? Guid.NewGuid().ToString() // Use MessageId or generate new Guid
                );

                // Queue the request
                await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(ingestionRequest, cancellationToken); // Corrected method name

                // Return a result indicating the task was queued
                _logger.LogInformation("Interaction successfully enqueued for ConversationId {ConversationId}", request.ConversationId);
                // Use AcceptedAsync status and provide a success response
                return new OrchestrationResult(OrchestrationStatus.AcceptedAsync, new AdapterResponse(Success: true, ResponseMessage: "Request accepted for asynchronous processing."));
            }
        }
        catch (Exception ex)
        {            
            _logger.LogError(ex, "[Orchestration] Unhandled exception during ProcessInteractionAsync for ConversationId: {ConversationId}. Error: {ErrorMessage}", request.ConversationId, ex.Message);
            return new OrchestrationResult(OrchestrationStatus.Error, new AdapterResponse(Success: false, ErrorMessage: "An internal error occurred during orchestration.", ResponseMessage: ""));
        }
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult?> HandleQueuedInteractionAsync(NucleusIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("[HandleQueued] Processing queued interaction. Platform: {PlatformType}, ConversationId: {ConversationId}, OriginatingMessageId: {OriginatingMessageId}",
            request.PlatformType, request.OriginatingConversationId, request.OriginatingMessageId);

        try
        {
            // First, parse the platform type string
            (PlatformType platformTypeEnum, AdapterResponse? platformParseError) = ParsePlatformType(request.PlatformType);
            if (platformParseError is not null)
            {
                _logger.LogWarning("[HandleQueued] Could not parse PlatformType '{PlatformTypeString}'. Returning error.", request.PlatformType);
                return new OrchestrationResult(OrchestrationStatus.Error, platformParseError);
            }

            // Directly call the core processing logic, bypassing activation and sync/async decision.
            // This assumes the NucleusIngestionRequest contains all necessary resolved information.
            // TODO: Verify NucleusIngestionRequest has everything needed by ProcessInteractionCoreAsync.

            AdapterResponse response = await ProcessInteractionCoreAsync(
                request.ResolvedPersonaId,
                new InteractionContext(
                    new AdapterRequest(
                        PlatformType: request.PlatformType,
                        ConversationId: request.OriginatingConversationId,
                        UserId: request.OriginatingUserId,
                        QueryText: request.QueryText,
                        MessageId: request.OriginatingMessageId,
                        ReplyToMessageId: request.OriginatingReplyToMessageId,
                        ArtifactReferences: request.ArtifactReferences,
                        Metadata: request.Metadata
                    ),
                    platformTypeEnum,
                    request.ResolvedPersonaId,
                    new List<ContentExtractionResult>() // Pass empty list for now
                ),
                cancellationToken
            );

            // Determine the status based on the core processing outcome
            var finalStatus = response.Success ? OrchestrationStatus.ProcessedSync : OrchestrationStatus.Error;

            _logger.LogInformation("[HandleQueued] Finished processing queued interaction for ConversationId {ConversationId}. Status: {Status}, Success: {Success}",
                request.OriginatingConversationId, finalStatus, response.Success);
                
            // Wrap the AdapterResponse in an OrchestrationResult
            return new OrchestrationResult(finalStatus, response);
        }
        catch (Exception ex)
        {
            // Catch exceptions specifically from the queued handling logic itself
            _logger.LogError(ex, "[HandleQueued] Error during queued interaction processing for ConversationId {ConversationId}. Error: {ErrorMessage}", 
                request.OriginatingConversationId, ex.Message);
            // Return an Error result
            return new OrchestrationResult(OrchestrationStatus.Error, new AdapterResponse(Success: false, ResponseMessage: "", ErrorMessage: "An internal error occurred during queued processing."));
        }
    }

    /// <summary>
    /// Helper method to parse the platform type string.
    /// Now returns a tuple: (PlatformType ParsedType, AdapterResponse? ErrorResponse)
    /// </summary>
    /// <param name="platformTypeString">The platform type string from the request.</param>
    /// <returns>A tuple containing the parsed PlatformType and an optional error response.</returns>
    /// <remarks>Marked static as it doesn't access instance members.</remarks>
    private static (PlatformType ParsedType, AdapterResponse? ErrorResponse) ParsePlatformType(string platformTypeString)
    {
        if (Enum.TryParse<PlatformType>(platformTypeString, ignoreCase: true, out var parsedType))
        {
            return (parsedType, null);
        }
        else
        {
            return (default, new AdapterResponse(Success: false, ErrorMessage: $"Invalid PlatformType: {platformTypeString}", ResponseMessage: ""));
        }
    }

    /// <summary>
    /// Executes the core logic for processing an interaction after activation and persona resolution.
    /// Resolves the appropriate PersonaRuntime and calls its ExecuteAsync method.
    /// </summary>
    private async Task<AdapterResponse> ProcessInteractionCoreAsync(string resolvedPersonaId, InteractionContext interactionContext, CancellationToken cancellationToken)
    {
        // Ensure interactionContext isn't null after construction
        ArgumentNullException.ThrowIfNull(interactionContext);
        var conversationId = interactionContext.SessionContext?.ConversationId ?? "N/A"; // Use null-conditional access

        _logger.LogInformation("[Core Processing] Starting core processing for Persona '{ResolvedPersonaId}', ConversationId: {ConversationId}", resolvedPersonaId, conversationId);

        try
        {
            // --- Start Refactoring ---
            // Load the configuration using the provider
            _logger.LogInformation("[Core Processing] Attempting to load PersonaConfiguration for Persona ID: {ResolvedPersonaId}", resolvedPersonaId);
            var personaConfig = await _personaConfigurationProvider.GetConfigurationAsync(resolvedPersonaId, cancellationToken);

            if (personaConfig == null)
            {
                _logger.LogError("[Core Processing] PersonaConfiguration not found for Persona ID: {ResolvedPersonaId}", resolvedPersonaId);
                return new AdapterResponse(Success: false, ErrorMessage: $"Internal configuration error: Persona configuration '{resolvedPersonaId}' not found.", ResponseMessage: "");
            }

            _logger.LogInformation("[Core Processing] Successfully loaded PersonaConfiguration '{DisplayName}' for Persona ID: {PersonaId}", personaConfig.DisplayName, personaConfig.PersonaId);

            // Invoke the generic Persona Runtime with the loaded configuration and context
            _logger.LogInformation("[Core Processing] Invoking PersonaRuntime for Persona '{PersonaId}', Strategy '{StrategyKey}', ConversationId {ConversationId}", 
                personaConfig.PersonaId, personaConfig.AgenticStrategy.StrategyKey, conversationId);

            AdapterResponse response = await _personaRuntime.ExecuteAsync(personaConfig, interactionContext, cancellationToken);

            _logger.LogInformation("[Core Processing] Finished PersonaRuntime execution for Persona '{PersonaId}', ConversationId {ConversationId}. Success: {Success}",
                personaConfig.PersonaId, conversationId, response.Success);
            
            return response;
        }
        catch (Exception ex)
        {
            // Log the exception specific to this core processing step
            _logger.LogError(ex, "[Core Processing] Error during interaction processing for Persona '{ResolvedPersonaId}', ConversationId: {ConversationId}. Error: {ErrorMessage}", resolvedPersonaId, conversationId, ex.Message);
            // Return a generic error response
            return new AdapterResponse(Success: false, ErrorMessage: "An internal error occurred during interaction processing.", ResponseMessage: "");
        }
    }

    private static string ConstructSourceIdentifier(SourceSystemType type, string referenceId)
    {
        return $"{type}:{referenceId}";
    }

    /// <summary>
    /// Selects the appropriate artifact provider based on the reference type.
    /// </summary>
    /// <param name="reference">The artifact reference.</param>
    /// <returns>The matching IArtifactProvider.</returns>
    /// <exception cref="NotSupportedException">Thrown if no provider supports the given reference type.</exception>
    private IArtifactProvider GetArtifactProvider(ArtifactReference reference)
    {
        var provider = _artifactProviders.FirstOrDefault(p =>
            p.SupportedReferenceTypes.Contains(reference.ReferenceType, StringComparer.OrdinalIgnoreCase));

        if (provider == null)
        {
            _logger.LogError("No IArtifactProvider found supporting ReferenceType: {ReferenceType}", reference.ReferenceType);
            throw new NotSupportedException($"No artifact provider registered for reference type '{reference.ReferenceType}'.");
        }

        _logger.LogTrace("Selected artifact provider {ProviderType} for ReferenceType: {ReferenceType}", provider.GetType().Name, reference.ReferenceType);
        return provider;
    }

    /// <summary>
    /// Selects the appropriate content extractor based on the MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type of the content.</param>
    /// <returns>The matching IContentExtractor.</returns>
    /// <exception cref="NotSupportedException">Thrown if no extractor supports the given MIME type.</exception>
    private IContentExtractor GetContentExtractor(string mimeType)
    {
        var extractor = _contentExtractors.FirstOrDefault(e => 
            e.SupportedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase));

        if (extractor == null)
        {
            _logger.LogWarning("No IContentExtractor found supporting MimeType: {MimeType}. Falling back to PlainTextContentExtractor if available.", mimeType);
            // Fallback to plain text if specific extractor not found
            extractor = _contentExtractors.FirstOrDefault(e => e.SupportedMimeTypes.Contains("text/plain", StringComparer.OrdinalIgnoreCase));

            if (extractor == null)
            {
                 _logger.LogError("No IContentExtractor found supporting MimeType: {MimeType}, and no fallback PlainTextContentExtractor registered.", mimeType);
                 throw new NotSupportedException($"No content extractor registered for MIME type '{mimeType}', and no fallback plain text extractor available.");
            }
        }

        _logger.LogTrace("Selected content extractor {ExtractorType} for MimeType: {MimeType}", extractor.GetType().Name, mimeType);
        return extractor;
    }
}
