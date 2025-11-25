using PoFastType.Api.Services;
using PoFastType.Api.Services.HealthChecks;
using PoFastType.Api.Repositories;
using PoFastType.Api.Middleware;
using PoFastType.Api.HealthChecks;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault for both Development and Production
// In Development: Uses Azure CLI credentials (az login) or Visual Studio credentials
// In Production: Uses Managed Identity
var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
        
        Log.Information("Azure Key Vault configured successfully: {VaultUri}", keyVaultUri);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to configure Azure Key Vault. Falling back to local configuration.");
    }
}

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PoFastType")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

Log.Information("PoFastType application starting up at {Timestamp}", DateTime.UtcNow);

builder.Host.UseSerilog();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("PoFastType")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("PoFastType.Metrics"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for diagnostic checks
builder.Services.AddHttpClient();

// Add CORS policy
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // In development, allow all localhost origins for convenience
                policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // In production, use configured origins
                var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
                    ?? new[] { "https://pofasttype.azurewebsites.net" };
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
});

// Register application services - Dependency Injection Container (IoC Pattern)
// Single Responsibility Principle (SOLID) - Each service has one responsibility
// Dependency Inversion Principle (SOLID) - Depend on abstractions, not concretions
builder.Services.AddScoped<IGameResultRepository, AzureTableGameResultRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ITextGenerationStrategy, HardcodedTextStrategy>();
builder.Services.AddScoped<ITextGenerationService, TextGenerationService>();

// Register biometrics services
builder.Services.AddScoped<IKeystrokeRepository, AzureTableKeystrokeRepository>();
builder.Services.AddScoped<IBiometricsService, BiometricsService>();

// Register health check strategies (Strategy Pattern for reduced complexity)
builder.Services.AddScoped<IHealthCheckStrategy, InternetConnectivityHealthCheck>();
builder.Services.AddScoped<IHealthCheckStrategy, AzureConnectivityHealthCheck>();
builder.Services.AddScoped<IHealthCheckStrategy, SelfHealthCheck>();
builder.Services.AddScoped<IHealthCheckStrategy, ApiHealthCheck>();
builder.Services.AddScoped<IHealthCheckStrategy, TableStorageHealthCheck>();
builder.Services.AddScoped<IHealthCheckStrategy, OpenAIHealthCheck>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<AzureTableStorageHealthCheck>(
        "azure_table_storage",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "storage", "database" });

var app = builder.Build();

// Add global exception handling middleware first
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

// Static files must come early in the pipeline - configure for Blazor WebAssembly
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Use CORS policy - must be early in the pipeline
app.UseCors(MyAllowSpecificOrigins);

app.UseRouting();

// Map API controllers first - they have priority over fallback routes
app.MapControllers();

// Map Health Check endpoint
app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                exception = e.Value.Exception?.Message
            }),
            totalDuration = report.TotalDuration.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});

// Fallback to serve the Blazor WebAssembly app for non-API routes
app.MapFallbackToFile("index.html");

Log.Information("PoFastType application configured and ready to serve requests");

try
{
    Log.Information("Starting PoFastType web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PoFastType application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("PoFastType application shutting down at {Timestamp}", DateTime.UtcNow);
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
