namespace PoFastType.Shared.Models;

public class UserIdentity
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserIdentityType IdentityType { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAnonymous => IdentityType == UserIdentityType.Anonymous;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserIdentityType
{
    Anonymous,      // 'Anon' user - either local development or anonymous Azure user
    Authenticated   // Real user authenticated via Azure Easy Auth
}

public static class WellKnownUsers
{
    public const string AnonymousUserId = "anon";
    public const string AnonymousUsername = "Anon";
    public const string AnonymousEmail = "";
}
