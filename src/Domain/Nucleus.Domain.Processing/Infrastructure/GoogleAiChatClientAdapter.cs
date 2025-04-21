using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mscc.GenerativeAI; // Using the core Mscc.GenerativeAI namespace
using Nucleus.Abstractions.Models.Configuration; // Using the class from Abstractions
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Domain.Processing.Infrastructure;

/// <summary>
/// Adapter for Google AI Gemini using the Mscc.GenerativeAI library with API Key authentication.
/// Implements basic chat completion functionality.
/// See: Docs/Architecture/Processing/Orchestration/ARCHITECTURE_ORCHESTRATION_LLM_INTEGRATION.md
/// </summary>
public class GoogleAiChatClientAdapter
{
    private readonly ILogger<GoogleAiChatClientAdapter> _logger;
    private readonly GoogleAiOptions _options;
    private readonly string _modelName;

    public GoogleAiChatClientAdapter(
        ILogger<GoogleAiChatClientAdapter> logger,
        IOptions<GoogleAiOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _modelName = !string.IsNullOrWhiteSpace(_options.ModelId) ? _options.ModelId : Model.GeminiPro; // Use specified or default

        // Validate API Key is present
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ArgumentNullException(nameof(_options.ApiKey), "Google AI API Key cannot be null or empty.");
        }
        _logger.LogInformation("GoogleAiChatClientAdapter initialized with Model: {ModelName}", _modelName);
        _logger.LogDebug("Attempting to use Google AI API Key with length: {ApiKeyLength}", _options.ApiKey.Length);
    }

    /// <summary>
    /// Sends a prompt to the Google AI Gemini model and returns the generated response as a string.
    /// Uses a simple GenerateContentAsync call for stateless completion.
    /// </summary>
    public async Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt); // Added null check for prompt (CA1062)

        _logger.LogDebug("GetCompletionAsync called with prompt ({Length} chars) for model {ModelName}.", prompt.Length, _modelName);

        try
        {
            // Validate API Key is present
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "Error: Google AI API Key is missing.";
            }

            // Instantiate GoogleAI service first
            var googleAI = new GoogleAI(apiKey: _options.ApiKey);
            // Get the GenerativeModel from the service
            var model = googleAI.GenerativeModel(model: _modelName);

            // Start a chat session (stateless for this adapter's purpose)
            var chat = model.StartChat(); 
            _logger.LogInformation("Sending SendMessage request via chat session to Google AI model: {ModelName}", _modelName);

            // Send the prompt via the chat session
            var response = await chat.SendMessage(prompt, cancellationToken: cancellationToken);
            _logger.LogInformation("Received SendMessage response from Google AI chat session.");

            // Extract the response text
            // Ensure response and necessary nested properties are not null
            string responseText = response?.Text ?? response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(responseText))
            {
                _logger.LogWarning("Google AI response did not contain expected text content. Response: {@Response}", response); // Log entire response if text is missing
                return "Error: Could not parse Google AI response."; // Default error message
            }
            
            _logger.LogDebug("Extracted response text ({Length} chars).", responseText.Length);
            return responseText;

        }
        catch (Exception ex) // Catch broader exceptions initially, refine if needed
        {
            _logger.LogError(ex, "Error communicating with Google AI (Mscc.GenerativeAI) for model {ModelName}: {ErrorMessage}", _modelName, ex.Message);
            // Consider specific exception types from Mscc.GenerativeAI if available
            // Re-throw or return a specific error message
            return $"Error: Failed to get completion from Google AI. Details: {ex.Message}";
        }
    }

    // No Dispose method needed unless IGenerativeAI requires disposal,
    // which is unlikely for a service registered as Singleton/Scoped.
}
