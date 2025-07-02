using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

public class UserIdentityService : IUserIdentityService
{
    private readonly ILogger<UserIdentityService> _logger;

    public UserIdentityService(ILogger<UserIdentityService> logger)
    {
        _logger = logger;
    }    public UserIdentity GetCurrentUserIdentity(HttpContext httpContext)
    {
        // Always return ANON user - no authentication
        _logger.LogDebug("Returning ANON user identity");
        return CreateAnonymousIdentity();
    }

    private UserIdentity CreateAnonymousIdentity()
    {
        return new UserIdentity
        {
            UserId = WellKnownUsers.AnonymousUserId,
            Username = WellKnownUsers.AnonymousUsername,
            Email = WellKnownUsers.AnonymousEmail,
            CreatedAt = DateTime.UtcNow
        };
    }
}
