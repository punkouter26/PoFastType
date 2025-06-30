# Azure AD B2C Tenant Creation Steps

## Step 1: Create Azure AD B2C Tenant

1. **Go to Azure Portal**: Open https://portal.azure.com
2. **Search for B2C**: In the search bar, type "Azure AD B2C" and select it
3. **Create Tenant**: Click "Create a tenant" 
4. **Select B2C**: Choose "Azure Active Directory B2C"
5. **Fill Details**:
   - **Organization name**: `PoFastType B2C`
   - **Initial domain name**: `pofasttypeb2c` (this will create pofasttypeb2c.onmicrosoft.com)
   - **Country/Region**: Select your country
   - **Subscription**: Azure subscription 1 (f0504e26-451a-4249-8fb3-46270defdd5b)
   - **Resource group**: Create new named `PoFastType-B2C`
6. **Review and Create**: Click "Review + create" then "Create"

⚠️ **Note**: B2C tenant creation can take 2-3 minutes.

## Step 2: Switch to B2C Tenant

After creation:
1. **Switch Directory**: Click your profile in top right > "Switch directory"
2. **Find B2C Tenant**: Look for "PoFastType B2C" or "pofasttypeb2c.onmicrosoft.com"
3. **Switch**: Click "Switch" to enter the B2C tenant

## Step 3: Create User Flow

1. **Navigate to User Flows**: In B2C tenant, go to "User flows" in left menu
2. **Create New Flow**: Click "+ New user flow"
3. **Select Type**: Choose "Sign up and sign in"
4. **Version**: Select "Recommended"
5. **Configure**:
   - **Name**: `signup_signin` (this creates `B2C_1_signup_signin`)
   - **Identity providers**: Check "Email signup"
   - **User attributes**: Select "Display Name", "Given Name", "Surname"
   - **Application claims**: Select "Display Name", "Given Name", "User's Object ID", "Email Addresses"
6. **Create**: Click "Create"

## Step 4: Register Application

1. **App Registrations**: Go to "App registrations" in left menu
2. **New Registration**: Click "+ New registration"
3. **Configure**:
   - **Name**: `PoFastType`
   - **Supported account types**: "Accounts in any identity provider..."
   - **Redirect URI**: 
     - Platform: "Single-page application (SPA)"
     - URI: `http://localhost:5000/authentication/login-callback`
4. **Register**: Click "Register"
5. **Note the Application (client) ID** - you'll need this!

## Step 5: Configure Redirect URIs

After app registration:
1. **Authentication**: Go to "Authentication" in left menu
2. **Add Platform**: Click "Add platform" > "Single-page application"
3. **Add URIs**:
   - `http://localhost:5000/authentication/login-callback`
   - `https://your-app-name.azurewebsites.net/authentication/login-callback`
4. **Save**: Click "Save"

## What You'll Need for Configuration

After completing these steps, you'll have:
- **Tenant Domain**: `pofasttypeb2c.onmicrosoft.com`
- **User Flow Name**: `B2C_1_signup_signin`
- **Application (Client) ID**: (from app registration)
- **Authority URL**: `https://pofasttypeb2c.b2clogin.com/pofasttypeb2c.onmicrosoft.com/b2c_1_signup_signin`

## Next Steps

Once you complete these steps:
1. Provide me with the **Application (Client) ID**
2. I'll update your app configuration with the correct values
3. We'll test the authentication locally
