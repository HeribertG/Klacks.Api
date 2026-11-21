# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds the office-PC package klacks-office-pc-<rid>.zip from a signed native bundle: bundle (unchanged bytes),
# PostgreSQL with pgvector (bin, lib, share only), the Visual C++ redistributable, the public update key, the install
# scripts and the launcher. The file name is fixed per runtime identifier so
# releases/latest/download/klacks-office-pc-win-x64.zip always serves the newest package. Used by
# .github/workflows/native-windows.yml.
#
# Parameters: Rid = win-x64 (default) or win-arm64; BundleZip/BundleEntry = output of build-native-bundle.ps1 for that Rid;
# PostgresDirectory = EDB pgsql folder with pgvector installed (win-x64 only); VcRedistPath = vc_redist.x64.exe or
# vc_redist.arm64.exe (the ONNX runtime of the API needs it on both); PublicKeyPath = PEM of the update signing key.
# win-arm64 never builds PostgreSQL: it takes the signed engine ZIP of the engine release named by
# deploy/postgres/engine.json (native.win-arm64.releaseSuffix), downloaded unless EngineZip/EngineEntry name local copies,
# and only uses it when the entry names win-arm64 and the expected URL, its signature verifies with PublicKeyPath
# (sign-engine-entry.ps1 -Verify) and the ZIP has the SHA-256 of the entry. deploy/postgres is only read for win-arm64.
# -BuildInstaller additionally compiles Klacks-Setup-<rid>.exe from the same staging folder with Inno Setup
# (installer\Klacks.iss /DArch=x64|arm64); IsccPath defaults to ISCC.exe on PATH or the Inno Setup 6 install folders.
# SignToolCommand is an optional Inno SignTool command line (e.g. 'signtool sign ... $f'); when given, the setup and
# uninstaller are signed.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BundleZip,
    [Parameter(Mandatory)][string]$BundleEntry,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$')][string]$Version,
    [string]$PostgresDirectory = '',
    [Parameter(Mandatory)][string]$VcRedistPath,
    [Parameter(Mandatory)][string]$PublicKeyPath,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [switch]$BuildInstaller,
    [string]$IsccPath = '',
    [string]$SignToolCommand = '',
    [ValidateSet('win-x64', 'win-arm64')][string]$Rid = 'win-x64',
    [string]$EngineZip = '',
    [string]$EngineEntry = ''
)

$ErrorActionPreference = 'Stop'

$Arch = $Rid.Substring('win-'.Length)
$PackageFileName = "klacks-office-pc-$Rid.zip"
$VcRedistFileName = "vc_redist.$Arch.exe"
$PostgresParts = @('bin', 'lib', 'share')
$Scripts = @('Install-Klacks.ps1', 'Install-KlacksPackage.ps1', 'Uninstall-Klacks.ps1', 'Get-KlacksDiagnostics.ps1')
$PackageTemplates = @('Installieren.cmd', 'LIESMICH.txt', 'README.txt')
$InstallerScript = Join-Path $PSScriptRoot 'installer\Klacks.iss'
$RegionsIncludeName = 'Regions.generated.inc'
$RegionProfilePrefix = 'app/deploy/onprem/regions/'
$InstallerFileName = "Klacks-Setup-$Rid.exe"
$EngineJsonPath = Join-Path $PSScriptRoot '..\..\postgres\engine.json'
$EngineEntryScript = Join-Path $PSScriptRoot '..\..\postgres\sign-engine-entry.ps1'
$EngineReleaseBase = 'https://github.com/HeribertG/Klacks.Api/releases/download'
$InnoSetupFolders = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6'))

function Find-Iscc([string]$Explicit) {
    if ($Explicit) {
        if (-not (Test-Path $Explicit)) { throw "ISCC.exe not found at '$Explicit'." }
        return $Explicit
    }
    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    foreach ($folder in $InnoSetupFolders) {
        $candidate = Join-Path $folder 'ISCC.exe'
        if (Test-Path $candidate) { return $candidate }
    }
    throw 'ISCC.exe (Inno Setup 6) not found; install it or pass -IsccPath.'
}

# Bytes the installation unpacks below C:\Program Files\Klacks: the bundle's files plus PostgreSQL. Shown under installed
# apps; Inno itself would only count the few files in {app}. Data (database, AI models, backups) is not counted.
function Get-InstalledProgramBytes([string]$BundlePath, [string]$PostgresPath) {
    $zip = [IO.Compression.ZipFile]::OpenRead($BundlePath)
    try {
        $bundleBytes = ($zip.Entries | Measure-Object -Property Length -Sum).Sum
    }
    finally {
        $zip.Dispose()
    }
    $postgresBytes = (Get-ChildItem -Recurse -File -Force $PostgresPath | Measure-Object -Property Length -Sum).Sum
    return [long]($bundleBytes + $postgresBytes)
}

# Pascal source of FillRegions(Names, Codes) for the wizard's country list: one entry per region profile in the bundle.
function New-RegionsInclude([string]$BundlePath, [string]$TargetPath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($BundlePath)
    try {
        $codes = foreach ($zipEntry in $zip.Entries) {
            $name = $zipEntry.FullName.Replace([string][char]92, '/')
            if ($name.StartsWith($RegionProfilePrefix) -and $name.EndsWith('.json')) {
                $file = $name.Substring($RegionProfilePrefix.Length)
                if ($file.IndexOf('/') -lt 0) { [IO.Path]::GetFileNameWithoutExtension($file).ToUpperInvariant() }
            }
        }
    }
    finally {
        $zip.Dispose()
    }
    foreach ($code in $codes) {
        if ($code -cnotmatch '^[A-Z]{2}$') { throw "Region profile '$code' is not an ISO country code; it would be compiled into the setup script." }
    }
    $regions = foreach ($code in ($codes | Sort-Object -Unique)) {
        $label = $code
        try { $label = (New-Object Globalization.RegionInfo($code)).EnglishName } catch { }
        [pscustomobject]@{ Code = $code; Name = $label }
    }
    if (-not $regions) { throw 'The bundle contains no region profiles; the wizard would offer no country.' }
    $lines = @('procedure FillRegions(Names, Codes: TStrings);', 'begin')
    foreach ($region in ($regions | Sort-Object Name)) {
        $lines += "  Codes.Add('$($region.Code)'); Names.Add('$($region.Name.Replace("'", "''"))');"
    }
    $lines += 'end;'
    Set-Content -Path $TargetPath -Value $lines -Encoding utf8BOM
    Write-Host "Wizard countries: $(@($regions).Count)"
}

# Signed PostgreSQL engine of deploy/postgres/engine.json for $Rid, extracted below $TargetDirectory. Same release and file
# names as New-EngineManifestBlock.sh; nothing is extracted before RID, URL, signature and SHA-256 are verified.
function Expand-SignedEngine([string]$TargetDirectory) {
    $pin = Get-Content -Raw $EngineJsonPath | ConvertFrom-Json
    $native = $pin.native.$Rid
    if (-not $native) { throw "deploy/postgres/engine.json has no native entry for $Rid." }
    $tag = "postgres-engine-$($pin.version)-pgvector$($pin.pgvector)$($native.releaseSuffix)"
    $baseName = "klacks-postgres-$($pin.version)-$Rid"
    $expectedUrl = "$EngineReleaseBase/$tag/$baseName.zip"
    $download = Join-Path $TargetDirectory 'download'
    New-Item -ItemType Directory -Force -Path $download | Out-Null
    $entryPath = $EngineEntry
    $zipPath = $EngineZip
    if (-not $entryPath) {
        $entryPath = Join-Path $download "$baseName.json"
        Invoke-WebRequest -UseBasicParsing -Uri "$EngineReleaseBase/$tag/$baseName.json" -OutFile $entryPath
    }
    $entry = Get-Content -Raw $entryPath | ConvertFrom-Json
    if ($entry.rid -ne $Rid) { throw "The engine entry is for '$($entry.rid)', the package is $Rid." }
    if ($entry.bundleUrl -ne $expectedUrl) { throw "The engine entry names '$($entry.bundleUrl)', expected '$expectedUrl'." }
    & $EngineEntryScript -EntryPath $entryPath -Major ([int]$pin.major) -PublicKeyPath $PublicKeyPath -Verify
    if (-not $zipPath) {
        $zipPath = Join-Path $download "$baseName.zip"
        Invoke-WebRequest -UseBasicParsing -Uri $expectedUrl -OutFile $zipPath
    }
    $engineSha = (Get-FileHash -Algorithm SHA256 $zipPath).Hash.ToLowerInvariant()
    if ($engineSha -ne $entry.sha256) { throw "The engine ZIP has SHA-256 $engineSha, the signed entry names $($entry.sha256)." }
    $engineRoot = Join-Path $TargetDirectory 'pgsql'
    Expand-Archive -Path $zipPath -DestinationPath $engineRoot
    Write-Host "PostgreSQL engine: $tag/$baseName.zip (SHA-256 $engineSha, signature verified)"
    return $engineRoot
}

foreach ($required in @($BundleZip, $BundleEntry, $VcRedistPath, $PublicKeyPath)) {
    if (-not (Test-Path $required)) { throw "Missing input '$required'." }
}
$engineWork = ''
if ($Rid -eq 'win-x64') {
    if (-not $PostgresDirectory) { throw 'win-x64 needs -PostgresDirectory (EDB pgsql folder with pgvector).' }
}
else {
    if ($PostgresDirectory) { throw "$Rid takes the signed engine release, not -PostgresDirectory." }
    if ([bool]$EngineZip -ne [bool]$EngineEntry) { throw 'Pass -EngineZip and -EngineEntry together.' }
    $engineWork = Join-Path ([IO.Path]::GetTempPath()) "klacks-engine-$([guid]::NewGuid().ToString('N'))"
    $PostgresDirectory = Expand-SignedEngine $engineWork
}
if (-not (Test-Path (Join-Path $PostgresDirectory 'lib\vector.dll'))) { throw "pgvector (lib\vector.dll) is missing below '$PostgresDirectory'." }

$entry = Get-Content -Raw $BundleEntry | ConvertFrom-Json
$sha256 = (Get-FileHash -Algorithm SHA256 $BundleZip).Hash.ToLowerInvariant()
if ($entry.sha256 -ne $sha256) { throw "Bundle SHA-256 $sha256 does not match the entry ($($entry.sha256))." }
if ($entry.rid -ne $Rid) { throw "The bundle is for '$($entry.rid)', the package is $Rid." }
if (-not $entry.signature) { throw 'The bundle entry is unsigned; a package must carry a signed bundle.' }

$staging = Join-Path ([IO.Path]::GetTempPath()) "klacks-office-pc-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $staging, (Join-Path $staging 'scripts'), (Join-Path $staging 'pgsql'), $OutputDirectory | Out-Null
try {
    $bundleName = Split-Path -Leaf $BundleZip
    Copy-Item $BundleZip (Join-Path $staging $bundleName)
    foreach ($part in $PostgresParts) {
        Copy-Item -Recurse (Join-Path $PostgresDirectory $part) (Join-Path $staging "pgsql\$part")
    }
    Copy-Item $VcRedistPath (Join-Path $staging $VcRedistFileName)
    Copy-Item $PublicKeyPath (Join-Path $staging 'update-public-key.pem')
    foreach ($script in $Scripts) {
        Copy-Item (Join-Path $PSScriptRoot $script) (Join-Path $staging "scripts\$script")
    }
    foreach ($template in $PackageTemplates) {
        Copy-Item (Join-Path $PSScriptRoot "package\$template") (Join-Path $staging $template)
    }
    [ordered]@{ version = $Version; rid = $Rid; bundle = $bundleName; sha256 = $sha256 } |
        ConvertTo-Json | Set-Content -Encoding utf8NoBOM (Join-Path $staging 'package.json')

    $packagePath = Join-Path $OutputDirectory $PackageFileName
    if (Test-Path $packagePath) { Remove-Item $packagePath }
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $packagePath, [IO.Compression.CompressionLevel]::Optimal, $false)

    if ($BuildInstaller) {
        $iscc = Find-Iscc $IsccPath
        $regionsInclude = Join-Path (Split-Path -Parent $InstallerScript) $RegionsIncludeName
        New-RegionsInclude $BundleZip $regionsInclude
        $versionCore = ($Version -split '-')[0]
        # ISCC resolves a relative /DOutputDir against the folder of the .iss, not the current directory.
        $absoluteOutput = (Resolve-Path $OutputDirectory).Path
        $installedBytes = Get-InstalledProgramBytes $BundleZip (Join-Path $staging 'pgsql')
        Write-Host "Installed size: $([math]::Round($installedBytes / 1MB)) MB"
        $isccArguments = @('/Q', "/DVersion=$Version", "/DVersionCore=$versionCore", "/DSourceDir=$staging", "/DOutputDir=$absoluteOutput",
            "/DInstalledSizeBytes=$installedBytes", "/DArch=$Arch")
        if ($SignToolCommand) { $isccArguments += @('/DSign', "/Ssigntool=$SignToolCommand") }
        $isccArguments += $InstallerScript
        & $iscc @isccArguments
        if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed with exit code $LASTEXITCODE." }
        $installerPath = Join-Path $absoluteOutput $InstallerFileName
        if (-not (Test-Path $installerPath)) { throw "Inno Setup did not produce $installerPath." }
        Write-Host "Installer: $installerPath ($([math]::Round((Get-Item $installerPath).Length / 1MB)) MB)"
    }
}
finally {
    if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
    if ($engineWork -and (Test-Path $engineWork)) { Remove-Item -Recurse -Force $engineWork }
}

Write-Host "Package: $packagePath ($([math]::Round((Get-Item $packagePath).Length / 1MB)) MB)"
