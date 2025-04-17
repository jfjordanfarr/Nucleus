// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Needed for GetSection
using System.Net.Http; // Needed for HttpClient
using Nucleus.Processing; // Needed for AddProcessingServices
using Nucleus.Processing.Services; // Needed for DatavizHtmlBuilder
using Nucleus.Console.Services; // For NucleusApiClient
using Nucleus.Console.Contracts; // Needed for Contracts
using System;
using System.IO;
using System.Threading.Tasks;
using System.CommandLine; // For command line parsing
using System.CommandLine.Invocation; // For InvocationContext

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

    // --- HttpClient Setup using Aspire Service Discovery ---
    // Register IHttpClientFactory and configure a typed HttpClient for the Nucleus API.
    // Aspire's AddServiceDiscovery will automatically resolve the BaseAddress for the "nucleusapi" service.
    services.AddHttpClient<NucleusApiClient>() // Use typed client
        .AddServiceDiscovery(); // <-- Let Aspire handle the BaseAddress

    // Register the API client service
    services.AddTransient<NucleusApiClient>();
});

// Build the host
var host = builder.Build();

Console.WriteLine("Host built. Nucleus.Console starting...");

// --- Command Line Setup ---
var rootCommand = new RootCommand("Nucleus Console client for interacting with the Nucleus API.");

// --- Define 'ingest' command --- 
var fileOption = new Option<FileInfo>(
    name: "--file-path",
    description: "The absolute path to the file to ingest.")
{ 
    IsRequired = true // Make the file path mandatory
};
fileOption.AddAlias("-f");

var ingestCommand = new Command("ingest", "Ingest a local file into the Nucleus API for context.");
ingestCommand.AddOption(fileOption);

ingestCommand.SetHandler(async (InvocationContext context) => 
{
    var fileInfo = context.ParseResult.GetValueForOption(fileOption);
    var serviceProvider = context.BindingContext.GetService(typeof(IServiceProvider)) as IServiceProvider;
    var logger = serviceProvider?.GetRequiredService<ILogger<Program>>();
    var apiClient = serviceProvider?.GetRequiredService<NucleusApiClient>();

    if (fileInfo == null || !fileInfo.Exists)
    {
        logger?.LogError("File not found or path invalid: {FilePath}", fileInfo?.FullName ?? "<null>");
        context.ExitCode = 1; // Indicate error
        return;
    }

    if (apiClient == null || logger == null)
    {
        Console.WriteLine("Error: Could not resolve required services (Logger or ApiClient).");
        context.ExitCode = 1;
        return;
    }

    logger.LogInformation("Attempting to ingest file: {FilePath}", fileInfo.FullName);
    bool success = await apiClient.IngestFileAsync(fileInfo.FullName);
    if (success)
    {
        logger.LogInformation("Ingestion request sent successfully for: {FilePath}", fileInfo.FullName);
        context.ExitCode = 0;
    }
    else
    {
        logger.LogError("Ingestion request failed for: {FilePath}", fileInfo.FullName);
        context.ExitCode = 1;
    }
});

rootCommand.AddCommand(ingestCommand);

// --- Define 'query' command ---
var queryTextOption = new Option<string>(
    name: "--query-text",
    description: "The text of the query to send to the API.")
{ 
    IsRequired = true 
};
queryTextOption.AddAlias("-q");

var contextIdOption = new Option<string?>(
    name: "--context-id",
    description: "Optional identifier (e.g., file path used in ingest) for context.",
    getDefaultValue: () => null);
contextIdOption.AddAlias("-c");

var queryCommand = new Command("query", "Send a query to the Nucleus API, optionally using ingested context.");
queryCommand.AddOption(queryTextOption);
queryCommand.AddOption(contextIdOption);

queryCommand.SetHandler(async (InvocationContext context) => 
{
    var queryText = context.ParseResult.GetValueForOption(queryTextOption);
    var contextId = context.ParseResult.GetValueForOption(contextIdOption);
    var serviceProvider = context.BindingContext.GetService(typeof(IServiceProvider)) as IServiceProvider;
    var logger = serviceProvider?.GetRequiredService<ILogger<Program>>();
    var apiClient = serviceProvider?.GetRequiredService<NucleusApiClient>();

    if (string.IsNullOrWhiteSpace(queryText))
    {
        logger?.LogError("Query text cannot be empty.");
        context.ExitCode = 1;
        return;
    }

    if (apiClient == null || logger == null)
    {
        Console.WriteLine("Error: Could not resolve required services (Logger or ApiClient).");
        context.ExitCode = 1;
        return;
    }

    logger.LogInformation("Attempting to send query: '{Query}' with ContextId: '{ContextId}'", queryText, contextId ?? "None");

    // Construct the request using the console's contract
    var request = new QueryRequest(queryText, null, contextId); // Using null for UserId for now

    var response = await apiClient.QueryAsync(request);

    if (response != null)
    {
        logger.LogInformation("Query request successful.");
        Console.WriteLine("\n--- API Response ---");
        Console.WriteLine(response.ResponseText);
        if (response.SourceReferences != null && response.SourceReferences.Count > 0)
        {
            Console.WriteLine("\nSources:");
            foreach (var source in response.SourceReferences)
            {
                Console.WriteLine($"- {source}");
            }
        }
        Console.WriteLine("--------------------");
        context.ExitCode = 0;
    }
    else
    {
        logger.LogError("Query request failed or returned no response.");
        context.ExitCode = 1;
    }
});

rootCommand.AddCommand(queryCommand);

Console.WriteLine("Invoking command line processor...");
// Pass services from the host to the command handlers via binding or manual resolution
// System.CommandLine recommends binding, but direct resolution is simpler for this stage.

// Pass args from the entry point
return await rootCommand.InvokeAsync(args);

Console.WriteLine("Nucleus.Console finished.");

// Define a simple class for the logger category name
public partial class Program { }
