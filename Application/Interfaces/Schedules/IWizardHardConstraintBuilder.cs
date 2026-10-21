// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

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
    /// <param name="replanFrom">Frozen-prefix cut: every existing work dated before this day is
    /// returned as a locked work regardless of its LockLevel, so a replanning run keeps the head of
    /// the plan untouched and only the tail from this day on is planned. Null = no freeze</param>
    Task<HardConstraintResult> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct,
        DateOnly? replanFrom = null);
}
