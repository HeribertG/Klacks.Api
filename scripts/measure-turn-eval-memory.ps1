<#
.SYNOPSIS
    Samples memory of the turn-eval test host (and related processes) from outside and writes a CSV.

.DESCRIPTION
    Start this sampler FIRST in one terminal, then start scripts\nightly-turn-eval.ps1 in a second one.
    One CSV row per sample and process: working set, private (commit) memory, peak working set, plus
    system-wide available memory and committed memory. With -LogPath, phase markers found in the eval
    test log are written to <OutputPath>.markers.csv so the memory curve can be correlated with phases.
    Marker timestamps are the time the sampler noticed the line (the log has no timestamps), so their
    precision is the log check interval.

    Stops on Ctrl+C, after -DurationMinutes, or (default) when the test host appeared and vanished again.

.PARAMETER IntervalSeconds
    Sampling interval. Default: 1 second.

.PARAMETER OutputPath
    CSV file. Relative paths resolve against the Klacks.Api repo root.
    Default: artifacts\turn-eval\memory-<timestamp>.csv.

.PARAMETER ProcessNames
    Process names (wildcards allowed, without .exe) to sample.

.PARAMETER DurationMinutes
    Optional fixed run time. Without it the sampler waits for the test host to appear, then stops when
    it is gone.

.PARAMETER LogPath
    Optional test log (turneval-*.log). Wildcards allowed: the newest matching file written after the
    sampler started is followed, so the timestamped name need not be known in advance.

.EXAMPLE
    pwsh .\scripts\measure-turn-eval-memory.ps1 -LogPath 'artifacts\turn-eval\turneval-*.log'
#>

[CmdletBinding()]
param(
    [double]$IntervalSeconds = 0,
    [string]$OutputPath,
    [string[]]$ProcessNames,
    [double]$DurationMinutes = 0,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Constants ---------------------------------------------------------------
$DefaultIntervalSeconds   = 1
$DefaultProcessNames      = @("testhost*", "dotnet", "MSBuild", "VBCSCompiler", "Klacks.Api", "node", "postgres")
$TesthostNamePattern      = "testhost*"
$DefaultOutputRelative    = "artifacts\turn-eval"
$OutputFilePrefix         = "memory-"
$OutputFileExtension      = ".csv"
$MarkersFileSuffix        = ".markers.csv"
$TimestampFileFormat      = "yyyyMMdd-HHmmss"
$TimestampRowFormat       = "yyyy-MM-ddTHH:mm:ss.fffzzz"
$TesthostWaitTimeoutSeconds = 900
$TesthostGoneSamples      = 2
$BytesPerMb               = 1MB
$KbPerMb                  = 1024
$RoundDigits              = 1
$LogCheckEverySamples     = 3
$LogChunkBytes            = 2MB
$LineFeedByte             = 10
$MarkerDetailMaxChars     = 200
$CounterAvailablePath     = "\Memory\Available MBytes"
$CounterCommittedPath     = "\Memory\Committed Bytes"
$CounterAvailableSuffix   = "*\available mbytes"
$CounterCommittedSuffix   = "*\committed bytes"
$CsvHeader                = "Timestamp,Name,Id,WorkingSetMB,PrivateMB,PeakWorkingSetMB,SysAvailableMB,SysCommittedMB"
$MarkersCsvHeader         = "Timestamp,Marker,ItemCount,Detail"
$MarkerDefinitions = @(
    @{ Name = "BuildOutput";        Pattern = "Klacks.Api ->";            EveryNth = 0 },
    @{ Name = "OnnxEmbeddingReady"; Pattern = "ONNX warm-up: embedding";  EveryNth = 0 },
    @{ Name = "OnnxRerankerReady";  Pattern = "ONNX warm-up: reranker";   EveryNth = 0 },
    @{ Name = "FirstRetrieval";     Pattern = "[retrieval]";              EveryNth = 0 },
    @{ Name = "TurnReplayItem";     Pattern = "TurnReplay item";          EveryNth = 10 }
)

# --- Resolve parameters ------------------------------------------------------
$RepoRoot = Split-Path -Parent $PSScriptRoot
if ($IntervalSeconds -le 0) { $IntervalSeconds = $DefaultIntervalSeconds }
if (-not $ProcessNames -or $ProcessNames.Count -eq 0) { $ProcessNames = $DefaultProcessNames }
if (-not $OutputPath) {
    $OutputPath = Join-Path $RepoRoot (Join-Path $DefaultOutputRelative ($OutputFilePrefix + (Get-Date -Format $TimestampFileFormat) + $OutputFileExtension))
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot $OutputPath
}
$MarkersPath = $OutputPath + $MarkersFileSuffix
if ($LogPath -and -not [System.IO.Path]::IsPathRooted($LogPath)) { $LogPath = Join-Path $RepoRoot $LogPath }
$LogHasWildcard = $LogPath -and [System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($LogPath)
$StartedAt = Get-Date

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Open-AppendWriter {
    param([string]$Path, [string]$Header)
    $stream = [System.IO.FileStream]::new($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
    $writer = [System.IO.StreamWriter]::new($stream, $Utf8NoBom)
    $writer.AutoFlush = $true
    $writer.WriteLine($Header)
    return $writer
}

function ConvertTo-CsvField {
    param([string]$Text)
    return '"' + $Text.Replace('"', '""') + '"'
}

function ConvertTo-Mb {
    param([double]$Bytes)
    return [math]::Round($Bytes / $BytesPerMb, $RoundDigits).ToString([System.Globalization.CultureInfo]::InvariantCulture)
}

# --- System-wide memory ------------------------------------------------------
$useCounters = $true
try {
    Get-Counter -Counter @($CounterAvailablePath, $CounterCommittedPath) -ErrorAction Stop | Out-Null
} catch {
    $useCounters = $false
    Write-Host "Performance counters unavailable or localized - falling back to CIM Win32_OperatingSystem." -ForegroundColor Yellow
}

function Get-SystemMemory {
    $available = [double]::NaN
    $committed = [double]::NaN
    if ($useCounters) {
        try {
            $samples = (Get-Counter -Counter @($CounterAvailablePath, $CounterCommittedPath) -ErrorAction Stop).CounterSamples
            foreach ($s in $samples) {
                if ($s.Path -like $CounterAvailableSuffix) { $available = [double]$s.CookedValue }
                elseif ($s.Path -like $CounterCommittedSuffix) { $committed = [double]$s.CookedValue / $BytesPerMb }
            }
        } catch { }
    }
    if ([double]::IsNaN($available) -or [double]::IsNaN($committed)) {
        $os = Get-CimInstance -ClassName Win32_OperatingSystem
        $available = [double]$os.FreePhysicalMemory / $KbPerMb
        $committed = ([double]$os.TotalVirtualMemorySize - [double]$os.FreeVirtualMemory) / $KbPerMb
    }
    return @{
        Available = [math]::Round($available, $RoundDigits).ToString([System.Globalization.CultureInfo]::InvariantCulture)
        Committed = [math]::Round($committed, $RoundDigits).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
}

# --- Log marker scanning -----------------------------------------------------
$script:LogFile = $null
$script:LogPosition = 0L
$script:MarkerCounts = @{}

function Resolve-LogFile {
    if ($LogHasWildcard) {
        $found = Get-ChildItem -Path $LogPath -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -ge $StartedAt } |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
        return $null
    }
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) { return $LogPath }
    return $null
}

function Read-LogMarkers {
    param([System.IO.StreamWriter]$MarkerWriter)
    $file = Resolve-LogFile
    if (-not $file) { return }
    if ($file -ne $script:LogFile) {
        $script:LogFile = $file
        $script:LogPosition = 0L
        $script:MarkerCounts = @{}
    }
    $stream = $null
    try {
        $stream = [System.IO.FileStream]::new($file, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $available = $stream.Length - $script:LogPosition
        if ($available -le 0) { return }
        $count = [int][math]::Min($available, $LogChunkBytes)
        $buffer = New-Object byte[] $count
        [void]$stream.Seek($script:LogPosition, [System.IO.SeekOrigin]::Begin)
        $read = $stream.Read($buffer, 0, $count)
        $lastLineFeed = [Array]::LastIndexOf($buffer, [byte]$LineFeedByte, $read - 1)
        if ($lastLineFeed -lt 0) { return }
        $script:LogPosition += $lastLineFeed + 1
        $text = $Utf8NoBom.GetString($buffer, 0, $lastLineFeed + 1)
    } catch {
        return
    } finally {
        if ($stream) { $stream.Dispose() }
    }
    $stamp = [DateTimeOffset]::Now.ToString($TimestampRowFormat)
    foreach ($line in $text.Split("`n")) {
        foreach ($marker in $MarkerDefinitions) {
            if (-not $line.Contains($marker.Pattern)) { continue }
            $n = 1 + $(if ($script:MarkerCounts.ContainsKey($marker.Name)) { $script:MarkerCounts[$marker.Name] } else { 0 })
            $script:MarkerCounts[$marker.Name] = $n
            $emit = ($n -eq 1) -or ($marker.EveryNth -gt 0 -and ($n % $marker.EveryNth) -eq 0)
            if ($emit) {
                $detail = $line.Trim()
                if ($detail.Length -gt $MarkerDetailMaxChars) { $detail = $detail.Substring(0, $MarkerDetailMaxChars) }
                $MarkerWriter.WriteLine("$stamp,$($marker.Name),$n,$(ConvertTo-CsvField $detail)")
            }
            break
        }
    }
}

# --- Main loop ---------------------------------------------------------------
$writer = Open-AppendWriter -Path $OutputPath -Header $CsvHeader
$markerWriter = $null
if ($LogPath) { $markerWriter = Open-AppendWriter -Path $MarkersPath -Header $MarkersCsvHeader }

$stats = @{}
$testhostFirst = $null
$testhostLast = $null
$testhostPeak = $null
$testhostPeakAt = $null
$testhostSeen = $false
$testhostGone = 0
$sampleIndex = 0
$stopReason = "Ctrl+C"
$fixedEnd = if ($DurationMinutes -gt 0) { $StartedAt.AddMinutes($DurationMinutes) } else { $null }
$waitDeadline = $StartedAt.AddSeconds($TesthostWaitTimeoutSeconds)
$watch = [System.Diagnostics.Stopwatch]::new()

Write-Host "Sampling every ${IntervalSeconds}s -> $OutputPath" -ForegroundColor Cyan
if ($LogPath) { Write-Host "Markers from '$LogPath' -> $MarkersPath" -ForegroundColor Cyan }
if ($fixedEnd) { Write-Host "Fixed duration: $DurationMinutes min." -ForegroundColor Cyan }
else { Write-Host "Waiting for the test host ($TesthostNamePattern); stops when it is gone. Ctrl+C to stop earlier." -ForegroundColor Cyan }

try {
    while ($true) {
        $watch.Restart()
        $now = Get-Date
        $stamp = [DateTimeOffset]::new($now).ToString($TimestampRowFormat)
        $sys = Get-SystemMemory
        $testhostCommitSum = 0.0
        $testhostCount = 0

        $processes = @(Get-Process -Name $ProcessNames -ErrorAction SilentlyContinue)
        foreach ($p in $processes) {
            try {
                $name = $p.ProcessName
                $procId = $p.Id
                $ws = [double]$p.WorkingSet64
                $commit = [double]$p.PrivateMemorySize64
                $peak = [double]$p.PeakWorkingSet64
            } catch {
                continue
            } finally {
                $p.Dispose()
            }
            $writer.WriteLine("$stamp,$name,$procId,$(ConvertTo-Mb $ws),$(ConvertTo-Mb $commit),$(ConvertTo-Mb $peak),$($sys.Available),$($sys.Committed)")

            if (-not $stats.ContainsKey($name)) {
                $stats[$name] = @{ MaxWs = 0.0; MaxWsAt = $stamp; MaxCommit = 0.0; MaxCommitAt = $stamp }
            }
            $entry = $stats[$name]
            if ($ws -gt $entry.MaxWs) { $entry.MaxWs = $ws; $entry.MaxWsAt = $stamp }
            if ($commit -gt $entry.MaxCommit) { $entry.MaxCommit = $commit; $entry.MaxCommitAt = $stamp }

            if ($name -like $TesthostNamePattern) {
                $testhostCount++
                $testhostCommitSum += $commit
            }
        }

        if ($testhostCount -gt 0) {
            $testhostSeen = $true
            $testhostGone = 0
            if ($null -eq $testhostFirst) { $testhostFirst = $testhostCommitSum }
            $testhostLast = $testhostCommitSum
            if ($null -eq $testhostPeak -or $testhostCommitSum -gt $testhostPeak) { $testhostPeak = $testhostCommitSum; $testhostPeakAt = $stamp }
        } elseif ($testhostSeen) {
            $testhostGone++
        }

        if ($markerWriter -and ($sampleIndex % $LogCheckEverySamples) -eq 0) { Read-LogMarkers -MarkerWriter $markerWriter }
        $sampleIndex++

        if ($fixedEnd) {
            if ($now -ge $fixedEnd) { $stopReason = "duration elapsed"; break }
        } elseif ($testhostSeen -and $testhostGone -ge $TesthostGoneSamples) {
            $stopReason = "test host exited"; break
        } elseif (-not $testhostSeen -and $now -ge $waitDeadline) {
            $stopReason = "test host did not appear within $TesthostWaitTimeoutSeconds s"; break
        }

        $remaining = [int]($IntervalSeconds * 1000 - $watch.ElapsedMilliseconds)
        if ($remaining -gt 0) { Start-Sleep -Milliseconds $remaining }
    }
}
finally {
    if ($markerWriter) {
        Read-LogMarkers -MarkerWriter $markerWriter
        $markerWriter.Dispose()
    }
    $writer.Dispose()

    Write-Host ""
    Write-Host "Stopped: $stopReason. Samples: $sampleIndex. CSV: $OutputPath" -ForegroundColor Cyan
    foreach ($name in ($stats.Keys | Sort-Object)) {
        $e = $stats[$name]
        Write-Host ("  {0,-14} max WorkingSet {1,9} MB at {2} | max Commit {3,9} MB at {4}" -f $name, (ConvertTo-Mb $e.MaxWs), $e.MaxWsAt, (ConvertTo-Mb $e.MaxCommit), $e.MaxCommitAt)
    }
    if ($null -ne $testhostFirst) {
        $delta = ConvertTo-Mb ($testhostLast - $testhostFirst)
        Write-Host ("  Test host commit (sum): start {0} MB, end {1} MB, delta {2} MB, peak {3} MB at {4}" -f (ConvertTo-Mb $testhostFirst), (ConvertTo-Mb $testhostLast), $delta, (ConvertTo-Mb $testhostPeak), $testhostPeakAt) -ForegroundColor Green
    } else {
        Write-Host "  Test host never observed." -ForegroundColor Yellow
    }
}
