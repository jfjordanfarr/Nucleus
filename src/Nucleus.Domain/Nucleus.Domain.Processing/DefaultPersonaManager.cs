using Microsoft.Extensions.DependencyInjection; // Keep for potential future use, though removing GetKeyedService here
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models; // ADDED for AdapterResponse, PersonaConfiguration, InteractionContext, SalienceCheckResult etc.
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions.Repositories; // Added for IArtifactProvider and IArtifactMetadataRepository
using Nucleus.Abstractions; // Corrected namespace for IPersona<>
using Nucleus.Domain.Personas.Core; // Ensure this using exists for EmptyAnalysisData
// using Nucleus.Personas.Core; // May be needed later for implementation types, but not IPersona interface
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions.Extraction; // Added for ContentExtractionResult

namespace Nucleus.Domain.Processing;

/// <summary>
/// Default implementation of <see cref="IPersonaManager"/> that acts as a basic router
/// using keyed service resolution based on PersonaTypeId. It does not maintain any state itself.
/// <seealso cref="../../../../Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_INTERACTION_LIFECYCLE.md"/>
/// </summary>
public class DefaultPersonaManager : IPersonaManager
{
    private readonly ILogger<DefaultPersonaManager> _logger;
    private readonly IServiceProvider _serviceProvider; // To resolve the actual IPersona
    private readonly IPersona _managedPersona; 
    private readonly IEnumerable<IArtifactProvider> _artifactProviders;
    private readonly IArtifactMetadataRepository _artifactMetadataRepository; // Added
    private readonly IOptions<List<PersonaConfiguration>> _personaConfigurations; // Added

    // TODO: Refactor key to use NucleusConstants
    public const string DefaultPersonaTypeId = "Default_v1";

    // Implementation for IPersonaManager
    public string ManagedPersonaTypeId => DefaultPersonaTypeId;

    // Implementation for IPersonaManager
    public PersonaConfiguration Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPersonaManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for resolving keyed services.</param>
    /// <param name="personaConfigurations">All registered persona configurations.</param> // Added
    /// <param name="artifactMetadataRepository">Repository for artifact metadata.</param> // Added
    /// <param name="managedPersona">Managed persona instance.</param>
    /// <param name="artifactProviders">Artifact providers.</param>
    public DefaultPersonaManager(
        ILogger<DefaultPersonaManager> logger,
        IServiceProvider serviceProvider,
        IOptions<List<PersonaConfiguration>> personaConfigurations, // Added
        IArtifactMetadataRepository artifactMetadataRepository, // Added
        IPersona managedPersona,
        IEnumerable<IArtifactProvider> artifactProviders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _personaConfigurations = personaConfigurations ?? throw new ArgumentNullException(nameof(personaConfigurations)); // Added
        _artifactMetadataRepository = artifactMetadataRepository ?? throw new ArgumentNullException(nameof(artifactMetadataRepository)); // Added
        _managedPersona = managedPersona ?? throw new ArgumentNullException(nameof(managedPersona));
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));

        // Find the specific configuration for this manager instance
        Configuration = _personaConfigurations.Value.FirstOrDefault(p => p.PersonaId == ManagedPersonaTypeId)
                        ?? throw new InvalidOperationException($"Configuration for persona '{ManagedPersonaTypeId}' not found.");

        _logger.LogDebug("DefaultPersonaManager initialized for PersonaTypeId: {PersonaId}", ManagedPersonaTypeId);
    }

    /// <inheritdoc />
    public Task<SalienceCheckResult> CheckSalienceAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("CheckSalienceAsync called for Persona '{PersonaId}'. Returning NotSalient by default.", ManagedPersonaTypeId);
        // Basic implementation: Default manager doesn't handle ongoing sessions or salience.
        // A real implementation might query session state based on context.OriginalRequest.ConversationId
        return Task.FromResult(SalienceCheckResult.NotSalient()); // Call factory method
    }

    /// <inheritdoc />
    public virtual Task<NewSessionEvaluationResult> EvaluateForNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public virtual Task<AdapterResponse> ProcessSalientInteractionAsync(InteractionContext context, string sessionId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<AdapterResponse> InitiateNewSessionAsync(InteractionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Initiating new session for persona {PersonaId} based on interaction from user {UserId}.",
            ManagedPersonaTypeId, context.OriginalRequest.UserId);

        try
        {
            // Content extraction is now handled upstream by OrchestrationService.
            // The extracted content is available in context.ExtractedContents.

            // Resolve the actual IPersona instance using the injected one directly.
            // This manager acts as a pass-through or simple state manager for *a single* persona type.
            var persona = _managedPersona; // Use the injected instance

            // Construct UserQuery from InteractionContext
            var userQuery = new UserQuery(
                QueryText: context.OriginalRequest.QueryText ?? string.Empty,
                UserId: context.OriginalRequest.UserId ?? "UnknownUser",
                Context: context.OriginalRequest.Context ?? new Dictionary<string, object>()
            );

            // Call HandleQueryAsync with UserQuery and the ExtractedContents from the context
            var personaResponse = await persona.HandleQueryAsync(userQuery, context.ExtractedContents, cancellationToken);

            // Log ResponseText
            _logger.LogInformation("Persona {PersonaId} handled query. Response Text: {ResponseText}", ManagedPersonaTypeId, personaResponse.ResponseText ?? "<null>");

            // Return the response text generated by the underlying persona
            var response = new AdapterResponse(
                Success: true, 
                ResponseMessage: personaResponse.ResponseText ?? "No response generated.", // Use ':' for named arg
                GeneratedArtifactReference: personaResponse.GeneratedArtifactReference // Use ':' for named arg
            );

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while persona {PersonaId} handled query.", ManagedPersonaTypeId);
            return new AdapterResponse(Success: false, ResponseMessage: "Error occurred during query handling.", ErrorMessage: ex.Message);
        }
    }
}