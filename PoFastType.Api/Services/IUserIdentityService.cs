using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

public interface IUserIdentityService
{
    /// <summary>
    /// Gets the current user identity from the request context
    /// Returns Anonymous user if not authenticated or in development environment
    /// </summary>
    UserIdentity GetCurrentUserIdentity(HttpContext httpContext);
    
    /// <summary>
    /// Checks if the current environment requires authentication
    /// Returns false in development, true in production Azure
    /// </summary>
    bool RequiresAuthentication();
    
    /// <summary>
    /// Creates an anonymous user identity
    /// </summary>
    UserIdentity CreateAnonymousIdentity();
    
    /// <summary>
    /// Checks if we're running in Azure environment
    /// </summary>
    bool IsAzureEnvironment();
}
