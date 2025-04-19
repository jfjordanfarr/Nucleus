using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI; 
using Nucleus.Abstractions; 
using Nucleus.ApiService; 
using Nucleus.ApiService.Configuration; 
using Nucleus.Personas.Core; 
using System;
using System.Security.Cryptography;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Nucleus.Adapters.Teams; 

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components (if used).
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Configure API controllers
builder.Services.AddControllers();

// Add OpenAPI/Swagger services
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Nucleus API Service", Version = "v1" });

    // Optional: Configure XML comments path if you want descriptions from XML docs
    // var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Register the core IGenerativeAI service for Gemini
// This uses the base Mscc.GenerativeAI library pattern found in examples,
// as the Mscc.GenerativeAI.Microsoft layer seems tied to the missing IChatClient.
// See: ../Docs/Architecture/08_ARCHITECTURE_AI_INTEGRATION.md
builder.Services.AddSingleton<IGenerativeAI>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); // Get logger

    // Prioritize Environment Variable, Fallback to AppSettings
    string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    string apiKeySource = "environment variable (GEMINI_API_KEY)";

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        apiKey = configuration["AiProviders:GoogleAI:ApiKey"];
        apiKeySource = "appsettings.json (AiProviders:GoogleAI:ApiKey)";
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        logger.LogError("Gemini API Key not found in environment variable 'GEMINI_API_KEY' or configuration 'AiProviders:GoogleAI:ApiKey'.");
        throw new InvalidOperationException("Gemini API Key is missing. Please set the GEMINI_API_KEY environment variable or configure it in appsettings.json under AiProviders:GoogleAI:ApiKey.");
    }

    logger.LogInformation("Using Gemini API Key from {ApiKeySource}", apiKeySource);

    // Assuming GoogleAI is the desired implementation for Gemini
    // The model name (e.g., "gemini-1.5-pro-latest") might also need configuration later
    return new GoogleAI(apiKey);
});

// Register Personas
builder.Services.AddScoped<IPersona<EmptyAnalysisData>, BootstrapperPersona>();

// Add services for controllers (required by app.MapControllers)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

// --- Configure Strongly Typed Options ---
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

// Configure Teams Adapter
builder.Services.Configure<TeamsAdapterConfiguration>(builder.Configuration.GetSection("TeamsAdapter"));

// Create the Bot Framework Authentication object.
// The Bot Framework requires apps to provide their own implementation of IBotFrameworkHttpAdapter.
// See https://aka.ms/botframework-authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the Bot Adapter with error handling enabled.
// The Bot Framework Adapter is responsible for processing incoming activities.
// It directs incoming activities to the Bot popular flow or handler logic.
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
// See: https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0&tabs=csharp#bot-logic
builder.Services.AddTransient<IBot, TeamsAdapterBot>();

// ---> Register Graph Client Service ---
builder.Services.AddScoped<GraphClientService>();
// <--- Register Graph Client Service ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nucleus API Service V1");
        options.RoutePrefix = string.Empty; // Serve UI at app root
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

// Map API controllers
app.MapControllers();

// The Bot Framework endpoint is typically hosted at /api/messages
// Ensure the Bot Framework adapter is configured to handle requests here.
// Note: app.MapControllers() might already handle this if the BotController inherits correctly,
// but explicit mapping or configuration within AddBot/AddBotFrameworkAdapter is safer.
// Check Bot Framework docs for the latest best practice on endpoint mapping in Minimal API.
// For now, we rely on the services registered above and assume MapControllers() picks it up.
// If issues arise, investigate explicit mapping: `app.MapPost("/api/messages", ...)`

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            System.Security.Cryptography.RandomNumberGenerator.GetInt32(-20, 55),
            summaries[System.Security.Cryptography.RandomNumberGenerator.GetInt32(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
