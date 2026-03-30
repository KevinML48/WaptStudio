param(
    [string]$PackageRoot = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'artifacts\wapt-package\cd48-waptstudio'),
    [switch]$BuildWithWapt,
    [string]$WaptExecutablePath = 'wapt-get.exe'
)

$ErrorActionPreference = 'Stop'

function Get-ControlMetadata {
    param([string]$ControlPath)

    $metadata = @{}
    foreach ($line in Get-Content -Path $ControlPath) {
        if ($line -match '^\s*([^:#]+?)\s*:\s*(.*?)\s*$') {
            $metadata[$matches[1].Trim().ToLowerInvariant()] = $matches[2].Trim()
        }
    }

    return $metadata
}

function Resolve-WaptExecutable {
    param([string]$Candidate)

    if (Test-Path $Candidate) {
        return (Resolve-Path $Candidate).Path
    }

    $command = Get-Command $Candidate -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw "Executable WAPT introuvable: $Candidate"
}

if (-not (Test-Path $PackageRoot)) {
    throw "Dossier paquet introuvable: $PackageRoot"
}

$requiredPaths = @(
    (Join-Path $PackageRoot 'setup.py'),
    (Join-Path $PackageRoot 'WAPT\control'),
    (Join-Path $PackageRoot 'README.md'),
    (Join-Path $PackageRoot 'package-build-manifest.json'),
    (Join-Path $PackageRoot 'sources\app\WaptStudio.App.exe')
)

foreach ($requiredPath in $requiredPaths) {
    if (-not (Test-Path $requiredPath)) {
        throw "Element requis manquant: $requiredPath"
    }
}

$setupPy = Get-Content -Path (Join-Path $PackageRoot 'setup.py') -Raw
$controlPath = Join-Path $PackageRoot 'WAPT\control'
$control = Get-Content -Path $controlPath -Raw
$manifest = Get-Content -Path (Join-Path $PackageRoot 'package-build-manifest.json') -Raw | ConvertFrom-Json
$controlMetadata = Get-ControlMetadata -ControlPath $controlPath

if ($setupPy.Contains('__PACKAGE_') -or $control.Contains('__PACKAGE_')) {
    throw 'Des tokens non remplaces sont encore presents dans le paquet WAPT.'
}

if (-not $setupPy.Contains('ProgramFiles')) {
    throw 'Le setup.py ne cible pas Program Files pour les fichiers applicatifs.'
}

$destructiveLocalDataPattern = '(?im)^.*(?:shutil\.rmtree|os\.remove)\s*\([^\r\n]*(?:LOCALAPPDATA|AppData\\\\Local\\\\WaptStudio)[^\r\n]*$'
if ($setupPy -match $destructiveLocalDataPattern) {
    throw 'Le setup.py ne doit pas supprimer les donnees utilisateur locales.'
}

$controlPackage = if ($controlMetadata.ContainsKey('package')) { $controlMetadata['package'] } else { $null }
$controlVersion = if ($controlMetadata.ContainsKey('version')) { $controlMetadata['version'] } else { $null }

if ([string]::IsNullOrWhiteSpace($controlPackage)) {
    throw 'Le control WAPT a un champ package vide ou absent.'
}

if ([string]::IsNullOrWhiteSpace($controlVersion)) {
    throw 'Le control WAPT a un champ version vide ou absent.'
}

if ($controlPackage -ne $manifest.packageId) {
    throw "Le control WAPT contient un package inattendu '$controlPackage' au lieu de '$($manifest.packageId)'."
}

if ($controlVersion -ne $manifest.packageVersion) {
    throw "Le control WAPT contient une version inattendue '$controlVersion' au lieu de '$($manifest.packageVersion)'."
}

Write-Host 'Validation structurelle du paquet WAPT: OK' -ForegroundColor Green

if ($BuildWithWapt) {
    $resolvedWapt = Resolve-WaptExecutable -Candidate $WaptExecutablePath
    Write-Host "Build-package via $resolvedWapt..." -ForegroundColor Cyan
    & $resolvedWapt build-package $PackageRoot
}