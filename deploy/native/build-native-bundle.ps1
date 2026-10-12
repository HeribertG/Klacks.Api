# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds one native Klacks bundle (self-contained API + built UI) for a runtime identifier and writes the
# manifest entry the native updater expects: { rid, bundleUrl, sha256, signature }. The signature covers
# "{version}\n{bundleUrl}\n{sha256}" (Klacks.Updater ArtifactSignaturePayload) with RSA-SHA256/PKCS#1, exactly
# like the Docker manifest. Local tool for Phase 3-5 of docs/plans/native-installer-windows-mac-2026-10-12.md;
# it is deliberately not wired into the release workflow yet.
#
# Bundle layout:  app/      dotnet publish output (Klacks.Api.exe), version stamped like the Dockerfile does
#                 ui/       Angular browser build (index.html)
#                 updater/  Klacks.Updater publish output (only with -UpdaterProject)
#
# Example:
#   pwsh deploy/native/build-native-bundle.ps1 -Rid win-x64 -Version 1.0.40 `
#        -UiDist ..\Klacks.Ui\dist\klacks.ui\browser -OutputDirectory C:\temp\bundles `
#        -BundleBaseUrl https://github.com/HeribertG/Klacks.Api/releases/download/v1.0.40 `
#        -SigningKeyPath C:\keys\update-private.pem

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('win-x64', 'win-arm64', 'osx-arm64')][string]$Rid,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$UiDist,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$BundleBaseUrl,
    [string]$SigningKeyPath = '',
    [string]$UpdaterProject = '',
    [string]$BuildKey = 'local'
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot '..\..\Klacks.Api.csproj' | Resolve-Path
if (-not (Test-Path (Join-Path $UiDist 'index.html'))) {
    throw "UiDist '$UiDist' contains no index.html."
}

$constantsDirectory = Join-Path $PSScriptRoot '..\..\Application\Constants' | Resolve-Path
$versionFile = Join-Path $constantsDirectory 'VersionConstant.cs'
$variantFile = Join-Path $constantsDirectory 'BuildVariantConstant.cs'
$numeric = ($Version -split '-')[0] -split '\.'

function Set-VersionStamp {
    $text = Get-Content -Raw $versionFile
    $text = $text -replace 'public const int CMajor = \d+;', "public const int CMajor = $($numeric[0]);"
    $text = $text -replace 'public const int CMinor = \d+;', "public const int CMinor = $($numeric[1]);"
    $text = $text -replace 'public const int CPatch = \d+;', "public const int CPatch = $($numeric[2]);"
    $text = $text -replace 'public const string CBuildKey = "[^"]*";', "public const string CBuildKey = `"$BuildKey`";"
    $text = $text -replace 'public const string CBuildTimestamp = "[^"]*";', "public const string CBuildTimestamp = `"$((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd'))`";"
    Set-Content -NoNewline -Path $versionFile -Value $text
    $variant = (Get-Content -Raw $variantFile) -replace 'public static readonly string CVar = "[^"]*";', 'public static readonly string CVar = "RELEASE";'
    Set-Content -NoNewline -Path $variantFile -Value $variant
}

$bundleName = "klacks-$Version-$Rid"
$staging = Join-Path ([IO.Path]::GetTempPath()) "$bundleName-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$originalVersion = Get-Content -Raw $versionFile
$originalVariant = Get-Content -Raw $variantFile
try {
    Set-VersionStamp
    dotnet publish $project -c Release -r $Rid --self-contained true -o (Join-Path $staging 'app') | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

    if ($UpdaterProject) {
        dotnet publish $UpdaterProject -c Release -r $Rid --self-contained true -o (Join-Path $staging 'updater') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish of the updater failed ($LASTEXITCODE)." }
    }

    Copy-Item -Recurse -Path $UiDist -Destination (Join-Path $staging 'ui')

    $zipPath = Join-Path $OutputDirectory "$bundleName.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath)
}
finally {
    Set-Content -NoNewline -Path $versionFile -Value $originalVersion
    Set-Content -NoNewline -Path $variantFile -Value $originalVariant
    if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
}

$sha256 = (Get-FileHash -Algorithm SHA256 $zipPath).Hash.ToLowerInvariant()
$bundleUrl = "$($BundleBaseUrl.TrimEnd('/'))/$bundleName.zip"

$signature = ''
if ($SigningKeyPath) {
    $rsa = [Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem((Get-Content -Raw $SigningKeyPath))
        $payload = [Text.Encoding]::UTF8.GetBytes("$Version`n$bundleUrl`n$sha256")
        $signature = [Convert]::ToBase64String($rsa.SignData(
            $payload, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1))
    }
    finally {
        $rsa.Dispose()
    }
}

$entry = [ordered]@{ rid = $Rid; bundleUrl = $bundleUrl; sha256 = $sha256; signature = $signature }
$entryPath = Join-Path $OutputDirectory "$bundleName.json"
$entry | ConvertTo-Json | Set-Content -Encoding utf8 $entryPath

Write-Host "Bundle:   $zipPath"
Write-Host "SHA-256:  $sha256"
Write-Host "Manifest: $entryPath"
if (-not $signature) { Write-Warning 'No -SigningKeyPath given: the entry is unsigned and will never be selected.' }
