using PoFastType.Api.Services;
using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure logging to write to a single log.txt file
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add file logging - this will create a single log.txt file (overwrite each run)
// Note: AddFile might not be available by default, commenting out for now
// builder.Logging.AddFile("log.txt");

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
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();

// Configure Azure AD authentication (replaced B2C with regular Entra ID)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // Azure AD configuration using your existing tenant
        var tenantId = "5da66fe6-bd58-4517-8727-deebc8525dcb";
        var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        var clientId = "5eaad8fd-fdba-4bd9-8209-2e22ea3e5f3a";
            
        options.Authority = authority;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Log the error but don't fail - allow anonymous access for typing game
                Console.WriteLine($"Azure AD JWT Auth failed: {context.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
    });

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

// Use authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
