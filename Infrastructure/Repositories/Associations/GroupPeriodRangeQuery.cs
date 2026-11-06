// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads per client the group period the schedule grid derives from its items in the given groups: the earliest
/// ValidFrom and the latest ValidUntil (GroupItemValidityRange). Only clients with at least one non-deleted item in those
/// groups get an entry; the group ids are matched exactly, not by subtree. Single source of the grid's locked cells,
/// the planning period guard and the wizards' closed days, so all three agree on which day is outside the group period.
/// </summary>
/// <param name="context">EF Core context holding the group items</param>
/// <param name="clientIds">Clients whose group items are read</param>
/// <param name="groupIds">Groups the items must belong to</param>

using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

internal static class GroupPeriodRangeQuery
{
    public static async Task<Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0 || groupIds.Count == 0)
        {
            return new Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>();
        }

        var clientIdList = clientIds.ToList();
        var groupIdList = groupIds.ToList();
        var items = await context.GroupItem
            .AsNoTracking()
            .Where(gi => !gi.IsDeleted
                         && gi.ClientId.HasValue
                         && clientIdList.Contains(gi.ClientId.Value)
                         && groupIdList.Contains(gi.GroupId))
            .Select(gi => new { gi.ClientId, gi.ValidFrom, gi.ValidUntil })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(gi => gi.ClientId!.Value)
            .ToDictionary(
                group => group.Key,
                group => GroupItemValidityRange.Combine(group.Select(gi => (gi.ValidFrom, gi.ValidUntil)).ToList()));
    }
}
