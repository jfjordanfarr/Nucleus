using Mscc.GenerativeAI;
using Nucleus.Abstractions;
using Nucleus.Personas.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

// Map attribute-routed controllers like QueryController
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
