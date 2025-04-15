// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Processing; // Needed for AddProcessingServices
using Nucleus.Processing.Services; // Needed for DatavizHtmlBuilder
using System;
using System.IO;
using System.Threading.Tasks;

// Set up the host builder for dependency injection, logging, configuration
var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // Register Logging (optional, but good practice)
    services.AddLogging(configure => configure.AddConsole());

    // Register services from Nucleus.Processing
    services.AddProcessingServices();

    // TODO: Register services from Nucleus.Adapters.Console when available
    // services.AddConsoleAdapterServices();

    // TODO: Register the main console application logic service/hosted service
    // services.AddHostedService<ConsoleAppWorker>();

    // Note: AddProcessingServices already registers DatavizHtmlBuilder as Transient
    // No need to register it again here unless a different lifetime is needed.

});

// Build the host
var host = builder.Build();

Console.WriteLine("Host built. Nucleus.Console starting...");

// In a real app, you'd run the host which starts Hosted Services.
// await host.RunAsync();

// For initial testing, let's resolve the service and call it directly
// This bypasses the typical Hosted Service pattern for now.
await RunTestLogicAsync(host.Services);

Console.WriteLine("Nucleus.Console finished.");

// --- Test Logic --- 
static async Task RunTestLogicAsync(IServiceProvider services)
{
    using (var serviceScope = services.CreateScope())
    {
        var provider = serviceScope.ServiceProvider;
        var logger = provider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Resolving DatavizHtmlBuilder...");
            var datavizBuilder = provider.GetRequiredService<DatavizHtmlBuilder>();
            logger.LogInformation("Successfully resolved DatavizHtmlBuilder.");

            // --- Example Usage (replace with actual call later) ---
            logger.LogInformation("Simulating Dataviz generation request...");
            // Example Python using plotly (ensure template matches)
            string fakePython = @"
import plotly.express as px
import pandas as pd
import json

# Assuming input_data_json is preloaded dictionary
df = pd.DataFrame(input_data_json)

fig = px.scatter(df, x='x_col', y='y_col', title='Sample Scatter Plot')

# Assign to the expected output variable
plotly_figure = fig
";
            // Example JSON matching the Python
            string fakeJson = "{\"x_col\": [1, 2, 3, 4, 5], \"y_col\": [10, 11, 12, 13, 14]}";

            string? generatedHtml = await datavizBuilder.BuildVisualizationHtmlAsync(fakePython, fakeJson);

            if (!string.IsNullOrEmpty(generatedHtml))
            {
                logger.LogInformation($"Successfully generated Dataviz HTML ({generatedHtml.Length} bytes).");
                // In a real scenario, the Console Adapter would handle saving/displaying this.
                // For testing, let's save it to a temp file.
                var tempPath = Path.Combine(Path.GetTempPath(), $"nucleus_dataviz_{DateTime.Now:yyyyMMddHHmmss}.html");
                await File.WriteAllTextAsync(tempPath, generatedHtml);
                logger.LogInformation($"Saved generated HTML to: {tempPath}");
                Console.WriteLine($"---> Output HTML saved to: {tempPath}"); // Make it visible
            }
            else
            {
                logger.LogError("Failed to generate Dataviz HTML.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            logger.LogError(ex, "Error during console test execution.");
        }
    }
}

// Define a simple class for the logger category name
public partial class Program { }
