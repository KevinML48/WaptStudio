$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $scriptRoot 'WaptStudio.sln'
$projectPath = Join-Path $scriptRoot 'WaptStudio.App\WaptStudio.App.csproj'
$distRoot = Join-Path $scriptRoot 'dist'
$publishPath = Join-Path $distRoot 'publish'

Write-Host 'Verification de l''environnement .NET...' -ForegroundColor Cyan
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Le SDK .NET n'est pas installe ou n'est pas disponible dans le PATH.`nInstalle .NET 10 puis verifie avec: dotnet --info"
}

$sdkVersion = & dotnet --version
Write-Host "SDK detecte: $sdkVersion" -ForegroundColor Green

if (-not (Test-Path $solutionPath)) {
    throw "Solution introuvable: $solutionPath"
}

try {
    if (Test-Path $distRoot) {
        Remove-Item $distRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

    Write-Host 'Restauration des dependances...' -ForegroundColor Cyan
    dotnet restore $solutionPath

    Write-Host 'Compilation Release...' -ForegroundColor Cyan
    dotnet build $solutionPath -c Release --no-restore

    Write-Host 'Execution des tests...' -ForegroundColor Cyan
    dotnet test $solutionPath -c Release --no-build

    Write-Host 'Publication de l''application...' -ForegroundColor Cyan
    dotnet publish $projectPath -c Release -o $publishPath --no-build --framework net10.0-windows

    Copy-Item (Join-Path $scriptRoot 'README.md') $distRoot -Force
    Copy-Item (Join-Path $scriptRoot 'Start-WaptStudio.ps1') $distRoot -Force
    Copy-Item (Join-Path $scriptRoot 'Build-Release.ps1') $distRoot -Force

    Write-Host "Publication terminee dans $distRoot" -ForegroundColor Green
}
catch {
    Write-Host "Echec du build release: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Pensez a verifier la disponibilite du SDK avec dotnet --info avant de relancer.' -ForegroundColor Yellow
    throw
}

Write-Host 'Build release termine.' -ForegroundColor Green