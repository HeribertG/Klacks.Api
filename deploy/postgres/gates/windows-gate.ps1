# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Engine upgrade gate, Windows native driver (office-PC installation). Called step by step by
# .github/workflows/engine-upgrade-gate.yml; every -Action is one step of a scenario:
#   InstallBaseline     released office-PC package (GATE_BASELINE_VERSION, PostgreSQL 17) through its Install-Klacks.ps1;
#                       the manifest URL points nowhere, so the API never queues an app update during the gate
#   ApplyCandidate      publishes Klacks.Updater from GATE_UPDATER_DIR (checkout at the candidate commit), points the
#                       KlacksUpdater service at it and makes it trust the throw-away CI key (klacks.native.json, which
#                       the updater reads after its environment) - plus the fault phase when GATE_FAULT_PHASE is set
#   Seed / Calibrate / Snapshot -Label / Compare -Label -OtherLabel / Enqueue / KillDuringRestore /
#   WaitTerminal -Expected / AssertSuccess / AssertRollback / RestartServices / Diagnostics -Directory
#                       same meaning as in docker-gate.sh
# AssertRollback covers fail-after-restore, kill-during-restore and bad-hash: PostgreSQL GATE_FROM_MAJOR active, no
# pointer, no failed pgdata-<to> folder, KlacksPostgres service path unchanged, journal outcome = final row status.
#
# Environment: GATE_REPO_DIR, GATE_STATE_DIR, GATE_WORK_DIR, GATE_BASELINE_VERSION, GATE_UPDATER_DIR,
# GATE_PUBLIC_KEY_FILE, GATE_FAULT_PHASE (optional), GATE_FROM_MAJOR, GATE_TO_MAJOR, GATE_EXPECTED_VECTOR,
# GATE_ENGINE_REF, GATE_ENGINE_SHA, GATE_SIGNATURE_FILE, GATE_FILLER_ROWS.
#
# Windows PowerShell 5.1 compatible and ASCII only, like the installer it drives.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('InstallBaseline', 'ApplyCandidate', 'Seed', 'Calibrate', 'Snapshot', 'Compare', 'Enqueue',
        'KillDuringRestore', 'WaitTerminal', 'AssertSuccess', 'AssertRollback', 'RestartServices', 'Diagnostics')]
    [string]$Action,
    [string]$Label = '',
    [string]$OtherLabel = '',
    [string]$Expected = '',
    [string]$Directory = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$ProgramRoot = Join-Path $env:ProgramFiles 'Klacks'
$DataRoot = Join-Path $env:ProgramData 'Klacks'
$ConfigFile = Join-Path $DataRoot 'config\klacks.native.json'
$SecretsFile = Join-Path $DataRoot 'config\secrets.json'
$PointerFile = Join-Path $DataRoot 'config\postgres-engine.json'
$LegacyPostgresDirectory = Join-Path $ProgramRoot 'pgsql'
$LegacyDataDirectory = Join-Path $DataRoot 'pgdata'
$Psql = Join-Path $LegacyPostgresDirectory 'bin\psql.exe'
$CandidateUpdaterDirectory = 'C:\klacks-gate\updater'
$GitBash = Join-Path $env:ProgramFiles 'Git\bin\bash.exe'
$PostgresServiceName = 'KlacksPostgres'
$ApiServiceName = 'Klacks'
$UpdaterServiceName = 'KlacksUpdater'
$UpdaterProcessName = 'Klacks.Updater'
$PostgresSuperUser = 'klacks'
$KlacksDatabase = 'Klacks'
$SideDatabases = @('klacks_diag', 'klacks_telemetry')
$FillerDatabase = 'klacks_diag'
$HttpPort = 5180
$BaseUrl = "http://127.0.0.1:$HttpPort"
$NoManifestUrl = 'https://127.0.0.1:9/engine-upgrade-gate-no-manifest'
$ReleaseDownloadBase = 'https://github.com/HeribertG/Klacks.Api/releases/download'
$PackageFileName = 'klacks-office-pc-win-x64.zip'
$SeedAdminEmail = 'admin@test.com'
$SeedAdminPassword = 'P@ssw0rt1'
$Capability = 'postgres-engine-upgrade/1'
$FixedVolatileTables = @('public.update_history')
$GateTablePrefix = 'public.engine_gate_'
$UsersSid = 'S-1-5-32-545'
$HealthTimeoutSeconds = 1800
$CapabilityTimeoutSeconds = 300
$UpgradeTimeoutSeconds = 2700
$PollSeconds = 10
$KillPollMilliseconds = 200
$CalibrationIdleSeconds = 60
$ServiceTimeout = [TimeSpan]::FromMinutes(5)
$StatusSucceeded = 2
$StatusFailed = 3
$TerminalStatuses = @(2, 3, 4, 6)
$OutcomeByStatus = @{ 2 = 'Succeeded'; 3 = 'Failed'; 4 = 'RolledBack'; 6 = 'RollbackFailed' }
$KillPhases = @('NewEngineActive', 'Restored', 'Verified')
$LatePhases = @('ConfigurationCommitted', 'ApplicationHealthy', 'Committed', 'RolledBack', 'RollbackFailed')
$KnnListCount = 20
$ExitCodeOperationActive = 3

function Fail([string]$Message) { throw "ENGINE GATE: $Message" }
function Write-Note([string]$Message) { Write-Host "==> $Message" -ForegroundColor Cyan }

function Get-RequiredEnv([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($value)) { Fail "$Name is not set." }
    return $value
}

function Get-StatePath([string]$Name) { return Join-Path (Get-RequiredEnv 'GATE_STATE_DIR') $Name }

function Read-Config { return Get-Content -Raw $ConfigFile | ConvertFrom-Json }

function Get-PostgresPort {
    $connection = (Read-Config).ConnectionStrings.DefaultConnection
    if ($connection -notmatch 'Port=(\d+)') { Fail 'No port in the connection string of klacks.native.json.' }
    return [int]$Matches[1]
}

function Set-PostgresPassword {
    $env:PGPASSWORD = (Get-Content -Raw $SecretsFile | ConvertFrom-Json).DatabasePassword
}

function Invoke-GatePsql([string]$Database, [string[]]$Arguments, [switch]$AllowFailure) {
    Set-PostgresPassword
    $output = & $Psql -X -q -v ON_ERROR_STOP=1 -At -h 127.0.0.1 -p (Get-PostgresPort) -U $PostgresSuperUser -d $Database @Arguments
    if ($LASTEXITCODE -ne 0) {
        if ($AllowFailure) { return $null }
        Fail "psql against $Database failed with exit code $LASTEXITCODE."
    }
    return @($output | ForEach-Object { "$_".TrimEnd("`r") })
}

function Invoke-GateSqlFile([string]$Database, [string]$FileName, [string[]]$Variables = @()) {
    $file = Join-Path (Join-Path (Get-RequiredEnv 'GATE_REPO_DIR') 'deploy\postgres\gates') $FileName
    $arguments = @()
    foreach ($variable in $Variables) { $arguments += @('-v', $variable) }
    return Invoke-GatePsql $Database ($arguments + @('-f', $file))
}

function Invoke-GateQuery([string]$Database, [string]$Sql, [switch]$AllowFailure) {
    return Invoke-GatePsql $Database @('-c', $Sql) -AllowFailure:$AllowFailure
}

function Wait-Healthy {
    $deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if ((Invoke-WebRequest -UseBasicParsing "$BaseUrl/internal/health/deep" -TimeoutSec 10).StatusCode -eq 200) {
                Write-Note 'Klacks is healthy.'
                return
            }
        }
        catch { }
        Start-Sleep -Seconds $PollSeconds
    }
    Fail "Klacks is not healthy after $HealthTimeoutSeconds s."
}

function Test-Login {
    $body = @{ email = $SeedAdminEmail; password = $SeedAdminPassword } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -ContentType 'application/json' -Uri "$BaseUrl/api/backend/Accounts/LoginUser" -Body $body
    if (-not $login.token) { Fail 'Sign-in with the seeded admin returned no token.' }
    Write-Note 'Seeded admin signed in.'
}

function Disable-AutoUpdate {
    Invoke-GateQuery $KlacksDatabase "UPDATE settings SET value = 'False' WHERE type = 'UPDATE_AUTO_ENABLED';" | Out-Null
    Write-Note 'Automatic app updates switched off (the gate must be the only one queueing operations).'
}

function Get-ServicePath([string]$Name) { return (Get-CimInstance Win32_Service -Filter "Name='$Name'").PathName }

function Wait-ServiceStatus([string]$Name, [string]$Status) {
    (Get-Service -Name $Name).WaitForStatus($Status, $ServiceTimeout)
}

function Wait-Capability {
    $deadline = (Get-Date).AddSeconds($CapabilityTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $value = "$(Invoke-GateQuery $KlacksDatabase "SELECT value FROM settings WHERE type = 'UPDATER_CAPABILITIES' LIMIT 1;" -AllowFailure)"
        if ($value.Contains($Capability) -and $value.Contains('Native')) {
            Write-Note "Updater capability: $value"
            return
        }
        Start-Sleep -Seconds $PollSeconds
    }
    Fail "No UPDATER_CAPABILITIES row with $Capability and mode Native after $CapabilityTimeoutSeconds s (candidate updater not running, or the native adapter is not registered)."
}

function Get-JournalDirectory { return Join-Path (Read-Config).Updater.BackupDirectory 'engine-upgrade' }

function Get-OperationId { return (Get-Content -Raw (Get-StatePath 'operation-id')).Trim() }

function Get-RowState {
    $id = Get-OperationId
    $state = Invoke-GateQuery $KlacksDatabase "SELECT status || '|' || coalesce(message, '') FROM update_history WHERE id = '$id';" -AllowFailure
    if (-not $state) { return $null }
    return "$($state[0])"
}

function Get-Status([string]$State) {
    if (-not $State) { return -1 }
    return [int]$State.Split('|')[0]
}

function Get-RowSnapshot {
    $lines = @()
    foreach ($database in @($KlacksDatabase) + $SideDatabases) {
        foreach ($line in (Invoke-GateSqlFile $database 'snapshot-database.sql')) { $lines += "$database|$line" }
    }
    return $lines
}

function Get-VolatileTables {
    return @(Get-Content (Get-StatePath 'volatile-tables.txt') | Where-Object { $_ })
}

function Get-ComparableRows([string]$File) {
    $volatile = Get-VolatileTables
    $result = @()
    foreach ($line in (Get-Content $File)) {
        $parts = $line.Split('|')
        if ($parts.Count -ne 4 -or $parts[1] -ne 'rows') { continue }
        if ($parts[0] -eq $KlacksDatabase -and $volatile -contains $parts[2]) { continue }
        $result += $line
    }
    return $result
}

function Assert-SameLines([string[]]$Before, [string[]]$After, [string]$What) {
    $difference = Compare-Object -ReferenceObject @($Before) -DifferenceObject @($After)
    if ($difference) {
        $difference | Format-Table -AutoSize | Out-String -Width 400 | Write-Host
        Fail "$What differ (see above)."
    }
}

function Assert-Journal([string]$ExpectedOutcome) {
    $directory = Get-JournalDirectory
    if (Test-Path (Join-Path $directory 'journal.json')) { Fail 'The engine upgrade journal is still open after the operation finished.' }
    $archive = Join-Path $directory ("journal-{0}.json" -f (Get-OperationId))
    if (-not (Test-Path $archive)) { Fail "No archived journal $archive." }
    $journal = Get-Content -Raw $archive | ConvertFrom-Json
    Write-Host ("journal: phase {0}, outcome {1}, {2} -> {3}, mode {4}" -f $journal.phase, $journal.outcome, $journal.fromMajor, $journal.toMajor, $journal.mode)
    if ($journal.outcome -ne $ExpectedOutcome) { Fail "Journal outcome is $($journal.outcome), expected $ExpectedOutcome." }
}

function Assert-EngineMajor([int]$Major, [string]$VectorVersion) {
    Invoke-GateSqlFile $KlacksDatabase 'assert-engine.sql' @("expected_major=$Major", "expected_vector=$VectorVersion") | Write-Host
}

function Invoke-InstallBaseline {
    $work = Get-RequiredEnv 'GATE_WORK_DIR'
    $version = Get-RequiredEnv 'GATE_BASELINE_VERSION'
    New-Item -ItemType Directory -Force -Path $work, (Get-RequiredEnv 'GATE_STATE_DIR') | Out-Null
    $zip = Join-Path $work $PackageFileName
    & curl.exe -fsSL --retry 3 -o $zip "$ReleaseDownloadBase/v$version/$PackageFileName"
    if ($LASTEXITCODE -ne 0) { Fail "Download of $PackageFileName $version failed." }
    $packageDirectory = Join-Path $work 'package'
    New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null
    & tar.exe -xf $zip -C $packageDirectory
    if ($LASTEXITCODE -ne 0) { Fail "Could not unpack $zip." }
    $manifest = Get-ChildItem -Recurse -Filter 'package.json' $packageDirectory | Select-Object -First 1
    if (-not $manifest) { Fail 'The package has no package.json.' }
    $root = $manifest.DirectoryName
    $package = Get-Content -Raw $manifest.FullName | ConvertFrom-Json
    if ($package.version -ne $version) { Fail "The package is version $($package.version), expected $version." }
    $bundle = Join-Path $root $package.bundle
    if ((Get-FileHash -Algorithm SHA256 $bundle).Hash.ToLowerInvariant() -ne $package.sha256) { Fail 'The bundle does not match package.json.' }
    $arguments = @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $root 'scripts\Install-Klacks.ps1'),
        '-BundleZip', $bundle, '-Version', $version, '-PostgresDirectory', (Join-Path $root 'pgsql'),
        '-UpdatePublicKeyPath', (Join-Path $root 'update-public-key.pem'), '-VcRedistPath', (Join-Path $root 'vc_redist.x64.exe'),
        '-HttpPort', $HttpPort, '-UpdateManifestBaseUrl', $NoManifestUrl)
    & powershell.exe @arguments
    if ($LASTEXITCODE -ne 0) { Fail "Install-Klacks.ps1 $version exited with $LASTEXITCODE." }
    Wait-Healthy
    Disable-AutoUpdate
    Test-Login
    $major = "$(Get-Content -TotalCount 1 (Join-Path $LegacyDataDirectory 'PG_VERSION'))".Trim()
    if ($major -ne (Get-RequiredEnv 'GATE_FROM_MAJOR')) { Fail "The baseline cluster is PostgreSQL $major." }
    Get-ServicePath $PostgresServiceName | Set-Content -Encoding ASCII (Get-StatePath 'postgres-service-path.txt')
    Write-Note "Baseline $version installed on PostgreSQL $major; KlacksPostgres: $(Get-ServicePath $PostgresServiceName)"
}

function Invoke-ApplyCandidate {
    $source = Get-RequiredEnv 'GATE_UPDATER_DIR'
    $project = Join-Path $source 'Klacks.Updater\Klacks.Updater.csproj'
    & dotnet publish $project -c Release -r win-x64 --self-contained true -o $CandidateUpdaterDirectory | Out-Host
    if ($LASTEXITCODE -ne 0) { Fail "dotnet publish of the candidate updater failed ($LASTEXITCODE)." }
    $exe = Join-Path $CandidateUpdaterDirectory "$UpdaterProcessName.exe"

    Stop-Service -Name $UpdaterServiceName
    Wait-ServiceStatus $UpdaterServiceName 'Stopped'
    $service = Get-CimInstance Win32_Service -Filter "Name='$UpdaterServiceName'"
    $result = Invoke-CimMethod -InputObject $service -MethodName Change -Arguments @{ PathName = "`"$exe`"" }
    if ($result.ReturnValue -ne 0) { Fail "Changing the KlacksUpdater service path failed (Win32_Service.Change $($result.ReturnValue))." }

    $config = Read-Config
    $config.Updater.SignaturePublicKey = (Get-Content -Raw (Get-RequiredEnv 'GATE_PUBLIC_KEY_FILE')).Replace("`r`n", "`n").Trim()
    $faultPhase = [Environment]::GetEnvironmentVariable('GATE_FAULT_PHASE')
    if ($faultPhase) {
        if ($faultPhase -notmatch '^[A-Za-z]+$') { Fail "GATE_FAULT_PHASE '$faultPhase' is not a phase name." }
        $config.Updater | Add-Member -NotePropertyName 'EngineUpgradeFaultPhase' -NotePropertyValue $faultPhase -Force
    }
    $config | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $ConfigFile

    Start-Service -Name $UpdaterServiceName
    Wait-ServiceStatus $UpdaterServiceName 'Running'
    $path = Get-ServicePath $UpdaterServiceName
    if ($path -ne "`"$exe`"") { Fail "KlacksUpdater runs $path." }
    Write-Note "KlacksUpdater runs the candidate $path$(if ($faultPhase) { " (fault after $faultPhase)" })."
    Wait-Capability
}

function Invoke-Seed {
    Invoke-GateSqlFile $KlacksDatabase 'seed-databases.sql' | Write-Host
    Invoke-GateSqlFile $KlacksDatabase 'seed-gate-vectors.sql' | Write-Host
    foreach ($database in $SideDatabases) {
        $rows = if ($database -eq $FillerDatabase) { Get-RequiredEnv 'GATE_FILLER_ROWS' } else { '0' }
        Invoke-GateSqlFile $database 'seed-side-database.sql' @("filler_rows=$rows") | Write-Host
    }
    Disable-AutoUpdate
}

function Invoke-Calibrate {
    $before = Get-RowSnapshot
    Restart-Service -Name $ApiServiceName
    Wait-Healthy
    Test-Login
    Start-Sleep -Seconds $CalibrationIdleSeconds
    $after = Get-RowSnapshot
    $volatile = @($FixedVolatileTables)
    foreach ($change in (Compare-Object -ReferenceObject $before -DifferenceObject $after)) {
        $parts = "$($change.InputObject)".Split('|')
        if ($parts.Count -eq 4 -and $parts[0] -eq $KlacksDatabase -and $parts[1] -eq 'rows' -and -not $parts[2].StartsWith($GateTablePrefix)) {
            $volatile += $parts[2]
        }
    }
    $volatile = @($volatile | Sort-Object -Unique)
    $volatile | Set-Content -Encoding ASCII (Get-StatePath 'volatile-tables.txt')
    Write-Note 'Klacks tables that change without an upgrade (excluded from the row-count comparison):'
    $volatile | Write-Host
}

function Invoke-Snapshot {
    if (-not $Label) { Fail '-Label is required.' }
    Get-RowSnapshot | Set-Content -Encoding ASCII (Get-StatePath "$Label-rows.txt")
    $knn = Invoke-GateSqlFile $KlacksDatabase 'snapshot-knn.sql'
    $knn | Set-Content -Encoding ASCII (Get-StatePath "$Label-knn.txt")
    if (@($knn | Where-Object { $_.StartsWith('knn|') }).Count -ne $KnnListCount) { Fail "Snapshot $Label has no $KnnListCount KNN lists." }
    Write-Note "Snapshot $Label written."
}

function Invoke-Compare {
    if (-not $Label -or -not $OtherLabel) { Fail '-Label and -OtherLabel are required.' }
    $a = Get-StatePath "$Label-rows.txt"
    $b = Get-StatePath "$OtherLabel-rows.txt"
    $comparable = Get-ComparableRows $b
    Assert-SameLines (Get-ComparableRows $a) $comparable "Row counts of $Label and $OtherLabel"
    $extensionsA = @(Get-Content $a | Where-Object { $_.Split('|')[1] -eq 'ext' } | ForEach-Object { $p = $_.Split('|'); "$($p[0])|$($p[2])" })
    $extensionsB = @(Get-Content $b | Where-Object { $_.Split('|')[1] -eq 'ext' } | ForEach-Object { $p = $_.Split('|'); "$($p[0])|$($p[2])" })
    Assert-SameLines $extensionsA $extensionsB "Installed extensions of $Label and $OtherLabel"
    $invalid = @(Get-Content $b | Where-Object { $_.Split('|')[1] -eq 'invalid_index' })
    if ($invalid.Count -gt 0) { $invalid | Write-Host; Fail "Invalid indexes in snapshot $OtherLabel." }
    $knnA = @(Get-Content (Get-StatePath "$Label-knn.txt") | Where-Object { $_.StartsWith('knn|') })
    $knnB = @(Get-Content (Get-StatePath "$OtherLabel-knn.txt") | Where-Object { $_.StartsWith('knn|') })
    if (Compare-Object -ReferenceObject $knnA -DifferenceObject $knnB -SyncWindow 0) {
        Get-Content (Get-StatePath "$Label-knn.txt"), (Get-StatePath "$OtherLabel-knn.txt") | Write-Host
        Fail "Exact top-10 lists differ between $Label and $OtherLabel."
    }
    Write-Note "Snapshots $Label and $OtherLabel match: $($comparable.Count) tables compared, $KnnListCount KNN lists identical."
}

function Invoke-EnqueueScript {
    Set-PostgresPassword
    $psqlUnix = '/' + $Psql.Substring(0, 1).ToLowerInvariant() + $Psql.Substring(2).Replace('\', '/')
    $prefix = "'$psqlUnix' -h 127.0.0.1 -p $(Get-PostgresPort) -U $PostgresSuperUser -d $KlacksDatabase"
    $script = Join-Path (Get-RequiredEnv 'GATE_REPO_DIR') 'deploy\postgres\enqueue-engine-upgrade.sh'
    $signature = (Get-Content -Raw (Get-RequiredEnv 'GATE_SIGNATURE_FILE')).Trim()
    $output = & $GitBash $script --psql $prefix --from (Get-RequiredEnv 'GATE_FROM_MAJOR') --to (Get-RequiredEnv 'GATE_TO_MAJOR') `
        --ref (Get-RequiredEnv 'GATE_ENGINE_REF') --sha (Get-RequiredEnv 'GATE_ENGINE_SHA') --sig $signature --requested-by 'engine-upgrade-gate'
    return @{ ExitCode = $LASTEXITCODE; Output = "$output".Trim() }
}

function Invoke-Enqueue {
    $first = Invoke-EnqueueScript
    if ($first.ExitCode -ne 0) { Fail "enqueue-engine-upgrade.sh exited with $($first.ExitCode)." }
    $first.Output | Set-Content -Encoding ASCII (Get-StatePath 'operation-id')
    Write-Note "Queued engine upgrade $($first.Output) ($env:GATE_ENGINE_REF, $env:GATE_ENGINE_SHA)."
    $second = Invoke-EnqueueScript
    if ($second.ExitCode -ne $ExitCodeOperationActive) { Fail "A second enqueue while $($first.Output) is active ended with exit code $($second.ExitCode), expected $ExitCodeOperationActive." }
    Write-Note "A second enqueue was refused while the first operation is active (exit code $ExitCodeOperationActive)."
}

function Invoke-WaitTerminal {
    if (-not $Expected) { Fail '-Expected is required (comma-separated statuses).' }
    $deadline = (Get-Date).AddSeconds($UpgradeTimeoutSeconds)
    while ($true) {
        $state = Get-RowState
        $status = Get-Status $state
        if ($TerminalStatuses -contains $status) { break }
        if ((Get-Date) -gt $deadline) { Fail "The engine upgrade did not finish within $UpgradeTimeoutSeconds s (last state '$state')." }
        Write-Host ("{0:HH:mm:ss} status {1}" -f (Get-Date).ToUniversalTime(), $(if ($status -ge 0) { $status } else { 'unreachable' }))
        Start-Sleep -Seconds $PollSeconds
    }
    "$status" | Set-Content -Encoding ASCII (Get-StatePath 'final-status')
    Write-Note "Engine upgrade finished: $state"
    if (($Expected.Split(',') | ForEach-Object { [int]$_ }) -notcontains $status) { Fail "Status $status, expected one of $Expected." }
}

function Invoke-KillDuringRestore {
    $journalFile = Join-Path (Get-JournalDirectory) 'journal.json'
    $deadline = (Get-Date).AddSeconds($UpgradeTimeoutSeconds)
    while ($true) {
        $phase = $null
        try { if (Test-Path $journalFile) { $phase = (Get-Content -Raw $journalFile | ConvertFrom-Json).phase } } catch { }
        if ($KillPhases -contains $phase) {
            $processId = (Get-CimInstance Win32_Service -Filter "Name='$UpdaterServiceName'").ProcessId
            & taskkill.exe /F /T /PID $processId | Out-Host
            "$phase" | Set-Content -Encoding ASCII (Get-StatePath 'kill-phase')
            Write-Note "KlacksUpdater (process $processId) killed in phase $phase."
            break
        }
        if ($LatePhases -contains $phase) { Fail "The upgrade reached $phase before the gate could kill the updater (window missed; raise GATE_FILLER_ROWS)." }
        if ((Get-Date) -gt $deadline) { Fail 'The restore phase was never reached.' }
        Start-Sleep -Milliseconds $KillPollMilliseconds
    }
    Wait-ServiceStatus $UpdaterServiceName 'Stopped'
    Start-Service -Name $UpdaterServiceName
    Write-Note 'KlacksUpdater started again; its startup recovery has to revert the upgrade.'
}

function Assert-NoUsersAccess([string]$Path) {
    $users = New-Object System.Security.Principal.SecurityIdentifier($UsersSid)
    $open = (Get-Acl $Path).Access | Where-Object {
        try { $_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]) -eq $users } catch { $false }
    }
    if ($open) { Fail "$Path is accessible for the Users group." }
}

function Invoke-AssertSuccess {
    $from = Get-RequiredEnv 'GATE_FROM_MAJOR'
    $to = Get-RequiredEnv 'GATE_TO_MAJOR'
    Assert-EngineMajor $to (Get-RequiredEnv 'GATE_EXPECTED_VECTOR')
    Invoke-GateSqlFile $KlacksDatabase 'assert-hnsw.sql' | Write-Host

    $postgresDirectory = Join-Path $ProgramRoot "pgsql-$to"
    $dataDirectory = Join-Path $DataRoot "pgdata-$to"
    if (-not (Test-Path $PointerFile)) { Fail "No engine pointer $PointerFile." }
    $pointer = Get-Content -Raw $PointerFile | ConvertFrom-Json
    if ($pointer.schemaVersion -ne 1 -or $pointer.major -ne [int]$to) { Fail "Pointer schemaVersion $($pointer.schemaVersion), major $($pointer.major)." }
    if ($pointer.postgresDirectory.TrimEnd('\') -ne $postgresDirectory) { Fail "Pointer postgresDirectory is $($pointer.postgresDirectory), expected $postgresDirectory." }
    if ($pointer.dataDirectory.TrimEnd('\') -ne $dataDirectory) { Fail "Pointer dataDirectory is $($pointer.dataDirectory), expected $dataDirectory." }

    $servicePath = Get-ServicePath $PostgresServiceName
    Write-Host "KlacksPostgres: $servicePath"
    $normalized = $servicePath.Replace('/', '\')
    if (-not $normalized.Contains("`"$postgresDirectory\bin\pg_ctl.exe`"")) { Fail "KlacksPostgres does not run $postgresDirectory\bin\pg_ctl.exe (quoted)." }
    if (-not $normalized.Contains("`"$dataDirectory`"")) { Fail "KlacksPostgres does not use $dataDirectory (quoted)." }

    $dependencies = @((Get-Service -Name $UpdaterServiceName).ServicesDependedOn | ForEach-Object { $_.Name })
    if ($dependencies -contains $PostgresServiceName) { Fail 'KlacksUpdater still depends on KlacksPostgres.' }
    Assert-NoUsersAccess $dataDirectory

    $binDirectory = (Read-Config).Updater.PostgresBinDirectory
    if ($binDirectory.TrimEnd('\') -ne (Join-Path $postgresDirectory 'bin')) { Fail "Updater.PostgresBinDirectory is $binDirectory." }

    $oldMajor = "$(Get-Content -TotalCount 1 (Join-Path $LegacyDataDirectory 'PG_VERSION'))".Trim()
    if ($oldMajor -ne $from) { Fail "The old cluster $LegacyDataDirectory has PG_VERSION '$oldMajor', expected $from (kept for rollback)." }
    if (-not (Test-Path (Join-Path $LegacyPostgresDirectory 'bin\pg_ctl.exe'))) { Fail 'The old PostgreSQL binaries are gone.' }

    Assert-Journal 'Succeeded'
    Wait-Healthy
    Test-Login
    Write-Note "Upgrade verified: PostgreSQL $to from $postgresDirectory, data $dataDirectory, old $from cluster kept."
}

function Invoke-AssertRollback {
    $from = Get-RequiredEnv 'GATE_FROM_MAJOR'
    $to = Get-RequiredEnv 'GATE_TO_MAJOR'
    $status = [int](Get-Content -Raw (Get-StatePath 'final-status')).Trim()
    Assert-EngineMajor $from ''
    if (Test-Path $PointerFile) { Fail "The engine pointer $PointerFile still exists after the rollback." }
    $failedData = Join-Path $DataRoot "pgdata-$to"
    if (Test-Path (Join-Path $failedData 'PG_VERSION')) { Fail "The failed cluster $failedData is still there (the installer refuses to run next to it)." }
    $expectedPath = (Get-Content -Raw (Get-StatePath 'postgres-service-path.txt')).Trim()
    $servicePath = Get-ServicePath $PostgresServiceName
    if ($servicePath -ne $expectedPath) { Fail "KlacksPostgres service path changed: '$servicePath', expected '$expectedPath'." }
    $binDirectory = (Read-Config).Updater.PostgresBinDirectory
    if ($binDirectory.TrimEnd('\') -ne (Join-Path $LegacyPostgresDirectory 'bin')) { Fail "Updater.PostgresBinDirectory is $binDirectory after the rollback." }
    if ($status -eq $StatusFailed) {
        # Failed comes from a refusal before Accepted: the updater never opened a journal.
        if (Test-Path (Join-Path (Get-JournalDirectory) 'journal.json')) { Fail 'An engine upgrade journal is open although the operation was refused.' }
    }
    else {
        Assert-Journal $OutcomeByStatus[$status]
    }
    Wait-Healthy
    Test-Login
    Write-Note "Rollback verified (status $status): PostgreSQL $from active, layout unchanged."
}

function Invoke-RestartServices {
    $expectedMajor = if ([int](Get-Content -Raw (Get-StatePath 'final-status')).Trim() -eq $StatusSucceeded) { Get-RequiredEnv 'GATE_TO_MAJOR' } else { Get-RequiredEnv 'GATE_FROM_MAJOR' }
    foreach ($name in $ApiServiceName, $UpdaterServiceName, $PostgresServiceName) {
        Stop-Service -Name $name
        Wait-ServiceStatus $name 'Stopped'
    }
    foreach ($name in $PostgresServiceName, $ApiServiceName, $UpdaterServiceName) {
        Start-Service -Name $name
    }
    Wait-ServiceStatus $UpdaterServiceName 'Running'
    Wait-Healthy
    Assert-EngineMajor $expectedMajor ''
    Test-Login
    Write-Note "All services restarted: healthy on PostgreSQL $expectedMajor."
}

function Invoke-Diagnostics {
    if (-not $Directory) { Fail '-Directory is required.' }
    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    $ErrorActionPreference = 'Continue'
    try { & (Join-Path (Get-RequiredEnv 'GATE_REPO_DIR') 'deploy\native\windows\Get-KlacksDiagnostics.ps1') -ReportDirectory $Directory } catch { Write-Warning $_ }
    Copy-Item -Recurse -ErrorAction SilentlyContinue (Join-Path $DataRoot 'logs') (Join-Path $Directory 'logs')
    try { Copy-Item -Recurse -ErrorAction SilentlyContinue (Get-JournalDirectory) (Join-Path $Directory 'engine-upgrade') } catch { }
    Copy-Item -ErrorAction SilentlyContinue $PointerFile $Directory
    Get-ChildItem -ErrorAction SilentlyContinue (Join-Path $DataRoot 'pgdata-*'), (Join-Path $ProgramRoot 'pgsql-*') |
        Select-Object FullName | Out-File (Join-Path $Directory 'versioned-folders.txt')
    foreach ($name in $PostgresServiceName, $ApiServiceName, $UpdaterServiceName) {
        "$name : $(Get-ServicePath $name)" | Out-File -Append (Join-Path $Directory 'services.txt')
    }
    try {
        Invoke-GateQuery $KlacksDatabase 'SELECT id, operation_type, status, from_version, target_version, artifact_ref, artifact_sha256, requested_at, started_at, completed_at, message FROM update_history ORDER BY requested_at;' -AllowFailure |
            Out-File (Join-Path $Directory 'update-history.txt')
    }
    catch { Write-Warning $_ }
    $state = [Environment]::GetEnvironmentVariable('GATE_STATE_DIR')
    if ($state -and (Test-Path $state)) { Copy-Item -ErrorAction SilentlyContinue (Join-Path $state '*.txt') $Directory }
    # The uploaded artifact must not carry the secrets of the installation (logs are copied raw).
    try {
        $secrets = Get-Content -Raw $SecretsFile | ConvertFrom-Json
        $values = @($secrets.DatabasePassword, $secrets.JwtSecret, $secrets.DataProtectionPassword) | Where-Object { $_ }
        foreach ($file in (Get-ChildItem -Recurse -File $Directory)) {
            $text = [IO.File]::ReadAllText($file.FullName)
            $masked = $text
            foreach ($value in $values) { $masked = $masked.Replace([string]$value, '***') }
            if ($masked -ne $text) { [IO.File]::WriteAllText($file.FullName, $masked) }
        }
    }
    catch { Write-Warning "Masking the diagnostics failed: $_" }
}

switch ($Action) {
    'InstallBaseline' { Invoke-InstallBaseline }
    'ApplyCandidate' { Invoke-ApplyCandidate }
    'Seed' { Invoke-Seed }
    'Calibrate' { Invoke-Calibrate }
    'Snapshot' { Invoke-Snapshot }
    'Compare' { Invoke-Compare }
    'Enqueue' { Invoke-Enqueue }
    'KillDuringRestore' { Invoke-KillDuringRestore }
    'WaitTerminal' { Invoke-WaitTerminal }
    'AssertSuccess' { Invoke-AssertSuccess }
    'AssertRollback' { Invoke-AssertRollback }
    'RestartServices' { Invoke-RestartServices }
    'Diagnostics' { Invoke-Diagnostics }
}
exit 0
