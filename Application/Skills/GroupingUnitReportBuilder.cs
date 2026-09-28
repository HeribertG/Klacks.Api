// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the planning-unit summaries of a grouping report into the section the chat model presents: only
/// units in the caller's group scope (GroupingScopeVisibility), only units with a gap listed, the most
/// severe first (uncovered plus unfillable duties, then capacity shortfall days, then blocked employees,
/// then name), at most MaxListedPlanningUnits with totals over all visible units, and one blocking-cause
/// data line per listed unit whose dominant reason covers more than half of its employees
/// (reason=CODE employees=N/M unit="NAME" meaning="..."), which the model phrases in the user's language.
/// </summary>
/// <param name="report">Analysis with unit summaries and group names.</param>
/// <param name="scope">Caller's group scope; units outside it are neither listed nor counted.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupingUnitReportBuilder
{
    private const string BlockingCauseTemplate = "reason={0} employees={1}/{2} unit=\"{3}\" meaning=\"{4}\"";

    public static GroupingUnitSection Build(GroupingFeasibilityReport report, GroupScopeAccess scope)
    {
        var visible = report.UnitSummaries
            .Where(unit => GroupingScopeVisibility.IsGroupInScope(report, scope, unit.UnitId))
            .ToList();
        var withGaps = visible
            .Where(unit => unit.HasGaps)
            .OrderByDescending(unit => unit.DutiesUncoveredInUnit + unit.DutiesUnfillableGlobally)
            .ThenByDescending(unit => unit.CapacityGaps.Count)
            .ThenByDescending(unit => unit.DominantReasonEmployees)
            .ThenBy(unit => NameOf(report, unit), StringComparer.Ordinal)
            .ThenBy(unit => unit.UnitId)
            .ToList();
        var listed = withGaps.Take(GroupingFeasibilityDefaults.MaxListedPlanningUnits).ToList();

        return new GroupingUnitSection(
            listed.Select(unit => ToView(report, unit)).ToList(),
            new GroupingUnitTotals(visible.Count, withGaps.Count, withGaps.Count - listed.Count),
            listed
                .Where(unit => unit.DominantReason is not null)
                .Select(unit => string.Format(
                    CultureInfo.InvariantCulture, BlockingCauseTemplate,
                    unit.DominantReason, unit.DominantReasonEmployees, unit.EmployeesInScope, NameOf(report, unit),
                    GroupingReportTexts.Reasons[unit.DominantReason!.Value]))
                .ToList());
    }

    private static GroupingUnitView ToView(GroupingFeasibilityReport report, GroupingUnitSummary unit) => new(
        NameOf(report, unit),
        unit.DutiesAnalysed,
        unit.DutiesUncoveredInUnit,
        unit.DutiesUnfillableGlobally,
        unit.EmployeesInScope,
        unit.EmployeesWithoutContract,
        unit.DominantReason is { } reason ? GroupingReportTexts.Reasons[reason] : null,
        unit.DominantReasonEmployees,
        unit.CapacityGaps.Select(gap => new GroupingCapacityGapView(gap.Weekday.ToString(), gap.Demand, gap.Supply)).ToList());

    private static string NameOf(GroupingFeasibilityReport report, GroupingUnitSummary unit) =>
        report.GroupNames.GetValueOrDefault(unit.UnitId, string.Empty);
}
