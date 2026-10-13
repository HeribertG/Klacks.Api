# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Collects everything needed to diagnose a native Klacks installation into one text file next to this script (e.g. on
# the USB stick it was started from), so nobody has to copy console output. Secrets are never written: the settings
# file is printed with every password, secret, certificate and connection string masked. Run in an elevated PowerShell;
# without elevation the protected configuration and the database counts are skipped.
#
# Example:
#   D:\Get-KlacksDiagnostics.ps1

[CmdletBinding()]
param(
    [string]$ReportDirectory = '',
    [string]$ProgramRoot = (Join-Path $env:ProgramFiles 'Klacks'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'Klacks'),
    [int]$HttpPort = 5180,
    [int]$PostgresPort = 5439
)

$ErrorActionPreference = 'Continue'

# Windows PowerShell 5.1 leaves $PSScriptRoot empty in parameter defaults.
if (-not $ReportDirectory) { $ReportDirectory = $PSScriptRoot }

$ServiceNames = @('Klacks', 'KlacksUpdater', 'KlacksPostgres')
$ProcessNames = @('Klacks.Api', 'Klacks.Updater', 'postgres')
$EventProviders = @('Klacks.Api', 'Klacks.Updater', 'PostgreSQL', '.NET Runtime', 'Application Error')
$EventLookbackHours = 24
$MaxEvents = 80
$MaxEventMessageLength = 1500
$HttpTimeoutSeconds = 10
$DevelopmentApiMarker = 'localhost:5001'
$SecretNamePattern = 'password|passphrase|pwd|secret|certificate|connectionstring|credential|apikey|token|key$'
$Masked = '***'
$PostgresUser = 'klacks'
$DatabaseName = 'Klacks'

$reportFile = Join-Path $ReportDirectory ('Klacks-Diagnose-{0:yyyyMMdd-HHmmss}.txt' -f (Get-Date))
$baseUrl = "http://localhost:$HttpPort"
$configFile = Join-Path $DataRoot 'config\klacks.native.json'
$secretsFile = Join-Path $DataRoot 'config\secrets.json'

function Write-Report([string]$text) { Add-Content -Path $reportFile -Value $text -Encoding UTF8 }

function Add-Section([string]$title, [scriptblock]$block) {
    Write-Host "  $title"
    Write-Report ''
    Write-Report "===== $title ====="
    try {
        $output = & $block 2>&1 | Out-String -Width 250
        Write-Report $output.TrimEnd()
    }
    catch {
        Write-Report "ERROR: $($_.Exception.Message)"
    }
}

function Hide-Secrets($node) {
    if ($node -is [System.Array]) {
        foreach ($item in $node) { Hide-Secrets $item }
        return
    }
    if ($node -is [System.Management.Automation.PSCustomObject]) {
        foreach ($property in $node.PSObject.Properties) {
            if ($property.Name -match $SecretNamePattern) {
                $property.Value = $Masked
            }
            else {
                Hide-Secrets $property.Value
            }
        }
    }
}

function Test-Url([string]$path) {
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds($HttpTimeoutSeconds)
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = $client.GetAsync("$baseUrl$path").GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $preview = if ($body.Length -gt 120) { $body.Substring(0, 120) + '...' } else { $body }
        '{0,-32} HTTP {1} in {2} ms  {3}' -f $path, [int]$response.StatusCode, $watch.ElapsedMilliseconds, ($preview -replace '\s+', ' ')
    }
    catch {
        '{0,-32} FAILED after {1} ms: {2}' -f $path, $watch.ElapsedMilliseconds, $_.Exception.GetBaseException().Message
    }
    finally {
        $client.Dispose()
    }
}

$isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

Write-Host "Writing diagnostics to $reportFile"
Write-Report "Klacks diagnostics, $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'), elevated: $isAdmin"

Add-Section 'Computer' {
    $os = Get-CimInstance Win32_OperatingSystem
    $computer = Get-CimInstance Win32_ComputerSystem
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
    "OS:        $($os.Caption) $($os.Version) build $($os.BuildNumber), $($os.OSArchitecture)"
    "Culture:   $((Get-Culture).Name), UI $((Get-UICulture).Name), time zone $((Get-TimeZone).Id)"
    "CPU:       $($computer.NumberOfLogicalProcessors) logical processors"
    "RAM:       {0:N1} GB total, {1:N1} GB free" -f ($computer.TotalPhysicalMemory / 1GB), ($os.FreePhysicalMemory * 1KB / 1GB)
    "Disk C:    {0:N1} GB free of {1:N1} GB" -f ($disk.FreeSpace / 1GB), ($disk.Size / 1GB)
    "PowerShell $($PSVersionTable.PSVersion)"
}

Add-Section 'Services' {
    Get-Service -Name $ServiceNames -ErrorAction SilentlyContinue | Format-Table Status, Name, StartType -AutoSize
    foreach ($name in $ServiceNames) { sc.exe qc $name }
}

Add-Section 'Processes' {
    Get-Process -Name $ProcessNames -ErrorAction SilentlyContinue |
        Select-Object Name, Id, @{ n = 'CpuSeconds'; e = { [int]$_.CPU } }, @{ n = 'RamMB'; e = { [int]($_.WorkingSet64 / 1MB) } }, StartTime |
        Format-Table -AutoSize
}

Add-Section 'Listening ports' {
    Get-NetTCPConnection -State Listen -LocalPort $HttpPort, $PostgresPort -ErrorAction SilentlyContinue |
        Select-Object LocalAddress, LocalPort, OwningProcess, @{ n = 'Process'; e = { (Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue).Name } } |
        Format-Table -AutoSize
}

Add-Section 'HTTP checks' {
    foreach ($path in @('/api/backend/health', '/api/Version', '/', '/version.json', '/assets/i18n/de.json')) { Test-Url $path }
}

Add-Section 'Program files' {
    $current = Join-Path $ProgramRoot 'current'
    if (Test-Path $current) {
        "current -> $((Get-Item $current).Target)"
        $uiIndex = Join-Path $current 'ui\index.html'
        "ui\index.html present: $(Test-Path $uiIndex)"
        $uiFiles = Get-ChildItem (Join-Path $current 'ui') -Recurse -File -ErrorAction SilentlyContinue
        "ui files: $($uiFiles.Count), assets\i18n\de.json present: $(Test-Path (Join-Path $current 'ui\assets\i18n\de.json'))"
        $devBuild = $uiFiles | Where-Object Extension -eq '.js' | Select-String -SimpleMatch $DevelopmentApiMarker -List | Select-Object -First 1
        "ui is a development build ($DevelopmentApiMarker found): $([bool]$devBuild)"
    }
    else {
        "No 'current' junction below $ProgramRoot"
    }
    Get-ChildItem $ProgramRoot -ErrorAction SilentlyContinue | Format-Table Mode, LastWriteTime, Name -AutoSize
    Get-ChildItem (Join-Path $ProgramRoot 'versions') -ErrorAction SilentlyContinue | Format-Table Mode, LastWriteTime, Name -AutoSize
}

Add-Section 'Data directory' {
    Get-ChildItem $DataRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $size = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        '{0,-20} {1,10:N0} MB' -f $_.Name, ($size / 1MB)
    }
    Get-ChildItem (Join-Path $DataRoot 'Cache\Models') -Recurse -File -ErrorAction SilentlyContinue |
        Select-Object @{ n = 'MB'; e = { [int]($_.Length / 1MB) } }, LastWriteTime, FullName | Format-Table -AutoSize
    Get-ChildItem (Join-Path $DataRoot 'logs') -File -ErrorAction SilentlyContinue | Format-Table Length, LastWriteTime, Name -AutoSize
}

Add-Section 'Settings (secrets masked)' {
    if (-not $isAdmin) { 'Skipped: needs an elevated PowerShell.'; return }
    $config = Get-Content -Raw $configFile | ConvertFrom-Json
    Hide-Secrets $config
    $config | ConvertTo-Json -Depth 6
}

Add-Section 'Database' {
    if (-not $isAdmin) { 'Skipped: needs an elevated PowerShell.'; return }
    $psql = Join-Path $ProgramRoot 'pgsql\bin\psql.exe'
    # The query goes through a file: Windows PowerShell 5.1 strips the double quotes of native arguments.
    $queryFile = Join-Path $env:TEMP 'klacks-diagnostics.sql'
    Set-Content -Path $queryFile -Encoding ASCII -Value (
        'select (select count(1) from "__EFMigrationsHistory") as migrations, ' +
        '(select max(migration_id) from "__EFMigrationsHistory") as last_migration, ' +
        '(select count(1) from knowledge_index) as index_rows, (select count(1) from agent_skills) as skills, ' +
        'current_setting(''TimeZone'') as time_zone, current_setting(''lc_collate'') as collation;')
    $env:PGPASSWORD = (Get-Content -Raw $secretsFile | ConvertFrom-Json).DatabasePassword
    try {
        & $psql -h localhost -p $PostgresPort -U $PostgresUser -d $DatabaseName -x -f $queryFile
    }
    finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        Remove-Item $queryFile -ErrorAction SilentlyContinue
    }
}

Add-Section "Application events (last $EventLookbackHours h)" {
    $since = (Get-Date).AddHours(-$EventLookbackHours)
    Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $since } -ErrorAction SilentlyContinue |
        Where-Object { $EventProviders -contains $_.ProviderName } |
        Select-Object -First $MaxEvents |
        ForEach-Object {
            $message = if ($_.Message) { $_.Message } else { '' }
            if ($message.Length -gt $MaxEventMessageLength) { $message = $message.Substring(0, $MaxEventMessageLength) + ' ...' }
            "[$($_.TimeCreated.ToString('HH:mm:ss'))] $($_.LevelDisplayName) $($_.ProviderName) $($_.Id)`n  $($message -replace "`r?`n", "`n  ")"
        }
}

Add-Section "Service control manager events (last $EventLookbackHours h)" {
    $since = (Get-Date).AddHours(-$EventLookbackHours)
    Get-WinEvent -FilterHashtable @{ LogName = 'System'; ProviderName = 'Service Control Manager'; StartTime = $since } -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -match 'Klacks' } |
        Select-Object -First $MaxEvents |
        ForEach-Object { "[$($_.TimeCreated.ToString('HH:mm:ss'))] $($_.Id) $($_.Message -replace "`r?`n", ' ')" }
}

Write-Host ''
Write-Host "Done. Please bring back this file: $reportFile" -ForegroundColor Green
