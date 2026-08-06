# TRADER Windows PowerShell Build & Test Script
$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  TRADER Monorepo Build & Test Pipeline   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "[1/4] Restoring solution dependencies..." -ForegroundColor Yellow
    dotnet restore TRADER.sln

    Write-Host "[2/4] Building full solution..." -ForegroundColor Yellow
    dotnet build TRADER.sln -c Release --no-restore

    Write-Host "[3/4] Running backend unit tests..." -ForegroundColor Yellow
    dotnet test tests/Trader.Backend.Tests/Trader.Backend.Tests.csproj -c Release --no-build

    Write-Host "[4/4] Running NexusBrain unit tests..." -ForegroundColor Yellow
    dotnet test tests/NexusBrain.Tests/NexusBrain.Tests.csproj -c Release --no-build

    Write-Host "[SUCCESS] Monorepo built and all test suites passed!" -ForegroundColor Green
} else {
    Write-Host "[INFO] .NET SDK not detected. Please install .NET 8 SDK from https://dotnet.microsoft.com" -ForegroundColor Magenta
}
