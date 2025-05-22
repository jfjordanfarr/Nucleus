using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.ApiContracts;
using Nucleus.Abstractions.Orchestration;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Nucleus.Services.Api.Endpoints;

/// <summary>
/// Defines ingestion-related endpoints.
/// </summary>
public static class IngestEndpoints
{
    /// <summary>
    /// Maps ingestion endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/ingest").WithTags("Ingest");

        group.MapPost("/queue-test", HandleQueueTestAsync)
             .WithName("QueueTest")
             .WithSummary("Queues a test message for background processing.")
             .WithDescription("Accepts a QueueTestRequest and uses IBackgroundTaskQueue to enqueue it.")
             .Produces(StatusCodes.Status202Accepted)
             .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> HandleQueueTestAsync(
        [FromBody] QueueTestRequest request,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Create logger instance
        var logger = loggerFactory.CreateLogger("Nucleus.Services.Api.Endpoints.Ingest");

        logger.LogInformation("Received request for /queue-test with data: {TestData}", request.TestData);

        try
        {
            // Create a NucleusIngestionRequest from the test request data.
            // We'll use minimal required fields for this test.
            var ingestionRequest = new NucleusIngestionRequest(
                PlatformType: Nucleus.Abstractions.Models.PlatformType.Test,              // REQUIRED
                OriginatingUserId: "test-user",                                       // REQUIRED
                OriginatingConversationId: "test-conversation-" + System.Guid.NewGuid().ToString(), // REQUIRED - Mapped from old PlatformInteractionId concept for test
                TimestampUtc: DateTimeOffset.UtcNow,                                // REQUIRED
                OriginatingReplyToMessageId: null,                                  // Optional
                OriginatingMessageId: "queue-test-message-" + System.Guid.NewGuid().ToString(), // Optional - Simulating a new message ID
                ResolvedPersonaId: "test-persona",                                  // Optional - Mapped from old PersonaId for test
                QueryText: request.TestData,                                        // Optional - Mapped from old MessageContent for test
                ArtifactReferences: new List<Nucleus.Abstractions.Models.ArtifactReference>(), // Optional
                TenantId: "test-tenant", // ADDED TenantId for testing
                CorrelationId: System.Guid.NewGuid().ToString(),                    // Optional
                Metadata: null                                                      // Optional
            );

            await queue.QueueBackgroundWorkItemAsync(ingestionRequest, cancellationToken);

            logger.LogInformation("Successfully queued test data: {TestData}", request.TestData);
            return Results.Accepted(); // 202 Accepted is appropriate for async processing
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error queuing test message: {ErrorMessage}", ex.Message);
            return Results.Problem("An error occurred while queuing the test message.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
