$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'WaptStudio.App\WaptStudio.App.csproj'

Write-Host 'Verification de l''environnement .NET...' -ForegroundColor Cyan

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Le SDK .NET n'est pas installe ou n'est pas disponible dans le PATH.`nInstalle .NET 10 puis verifie avec: dotnet --info"
}

$sdkVersion = & dotnet --version
Write-Host "SDK detecte: $sdkVersion" -ForegroundColor Green
Write-Host 'Conseil: executez aussi dotnet --info avant le premier lancement reel.' -ForegroundColor DarkGray

if (-not (Test-Path $projectPath)) {
    throw "Projet introuvable: $projectPath"
}

Write-Host 'Restauration et lancement de WaptStudio...' -ForegroundColor Cyan
try {
    dotnet run --project $projectPath --framework net10.0-windows
}
catch {
    Write-Host "Le lancement a echoue: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Verifiez aussi le chemin WAPT configure dans l''application si vous testez un flux reel.' -ForegroundColor Yellow
    throw
}
