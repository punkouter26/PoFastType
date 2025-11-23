# Start Azurite for local Azure Storage emulation

Write-Host "Starting Azurite (Azure Storage Emulator)..." -ForegroundColor Green

# Check if Azurite is installed
$azuriteInstalled = Get-Command azurite -ErrorAction SilentlyContinue

if (-not $azuriteInstalled) {
    Write-Host "Azurite is not installed. Installing via npm..." -ForegroundColor Yellow
    npm install -g azurite
}

# Start Azurite with default settings
# - Blob storage on port 10000
# - Queue storage on port 10001
# - Table storage on port 10002
Write-Host "Starting Azurite..." -ForegroundColor Cyan
Start-Process -FilePath "azurite" -ArgumentList "--silent", "--location", "azurite-data", "--debug", "azurite-debug.log"

Write-Host "Azurite started successfully!" -ForegroundColor Green
Write-Host "Blob endpoint: http://127.0.0.1:10000" -ForegroundColor White
Write-Host "Queue endpoint: http://127.0.0.1:10001" -ForegroundColor White
Write-Host "Table endpoint: http://127.0.0.1:10002" -ForegroundColor White
