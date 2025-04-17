using System;
using System.Net.Http;
using System.Net.Http.Json; // Requires Microsoft.Extensions.Http
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Console.Contracts;

namespace Nucleus.Console.Services;

/// <summary>
/// Client for interacting with the Nucleus API service.
/// Uses HttpClient configured via DI with Aspire service discovery.
/// </summary>
public class NucleusApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NucleusApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web); // Use web defaults

    // Inject HttpClient directly thanks to typed client registration in Program.cs
    public NucleusApiClient(HttpClient httpClient, ILogger<NucleusApiClient> logger)
    {
        _httpClient = httpClient; // HttpClient is pre-configured with BaseAddress by Aspire
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a request to the API to ingest a local file.
    /// </summary>
    /// <param name="filePath">The absolute path of the file to ingest.</param>
    /// <returns>True if the request was successful (HTTP 2xx), false otherwise.</returns>
    public async Task<bool> IngestFileAsync(string filePath)
    {
        var request = new IngestLocalFileRequest(filePath);

        try
        {
            _logger.LogInformation("Sending ingestion request for file: {FilePath}", filePath);
            using var response = await _httpClient.PostAsJsonAsync("/api/ingestion/localfile", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ingestion request successful (Status: {StatusCode}).", response.StatusCode);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ingestion request failed. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}", 
                                 response.StatusCode, response.ReasonPhrase, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception during ingestion for {FilePath}. Ensure API service is running at {BaseAddress}.", 
                             filePath, _httpClient.BaseAddress);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during ingestion request for {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Sends a query request to the API.
    /// </summary>
    /// <param name="query">The query request details.</param>
    /// <returns>The QueryResponse from the API, or null if the request failed.</returns>
    public async Task<QueryResponse?> QueryAsync(QueryRequest query)
    {
        try
        {
            _logger.LogInformation("Sending query request: '{QueryText}', ContextId: '{ContextId}'", 
                                 query.QueryText, query.ContextSourceIdentifier ?? "None");
            using var response = await _httpClient.PostAsJsonAsync("/api/query", query, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Query request successful (Status: {StatusCode}). Attempting to deserialize response...", response.StatusCode);
                var queryResponse = await response.Content.ReadFromJsonAsync<QueryResponse>(_jsonOptions);
                if (queryResponse != null)
                {
                    _logger.LogInformation("Query response deserialized successfully.");
                    return queryResponse;
                }
                else
                {
                     _logger.LogWarning("Query request succeeded, but response body was empty or could not be deserialized.");
                     return null;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Query request failed. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}", 
                                 response.StatusCode, response.ReasonPhrase, errorContent);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception during query. Ensure API service is running at {BaseAddress}.", _httpClient.BaseAddress);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize query response JSON.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during query request.");
            return null;
        }
    }
}
