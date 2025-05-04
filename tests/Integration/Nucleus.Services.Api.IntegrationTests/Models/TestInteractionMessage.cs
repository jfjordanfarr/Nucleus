namespace Nucleus.Services.Api.IntegrationTests.Models;

/// <summary>
/// A simple message structure for Service Bus integration testing.
/// </summary>
public record TestInteractionMessage(Guid MessageId, string Content);
