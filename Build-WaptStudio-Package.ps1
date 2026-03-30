param(
    [string]$PackageId = 'cd48-waptstudio',
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$PackageVersion,
    [string]$PublishOutputPath,
    [string]$OutputRoot,
    [switch]$SkipPublish,
    [switch]$BuildWithWapt,
    [string]$WaptExecutablePath = 'wapt-get.exe'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$templateRoot = Join-Path $scriptRoot 'packaging\wapt\cd48-waptstudio'
$defaultPublishPath = Join-Path $scriptRoot "dist\$RuntimeIdentifier\self-contained"
$publishPath = if ([string]::IsNullOrWhiteSpace($PublishOutputPath)) { $defaultPublishPath } else { $PublishOutputPath }
$resolvedOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) { Join-Path $scriptRoot 'artifacts\wapt-package' } else { $OutputRoot }
$stagingRoot = Join-Path $resolvedOutputRoot $PackageId
$payloadTargetPath = Join-Path $stagingRoot 'sources\app'
$controlPath = Join-Path $stagingRoot 'WAPT\control'
$setupPyPath = Join-Path $stagingRoot 'setup.py'
$packageReadmePath = Join-Path $stagingRoot 'README.md'
$manifestPath = Join-Path $stagingRoot 'package-build-manifest.json'

function Write-Utf8NoBomFile {
    param(
        [string]$FilePath,
        [string]$Content
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($FilePath, $Content, $utf8NoBom)
}

function Get-PackageVersion {
    param([string]$RepositoryRoot)

    $propsPath = Join-Path $RepositoryRoot 'Directory.Build.props'
    if (-not (Test-Path $propsPath)) {
        throw "Fichier introuvable: $propsPath"
    }

    [xml]$props = Get-Content -Path $propsPath
    $version = $props.Project.PropertyGroup.FileVersion
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = $props.Project.PropertyGroup.Version
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw 'Impossible de determiner la version du paquet depuis Directory.Build.props.'
    }

    if ($version.Contains('-')) {
        $version = $version.Split('-')[0]
    }

    return $version.Trim()
}

function Replace-TokensInFile {
    param(
        [string]$FilePath,
        [hashtable]$TokenMap
    )

    $content = Get-Content -Path $FilePath -Raw
    foreach ($entry in $TokenMap.GetEnumerator()) {
        $content = $content.Replace($entry.Key, $entry.Value)
    }

    Write-Utf8NoBomFile -FilePath $FilePath -Content $content
}

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

function Assert-ControlMetadata {
    param(
        [string]$ControlPath,
        [string]$ExpectedPackageId,
        [string]$ExpectedPackageVersion
    )

    $metadata = Get-ControlMetadata -ControlPath $ControlPath
    $packageId = if ($metadata.ContainsKey('package')) { $metadata['package'] } else { $null }
    $packageVersion = if ($metadata.ContainsKey('version')) { $metadata['version'] } else { $null }

    if ([string]::IsNullOrWhiteSpace($packageId)) {
        throw "Le control WAPT genere a un champ package vide ou absent: $ControlPath"
    }

    if ($packageId -ne $ExpectedPackageId) {
        throw "Le control WAPT genere contient un package inattendu '$packageId' au lieu de '$ExpectedPackageId'."
    }

    if ([string]::IsNullOrWhiteSpace($packageVersion)) {
        throw "Le control WAPT genere a un champ version vide ou absent: $ControlPath"
    }

    if ($packageVersion -ne $ExpectedPackageVersion) {
        throw "Le control WAPT genere contient une version inattendue '$packageVersion' au lieu de '$ExpectedPackageVersion'."
    }
}

function Resolve-WaptExecutable {
    param([string]$Candidate)

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        throw 'Aucun executable WAPT n''a ete fourni.'
    }

    if (Test-Path $Candidate) {
        return (Resolve-Path $Candidate).Path
    }

    $command = Get-Command $Candidate -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw "Executable WAPT introuvable: $Candidate"
}

if (-not (Test-Path $templateRoot)) {
    throw "Template de paquet introuvable: $templateRoot"
}

if (-not $SkipPublish) {
    Write-Host 'Publication Release self-contained de WaptStudio...' -ForegroundColor Cyan
    & (Join-Path $scriptRoot 'Build-Release.ps1')
}

if (-not (Test-Path $publishPath)) {
    throw "Dossier publie introuvable: $publishPath"
}

$publishedExecutable = Join-Path $publishPath 'WaptStudio.App.exe'
if (-not (Test-Path $publishedExecutable)) {
    throw "Executable publie introuvable: $publishedExecutable"
}

if ([string]::IsNullOrWhiteSpace($PackageVersion)) {
    $PackageVersion = Get-PackageVersion -RepositoryRoot $scriptRoot
}

Write-Host "Version du paquet WAPT: $PackageVersion" -ForegroundColor Green

if (Test-Path $stagingRoot) {
    Remove-Item $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedOutputRoot -Force | Out-Null
Copy-Item -Path $templateRoot -Destination $stagingRoot -Recurse

if (Test-Path $payloadTargetPath) {
    Remove-Item $payloadTargetPath -Recurse -Force
}

New-Item -ItemType Directory -Path $payloadTargetPath -Force | Out-Null
Copy-Item -Path (Join-Path $publishPath '*') -Destination $payloadTargetPath -Recurse -Force

$resolvedPublishPath = (Resolve-Path $publishPath).Path
$tokenMap = @{
    '__PACKAGE_ID__' = $PackageId
    '__PACKAGE_VERSION__' = $PackageVersion
    '__PUBLISH_RUNTIME__' = $RuntimeIdentifier
    '__PUBLISH_SOURCE__' = $resolvedPublishPath
}

Replace-TokensInFile -FilePath $controlPath -TokenMap $tokenMap
Replace-TokensInFile -FilePath $setupPyPath -TokenMap $tokenMap
Replace-TokensInFile -FilePath $packageReadmePath -TokenMap $tokenMap
Assert-ControlMetadata -ControlPath $controlPath -ExpectedPackageId $PackageId -ExpectedPackageVersion $PackageVersion

$payloadFiles = Get-ChildItem -Path $payloadTargetPath -Recurse -File | Sort-Object FullName
$manifest = [ordered]@{
    packageId = $PackageId
    packageVersion = $PackageVersion
    runtimeIdentifier = $RuntimeIdentifier
    publishSource = $resolvedPublishPath
    stagingRoot = $stagingRoot
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('O')
    payloadFileCount = $payloadFiles.Count
    payloadFiles = @($payloadFiles | ForEach-Object { $_.FullName.Substring($payloadTargetPath.Length).TrimStart('\') })
}

$manifestJson = $manifest | ConvertTo-Json -Depth 4
Write-Utf8NoBomFile -FilePath $manifestPath -Content $manifestJson

Write-Host "Paquet WAPT stage dans: $stagingRoot" -ForegroundColor Green
Write-Host 'Validation conseillee: .\Test-WaptStudio-Package.ps1' -ForegroundColor Yellow

if ($BuildWithWapt) {
    $resolvedWapt = Resolve-WaptExecutable -Candidate $WaptExecutablePath
    Write-Host "Construction du .wapt via $resolvedWapt..." -ForegroundColor Cyan
    & $resolvedWapt build-package $stagingRoot
    Assert-ControlMetadata -ControlPath $controlPath -ExpectedPackageId $PackageId -ExpectedPackageVersion $PackageVersion
}