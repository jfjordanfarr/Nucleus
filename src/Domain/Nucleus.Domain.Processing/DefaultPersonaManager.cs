using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Domain.Processing.Infrastructure;

namespace Nucleus.Domain.Processing;

/// <summary>
/// The default implementation of IPersonaManager.
/// This manager does not manage any active sessions and will not initiate new ones.
/// It serves as a baseline for concrete persona manager implementations.
/// </summary>
public class DefaultPersonaManager : IPersonaManager
{
    // Constants
    public const string ManagedPersonaTypeIdConstant = "Default_v1";

    private readonly ILogger<DefaultPersonaManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly GoogleAiChatClientAdapter _googleAiAdapter;
    private readonly IArtifactProvider _artifactProvider;

    // TODO: Replace this placeholder with a meaningful type ID for a concrete persona.
    public string ManagedPersonaTypeId => ManagedPersonaTypeIdConstant;

    public DefaultPersonaManager(
        ILogger<DefaultPersonaManager> logger,
        IServiceProvider serviceProvider,
        GoogleAiChatClientAdapter googleAiAdapter,
        IArtifactProvider artifactProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _googleAiAdapter = googleAiAdapter ?? throw new ArgumentNullException(nameof(googleAiAdapter));
        _artifactProvider = artifactProvider ?? throw new ArgumentNullException(nameof(artifactProvider));
    }

    /// <inheritdoc />
    public Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("DefaultPersonaManager ({ManagedPersonaTypeId}) checking salience for request {MessageId}. Returning NotSalient.", ManagedPersonaTypeId, context.OriginalRequest.MessageId ?? "N/A");
        // TODO: Implement actual logic to check salience against active sessions managed by this instance.
        // This would involve checking internal session state (e.g., in-memory dictionary) and potentially LLM checks.
        return Task.FromResult(SalienceCheckResult.NotSalient());
    }

    /// <inheritdoc />
    public Task<NewSessionEvaluationResult> EvaluateForNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var newSessionId = Guid.NewGuid().ToString();
        _logger.LogInformation("DefaultPersonaManager ({ManagedPersonaTypeId}) deciding to initiate new session {NewSessionId} for request {MessageId}.", ManagedPersonaTypeId, newSessionId, context.OriginalRequest.MessageId ?? "N/A");
        // TODO: Implement logic to determine if a new session *should* be initiated based on context.
        // For now, assume we always want to initiate if no active session exists for this context.
        // Use the correct factory method as defined in Nucleus.Abstractions.Orchestration.NewSessionEvaluationResult
        // See: [NewSessionEvaluationResult.cs](cci:7://file:///d:/Projects/Nucleus/src/Abstractions/Nucleus.Abstractions/Orchestration/NewSessionEvaluationResult.cs:0:0-0:0)
        return Task.FromResult(NewSessionEvaluationResult.InitiateNewSession(newSessionId));
    }

    /// <inheritdoc />
    public Task ProcessSalientInteractionAsync(InteractionContext context, string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("DefaultPersonaManager ({ManagedPersonaTypeId}) received salient interaction for session {SessionId}, but does not manage sessions. Interaction ignored.", ManagedPersonaTypeId, sessionId);
        // Default implementation does nothing as it doesn't manage active sessions.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task InitiateNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Initiating NEW session for request ID: {RequestId}", context.OriginalRequest.MessageId);

        // --- 1. Extract Initial Prompt/Query --- 
        // TODO: Refine how the initial prompt is determined (e.g., from specific properties or parsing content)
        var initialPrompt = context.OriginalRequest.QueryText;
        _logger.LogDebug("Extracted Initial Prompt: '{Prompt}'", initialPrompt);

        // --- 2. Retrieve Relevant Artifact Content (if any) ---
        var artifactReferences = context.OriginalRequest.ArtifactReferences ?? new List<ArtifactReference>();

        _logger.LogDebug("Artifact References Count: {Count}", artifactReferences.Count);

        // --- 2. Prepare LLM Request (RAG) --- 
        var promptBuilder = new StringBuilder();
        string artifactContent = string.Empty;

        // Fetch Artifact Content (if references exist)
        if (artifactReferences.Any()) 
        {
            // TODO: Handle multiple artifacts? For now, take the first.
            var firstRef = artifactReferences.First();
            _logger.LogInformation("Attempting to retrieve artifact: Platform='{PlatformType}', Id='{ArtifactId}'", firstRef.PlatformType, firstRef.ArtifactId);

            // Use the injected artifact provider
            // Note: This assumes DI provides the *correct* IArtifactProvider for this context.
            if (_artifactProvider.SupportedPlatformType == firstRef.PlatformType)
            {
                var (stream, fileName, _, error) = await _artifactProvider.GetArtifactStreamAsync(firstRef, cancellationToken);
                if (stream != null && string.IsNullOrEmpty(error))
                {
                    using var reader = new StreamReader(stream);
                    artifactContent = await reader.ReadToEndAsync(cancellationToken);
                    _logger.LogInformation("Successfully retrieved content for artifact '{FileName}' ({Length} chars).", fileName ?? firstRef.ArtifactId, artifactContent.Length);
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve artifact {ArtifactId}: {Error}", firstRef.ArtifactId, error ?? "Stream was null.");
                }
            }
            else
            {
                _logger.LogWarning("Injected IArtifactProvider '{ProviderType}' does not support PlatformType '{ReferenceType}' for artifact {ArtifactId}.",
                    _artifactProvider.GetType().Name, firstRef.PlatformType, firstRef.ArtifactId);
            }
        }
        
        // Construct Prompt
        promptBuilder.AppendLine("You are a helpful assistant."); // Base persona instruction
        if (!string.IsNullOrWhiteSpace(artifactContent))
        {
            promptBuilder.AppendLine("Use the following document content to answer the user's question:");
            promptBuilder.AppendLine("--- Document Start ---");
            promptBuilder.AppendLine(artifactContent);
            promptBuilder.AppendLine("--- Document End ---");
        }
        promptBuilder.AppendLine("\nUser Question:");
        promptBuilder.AppendLine(initialPrompt); // Use the extracted prompt/query here
        
        var combinedPrompt = promptBuilder.ToString();
        _logger.LogDebug("Constructed LLM Prompt:\n{Prompt}", combinedPrompt);

        // --- 3. Call LLM via GoogleAiChatClientAdapter --- 
        try
        {
            _logger.LogInformation("Sending request to Google AI Adapter...");
            // Use the injected GoogleAiChatClientAdapter
            string responseText = await _googleAiAdapter.GetCompletionAsync(combinedPrompt, cancellationToken);
            _logger.LogInformation("Received response from Google AI Adapter ({Length} chars).", responseText.Length);
            
            // --- 4. Process Response (TODO) ---
            // Handle the response, potentially update context, notify user, etc.
            // For now, we just log it.
            // How do we send this back? InteractionContext doesn't have a reply mechanism.
            // This likely needs integration with IPlatformNotifier or similar.
            _logger.LogInformation("LLM Response: {Response}", responseText);
            
            // Since the method now returns Task, we don't return the string directly.
            // The response handling logic needs to be implemented elsewhere (e.g., caller or via notifier).
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM interaction in InitiateNewSessionAsync.");
            // Handle the error appropriately - maybe update context with error status?
            // Rethrow or swallow depending on desired behavior
            throw;
        }

        _logger.LogInformation("Finished Initiating session for request ID: {RequestId}", context.OriginalRequest.MessageId);
    }
}
