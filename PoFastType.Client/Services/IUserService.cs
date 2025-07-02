using PoFastType.Shared.Models;

namespace PoFastType.Client.Services;

public interface IUserService
{
    Task<UserProfile?> GetCurrentUserAsync();
    Task<UserIdentity?> GetCurrentUserIdentityAsync();
    void ClearCache();
    Task RefreshUserAsync();
}
