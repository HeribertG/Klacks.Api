<#
.SYNOPSIS
    Non-blocking nightly turn-selection eval for the Klacksy assistant (AP 0.4).

.DESCRIPTION
    Drives the turn-selection goldset ('turn-selection-v1') against a small, fixed set of
    models and produces a scorecard file plus a loud regression signal - WITHOUT ever failing
    the build. It is meant to run locally (dev machine / Windows Task Scheduler), never in CI,
    because it makes real, paid LLM provider calls and needs provider API keys.

    IMPORTANT - where the secrets live:
        Provider API keys are stored ENCRYPTED IN THE DATABASE (llm_providers.api_key), never
        in the repo and never in GitHub. This script reads nothing sensitive; it only tells the
        already-configured backend which model to replay. That is why the nightly is local-only.

    How it runs the eval:
        The eval is exposed as the [Explicit] integration test
        Klacks.IntegrationTest/Assistant/TurnSelectionGoldenSetTests. That test self-hosts the API
        (WebApplicationFactory) against the real dev DB on port 5434, replays every goldset item
        once against the model named in the TURNEVAL_MODEL_ID environment variable, and PERSISTS
        one row per run into the eval_runs table (composite_score, regression_vs_baseline, ...).
        This script invokes that test once per model, then reads the authoritative result back
        from eval_runs via psql (console scraping is deliberately avoided - it is adapter/verbosity
        fragile).

    Non-blocking contract:
        The script exits 0 when no model regressed and 2 when a regression beyond the threshold
        was detected, so the Windows Task Scheduler records a failed last-run result. A skipped or
        unavailable model, or a "0 tests executed" invocation produces a loud WARNING in the console
        and in the scorecard file but is not itself a failure (exit stays 0).

.PARAMETER Models
    Comma-separated llm_models.model_id values to evaluate. Default is the production model only,
    per owner decision 2026-08-30 (D1: "Prod-Modell + Haiku 4.5, nicht mehr"; claude-haiku-45 is
    disabled in the current dev DB, so only deepseek-v4-pro runs until it is re-enabled).
    Verify availability yourself before trusting a default: the live catalog differs per machine.
    Query:  SELECT model_id, is_default, cost_per_input_token FROM llm_models WHERE is_enabled AND NOT is_deleted;
    Swap in other presets via -Models once their provider is enabled and keyed - the pre-flight
    below skips anything not runnable.

.PARAMETER Goldset
    Goldset name (without .json). Default: turn-selection-v1.

.PARAMETER RegressionThreshold
    Composite drop (vs. the same model's previous baseline run) that triggers a loud warning.
    Positive number; a regression_vs_baseline of -0.03 with threshold 0.02 warns. Default: 0.02.

.PARAMETER OutputDir
    Directory for scorecard files. Default: <repo>/artifacts/turn-eval.

.PARAMETER RepoRoot
    Klacks.Api repo root. Default: the parent of this script's folder.

.PARAMETER DryRun
    Validate prerequisites and PRINT the exact per-model commands that would run, WITHOUT
    invoking dotnet test and WITHOUT any LLM call or cost. Use this to sanity-check wiring.

.EXAMPLE
    pwsh ./scripts/nightly-turn-eval.ps1 -DryRun

.EXAMPLE
    pwsh ./scripts/nightly-turn-eval.ps1 -Models "gpt-54,gemini-31-flash-lite,claude-sonnet-5"

.NOTES
    Prerequisites for a REAL run:
        - .NET 10 SDK on PATH.
        - PostgreSQL reachable at localhost:5434 (db 'klacks', user 'postgres', pw 'admin') - the
          shared dev/integration DB. psql.exe must be reachable (default path below or on PATH).
        - Each evaluated model must be enabled in llm_models AND its provider enabled + keyed in
          llm_providers. The script pre-flights this and skips (with a warning) any model that is not.
        - No running backend is required: the integration test self-hosts its own host.
        - Real provider calls cost money.
    This script performs NO git actions and NO deployment.

    Register as a Windows Scheduled Task (run once, elevated PowerShell; adjust the path).
    DO NOT commit any credentials; the task runs as the current interactive user so it inherits the
    same DB access the dev tools already have:

        schtasks /Create ^
          /TN "Klacks\NightlyTurnEval" ^
          /TR "powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\SourceCode\Klacks.Api\scripts\nightly-turn-eval.ps1" ^
          /SC DAILY /ST 03:30 /RL LIMITED /F

    Inspect / remove later:
        schtasks /Query /TN "Klacks\NightlyTurnEval" /V /FO LIST
        schtasks /Delete /TN "Klacks\NightlyTurnEval" /F
#>

[CmdletBinding()]
param(
    [string]$Models = "deepseek-v4-pro",
    [string]$Goldset = "turn-selection-v1",
    [double]$RegressionThreshold = 0.02,
    [string]$OutputDir,
    [string]$RepoRoot,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Constants ---------------------------------------------------------------
$ModelEnvVar          = "TURNEVAL_MODEL_ID"
$IntegrationProject   = "Klacks.IntegrationTest/Klacks.IntegrationTest.csproj"
$TestFilter           = "FullyQualifiedName~TurnSelectionGoldenSetTests"
$EvalRunsTable        = "eval_runs"
$DbHost               = "localhost"
$DbPort               = "5434"
$DbName               = "klacks"
$DbUser               = "postgres"
$DbPassword           = "admin"
$PsqlDefaultPath      = "C:\Program Files\PostgreSQL\17\bin\psql.exe"

# --- Resolve paths -----------------------------------------------------------
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepoRoot)  { $RepoRoot  = Split-Path -Parent $ScriptDir }
if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot "artifacts/turn-eval" }

$ModelList = @($Models -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })
if ($ModelList.Count -eq 0) { throw "No models provided." }

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
$anyWarning    = $false

try {
    $modeText = if ($DryRun) { "DRY-RUN (no dotnet test, no LLM calls)" } else { "LIVE" }
    Write-Line "# Turn-selection nightly eval - $Timestamp" $sw
    Write-Line "" $sw
    Write-Line "Goldset:              $Goldset" $sw
    Write-Line "Models:               $($ModelList -join ', ')" $sw
    Write-Line "Regression threshold: -$RegressionThreshold (composite vs. same-model baseline)" $sw
    Write-Line "Mode:                 $modeText" $sw
    Write-Line "DB reachable:         $dbReachable" $sw
    Write-Line "" $sw

    foreach ($model in $ModelList) {
        Write-Line "## Model: $model" $sw

        # -- Enablement pre-flight (best-effort; needs a reachable DB) --------
        if ($dbReachable) {
            $sqlEnabled = "SELECT (m.is_enabled AND p.is_enabled AND (NOT p.requires_api_key OR p.api_key IS NOT NULL)) FROM llm_models m JOIN llm_providers p ON p.provider_id = m.provider_id WHERE m.model_id = '$model' AND m.is_deleted = false AND p.is_deleted = false LIMIT 1;"
            $en = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlEnabled
            if (-not $en.Ok -or [string]::IsNullOrWhiteSpace($en.Value)) {
                Write-Line "  WARNING: model '$model' not found in llm_models - skipping." $sw "Yellow"
                $anyWarning = $true
                Write-Line "" $sw
                continue
            }
            if ($en.Value -ne "t") {
                Write-Line "  WARNING: model '$model' or its provider is disabled / missing an API key - skipping." $sw "Yellow"
                $anyWarning = $true
                Write-Line "" $sw
                continue
            }
        } elseif (-not $DryRun) {
            Write-Line "  WARNING: DB not reachable for enablement pre-flight - attempting anyway." $sw "Yellow"
            $anyWarning = $true
        }

        $cmd = "dotnet test $IntegrationProject --filter `"$TestFilter`" --configuration Release --logger `"trx;LogFileName=turneval-$model-$Timestamp.trx`" (env $ModelEnvVar=$model)"

        if ($DryRun) {
            Write-Line "  [dry-run] would run: $cmd" $sw
            Write-Line "  [dry-run] would then read latest $EvalRunsTable row for goldset='$Goldset', model='$model'." $sw
            Write-Line "" $sw
            continue
        }

        # -- Baseline BEFORE the run (to detect that a new row was actually written) --
        $sqlCountBefore = "SELECT count(*) FROM $EvalRunsTable WHERE goldset = '$Goldset' AND model = '$model' AND is_deleted = false;"
        $before = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlCountBefore
        $countBefore = 0
        [int]::TryParse($before.Value, [ref]$countBefore) | Out-Null

        # -- Run the [Explicit] integration test for this model --------------
        Write-Host "  Running eval for '$model' ..." -ForegroundColor Cyan
        $prevModel = $env:TURNEVAL_MODEL_ID
        $env:TURNEVAL_MODEL_ID = $model
        $testOutput = ""
        try {
            $testOutput = & dotnet test $IntegrationProject `
                --filter $TestFilter `
                --configuration Release `
                --logger "trx;LogFileName=turneval-$model-$Timestamp.trx" 2>&1 | Out-String
        } catch {
            $testOutput = "$testOutput`n$($_.Exception.Message)"
        } finally {
            $env:TURNEVAL_MODEL_ID = $prevModel
        }

        # -- Guard: did the [Explicit] test actually execute? ----------------
        # A name filter that fails to select the Explicit test runs NOTHING and still exits 0.
        # Detect it two ways: (a) the vstest summary reporting zero total, (b) no new eval_runs row.
        $ranZero = $testOutput -match "Passed!\s*-\s*Failed:\s*0,\s*Passed:\s*0" `
            -or $testOutput -match "No test (matches|is available|source files)" `
            -or $testOutput -match "Total tests:\s*0" `
            -or ($testOutput -match "Passed:\s*0" -and $testOutput -notmatch "Passed:\s*[1-9]")

        $after = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlCountBefore
        $countAfter = 0
        [int]::TryParse($after.Value, [ref]$countAfter) | Out-Null
        $newRowWritten = ($countAfter -gt $countBefore)

        if ($ranZero -or -not $newRowWritten) {
            Write-Line "  WARNING: no eval executed for '$model' (0 tests ran or no eval_runs row written)." $sw "Red"
            Write-Line "           The --filter may not have selected the [Explicit] test, or the run errored before persisting." $sw "Red"
            $anyWarning = $true
            Write-Line "" $sw
            continue
        }

        # -- Read the authoritative scorecard back from eval_runs ------------
        $sqlLatest = "SELECT composite_score, coalesce(regression_vs_baseline::text, 'n/a'), items_total, items_passed, provider FROM $EvalRunsTable WHERE goldset = '$Goldset' AND model = '$model' AND is_deleted = false ORDER BY create_time DESC LIMIT 1;"
        $row = Invoke-PsqlScalar -PsqlPath $psql -Sql $sqlLatest
        if (-not $row.Ok -or [string]::IsNullOrWhiteSpace($row.Value)) {
            Write-Line "  WARNING: could not read the persisted eval_runs row for '$model'." $sw "Yellow"
            $anyWarning = $true
            Write-Line "" $sw
            continue
        }

        $cols = $row.Value.Split("|")
        $composite  = $cols[0].Trim()
        $regression = $cols[1].Trim()
        $itemsTotal = if ($cols.Count -gt 2) { $cols[2].Trim() } else { "?" }
        $itemsPass  = if ($cols.Count -gt 3) { $cols[3].Trim() } else { "?" }
        $provider   = if ($cols.Count -gt 4) { $cols[4].Trim() } else { "?" }

        Write-Line "  provider:   $provider" $sw
        Write-Line "  composite:  $composite" $sw
        Write-Line "  regression: $regression" $sw
        Write-Line "  items:      passed=$itemsPass / total=$itemsTotal" $sw

        $regValue = 0.0
        if ([double]::TryParse($regression, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$regValue)) {
            if ($regValue -le (-1 * $RegressionThreshold)) {
                Write-Line "  >>> REGRESSION: composite dropped by $regValue vs baseline (threshold -$RegressionThreshold) <<<" $sw "Red"
                $anyRegression = $true
            }
        }
        Write-Line "" $sw
    }

    Write-Line "---" $sw
    if ($DryRun) {
        Write-Line "DRY-RUN complete. No tests were executed and no LLM calls were made." $sw "Green"
    } elseif ($anyRegression) {
        Write-Line "RESULT: REGRESSION DETECTED - review the models flagged above." $sw "Red"
    } elseif ($anyWarning) {
        Write-Line "RESULT: completed WITH WARNINGS (skipped models / missing rows) - review above." $sw "Yellow"
    } else {
        Write-Line "RESULT: OK - no regression beyond threshold." $sw "Green"
    }
    Write-Line "Scorecard: $ScorecardOut" $sw
}
finally {
    $sw.Flush()
    $sw.Close()
}

Write-Host ""
Write-Host "Scorecard written to: $ScorecardOut" -ForegroundColor Cyan

# Regression is signalled loudly above AND via a non-zero exit code, so the scheduled task
# records a failed run when the composite drops beyond the threshold.
if ($anyRegression) { exit 2 }
exit 0
