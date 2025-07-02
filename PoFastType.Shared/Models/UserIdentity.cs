namespace PoFastType.Shared.Models;

public class UserIdentity
{
    public string UserId { get; set; } = "ANON";
    public string Username { get; set; } = "ANON";
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class WellKnownUsers
{
    public const string AnonymousUserId = "ANON";
    public const string AnonymousUsername = "ANON";
    public const string AnonymousEmail = "";
}
