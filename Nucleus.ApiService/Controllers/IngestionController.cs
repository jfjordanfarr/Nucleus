using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Personas.Core;
using Nucleus.ApiService.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Nucleus.ApiService.Controllers;

/// <summary>
/// Controller responsible for initiating content ingestion processes.
/// Handles receiving requests to ingest data (like local files) and passing them
/// to the appropriate persona for ephemeral analysis or caching.
/// See: [Architecture: AI Integration - Ephemeral Context](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#23-ephemeral-context-via-api-local-files)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly ILogger<IngestionController> _logger;
    private readonly IPersona<EmptyAnalysisData> _persona; // Inject the registered interface

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="persona">The persona instance responsible for content analysis.</param>
    public IngestionController(ILogger<IngestionController> logger, IPersona<EmptyAnalysisData> persona)
    {
        _logger = logger;
        _persona = persona;
    }

    /// <summary>
    /// Initiates the ingestion and ephemeral processing of a local file.
    /// Reads the file content and passes it to <see cref="IPersona{TAnalysisData}.AnalyzeEphemeralContentAsync"/> for caching.
    /// See: [Architecture: AI Integration - Ingestion Endpoint](~/../../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md#1-ingestion-endpoint-post-apiingestionlocalfile)
    /// </summary>
    /// <param name="request">The request containing the path to the local file.</param>
    /// <returns>An IActionResult indicating the acceptance of the request.</returns>
    /// <seealso cref="IngestLocalFileRequest"/>
    [HttpPost("localfile")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestLocalFileAsync([FromBody] IngestLocalFileRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FilePath))
        {
            _logger.LogWarning("IngestLocalFileAsync received invalid request model.");
            return BadRequest("Request body must contain a valid FilePath.");
        }

        _logger.LogInformation("Received request to ingest local file: {FilePath}", request.FilePath);

        try
        {
            if (!System.IO.File.Exists(request.FilePath))
            {
                _logger.LogWarning("File not found at path: {FilePath}", request.FilePath);
                return NotFound($"File not found: {request.FilePath}");
            }

            // --- Plaintext Processor Simulation (MVP) ---
            // Read the file content. In a full implementation, this might involve
            // routing to specific processors (PDF, Office, etc.) which ultimately
            // produce ephemeral Markdown.
            _logger.LogDebug("Reading content from file: {FilePath}", request.FilePath);
            string ephemeralContent = await System.IO.File.ReadAllTextAsync(request.FilePath);
            _logger.LogDebug("Read {ContentLength} characters from file.", ephemeralContent.Length);

            // --- Pass Ephemeral Content to Persona for Analysis ---
            _logger.LogInformation("Passing ephemeral content from {FilePath} to BootstrapperPersona for analysis.", request.FilePath);

            // Call the persona to analyze/cache the ephemeral content.
            // The cancellation token is not strictly needed for the simple cache implementation,
            // but included for future-proofing.
            await _persona.AnalyzeEphemeralContentAsync(ephemeralContent, request.FilePath, HttpContext.RequestAborted);

            return Accepted(); // Indicate the request is accepted for processing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing local file ingestion for {FilePath}", request.FilePath);
            return StatusCode(500, "An unexpected error occurred during file ingestion.");
        }
    }
}
