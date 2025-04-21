namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Configuration options for the Google AI (Gemini) client.
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
