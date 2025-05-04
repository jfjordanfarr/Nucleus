using Aspire.Hosting; // Ensure this is present for WithVolumeMount
using Aspire.Hosting.ApplicationModel; // Ensure this is present for VolumeMountType
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO; // Added for Path.Combine

var builder = DistributedApplication.CreateBuilder(args);

// Check if running in the context of Aspire.Hosting.Testing
var isTestEnvironment = Environment.GetEnvironmentVariable("ASPIRE_TESTING_ENVIRONMENT") == "true";

// Load user secrets for local development (but not during testing)
if (builder.Environment.IsDevelopment() && !isTestEnvironment)
{
    builder.Configuration.AddUserSecrets<Program>();
}

// --- Data Persistence ---

// Azure Cosmos DB emulator resource
var cosmosDbBuilder = builder.AddAzureCosmosDB("cosmosdb")
                           .RunAsPreviewEmulator(e => e.WithDataExplorer());

// --- Messaging ---

// Azure Service Bus emulator resource
var serviceBusBuilder = builder.AddAzureServiceBus("servicebus")
                               .RunAsEmulator(emulator =>
                               {
                                   // Base EULA acceptance
                                   emulator.WithEnvironment("ACCEPT_EULA", "Y");

                                   // Attempt to reduce logging verbosity ONLY in test environment
                                   if (isTestEnvironment)
                                   {
                                       emulator.WithEnvironment("RuntimeLogLevel", "Warning");
                                       emulator.WithEnvironment("Logging__LogLevel__Default", "Warning"); 
                                   }

                                   // Add the bind mount for the minimal mssql.conf
                                   emulator.WithBindMount(
                                       source: Path.Combine(AppContext.BaseDirectory, "mssql.conf"), // Use path relative to output dir
                                       target: "/var/opt/mssql/mssql.conf",       
                                       isReadOnly: true                
                                   );

                                   // We are omitting WithEndpoint here initially.
                                   // RunAsEmulator often handles necessary port mapping/exposure implicitly.
                                   // If connection fails later, we might need to add it back.
                               });

// Declaratively add the required queues (Aspire pattern)
serviceBusBuilder.AddServiceBusQueue("nucleus-ingestion-requests"); // Used by integration tests
serviceBusBuilder.AddServiceBusQueue("nucleus-background-tasks");  // Used by the API service

// --- API Services ---

// Reference the API service project.
var apiServiceBuilder = builder.AddProject<Projects.Nucleus_Services_Api>("nucleusapi") // Use typed reference
       .WithReference(cosmosDbBuilder) // Reference the builder
       .WithReference(serviceBusBuilder) // Reference the builder
       .WithEnvironment(ctx =>
       {
           // Inject the Cosmos Database Name for the Test Environment
           // Note: Use double underscore __ for hierarchical keys in env vars
           if (isTestEnvironment)
           {
               ctx.EnvironmentVariables["CosmosDb__DatabaseName"] = "NucleusTestDb";
           }

           // Propagate Logging settings for testing
           if (isTestEnvironment)
           {
               ctx.EnvironmentVariables["Logging__LogLevel__Default"] = "Warning";
               ctx.EnvironmentVariables["Logging__LogLevel__Nucleus"] = "Information"; // Keep our code's logs at Info
           }
           return Task.CompletedTask;
       })
       .WithEndpoint(port: 19110, scheme: "https", name: "httpsExternal", isExternal: true); // Expose a specific HTTPS endpoint

if (isTestEnvironment)
{
    // Inject the test database name when running integration tests
    apiServiceBuilder.WithEnvironment("CosmosDb__DatabaseName", "NucleusTestDb");

    // Set a more permissive logging level for the API service during tests
    // to ensure diagnostic messages (like config checks) are visible.
    apiServiceBuilder.WithEnvironment(context =>
    {
        context.EnvironmentVariables["Logging__LogLevel__Nucleus.Services.Api"] = "Information";
    });
}

// Conditional logging adjustments for testing
if (isTestEnvironment)
{
    Console.WriteLine("ASPIRE_TESTING_ENVIRONMENT detected. Applying reduced logging levels to API service and Service Bus emulator."); // Adjusted diagnostic output
}

builder.Build().Run();
