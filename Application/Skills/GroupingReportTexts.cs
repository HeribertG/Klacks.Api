// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plain English sentences the grouping report carries instead of internal codes, so the model can
/// phrase the report in the user's language without ever seeing or repeating an enum name. The only codes
/// are the blocking-cause data lines, which carry the reason's meaning next to the code. Per planning unit
/// the text states the gaps, and whenever a gap needs master data (unfillable duties, capacity shortfalls, a
/// dominant blocking reason such as missing contracts) it says explicitly that group changes do not fix it.
/// </summary>
/// <param name="view">Report view whose counts, period, per-kind proposal counts, plan code and planning units the summary states.</param>
/// <param name="recomputed">Report view recomputed after an apply, whose remaining gaps the apply result states.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class GroupingReportTexts
{
    public const string NewGroup = "(new group, name still to be chosen)";

    public const string MasterDataSentence =
        " The proposed group changes do NOT resolve duties no employee can take, capacity shortfalls or missing contracts "
        + "and qualifications; these must be fixed in the master data (contracts, qualifications) before planning can start.";
    public const string RemainingGapsSentence =
        " These remaining gaps are not solved by the group changes: they require master-data changes (contracts, qualifications), "
        + "so do not tell the user that the duties can now be planned.";
    public const string NoRemainingGapsSentence =
        " The recomputed check finds no uncovered or unfillable duty and no capacity shortfall in the planning units "
        + "(capacity is checked arithmetically only: hours, rest days and absences are not considered).";
    public const string RecomputeFailedSentence =
        " The remaining gaps could not be recomputed; run the group plannability check again before telling the user "
        + "whether the duties can be planned.";

    private const string UnitsHeaderTemplate = " Planning units with gaps: {0} of {1}.";
    private const string UnitsNotListedTemplate = " {0} further planning units with gaps are not listed.";
    private const string UnitTemplate =
        " Planning unit \"{0}\": {1} duties analysed, {2} duties nobody in the unit can take with the current group memberships, "
        + "{3} duties no employee in the company can take; {4} employees in the unit, {5} of them without an active contract in the period.";
    private const string UnitCapacityTemplate =
        " Arithmetically too few suitable employees in \"{0}\" (counting the proposed group changes) on {1}.";
    private const string CapacityGapTemplate = "{0} (demand {1}, available {2})";
    private const string BlockingCausesTemplate = " Blocking causes (data; phrase them for the user): {0}.";
    private const string RemainingTemplate =
        " Recomputed after the change: {0} duties no employee in the company can take, {1} weekdays with a capacity shortfall, "
        + "{2} further proposed group changes.";
    private const string ItemSeparator = ", ";
    private const string LineSeparator = "; ";

    private const string SummaryTemplate =
        "Group plannability check for the period {0}: {1} duties no employee in the company can take, {2} employees who fit no duty, "
        + "{3} weekdays on which a group has arithmetically too few suitable employees, {4} proposed group changes: "
        + "{5} new groups, {6} duties added to a group, {7} employees added to a group, {8} removals of employees from a group. "
        + "Nothing was changed.";
    private const string PlanCodeSentence = " The code of this plan is {0}.";
    private const string CapacityNote =
        " Capacity findings are arithmetic only: enough people is necessary, not sufficient (hours, rest days and absences are not considered).";

    public static readonly IReadOnlyDictionary<(GroupingFindingCode Code, bool ReportOnly), string> Findings =
        new Dictionary<(GroupingFindingCode, bool), string>
        {
            [(GroupingFindingCode.ShiftUnfillableGlobally, true)] = "No employee in the whole company can take this duty",
            [(GroupingFindingCode.ShiftUncoveredInGroup, false)] = "Nobody in this group can take this duty; an employee from another group is proposed",
            [(GroupingFindingCode.ShiftUncoveredInGroup, true)] = "Nobody in this group can take this duty and no suitable employee was found elsewhere",
            [(GroupingFindingCode.ShiftWithoutGroup, false)] = "Duty without any group; a group is proposed",
            [(GroupingFindingCode.ClientWithoutGroup, false)] = "Employee without any group; a group with fitting duties is proposed",
            [(GroupingFindingCode.ClientWithoutGroup, true)] = "Employee without any group; no group has a duty this employee can take",
            [(GroupingFindingCode.ClientFitsNoShift, true)] = "Employee who can take no duty at all",
            [(GroupingFindingCode.ClientDeadMembership, false)] = "Member of a group in which the employee can take no duty; removal from this group is proposed",
            [(GroupingFindingCode.ClientDeadMembership, true)] = "Member of a group in which the employee can take no duty; kept because of upcoming assignments there or because it is the only group",
            [(GroupingFindingCode.CapacityShortfall, true)] = "Arithmetically too few suitable employees at the busiest time of this weekday",
        };

    public static readonly IReadOnlyDictionary<GroupingIneligibilityReason, string> Reasons =
        new Dictionary<GroupingIneligibilityReason, string>
        {
            [GroupingIneligibilityReason.NoActiveContract] = "no active contract on the days the duty runs",
            [GroupingIneligibilityReason.WeekdayNotAllowed] = "the contract does not allow the weekdays on which the duty runs",
            [GroupingIneligibilityReason.NotShiftWorker] = "does not do shift work and the duty is not an early duty",
            [GroupingIneligibilityReason.MandatoryQualificationMissing] = "a mandatory qualification is missing or has expired",
            [GroupingIneligibilityReason.Blacklisted] = "has excluded this duty",
            [GroupingIneligibilityReason.Unavailable] = "marked as unavailable during the duty's hours",
        };

    public static readonly IReadOnlyDictionary<GroupingProposalKind, string> Actions =
        new Dictionary<GroupingProposalKind, string>
        {
            [GroupingProposalKind.CreateGroup] = "create a new group",
            [GroupingProposalKind.AddShift] = "add the duty to the group",
            [GroupingProposalKind.AddClient] = "add the employee to the group",
            [GroupingProposalKind.RemoveClient] = "remove the employee from the group",
        };

    public static string Summary(GroupingReportView view)
    {
        var text = string.Format(
            CultureInfo.InvariantCulture, SummaryTemplate,
            view.Period, view.Counts.UnfillableShifts, view.Counts.UnmatchedClients,
            view.Counts.CapacityShortfalls, view.Counts.Proposals,
            view.ProposalCounts.NewGroups, view.ProposalCounts.DutiesAdded,
            view.ProposalCounts.EmployeesAdded, view.ProposalCounts.EmployeesRemoved);
        if (view.Counts.CapacityShortfalls > 0)
        {
            text += CapacityNote;
        }

        text += UnitSection(view.PlanningUnits);
        if (NeedsMasterData(view))
        {
            text += MasterDataSentence;
        }

        if (view.Counts.Proposals > 0)
        {
            text += string.Format(CultureInfo.InvariantCulture, PlanCodeSentence, view.PlanCode);
        }

        return text;
    }

    public static string RemainingGaps(GroupingReportView recomputed)
    {
        var text = string.Format(
            CultureInfo.InvariantCulture, RemainingTemplate,
            recomputed.Counts.UnfillableShifts, recomputed.Counts.CapacityShortfalls, recomputed.Counts.Proposals)
            + UnitSection(recomputed.PlanningUnits);
        var hasGaps = recomputed.PlanningUnits.Totals.UnitsWithGaps > 0
            || recomputed.Counts.UnfillableShifts > 0
            || recomputed.Counts.CapacityShortfalls > 0;
        return text + (hasGaps ? RemainingGapsSentence : NoRemainingGapsSentence);
    }

    private static bool NeedsMasterData(GroupingReportView view) =>
        view.Counts.UnfillableShifts > 0
        || view.Counts.CapacityShortfalls > 0
        || view.PlanningUnits.Units.Any(unit =>
            unit.DutiesNobodyInCompanyCanTake > 0 || unit.CapacityShortfalls.Count > 0 || unit.DominantBlockingReason is not null);

    private static string UnitSection(GroupingUnitSection section)
    {
        if (section.Units.Count == 0)
        {
            return string.Empty;
        }

        var text = string.Format(
            CultureInfo.InvariantCulture, UnitsHeaderTemplate, section.Totals.UnitsWithGaps, section.Totals.PlanningUnits);
        foreach (var unit in section.Units)
        {
            text += string.Format(
                CultureInfo.InvariantCulture, UnitTemplate,
                unit.Unit, unit.DutiesAnalysed, unit.DutiesNobodyInUnitCanTake, unit.DutiesNobodyInCompanyCanTake,
                unit.EmployeesInScope, unit.EmployeesWithoutActiveContract);
            if (unit.CapacityShortfalls.Count > 0)
            {
                text += string.Format(
                    CultureInfo.InvariantCulture, UnitCapacityTemplate, unit.Unit,
                    string.Join(ItemSeparator, unit.CapacityShortfalls.Select(gap => string.Format(
                        CultureInfo.InvariantCulture, CapacityGapTemplate, gap.Weekday, gap.Demand, gap.Supply))));
            }
        }

        if (section.Totals.UnitsNotListed > 0)
        {
            text += string.Format(CultureInfo.InvariantCulture, UnitsNotListedTemplate, section.Totals.UnitsNotListed);
        }

        if (section.BlockingCauses.Count > 0)
        {
            text += string.Format(
                CultureInfo.InvariantCulture, BlockingCausesTemplate, string.Join(LineSeparator, section.BlockingCauses));
        }

        return text;
    }
}
