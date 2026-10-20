// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure part of the planning-period diagnosis: evaluates every day of a range against the employee's employment window
/// and group membership periods exactly like the schedule grid does (isCellOutsideMembershipPeriod and
/// isCellOutsideGroupPeriod: inclusive bounds, a missing bound is open, an employee without a record is not blocked) and
/// attaches the day seals it is handed. The group period is the combined range of all memberships handed in (earliest
/// start to latest end, as GroupItemValidityRange.Combine does for the shown groups), not each group on its own. The seal sets come from the same repository calls the write guards use, so the
/// diagnosis cannot drift from them.
/// </summary>
/// <param name="person">Display name of the employee</param>
/// <param name="employment">Employment window, null without a membership record</param>
/// <param name="groups">Group membership periods the caller may see (already filtered to the requested group)</param>
/// <param name="hiddenGroupMembershipCount">Memberships in groups outside the caller's scope</param>
/// <param name="requestedGroupName">Group the question was limited to, null when none</param>
/// <param name="notMemberOfRequestedGroup">True when a group was named and the employee is no member of it</param>
/// <param name="from">First day of the range</param>
/// <param name="until">Last day of the range</param>
/// <param name="sealedDays">Days sealed for the employee (day approval or closed period)</param>
/// <param name="closedDays">Days of a closed period for the employee (a subset of sealedDays)</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class PlanningPeriodDiagnosisBuilder
{
    public static PlanningPeriodDiagnosis Build(
        string person,
        MembershipWindow? employment,
        IReadOnlyList<PlanningGroupMembershipPeriod> groups,
        int hiddenGroupMembershipCount,
        string? requestedGroupName,
        bool notMemberOfRequestedGroup,
        DateOnly from,
        DateOnly until,
        IReadOnlySet<DateOnly> sealedDays,
        IReadOnlySet<DateOnly> closedDays)
    {
        var (combinedFrom, combinedUntil) = Combine(groups);
        var days = new List<PlanningDayStatus>();
        for (var date = from; date <= until; date = date.AddDays(1))
        {
            days.Add(new PlanningDayStatus(
                date,
                employment is not null && !employment.Contains(date),
                groups.Count > 0 && IsOutside(combinedFrom, combinedUntil, date),
                groups.Where(group => IsOutside(group.From, group.Until, date)).Select(group => group.GroupName)
                    .OrderBy(name => name, StringComparer.Ordinal).ToList(),
                SealOf(date, sealedDays, closedDays)));
        }

        return new PlanningPeriodDiagnosis(
            person, employment, groups, hiddenGroupMembershipCount, requestedGroupName, notMemberOfRequestedGroup, days,
            combinedFrom, combinedUntil);
    }

    private static (DateOnly? From, DateOnly? Until) Combine(IReadOnlyList<PlanningGroupMembershipPeriod> groups)
    {
        if (groups.Count == 0)
        {
            return (null, null);
        }

        return (
            groups.Any(group => group.From is null) ? null : groups.Min(group => group.From),
            groups.Any(group => group.Until is null) ? null : groups.Max(group => group.Until));
    }

    private static bool IsOutside(DateOnly? from, DateOnly? until, DateOnly date) =>
        (from.HasValue && date < from.Value) || (until.HasValue && date > until.Value);

    private static WorkBookingSealKind? SealOf(DateOnly date, IReadOnlySet<DateOnly> sealedDays, IReadOnlySet<DateOnly> closedDays)
    {
        if (!sealedDays.Contains(date))
        {
            return null;
        }

        return closedDays.Contains(date) ? WorkBookingSealKind.PeriodClosed : WorkBookingSealKind.DayApproval;
    }
}
