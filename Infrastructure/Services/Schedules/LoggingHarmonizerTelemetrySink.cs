// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Telemetry;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default telemetry sink. Writes one structured Information log entry per run summary plus
/// one Debug entry per row so structured-log queries can correlate the threshold value with
/// per-row outcomes for autoresearch ingestion.
/// </summary>
/// <param name="logger">Microsoft.Extensions.Logging logger</param>
public sealed class LoggingHarmonizerTelemetrySink : IHarmonizerTelemetrySink
{
    private readonly ILogger<LoggingHarmonizerTelemetrySink> _logger;

    public LoggingHarmonizerTelemetrySink(ILogger<LoggingHarmonizerTelemetrySink> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(HarmonizerRunTelemetry telemetry, CancellationToken ct)
    {
        _logger.LogInformation(
            "HarmonizerTelemetry summary jobId={JobId} from={From} until={Until} rows={RowCount} fitnessBefore={Before:F4} fitnessAfter={After:F4} delta={Delta:F4} threshold={Threshold:F2} generations={Generations} emergencyUnlocks={EmergencyUnlocks} durationMs={DurationMs}",
            telemetry.JobId,
            telemetry.PeriodFrom,
            telemetry.PeriodUntil,
            telemetry.RowCount,
            telemetry.InitialFitness,
            telemetry.FinalFitness,
            telemetry.FitnessDelta,
            telemetry.EmergencyThreshold,
            telemetry.GenerationsRun,
            telemetry.TotalEmergencyUnlocks,
            telemetry.DurationMs);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var row in telemetry.Rows)
            {
                _logger.LogDebug(
                    "HarmonizerTelemetry row jobId={JobId} agentId={AgentId} rowIndex={RowIndex} initial={Initial:F4} final={Final:F4} moves={Moves} emergency={Emergency}",
                    telemetry.JobId,
                    row.AgentId,
                    row.RowIndex,
                    row.InitialScore,
                    row.FinalScore,
                    row.MovesApplied,
                    row.EmergencyUnlockTriggered);
            }
        }

        return Task.CompletedTask;
    }
}
