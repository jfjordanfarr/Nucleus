using Microsoft.Extensions.DependencyInjection; // Keep for potential future use, though removing GetKeyedService here
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Orchestration;
using Nucleus.Abstractions; // Corrected namespace for IPersona<>
// using Nucleus.Personas.Core; // May be needed later for implementation types, but not IPersona interface
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing;

/// <summary>
/// Default implementation of <see cref="IPersonaManager"/> that acts as a basic router
/// using keyed service resolution based on PersonaTypeId. It does not maintain any state itself.
/// </summary>
public class DefaultPersonaManager : IPersonaManager
{
    private readonly string _personaId;
    private readonly ILogger<DefaultPersonaManager> _logger;
    private readonly IPersona<object> _managedPersona; 
    private readonly IEnumerable<IArtifactProvider> _artifactProviders;

    /// <summary>
    /// Gets the configuration settings associated with this specific persona instance.
    /// </summary>
    public PersonaConfiguration Configuration { get; private set; }

    /// <inheritdoc />
    public string ManagedPersonaTypeId => _personaId; 

    public DefaultPersonaManager(
        string personaId, 
        ILogger<DefaultPersonaManager> logger,
        IOptionsMonitor<List<PersonaConfiguration>> personaConfigsOptions,
        IPersona<object> managedPersona,
        IEnumerable<IArtifactProvider> artifactProviders
        /* Inject other dependencies here */)
    {
        _personaId = personaId ?? throw new ArgumentNullException(nameof(personaId));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(personaConfigsOptions);
        _managedPersona = managedPersona ?? throw new ArgumentNullException(nameof(managedPersona)); 
        _artifactProviders = artifactProviders ?? throw new ArgumentNullException(nameof(artifactProviders));

        // Find the configuration for this specific persona instance
        var personaConfigs = personaConfigsOptions.CurrentValue ?? new List<PersonaConfiguration>();
#pragma warning disable CS8601 // Null check and default assignment handle this case
        Configuration = personaConfigs.FirstOrDefault(p => 
                            p.PersonaId.Equals(_personaId, StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS8601

        if (Configuration == null)
        {            
            _logger.LogWarning("Configuration for PersonaId '{PersonaId}' not found. Using default configuration.", _personaId);
            // Create a default configuration if not found
            Configuration = new PersonaConfiguration { PersonaId = _personaId };
        }
        else
        {
            _logger.LogInformation("Loaded configuration for PersonaId '{PersonaId}'. Processing Preference: {Preference}", 
                _personaId, Configuration.ProcessingPreference);
        }

        // TODO: Add listener for configuration changes if needed using personaConfigsOptions.OnChange
    }

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
        _logger.LogInformation("Initiating new session processing via DefaultPersonaManager for PersonaId '{PersonaId}' (Conversation: {ConversationId})",
            _personaId, context.OriginalRequest.ConversationId);

        try
        {
            // Use the injected _managedPersona instance directly
            var persona = _managedPersona;
            // Removed incorrect GetService/GetKeyedService calls

            // Extract UserQuery components from context
            var queryText = context.OriginalRequest.QueryText ?? string.Empty;
            var userId = context.OriginalRequest.UserId ?? "UnknownUser"; // Provide a default if UserId is null
            var userQuery = new UserQuery(queryText, userId, new Dictionary<string, object>());

            // Fetch content based on ArtifactReferences
            List<ArtifactContent>? fetchedContents = null;
            if (context.OriginalRequest.ArtifactReferences?.Any() == true)
            {
                _logger.LogInformation("Found {Count} artifact references to process.", context.OriginalRequest.ArtifactReferences.Count);
                fetchedContents = new List<ArtifactContent>();
                foreach (var artifactRef in context.OriginalRequest.ArtifactReferences)
                {
                    if (artifactRef == null) continue;

                    var provider = _artifactProviders.FirstOrDefault(p => 
                        p.SupportedReferenceTypes.Contains(artifactRef.ReferenceType, StringComparer.OrdinalIgnoreCase));

                    if (provider != null)
                    {
                        try
                        {
                            _logger.LogInformation("Attempting to fetch content for ReferenceType '{Type}' using provider {ProviderType}.", 
                                artifactRef.ReferenceType, provider.GetType().Name);
                            var content = await provider.GetContentAsync(artifactRef, cancellationToken);
                            if (content != null)
                            {
                                fetchedContents.Add(content);
                                _logger.LogInformation("Successfully fetched content for ReferenceType '{Type}'. Size: {Size} bytes.", 
                                    artifactRef.ReferenceType, content.ContentStream?.Length ?? -1);
                            }
                            else
                            {
                                _logger.LogWarning("Provider {ProviderType} returned null content for ReferenceType '{Type}'.", 
                                    provider.GetType().Name, artifactRef.ReferenceType);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error fetching artifact content for ReferenceType '{Type}' with provider {ProviderType}. ReferenceId: {ReferenceId}", 
                                artifactRef.ReferenceType, provider.GetType().Name, artifactRef.ReferenceId);
                            // Decide if we should continue processing other artifacts or fail the whole request
                            // For now, log and continue
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No IArtifactProvider found for ReferenceType '{Type}'. Skipping artifact.", artifactRef.ReferenceType);
                    }
                }
            }

            // Call HandleQueryAsync with UserQuery object and fetched contents
            var personaResponse = await persona.HandleQueryAsync(userQuery, fetchedContents, cancellationToken);

            // Log ResponseText
            _logger.LogInformation("Persona {PersonaId} handled query. Response Text: {ResponseText}", _personaId, personaResponse.ResponseText ?? "<null>");

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
            _logger.LogError(ex, "Error occurred while persona {PersonaId} handled query.", _personaId);
            return new AdapterResponse(Success: false, ResponseMessage: "Error occurred during query handling.", ErrorMessage: ex.Message);
        }
    }
}