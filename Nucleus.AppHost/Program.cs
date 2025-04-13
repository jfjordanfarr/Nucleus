var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Nucleus_ApiService>("apiservice");

// Remove the web frontend reference
//builder.AddProject<Projects.Nucleus_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService)
//    .WaitFor(apiService);

// Add the console adapter reference
builder.AddProject<Projects.Nucleus_Console>("consoleadapter");

builder.Build().Run();
