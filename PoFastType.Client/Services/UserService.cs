using PoFastType.Shared.Models;

namespace PoFastType.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private UserIdentity? _cachedIdentity;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<UserIdentity?> GetCurrentUserIdentityAsync()
    {
        if (_cachedIdentity != null)
            return Task.FromResult<UserIdentity?>(_cachedIdentity);

        // For anonymous users, return a default identity
        _cachedIdentity = new UserIdentity
        {
            UserId = WellKnownUsers.AnonymousUserId,
            Username = WellKnownUsers.AnonymousUsername,
            Email = WellKnownUsers.AnonymousEmail,
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult<UserIdentity?>(_cachedIdentity);
    }

    public void ClearCache()
    {
        _cachedIdentity = null;
    }

    public Task RefreshUserAsync()
    {
        ClearCache();
        return GetCurrentUserIdentityAsync();
    }
}