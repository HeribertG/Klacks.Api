// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single definition of which absences (Break rows) belong to a group for a day, shared by the group-scoped
/// period seal/unseal, the sealing summary and the payroll export so that what a group close seals is exactly what its
/// export reads. A break belongs to the group when the employee worked a shift of the group on that day (the
/// original rule, which also covers borrowed staff who are not members) or when the employee is an active
/// member of the group on that day: a real-plan, non-deleted GroupItem of the client for exactly this group (no
/// subgroup cascade) whose validity and the client's Membership both cover the day (GroupMembershipWindow).
/// The member rule exists so that absences on days without any work - vacation, sickness, on-call duty without
/// a call-out - are sealed and exported with the group too.
/// The day test runs in memory through GroupMembershipWindow.IsActiveOn and the result is handed back to SQL as
/// a list of break ids, so no DateOnly/DateTime comparison has to be translated by Npgsql.
/// </summary>
/// <param name="context">EF Core context holding GroupItem, Membership, Break and Work</param>
/// <param name="groupId">The group whose own members are resolved (no subgroups)</param>
/// <param name="fromDate">First day (inclusive) of the break range</param>
/// <param name="untilDate">Last day (inclusive) of the break range</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class GroupBreakScope
{
    public static async Task<List<Guid>> LoadMemberBreakIdsAsync(
        DataBaseContext context,
        Guid groupId,
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken)
    {
        var windows = await GroupMembershipWindowLoader.LoadAsync(
            context, [groupId], null, fromDate, untilDate, cancellationToken);

        if (windows.Count == 0)
        {
            return [];
        }

        var windowsByClient = windows
            .Select(entry => entry.Window)
            .ToLookup(window => window.ClientId);

        var memberClientIds = windowsByClient.Select(g => g.Key).ToList();

        var candidates = await context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate
                && memberClientIds.Contains(b.ClientId))
            .Select(b => new { b.Id, b.ClientId, b.CurrentDate })
            .ToListAsync(cancellationToken);

        return candidates
            .Where(c => windowsByClient[c.ClientId].Any(window => window.IsActiveOn(c.CurrentDate)))
            .Select(c => c.Id)
            .ToList();
    }

    public static IQueryable<Break> WhereAttributedToGroup(
        this IQueryable<Break> breaks,
        DataBaseContext context,
        Guid groupId,
        IReadOnlyCollection<Guid> memberBreakIds)
    {
        return breaks.Where(b => memberBreakIds.Contains(b.Id)
            || context.Work.Any(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.ClientId == b.ClientId
                && w.CurrentDate == b.CurrentDate
                && context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId
                    && gi.GroupId == groupId
                    && !gi.IsDeleted
                    && gi.AnalyseToken == null
                    && gi.ScenarioSourceGroupItemId == null)));
    }

    /// <summary>
    /// The breaks a group's period reopen may lift: every break this group's close sealed (Break.SealedByGroupId),
    /// even when the employee is no longer attributed to the group, and - for seals without a recorded owner
    /// (global seals and rows sealed before ownership existed) - only the breaks the original work-based rule
    /// attributes to the group. The membership rule is deliberately not applied to owner-less seals: those were
    /// never sealed by this group, and lifting them would leave an absence open under a seal that still stands.
    /// A break another group sealed is never lifted.
    /// </summary>
    public static IQueryable<Break> WhereReopenableByGroup(
        this IQueryable<Break> breaks,
        DataBaseContext context,
        Guid groupId)
    {
        return breaks.Where(b => b.SealedByGroupId == groupId
            || (b.SealedByGroupId == null
                && context.Work.Any(w => !w.IsDeleted
                    && w.AnalyseToken == null
                    && w.ClientId == b.ClientId
                    && w.CurrentDate == b.CurrentDate
                    && context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId
                        && gi.GroupId == groupId
                        && !gi.IsDeleted
                        && gi.AnalyseToken == null
                        && gi.ScenarioSourceGroupItemId == null))));
    }
}