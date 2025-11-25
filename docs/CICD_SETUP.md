# CI/CD Setup Guide

## Overview

PoFastType uses GitHub Actions with Azure Developer CLI (azd) for automated deployment using **Federated Credentials (OIDC)** for secure, secret-less authentication.

## Prerequisites

1. Azure subscription
2. Azure CLI installed locally
3. GitHub repository with admin access
4. Azure Developer CLI (azd) installed

## Step 1: Create Azure Service Principal with Federated Credentials

Run these commands in your terminal:

```powershell
# Set variables
$subscriptionId = "f0504e26-451a-4249-8fb3-46270defdd5b"
$resourceGroup = "PoFastType"
$appName = "PoFastType-GitHub-Actions"
$githubOrg = "punkouter26"
$githubRepo = "PoFastType"

# Login to Azure
az login

# Set subscription
az account set --subscription $subscriptionId

# Create service principal with federated credential
az ad sp create-for-rbac `
  --name $appName `
  --role contributor `
  --scopes /subscriptions/$subscriptionId/resourceGroups/$resourceGroup `
  --sdk-auth

# Note the output - you'll need:
# - appId (AZURE_CLIENT_ID)
# - tenant (AZURE_TENANT_ID)
```

## Step 2: Configure Federated Credential for GitHub Actions

```powershell
# Get the service principal's appId from previous step
$appId = "<YOUR_APP_ID_FROM_STEP_1>"

# Create federated credential for main/master branch
az ad app federated-credential create `
  --id $appId `
  --parameters "{
    \"name\": \"PoFastType-GitHub-Master\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:$githubOrg/${githubRepo}:ref:refs/heads/master\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# Optional: Create federated credential for pull requests
az ad app federated-credential create `
  --id $appId `
  --parameters "{
    \"name\": \"PoFastType-GitHub-PR\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:$githubOrg/${githubRepo}:pull_request\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
```

## Step 3: Configure GitHub Repository Variables

Go to your GitHub repository settings: `Settings > Secrets and variables > Actions > Variables`

Add these **Variables** (not secrets - they're not sensitive):

| Variable Name | Value | Description |
|--------------|-------|-------------|
| `AZURE_CLIENT_ID` | `<appId from Step 1>` | Service principal application ID |
| `AZURE_TENANT_ID` | `5da66fe6-bd58-4517-8727-deebc8525dcb` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | `f0504e26-451a-4249-8fb3-46270defdd5b` | Azure subscription ID |
| `AZURE_ENV_NAME` | `PoFastType` | Azure Developer CLI environment name |
| `AZURE_LOCATION` | `canadacentral` | Azure region for deployment |

## Step 4: Verify Workflow Configuration

The workflow file `.github/workflows/azure-dev.yml` should have:

- ✅ Correct .NET version (10.0.x)
- ✅ Federated credential authentication
- ✅ Provision and deploy steps
- ✅ Proper permissions (`id-token: write`)

## Step 5: Test the Deployment

1. **Push to trigger workflow:**
   ```bash
   git add .
   git commit -m "Test CI/CD deployment"
   git push origin master
   ```

2. **Monitor workflow:**
   - Go to Actions tab in GitHub
   - Watch the workflow run
   - Check for any errors

3. **Verify deployment:**
   - Visit: https://pofasttype.azurewebsites.net
   - Check health: https://pofasttype.azurewebsites.net/api/health

## Troubleshooting

### Error: "AADSTS70021: No matching federated identity record found"

**Solution:** Verify the federated credential subject matches exactly:
```
repo:punkouter26/PoFastType:ref:refs/heads/master
```

### Error: "Forbidden - AuthorizationFailed"

**Solution:** Ensure service principal has `Contributor` role on the resource group:
```powershell
az role assignment create `
  --assignee $appId `
  --role Contributor `
  --scope /subscriptions/$subscriptionId/resourceGroups/$resourceGroup
```

### Error: ".NET SDK not found"

**Solution:** Update workflow to match global.json (.NET 10.0.x)

### Error: "azd provision failed"

**Solution:** 
1. Check that all GitHub variables are set correctly
2. Verify service principal has proper permissions
3. Ensure resource group exists in Azure
4. Check that location matches existing resources

## Security Benefits

✅ **No secrets stored** - Uses OIDC tokens instead of service principal passwords
✅ **Short-lived tokens** - Credentials expire after workflow completes
✅ **Auditable** - All authentication tracked in Azure AD
✅ **Scoped permissions** - Limited to specific resource group

## Workflow Details

The GitHub Actions workflow:

1. **Triggers on:**
   - Push to `master` branch
   - Manual workflow dispatch

2. **Steps:**
   - Checkout code
   - Setup .NET 10.0
   - Install Azure Developer CLI
   - Authenticate with federated credentials
   - Provision infrastructure (Bicep)
   - Deploy application code

3. **Deployment time:** ~3-5 minutes

## Additional Resources

- [Azure Federated Credentials Documentation](https://learn.microsoft.com/azure/developer/github/connect-from-azure)
- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
