[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}
if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    throw 'No se pudo cargar System.IO.Compression.ZipFile.'
}

function Get-FullChildPath {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Parent
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullParent = [System.IO.Path]::TrimEndingDirectorySeparator(
        [System.IO.Path]::GetFullPath($Parent)) +
        [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith(
            $fullParent,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta '$fullPath' debe estar dentro de '$fullParent'."
    }

    return $fullPath
}

function Complete-PortablePayload {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $RepositoryRoot
    )

    Get-ChildItem -LiteralPath $Root -File -Recurse -Filter '*.pdb' |
        Remove-Item -Force

    foreach ($fileName in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'PRIVACY.md')) {
        Copy-Item `
            -LiteralPath (Join-Path $RepositoryRoot $fileName) `
            -Destination $Root
    }
}

function Assert-PortablePayload {
    param([Parameter(Mandatory)] [string] $Root)

    $expectedNames = @('TubeVault.exe', 'LICENSE', 'PRIVACY.md', 'THIRD-PARTY-NOTICES.md')
    $items = @(Get-ChildItem -LiteralPath $Root -Force -Recurse)
    # La lista cerrada impide incluir datos de ejecuciÃ³n o archivos ajenos.
    if ($items.Count -ne $expectedNames.Count -or
        @($items | Where-Object { $_.PSIsContainer -or $_.Name -notin $expectedNames }).Count -gt 0) {
        throw 'El payload debe contener Ãºnicamente los cuatro archivos pÃºblicos previstos.'
    }
    foreach ($fileName in $expectedNames) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $fileName) -PathType Leaf)) {
            throw "El payload no contiene '$fileName' en la raÃ­z."
        }
    }
    if ((Get-Item -LiteralPath (Join-Path $Root 'TubeVault.exe')).Length -le 0) {
        throw 'El payload contiene un TubeVault.exe vacÃ­o.'
    }
}

function Assert-PortableZip {
    param([Parameter(Mandatory)] [string] $ZipPath)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entries = @($archive.Entries)
        $expectedNames = @('TubeVault.exe', 'LICENSE', 'PRIVACY.md', 'THIRD-PARTY-NOTICES.md')
        if ($entries.Count -ne $expectedNames.Count) {
            throw 'El ZIP debe contener exactamente los cuatro archivos pÃºblicos previstos.'
        }
        foreach ($fileName in $expectedNames) {
            $matches = @($entries | Where-Object { $_.FullName.Replace('\', '/') -eq $fileName })
            if ($matches.Count -ne 1) {
                throw "El ZIP debe contener exactamente un '$fileName' en la raÃ­z."
            }
            if ($fileName -eq 'TubeVault.exe' -and $matches[0].Length -le 0) {
                throw 'El ZIP contiene un TubeVault.exe vacÃ­o.'
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

function New-PortableZip {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $SourceRoot,
        [Parameter(Mandatory)] [string] $ZipPath
    )

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $SourceRoot,
        $ZipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    Assert-PortableZip -ZipPath $ZipPath

    $zipFile = Get-Item -LiteralPath $ZipPath
    $hash = Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256
    Write-Host "$Name ZIP: $($zipFile.FullName)"
    Write-Host "$Name size: $($zipFile.Length) bytes"
    Write-Host "$Name SHA-256: $($hash.Hash)"
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $repoRoot 'artifacts'
$portableInputRoot = Get-FullChildPath `
    -Path (Join-Path $artifactsRoot 'portable-input') `
    -Parent $artifactsRoot
$portableOutputRoot = Get-FullChildPath `
    -Path (Join-Path $artifactsRoot 'portable') `
    -Parent $artifactsRoot
$appProjectPath = Join-Path $repoRoot 'src\TubeVault\TubeVault.csproj'
[xml] $appProject = Get-Content -LiteralPath $appProjectPath -Raw
$versionNode = $appProject.SelectSingleNode('/Project/PropertyGroup/InformationalVersion')
if ($null -eq $versionNode) {
    throw 'TubeVault.csproj no contiene InformationalVersion.'
}

$fullVersion = $versionNode.InnerText.Trim()
if ($fullVersion -notmatch '^\d{4}\.\d{2}\.\d{3}$') {
    throw "InformationalVersion '$fullVersion' no cumple AAAA.MM.RRR."
}

$zipPath = Get-FullChildPath `
    -Path (Join-Path $portableOutputRoot "TubeVault-$fullVersion-win-x64-portable.zip") `
    -Parent $portableOutputRoot

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCommand) {
    throw 'No se encontrÃ³ dotnet en PATH.'
}

$sdkVersion = & dotnet --version
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkVersion)) {
    throw 'No se pudo resolver el SDK requerido por global.json.'
}

Write-Host "TubeVault version: $fullVersion"
Write-Host "SDK: $sdkVersion"

# Todas las rutas se calculan y validan antes de limpiar estos dos Ã¡rboles.
foreach ($root in @($portableInputRoot, $portableOutputRoot)) {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $portableInputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $portableOutputRoot -Force | Out-Null

Write-Host 'Publishing Portable...'
& dotnet publish $appProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $portableInputRoot
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish terminÃ³ con cÃ³digo $LASTEXITCODE."
}
Complete-PortablePayload -Root $portableInputRoot -RepositoryRoot $repoRoot
Assert-PortablePayload -Root $portableInputRoot

New-PortableZip `
    -Name 'Portable' `
    -SourceRoot $portableInputRoot `
    -ZipPath $zipPath
