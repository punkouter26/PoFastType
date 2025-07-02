using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

public interface IUserIdentityService
{
    /// <summary>
    /// Gets the current user identity - always returns ANON user
    /// </summary>
    UserIdentity GetCurrentUserIdentity(HttpContext httpContext);
}
