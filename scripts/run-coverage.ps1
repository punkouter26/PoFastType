# Run code coverage analysis

Write-Host "Running code coverage analysis..." -ForegroundColor Green

# Clean previous coverage data
if (Test-Path "docs/coverage") {
    Remove-Item -Recurse -Force "docs/coverage/*"
}

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./docs/coverage/raw

# Generate HTML report (requires reportgenerator tool)
$reportGeneratorInstalled = Get-Command reportgenerator -ErrorAction SilentlyContinue

if ($reportGeneratorInstalled) {
    reportgenerator `
        -reports:"docs/coverage/raw/**/coverage.cobertura.xml" `
        -targetdir:"docs/coverage" `
        -reporttypes:"Html;TextSummary"
    
    Write-Host ""
    Write-Host "Coverage report generated at: docs/coverage/index.html" -ForegroundColor Green
    Get-Content "docs/coverage/Summary.txt"
}
else {
    Write-Host ""
    Write-Host "Install reportgenerator for HTML reports:" -ForegroundColor Yellow
    Write-Host "dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor White
}

Write-Host ""
Write-Host "Coverage analysis complete!" -ForegroundColor Green
