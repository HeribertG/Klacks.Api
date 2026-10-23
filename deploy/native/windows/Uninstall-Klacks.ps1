# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Removes a native Klacks installation made by Install-Klacks.ps1: stops and deletes the services Klacks, KlacksUpdater,
# KlacksStt (when present) and KlacksPostgres and deletes the program directory. The data directory (database,
# configuration, backups, speech models) is kept unless -RemoveData is given, so a later installation can take it over. Run in an elevated PowerShell.
# Exit code 0 = removed, 1 = something could not be removed (details in the console or -LogPath). The script can be run
# again after a partial failure: services and directories that are already gone are skipped.
#
# Example:
#   .\Uninstall-Klacks.ps1               # keep C:\ProgramData\Klacks
#   .\Uninstall-Klacks.ps1 -RemoveData   # also delete the database and all settings
#   .\Uninstall-Klacks.ps1 -LogPath C:\Temp\uninstall.txt   # also write the output to a report file

[CmdletBinding()]
param(
    [string]$ProgramRoot = (Join-Path $env:ProgramFiles 'Klacks'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'Klacks'),
    [switch]$RemoveData,
    [string]$LogPath
)

$ErrorActionPreference = 'Stop'

$ServiceNames = @('KlacksUpdater', 'KlacksStt', 'Klacks', 'KlacksPostgres')
$ServiceStopTimeout = [TimeSpan]::FromMinutes(2)
$KlacksProcessNames = @('Klacks.Api', 'Klacks.Updater', 'Klacks.Stt')
$ProcessExitTimeoutSeconds = 60
$RemoveAttempts = 10
$RemoveRetryDelaySeconds = 3
$ScExitCodeSuccess = 0
$ScExitCodeServiceDoesNotExist = 1060
$ScExitCodeServiceMarkedForDelete = 1072
$ExitCodeSuccess = 0
$ExitCodeFailed = 1

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }

function Remove-KlacksService([string]$Name) {
    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service) { return }

    if ($service.Status -ne 'Stopped') {
        Write-Step "Stopping service $Name"
        Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
        try { $service.WaitForStatus('Stopped', $ServiceStopTimeout) } catch { Write-Warning "Service $Name did not stop in time." }
    }

    Write-Step "Removing service $Name"
    sc.exe delete $Name | Out-Null
    $exitCode = $LASTEXITCODE
    switch ($exitCode) {
        $ScExitCodeSuccess { }
        $ScExitCodeServiceDoesNotExist { }
        $ScExitCodeServiceMarkedForDelete { Write-Warning "Service $Name is already marked for deletion; Windows removes it once its process has ended." }
        default { throw "Could not remove the service $Name (sc.exe delete exited with $exitCode)." }
    }
}

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

# Junctions ("current", and "current.pending" left by an interrupted update) are unlinked first so that deleting the
# program directory never follows them, also when their target was already deleted by an earlier, failed run.
function Remove-Junctions([string]$Root) {
    Get-ChildItem -LiteralPath $Root -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | ForEach-Object {
        cmd /c rmdir "$($_.FullName)" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not remove the link $($_.FullName)." }
    }
}

# A file handle can outlive its process for a moment (slow machines, emulation on ARM64, virus scanners), so a locked
# file is retried instead of failing the whole uninstallation.
function Remove-DirectoryWithRetry([string]$Path) {
    for ($attempt = 1; $attempt -le $RemoveAttempts; $attempt++) {
        if (-not (Test-Path -LiteralPath $Path)) { return }
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force
            return
        }
        catch {
            if ($attempt -eq $RemoveAttempts) { throw }
            Write-Warning "Files in $Path are still in use, retrying ($attempt/$RemoveAttempts): $($_.Exception.Message)"
            Start-Sleep -Seconds $RemoveRetryDelaySeconds
        }
    }
}

$transcribing = $false
if ($LogPath) {
    try {
        Start-Transcript -LiteralPath $LogPath -Force | Out-Null
        $transcribing = $true
    }
    catch {
        Write-Warning "Could not write the report ${LogPath}: $($_.Exception.Message)"
    }
}

$exitCode = $ExitCodeSuccess
try {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Please run this script in a PowerShell started as administrator.'
    }

    foreach ($name in $ServiceNames) { Remove-KlacksService $name }

    Stop-KlacksProcesses $ProgramRoot

    if (Test-Path -LiteralPath $ProgramRoot) {
        Write-Step "Deleting $ProgramRoot"
        Remove-Junctions $ProgramRoot
        Remove-DirectoryWithRetry $ProgramRoot
    }

    if ($RemoveData) {
        if (Test-Path -LiteralPath $DataRoot) {
            Write-Step "Deleting $DataRoot (database, configuration, backups)"
            Remove-DirectoryWithRetry $DataRoot
        }
    }
    elseif (Test-Path -LiteralPath $DataRoot) {
        Write-Host "Data kept in $DataRoot. Delete it with -RemoveData if you no longer need it." -ForegroundColor Yellow
    }

    Write-Host 'Klacks has been removed.' -ForegroundColor Green
}
catch {
    $exitCode = $ExitCodeFailed
    Write-Host "UNINSTALLATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
}
finally {
    if ($transcribing) { Stop-Transcript | Out-Null }
}

exit $exitCode
