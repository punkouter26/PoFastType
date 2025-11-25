# Grant Key Vault access to the current user for local development
# This script assigns the "Key Vault Secrets User" role to your Azure account
# so you can access Key Vault secrets when running locally

param(
    [string]$ResourceGroup = "PoFastType",
    [string]$KeyVaultName = "pofasttype-kv"
)

Write-Host "Granting Key Vault access for local development..." -ForegroundColor Cyan

# Get current user's object ID
$currentUser = az ad signed-in-user show --query id -o tsv

if (-not $currentUser) {
    Write-Host "Error: Could not get current user. Make sure you're logged in with 'az login'" -ForegroundColor Red
    exit 1
}

Write-Host "Current User Object ID: $currentUser" -ForegroundColor Yellow

# Assign Key Vault Secrets User role
Write-Host "Assigning 'Key Vault Secrets User' role..." -ForegroundColor Yellow
az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee $currentUser `
    --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$ResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Key Vault access granted successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the application locally and it will access Azure Key Vault using your Azure CLI credentials." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Note: It may take a few minutes for the role assignment to propagate." -ForegroundColor Yellow
} else {
    Write-Host "✗ Failed to grant Key Vault access" -ForegroundColor Red
    exit 1
}
