// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions; // For NucleusConstants
// using Nucleus.Abstractions.Models.Static; // Assuming NucleusAgenticStrategyKeys might go here if created
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Infrastructure.Persistence.Configuration;

/// <summary>
/// A temporary, in-memory implementation of <see cref="IPersonaConfigurationProvider"/>.
/// Loads hardcoded configurations for testing and development purposes, aligned with definitions
/// in Nucleus.Abstractions and architecture documents.
/// </summary>
/// <remarks>
/// This provider is intended for development and testing ONLY.
/// For production, replace this with a provider that utilizes the standard .NET IConfiguration system.
/// See: [Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md](cci:7://file:///d:/Projects/Nucleus/Docs/Architecture/Personas/ARCHITECTURE_PERSONAS_CONFIGURATION.md:0:0-0:0)
/// </remarks>
public class InMemoryPersonaConfigurationProvider : IPersonaConfigurationProvider
{
    private readonly ILogger<InMemoryPersonaConfigurationProvider> _logger;
    private readonly Dictionary<string, PersonaConfiguration> _configurations;

    public InMemoryPersonaConfigurationProvider(ILogger<InMemoryPersonaConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurations = new Dictionary<string, PersonaConfiguration>(StringComparer.OrdinalIgnoreCase);
        LoadDefaultConfigurations();
    }

    private void LoadDefaultConfigurations()
    {
        // --- Bootstrapper Persona ---
        // Based on ARCHITECTURE_PERSONAS_BOOTSTRAPPER.md and PersonaConfiguration.cs model
        var bootstrapperConfig = new PersonaConfiguration
        {
            PersonaId = "Bootstrapper_v1", // Consistent ID
            DisplayName = "Nucleus Bootstrapper",
            Description = "Handles initial setup, basic interactions, and serves as a fallback.",
            ShowYourWork = false, // Keep it simple
            ActivationTriggers = new List<string> { "@Nucleus", "/nucleus", "setup" }, // Corrected: List<string>
            ContextScope = new Dictionary<string, object> { { "Platform", "Any" } }, // Example scope
            LlmConfiguration = new LlmConfiguration
            {
                Provider = NucleusConstants.Llm.GoogleProvider,
                ChatModelId = NucleusConstants.Llm.GeminiChatModel,
                EmbeddingModelId = NucleusConstants.Llm.GeminiEmbeddingModel
            },
            KnowledgeScope = new KnowledgeScope
            {
                Strategy = KnowledgeScopeStrategy.NoUserArtifacts, // Correct Enum
                MaxContextDocuments = 0 // No external docs needed
            },
            SystemMessage = "You are the Nucleus system's Bootstrapper assistant. You handle initial interactions and basic setup guidance. Be concise and helpful.",
            ResponseGuidelines = "Provide clear, direct responses. If unable to handle a request, indicate that.",
            AgenticStrategy = new AgenticStrategyConfiguration // Corrected Type
            {
                StrategyKey = NucleusConstants.AgenticStrategies.Echo, // Use Echo or SimpleRag for bootstrapping
                MaxIterations = 1
                // Parameters = null (or specific parameter object if needed)
            },
            EnabledTools = new List<string>() // Bootstrapper typically has no tools
        };
        _configurations.Add(bootstrapperConfig.PersonaId, bootstrapperConfig);
        _logger.LogInformation("Loaded default persona configuration: {PersonaId}", bootstrapperConfig.PersonaId);

        // --- Educator Persona ---
        // Based on ARCHITECTURE_PERSONAS_EDUCATOR.md and PersonaConfiguration.cs model
        var educatorConfig = new PersonaConfiguration
        {
            PersonaId = "Educator_v1", // Consistent ID
            DisplayName = "Educator Assistant",
            Description = "Assists with personalized learning, analyzes educational artifacts, and generates tailored content.",
            ShowYourWork = true,
            ActivationTriggers = new List<string> { "teach", "explain", "analyze my writing", "review this report", "help me learn" }, // Corrected: List<string>
            ContextScope = new Dictionary<string, object> { { "Subject", "General" } }, // Example scope
            LlmConfiguration = new LlmConfiguration
            {
                Provider = NucleusConstants.Llm.GoogleProvider,
                ChatModelId = NucleusConstants.Llm.GeminiChatModel,
                EmbeddingModelId = NucleusConstants.Llm.GeminiEmbeddingModel,
                Temperature = 0.6f
            },
            KnowledgeScope = new KnowledgeScope
            {
                Strategy = KnowledgeScopeStrategy.AllUserArtifacts, // Corrected Enum (Maps from 'Contextual')
                // CollectionIds = new List<string> { "CurriculumMaterials", "StudentSubmissions" }, // Example if using SpecificCollectionIds
                MaxContextDocuments = 15 // Educator may need more context
                // TargetKnowledgeContainerId = "EducatorKnowledgeBase" // Example if using dedicated knowledge
            },
            SystemMessage = "You are an encouraging and knowledgeable Educator AI. Your goal is to support the user's learning journey by analyzing provided materials (like essays, reports, or readings) and offering constructive feedback, explanations, and tailored learning activities. Focus on clarity, encouragement, and adapting to the user's needs. Be mindful of the sensitivity of educational data.",
            ResponseGuidelines = "Break down complex topics. Use analogies. Ask clarifying questions. Cite sources if using web search.",
            AgenticStrategy = new AgenticStrategyConfiguration // Corrected Type
            {
                StrategyKey = NucleusConstants.AgenticStrategies.MultiStepReasoning, // Corrected Key (Maps from PlanThenExecute concept)
                MaxIterations = 5 // Corrected property (Maps from MaxTurns concept)
                // Parameters = null (or specific parameter object if needed)
            },
            EnabledTools = new List<string> { "WebSearch", "ContentGeneration" } // Example tools
        };
        _configurations.Add(educatorConfig.PersonaId, educatorConfig);
        _logger.LogInformation("Loaded default persona configuration: {PersonaId}", educatorConfig.PersonaId);

        // --- Professional Persona ---
        // Based on ARCHITECTURE_PERSONAS_PROFESSIONAL.md and PersonaConfiguration.cs model
        var professionalConfig = new PersonaConfiguration
        {
            PersonaId = "Professional_v1", // Consistent ID
            DisplayName = "Professional Colleague",
            Description = "Acts as a helpful professional colleague, skilled in analyzing work documents, drafting communications, and providing concise summaries or insights.",
            ShowYourWork = true, // Can be useful for complex tasks
            ActivationTriggers = new List<string> { "draft email", "summarize this", "analyze report", "review document", "professional opinion" }, // Corrected: List<string>
            ContextScope = new Dictionary<string, object> { { "Domain", "Business" } }, // Example scope
            LlmConfiguration = new LlmConfiguration
            {
                Provider = NucleusConstants.Llm.GoogleProvider,
                ChatModelId = NucleusConstants.Llm.GeminiChatModel,
                EmbeddingModelId = NucleusConstants.Llm.GeminiEmbeddingModel,
                Temperature = 0.5f,
                MaxOutputTokens = 2048
            },
            KnowledgeScope = new KnowledgeScope
            {
                Strategy = KnowledgeScopeStrategy.SpecificCollectionIds, // Corrected Enum (Maps from 'FocusedRetrieval')
                CollectionIds = new List<string> { "ProjectDocs", "MeetingNotes", "TeamRepository" }, // Requires specific collections
                MaxContextDocuments = 10
                // TargetKnowledgeContainerId = "CorporateWiki" // Example if using dedicated knowledge
            },
            SystemMessage = "You are a competent and efficient Professional AI Colleague. Respond concisely and directly to requests. Analyze provided documents thoroughly, extract key information, draft professional communications (emails, reports), and offer actionable insights. Assume a professional context and maintain confidentiality.",
            ResponseGuidelines = "Be formal and objective. Structure complex information clearly. Reference data sources accurately.",
            AgenticStrategy = new AgenticStrategyConfiguration // Corrected Type
            {
                StrategyKey = NucleusConstants.AgenticStrategies.MultiStepReasoning, // Corrected Key (Maps from PlanThenExecute concept)
                MaxIterations = 7 // Corrected property (Maps from MaxTurns concept)
                // Parameters = null (or specific parameter object if needed)
            },
            EnabledTools = new List<string> { "DocumentAnalysis", "EmailDrafting", "Summarization" } // Example tools
        };
        _configurations.Add(professionalConfig.PersonaId, professionalConfig);
        _logger.LogInformation("Loaded default persona configuration: {PersonaId}", professionalConfig.PersonaId);

    }

    /// <inheritdoc />
    public Task<PersonaConfiguration?> GetConfigurationAsync(string personaId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personaId);

        if (_configurations.TryGetValue(personaId, out var config))
        {
            _logger.LogDebug("Found configuration for Persona ID: {PersonaId}", personaId);
            // Return a clone to prevent modification of the cached instance if necessary.
            // For now, returning direct reference assuming read-only usage after init.
            return Task.FromResult<PersonaConfiguration?>(config);
        }
        else
        {
            _logger.LogWarning("Configuration not found for Persona ID: {PersonaId}", personaId);
            return Task.FromResult<PersonaConfiguration?>(null);
        }
    }
}