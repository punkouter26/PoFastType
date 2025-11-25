# Validate Azure Key Vault deployment and configuration
# This script checks that everything is properly configured

param(
    [string]$ResourceGroup = "PoFastType",
    [string]$KeyVaultName = "pofasttype-kv",
    [string]$AppServiceName = "PoFastType"
)

Write-Host "=== Azure Key Vault Deployment Validation ===" -ForegroundColor Cyan
Write-Host ""

# Function to check and display result
function Test-Resource {
    param(
        [string]$Name,
        [scriptblock]$Test
    )
    
    Write-Host "Checking: $Name..." -NoNewline
    try {
        $result = & $Test
        if ($result) {
            Write-Host " ✓" -ForegroundColor Green
            return $true
        } else {
            Write-Host " ✗" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host " ✗ (Error: $($_.Exception.Message))" -ForegroundColor Red
        return $false
    }
}

# Check if logged in to Azure
Write-Host "Step 1: Azure Login Status" -ForegroundColor Yellow
$loggedIn = Test-Resource "Azure CLI Login" {
    $account = az account show 2>$null
    return $null -ne $account
}

if (-not $loggedIn) {
    Write-Host "Please run 'az login' first" -ForegroundColor Red
    exit 1
}

# Check Resource Group exists
Write-Host "`nStep 2: Resource Group" -ForegroundColor Yellow
$rgExists = Test-Resource "Resource Group '$ResourceGroup'" {
    $rg = az group show --name $ResourceGroup 2>$null
    return $null -ne $rg
}

if (-not $rgExists) {
    Write-Host "Resource group not found. Run 'azd up' to deploy infrastructure." -ForegroundColor Red
    exit 1
}

# Check Key Vault exists
Write-Host "`nStep 3: Key Vault" -ForegroundColor Yellow
$kvExists = Test-Resource "Key Vault '$KeyVaultName'" {
    $kv = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup 2>$null
    return $null -ne $kv
}

# Check Key Vault RBAC is enabled
if ($kvExists) {
    Test-Resource "Key Vault RBAC enabled" {
        $kv = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup --query "properties.enableRbacAuthorization" -o tsv
        return $kv -eq "true"
    }
}

# Check Secrets exist
Write-Host "`nStep 4: Key Vault Secrets" -ForegroundColor Yellow
if ($kvExists) {
    Test-Resource "Secret: AzureTableStorage--ConnectionString" {
        $secret = az keyvault secret show --vault-name $KeyVaultName --name "AzureTableStorage--ConnectionString" 2>$null
        return $null -ne $secret
    }
    
    Test-Resource "Secret: ApplicationInsights--ConnectionString" {
        $secret = az keyvault secret show --vault-name $KeyVaultName --name "ApplicationInsights--ConnectionString" 2>$null
        return $null -ne $secret
    }
}

# Check App Service exists
Write-Host "`nStep 5: App Service" -ForegroundColor Yellow
$appExists = Test-Resource "App Service '$AppServiceName'" {
    $app = az webapp show --name $AppServiceName --resource-group $ResourceGroup 2>$null
    return $null -ne $app
}

# Check App Service Managed Identity
if ($appExists) {
    Test-Resource "System-Assigned Managed Identity" {
        $identity = az webapp identity show --name $AppServiceName --resource-group $ResourceGroup --query "principalId" -o tsv 2>$null
        return -not [string]::IsNullOrEmpty($identity)
    }
}

# Check App Service app settings
Write-Host "`nStep 6: App Service Configuration" -ForegroundColor Yellow
if ($appExists) {
    Test-Resource "App Setting: ASPNETCORE_ENVIRONMENT" {
        $setting = az webapp config appsettings list --name $AppServiceName --resource-group $ResourceGroup --query "[?name=='ASPNETCORE_ENVIRONMENT'].value" -o tsv 2>$null
        return $setting -eq "Production"
    }
    
    Test-Resource "App Setting: AzureKeyVault__VaultUri" {
        $setting = az webapp config appsettings list --name $AppServiceName --resource-group $ResourceGroup --query "[?name=='AzureKeyVault__VaultUri'].value" -o tsv 2>$null
        return -not [string]::IsNullOrEmpty($setting)
    }
    
    Test-Resource "App Setting: Key Vault reference for Storage" {
        $setting = az webapp config appsettings list --name $AppServiceName --resource-group $ResourceGroup --query "[?name=='AzureTableStorage__ConnectionString'].value" -o tsv 2>$null
        return $setting -match "@Microsoft.KeyVault"
    }
}

# Check Role Assignments
Write-Host "`nStep 7: RBAC Role Assignments" -ForegroundColor Yellow
if ($kvExists -and $appExists) {
    $appIdentity = az webapp identity show --name $AppServiceName --resource-group $ResourceGroup --query "principalId" -o tsv 2>$null
    
    if ($appIdentity) {
        Test-Resource "App Service → Key Vault (Secrets User)" {
            $kvId = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup --query "id" -o tsv
            $role = az role assignment list --assignee $appIdentity --scope $kvId --query "[?roleDefinitionName=='Key Vault Secrets User']" -o tsv 2>$null
            return -not [string]::IsNullOrEmpty($role)
        }
        
        Test-Resource "App Service → Storage (Table Data Contributor)" {
            $storageAccounts = az storage account list --resource-group $ResourceGroup --query "[?contains(name, 'pofasttype')].id" -o tsv
            foreach ($storageId in $storageAccounts) {
                $role = az role assignment list --assignee $appIdentity --scope $storageId --query "[?roleDefinitionName=='Storage Table Data Contributor']" -o tsv 2>$null
                if (-not [string]::IsNullOrEmpty($role)) {
                    return $true
                }
            }
            return $false
        }
    }
}

# Check current user access
Write-Host "`nStep 8: Your User Access" -ForegroundColor Yellow
if ($kvExists) {
    $currentUser = az ad signed-in-user show --query id -o tsv 2>$null
    if ($currentUser) {
        Test-Resource "Your access to Key Vault (Secrets User)" {
            $kvId = az keyvault show --name $KeyVaultName --resource-group $ResourceGroup --query "id" -o tsv
            $role = az role assignment list --assignee $currentUser --scope $kvId --query "[?roleDefinitionName=='Key Vault Secrets User']" -o tsv 2>$null
            return -not [string]::IsNullOrEmpty($role)
        }
    }
}

# Test App Service health
Write-Host "`nStep 9: Application Health" -ForegroundColor Yellow
if ($appExists) {
    Test-Resource "App Service running" {
        $state = az webapp show --name $AppServiceName --resource-group $ResourceGroup --query "state" -o tsv
        return $state -eq "Running"
    }
    
    Write-Host "Testing health endpoint..." -NoNewline
    try {
        $url = "https://$AppServiceName.azurewebsites.net/api/health"
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 30 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host " ✓" -ForegroundColor Green
        } else {
            Write-Host " ✗ (Status: $($response.StatusCode))" -ForegroundColor Red
        }
    } catch {
        Write-Host " ✗ (Error: $($_.Exception.Message))" -ForegroundColor Red
    }
}

Write-Host "`n=== Validation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. If any checks failed, run 'azd up' to deploy/update infrastructure"
Write-Host "  2. If user access failed, run '.\scripts\grant-keyvault-access.ps1'"
Write-Host "  3. Test locally with 'dotnet run --project src/PoFastType.Api'"
Write-Host "  4. Check App Service logs if health check failed"
Write-Host ""
