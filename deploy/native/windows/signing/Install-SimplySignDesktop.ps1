# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# CI-only: installs Certum SimplySign Desktop on a GitHub Windows runner from Certum's MSI after checking its SHA-256 and
# its Authenticode publisher, before jay0lee/certum-cloud-code-sign runs. The action then finds the program installed and
# skips its own download, which it would install without any integrity check. Throws on any mismatch.
#
# Example:
#   .\Install-SimplySignDesktop.ps1

$ErrorActionPreference = 'Stop'

$MsiUrl = 'https://files.certum.eu/software/SimplySignDesktop/Windows/9.4.4.92/SimplySignDesktop-9.4.4.92-64-bit-en.msi'
$MsiSha256 = '8ec420fc27798b86078b7bd02fe7152097e1b3005bab51820eaca8e57df84da3'
$MsiPublisher = 'O=Asseco Data Systems S.A.'
$AppPath = Join-Path $env:ProgramFiles 'Certum\SimplySign Desktop\SimplySignDesktop.exe'
$MsiSuccessCodes = @(0, 3010)

$msi = Join-Path ([IO.Path]::GetTempPath()) 'SimplySignDesktop.msi'
Invoke-WebRequest -UseBasicParsing -Uri $MsiUrl -OutFile $msi
$hash = (Get-FileHash -Algorithm SHA256 $msi).Hash
if ($hash -ne $MsiSha256) { throw "SimplySign MSI hash mismatch ($hash)." }
$signature = Get-AuthenticodeSignature $msi
if ($signature.Status -ne 'Valid') { throw "SimplySign MSI signature is '$($signature.Status)'." }
if ($signature.SignerCertificate.Subject -notlike "*$MsiPublisher*") {
    throw "SimplySign MSI is signed by an unexpected publisher: $($signature.SignerCertificate.Subject)"
}
$process = Start-Process msiexec.exe -ArgumentList @('/i', "`"$msi`"", '/qn', '/norestart') -Wait -PassThru
if ($process.ExitCode -notin $MsiSuccessCodes) { throw "msiexec failed with exit code $($process.ExitCode)." }
if (-not (Test-Path $AppPath)) { throw "SimplySign Desktop is not at $AppPath; the action would download it unverified." }
Write-Host "SimplySign Desktop installed from the verified MSI."
