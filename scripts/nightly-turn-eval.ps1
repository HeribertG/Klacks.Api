<#
.SYNOPSIS
    Nightly turn-selection eval for the Klacksy assistant (AP 0.4).

.DESCRIPTION
    Drives a turn-eval goldset against a small, fixed set of models, produces a scorecard file and
    signals both regressions and apparatus failures through the exit code. It is meant to run
    locally (dev machine / Windows Task Scheduler), never in CI, because it makes real, paid LLM
    provider calls and needs provider API keys.

    IMPORTANT - where the secrets live:
        Provider API keys are stored ENCRYPTED IN THE DATABASE (llm_providers.api_key), never
        in the repo and never in GitHub. This script reads nothing sensitive; it only tells the
        already-configured backend which model to replay. That is why the nightly is local-only.

    How it runs the eval:
        The eval is exposed as the [Explicit] integration test
        Klacks.IntegrationTest/Assistant/TurnSelectionGoldenSetTests. That test self-hosts the API
        (WebApplicationFactory) against the real dev DB on port 5434, replays every goldset item
        once against the model named in TURNEVAL_MODEL_ID, and PERSISTS one row per run into the
        eval_runs table (composite_score, regression_vs_baseline, scorer_version, is_partial, ...).
        This script invokes that test once per model, then reads the authoritative result back from
        eval_runs via psql (console scraping is deliberately avoided - it is adapter/verbosity
        fragile).

        The test is selected by its FULL name, not by a substring. Both forms were verified to
        execute an [Explicit] fixture on this machine (2026-09-02, .NET 10 / NUnit adapter: an
        explicit test DOES run when a --filter selects it), so the exact name is used purely for
        precision - it can never drag in a sibling fixture and spend money on it.

    Environment contract (this is what the test reads):
        TURNEVAL_MODEL_ID   - model to replay; set per model by this script.
        TURNEVAL_GOLDSET    - goldset name; exported from -Goldset. Previously never exported, so
                              -Goldset silently had no effect on the run while the result was still
                              looked up under that name - a run of one goldset could be reported as
                              another. Now they cannot diverge.
        TURNEVAL_MAX_ITEMS  - item cap; exported from -MaxItems / -Profile.
        TURNEVAL_MIN_PASS_RATE - optional absolute gate; not set here, the test's own baseline
                              ratchet plus its absolute floor decide.

    Exit contract (a scheduled task must be able to see a broken apparatus):
        0 - every model produced exactly one new eval_runs row and no composite regressed.
        2 - at least one model regressed beyond -RegressionThreshold.
        3 - APPARATUS FAILURE: a model was skipped, dotnet test returned non-zero, no test ran, or
            the run did not add exactly one eval_runs row. A run that measured nothing is a failure,
            not a warning - the previous version exited 0 here, which is why five months of "green"
            nightlies contained no measurement at all.
        Precedence: 3 beats 2, because a broken apparatus makes the numbers untrustworthy.

.PARAMETER Models
    Comma-separated llm_models.model_id values to evaluate. Default is the production model only,
    per owner decision 2026-08-30 (D1: "Prod-Modell + Haiku 4.5, nicht mehr"; claude-haiku-45 is
    disabled in the current dev DB, so only deepseek-v4-pro runs until it is re-enabled).
    Verify availability yourself before trusting a default: the live catalog differs per machine.
    Query:  SELECT model_id, is_default, cost_per_input_token FROM llm_models WHERE is_enabled AND NOT is_deleted;

.PARAMETER Goldset
    Goldset name (without .json). Default: turn-selection-v1. Exported as TURNEVAL_GOLDSET.

.PARAMETER Profile
    daily  - caps the run at the first 70 goldset items (the curated head of turn-selection-v1;
             RunAsync takes them with Take, so positions 0-69). Roughly a fifth of the cost.
             NOTE: a capped run is a PARTIAL run. Partial runs are never used as a baseline, so the
             daily series does not move the ratchet - its gate is the absolute floor in
             TurnEvalPassRateGate. Only the weekly full run advances the baseline. That is the
             intended trade: cheap daily smoke detection, weekly ratchet.
    weekly - full goldset, no cap. This is the run that sets the baseline.
    Explicitly passing -MaxItems overrides the profile.

.PARAMETER MaxItems
    Item cap; 0 means "use the profile". Exported as TURNEVAL_MAX_ITEMS.

.PARAMETER RegressionThreshold
    Composite drop (vs. the best comparable baseline run) that triggers exit code 2. Positive
    number; a regression_vs_baseline of -0.03 with threshold 0.02 fires. Default: 0.02.

.PARAMETER OutputDir
    Directory for scorecard and test-log files. Default: <repo>/artifacts/turn-eval.

.PARAMETER RepoRoot
    Klacks.Api repo root. Default: the parent of this script's folder.

.PARAMETER DryRun
    Validate prerequisites and PRINT the exact per-model commands that would run, WITHOUT
    invoking dotnet test and WITHOUT any LLM call or cost.

.EXAMPLE
    pwsh ./scripts/nightly-turn-eval.ps1 -DryRun

.EXAMPLE
    pwsh ./scripts/nightly-turn-eval.ps1 -Profile weekly

.NOTES
    Prerequisites for a REAL run:
        - .NET 10 SDK on PATH.
        - PostgreSQL reachable at localhost:5434 (db 'klacks', user 'postgres', pw 'admin') - the
          shared dev/integration DB. psql.exe must be reachable (default path below or on PATH).
        - Each evaluated model must be enabled in llm_models AND its provider enabled + keyed in
          llm_providers. The script pre-flights this and FAILS (exit 3) on anything not runnable.
        - No running backend is required: the integration test self-hosts its own host.
        - Real provider calls cost money.
    This script performs NO git actions and NO deployment.

    Recommended scheduled tasks (register manually; this script does not touch the scheduler).
    Use Register-ScheduledTask, not a bare `schtasks /TR "powershell.exe ... -File ..."`: schtasks'
    /TR runs powershell.exe as the executable with no working-directory switch, so the task starts
    in %SystemRoot%\System32, not the repo - which is exactly what turned an MSB1009 (IT project
    resolved as a path relative to the CWD) into a silent apparatus failure on 2026-09-02/03. The
    script now resolves every path from $PSScriptRoot and no longer needs a specific CWD to find
    the IT project, but WorkingDirectory is still set below as defense in depth:

        $action = New-ScheduledTaskAction -Execute "powershell.exe" `
          -Argument '-NoProfile -ExecutionPolicy Bypass -File C:\SourceCode\Klacks.Api\scripts\nightly-turn-eval.ps1 -Profile daily' `
          -WorkingDirectory 'C:\SourceCode'
        $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At 03:30
        $principal = New-ScheduledTaskPrincipal -UserId 'hgasp' -LogonType Interactive -RunLevel Limited
        $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 4) -StartWhenAvailable
        Register-ScheduledTask -TaskPath '\Klacks\' -TaskName 'NightlyTurnEval-Daily' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force

        # NightlyTurnEval-Weekly: same shape, -Profile weekly, -DaysOfWeek Saturday, -At 02:00.

    Three scheduling constraints, all of them load-bearing:
      - The weekly run must NOT collide with Klacks-GoldenSet-Nightly-Full (Sunday 00:00), hence
        Saturday.
      - The daily must NOT fire on Saturday (weekdays only, not /SC DAILY): the weekly full run
        over 334 items takes roughly 95 min at the measured ~17 s/item, so a 03:30 daily would
        start while it is still running, both would write eval_runs, and the "exactly one new row"
        guard would report an apparatus failure for a run that was in fact fine.
      - Neither should run while a backend or the E2E suite occupies the dev DB on 5434.
    Inspect / remove:
        schtasks /Query /TN "Klacks\NightlyTurnEval-Daily" /V /FO LIST
        schtasks /Delete /TN "Klacks\NightlyTurnEval-Daily" /F
#>

[CmdletBinding()]
param(
    [string]$Models = "deepseek-v4-pro",
    [string]$Goldset = "turn-selection-v1",
    [ValidateSet("daily", "weekly")]
    [string]$Profile = "daily",
    [int]$MaxItems = 0,
    [double]$RegressionThreshold = 0.02,
    [string]$OutputDir,
    [string]$RepoRoot,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Constants ---------------------------------------------------------------
$ModelEnvVar          = "TURNEVAL_MODEL_ID"
$GoldsetEnvVar        = "TURNEVAL_GOLDSET"
$MaxItemsEnvVar       = "TURNEVAL_MAX_ITEMS"
$IntegrationProjectRelative = "Klacks.IntegrationTest/Klacks.IntegrationTest.csproj"
$TestFullName         = "Klacks.IntegrationTest.Assistant.TurnSelectionGoldenSetTests.TurnSelectionGoldset_ReplaysAllItemsAndReportsScorecard"
$TestFilter           = "FullyQualifiedName=$TestFullName"
$EvalRunsTable        = "eval_runs"
$DailyMaxItems        = 70
$DbHost               = "localhost"
$DbPort               = "5434"
$DbName               = "klacks"
$DbUser               = "postgres"
$DbPassword           = "admin"
$PsqlDefaultPath      = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
$ExitOk               = 0
$ExitRegression       = 2
$ExitApparatusFailure = 3

# --- Resolve paths and run scope ---------------------------------------------
# The scheduled task's action has no WorkingDirectory (verified 2026-09-03 on
# NightlyTurnEval-Weekly, which failed with MSB1009 for exactly this reason), so every path below
# is anchored to $PSScriptRoot, never to the process CWD. $SolutionRoot is the super-repo root
# (C:\SourceCode) - the parent of Klacks.Api - because $IntegrationProjectRelative crosses into
# the sibling Klacks.IntegrationTest repo.
$SolutionRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $SolutionRoot

if (-not $RepoRoot)  { $RepoRoot  = Split-Path -Parent $PSScriptRoot }
if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot "artifacts/turn-eval" }
$IntegrationProject = Join-Path $SolutionRoot $IntegrationProjectRelative

$ModelList = @($Models -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })
if ($ModelList.Count -eq 0) { throw "No models provided." }

$EffectiveMaxItems = if ($MaxItems -gt 0) { $MaxItems } elseif ($Profile -eq "daily") { $DailyMaxItems } else { 0 }
$ScopeText = if ($EffectiveMaxItems -gt 0) { "first $EffectiveMaxItems items (PARTIAL run - never a baseline)" } else { "full goldset" }

$Timestamp    = Get-Date -Format "yyyyMMdd-HHmmss"
$ScorecardOut = Join-Path $OutputDir "turn-eval-$Timestamp.md"

# --- Small helpers -----------------------------------------------------------
function Resolve-Psql {
    if (Test-Path $PsqlDefaultPath) { return $PsqlDefaultPath }
    $cmd = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $cmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Invoke-PsqlScalar {
    param([string]$PsqlPath, [string]$Sql)
    $prev = $env:PGPASSWORD
    $env:PGPASSWORD = $DbPassword
    try {
        $out = & $PsqlPath -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -A -c $Sql 2>&1
        return @{ Ok = ($LASTEXITCODE -eq 0); Value = ($out | Out-String).Trim() }
    } finally {
        $env:PGPASSWORD = $prev
    }
}

function Write-Line {
    param([string]$Text, [System.IO.StreamWriter]$Writer, [string]$Color)
    if ($Color) { Write-Host $Text -ForegroundColor $Color } else { Write-Host $Text }
    $Writer.WriteLine($Text)
}

# --- Preflight ---------------------------------------------------------------
$psql = Resolve-Psql
$dbReachable = $false
if ($psql) {
    $ping = Invoke-PsqlScalar -PsqlPath $psql -Sql "SELECT 1"
    $dbReachable = $ping.Ok -and ($ping.Value -eq "1")
}

if (-not $DryRun) {
    if (-not $psql)        { throw "psql not found (looked at '$PsqlDefaultPath' and PATH). Cannot read eval_runs." }
    if (-not $dbReachable) { throw "PostgreSQL not reachable at ${DbHost}:${DbPort}/${DbName}. Start the dev DB and retry." }
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) { throw ".NET SDK (dotnet) not found on PATH." }
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$sw = [System.IO.StreamWriter]::new($ScorecardOut, $false, [System.Text.UTF8Encoding]::new($false))

$anyRegression = $false
$anyFailure    = $false

try {
    $modeText = if ($DryRun) { "DRY-RUN (no dotnet test, no LLM calls)" } else { "LIVE" }
    Write-Line "# Turn-selection nightly eval - $Timestamp" $sw
    Write-Line "" $sw
    Write-Line "Goldset:              $Goldset" $sw
    Write-Line "Profile:              $Profile -> $ScopeText" $sw
    Write-Line "Models:               $($ModelList -join ', ')" $sw
    Write-Line "Regression threshold: -$RegressionThreshold (composite vs. best comparable baseline)" $sw
    Write-Line "Mode:                 $modeText" $sw
    Write-Line "DB reachable:         $dbReachable" $sw
    Write-Line "" $sw

    foreach ($model in $ModelList) {
        Write-Line "## Model: $model" $sw

        # -- Enablement pre-flight -------------------------------------------
        if ($dbReachable) {
            $sqlEnabled = "SELECT (m.is_enabled AND p.is_enabled AND (NOT p.requires_api_key OR p.api_key IS NOT NULL)) FROM llm_models m JOIN llm_providers p ON p.provider_id = m.provider_id WHERE m.model_id = '$model' AND m.is_deleted = false AND p.is_deleted = false LIMIT 1;"
            $en = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlEnabled
            if (-not $en.Ok -or [string]::IsNullOrWhiteSpace($en.Value)) {
                Write-Line "  FAILURE: model '$model' not found in llm_models - skipped, nothing was measured." $sw "Red"
                $anyFailure = $true
                Write-Line "" $sw
                continue
            }
            if ($en.Value -ne "t") {
                Write-Line "  FAILURE: model '$model' or its provider is disabled / missing an API key - skipped, nothing was measured." $sw "Red"
                $anyFailure = $true
                Write-Line "" $sw
                continue
            }
        } elseif (-not $DryRun) {
            Write-Line "  FAILURE: DB not reachable for the enablement pre-flight." $sw "Red"
            $anyFailure = $true
            Write-Line "" $sw
            continue
        }

        $envText = "$ModelEnvVar=$model $GoldsetEnvVar=$Goldset $MaxItemsEnvVar=$EffectiveMaxItems"
        $cmd = "dotnet test $IntegrationProject --filter `"$TestFilter`" --configuration Release (env $envText)"

        if ($DryRun) {
            Write-Line "  [dry-run] would run: $cmd" $sw
            Write-Line "  [dry-run] would then require exactly one new $EvalRunsTable row for goldset='$Goldset', model='$model'." $sw
            Write-Line "" $sw
            continue
        }

        # -- Row count BEFORE the run ----------------------------------------
        $sqlCount = "SELECT count(*) FROM $EvalRunsTable WHERE goldset = '$Goldset' AND model = '$model' AND is_deleted = false;"
        $before = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlCount
        if (-not $before.Ok) {
            Write-Line "  FAILURE: could not read the $EvalRunsTable row count before the run." $sw "Red"
            $anyFailure = $true
            Write-Line "" $sw
            continue
        }
        $countBefore = 0
        [int]::TryParse($before.Value, [ref]$countBefore) | Out-Null

        # -- Run the [Explicit] integration test for this model --------------
        Write-Host "  Running eval for '$model' ($ScopeText) ..." -ForegroundColor Cyan
        $logPath = Join-Path $OutputDir "turneval-$model-$Timestamp.log"
        $prevModel    = $env:TURNEVAL_MODEL_ID
        $prevGoldset  = $env:TURNEVAL_GOLDSET
        $prevMaxItems = $env:TURNEVAL_MAX_ITEMS
        $env:TURNEVAL_MODEL_ID  = $model
        $env:TURNEVAL_GOLDSET   = $Goldset
        $env:TURNEVAL_MAX_ITEMS = "$EffectiveMaxItems"
        $testExitCode = -1
        try {
            $testOutput = & dotnet test $IntegrationProject `
                --filter $TestFilter `
                --configuration Release `
                --logger "trx;LogFileName=turneval-$model-$Timestamp.trx" 2>&1 | Out-String
            $testExitCode = $LASTEXITCODE
        } catch {
            $testOutput = "$($_.Exception.Message)"
            $testExitCode = -1
        } finally {
            $env:TURNEVAL_MODEL_ID  = $prevModel
            $env:TURNEVAL_GOLDSET   = $prevGoldset
            $env:TURNEVAL_MAX_ITEMS = $prevMaxItems
        }

        # The full console output is the only place a build error or a provider failure is visible,
        # and losing it is what made the 01.09. empty nightly undiagnosable. Always keep it.
        Set-Content -Path $logPath -Value $testOutput -Encoding UTF8

        # -- Guard: exactly one new row. Locale-independent, unlike scraping the vstest summary
        #    (a German SDK prints "erfolgreich:", so every English "Passed: 0" heuristic is blind).
        $after = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlCount
        $countAfter = 0
        [int]::TryParse($after.Value, [ref]$countAfter) | Out-Null
        $rowsAdded = $countAfter - $countBefore

        Write-Line "  dotnet test exit code: $testExitCode" $sw
        Write-Line "  eval_runs rows:        $countBefore -> $countAfter (added $rowsAdded, expected 1)" $sw
        Write-Line "  test log:              $logPath" $sw

        if ($testExitCode -ne 0) {
            Write-Line "  FAILURE: dotnet test returned $testExitCode - the eval did not complete cleanly. See the log above." $sw "Red"
            $anyFailure = $true
        }

        if ($rowsAdded -ne 1) {
            Write-Line "  FAILURE: the run added $rowsAdded rows to $EvalRunsTable, expected exactly 1." $sw "Red"
            Write-Line "           Either no test executed, or the run errored before persisting, or the goldset/model" $sw "Red"
            Write-Line "           the test used differs from the one queried here. NOTHING WAS MEASURED." $sw "Red"
            $anyFailure = $true
            Write-Line "" $sw
            continue
        }

        # -- Read the authoritative scorecard back from eval_runs ------------
        $sqlLatest = "SELECT composite_score, coalesce(regression_vs_baseline::text, 'n/a'), items_total, items_passed, provider, scorer_version, is_partial FROM $EvalRunsTable WHERE goldset = '$Goldset' AND model = '$model' AND is_deleted = false ORDER BY create_time DESC LIMIT 1;"
        $row = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlLatest
        if (-not $row.Ok -or [string]::IsNullOrWhiteSpace($row.Value)) {
            Write-Line "  FAILURE: could not read the persisted $EvalRunsTable row for '$model'." $sw "Red"
            $anyFailure = $true
            Write-Line "" $sw
            continue
        }

        $cols = $row.Value.Split("|")
        $composite     = $cols[0].Trim()
        $regression    = $cols[1].Trim()
        $itemsTotal    = if ($cols.Count -gt 2) { $cols[2].Trim() } else { "?" }
        $itemsPass     = if ($cols.Count -gt 3) { $cols[3].Trim() } else { "?" }
        $provider      = if ($cols.Count -gt 4) { $cols[4].Trim() } else { "?" }
        $scorerVersion = if ($cols.Count -gt 5) { $cols[5].Trim() } else { "?" }
        $isPartial     = if ($cols.Count -gt 6) { $cols[6].Trim() } else { "?" }

        Write-Line "  provider:       $provider" $sw
        Write-Line "  scorer version: $scorerVersion (composites are comparable only within one version)" $sw
        Write-Line "  partial run:    $isPartial" $sw
        Write-Line "  composite:      $composite" $sw
        Write-Line "  regression:     $regression" $sw
        Write-Line "  items:          passed=$itemsPass / total=$itemsTotal" $sw

        $regValue = 0.0
        if ([double]::TryParse($regression, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$regValue)) {
            if ($regValue -le (-1 * $RegressionThreshold)) {
                Write-Line "  >>> REGRESSION: composite dropped by $regValue vs baseline (threshold -$RegressionThreshold) <<<" $sw "Red"
                $anyRegression = $true
            }
        } elseif ($isPartial -eq "t") {
            Write-Line "  (no regression figure: partial runs have no comparable baseline by design)" $sw
        } else {
            Write-Line "  (no regression figure: no comparable baseline of the same size and scorer version yet)" $sw
        }
        Write-Line "" $sw
    }

    Write-Line "---" $sw
    if ($DryRun) {
        Write-Line "DRY-RUN complete. No tests were executed and no LLM calls were made." $sw "Green"
    } elseif ($anyFailure) {
        Write-Line "RESULT: APPARATUS FAILURE - at least one model measured nothing. The numbers above are incomplete." $sw "Red"
    } elseif ($anyRegression) {
        Write-Line "RESULT: REGRESSION DETECTED - review the models flagged above." $sw "Red"
    } else {
        Write-Line "RESULT: OK - every model produced exactly one run, none regressed beyond the threshold." $sw "Green"
    }
    Write-Line "Scorecard: $ScorecardOut" $sw
}
finally {
    $sw.Flush()
    $sw.Close()
}

Write-Host ""
Write-Host "Scorecard written to: $ScorecardOut" -ForegroundColor Cyan

if ($DryRun)       { exit $ExitOk }
if ($anyFailure)   { exit $ExitApparatusFailure }
if ($anyRegression){ exit $ExitRegression }
exit $ExitOk
