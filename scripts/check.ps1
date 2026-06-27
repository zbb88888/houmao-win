# Check script for Houmao Windows
# Usage: .\scripts\check.ps1

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

Write-Host "Checking Houmao Windows project..." -ForegroundColor Cyan

# Check for .NET SDK
Write-Host "`nChecking for .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  Found .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  .NET SDK not found. Please install .NET 9 SDK." -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

# Check project files
Write-Host "`nChecking project files..." -ForegroundColor Yellow

$requiredFiles = @(
    "src/Houmao/Houmao.csproj",
    "src/Houmao/App.xaml",
    "src/Houmao/App.xaml.cs",
    "src/Houmao/Views/MainWindow.xaml",
    "src/Houmao/Views/MainWindow.xaml.cs",
    "tests/Houmao.Tests/Houmao.Tests.csproj"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $RootDir $file
    if (Test-Path $path) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (missing)" -ForegroundColor Red
    }
}

# Check directories
Write-Host "`nChecking directories..." -ForegroundColor Yellow

$directories = @(
    "src/Houmao/Models",
    "src/Houmao/Services",
    "src/Houmao/ViewModels",
    "src/Houmao/Views",
    "src/Houmao/Interop",
    "src/Houmao/Converters",
    "src/Houmao/Resources",
    "tests/Houmao.Tests"
)

foreach ($dir in $directories) {
    $path = Join-Path $RootDir $dir
    if (Test-Path $path) {
        Write-Host "  ✓ $dir" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $dir (missing)" -ForegroundColor Red
    }
}

# Try to restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
try {
    $projectPath = Join-Path $RootDir "src/Houmao/Houmao.csproj"
    dotnet restore $projectPath --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Packages restored successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Failed to restore packages" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ Error restoring packages: $_" -ForegroundColor Red
}

# Try to build
Write-Host "`nBuilding project..." -ForegroundColor Yellow
try {
    dotnet build $projectPath --configuration Debug --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Build failed" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ Error building project: $_" -ForegroundColor Red
}

Write-Host "`nCheck completed!" -ForegroundColor Cyan