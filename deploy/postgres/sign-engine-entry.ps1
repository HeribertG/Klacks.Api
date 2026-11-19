# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Signs or verifies a Windows engine manifest entry { rid, bundleUrl, sha256, signature } written by
# build-engine-artifact.ps1. The signature is RSA-SHA256/PKCS#1 over the UTF-8 bytes (no BOM, no trailing newline) of
# "klacks-postgres-engine\n{major}\n{bundleUrl}\n{sha256}" - the domain-separated payload of the engine upgrade contract,
# which an updater of an older release rejects at its signature check for an application update.
#
# Sign mode (default): needs SigningKeyPath (PEM, the release signing key); writes the signature into the entry and
# verifies it again with the public key derived from that key. Verify mode (-Verify): needs PublicKeyPath only and throws
# when the signature does not verify. Run signing in a step of its own and delete the key file afterwards.
#
# Parameters: EntryPath = entry JSON; Major = target major as in deploy/postgres/engine.json; SigningKeyPath = private key
# PEM; PublicKeyPath = public key PEM; Verify = verify instead of sign.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EntryPath,
    [Parameter(Mandatory)][int]$Major,
    [string]$SigningKeyPath = '',
    [string]$PublicKeyPath = '',
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'

$PayloadPrefix = 'klacks-postgres-engine'
$Sha256Pattern = '^[0-9a-f]{64}$'

$entry = Get-Content -Raw -LiteralPath $EntryPath | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($entry.bundleUrl)) { throw 'The entry has no bundleUrl.' }
if ($entry.sha256 -cnotmatch $Sha256Pattern) { throw "The entry sha256 '$($entry.sha256)' is not 64 lowercase hex characters." }

$payload = [Text.Encoding]::UTF8.GetBytes("$PayloadPrefix`n$Major`n$($entry.bundleUrl)`n$($entry.sha256)")
$hash = [Security.Cryptography.HashAlgorithmName]::SHA256
$padding = [Security.Cryptography.RSASignaturePadding]::Pkcs1

function Test-Signature {
    param([string]$PublicKeyPem, [string]$Signature)
    $rsa = [Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem($PublicKeyPem)
        return $rsa.VerifyData($payload, [Convert]::FromBase64String($Signature), $hash, $padding)
    }
    finally {
        $rsa.Dispose()
    }
}

if ($Verify) {
    if (-not $PublicKeyPath) { throw '-Verify needs -PublicKeyPath.' }
    if ([string]::IsNullOrWhiteSpace($entry.signature)) { throw 'The entry carries no signature.' }
    if (-not (Test-Signature (Get-Content -Raw -LiteralPath $PublicKeyPath) $entry.signature)) {
        throw 'The entry signature does not verify with the update public key.'
    }
    Write-Host 'Engine entry signature verified.'
    return
}

if (-not $SigningKeyPath) { throw 'Signing needs -SigningKeyPath.' }
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $rsa.ImportFromPem((Get-Content -Raw -LiteralPath $SigningKeyPath))
    $signature = [Convert]::ToBase64String($rsa.SignData($payload, $hash, $padding))
    $publicPem = $rsa.ExportSubjectPublicKeyInfoPem()
}
finally {
    $rsa.Dispose()
}
if (-not (Test-Signature $publicPem $signature)) { throw 'The fresh signature does not verify with the derived public key.' }

$entry.signature = $signature
[IO.File]::WriteAllText($EntryPath, ($entry | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
Write-Host "Signed engine entry for major $Major."
