using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing.Infrastructure;

/// <summary>
/// Adapter for Google Cloud Vertex AI SDK (Gemini/GPT) for LLM chat completion.
/// Directly uses Google.Cloud.AIPlatform.V1 types and logic.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_LLM_INTEGRATION.md
/// </summary>
public class VertexAiChatClientAdapter : IDisposable
{
    private readonly PredictionServiceClient _predictionServiceClient;
    private readonly VertexAiOptions _options;
    private readonly ILogger<VertexAiChatClientAdapter> _logger;
    private readonly EndpointName _endpointName;

    public VertexAiChatClientAdapter(
        PredictionServiceClient predictionServiceClient,
        IOptions<VertexAiOptions> options,
        ILogger<VertexAiChatClientAdapter> logger)
    {
        _predictionServiceClient = predictionServiceClient ?? throw new ArgumentNullException(nameof(predictionServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate essential options and build endpoint name
        if (string.IsNullOrWhiteSpace(_options.Project) ||
            string.IsNullOrWhiteSpace(_options.Location) ||
            string.IsNullOrWhiteSpace(_options.Publisher) ||
            string.IsNullOrWhiteSpace(_options.ModelId))
        {
            throw new ArgumentException("Vertex AI options (Project, Location, Publisher, ModelId) must be configured.", nameof(options));
        }

        _endpointName = EndpointName.FromProjectLocationPublisherModel(_options.Project, _options.Location, _options.Publisher, _options.ModelId);
        _logger.LogInformation("Vertex AI Chat Client Adapter configured for endpoint: {Endpoint}", _endpointName);
    }

    /// <summary>
    /// Sends a prompt to Vertex AI and returns the generated response as a string.
    /// </summary>
    public async Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetCompletionAsync called with prompt ({Length} chars).", prompt.Length);

        var instance = Google.Protobuf.WellKnownTypes.Value.ForStruct(new Google.Protobuf.WellKnownTypes.Struct
        {
            Fields = { { "prompt", Google.Protobuf.WellKnownTypes.Value.ForString(prompt) } }
        });

        var request = new PredictRequest
        {
            EndpointAsEndpointName = _endpointName,
            Instances = { instance },
            Parameters = new Google.Protobuf.WellKnownTypes.Value { StructValue = new Google.Protobuf.WellKnownTypes.Struct() }
        };

        try
        {
            _logger.LogInformation("Sending request to Vertex AI endpoint: {Endpoint}", _endpointName);
            PredictResponse response = await _predictionServiceClient.PredictAsync(request, cancellationToken);
            _logger.LogInformation("Received response from Vertex AI.");

            string responseText = "Error: Could not parse Vertex AI response.";
            if (response.Predictions.Count > 0 &&
                response.Predictions[0].KindCase == Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StructValue &&
                response.Predictions[0].StructValue.Fields.TryGetValue("content", out var contentValue) &&
                contentValue.KindCase == Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StringValue)
            {
                responseText = contentValue.StringValue;
            }
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Vertex AI PredictAsync call.");
            throw;
        }
    }

    // Optionally, add streaming support if needed.
    // public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    // {
    //     // Not implemented: Vertex AI streaming support can be added here.
    //     yield break;
    // }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
