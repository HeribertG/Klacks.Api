// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the membership windows of employees in a set of groups: a real-plan, non-deleted GroupItem of the client
/// for exactly that group (no subgroup cascade) intersected with the client's Membership. Shared by the group break
/// attribution (GroupBreakScope) and the day lock (SealedDayRepository) so that "member of the group on that day"
/// means the same in sealing, exporting and write protection. The day test itself is GroupMembershipWindow.IsActiveOn.
/// </summary>
/// <param name="context">EF Core context holding GroupItem and Membership</param>
/// <param name="groupIds">Groups whose own members are loaded</param>
/// <param name="clientIds">Optional restriction to these clients; null loads every member</param>
/// <param name="fromDate">First day (inclusive) a window must overlap</param>
/// <param name="untilDate">Last day (inclusive) a window must overlap</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class GroupMembershipWindowLoader
{
    public static async Task<List<(Guid GroupId, GroupMembershipWindow Window)>> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> groupIds,
        IReadOnlyCollection<Guid>? clientIds,
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0 || clientIds is { Count: 0 })
        {
            return [];
        }

        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilUtc = untilDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var groupIdList = groupIds.ToList();
        var clientIdList = clientIds?.ToList();

        var query =
            from groupItem in context.GroupItem
            join membership in context.Membership on groupItem.ClientId equals membership.ClientId
            where groupItem.ClientId != null
                && groupIdList.Contains(groupItem.GroupId)
                && !groupItem.IsDeleted
                && groupItem.AnalyseToken == null
                && groupItem.ScenarioSourceGroupItemId == null
                && !membership.IsDeleted
                && membership.ValidFrom <= untilUtc
                && (membership.ValidUntil == null || membership.ValidUntil >= fromUtc)
                && (groupItem.ValidFrom == null || groupItem.ValidFrom <= untilUtc)
                && (groupItem.ValidUntil == null || groupItem.ValidUntil >= fromUtc)
            select new { groupItem, membership };

        if (clientIdList is not null)
        {
            query = query.Where(row => clientIdList.Contains(row.groupItem.ClientId!.Value));
        }

        var rows = await query
            .Select(row => new
            {
                row.groupItem.GroupId,
                ClientId = row.groupItem.ClientId!.Value,
                row.membership.ValidFrom,
                MembershipUntil = row.membership.ValidUntil,
                GroupItemFrom = row.groupItem.ValidFrom,
                GroupItemUntil = row.groupItem.ValidUntil,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => (row.GroupId, new GroupMembershipWindow(
                row.ClientId,
                DateOnly.FromDateTime(row.ValidFrom),
                ToDate(row.MembershipUntil),
                ToDate(row.GroupItemFrom),
                ToDate(row.GroupItemUntil))))
            .Distinct()
            .ToList();
    }

    private static DateOnly? ToDate(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
}
