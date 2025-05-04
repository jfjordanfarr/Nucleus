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

    /// <summary>
    /// Defines string keys for registering and resolving IContentExtractor implementations.
    /// </summary>
    public static class ExtractorKeys
    {
        /// <summary>
        /// Key for the extractor that handles plain text (.txt).
        /// </summary>
        public const string PlainText = "PlainText";

        /// <summary>
        /// Key for the extractor that handles HTML (.html, .htm).
        /// </summary>
        public const string Html = "Html";

        /// <summary>
        /// Key for a future extractor that might handle Markdown (.md).
        /// </summary>
        public const string Markdown = "Markdown";

        /// <summary>
        /// Key for a future extractor that might handle XML (.xml).
        /// </summary>
        public const string Xml = "Xml";

        /// <summary>
        /// Key for a future extractor that might handle JSON (.json).
        /// </summary>
        public const string Json = "Json";

        /// <summary>
        /// Key for a future extractor that might handle PDFs (.pdf).
        /// </summary>
        public const string Pdf = "Pdf";

        /// <summary>
        /// Key for a generic extractor or a fallback when no specific type is matched.
        /// </summary>
        public const string Default = "Default"; // Or PlainText often serves as default

        /// <summary>
        /// Key for a fallback extractor for unknown binary types.
        /// </summary>
        public const string DefaultBinary = "DefaultBinary";
    }

    /// <summary>
    /// Defines standard MIME type constants used for content identification.
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// Common MIME types for text-based content.
        /// </summary>
        public static class Textual
        {
            /// <summary>
            /// MIME type for HTML: text/html
            /// </summary>
            public const string Html = "text/html";

            /// <summary>
            /// MIME type for Plain Text: text/plain
            /// </summary>
            public const string Plain = "text/plain";

            /// <summary>
            /// MIME type for Markdown: text/markdown
            /// </summary>
            public const string Markdown = "text/markdown";

            /// <summary>
            /// MIME type for CSV: text/csv
            /// </summary>
            public const string Csv = "text/csv";

            // Add other text types like xml, json as needed - Note: XML/JSON often use application/*
        }

        /// <summary>
        /// Application-specific MIME types.
        /// </summary>
        public static class Application
        {
            /// <summary>
            /// MIME type for JSON: application/json
            /// </summary>
            public const string Json = "application/json";

            /// <summary>
            /// MIME type for PDF: application/pdf
            /// </summary>
            public const string Pdf = "application/pdf";

            /// <summary>
            /// MIME type for XML: application/xml
            /// </summary>
            public const string Xml = "application/xml";

            /// <summary>
            /// MIME type for generic binary data: application/octet-stream
            /// </summary>
            public const string OctetStream = "application/octet-stream";

            // Add other application types like docx, xlsx as needed
        }

        // Add other categories like Image, Audio, Video as needed
    }

    /// <summary>
    /// Defines standard keys used in metadata dictionaries (e.g., AdapterRequest.Metadata).
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// The key used in AdapterRequest.Metadata to specify the desired PersonaId for processing.
        /// </summary>
        public const string TargetPersonaId = "TargetPersonaId";

        // Add other metadata keys as needed
    }

    // Add other constant groups as needed (e.g., ServiceBus Queues, Cache Keys, etc.)
}

/// <summary>
/// Enum representing the confidence level of an analysis or assertion.
/// Moved here from IPersonaKnowledgeRepository.cs for broader availability.
/// </summary>
public enum ConfidenceLevel
{
    Unknown = 0,
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    VeryHigh = 5,
    Certain = 6
}

/// <summary>
/// Represents the status of a Persona's execution attempt via IPersonaRuntime.
/// </summary>
public enum PersonaExecutionStatus
{
    /// <summary>
    /// Execution status is unknown or not yet determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The persona executed successfully and produced a response.
    /// </summary>
    Success = 1,

    /// <summary>
    /// The persona encountered an error during execution.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The persona determined the input should be filtered or ignored (e.g., based on salience, rules).
    /// </summary>
    Filtered = 3,

    /// <summary>
    /// The persona executed successfully but determined no specific action or response was required.
    /// </summary>
    NoActionTaken = 4
}

// End of namespace
