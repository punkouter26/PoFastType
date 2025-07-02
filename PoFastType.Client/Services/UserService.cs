using PoFastType.Shared.Models;

namespace PoFastType.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private UserProfile? _cachedProfile;
    private UserIdentity? _cachedIdentity;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        if (_cachedProfile != null)
            return _cachedProfile;

        // For anonymous users, return a default profile
        _cachedProfile = new UserProfile
        {
            UserId = WellKnownUsers.AnonymousUserId,
            Username = WellKnownUsers.AnonymousUsername,
            Email = WellKnownUsers.AnonymousEmail,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            TotalTestsCompleted = 0,
            BestNetWPM = 0,
            AverageNetWPM = 0,
            AverageAccuracy = 0,
            TotalTypingTime = TimeSpan.Zero,
            ProblemKeys = new Dictionary<string, int>()
        };

        return _cachedProfile;
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
        _cachedProfile = null;
        _cachedIdentity = null;
    }

    public async Task RefreshUserAsync()
    {
        ClearCache();
        await GetCurrentUserAsync();
        await GetCurrentUserIdentityAsync();
    }
}