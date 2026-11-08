# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# CI-only: checks that every given file carries a valid Authenticode signature from the expected certificate and, when a
# signtool path is given, that the signature verifies under the default policy with an RFC 3161 timestamp. Throws on the
# first file that fails, so an unsigned or wrongly signed EXE never passes a gate.
#
# Parameters:
#   Path          Files to check (setup EXE, uninstaller, ...)
#   Thumbprint    SHA-1 thumbprint of the expected signing certificate
#   SignToolPath  Optional path to signtool.exe; enables the policy and timestamp check
#
# Example:
#   .\Assert-CodeSignature.ps1 -Path .\Klacks-Setup-win-x64.exe -Thumbprint AE4D... -SignToolPath $signtool

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$Path,
    [Parameter(Mandatory)][string]$Thumbprint,
    [string]$SignToolPath = ''
)

$ErrorActionPreference = 'Stop'

foreach ($file in $Path) {
    if (-not (Test-Path $file)) { throw "$file does not exist." }
    $signature = Get-AuthenticodeSignature $file
    if ($signature.Status -ne 'Valid') { throw "$file has signature status '$($signature.Status)': $($signature.StatusMessage)" }
    if ($signature.SignerCertificate.Thumbprint -ne $Thumbprint) {
        throw "$file is signed with an unexpected certificate ($($signature.SignerCertificate.Thumbprint))."
    }
    if ($SignToolPath) {
        $output = & $SignToolPath verify /pa /v $file 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { Write-Host $output; throw "signtool verify failed for $file with exit code $LASTEXITCODE." }
        if ($output -notmatch 'The signature is timestamped') { throw "$file carries no timestamp." }
    }
    Write-Host "$file signed by $($signature.SignerCertificate.Subject)"
}
