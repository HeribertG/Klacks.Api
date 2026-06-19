// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Runs a synchronous wizard benchmark for training/measurement.
/// Same algorithm as the async Wizard/Start job, but returns all metrics directly
/// (no SignalR progress, no apply). The caller receives wall-clock time, final
/// fitness stages, token counts, coverage, and duplicate statistics in one call.
/// </summary>

public interface IWizardBenchmarkService
{
    /// <summary>
    /// Run a single benchmark with the supplied context. No DB persistence by default.
    /// </summary>
    Task<WizardBenchmarkResponse> RunAsync(WizardContextRequest request, CancellationToken ct);

    /// <summary>
    /// Run a single benchmark and persist the result as a WizardTrainingRun with the given source tag.
    /// Used by ad-hoc training runs and the background autotraining service.
    /// </summary>
    Task<WizardBenchmarkResponse> RunAndPersistAsync(
        WizardContextRequest request, string source, CancellationToken ct);

    /// <summary>
    /// Build a realistic context from the current Dev DB (first N clients / M shifts active
    /// in the period) and run a benchmark against it. Used by the background autotraining service
    /// so callers need no IDs. Returns null if the DB lacks data for a meaningful run.
    /// </summary>
    Task<WizardBenchmarkResponse?> RunAutoPopulatedAsync(
        int maxAgents,
        int maxShifts,
        DateOnly periodFrom,
        DateOnly periodUntil,
        WizardTrainingOverrides? overrides,
        string source,
        CancellationToken ct);
}
