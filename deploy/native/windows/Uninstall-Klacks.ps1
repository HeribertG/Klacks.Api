# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Removes a native Klacks installation made by Install-Klacks.ps1: stops and deletes the services Klacks, KlacksUpdater
# and KlacksPostgres and deletes the program directory. The data directory (database, configuration, backups) is kept
# unless -RemoveData is given, so a later installation can take it over. Run in an elevated PowerShell.
#
# Example:
#   .\Uninstall-Klacks.ps1               # keep C:\ProgramData\Klacks
#   .\Uninstall-Klacks.ps1 -RemoveData   # also delete the database and all settings

[CmdletBinding()]
param(
    [string]$ProgramRoot = (Join-Path $env:ProgramFiles 'Klacks'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'Klacks'),
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'

$ServiceNames = @('KlacksUpdater', 'Klacks', 'KlacksPostgres')
$ServiceStopTimeout = [TimeSpan]::FromMinutes(2)

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Please run this script in a PowerShell started as administrator.'
}

foreach ($name in $ServiceNames) {
    $service = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $service) { continue }

    if ($service.Status -ne 'Stopped') {
        Write-Step "Stopping service $name"
        Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
        try { $service.WaitForStatus('Stopped', $ServiceStopTimeout) } catch { Write-Warning "Service $name did not stop in time." }
    }

    Write-Step "Removing service $name"
    sc.exe delete $name | Out-Null
}

$apiProcess = Get-Process -Name 'Klacks.Api', 'Klacks.Updater' -ErrorAction SilentlyContinue
if ($apiProcess) {
    Write-Step 'Ending remaining Klacks processes'
    $apiProcess | Stop-Process -Force
}

if (Test-Path $ProgramRoot) {
    Write-Step "Deleting $ProgramRoot"
    $currentLink = Join-Path $ProgramRoot 'current'
    if (Test-Path $currentLink) { cmd /c rmdir "$currentLink" | Out-Null }
    Remove-Item -Recurse -Force $ProgramRoot
}

if ($RemoveData) {
    if (Test-Path $DataRoot) {
        Write-Step "Deleting $DataRoot (database, configuration, backups)"
        Remove-Item -Recurse -Force $DataRoot
    }
}
elseif (Test-Path $DataRoot) {
    Write-Host "Data kept in $DataRoot. Delete it with -RemoveData if you no longer need it." -ForegroundColor Yellow
}

Write-Host 'Klacks has been removed.' -ForegroundColor Green
