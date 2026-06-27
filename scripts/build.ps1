# Build script for Houmao Windows
# Usage: .\scripts\build.ps1 [-Configuration Debug|Release] [-Clean]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Set project paths
$ProjectPath = Join-Path $RootDir "src\Houmao\Houmao.csproj"
$TestProjectPath = Join-Path $RootDir "tests\Houmao.Tests\Houmao.Tests.csproj"

Write-Host "Building Houmao Windows..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Root Directory: $RootDir" -ForegroundColor Yellow

# Clean if requested
if ($Clean) {
    Write-Host "`nCleaning..." -ForegroundColor Yellow
    dotnet clean $ProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Clean failed!" -ForegroundColor Red
        exit 1
    }
}

# Restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

# Build
Write-Host "`nBuilding..." -ForegroundColor Yellow
dotnet build $ProjectPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Yellow
dotnet test $TestProjectPath --configuration $Configuration --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Output: src\Houmao\bin\$Configuration\net9.0-windows\" -ForegroundColor Cyan