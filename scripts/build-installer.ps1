[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

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

function Assert-Payload {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [switch] $AllowNestedExecutable
    )

    $tubeVaultExecutable = if ($AllowNestedExecutable) {
        Get-ChildItem -LiteralPath $Root -Recurse -File -Filter 'TubeVault.exe'
    }
    else {
        Get-Item -LiteralPath (Join-Path $Root 'TubeVault.exe') -ErrorAction SilentlyContinue
    }

    if (-not $tubeVaultExecutable) {
        throw 'El staging no contiene TubeVault.exe.'
    }

    $forbiddenFiles = @('portable.flag', 'yt-dlp.exe', 'ffmpeg.exe', 'ffprobe.exe')
    $foundFiles = Get-ChildItem -LiteralPath $Root -Recurse -File |
        Where-Object Name -In $forbiddenFiles

    if ($foundFiles) {
        $names = ($foundFiles.FullName -join ', ')
        throw "El staging contiene archivos prohibidos: $names"
    }

    $forbiddenDirectories = @('config', 'logs', 'tools')
    $foundDirectories = Get-ChildItem -LiteralPath $Root -Recurse -Directory |
        Where-Object Name -In $forbiddenDirectories

    if ($foundDirectories) {
        $names = ($foundDirectories.FullName -join ', ')
        throw "El staging contiene directorios prohibidos: $names"
    }
}

function Assert-AdministrativeExtraction {
    param([Parameter(Mandatory)] [string] $Root)

    $requiredFiles = @(
        'TubeVault.exe',
        'LICENSE',
        'THIRD-PARTY-NOTICES.md',
        'PRIVACY.md'
    )

    foreach ($fileName in $requiredFiles) {
        $matches = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $fileName
        if (-not $matches) {
            throw "La extracción administrativa no contiene $fileName."
        }
    }

    Assert-Payload -Root $Root -AllowNestedExecutable

    $pdbFiles = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.pdb'
    if ($pdbFiles) {
        throw 'La extracción administrativa contiene archivos PDB.'
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $repoRoot 'artifacts'
$stagingRoot = Get-FullChildPath `
    -Path (Join-Path $artifactsRoot 'installer-input\TubeVault-win-x64') `
    -Parent $artifactsRoot
$installerOutput = Get-FullChildPath `
    -Path (Join-Path $artifactsRoot 'installer') `
    -Parent $artifactsRoot
$administrativeRoot = Get-FullChildPath `
    -Path (Join-Path $artifactsRoot 'installer-administrative-test') `
    -Parent $artifactsRoot

$appProjectPath = Join-Path $repoRoot 'src\TubeVault\TubeVault.csproj'
$installerProjectPath = Join-Path $repoRoot 'installer\TubeVault.Installer\TubeVault.Installer.wixproj'

[xml] $appProject = Get-Content -LiteralPath $appProjectPath -Raw
$versionNode = $appProject.SelectSingleNode('/Project/PropertyGroup/InformationalVersion')
$fullVersion = $versionNode.InnerText.Trim()

$versionMatch = [regex]::Match(
    $fullVersion,
    '^(?<year>\d{4})\.(?<month>\d{2})\.(?<revision>\d{3})$')
if (-not $versionMatch.Success) {
    throw "InformationalVersion '$fullVersion' no cumple AAAA.MM.REVISION."
}

$year = [int] $versionMatch.Groups['year'].Value
$month = [int] $versionMatch.Groups['month'].Value
$revision = [int] $versionMatch.Groups['revision'].Value
$msiMajor = $year - 2000

if ($msiMajor -lt 0 -or $msiMajor -gt 255) {
    throw "El major MSI calculado ($msiMajor) debe estar entre 0 y 255."
}
if ($month -lt 1 -or $month -gt 12 -or $month -gt 255) {
    throw "El minor MSI ($month) debe ser un mes válido y no superar 255."
}
if ($revision -lt 0 -or $revision -gt 65535) {
    throw "El build MSI ($revision) debe estar entre 0 y 65535."
}

$msiVersion = "$msiMajor.$month.$revision"
$msiName = "TubeVault-$fullVersion-win-x64.msi"
$msiPath = Join-Path $installerOutput $msiName

Write-Host "TubeVault version: $fullVersion"
Write-Host "MSI ProductVersion: $msiVersion"

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null

if (Test-Path -LiteralPath $msiPath) {
    Remove-Item -LiteralPath $msiPath -Force
}

& dotnet publish $appProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $stagingRoot
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish terminó con código $LASTEXITCODE."
}

Get-ChildItem -LiteralPath $stagingRoot -Recurse -File -Filter '*.pdb' |
    Remove-Item -Force

Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'PRIVACY.md') -Destination $stagingRoot

Assert-Payload -Root $stagingRoot

& dotnet restore $installerProjectPath
if ($LASTEXITCODE -ne 0) {
    throw "La restauración de WiX terminó con código $LASTEXITCODE."
}

& dotnet build $installerProjectPath `
    -c Release `
    --no-restore `
    "-p:PayloadDir=$stagingRoot" `
    "-p:FullVersion=$fullVersion" `
    "-p:MsiVersion=$msiVersion" `
    "-p:OutputPath=$installerOutput\"
if ($LASTEXITCODE -ne 0) {
    throw "El build WiX terminó con código $LASTEXITCODE."
}

$msiFiles = Get-ChildItem -LiteralPath $installerOutput -File -Filter '*.msi'
if ($msiFiles.Count -ne 1 -or $msiFiles[0].FullName -ne $msiPath) {
    $found = ($msiFiles.Name -join ', ')
    throw "Se esperaba únicamente '$msiName'. Encontrados: $found"
}

$externalCabFiles = Get-ChildItem -LiteralPath $installerOutput -File -Filter '*.cab'
if ($externalCabFiles) {
    throw "Se generaron CAB externos: $($externalCabFiles.Name -join ', ')"
}

$hash = Get-FileHash -LiteralPath $msiPath -Algorithm SHA256
$size = (Get-Item -LiteralPath $msiPath).Length

if (Test-Path -LiteralPath $administrativeRoot) {
    Remove-Item -LiteralPath $administrativeRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $administrativeRoot -Force | Out-Null

try {
    $arguments = @(
        '/a',
        "`"$msiPath`"",
        '/qn',
        "TARGETDIR=`"$administrativeRoot`""
    )
    $process = Start-Process -FilePath 'msiexec.exe' `
        -ArgumentList $arguments `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "msiexec /a terminó con código $($process.ExitCode)."
    }

    Assert-AdministrativeExtraction -Root $administrativeRoot
    Write-Host 'Administrative extraction: OK'
}
finally {
    if (Test-Path -LiteralPath $administrativeRoot) {
        Remove-Item -LiteralPath $administrativeRoot -Recurse -Force
    }
}

Write-Host "MSI: $msiPath"
Write-Host "Size: $size bytes"
Write-Host "SHA-256: $($hash.Hash)"
