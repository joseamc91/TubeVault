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

function Invoke-PortablePublish {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $ProjectPath,
        [Parameter(Mandatory)] [string] $OutputPath,
        [string[]] $AdditionalArguments = @()
    )

    $arguments = @(
        'publish',
        $ProjectPath,
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained', 'true',
        '-o', $OutputPath
    ) + $AdditionalArguments

    Write-Host "Publishing $Name..."
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish para $Name terminó con código $LASTEXITCODE."
    }
}

function Complete-PortablePayload {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $RepositoryRoot
    )

    Get-ChildItem -LiteralPath $Root -File -Recurse -Filter '*.pdb' |
        Remove-Item -Force

    $portableFlagPath = Join-Path $Root 'portable.flag'
    [System.IO.File]::WriteAllBytes($portableFlagPath, [byte[]]::new(0))

    foreach ($fileName in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'PRIVACY.md')) {
        Copy-Item `
            -LiteralPath (Join-Path $RepositoryRoot $fileName) `
            -Destination $Root
    }
}

function Assert-PortablePayload {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Root,
        [switch] $RequireApplicationDll
    )

    foreach ($fileName in @(
            'TubeVault.exe',
            'portable.flag',
            'LICENSE',
            'THIRD-PARTY-NOTICES.md',
            'PRIVACY.md')) {
        $requiredPath = Join-Path $Root $fileName
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "$Name no contiene '$fileName' en la raíz."
        }
    }

    $rootPortableFlag = [System.IO.Path]::GetFullPath(
        (Join-Path $Root 'portable.flag'))
    $portableFlags = @(Get-ChildItem -LiteralPath $Root -File -Recurse |
        Where-Object Name -eq 'portable.flag')
    if ($portableFlags.Count -ne 1) {
        throw "$Name debe contener exactamente un portable.flag; encontrados: $($portableFlags.Count)."
    }
    if (-not $portableFlags[0].FullName.Equals(
            $rootPortableFlag,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Name contiene portable.flag fuera de la raíz."
    }
    if ($portableFlags[0].Length -ne 0) {
        throw "$Name contiene un portable.flag que no está vacío."
    }

    if ($RequireApplicationDll -and
        -not (Test-Path -LiteralPath (Join-Path $Root 'TubeVault.dll') -PathType Leaf)) {
        throw "$Name no contiene TubeVault.dll."
    }

    $forbiddenNames = @(
        'yt-dlp.exe',
        'ffmpeg.exe',
        'ffprobe.exe',
        'settings.json'
    )
    $forbiddenExtensions = @(
        '.pdb',
        '.mp3',
        '.webm',
        '.part',
        '.msi',
        '.wixpdb',
        '.cab'
    )

    $forbiddenFiles = @(Get-ChildItem -LiteralPath $Root -File -Recurse |
        Where-Object {
            $_.Name -in $forbiddenNames -or
            $_.Extension -in $forbiddenExtensions
        })
    if ($forbiddenFiles.Count -gt 0) {
        $names = ($forbiddenFiles.FullName -join ', ')
        throw "$Name contiene archivos prohibidos: $names"
    }

    $forbiddenDirectories = @(Get-ChildItem -LiteralPath $Root -Directory -Recurse |
        Where-Object Name -In @('config', 'logs', 'tools'))
    if ($forbiddenDirectories.Count -gt 0) {
        $names = ($forbiddenDirectories.FullName -join ', ')
        throw "$Name contiene directorios prohibidos: $names"
    }
}

function Show-SingleFileStructure {
    param([Parameter(Mandatory)] [string] $Root)

    $files = @(Get-ChildItem -LiteralPath $Root -File -Recurse)
    $directories = @(Get-ChildItem -LiteralPath $Root -Directory -Recurse)
    $looseDlls = @($files | Where-Object Extension -eq '.dll')

    Write-Host 'Single-file publish structure:'
    Write-Host "  Files: $($files.Count)"
    Write-Host "  Directories: $($directories.Count)"
    Write-Host "  TubeVault.dll: $(Test-Path -LiteralPath (Join-Path $Root 'TubeVault.dll') -PathType Leaf)"
    Write-Host "  TubeVault.deps.json: $(Test-Path -LiteralPath (Join-Path $Root 'TubeVault.deps.json') -PathType Leaf)"
    Write-Host "  TubeVault.runtimeconfig.json: $(Test-Path -LiteralPath (Join-Path $Root 'TubeVault.runtimeconfig.json') -PathType Leaf)"
    Write-Host "  Loose DLL files: $($looseDlls.Count)"
    Write-Host "  en directory: $(Test-Path -LiteralPath (Join-Path $Root 'en') -PathType Container)"
    Write-Host "  es directory: $(Test-Path -LiteralPath (Join-Path $Root 'es') -PathType Container)"

    if ($looseDlls.Count -gt 0) {
        Write-Host '  Loose DLL list:'
        foreach ($file in $looseDlls) {
            Write-Host "    $($file.FullName.Substring($Root.Length).TrimStart('\'))"
        }
    }
}

function Assert-PortableZip {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $ZipPath
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entries = @($archive.Entries)
        $entryNames = @($entries | ForEach-Object {
                $_.FullName.Replace('\', '/')
            })

        foreach ($fileName in @(
                'LICENSE',
                'THIRD-PARTY-NOTICES.md',
                'PRIVACY.md')) {
            if ($fileName -notin $entryNames) {
                throw "$Name no contiene '$fileName' en la raíz del ZIP."
            }
        }

        $portableFlags = @($entries | Where-Object Name -eq 'portable.flag')
        if ($portableFlags.Count -ne 1) {
            throw "$Name debe contener exactamente un portable.flag; encontrados: $($portableFlags.Count)."
        }
        $portableFlagName = $portableFlags[0].FullName.Replace('\', '/')
        if ($portableFlagName -ne 'portable.flag') {
            throw "$Name contiene portable.flag fuera de la raíz del ZIP."
        }
        if ($portableFlags[0].Length -ne 0) {
            throw "$Name contiene un portable.flag no vacío dentro del ZIP."
        }

        $tubeVaultExecutables = @($entries | Where-Object Name -eq 'TubeVault.exe')
        if ($tubeVaultExecutables.Count -ne 1) {
            throw "$Name debe contener exactamente un TubeVault.exe; encontrados: $($tubeVaultExecutables.Count)."
        }
        $tubeVaultExecutableName = $tubeVaultExecutables[0].FullName.Replace('\', '/')
        if ($tubeVaultExecutableName -ne 'TubeVault.exe') {
            throw "$Name contiene TubeVault.exe fuera de la raíz del ZIP."
        }
        if ($tubeVaultExecutables[0].Length -le 0) {
            throw "$Name contiene un TubeVault.exe vacío."
        }

        $forbiddenNames = @(
            'yt-dlp.exe',
            'ffmpeg.exe',
            'ffprobe.exe',
            'settings.json'
        )
        $forbiddenExtensions = @(
            '.pdb',
            '.mp3',
            '.webm',
            '.part',
            '.msi',
            '.wixpdb',
            '.cab'
        )

        $forbiddenEntries = @($entryNames | Where-Object {
                $entryName = $_
                $leafName = [System.IO.Path]::GetFileName($entryName)
                $extension = [System.IO.Path]::GetExtension($entryName)

                $leafName -in $forbiddenNames -or
                $extension -in $forbiddenExtensions -or
                $entryName -match '(^|/)(config|logs|tools)(/|$)'
            })
        if ($forbiddenEntries.Count -gt 0) {
            throw "$Name contiene entradas prohibidas: $($forbiddenEntries -join ', ')"
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

    Assert-PortableZip -Name $Name -ZipPath $ZipPath

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
$classicRoot = Get-FullChildPath `
    -Path (Join-Path $portableInputRoot 'classic') `
    -Parent $portableInputRoot
$singleFileRoot = Get-FullChildPath `
    -Path (Join-Path $portableInputRoot 'single-file') `
    -Parent $portableInputRoot

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

$classicZipPath = Get-FullChildPath `
    -Path (Join-Path $portableOutputRoot "TubeVault-$fullVersion-win-x64-portable-classic.zip") `
    -Parent $portableOutputRoot
$singleFileZipPath = Get-FullChildPath `
    -Path (Join-Path $portableOutputRoot "TubeVault-$fullVersion-win-x64-portable-single-file.zip") `
    -Parent $portableOutputRoot

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCommand) {
    throw 'No se encontró dotnet en PATH.'
}

$sdkVersion = & dotnet --version
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkVersion)) {
    throw 'No se pudo resolver el SDK requerido por global.json.'
}

Write-Host "TubeVault version: $fullVersion"
Write-Host "SDK: $sdkVersion"

# Todas las rutas se calculan y validan antes de limpiar estos dos árboles.
foreach ($root in @($portableInputRoot, $portableOutputRoot)) {
    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $classicRoot -Force | Out-Null
New-Item -ItemType Directory -Path $singleFileRoot -Force | Out-Null
New-Item -ItemType Directory -Path $portableOutputRoot -Force | Out-Null

Invoke-PortablePublish `
    -Name 'Portable Classic' `
    -ProjectPath $appProjectPath `
    -OutputPath $classicRoot
Complete-PortablePayload -Root $classicRoot -RepositoryRoot $repoRoot
Assert-PortablePayload `
    -Name 'Portable Classic' `
    -Root $classicRoot `
    -RequireApplicationDll

Invoke-PortablePublish `
    -Name 'Portable Single File' `
    -ProjectPath $appProjectPath `
    -OutputPath $singleFileRoot `
    -AdditionalArguments @(
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:PublishTrimmed=false'
    )
Complete-PortablePayload -Root $singleFileRoot -RepositoryRoot $repoRoot
Assert-PortablePayload -Name 'Portable Single File' -Root $singleFileRoot
Show-SingleFileStructure -Root $singleFileRoot

New-PortableZip `
    -Name 'Portable Classic' `
    -SourceRoot $classicRoot `
    -ZipPath $classicZipPath
New-PortableZip `
    -Name 'Portable Single File' `
    -SourceRoot $singleFileRoot `
    -ZipPath $singleFileZipPath
