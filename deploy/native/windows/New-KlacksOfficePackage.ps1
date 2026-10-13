# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds the office-PC package klacks-office-pc-win-x64.zip from a signed native bundle: bundle (unchanged bytes),
# PostgreSQL with pgvector (bin, lib, share only), the Visual C++ redistributable, the public update key, the install
# scripts and the launcher. The file name is fixed so releases/latest/download/klacks-office-pc-win-x64.zip always
# serves the newest package. Used by .github/workflows/native-windows.yml.
#
# Parameters: BundleZip/BundleEntry = output of build-native-bundle.ps1; PostgresDirectory = EDB pgsql folder with
# pgvector installed; VcRedistPath = vc_redist.x64.exe; PublicKeyPath = PEM of the update signing key.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BundleZip,
    [Parameter(Mandatory)][string]$BundleEntry,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$PostgresDirectory,
    [Parameter(Mandatory)][string]$VcRedistPath,
    [Parameter(Mandatory)][string]$PublicKeyPath,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$PackageFileName = 'klacks-office-pc-win-x64.zip'
$Rid = 'win-x64'
$PostgresParts = @('bin', 'lib', 'share')
$Scripts = @('Install-Klacks.ps1', 'Install-KlacksPackage.ps1', 'Uninstall-Klacks.ps1', 'Get-KlacksDiagnostics.ps1')
$PackageTemplates = @('Installieren.cmd', 'LIESMICH.txt', 'README.txt')

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
}
finally {
    if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
}

Write-Host "Package: $packagePath ($([math]::Round((Get-Item $packagePath).Length / 1MB)) MB)"
