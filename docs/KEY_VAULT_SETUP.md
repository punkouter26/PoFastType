# Azure Key Vault Setup

## Overview
This project now uses Azure Key Vault to store sensitive configuration values (connection strings, API keys, etc.) for both local development and production environments.

## Architecture Changes

### What Changed?
1. **Added Azure Key Vault** to the infrastructure (already existed but now properly configured)
2. **Removed User-Assigned Managed Identity** - Now using System-Assigned Managed Identity on the App Service
3. **Configured RBAC Roles**:
   - App Service has "Key Vault Secrets User" role
   - App Service has "Storage Table Data Contributor" role
4. **Secrets Stored in Key Vault**:
   - `AzureTableStorage--ConnectionString`
   - `ApplicationInsights--ConnectionString`

### Local Development Setup

#### Prerequisites
- Azure CLI installed
- Logged into Azure: `az login`
- Appropriate permissions on the PoFastType resource group

#### Grant Yourself Access
Run this script to grant your user account access to Key Vault:

```powershell
.\scripts\grant-keyvault-access.ps1
```

This assigns the "Key Vault Secrets User" role to your Azure account.

#### How It Works Locally
- The app uses `DefaultAzureCredential` which automatically tries:
  1. Environment variables
  2. Managed Identity (in Azure)
  3. Visual Studio credentials
  4. Azure CLI credentials (`az login`)
  5. Azure PowerShell
  
- When running locally, it will use your Azure CLI credentials to access Key Vault
- Falls back to local `appsettings.json` values if Key Vault is unavailable

### Production Environment

#### How It Works in Azure
- App Service uses its System-Assigned Managed Identity
- Automatically has access via RBAC roles assigned in Bicep
- App settings reference Key Vault secrets using special syntax:
  ```
  @Microsoft.KeyVault(SecretUri=https://pofasttype-kv.vault.azure.net/secrets/AzureTableStorage--ConnectionString/)
  ```

## Deployment

### Deploy Infrastructure
```powershell
# Deploy the updated Bicep templates
azd up
```

This will:
1. Create/update the Key Vault
2. Store secrets in Key Vault
3. Assign RBAC roles to the App Service
4. Configure App Service to reference Key Vault secrets

### Verify Deployment
After deployment, check:
1. Key Vault exists: https://portal.azure.com → PoFastType resource group → pofasttype-kv
2. Secrets are created:
   - `AzureTableStorage--ConnectionString`
   - `ApplicationInsights--ConnectionString`
3. App Service has System-Assigned Managed Identity enabled
4. Role assignments exist (under Key Vault → Access control (IAM))

## Security Benefits

1. **No Secrets in Code**: Connection strings never stored in appsettings (except local dev fallback)
2. **RBAC-Based Access**: Modern role-based permissions instead of access policies
3. **Managed Identity**: No credentials to manage or rotate
4. **Audit Trail**: All Key Vault access is logged in Azure Monitor
5. **Secret Rotation**: Secrets can be rotated in Key Vault without code changes

## Troubleshooting

### Local Development Issues

**Problem**: "Failed to configure Azure Key Vault"
- **Solution**: Run `az login` and ensure you have access to the resource group
- **Solution**: Run `.\scripts\grant-keyvault-access.ps1` to grant yourself access
- **Solution**: Wait a few minutes for RBAC role assignments to propagate

**Problem**: App works locally with Azurite but not with Azure resources
- **Solution**: Your local `appsettings.json` has `UseDevelopmentStorage=true`
- **Solution**: Key Vault secrets override this in production
- **Solution**: To test with Azure resources locally, temporarily change connection string or disable Key Vault URI

### Production Issues

**Problem**: App Service can't access Key Vault
- **Solution**: Check that System-Assigned Managed Identity is enabled on App Service
- **Solution**: Verify role assignment: Key Vault → Access control (IAM) → Role assignments
- **Solution**: Check App Service logs for authentication errors

**Problem**: Secrets not found
- **Solution**: Verify secrets exist in Key Vault with correct names (use `--` not `:` in secret names)
- **Solution**: Check App Service configuration references correct Key Vault URI

## Cost Impact

- **Key Vault**: ~$0.03/10,000 operations (very minimal for this app)
- **Removed**: User-Assigned Managed Identity ($0/month - free, but removed for simplicity)
- **Net Impact**: Negligible cost increase, significant security improvement

## Best Practices

1. **Never commit secrets to Git** - Use Key Vault or User Secrets for local dev
2. **Use RBAC over Access Policies** - Modern, more granular, better auditing
3. **Principle of Least Privilege** - Grant only "Secrets User" not "Secrets Officer"
4. **Regular Secret Rotation** - Even with Key Vault, rotate secrets periodically
5. **Monitor Access** - Use Application Insights to monitor Key Vault access patterns

## Next Steps

1. Add more secrets as needed (API keys, database passwords, etc.)
2. Set up secret expiration and rotation policies
3. Configure Key Vault network restrictions (if needed)
4. Add additional RBAC roles as new resources are added
