var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Nucleus_ApiService>("apiservice");

builder.AddProject<Projects.Nucleus_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
