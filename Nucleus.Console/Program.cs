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

            if (artifactProvider.SupportedArtifactType != "localfile")
            {
                logger.LogError("Configured artifact provider does not support 'localfile'. Provider type: {ProviderType}", artifactProvider.GetType().Name);
                Console.Error.WriteLine("Error: Configured artifact provider does not support 'localfile'.");
                context.ExitCode = 1;
                return;
            }

            var artifactRef = new ArtifactReference(artifactProvider.SupportedArtifactType, fileInfo.FullName, fileInfo.Name); 

            var adapterRequest = new AdapterRequest(
                SessionId: sessionId,
                InputText: $"Ingest file: {fileInfo.Name}",
                ContextSourceIdentifier: null,
                Artifacts: new List<ArtifactReference> { artifactRef }
            );

            var response = await apiAgent.SendInteractionAsync(adapterRequest);

            if (response != null && !response.IsError)
            {
                logger.LogInformation("Ingestion interaction successful. Response: {ResponseText}", response.ResponseText);
                Console.WriteLine($"Successfully initiated ingestion for: {fileInfo.Name}");
                if (response.SourceReferences?.Any() ?? false)
                {
                    logger.LogInformation("Sources returned by API: {Sources}", string.Join(", ", response.SourceReferences));
                }
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

            var adapterRequest = new AdapterRequest(
                SessionId: sessionId,
                InputText: queryText,
                ContextSourceIdentifier: contextId,
                Artifacts: null
            );

            var response = await apiAgent.SendInteractionAsync(adapterRequest);

            if (response != null && !response.IsError)
            {
                logger.LogInformation("Query interaction successful.");
                Console.WriteLine("\n---");
                Console.WriteLine($"Response: {response.ResponseText}");
                if (response.SourceReferences?.Any() ?? false)
                {
                    Console.WriteLine($"Sources: {string.Join(", ", response.SourceReferences)}");
                    foreach (var reference in response.SourceReferences)
                    {
                        Console.WriteLine($"- {reference}");
                        if (ArtifactReference.TryParse(reference, out var artifactRef) && artifactRef != null)
                        {
                            if (artifactRef.Type.Equals("file", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var content = await artifactProvider.GetArtifactContentAsync(artifactRef);
                                    if (content != null)
                                    {
                                        // Read the stream content into a string
                                        string contentString;
                                        using (var reader = new StreamReader(content)) 
                                        {
                                            contentString = await reader.ReadToEndAsync();
                                        }

                                        if (!string.IsNullOrEmpty(contentString))
                                        {
                                            Console.WriteLine("  Content Preview:");
                                            var lines = contentString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                            foreach (var line in lines.Take(5))
                                            {
                                                Console.WriteLine($"    {line}");
                                            }
                                            if (lines.Length > 5) Console.WriteLine("    [...]");
                                        }
                                        else
                                        {
                                            Console.WriteLine("  (Could not retrieve content)");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("  (Could not retrieve content)");
                                    }
                                }
                                catch (Exception fileEx) 
                                {
                                    logger.LogWarning(fileEx, "Failed to retrieve content for source reference: {Reference}", reference);
                                    Console.WriteLine("  (Error retrieving content)");
                                }
                            }
                        }
                        else
                        {
                            logger.LogTrace("Could not parse source reference: {Reference}", reference);
                        }
                    }
                }
                Console.WriteLine("---");
            }
            else
            {
                logger.LogError("Query interaction failed. Error: {ErrorMessage}", response?.ErrorMessage ?? "Unknown error");
                Console.Error.WriteLine($"\nError: {response?.ErrorMessage ?? "Request failed."}");
                context.ExitCode = 1; 
            }
        });
        rootCommand.AddCommand(queryCommand);

        return rootCommand;
    } 
}
