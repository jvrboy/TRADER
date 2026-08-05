$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Dist = Join-Path $Root "dist"
$Stage = Join-Path $Dist "BrainSystem"

Remove-Item -Recurse -Force $Dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $Stage | Out-Null
dotnet restore (Join-Path $Root "BrainSystem.sln")
dotnet test (Join-Path $Root "BrainSystem.sln") --configuration Release --no-restore
dotnet publish (Join-Path $Root "src/BrainSystem.Api/BrainSystem.Api.csproj") --configuration Release --output (Join-Path $Stage "bin") --no-restore
Copy-Item (Join-Path $Root "src"), (Join-Path $Root "tests"), (Join-Path $Root "config"), (Join-Path $Root "docs"), (Join-Path $Root "models"), (Join-Path $Root "scripts") -Destination $Stage -Recurse
Copy-Item (Join-Path $Root "README.md"), (Join-Path $Root "BrainSystem.sln"), (Join-Path $Root "Directory.Build.props"), (Join-Path $Root "Directory.Packages.props") -Destination $Stage
Get-ChildItem $Stage -Include bin,obj -Recurse -Directory | Where-Object { $_.FullName -notlike "*\bin\*" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $Stage -DestinationPath (Join-Path $Dist "BrainSystem.zip") -Force
Copy-Item (Join-Path $Root "download/index.html") -Destination (Join-Path $Dist "index.html")
Get-FileHash (Join-Path $Dist "BrainSystem.zip") -Algorithm SHA256 | Select-Object -ExpandProperty Hash | Set-Content (Join-Path $Dist "BrainSystem.zip.sha256")
Write-Host "Created $(Join-Path $Dist 'BrainSystem.zip')"