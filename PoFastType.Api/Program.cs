using PoFastType.Api.Services;
using PoFastType.Api.Repositories;
using PoFastType.Api.Middleware;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Ensure DEBUG directory exists
var debugPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DEBUG");
Directory.CreateDirectory(debugPath);

// Configure Serilog with structured JSON logging and overwrite behavior
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        Path.Combine(debugPath, "log.txt"),
        rollingInterval: RollingInterval.Infinite,
        rollOnFileSizeLimit: false,
        shared: false,
        buffered: false,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .Enrich.WithProperty("Application", "PoFastType")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

// Log application startup
Log.Information("PoFastType application starting up at {Timestamp}", DateTime.UtcNow);

builder.Host.UseSerilog();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure logging to write to console and file
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
                // In production, be more restrictive
                policy.WithOrigins("https://pofasttype.azurewebsites.net")
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
builder.Services.AddScoped<IUserIdentityService>(provider =>
    new UserIdentityService(provider.GetRequiredService<ILogger<UserIdentityService>>()));

var app = builder.Build();

// Add global exception handling middleware first
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Add this for detailed error pages in development
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

// Fallback to serve the Blazor WebAssembly app for non-API routes
app.MapFallbackToFile("index.html");

// Log application ready state
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
