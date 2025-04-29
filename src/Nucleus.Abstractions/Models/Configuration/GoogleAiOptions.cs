namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Represents the configuration options specifically for Google AI services (e.g., Gemini).
/// Used for binding values from `appsettings.json` (typically under the "AI:GoogleAI" section).
/// <seealso cref="../../../../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md"/>
/// </summary>
public class GoogleAiOptions
{
    public const string SectionName = "AI:GoogleAI";

    /// <summary>
    /// The API Key for authenticating with the Google AI service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The specific model ID to use (e.g., "gemini-1.5-flash-latest").
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
}
