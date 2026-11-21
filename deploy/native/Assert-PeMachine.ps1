# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# PE gate of a native bundle: every .exe/.dll/.node file below Root must run on the machine of the runtime identifier.
# Accepted are native files of exactly that machine (win-x64 0x8664, win-arm64 0xAA64) and managed files that are pure IL
# (CLR header, machine 0x014C = AnyCPU) or compiled for that machine (ReadyToRun). Anything else - an x64 DLL in an arm64
# bundle, an arm64EC or x86 native file - is listed and throws, so build-native-bundle.ps1 never signs such a bundle.
# Files without an MZ/PE header are skipped. osx-* runtime identifiers are not PE based and pass without a check.
#
# Parameters: Root = folder to scan (recursive); Rid = runtime identifier the bundle is published for.

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][ValidateSet('win-x64', 'win-arm64', 'osx-arm64')][string]$Rid
)

$ErrorActionPreference = 'Stop'

$MachineByRid = @{ 'win-x64' = 0x8664; 'win-arm64' = 0xAA64 }
$AnyCpuMachine = 0x014C
$DosSignature = 0x5A4D
$PeSignature = 0x4550
$Pe32PlusMagic = 0x20B
$PeHeaderOffsetPosition = 0x3C
$OptionalHeaderOffset = 24
$DataDirectoryOffsetPe32Plus = 112
$DataDirectoryOffsetPe32 = 96
$ClrDataDirectoryIndex = 14
$DataDirectoryEntrySize = 8
$MinimumPeLength = 64
$PeExtensions = @('*.exe', '*.dll', '*.node')

if (-not $MachineByRid.ContainsKey($Rid)) {
    Write-Host "PE gate: $Rid is not a Windows runtime identifier, nothing to check."
    return
}
if (-not (Test-Path -LiteralPath $Root)) { throw "PE gate: '$Root' does not exist." }
$expected = $MachineByRid[$Rid]
$rootPath = (Resolve-Path -LiteralPath $Root).Path

function Read-PeInfo([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $reader = [IO.BinaryReader]::new($stream)
        if ($stream.Length -lt $MinimumPeLength -or $reader.ReadUInt16() -ne $DosSignature) { return $null }
        $stream.Position = $PeHeaderOffsetPosition
        $peOffset = $reader.ReadInt32()
        if ($peOffset -le 0 -or $peOffset -ge $stream.Length) { return $null }
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne $PeSignature) { return $null }
        $machine = $reader.ReadUInt16()
        $stream.Position = $peOffset + $OptionalHeaderOffset
        $magic = $reader.ReadUInt16()
        $dataDirectory = if ($magic -eq $Pe32PlusMagic) { $DataDirectoryOffsetPe32Plus } else { $DataDirectoryOffsetPe32 }
        $stream.Position = $peOffset + $OptionalHeaderOffset + $dataDirectory + $ClrDataDirectoryIndex * $DataDirectoryEntrySize
        $clrRva = $reader.ReadUInt32()
        return [pscustomobject]@{ Machine = [int]$machine; Managed = ($clrRva -ne 0) }
    }
    finally {
        $stream.Dispose()
    }
}

$checked = 0
$offenders = foreach ($file in Get-ChildItem -LiteralPath $rootPath -Recurse -File -Include $PeExtensions) {
    $info = Read-PeInfo $file.FullName
    if (-not $info) { continue }
    $checked++
    $accepted = ($info.Machine -eq $expected) -or ($info.Managed -and $info.Machine -eq $AnyCpuMachine)
    if (-not $accepted) {
        '{0}: machine 0x{1:X4}{2}, expected 0x{3:X4}' -f $file.FullName.Substring($rootPath.Length + 1), $info.Machine,
            $(if ($info.Managed) { ' (managed)' } else { '' }), $expected
    }
}

if ($offenders) {
    throw "PE gate: $(@($offenders).Count) file(s) below '$rootPath' do not run as $Rid`n$($offenders -join "`n")"
}
Write-Host ('PE gate: {0} PE files below {1} match {2} (0x{3:X4} or pure IL).' -f $checked, $rootPath, $Rid, $expected)
