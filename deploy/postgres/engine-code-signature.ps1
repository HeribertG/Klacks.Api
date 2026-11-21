# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Authenticode signing and checking of the PE files (*.exe, *.dll) below bin\ and lib\ of an unpacked engine tree (the
# self-built win-arm64 engine; see .github/workflows/postgres-engine-artifact-arm64.yml). Whether a file needs our signature
# is decided by its PE certificate table, not by Get-AuthenticodeSignature alone: a CRT DLL whose hash sits in a Windows
# catalog of the build machine reports "Valid (Catalog)" there and "NotSigned" on a customer PC. So every file without an
# embedded signature is signed; a file that already carries an embedded, valid Microsoft signature (part of the app-local
# CRT) is kept as it is; any other embedded signature stops the run.
#
# Modes:
#   Plan    prints and returns the classification (Sign / KeepMicrosoft), changes nothing, needs no certificate
#   Sign    signs every Sign file with signtool (cloud certificate by thumbprint, RFC 3161 timestamp) in batches, then Assert
#   Assert  every PE file has the expected machine (if given) and an embedded signature that verifies, made either with
#           the expected thumbprint and a timestamp or by Microsoft; throws listing every failing file
#
# Parameters: Root = engine folder holding bin\ and lib\; Mode = Plan, Sign or Assert; Thumbprint = SHA-1 thumbprint of our
# signing certificate (Sign, Assert); SignToolPath = signtool.exe (Sign); TimestampUrl = RFC 3161 server (Sign);
# ExpectedMachine = PE machine every file must have, e.g. 0xAA64 (0 = no check); BatchSize = files per signtool call;
# Retries = attempts per batch.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][ValidateSet('Plan', 'Sign', 'Assert')][string]$Mode,
    [string]$Thumbprint = '',
    [string]$SignToolPath = '',
    [string]$TimestampUrl = '',
    [int]$ExpectedMachine = 0,
    [ValidateRange(1, 200)][int]$BatchSize = 20,
    [ValidateRange(1, 10)][int]$Retries = 3
)

$ErrorActionPreference = 'Stop'

$SignedParts = @('bin', 'lib')
$PeExtensions = @('*.exe', '*.dll')
$MicrosoftSubjectPattern = '*O=Microsoft Corporation*'
$RetryDelaySeconds = 20
$DosSignature = 0x5A4D
$NtSignature = 0x00004550
$Pe32Magic = 0x10B
$Pe32PlusMagic = 0x20B
$SecurityDirectoryIndex = 4
$DataDirectoryEntryBytes = 8

# Reads machine and the size of the certificate table (data directory 4) from the PE headers; $null for a non-PE file.
function Get-PeInfo {
    param([string]$File)
    $fs = [IO.File]::OpenRead($File)
    try {
        $br = [IO.BinaryReader]::new($fs)
        if ($fs.Length -lt 0x40 -or $br.ReadUInt16() -ne $DosSignature) { return $null }
        $fs.Position = 0x3C
        $peOffset = $br.ReadInt32()
        if ($peOffset -lt 0 -or $peOffset + 24 -gt $fs.Length) { return $null }
        $fs.Position = $peOffset
        if ($br.ReadUInt32() -ne $NtSignature) { return $null }
        $machine = $br.ReadUInt16()
        $optionalHeader = $peOffset + 24
        $certificateBytes = 0
        if ($optionalHeader + 2 -le $fs.Length) {
            $fs.Position = $optionalHeader
            $magic = $br.ReadUInt16()
            $countOffset = switch ($magic) { $Pe32PlusMagic { 108 } $Pe32Magic { 92 } default { -1 } }
            if ($countOffset -ge 0 -and $optionalHeader + $countOffset + 4 -le $fs.Length) {
                $fs.Position = $optionalHeader + $countOffset
                $count = $br.ReadUInt32()
                $entry = $optionalHeader + $countOffset + 4 + $SecurityDirectoryIndex * $DataDirectoryEntryBytes
                if ($count -gt $SecurityDirectoryIndex -and $entry + $DataDirectoryEntryBytes -le $fs.Length) {
                    $fs.Position = $entry + 4
                    $certificateBytes = $br.ReadUInt32()
                }
            }
        }
        return [pscustomobject]@{ Machine = $machine; HasEmbeddedSignature = $certificateBytes -gt 0 }
    }
    finally {
        $fs.Dispose()
    }
}

function Get-EnginePeFiles {
    $dirs = foreach ($part in $SignedParts) {
        $dir = Join-Path $Root $part
        if (-not (Test-Path -LiteralPath $dir)) { throw "'$part' is missing below '$Root'." }
        $dir
    }
    return @(Get-ChildItem -LiteralPath $dirs -Recurse -File -Include $PeExtensions | Sort-Object FullName)
}

# The embedded signer is read from the certificate table itself: where Windows holds a catalog for the file's hash,
# Get-AuthenticodeSignature reports the catalog signer (and type Catalog) instead of the embedded one. Status Valid then
# still proves the file is the one Microsoft signed.
function Test-MicrosoftEmbedded {
    param([string]$Path, $Signature)
    if ($Signature.Status -ne 'Valid') { return $false }
    try { $embedded = [Security.Cryptography.X509Certificates.X509Certificate]::CreateFromSignedFile($Path) }
    catch { return $false }
    return $embedded.Subject -like $MicrosoftSubjectPattern
}

function Get-SigningPlan {
    $plan = foreach ($file in Get-EnginePeFiles) {
        $info = Get-PeInfo $file.FullName
        if ($null -eq $info) { throw "$($file.FullName) is not a PE file." }
        $action = if (-not $info.HasEmbeddedSignature) { 'Sign' }
            elseif (Test-MicrosoftEmbedded $file.FullName (Get-AuthenticodeSignature -LiteralPath $file.FullName)) { 'KeepMicrosoft' }
            else { throw "$($file.FullName) already carries an embedded signature that is not a valid Microsoft one." }
        [pscustomobject]@{ Path = $file.FullName; Action = $action }
    }
    return @($plan)
}

function Invoke-Assert {
    $failures = [Collections.Generic.List[string]]::new()
    $ours = 0
    $microsoft = 0
    foreach ($file in Get-EnginePeFiles) {
        $info = Get-PeInfo $file.FullName
        if ($null -eq $info) { $failures.Add("$($file.FullName): not a PE file"); continue }
        if ($ExpectedMachine -ne 0 -and $info.Machine -ne $ExpectedMachine) {
            $failures.Add(('{0}: machine 0x{1:X4}, expected 0x{2:X4}' -f $file.FullName, $info.Machine, $ExpectedMachine)); continue
        }
        if (-not $info.HasEmbeddedSignature) { $failures.Add("$($file.FullName): no embedded signature"); continue }
        $signature = Get-AuthenticodeSignature -LiteralPath $file.FullName
        if ($signature.Status -ne 'Valid') {
            $failures.Add("$($file.FullName): signature $($signature.Status) ($($signature.SignatureType))"); continue
        }
        if ($signature.SignatureType -eq 'Authenticode' -and $signature.SignerCertificate.Thumbprint -eq $Thumbprint) {
            if (-not $signature.TimeStamperCertificate) { $failures.Add("$($file.FullName): signed without timestamp"); continue }
            $ours++
        }
        elseif (Test-MicrosoftEmbedded $file.FullName $signature) { $microsoft++ }
        else { $failures.Add("$($file.FullName): signed by $($signature.SignerCertificate.Subject)") }
    }
    if ($failures.Count -gt 0) {
        $failures | ForEach-Object { Write-Host $_ }
        throw "$($failures.Count) PE file(s) failed the signature check."
    }
    Write-Host "All $($ours + $microsoft) PE files signed: $ours with $Thumbprint (timestamped), $microsoft by Microsoft."
}

if ($Mode -ne 'Plan' -and $Thumbprint -cnotmatch '^[0-9A-Fa-f]{40}$') { throw 'Thumbprint must be a 40 character SHA-1 thumbprint.' }

switch ($Mode) {
    'Plan' {
        $plan = Get-SigningPlan
        $plan | Group-Object Action | ForEach-Object { Write-Host "$($_.Name): $($_.Count)" }
        return $plan
    }
    'Sign' {
        if (-not $SignToolPath -or -not (Test-Path -LiteralPath $SignToolPath)) { throw 'Sign needs -SignToolPath.' }
        if ($TimestampUrl -cnotmatch '^https?://\S+$') { throw 'Sign needs -TimestampUrl.' }
        $toSign = @(Get-SigningPlan | Where-Object Action -eq 'Sign' | ForEach-Object Path)
        Write-Host "Signing $($toSign.Count) PE file(s) in batches of $BatchSize."
        for ($i = 0; $i -lt $toSign.Count; $i += $BatchSize) {
            $batch = $toSign[$i..([math]::Min($i + $BatchSize, $toSign.Count) - 1)]
            for ($attempt = 1; ; $attempt++) {
                $output = & $SignToolPath sign /sha1 $Thumbprint /fd sha256 /tr $TimestampUrl /td sha256 @batch 2>&1 | Out-String
                if ($LASTEXITCODE -eq 0) { break }
                if ($attempt -ge $Retries) { Write-Host $output; throw "signtool sign failed for batch starting at file $i (exit $LASTEXITCODE)." }
                Write-Host "signtool attempt $attempt failed (exit $LASTEXITCODE), retrying in $RetryDelaySeconds s."
                Start-Sleep -Seconds $RetryDelaySeconds
            }
            Write-Host "Signed $([math]::Min($i + $BatchSize, $toSign.Count)) / $($toSign.Count)"
        }
        Invoke-Assert
    }
    'Assert' { Invoke-Assert }
}
