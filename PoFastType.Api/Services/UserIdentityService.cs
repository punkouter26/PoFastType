using PoFastType.Shared.Models;
using System.Security.Claims;

namespace PoFastType.Api.Services;

public class UserIdentityService : IUserIdentityService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserIdentityService> _logger;

    public UserIdentityService(IWebHostEnvironment environment, ILogger<UserIdentityService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public UserIdentity GetCurrentUserIdentity(HttpContext httpContext)
    {
        try
        {
            // Check if we have an authenticated user from JWT (MSAL) or Easy Auth
            var authenticatedUser = GetAuthenticatedUser(httpContext);
            if (authenticatedUser != null)
            {
                _logger.LogDebug("Authenticated user found - {UserId}", authenticatedUser.UserId);
                return authenticatedUser;
            }

            // Fallback to anonymous user
            _logger.LogDebug("No authenticated user found, returning anonymous user");
            return CreateAnonymousIdentity();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user identity, falling back to anonymous");
            return CreateAnonymousIdentity();
        }
    }

    public bool RequiresAuthentication()
    {
        // Only require authentication in Azure for certain premium features
        // For now, allow anonymous access everywhere
        return false;
    }

    public UserIdentity CreateAnonymousIdentity()
    {
        return new UserIdentity
        {
            UserId = WellKnownUsers.AnonymousUserId,
            Username = WellKnownUsers.AnonymousUsername,
            Email = WellKnownUsers.AnonymousEmail,
            IdentityType = UserIdentityType.Anonymous,
            IsAuthenticated = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsAzureEnvironment()
    {
        // In development, always return false to prevent Easy Auth logic from running locally
        if (_environment.IsDevelopment())
        {
            return false;
        }
        // Check for Azure-specific environment variables
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"));
    }    private UserIdentity? GetAuthenticatedUser(HttpContext httpContext)
    {
        // Only check for JWT authentication (B2C tokens)
        var jwtUser = GetB2CUser(httpContext);
        if (jwtUser != null)
        {
            return jwtUser;
        }

        return null;
    }    private UserIdentity? GetB2CUser(HttpContext httpContext)
    {
        try
        {
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // B2C specific claims
                var userId = user.FindFirst("oid") ?? user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);
                var name = user.FindFirst("name") ?? user.FindFirst("given_name") ?? user.FindFirst(ClaimTypes.Name);
                var email = user.FindFirst("emails") ?? user.FindFirst("email") ?? user.FindFirst("preferred_username");

                if (userId?.Value != null)
                {
                    _logger.LogDebug("B2C User authenticated: {UserId}, {Name}, {Email}", 
                        userId.Value, name?.Value, email?.Value);

                    return new UserIdentity
                    {
                        UserId = userId.Value,
                        Username = name?.Value ?? "B2C User",
                        Email = email?.Value ?? "",
                        IdentityType = UserIdentityType.Authenticated,
                        IsAuthenticated = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get B2C user from JWT token");
            return null;
        }
    }
}
