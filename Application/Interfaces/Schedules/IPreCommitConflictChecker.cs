// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Tool-boundary guardrail: given one or more not-yet-committed work rows, reports which schedule
/// rule violations they would NEWLY introduce, by replaying the period validator over the affected
/// clients with and without the planned rows and diffing the result. Reuses the same engine as the
/// period-closing validator and detect_conflicts, so a value blocked here is consistent everywhere.
/// </summary>
public interface IPreCommitConflictChecker
{
    Task<PreCommitCheckResult> CheckAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same check, but the plan also vacates intervals. A swap gives one agent a shift and takes another
    /// away; judging only the additions reports collisions against work the plan is about to remove.
    /// </summary>
    /// <param name="plannedRows">Rows the plan would add.</param>
    /// <param name="removals">Intervals the plan would vacate.</param>
    /// <param name="analyseToken">Scenario token the plan lives in, null for the real plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PreCommitCheckResult> CheckAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        IReadOnlyList<PlannedRemovalRow> removals,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
