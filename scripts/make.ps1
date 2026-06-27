# Houmao Windows Build Script
# Usage: .\scripts\make.ps1 <command>
#
# Commands:
#   build       - Build Debug
#   release     - Build Release
#   test        - Run tests
#   publish     - Publish single-file executable
#   clean       - Clean build artifacts
#   check       - Check project structure
#   install     - Install dependencies and build
#   run         - Build and run
#   format      - Format code
#   restore     - Restore packages
#   help        - Show help

param(
    [Parameter(Position=0)]
    [ValidateSet("build", "release", "test", "publish", "clean", "check", "install", "run", "format", "restore", "help")]
    [string]$Command = "help"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

function Show-Help {
    Write-Host @"

Houmao Windows Build System
==========================

Usage: .\scripts\make.ps1 <command>

Commands:
  build       Build Debug configuration
  release     Build Release configuration
  test        Run unit tests
  publish     Publish single-file executable
  clean       Clean build artifacts
  check       Check project structure and dependencies
  install     Check and build (Debug)
  run         Build and run the application
  format      Format code using dotnet format
  restore     Restore NuGet packages
  help        Show this help message

Examples:
  .\scripts\make.ps1 build        # Build Debug
  .\scripts\make.ps1 release      # Build Release
  .\scripts\make.ps1 test         # Run tests
  .\scripts\make.ps1 publish      # Create publish package
  .\scripts\make.ps1 run          # Build and run

"@
}

function Invoke-Build {
    param([string]$Config = "Debug")
    Write-Host "Building $Config..." -ForegroundColor Cyan
    & "$ScriptDir\build.ps1" -Configuration $Config
}

function Invoke-Test {
    Write-Host "Running tests..." -ForegroundColor Cyan
    $testProject = Join-Path $RootDir "tests\Houmao.Tests\Houmao.Tests.csproj"
    dotnet test $testProject --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Tests passed!" -ForegroundColor Green
}

function Invoke-Publish {
    Write-Host "Publishing..." -ForegroundColor Cyan
    & "$ScriptDir\publish.ps1" -Configuration Release -SingleFile
}

function Invoke-Clean {
    Write-Host "Cleaning..." -ForegroundColor Cyan
    
    $srcProject = Join-Path $RootDir "src\Houmao\Houmao.csproj"
    $testProject = Join-Path $RootDir "tests\Houmao.Tests\Houmao.Tests.csproj"
    
    dotnet clean $srcProject --verbosity quiet 2>$null
    dotnet clean $testProject --verbosity quiet 2>$null
    
    $publishDir = Join-Path $RootDir "publish"
    if (Test-Path $publishDir) {
        Remove-Item -Recurse -Force $publishDir
    }
    
    # Remove zip files
    Get-ChildItem -Path $RootDir -Filter "*.zip" | Remove-Item -Force
    
    Write-Host "Clean complete!" -ForegroundColor Green
}

function Invoke-Check {
    & "$ScriptDir\check.ps1"
}

function Invoke-Install {
    Invoke-Check
    Invoke-Build -Config "Debug"
    Write-Host "`nInstallation complete!" -ForegroundColor Green
}

function Invoke-Run {
    Invoke-Build -Config "Debug"
    Write-Host "`nRunning Houmao..." -ForegroundColor Cyan
    $projectPath = Join-Path $RootDir "src\Houmao\Houmao.csproj"
    dotnet run --project $projectPath
}

function Invoke-Format {
    Write-Host "Formatting code..." -ForegroundColor Cyan
    $projectPath = Join-Path $RootDir "src\Houmao\Houmao.csproj"
    dotnet format $projectPath
    Write-Host "Format complete!" -ForegroundColor Green
}

function Invoke-Restore {
    Write-Host "Restoring packages..." -ForegroundColor Cyan
    $srcProject = Join-Path $RootDir "src\Houmao\Houmao.csproj"
    $testProject = Join-Path $RootDir "tests\Houmao.Tests\Houmao.Tests.csproj"
    
    dotnet restore $srcProject
    dotnet restore $testProject
    Write-Host "Restore complete!" -ForegroundColor Green
}

# Main execution
switch ($Command) {
    "build"     { Invoke-Build -Config "Debug" }
    "release"   { Invoke-Build -Config "Release" }
    "test"      { Invoke-Test }
    "publish"   { Invoke-Publish }
    "clean"     { Invoke-Clean }
    "check"     { Invoke-Check }
    "install"   { Invoke-Install }
    "run"       { Invoke-Run }
    "format"    { Invoke-Format }
    "restore"   { Invoke-Restore }
    "help"      { Show-Help }
    default     { Show-Help }
}
