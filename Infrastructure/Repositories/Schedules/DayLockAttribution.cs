// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves which day seals (SealedDay rows) lock a client on a day. A global seal (GroupId null) locks every
/// client; a group seal locks a client who worked a shift of that group on that day or who is an active member of
/// that group on that day (GroupMembershipWindowLoader). Scenario rows never count: works and group items with an
/// analyse token, and group items cloned for a scenario, are ignored. Shared by the day lock (SealedDayRepository)
/// and the group unseal (BreakRepository), which keeps an absence sealed while another seal still locks its day.
/// </summary>
/// <param name="context">EF Core context holding SealedDay, Work, GroupItem and Membership</param>
/// <param name="pairs">The (date, client) pairs to resolve</param>
/// <param name="excludedGroupId">A group whose seals are ignored (the group being reopened), or null for none</param>
/// <param name="onlyLevel">Restricts to seals of this level (Closed for a period reopen), or null for every seal</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class DayLockAttribution
{
    /// <summary>
    /// Every (client, date) of the range that a day seal locks, for the wizards: such a cell is an immutable fixed
    /// cell (no placement, no hours), so an apply never runs into the day lock.
    /// </summary>
    public static async Task<HashSet<(Guid ClientId, DateOnly Date)>> LoadLockedClientDaysAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return [];
        }

        var sealedDates = await context.SealedDay
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= until)
            .Select(s => s.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sealedDates.Count == 0)
        {
            return [];
        }

        var pairs = sealedDates.SelectMany(date => clientIds.Select(clientId => (date, clientId))).ToList();
        var locking = await LoadLockingSealsAsync(context, pairs, null, cancellationToken);
        return locking.Keys.Select(k => (k.ClientId, k.Date)).ToHashSet();
    }

    public static async Task<Dictionary<(DateOnly Date, Guid ClientId), List<Guid?>>> LoadLockingSealsAsync(
        DataBaseContext context,
        IReadOnlyCollection<(DateOnly Date, Guid ClientId)> pairs,
        Guid? excludedGroupId,
        CancellationToken cancellationToken,
        WorkLockLevel? onlyLevel = null)
    {
        var result = new Dictionary<(DateOnly Date, Guid ClientId), List<Guid?>>();
        if (pairs.Count == 0)
        {
            return result;
        }

        var dates = pairs.Select(p => p.Date).Distinct().ToList();
        var clientIds = pairs.Select(p => p.ClientId).Distinct().ToList();

        var seals = (await context.SealedDay
                .AsNoTracking()
                .Where(s => dates.Contains(s.Date) && (onlyLevel == null || s.Level == onlyLevel))
                .Select(s => new { s.Date, s.GroupId })
                .ToListAsync(cancellationToken))
            .Where(s => s.GroupId == null || s.GroupId != excludedGroupId)
            .Distinct()
            .ToList();

        if (seals.Count == 0)
        {
            return result;
        }

        var globallySealedDates = seals.Where(s => s.GroupId == null).Select(s => s.Date).ToHashSet();
        var groupSealsByDate = seals
            .Where(s => s.GroupId != null)
            .ToLookup(s => s.Date, s => s.GroupId!.Value);
        var sealedGroupIds = groupSealsByDate.SelectMany(g => g).Distinct().ToList();

        var workedGroups = new HashSet<(DateOnly Date, Guid ClientId, Guid GroupId)>();
        var windowsByGroupAndClient = new Dictionary<(Guid GroupId, Guid ClientId), List<GroupMembershipWindow>>();

        if (sealedGroupIds.Count > 0)
        {
            var worked = await context.Work
                .AsNoTracking()
                .Where(w => !w.IsDeleted
                    && w.AnalyseToken == null
                    && clientIds.Contains(w.ClientId)
                    && dates.Contains(w.CurrentDate))
                .SelectMany(w => context.GroupItem
                    .Where(gi => gi.ShiftId == w.ShiftId
                        && !gi.IsDeleted
                        && gi.AnalyseToken == null
                        && gi.ScenarioSourceGroupItemId == null
                        && sealedGroupIds.Contains(gi.GroupId))
                    .Select(gi => new { w.CurrentDate, w.ClientId, gi.GroupId }))
                .Distinct()
                .ToListAsync(cancellationToken);
            workedGroups = worked.Select(w => (w.CurrentDate, w.ClientId, w.GroupId)).ToHashSet();

            var windows = await GroupMembershipWindowLoader.LoadAsync(
                context, sealedGroupIds, clientIds, dates.Min(), dates.Max(), cancellationToken);
            windowsByGroupAndClient = windows
                .GroupBy(entry => (entry.GroupId, entry.Window.ClientId))
                .ToDictionary(g => g.Key, g => g.Select(entry => entry.Window).ToList());
        }

        foreach (var (date, clientId) in pairs.Distinct())
        {
            var locking = new List<Guid?>();
            if (globallySealedDates.Contains(date))
            {
                locking.Add(null);
            }

            foreach (var groupId in groupSealsByDate[date])
            {
                var isMember = windowsByGroupAndClient.TryGetValue((groupId, clientId), out var clientWindows)
                    && clientWindows.Any(window => window.IsActiveOn(date));
                if (isMember || workedGroups.Contains((date, clientId, groupId)))
                {
                    locking.Add(groupId);
                }
            }

            if (locking.Count > 0)
            {
                result[(date, clientId)] = locking;
            }
        }

        return result;
    }
}