# Test structure script for Houmao Windows
# Usage: .\scripts\test-structure.ps1

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

Write-Host "Testing Houmao Windows project structure..." -ForegroundColor Cyan

# Test 1: Check required files
Write-Host "`n1. Checking required files..." -ForegroundColor Yellow

$requiredFiles = @(
    "README.md",
    "BUILD.md",
    "DEVELOPMENT.md",
    "SUMMARY.md",
    "src/Houmao/Houmao.csproj",
    "src/Houmao/App.xaml",
    "src/Houmao/App.xaml.cs",
    "src/Houmao/GlobalUsings.cs",
    "tests/Houmao.Tests/Houmao.Tests.csproj"
)

$allFilesExist = $true
foreach ($file in $requiredFiles) {
    $path = Join-Path $RootDir $file
    if (Test-Path $path) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (missing)" -ForegroundColor Red
        $allFilesExist = $false
    }
}

# Test 2: Check directory structure
Write-Host "`n2. Checking directory structure..." -ForegroundColor Yellow

$directories = @(
    "src/Houmao/Models",
    "src/Houmao/Services",
    "src/Houmao/ViewModels",
    "src/Houmao/Views",
    "src/Houmao/Views/Controls",
    "src/Houmao/Interop",
    "src/Houmao/Converters",
    "src/Houmao/Resources",
    "src/Houmao/Resources/Icons",
    "src/Houmao/Resources/Styles",
    "tests/Houmao.Tests/Services",
    "tests/Houmao.Tests/ViewModels",
    "scripts",
    "docs"
)

$allDirectoriesExist = $true
foreach ($dir in $directories) {
    $path = Join-Path $RootDir $dir
    if (Test-Path $path) {
        Write-Host "  ✓ $dir" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $dir (missing)" -ForegroundColor Red
        $allDirectoriesExist = $false
    }
}

# Test 3: Check C# files
Write-Host "`n3. Checking C# files..." -ForegroundColor Yellow

$csFiles = Get-ChildItem -Path $RootDir -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*"
}

Write-Host "  Found $($csFiles.Count) C# files" -ForegroundColor Cyan

# Test 4: Check XAML files
Write-Host "`n4. Checking XAML files..." -ForegroundColor Yellow

$xamlFiles = Get-ChildItem -Path $RootDir -Recurse -Filter "*.xaml" | Where-Object {
    $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*"
}

Write-Host "  Found $($xamlFiles.Count) XAML files" -ForegroundColor Cyan

# Test 5: Check project references
Write-Host "`n5. Checking project references..." -ForegroundColor Yellow

$projectFiles = Get-ChildItem -Path $RootDir -Recurse -Filter "*.csproj"
foreach ($projectFile in $projectFiles) {
    $content = Get-Content $projectFile.FullName -Raw
    if ($content -match "ProjectReference") {
        Write-Host "  ✓ $($projectFile.Name) has project references" -ForegroundColor Green
    } else {
        Write-Host "  ✓ $($projectFile.Name) (no project references)" -ForegroundColor Green
    }
}

# Test 6: Check for common issues
Write-Host "`n6. Checking for common issues..." -ForegroundColor Yellow

# Check for empty files
$emptyFiles = Get-ChildItem -Path $RootDir -Recurse -File | Where-Object {
    $_.Length -eq 0 -and $_.Extension -ne ".ico"
}

if ($emptyFiles.Count -eq 0) {
    Write-Host "  ✓ No empty files found" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Found $($emptyFiles.Count) empty files" -ForegroundColor Yellow
    foreach ($file in $emptyFiles) {
        Write-Host "    - $($file.Name)" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "="*50 -ForegroundColor Cyan

if ($allFilesExist -and $allDirectoriesExist) {
    Write-Host "✓ Project structure is valid" -ForegroundColor Green
    Write-Host "✓ All required files and directories exist" -ForegroundColor Green
} else {
    Write-Host "✗ Project structure has issues" -ForegroundColor Red
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Install .NET 9 SDK if not already installed" -ForegroundColor White
Write-Host "2. Run: dotnet restore" -ForegroundColor White
Write-Host "3. Run: dotnet build" -ForegroundColor White
Write-Host "4. Run: dotnet run --project src/Houmao/Houmao.csproj" -ForegroundColor White

Write-Host "`nTest completed!" -ForegroundColor Cyan