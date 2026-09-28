// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Per-planning-unit numbers of one grouping analysis. Duties, employees, uncovered duties (F2), employees
/// without an active contract and the dominant blocking reason describe the real memberships before any
/// proposal; CapacityGaps are the unit's F7 findings, which count the proposed changes. A unit has gaps
/// when a duty is uncovered or unfillable, a weekday lacks capacity or a blocking reason dominates.
/// </summary>
/// <param name="UnitId">Planning unit (group with at least one direct duty).</param>
/// <param name="DutiesAnalysed">Analysed duties in the unit's scope, sub-groups included.</param>
/// <param name="DutiesUncoveredInUnit">Duties nobody in the unit's scope can take, although someone in the company can (F2).</param>
/// <param name="DutiesUnfillableGlobally">Duties of the unit no employee in the company can take (F1).</param>
/// <param name="EmployeesInScope">Analysed employees in the unit's scope.</param>
/// <param name="EmployeesWithoutContract">Employees in scope without any active contract day in the period.</param>
/// <param name="DominantReason">Blocking reason shared by more than half of the employees in scope who can take none of the unit's duties, or null.</param>
/// <param name="DominantReasonEmployees">Employees blocked by DominantReason.</param>
/// <param name="CapacityGaps">Weekdays with arithmetically too few suitable employees (F7), Monday first.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingUnitSummary(
    Guid UnitId,
    int DutiesAnalysed,
    int DutiesUncoveredInUnit,
    int DutiesUnfillableGlobally,
    int EmployeesInScope,
    int EmployeesWithoutContract,
    GroupingIneligibilityReason? DominantReason,
    int DominantReasonEmployees,
    IReadOnlyList<GroupingCapacityGap> CapacityGaps)
{
    public bool HasGaps =>
        DutiesUncoveredInUnit > 0 || DutiesUnfillableGlobally > 0 || CapacityGaps.Count > 0 || DominantReason is not null;
}
