# PowerShell script to convert Mermaid (.mmd) files to SVG
# Requires Mermaid CLI to be installed: npm install -g @mermaid-js/mermaid-cli

param(
    [string]$DiagramsPath = ".",
    [switch]$Help
)

function Show-Help {
    Write-Host @"
PoFastType Diagram Generator

This script converts all Mermaid (.mmd) files in the Diagrams folder to SVG format.

PREREQUISITES:
    Node.js and npm must be installed
    Mermaid CLI: npm install -g @mermaid-js/mermaid-cli

USAGE:
    .\generate-svg.ps1 [-DiagramsPath <path>] [-Help]

PARAMETERS:
    -DiagramsPath    Path to the diagrams directory (default: current directory)
    -Help           Show this help message

EXAMPLES:
    .\generate-svg.ps1
    .\generate-svg.ps1 -DiagramsPath "C:\MyProject\Diagrams"

OUTPUT:
    Creates .svg files alongside each .mmd file
    
DIAGRAM FILES:
    - project-dependency-diagram.mmd → project-dependency-diagram.svg
    - class-diagram-domain-entities.mmd → class-diagram-domain-entities.svg
    - sequence-diagram-api-calls.mmd → sequence-diagram-api-calls.svg
    - flowchart-use-case.mmd → flowchart-use-case.svg
    - component-hierarchy-diagram.mmd → component-hierarchy-diagram.svg
"@
}

if ($Help) {
    Show-Help
    exit 0
}

# Set the working directory
$DiagramsPath = Resolve-Path $DiagramsPath -ErrorAction SilentlyContinue
if (-not $DiagramsPath) {
    Write-Error "Directory path '$DiagramsPath' does not exist."
    exit 1
}

Set-Location $DiagramsPath
Write-Host "Working in directory: $DiagramsPath" -ForegroundColor Green

# Check if Mermaid CLI is installed
try {
    $mermaidVersion = mmdc --version 2>$null
    Write-Host "Mermaid CLI version: $mermaidVersion" -ForegroundColor Green
} catch {
    Write-Error @"
Mermaid CLI is not installed or not in PATH.
Please install it using: npm install -g @mermaid-js/mermaid-cli

If you don't have Node.js, install it from: https://nodejs.org/
"@
    exit 1
}

# Find all .mmd files
$mmdFiles = Get-ChildItem -Filter "*.mmd" | Sort-Object Name

if ($mmdFiles.Count -eq 0) {
    Write-Warning "No .mmd files found in the current directory."
    exit 0
}

Write-Host "`nFound $($mmdFiles.Count) Mermaid diagram(s):" -ForegroundColor Cyan
$mmdFiles | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }

# Create SVG configuration file for better output
$configContent = @"
{
  "theme": "default",
  "themeVariables": {
    "fontFamily": "Segoe UI, Arial, sans-serif",
    "fontSize": "14px"
  },
  "flowchart": {
    "htmlLabels": true,
    "curve": "linear"
  },
  "sequence": {
    "diagramMarginX": 50,
    "diagramMarginY": 10,
    "actorMargin": 50,
    "width": 150,
    "height": 65,
    "boxMargin": 10,
    "boxTextMargin": 5,
    "noteMargin": 10,
    "messageMargin": 35
  },
  "classDiagram": {
    "titleTopMargin": 25,
    "diagramPadding": 20
  }
}
"@

$configFile = "mermaid-config.json"
Set-Content -Path $configFile -Value $configContent
Write-Host "`nCreated Mermaid configuration file: $configFile" -ForegroundColor Green

# Convert each .mmd file to .svg
Write-Host "`nConverting diagrams..." -ForegroundColor Cyan
$successCount = 0
$failCount = 0

foreach ($file in $mmdFiles) {
    $inputFile = $file.Name
    $outputFile = $file.BaseName + ".svg"
    
    Write-Host "Converting: $inputFile → $outputFile" -ForegroundColor Yellow
    
    try {
        # Use mermaid CLI to convert with configuration
        $result = mmdc -i $inputFile -o $outputFile -c $configFile -b white --scale 2 2>&1
        
        if (Test-Path $outputFile) {
            $fileSize = (Get-Item $outputFile).Length
            Write-Host "  ✓ Success ($([math]::Round($fileSize/1KB, 1)) KB)" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  ✗ Failed: Output file not created" -ForegroundColor Red
            Write-Host "    Error: $result" -ForegroundColor Red
            $failCount++
        }
    } catch {
        Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

# Clean up configuration file
Remove-Item $configFile -ErrorAction SilentlyContinue

# Summary
Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "CONVERSION SUMMARY" -ForegroundColor Cyan
Write-Host "="*50 -ForegroundColor Cyan
Write-Host "Successfully converted: $successCount diagrams" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "Failed conversions: $failCount diagrams" -ForegroundColor Red
}
Write-Host "Output directory: $DiagramsPath" -ForegroundColor Yellow

if ($successCount -gt 0) {
    Write-Host "`nGenerated SVG files:" -ForegroundColor Green
    Get-ChildItem -Filter "*.svg" | ForEach-Object {
        $size = [math]::Round($_.Length/1KB, 1)
        Write-Host "  📊 $($_.Name) ($size KB)" -ForegroundColor Green
    }
}

Write-Host "`n💡 Tip: You can now include these SVG files in your documentation!" -ForegroundColor Cyan

if ($failCount -eq 0) {
    Write-Host "🎉 All diagrams converted successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some conversions failed. Check the error messages above." -ForegroundColor Yellow
    exit 1
}
