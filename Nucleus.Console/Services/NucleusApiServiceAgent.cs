using System.Net.Http;
using System.Net.Http.Json; // Requires Microsoft.Extensions.Http
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions; // Use Abstractions

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
            return new AdapterResponse("Internal error: Request cannot be null.", IsError: true, ErrorMessage: "Null AdapterRequest");
        }

        try
        {
            _logger.LogInformation("Sending interaction request to {Endpoint}. SessionId: {SessionId}, Input: '{InputText}', ContextId: '{ContextId}', Artifacts: {ArtifactCount}",
                                 "/api/interaction",
                                 request.SessionId,
                                 request.InputText,
                                 request.ContextSourceIdentifier ?? "None",
                                 request.Artifacts?.Count ?? 0);

            using var response = await _httpClient.PostAsJsonAsync("/api/interaction", request, _jsonOptions);

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
                     // Return a response indicating the issue
                     return new AdapterResponse("API returned success but response was empty or invalid.", IsError: true, ErrorMessage: "Failed to deserialize successful response");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Interaction request failed. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}",
                                 response.StatusCode, response.ReasonPhrase, errorContent);
                // Try to parse error response if possible, otherwise return a generic error
                 try
                 {
                     var errorResponse = JsonSerializer.Deserialize<AdapterResponse>(errorContent, _jsonOptions);
                     if (errorResponse != null) return errorResponse;
                 }
                 catch (JsonException) { /* Ignore if error content is not a valid AdapterResponse */ }

                return new AdapterResponse($"API request failed with status {response.StatusCode}.", IsError: true, ErrorMessage: $"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception during interaction. Ensure API service is running at {BaseAddress}.", _httpClient.BaseAddress);
            return new AdapterResponse("Failed to connect to the API service.", IsError: true, ErrorMessage: ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize request or deserialize response JSON during interaction.");
            return new AdapterResponse("Error processing JSON data.", IsError: true, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during interaction request.");
            return new AdapterResponse("An unexpected error occurred.", IsError: true, ErrorMessage: ex.Message);
        }
    }
}
