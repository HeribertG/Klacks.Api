# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Checks how Install-Klacks.ps1 finds the PostgreSQL folders after an engine upgrade (engine pointer
# <DataRoot>\config\postgres-engine.json, written by the updater).
# Layout part (always; no administrator rights, no PostgreSQL): loads Resolve-PostgresEngineLayout and its helpers from
# Install-Klacks.ps1 through the PowerShell parser, so the shipped implementation is tested, and runs it against folder
# layouts in a temporary directory: no pointer, valid pointer (also with BOM, spaces and an umlaut in the path), and
# the pointers the installer must refuse (missing cluster folder, other major version, unreadable, wrong schema,
# relative path).
# Installer part (only with -BundleZip, -Version and -PostgresDirectory, e.g. on the Windows gate runner): installs into
# a temporary root in process mode (-NoServices), turns that installation into an upgraded layout (pgsql-<major>,
# pgdata-<major>, pointer), re-runs the installer and checks that it created no new cluster and kept the versioned
# folders; a pointer to a missing folder must stop the installer before it creates anything.
# Exit code 0 = every check passed, 1 = at least one failed.
#
# Windows PowerShell 5.1 compatible on purpose, like the installer it tests.
#
# Example:
#   .\Test-PostgresEngineLayout.ps1
#   .\Test-PostgresEngineLayout.ps1 -BundleZip C:\pkg\klacks-1.0.79-win-x64.zip -Version 1.0.79 -PostgresDirectory C:\pkg\pgsql

[CmdletBinding()]
param(
    [string]$InstallScript = '',
    [string]$BundleZip = '',
    [string]$Version = '',
    [string]$PostgresDirectory = '',
    [string]$WorkDirectory = ''
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 leaves $PSScriptRoot empty in parameter defaults.
if (-not $InstallScript) { $InstallScript = Join-Path $PSScriptRoot 'Install-Klacks.ps1' }
if (-not $WorkDirectory) { $WorkDirectory = [System.IO.Path]::GetTempPath() }
# Own subfolder: only what this run created is deleted at the end, never the folder passed in.
$WorkDirectory = Join-Path $WorkDirectory ('klacks-engine-layout-' + [guid]::NewGuid().ToString('N'))

$LoadedFunctions = @('Resolve-PostgresEngineLayout', 'Get-ClusterMajor')
$LoadedConstants = @('EngineLayoutFileName', 'EngineLayoutSchemaVersion', 'LegacyPostgresDirectoryName', 'LegacyDataDirectoryName')
$TestMajor = 19
$OtherMajor = 17
$MissingMajor = 99
$NewerMajor = 20
$InstallerPostgresPort = 55439
$InstallerHealthTimeoutSeconds = 10
$PostgresStopTimeoutSeconds = 60
$ConfigBlockMarker = '# Klacks native installation'
$ClusterMarkerFileName = 'engine-layout-test.marker'
$ExitCodeSuccess = 0
$ExitCodeFailed = 1
# U+00DC (capital U with diaeresis), built at run time: this file has to stay ASCII for Windows PowerShell 5.1.
$Umlaut = [string][char]0x00DC

$script:failures = 0

function Write-Check([string]$name, [bool]$passed, [string]$detail = '') {
    if ($passed) { Write-Host "PASS  $name" -ForegroundColor Green }
    else {
        $script:failures++
        Write-Host "FAIL  $name $detail" -ForegroundColor Red
    }
}

function Test-Throws([string]$name, [scriptblock]$action, [string]$expectedText) {
    try {
        & $action | Out-Null
        Write-Check $name $false '(no error)'
    }
    catch {
        $message = $_.Exception.Message
        Write-Check $name ($message -like "*$expectedText*") "(error: $message)"
    }
}

function Write-Utf8File([string]$path, [string]$text, [bool]$withBom) {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($withBom)))
}

function New-Cluster([string]$dataDirectory, [int]$major) {
    Write-Utf8File (Join-Path $dataDirectory 'PG_VERSION') "$major`n" $false
}

function Write-Pointer([string]$pointerFile, $schemaVersion, $major, [string]$postgresDirectory, [string]$dataDirectory, [bool]$withBom = $false) {
    $pointer = [ordered]@{ schemaVersion = $schemaVersion; major = $major; postgresDirectory = $postgresDirectory; dataDirectory = $dataDirectory }
    Write-Utf8File $pointerFile ($pointer | ConvertTo-Json) $withBom
}

# Loads the named functions and constants of the installer without running it.
$parseTokens = $null
$parseErrors = $null
$installAst = [System.Management.Automation.Language.Parser]::ParseFile($InstallScript, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw "Install-Klacks.ps1 does not parse: $($parseErrors[0].Message)" }
$functionAsts = $installAst.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $false)
foreach ($name in $LoadedFunctions) {
    $definition = $functionAsts | Where-Object { $_.Name -eq $name } | Select-Object -First 1
    if (-not $definition) { throw "Install-Klacks.ps1 defines no function $name." }
    . ([scriptblock]::Create($definition.Extent.Text))
}
foreach ($name in $LoadedConstants) {
    $assignment = $installAst.EndBlock.Statements | Where-Object {
        $_ -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $_.Left -is [System.Management.Automation.Language.VariableExpressionAst] -and
        $_.Left.VariablePath.UserPath -eq $name
    } | Select-Object -First 1
    if (-not $assignment) { throw "Install-Klacks.ps1 defines no constant `$$name." }
    . ([scriptblock]::Create($assignment.Extent.Text))
}

function Stop-TestInstallation([string]$programRoot, [string]$dataRoot) {
    $prefix = $programRoot.TrimEnd([char]92) + [char]92
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $path = $null
        try { $path = $_.Path } catch { }
        $path -and $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -and $_.ProcessName -ne 'postgres'
    } | Stop-Process -Force -ErrorAction SilentlyContinue
    foreach ($binary in @(Get-ChildItem -LiteralPath $programRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "$LegacyPostgresDirectoryName*" })) {
        $pgCtl = Join-Path $binary.FullName 'bin\pg_ctl.exe'
        if (-not (Test-Path -LiteralPath $pgCtl)) { continue }
        foreach ($cluster in @(Get-ChildItem -LiteralPath $dataRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "$LegacyDataDirectoryName*" })) {
            & $pgCtl status -D $cluster.FullName *> $null
            if ($LASTEXITCODE -eq 0) { & $pgCtl stop -D $cluster.FullName -m fast -w -t $PostgresStopTimeoutSeconds *> $null }
        }
    }
}

function Get-FreeTcpPort {
    $listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, 0)
    try { $listener.Start(); return $listener.LocalEndpoint.Port } finally { $listener.Stop() }
}

function Invoke-Installer([string]$programRoot, [string]$dataRoot, [int]$httpPort) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$InstallScript`"", '-BundleZip', "`"$BundleZip`"",
        '-Version', $Version, '-PostgresDirectory', "`"$PostgresDirectory`"", '-ProgramRoot', "`"$programRoot`"",
        '-DataRoot', "`"$dataRoot`"", '-HttpPort', $httpPort, '-PostgresPort', $InstallerPostgresPort,
        '-HealthTimeoutSeconds', $InstallerHealthTimeoutSeconds, '-AllowUnsignedUpdates', '-NoServices')
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -NoNewWindow -PassThru
    $null = $process.Handle
    $process.WaitForExit()
    return $process.ExitCode
}

function Test-LayoutResolution([string]$root) {
    $programRoot = Join-Path $root 'Program Files\Klacks'
    $dataRoot = Join-Path $root ("Daten $Umlaut\Klacks")
    $pointerFile = Join-Path $dataRoot "config\$EngineLayoutFileName"
    $legacyData = Join-Path $dataRoot $LegacyDataDirectoryName
    $versionedRoot = Join-Path $programRoot "$LegacyPostgresDirectoryName-$TestMajor"
    $versionedData = Join-Path $dataRoot "$LegacyDataDirectoryName-$TestMajor"
    New-Cluster $legacyData $OtherMajor

    $layout = Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile
    Write-Check 'no pointer: legacy layout' (($null -eq $layout.Major) -and
        ($layout.PostgresRoot -eq (Join-Path $programRoot $LegacyPostgresDirectoryName)) -and ($layout.DataDirectory -eq $legacyData)) "($($layout | Out-String))"

    New-Cluster $versionedData $TestMajor
    Test-Throws 'no pointer but a versioned cluster: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'pointer'

    foreach ($withBom in @($false, $true)) {
        Write-Pointer $pointerFile $EngineLayoutSchemaVersion $TestMajor $versionedRoot $versionedData $withBom
        $layout = Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile
        Write-Check "pointer (BOM: $withBom): versioned layout with umlaut path" (($layout.Major -eq $TestMajor) -and
            ($layout.PostgresRoot -eq $versionedRoot) -and ($layout.DataDirectory -eq $versionedData)) "($($layout | Out-String))"
    }

    Write-Pointer $pointerFile $EngineLayoutSchemaVersion $TestMajor ($versionedRoot + '\') ($versionedData + '\')
    $layout = Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile
    Write-Check 'pointer with trailing backslashes: accepted' ($layout.DataDirectory -eq $versionedData)

    $missingData = Join-Path $dataRoot "$LegacyDataDirectoryName-$MissingMajor"
    Write-Pointer $pointerFile $EngineLayoutSchemaVersion $MissingMajor (Join-Path $programRoot "$LegacyPostgresDirectoryName-$MissingMajor") $missingData
    Test-Throws 'pointer to a missing cluster folder: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'holds no database'

    $mismatchData = Join-Path $dataRoot "$LegacyDataDirectoryName-$OtherMajor"
    New-Cluster $mismatchData $TestMajor
    Write-Pointer $pointerFile $EngineLayoutSchemaVersion $OtherMajor (Join-Path $programRoot "$LegacyPostgresDirectoryName-$OtherMajor") $mismatchData
    Test-Throws 'pointer major differs from PG_VERSION: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } "PostgreSQL $TestMajor"
    Remove-Item -LiteralPath $mismatchData -Recurse -Force

    $foreignData = Join-Path $root "elsewhere\$LegacyDataDirectoryName-$TestMajor"
    New-Cluster $foreignData $TestMajor
    $foreignFolders = @("$LegacyDataDirectoryName-$TestMajor", $foreignData, "\\server\share\$LegacyDataDirectoryName-$TestMajor",
        (Join-Path $dataRoot "..\$LegacyDataDirectoryName-$TestMajor"))
    foreach ($foreign in $foreignFolders) {
        Write-Pointer $pointerFile $EngineLayoutSchemaVersion $TestMajor $versionedRoot $foreign
        Test-Throws "foreign data folder ('$foreign'): abort" { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'expected'
    }
    Write-Pointer $pointerFile $EngineLayoutSchemaVersion $TestMajor (Join-Path $root "elsewhere\$LegacyPostgresDirectoryName-$TestMajor") $versionedData
    Test-Throws 'foreign binaries folder: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'expected'

    $newerData = Join-Path $dataRoot "$LegacyDataDirectoryName-$NewerMajor"
    New-Cluster $newerData $NewerMajor
    Write-Pointer $pointerFile $EngineLayoutSchemaVersion $TestMajor $versionedRoot $versionedData
    Test-Throws 'newer versioned cluster than the pointer: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'newer database folder'
    Remove-Item -LiteralPath $newerData -Recurse -Force

    $realData = "$versionedData-real"
    Move-Item -LiteralPath $versionedData -Destination $realData
    cmd /c mklink /J "$versionedData" "$realData" | Out-Null
    try {
        Test-Throws 'data folder is a link: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'is a link'
    }
    finally {
        cmd /c rmdir "$versionedData" | Out-Null
        Move-Item -LiteralPath $realData -Destination $versionedData
    }

    Write-Pointer $pointerFile ($EngineLayoutSchemaVersion + 1) $TestMajor $versionedRoot $versionedData
    Test-Throws 'unknown schema version: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'schema version'

    Write-Pointer $pointerFile $EngineLayoutSchemaVersion 'nineteen' $versionedRoot $versionedData
    Test-Throws 'invalid major: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'major version'

    Write-Utf8File $pointerFile '{}' $false
    Test-Throws 'pointer without keys: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'schema version'

    Write-Utf8File $pointerFile '{ "schemaVersion": 1, "major": ' $false
    Test-Throws 'unreadable pointer: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'cannot be read'

    Write-Utf8File $pointerFile '' $false
    Test-Throws 'empty pointer file: abort' { Resolve-PostgresEngineLayout $programRoot $dataRoot $pointerFile } 'pointer'
}

function Test-InstallerRerun([string]$root) {
    $programRoot = Join-Path $root 'Program Files\Klacks'
    $dataRoot = Join-Path $root 'Data\Klacks'
    $pointerFile = Join-Path $dataRoot "config\$EngineLayoutFileName"
    $legacyRoot = Join-Path $programRoot $LegacyPostgresDirectoryName
    $legacyData = Join-Path $dataRoot $LegacyDataDirectoryName
    $httpPort = Get-FreeTcpPort
    try {
        Write-Check 'installer: first installation (legacy layout)' ((Invoke-Installer $programRoot $dataRoot $httpPort) -eq 0)
        Stop-TestInstallation $programRoot $dataRoot
        $major = [int]"$(Get-Content -LiteralPath (Join-Path $legacyData 'PG_VERSION') -TotalCount 1)".Trim()
        $versionedRoot = Join-Path $programRoot "$LegacyPostgresDirectoryName-$major"
        $versionedData = Join-Path $dataRoot "$LegacyDataDirectoryName-$major"

        $missingRoot = Join-Path $programRoot "$LegacyPostgresDirectoryName-$MissingMajor"
        $missingData = Join-Path $dataRoot "$LegacyDataDirectoryName-$MissingMajor"
        Write-Pointer $pointerFile $EngineLayoutSchemaVersion $MissingMajor $missingRoot $missingData
        $exitCode = Invoke-Installer $programRoot $dataRoot $httpPort
        Stop-TestInstallation $programRoot $dataRoot
        Write-Check 'installer: pointer to a missing folder stops it' ($exitCode -ne 0) "(exit code $exitCode)"
        Write-Check 'installer: ... and creates no folder' (-not (Test-Path -LiteralPath $missingData) -and
            -not (Test-Path -LiteralPath $missingRoot))

        Move-Item -LiteralPath $legacyRoot -Destination $versionedRoot
        Move-Item -LiteralPath $legacyData -Destination $versionedData
        Set-Content -LiteralPath (Join-Path $versionedData $ClusterMarkerFileName) -Value 'kept' -Encoding Ascii
        Write-Pointer $pointerFile $EngineLayoutSchemaVersion $major $versionedRoot $versionedData
        Write-Check 'installer: re-run on the upgraded layout' ((Invoke-Installer $programRoot $dataRoot $httpPort) -eq 0)
        Stop-TestInstallation $programRoot $dataRoot

        Write-Check 'installer: no new cluster in pgdata' (-not (Test-Path -LiteralPath $legacyData))
        Write-Check 'installer: no binaries copied to pgsql' (-not (Test-Path -LiteralPath $legacyRoot))
        Write-Check 'installer: versioned cluster kept' (Test-Path -LiteralPath (Join-Path $versionedData $ClusterMarkerFileName))
        $config = Get-Content -LiteralPath (Join-Path $dataRoot 'config\klacks.native.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        $expectedBin = Join-Path $versionedRoot 'bin'
        Write-Check 'installer: Updater.PostgresBinDirectory names the versioned binaries' ($config.Updater.PostgresBinDirectory -eq $expectedBin) "($($config.Updater.PostgresBinDirectory))"
        $blocks = @(Get-Content -LiteralPath (Join-Path $versionedData 'postgresql.conf') | Where-Object { $_ -eq $ConfigBlockMarker }).Count
        Write-Check 'installer: postgresql.conf keeps one Klacks block' ($blocks -eq 1) "($blocks blocks)"
    }
    finally {
        Stop-TestInstallation $programRoot $dataRoot
    }
}

$exitCode = $ExitCodeSuccess
try {
    New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
    Test-LayoutResolution (Join-Path $WorkDirectory 'layout')
    if ($BundleZip -and $Version -and $PostgresDirectory) {
        Test-InstallerRerun (Join-Path $WorkDirectory 'installer')
    }
    else {
        Write-Host 'Installer part skipped (pass -BundleZip, -Version and -PostgresDirectory to run it).' -ForegroundColor Yellow
    }
}
catch {
    $script:failures++
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
}
finally {
    Remove-Item -LiteralPath $WorkDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

if ($script:failures -gt 0) {
    Write-Host "$($script:failures) check(s) failed." -ForegroundColor Red
    $exitCode = $ExitCodeFailed
}
else {
    Write-Host 'All checks passed.' -ForegroundColor Green
}
exit $exitCode
