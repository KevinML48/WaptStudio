$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'WaptStudio.App\WaptStudio.App.csproj'

Write-Host 'Verification de l''environnement .NET...' -ForegroundColor Cyan

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw 'Le SDK .NET n''est pas installe ou n''est pas disponible dans le PATH.'
}

$sdkVersion = & dotnet --version
Write-Host "SDK detecte: $sdkVersion" -ForegroundColor Green

if (-not (Test-Path $projectPath)) {
    throw "Projet introuvable: $projectPath"
}

Write-Host 'Restauration et lancement de WaptStudio...' -ForegroundColor Cyan
dotnet run --project $projectPath --framework net10.0-windows
