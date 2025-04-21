using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection; // Added for AddGoogleAI extension method
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.ServiceDiscovery; 
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Mscc.GenerativeAI; // Added for AddGoogleAI extension method
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Adapters.Teams;
using Nucleus.ApiService.Diagnostics;
using Nucleus.ApiService.Infrastructure.Messaging;
using Nucleus.Services.Api.Infrastructure.Messaging; 
using Nucleus.ApiService; // For NucleusServiceExtensions
using Nucleus.Domain.Processing; // For AddProcessingServices
using Nucleus.Domain.Processing.Infrastructure; // Added for Adapter
using Nucleus.Services.Api.Infrastructure; // Added for NullArtifactProvider

// *** Obtain logger early for setup logging ***
using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
    .SetMinimumLevel(LogLevel.Information)
    .AddConsole());
var _logger = loggerFactory.CreateLogger("Program");

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

// Retrieve Service Bus connection string
var serviceBusConnectionString = builder.Configuration.GetConnectionString("servicebus");

// Conditionally register Azure Service Bus or Null publisher
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    _logger.LogInformation("Azure Service Bus connection string found. Configuring Azure Service Bus.");
    // Configure Azure Service Bus Client
    builder.Services.AddAzureClients(clientBuilder =>
    {
        // Use standard GetConnectionString, expecting Aspire to inject it via WithReference
        clientBuilder.AddServiceBusClientWithNamespace(serviceBusConnectionString) // Use the retrieved connection string
            .WithCredential(new DefaultAzureCredential());
    });

    // Register the Azure Service Bus Publisher
    builder.Services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(AzureServiceBusPublisher<>));
    _logger.LogInformation("Azure Service Bus Publisher registered.");

    // --- Register Service Bus Consumer Hosted Service --- 
    // Only add the consumer if we actually have a connection string
    _logger.LogInformation("Registering Azure Service Bus Consumer Hosted Service.");
    builder.Services.AddHostedService<ServiceBusQueueConsumerService>();
}
else
{
    // Register the Null Publisher if Service Bus is not configured
    _logger.LogWarning("Azure Service Bus connection string not found. Registered NullMessageQueuePublisher. Messaging will be disabled.");
    builder.Services.AddSingleton(typeof(IMessageQueuePublisher<>), typeof(NullMessageQueuePublisher<>));
}
// --- End Service Bus Configuration & Consumer Registration ---

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Add authentication
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Add authorization
// builder.Services.AddAuthorization();

// Add Nucleus specific services (using extension methods for organization)
builder.Services.AddNucleusServices(builder.Configuration);
builder.Services.AddProcessingServices(); 

_logger.LogInformation("Registering Core AI and Artifact Services...");

// Define a temporary class to hold Google AI configuration
/* // REMOVED Local definition
public class GoogleAiOptions
{
    public const string SectionName = "AI:GoogleAI";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}
*/

// -------------------- Google AI (Gemini via Mscc) Configuration -------------------- 

// Attempt to bind configuration
var googleAiOptions = new GoogleAiOptions();
builder.Configuration.GetSection(GoogleAiOptions.SectionName).Bind(googleAiOptions);

// Validate mandatory options
if (string.IsNullOrWhiteSpace(googleAiOptions.ApiKey))
{
    _logger.LogError("Google AI (Gemini via Mscc) configuration is missing required ApiKey.");
    throw new Exception("Google AI (Gemini via Mscc) configuration is missing required ApiKey.");
}

// --- Core AI and Artifact Services --- 

// --- Configure Google AI (Gemini via Mscc.GenerativeAI) ---
builder.Services.Configure<GoogleAiOptions>(builder.Configuration.GetSection(GoogleAiOptions.SectionName));
_logger.LogInformation("Google AI (Mscc) Options configured from section: {SectionName}", GoogleAiOptions.SectionName);

// Register our custom adapter 
builder.Services.AddSingleton<GoogleAiChatClientAdapter>(); 
_logger.LogInformation("Registered custom chat adapter GoogleAiChatClientAdapter.");

// Register a Null provider for the API service context
builder.Services.AddSingleton<IArtifactProvider, NullArtifactProvider>();
_logger.LogInformation("Registered NullArtifactProvider for API service context.");

// --- End Core AI and Artifact Services --- 

// Add services for MVC Controllers (needed for Bot Framework endpoint)
builder.Services.AddControllers();

// Remove obsolete/incorrect Teams adapter registration call
// builder.Services.AddTeamsCloudAdapter(builder.Configuration.GetSection("BotFramework"));

// --- Register Bot Framework specific services ---
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
// Register CloudAdapter as the IBotFrameworkHttpAdapter
builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp =>
{
    var auth = sp.GetRequiredService<BotFrameworkAuthentication>();
    var logger = sp.GetRequiredService<ILogger<CloudAdapter>>(); // Get logger specifically for CloudAdapter
    return new CloudAdapter(auth, logger);
});
// Keep TeamsAdapterConfiguration for potential use by GraphClientService or other components
builder.Services.Configure<TeamsAdapterConfiguration>(builder.Configuration.GetSection("TeamsAdapter"));
builder.Services.AddTransient<IBot, TeamsAdapterBot>();

// Register the GraphClientService needed by TeamsGraphFileFetcher
builder.Services.AddSingleton<GraphClientService>(); 

// --- Platform Abstraction Implementations (Specific to Teams in this ApiService context) ---
builder.Services.AddSingleton<IPlatformAttachmentFetcher, TeamsGraphFileFetcher>();
builder.Services.AddSingleton<IPlatformNotifier, TeamsNotifier>();

// --- Development Only Diagnostics ---
if (builder.Environment.IsDevelopment())
{
    // Add development-specific services here
    // Conditionally register the smoke test service only if Service Bus is configured
    if (!string.IsNullOrEmpty(serviceBusConnectionString))
    {
        builder.Services.AddHostedService<ServiceBusSmokeTestService>(); 
    }
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

// Define WeatherForecast record if not defined elsewhere
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
