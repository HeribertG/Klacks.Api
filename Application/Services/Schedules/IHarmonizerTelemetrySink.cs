// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Harmonizer.Telemetry;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Persistence target for per-run harmonizer telemetry. Implementations can log, write to
/// a file or stream into the autoresearch pipeline. The sink is invoked by HarmonizerJobRunner
/// after a run completes (success, cancel or failure all produce records — the consumer
/// decides which to keep).
/// </summary>
public interface IHarmonizerTelemetrySink
{
    Task RecordAsync(HarmonizerRunTelemetry telemetry, CancellationToken ct);
}
