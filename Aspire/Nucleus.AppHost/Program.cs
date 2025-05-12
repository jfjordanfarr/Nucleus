// This AppHost project defines the services and their configurations for local development and integration testing.
// For details on the overall testing strategy, including how this AppHost is utilized, 
// see ../../Docs/Architecture/09_ARCHITECTURE_TESTING.md

using Aspire.Hosting; // Ensure this is present for WithVolumeMount
using Aspire.Hosting.ApplicationModel; // Ensure this is present for VolumeMountType
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // Added
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Added
using System;
using System.IO; // Added for Path.Combine

var builder = DistributedApplication.CreateBuilder(args);

// Get a logger instance
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Check if running in the context of Aspire.Hosting.Testing
var isTestEnvironment = Environment.GetEnvironmentVariable("ASPIRE_TESTING_ENVIRONMENT") == "true";
// Check if simple console logging is requested by the test
var useSimpleConsoleFormatterForTest = Environment.GetEnvironmentVariable("NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE") == "true";

// Load user secrets for local development (but not during testing)
if (builder.Environment.IsDevelopment() && !isTestEnvironment)
{
    builder.Configuration.AddUserSecrets<Program>();
}

// --- Data Persistence ---

// Azure Cosmos DB emulator resource
var cosmosResource = builder.AddAzureCosmosDB("cosmosdb")
                           .RunAsPreviewEmulator(e => e.WithDataExplorer());

// Define the main database for Cosmos DB
var mainDatabase = cosmosResource.AddCosmosDatabase("NucleusDb");

// --- Messaging ---

// Azure Service Bus emulator resource
var serviceBusBuilder = builder.AddAzureServiceBus("sbemulatorns")
    .RunAsEmulator(sb =>
    {
        // Explicitly define the AMQP endpoint for messaging
        sb.WithEndpoint(port: 5672, targetPort: 5672, name: "amqp");
        
        // Add HTTP endpoint for management operations
        sb.WithEndpoint(port: 8706, targetPort: 8706, name: "management");
        
        // Generate a random development-only password
        var devPassword = $"Dev_{Guid.NewGuid():N}".Substring(0, 16);
        
        // Configure SQL Edge
        sb.WithEnvironment("ACCEPT_EULA", "Y");
        sb.WithEnvironment("MSSQL_TELEMETRY_ENABLED", "FALSE");
        sb.WithEnvironment("SQL_WAIT_INTERVAL", "1");
        
    });

// Declaratively add the required queues (Aspire pattern)
serviceBusBuilder.AddServiceBusQueue("nucleus-ingestion-requests"); // Used by integration tests
serviceBusBuilder.AddServiceBusQueue("nucleus-background-tasks");  // Used by the API service

// --- API Services ---

// Reference the API service project.
var apiServiceBuilder = builder.AddProject<Projects.Nucleus_Services_Api>("nucleusapi") // Use typed reference
       .WithReference(mainDatabase) // Reference the main Cosmos DATABASE resource
       .WithReference(cosmosResource) // Ensure nucleusapi has a reference to cosmos for normal operation
       .WithReference(serviceBusBuilder, connectionName: "sbBackgroundTasks") 
       // Add a default reference to the Service Bus with the standard connection name
       .WithReference(serviceBusBuilder) 
       // Add explicit reference to use AMQP with ServiceBus
       .WithEnvironment("ServiceBusClientOptions__TransportType", "AmqpTcp")
       .WithEnvironment(ctx =>
       {
           // Inject the Cosmos Database Name for the Test Environment
           // Note: Use double underscore __ for hierarchical keys in env vars
           if (isTestEnvironment)
           { // This is a RUNTIME check
               ctx.EnvironmentVariables["CosmosDb__DatabaseName"] = "NucleusTestDb";
               
               // Explicit setting of CosmosDb__ConnectionString is removed from this lambda.
               // .WithReference(mainDatabase) handles the default connection string.
               // The #if NUCLEUS_TEST_ENVIRONMENT block handles specific override for those builds.
           }

           // Propagate Logging settings for testing
           // This section can be potentially superseded by the more specific NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE settings below,
           // or you can keep it as a fallback.
           if (isTestEnvironment && !useSimpleConsoleFormatterForTest) // Only apply if not overridden by simple formatter
           {
               ctx.EnvironmentVariables["Logging__LogLevel__Default"] = "Warning";
               ctx.EnvironmentVariables["Logging__LogLevel__Nucleus"] = "Information"; // Keep our code's logs at Info
           }

           // Propagate NUCLEUS_TEST_PERSONA_CONFIGS_JSON if set
           var testPersonaConfigsJson = Environment.GetEnvironmentVariable("NUCLEUS_TEST_PERSONA_CONFIGS_JSON");
           if (!string.IsNullOrEmpty(testPersonaConfigsJson))
           {
               ctx.EnvironmentVariables["NUCLEUS_TEST_PERSONA_CONFIGS_JSON"] = testPersonaConfigsJson;
               logger.LogInformation("Propagating NUCLEUS_TEST_PERSONA_CONFIGS_JSON to nucleusapi service."); // Added logging
           }

           return Task.CompletedTask;
       })
       .WithEndpoint(port: 19110, scheme: "https", name: "httpsExternal", isExternal: true); // Expose a specific HTTPS endpoint

#if NUCLEUS_TEST_ENVIRONMENT // This is a COMPILE-TIME check
    // Ensure the API service uses the emulator connection string for the NucleusTestDb during tests.
    
    // Define a specific database resource for the test environment.
    var testDatabase = cosmosResource.AddCosmosDatabase("NucleusTestDb");

    if (testDatabase != null)
    {
        var connectionString = testDatabase.GetConnectionString(); 
        if (apiServiceBuilder != null && connectionString != null)
        {
             apiServiceBuilder.WithEnvironment("CosmosDb__ConnectionString", connectionString);
             logger.LogInformation("[Nucleus.AppHost] In NUCLEUS_TEST_ENVIRONMENT: Explicitly setting CosmosDb__ConnectionString for 'nucleusapi' project using 'testDatabase' resource.");
        }
        else if (connectionString == null)
        {
            logger.LogWarning("[Nucleus.AppHost] In NUCLEUS_TEST_ENVIRONMENT: 'testDatabase' resource returned a null connection string. Cannot set for 'nucleusapi'.");
        }
    }
    else
    {
        logger.LogWarning("[Nucleus.AppHost] In NUCLEUS_TEST_ENVIRONMENT: 'testDatabase' resource builder was null (failed to define from cosmosResource). Cannot set CosmosDb__ConnectionString for 'nucleusapi'.");
    }
#endif

// Apply specific logging formatter and levels if requested by the test
if (useSimpleConsoleFormatterForTest)
{
    logger.LogInformation("[Nucleus.AppHost] Applying Simple console logger settings to 'nucleusapi' for test run based on NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE=true.");
    apiServiceBuilder.WithEnvironment("Logging__Console__FormatterName", "Simple");
    apiServiceBuilder.WithEnvironment("Logging__Console__FormatterOptions__JsonWriterOptions__Indented", "false"); // Ensure JSON options don't interfere
    apiServiceBuilder.WithEnvironment("Logging__LogLevel__Default", "Information"); 
    apiServiceBuilder.WithEnvironment("Logging__LogLevel__Nucleus.Services.Api", "Debug");
    apiServiceBuilder.WithEnvironment("Logging__LogLevel__Nucleus.Infrastructure.Data.Persistence", "Debug");
}

// Conditional logging adjustments for testing
if (isTestEnvironment)
{
    Console.WriteLine("ASPIRE_TESTING_ENVIRONMENT detected. Applying reduced logging levels to API service and Service Bus emulator."); // Adjusted diagnostic output
    if (useSimpleConsoleFormatterForTest) {
        Console.WriteLine("[Nucleus.AppHost] NUCLEUS_TEST_LOGGING_FORMAT_SIMPLE=true. 'nucleusapi' service configured for Simple console logging.");
    }
}

builder.Build().Run();
