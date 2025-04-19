using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Nucleus.Console.Services;
using Nucleus.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Hosting; 
using System.CommandLine.Builder; 
using System.CommandLine.Invocation;
using System.CommandLine.Parsing; 
using System.Collections.Generic;
using System.Linq;

public class Program
{
    // --- Application Entry Point ---
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = BuildRootCommand();

        var commandLineBuilder = new CommandLineBuilder(rootCommand);

        commandLineBuilder.UseDefaults(); 

        // Configure the host using the UseHost extension on the builder
        commandLineBuilder.UseHost(Host.CreateDefaultBuilder, 
            hostBuilder =>
            {
                // Configure services using our existing method
                hostBuilder.ConfigureServices(ConfigureServices);
                // Add other host configurations if needed
            });

        // Build the parser from the configured builder
        var parser = commandLineBuilder.Build();

        // Invoke the command line application via the parser
        return await parser.InvokeAsync(args);
    }

    // --- Service Configuration ---
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient<NucleusApiServiceAgent>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration.GetValue<string>("NucleusApi:BaseUrl") ?? "http://localhost:5141");
        });
        services.AddSingleton<IArtifactProvider, ConsoleArtifactProvider>();
        services.AddLogging();
    }

    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Nucleus Console Interface - Interact with the Nucleus engine.");

        var fileArgument = new Argument<FileInfo>("file", "The path to the file to ingest.") { Arity = ArgumentArity.ExactlyOne };
        fileArgument.ExistingOnly();
        var ingestCommand = new Command("ingest", "Ingest a file into the Nucleus knowledge base.") { fileArgument };

        ingestCommand.SetHandler(async (InvocationContext context) =>
        {
            var fileInfo = context.ParseResult.GetValueForArgument(fileArgument);
            var logger = context.BindingContext.GetService<ILogger<Program>>()!;
            var apiAgent = context.BindingContext.GetService<NucleusApiServiceAgent>()!;
            var artifactProvider = context.BindingContext.GetService<IArtifactProvider>()!;

            var sessionId = Guid.NewGuid().ToString(); 

            logger.LogInformation("Executing 'ingest' command for file: {FilePath} with SessionId: {SessionId}", fileInfo.FullName, sessionId);

            // Check if the provider is the expected type for this adapter
            if (!(artifactProvider is ConsoleArtifactProvider))
            {
                logger.LogError("Configured artifact provider is not ConsoleArtifactProvider. Provider type: {ProviderType}", artifactProvider.GetType().Name);
                Console.Error.WriteLine("Error: Configured artifact provider is not ConsoleArtifactProvider.");
                context.ExitCode = 1;
                return;
            }

            // Corrected ArtifactReference constructor
            var artifactRef = new ArtifactReference(PlatformType: "Console", ArtifactId: fileInfo.FullName, OptionalContext: null); 

            // Corrected AdapterRequest constructor
            var adapterRequest = new AdapterRequest(
                PlatformType: "Console",
                ConversationId: sessionId,
                UserId: "ConsoleUser", // Placeholder User ID for Console
                QueryText: $"Ingest file: {fileInfo.Name}",
                ArtifactReferences: new List<ArtifactReference> { artifactRef },
                MessageId: null,
                ReplyToMessageId: null,
                Metadata: null
            );

            var response = await apiAgent.SendInteractionAsync(adapterRequest);

            // Use Success property instead of IsError
            if (response != null && response.Success)
            {
                // Use ResponseMessage property instead of ResponseText
                logger.LogInformation("Ingestion interaction successful. Response: {ResponseMessage}", response.ResponseMessage);
                Console.WriteLine($"Successfully initiated ingestion for: {fileInfo.Name}");
                // Removed logging for SourceReferences as it no longer exists
            }
            else
            {
                logger.LogError("Ingestion interaction failed. Error: {ErrorMessage}", response?.ErrorMessage ?? "Unknown error");
                Console.Error.WriteLine($"Error ingesting file: {response?.ErrorMessage ?? "Unknown error"}");
                context.ExitCode = 1;
            }
        });
        rootCommand.AddCommand(ingestCommand);

        var queryArgument = new Argument<string>("query", "The query text to send to the Nucleus engine.") { Arity = ArgumentArity.ExactlyOne };
        var contextOption = new Option<string>(new[] { "--context", "-c" }, "Optional context identifier (e.g., file path, message ID).");
        var queryCommand = new Command("query", "Send a query to the Nucleus engine.") { queryArgument, contextOption };

        queryCommand.SetHandler(async (InvocationContext context) =>
        {
            var queryText = context.ParseResult.GetValueForArgument(queryArgument);
            var contextId = context.ParseResult.GetValueForOption(contextOption);
            var logger = context.BindingContext.GetService<ILogger<Program>>()!;
            var apiAgent = context.BindingContext.GetService<NucleusApiServiceAgent>()!;
            var artifactProvider = context.BindingContext.GetService<IArtifactProvider>()!;

            var sessionId = Guid.NewGuid().ToString(); 

            logger.LogInformation("Executing 'query' command. Query: '{QueryText}', ContextId: '{ContextId}', SessionId: {SessionId}",
                queryText, contextId ?? "None", sessionId);

            // Corrected AdapterRequest constructor
            var adapterRequest = new AdapterRequest(
                PlatformType: "Console",
                ConversationId: sessionId,
                UserId: "ConsoleUser", // Placeholder User ID for Console
                QueryText: queryText,
                ReplyToMessageId: contextId, // Map contextId to ReplyToMessageId
                ArtifactReferences: null,
                MessageId: null,
                Metadata: null
            );

            var response = await apiAgent.SendInteractionAsync(adapterRequest);

            // Use Success property instead of IsError
            if (response != null && response.Success)
            {
                logger.LogInformation("Query interaction successful.");
                Console.WriteLine("\n---");
                // Use ResponseMessage property instead of ResponseText
                Console.WriteLine($"Response: {response.ResponseMessage}");
                // Removed logic for SourceReferences as it no longer exists
                // Also removed GetArtifactContentAsync call
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    Console.WriteLine($"API reported error: {response.ErrorMessage}");
                }
                // Optionally, display GeneratedArtifactReference if present
                if (response.GeneratedArtifactReference != null)
                {
                    Console.WriteLine($"Generated Artifact: Platform={response.GeneratedArtifactReference.PlatformType}, Id={response.GeneratedArtifactReference.ArtifactId}");
                }
            }
            else
            {
                logger.LogError("Query interaction failed. Error: {ErrorMessage}", response?.ErrorMessage ?? "Unknown error");
                Console.Error.WriteLine($"Error executing query: {response?.ErrorMessage ?? "Unknown error"}");
                context.ExitCode = 1;
            }
        });
        rootCommand.AddCommand(queryCommand);

        return rootCommand;
    } 
}
