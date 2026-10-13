# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Adds the native bundle entry to a release's stable.json, fail-closed: the entry must name the expected RID, the
# versioned release URL (never latest/download: the signature binds the URL), the SHA-256 of the bundle as actually
# downloaded (lowercase hex), and carry a valid RSA-SHA256/PKCS#1 signature over "{version}\n{bundleUrl}\n{sha256}"
# (Klacks.Updater ArtifactSignaturePayload). Every other field of the manifest - the Docker artifact and its
# signature included - must stay exactly as it was. JsonNode keeps all values as written (no date conversion).
#
# Parameters: ManifestPath = current stable.json; EntryPath = entry written by build-native-bundle.ps1; BundlePath =
# bundle downloaded back from the release; PublicKeyPath = update signing public key (PEM); OutputPath = merged file.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$EntryPath,
    [Parameter(Mandatory)][string]$BundlePath,
    [Parameter(Mandatory)][string]$PublicKeyPath,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$ExpectedBundleUrl,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$Rid = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$Sha256Pattern = '^[0-9a-f]{64}$'
$NativeKey = 'native'

$manifest = [System.Text.Json.Nodes.JsonNode]::Parse((Get-Content -Raw $ManifestPath))
$entry = [System.Text.Json.Nodes.JsonNode]::Parse((Get-Content -Raw $EntryPath))

$latestVersion = [string]$manifest['latestVersion']
if ($latestVersion -ne $Version) { throw "stable.json is for $latestVersion, not $Version." }
if ($null -eq $manifest['artifacts'] -or $null -eq $manifest['artifacts']['docker']) { throw 'stable.json carries no Docker artifact.' }

$entryRid = [string]$entry['rid']
$entryUrl = [string]$entry['bundleUrl']
$entrySha = [string]$entry['sha256']
$entrySignature = [string]$entry['signature']
if ($entryRid -cne $Rid) { throw "Entry RID '$entryRid' is not '$Rid'." }
if ($entryUrl -cne $ExpectedBundleUrl) { throw "Entry URL '$entryUrl' is not '$ExpectedBundleUrl'." }
if ($entrySha -cnotmatch $Sha256Pattern) { throw "Entry SHA-256 '$entrySha' is not 64 lowercase hex characters." }
$actualSha = (Get-FileHash -Algorithm SHA256 $BundlePath).Hash.ToLowerInvariant()
if ($actualSha -cne $entrySha) { throw "Downloaded bundle has SHA-256 $actualSha, the entry says $entrySha." }

$rsa = [System.Security.Cryptography.RSA]::Create()
try {
    $rsa.ImportFromPem((Get-Content -Raw $PublicKeyPath))
    $payload = [System.Text.Encoding]::UTF8.GetBytes("$Version`n$entryUrl`n$entrySha")
    $signature = [Convert]::FromBase64String($entrySignature)
    $valid = $rsa.VerifyData($payload, $signature, [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    if (-not $valid) { throw 'The entry signature does not verify with the update public key.' }
}
finally {
    $rsa.Dispose()
}

$merged = $manifest.DeepClone()
$native = [System.Text.Json.Nodes.JsonArray]::new()
$existing = $merged['artifacts'][$NativeKey]
if ($null -ne $existing) {
    foreach ($item in $existing.AsArray()) {
        if ([string]$item['rid'] -ne $Rid) { $native.Add($item.DeepClone()) }
    }
}
$native.Add($entry.DeepClone())
$merged['artifacts'][$NativeKey] = $native

$before = $manifest.DeepClone(); [void]$before['artifacts'].AsObject().Remove($NativeKey)
$after = $merged.DeepClone(); [void]$after['artifacts'].AsObject().Remove($NativeKey)
if (-not [System.Text.Json.Nodes.JsonNode]::DeepEquals($before, $after)) { throw 'Merging changed fields outside artifacts.native.' }

$options = [System.Text.Json.JsonSerializerOptions]::new()
$options.WriteIndented = $true
[System.IO.File]::WriteAllText($OutputPath, $merged.ToJsonString($options), [System.Text.UTF8Encoding]::new($false))
Write-Host "Merged native $Rid entry for $Version into $OutputPath"
