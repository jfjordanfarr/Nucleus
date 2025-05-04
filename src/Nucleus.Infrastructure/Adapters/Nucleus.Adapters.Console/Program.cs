using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Nucleus.Infrastructure.Adapters.Console.Services;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Hosting; 
using System.CommandLine.Builder; 
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing; 
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ServiceDiscovery; 
using Microsoft.Extensions.ServiceDiscovery.Http; 
using System.Text.RegularExpressions; // Added for regex parsing

/// <summary>
/// Main entry point for the Nucleus Console Client application.
/// Configures services, command-line parsing (using System.CommandLine), and command handlers.
/// </summary>
/// <seealso cref="Docs/Architecture/05_ARCHITECTURE_CLIENTS.md"/>
public class NucleusConsoleAdapter
{
    // --- Application Entry Point ---
    public static async Task<int> Main(string[] args)
    {
        // 1. Build the host manually first
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();
        
        // 2. Get the service provider from the host
        var serviceProvider = host.Services;

        // 3. Pass the provider to BuildRootCommand
        var rootCommand = BuildRootCommand(serviceProvider); 

        var commandLineBuilder = new CommandLineBuilder(rootCommand);

        commandLineBuilder.UseDefaults(); 

        // 4. Remove the UseHost call - we built the host manually
        // commandLineBuilder.UseHost(Host.CreateDefaultBuilder, 
        //     hostBuilder =>
        //     {
        //         hostBuilder.ConfigureServices(ConfigureServices);
        //     });

        // Build the parser from the configured builder
        var parser = commandLineBuilder.Build();

        // --- MODIFICATION: Default to interactive mode if no args --- 
        ArgumentNullException.ThrowIfNull(args); // Add null check here
        if (args.Length == 0)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<NucleusConsoleAdapter>>();
            logger.LogInformation("No command specified, starting interactive mode.");
            var apiAgent = serviceProvider.GetRequiredService<NucleusApiServiceAgent>();
            await HandleInteractiveAsync(logger, apiAgent);
            return 0; // Exit cleanly after interactive mode finishes
        }
        // --- END MODIFICATION ---

        // Invoke the command line application via the parser
        // Note: The host lifecycle isn't automatically managed by System.CommandLine now.
        // We might need to start/stop the host explicitly if needed, but for simple DI resolution it might not be necessary.
        return await parser.InvokeAsync(args);
    }

    // --- Service Configuration --- (Keep as is)
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Explicitly add logging services
        services.AddLogging(); 

        // Add core service discovery services
        services.AddServiceDiscovery();

        services.AddHttpClient<NucleusApiServiceAgent>(client =>
        {
            // Set the BaseAddress to the direct URL of the API service
            client.BaseAddress = new Uri("https://localhost:19110"); 
        });

        // REMOVED: No longer registering ConsoleArtifactProvider
        // services.AddSingleton<IArtifactProvider, ConsoleArtifactProvider>();
    }

    // --- Command Configuration --- 
    /// <summary>
    /// Configures the command-line structure using System.CommandLine.
    /// Defines 'ingest' and 'interactive' commands and their handlers.
    /// </summary>
    /// <seealso cref="Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md"/>
    private static RootCommand BuildRootCommand(IServiceProvider serviceProvider) 
    {
        var rootCommand = new RootCommand("Nucleus Console Interface - Interact with the Nucleus engine.");

        // --- Ingest Command --- 
        var fileArgument = new Argument<FileInfo>("file", "The path to the file to ingest.") { Arity = ArgumentArity.ExactlyOne }; 
        fileArgument.ExistingOnly();
        var ingestCommand = new Command("ingest", "Ingest a file into the Nucleus knowledge base.") { fileArgument };

        // Revert to basic SetHandler with manual resolution inside
        ingestCommand.SetHandler(async (InvocationContext context) => 
        {
            // Manually get arguments
            var fileInfo = context.ParseResult.GetValueForArgument(fileArgument)!;
            
            // Resolve services using the passed-in provider
            using var scope = serviceProvider.CreateScope(); // Optional: Use scope if services are scoped
            var provider = scope.ServiceProvider; // Use scoped provider if scope is created
            // var provider = serviceProvider; // Use direct provider if services are singleton/transient

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<NucleusConsoleAdapter>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();
            // REMOVED: var artifactProvider = provider.GetRequiredService<IArtifactProvider>();
            
            // Call the static handler method, passing the resolved dependencies
            await HandleIngestAsync(fileInfo, logger, apiAgent);
            
            // Potentially set ExitCode based on handler outcome if needed
            // context.ExitCode = ...; 
        });
        rootCommand.AddCommand(ingestCommand);

        // --- Interactive Command --- 
        var interactiveCommand = new Command("interactive", "Start an interactive query session with the Nucleus engine.");
        interactiveCommand.SetHandler(async (InvocationContext context) =>
        {
            // Resolve services using the passed-in provider
            using var scope = serviceProvider.CreateScope(); // Optional: Use scope if services are scoped
            var provider = scope.ServiceProvider; // Use scoped provider if scope is created
            // var provider = serviceProvider; // Use direct provider if services are singleton/transient

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<NucleusConsoleAdapter>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();

            // Call the static handler method, passing the resolved dependencies
            await HandleInteractiveAsync(logger, apiAgent);
        });
        rootCommand.AddCommand(interactiveCommand);

        return rootCommand;
    }

    // --- Static Handler Methods ---

    /// <summary>
    /// Handles the 'ingest' command logic.
    /// Constructs an AdapterRequest for ingestion (sending only the file path) and sends it via the NucleusApiServiceAgent.
    /// </summary>
    /// <seealso cref="Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md"/> 
    /// <seealso cref="Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md"/> 
    /// <seealso cref="Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md"/> 
    /// <seealso cref="Docs/Architecture/Api/ARCHITECTURE_API_INGESTION.md"/> 
    private static async Task HandleIngestAsync(FileInfo fileInfo, ILogger<NucleusConsoleAdapter> logger, NucleusApiServiceAgent apiAgent)
    {
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Executing 'ingest' command for file: {FilePath} with SessionId: {SessionId}", fileInfo.FullName, sessionId);

        // Prepare ArtifactReference using the provider
        var artifactReference = new ArtifactReference(
            ReferenceId: fileInfo.FullName,
            ReferenceType: "file", // Use the type defined in ConsoleArtifactProvider
            SourceUri: new Uri(fileInfo.FullName, UriKind.Absolute),
            TenantId: "DefaultTenant", // Placeholder for console context
            FileName: fileInfo.Name
            // MimeType can remain null
            );
        var artifactList = new List<ArtifactReference> { artifactReference };

        // Prepare AdapterRequest
        var request = new AdapterRequest(
            PlatformType: "Console",
            ConversationId: sessionId, 
            UserId: Environment.UserName,
            QueryText: $"Ingest file: {fileInfo.Name}", 
            ArtifactReferences: artifactList
        );

        // Send request
        logger.LogInformation("Sending ingest request to API...");
        var response = await apiAgent.SendInteractionAsync(request);

        // Handle response
        if (response?.Success == true)
        {
            logger.LogInformation("Ingestion successful. Response: {ResponseMessage}", response.ResponseMessage);
            Console.WriteLine($"Successfully ingested file. Response: {response.ResponseMessage}");
        }
        else
        {
            logger.LogError("Ingestion interaction failed. Error: {ErrorMessage}", response?.ErrorMessage ?? "Unknown error");
            Console.Error.WriteLine($"Error ingesting file: {response?.ErrorMessage ?? "Unknown error"}");
            // Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Handles the 'interactive' command logic.
    /// Runs a loop to get user input, parse it, construct AdapterRequests (potentially with ArtifactReferences based on file paths),
    /// and sends them via the NucleusApiServiceAgent.
    /// </summary>
    /// <seealso cref="Docs/Architecture/Api/ARCHITECTURE_API_CLIENT_INTERACTION.md"/> 
    /// <seealso cref="Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTER_INTERFACES.md"/> 
    /// <seealso cref="Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_CONSOLE.md"/> 
    private static async Task HandleInteractiveAsync(ILogger<NucleusConsoleAdapter> logger, NucleusApiServiceAgent apiAgent)
    {
        logger.LogInformation("Nucleus Console Adapter started. Type 'exit' to quit.");
        logger.LogInformation("You can include local file paths in backticks (e.g., analyze `C:\\path\\file.txt`)");

        string? sessionId = null; // Initialize sessionId outside the loop
        var pathRegex = new Regex(@"`([^`]+)`"); // Keep regex outside loop

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("You: ");
            Console.ResetColor();
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Exiting interactive mode.");
                break; // Exit the loop
            }

            // Initialize session ID on the first valid input
            sessionId ??= Guid.NewGuid().ToString();

            // --- Parsing Logic (Moved inside loop) ---
            List<ArtifactReference>? artifactList = null;
            string queryText = input; // Default query text is the full input

            var matches = pathRegex.Matches(input);
            if (matches.Count > 0)
            {
                artifactList = new List<ArtifactReference>();
                var queryWithoutPaths = input;

                foreach (Match match in matches.Cast<Match>())
                {
                    string fullMatch = match.Value; // e.g., `path/to/file.txt`
                    string filePath = match.Groups[1].Value.Trim(); // e.g., path/to/file.txt

                    // Basic validation (check if it looks like a path)
                    if (!string.IsNullOrWhiteSpace(filePath) && (filePath.Contains('/') || filePath.Contains('\\')))
                    {
                        // Check if file exists before adding
                        if (File.Exists(filePath))
                        {
                            logger.LogInformation("Found valid file reference: {FilePath}", filePath);
                            artifactList.Add(new ArtifactReference(
                                ReferenceId: filePath,
                                ReferenceType: "file", // Use the type defined in ConsoleArtifactProvider
                                SourceUri: new Uri(filePath, UriKind.Absolute),
                                TenantId: "DefaultTenant", // Placeholder for console context
                                FileName: Path.GetFileName(filePath)
                                // MimeType can remain null
                                ));
                            // Remove the matched backtick expression from the query
                            queryWithoutPaths = queryWithoutPaths.Replace(fullMatch, "");
                        }
                        else
                        {
                            logger.LogWarning("File reference found but file does not exist: {FilePath}", filePath);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Ignoring potential path reference that doesn't seem valid: {FilePath}", filePath);
                    }
                }
                // Clean up extra whitespace in query text after removing paths
                queryText = queryWithoutPaths.Trim();
                if (artifactList.Count == 0) artifactList = null; // Set back to null if no valid files found
            }
            // --- End Parsing Logic ---

            try
            {
                // Check if there's query text or artifact references
                if (string.IsNullOrWhiteSpace(queryText) && (artifactList == null || artifactList.Count == 0))
                {
                    logger.LogWarning("Input resulted in empty query and no valid file references. Please provide query text or valid context files (`path/to/file`).");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: No query text or valid file references provided.");
                    Console.ResetColor();
                    continue; // Skip to next iteration of the loop
                }

                // Prepare AdapterRequest
                var request = new AdapterRequest(
                    PlatformType: "Console",
                    ConversationId: sessionId, // Use the persistent sessionId
                    UserId: Environment.UserName,
                    QueryText: queryText, // Send the cleaned query text
                    ArtifactReferences: artifactList // Send parsed references (can be null)
                );

                logger.LogInformation("Sending interactive request to API (SessionId: {SessionId})...", sessionId);
                var response = await apiAgent.SendInteractionAsync(request);

                if (response != null && response.Success)
                {
                    logger.LogInformation("API Response: {ResponseMessage}", response.ResponseMessage);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"API: {response.ResponseMessage}");
                    Console.ResetColor();
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from API.";
                    logger.LogError("API call failed: {ErrorMessage}", errorMessage);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {errorMessage}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the interactive operation.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        } // End while(true)
    }
}
