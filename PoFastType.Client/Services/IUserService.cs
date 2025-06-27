using PoFastType.Shared.Models;

namespace PoFastType.Client.Services;

public interface IUserService
{
    Task<UserProfile?> GetCurrentUserAsync();
    Task<UserIdentity?> GetCurrentUserIdentityAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsRealUserAsync();
    Task<bool> IsAnonymousAsync();
    void ClearCache();
    Task RefreshUserAsync();
}
