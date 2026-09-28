// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Summarises every planning unit of one analysis so the report can state root causes instead of only
/// proposals. Scope, uncovered duties (F2), employees without an active contract and the dominant blocking
/// reason are read from the real memberships before any proposal; globally unfillable duties (F1) and the
/// capacity shortfalls (F7, which count the proposed changes) are taken from the findings. The dominant
/// reason is the most frequent per-employee reason among the employees who can take none of the unit's
/// duties (per employee: the reason blocking most of the unit's duties, a tie going to the lower enum value),
/// reported only when it covers more than half of the unit's employees. Units are the groups with a direct
/// duty plus every real group with a capacity finding, restricted to the focus subtree, ordered by name.
/// </summary>
/// <param name="tree">Real group hierarchy (without the virtual new group of the plan).</param>
/// <param name="state">Real memberships before any proposal.</param>
/// <param name="eligibility">Static eligibility of the analysis.</param>
/// <param name="findings">All findings of the analysis (F1 and F7 are read).</param>
/// <param name="focusGroupId">Optional subtree the analysis is restricted to.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupingUnitSummaryBuilder
{
    private const int MajorityDivisor = 2;

    public static IReadOnlyList<GroupingUnitSummary> Build(
        GroupingGroupTree tree,
        GroupingMembershipState state,
        IGroupingEligibilityOracle eligibility,
        IReadOnlyList<GroupingFinding> findings,
        Guid? focusGroupId)
    {
        var focus = focusGroupId is Guid focusId && tree.Contains(focusId) ? tree.SelfAndDescendants(focusId) : null;
        var unfillable = findings
            .Where(finding => finding.Code == GroupingFindingCode.ShiftUnfillableGlobally && finding.ShiftId is not null)
            .Select(finding => finding.ShiftId!.Value)
            .ToHashSet();
        var capacityByUnit = findings
            .Where(finding => finding.Code == GroupingFindingCode.CapacityShortfall
                && finding.GroupId is Guid groupId && tree.Contains(groupId)
                && finding.Weekday is not null)
            .GroupBy(finding => finding.GroupId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<GroupingCapacityGap>)group
                    .Select(finding => new GroupingCapacityGap(finding.Weekday!.Value, finding.Demand ?? 0, finding.Supply ?? 0))
                    .OrderBy(gap => IsoOrder(gap.Weekday))
                    .ToList());

        return state.PlanningUnits()
            .Concat(capacityByUnit.Keys)
            .Distinct()
            .Where(unit => tree.Contains(unit) && (focus is null || focus.Contains(unit)))
            .OrderBy(unit => tree.Get(unit).Name, StringComparer.Ordinal)
            .ThenBy(unit => unit)
            .Select(unit => Summarise(
                unit, tree, state, eligibility, unfillable, capacityByUnit.GetValueOrDefault(unit) ?? []))
            .ToList();
    }

    private static GroupingUnitSummary Summarise(
        Guid unit,
        GroupingGroupTree tree,
        GroupingMembershipState state,
        IGroupingEligibilityOracle eligibility,
        IReadOnlySet<Guid> unfillable,
        IReadOnlyList<GroupingCapacityGap> capacityGaps)
    {
        var shifts = state.ScopeShifts(unit, tree);
        var clients = state.ScopeClients(unit, tree);
        var uncovered = shifts.Count(shift =>
            !unfillable.Contains(shift) && !clients.Any(client => eligibility.IsEligible(client, shift)));
        var (dominant, dominantCount) = DominantReason(shifts, clients, eligibility);

        return new GroupingUnitSummary(
            unit,
            shifts.Count,
            uncovered,
            shifts.Count(unfillable.Contains),
            clients.Count,
            clients.Count(client => !eligibility.HasActiveContractInPeriod(client)),
            dominant,
            dominant is null ? 0 : dominantCount,
            capacityGaps);
    }

    private static (GroupingIneligibilityReason? Reason, int Count) DominantReason(
        IReadOnlySet<Guid> shifts, IReadOnlySet<Guid> clients, IGroupingEligibilityOracle eligibility)
    {
        if (shifts.Count == 0 || clients.Count == 0)
        {
            return (null, 0);
        }

        var top = clients
            .Where(client => !shifts.Any(shift => eligibility.IsEligible(client, shift)))
            .Select(client => BlockingReasonOf(client, shifts, eligibility))
            .GroupBy(reason => reason)
            .Select(group => (Reason: group.Key, Count: group.Count()))
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Reason)
            .FirstOrDefault();

        return top.Count * MajorityDivisor > clients.Count ? (top.Reason, top.Count) : (null, 0);
    }

    private static GroupingIneligibilityReason BlockingReasonOf(
        Guid client, IReadOnlySet<Guid> shifts, IGroupingEligibilityOracle eligibility)
    {
        if (!eligibility.HasActiveContractInPeriod(client))
        {
            return GroupingIneligibilityReason.NoActiveContract;
        }

        return shifts
            .Select(shift => eligibility.Evaluate(client, shift).Reason ?? GroupingIneligibilityReason.NoActiveContract)
            .GroupBy(reason => reason)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .First()
            .Key;
    }

    private static int IsoOrder(DayOfWeek weekday) => weekday == DayOfWeek.Sunday ? (int)DayOfWeek.Saturday + 1 : (int)weekday;
}
