// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Assembles the four hard-constraint input collections for the wizard context:
/// ScheduleCommands (per-day keywords), ShiftPreferences (master data), BreakBlockers (time-off),
/// LockedWorks (existing works with LockLevel>0). Filtered by agent ids, period and the scenario AnalyseToken.
/// </summary>
public interface IWizardHardConstraintBuilder
{
    /// <summary>
    /// Loads all four constraint sets for the given agents, period and scenario token.
    /// </summary>
    /// <param name="agentIds">Client ids to include</param>
    /// <param name="from">Period start (inclusive)</param>
    /// <param name="until">Period end (inclusive)</param>
    /// <param name="analyseToken">Scenario isolation token; null = main scenario</param>
    /// <param name="ct">Cancellation token</param>
    Task<HardConstraintResult> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct);
}

/// <summary>
/// Aggregated hard-constraint inputs for a wizard run.
/// </summary>
/// <param name="ScheduleCommands">Per-day planning keywords (FREE/EARLY/LATE/NIGHT and their negations)</param>
/// <param name="ShiftPreferences">Master-data preferences per client and shift</param>
/// <param name="BreakBlockers">Absence/break blockers (one entry per affected day)</param>
/// <param name="LockedWorks">Existing Work entities with LockLevel > 0</param>
public sealed record HardConstraintResult(
    IReadOnlyList<CoreScheduleCommand> ScheduleCommands,
    IReadOnlyList<CoreShiftPreference> ShiftPreferences,
    IReadOnlyList<CoreBreakBlocker> BreakBlockers,
    IReadOnlyList<CoreLockedWork> LockedWorks);
