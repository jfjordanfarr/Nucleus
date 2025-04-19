namespace Nucleus.ApiService.Configuration
{
    /// <summary>
    /// Configuration options for the Gemini AI provider.
    /// Maps to the "Gemini" section in configuration (e.g., appsettings.json).
    /// </summary>
    public class GeminiOptions
    {
        public const string SectionName = "Gemini"; // Optional: constant for section name

        /// <summary>
        /// The API Key required to authenticate with the Gemini API.
        /// Should typically be sourced from user secrets or environment variables.
        /// </summary>
        public string? ApiKey { get; set; }

        // Add other Gemini-specific options here if needed in the future
        // e.g., DefaultModel, TimeoutSeconds, etc.
    }
}
