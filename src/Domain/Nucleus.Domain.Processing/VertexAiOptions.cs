namespace Nucleus.Domain.Processing;

/// <summary>
/// Configuration options for connecting to Google Cloud Vertex AI.
/// </summary>
public class VertexAiOptions
{
    public const string SectionName = "AI:VertexAI";

    /// <summary>
    /// Gets or sets the Google Cloud Project ID.
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets the Google Cloud Location (e.g., "us-central1").
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the Model Publisher (e.g., "google").
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the Model ID (e.g., "gemini-1.5-flash-preview-0514").
    /// </summary>
    public string? ModelId { get; set; }
}
