// In Nucleus.Abstractions/NucleusConstants.cs
namespace Nucleus.Abstractions;

/// <summary>
/// Defines constants used throughout the Nucleus application.
/// Helps prevent 'magic strings' and ensures consistency.
/// </summary>
public static class NucleusConstants
{
    /// <summary>
    /// Constants related to Persona identification and keys.
    /// </summary>
    public static class PersonaKeys
    {
        /// <summary>
        /// The key used for the default/bootstrapper persona.
        /// </summary>
        public const string Default = "Default_v1";
        // Add other persona keys here later as needed
    }

    /// <summary>
    /// Constants related to configuration keys/sections.
    /// Defines standard keys used in `appsettings.json` or other configuration sources.
    /// <seealso cref="../../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string PersonasSection = "Personas";
        public const string GoogleAISection = "AI:GoogleAI";
        // Add other configuration keys here later as needed
    }

    /// <summary>
    /// Constants related to Platform identifiers.
    /// </summary>
    public static class PlatformIdentifiers
    {
        // Use constants defined in PlatformType enum where possible,
        // but define explicit strings if needed elsewhere.
        public const string Api = "Api";
        public const string Console = "Console";
        public const string Teams = "Teams";
    }

    /// <summary>
    /// Constants related to LLM Providers and Models.
    /// </summary>
    public static class Llm
    {
        public const string GoogleProvider = "GoogleGemini";
        public const string GeminiChatModel = "gemini-2.0-flash"; // Baseline model
        public const string GeminiEmbeddingModel = "text-embedding-004"; // Current embedding model
        // Add other provider/model constants as needed
    }

    /// <summary>
    /// Constants defining string keys for resolving IAgenticStrategyHandler implementations.
    /// These correspond to the StrategyKey in AgenticStrategyConfiguration.
    /// </summary>
    public static class AgenticStrategies
    {
        /// <summary>
        /// Key for the basic Retrieve-Augment-Generate strategy handler.
        /// </summary>
        public const string SimpleRag = "SimpleRag";

        /// <summary>
        /// Key for strategies involving internal planning and potentially multiple steps.
        /// </summary>
        public const string MultiStepReasoning = "MultiStepReasoning";

        /// <summary>
        /// Key for strategies that can utilize registered tools.
        /// </summary>
        public const string ToolUsing = "ToolUsing";

        /// <summary>
        /// Key for a simple testing strategy that echoes the input.
        /// </summary>
        public const string Echo = "Echo";
    }

    // Add other constant groups as needed (e.g., ServiceBus Queues, Cache Keys, etc.)
}
