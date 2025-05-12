using Nucleus.Abstractions.Models.Configuration;
using System.Collections.Generic;

namespace Nucleus.Infrastructure.Testing.Utilities
{
    /// <summary>
    /// A fluent builder for creating <see cref="PersonaConfiguration"/> instances for testing purposes.
    /// </summary>
    public class PersonaConfigurationTestDataBuilder
    {
        private readonly PersonaConfiguration _configuration;

        private PersonaConfigurationTestDataBuilder()
        {
            _configuration = new PersonaConfiguration();
            // Initialize complex types to avoid null references if not explicitly set by the builder
            _configuration.LlmConfiguration = new LlmConfiguration();
            _configuration.DataGovernance = new DataGovernanceConfiguration();
            _configuration.KnowledgeScope = new KnowledgeScope();
            _configuration.AgenticStrategy = new AgenticStrategyConfiguration();
        }

        /// <summary>
        /// Creates a new instance of the builder.
        /// </summary>
        public static PersonaConfigurationTestDataBuilder Create()
        {
            return new PersonaConfigurationTestDataBuilder();
        }

        public PersonaConfigurationTestDataBuilder WithPersonaId(string personaId)
        {
            _configuration.PersonaId = personaId;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithDisplayName(string displayName)
        {
            _configuration.DisplayName = displayName;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithDescription(string? description)
        {
            _configuration.Description = description;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithShowYourWork(bool showYourWork)
        {
            _configuration.ShowYourWork = showYourWork;
            return this;
        }

        public PersonaConfigurationTestDataBuilder AddActivationTrigger(string trigger)
        {
            _configuration.ActivationTriggers.Add(trigger);
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithActivationTriggers(List<string> triggers)
        {
            _configuration.ActivationTriggers.Clear();
            _configuration.ActivationTriggers.AddRange(triggers);
            return this;
        }

        public PersonaConfigurationTestDataBuilder AddContextScope(string key, object value)
        {
            _configuration.ContextScope[key] = value;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithContextScope(Dictionary<string, object> scope)
        {
            ArgumentNullException.ThrowIfNull(scope);
            _configuration.ContextScope.Clear();
            foreach (var kvp in scope)
            {
                _configuration.ContextScope.Add(kvp.Key, kvp.Value);
            }
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithLlmConfiguration(LlmConfiguration llmConfig)
        {
            _configuration.LlmConfiguration = llmConfig;
            return this;
        }

        // Helper for LlmConfiguration properties if needed, e.g.:
        public PersonaConfigurationTestDataBuilder WithLlmModelId(string modelId)
        {
            _configuration.LlmConfiguration.ChatModelId = modelId; 
            return this;
        }

        public PersonaConfigurationTestDataBuilder AddEnabledTool(string toolId)
        {
            _configuration.EnabledTools.Add(toolId);
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithEnabledTools(List<string> tools)
        {
            _configuration.EnabledTools.Clear();
            _configuration.EnabledTools.AddRange(tools);
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithDataGovernance(DataGovernanceConfiguration dataGovernance)
        {
            _configuration.DataGovernance = dataGovernance;
            return this;
        }

        public PersonaConfigurationTestDataBuilder AddAllowedTenantId(string tenantId)
        {
            _configuration.DataGovernance.AllowedTenantIds.Add(tenantId);
            return this;
        }
        
        public PersonaConfigurationTestDataBuilder WithAllowedTenantIds(List<string> tenantIds)
        {
            _configuration.DataGovernance.AllowedTenantIds.Clear();
            _configuration.DataGovernance.AllowedTenantIds.AddRange(tenantIds);
            return this;
        }

        public PersonaConfigurationTestDataBuilder AddAllowedConversationId(string conversationId)
        {
            _configuration.DataGovernance.AllowedConversationIds.Add(conversationId);
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithAllowedConversationIds(List<string> conversationIds)
        {
            _configuration.DataGovernance.AllowedConversationIds.Clear();
            _configuration.DataGovernance.AllowedConversationIds.AddRange(conversationIds);
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithKnowledgeScope(KnowledgeScope knowledgeScope)
        {
            _configuration.KnowledgeScope = knowledgeScope;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithKnowledgeScopeStrategy(KnowledgeScopeStrategy strategy)
        {
            _configuration.KnowledgeScope.Strategy = strategy; 
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithTargetKnowledgeContainerId(string? containerId)
        {
            _configuration.KnowledgeScope.TargetKnowledgeContainerId = containerId;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithAgenticStrategy(AgenticStrategyConfiguration agenticStrategy)
        {
            _configuration.AgenticStrategy = agenticStrategy;
            return this;
        }

        public PersonaConfigurationTestDataBuilder WithAgenticStrategyKey(string strategyKey)
        {
            _configuration.AgenticStrategy.StrategyKey = strategyKey;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured <see cref="PersonaConfiguration"/> instance.
        /// </summary>
        public PersonaConfiguration Build()
        {
            // Perform any final validation or default setting if necessary before returning
            if (string.IsNullOrEmpty(_configuration.PersonaId))
            {
                _configuration.PersonaId = System.Guid.NewGuid().ToString(); // Default if not set
            }
            if (string.IsNullOrEmpty(_configuration.DisplayName))
            {
                _configuration.DisplayName = "Test Persona"; // Default if not set
            }
            return _configuration;
        }
    }
}
