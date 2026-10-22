# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Removes a native Klacks installation made by Install-Klacks.ps1: stops and deletes the services Klacks, KlacksUpdater,
# KlacksStt (when present) and KlacksPostgres and deletes the program directory. The data directory (database,
# configuration, backups, speech models) is kept unless -RemoveData is given, so a later installation can take it over. Run in an elevated PowerShell.
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

$ServiceNames = @('KlacksUpdater', 'KlacksStt', 'Klacks', 'KlacksPostgres')
$ServiceStopTimeout = [TimeSpan]::FromMinutes(2)
$KlacksProcessNames = @('Klacks.Api', 'Klacks.Updater', 'Klacks.Stt')
$ProcessExitTimeoutSeconds = 60
$RemoveAttempts = 10
$RemoveRetryDelaySeconds = 3

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }

function Stop-KlacksProcesses([string]$Root) {
    $prefix = $Root.TrimEnd([char]92) + [char]92
    $running = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $path = $null
        try { $path = $_.Path } catch { }
        ($KlacksProcessNames -contains $_.ProcessName) -or ($path -and $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase))
    })
    if ($running.Count -eq 0) { return }
    Write-Step "Ending remaining Klacks processes ($((@($running | ForEach-Object { $_.ProcessName }) | Sort-Object -Unique) -join ', '))"
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    $running | Wait-Process -Timeout $ProcessExitTimeoutSeconds -ErrorAction SilentlyContinue
}

# A file handle can outlive its process for a moment (slow machines, emulation on ARM64, virus scanners), so a locked
# file is retried instead of failing the whole uninstallation.
function Remove-DirectoryWithRetry([string]$Path) {
    for ($attempt = 1; $attempt -le $RemoveAttempts; $attempt++) {
        try {
            Remove-Item -Recurse -Force $Path
            return
        }
        catch {
            if ($attempt -eq $RemoveAttempts) { throw }
            Write-Warning "Files in $Path are still in use, retrying ($attempt/$RemoveAttempts): $($_.Exception.Message)"
            Start-Sleep -Seconds $RemoveRetryDelaySeconds
        }
    }
}

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

Stop-KlacksProcesses $ProgramRoot

if (Test-Path $ProgramRoot) {
    Write-Step "Deleting $ProgramRoot"
    $currentLink = Join-Path $ProgramRoot 'current'
    if (Test-Path $currentLink) { cmd /c rmdir "$currentLink" | Out-Null }
    Remove-DirectoryWithRetry $ProgramRoot
}

if ($RemoveData) {
    if (Test-Path $DataRoot) {
        Write-Step "Deleting $DataRoot (database, configuration, backups)"
        Remove-DirectoryWithRetry $DataRoot
    }
}
elseif (Test-Path $DataRoot) {
    Write-Host "Data kept in $DataRoot. Delete it with -RemoveData if you no longer need it." -ForegroundColor Yellow
}

Write-Host 'Klacks has been removed.' -ForegroundColor Green
