# TaskBar Widget - Build Script
# Erstellt eine ausführbare Version des Widgets

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TaskBar Widget - Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Überprüfe .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Build Release Version
Write-Host "Building release version..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Executable location:" -ForegroundColor Yellow
Write-Host "  bin\Release\net8.0-windows\win-x64\publish\TaskBarWidget.exe" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run install.ps1 to install the widget" -ForegroundColor White
Write-Host "  2. Or copy the .exe file manually to your desired location" -ForegroundColor White
Write-Host ""

# Öffne Output-Ordner
$outputPath = "bin\Release\net8.0-windows\win-x64\publish"
if (Test-Path $outputPath) {
    Write-Host "Opening output folder..." -ForegroundColor Yellow
    explorer.exe $outputPath
}
