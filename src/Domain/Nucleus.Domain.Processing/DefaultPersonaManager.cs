using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Default implementation of <see cref="IPersonaManager"/> that acts as a basic router
/// using keyed service resolution based on PersonaTypeId. It does not maintain any state itself.
/// </summary>
public class DefaultPersonaManager : IPersonaManager
{
    private readonly ILogger<DefaultPersonaManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPersonaManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider for resolving personas.</param>
    public DefaultPersonaManager(ILogger<DefaultPersonaManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        // Note: ManagedPersonaTypeId is intended to be set by a derived class or configuration if needed.
        // For this default manager, it might not be strictly necessary if it handles *any* type via resolution.
        // Setting a default value might be misleading.
        ManagedPersonaTypeId = "DefaultPersonaManager"; // Placeholder, consider removing or making abstract.
    }

    /// <inheritdoc />
    public string ManagedPersonaTypeId { get; protected set; }

    /// <inheritdoc />
    public virtual Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        // DefaultPersonaManager doesn't manage state, so interactions are never salient to it directly.
        _logger.LogInformation("DefaultPersonaManager ({ManagedPersonaTypeId}) CheckSalienceAsync called. Defaulting to NotSalient.", ManagedPersonaTypeId);
        // Explicitly specify type argument for Task.FromResult and CALL NotSalient() method
        return Task.FromResult<SalienceCheckResult>(SalienceCheckResult.NotSalient());
    }

    /// <inheritdoc />
    public virtual Task<NewSessionEvaluationResult> EvaluateForNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Default implementation always suggests initiating a new session if asked.
        // More sophisticated managers might check context, user preferences, etc.
        _logger.LogInformation("DefaultPersonaManager ({ManagedPersonaTypeId}) EvaluateForNewSessionAsync called. Defaulting to initiate new session.", ManagedPersonaTypeId);
        var newSessionId = Guid.NewGuid().ToString();
        return Task.FromResult(NewSessionEvaluationResult.InitiateNewSession(newSessionId));
    }


    /// <inheritdoc />
    // InteractionContext should now be found
    public Task ProcessSalientInteractionAsync(InteractionContext context, string sessionId, CancellationToken cancellationToken = default)
    {
        // Default manager ignores salient interactions as it doesn't track sessions.
        _logger.LogWarning("DefaultPersonaManager ({ManagedPersonaTypeId}) received salient interaction for session {SessionId}, but does not manage session state. Interaction ignored.", ManagedPersonaTypeId, sessionId);
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    // Corrected return type to Task to match IPersonaManager
    public async Task InitiateNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Use ResolvedPersonaId instead of PersonaTypeId
        _logger.LogInformation("DefaultPersonaManager ({ManagedPersonaTypeId}) initiating new session for persona ID {PersonaId}", ManagedPersonaTypeId, context.ResolvedPersonaId ?? "<null>");

        // Null check for ResolvedPersonaId
        if (string.IsNullOrEmpty(context.ResolvedPersonaId))
        {
            _logger.LogError("Cannot initiate session: ResolvedPersonaId is null or empty in the InteractionContext.");
            return; // Exit if no persona ID is resolved
        }

        // Resolve the persona using the service provider and the key from the context
        // Provide generic type argument object for IPersona<object>
        var persona = _serviceProvider.GetKeyedService<IPersona<object>>(context.ResolvedPersonaId);
        if (persona == null)
        {
            // Use ResolvedPersonaId in error message
            var errorMessage = $"Could not resolve persona for ID '{context.ResolvedPersonaId}'. Ensure it is registered with AddKeyedSingleton.";
            _logger.LogError(errorMessage);
            // Removed return statement as method now returns Task
            return; // Exit if persona cannot be resolved
        }

        _logger.LogInformation("Resolved persona implementation: {PersonaImplementationType}", persona.GetType().FullName);

        try
        {
            _logger.LogInformation("Attempting to handle query using persona {PersonaId} (Implementation: {PersonaImplementationType})", context.ResolvedPersonaId, persona.GetType().FullName);

            // Construct UserQuery from AdapterRequest
            var userQuery = new UserQuery(
                context.OriginalRequest.QueryText,
                context.OriginalRequest.UserId,
                // TODO: Populate Context dictionary if needed later
                new Dictionary<string, object>() 
            );

            // Call HandleQueryAsync with UserQuery and null for contextContent
            var personaResponse = await persona.HandleQueryAsync(userQuery, null, cancellationToken);

            // Log ResponseText, not non-existent AnalysisResult.Status
            _logger.LogInformation("Persona {PersonaId} handled query. Response Text: {ResponseText}", context.ResolvedPersonaId, personaResponse.ResponseText ?? "<null>");

            // Removed return statement as method now returns Task
        }
        catch (Exception ex)
        {
            // Use ResolvedPersonaId in error message
            _logger.LogError(ex, "Error occurred while persona {PersonaId} (Implementation: {PersonaImplementationType}) handled query.", context.ResolvedPersonaId, persona.GetType().FullName);
            // Removed return statement as method now returns Task
        }
        // Method implicitly completes the Task here
    }
}