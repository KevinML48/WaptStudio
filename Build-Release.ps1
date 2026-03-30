$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $scriptRoot 'WaptStudio.sln'
$projectPath = Join-Path $scriptRoot 'WaptStudio.App\WaptStudio.App.csproj'
$distRoot = Join-Path $scriptRoot 'dist'
$runtimeIdentifier = 'win-x64'
$publishPath = Join-Path $distRoot "$runtimeIdentifier\self-contained"

function Invoke-NativeCommand {
    param(
        [scriptblock]$Command,
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

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
    Invoke-NativeCommand -Command { dotnet restore $solutionPath } -FailureMessage 'La restauration de la solution a echoue.'

    Write-Host 'Compilation Release...' -ForegroundColor Cyan
    Invoke-NativeCommand -Command { dotnet build $solutionPath -c Release --no-restore } -FailureMessage 'La compilation Release a echoue.'

    Write-Host 'Execution des tests...' -ForegroundColor Cyan
    Invoke-NativeCommand -Command { dotnet test $solutionPath -c Release --no-build } -FailureMessage 'Les tests Release ont echoue.'

    Write-Host 'Restauration du runtime win-x64 pour la publication...' -ForegroundColor Cyan
    Invoke-NativeCommand -Command { dotnet restore $projectPath -r $runtimeIdentifier } -FailureMessage 'La restauration pour le runtime win-x64 a echoue.'

    Write-Host 'Publication de l''application...' -ForegroundColor Cyan
    Invoke-NativeCommand -Command { dotnet publish $projectPath -c Release -r $runtimeIdentifier --self-contained true -o $publishPath --no-restore --framework net10.0-windows } -FailureMessage 'La publication self-contained win-x64 a echoue.'

    Copy-Item (Join-Path $scriptRoot 'README.md') $distRoot -Force
    Copy-Item (Join-Path $scriptRoot 'Start-WaptStudio.ps1') $distRoot -Force
    Copy-Item (Join-Path $scriptRoot 'Build-Release.ps1') $distRoot -Force

    Write-Host "Publication self-contained terminee dans $publishPath" -ForegroundColor Green
}
catch {
    Write-Host "Echec du build release: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Pensez a verifier la disponibilite du SDK avec dotnet --info avant de relancer.' -ForegroundColor Yellow
    throw
}

Write-Host 'Build release termine.' -ForegroundColor Green