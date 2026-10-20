// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure part of the group-level planning-period summary: counts, over the employees the schedule shows for a group, who has a
/// day of the checked range outside the employment (dark) and outside the combined group period (blue), with the grid bounds
/// inclusive and open ends open (an employee without a group item is never blue), ranks the starts of the memberships that
/// begin after the first checked day (most common, ties to the earlier date, and earliest), and counts the sealed employees per
/// day. A day of the range lies outside a period exactly when the range begins before the period or ends after it.
/// </summary>
/// <param name="groupName">Display name of the group</param>
/// <param name="from">First checked day</param>
/// <param name="until">Last checked day</param>
/// <param name="members">The employees the schedule shows for the group on those days</param>
/// <param name="sealedClientDays">Sealed (employee, day) pairs of the range</param>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class PlanningGroupSummaryBuilder
{
    public static PlanningGroupSummary Build(
        string groupName,
        DateOnly from,
        DateOnly until,
        IReadOnlyList<PlanningGroupMember> members,
        IReadOnlySet<(Guid ClientId, DateOnly Date)> sealedClientDays)
    {
        var outsideEmployment = members.Count(member => IsOutside(member.Employment.ValidFrom, member.Employment.ValidUntil, from, until));
        var lateStarts = members
            .Where(member => member.HasGroupItem && member.GroupFrom.HasValue && member.GroupFrom.Value > from)
            .Select(member => member.GroupFrom!.Value)
            .ToList();
        var earlyEnds = members.Count(member => member.HasGroupItem && member.GroupUntil.HasValue && member.GroupUntil.Value < until);
        var outsideGroup = members.Count(member =>
            member.HasGroupItem && IsOutside(member.GroupFrom, member.GroupUntil, from, until));

        var mostCommon = lateStarts
            .GroupBy(start => start)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault();

        return new PlanningGroupSummary(
            groupName,
            from,
            until,
            members.Count,
            outsideEmployment,
            outsideGroup,
            lateStarts.Count,
            earlyEnds,
            lateStarts.Count == 0 ? null : lateStarts.Min(),
            mostCommon?.Key,
            mostCommon?.Count() ?? 0,
            SealedPerDay(members, from, until, sealedClientDays));
    }

    private static bool IsOutside(DateOnly? periodFrom, DateOnly? periodUntil, DateOnly from, DateOnly until) =>
        (periodFrom.HasValue && from < periodFrom.Value) || (periodUntil.HasValue && until > periodUntil.Value);

    private static List<PlanningSealedDayCount> SealedPerDay(
        IReadOnlyList<PlanningGroupMember> members,
        DateOnly from,
        DateOnly until,
        IReadOnlySet<(Guid ClientId, DateOnly Date)> sealedClientDays)
    {
        var memberIds = members.Select(member => member.ClientId).ToHashSet();
        return sealedClientDays
            .Where(pair => memberIds.Contains(pair.ClientId) && pair.Date >= from && pair.Date <= until)
            .GroupBy(pair => pair.Date)
            .OrderBy(group => group.Key)
            .Select(group => new PlanningSealedDayCount(group.Key, group.Count()))
            .ToList();
    }
}
