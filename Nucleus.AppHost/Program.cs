var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Nucleus_ApiService>("apiservice");

// Add the console adapter reference
builder.AddProject<Projects.Nucleus_Console>("consoleadapter");

builder.Build().Run();
