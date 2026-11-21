# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds PostgreSQL + pgvector natively for Windows ARM64 (win-arm64) from the pins in pins.json and tests the result.
# Runs locally and on the GitHub runner windows-11-arm (.github/workflows/postgres-engine-artifact-arm64.yml); the result
# in <WorkDir>\pgsql (bin, lib, share) is packed by deploy/postgres/build-engine-artifact.ps1 -Rid win-arm64.
# Steps (run all, or a range with -From/-To): Preflight, Fetch, Deps, Configure, Build, Test, Install, Pgvector, Package,
# Verify, Smoke. Preflight only checks the machine and the pinned MSVC toolset, so a runner without it fails in seconds.
# Idempotent: every step can be re-run; finished downloads/clones are re-verified instead of re-fetched.
#
# Parameters: WorkDir = build root (sources, tools, logs, install; keep it short, vcpkg hits MAX_PATH); PinsFile = pin file
# (default: pins.json next to this script); Jobs = build/test parallelism (default: processor count); Port = TCP port of
# the temporary test cluster (must be free); From/To = step range; VsInstallPath = Visual Studio root (default: the first
# vswhere instance that carries the pinned toolset msvc.vcvarsVer with the native arm64 compiler); SkipTests = skip
# 'meson test'. Environment: PG_ARM64_PYTHON = path of a native ARM64 python.exe when the one on PATH is not.

#Requires -Version 7

[CmdletBinding()]
param(
    [string]$WorkDir = 'C:\PgArm64Build',
    [string]$PinsFile = (Join-Path $PSScriptRoot 'pins.json'),
    [ValidateRange(1, 256)]
    [int]$Jobs = [Environment]::ProcessorCount,
    [int]$Port = 5499,
    [ValidateSet('Preflight', 'Fetch', 'Deps', 'Configure', 'Build', 'Test', 'Install', 'Pgvector', 'Package', 'Verify', 'Smoke')]
    [string]$From = 'Preflight',
    [ValidateSet('Preflight', 'Fetch', 'Deps', 'Configure', 'Build', 'Test', 'Install', 'Pgvector', 'Package', 'Verify', 'Smoke')]
    [string]$To = 'Smoke',
    [string]$VsInstallPath,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Steps = @('Preflight', 'Fetch', 'Deps', 'Configure', 'Build', 'Test', 'Install', 'Pgvector', 'Package', 'Verify', 'Smoke')
$PeMachineArm64 = 0xAA64
$LoopbackAddress = '127.0.0.1'
$StartTimeoutSeconds = 120
$SmokeVectorCount = 2000
$SmokeVectorDimensions = 8
$SmokeProbeResults = 5
$HnswIndexName = 'smoke_vectors_hnsw'
$DependencyDllPatterns = @('zlib*.dll', 'libz.dll', 'libssl-*.dll', 'libcrypto-*.dll', 'icu*.dll', 'lz4*.dll', 'zstd*.dll')

$Pins = Get-Content -Raw -LiteralPath $PinsFile | ConvertFrom-Json

$Paths = [ordered]@{
    Downloads   = Join-Path $WorkDir 'downloads'
    Tools       = Join-Path $WorkDir 'tools'
    Src         = Join-Path $WorkDir 'src'
    Logs        = Join-Path $WorkDir 'logs'
    Venv        = Join-Path $WorkDir 'venv'
    Vcpkg       = Join-Path $WorkDir 'vcpkg'
    VcpkgCache  = Join-Path $WorkDir 'vcpkg-cache'
    VcpkgDl     = Join-Path $WorkDir 'vcpkg-downloads'
    VcpkgMan    = Join-Path $WorkDir 'vcpkg-manifest'
    VcpkgTrip   = Join-Path $WorkDir 'vcpkg-triplets'
    Deps        = Join-Path $WorkDir 'deps'
    Build       = Join-Path $WorkDir 'build'
    Install     = Join-Path $WorkDir 'pgsql'
    Smoke       = Join-Path $WorkDir 'smoke'
}
foreach ($p in $Paths.Values) { New-Item -ItemType Directory -Force -Path $p | Out-Null }

$PgSrc = Join-Path $Paths.Src "postgresql-$($Pins.postgresql.version)"
$PgvSrc = Join-Path $Paths.Src "pgvector-$($Pins.pgvector.version)"
$Triplet = $Pins.vcpkg.triplet
$DepsRoot = Join-Path $Paths.Deps $Triplet
$PerlExe = Join-Path $Paths.Tools 'strawberry\perl\bin\perl.exe'
$FlexBisonDir = Join-Path $Paths.Tools 'winflexbison'
$TimingsFile = Join-Path $Paths.Logs 'timings.json'

function Write-Step([string]$Text) { Write-Host "`n=== $Text ===" -ForegroundColor Cyan }

function Save-Timing([string]$Step, [TimeSpan]$Elapsed) {
    $t = [ordered]@{}
    if (Test-Path $TimingsFile) { (Get-Content -Raw $TimingsFile | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $t[$_.Name] = $_.Value } }
    $t[$Step] = [math]::Round($Elapsed.TotalMinutes, 1)
    $t | ConvertTo-Json | Set-Content -Encoding utf8 $TimingsFile
}

function Invoke-Logged {
    param([string]$Log, [string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory = $WorkDir)
    $logPath = Join-Path $Paths.Logs $Log
    Push-Location $WorkingDirectory
    try {
        "`n>>> $FilePath $($Arguments -join ' ')" | Add-Content -Encoding utf8 $logPath
        & $FilePath @Arguments *>&1 | ForEach-Object { "$_" } | Add-Content -Encoding utf8 $logPath
        $code = $LASTEXITCODE
    }
    finally { Pop-Location }
    if ($code -ne 0) {
        Get-Content $logPath -Tail 40 | Write-Host
        throw "$FilePath exited with $code (log: $logPath)"
    }
}

function Get-VerifiedDownload([string]$Url, [string]$Sha256) {
    $file = Join-Path $Paths.Downloads ([IO.Path]::GetFileName(([Uri]$Url).AbsolutePath))
    if (-not (Test-Path $file) -or (Get-FileHash -Algorithm SHA256 $file).Hash -ne $Sha256.ToUpperInvariant()) {
        Invoke-WebRequest -Uri $Url -OutFile $file
    }
    $actual = (Get-FileHash -Algorithm SHA256 $file).Hash.ToLowerInvariant()
    if ($actual -ne $Sha256.ToLowerInvariant()) { Remove-Item $file; throw "SHA-256 mismatch for $Url`: got $actual, pinned $Sha256" }
    return $file
}

function Get-PinnedGitCheckout([string]$Repo, [string]$Ref, [string]$Commit, [string]$Dir) {
    if (-not (Test-Path (Join-Path $Dir '.git'))) {
        git init -q $Dir
        git -C $Dir remote add origin $Repo
    }
    git -C $Dir fetch -q --depth 1 origin $Ref
    git -C $Dir checkout -q --force FETCH_HEAD
    $head = (git -C $Dir rev-parse 'HEAD^{commit}').Trim()
    if ($head -ne $Commit) { throw "$Repo $Ref resolves to $head, pinned $Commit" }
}

# Returns the Visual Studio root whose VC\Tools\MSVC holds the pinned toolset (msvc.vcvarsVer) with the native arm64
# compiler. All vswhere instances are searched, not only the latest: a runner image may carry the pinned toolset in an
# older or side-by-side installation. Throws with the list of toolsets found, so a missing toolset is a clear message.
function Get-VsRoot {
    if ($script:VsRoot) { return $script:VsRoot }
    $want = [string]$Pins.msvc.vcvarsVer
    $candidates = if ($VsInstallPath) { @($VsInstallPath) } else {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found at $vswhere; is Visual Studio installed?" }
        @(& $vswhere -all -prerelease -products * -property installationPath)
    }
    $found = [Collections.Generic.List[string]]::new()
    foreach ($root in @($candidates | Where-Object { $_ } | ForEach-Object { $_.Trim() })) {
        $toolsDir = Join-Path $root 'VC\Tools\MSVC'
        $versions = @(Get-ChildItem -LiteralPath $toolsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object Name)
        foreach ($v in $versions) {
            $hasArm64 = Test-Path (Join-Path $toolsDir "$v\bin\Hostarm64\arm64\cl.exe")
            $found.Add("$v$(if (-not $hasArm64) { ' (no native arm64 cl)' }) in $root")
            if ($hasArm64 -and $v.StartsWith("$want.", [StringComparison]::Ordinal)) { $script:VsRoot = $root; return $root }
        }
    }
    throw ("MSVC toolset $want.* with the native arm64 compiler (bin\Hostarm64\arm64\cl.exe) is not installed. " +
        "Toolsets found: $(if ($found.Count) { $found -join '; ' } else { 'none' }). Install the VS component " +
        "'MSVC ARM64 build tools' of that version or change msvc.vcvarsVer in pins.json; a new pin is a new build.")
}

# The CRT shipped app-local must come from the same toolset line as the compiler, not from the image's default redist.
function Get-PinnedRedistDirectory {
    $want = [string]$Pins.msvc.vcvarsVer
    $redistRoot = Join-Path (Get-VsRoot) 'VC\Redist\MSVC'
    $dir = Get-ChildItem -LiteralPath $redistRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^\d+(\.\d+)+$' -and $_.Name.StartsWith("$want.", [StringComparison]::Ordinal) -and (Test-Path (Join-Path $_.FullName 'arm64')) } |
        Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    if (-not $dir) { throw "No arm64 CRT redist for MSVC $want.* below $redistRoot." }
    return $dir.FullName
}

function Step-Preflight {
    if ($env:PROCESSOR_ARCHITECTURE -ne 'ARM64') { throw "This build must run in a native ARM64 process (got $env:PROCESSOR_ARCHITECTURE)." }
    $vs = Get-VsRoot
    $redist = Get-PinnedRedistDirectory
    Write-Host "Visual Studio: $vs; MSVC pin $($Pins.msvc.vcvarsVer) (observed locally $($Pins.msvc.toolsetObserved)); CRT redist: $redist; jobs: $Jobs"
}

# Imports the native arm64 MSVC environment and builds a minimal PATH: VS/SDK entries, the pinned build tools, git,
# the dependency DLLs and System32. Inherited user PATH entries (msys clang, Git perl, pkg-config) are dropped on purpose.
function Enter-Arm64DevShell {
    if ($env:PROCESSOR_ARCHITECTURE -ne 'ARM64') { throw "This build must run in a native ARM64 process (got $env:PROCESSOR_ARCHITECTURE)." }
    if ($script:DevShellReady) { return }
    $vs = Get-VsRoot
    $vcvars = Join-Path $vs 'VC\Auxiliary\Build\vcvarsall.bat'
    $env:PATH = (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer') + ';' + $env:PATH
    $before = $env:PATH -split ';'
    $envDump = cmd /c "`"$vcvars`" arm64 -vcvars_ver=$($Pins.msvc.vcvarsVer) >nul && set"
    if ($LASTEXITCODE -ne 0) { throw 'vcvarsall arm64 failed.' }
    foreach ($line in $envDump) {
        if ($line -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2]) }
    }
    $vsEntries = ($env:PATH -split ';') | Where-Object { $_ -and ($before -notcontains $_ -or $_ -like "$vs*" -or $_ -like '*Windows Kits*') }
    $gitDir = Split-Path (Get-Command git.exe -CommandType Application | Select-Object -First 1).Source
    $sys = Join-Path $env:SystemRoot 'System32'
    $env:PATH = (@(
        (Join-Path $Paths.Venv 'Scripts'),
        (Join-Path $Paths.Tools 'strawberry\perl\bin'),
        $FlexBisonDir,
        (Join-Path $DepsRoot 'bin'),
        $vsEntries,
        $gitDir,
        $sys, $env:SystemRoot, (Join-Path $sys 'Wbem'), (Join-Path $sys 'WindowsPowerShell\v1.0')
    ) | ForEach-Object { $_ } | Where-Object { $_ } | Select-Object -Unique) -join ';'
    # pg_regress shells out to 'diff'; Git for Windows ships one. Appended last so nothing else in usr\bin shadows our tools.
    $diffDir = Join-Path (Split-Path $gitDir) 'usr\bin'
    if (-not (Test-Path (Join-Path $diffDir 'diff.exe'))) { throw "diff.exe not found next to git ($diffDir); pg_regress needs it." }
    $env:PATH = "$env:PATH;$diffDir"
    $cl = (Get-Command cl.exe -CommandType Application | Select-Object -First 1).Source
    if ($cl -notmatch '\\bin\\Hostarm64\\arm64\\cl\.exe$') { throw "cl.exe is not the native arm64 compiler: $cl" }
    $env:PKG_CONFIG = Join-Path $DepsRoot 'tools\pkgconf\pkgconf.exe'
    $env:PKG_CONFIG_PATH = Join-Path $DepsRoot 'lib\pkgconfig'
    $env:VCPKG_DEFAULT_HOST_TRIPLET = $Triplet
    Write-Host "MSVC $env:VCToolsVersion, SDK $env:WindowsSDKVersion, cl = $cl"
    $script:DevShellReady = $true
}

function Get-PeMachine([string]$File) {
    $fs = [IO.File]::OpenRead($File)
    try {
        $br = [IO.BinaryReader]::new($fs)
        if ($br.ReadUInt16() -ne 0x5A4D) { return $null }
        $fs.Position = 0x3C
        $fs.Position = $br.ReadInt32()
        if ($br.ReadUInt32() -ne 0x00004550) { return $null }
        return $br.ReadUInt16()
    }
    finally { $fs.Dispose() }
}

function Get-FreePortOrThrow([int]$P) {
    if (Get-NetTCPConnection -LocalPort $P -ErrorAction SilentlyContinue) { throw "Port $P is in use; pass -Port." }
}

function Start-Cluster([string]$DataDir, [string]$LogFile, [hashtable]$ChildEnv) {
    $info = [Diagnostics.ProcessStartInfo]::new((Join-Path $Paths.Install 'bin\pg_ctl.exe'))
    $info.UseShellExecute = $false
    foreach ($a in @('start', '-w', '-t', "$StartTimeoutSeconds", '-D', $DataDir, '-l', $LogFile, '-o', "-p $Port -c listen_addresses=$LoopbackAddress")) { $info.ArgumentList.Add($a) }
    if ($ChildEnv) { $info.Environment.Clear(); foreach ($k in $ChildEnv.Keys) { $info.Environment[$k] = $ChildEnv[$k] } }
    $proc = [Diagnostics.Process]::Start($info)
    try { $proc.WaitForExit(); if ($proc.ExitCode -ne 0) { Get-Content $LogFile -Tail 30 | Write-Host; throw "pg_ctl start exited with $($proc.ExitCode)" } }
    finally { $proc.Dispose() }
}

function Stop-Cluster([string]$DataDir) {
    $pgctl = Join-Path $Paths.Install 'bin\pg_ctl.exe'
    if (Test-Path (Join-Path $DataDir 'postmaster.pid')) { & $pgctl stop -m fast -w -D $DataDir 2>&1 | Out-Null }
}

function Step-Fetch {
    $tarball = Get-VerifiedDownload $Pins.postgresql.url $Pins.postgresql.sha256
    if (-not (Test-Path (Join-Path $PgSrc 'meson.build'))) {
        & (Join-Path $env:SystemRoot 'System32\tar.exe') -xjf $tarball -C $Paths.Src
        if ($LASTEXITCODE -ne 0) { throw 'Extracting the PostgreSQL tarball failed.' }
    }
    Get-PinnedGitCheckout $Pins.pgvector.repo "refs/tags/$($Pins.pgvector.tag)" $Pins.pgvector.commit $PgvSrc

    $wfb = Get-VerifiedDownload $Pins.winflexbison.url $Pins.winflexbison.sha256
    if (-not (Test-Path (Join-Path $FlexBisonDir 'win_bison.exe'))) { Expand-Archive -Force $wfb $FlexBisonDir }
    $perlZip = Get-VerifiedDownload $Pins.strawberryPerl.url $Pins.strawberryPerl.sha256
    if (-not (Test-Path $PerlExe)) { Expand-Archive -Force $perlZip (Join-Path $Paths.Tools 'strawberry') }

    $py = (Get-Command python.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1).Source
    if ($env:PG_ARM64_PYTHON) { $py = $env:PG_ARM64_PYTHON }
    $pyMachine = & $py -c 'import platform;print(platform.machine())'
    if ($pyMachine -ne 'ARM64') { throw "Python at $py is $pyMachine, need a native ARM64 Python (set PG_ARM64_PYTHON)." }
    $venvPy = Join-Path $Paths.Venv 'Scripts\python.exe'
    if (-not (Test-Path $venvPy)) { & $py -m venv $Paths.Venv }
    $req = Join-Path $Paths.Venv 'requirements.txt'
    ($Pins.python.requirements | ForEach-Object { "$($_.name)==$($_.version) --hash=sha256:$($_.sha256)" }) | Set-Content -Encoding ascii $req
    Invoke-Logged 'fetch-pip.log' $venvPy @('-m', 'pip', 'install', '--disable-pip-version-check', '--only-binary=:all:', '--require-hashes', '-r', $req)

    Get-PinnedGitCheckout $Pins.vcpkg.repo $Pins.vcpkg.commit $Pins.vcpkg.commit $Paths.Vcpkg
    if (-not (Test-Path (Join-Path $Paths.Vcpkg 'vcpkg.exe'))) {
        Invoke-Logged 'fetch-vcpkg-bootstrap.log' 'cmd.exe' @('/c', (Join-Path $Paths.Vcpkg 'bootstrap-vcpkg.bat'), '-disableMetrics')
    }
    $vcpkgMachine = Get-PeMachine (Join-Path $Paths.Vcpkg 'vcpkg.exe')
    Write-Host ('vcpkg.exe machine 0x{0:X4}' -f $vcpkgMachine)
}

function Step-Deps {
    Enter-Arm64DevShell
    @"
set(VCPKG_TARGET_ARCHITECTURE arm64)
set(VCPKG_CRT_LINKAGE dynamic)
set(VCPKG_LIBRARY_LINKAGE dynamic)
set(VCPKG_BUILD_TYPE release)
"@ | Set-Content -Encoding ascii (Join-Path $Paths.VcpkgTrip "$Triplet.cmake")
    $manifest = [ordered]@{
        name = 'pg-arm64-deps'; 'version-string' = '1'; 'builtin-baseline' = $Pins.vcpkg.commit
        dependencies = @($Pins.vcpkg.ports | ForEach-Object { if ($_ -eq 'pkgconf') { [ordered]@{ name = 'pkgconf'; host = $true } } else { $_ } })
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $Paths.VcpkgMan 'vcpkg.json')
    $env:VCPKG_ROOT = $Paths.Vcpkg
    $env:VCPKG_DOWNLOADS = $Paths.VcpkgDl
    $env:VCPKG_DEFAULT_BINARY_CACHE = $Paths.VcpkgCache
    $env:VCPKG_MAX_CONCURRENCY = "$Jobs"
    $env:VCPKG_DISABLE_METRICS = '1'
    Invoke-Logged 'deps-vcpkg.log' (Join-Path $Paths.Vcpkg 'vcpkg.exe') @(
        'install', "--x-manifest-root=$($Paths.VcpkgMan)", "--x-install-root=$($Paths.Deps)",
        "--overlay-triplets=$($Paths.VcpkgTrip)", "--triplet=$Triplet", "--host-triplet=$Triplet", '--clean-after-build')
    & (Join-Path $Paths.Vcpkg 'vcpkg.exe') list "--x-install-root=$($Paths.Deps)" "--x-manifest-root=$($Paths.VcpkgMan)" |
        Set-Content -Encoding utf8 (Join-Path $Paths.Logs 'deps-versions.txt')
}

function Step-Configure {
    Enter-Arm64DevShell
    $meson = Join-Path $Paths.Venv 'Scripts\meson.exe'
    $setupArgs = @(
        'setup', $Paths.Build, $PgSrc, "--prefix=$($Paths.Install)", '--buildtype=release', '--wrap-mode=nofallback',
        '-Dauto_features=disabled', '-Dzlib=enabled', '-Dssl=openssl', '-Dicu=enabled', '-Dlz4=enabled', '-Dzstd=enabled',
        '-Dldap=enabled', '-Dnls=disabled', '-Dtap_tests=disabled',
        "-DPERL=$PerlExe", "-DBISON=$(Join-Path $FlexBisonDir 'win_bison.exe')", "-DFLEX=$(Join-Path $FlexBisonDir 'win_flex.exe')",
        "-Dextra_include_dirs=$(Join-Path $DepsRoot 'include')", "-Dextra_lib_dirs=$(Join-Path $DepsRoot 'lib')"
    )
    if (Test-Path (Join-Path $Paths.Build 'build.ninja')) { $setupArgs += '--reconfigure' }
    $env:CC = 'cl'
    Invoke-Logged 'configure.log' $meson $setupArgs
}

function Step-Build {
    Enter-Arm64DevShell
    Invoke-Logged 'build.log' (Join-Path $Paths.Venv 'Scripts\ninja.exe') @('-C', $Paths.Build, "-j$Jobs")
}

function Step-Test {
    if ($SkipTests) { Write-Host 'Tests skipped.'; return }
    Enter-Arm64DevShell
    Get-FreePortOrThrow $Port
    $env:PG_TEST_PORT_DIR = $null
    $meson = Join-Path $Paths.Venv 'Scripts\meson.exe'
    $logPath = Join-Path $Paths.Logs 'test.log'
    Push-Location $Paths.Build
    try {
        & $meson test -C $Paths.Build --print-errorlogs --num-processes $Jobs *>&1 | ForEach-Object { "$_" } | Tee-Object -FilePath $logPath | Select-Object -Last 15
        $code = $LASTEXITCODE
    }
    finally { Pop-Location }
    if ($code -ne 0) { throw "meson test failed with $code (log: $logPath)" }
}

function Step-Install {
    Enter-Arm64DevShell
    if (Test-Path $Paths.Install) { Remove-Item -Recurse -Force $Paths.Install }
    Invoke-Logged 'install.log' (Join-Path $Paths.Venv 'Scripts\meson.exe') @('install', '-C', $Paths.Build, '--quiet')
}

function Step-Pgvector {
    Enter-Arm64DevShell
    Get-FreePortOrThrow $Port
    $env:PGROOT = $Paths.Install
    Invoke-Logged 'pgvector-build.log' 'nmake.exe' @('/NOLOGO', '/F', 'Makefile.win') $PgvSrc
    Invoke-Logged 'pgvector-build.log' 'nmake.exe' @('/NOLOGO', '/F', 'Makefile.win', 'install') $PgvSrc
    $data = Join-Path $Paths.Smoke 'pgvector-data'
    if (Test-Path $data) { Remove-Item -Recurse -Force $data }
    Invoke-Logged 'pgvector-check.log' (Join-Path $Paths.Install 'bin\initdb.exe') @('-D', $data, '-U', 'postgres', '-A', 'trust', '-E', 'UTF8', '--no-locale')
    Start-Cluster $data (Join-Path $Paths.Logs 'pgvector-server.log') $null
    try {
        $env:PGHOST = $LoopbackAddress; $env:PGPORT = "$Port"; $env:PGUSER = 'postgres'
        $env:PATH = (Join-Path $Paths.Install 'bin') + ';' + $env:PATH
        Invoke-Logged 'pgvector-check.log' 'nmake.exe' @('/NOLOGO', '/F', 'Makefile.win', 'installcheck') $PgvSrc
    }
    finally {
        Stop-Cluster $data
        Remove-Item Env:PGHOST, Env:PGPORT, Env:PGUSER -ErrorAction SilentlyContinue
    }
}

function Step-Package {
    Enter-Arm64DevShell
    $bin = Join-Path $Paths.Install 'bin'
    foreach ($pattern in $DependencyDllPatterns) {
        Get-ChildItem (Join-Path $DepsRoot 'bin') -Filter $pattern | Copy-Item -Destination $bin -Force
    }
    $redistDir = Get-PinnedRedistDirectory
    $crt = Get-ChildItem (Join-Path $redistDir 'arm64') -Directory -Filter 'Microsoft.VC*.CRT' | Select-Object -First 1
    if (-not $crt) { throw "No arm64 CRT redist found below $redistDir." }
    # The arm64 redist also carries an x64 vcruntime140_1.dll (for ARM64EC); native arm64 code never imports it.
    foreach ($dll in Get-ChildItem $crt.FullName -Filter *.dll) {
        $target = Join-Path $bin $dll.Name
        if ((Get-PeMachine $dll.FullName) -eq $PeMachineArm64) { Copy-Item $dll.FullName $target -Force }
        elseif (Test-Path $target) { Remove-Item -Force $target }
    }
    Write-Host "Copied CRT from $($crt.FullName)"
}

function Step-Verify {
    Enter-Arm64DevShell
    $files = Get-ChildItem -Recurse -File (Join-Path $Paths.Install 'bin'), (Join-Path $Paths.Install 'lib') -Include *.exe, *.dll
    $bad = foreach ($f in $files) {
        $m = Get-PeMachine $f.FullName
        if ($m -ne $PeMachineArm64) { '{0} machine={1}' -f $f.FullName, ($(if ($null -eq $m) { 'not-PE' } else { '0x{0:X4}' -f $m })) }
    }
    "PE files checked: $($files.Count); non-ARM64: $(@($bad).Count)" | Tee-Object -FilePath (Join-Path $Paths.Logs 'pe-check.txt')
    if ($bad) { $bad | Add-Content (Join-Path $Paths.Logs 'pe-check.txt'); $bad | Write-Host; throw 'Non-ARM64 PE files found.' }

    $bin = Join-Path $Paths.Install 'bin'
    $sys = Join-Path $env:SystemRoot 'System32'
    $missing = foreach ($f in $files) {
        $deps = & dumpbin.exe /NOLOGO /DEPENDENTS $f.FullName | Where-Object { $_ -match '^\s+\S+\.dll\s*$' } | ForEach-Object { $_.Trim() }
        foreach ($d in $deps) {
            if ($d -like 'api-ms-*' -or $d -like 'ext-ms-*' -or $d -ieq 'postgres.exe') { continue }
            if (-not (Test-Path (Join-Path $bin $d)) -and -not (Test-Path (Join-Path $sys $d))) { "$($f.Name) -> $d" }
        }
    }
    if ($missing) { $missing | Write-Host; throw 'Unresolved DLL imports.' }

    $cleanEnv = @{ PATH = $sys; SystemRoot = $env:SystemRoot; TEMP = $env:TEMP; TMP = $env:TMP }
    foreach ($exe in @('postgres.exe', 'pg_dump.exe', 'psql.exe', 'initdb.exe')) {
        $info = [Diagnostics.ProcessStartInfo]::new((Join-Path $bin $exe), '--version')
        $info.UseShellExecute = $false; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.Environment.Clear(); foreach ($k in $cleanEnv.Keys) { $info.Environment[$k] = $cleanEnv[$k] }
        $p = [Diagnostics.Process]::Start($info); $out = $p.StandardOutput.ReadToEnd() + $p.StandardError.ReadToEnd(); $p.WaitForExit()
        if ($p.ExitCode -ne 0) { throw "$exe --version failed with clean PATH (exit $('0x{0:X8}' -f $p.ExitCode)): $out" }
        Write-Host "clean PATH: $($out.Trim())"
    }
}

function Step-Smoke {
    Get-FreePortOrThrow $Port
    $bin = Join-Path $Paths.Install 'bin'
    $sys = Join-Path $env:SystemRoot 'System32'
    $data = Join-Path $Paths.Smoke 'data'
    $dump = Join-Path $Paths.Smoke 'smoke.dump'
    if (Test-Path $data) { Remove-Item -Recurse -Force $data }
    $savedPath = $env:PATH
    $env:PATH = $sys
    try {
        Invoke-Logged 'smoke.log' (Join-Path $bin 'initdb.exe') @('-D', $data, '-U', 'postgres', '-A', 'trust', '-E', 'UTF8', '--locale-provider=icu', '--icu-locale=de-CH', '--locale=C')
        Start-Cluster $data (Join-Path $Paths.Logs 'smoke-server.log') @{ PATH = $sys; SystemRoot = $env:SystemRoot; TEMP = $env:TEMP; TMP = $env:TMP }
        try {
            function Invoke-Sql([string]$Sql, [string]$Db = 'postgres') {
                $out = & (Join-Path $bin 'psql.exe') -h $LoopbackAddress -p $Port -U postgres -d $Db -X -q -t -A -v ON_ERROR_STOP=1 -c $Sql 2>&1
                if ($LASTEXITCODE -ne 0) { throw "psql failed: $Sql`n$out" }
                return ($out | Out-String).Trim()
            }
            $r = [ordered]@{}
            $r.version = Invoke-Sql 'SELECT version()'
            $r.ssl_library = Invoke-Sql 'SHOW ssl_library'
            $r.icu_collation = Invoke-Sql "SELECT datlocprovider::text || ':' || coalesce(datlocale, '') FROM pg_database WHERE datname = 'postgres'"
            $r.icu_sort = Invoke-Sql "SELECT string_agg(w, ',' ORDER BY w COLLATE `"de-CH-x-icu`") FROM (VALUES ('Zebra'), (U&'\00C4pfel'), ('apfel'), ('Aepfel')) v(w)"
            Invoke-Sql 'CREATE EXTENSION vector; CREATE EXTENSION pg_trgm' | Out-Null
            $r.pgvector = Invoke-Sql "SELECT extversion FROM pg_extension WHERE extname = 'vector'"
            if ($r.pgvector -ne $Pins.pgvector.version) { throw "pgvector reports $($r.pgvector)" }
            $r.trgm = Invoke-Sql "SELECT similarity('klacks', 'klacks')"
            Invoke-Sql "CREATE TABLE lz (id int, t text COMPRESSION lz4); INSERT INTO lz SELECT g, repeat('klacks', 2000) FROM generate_series(1, 50) g" | Out-Null
            $r.lz4 = Invoke-Sql 'SELECT DISTINCT pg_column_compression(t) FROM lz'
            if ($r.lz4 -ne 'lz4') { throw "lz4 compression not used: $($r.lz4)" }
            Invoke-Sql "CREATE TABLE smoke_vectors (id integer PRIMARY KEY, embedding vector($SmokeVectorDimensions))" | Out-Null
            Invoke-Sql ("INSERT INTO smoke_vectors SELECT g, (SELECT array_agg((((g * 31 + d * 17) % 101) + 1)::real / 102) FROM generate_series(1, $SmokeVectorDimensions) d)::vector FROM generate_series(1, $SmokeVectorCount) g") | Out-Null
            Invoke-Sql "CREATE INDEX $HnswIndexName ON smoke_vectors USING hnsw (embedding vector_cosine_ops); ANALYZE smoke_vectors" | Out-Null
            $probe = '[' + ((1..$SmokeVectorDimensions | ForEach-Object { '0.5' }) -join ',') + ']'
            $query = "SELECT id FROM smoke_vectors ORDER BY embedding <=> '$probe' LIMIT $SmokeProbeResults"
            $plan = Invoke-Sql "SET enable_seqscan = off; EXPLAIN $query"
            if ($plan -notmatch $HnswIndexName) { throw "HNSW index not used:`n$plan" }
            $r.hnsw_plan = ($plan -split "`n")[0..1] -join ' | '
            Invoke-Logged 'smoke.log' (Join-Path $bin 'pg_dump.exe') @('-h', $LoopbackAddress, '-p', "$Port", '-U', 'postgres', '-Fc', '-Z', 'zstd', '-f', $dump, 'postgres')
            Invoke-Sql 'CREATE DATABASE restored' | Out-Null
            Invoke-Logged 'smoke.log' (Join-Path $bin 'pg_restore.exe') @('-h', $LoopbackAddress, '-p', "$Port", '-U', 'postgres', '-d', 'restored', '--exit-on-error', $dump)
            $r.restored_vectors = Invoke-Sql 'SELECT count(*) FROM smoke_vectors' 'restored'
            $r.restored_lz4 = Invoke-Sql 'SELECT count(*) FROM lz' 'restored'
            if ($r.restored_vectors -ne "$SmokeVectorCount" -or $r.restored_lz4 -ne '50') { throw 'Restore row counts differ.' }
            $plan2 = Invoke-Sql "SET enable_seqscan = off; EXPLAIN $query" 'restored'
            if ($plan2 -notmatch $HnswIndexName) { throw 'HNSW index not used after restore.' }
            $r.dump_bytes = (Get-Item $dump).Length
            $gzipDump = Join-Path $Paths.Smoke 'smoke-gzip.dump'
            Invoke-Logged 'smoke.log' (Join-Path $bin 'pg_dump.exe') @('-h', $LoopbackAddress, '-p', "$Port", '-U', 'postgres', '-Fc', '-f', $gzipDump, 'postgres')
            Invoke-Sql 'CREATE DATABASE restored_gzip' | Out-Null
            Invoke-Logged 'smoke.log' (Join-Path $bin 'pg_restore.exe') @('-h', $LoopbackAddress, '-p', "$Port", '-U', 'postgres', '-d', 'restored_gzip', '--exit-on-error', $gzipDump)
            $r.gzip_dump_compression = ((& (Join-Path $bin 'pg_restore.exe') -l $gzipDump) | Select-String 'Compression:').Line.Trim()
            $r.restored_gzip_vectors = Invoke-Sql 'SELECT count(*) FROM smoke_vectors' 'restored_gzip'
            if ($r.restored_gzip_vectors -ne "$SmokeVectorCount" -or $r.gzip_dump_compression -notmatch 'gzip') { throw "gzip (zlib) dump roundtrip failed: $($r.gzip_dump_compression)" }
            $plan3 = Invoke-Sql "SET enable_seqscan = off; EXPLAIN $query" 'restored_gzip'
            if ($plan3 -notmatch $HnswIndexName) { throw 'HNSW index not used after gzip restore.' }
            $r | ConvertTo-Json | Tee-Object -FilePath (Join-Path $Paths.Logs 'smoke-result.json')
        }
        finally { Stop-Cluster $data }
    }
    finally { $env:PATH = $savedPath }
}

$fromIdx = $Steps.IndexOf($From); $toIdx = $Steps.IndexOf($To)
foreach ($step in $Steps[$fromIdx..$toIdx]) {
    Write-Step $step
    $sw = [Diagnostics.Stopwatch]::StartNew()
    & "Step-$step"
    $sw.Stop()
    Save-Timing $step $sw.Elapsed
    Write-Host "$step done in $([math]::Round($sw.Elapsed.TotalMinutes, 1)) min"
}
