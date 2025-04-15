using System;
using System.IO;
using System.Reflection; // Added for Assembly
using System.Text;
using System.Text.Json; // For potential JSON escaping if needed later
using System.Text.Encodings.Web; // For JavaScript escaping
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net; // Added for WebUtility

namespace Nucleus.Processing.Services;

/// <summary>
/// Service responsible for building the self-contained HTML for Pyodide-based data visualizations.
/// Reads template files and injects Persona-provided Python code and JSON data.
/// </summary>
/// <seealso cref="../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md"/>
/// <seealso cref="../../Docs/Architecture/Processing/Dataviz/ARCHITECTURE_DATAVIZ_TEMPLATE.md"/>
public class DatavizHtmlBuilder
{
    private readonly ILogger<DatavizHtmlBuilder> _logger;
    private readonly string _basePath; // Field to store calculated base path

    // Properties to get full paths dynamically
    private string HtmlTemplatePath => Path.Combine(_basePath, "dataviz_template.html");
    private string CssPath => Path.Combine(_basePath, "dataviz_styles.css");
    private string MainScriptPath => Path.Combine(_basePath, "dataviz_script.js");
    private string PythonScriptTemplatePath => Path.Combine(_basePath, "dataviz_plotly_script.py");
    private string WorkerScriptPath => Path.Combine(_basePath, "dataviz_worker.js");

    public DatavizHtmlBuilder(ILogger<DatavizHtmlBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Calculate base path relative to the executing assembly of this class
        var assemblyLocation = Path.GetDirectoryName(typeof(DatavizHtmlBuilder).Assembly.Location);
        if (string.IsNullOrEmpty(assemblyLocation))
        {
            _logger.LogError("Could not determine assembly location for DatavizHtmlBuilder resources.");
            throw new InvalidOperationException("Could not determine assembly location for DatavizHtmlBuilder resources.");
        }
        _basePath = Path.Combine(assemblyLocation, "Resources", "Dataviz");
        _logger.LogInformation("Dataviz resource base path resolved to: {BasePath}", _basePath);

        // Optional: Check if the directory exists early
        if (!Directory.Exists(_basePath))
        {
             _logger.LogWarning("Dataviz resource directory does not exist at the expected path: {BasePath}. Build might have failed to copy resources.", _basePath);
             // Depending on strictness, could throw here.
        }
    }

    /// <summary>
    /// Assembles a self-contained HTML data visualization artifact.
    /// </summary>
    /// <param name="personaPythonScript">The specific Python code snippet provided by the Persona.</param>
    /// <param name="jsonData">The JSON data string required by the Python script.</param>
    /// <returns>A string containing the complete HTML artifact, or null if an error occurred.</returns>
    /// <seealso cref="../../Docs/Architecture/Processing/ARCHITECTURE_PROCESSING_DATAVIZ.md"/>
    /// <seealso cref="../../Docs/Architecture/Processing/Dataviz/ARCHITECTURE_DATAVIZ_TEMPLATE.md"/>
    public async Task<string?> BuildVisualizationHtmlAsync(string personaPythonScript, string jsonData)
    {
        _logger.LogInformation("Attempting to build Dataviz HTML artifact.");
        try
        {
            // 1. Read all template files asynchronously (using the properties)
            var htmlTemplateTask = File.ReadAllTextAsync(HtmlTemplatePath);
            var cssContentTask = File.ReadAllTextAsync(CssPath);
            var mainScriptContentTask = File.ReadAllTextAsync(MainScriptPath);
            var pythonScriptTemplateTask = File.ReadAllTextAsync(PythonScriptTemplatePath); // For view modal
            var workerScriptContentTask = File.ReadAllTextAsync(WorkerScriptPath);

            await Task.WhenAll(htmlTemplateTask, cssContentTask, mainScriptContentTask, pythonScriptTemplateTask, workerScriptContentTask);

            var htmlTemplate = await htmlTemplateTask;
            var cssContent = await cssContentTask; 
            var mainScriptContent = await mainScriptContentTask;
            var pythonScriptTemplate = await pythonScriptTemplateTask;
            var workerScriptContent = await workerScriptContentTask;

            // Ensure JSON is not null or empty; default if necessary
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                _logger.LogWarning("Input JSON data is null or empty. Defaulting to empty object {{}}.");
                jsonData = "{}"; // Default to empty object if empty/null
            }

            _logger.LogDebug("Preparing final HTML artifact.");

            // 5. Perform replacements in the HTML Template using correct {{PLACEHOLDER}} names
            var finalHtmlBuilder = new StringBuilder(htmlTemplate);
            // NOTE: A rule in dataviz_styles.css was previously conflicting with plot visibility.
            // If the plot disappears again, debug dataviz_styles.css to find the conflict.
            finalHtmlBuilder.Replace("<!-- {{STYLES_PLACEHOLDER}} -->", $"<style>\n{cssContent}\n</style>");
            // Inject the ORIGINAL main script content
            finalHtmlBuilder.Replace("{{MAIN_SCRIPT}}", mainScriptContent); // Use original script
            // Encode content before injection to handle special HTML characters safely
            var encodedPythonScript = WebUtility.HtmlEncode(pythonScriptTemplate);
            var encodedInputJson = WebUtility.HtmlEncode(jsonData);
            var encodedWorkerScript = WebUtility.HtmlEncode(workerScriptContent);

            // Inject RAW python script and JSON into their respective template tags
            finalHtmlBuilder.Replace("{{PYTHON_SCRIPT}}", encodedPythonScript); // Inject template for modals/plain script tags
            finalHtmlBuilder.Replace("{{WORKER_SCRIPT}}", encodedWorkerScript);        // Inject worker script template
            finalHtmlBuilder.Replace("{{JSON_DATA}}", encodedInputJson);                       // Inject raw JSON for data script tag/modal

            var finalHtml = finalHtmlBuilder.ToString();

            _logger.LogInformation("Successfully built Dataviz HTML artifact.");
            return finalHtml;
        }
        catch (FileNotFoundException fnfEx)
        {
            _logger.LogError(fnfEx, "Dataviz template file not found: {FilePath}. Base directory: {BaseDir}", fnfEx.FileName, AppContext.BaseDirectory);
            return null;
        }
        catch (IOException ioEx)
        {
            _logger.LogError(ioEx, "Error reading dataviz template file.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while building the dataviz HTML.");
            return null;
        }
    }
}
