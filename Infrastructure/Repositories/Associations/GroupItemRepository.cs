// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupItemRepository : BaseRepository<GroupItem>, IGroupItemRepository
{
    private const int IdChunkSize = 2000;

    public GroupItemRepository(DataBaseContext context, ILogger<GroupItem> logger)
        : base(context, logger)
    {
    }

    public async Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId == clientId && gi.GroupId == groupId && gi.AnalyseToken == null)
            .OrderBy(gi => gi.ValidUntil == null ? 0 : 1)
            .ThenByDescending(gi => gi.ValidFrom)
            .ThenBy(gi => gi.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CountExistingByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        var idList = ids.ToList();
        return await context.GroupItem
            .AsNoTracking()
            .Where(gi => idList.Contains(gi.Id) && !gi.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> PullForwardValidFromAsync(
        IReadOnlyDictionary<Guid, DateTime> newStarts, CancellationToken cancellationToken = default)
    {
        var changed = 0;
        foreach (var chunk in newStarts.Keys.Chunk(IdChunkSize))
        {
            var items = await context.GroupItem
                .Where(gi => chunk.Contains(gi.Id) && !gi.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var start = newStarts[item.Id];
                var saved = context.Entry(item).Property(gi => gi.ValidFrom).OriginalValue;
                if (saved.HasValue && saved.Value > start)
                {
                    item.ValidFrom = start;
                    changed++;
                }
            }
        }

        return changed;
    }

    public async Task<IReadOnlyList<(Guid ClientId, Guid GroupId, DateTime ValidUntil)>> GetEndedMembershipsAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        var result = new List<(Guid ClientId, Guid GroupId, DateTime ValidUntil)>();
        foreach (var chunk in clientIds.Distinct().Chunk(IdChunkSize))
        {
            var rows = await context.GroupItem
                .AsNoTracking()
                .Where(gi => gi.ClientId != null && chunk.Contains(gi.ClientId.Value)
                    && gi.ShiftId == null && gi.AnalyseToken == null && gi.ValidUntil != null && !gi.IsDeleted)
                .Select(gi => new { ClientId = gi.ClientId!.Value, gi.GroupId, ValidUntil = gi.ValidUntil!.Value })
                .ToListAsync(cancellationToken);

            result.AddRange(rows.Select(row => (row.ClientId, row.GroupId, row.ValidUntil)));
        }

        return result;
    }

    public async Task<Dictionary<Guid, DateTime?>> GetValidFromByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, DateTime?>();
        foreach (var chunk in ids.Chunk(IdChunkSize))
        {
            var rows = await context.GroupItem
                .AsNoTracking()
                .Where(gi => chunk.Contains(gi.Id) && !gi.IsDeleted)
                .Select(gi => new { gi.Id, gi.ValidFrom })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                result[row.Id] = row.ValidFrom;
            }
        }

        return result;
    }

    public async Task<int> RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        var idList = ids.ToList();
        var items = await context.GroupItem
            .Where(gi => idList.Contains(gi.Id))
            .ToListAsync(cancellationToken);

        context.GroupItem.RemoveRange(items);
        return items.Count;
    }

    public async Task<IReadOnlyList<LinkedShiftName>> GetLinkedShiftNamesAsync(
        IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        var groupIdList = groupIds.ToList();
        var shifts = await context.GroupItem
            .Where(gi => groupIdList.Contains(gi.GroupId) && gi.ShiftId != null)
            .Where(gi => gi.Shift != null)
            .Select(gi => new { gi.Shift!.Id, gi.Shift.Name, gi.Shift.Abbreviation })
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return shifts.Select(s => new LinkedShiftName(s.Id, s.Name, s.Abbreviation)).ToList();
    }

    public async Task<Dictionary<Guid, int>> CountRealClientRowsPerGroupAsync(
        IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        var groupIdList = groupIds.ToList();
        return await context.GroupItem
            .Where(gi => groupIdList.Contains(gi.GroupId) && gi.ClientId != null && gi.AnalyseToken == null)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public Task<Guid?> FindShiftLinkIdAsync(Guid shiftId, Guid groupId, CancellationToken cancellationToken = default) =>
        context.GroupItem
            .Where(gi => gi.ShiftId == shiftId && gi.GroupId == groupId)
            .Select(gi => (Guid?)gi.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>> GetValidityRangesAsync(
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default)
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

    public async Task<List<Guid>> GetGroupIdsByShiftId(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ShiftId == shiftId)
            .Select(gi => gi.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetShiftIdsByGroupIds(List<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ShiftId.HasValue)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.Customer)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetEmployeeCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.Employee)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetExternEmpCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.ExternEmp)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<List<Guid>> GetGroupTreeIdsForClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var clientGroupIds = await context.GroupItem
            .Where(gi => gi.ClientId == clientId)
            .Select(gi => gi.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (clientGroupIds.Count == 0)
            return [];

        var rootIds = await context.Group
            .Where(g => clientGroupIds.Contains(g.Id))
            .Select(g => g.Root ?? g.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await context.Group
            .Where(g => rootIds.Contains(g.Id) || (g.Root != null && rootIds.Contains(g.Root.Value)))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientVisibilityMembership?> GetVisibilityMembershipAsync(
        Guid clientId, CancellationToken cancellationToken = default)
    {
        var row = await context.Client
            .AsNoTracking()
            .Where(client => client.Id == clientId)
            .Select(client => new
            {
                HasAnyGroupItem = client.GroupItems.Any(),
                ActiveGroupIds = client.GroupItems
                    .Where(gi => gi.AnalyseToken == null)
                    .Select(gi => gi.GroupId)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ClientVisibilityMembership(row.HasAnyGroupItem, row.ActiveGroupIds.Distinct().ToList());
    }
}
