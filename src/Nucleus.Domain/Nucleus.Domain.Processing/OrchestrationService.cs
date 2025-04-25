// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider and GetKeyedService
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models; // Used for AdapterRequest, NucleusIngestionRequest, InteractionContext etc.
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories;

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
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_ROUTING.md
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_SESSION_INITIATION.md
/// </summary>
public class OrchestrationService : IOrchestrationService
{
    private readonly ILogger<OrchestrationService> _logger;
    private readonly IPersonaResolver _personaResolver;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActivationChecker _activationChecker; // Added
    private readonly IBackgroundTaskQueue _backgroundTaskQueue; // Added
    private readonly IArtifactMetadataRepository _artifactMetadataRepository; // Added
    private readonly IEnumerable<IArtifactProvider> _artifactProviders; // Added

    public OrchestrationService(
        ILogger<OrchestrationService> logger,
        IPersonaResolver personaResolver,
        IServiceProvider serviceProvider,
        IActivationChecker activationChecker,
        IBackgroundTaskQueue backgroundTaskQueue,
        IArtifactMetadataRepository artifactMetadataRepository,
        IEnumerable<IArtifactProvider> artifactProviders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _personaResolver = personaResolver ?? throw new ArgumentNullException(nameof(personaResolver));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _activationChecker = activationChecker ?? throw new ArgumentNullException(nameof(activationChecker));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _artifactMetadataRepository = artifactMetadataRepository ?? throw new ArgumentNullException(nameof(artifactMetadataRepository));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> ProcessInteractionAsync(AdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("[Orchestration] Processing interaction. Platform: {PlatformType}, ConversationId: {ConversationId}, MessageId: {MessageId}",
            request.PlatformType, request.ConversationId, request.MessageId);

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
                    parsedPlatformType,
                    resolvedPersonaId,
                    request.ConversationId,
                    request.UserId,
                    request.MessageId, // Pass directly, method accepts nullable
                    request.ReplyToMessageId,
                    request.QueryText,
                    request.ArtifactReferences?.ToList(), // Convert to List
                    request.Metadata, // Corrected type: nullable Dictionary
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
                    ArtifactReferences: request.ArtifactReferences?.ToList(), // Pass artifact references
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
                platformTypeEnum, // Use the parsed enum
                request.ResolvedPersonaId ?? "", // If null, pass empty string (core expects non-null)
                request.OriginatingConversationId,
                request.OriginatingUserId,
                request.OriginatingMessageId,
                request.OriginatingReplyToMessageId, // Use the field added to NucleusIngestionRequest
                request.QueryText ?? "",            // Use the renamed field, default to empty if null
                request.ArtifactReferences,          // Use the corrected field
                request.Metadata,                    // Use the renamed field
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
    /// Resolves the appropriate PersonaManager and calls its InitiateNewSessionAsync method.
    /// </summary>
    private async Task<AdapterResponse> ProcessInteractionCoreAsync(
        PlatformType platformType,
        string? resolvedPersonaId, // Changed to nullable string
        string conversationId,
        string userId,
        string? messageId, // Changed to nullable string
        string? replyToMessageId,
        string queryText,
        List<ArtifactReference>? artifactReferences, // Corrected type: nullable List
        Dictionary<string, string>? metadata, // Corrected type: nullable Dictionary
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("[Core Processing] Initiating for Persona '{ResolvedPersonaId}', ConversationId {ConversationId}",
                resolvedPersonaId, conversationId);

            // Step 1: Get the corresponding Persona Manager using the resolved ID as a key
            IPersonaManager personaManager;
            try
            {                
                personaManager = _serviceProvider.GetRequiredKeyedService<IPersonaManager>(resolvedPersonaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get required keyed service IPersonaManager for key '{ResolvedPersonaId}'. Ensure it's registered.", resolvedPersonaId);
                return new AdapterResponse(Success: false, ErrorMessage: $"Internal configuration error for persona: {resolvedPersonaId}", ResponseMessage: "");
            }

            _logger.LogInformation("[Core Processing] Retrieved PersonaManager {PersonaManagerType} (managing {ManagedPersonaTypeId}) for ConversationId {ConversationId}",
                personaManager.GetType().Name, personaManager.ManagedPersonaTypeId, conversationId);

            // Step 2: Construct the InteractionContext required by the manager
            // Create a *minimal* AdapterRequest just for the context - avoids complex reconstruction.
            var contextRequest = new AdapterRequest(
                PlatformType: platformType.ToString(),
                ConversationId: conversationId,
                UserId: userId,
                QueryText: queryText,
                MessageId: messageId,
                ReplyToMessageId: replyToMessageId,
                ArtifactReferences: artifactReferences,
                Metadata: metadata
            );
            var interactionContext = new InteractionContext(contextRequest, platformType, resolvedPersonaId);

            // Step 3: Initiate the session processing via the resolved manager
            _logger.LogDebug("[Core Processing] Calling InitiateNewSessionAsync for PersonaManager {PersonaManagerType}, ConversationId {ConversationId}",
                personaManager.GetType().Name, conversationId);
            
            AdapterResponse response = await personaManager.InitiateNewSessionAsync(interactionContext, cancellationToken);

            _logger.LogInformation("[Core Processing] Finished InitiateNewSessionAsync for PersonaManager {PersonaManagerType}, ConversationId {ConversationId}. Success: {Success}",
                personaManager.GetType().Name, conversationId, response.Success);
            
            // TODO: Consider if notification logic needs to live here or solely within the PersonaManager/Adapter layer.
            // Currently, InitiateNewSessionAsync returns the final response.

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
}
