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

    public async Task<UserIdentity?> GetCurrentUserIdentityAsync()
    {
        if (_cachedIdentity != null)
            return _cachedIdentity;

        // For anonymous users, return a default identity
        _cachedIdentity = new UserIdentity
        {
            UserId = WellKnownUsers.AnonymousUserId,
            Username = WellKnownUsers.AnonymousUsername,
            Email = WellKnownUsers.AnonymousEmail,
            CreatedAt = DateTime.UtcNow
        };

        return _cachedIdentity;
    }

    public void ClearCache()
    {
        _cachedIdentity = null;
    }

    public async Task RefreshUserAsync()
    {
        ClearCache();
        await GetCurrentUserIdentityAsync();
    }
}