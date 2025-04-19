using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.ServiceDiscovery; // Added for GetServiceConnectionString
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Adapters.Teams;
using Nucleus.ApiService.Diagnostics;
using Nucleus.ApiService.Infrastructure.Messaging;
using Nucleus.ApiService;
using Nucleus.Domain.Processing;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// --- Aspire Service Discovery ---
// builder.AddServiceDiscovery(); // Removed incorrect call
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
// --- End Service Discovery Block ---

// --- Azure Service Bus Configuration ---
builder.Services.AddAzureClients(clientBuilder =>
{
    // Use standard GetConnectionString, expecting Aspire to inject it via WithReference
    clientBuilder.AddServiceBusClientWithNamespace(builder.Configuration.GetConnectionString("servicebus"))
        .WithCredential(new DefaultAzureCredential());
});

// Register our generic publisher implementation
builder.Services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(AzureServiceBusPublisher<>));

// --- Register Service Bus Consumer Hosted Service ---
builder.Services.AddHostedService<ServiceBusQueueConsumerService>();
// --- End Service Bus Configuration & Consumer Registration ---

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authentication
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Add authorization
// builder.Services.AddAuthorization();

// Add Nucleus specific services (using extension methods for organization)
builder.Services.AddNucleusServices(builder.Configuration);
builder.Services.AddNucleusProcessing(builder.Configuration); 

// --- Register Bot Framework specific services ---
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.Configure<TeamsAdapterConfiguration>(builder.Configuration.GetSection("TeamsAdapter"));
builder.Services.AddTransient<IBot, TeamsAdapterBot>();

// --- Platform Abstraction Implementations (Specific to Teams in this ApiService context) ---
builder.Services.AddSingleton<IPlatformAttachmentFetcher, TeamsGraphFileFetcher>();
builder.Services.AddSingleton<IPlatformNotifier, TeamsNotifier>();

// --- Registration for OrchestrationService moved to NucleusProcessingServiceExtensions below ---

// --- Development Only Diagnostics ---
if (builder.Environment.IsDevelopment())
{
    // Add development-specific services here
    builder.Services.AddHostedService<ServiceBusSmokeTestService>(); // Register the smoke test service
}
// --- End Development Only Diagnostics ---

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Add service defaults & Aspire components.
app.MapDefaultEndpoints(); // Health checks, etc.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication & Authorization
// app.UseAuthentication();
// app.UseAuthorization();

// Map controllers (needed for the Bot Framework endpoint)
app.MapControllers();

// Example minimal API endpoint
app.MapGet("/weatherforecast", () => { 
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

    var forecast = Enumerable.Range(1, 5).Select(index =>
    {
#pragma warning disable CA5394 
        return new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        );
#pragma warning restore CA5394 
    })
    .ToArray();
    return forecast;
 });

app.Run();

// Separate extension method for Nucleus specific services (example)
public static class NucleusServiceExtensions
{
    public static IServiceCollection AddNucleusServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register other Nucleus core services here that AREN'T processing specific
        // e.g., configuration models, utility services
        
        return services;
    }
}

// Extension method for registering Nucleus Processing services
public static class NucleusProcessingServiceExtensions
{
    public static IServiceCollection AddNucleusProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the Core Orchestration Service
        services.AddScoped<IOrchestrationService, OrchestrationService>(); 
        
        // Register other processing related services based on Architecture Docs
        // e.g., services.AddScoped<ISessionManager, SessionManager>();
        // e.g., services.AddSingleton<IPersonaRepository, PersonaRepository>();
        // e.g., services.AddScoped<IProcessorRouter, ProcessorRouter>();
        // e.g., services.AddScoped<IContentExtractor, DefaultContentExtractor>();
        
        // Configure options if needed
        // services.Configure<ProcessingOptions>(configuration.GetSection("Processing"));

        return services;
    }
}

// Define WeatherForecast record if not defined elsewhere
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
