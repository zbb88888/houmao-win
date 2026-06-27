# Publish script for Houmao Windows
# Usage: .\scripts\publish.ps1 [-Configuration Release] [-SelfContained] [-SingleFile]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SelfContained,
    [switch]$SingleFile
)

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Set project paths
$ProjectPath = Join-Path $RootDir "src\Houmao\Houmao.csproj"
$OutputDir = Join-Path $RootDir "publish"

Write-Host "Publishing Houmao Windows..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Self-Contained: $SelfContained" -ForegroundColor Yellow
Write-Host "Single File: $SingleFile" -ForegroundColor Yellow

# Clean publish directory
if (Test-Path $OutputDir) {
    Write-Host "`nCleaning publish directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}

# Build publish command
$PublishArgs = @(
    "publish",
    $ProjectPath,
    "--configuration", $Configuration,
    "--output", $OutputDir
)

if ($SelfContained) {
    $PublishArgs += "--self-contained"
} else {
    $PublishArgs += "--no-self-contained"
}

if ($SingleFile) {
    $PublishArgs += "/p:PublishSingleFile=true"
}

# Publish
Write-Host "`nPublishing..." -ForegroundColor Yellow
dotnet @PublishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Create zip package
Write-Host "`nCreating zip package..." -ForegroundColor Yellow
$ZipPath = Join-Path $RootDir "Houmao-Windows-$Configuration.zip"
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipPath -Force

Write-Host "`nPublish completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
Write-Host "Zip package: $ZipPath" -ForegroundColor Cyan

# List files
Write-Host "`nPublished files:" -ForegroundColor Yellow
Get-ChildItem -Path $OutputDir | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor Gray
}