using PoFastType.Shared.Models;

namespace PoFastType.Client.Services;

public interface IUserService
{
    Task<UserIdentity?> GetCurrentUserIdentityAsync();
    void ClearCache();
    Task RefreshUserAsync();
}
