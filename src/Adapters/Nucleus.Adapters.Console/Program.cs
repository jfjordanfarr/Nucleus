using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Nucleus.Console.Services;
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

public class Program
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
        if (args.Length == 0)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
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
            // Set the BaseAddress to the service name URI scheme
            // Service discovery will resolve 'nucleusapi', trying HTTPS first then HTTP.
            client.BaseAddress = new Uri("https+http://nucleusapi"); 
        })
        .AddServiceDiscovery(); // Add service discovery capabilities

        services.AddSingleton<IArtifactProvider, ConsoleArtifactProvider>();
    }

    // --- Command Configuration --- 
    // Modify BuildRootCommand to accept IServiceProvider
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
            var logger = loggerFactory.CreateLogger<Program>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();
            var artifactProvider = provider.GetRequiredService<IArtifactProvider>();
            
            // Call the static handler method, passing the resolved dependencies
            await HandleIngestAsync(fileInfo, logger, apiAgent, artifactProvider);
            
            // Potentially set ExitCode based on handler outcome if needed
            // context.ExitCode = ...; 
        });
        rootCommand.AddCommand(ingestCommand);

        // --- Query Command --- 
        var queryArgument = new Argument<string>("query", "The query text to send.") { Arity = ArgumentArity.ExactlyOne }; 
        var contextOption = new Option<string?>(new[] { "--context", "-c" }, "Optional context identifier (e.g., file path, message ID)."); 
        var filesOption = new Option<FileInfo[]>(new[] { "--files", "-f" }, "Optional files to attach to the query."); 
        var queryCommand = new Command("query", "Send a query to the Nucleus engine.") { queryArgument, contextOption, filesOption };

        // Revert to basic SetHandler with manual resolution inside
        queryCommand.SetHandler(async (InvocationContext context) =>
        {
            // Manually get services and arguments/options
            var queryText = context.ParseResult.GetValueForArgument(queryArgument)!;
            var contextId = context.ParseResult.GetValueForOption(contextOption);
            var files = context.ParseResult.GetValueForOption(filesOption);
            
            // Resolve services using the passed-in provider
            using var scope = serviceProvider.CreateScope(); // Optional: Use scope if services are scoped
            var provider = scope.ServiceProvider; // Use scoped provider if scope is created
            // var provider = serviceProvider; // Use direct provider if services are singleton/transient

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();
            var artifactProvider = provider.GetRequiredService<IArtifactProvider>();

            // Call the static handler method, passing the resolved dependencies
            await HandleQueryAsync(context, logger, apiAgent, artifactProvider, queryText, files);

            // Potentially set ExitCode based on handler outcome if needed
            // context.ExitCode = ...;
        });
        rootCommand.AddCommand(queryCommand);

        // --- Interactive Command --- 
        var interactiveCommand = new Command("interactive", "Start an interactive query session with the Nucleus engine.");
        interactiveCommand.SetHandler(async (InvocationContext context) =>
        {
            // Resolve services using the passed-in provider
            using var scope = serviceProvider.CreateScope(); // Optional: Use scope if services are scoped
            var provider = scope.ServiceProvider; // Use scoped provider if scope is created
            // var provider = serviceProvider; // Use direct provider if services are singleton/transient

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();

            // Call the static handler method, passing the resolved dependencies
            await HandleInteractiveAsync(logger, apiAgent);
        });
        rootCommand.AddCommand(interactiveCommand);

        return rootCommand;
    }

    // --- Static Handler Methods ---

    private static async Task HandleIngestAsync(FileInfo fileInfo, ILogger<Program> logger, NucleusApiServiceAgent apiAgent, IArtifactProvider artifactProvider)
    {
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Executing 'ingest' command for file: {FilePath} with SessionId: {SessionId}", fileInfo.FullName, sessionId);

        // Ensure the artifact provider is the correct type
        if (artifactProvider is not ConsoleArtifactProvider consoleProvider)
        {
            logger.LogError("Configured artifact provider is not ConsoleArtifactProvider. Provider type: {ProviderType}", artifactProvider.GetType().Name);
            Console.Error.WriteLine("Error: Configured artifact provider is not ConsoleArtifactProvider.");
            // Set exit code appropriately if possible (CommandHandler doesn't expose InvocationContext directly here)
            // Environment.ExitCode = 1; 
            return;
        }

        // Prepare ArtifactReference using the provider
        var artifactReference = new ArtifactReference(PlatformType: "Console", ArtifactId: fileInfo.FullName);
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

    private static async Task HandleQueryAsync(InvocationContext context, ILogger<Program> logger, NucleusApiServiceAgent apiAgent, IArtifactProvider artifactProvider, string query, FileInfo[]? files)
    {
        logger.LogInformation("Handling query: '{Query}'", query);

        // Prepare Artifact References if files are provided
        List<ArtifactReference>? artifactList = null;
        if (files != null && files.Length > 0)
        {
            logger.LogInformation("Found {FileCount} file(s) attached to query.", files.Length);
            artifactList = new List<ArtifactReference>(); // Initialize the list
            foreach (var file in files)
            {
                if (file.Exists)
                {
                    logger.LogDebug("Creating artifact reference for: {FilePath}", file.FullName);
                    artifactList.Add(new ArtifactReference(
                        PlatformType: PlatformType.Console, // Corrected: Use enum value
                        ArtifactId: file.FullName // Corrected: Use full absolute path
                    ));
                }
                else
                {
                    logger.LogWarning("Provided file path does not exist, skipping: {FilePath}", file.FullName);
                }
            }
        }

        // Create Request
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Executing 'query' command. Query: '{QueryText}', SessionId: {SessionId}", 
                           query, sessionId);

        // Prepare AdapterRequest
        var request = new AdapterRequest(
            PlatformType: "Console",
            ConversationId: sessionId, 
            UserId: Environment.UserName,
            QueryText: query,
            ArtifactReferences: artifactList 
            // ReplyToMessageId could potentially be linked to contextId if it represents a previous message
        );

        // Send request
        logger.LogInformation("Sending query request to API...");
        var response = await apiAgent.SendInteractionAsync(request);

        // Handle response
        if (response?.Success == true)
        {
            logger.LogInformation("API request acknowledged. Response: {ResponseMessage}", response.ResponseMessage);
            Console.WriteLine($"API: {response.ResponseMessage}");

            // Handle generated artifact if present
            if (response.GeneratedArtifactReference != null && artifactProvider is ConsoleArtifactProvider consoleProvider)
            {
                try
                {
                    var filePath = response.GeneratedArtifactReference.ArtifactId;
                    Console.WriteLine($"Generated artifact saved to: {filePath}");
                    // Potentially open the file?
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process generated artifact reference.");
                    Console.Error.WriteLine($"Error processing generated artifact: {ex.Message}");
                }
            }
        }
        else
        {
            logger.LogError("Query interaction failed. Error: {ErrorMessage}", response?.ErrorMessage ?? "Unknown error");
            Console.Error.WriteLine($"Error executing query: {response?.ErrorMessage ?? "Unknown error"}");
            // Environment.ExitCode = 1;
        }
    }

    private static async Task HandleInteractiveAsync(ILogger<Program> logger, NucleusApiServiceAgent apiAgent)
    {
        logger.LogInformation("Nucleus Console Adapter started. Type 'exit' to quit.");
        logger.LogInformation("You can include local file paths in quotes (e.g., analyze \"C:\\path\\file.txt\")");

        // --- Interactive Loop ---
        string? sessionId = null; // Keep track of the session across multiple queries
        // Regex to find quoted strings, handling potential escaped quotes inside
        var quotedPathRegex = new Regex(@"""([^""\]*(?:\.[^""\]*)*)""", RegexOptions.Compiled);

        while (true)
        {
            Console.Write("> ");
            string? userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            // Use existing session ID or create a new one for the first query
            sessionId ??= Guid.NewGuid().ToString();

            // --- Parse Input for Query and Files ---
            string queryText = userInput;
            List<ArtifactReference>? artifactList = null;

            var matches = quotedPathRegex.Matches(userInput);
            if (matches.Count > 0)
            {
                artifactList = new List<ArtifactReference>();
                var processedPaths = new HashSet<string>(); // Avoid duplicates if path mentioned twice
                foreach (Match match in matches.Cast<Match>()) // Use Cast<Match> for type safety
                {
                    // Group 1 captures the content inside the quotes
                    string potentialPath = match.Groups[1].Value;
                    // Basic check if it looks like a path (optional, could be more robust)
                    if (!string.IsNullOrWhiteSpace(potentialPath))
                    {
                        // Resolve to absolute path
                        string absolutePath;
                        try
                        {
                            absolutePath = Path.GetFullPath(potentialPath);
                        }
                        catch (ArgumentException ex) // Handle invalid path characters
                        {
                            logger.LogWarning("Invalid characters in potential path '{Path}', skipping: {Error}", potentialPath, ex.Message);
                            continue;
                        }
                        catch (PathTooLongException ex)
                        {
                            logger.LogWarning("Potential path '{Path}' is too long, skipping: {Error}", potentialPath, ex.Message);
                            continue;
                        }

                        if (File.Exists(absolutePath) && processedPaths.Add(absolutePath)) // Check existence and avoid duplicates
                        {
                            logger.LogDebug("Found file reference in input: {FilePath}", absolutePath);
                            artifactList.Add(new ArtifactReference(
                                PlatformType: PlatformType.Console, // Updated from LocalFileSystem
                                ArtifactId: absolutePath
                            ));
                            // Remove the matched quoted string (including quotes) from the query text
                            queryText = queryText.Replace(match.Value, string.Empty, StringComparison.Ordinal); 
                        }
                        else if (!processedPaths.Contains(absolutePath))
                        {
                            logger.LogWarning("Quoted text '{Path}' resolved to '{AbsolutePath}' does not exist or is a duplicate, ignoring as file path.", potentialPath, absolutePath);
                        }
                    }
                }
                // Clean up extra whitespace in query text after removing paths
                queryText = queryText.Trim(); 
                if (artifactList.Count == 0) artifactList = null; // Set back to null if no valid files found
            }
            // --- End Parsing Logic ---

            try
            {
                // Ensure there's still some query text left after removing paths
                if (string.IsNullOrWhiteSpace(queryText) && (artifactList == null || artifactList.Count == 0))
                {
                    logger.LogWarning("Input resulted in empty query after processing file paths. Please provide query text.");
                    Console.WriteLine("Error: No query text provided after removing file paths.");
                    continue; 
                }
                
                logger.LogInformation("Sending query: '{Query}' with {ArtifactCount} artifacts.", queryText, artifactList?.Count ?? 0);
                
                var request = new AdapterRequest(
                    PlatformType: "Console",
                    ConversationId: sessionId,
                    UserId: Environment.UserName, // Use current user
                    QueryText: queryText, 
                    ArtifactReferences: artifactList 
                );

                var response = await apiAgent.SendInteractionAsync(request);

                if (response != null && response.Success)
                {
                    logger.LogInformation("API request acknowledged. Response: {ResponseMessage}", response.ResponseMessage);
                    Console.WriteLine($"API: {response.ResponseMessage}"); // Show acknowledgement to user
                }
                else
                {
                    string errorMessage = response?.ErrorMessage ?? "Unknown error from API.";
                    logger.LogError("API call failed: {ErrorMessage}", errorMessage);
                    Console.WriteLine($"Error: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing input.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        logger.LogInformation("Nucleus Console Adapter stopped.");
    }
}
