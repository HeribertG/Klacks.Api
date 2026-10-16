# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds the office-PC package klacks-office-pc-win-x64.zip from a signed native bundle: bundle (unchanged bytes),
# PostgreSQL with pgvector (bin, lib, share only), the Visual C++ redistributable, the public update key, the install
# scripts and the launcher. The file name is fixed so releases/latest/download/klacks-office-pc-win-x64.zip always
# serves the newest package. Used by .github/workflows/native-windows.yml.
#
# Parameters: BundleZip/BundleEntry = output of build-native-bundle.ps1; PostgresDirectory = EDB pgsql folder with
# pgvector installed; VcRedistPath = vc_redist.x64.exe; PublicKeyPath = PEM of the update signing key.
# -BuildInstaller additionally compiles Klacks-Setup-win-x64.exe from the same staging folder with Inno Setup
# (installer\Klacks.iss); IsccPath defaults to ISCC.exe on PATH or the Inno Setup 6 install folders. SignToolCommand is
# an optional Inno SignTool command line (e.g. 'signtool sign ... $f'); when given, the setup and uninstaller are signed.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BundleZip,
    [Parameter(Mandatory)][string]$BundleEntry,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$PostgresDirectory,
    [Parameter(Mandatory)][string]$VcRedistPath,
    [Parameter(Mandatory)][string]$PublicKeyPath,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [switch]$BuildInstaller,
    [string]$IsccPath = '',
    [string]$SignToolCommand = ''
)

$ErrorActionPreference = 'Stop'

$PackageFileName = 'klacks-office-pc-win-x64.zip'
$Rid = 'win-x64'
$PostgresParts = @('bin', 'lib', 'share')
$Scripts = @('Install-Klacks.ps1', 'Install-KlacksPackage.ps1', 'Uninstall-Klacks.ps1', 'Get-KlacksDiagnostics.ps1')
$PackageTemplates = @('Installieren.cmd', 'LIESMICH.txt', 'README.txt')
$InstallerScript = Join-Path $PSScriptRoot 'installer\Klacks.iss'
$RegionsIncludeName = 'Regions.generated.inc'
$RegionProfilePrefix = 'app/deploy/onprem/regions/'
$InstallerFileName = 'Klacks-Setup-win-x64.exe'
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

foreach ($required in @($BundleZip, $BundleEntry, $VcRedistPath, $PublicKeyPath)) {
    if (-not (Test-Path $required)) { throw "Missing input '$required'." }
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
    Copy-Item $VcRedistPath (Join-Path $staging 'vc_redist.x64.exe')
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
        $isccArguments = @('/Q', "/DVersion=$Version", "/DVersionCore=$versionCore", "/DSourceDir=$staging", "/DOutputDir=$absoluteOutput")
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
}

Write-Host "Package: $packagePath ($([math]::Round((Get-Item $packagePath).Length / 1MB)) MB)"
