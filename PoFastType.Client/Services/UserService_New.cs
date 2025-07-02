using PoFastType.Shared.Models;
using System.Net.Http.Json;

namespace PoFastType.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserService> _logger;
    private UserProfile? _cachedUser;
    private UserIdentity? _cachedIdentity;

    public UserService(HttpClient httpClient, ILogger<UserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserIdentity?> GetCurrentUserIdentityAsync()
    {
        try
        {
            // Return cached identity if available
            if (_cachedIdentity != null)
                return _cachedIdentity;

            // Call the server to get the current user identity
            var response = await _httpClient.GetAsync("/api/user/identity");
            
            if (response.IsSuccessStatusCode)
            {
                _cachedIdentity = await response.Content.ReadFromJsonAsync<UserIdentity>();
                return _cachedIdentity;
            }
            else
            {
                _logger.LogWarning("Failed to get user identity. Status: {StatusCode}", response.StatusCode);
                
                // Fallback to anonymous identity
                _cachedIdentity = new UserIdentity
                {
                    UserId = WellKnownUsers.AnonymousUserId,
                    Username = WellKnownUsers.AnonymousUsername,
                    Email = WellKnownUsers.AnonymousEmail,
                    CreatedAt = DateTime.UtcNow
                };
                
                return _cachedIdentity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user identity");
            
            // Return anonymous identity as fallback
            return new UserIdentity
            {
                UserId = WellKnownUsers.AnonymousUserId,
                Username = WellKnownUsers.AnonymousUsername,
                Email = WellKnownUsers.AnonymousEmail,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        try
        {
            // Return cached user if available
            if (_cachedUser != null)
                return _cachedUser;

            // Always try to get user profile (ANON user)
            var response = await _httpClient.GetAsync("/api/user/profile");
            
            if (response.IsSuccessStatusCode)
            {
                _cachedUser = await response.Content.ReadFromJsonAsync<UserProfile>();
                return _cachedUser;
            }
            else
            {
                _logger.LogWarning("Failed to get user profile. Status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user profile");
            return null;
        }
    }

    public void ClearCache()
    {
        _cachedUser = null;
        _cachedIdentity = null;
    }

    public async Task RefreshUserAsync()
    {
        ClearCache();
        await GetCurrentUserAsync();
        await GetCurrentUserIdentityAsync();
    }
}
