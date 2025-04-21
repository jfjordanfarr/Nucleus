using System.Net.Http;
using System.Net.Http.Json; // Requires Microsoft.Extensions.Http
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models; // Added for AdapterRequest/Response

namespace Nucleus.Console.Services;

/// <summary>
/// Agent for interacting with the Nucleus API service's interaction endpoint.
/// Uses HttpClient configured via DI with Aspire service discovery.
/// </summary>
public class NucleusApiServiceAgent // Rename class
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NucleusApiServiceAgent> _logger; // Update logger type
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    // Inject HttpClient directly thanks to typed client registration in Program.cs
    public NucleusApiServiceAgent(HttpClient httpClient, ILogger<NucleusApiServiceAgent> logger) // Update constructor
    {
        _httpClient = httpClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a standardized interaction request to the API.
    /// </summary>
    /// <param name="request">The AdapterRequest containing interaction details.</param>
    /// <returns>The AdapterResponse from the API, or null if the request failed or the response couldn't be parsed.</returns>
    public async Task<AdapterResponse?> SendInteractionAsync(AdapterRequest request)
    {
        if (request == null)
        {
            _logger.LogError("SendInteractionAsync called with a null request.");
            return new AdapterResponse(Success: false, ResponseMessage: "Internal error: Request cannot be null.", ErrorMessage: "Null AdapterRequest");
        }

        try
        {
            _logger.LogInformation("Sending interaction request to {Endpoint}. Platform: {PlatformType}, ConversationId: {ConversationId}, UserId: {UserId}, Query: '{QueryText}', Artifacts: {ArtifactCount}",
                                 "/api/interaction/process",
                                 request.PlatformType,
                                 request.ConversationId,
                                 request.UserId,
                                 request.QueryText,
                                 request.ArtifactReferences?.Count ?? 0);

            using var response = await _httpClient.PostAsJsonAsync("/api/interaction/process", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Interaction request successful (Status: {StatusCode}). Attempting to deserialize response...", response.StatusCode);
                var adapterResponse = await response.Content.ReadFromJsonAsync<AdapterResponse>(_jsonOptions);
                if (adapterResponse != null)
                {
                    _logger.LogInformation("Interaction response deserialized successfully.");
                    return adapterResponse;
                }
                else
                {
                     _logger.LogWarning("Interaction request succeeded, but response body was empty or could not be deserialized.");
                     return new AdapterResponse(Success: false, ResponseMessage: "API returned success but response was empty or invalid.", ErrorMessage: "Failed to deserialize successful response");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Interaction request failed. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}",
                                 response.StatusCode, response.ReasonPhrase, errorContent);
                 try
                 {
                     var errorResponse = JsonSerializer.Deserialize<AdapterResponse>(errorContent, _jsonOptions);
                     if (errorResponse != null) return errorResponse;
                 }
                 catch (JsonException) { /* Ignore if error content is not a valid AdapterResponse */ }

                return new AdapterResponse(Success: false, ResponseMessage: $"API request failed with status {response.StatusCode}.", ErrorMessage: $"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception during interaction. Ensure API service is running at {BaseAddress}.", _httpClient.BaseAddress);
            return new AdapterResponse(Success: false, ResponseMessage: "Failed to connect to the API service.", ErrorMessage: ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize request or deserialize response JSON during interaction.");
            return new AdapterResponse(Success: false, ResponseMessage: "Error processing JSON data.", ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during interaction request.");
            return new AdapterResponse(Success: false, ResponseMessage: "An unexpected error occurred.", ErrorMessage: ex.Message);
        }
    }
}
