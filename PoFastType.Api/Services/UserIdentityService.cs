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
            // In development environment, check if we have a development user from the auth handler
            if (_environment.IsDevelopment())
            {
                var developmentUser = GetDevelopmentUser(httpContext);
                if (developmentUser != null)
                {
                    _logger.LogDebug("Development environment: returning development user - {UserId}", developmentUser.UserId);
                    return developmentUser;
                }
                
                _logger.LogDebug("Development environment: returning anonymous user");
                return CreateAnonymousIdentity();
            }

            // In Azure, check if user is authenticated via Easy Auth
            if (IsAzureEnvironment())
            {
                var authenticatedUser = GetEasyAuthUser(httpContext);
                if (authenticatedUser != null)
                {
                    _logger.LogDebug("Azure environment: authenticated user found - {UserId}", authenticatedUser.UserId);
                    return authenticatedUser;
                }
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
    }

    private UserIdentity? GetEasyAuthUser(HttpContext httpContext)
    {
        try
        {
            // Check for Easy Auth headers
            var userIdHeader = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault();
            var userNameHeader = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault();
            var userPrincipalHeader = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdHeader))
            {
                return null;
            }

            var username = userNameHeader ?? "User";
            var email = "";

            // Try to extract email from the principal if available
            if (!string.IsNullOrEmpty(userPrincipalHeader))
            {
                try
                {
                    var principalBytes = Convert.FromBase64String(userPrincipalHeader);
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
                                
                                if (claimType == "email" || claimType == "preferred_username")
                                {
                                    email = claimValue ?? "";
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse user principal header");
                }
            }

            return new UserIdentity
            {
                UserId = userIdHeader,
                Username = username,
                Email = email,
                IdentityType = UserIdentityType.Authenticated,
                IsAuthenticated = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Easy Auth user");
            return null;
        }
    }

    private UserIdentity? GetDevelopmentUser(HttpContext httpContext)
    {
        try
        {
            // Check if the request has authentication claims from the development auth handler
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = user.FindFirst(ClaimTypes.Name)?.Value;
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                var identityType = user.FindFirst("identity_type")?.Value;

                // If this is a development user (not anonymous), return it
                if (!string.IsNullOrEmpty(userId) && identityType == "development")
                {
                    return new UserIdentity
                    {
                        UserId = userId,
                        Username = username ?? "Development User",
                        Email = email ?? "dev@pofasttype.com",
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
            _logger.LogWarning(ex, "Failed to get development user");
            return null;
        }
    }
}
