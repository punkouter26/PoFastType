# Adding Google Authentication to PoFastType

## Option 1: Google as External Identity Provider (Advanced)

### Requirements:
1. Google Cloud Console project
2. OAuth 2.0 credentials
3. Entra ID federation setup

### Configuration Steps:
1. **Create Google OAuth App**:
   - Go to Google Cloud Console
   - Create OAuth 2.0 client ID
   - Set redirect URI: `https://login.microsoftonline.com/te/{tenant}/oauth2/authresp`

2. **Configure Entra ID Federation**:
   - Add Google as identity provider in Entra ID
   - Configure claims mapping

## Option 2: Dual Authentication Setup (Recommended)

### Add Google Authentication Package:
```bash
dotnet add PoFastType.Client package Google.AspNetCore.Authentication.Google
```

### Update Program.cs:
```csharp
// Add both Microsoft and Google authentication
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = "your-microsoft-client-id";
        options.ClientSecret = "your-microsoft-client-secret";
    })
    .AddGoogle(options =>
    {
        options.ClientId = "your-google-client-id.apps.googleusercontent.com";
        options.ClientSecret = "your-google-client-secret";
    });
```

## Option 3: Current Capabilities (What Works Now)

### Microsoft Accounts:
- ✅ Personal Microsoft accounts (@outlook.com, @hotmail.com)
- ✅ Work/school accounts
- ✅ Guest user invitations

### Google Users via Guest Access:
1. **Invite Google users** as guests to your tenant
2. **They authenticate** with their Google accounts through Microsoft's B2B flow
3. **No additional setup** required

## Recommended Approach

For PoFastType, I recommend **starting with Option 3** (current capabilities):

1. **Test Microsoft authentication** first
2. **Invite Google users as guests** for immediate Google account support
3. **Later add native Google auth** if needed

This gives you both Microsoft and Google user support without additional complexity.

## Test Commands

```bash
# Test current setup
curl http://localhost:5000/api/user/profile
# Should work with Microsoft authentication

# Test guest user flow
# Invite a Google user as guest in Azure Portal
# They can sign in with their Google account
```
