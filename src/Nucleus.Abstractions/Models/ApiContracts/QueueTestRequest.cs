namespace Nucleus.Abstractions.Models.ApiContracts;

/// <summary>
/// Request model for the queue test endpoint.
/// </summary>
public record QueueTestRequest
{
    /// <summary>
    /// Simple string data to be included in the queued message.
    /// </summary>
    public string TestData { get; init; } = string.Empty;
}
