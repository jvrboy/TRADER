# BrainSystem v1.0 - Build Script (PowerShell)
# Restores NuGet packages, builds the solution, and runs tests.

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$solutionPath = Join-Path $PSScriptRoot ".." "BrainSystem.sln"
$solutionPath = (Resolve-Path $solutionPath).Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BrainSystem Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Restore NuGet packages
Write-Host "[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) { Write-Host "Restore failed!" -ForegroundColor Red; exit 1 }
Write-Host "Restore complete." -ForegroundColor Green
Write-Host ""

# Step 2: Build solution
Write-Host "[2/4] Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build $solutionPath -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; exit 1 }
Write-Host "Build complete." -ForegroundColor Green
Write-Host ""

# Step 3: Run tests
if (-not $SkipTests) {
    Write-Host "[3/4] Running tests..." -ForegroundColor Yellow
    dotnet test $solutionPath -c $Configuration --no-build --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) { Write-Host "Tests failed!" -ForegroundColor Red; exit 1 }
    Write-Host "All tests passed." -ForegroundColor Green
} else {
    Write-Host "[3/4] Tests skipped." -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Package
if ($Package) {
    Write-Host "[4/4] Creating ZIP package..." -ForegroundColor Yellow
    $outputDir = Join-Path $PSScriptRoot ".." "dist"
    if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    # Publish API
    $apiPublishDir = Join-Path $outputDir "publish" "api"
    dotnet publish (Join-Path $PSScriptRoot ".." "src" "Brain.API" "Brain.API.csproj") -c $Configuration -o $apiPublishDir --no-build

    # Publish Launcher
    $launcherPublishDir = Join-Path $outputDir "publish" "launcher"
    dotnet publish (Join-Path $PSScriptRoot ".." "src" "Brain.Launcher" "Brain.Launcher.csproj") -c $Configuration -o $launcherPublishDir --no-build

    # Create ZIP
    $zipPath = Join-Path $outputDir "BrainSystem.zip"
    Compress-Archive -Path (Join-Path $outputDir "*") -DestinationPath $zipPath -Force

    $hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
    Write-Host "ZIP created: $zipPath" -ForegroundColor Green
    Write-Host "SHA-256: $hash" -ForegroundColor Green
} else {
    Write-Host "[4/4] Packaging skipped (use -Package to create ZIP)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
