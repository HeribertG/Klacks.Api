# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Entry point of the office-PC package (klacks-office-pc-win-x64.zip): checks the bundle against package.json, removes
# the download mark of the extracted files and runs Install-Klacks.ps1 with the PostgreSQL, Visual C++ runtime and
# update key that ship in the package. Everything it prints goes into a report next to the package (USB stick), or to
# the desktop when the package folder is read-only. Started by Installieren.cmd; asks for administrator rights
# itself and waits for Enter at the end unless KLACKS_NO_PAUSE is set (CI).
#
# Windows PowerShell 5.1 compatible on purpose.

[CmdletBinding()]
param(
    [string]$Region = '',
    [int]$HttpPort = 5180
)

$ErrorActionPreference = 'Stop'

$NoPauseVariable = 'KLACKS_NO_PAUSE'

function Wait-ForKey {
    if (-not [Environment]::GetEnvironmentVariable($NoPauseVariable)) { Read-Host 'Press Enter to close this window' | Out-Null }
}

$principal = New-Object System.Security.Principal.WindowsPrincipal([System.Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    # A second window with administrator rights runs the installation; this one only starts it. No loop: the new window
    # either has the rights or Start-Process throws (prompt declined).
    $elevatedArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-HttpPort', $HttpPort)
    if ($Region) { $elevatedArguments += @('-Region', $Region) }
    try {
        Start-Process -FilePath 'powershell.exe' -ArgumentList $elevatedArguments -Verb RunAs | Out-Null
        exit 0
    }
    catch {
        Write-Host 'Klacks needs administrator rights. Please start Installieren.cmd again and confirm the Windows prompt with "Yes".' -ForegroundColor Red
        Wait-ForKey
        exit 1
    }
}

$packageRoot = Split-Path -Parent $PSScriptRoot
$reportName = "Klacks-Installationsbericht-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
$reportPath = Join-Path $packageRoot $reportName
try {
    [System.IO.File]::WriteAllText($reportPath, '')
}
catch {
    $reportPath = Join-Path ([Environment]::GetFolderPath('Desktop')) $reportName
}

$failed = $false
Start-Transcript -Path $reportPath | Out-Null
try {
    Get-ChildItem -Recurse -File $packageRoot | Unblock-File
    $package = Get-Content -Raw (Join-Path $packageRoot 'package.json') | ConvertFrom-Json
    $bundle = Join-Path $packageRoot $package.bundle
    $actual = (Get-FileHash -Algorithm SHA256 $bundle).Hash.ToLowerInvariant()
    if ($actual -ne $package.sha256) {
        throw "The bundle $($package.bundle) is damaged (SHA-256 $actual, expected $($package.sha256)). Please download the package again."
    }

    $arguments = @{
        BundleZip           = $bundle
        Version             = $package.version
        PostgresDirectory   = (Join-Path $packageRoot 'pgsql')
        UpdatePublicKeyPath = (Join-Path $packageRoot 'update-public-key.pem')
        VcRedistPath        = (Join-Path $packageRoot 'vc_redist.x64.exe')
        HttpPort            = $HttpPort
    }
    if ($Region) { $arguments.Region = $Region }
    & (Join-Path $PSScriptRoot 'Install-Klacks.ps1') @arguments
}
catch {
    $failed = $true
    Write-Host "INSTALLATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
}
finally {
    Write-Host "Report: $reportPath"
    Stop-Transcript | Out-Null
}

Wait-ForKey
if ($failed) { exit 1 }
