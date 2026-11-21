# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Tests build-engine-artifact.ps1 (packing, layout, entry, the layout checks of -SmokeArchive; the smoke test needs real
# PostgreSQL binaries and runs only in the engine artifact workflows), engine-code-signature.ps1 (plan, refusal of unsigned
# files, of another PE machine and of a non-PE file; real signing needs the Certum certificate and runs only in
# postgres-engine-artifact-arm64.yml) and sign-engine-entry.ps1 (sign, verify, refusal of a tampered entry, a key of
# another pair and a payload of another major). Uses a fake engine tree and a throw-away RSA key; nothing leaves the temp folder.
#
# Usage: pwsh -File Test-EngineArtifactScripts.ps1

#Requires -Version 7

$ErrorActionPreference = 'Stop'

$build = Join-Path $PSScriptRoot 'build-engine-artifact.ps1'
$sign = Join-Path $PSScriptRoot 'sign-engine-entry.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) ("klacks-engine-test-" + [Guid]::NewGuid().ToString('N'))
$failures = 0

function Assert-That {
    param([string]$Name, [scriptblock]$Condition)
    $ok = $false
    try { $ok = [bool](& $Condition) } catch { Write-Host "  ($($_.Exception.Message))" }
    if ($ok) { Write-Host "PASS $Name" } else { Write-Host "FAIL $Name"; $script:failures++ }
}

function Assert-Throws {
    param([string]$Name, [scriptblock]$Action, [string]$MessagePattern = '')
    $threw = $false
    try { & $Action | Out-Null } catch { $threw = (-not $MessagePattern) -or ($_.Exception.Message -match $MessagePattern); if (-not $threw) { Write-Host "  ($($_.Exception.Message))" } }
    if ($threw) { Write-Host "PASS $Name" } else { Write-Host "FAIL $Name"; $script:failures++ }
}

function New-FakeEngine {
    param([string]$Root, [string]$PgvectorVersion)
    foreach ($file in @('bin\postgres.exe', 'bin\initdb.exe', 'bin\pg_ctl.exe', 'bin\pg_dump.exe', 'bin\pg_restore.exe', 'bin\psql.exe',
            'lib\vector.dll', 'share\extension\pg_trgm.control', 'share\timezonesets\Default')) {
        $path = Join-Path $Root $file
        New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
        Set-Content -LiteralPath $path -Value "fake $file"
    }
    Set-Content -LiteralPath (Join-Path $Root 'share\extension\vector.control') -Value "default_version = '$PgvectorVersion'"
    New-Item -ItemType Directory -Force -Path (Join-Path $Root 'doc') | Out-Null
    Set-Content -LiteralPath (Join-Path $Root 'doc\ignored.txt') -Value 'not packed'
}

try {
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    $engine = Join-Path $work 'pgsql'
    New-FakeEngine $engine '0.8.7'
    $url = 'https://example.invalid/postgres-engine-19.0-pgvector0.8.7/klacks-postgres-19.0-win-x64.zip'
    $out = Join-Path $work 'out'

    & $build -PostgresDirectory $engine -Major 19 -Version '19.0' -PgvectorVersion '0.8.7' -BundleUrl $url -OutputDirectory $out -SkipSmoke
    $zip = Join-Path $out 'klacks-postgres-19.0-win-x64.zip'
    $entryPath = Join-Path $out 'klacks-postgres-19.0-win-x64.json'
    Assert-That 'ZIP and entry are written' { (Test-Path $zip) -and (Test-Path $entryPath) }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $names = @($archive.Entries | ForEach-Object { $_.FullName })
        Assert-That 'ZIP root holds only bin, lib, share' { (($names | ForEach-Object { $_.Split('/')[0] } | Sort-Object -Unique) -join ',') -ceq 'bin,lib,share' }
        Assert-That 'entries use forward slashes' { -not ($names | Where-Object { $_.Contains('\') }) }
        Assert-That 'postgres.exe and vector.dll are packed' { ($names -contains 'bin/postgres.exe') -and ($names -contains 'lib/vector.dll') }
        Assert-That 'doc folder is not packed' { -not ($names | Where-Object { $_.StartsWith('doc/') }) }
    }
    finally {
        $archive.Dispose()
    }

    $entry = Get-Content -Raw $entryPath | ConvertFrom-Json
    Assert-That 'entry fields' { $entry.rid -ceq 'win-x64' -and $entry.bundleUrl -ceq $url -and $entry.signature -ceq '' }
    Assert-That 'entry sha256 is the ZIP hash in lowercase hex' { $entry.sha256 -ceq (Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant() }

    Assert-Throws 'pgvector version mismatch is refused' {
        & $build -PostgresDirectory $engine -Major 19 -Version '19.0' -PgvectorVersion '0.9.0' -BundleUrl $url -OutputDirectory (Join-Path $work 'out2') -SkipSmoke
    }
    Assert-Throws 'version of another major is refused' {
        & $build -PostgresDirectory $engine -Major 20 -Version '19.0' -PgvectorVersion '0.8.7' -BundleUrl $url -OutputDirectory (Join-Path $work 'out3') -SkipSmoke
    }
    Assert-Throws 'BundleUrl of another file name is refused' {
        & $build -PostgresDirectory $engine -Major 19 -Version '19.0' -PgvectorVersion '0.8.7' -BundleUrl 'https://example.invalid/x.zip' -OutputDirectory (Join-Path $work 'out4') -SkipSmoke
    }
    Assert-Throws 'SmokeArchive with another file name is refused' {
        $renamed = Join-Path $work 'klacks-postgres-19.0-win-arm64.zip'
        Copy-Item $zip $renamed
        & $build -SmokeArchive $renamed -Major 19 -Version '19.0' -PgvectorVersion '0.8.7'
    } -MessagePattern 'must be named'

    Assert-Throws 'SmokeArchive without the required files is refused before any smoke run' {
        $badRoot = Join-Path $work 'bad-layout'
        New-Item -ItemType Directory -Force -Path (Join-Path $badRoot 'bin'), (Join-Path $badRoot 'lib'), (Join-Path $badRoot 'share') | Out-Null
        Set-Content -LiteralPath (Join-Path $badRoot 'bin\postgres.exe') -Value 'fake'
        $badZip = Join-Path $work 'bad\klacks-postgres-19.0-win-x64.zip'
        New-Item -ItemType Directory -Force -Path (Split-Path $badZip) | Out-Null
        [IO.Compression.ZipFile]::CreateFromDirectory($badRoot, $badZip)
        & $build -SmokeArchive $badZip -Major 19 -Version '19.0' -PgvectorVersion '0.8.7'
    } -MessagePattern 'ZIP lacks'


    Remove-Item -Force (Join-Path $engine 'lib\vector.dll')
    Assert-Throws 'missing vector.dll is refused' {
        & $build -PostgresDirectory $engine -Major 19 -Version '19.0' -PgvectorVersion '0.8.7' -BundleUrl $url -OutputDirectory (Join-Path $work 'out5') -SkipSmoke
    }

    $peRoot = Join-Path $work 'pe-engine'
    function New-FakePe {
        param([string]$Path, [uint16]$Machine)
        $bytes = [byte[]]::new(512)
        $bytes[0] = 0x4D; $bytes[1] = 0x5A
        [BitConverter]::GetBytes([int]0x80).CopyTo($bytes, 0x3C)
        $bytes[0x80] = 0x50; $bytes[0x81] = 0x45
        [BitConverter]::GetBytes($Machine).CopyTo($bytes, 0x84)
        [BitConverter]::GetBytes([uint16]0x20B).CopyTo($bytes, 0x98)
        [BitConverter]::GetBytes([uint32]16).CopyTo($bytes, 0x98 + 108)
        New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
        [IO.File]::WriteAllBytes($Path, $bytes)
    }
    $codeSign = Join-Path $PSScriptRoot 'engine-code-signature.ps1'
    New-FakePe (Join-Path $peRoot 'bin\postgres.exe') 0xAA64
    New-FakePe (Join-Path $peRoot 'lib\vector.dll') 0xAA64
    Set-Content -LiteralPath (Join-Path $peRoot 'lib\readme.txt') -Value 'not a PE file, not scanned'
    $plan = @(& $codeSign -Root $peRoot -Mode Plan 6>$null)
    Assert-That 'code signing plan lists every unsigned PE file in bin and lib' {
        $plan.Count -eq 2 -and @($plan | Where-Object Action -ne 'Sign').Count -eq 0
    }
    Assert-Throws 'unsigned PE files fail the signature check' {
        & $codeSign -Root $peRoot -Mode Assert -Thumbprint ('A' * 40) -ExpectedMachine 0xAA64 6>$null
    } -MessagePattern '2 PE file\(s\) failed'
    New-FakePe (Join-Path $peRoot 'bin\x64tool.exe') 0x8664
    $machineReport = & { try { & $codeSign -Root $peRoot -Mode Assert -Thumbprint ('A' * 40) -ExpectedMachine 0xAA64 6>&1 } catch { } } | Out-String
    Assert-That 'a PE file of another machine is reported' { $machineReport -match 'x64tool\.exe: machine 0x8664, expected 0xAA64' }
    Assert-Throws 'a non-PE .dll stops the plan' {
        Set-Content -LiteralPath (Join-Path $peRoot 'lib\fake.dll') -Value 'text'
        & $codeSign -Root $peRoot -Mode Plan 6>$null
    } -MessagePattern 'not a PE file'
    Assert-Throws 'Assert needs a SHA-1 thumbprint' { & $codeSign -Root $peRoot -Mode Assert -Thumbprint 'abc' } -MessagePattern 'Thumbprint'

    $rsa = [Security.Cryptography.RSA]::Create(2048)
    $other = [Security.Cryptography.RSA]::Create(2048)
    $keyPath = Join-Path $work 'key.pem'
    $publicPath = Join-Path $work 'public.pem'
    $otherPublicPath = Join-Path $work 'other-public.pem'
    Set-Content -LiteralPath $keyPath -Value $rsa.ExportPkcs8PrivateKeyPem()
    Set-Content -LiteralPath $publicPath -Value $rsa.ExportSubjectPublicKeyInfoPem()
    Set-Content -LiteralPath $otherPublicPath -Value $other.ExportSubjectPublicKeyInfoPem()

    & $sign -EntryPath $entryPath -Major 19 -SigningKeyPath $keyPath
    $signed = Get-Content -Raw $entryPath | ConvertFrom-Json
    Assert-That 'signature is written as Base64' { $signed.signature -cmatch '^[A-Za-z0-9+/]+=*$' -and $signed.sha256 -ceq $entry.sha256 }
    Assert-That 'signature verifies' { & $sign -EntryPath $entryPath -Major 19 -PublicKeyPath $publicPath -Verify; $true }

    $payload = [Text.Encoding]::UTF8.GetBytes("klacks-postgres-engine`n19`n$url`n$($entry.sha256)")
    Assert-That 'payload is byte exact (independent verification)' {
        $rsa.VerifyData($payload, [Convert]::FromBase64String($signed.signature), [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    }
    Assert-Throws 'a key of another pair does not verify' { & $sign -EntryPath $entryPath -Major 19 -PublicKeyPath $otherPublicPath -Verify }
    Assert-Throws 'another major does not verify' { & $sign -EntryPath $entryPath -Major 20 -PublicKeyPath $publicPath -Verify }

    $tampered = Join-Path $work 'tampered.json'
    $copy = Get-Content -Raw $entryPath | ConvertFrom-Json
    $copy.bundleUrl = 'https://example.invalid/other/klacks-postgres-19.0-win-x64.zip'
    Set-Content -LiteralPath $tampered -Value ($copy | ConvertTo-Json)
    Assert-Throws 'a tampered URL does not verify' { & $sign -EntryPath $tampered -Major 19 -PublicKeyPath $publicPath -Verify }
    $unsigned = Join-Path $work 'unsigned.json'
    $copy.signature = ''
    Set-Content -LiteralPath $unsigned -Value ($copy | ConvertTo-Json)
    Assert-Throws 'an unsigned entry does not verify' { & $sign -EntryPath $unsigned -Major 19 -PublicKeyPath $publicPath -Verify }
}
finally {
    if (Test-Path $work) { Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $work }
}

if ($failures -ne 0) { Write-Host "$failures check(s) failed"; exit 1 }
Write-Host 'All checks passed'
