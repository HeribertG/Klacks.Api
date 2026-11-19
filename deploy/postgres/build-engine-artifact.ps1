# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Packs the Windows PostgreSQL engine (EDB binaries plus pgvector built with MSVC) into klacks-postgres-<version>-<rid>.zip
# and smoke-tests exactly that ZIP: initdb, start, CREATE EXTENSION vector and pg_trgm, an HNSW index that the planner
# uses. The ZIP root holds bin\, lib\ and share\ (no top-level folder), which the updater unpacks to pgsql-<major>.
# Writes the manifest entry { rid, bundleUrl, sha256, signature } with an EMPTY signature next to the ZIP; the signature
# is added by sign-engine-entry.ps1 in a separate step, so no key is present while third-party binaries run.
#
# Parameters: PostgresDirectory = EDB pgsql folder with pgvector installed; Major / Version / PgvectorVersion = values of
# deploy/postgres/engine.json; BundleUrl = public download URL of the ZIP (the signature binds it); OutputDirectory =
# where ZIP and entry are written; Rid = runtime identifier; SkipSmoke = pack only (structure checks still run).

#Requires -Version 7

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PostgresDirectory,
    [Parameter(Mandatory)][int]$Major,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$PgvectorVersion,
    [Parameter(Mandatory)][string]$BundleUrl,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Rid = 'win-x64',
    [switch]$SkipSmoke
)

$ErrorActionPreference = 'Stop'

$PackedParts = @('bin', 'lib', 'share')
$RequiredFiles = @(
    'bin\postgres.exe', 'bin\initdb.exe', 'bin\pg_ctl.exe', 'bin\pg_dump.exe', 'bin\pg_restore.exe', 'bin\psql.exe',
    'lib\vector.dll', 'share\extension\vector.control', 'share\extension\pg_trgm.control'
)
$SmokeVectorCount = 2000
$SmokeVectorDimensions = 8
$SmokeProbeResults = 5
$SmokeStartTimeoutSeconds = 90
$LoopbackAddress = '127.0.0.1'
$HnswIndexName = 'smoke_vectors_hnsw'

if ($Version -cnotmatch '^[0-9]+\.[0-9]+\z') { throw "Version '$Version' is not <major>.<minor>." }
if ($PgvectorVersion -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+\z') { throw "PgvectorVersion '$PgvectorVersion' is not X.Y.Z." }
if ($Version.Split('.')[0] -ne [string]$Major) { throw "Version '$Version' does not belong to major $Major." }

foreach ($relative in $RequiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $PostgresDirectory $relative))) {
        throw "'$relative' is missing below '$PostgresDirectory'."
    }
}
$controlText = Get-Content -Raw -LiteralPath (Join-Path $PostgresDirectory 'share\extension\vector.control')
if ($controlText -notmatch "default_version\s*=\s*'$([regex]::Escape($PgvectorVersion))'") {
    throw "vector.control does not carry default_version '$PgvectorVersion'."
}

$zipName = "klacks-postgres-$Version-$Rid.zip"
$entryName = "klacks-postgres-$Version-$Rid.json"
if (-not $BundleUrl.EndsWith("/$zipName", [StringComparison]::Ordinal)) { throw "BundleUrl must end with /$zipName." }

$work = Join-Path ([IO.Path]::GetTempPath()) ("klacks-pg-engine-" + [Guid]::NewGuid().ToString('N'))
$staging = Join-Path $work 'staging'
New-Item -ItemType Directory -Force -Path $staging, $OutputDirectory | Out-Null
$zipPath = Join-Path (Resolve-Path $OutputDirectory) $zipName
$entryPath = Join-Path (Resolve-Path $OutputDirectory) $entryName
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

function Invoke-Native {
    param([string]$FilePath, [string[]]$Arguments)
    $output = & $FilePath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "$FilePath exited with $LASTEXITCODE`n$output" }
    return ($output | Out-String).Trim()
}

# pg_ctl start leaves the postmaster running with inherited handles: capturing its output through a pipe would block until
# the server stops, so it runs without redirection and only the pg_ctl process itself is awaited.
function Start-PgCtl {
    param([string]$PgCtl, [string[]]$Arguments)
    $info = [Diagnostics.ProcessStartInfo]::new($PgCtl)
    $info.UseShellExecute = $false
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($info)
    try {
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "pg_ctl start exited with $($process.ExitCode); see the server log." }
    }
    finally {
        $process.Dispose()
    }
}

function Get-FreeTcpPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Parse($LoopbackAddress), 0)
    $listener.Start()
    try { return $listener.LocalEndpoint.Port } finally { $listener.Stop() }
}

function Invoke-Smoke {
    param([string]$Archive)

    $root = Join-Path $work 'smoke'
    $engine = Join-Path $root 'engine'
    $data = Join-Path $root 'data'
    [IO.Compression.ZipFile]::ExtractToDirectory($Archive, $engine)

    $postgres = Join-Path $engine 'bin\postgres.exe'
    $versionText = Invoke-Native $postgres @('--version')
    Write-Host $versionText
    if ($versionText -notmatch "PostgreSQL\)\s+$Major\.") { throw "postgres.exe reports '$versionText', expected major $Major." }

    Invoke-Native (Join-Path $engine 'bin\initdb.exe') @('-D', $data, '-U', 'postgres', '-A', 'trust', '-E', 'UTF8', '--no-locale') | Out-Null
    $port = Get-FreeTcpPort
    $log = Join-Path $root 'postgres.log'
    $pgCtl = Join-Path $engine 'bin\pg_ctl.exe'
    $psql = Join-Path $engine 'bin\psql.exe'
    $started = $false
    try {
        Start-PgCtl -PgCtl $pgCtl -Arguments @('start', '-w', '-t', [string]$SmokeStartTimeoutSeconds, '-D', $data, '-l', $log, '-o', "-p $port -c listen_addresses=$LoopbackAddress")
        $started = $true

        function Invoke-Sql {
            param([string]$Sql)
            return Invoke-Native $psql @('-h', $LoopbackAddress, '-p', $port, '-U', 'postgres', '-X', '-q', '-t', '-A', '-v', 'ON_ERROR_STOP=1', '-c', $Sql)
        }

        $versionNum = [int](Invoke-Sql 'SHOW server_version_num')
        if ([math]::Floor($versionNum / 10000) -ne $Major) { throw "Server reports version number $versionNum, expected major $Major." }

        Invoke-Sql 'CREATE EXTENSION vector' | Out-Null
        Invoke-Sql 'CREATE EXTENSION pg_trgm' | Out-Null
        $extversion = Invoke-Sql "SELECT extversion FROM pg_extension WHERE extname = 'vector'"
        if ($extversion -ne $PgvectorVersion) { throw "pgvector reports '$extversion', expected '$PgvectorVersion'." }
        if ((Invoke-Sql "SELECT similarity('klacks', 'klacks')") -ne '1') { throw 'pg_trgm similarity() did not return 1.' }

        Invoke-Sql "CREATE TABLE smoke_vectors (id integer PRIMARY KEY, embedding vector($SmokeVectorDimensions))" | Out-Null
        Invoke-Sql ("INSERT INTO smoke_vectors SELECT g, (SELECT array_agg((((g * 31 + d * 17) % 101) + 1)::real / 102) FROM generate_series(1, $SmokeVectorDimensions) d)::vector " +
            "FROM generate_series(1, $SmokeVectorCount) g") | Out-Null
        Invoke-Sql "CREATE INDEX $HnswIndexName ON smoke_vectors USING hnsw (embedding vector_cosine_ops)" | Out-Null
        Invoke-Sql 'ANALYZE smoke_vectors' | Out-Null

        $probe = '[' + ((1..$SmokeVectorDimensions | ForEach-Object { '0.5' }) -join ',') + ']'
        $query = "SELECT id FROM smoke_vectors ORDER BY embedding <=> '$probe' LIMIT $SmokeProbeResults"
        $plan = Invoke-Sql "SET enable_seqscan = off; EXPLAIN $query"
        if ($plan -notmatch [regex]::Escape($HnswIndexName)) { throw "The planner does not use the HNSW index:`n$plan" }
        $rows = @((Invoke-Sql $query) -split "`n" | Where-Object { $_.Trim() })
        if ($rows.Count -ne $SmokeProbeResults) { throw "HNSW query returned $($rows.Count) rows, expected $SmokeProbeResults." }
        Write-Host "Smoke test passed: PostgreSQL $Major, pgvector $extversion, pg_trgm, HNSW index scan."
    }
    catch {
        if (Test-Path $log) { Write-Host '--- server log (tail) ---'; Get-Content -LiteralPath $log -Tail 40 | Write-Host }
        throw
    }
    finally {
        if ($started) { & $pgCtl stop -m immediate -D $data 2>&1 | Out-Null }
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
try {
    foreach ($part in $PackedParts) {
        Copy-Item -Recurse -LiteralPath (Join-Path $PostgresDirectory $part) -Destination (Join-Path $staging $part)
    }
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)

    $entries = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $topLevel = $entries.Entries | ForEach-Object { $_.FullName.Split('/')[0] } | Sort-Object -Unique
        if (($topLevel -join ',') -cne ($PackedParts -join ',')) { throw "ZIP root holds '$($topLevel -join ',')', expected '$($PackedParts -join ',')'." }
        foreach ($relative in $RequiredFiles) {
            if (-not ($entries.Entries | Where-Object { $_.FullName -ceq $relative.Replace('\', '/') })) { throw "ZIP lacks $relative." }
        }
    }
    finally {
        $entries.Dispose()
    }

    $sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
    if (-not $SkipSmoke) { Invoke-Smoke -Archive $zipPath }

    $entry = [ordered]@{ rid = $Rid; bundleUrl = $BundleUrl; sha256 = $sha256; signature = '' }
    [IO.File]::WriteAllText($entryPath, ($entry | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    $sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "Packed $zipName ($sizeMb MB), SHA-256 $sha256"
}
finally {
    if (Test-Path $work) { Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $work }
}
