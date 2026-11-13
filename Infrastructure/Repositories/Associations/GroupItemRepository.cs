// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Linq.Expressions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupItemRepository : BaseRepository<GroupItem>, IGroupItemRepository
{
    private static readonly Expression<Func<GroupItem, bool>> IsRealShiftLink = gi =>
        gi.ShiftId != null
        && gi.AnalyseToken == null
        && gi.Shift != null
        && gi.Shift.AnalyseToken == null
        && gi.Shift.ScenarioSourceShiftId == null;

    public GroupItemRepository(DataBaseContext context, ILogger<GroupItem> logger)
        : base(context, logger)
    {
    }

    public async Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId == clientId && gi.GroupId == groupId)
            .Where(RegularMembership.IsRegular)
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
        foreach (var chunk in newStarts.Keys.Chunk(IdListQueryLimits.ChunkSize))
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
        foreach (var chunk in clientIds.Distinct().Chunk(IdListQueryLimits.ChunkSize))
        {
            var rows = await context.GroupItem
                .AsNoTracking()
                .Where(gi => gi.ClientId != null && chunk.Contains(gi.ClientId.Value)
                    && gi.ShiftId == null && gi.ValidUntil != null && !gi.IsDeleted)
                .Where(RegularMembership.IsRegular)
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
        foreach (var chunk in ids.Chunk(IdListQueryLimits.ChunkSize))
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
            .Where(gi => groupIdList.Contains(gi.GroupId))
            .Where(IsRealShiftLink)
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
            .Where(gi => groupIdList.Contains(gi.GroupId) && gi.ClientId != null)
            .Where(RegularMembership.IsRegular)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public Task<Guid?> FindShiftLinkIdAsync(Guid shiftId, Guid groupId, CancellationToken cancellationToken = default) =>
        context.GroupItem
            .Where(gi => gi.ShiftId == shiftId && gi.GroupId == groupId)
            .Select(gi => (Guid?)gi.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>> GetValidityRangesAsync(
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default)
        => GroupPeriodRangeQuery.LoadAsync(context, clientIds, groupIds, cancellationToken);

    public async Task<HashSet<Guid>> GetBorrowedOnlyClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default)
    {
        var result = new HashSet<Guid>();
        if (groupIds.Count == 0)
        {
            return result;
        }

        var groupIdList = groupIds.ToList();
        foreach (var chunk in clientIds.Distinct().Chunk(IdListQueryLimits.ChunkSize))
        {
            var regular = context.GroupItem
                .Where(gi => gi.ClientId != null && chunk.Contains(gi.ClientId.Value) && groupIdList.Contains(gi.GroupId))
                .Where(RegularMembership.IsRegular)
                .Select(gi => gi.ClientId!.Value);

            var borrowedOnly = await context.GroupItem
                .AsNoTracking()
                .Where(gi => gi.ClientId != null && chunk.Contains(gi.ClientId.Value) && groupIdList.Contains(gi.GroupId)
                    && gi.IsTemporary)
                .Where(RegularMembership.OrTemporary)
                .Select(gi => gi.ClientId!.Value)
                .Where(clientId => !regular.Contains(clientId))
                .Distinct()
                .ToListAsync(cancellationToken);

            result.UnionWith(borrowedOnly);
        }

        return result;
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
            .Where(gi => groupIds.Contains(gi.GroupId))
            .Where(IsRealShiftLink)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(IsRealShiftLink)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.Customer)
            .Where(RegularMembership.IsRegular)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetEmployeeCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.Employee)
            .Where(RegularMembership.IsRegular)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetExternEmpCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.ExternEmp)
            .Where(RegularMembership.IsRegular)
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
