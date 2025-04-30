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
            }
        };
        // Populate read-only collections after initialization
        bootstrapperConfig.ActivationTriggers.Add("@Nucleus");
        bootstrapperConfig.ActivationTriggers.Add("/nucleus");
        bootstrapperConfig.ActivationTriggers.Add("setup");
        bootstrapperConfig.ContextScope.Add("Platform", "Any");
        // EnabledTools remains empty, no Add calls needed.

        _configurations.Add(bootstrapperConfig.PersonaId, bootstrapperConfig);
        _logger.LogInformation("Loaded default persona configuration: {PersonaId}", bootstrapperConfig.PersonaId);

        // --- Educator Persona ---
        // Based on ARCHITECTURE_PERSONAS_EDUCATOR.md and PersonaConfiguration.cs model
        var educatorConfig = new PersonaConfiguration
        {
            PersonaId = "Educator_v1", // Consistent ID
            DisplayName = "Homeschool Educator Assistant",
            Description = "Assists with lesson planning, educational content generation, and progress tracking for homeschooling.",
            ShowYourWork = false, // Less important for direct educational interaction
            LlmConfiguration = new LlmConfiguration
            {
                Provider = NucleusConstants.Llm.GoogleProvider,
                ChatModelId = NucleusConstants.Llm.GeminiChatModel, // Or a model fine-tuned for education
                EmbeddingModelId = NucleusConstants.Llm.GeminiEmbeddingModel,
                Temperature = 0.7f, // Allow more creativity
                MaxOutputTokens = 1500
            },
            KnowledgeScope = new KnowledgeScope
            {
                Strategy = KnowledgeScopeStrategy.SpecificCollectionIds, // Corrected Enum (Maps from 'UserContextual')
                MaxContextDocuments = 5,
                CollectionIds = { "HomeschoolingResources" }
                // TargetKnowledgeContainerId = "FamilyNotebook" // Example if using dedicated knowledge
            },
            SystemMessage = "You are a helpful and patient AI assistant for homeschooling. Generate age-appropriate educational content, explain concepts clearly, create simple quizzes, and help with lesson planning. Adapt to the specified subject and grade level.",
            ResponseGuidelines = "Be encouraging and clear. Use simple language suitable for children and educators. Prioritize educational value.",
            AgenticStrategy = new AgenticStrategyConfiguration // Corrected Type
            {
                StrategyKey = NucleusConstants.AgenticStrategies.SimpleRag, // Corrected Key (Maps from BasicRetrieval concept)
                MaxIterations = 3 // Corrected property (Maps from MaxTurns concept)
                // Parameters = null (or specific parameter object if needed)
            }
        };
        // Populate read-only collections after initialization
        educatorConfig.ActivationTriggers.Add("lesson plan");
        educatorConfig.ActivationTriggers.Add("explain concept");
        educatorConfig.ActivationTriggers.Add("create quiz");
        educatorConfig.ActivationTriggers.Add("homeschool help");
        educatorConfig.ContextScope.Add("Subject", "General");
        educatorConfig.ContextScope.Add("GradeLevel", "Elementary");
        educatorConfig.EnabledTools.Add("ContentGeneration");
        educatorConfig.EnabledTools.Add("QuizCreation");

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
            }
        };
        // Populate read-only collections after initialization
        professionalConfig.ActivationTriggers.Add("draft email");
        professionalConfig.ActivationTriggers.Add("summarize this");
        professionalConfig.ActivationTriggers.Add("analyze report");
        professionalConfig.ActivationTriggers.Add("review document");
        professionalConfig.ActivationTriggers.Add("professional opinion");
        professionalConfig.ContextScope.Add("Domain", "Business");
        professionalConfig.KnowledgeScope.CollectionIds.Add("ProjectDocs");
        professionalConfig.KnowledgeScope.CollectionIds.Add("MeetingNotes");
        professionalConfig.KnowledgeScope.CollectionIds.Add("TeamRepository");
        professionalConfig.EnabledTools.Add("DocumentAnalysis");
        professionalConfig.EnabledTools.Add("EmailDrafting");
        professionalConfig.EnabledTools.Add("Summarization");

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