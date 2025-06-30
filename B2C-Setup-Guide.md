# Azure AD B2C Setup Guide for PoFastType

## Overview
This guide will help you set up Azure AD B2C (Business to Consumer) for the PoFastType application, replacing Azure Easy Auth with a modern, scalable identity solution.

## Step 1: Create Azure AD B2C Tenant

1. **Navigate to Azure Portal**: https://portal.azure.com
2. **Create B2C Tenant**:
   - Search for "Azure AD B2C" in the search bar
   - Click "Create a B2C tenant"
   - Choose "Create a new Azure AD B2C Tenant"
   - Fill in the details:
     - Organization name: `PoFastType`
     - Initial domain name: `pofasttypeb2c` (this will create pofasttypeb2c.onmicrosoft.com)
     - Country/Region: Select your region
     - Subscription: Your Azure subscription
     - Resource group: Create new or use existing

## Step 2: Configure User Flows

1. **Navigate to your B2C tenant**
2. **Create Sign-up/Sign-in User Flow**:
   - Go to "User flows" in the left menu
   - Click "New user flow"
   - Select "Sign up and sign in"
   - Choose "Recommended" version
   - Name: `B2C_1_signup_signin`
   - Identity providers:
     - ✅ Email signup
     - ✅ Microsoft Account (optional)
     - ✅ Google (optional - requires setup)
     - ✅ Facebook (optional - requires setup)
   - User attributes and claims:
     - **Collect attributes**: Email Address, Given Name, Surname
     - **Return claims**: Email Addresses, Given Name, Surname, User's Object ID
   - Click "Create"

## Step 3: Register Application

1. **App registrations** in your B2C tenant
2. **New registration**:
   - Name: `PoFastType-App`
   - Supported account types: "Accounts in any identity provider or organizational directory"
   - Redirect URI:
     - Type: "Single-page application (SPA)"
     - URLs:
       - `http://localhost:5000/authentication/login-callback` (development)
       - `https://pofasttype.azurewebsites.net/authentication/login-callback` (production)

3. **Configure Authentication**:
   - Go to "Authentication" in your app
   - Add additional redirect URIs if needed
   - Configure logout URLs:
     - `http://localhost:5000/` (development)
     - `https://pofasttype.azurewebsites.net/` (production)
   - Enable "Access tokens" and "ID tokens"

4. **Note down these values**:
   - Application (client) ID: `[YOUR_B2C_CLIENT_ID]`
   - Directory (tenant) ID: `[YOUR_B2C_TENANT_ID]`

## Step 4: Update Application Configuration

### Frontend Configuration

Update the following files in your PoFastType application:

**PoFastType.Client/wwwroot/appsettings.Development.json:**
```json
{
  "ApiBaseAddress": "http://localhost:5000/",
  "AzureAdB2C": {
    "Authority": "https://pofasttypeb2c.b2clogin.com/pofasttypeb2c.onmicrosoft.com/b2c_1_signup_signin",
    "ClientId": "[YOUR_B2C_CLIENT_ID]",
    "ValidateAuthority": false,
    "RedirectUri": "http://localhost:5000/authentication/login-callback",
    "KnownAuthorities": ["pofasttypeb2c.b2clogin.com"],
    "PostLogoutRedirectUri": "http://localhost:5000/"
  }
}
```

**PoFastType.Client/wwwroot/appsettings.json:**
```json
{
  "AzureAdB2C": {
    "Authority": "https://pofasttypeb2c.b2clogin.com/pofasttypeb2c.onmicrosoft.com/b2c_1_signup_signin",
    "ClientId": "[YOUR_B2C_CLIENT_ID]",
    "ValidateAuthority": false,
    "RedirectUri": "https://pofasttype.azurewebsites.net/authentication/login-callback",
    "KnownAuthorities": ["pofasttypeb2c.b2clogin.com"],
    "PostLogoutRedirectUri": "https://pofasttype.azurewebsites.net/"
  },
  "ApiBaseAddress": "https://pofasttype.azurewebsites.net"
}
```

### Backend Configuration

The backend has already been configured to support B2C JWT tokens. Make sure the authority and audience are correct in `Program.cs`.

## Step 5: Test Authentication Flow

1. **Start the application**: `dotnet run`
2. **Navigate to**: `http://localhost:5000`
3. **Click "Sign In"**: Should redirect to B2C login page
4. **Test sign-up**: Create a new account
5. **Test sign-in**: Login with existing account
6. **Verify claims**: Check that user information is displayed correctly

## Step 6: Optional - Configure Social Identity Providers

### Microsoft Account (Already configured by default)
Microsoft personal accounts are supported out of the box.

### Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create OAuth 2.0 credentials
3. Add authorized redirect URI: `https://pofasttypeb2c.b2clogin.com/pofasttypeb2c.onmicrosoft.com/oauth2/authresp`
4. In B2C, go to "Identity providers" → "Google" and configure with your Google credentials

### Facebook Login
1. Go to [Facebook Developers](https://developers.facebook.com)
2. Create a Facebook app
3. Add Facebook Login product
4. Configure OAuth redirect URI: `https://pofasttypeb2c.b2clogin.com/pofasttypeb2c.onmicrosoft.com/oauth2/authresp`
5. In B2C, go to "Identity providers" → "Facebook" and configure

## Step 7: Deploy to Azure (Removing Easy Auth)

1. **Disable Easy Auth** in your Azure App Service:
   - Go to your App Service in Azure Portal
   - Navigate to "Authentication"
   - Turn OFF "App Service Authentication"

2. **Deploy your updated application**:
   ```bash
   dotnet publish
   # Deploy to Azure App Service
   ```

3. **Test production authentication** at your Azure URL

## Benefits of B2C vs Easy Auth

✅ **More Control**: Full control over user experience and UI
✅ **Social Providers**: Easy integration with Google, Facebook, etc.
✅ **Custom Policies**: Advanced customization options
✅ **Scalability**: Better performance for high-traffic applications
✅ **Modern Standards**: Uses latest OAuth 2.0 and OpenID Connect standards
✅ **Multi-tenant**: Support for multiple user directories
✅ **Local Development**: Works seamlessly in localhost

## Troubleshooting

### Common Issues:
1. **CORS Errors**: Ensure redirect URIs are correctly configured
2. **Token Validation**: Check authority URLs match exactly
3. **Claims Missing**: Verify user flow returns required claims
4. **Localhost Issues**: Ensure B2C app registration includes localhost URLs

### Debug Tips:
1. Use browser developer tools to inspect network requests
2. Check B2C audit logs in Azure Portal
3. Enable detailed logging in your application
4. Test authentication with the standalone test page: `/msal-test.html`

## Security Considerations

1. **Always use HTTPS in production**
2. **Configure proper CORS policies**
3. **Set appropriate token lifetimes**
4. **Enable conditional access policies if needed**
5. **Monitor authentication logs**

## Next Steps

After B2C is working:
1. Configure custom branding for login pages
2. Set up custom domains (optional)
3. Implement advanced user flows (password reset, profile editing)
4. Add multi-factor authentication
5. Configure advanced security policies
