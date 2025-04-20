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
            // Service discovery will resolve 'nucleusapi', trying HTTP first
            client.BaseAddress = new Uri("http://nucleusapi"); 
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
        var queryCommand = new Command("query", "Send a query to the Nucleus engine.") { queryArgument, contextOption };

        // Revert to basic SetHandler with manual resolution inside
        queryCommand.SetHandler(async (InvocationContext context) =>
        {
            // Manually get services and arguments/options
            var queryText = context.ParseResult.GetValueForArgument(queryArgument)!;
            var contextId = context.ParseResult.GetValueForOption(contextOption);
            
            // Resolve services using the passed-in provider
            using var scope = serviceProvider.CreateScope(); // Optional: Use scope if services are scoped
            var provider = scope.ServiceProvider; // Use scoped provider if scope is created
            // var provider = serviceProvider; // Use direct provider if services are singleton/transient

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>(); // Create the specific logger
            var apiAgent = provider.GetRequiredService<NucleusApiServiceAgent>();
            var artifactProvider = provider.GetRequiredService<IArtifactProvider>();

            // Call the static handler method, passing the resolved dependencies
            await HandleQueryAsync(queryText, contextId, logger, apiAgent, artifactProvider);

            // Potentially set ExitCode based on handler outcome if needed
            // context.ExitCode = ...;
        });
        rootCommand.AddCommand(queryCommand);

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

    private static async Task HandleQueryAsync(string queryText, string? contextId, ILogger<Program> logger, NucleusApiServiceAgent apiAgent, IArtifactProvider artifactProvider)
    {
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Executing 'query' command. Query: '{QueryText}', ContextId: '{ContextId}', SessionId: {SessionId}", 
                           queryText, contextId ?? "<none>", sessionId);

        List<ArtifactReference>? artifactList = null;
        // If contextId is provided and it's a valid file path, treat it as an artifact
        if (!string.IsNullOrEmpty(contextId) && File.Exists(contextId))
        {
            if (artifactProvider is ConsoleArtifactProvider consoleProvider) // Keep check for type safety
            {
                var artifactReference = new ArtifactReference(PlatformType: "Console", ArtifactId: contextId);
                artifactList = new List<ArtifactReference> { artifactReference };
                logger.LogInformation("Context ID identified as file path, creating ArtifactReference.");
            }
            else
            {
                logger.LogWarning("Context ID looks like a file path, but artifact provider is not ConsoleArtifactProvider. Cannot create reference.");
            }
        }

        // Prepare AdapterRequest
        var request = new AdapterRequest(
            PlatformType: "Console",
            ConversationId: sessionId, 
            UserId: Environment.UserName,
            QueryText: queryText,
            ArtifactReferences: artifactList 
            // ReplyToMessageId could potentially be linked to contextId if it represents a previous message
        );

        // Send request
        logger.LogInformation("Sending query request to API...");
        var response = await apiAgent.SendInteractionAsync(request);

        // Handle response
        if (response?.Success == true)
        {
            logger.LogInformation("Query successful. Response: {ResponseMessage}", response.ResponseMessage);
            Console.WriteLine($"Response: {response.ResponseMessage}");

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
}
