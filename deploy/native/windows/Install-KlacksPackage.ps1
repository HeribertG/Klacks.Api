# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Entry point of the office-PC package (klacks-office-pc-win-x64.zip): checks the bundle against package.json, removes
# the download mark of the extracted files and runs Install-Klacks.ps1 with the PostgreSQL, Visual C++ runtime and
# update key that ship in the package. Everything it prints goes into a report next to the package (USB stick), or to
# the desktop when the package folder is read-only. Started by Installieren.cmd; asks for administrator rights
# itself, asks for the country whose region profile Klacks applies (holidays, time zone, working-time rules; the PC's
# own country is the default) unless -Region is given, and waits for Enter at the end. KLACKS_NO_PAUSE (CI) skips
# both questions; without -Region the installation then has no region profile.
#
# Setup.exe (installer\Klacks.iss) runs it hidden with -Unattended (no questions, no pause), -ReportPath (the report
# goes to a fixed place instead of next to the package), -StatusFile (progress lines for the wizard) and
# -HealthTimeoutSeconds (how long the wizard waits for the first start).
#
# Windows PowerShell 5.1 compatible on purpose.

[CmdletBinding()]
param(
    [string]$Region = '',
    [int]$HttpPort = 5180,
    [switch]$Unattended,
    [string]$ReportPath = '',
    [string]$StatusFile = '',
    [int]$HealthTimeoutSeconds = 0
)

$ErrorActionPreference = 'Stop'

$NoPauseVariable = 'KLACKS_NO_PAUSE'
$RegionProfilePrefix = 'app/deploy/onprem/regions/'
$RegionProfileExtension = '.json'
$NoRegionChoice = '0'
$ReadyMarker = 'READY'
$CompletedMarker = 'COMPLETED'
$FailedMarker = 'FAILED'

# Last line of the status file (see Install-Klacks.ps1): READY = health check passed, COMPLETED = installed but still
# starting, FAILED = the installation threw. Setup.exe reads it; a script that dies without any marker counts as failed.
function Write-StatusOnce([string]$Marker) {
    if (-not $StatusFile) { return }
    # Best effort, see Write-Status in Install-Klacks.ps1: a lost marker must not turn a good install into a failed one.
    try {
        $last = Get-Content -Path $StatusFile -Tail 1 -ErrorAction SilentlyContinue
        if ($last -ne $ReadyMarker) { Add-Content -Path $StatusFile -Value $Marker -Encoding Ascii }
    }
    catch { }
}

function Test-Interactive {
    -not ($Unattended -or [Environment]::GetEnvironmentVariable($NoPauseVariable))
}

function Wait-ForKey {
    if (Test-Interactive) { Read-Host 'Press Enter to close this window' | Out-Null }
}

function Get-BundleRegionCodes([string]$BundlePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($BundlePath)
    try {
        $codes = foreach ($entry in $zip.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            $file = $name.Substring([Math]::Min($name.Length, $RegionProfilePrefix.Length))
            if ($name.StartsWith($RegionProfilePrefix) -and $file.EndsWith($RegionProfileExtension) -and $file.IndexOf('/') -lt 0) {
                [System.IO.Path]::GetFileNameWithoutExtension($file).ToUpperInvariant()
            }
        }
        return @($codes | Sort-Object -Unique)
    }
    finally {
        $zip.Dispose()
    }
}

function Get-CountryName([string]$Code) {
    try { return (New-Object System.Globalization.RegionInfo($Code)).DisplayName }
    catch { return $Code }
}

function Select-Region([string[]]$Codes) {
    $default = ''
    try { $default = [System.Globalization.RegionInfo]::CurrentRegion.TwoLetterISORegionName.ToUpperInvariant() } catch { }
    if ($Codes -notcontains $default) { $default = '' }
    Write-Host ''
    Write-Host 'Which country is this Klacks installation for?' -ForegroundColor Cyan
    Write-Host 'Klacks applies the country''s region profile: public holidays, time zone and working-time rules.'
    Write-Host ('  {0,2}  no region profile (set it up later in Klacks)' -f $NoRegionChoice)
    for ($i = 0; $i -lt $Codes.Count; $i++) {
        Write-Host ('  {0,2}  {1}  {2}' -f ($i + 1), $Codes[$i], (Get-CountryName $Codes[$i]))
    }
    $defaultLabel = if ($default) { "$default $(Get-CountryName $default)" } else { 'no region profile' }
    while ($true) {
        $answer = ([string](Read-Host "Number or country code (Enter = $defaultLabel)")).Trim()
        if (-not $answer) { return $default }
        if ($answer -eq $NoRegionChoice) { return '' }
        $number = 0
        if ([int]::TryParse($answer, [ref]$number) -and $number -ge 1 -and $number -le $Codes.Count) { return $Codes[$number - 1] }
        if ($Codes -contains $answer.ToUpperInvariant()) { return $answer.ToUpperInvariant() }
        Write-Host "Unknown choice '$answer'. Please enter a number from the list or a two-letter country code." -ForegroundColor Yellow
    }
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
if ($ReportPath) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ReportPath) | Out-Null
}
else {
    $ReportPath = Join-Path $packageRoot $reportName
    try {
        [System.IO.File]::WriteAllText($ReportPath, '')
    }
    catch {
        $ReportPath = Join-Path ([Environment]::GetFolderPath('Desktop')) $reportName
    }
}
if ($StatusFile) { $env:KLACKS_STATUS_FILE = $StatusFile }

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

    if (-not $Region -and (Test-Interactive)) {
        $Region = Select-Region (Get-BundleRegionCodes $bundle)
    }
    Write-Host "Region profile: $(if ($Region) { $Region.ToUpperInvariant() } else { 'none' })"

    $arguments = @{
        BundleZip           = $bundle
        Version             = $package.version
        PostgresDirectory   = (Join-Path $packageRoot 'pgsql')
        UpdatePublicKeyPath = (Join-Path $packageRoot 'update-public-key.pem')
        VcRedistPath        = (Join-Path $packageRoot 'vc_redist.x64.exe')
        HttpPort            = $HttpPort
    }
    if ($Region) { $arguments.Region = $Region }
    if ($HealthTimeoutSeconds -gt 0) { $arguments.HealthTimeoutSeconds = $HealthTimeoutSeconds }
    & (Join-Path $PSScriptRoot 'Install-Klacks.ps1') @arguments
    Write-StatusOnce $CompletedMarker
}
catch {
    $failed = $true
    Write-StatusOnce $FailedMarker
    Write-Host "INSTALLATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
}
finally {
    Write-Host "Report: $reportPath"
    Stop-Transcript | Out-Null
}

Wait-ForKey
if ($failed) { exit 1 }
