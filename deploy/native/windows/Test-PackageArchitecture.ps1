# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Checks the architecture rules of Install-Klacks.ps1 without administrator rights, PostgreSQL or a real bundle.
# Function part: loads Read-PeMachine, Get-ArchitectureName, Get-BundleArchitecture, Get-InstalledArchitecture and
# Assert-SupportedWindows with their constants from Install-Klacks.ps1 through the PowerShell parser (the shipped code is
# tested) and runs them against fake PE files and bundles in a temporary folder; Win32_Processor is replaced by a stub, so
# x64, ARM64 and x86 PCs are simulated on any machine.
# Installer part: runs Install-Klacks.ps1 itself in process mode (-NoServices) against temporary roots and checks that an
# arm64 package over an x64 installation and an arm64 package on a simulated x64 PC both stop before any folder is
# created. Exit code 0 = every check passed, 1 = at least one failed.
#
# Windows PowerShell 5.1 compatible on purpose, like the installer it tests.
#
# Example:  .\Test-PackageArchitecture.ps1

[CmdletBinding()]
param(
    [string]$InstallScript = '',
    [string]$WorkDirectory = ''
)

$ErrorActionPreference = 'Stop'

if (-not $InstallScript) { $InstallScript = Join-Path $PSScriptRoot 'Install-Klacks.ps1' }
if (-not $WorkDirectory) { $WorkDirectory = [System.IO.Path]::GetTempPath() }
$WorkDirectory = Join-Path $WorkDirectory ('klacks-package-architecture-' + [guid]::NewGuid().ToString('N'))

$LoadedFunctions = @('Read-PeMachine', 'Get-ArchitectureName', 'Get-BundleArchitecture', 'Get-InstalledArchitecture', 'Assert-SupportedWindows')
$LoadedConstants = @('PackageArchitectures', 'ProcessorArchitectureArm64', 'ApiExecutableEntry', 'PeHeaderBytes')
$MachineX64 = 0x8664
$MachineArm64 = 0xAA64
$MachineI386 = 0x014C
$ProcessorX86 = 0
$ProcessorX64 = 9
$ProcessorArm64 = 12
$FakePeLength = 512
$FakePeOffset = 0x40

$script:Failures = 0

function Write-Check([string]$name, [bool]$passed, [string]$detail = '') {
    if ($passed) { Write-Host "PASS  $name" -ForegroundColor Green }
    else {
        $script:Failures++
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
        Write-Check $name ($message -like "*$expectedText*") "(message: $message)"
    }
}

function New-FakePeBytes([int]$machine) {
    $bytes = New-Object byte[] $FakePeLength
    $bytes[0] = 0x4D; $bytes[1] = 0x5A
    [BitConverter]::GetBytes([int]$FakePeOffset).CopyTo($bytes, 0x3C)
    $bytes[$FakePeOffset] = 0x50; $bytes[$FakePeOffset + 1] = 0x45
    [BitConverter]::GetBytes([uint16]$machine).CopyTo($bytes, $FakePeOffset + 4)
    return , $bytes
}

function New-FakeBundle([string]$path, [string]$entryName, [byte[]]$content) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::Open($path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $stream = $zip.CreateEntry($entryName).Open()
        try { $stream.Write($content, 0, $content.Length) } finally { $stream.Dispose() }
    }
    finally {
        $zip.Dispose()
    }
}

function Set-SimulatedProcessor([int]$architecture) {
    $global:KlacksTestProcessorArchitecture = $architecture
    Set-Item -Path function:global:Get-CimInstance -Value { [pscustomobject]@{ Architecture = $global:KlacksTestProcessorArchitecture } }
}

function Remove-SimulatedProcessor {
    Remove-Item -Path function:global:Get-CimInstance -ErrorAction SilentlyContinue
    Remove-Variable -Scope Global -Name KlacksTestProcessorArchitecture -ErrorAction SilentlyContinue
}

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

New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
try {
    $arm64Bundle = Join-Path $WorkDirectory 'arm64.zip'
    $x64Bundle = Join-Path $WorkDirectory 'x64.zip'
    New-FakeBundle $arm64Bundle 'app/Klacks.Api.exe' (New-FakePeBytes $MachineArm64)
    New-FakeBundle $x64Bundle 'app/Klacks.Api.exe' (New-FakePeBytes $MachineX64)
    $noExeBundle = Join-Path $WorkDirectory 'no-exe.zip'
    New-FakeBundle $noExeBundle 'ui/index.html' ([System.Text.Encoding]::ASCII.GetBytes('<html></html>'))
    $i386Bundle = Join-Path $WorkDirectory 'i386.zip'
    New-FakeBundle $i386Bundle 'app/Klacks.Api.exe' (New-FakePeBytes $MachineI386)
    $textBundle = Join-Path $WorkDirectory 'text.zip'
    New-FakeBundle $textBundle 'app/Klacks.Api.exe' ([System.Text.Encoding]::ASCII.GetBytes('not a program'))

    Write-Check 'arm64 bundle: arm64' ((Get-BundleArchitecture $arm64Bundle) -eq 'arm64')
    Write-Check 'x64 bundle: x64' ((Get-BundleArchitecture $x64Bundle) -eq 'x64')
    Test-Throws 'bundle without app/Klacks.Api.exe: abort' { Get-BundleArchitecture $noExeBundle } 'contains no app/Klacks.Api.exe'
    Test-Throws 'x86 program: abort' { Get-BundleArchitecture $i386Bundle } 'unsupported machine type 0x014C'
    Test-Throws 'no PE file: abort' { Get-BundleArchitecture $textBundle } 'is not a Windows program file'

    $installedExe = Join-Path $WorkDirectory 'installed\Klacks.Api.exe'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $installedExe) | Out-Null
    [System.IO.File]::WriteAllBytes($installedExe, (New-FakePeBytes $MachineX64))
    Write-Check 'installed x64 program: x64' ((Get-InstalledArchitecture $installedExe) -eq 'x64')
    [System.IO.File]::WriteAllBytes($installedExe, [byte[]]@(1, 2, 3))
    Test-Throws 'installed program unreadable as PE: abort' { Get-InstalledArchitecture $installedExe } 'is not a Windows program file'

    try {
        Set-SimulatedProcessor $ProcessorX64
        Test-Throws 'arm64 package on an x64 PC: abort' { Assert-SupportedWindows 'arm64' } 'built for arm64 processors'
        Assert-SupportedWindows 'x64'
        Write-Check 'x64 package on an x64 PC: allowed' $true
        Set-SimulatedProcessor $ProcessorArm64
        Assert-SupportedWindows 'arm64'
        Write-Check 'arm64 package on an ARM64 PC: allowed' $true
        Assert-SupportedWindows 'x64'
        Write-Check 'x64 package on an ARM64 PC (emulation): allowed' $true
        Set-SimulatedProcessor $ProcessorX86
        Assert-SupportedWindows 'x64'
        Write-Check 'x64 package: no processor query (32-bit Windows is refused by the setup architecture check)' $true
        Test-Throws 'arm64 package on an x86 PC: abort' { Assert-SupportedWindows 'arm64' } 'built for arm64 processors'
    }
    finally {
        Remove-SimulatedProcessor
    }

    $postgres = Join-Path $WorkDirectory 'pgsql'
    New-Item -ItemType Directory -Force -Path (Join-Path $postgres 'bin') | Out-Null
    Set-Content -Path (Join-Path $postgres 'bin\pg_ctl.exe') -Value 'stub'
    $installArguments = @{ Version = '1.0.0'; PostgresDirectory = $postgres; NoServices = $true; AllowUnsignedUpdates = $true }

    $programRoot = Join-Path $WorkDirectory 'over-x64\program'
    $dataRoot = Join-Path $WorkDirectory 'over-x64\data'
    New-Item -ItemType Directory -Force -Path (Join-Path $programRoot 'current\app') | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $programRoot 'current\app\Klacks.Api.exe'), (New-FakePeBytes $MachineX64))
    try {
        Set-SimulatedProcessor $ProcessorArm64
        Test-Throws 'installer: arm64 package over an x64 installation: abort' {
            & $InstallScript @installArguments -BundleZip $arm64Bundle -ProgramRoot $programRoot -DataRoot $dataRoot 6>$null
        } 'cannot change its architecture'
        Write-Check 'installer: nothing created after the architecture lock' (-not (Test-Path $dataRoot) -and -not (Test-Path (Join-Path $programRoot 'versions')))

        Set-SimulatedProcessor $ProcessorX64
        $programRoot = Join-Path $WorkDirectory 'fresh\program'
        $dataRoot = Join-Path $WorkDirectory 'fresh\data'
        Test-Throws 'installer: arm64 package on an x64 PC: abort' {
            & $InstallScript @installArguments -BundleZip $arm64Bundle -ProgramRoot $programRoot -DataRoot $dataRoot 6>$null
        } 'built for arm64 processors'
        Write-Check 'installer: nothing created on the refused PC' (-not (Test-Path $dataRoot) -and -not (Test-Path $programRoot))
    }
    finally {
        Remove-SimulatedProcessor
    }
}
finally {
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $WorkDirectory
}

if ($script:Failures -gt 0) {
    Write-Host "$script:Failures check(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host 'All checks passed.' -ForegroundColor Green
