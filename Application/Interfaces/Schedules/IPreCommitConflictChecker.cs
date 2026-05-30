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
}
