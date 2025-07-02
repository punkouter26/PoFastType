using PoFastType.Api.Services;
using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for file logging to create a single log.txt file (overwrite each run)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", 
        rollingInterval: RollingInterval.Infinite,
        rollOnFileSizeLimit: false,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

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
            }            else
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

app.Run();

// Make Program class accessible for testing
public partial class Program { }
