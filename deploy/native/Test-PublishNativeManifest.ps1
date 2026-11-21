# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Local checks for Publish-NativeManifest.ps1: a valid entry merges and leaves every Docker field untouched, an entry of
# the other RID (win-x64 / win-arm64) stays as it was, so both RIDs can be merged one after the other; a wrong hash, URL,
# version, RID or signature fails before anything is written. Run: pwsh deploy/native/Test-PublishNativeManifest.ps1

#Requires -Version 7

$ErrorActionPreference = 'Stop'
$publish = Join-Path $PSScriptRoot 'Publish-NativeManifest.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "publish-native-test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $work | Out-Null
$failures = 0

function New-Case($name, [scriptblock]$mutate, [bool]$expectSuccess, [string]$expectedError = '', [string]$rid = 'win-x64',
    [string[]]$expectedRids = @('win-x64')) {
    $version = '1.0.43'
    $url = "https://github.com/HeribertG/Klacks.Api/releases/download/v$version/klacks-$version-$rid.zip"
    $bundle = Join-Path $work "$name.zip"
    [IO.File]::WriteAllBytes($bundle, [byte[]](1..64))
    $sha = (Get-FileHash -Algorithm SHA256 $bundle).Hash.ToLowerInvariant()
    $payload = [Text.Encoding]::UTF8.GetBytes("$version`n$url`n$sha")
    $signature = [Convert]::ToBase64String($script:rsa.SignData($payload, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1))
    $case = [ordered]@{
        Version = $version; Url = $url; Bundle = $bundle
        Entry = [ordered]@{ rid = $rid; bundleUrl = $url; sha256 = $sha; signature = $signature }
        Manifest = '{"channel":"Stable","latestVersion":"1.0.43","minUpgradableFrom":"1.0.0","releasedAt":"2026-10-14T08:00:00Z","containsMigrations":true,"artifacts":{"docker":{"apiImage":"ghcr.io/heribertg/klacks-api:1.0.43","uiImage":"ghcr.io/heribertg/klacks-ui:1.0.43","sha256":"sha256:abc"}},"signature":"DOCKER+SIG/=="}'
    }
    & $mutate $case
    $entryPath = Join-Path $work "$name.entry.json"; $case.Entry | ConvertTo-Json | Set-Content $entryPath
    $manifestPath = Join-Path $work "$name.stable.json"; [IO.File]::WriteAllText($manifestPath, $case.Manifest)
    $outPath = Join-Path $work "$name.out.json"
    $succeeded = $true
    try {
        & $publish -ManifestPath $manifestPath -EntryPath $entryPath -BundlePath $case.Bundle -PublicKeyPath $script:publicKeyPath `
            -Version $case.Version -ExpectedBundleUrl $case.Url -OutputPath $outPath -Rid $rid | Out-Null
    }
    catch { $succeeded = $false; $message = $_.Exception.Message }
    if ($succeeded -ne $expectSuccess) {
        $script:failures++
        Write-Host "FAIL $name (succeeded=$succeeded) $message" -ForegroundColor Red
        return
    }
    if (-not $expectSuccess -and $message -notmatch $expectedError) {
        $script:failures++
        Write-Host "FAIL $name (wrong reason) $message" -ForegroundColor Red
        return
    }
    if ($expectSuccess) {
        $merged = Get-Content -Raw $outPath | ConvertFrom-Json -DateKind String
        $mergedRids = @($merged.artifacts.native | ForEach-Object { $_.rid })
        $kept = @($merged.artifacts.native | Where-Object { $_.rid -ne $rid })
        $added = @($merged.artifacts.native | Where-Object { $_.rid -eq $rid })
        if (($mergedRids -join ',') -cne ($expectedRids -join ',') -or $added.Count -ne 1 -or $added[0].sha256 -ne $case.Entry.sha256 -or
            @($kept | Where-Object { $_.bundleUrl -ne 'kept' }).Count -ne 0 -or
            $merged.signature -ne 'DOCKER+SIG/==' -or $merged.artifacts.docker.sha256 -ne 'sha256:abc' -or
            $merged.releasedAt -ne '2026-10-14T08:00:00Z' -or -not (Get-Content -Raw $outPath).Contains('"DOCKER+SIG/=="')) {
            $script:failures++; Write-Host "FAIL $name (merged content)" -ForegroundColor Red; return
        }
    }
    Write-Host "ok   $name"
}

$script:rsa = [Security.Cryptography.RSA]::Create(2048)
$script:publicKeyPath = Join-Path $work 'public.pem'
Set-Content $script:publicKeyPath $script:rsa.ExportSubjectPublicKeyInfoPem()
try {
    New-Case 'valid' { } $true
    New-Case 'existing-native-replaced' { param($c) $c.Manifest = $c.Manifest.Replace('"artifacts":{', '"artifacts":{"native":[{"rid":"win-x64","bundleUrl":"old","sha256":"old","signature":"old"}],') } $true
    $otherEntry = '"artifacts":{"native":[{"rid":"OTHER","bundleUrl":"kept","sha256":"kept","signature":"kept"}],'
    New-Case 'x64-keeps-arm64' { param($c) $c.Manifest = $c.Manifest.Replace('"artifacts":{', $otherEntry.Replace('OTHER', 'win-arm64')) } $true '' 'win-x64' @('win-arm64', 'win-x64')
    New-Case 'arm64-keeps-x64' { param($c) $c.Manifest = $c.Manifest.Replace('"artifacts":{', $otherEntry.Replace('OTHER', 'win-x64')) } $true '' 'win-arm64' @('win-x64', 'win-arm64')
    New-Case 'arm64-replaces-arm64' { param($c) $c.Manifest = $c.Manifest.Replace('"artifacts":{', '"artifacts":{"native":[{"rid":"win-x64","bundleUrl":"kept","sha256":"kept","signature":"kept"},{"rid":"win-arm64","bundleUrl":"old","sha256":"old","signature":"old"}],') } $true '' 'win-arm64' @('win-x64', 'win-arm64')
    New-Case 'arm64-entry-as-x64' { param($c) $c.Entry.rid = 'win-arm64' } $false 'Entry RID'
    New-Case 'wrong-sha' { param($c) $c.Entry.sha256 = ('0' * 64) } $false 'Downloaded bundle has SHA-256'
    New-Case 'uppercase-sha' { param($c) $c.Entry.sha256 = $c.Entry.sha256.ToUpperInvariant() } $false 'lowercase hex'
    New-Case 'latest-url' { param($c) $c.Entry.bundleUrl = 'https://github.com/HeribertG/Klacks.Api/releases/latest/download/klacks-1.0.43-win-x64.zip' } $false 'Entry URL'
    New-Case 'version-mismatch' { param($c) $c.Manifest = $c.Manifest.Replace('"latestVersion":"1.0.43"', '"latestVersion":"1.0.42"') } $false 'stable.json is for'
    New-Case 'wrong-rid' { param($c) $c.Entry.rid = 'osx-arm64' } $false 'Entry RID'
    New-Case 'bad-signature' { param($c) $c.Entry.signature = [Convert]::ToBase64String([byte[]]::new(256)) } $false 'does not verify'
    New-Case 'no-docker' { param($c) $c.Manifest = '{"latestVersion":"1.0.43","artifacts":{},"signature":"DOCKER+SIG/=="}' } $false 'no Docker artifact'
}
finally {
    $script:rsa.Dispose()
    Remove-Item -Recurse -Force $work
}
if ($failures) { throw "$failures case(s) failed." }
Write-Host 'All cases passed.' -ForegroundColor Green
