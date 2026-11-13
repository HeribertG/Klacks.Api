// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Assembles the four hard-constraint input collections for the wizard context:
/// ScheduleCommands (per-day keywords), ShiftPreferences (master data), BreakBlockers (time-off),
/// LockedWorks (existing works with LockLevel>0). Filtered by agent ids, period and the scenario AnalyseToken.
/// Also names the seats that fixed works of persons outside the run already hold (LoadForeignFixedSeatsAsync).
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

    /// <summary>
    /// Loads the slot (shift, date) of every fixed work that a person outside the run holds on one of the given shifts in
    /// the period, one entry per work. Fixed means LockLevel above None or one of the works the apply paths protect
    /// (CorrectedWorkQuery.LoadFixedWorkIdsAsync: replacement covers, corrected and moved works). Reads only works of exactly
    /// the given scenario token (null = real plan), like the locked works of <see cref="BuildAsync"/>; unlike its existing-work
    /// blockers, a scenario run does not add real-plan works here, because they never sit on the scenario's shift ids. An
    /// unlocked work on a day sealed for its employee is not counted (the apply keeps it; known gap, agents alike).
    /// </summary>
    /// <param name="agentIds">Client ids the run plans; their works are left out (they become locked tokens)</param>
    /// <param name="shiftIds">Shift ids of the run's slots</param>
    /// <param name="from">Period start (inclusive)</param>
    /// <param name="until">Period end (inclusive)</param>
    /// <param name="analyseToken">Scenario isolation token; null = main scenario</param>
    /// <param name="ct">Cancellation token</param>
    Task<IReadOnlyList<(Guid ShiftId, DateOnly Date)>> LoadForeignFixedSeatsAsync(
        IReadOnlyList<Guid> agentIds,
        IReadOnlyCollection<Guid> shiftIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct);
}
