using Aspire.Hosting; // Add this using for AddDashboard extension

var builder = DistributedApplication.CreateBuilder(args);

// Explicitly add the Aspire Dashboard
//builder.AddDashboard(); // Temporarily commented out due to persistent build errors

// Reference the API service project. We'll refer to it as "nucleusapi" for service discovery.
var apiService = builder.AddProject<Projects.Nucleus_ApiService>("nucleusapi"); // Corrected Project Name, Set Service Name

// Add the console adapter reference
// Reference the Console project. Let's name it "nucleusconsole".
// Crucially, tell Aspire that this project needs the URL of "nucleusapi".
builder.AddProject<Projects.Nucleus_Console>("nucleusconsole") // Corrected Project Name
       .WithReference(apiService);             // Add Service Discovery Link

builder.Build().Run();
