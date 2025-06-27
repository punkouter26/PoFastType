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
            }
            else
            {
                // In production, be more restrictive
                policy.WithOrigins("https://yourdomain.com")
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

// Configure authentication for Azure Easy Auth
if (builder.Environment.IsDevelopment())
{
    // Development: Use anonymous authentication by default, but still support identity
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DevelopmentAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DevelopmentAuthenticationDefaults.AuthenticationScheme;
    })
    .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationDefaults.AuthenticationScheme, options => { });
}
else
{
    // Production: Azure Easy Auth handles authentication, but allow anonymous access
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "EasyAuth";
        options.DefaultChallengeScheme = "EasyAuth";
    })
    .AddScheme<EasyAuthAuthenticationOptions, EasyAuthAuthenticationHandler>("EasyAuth", options => { });
}

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

// Use CORS policy - must be early in the pipeline
app.UseCors(MyAllowSpecificOrigins);

app.UseRouting();

// Use authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();
app.MapFallbackToFile("index.html");

app.Run();

// Make Program class accessible for testing
public partial class Program { }

// Development-only authentication for local testing
public static class DevelopmentAuthenticationDefaults
{
    public const string AuthenticationScheme = "DevelopmentAuthentication";
}

public class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class DevelopmentAuthenticationHandler : AuthenticationHandler<DevelopmentAuthenticationOptions>
{
    public DevelopmentAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<DevelopmentAuthenticationOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if this is a development user request (indicated by Authorization header with "Bearer dummy_token")
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer dummy_token"))
        {
            // Return development user claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
                new Claim(ClaimTypes.Name, "Development User"),
                new Claim(ClaimTypes.Email, "dev@pofasttype.com"),
                new Claim("identity_type", "development")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        // In development, provide anonymous identity by default
        var anonymousClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, WellKnownUsers.AnonymousUserId),
            new Claim(ClaimTypes.Name, WellKnownUsers.AnonymousUsername),
            new Claim(ClaimTypes.Email, WellKnownUsers.AnonymousEmail),
            new Claim("identity_type", "anonymous")
        };
        var anonymousIdentity = new ClaimsIdentity(anonymousClaims, Scheme.Name);
        var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);
        var anonymousTicket = new AuthenticationTicket(anonymousPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(anonymousTicket));
    }
}

// Easy Auth authentication for Azure production
public class EasyAuthAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class EasyAuthAuthenticationHandler : AuthenticationHandler<EasyAuthAuthenticationOptions>
{
    public EasyAuthAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<EasyAuthAuthenticationOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Easy Auth forwards user information via headers
        var userIdHeader = Context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
        var userNameHeader = Context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
        var userEmailHeader = Context.Request.Headers["X-MS-CLIENT-PRINCIPAL"];

        var claims = new List<Claim>();

        if (!string.IsNullOrEmpty(userIdHeader))
        {
            // Authenticated user via Easy Auth
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdHeader));
            claims.Add(new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userIdHeader));
            claims.Add(new Claim("identity_type", "authenticated"));

            if (!string.IsNullOrEmpty(userNameHeader))
            {
                claims.Add(new Claim(ClaimTypes.Name, userNameHeader));
            }

            // Parse the base64 encoded principal if available for more claims
            if (!string.IsNullOrEmpty(userEmailHeader))
            {
                try
                {
                    var principalBytes = Convert.FromBase64String(userEmailHeader);
                    var principalJson = System.Text.Encoding.UTF8.GetString(principalBytes);
                    var principal = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(principalJson);
                    
                    if (principal.TryGetProperty("claims", out var claimsElement))
                    {
                        foreach (var claim in claimsElement.EnumerateArray())
                        {
                            if (claim.TryGetProperty("typ", out var typElement) && 
                                claim.TryGetProperty("val", out var valElement))
                            {
                                var claimType = typElement.GetString();
                                var claimValue = valElement.GetString();
                                
                                if (!string.IsNullOrEmpty(claimType) && !string.IsNullOrEmpty(claimValue))
                                {
                                    claims.Add(new Claim(claimType, claimValue));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Log error if needed, but continue with basic claims
                }
            }
        }
        else
        {
            // Anonymous user - provide default anonymous identity
            claims.Add(new Claim(ClaimTypes.NameIdentifier, WellKnownUsers.AnonymousUserId));
            claims.Add(new Claim(ClaimTypes.Name, WellKnownUsers.AnonymousUsername));
            claims.Add(new Claim(ClaimTypes.Email, WellKnownUsers.AnonymousEmail));
            claims.Add(new Claim("identity_type", "anonymous"));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
