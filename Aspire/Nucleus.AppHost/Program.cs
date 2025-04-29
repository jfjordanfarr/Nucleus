using Aspire.Hosting; // Add this using for AddDashboard extension

var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Cosmos DB emulator resource
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb").RunAsPreviewEmulator(e => e.WithDataExplorer()); // Corrected method

// Temporarily disable Azure Service Bus for local testing
// Add Azure Service Bus resource
//var serviceBus = builder.AddAzureServiceBus("servicebus");

// Explicitly adding the Aspire Dashboard is no longer required
//builder.AddDashboard(); // Uncomment this for free build errors! Yippee!

// Reference the API service project. We'll refer to it as "nucleusapi" for service discovery.
var apiService = builder.AddProject<Projects.Nucleus_Services_Api>("nucleusapi") // Use typed reference
       .WithReference(cosmosDb) // Add reference to Cosmos DB
       .WithEndpoint(port: 19110, scheme: "https", name: "httpsExternal", isExternal: true); // Expose a specific HTTPS endpoint
       //.WithReference(serviceBus); // Temporarily remove reference to Service Bus

builder.Build().Run();
