using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Services.Api.Diagnostics; // For HealthCheckResponseWriter
using Nucleus.Services.Api.Endpoints; // For MapIngestEndpoints
using System;

namespace Nucleus.Services.Api;

/// <summary>
/// Provides extension methods for configuring the <see cref="WebApplication"/>.
/// </summary>
/// <seealso cref="../../../../Docs/Architecture/06_ARCHITECTURE_SECURITY.md"/>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps Nucleus specific endpoints and configures middleware.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>The configured <see cref="WebApplication"/>.</returns>
    public static WebApplication MapNucleusEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var logger = app.Services.GetRequiredService<ILogger<Program>>(); // Get logger from DI
        logger.LogInformation("Mapping Nucleus endpoints...");

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            logger.LogInformation("Swagger UI enabled for Development environment.");
        }

        app.UseHttpsRedirection();
        logger.LogInformation("HTTPS redirection enabled.");

        app.UseRouting(); // Explicitly adding UseRouting before UseAuthentication/UseAuthorization

        // Authentication & Authorization Middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Map Controllers (if using attribute routing)
        app.MapControllers();

        // Map Aspire default endpoints (e.g., /health, /metrics)
        app.MapDefaultEndpoints();
        logger.LogInformation("Mapped Aspire default endpoints (e.g., /health, /metrics).");

        // Map Ingest Endpoints
        app.MapIngestEndpoints();

        // --- Explicit API Endpoints (Example - Can be removed if controllers handle all) ---
        // var apiGroup = app.MapGroup("/api/v1");
        // Example: A simple health check or version endpoint
        // apiGroup.MapGet("/status", () => Results.Ok(new { Status = "OK", Version = "1.0" }))
        //         .WithName("GetApiStatus")
        //         .WithTags("Diagnostics");

        logger.LogInformation("Nucleus endpoint mapping complete.");
        return app;
    }
}
